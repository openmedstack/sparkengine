using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace OpenMedStack.SparkEngine.Web.Tests;

using System;
using Hl7.Fhir.Serialization;
using Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using SparkEngine.Service.FhirServiceExtensions;

public class ServerStartup
{
    public void ConfigureServices(IServiceCollection services, ITestOutputHelper outputHelper)
    {
        services.AddLogging(l => l.AddXunit(outputHelper));
        services.AddFhir<TestFhirController>(
            new SparkSettings
            {
                Endpoint = new Uri("https://localhost:60001/fhir"),
                ParserSettings = ParserSettings.CreateDefault(),
                SerializerSettings = SerializerSettings.CreateDefault()
            });
        services.AddSingleton<IGenerator, GuidGenerator>();
        services.AddInMemoryFhirStores();
        //services.AddPostgresFhirStore(
        //        new StoreSettings(
        //            "Server=odin;Port=5432;Database=fhirserver;Search Path=test;User Id=fhir;Password=AxeeFE6wSG553ii;Pooling=true;MaxPoolSize=900;Timeout=600;",
        //            new JsonSerializerSettings
        //            {
        //                ContractResolver = new CamelCasePropertyNamesContractResolver(),
        //                DateFormatHandling = DateFormatHandling.IsoDateFormat,
        //                DateTimeZoneHandling = DateTimeZoneHandling.RoundtripKind,
        //                NullValueHandling = NullValueHandling.Include,
        //                DefaultValueHandling = DefaultValueHandling.Include,
        //                TypeNameHandling = TypeNameHandling.Auto,
        //                Formatting = Formatting.None,
        //                DateParseHandling = DateParseHandling.DateTimeOffset
        //            }))
        //    .AddS3Persistence(
        //        new S3PersistenceConfiguration(
        //            "fhirstore:O76W8n3MMpePgv0tuZwQ",
        //            "yhxwfaFKh0eQQammadwlAxLfizwHPZnQ",
        //            "fhir",
        //            new Uri("http://nas:8010"),
        //            true,
        //            true,
        //            true));
        //.AddDiskPersistence(new DiskPersistenceConfiguration(Path.Combine(".", "fhir"), true));
        services.AddSingleton<IPatchService, PatchService>();
        services.AddCors();
        services.AddAuthorization()
            .AddAuthentication()
            .AddJwtBearer(
                o =>
                {
                    o.Authority = "https://identity.reimers.dk";
                    o.TokenValidationParameters = new TokenValidationParameters
                    {
                        LifetimeValidator = (_, _, _, _) => true,
                        ValidateAudience = false,
                        ValidateActor = false,
                        ValidateIssuer = false,
                        ValidateLifetime = false,
                        ValidateIssuerSigningKey = false,
                        ValidateTokenReplay = false
                    };
                });
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseRouting().UseCors().UseAuthentication().UseAuthorization().UseEndpoints(e => e.MapControllers());
    }
}
