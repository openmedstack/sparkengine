// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

using FhirModel = Hl7.Fhir.Model;

namespace OpenMedStack.SparkEngine.Web.Tests.Formatters;

using System.IO;
using System.Net;
using System.Text;
using Core;
using Hl7.Fhir.Serialization;
using Microsoft.AspNetCore.Http;
using Utility;
using Web.Formatters;
using Xunit;
using Task = System.Threading.Tasks.Task;

public class ResourceJsonInputFormatterTests : FormatterTestBase
{
    private const string DEFAULT_CONTENT_TYPE = "application/json";

    [Theory]
    [InlineData("application/fhir+json", true)]
    [InlineData("application/fhir+json;fhirVersion=4", true)]
    [InlineData("application/json+fhir", true)]
    [InlineData("application/json", true)]
    [InlineData("application/*", false)]
    [InlineData("*/*", false)]
    [InlineData("text/json", true)]
    [InlineData("text/*", false)]
    [InlineData("text/xml", false)]
    [InlineData("application/xml", false)]
    [InlineData("application/some.entity+json", false)]
    [InlineData("application/some.entity+json;v=2", false)]
    [InlineData("application/some.entity+xml", false)]
    [InlineData("application/some.entity+*", false)]
    [InlineData("text/some.entity+json", false)]
    [InlineData("", false)]
    //[InlineData(null, false)]
    [InlineData("invalid", false)]
    public void CanRead_ReturnsTrueForSupportedContent(string contentType, bool expectedCanRead)
    {
        var formatter = GetInputFormatter();

        var contentBytes = Encoding.UTF8.GetBytes(
            "{ \"resourceType\": \"Patient\", \"id\": \"example\", \"active\": true }");
        var httpContext = GetHttpContext(contentBytes, contentType);

        var formatterContext = CreateInputFormatterContext(typeof(Hl7.Fhir.Model.Resource), httpContext);

        var result = formatter.CanRead(formatterContext);

        Assert.Equal(expectedCanRead, result);
    }

    [Fact]
    public void SupportedMediaTypes_DefaultMediaType_ReturnsApplicationFhirJson()
    {
        var formatter = GetInputFormatter();

        var mediaType = formatter.SupportedMediaTypes[0];

        Assert.Equal("application/fhir+json", mediaType);
    }

    [Fact]
    public async Task ReadAsync_RequestBody_IsBuffered_And_IsSeekable()
    {
        var formatter = GetInputFormatter();

        var fhirVersionMoniker = FhirVersionUtility.GetFhirVersionMoniker(FhirVersionMoniker.R5);
        var content = GetResourceFromFileAsString(
            Path.Combine("TestData", fhirVersionMoniker.ToString(), "patient-example.json"));
        var contentBytes = Encoding.UTF8.GetBytes(content);
        var httpContext = new DefaultHttpContext { Request = { ContentType = DEFAULT_CONTENT_TYPE, Body = new MemoryStream(contentBytes) } };

        var formatterContext = CreateInputFormatterContext(typeof(Hl7.Fhir.Model.Resource), httpContext);

        var result = await formatter.ReadAsync(formatterContext);

        Assert.False(result.HasError);

        var patient = Assert.IsType<Hl7.Fhir.Model.Patient>(result.Model);
        Assert.Equal("example", patient.Id);
        Assert.Equal(true, patient.Active);

        Assert.True(httpContext.Request.Body.CanSeek);
        httpContext.Request.Body.Seek(0L, SeekOrigin.Begin);

        // Try again

        result = await formatter.ReadAsync(formatterContext);

        Assert.False(result.HasError);

        patient = Assert.IsType<Hl7.Fhir.Model.Patient>(result.Model);
        Assert.Equal("example", patient.Id);
        Assert.Equal(true, patient.Active);
    }

    [Fact]
    public async Task ReadAsync_ThrowsSparkException_BadRequest_OnNonUtf8Content()
    {
        var formatter = GetInputFormatter();

        var content = "ɊɋɌɍɎɏ";
        var contentBytes = Encoding.Unicode.GetBytes(content);

        var httpContext = GetHttpContext(contentBytes, DEFAULT_CONTENT_TYPE);

        var formatterContext = CreateInputFormatterContext(typeof(Hl7.Fhir.Model.Resource), httpContext);

        var exception = await Assert.ThrowsAsync<SparkException>(() => formatter.ReadAsync(formatterContext));
        Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
    }

    private static AsyncResourceJsonInputFormatter GetInputFormatter(ParserSettings? parserSettings = null)
    {
        parserSettings ??= new ParserSettings {PermissiveParsing = false};
        return new AsyncResourceJsonInputFormatter(new FhirJsonParser(parserSettings));
    }
}
