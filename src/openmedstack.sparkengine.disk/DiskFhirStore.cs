namespace OpenMedStack.SparkEngine.Disk;

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Core;
using Extensions;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Interfaces;
using Task = System.Threading.Tasks.Task;

public class DiskFhirStore : IFhirStore
{
    private readonly FhirJsonSerializer _serializer;
    private readonly FhirJsonPocoDeserializer _deserializer;
    private readonly string _rootPath;

    private readonly ConcurrentDictionary<string, string> _entries = new();

    public DiskFhirStore(DiskPersistenceConfiguration configuration)
    {
        _serializer = new FhirJsonSerializer();
        _deserializer = new FhirJsonPocoDeserializer();
        _rootPath = Path.GetFullPath(Path.Combine(configuration.RootPath, "resources"));
        if (configuration.CreateDirectoryIfNotExists)
        {
            Directory.CreateDirectory(_rootPath);
        }
    }

    /// <inheritdoc />
    public async Task<Entry> Add(Entry entry, CancellationToken cancellationToken = default)
    {
        if (entry.IsDelete)
        {
            _entries.Remove(entry.Key.ToStorageKey(), out _);
            _entries.Remove(entry.Key.WithoutVersion().ToStorageKey(), out _);
        }
        else
        {
            var fileName = $"{Guid.NewGuid():N}.json";
            var fullPath = Path.GetFullPath(Path.Combine(_rootPath, fileName));
            var json = await _serializer.SerializeToStringAsync(entry.Resource!).ConfigureAwait(false);
            await File.WriteAllTextAsync(fullPath, json, cancellationToken).ConfigureAwait(false);
            _entries.AddOrUpdate(entry.Key.ToOperationPath(), fileName, (_, f) => f);
            _entries.AddOrUpdate(entry.Key.WithoutVersion().ToStorageKey(), fileName, (_, f) => f);
        }

        return entry;
    }

    /// <inheritdoc />
    public async Task<Entry?> Get(IKey key, CancellationToken cancellationToken = default)
    {
        async Task<Entry?> GetEntry(IKey k, CancellationToken cancellationToken1)
        {
            if (_entries.TryGetValue(k.ToStorageKey(), out var entry))
            {
                var resource = Deserializer(
                    await File.ReadAllBytesAsync(Path.Combine(_rootPath, entry), cancellationToken1)
                        .ConfigureAwait(false));
                return Entry.Create(resource.ExtractKey(), resource);
            }

            return null;
        }

        var entry = await GetEntry(key, cancellationToken).ConfigureAwait(false)
                    ?? await GetEntry(key.WithoutVersion(), cancellationToken).ConfigureAwait(false);

        return entry;
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<Entry> Get(
        IEnumerable<IKey> localIdentifiers,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await Task.Yield();
        foreach (var localIdentifier in localIdentifiers)
        {
            if (_entries.TryGetValue(localIdentifier.ToStorageKey(), out var entry))
            {
                var envelope = Deserializer(
                    await File.ReadAllBytesAsync(Path.Combine(_rootPath, entry), cancellationToken)
                        .ConfigureAwait(false));
                yield return Entry.Create(envelope.ExtractKey(), envelope);
            }
        }
    }

    private Resource Deserializer(byte[] b)
    {
        var utf8JsonReader = new Utf8JsonReader(b);
        return _deserializer.DeserializeResource(ref utf8JsonReader);
    }

    /// <inheritdoc />
    public Task<bool> Exists(IKey key, CancellationToken cancellationToken = default)
    {
        var exists = _entries.ContainsKey(key.ToStorageKey());
        return Task.FromResult(exists);
    }
}
