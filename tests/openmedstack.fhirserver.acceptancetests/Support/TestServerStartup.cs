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

    public TestServerStartup(FhirServerConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddHttpClient()
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
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
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
            .UseEndpoints(e =>
            {
                e.MapControllers();
            });
    }
}
