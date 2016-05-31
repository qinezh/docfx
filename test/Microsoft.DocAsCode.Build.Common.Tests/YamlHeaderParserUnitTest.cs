// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Common.Tests
{
    using System.Linq;

    using Xunit;

    [Trait("Owner", "lianwei")]
    [Trait("EntityType", "Parser")]
    public class YamlHeaderParserUnitTest
    {
        [Trait("Related", "YamlHeader")]
        [Fact]
        public void TestYamlHeaderParser()
        {
            var input = "<yamlheader>uid: Cat\nname: Tom</yamlheader>";
            var yamlHeaders = YamlHeaderParser.Select(input).ToList();
            Assert.Equal(1, yamlHeaders.Count);

            Assert.Equal("Cat", yamlHeaders[0].Id);
            Assert.Equal("Tom", yamlHeaders[0].Properties["name"].ToString());
            Assert.Equal(string.Empty, yamlHeaders[0].Conceptual);

            // --- should be start with uid
            input = "<yamlheader>id: Cat\nname: Tom</yamlheader>";
            yamlHeaders = YamlHeaderParser.Select(input).ToList();
            Assert.Equal(0, yamlHeaders.Count);

            // multi yamlheader test
            input = "<yamlheader>uid: Cat\nname: Tom</yamlheader><p>Conceptual</p><yamlheader>uid: Dog\nname: Jerry</yamlheader>";
            yamlHeaders = YamlHeaderParser.Select(input).ToList();
            Assert.Equal(2, yamlHeaders.Count);

            Assert.Equal("Cat", yamlHeaders[0].Id);
            Assert.Equal("Tom", yamlHeaders[0].Properties["name"].ToString());
            Assert.Equal("<p>Conceptual</p>", yamlHeaders[0].Conceptual);

            Assert.Equal("Dog", yamlHeaders[1].Id);
            Assert.Equal("Jerry", yamlHeaders[1].Properties["name"].ToString());
            Assert.Equal(string.Empty, yamlHeaders[1].Conceptual);
        }
    }
}
