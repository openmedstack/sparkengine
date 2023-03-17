namespace OpenMedStack.SparkEngine.Web.Tests;

using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Hl7.Fhir.Serialization;
using TechTalk.SpecFlow;
using Xunit;

[Binding]
public class PatientLifeCycleStepDefinitions
{
    private TestFhirServer _server;
    private FhirClient _client;
    private Patient _patient = null!;
    
    [Given(@"a running server")]
    public void GivenARunningServer()
    {
        _server = new TestFhirServer("https://localhost:60001");
    }

    [Given(@"a FHIR client")]
    public void GivenAFHIRClient()
    {
        _client = new FhirClient(
            "https://localhost:60001/fhir",
            new FhirClientSettings
            {
                ParserSettings = ParserSettings.CreateDefault(),
                PreferCompressedResponses = true,
                PreferredFormat = ResourceFormat.Json,
                UseFormatParameter = false,
                VerifyFhirVersion = false
            },
            messageHandler: _server.Server.CreateHandler());
    }

    [When(@"creating a patient resource")]
    public async System.Threading.Tasks.Task WhenCreatingAPatientResource()
    {
        _patient = await _client.CreateAsync(
                new Patient
                {
                    Active = true,
                    Name =
                    {
                        new HumanName
                        {
                            Family = "Tester", Given = new[] { "Anne" }, Use = HumanName.NameUse.Usual
                        }
                    }
                })
            .ConfigureAwait(false);
    }

    [Then(@"patient has id")]
    public void ThenPatientHasId()
    {
        Assert.NotNull(_patient.Id);
    }

    [Then(@"patient can be updated")]
    public async System.Threading.Tasks.Task ThenPatientCanBeUpdated()
    {
        _patient.BirthDateElement = new Date(1970, 1, 1);
        _patient = await _client.UpdateAsync(_patient).ConfigureAwait(false);

        Assert.NotNull(_patient.BirthDate);
    }

    [Then(@"can be found when searched")]
    public async System.Threading.Tasks.Task ThenCanBeFoundWhenSearched()
    {
        var p = await _client.SearchByIdAsync<Patient>(_patient.Id).ConfigureAwait(false);
        Assert.NotEmpty(p.Children);
    }
}