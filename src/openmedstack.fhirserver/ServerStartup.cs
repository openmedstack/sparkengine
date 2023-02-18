namespace OpenMedStack.FhirServer
{
    using System;
    using System.Collections.Generic;
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
            const string authority = "https://identity.reimers.dk";
            services.AddHttpClient();
            services.AddLogging(
                l => l.AddJsonConsole(
                    o =>
                    {
                        o.IncludeScopes = true;
                        o.UseUtcTimestamp = true;
                        o.JsonWriterOptions = new JsonWriterOptions { Indented = false };
                    }));
            services.AddCors();
            services.AddControllers();
            services.AddFhir<UmaFhirController>(
                new SparkSettings
                {
                    UseAsynchronousIO = true,
                    Endpoint = new Uri("https://fhir.reimers.dk/fhir"),
                    FhirRelease = FhirRelease.R4.ToString(),
                    ParserSettings = ParserSettings.CreateDefault(),
                    SerializerSettings = SerializerSettings.CreateDefault()
                });
            var s = _configuration["CONNECTIONSTRING"]!;
            //services.AddPostgresFhirStore(new StoreSettings(s));
            services.AddInMemoryPersistence();
            services.AddSingleton<IGenerator, GuidGenerator>();
            services.AddSingleton<IPatchService, PatchService>();
            services.AddSingleton<ITokenClient>(
                sp => new TokenClient(
                    TokenCredentials.FromClientCredentials("fhir", "fvnfdjvnfsfhrgfhgre"),
                    () =>
                    {
                        var factory = sp.GetRequiredService<IHttpClientFactory>();
                        return factory.CreateClient();
                    },
                    new Uri(authority)));
            services.AddSingleton(
                sp =>
                {
                    return new UmaClient(
                        () =>
                        {
                            var factory = sp.GetRequiredService<IHttpClientFactory>();
                            return factory.CreateClient();
                        },
                        new Uri(authority));
                });
            services.AddSingleton<IUmaPermissionClient>(sp => sp.GetRequiredService<UmaClient>());
            services.AddSingleton<IResourceMap>(
                new StaticResourceMap(
                    new HashSet<KeyValuePair<string, string>>
                    {
                        KeyValuePair.Create("Patient/123", "7A38B4029C6ACD4AB1FF1A0D1DD8A1AC")
                    }));
            services.AddAuthentication(
                    options =>
                    {
                        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                    })
                .AddJwtBearer(
                    options =>
                    {
                        options.SecurityTokenValidators.Clear();
                        options.SecurityTokenValidators.Add(new CustomTokenValidator());
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
                .UseEndpoints(e => e.MapControllers());
        }
    }
}