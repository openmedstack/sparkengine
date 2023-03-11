namespace OpenMedStack.FhirServer;

using System;
using System.Net.Http;
using System.Text.Json;
using DotAuth.Client;
using DotAuth.Uma;
using Hl7.Fhir.Serialization;
using Hl7.Fhir.Specification;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using OpenMedStack.SparkEngine.Interfaces;
using OpenMedStack.SparkEngine.Service.FhirServiceExtensions;
using SparkEngine;
using SparkEngine.Postgres;
using SparkEngine.S3;
using SparkEngine.Web;

public class ServerStartup
{
    private readonly IConfiguration _configuration;

    public ServerStartup(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        var authority = _configuration["AUTHORITY"]!;
        services.AddHttpClient()
            .AddLogging(
            l => l.AddJsonConsole(
                o =>
                {
                    o.IncludeScopes = true;
                    o.UseUtcTimestamp = true;
                    o.JsonWriterOptions = new JsonWriterOptions { Indented = false };
                }))
            .AddCors()
            .AddControllers();
        services.AddFhir<UmaFhirController>(
            new SparkSettings
            {
                UseAsynchronousIO = true,
                Endpoint = new Uri(_configuration["FHIR:ROOT"]!),
                FhirRelease = FhirRelease.R5.ToString(),
                ParserSettings = ParserSettings.CreateDefault(),
                SerializerSettings = SerializerSettings.CreateDefault()
            });
        var s = _configuration["CONNECTIONSTRING"]!;
        services.AddPostgresFhirStore(new StoreSettings(s))
            .AddS3Persistence(
                new S3PersistenceConfiguration(
                    _configuration["STORAGE:ACCESSKEY"]!,
                    _configuration["STORAGE:SECRETKEY"]!,
                    new Uri(_configuration["STORAGE:STORAGEURL"]!),
                    true,
                    true))
            //services.AddInMemoryPersistence();
            .AddSingleton<IGenerator, GuidGenerator>()
            .AddSingleton<IPatchService, PatchService>()
            .AddSingleton<ITokenClient>(
                sp => new TokenClient(
                    TokenCredentials.FromClientCredentials(_configuration["OAUTH:CLIENTID"]!, _configuration["OAUTH:CLIENTSECRET"]!),
                    () =>
                    {
                        var factory = sp.GetRequiredService<IHttpClientFactory>();
                        return factory.CreateClient();
                    },
                    new Uri(authority)))
            .AddSingleton(
                sp =>
                {
                    return new UmaClient(
                        () =>
                        {
                            var factory = sp.GetRequiredService<IHttpClientFactory>();
                            return factory.CreateClient();
                        },
                        new Uri(authority));
                })
            .AddSingleton<IUmaPermissionClient>(sp => sp.GetRequiredService<UmaClient>())
            .AddTransient<IResourceMap>(_=>new DbSourceMap(s))
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
                    options.Authority = authority;
                    options.RequireHttpsMetadata = true;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateAudience = false,
                        ValidateIssuer = false,
                        ValidateIssuerSigningKey = false,
                        ValidIssuers = new[] { authority }
                    };
                });
    }

    public void Configure(IApplicationBuilder app)
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