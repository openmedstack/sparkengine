namespace OpenMedStack.FhirServer
{
    using System;
    using System.IdentityModel.Tokens.Jwt;
    using System.Linq;
    using System.Security.Claims;
    using Hl7.Fhir.Serialization;
    using Hl7.Fhir.Specification;
    using Microsoft.AspNetCore.Authentication.JwtBearer;
    using Microsoft.AspNetCore.Authorization.Infrastructure;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.IdentityModel.JsonWebTokens;
    using Microsoft.IdentityModel.Tokens;
    using OpenMedStack.SparkEngine.Interfaces;
    using OpenMedStack.SparkEngine.Service.FhirServiceExtensions;
    using OpenMedStack.SparkEngine.Store.Interfaces;
    using Persistence;
    using SparkEngine;
    using SparkEngine.Web;
    using SparkEngine.Web.Controllers;

    public class ServerStartup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            const string authority = "https://identity.reimers.dk";
            services.AddCors();
            services.AddControllers();
            services.AddTransient<FhirController>();
            services.AddTransient<ControllerBase, FhirController>();
            services.AddFhir(
                new SparkSettings
                {
                    UseAsynchronousIO = true,
                    Endpoint = new Uri("https://fhir.reimers.dk/fhir"),
                    FhirRelease = FhirRelease.R4.ToString(),
                    ParserSettings = ParserSettings.CreateDefault(),
                    SerializerSettings = SerializerSettings.CreateDefault()
                });
            services.AddSingleton<InMemoryFhirIndex>();
            services.AddSingleton<IFhirIndex>(sp => sp.GetRequiredService<InMemoryFhirIndex>());
            services.AddSingleton<ISnapshotStore, InMemorySnapshotStore>();
            services.AddSingleton<IHistoryStore, InMemoryHistoryStore>();
            services.AddSingleton<IGenerator, GuidGenerator>();
            services.AddSingleton<IFhirStore, InMemoryFhirStore>();
            services.AddSingleton<IIndexStore>(sp => sp.GetRequiredService<InMemoryFhirIndex>());
            services.AddSingleton<IPatchService, PatchService>();
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
}