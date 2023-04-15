namespace OpenMedStack.FhirServer;

using System;
using Hl7.Fhir.Serialization;
using Hl7.Fhir.Specification;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SparkEngine;
using SparkEngine.Postgres;
using SparkEngine.S3;
using SparkEngine.Web;
using Web.Autofac;

internal class ServerStartup : IConfigureWebApplication
{
    private readonly FhirServerConfiguration _configuration;

    public ServerStartup(FhirServerConfiguration configuration)
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
            });
        services.AddSingleton(new JsonSerializerSettings
                              {
                                  ContractResolver = new CamelCasePropertyNamesContractResolver(),
                                  DateFormatHandling = DateFormatHandling.IsoDateFormat,
                                  DateTimeZoneHandling = DateTimeZoneHandling.RoundtripKind,
                                  NullValueHandling = NullValueHandling.Include,
                                  DefaultValueHandling = DefaultValueHandling.Include,
                                  TypeNameHandling = TypeNameHandling.Auto,
                                  Formatting = Formatting.None,
                                  DateParseHandling = DateParseHandling.DateTimeOffset
                              });
        services.AddPostgresFhirStore(new StoreSettings(_configuration.ConnectionString))
            .AddS3Persistence(new S3PersistenceConfiguration(
                _configuration.AccessKey,
                _configuration.AccessSecret,
                _configuration.Bucket,
                _configuration.StorageServiceUrl,
                true,
                true,
                _configuration.CompressStorage))
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
                    options.RequireHttpsMetadata = false;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateActor = false,
                        ValidateLifetime = false,
                        ValidateTokenReplay = false,
                        LogValidationExceptions = true,
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
