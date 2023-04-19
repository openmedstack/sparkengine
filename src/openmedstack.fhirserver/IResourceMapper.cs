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

    string ClientId { get; }

    string Authority { get; }
}

internal class ApplicationNameProvider : IProvideApplicationName
{
    public ApplicationNameProvider(FhirServerConfiguration configuration)
    {
        ApplicationName = $"{configuration.TenantPrefix}-{configuration.Name}";
        ClientId = configuration.ClientId;
        Authority = configuration.TokenService;
    }

    public string ApplicationName { get; }

    public string ClientId { get; }

    public string Authority { get; }
}
