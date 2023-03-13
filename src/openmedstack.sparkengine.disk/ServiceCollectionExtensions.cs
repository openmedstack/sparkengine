namespace OpenMedStack.SparkEngine.Disk;

using Interfaces;
using Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDiskPersistence(this IServiceCollection services, DiskPersistenceConfiguration configuration)
    {
        var serviceDescriptors = services.Where(s => s.ServiceType == typeof(IResourcePersistence)).ToArray();
        foreach (var serviceDescriptor in serviceDescriptors)
        {
            services.Remove(serviceDescriptor);
        }
        services.AddSingleton(configuration);
        services.AddSingleton<IFhirStore, DiskFhirStore>();

        return services;
    }
}