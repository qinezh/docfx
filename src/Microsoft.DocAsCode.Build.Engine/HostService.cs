﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Composition;
    using System.IO;
    using System.Linq;
    using System.Web;

    using HtmlAgilityPack;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.MarkdownLite;
    using Microsoft.DocAsCode.Plugins;
    using Microsoft.DocAsCode.Utility;

    [Export(typeof(IHostService))]
    public sealed class HostService : IHostService, IDisposable
    {
        #region Fields
        private readonly object _syncRoot = new object();
        private readonly Dictionary<string, List<FileModel>> _uidIndex = new Dictionary<string, List<FileModel>>();
        private readonly LruList<ModelWithCache> _lru = LruList<ModelWithCache>.CreateSynchronized(0xC00, OnLruRemoving);
        #endregion

        #region Properties

        public ImmutableList<FileModel> Models { get; private set; }

        public ImmutableDictionary<string, FileAndType> SourceFiles { get; set; }

        public Dictionary<FileAndType, FileAndType> FileMap { get; } = new Dictionary<FileAndType, FileAndType>();

        public IMarkdownService MarkdownService { get; set; }

        #endregion

        #region Constructors

        public HostService(string baseDir, IEnumerable<FileModel> models)
        {
            LoadCore(models);
        }

        #endregion

        #region IHostService Members

        public ImmutableList<FileModel> GetModels(DocumentType? type)
        {
            if (type == null)
            {
                return Models;
            }
            return (from m in Models where m.Type == type select m).ToImmutableList();
        }

        public ImmutableHashSet<string> GetAllUids()
        {
            lock (_syncRoot)
            {
                return _uidIndex.Keys.ToImmutableHashSet();
            }
        }

        public ImmutableList<FileModel> LookupByUid(string uid)
        {
            lock (_syncRoot)
            {
                List<FileModel> result;
                if (_uidIndex.TryGetValue(uid, out result))
                {
                    return result.ToImmutableList();
                }
                return ImmutableList<FileModel>.Empty;
            }
        }

        public MarkupResult Markup(string markdown, FileAndType ft)
        {
            try
            {
                var mrs = MarkupCore(markdown, ft);
                return mrs.First();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Fail("Markup failed!");
                Logger.LogWarning($"Markup failed:{Environment.NewLine}  Markdown: {markdown}{Environment.NewLine}  Details:{ex.ToString()}");
                return new MarkupResult { Html = markdown };
            }
        }

        public ImmutableList<MarkupResult> MarkupMultiple(string markdown, FileAndType ft)
        {
            try
            {
                return MarkupCore(markdown, ft).ToImmutableList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Fail("Markup failed!");
                Logger.LogWarning($"Markup failed:{Environment.NewLine}  Markdown: {markdown}{Environment.NewLine}  Details:{ex.ToString()}");
                return new List<MarkupResult>().ToImmutableList();
            }
        }

        private List<MarkupResult> MarkupCore(string markdown, FileAndType ft)
        {
            var html = MarkdownService.Markup(markdown, ft.File);
            return ParseHtml(html, ft);
        }

        public void LogVerbose(string message, string file, string line)
        {
            Logger.LogVerbose(message, file: file, line: line);
        }

        public void LogInfo(string message, string file, string line)
        {
            Logger.LogInfo(message, file: file, line: line);
        }

        public void LogWarning(string message, string file, string line)
        {
            Logger.LogWarning(message, file: file, line: line);
        }

        public void LogError(string message, string file, string line)
        {
            Logger.LogError(message, file: file, line: line);
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            foreach (var m in Models)
            {
                m.FileOrBaseDirChanged -= HandleFileOrBaseDirChanged;
                m.UidsChanged -= HandleUidsChanged;
            }
        }

        #endregion

        public void Reload(IEnumerable<FileModel> models)
        {
            lock (_syncRoot)
            {
                LoadCore(models);
            }
        }

        #region Private Methods

        private void LoadCore(IEnumerable<FileModel> models)
        {
            EventHandler fileOrBaseDirChangedHandler = HandleFileOrBaseDirChanged;
            EventHandler<PropertyChangedEventArgs<ImmutableArray<UidDefinition>>> uidsChangedHandler = HandleUidsChanged;
            EventHandler contentAccessedHandler = ContentAccessedHandler;
            if (Models != null)
            {
                foreach (var m in Models)
                {
                    m.FileOrBaseDirChanged -= fileOrBaseDirChangedHandler;
                    m.UidsChanged -= uidsChangedHandler;
                    m.ContentAccessed -= contentAccessedHandler;
                }
            }
            Models = models.ToImmutableList();
            _uidIndex.Clear();
            FileMap.Clear();
            foreach (var m in Models)
            {
                m.FileOrBaseDirChanged += fileOrBaseDirChangedHandler;
                m.UidsChanged += uidsChangedHandler;
                m.ContentAccessed += contentAccessedHandler;
                foreach (var uid in m.Uids)
                {
                    List<FileModel> list;
                    if (!_uidIndex.TryGetValue(uid.Name, out list))
                    {
                        list = new List<FileModel>();
                        _uidIndex.Add(uid.Name, list);
                    }
                    list.Add(m);
                }
                if (m.Type != DocumentType.Overwrite)
                {
                    FileMap[m.FileAndType] = m.FileAndType;
                }
            }
        }

        private void ResolveLinks(string html, FileAndType ft, out HashSet<string> linkToFiles, out IEnumerable<string> linkToUids)
        {
            var part = new HtmlDocument();
            part.LoadHtml(html);

            linkToFiles = new HashSet<string>();
            linkToUids = Enumerable.Empty<string>();

            foreach (var pair in (from n in part.DocumentNode.Descendants()
                                  where !string.Equals(n.Name, "xref", StringComparison.OrdinalIgnoreCase)
                                  from attr in n.Attributes
                                  where string.Equals(attr.Name, "src", StringComparison.OrdinalIgnoreCase) ||
                                        string.Equals(attr.Name, "href", StringComparison.OrdinalIgnoreCase)
                                  where !string.IsNullOrWhiteSpace(attr.Value)
                                  select new { Node = n, Attr = attr }).ToList())
            {
                string linkFile;
                string anchor = null;
                var link = pair.Attr;
                if (PathUtility.IsRelativePath(link.Value))
                {
                    var index = link.Value.IndexOf('#');
                    if (index == -1)
                    {
                        linkFile = link.Value;
                    }
                    else if (index == 0)
                    {
                        continue;
                    }
                    else
                    {
                        linkFile = link.Value.Remove(index);
                        anchor = link.Value.Substring(index);
                    }
                    var path = (RelativePath)ft.File + (RelativePath)linkFile;
                    var file = path.GetPathFromWorkingFolder();
                    if (SourceFiles.ContainsKey(file))
                    {
                        link.Value = file;
                        if (!string.IsNullOrEmpty(anchor) &&
                            string.Equals(link.Name, "href", StringComparison.OrdinalIgnoreCase))
                        {
                            pair.Node.SetAttributeValue("anchor", anchor);
                        }
                    }
                    linkToFiles.Add(HttpUtility.UrlDecode(file));
                    linkToUids = (from n in part.DocumentNode.Descendants()
                                  where string.Equals(n.Name, "xref", StringComparison.OrdinalIgnoreCase)
                                  from attr in n.Attributes
                                  where string.Equals(attr.Name, "href", StringComparison.OrdinalIgnoreCase) || string.Equals(attr.Name, "uid", StringComparison.OrdinalIgnoreCase)
                                  where !string.IsNullOrWhiteSpace(attr.Value)
                                  select attr.Value);
                }
            }
        }

        private List<MarkupResult> ParseHtml(string html, FileAndType ft)
        {
            var mrs = new List<MarkupResult>();
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var nodes = doc.DocumentNode.SelectNodes("//yamlheader");

            // Case one: HTML without <yamlheader>
            if (nodes == null)
            {
                var mr = new MarkupResult
                {
                    Html = html
                };
                HashSet<string> linkToFiles;
                IEnumerable<string> linkToUids;
                ResolveLinks(mr.Html, ft, out linkToFiles, out linkToUids);
                mr.LinkToFiles = linkToFiles.ToImmutableArray();
                mr.LinkToUids = linkToUids.ToImmutableHashSet();
                mrs.Add(mr);
                return mrs;
            }

            // Case two: HTML with one or more <yamlheader>
            foreach (var node in nodes)
            {
                var mr = new MarkupResult
                {
                    SourceFile = node.GetAttributeValue("sourceFile", "NotFound"),
                    StartLine = node.GetAttributeValue("start", -1),
                    EndLine = node.GetAttributeValue("end", -1)
                };

                using (var sr = new StringReader(StringHelper.HtmlDecode(node.InnerHtml)))
                {
                    mr.YamlHeader = YamlUtility.Deserialize<Dictionary<string, object>>(sr).ToImmutableDictionary();
                }
                mrs.Add(mr);
            }

            var startIndexes = nodes.Select(node => node.StreamPosition).Skip(1).ToList();
            startIndexes.Add(html.Length);
            var endIndexes = nodes.Select(node => node.StreamPosition + node.OuterHtml.Replace("\"", "").Length - 1).ToList();

            for (var i = 0; i < mrs.Count; i++)
            {
                mrs[i].Html = html.Substring(endIndexes[i] + 1, startIndexes[i] - endIndexes[i] - 1);

                HashSet<string> linkToFiles;
                IEnumerable<string> linkToUids;
                ResolveLinks(mrs[i].Html, ft, out linkToFiles, out linkToUids);

                mrs[i].LinkToFiles = linkToFiles.ToImmutableArray();
                mrs[i].LinkToUids = linkToUids.ToImmutableHashSet();
            }
            return mrs;
        }


        private void HandleUidsChanged(object sender, PropertyChangedEventArgs<ImmutableArray<UidDefinition>> e)
        {
            var m = sender as FileModel;
            if (m == null)
            {
                return;
            }
            lock (_syncRoot)
            {
                var common = e.Original.Select(s => s.Name).Intersect(e.Current.Select(s => s.Name)).ToList();
                foreach (var added in e.Current.Select(s => s.Name).Except(common))
                {
                    List<FileModel> list;
                    if (!_uidIndex.TryGetValue(added, out list))
                    {
                        list = new List<FileModel>();
                        _uidIndex.Add(added, list);
                    }
                    list.Add(m);
                }
                foreach (var removed in e.Original.Select(s => s.Name).Except(common))
                {
                    List<FileModel> list;
                    if (_uidIndex.TryGetValue(removed, out list))
                    {
                        list.Remove(m);
                        if (list.Count == 0)
                        {
                            _uidIndex.Remove(removed);
                        }
                    }
                }
            }
        }

        private void HandleFileOrBaseDirChanged(object sender, EventArgs e)
        {
            var m = sender as FileModel;
            if (m == null)
            {
                return;
            }
            lock (_syncRoot)
            {
                FileMap[m.OriginalFileAndType] = m.FileAndType;
            }
        }

        private void ContentAccessedHandler(object sender, EventArgs e)
        {
            _lru.Access((ModelWithCache)sender);
        }

        private static void OnLruRemoving(ModelWithCache m)
        {
            try
            {
                m.Serialize();
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Unable to serialize model, details:{ex.ToString()}", file: m.File);
            }
        }

        #endregion
    }
}
