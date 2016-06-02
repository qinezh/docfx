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
            var mrs = host.MarkupMultiple(markdown, ft);
            return mrs.Select(mr => GenerateOverwriteModel(ft.FullPath, mr));
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

        private static OverwriteDocumentModel GenerateOverwriteModel(string filePath, MarkupResult mr)
        {
            if (mr == null) return null;

            var properties = mr.YamlHeader;
            string checkPropertyMessage;
            var checkPropertyStatus = CheckRequiredProperties(properties, RequiredProperties, out checkPropertyMessage);
            if (!checkPropertyStatus)
            {
                throw new InvalidDataException(checkPropertyMessage);
            }

            var overriden = RemoveRequiredProperties(properties, RequiredProperties);
            var repoInfo = GitUtility.GetGitDetail(filePath);

            return new OverwriteDocumentModel
            {
                Uid = properties[Constants.PropertyName.Uid].ToString(),
                Metadata = overriden,
                Conceptual = mr.Html,
                Documentation = new SourceDetail
                {
                    Remote = repoInfo,
                    StartLine = mr.StartLine,
                    EndLine = mr.EndLine,
                    Path = mr.SourceFile
                }
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
