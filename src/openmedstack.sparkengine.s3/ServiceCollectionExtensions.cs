namespace OpenMedStack.SparkEngine.S3;

using System.Linq;
using Interfaces;
using Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddS3Persistence(
        this IServiceCollection services,
        S3PersistenceConfiguration configuration)
    {
        services.AddSingleton(configuration);
        var resourcePersistence = services.Where(s => s.ServiceType == typeof(IResourcePersistence));
        var snapshotPersistence = services.Where(s => s.ServiceType == typeof(ISnapshotStore));
        var serviceDescriptors = resourcePersistence.Concat(snapshotPersistence).ToArray();
        foreach (var serviceDescriptor in serviceDescriptors)
        {
            services.Remove(serviceDescriptor);
        }

        services.AddScoped<IResourcePersistence, S3ResourcePersistence>();
        services.AddScoped<ISnapshotStore, S3SnapshotStore>();
        return services;
    }
}
