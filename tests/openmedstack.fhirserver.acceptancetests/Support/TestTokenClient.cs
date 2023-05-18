namespace OpenMedStack.FhirServer.AcceptanceTests.Support;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using DotAuth.Client;
using DotAuth.Shared;
using DotAuth.Shared.Models;
using DotAuth.Shared.Requests;
using DotAuth.Shared.Responses;
using Microsoft.IdentityModel.Tokens;

internal class TestTokenClient : ITokenClient
{
    private readonly DeploymentConfiguration _configuration;

    public TestTokenClient(DeploymentConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<Option<OauthIntrospectionResponse>> Introspect(
        DotAuth.Client.IntrospectionRequest introspectionRequest,
        CancellationToken cancellationToken = default)
    {
        await Task.Yield();
        return new OauthIntrospectionResponse();
    }

    public async Task<Option<GrantedTokenResponse>> GetToken(
        TokenRequest tokenRequest,
        CancellationToken cancellationToken = default)
    {
        await Task.Yield();
        using var rsa = RSA.Create(2048);
        var jwk = JsonWebKeyConverter.ConvertFromRSASecurityKey(new RsaSecurityKey(rsa));
        var handler = new JwtSecurityTokenHandler();

        var t = new JwtSecurityToken(
            new JwtHeader(new SigningCredentials(jwk, SecurityAlgorithms.RsaSha256)),
            new JwtPayload(new List<Claim>
            {
                new(StandardClaimNames.Issuer, _configuration.TokenService),
                new(StandardClaimNames.Audiences, _configuration.Name),
                new("sub", "123", ClaimValueTypes.String),
                new("scope", "dcr uma_protection", ClaimValueTypes.String)
            }));
        if (tokenRequest.Any(x => x is { Key: "grant_type", Value: GrantTypes.UmaTicket }))
        {
            var ticketId = tokenRequest.First(x => x.Key == "ticket").Value!;
            t.Payload.Add(
                "permissions",
                new[]
                {
                    new Permission
                    {
                        ResourceSetId = ticketId,
                        Scopes = new[] { "read", "write" },
                        Expiry = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds(),
                        IssuedAt = DateTimeOffset.UtcNow.AddMinutes(-1).ToUnixTimeSeconds(),
                        NotBefore = 0
                    }
                });
        }

        var accessToken = handler.WriteToken(t);

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

    public async Task<Option<Uri>> GetAuthorization(
        AuthorizationRequest request,
        CancellationToken cancellationToken = new())
    {
        await Task.Yield();
        return new Uri("https://localhost");
    }

    public async Task<Option<DeviceAuthorizationResponse>> GetAuthorization(
        DeviceAuthorizationRequest request,
        CancellationToken cancellationToken = new())
    {
        await Task.Yield();
        return new DeviceAuthorizationResponse();
    }

    public async Task<Option> RequestSms(
        ConfirmationCodeRequest request,
        CancellationToken cancellationToken = new())
    {
        await Task.Yield();
        return new Option.Success();
    }

    public async Task<Option> RevokeToken(
        RevokeTokenRequest revokeTokenRequest,
        CancellationToken cancellationToken = new())
    {
        await Task.Yield();
        return new Option.Success();
    }

    public async Task<Option<JwtPayload>> GetUserInfo(
        string accessToken,
        bool inBody = false,
        CancellationToken cancellationToken = new())
    {
        await Task.Yield();
        return new JwtPayload();
    }

    public async Task<JsonWebKeySet> GetJwks(CancellationToken cancellationToken = new())
    {
        await Task.Yield();
        return new JsonWebKeySet();
    }
}
