namespace OpenMedStack.FhirServer;

using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

internal class CustomTokenValidator : ISecurityTokenValidator
{
    /// <inheritdoc />
    public bool CanReadToken(string securityToken)
    {
        return true;
    }

    /// <inheritdoc />
    public ClaimsPrincipal ValidateToken(
        string securityToken,
        TokenValidationParameters validationParameters,
        out SecurityToken validatedToken)
    {
        var handler = new JsonWebTokenHandler();
        var jwt = handler.ReadJsonWebToken(securityToken);
        var payload = new JwtPayload(jwt.Claims.Where(x => x.Type != "exp"));
        payload.AddClaim(new Claim("exp", DateTimeOffset.UtcNow.AddDays(2).ToUnixTimeSeconds().ToString()));
        var s = payload.Base64UrlEncode();
        validatedToken = new JsonWebToken(handler.CreateToken(payload.SerializeToJson()));

        return new ClaimsPrincipal(new ClaimsIdentity(jwt.Claims, "jwt"));
    }

    /// <inheritdoc />
    public bool CanValidateToken { get; } = true;

    /// <inheritdoc />
    public int MaximumTokenSizeInBytes { get; set; } = int.MaxValue;
}