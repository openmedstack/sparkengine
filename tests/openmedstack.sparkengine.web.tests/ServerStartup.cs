namespace OpenMedStack.SparkEngine.Web.Tests
{
    using System;
    using Hl7.Fhir.Serialization;
    using Interfaces;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Persistence;
    using SparkEngine.Service.FhirServiceExtensions;
    using Store.Interfaces;
    using Xunit.Abstractions;

    public class ServerStartup
    {
        private readonly ITestOutputHelper _outputHelper;

        public ServerStartup(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddLogging(l => l.AddXunit(_outputHelper));
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
            services.AddSingleton<IFhirStore, InMemoryFhirStore>();
            services.AddSingleton<IIndexStore>(sp => sp.GetRequiredService<InMemoryFhirIndex>());
            services.AddSingleton<IPatchService, PatchService>();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseRouting()
                //.UseAuthentication().UseAuthorization()
                .UseEndpoints(e => e.MapControllers());
        }
    }
}