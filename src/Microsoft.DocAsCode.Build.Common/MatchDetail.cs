// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Common
{
    using System.Collections.Generic;

    public class MatchDetail
    {
        /// <summary>
        /// The Id from regular expression's content group, e.g. ABC from @ABC
        /// </summary>
        public string Id { get; set; }

        public Dictionary<string, object> Properties { get; set; } 

        public override int GetHashCode()
        {
            return string.IsNullOrEmpty(Id) ? string.Empty.GetHashCode() : Id.GetHashCode();
        }
    }
}
