// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace Spark.Engine.Test.Extensions
{
    using System.Collections.Generic;
    using Engine.Extensions;
    using Hl7.Fhir.Model;
    using Xunit;

    public class SearchParameterExtensionsTests
    {
        [Fact]
        public void TestSetPropertyPathWithSinglePath()
        {
            var sut = new SearchParameter {Base = new List<ResourceType?> {ResourceType.Appointment}};

            sut.SetPropertyPath(new[] {"Appointment.participant.actor"});

            Assert.Equal("//participant/actor", sut.Xpath);
        }

        [Fact]
        public void TestSetPropertyPathWithMultiplePath()
        {
            var sut = new SearchParameter {Base = new List<ResourceType?> {ResourceType.AuditEvent}};
            sut.SetPropertyPath(new[] {"AuditEvent.participant.reference", "AuditEvent.object.reference"});

            Assert.Equal("//participant/reference | //object/reference", sut.Xpath);
        }

        [Fact]
        public void TestGetPropertyPathWithSinglePath()
        {
            var sut = new SearchParameter {Xpath = "//participant/actor"};

            var paths = sut.GetPropertyPath();
            Assert.Single(paths);
            Assert.Contains("participant.actor", paths);
        }

        [Fact]
        public void TestGetPropertyPathWithMultiplePath()
        {
            var sut = new SearchParameter {Xpath = "//participant/reference | //object/reference"};

            var paths = sut.GetPropertyPath();
            Assert.Equal(2, paths.Length);
            Assert.Contains("participant.reference", paths);
            Assert.Contains("object.reference", paths);
        }

        [Fact]
        public void TestSetPropertyPathWithPredicate()
        {
            var sut = new SearchParameter {Base = new List<ResourceType?> {ResourceType.Slot}};
            sut.SetPropertyPath(new[] {"Slot.extension(url=http://foo.com/myextension).valueReference"});

            Assert.Equal("//extension(url=http://foo.com/myextension)/valueReference", sut.Xpath);
        }

        [Fact]
        public void TestGetPropertyPathWithPredicate()
        {
            var sut = new SearchParameter {Xpath = "//extension(url=http://foo.com/myextension)/valueReference"};

            var paths = sut.GetPropertyPath();
            Assert.Single(paths);
            Assert.Equal(@"extension(url=http://foo.com/myextension).valueReference", paths[0]);
        }

        [Fact]
        public void TestMatchExtension()
        {
            var input = "//extension(url=http://foo.com/myextension)/valueReference";
            var result = SearchParameterExtensions.XpathPattern.Match(input).Value;
            Assert.Equal(input, result);
        }
    }
}