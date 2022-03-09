namespace Spark.Engine.Web.Tests.Persistence
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Core;
    using Engine.Extensions;
    using Store.Interfaces;

    public class InMemoryFhirStore : IFhirStore
    {
        private readonly List<Entry> _entries = new();

        /// <inheritdoc />
        public Task Add(Entry entry)
        {
            if (entry == null)
            {
                return Task.CompletedTask;
            }

            lock (_entries)
            {
                if (entry.IsDelete)
                {
                    _entries.RemoveAll(
                        x => x.Key.EqualTo(entry.Key) || x.Key.EqualTo(entry.Key.WithoutBase().WithoutVersion()));
                }
                else
                {
                    _entries.Add(entry);
                }
            }
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task<Entry> Get(IKey key)
        {
            lock (_entries)
            {
                var result = _entries.FirstOrDefault(x => key.ToStorageKey().Equals(x.Key.ToStorageKey()) && !x.IsDelete)
                       ?? _entries.Where(
                               x => key.WithoutVersion().ToStorageKey().Equals(x.Key.WithoutVersion().ToStorageKey())
                                    && !x.IsDelete)
                           .OrderByDescending(x => x.When)
                           .FirstOrDefault();
                return Task.FromResult(result);
            }
        }

        /// <inheritdoc />
        public Task<IList<Entry>> Get(IEnumerable<IKey> localIdentifiers)
        {
            var storageKeys = localIdentifiers.Select(x => x.ToStorageKey()).ToArray();
            lock (_entries)
            {
                var result = _entries.Where(x => !x.IsDelete && storageKeys.Contains(x.Key.ToStorageKey())).ToArray();
                return Task.FromResult<IList<Entry>>(result);
            }
        }

        /// <inheritdoc />
        public Task<bool> Exists(IKey key)
        {
            lock (_entries)
            {
                var exists = _entries.Any(e => e.Key.EqualTo(key));
                return Task.FromResult(exists);
            }
        }
    }
}
