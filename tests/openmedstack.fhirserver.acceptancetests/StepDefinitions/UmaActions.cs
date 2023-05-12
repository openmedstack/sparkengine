using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using DotAuth.Client;
using DotAuth.Shared;
using DotAuth.Shared.Responses;
using DotAuth.Uma;
using Hl7.Fhir.Model;
using OpenMedStack.SparkEngine.Core;
using OpenMedStack.SparkEngine.Interfaces;
using Task = System.Threading.Tasks.Task;

namespace OpenMedStack.FhirServer.AcceptanceTests.StepDefinitions;

public partial class FeatureSteps
{
    private GrantedTokenResponse _umaToken = null!;
    private Patient? _patientResource;
    private UmaTicketInfo _ticketInfo = null!;

    [Given(@"FHIR resource registered as a UMA resource set")]
    public void GivenFhirResourceRegisteredAsAumaResourceSet()
    {
        var fhirStore = _chassis.Resolve<IFhirStore>();
        fhirStore.Add(Entry.Post(Key.Create("Patient", "abc"), new Patient { Id = "abc" }));
    }

    [Given(@"a valid UMA token")]
    public async Task GivenAValidUmaToken()
    {
        var option = await _tokenClient.GetToken(TokenRequest.FromTicketId("123", "token")).ConfigureAwait(false);
        var grantedToken = Assert.IsType<Option<GrantedTokenResponse>.Result>(option);
        _umaToken = grantedToken.Item;
    }

    [Given(@"an invalid UMA token")]
    public async Task GivenAnInvalidUmaToken()
    {
        var option = await _tokenClient.GetToken(TokenRequest.FromScopes("read", "uma_protection")).ConfigureAwait(false);
        var grantedToken = Assert.IsType<Option<GrantedTokenResponse>.Result>(option);
        _umaToken = grantedToken.Item;
    }

    [When(@"the resource is requested without an id token")]
    public async Task WhenTheResourceIsRequestedWithoutAnIdToken()
    {
        try
        {
            _umaFhirClient.RequestHeaders!.Authorization =
                new AuthenticationHeaderValue("Bearer", _umaToken.AccessToken);
            _patientResource = await _umaFhirClient.ReadAsync<Patient>("Patient/abc").ConfigureAwait(false);
        }
        catch
        {
            // ignored
        }
    }

    [When(@"the resource is requested with an id token")]
    public async Task WhenTheResourceIsRequestedWithAnIdToken()
    {
        try
        {
            _umaFhirClient.RequestHeaders!.Authorization =
                new AuthenticationHeaderValue("Bearer", _umaToken.AccessToken);
            _umaFhirClient.RequestHeaders!.Add("id_token", _umaToken.IdToken);
            _patientResource = await _umaFhirClient.ReadAsync<Patient>("Patient/abc").ConfigureAwait(false);
        }
        catch
        {
            // ignored
        }
    }

    [Then(@"the resource is returned")]
    public void ThenTheResourceIsReturned()
    {
        Assert.NotNull(_patientResource);
    }

    [Then(@"an UMA error is returned")]
    public void ThenAnUmaErrorIsReturned()
    {
        Assert.Equal("403", _umaFhirClient.LastResult?.Status);
    }

    [Then(@"an UMA ticket is returned")]
    public void ThenAnUmaTicketIsReturned()
    {
        Assert.Equal("401", _umaFhirClient.LastResult?.Status);
        var extension = _umaFhirClient.LastResult!.Extension[0].Value;

        var result = TryParse(extension.ToString(), out var info);
        Assert.True(result);
        _ticketInfo = info!;
    }

    [Then(@"ticket can be used to get token")]
    public async Task ThenTicketCanBeUsedToGetToken()
    {
        var token = await _tokenClient.GetToken(TokenRequest.FromTicketId(_ticketInfo.TicketId, "")).ConfigureAwait(false);
        var grantedToken = Assert.IsType<Option<GrantedTokenResponse>.Result>(token);
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(grantedToken.Item.AccessToken);

        Assert.Contains(jwt.Claims, c => c.Type == "permissions");
    }

    [GeneratedRegex("as_uri=\"(?<umaAuthority>.+)\", ticket=\"(?<ticketId>.+)\"( realm=\"(?<realm>.+)\")?")]
    private static partial Regex ParseRegex();

    private static bool TryParse(string? header, out UmaTicketInfo? info)
    {
        if (string.IsNullOrWhiteSpace(header))
        {
            info = null;
            return false;
        }

        var regex = ParseRegex();
        var match = regex.Match(header);
        if (match.Success)
        {
            info = new UmaTicketInfo(match.Groups["ticketId"].Value, match.Groups["umaAuthority"].Value,
                match.Groups["realm"].Value);
            return true;
        }

        info = null;
        return false;
    }
}
