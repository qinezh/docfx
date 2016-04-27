// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.DocAsCode.Plugins;
    using Microsoft.DocAsCode.Utility;

    public class TemplateBundle
    {
        public string Extension { get; }

        public IEnumerable<TemplateResourceInfo> Resources { get; }

        public IEnumerable<Template> Templates { get; }

        public string DocumentType { get; }

        public TemplateBundle(string documentType, IEnumerable<Template> templates)
        {
            if (string.IsNullOrEmpty(documentType)) throw new ArgumentNullException(nameof(documentType));
            if (templates == null) throw new ArgumentNullException(nameof(templates));

            DocumentType = documentType;
            Templates = templates.ToArray();

            var defaultTemplate = Templates.FirstOrDefault(s => s.TemplateType == TemplateType.Primary)
                ?? Templates.FirstOrDefault(s=>s.TemplateType != TemplateType.Auxiliary);
            Extension = defaultTemplate?.Extension ?? string.Empty;
            Resources = Templates.SelectMany(s => s.Resources).Distinct();
        }

        public void FeedXRefMap(ManifestItem item, IDocumentBuildContext context)
        {
            if (item == null)
            {
                return;
            }

            foreach (var template in Templates)
            {
                if (!template.ContainsXrefRegistration) continue;
                var xref = template.GetXref(item.Model.Content);
                if (xref == null)
                {
                    continue;
                }

                foreach (var pair in xref)
                {
                    context.RegisterInternalXrefSpec(pair.Value);
                }
            }
        }

        public IDictionary<string, object> GetGlobalVariables(ManifestItem item, IDictionary<string, object> inputGlobalVariables, IDocumentBuildContext context)
        {
            if (item == null)
            {
                return inputGlobalVariables;
            }

            IDictionary<string, object> globalVariables = inputGlobalVariables == null ? new Dictionary<string, object>() : new Dictionary<string, object>(inputGlobalVariables);
            foreach (var template in Templates)
            {
                if (!template.ContainsGlobalRegistration) continue;
                globalVariables = template.GetGlobalVariables(globalVariables, item.Model.Content);
            }

            return globalVariables;
        }
    }
}
