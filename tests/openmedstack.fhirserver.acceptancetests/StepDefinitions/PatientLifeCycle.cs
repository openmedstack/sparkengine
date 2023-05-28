namespace OpenMedStack.FhirServer.AcceptanceTests.StepDefinitions;

using Hl7.Fhir.Model;

public partial class FeatureSteps
{
    [When(@"creating a patient resource")]
    public async System.Threading.Tasks.Task WhenCreatingAPatientResource()
    {
        _patient = (await _fhirClient.CreateAsync(
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
            .ConfigureAwait(false))!;
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
        _patient = (await _fhirClient.UpdateAsync(_patient).ConfigureAwait(false))!;

        Assert.NotNull(_patient.BirthDate);
    }

    [Then(@"can be found when searched")]
    public async System.Threading.Tasks.Task ThenCanBeFoundWhenSearched()
    {
        var p = await _fhirClient.SearchByIdAsync<Patient>(_patient.Id).ConfigureAwait(false);
        Assert.NotEmpty(p!.Children);
    }
}
