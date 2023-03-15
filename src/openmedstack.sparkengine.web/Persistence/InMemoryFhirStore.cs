#nullable enable
namespace OpenMedStack.SparkEngine.Web.Persistence;

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Interfaces;
using OpenMedStack.SparkEngine.Core;
using OpenMedStack.SparkEngine.Extensions;
using Service;

public class InMemoryFhirStore : IFhirStore
{
    private readonly ITransfer _transfer;
    private readonly ConcurrentDictionary<string, Entry> _entries = new();

    public InMemoryFhirStore(ITransfer transfer)
    {
        _transfer = transfer;
    }

    /// <inheritdoc />
    public async Task<Entry> Add(Entry entry, CancellationToken cancellationToken = default)
    {
        if (entry.State != EntryState.Internal)
        {
            await _transfer.Internalize(entry, cancellationToken).ConfigureAwait(false);
        }

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

        return _transfer.Externalize(entry);
    }

    /// <inheritdoc />
    public Task<Entry?> Get(IKey key, CancellationToken cancellationToken = default)
    {
        if (key == null)
        {
            return Task.FromResult<Entry?>(null);
        }

        var result = _entries.TryGetValue(key.ToStorageKey(), out var entry) ? entry.IsDelete ? null : entry :
            _entries.TryGetValue(key.WithoutVersion().ToStorageKey(), out entry) ? entry.IsDelete ? null : entry : null;
        result = result == null ? null : _transfer.Externalize(result);
        return Task.FromResult(result);
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
                if (!entry.IsDelete)
                {
                    yield return _transfer.Externalize(entry);
                }
            }
        }
    }

    /// <inheritdoc />
    public Task<bool> Exists(IKey key, CancellationToken cancellationToken = default)
    {
        if (key == null)
        {
            return Task.FromResult(false);
        }

        var exists = _entries.ContainsKey(key.ToStorageKey());
        return Task.FromResult(exists);
    }
}