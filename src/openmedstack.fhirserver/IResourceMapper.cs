namespace OpenMedStack.FhirServer;

using System.Threading;
using System.Threading.Tasks;

public interface IResourceMapper
{
    Task MapResource(string resourceId, string resourceSetId, CancellationToken cancellationToken = default);
}

public interface IProvideApplicationName
{
    string ApplicationName { get; }
}

internal class ApplicationNameProvider : IProvideApplicationName
{
    public ApplicationNameProvider(FhirServerConfiguration configuration)
    {
        ApplicationName = $"{configuration.TenantPrefix}-{configuration.Name}";
    }

    public string ApplicationName { get; }
}
