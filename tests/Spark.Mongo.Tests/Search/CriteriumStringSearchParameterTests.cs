// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace Spark.Mongo.Tests.Search
{
    using System.Linq;
    using Engine.Search.ValueExpressionTypes;
    using Hl7.Fhir.Model;
    using Hl7.Fhir.Utility;
    using Mongo.Search.Searcher;
    using MongoDB.Bson;
    using Spark.Search.Mongo;
    using Xunit;

    public class CriteriumStringSearchParameterTests
    {
        [Fact]
        public void StringQueryTest()
        {
            var resourceType = ResourceType.Subscription.GetLiteral();
            var searchParamName = "criteria";

            var criterium = Criterium.Parse(
                "criteria=Observation?patient.identifier=http://somehost.no/fhir/Name%20Hospital|someId");

            criterium.SearchParameters.AddRange(
                ModelInfo.SearchParameters.Where(p => p.Resource == resourceType && p.Name == searchParamName));

            var filter = criterium.ToFilter(resourceType);

            var jsonFilter = filter?.Render(null, null)?.ToJson();
            Assert.Equal(
                "{ \"criteria\" : /^Observation?patient.identifier=http:\\/\\/somehost.no\\/fhir\\/Name%20Hospital|someId/i }",
                jsonFilter);
        }
    }
}