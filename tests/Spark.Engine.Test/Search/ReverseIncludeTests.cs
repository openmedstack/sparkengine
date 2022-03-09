// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace Spark.Engine.Test.Search
{
    using System;
    using Engine.Search.Model;
    using Xunit;

    public class ReverseIncludeTests
    {
        [Fact]
        public void TestParseValid()
        {
            var sut = ReverseInclude.Parse("Patient.actor");

            Assert.Equal("Patient", sut.ResourceType);
            Assert.Equal("actor", sut.SearchPath);
        }

        [Fact]
        public void TestParseValidLongerPath()
        {
            var sut = ReverseInclude.Parse("Provenance.target.patient");

            Assert.Equal("Provenance", sut.ResourceType);
            Assert.Equal("target.patient", sut.SearchPath);
        }

        [Fact]
        public void TestParseNull()
        {
            Assert.Throws<ArgumentNullException>(() => ReverseInclude.Parse(null));
        }

        [Fact]
        public void TestParseInvalid()
        {
            Assert.Throws<ArgumentException>(() => ReverseInclude.Parse("bla;foo"));
        }
    }
}