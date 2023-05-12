using Microsoft.AspNetCore.Authentication.OAuth;

namespace OpenMedStack.FhirServer;

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using OpenMedStack.FhirServer.Configuration;
using System;
using Hl7.Fhir.Serialization;
using Hl7.Fhir.Specification;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SparkEngine;
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
            .AddControllers()
            .AddNewtonsoftJson();
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
        services.AddFhir<UmaFhirController>(
            new SparkSettings
            {
                UseAsynchronousIO = true,
                Endpoint = new Uri(_configuration.FhirRoot),
                FhirRelease = FhirRelease.R5.ToString(),
                ParserSettings = ParserSettings.CreateDefault(),
                SerializerSettings = SerializerSettings.CreateDefault()
            });
        services.AddInMemoryFhirStores()
//        services.AddPostgresFhirStore(new StoreSettings(_configuration.ConnectionString))
//            .AddS3Persistence(new S3PersistenceConfiguration(
//                _configuration.AccessKey,
//                _configuration.AccessSecret,
//                _configuration.Bucket,
//                _configuration.StorageServiceUrl,
//                true,
//                true,
//                _configuration.CompressStorage))
//            .AddAuthentication(
//                options =>
//                {
//                    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
//                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
//                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
//                })
//            .AddJwtBearer(
//                options =>
//                {
//                    options.SaveToken = true;
//                    options.Authority = _configuration.TokenService;
//                    options.RequireHttpsMetadata = false;
//                    options.TokenValidationParameters = new TokenValidationParameters
//                    {
//                        ValidateActor = false,
//                        ValidateLifetime = true,
//                        ValidateTokenReplay = true,
//                        LogValidationExceptions = true,
//                        ValidateAudience = false,
//                        ValidateIssuer = false,
//                        ValidateIssuerSigningKey = false,
//                        ValidIssuers = new[] { _configuration.TokenService }
//                    };
//                })
            .AddAuthentication(o =>
            {
                o.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                o.DefaultAuthenticateScheme = OpenIdConnectDefaults.AuthenticationScheme;
                o.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            })
            .AddCookie()
//            .AddOAuth(OAuthDefaults.DisplayName, OAuthDefaults.DisplayName, options =>
//            {
//                var authority = "https://identity.reimers.dk";
//                options.Scope.Add("uma_protection");
//                options.DataProtectionProvider = new EphemeralDataProtectionProvider();
//                options.SaveTokens = true;
//                options.TokenEndpoint = $"{authority}/token";
//                options.AuthorizationEndpoint = $"{authority}/authorization";
//                options.UserInformationEndpoint = $"{authority}/userinfo";
//                options.UsePkce = true;
//                options.CallbackPath = "/callback";
//                options.ClientId = _configuration.ClientId;
//                options.ClientSecret = _configuration.Secret;
//            });
            .AddOpenIdConnect(options =>
            {
                options.DisableTelemetry = true;
                options.Scope.Add("uma_protection");
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
                //options.CallbackPath = "/callback";
                options.ClientId = _configuration.ClientId;
                options.ClientSecret = _configuration.Secret;
            });
        services.ConfigureOptions<ConfigureMvcNewtonsoftJsonOptions>();
        services.ConfigureOptions<ConfigureOpenIdConnectOptions>();
        services.ConfigureOptions<ConfigureOAuthOptions>();
    }

    /// <inheritdoc />
    public void ConfigureApplication(IApplicationBuilder app)
    {
        app.UseDeveloperExceptionPage();
        app.UseRouting()
            .UseCors(p => p.AllowAnyOrigin())
            .UseAuthentication()
            .UseAuthorization()
            .UseEndpoints(e => { e.MapControllers(); });
    }
}
