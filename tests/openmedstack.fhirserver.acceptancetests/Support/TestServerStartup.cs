using OpenMedStack.Web;

namespace OpenMedStack.FhirServer.AcceptanceTests.Support;

using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;
using System;
using Hl7.Fhir.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using OpenMedStack.SparkEngine;
using OpenMedStack.SparkEngine.Web;

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
                    Endpoint = new Uri(_configuration.FhirRoot),
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
                    options.TokenHandlers.Clear();
                    options.TokenHandlers.Add(new TestSecurityTokenValidator(new JwtSecurityTokenHandler()));
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
