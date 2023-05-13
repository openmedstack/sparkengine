using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace OpenMedStack.FhirServer.AcceptanceTests.Support;

using System;
using Hl7.Fhir.Serialization;
using Hl7.Fhir.Specification;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using OpenMedStack.SparkEngine;
using OpenMedStack.SparkEngine.Web;
using OpenMedStack.Web.Autofac;

internal class TestServerStartup : IConfigureWebApplication
{
    private readonly FhirServerConfiguration _configuration;
    private readonly ITestOutputHelper _outputHelper;

    public TestServerStartup(FhirServerConfiguration configuration, ITestOutputHelper outputHelper)
    {
        _configuration = configuration;
        _outputHelper = outputHelper;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddLogging(builder => builder.AddXunit(_outputHelper))
            .AddCors()
            .AddControllers();
        services.AddFhir<UmaFhirController>(
                new SparkSettings
                {
                    UseAsynchronousIO = true,
                    Endpoint = new Uri(_configuration.FhirRoot),
                    FhirRelease = FhirRelease.R5.ToString(),
                    ParserSettings = ParserSettings.CreateDefault(),
                    SerializerSettings = SerializerSettings.CreateDefault()
                })
            .AddInMemoryFhirStores()
            .AddAuthentication(
                options =>
                {
                    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
            .AddJwtBearer(
                options =>
                {
                    options.SaveToken = true;
                    options.Authority = _configuration.TokenService;
                    options.RequireHttpsMetadata = true;
                    options.SecurityTokenValidators.Clear();
                    options.SecurityTokenValidators.Add(new TestSecurityTokenValidator(new JwtSecurityTokenHandler()));
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        IssuerSigningKeyValidator = (_, _, _) => true,
                        ValidateAudience = false,
                        ValidateIssuer = false,
                        ValidateIssuerSigningKey = false,
                        ValidIssuers = new[] { _configuration.TokenService }
                    };
                });
    }

    /// <inheritdoc />
    public void ConfigureApplication(IApplicationBuilder app)
    {
        app.UseRouting()
            .UseCors(p => p.AllowAnyOrigin())
            .UseAuthentication()
            .UseAuthorization()
            .UseEndpoints(e => { e.MapControllers(); });
    }
}

internal class TestSecurityTokenValidator : ISecurityTokenValidator
{
    private readonly JwtSecurityTokenHandler _handler;

    public TestSecurityTokenValidator(JwtSecurityTokenHandler handler)
    {
        _handler = handler;
    }

    public bool CanReadToken(string securityToken)
    {
        return _handler.CanReadToken(securityToken);
    }

    public ClaimsPrincipal ValidateToken(string securityToken, TokenValidationParameters validationParameters,
        out SecurityToken validatedToken)
    {
        var jwt = _handler.ReadJwtToken(securityToken);
        validatedToken = jwt;
        return new ClaimsPrincipal(new ClaimsIdentity(jwt.Claims, "Bearer"));
    }

    public bool CanValidateToken { get; } = true;
    public int MaximumTokenSizeInBytes { get; set; } = int.MaxValue;
}
