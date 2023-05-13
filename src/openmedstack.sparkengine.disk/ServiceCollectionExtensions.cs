namespace OpenMedStack.SparkEngine.Disk;

using Interfaces;
using Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDiskPersistence(
        this IServiceCollection services,
        DiskPersistenceConfiguration configuration)
    {
        var resourcePersistence = services.Where(s => s.ServiceType == typeof(IResourcePersistence));
        var snapshotStore = services.Where(s => s.ServiceType == typeof(ISnapshotStore));
        var serviceDescriptors = resourcePersistence.Concat(snapshotStore).ToArray();
        foreach (var serviceDescriptor in serviceDescriptors)
        {
            services.Remove(serviceDescriptor);
        }

        services.AddSingleton(configuration);
        services.AddScoped<IFhirStore, DiskFhirStore>();
        services.AddScoped<ISnapshotStore, DiskSnapshotStore>();

        return services;
    }
}
