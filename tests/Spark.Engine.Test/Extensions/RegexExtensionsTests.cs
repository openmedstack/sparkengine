// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace Spark.Engine.Test.Extensions
{
    using System.Text.RegularExpressions;
    using Engine.Extensions;
    using Xunit;

    public class RegexExtensionsTests
    {
        public static readonly Regex Sut = new(@"[^a]*(?<alpha>a)[^a]*");

        [Fact]
        public void TestReplaceNamedGroupNoSuchGroup()
        {
            var input = @"bababa";
            var result = Sut.ReplaceGroup(input, "blabla", "c");
            Assert.Equal(@"bababa", result);
        }

        [Fact]
        public void TestReplaceNamedGroupNoCaptures()
        {
            var input = @"bbbbbb";
            var result = Sut.ReplaceGroup(input, "alpha", "c");
            Assert.Equal(@"bbbbbb", result);
        }

        [Fact]
        public void TestReplaceNamedGroupSingleCapture()
        {
            var input = @"babbbb";
            var result = Sut.ReplaceGroup(input, "alpha", "c");
            Assert.Equal(@"bcbbbb", result);
        }

        [Fact]
        public void TestReplaceNamedGroupMultipleCaptures()
        {
            var input = @"bababa";
            var result = Sut.ReplaceGroup(input, "alpha", "c");
            Assert.Equal(@"bcbcbc", result);
        }
    }
}