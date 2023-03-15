namespace OpenMedStack.SparkEngine.S3;

using System.Linq;
using Hl7.Fhir.Serialization;
using Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddS3Persistence(this IServiceCollection services, S3PersistenceConfiguration configuration)
    {
        services.AddSingleton(configuration);
        var resourcePersistence = services.Where(s => s.ServiceType == typeof(IResourcePersistence));
        var snapshotPersistence = services.Where(s => s.ServiceType == typeof(ISnapshotStore));
        var serviceDescriptors = resourcePersistence.Concat(snapshotPersistence).ToArray();
        foreach (var serviceDescriptor in serviceDescriptors)
        {
            services.Remove(serviceDescriptor);
        }
        services.AddTransient<IResourcePersistence>(
            sp => new S3ResourcePersistence(
                sp.GetRequiredService<S3PersistenceConfiguration>(),
                "fhir",
                sp.GetRequiredService<FhirJsonSerializer>(),
                sp.GetRequiredService<FhirJsonParser>(),
                sp.GetRequiredService<ILogger<S3ResourcePersistence>>()));
        services.AddTransient<ISnapshotStore>(
            sp => new S3SnapshotStore(
                sp.GetRequiredService<S3PersistenceConfiguration>(),
                "fhir",
                sp.GetRequiredService<Newtonsoft.Json.JsonSerializerSettings>(),
                sp.GetRequiredService<ILogger<S3SnapshotStore>>()));
        return services;
    }
}
