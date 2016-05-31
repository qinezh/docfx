// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Common
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Microsoft.DocAsCode.DataContracts.Common;
    using Microsoft.DocAsCode.Utility;
    using Microsoft.DocAsCode.Plugins;

    public class MarkdownReader
    {
        public static List<OverwriteDocumentModel> ReadMarkdownAsOverwrite(IHostService host, FileModel model)
        {
            // Order the list from top to bottom
            var file = model.FileAndType;
            var markdown = File.ReadAllText(file.FullPath);
            var mr = host.MarkupMultiple(markdown, file);

            ((HashSet<string>)model.Properties.LinkToFiles).UnionWith(mr.LinkToFiles);
            ((HashSet<string>)model.Properties.LinkToUids).UnionWith(mr.LinkToUids);

            return ReadMarkDownCore(file.FullPath, mr.Html).ToList();
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

        public static IEnumerable<OverwriteDocumentModel> ReadMarkDownCore(string file, string html)
        {
            var repoInfo = GitUtility.GetGitDetail(file);
            var yamlDetails = YamlHeaderParser.Select(html);

            foreach (var detail in yamlDetails)
            {
                yield return new OverwriteDocumentModel
                {
                    Uid = detail.Id,
                    Metadata = detail.Properties,
                    Conceptual = detail.Conceptual,
                    Documentation = new SourceDetail
                    {
                        Remote = repoInfo,
                        StartLine = detail.StartLine,
                        EndLine = detail.EndLine,
                        Path = Path.GetFullPath(file).ToDisplayPath()
                    }
                };
            }
        }
    }
}
