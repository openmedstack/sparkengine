// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace Spark.Engine.Test.Core
{
    using System.Linq;
    using Engine.Core;
    using Hl7.Fhir.Model;
    using Xunit;

    public class FhirModelTests
    {
        private static FhirModel _sut;

        public FhirModelTests() => _sut = new FhirModel();

        [Fact]
        public void TestCompartments()
        {
            var actual = _sut.FindCompartmentInfo(ResourceType.Patient);

            Assert.NotNull(actual);
            Assert.True(actual.ReverseIncludes.Any());
        }
    }
}