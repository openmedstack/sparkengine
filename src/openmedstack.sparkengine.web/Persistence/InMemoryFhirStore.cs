namespace OpenMedStack.SparkEngine.Web.Persistence;

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Hl7.Fhir.Model;
using Interfaces;
using OpenMedStack.SparkEngine.Core;
using OpenMedStack.SparkEngine.Extensions;
using Task = System.Threading.Tasks.Task;

public class InMemoryFhirStore : IFhirStore
{
    private readonly ConcurrentDictionary<string, Entry> _entries = new();
    
    /// <inheritdoc />
    public Task<Entry> Add(Entry entry, CancellationToken cancellationToken = default)
    {
        if (entry.IsDelete)
        {
            _entries.Remove(entry.Key!.ToStorageKey(), out _);
            _entries.Remove(entry.Key!.WithoutVersion().ToStorageKey(), out _);
        }
        else
        {
            _entries.AddOrUpdate(
                entry.Key!.ToStorageKey(),
                entry,
                (_, _) => entry);
            _entries.AddOrUpdate(
                entry.Key!.WithoutVersion().ToStorageKey(),
                entry,
                (_, _) => entry);
        }

        return Task.FromResult(entry);
    }

    /// <inheritdoc />
    public Task<ResourceInfo?> Get(IKey key, CancellationToken cancellationToken = default)
    {
        var result = _entries.TryGetValue(key.ToStorageKey(), out var entry) ? entry.IsDelete ? null : entry :
            _entries.TryGetValue(key.WithoutVersion().ToStorageKey(), out entry) ? entry.IsDelete ? null : entry : null;

        return Task.FromResult(result == null ? null : ResourceInfo.FromEntry(result));
    }

    /// <inheritdoc />
    public Task<Resource?> Load(IKey key, CancellationToken cancellationToken = default)
    {
        if (_entries.TryGetValue(key.ToStorageKey(),out var entry))
        {
            return Task.FromResult(entry.Resource);
        }

        return Task.FromResult<Resource?>(null);
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<ResourceInfo> Get(
        IEnumerable<IKey> localIdentifiers,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await Task.Yield();
        foreach (var localIdentifier in localIdentifiers)
        {
            if (_entries.TryGetValue(localIdentifier.ToStorageKey(), out var entry))
            {
                if (!entry.IsDelete)
                {
                    yield return ResourceInfo.FromEntry(entry);
                }
            }
        }
    }

    /// <inheritdoc />
    public Task<bool> Exists(IKey key, CancellationToken cancellationToken = default)
    {
        var exists = _entries.ContainsKey(key.ToStorageKey());
        return Task.FromResult(exists);
    }
}