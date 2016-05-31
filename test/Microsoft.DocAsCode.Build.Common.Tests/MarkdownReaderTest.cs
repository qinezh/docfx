// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Common.Tests
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Xunit;

    using Microsoft.DocAsCode.Dfm;

    public class MarkdownReaderTest
    {
        [Fact]
        public void TestReadMarkdownAsOverwrite()
        {
            var content = @"---
uid: Test
remarks: Hello
---

This is unit test!";
            content = Regex.Replace(content, "/r?/n", "/r/n");
            var html = DocfxFlavoredMarked.Markup(content);
            const string FileName = "ut_ReadMarkdownAsOverwrite.md";
            File.WriteAllText(FileName, content);
            var results = MarkdownReader.ReadMarkDownCore(FileName, html).ToList();
            Assert.NotNull(results);
            Assert.Equal(1, results.Count);
            Assert.Equal("Test", results[0].Uid);
            Assert.Equal("Hello", results[0].Metadata["remarks"]);
            Assert.Equal("<p>This is unit test!</p>\n", results[0].Conceptual);
            File.Delete(FileName);

            // Test conceptual content between two yamlheader
            content = @"---
uid: Test1
remarks: Hello
---
This is unit test!

---
uid: Test2
---
";
            content = Regex.Replace(content, "/r?/n", "/r/n");
            html = DocfxFlavoredMarked.Markup(content);
            File.WriteAllText(FileName, content);
            results = MarkdownReader.ReadMarkDownCore(FileName, html).ToList();
            Assert.NotNull(results);
            Assert.Equal(2, results.Count);
            Assert.Equal("Test1", results[0].Uid);
            Assert.Equal("Test2", results[1].Uid);
            Assert.Equal("Hello", results[0].Metadata["remarks"]);
            Assert.Equal("<p>This is unit test!</p>\n", results[0].Conceptual);
            Assert.Equal(string.Empty, results[1].Conceptual);
            File.Delete(FileName);

            //invalid yamlheader is not supported
            content = @"---
uid: Test1
remarks: Hello
---
This is unit test!
---
uid: Test2
---
";
            content = Regex.Replace(content, "/r?/n", "/r/n");
            html = DocfxFlavoredMarked.Markup(content);
            File.WriteAllText(FileName, content);
            results = MarkdownReader.ReadMarkDownCore(FileName, html).ToList();
            Assert.NotNull(results);
            Assert.Equal(1, results.Count);
            Assert.Equal("Test1", results[0].Uid);
            Assert.Equal("Hello", results[0].Metadata["remarks"]);
            Assert.Equal("<h2 id=\"this-is-unit-test-\">This is unit test!</h2>\n<h2 id=\"uid-test2\">uid: Test2</h2>\n", results[0].Conceptual);
            File.Delete(FileName);

            // Test conceptual content with extra empty line between two yamlheader
            content = @"---
uid: Test1
remarks: Hello
---


This is unit test!


---
uid: Test2
---
";
            content = Regex.Replace(content, "/r?/n", "/r/n");
            html = DocfxFlavoredMarked.Markup(content);
            File.WriteAllText(FileName, content);
            results = MarkdownReader.ReadMarkDownCore(FileName, html).ToList();
            Assert.NotNull(results);
            Assert.Equal(2, results.Count);
            Assert.Equal("Test1", results[0].Uid);
            Assert.Equal("Test2", results[1].Uid);
            Assert.Equal("Hello", results[0].Metadata["remarks"]);
            Assert.Equal("<p>This is unit test!</p>\n", results[0].Conceptual);
            Assert.Equal(string.Empty, results[1].Conceptual);
            File.Delete(FileName);

            // Test different line ending
            content = "---\nuid: Test\nremarks: Hello\n---\nThis is unit test!\n";
            html = DocfxFlavoredMarked.Markup(content);
            File.WriteAllText(FileName, content);
            results = MarkdownReader.ReadMarkDownCore(FileName, html).ToList();
            Assert.NotNull(results);
            Assert.Equal(1, results.Count);
            Assert.Equal("Test", results[0].Uid);
            Assert.Equal("Hello", results[0].Metadata["remarks"]);
            Assert.Equal("<p>This is unit test!</p>\n", results[0].Conceptual);
            File.Delete(FileName);
        }
    }
}
