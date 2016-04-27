// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Jint;
    using Jint.Native;
    using Jint.Native.Object;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Utility;

    public class TemplateJintPreprocessor : ITemplatePreprocessor
    {
        private const string TransformFuncVariableName = "transform";
        private const string ConsoleVariableName = "console";
        private const string ExportsVariableName = "exports";
        private const string XrefFuncVariableName = "xref";
        private const string GlobalVariableFuncVariableName = "global";
        private const string ModelFuncVariableName = "model";

        private static readonly object ConsoleObject = new
        {
            log = new Action<object>(s => Logger.Log(s)),
            info = new Action<object>(s => Logger.LogInfo(s.ToString())),
            warn = new Action<object>(s => Logger.LogWarning(s.ToString())),
            err = new Action<object>(s => Logger.LogError(s.ToString())),
            error = new Action<object>(s => Logger.LogError(s.ToString())),
        };

        public FuncWithParams GetXrefFunc { get; }

        public FuncWithParams GetGlobalVariablesFunc { get; }

        public FuncWithParams TransformModelFunc { get; }

        public TemplateJintPreprocessor(string script)
        {
            if (!string.IsNullOrWhiteSpace(script))
            {
                var engine = new Engine();

                engine.SetValue(ConsoleVariableName, ConsoleObject);
                engine.SetValue(ExportsVariableName, engine.Object.Construct(Jint.Runtime.Arguments.Empty));

                engine.Execute(script);
                var value = engine.GetValue(ExportsVariableName);
                if (value.IsObject())
                {
                    var exports = value.AsObject();
                    var xrefFuncValue = exports.Get(XrefFuncVariableName);

                    GetXrefFunc = GetFunc(XrefFuncVariableName, exports);
                    GetGlobalVariablesFunc = GetFunc(GlobalVariableFuncVariableName, exports);
                    TransformModelFunc = GetFunc(ModelFuncVariableName, exports);
                }
                else
                {
                    throw new InvalidPreprocessorException("Invalid 'exports' variable definition. 'exports' MUST be an object.");
                }
            }
        }

        private static FuncWithParams GetFunc(string funcName, ObjectInstance exports)
        {
            var func = exports.Get(funcName);
            if (func.IsUndefined() || func.IsNull())
            {
                return null;
            }
            if (func.Is<ICallable>())
            {
                return args =>
                {
                    var model = args.Select(s => JintProcessorHelper.ConvertStrongTypeToJsValue(s)).ToArray();
                    return func.Invoke(model).ToObject();
                };
            }
            else
            {
                throw new InvalidPreprocessorException($"Invalid '{funcName}' variable definition. '{funcName} MUST be a function");
            }
        }
    }
}
