namespace OpenMedStack.SparkEngine.S3;

using Hl7.Fhir.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddS3Persistence(this IServiceCollection services, S3PersistenceConfiguration configuration)
    {
        services.AddSingleton(configuration);
        services.AddTransient(
            sp => new S3ResourcePersistence(
                sp.GetRequiredService<S3PersistenceConfiguration>(),
                "fhir",
                sp.GetRequiredService<FhirJsonSerializer>(),
                sp.GetRequiredService<FhirJsonParser>(),
                sp.GetRequiredService<ILogger<S3ResourcePersistence>>()));
        return services;
    }
}
