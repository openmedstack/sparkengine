namespace OpenMedStack.SparkEngine.Disk;

using System.Runtime.CompilerServices;
using Core;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Interfaces;

public abstract class AbstractDiskFhirStore : IFhirStore
{
    protected FhirJsonPocoDeserializer Deserializer { get; }
    protected string ResourcePath { get; }
    protected string EntryPath { get; }
    protected FhirJsonSerializer Serializer { get; }

    protected AbstractDiskFhirStore(DiskPersistenceConfiguration configuration, IProvideTenant tenantProvider)
    {
        Serializer = new FhirJsonSerializer();
        Deserializer = new FhirJsonPocoDeserializer();
        ResourcePath = Path.GetFullPath(Path.Combine(configuration.RootPath, tenantProvider.GetTenantName(), "resources"));
        EntryPath = Path.GetFullPath(Path.Combine(configuration.RootPath, tenantProvider.GetTenantName(), "entries"));
        if (configuration.CreateDirectoryIfNotExists)
        {
            Directory.CreateDirectory(ResourcePath);
            Directory.CreateDirectory(Path.Combine(EntryPath, "deleted"));
        }
    }

    /// <inheritdoc />
    public abstract Task<Entry> Add(Entry entry, CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public abstract Task<bool> Exists(IKey key, CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public abstract Task<Resource?> Load(IKey key, CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public async IAsyncEnumerable<ResourceInfo> Get(
        IEnumerable<IKey> localIdentifiers,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var localIdentifier in localIdentifiers)
        {
            var info = await Get(localIdentifier, cancellationToken).ConfigureAwait(false);
            if (info != null)
            {
                yield return info;
            }
        }
    }

    /// <inheritdoc />
    public abstract Task<ResourceInfo?> Get(IKey key, CancellationToken cancellationToken = default);
}
