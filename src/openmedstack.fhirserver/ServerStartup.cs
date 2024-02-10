using DotAuth.Uma;

namespace OpenMedStack.FhirServer;

using System.IO.Compression;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenMedStack.SparkEngine.Postgres;
using OpenMedStack.SparkEngine.S3;
using OpenMedStack.Web;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using OpenMedStack.FhirServer.Configuration;
using System;
using Hl7.Fhir.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using SparkEngine;
using SparkEngine.Web;

internal class ServerStartup(FhirServerConfiguration configuration) : IConfigureWebApplication
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddHttpClient()
            .AddCors()
            .AddControllers()
            .AddNewtonsoftJson();
        services.AddResponseCompression(
                o =>
                {
                    o.EnableForHttps = true;
                    o.Providers.Add(
                        new GzipCompressionProvider(
                            new GzipCompressionProviderOptions { Level = CompressionLevel.Optimal }));
                    o.Providers.Add(
                        new BrotliCompressionProvider(
                            new BrotliCompressionProviderOptions { Level = CompressionLevel.Optimal }));
                })
            .AddAntiforgery(
                o =>
                {
                    o.FormFieldName = "XrsfField";
                    o.HeaderName = "XSRF-TOKEN";
                    o.SuppressXFrameOptionsHeader = false;
                })
            .AddCors(o => o.AddPolicy("AllowAll", p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()))
            .AddFhir<UmaFhirController>(
                new SparkSettings
                {
                    Endpoint = new Uri(configuration.FhirRoot),
                    ParserSettings = ParserSettings.CreateDefault(),
                    SerializerSettings = SerializerSettings.CreateDefault()
                })
//        services.AddInMemoryFhirStores()
            .AddPostgresFhirStore(new StoreSettings(configuration.ConnectionString))
            .AddS3Persistence(new S3PersistenceConfiguration(
                configuration.AccessKey,
                configuration.AccessSecret,
                configuration.Bucket,
                configuration.StorageServiceUrl,
                true,
                true,
                configuration.CompressStorage))
            .AddAuthentication(o =>
            {
                o.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                o.DefaultAuthenticateScheme = OpenIdConnectDefaults.AuthenticationScheme;
                o.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            })
            .AddCookie(c=>
            {
                c.SlidingExpiration = true;
                c.ExpireTimeSpan = TimeSpan.FromMinutes(10);
            })
            .AddJwtBearer(
                options =>
                {
                    options.SaveToken = true;
                    options.Authority = configuration.TokenService;
                    options.RequireHttpsMetadata = false;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateActor = false,
                        ValidateLifetime = true,
                        ValidateTokenReplay = true,
                        LogValidationExceptions = true,
                        ValidateAudience = false,
                        ValidateIssuer = false,
                        ValidateIssuerSigningKey = false,
                        ValidIssuers = new[] { configuration.TokenService }
                    };
                })
            .AddOpenIdConnect(options =>
            {
                options.DisableTelemetry = true;
                options.Scope.Add(UmaConstants.UmaProtectionScope);
                options.DataProtectionProvider = new EphemeralDataProtectionProvider();
                options.SaveTokens = true;
                options.Authority = "https://identity.reimers.dk";
                options.AuthenticationMethod = OpenIdConnectRedirectBehavior.RedirectGet;
                options.UsePkce = true;
                options.RequireHttpsMetadata = false;
                options.GetClaimsFromUserInfoEndpoint = false;
                options.ResponseType = OpenIdConnectResponseType.Code;
                options.ResponseMode = OpenIdConnectResponseMode.Query;
                options.ProtocolValidator.RequireNonce = true;
                options.ProtocolValidator.RequireState = false;
                options.ClientId = configuration.ClientId;
                options.ClientSecret = configuration.Secret;
            });
        services.ConfigureOptions<ConfigureMvcNewtonsoftJsonOptions>()
            .ConfigureOptions<ConfigureOpenIdConnectOptions>()
            .AddHealthChecks()
            .AddNpgSql(configuration.ConnectionString, failureStatus: HealthStatus.Unhealthy);
    }

    /// <inheritdoc />
    public void ConfigureApplication(IApplicationBuilder app)
    {
        var forwardedHeadersOptions = new ForwardedHeadersOptions
            { ForwardedHeaders = ForwardedHeaders.All, ForwardLimit = null };
        forwardedHeadersOptions.KnownNetworks.Clear();
        forwardedHeadersOptions.KnownProxies.Clear();

        app.UseForwardedHeaders(forwardedHeadersOptions)
            .UseResponseCompression()
            .UseRouting()
            .UseAuthentication()
            .UseAuthorization()
            .UseCors(p => p.AllowAnyOrigin())
            .UseEndpoints(e =>
            {
                e.MapControllers();
                e.MapHealthChecks("/health");
            });
    }
}
