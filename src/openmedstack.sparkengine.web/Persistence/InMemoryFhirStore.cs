#nullable enable
namespace OpenMedStack.SparkEngine.Web.Persistence
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using OpenMedStack.SparkEngine.Core;
    using OpenMedStack.SparkEngine.Extensions;
    using OpenMedStack.SparkEngine.Store.Interfaces;

    public class InMemoryFhirStore : IFhirStore
    {
        private readonly List<Entry> _entries = new();

        /// <inheritdoc />
        public Task Add(Entry entry, CancellationToken cancellationToken = default)
        {
            lock (_entries)
            {
                if (entry.IsDelete)
                {
                    _entries.RemoveAll(
                        x => x.Key.EqualTo(entry.Key) || entry.Key != null && x.Key.EqualTo(entry.Key.WithoutBase().WithoutVersion()));
                }
                else
                {
                    _entries.Add(entry);
                }
            }
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task<Entry?> Get(IKey? key, CancellationToken cancellationToken = default)
        {
            if (key == null)
            {
                return Task.FromResult<Entry?>(null);
            }
            lock (_entries)
            {
                var entries = _entries.Where(x => x.Key != null).ToArray();
                var result = entries
                                 .FirstOrDefault(x => key.ToStorageKey().Equals(x.Key!.ToStorageKey()) && !x.IsDelete)
                             ?? entries.Where(
                                     x => key.WithoutVersion().ToStorageKey().Equals(x.Key!.WithoutVersion().ToStorageKey())
                                          && !x.IsDelete)
                                 .OrderByDescending(x => x.When)
                                 .FirstOrDefault();
                return Task.FromResult(result);
            }
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<Entry> Get(
            IEnumerable<IKey> localIdentifiers,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            var storageKeys = localIdentifiers.Select(x => x.ToStorageKey()).ToArray();
            lock (_entries)
            {
                var result = _entries.Where(x => x is { IsDelete: false, Key: { } } && storageKeys.Contains(x.Key.ToStorageKey()));
                foreach (var entry in result)
                {
                    yield return entry;
                }
            }
        }

        /// <inheritdoc />
        public Task<bool> Exists(IKey? key, CancellationToken cancellationToken = default)
        {
            if (key == null)
            {
                return Task.FromResult(false);
            }
            lock (_entries)
            {
                var exists = _entries.Any(e => e.Key.EqualTo(key));
                return Task.FromResult(exists);
            }
        }
    }
}
