// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Common
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.DataContracts.Common;
    using Microsoft.DocAsCode.MarkdownLite;

    public static class YamlHeaderParser
    {
        public static IEnumerable<MatchDetail> Select(string html)
        {
            if (string.IsNullOrEmpty(html)) return null;
            var parts = YamlHtmlPart.SplitYamlHtml(html);

            var details = parts.Select(SelectSingle).Where(o => o != null);
            return details != null ? details : Enumerable.Empty<MatchDetail>();
        }

        private static readonly List<string> RequiredProperties = new List<string> { Constants.PropertyName.Uid };

        private static MatchDetail SelectSingle(YamlHtmlPart part)
        {
            var doc = part.Doc;
            Dictionary<string, object> properties = null;
            Dictionary<string, object> overridenProperties = null;
            var node = doc.DocumentNode.SelectSingleNode("//yamlheader");
            if (node != null)
            {
                var content = StringHelper.HtmlDecode(node.InnerHtml);
                string message;
                
                if (!TryExtractProperties(content, RequiredProperties, out properties, out message))
                {
                    Logger.Log(LogLevel.Warning, message);
                    return null;
                }

                overridenProperties = RemoveRequiredProperties(properties, RequiredProperties);
                node.Remove();
            }

            string conceptual;
            using (var sw = new StringWriter())
            {
                doc.Save(sw);
                conceptual = sw.ToString();
            }

            return new MatchDetail
            {
                Id = properties[Constants.PropertyName.Uid].ToString(),
                StartLine = part.StartLine,
                EndLine = part.EndLine,
                Conceptual = conceptual,
                Properties = overridenProperties
            };
        }

        private static Dictionary<string, object> RemoveRequiredProperties(Dictionary<string, object> properties, IEnumerable<string> requiredProperties)
        {
            if (properties == null) return null;

            var overridenProperties = new Dictionary<string, object>(properties);
            foreach (var requiredProperty in RequiredProperties)
            {
                if (requiredProperty != null) overridenProperties.Remove(requiredProperty);
            }

            return overridenProperties;
        }

        /// <summary>
        /// Extract YAML format content from yaml header
        /// </summary>
        /// <param name="content">the whole matched yaml header</param>
        /// <param name="requiredProperties">The properties that should be specified</param>
        /// <param name="properties"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        private static bool TryExtractProperties(string content, IEnumerable<string> requiredProperties, out Dictionary<string, object> properties, out string message)
        {
            properties = new Dictionary<string, object>();
            message = string.Empty;
            if (string.IsNullOrEmpty(content)) return false;
            try
            {
                using (StringReader reader = new StringReader(content))
                {
                    properties = YamlUtility.Deserialize<Dictionary<string, object>>(reader);
                    string checkPropertyMessage;
                    bool checkPropertyStatus = CheckRequiredProperties(properties, requiredProperties, out checkPropertyMessage);
                    if (!checkPropertyStatus)
                    {
                        throw new InvalidDataException(checkPropertyMessage);
                    }

                    if (!string.IsNullOrEmpty(checkPropertyMessage))
                    {
                        message += checkPropertyMessage;
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                // Yaml header could be very long.. substring it
                content = content?.Split('\n').FirstOrDefault()?.Trim();
                message += $@"yaml header '{content}' is not in a valid YAML format: {e.Message}.";
                return false;
            }
        }

        private static bool CheckRequiredProperties(Dictionary<string, object> properties, IEnumerable<string> requiredKeys, out string message)
        {
            Dictionary<string, bool> requiredKeyExistence = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
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
                message = string.Format("Required properties {{{{{0}}}}} are not set. Note that keys are case insensitive.", string.Join(",", notExistsKeys.Select(s => s.Key)));
                return false;
            }

            message = string.Empty;
            return true;
        }
    }
}
