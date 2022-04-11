// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

using FhirModel = Hl7.Fhir.Model;

namespace OpenMedStack.SparkEngine.Web.Tests.Formatters
{
    using System.IO;
    using System.Text;
    using Hl7.Fhir.Serialization;
    using Microsoft.AspNetCore.Http;
    using Utility;
    using Web.Formatters;
    using Xunit;
    using Task = System.Threading.Tasks.Task;

    public class ResourceXmlInputFormatterTests : FormatterTestBase
    {
        private const string DEFAULT_CONTENT_TYPE = "application/xml";

        [Theory]
        [InlineData("application/fhir+xml", true)]
        [InlineData("application/xml+fhir", true)]
        [InlineData("application/xml", true)]
        [InlineData("application/*", false)]
        [InlineData("*/*", false)]
        [InlineData("text/xml", true)]
        [InlineData("text/*", false)]
        [InlineData("text/json", false)]
        [InlineData("application/json", false)]
        [InlineData("application/some.entity+xml", false)]
        [InlineData("application/some.entity+xml;v=2", false)]
        [InlineData("application/some.entity+json", false)]
        [InlineData("application/some.entity+*", false)]
        [InlineData("text/some.entity+xml", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        [InlineData("invalid", false)]
        public void CanRead_ReturnsTrueForSupportedContent(string contentType, bool expectedCanRead)
        {
            var formatter = GetInputFormatter();

            var contentBytes = Encoding.UTF8.GetBytes(
                "{ \"resourceType\": \"Patient\", \"id\": \"example\", \"active\": true }");
            var httpContext = GetHttpContext(contentBytes, contentType);

            var formatterContext = CreateInputFormatterContext(typeof(FhirModel.Resource), httpContext);

            var result = formatter.CanRead(formatterContext);

            Assert.Equal(expectedCanRead, result);
        }

        [Fact]
        public void SupportedMediaTypes_DefaultMediaType_ReturnsApplicationFhirXml()
        {
            var formatter = GetInputFormatter();

            var mediaType = formatter.SupportedMediaTypes[0];

            Assert.Equal("application/fhir+xml", mediaType);
        }

        [Fact]
        public async Task ReadAsync_RequestBody_IsBuffered_And_IsSeekable()
        {
            var formatter = GetInputFormatter();

            var fhirVersionMoniker = FhirVersionUtility.GetFhirVersionMoniker();
            var content = GetResourceFromFileAsString(
                Path.Combine("TestData", fhirVersionMoniker.ToString(), "patient-example.xml"));
            var contentBytes = Encoding.UTF8.GetBytes(content);
            var httpContext = new DefaultHttpContext { Request = { ContentType = DEFAULT_CONTENT_TYPE, Body = new MemoryStream(contentBytes) } };

            var formatterContext = CreateInputFormatterContext(typeof(FhirModel.Resource), httpContext);

            var result = await formatter.ReadAsync(formatterContext).ConfigureAwait(false);

            Assert.False(result.HasError);

            var patient = Assert.IsType<FhirModel.Patient>(result.Model);
            Assert.Equal("example", patient.Id);
            Assert.Equal(true, patient.Active);

            Assert.True(httpContext.Request.Body.CanSeek);
            httpContext.Request.Body.Seek(0L, SeekOrigin.Begin);

            // Try again

            result = await formatter.ReadAsync(formatterContext).ConfigureAwait(false);

            Assert.False(result.HasError);

            patient = Assert.IsType<FhirModel.Patient>(result.Model);
            Assert.Equal("example", patient.Id);
            Assert.Equal(true, patient.Active);
        }

        protected static AsyncResourceXmlInputFormatter GetInputFormatter(ParserSettings parserSettings = null)
        {
            parserSettings ??= new ParserSettings { PermissiveParsing = false };
            return new AsyncResourceXmlInputFormatter(new FhirXmlParser(parserSettings));
        }
    }
}