using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using DotAuth.Client;
using DotAuth.Shared;
using DotAuth.Shared.Requests;
using DotAuth.Shared.Responses;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;

namespace OpenMedStack.FhirServer.AcceptanceTests.Support;

internal class TestTokenClient: ITokenClient
{
    private readonly DeploymentConfiguration _configuration;

    public TestTokenClient(DeploymentConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<Option<OauthIntrospectionResponse>> Introspect(DotAuth.Client.IntrospectionRequest introspectionRequest, CancellationToken cancellationToken = default)
    {
        return new OauthIntrospectionResponse();
    }

    public async Task<Option<GrantedTokenResponse>> GetToken(TokenRequest tokenRequest,
        CancellationToken cancellationToken = default)
    {
        using var rsa = RSA.Create(2048);
        var jwk = JsonWebKeyConverter.ConvertFromRSASecurityKey(new RsaSecurityKey(rsa));
        var handler = new JwtSecurityTokenHandler();
        var accessToken = handler.CreateEncodedJwt(_configuration.TokenService, _configuration.Name,
            new ClaimsIdentity(
                new[]
                {
                    new Claim("sub", "123", ClaimValueTypes.String),
                    new Claim("scope", "dcr", ClaimValueTypes.String)
                }, "Bearer"), DateTime.UtcNow,
            DateTime.UtcNow.AddDays(1), DateTime.UtcNow,
            new SigningCredentials(jwk, SecurityAlgorithms.RsaSha256));
        return new GrantedTokenResponse
        {
            AccessToken = accessToken,
            IdToken = accessToken,
            TokenType = "Bearer",
            ExpiresIn = 3600,
            RefreshToken = null,
            Scope = "openid profile email offline_access"
        };
    }

    public async Task<Option<Uri>> GetAuthorization(AuthorizationRequest request, CancellationToken cancellationToken = new CancellationToken())
    {
        return new Uri("https://localhost");
    }

    public async Task<Option<DeviceAuthorizationResponse>> GetAuthorization(DeviceAuthorizationRequest request,
        CancellationToken cancellationToken = new CancellationToken())
    {
        return new DeviceAuthorizationResponse();
    }

    public async Task<Option> RequestSms(ConfirmationCodeRequest request, CancellationToken cancellationToken = new CancellationToken())
    {
        return new Option.Success();
    }

    public async Task<Option> RevokeToken(RevokeTokenRequest revokeTokenRequest, CancellationToken cancellationToken = new CancellationToken())
    {
        return new Option.Success();
    }

    public async Task<Option<JwtPayload>> GetUserInfo(string accessToken, bool inBody = false,
        CancellationToken cancellationToken = new CancellationToken())
    {
        return new JwtPayload();
    }

    public async Task<JsonWebKeySet> GetJwks(CancellationToken cancellationToken = new CancellationToken())
    {
        return new JsonWebKeySet();
    }
}
