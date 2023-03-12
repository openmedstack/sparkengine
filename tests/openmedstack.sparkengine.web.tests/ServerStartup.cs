namespace OpenMedStack.SparkEngine.Web.Tests;

using System;
using System.IO;
using Disk;
using Hl7.Fhir.Serialization;
using Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using SparkEngine.Service.FhirServiceExtensions;
using Store.Interfaces;
using Web.Persistence;
using Xunit.Abstractions;
using InMemoryHistoryStore = Persistence.InMemoryHistoryStore;
using InMemorySnapshotStore = Persistence.InMemorySnapshotStore;

public class ServerStartup
{
    private readonly ITestOutputHelper _outputHelper;

    public ServerStartup(ITestOutputHelper outputHelper)
    {
        _outputHelper = outputHelper;
    }

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
        services.AddSingleton<InMemoryFhirIndex>();
        services.AddSingleton<IFhirIndex>(sp => sp.GetRequiredService<InMemoryFhirIndex>());
        services.AddSingleton<ISnapshotStore, InMemorySnapshotStore>();
        services.AddSingleton<IHistoryStore, InMemoryHistoryStore>();
        services.AddSingleton<IGenerator, GuidGenerator>();
        //services.AddSingleton<IFhirStore, InMemoryFhirStore>();
        services.AddSingleton(new DiskPersistenceConfiguration(Path.Combine(".", "fhir")));
        services.AddSingleton<IFhirStore, DiskFhirStore>();
        services.AddSingleton<IIndexStore>(sp => sp.GetRequiredService<InMemoryFhirIndex>());
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