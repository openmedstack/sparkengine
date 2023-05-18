namespace OpenMedStack.SparkEngine.Web.Tests;

using System;
using System.Net.Http;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Hl7.Fhir.Serialization;
using Microsoft.IdentityModel.Logging;
using Microsoft.Net.Http.Headers;
using Xunit;
using Xunit.Abstractions;
using Task = System.Threading.Tasks.Task;

public class FhirClientTests : IDisposable
{
    private readonly ITestOutputHelper _outputHelper;
    private readonly TestFhirServer _server;

    public FhirClientTests(ITestOutputHelper outputHelper)
    {
        IdentityModelEventSource.ShowPII = true;
        _outputHelper = outputHelper;
        _server = new TestFhirServer(outputHelper,"https://localhost:7266");
    }

    [Theory]
    [InlineData(ResourceFormat.Json)]
    [InlineData(ResourceFormat.Xml)]
    public async Task CanCreatePatientWithDifferentFormats(ResourceFormat format)
    {
        using var hc = new HttpClient(_server.Server.CreateHandler())
        {
            DefaultRequestHeaders =
            {
                {
                    HeaderNames.Authorization,
                    "Bearer eyJhbGciOiJSUzI1NiIsImtpZCI6IjEiLCJ0eXAiOiJKV1QifQ.eyJzY29wZSI6Im9wZW5pZCBwcm9maWxlIHVtYV9wcm90ZWN0aW9uIiwiYXpwIjoiZGF0YXN0dWRpbyIsInJvbGUiOiIiLCJzdWIiOiIyQzdBQUE1QzgyN0I3RjcxRkQwMjBBRTlCRDk2RjQ5ODgyQ0FFRDcxQTg2QTBFRTA4NTQ3RURDNjI0MTQ1MDIwIiwibmJmIjoxNjY4MTg5MTIwLCJleHAiOjE2NjgxOTA5MjAsImlhdCI6MTY2ODE4OTEyMCwiaXNzIjoiaHR0cHM6Ly9pZGVudGl0eS5yZWltZXJzLmRrIiwiYXVkIjoiZGF0YXN0dWRpbyJ9.jgB-TZXAaT3B9-clt-K5lGpR-LWJx5uyHzchWxIdJNAPvXTC2HYzN2hJO-_AtOjKUdJ2MIv8UIEwTLOq3UZ9uvSErEC5b8O35nAK1YB035jcLkbUem3292X1hfaAyCCP5sU3q6-PhgfjAEAepqqT1SKTwXBwoRu3izoUiqyDHnP_EjdBz-0jNUGNbh3lPb7Z6i56ynBsnB_F0vZ6A3i_4DHq6Is23rTEg2cwAHcsSrYRGePiWpJpvhdyvXF0gZ-cOiNf7JFpfVzYAJ5yKPLthyu9EePHIEC0ms60YW7Fiu1Ajumb3QA_h64GPmNscnYEGpbiqFKcAB5plFg3Hmlvww"
                }
            }
        };
        using var client = new FhirClient(
            "https://localhost:7266/fhir",
            hc,
            new FhirClientSettings
            {
                ParserSettings = ParserSettings.CreateDefault(),
                PreferCompressedResponses = true,
                PreferredFormat = format,
                UseFormatParameter = false,
                VerifyFhirVersion = false
            });
        client.Settings.ParserSettings!.ExceptionHandler = (_, args) => { _outputHelper.WriteLine($"{args.Severity}: {args.Message}"); };

        var patient = new Patient
        {
            Active = true,
            Name = { new HumanName
            {
                Family = "Tester",
                Given = new[]{"John"},
                Text = "John Tester",
                Use = HumanName.NameUse.Usual
            } }
        };

        var result = await client.CreateAsync(patient).ConfigureAwait(false);

        Assert.NotNull(result!.Id);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
