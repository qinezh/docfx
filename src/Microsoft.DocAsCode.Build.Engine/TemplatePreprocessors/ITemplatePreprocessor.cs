// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    public interface ITemplatePreprocessor
    {
        FuncWithParams GetXrefFunc { get; }
        FuncWithParams GetGlobalVariablesFunc { get; }
        FuncWithParams TransformModelFunc { get; }
    }

    public delegate object FuncWithParams(params object[] args);
}
