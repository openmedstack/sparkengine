using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Task = System.Threading.Tasks.Task;

namespace OpenMedStack.FhirServer.AcceptanceTests.StepDefinitions;

public partial class FeatureSteps
{
    private readonly Dictionary<string, string> _idMap = new();
    private Bundle _bundle = null!;
    private string _errorMessage = string.Empty;

    [Given(@"I have a valid patient resource with the following parameters")]
    public async Task GivenIHaveAValidPatientResourceWithTheFollowingParameters(Table table)
    {
        foreach (var row in table.Rows)
        {
            var patient = new Patient
            {
                Id = row["id"],
                Name =
                {
                    new HumanName
                    {
                        Text = $"{row["first_name"]} {row["last_name"]}",
                        Given = new[] { row["first_name"] },
                        Family = row["last_name"]
                    }
                },
                Gender = Enum.Parse<AdministrativeGender>(row["gender"], true)
            };
            var p = await _fhirClient.CreateAsync(patient).ConfigureAwait(false);
            Assert.NotNull(p);
            _idMap.Add(row["id"], p.Id);
        }
    }

    [When(@"I send a GET request to /(.*)/(.*)")]
    public async Task WhenISendAgetRequestToPatient(string resource, string p0)
    {
        var result = await _fhirClient.ReadAsync<Patient>($"{resource}/{_idMap[p0]}").ConfigureAwait(false);
        _patient = Assert.IsType<Patient>(result);
    }

    [Then(@"the response should have a status code of (.*)")]
    public void ThenTheResponseShouldHaveAStatusCodeOf(int p0)
    {
        Assert.Equal(p0, int.Parse(_fhirClient.LastResult!.Status));
    }

    [Then(@"the response body should contain a patient resource with id (.*)")]
    public void ThenTheResponseBodyShouldContainAPatientResourceWithId(int p0)
    {
        Assert.Equal(p0, int.Parse(_idMap.First(x => x.Value == _patient.Id).Key));
    }

    [When(@"I send a GET request to /Patient with the following parameters")]
    public async Task WhenISendAgetRequestToPatientWithTheFollowingParameters(Table table)
    {
        var row = table.Rows[0];
        var p = SearchParams.FromUriParamList(new[]
        {
            Tuple.Create("given", $"{row["first_name"]}"),
            Tuple.Create("family", $"{row["last_name"]}"),
            Tuple.Create("gender", row["gender"]),
        });
        _bundle = Assert.IsType<Bundle>(await _fhirClient.SearchAsync<Patient>(p));
    }

    [Then(@"the response body should contain a bundle of patient resources that match the criteria")]
    public void ThenTheResponseBodyShouldContainABundleOfPatientResourcesThatMatchTheCriteria()
    {
        Assert.True(_bundle.GetResources().Any());
        Assert.True(_bundle.GetResources().All(r => r is Patient));
    }

    [When(@"a GET request is made to the FHIR API with an invalid token")]
    public async Task WhenAgetRequestIsMadeToTheFhirapiWithAnInvalidToken()
    {
        try
        {
            _fhirClient.RequestHeaders!.Authorization = new("Bearer", "invalid");
            _ = await _fhirClient.ReadAsync<Patient>($"Patient/blah").ConfigureAwait(false);
        }
        catch
        {
            // Intentionally left blank
        }
    }

    [Then(@"the response should contain an error message indicating authentication failure")]
    public void ThenTheResponseShouldContainAnErrorMessageIndicatingAuthenticationFailure()
    {
        var extension = _fhirClient.LastResult?.Extension[0].Value.ToString();
        Assert.EndsWith("error=\"invalid_token\"", extension);
    }

    [When(@"a GET request is made to the FHIR API with the invalid resource ID")]
    public async Task WhenAgetRequestIsMadeToTheFhirapiWithTheInvalidResourceId()
    {
        try
        {
            _ = await _fhirClient.ReadAsync<Patient>("Patient/blah").ConfigureAwait(false);
        }
        catch (FhirOperationException e)
        {
            _errorMessage = e.Message;
        }
    }

    [Then(@"the response should contain an error message indicating resource not found")]
    public void ThenTheResponseShouldContainAnErrorMessageIndicatingResourceNotFound()
    {
        Assert.False(string.IsNullOrWhiteSpace(_errorMessage));
    }

    [When(@"a GET request is made to the FHIR API with an invalid parameter and value")]
    public async Task WhenAgetRequestIsMadeToTheFhirapiWithAnInvalidParameterAndValue()
    {
        try
        {
            var p = SearchParams.FromUriParamList(new[] { Tuple.Create("blah", "blah") });
            _ = await _fhirClient.SearchAsync<Patient>(p).ConfigureAwait(false);
        }
        catch (FhirOperationException e)
        {
            _errorMessage = e.Message;
        }
    }

    [Then(@"the response should contain an error message indicating invalid search criteria")]
    public void ThenTheResponseShouldContainAnErrorMessageIndicatingInvalidSearchCriteria()
    {
        Assert.False(string.IsNullOrWhiteSpace(_errorMessage));
    }

    [When(@"a GET request is made to the FHIR API with incomplete request parameters")]
    public void WhenAgetRequestIsMadeToTheFhirapiWithIncompleteRequestParameters()
    {
        try
        {
            _fhirClient.ReadAsync<Patient>("Patient").ConfigureAwait(false);
        }
        catch (ArgumentException e)
        {
            _errorMessage = e.Message;
        }
    }

    [Then(@"the response should contain an error message indicating missing or invalid request parameters")]
    public void ThenTheResponseShouldContainAnErrorMessageIndicatingMissingOrInvalidRequestParameters()
    {
        Assert.Equal("Must be a FHIR REST url containing the resource type in its path (Parameter 'location')",
            _errorMessage);
    }
}
