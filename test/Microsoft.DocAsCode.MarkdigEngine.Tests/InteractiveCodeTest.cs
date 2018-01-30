﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Tests
{
    using Xunit;

    public class InteractiveCodeTest
    {
        [Fact]
        [Trait("Related", "InteractiveCode")]
        public void TestInteractiveCode_CodeSnippetSimple()
        {
            var source = @"[!code-azurecli-interactive[](InteractiveCode/sample.code)]";
            var marked = TestUtility.MarkupWithoutSourceInfo(source, "Topic.md");

            Assert.Equal(
                @"<pre><code class=""lang-azurecli"" data-interactive=""azurecli"">hello world!
</code></pre>".Replace("\r\n", "\n"), marked.Html);
        }

        [Fact]
        [Trait("Related", "InteractiveCode")]
        public void TestInteractiveCode_FencedCodeSimple()
        {
            var source = @"```csharp-interactive
test
```";
            var marked = TestUtility.MarkupWithoutSourceInfo(source, "Topic.md");

            Assert.Equal(
                @"<pre><code class=""lang-csharp"" data-interactive=""csharp"">test
</code></pre>
".Replace("\r\n", "\n"), marked.Html);
        }
    }
}
