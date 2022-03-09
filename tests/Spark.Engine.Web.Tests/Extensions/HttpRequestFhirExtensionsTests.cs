// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

//using Spark.Engine.Extensions;
//using System;
//using System.Net.Http;
//using Hl7.Fhir.Rest;
//using Spark.Engine.Utility;
//using System.Linq;
//using Xunit;

//namespace Spark.Engine.Test.Extensions
//{
//    using Microsoft.AspNetCore.Http;
//    using Web.Extensions;

//    public class HttpRequestFhirExtensionsTests
//    {
//        [Fact]
//        public void TestGetDateParameter()
//        {
//            //CK: apparently only works well if you escape at least the '+' sign at the start of the offset (by %2B) .
//            var httpRequest = new HttpRequestMessage(HttpMethod.Get, new Uri("http://spark.furore.com/fhir/Encounter/_history?_since=2017-01-01T00%3A00%3A00%2B01%3A00", UriKind.Absolute));
//            var expected = new DateTimeOffset(2017, 1, 1, 0, 0, 0, new TimeSpan(1, 0, 0));
//            var actual = FhirParameterParser.ParseDateParameter(httpRequest.GetParameter("_since"));
//            Assert.Equal(expected, actual);
//        }

//        [Theory]
//        [InlineData("http://example.org/fhir/Patient", SummaryType.False)]                  // Default is SummaryType.False
//        [InlineData("http://example.org/fhir/Patient?_summary=text", SummaryType.Text)]     // _summary=text  is SummaryType.Text
//        [InlineData("http://example.org/fhir/Patient?_summary=data", SummaryType.Data)]     // _summary=data  is SummaryType.Data
//        [InlineData("http://example.org/fhir/Patient?_summary=true", SummaryType.True)]     // _summary=true  is SummaryType.True
//        [InlineData("http://example.org/fhir/Patient?_summary=false", SummaryType.False)]   // _summary=false is SummaryType.False
//        [InlineData("http://example.org/fhir/Patient?_summary=count", SummaryType.Count)]   // _summary=count is SummaryType.Count
//        [InlineData("http://example.org/fhir/Patient?_summary=", SummaryType.False)]        // _summary=      is SummaryType.False
//        public void RequestSummary_SummaryTypeDefaultIsFalse(string url, SummaryType expected)
//        {
//            var defaultHttpContext = new DefaultHttpContext();
//            HttpRequest request = defaultHttpContext.Request;// new HttpRequestMessage(HttpMethod.Get, url);
//            request.Method = HttpMethod.Get.Method;

//            var actual = request.RequestSummary();
//            Assert.Equal(expected, actual);
//        }

//        [Fact]
//        public void TupledParameters_WithMultipleIncludes()
//        {
//            var request = new HttpRequestMessage(HttpMethod.Get, "http://example.org/fhir/Patient?_include=A&_include=B");

//            var parameters = request.TupledParameters();

//            Assert.NotNull(parameters.FirstOrDefault(p => p.Equals(Tuple.Create("_include", "A"))));
//            Assert.NotNull(parameters.FirstOrDefault(p => p.Equals(Tuple.Create("_include", "B"))));
//        }

//        [Fact]
//        public void TupledParameters_MustPreserveComma()
//        {
//            var request = new HttpRequestMessage(HttpMethod.Get, "http://example.org/fhir/Patient?arg=A,B");

//            var parameters = request.TupledParameters();

//            Assert.NotNull(parameters.FirstOrDefault(p => p.Equals(Tuple.Create("arg", "A,B"))));
//        }
//    }
//}

