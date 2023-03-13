namespace OpenMedStack.SparkEngine.Web.Tests;

using System;
using System.IO;
using Disk;
using Hl7.Fhir.Serialization;
using Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Postgres;
using SparkEngine.Service.FhirServiceExtensions;
using Web.Persistence;
using Xunit.Abstractions;
using InMemoryHistoryStore = Persistence.InMemoryHistoryStore;
using InMemorySnapshotStore = Persistence.InMemorySnapshotStore;

public class ServerStartup
{
    public void ConfigureServices(IServiceCollection services)
    {
        //services.AddControllers();
        //services.AddLogging(l => l.AddXunit(_outputHelper));
        services.AddFhir<TestFhirController>(
            new SparkSettings
            {
                UseAsynchronousIO = true,
                Endpoint = new Uri("https://localhost:60001/fhir"),
                FhirRelease = "R5",
                ParserSettings = ParserSettings.CreateDefault(),
                SerializerSettings = SerializerSettings.CreateDefault()
            });
        services.AddSingleton<IGenerator, GuidGenerator>();
        services.AddPostgresFhirStore(new StoreSettings("Server=odin;Port=5432;Database=fhirserver;User Id=fhir;Password=AxeeFE6wSG553ii;"))
            .AddDiskPersistence(new DiskPersistenceConfiguration(Path.Combine(".", "fhir"), true));
        //services.AddSingleton<InMemoryFhirIndex>();
        //services.AddSingleton<IFhirIndex>(sp => sp.GetRequiredService<InMemoryFhirIndex>());
        //services.AddSingleton<ISnapshotStore, InMemorySnapshotStore>();
        //services.AddSingleton<IHistoryStore, InMemoryHistoryStore>();
        ////services.AddSingleton<IFhirStore, InMemoryFhirStore>();
        //services.AddSingleton<IIndexStore, InMemoryFhirIndex>();
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