// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace Spark.Engine.Web.Tests.Utility
{
    using Engine.Utility;
    using Hl7.Fhir.Model;
    using Xunit;

    public class FhirPathUtilTests
    {
        [Fact]
        public void Can_Convert_FhirPathExpression_To_XPathExpression_Test()
        {
            var fhirPathExpression = "Patient.name[0].family";
            var expected = "f:Patient/f:name[0]/f:family";

            var actual = FhirPathUtil.ConvertToXPathExpression(fhirPathExpression);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Resolve_Patient_FamilyElement_Test()
        {
            var resourceType = typeof(Patient);
            var expression = "Name[0].FamilyElement";
            var expected = "Patient.name[0].family";

            var actual = FhirPathUtil.ResolveToFhirPathExpression(resourceType, expression);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Resolve_Questionnaire_ItemElement_Hierarchy_Test()
        {
            var resourceType = typeof(Questionnaire);
            var expression = ModelInfo.Version == "1.0.2"
                ? "Group.Question[3].Group[0].Question[3]"
                : "Item[0].Item[3].Item[0].Item[3]";
            var expected = ModelInfo.Version == "1.0.2"
                ? "Questionnaire.group.question[3].group[0].question[3]"
                : "Questionnaire.item[0].item[3].item[0].item[3]";

            var resolvedExpression = FhirPathUtil.ResolveToFhirPathExpression(resourceType, expression);

            Assert.Equal(expected, resolvedExpression);
        }

        [Fact]
        public void Resolve_Questionnaire_RequriedElement_In_ItemElement_Hierarchy_Test()
        {
            var resourceType = typeof(Questionnaire);
            var expression = ModelInfo.Version == "1.0.2"
                ? "Group.Question[3].Group[0].Question[3].RequiredElement"
                : "Item[0].Item[3].Item[0].Item[3].RequiredElement";
            var expected = ModelInfo.Version == "1.0.2"
                ? "Questionnaire.group.question[3].group[0].question[3].required"
                : "Questionnaire.item[0].item[3].item[0].item[3].required";

            var resolvedExpression = FhirPathUtil.ResolveToFhirPathExpression(resourceType, expression);

            Assert.Equal(expected, resolvedExpression);
        }

        [Fact]
        public void Resolve_Questionnaire_Initial_In_ItemElement_Hierarchy_Test()
        {
            var resourceType = typeof(Questionnaire);
            // NOTE: Initial does not exist in DSTU2
            var expression = ModelInfo.Version == "1.0.2"
                ? "Group.Question[3].Group[0].Question[3].TextElement"
                : "Item[0].Item[3].Item[0].Item[3].Initial";
            var expected = ModelInfo.Version == "1.0.2"
                ? "Questionnaire.group.question[3].group[0].question[3].text"
                : "Questionnaire.item[0].item[3].item[0].item[3].initial";

            var resolvedExpression = FhirPathUtil.ResolveToFhirPathExpression(resourceType, expression);

            Assert.Equal(expected, resolvedExpression);
        }

        [Fact]
        public void Resolve_Patient_Communication_Language_Test()
        {
            var resourceType = typeof(Patient);
            var expression = "Communication[0].Language";

            var resolvedExpression = FhirPathUtil.ResolveToFhirPathExpression(resourceType, expression);

            Assert.Equal("Patient.communication[0].language", resolvedExpression);
        }
    }
}