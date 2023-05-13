using System.Diagnostics;
using System.Net;
using Hl7.Fhir.Model;
using Task = System.Threading.Tasks.Task;

namespace OpenMedStack.FhirServer.AcceptanceTests.StepDefinitions;

public partial class FeatureSteps
{
    [Given(@"a FHIR resource")]
    public void GivenAFhirResource()
    {
        _patient = new Patient
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = new List<HumanName> { new() { Family = "Doe", Given = new List<string> { "John" } } },
            Address =
            {
                new()
                {
                    Line = new List<string> { "Main Street 1" }, City = "New York", PostalCode = "12345"
                }
            },
            Contact =
            {
                new Patient.ContactComponent
                {
                    Telecom = new() { new ContactPoint { System = ContactPoint.ContactPointSystem.Url, Value = "123" } }
                }
            }
        };
    }

    [When(@"the resource is created")]
    public async System.Threading.Tasks.Task WhenTheResourceIsCreated()
    {
        var response = await _fhirClient.CreateAsync(_patient, _tokenSource.Token).ConfigureAwait(false);
        Assert.NotNull(response);

        _patient = response;
    }

    [When(@"the user registers it as a resource set")]
    public async Task WhenTheUserRegistersItAsAResourceSet()
    {
        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri("http://localhost/register"),
            Content = new FormUrlEncodedContent(new Dictionary<string, string> { ["key"] = _patient.GetKey()  })
        };
        var response = await _httpClient.SendAsync(request, _tokenSource.Token).ConfigureAwait(false);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Then(@"the resource is registered as a UMA resource")]
    // ReSharper disable once InconsistentNaming
    public async Task ThenTheResourceIsRegisteredAsAUMAResource()
    {
        await Task.Delay(TimeSpan.FromSeconds(Debugger.IsAttached ? 60 : 0.3)).ConfigureAwait(false);
        Assert.Equal(1, _map.MappedResourcesCount);
    }
}

internal static class ResourceExtensions
{
    public static string GetKey(this Resource resource)
    {
        return resource.HasVersionId
            ? $"{resource.TypeName}/{resource.Id}/_history/{resource.VersionId}"
            : $"{resource.TypeName}/{resource.Id}";
    }
}
