// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace Spark.Engine.Test.Core
{
    using System;
    using System.Collections.Generic;
    using Engine.Core;
    using Hl7.Fhir.Model;
    using Xunit;

    public class FhirPropertyIndexTests
    {
        private static readonly IFhirModel _fhirModel = new FhirModel();

        [Fact]
        public void TestGetIndex()
        {
            var index = new FhirPropertyIndex(_fhirModel, new List<Type> {typeof(Patient), typeof(Account)});
            Assert.NotNull(index);
        }

        [Fact]
        public void TestExistingPropertyIsFound()
        {
            var index = new FhirPropertyIndex(_fhirModel, new List<Type> {typeof(Patient), typeof(HumanName)});

            var pm = index.FindPropertyInfo("Patient", "name");
            Assert.NotNull(pm);

            pm = index.FindPropertyInfo("HumanName", "given");
            Assert.NotNull(pm);
        }

        [Fact]
        public void TestTypedNameIsFound()
        {
            var index = new FhirPropertyIndex(_fhirModel, new List<Type> {typeof(ClinicalImpression), typeof(Period)});

            var pm = index.FindPropertyInfo("ClinicalImpression", "effectivePeriod");
            Assert.NotNull(pm);
        }

        [Fact]
        public void TestNonExistingPropertyReturnsNull()
        {
            var index = new FhirPropertyIndex(_fhirModel, new List<Type> {typeof(Patient), typeof(Account)});

            var pm = index.FindPropertyInfo("TypeNotPresent", "subject");
            Assert.Null(pm);

            pm = index.FindPropertyInfo("Patient", "property_not_present");
            Assert.Null(pm);
        }
    }
}