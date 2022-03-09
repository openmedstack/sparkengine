// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace Spark.Postgres
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Engine.Core;
    using Engine.Extensions;
    using Engine.Store.Interfaces;
    using Marten;

    public class MartenFhirStore : IFhirStore
    {
        private readonly Func<IDocumentSession> _sessionFunc;

        public MartenFhirStore(Func<IDocumentSession> sessionFunc) => _sessionFunc = sessionFunc;

        /// <inheritdoc />
        public async Task Add(Entry entry)
        {
            await using var session = _sessionFunc();
            if (entry.IsPresent)
            {
                var existing = await session.Query<EntryEnvelope>()
                    .Where(x => x.Id == entry.Key.ToStorageKey())
                    .ToListAsync();
                foreach (var envelope in existing)
                {
                    envelope.IsPresent = false;
                }
                session.Store<EntryEnvelope>(existing);
                await session.SaveChangesAsync();
            }

            session.Store(
                new EntryEnvelope
                {
                    Id = entry.Key.ToStorageKey(),
                    ResourceType = entry.Resource.TypeName,
                    ResourceKey = entry.Key.WithoutVersion().ToStorageKey(),
                    State = entry.State,
                    Key = entry.Key,
                    Method = entry.Method,
                    When = entry.When,
                    Resource = entry.Resource,
                    IsPresent = entry.IsPresent,
                    Deleted = entry.IsDelete
                });
            await session.SaveChangesAsync().ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<Entry> Get(IKey key)
        {
            await using var session = _sessionFunc();
            var result = key.HasVersionId()
                ? await session.LoadAsync<EntryEnvelope>(key.ToStorageKey()).ConfigureAwait(false)
                : await session.Query<EntryEnvelope>()
                    .Where(
                        x => x.ResourceType == key.TypeName
                             && x.Deleted == false
                             && x.IsPresent
                             && x.ResourceKey == key.WithoutVersion().ToStorageKey())
                    .OrderByDescending(x => x.When)
                    .FirstOrDefaultAsync()
                    .ConfigureAwait(false);

            return result == null ? null : Entry.Create(result.Method, result.Key, result.Resource);
        }

        /// <inheritdoc />
        public async Task<IList<Entry>> Get(IEnumerable<IKey> localIdentifiers)
        {
            var localKeys = localIdentifiers.Select(x => x.ToStorageKey()).ToArray();
            await using var session = _sessionFunc();
            var results = await session.Query<EntryEnvelope>()
                .Where(e => e.Id.IsOneOf(localKeys))
                .ToListAsync()
                .ConfigureAwait(false);
            return results.Select(result => Entry.Create(result.Method, result.Key, result.Resource)).ToList();
        }

        /// <inheritdoc />
        public async Task<bool> Exists(IKey key)
        {
            await using var session = _sessionFunc();
            var count = await session.Query<EntryEnvelope>()
                      .CountAsync(
                          x => x.Key.TypeName == key.TypeName
                               && x.Key.ResourceId == key.ResourceId
                               && x.Key.VersionId == key.VersionId).ConfigureAwait(false);
            return count > 0;
        }
    }
}