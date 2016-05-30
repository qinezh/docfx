// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Common
{
    using System.Collections.Generic;

    using HtmlAgilityPack;

    public class YamlHtmlPart
    {
        public HtmlDocument Doc { get; set; }
        public int StartLine { get; set; }
        public int EndLine { get; set; }

        public static IList<YamlHtmlPart> SplitYamlHtml(string html)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            var parts = new List<YamlHtmlPart>();

            var nodes = doc.DocumentNode.SelectNodes("//yamlheader");
            if (nodes == null)
            {
                return parts;
            }

            foreach (var node in nodes)
            {
                var part = new HtmlDocument();

                var startLineStr = node.GetAttributeValue("start", "-1");
                var endLineStr = node.GetAttributeValue("end", "-1");

                int startLine, endLine;
                if (!int.TryParse(startLineStr, out startLine))
                {
                    startLine = -1;
                }
                if (!int.TryParse(endLineStr, out endLine))
                {
                    endLine = -1;
                }

                var currentNode = node;

                do
                {
                    var nextNode = currentNode.NextSibling;
                    part.DocumentNode.AppendChild(currentNode);
                    currentNode = nextNode;
                } while (currentNode != null && currentNode.Name != "yamlheader");

                parts.Add(new YamlHtmlPart { Doc = part, StartLine = startLine, EndLine = endLine });
            }

            return parts;
        }
    }
}
