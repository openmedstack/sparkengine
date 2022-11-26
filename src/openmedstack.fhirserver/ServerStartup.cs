namespace OpenMedStack.FhirServer
{
    using System;
    using System.Text.Json;
    using Hl7.Fhir.Serialization;
    using Hl7.Fhir.Specification;
    using Microsoft.AspNetCore.Authentication.JwtBearer;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.IdentityModel.Tokens;
    using OpenMedStack.SparkEngine.Interfaces;
    using OpenMedStack.SparkEngine.Service.FhirServiceExtensions;
    using SparkEngine;
    using SparkEngine.Postgres;
    using SparkEngine.Web;
    using SparkEngine.Web.Controllers;

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
            services.AddCors()
                .AddLogging(
                    l => l.AddJsonConsole(
                        o =>
                        {
                            o.IncludeScopes = true;
                            o.UseUtcTimestamp = true;
                            o.JsonWriterOptions = new JsonWriterOptions { Indented = false };
                        }))
                .AddControllers()
                .Services.AddTransient<FhirController>()
                .AddTransient<ControllerBase, FhirController>()
                .AddFhir(
                    new SparkSettings
                    {
                        UseAsynchronousIO = true,
                        Endpoint = new Uri("https://fhir.reimers.dk/fhir"),
                        FhirRelease = FhirRelease.R4.ToString(),
                        ParserSettings = ParserSettings.CreateDefault(),
                        SerializerSettings = SerializerSettings.CreateDefault()
                    });
            //services.AddInMemoryPersistence();
            var s = _configuration["CONNECTIONSTRING"]!;
            services.AddPostgresFhirStore(new StoreSettings(s))
                .AddSingleton<IGenerator, GuidGenerator>()
                .AddSingleton<IPatchService, PatchService>()
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
