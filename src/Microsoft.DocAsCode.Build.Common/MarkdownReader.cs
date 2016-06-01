// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Common
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.IO;
    using System.Linq;

    using Microsoft.DocAsCode.DataContracts.Common;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Utility;
    using Microsoft.DocAsCode.Plugins;

    public class MarkdownReader
    {
        public static IEnumerable<OverwriteDocumentModel> ReadMarkdownAsOverwrite(IHostService host, FileAndType ft)
        {
            // Order the list from top to bottom
            var markdown = File.ReadAllText(ft.FullPath);
            var parts = MarkupMultiple(host, markdown, ft);
            return parts.Select(part => ReadMarkDownCore(ft.FullPath, part));
        }

        public static Dictionary<string, object> ReadMarkdownAsConceptual(string baseDir, string file)
        {
            var filePath = Path.Combine(baseDir, file);
            var repoInfo = GitUtility.GetGitDetail(filePath);
            return new Dictionary<string, object>
            {
                [Constants.PropertyName.Conceptual] = File.ReadAllText(filePath),
                [Constants.PropertyName.Type] = "Conceptual",
                [Constants.PropertyName.Source] = new SourceDetail() { Remote = repoInfo },
                [Constants.PropertyName.Path] = file,
            };
        }

        private static OverwriteDocumentModel ReadMarkDownCore(string filePath, YamlHtmlPart part)
        {
            var repoInfo = GitUtility.GetGitDetail(filePath);
            var yamlDetail = Select(part);

            return new OverwriteDocumentModel
            {
                Uid = yamlDetail.Id,
                Metadata = yamlDetail.Properties,
                Conceptual = part.Conceptual,
                Documentation = new SourceDetail
                {
                    Remote = repoInfo,
                    StartLine = part.StartLine,
                    EndLine = part.EndLine,
                    Path = part.SourceFile
                }
            };
        }

        private static IEnumerable<YamlHtmlPart> MarkupMultiple(IHostService host, string markdown, FileAndType ft)
        {
            try
            {
                var html = host.MarkupToHtml(markdown, ft);
                var parts = YamlHtmlPart.SplitYamlHtml(html);
                foreach (var part in parts)
                {
                    var mr = host.MarkupCore(part.OriginHtml, ft, true);
                    part.LinkToFiles = mr.LinkToFiles;
                    part.LinkToUids = mr.LinkToUids;
                    part.YamlHeader = mr.YamlHeader;
                }
                return parts;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Fail("Markup failed!");
                Logger.LogWarning($"Markup failed:{Environment.NewLine}  Markdown: {markdown}{Environment.NewLine}  Details:{ex.ToString()}");
                return Enumerable.Empty<YamlHtmlPart>();
            }
        }

        private static MatchDetail Select(YamlHtmlPart part)
        {
            if (part == null)
            {
                return null;
            }

            var properties = part.YamlHeader;
            string checkPropertyMessage;
            var checkPropertyStatus = CheckRequiredProperties(properties, RequiredProperties, out checkPropertyMessage);
            if (!checkPropertyStatus)
            {
                throw new InvalidDataException(checkPropertyMessage);
            }

            var overriden = RemoveRequiredProperties(properties, RequiredProperties);

            return new MatchDetail
            {
                Id = properties[Constants.PropertyName.Uid].ToString(),
                Properties = overriden
            };
        }

        private static readonly List<string> RequiredProperties = new List<string> { Constants.PropertyName.Uid };

        private static Dictionary<string, object> RemoveRequiredProperties(ImmutableDictionary<string, object> properties, IEnumerable<string> requiredProperties)
        {
            if (properties == null) return null;

            var overridenProperties = new Dictionary<string, object>(properties);
            foreach (var requiredProperty in requiredProperties)
            {
                if (requiredProperty != null) overridenProperties.Remove(requiredProperty);
            }

            return overridenProperties;
        }

        private static bool CheckRequiredProperties(ImmutableDictionary<string, object> properties, IEnumerable<string> requiredKeys, out string message)
        {
            var requiredKeyExistence = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            foreach (var requiredKey in requiredKeys)
            {
                bool current;
                if (!requiredKeyExistence.TryGetValue(requiredKey, out current))
                {
                    requiredKeyExistence.Add(requiredKey, false);
                }
            }

            foreach (var property in properties)
            {
                if (requiredKeyExistence.ContainsKey(property.Key))
                {
                    requiredKeyExistence[property.Key] = true;
                }
            }

            var notExistsKeys = requiredKeyExistence.Where(s => !s.Value);
            if (notExistsKeys.Any())
            {
                message =
                    $"Required properties {{{{{string.Join(",", notExistsKeys.Select(s => s.Key))}}}}} are not set. Note that keys are case insensitive.";
                return false;
            }

            message = string.Empty;
            return true;
        }
    }
}
