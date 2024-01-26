using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace OpenMedStack.FhirServer.AcceptanceTests.Support;

internal class TestSecurityTokenValidator(JwtSecurityTokenHandler handler) : TokenHandler
{
    public override Task<TokenValidationResult> ValidateTokenAsync(
        string token,
        TokenValidationParameters validationParameters)
    {
        var jwt = ReadToken(token);
        return ValidateTokenAsync(jwt, validationParameters);
    }

    public override async Task<TokenValidationResult> ValidateTokenAsync(SecurityToken token, TokenValidationParameters validationParameters)
    {
        await Task.Yield();
        var jwt = (JsonWebToken)token;
        return new TokenValidationResult
        {
            ClaimsIdentity = new ClaimsIdentity(jwt.Claims, "Bearer"), 
            Issuer = jwt.Issuer, 
            SecurityToken = jwt,
            TokenType = "Bearer"
        };
    }

    public override SecurityToken ReadToken(string token)
    {
        return handler.ReadJwtToken(token);
    }

    public override int MaximumTokenSizeInBytes { get; set; } = int.MaxValue;
}
