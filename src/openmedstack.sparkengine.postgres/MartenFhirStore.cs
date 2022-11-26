﻿// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace OpenMedStack.SparkEngine.Postgres
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Core;
    using Marten;
    using SparkEngine.Extensions;
    using Store.Interfaces;

    public class MartenFhirStore : IFhirStore
    {
        private readonly Func<IDocumentSession> _sessionFunc;

        public MartenFhirStore(Func<IDocumentSession> sessionFunc) => _sessionFunc = sessionFunc;

        /// <inheritdoc />
        public async Task Add(Entry entry)
        {
            if (entry.Key == null || entry.Resource == null)
            {
                return;
            }

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
                    ResourceId = entry.Resource.Id,
                    VersionId = entry.Resource.VersionId,
                    ResourceType = entry.Resource.TypeName,
                    ResourceKey = entry.Key.WithoutVersion().ToStorageKey(),
                    State = entry.State,
                    //Key = entry.Key,
                    Method = entry.Method,
                    When = entry.When ?? DateTimeOffset.MinValue,
                    Resource = entry.Resource,
                    IsPresent = entry.IsPresent,
                    Deleted = entry.IsDelete
                });
            await session.SaveChangesAsync().ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<Entry?> Get(IKey? key)
        {
            if (key == null)
            {
                return null;
            }

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

            return result == null ? null : Entry.Create(result.Method, Key.Create(result.ResourceType, result.ResourceId, result.VersionId), result.Resource);
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<Entry> Get(IEnumerable<IKey> localIdentifiers)
        {
            var localKeys = localIdentifiers.Select(x => x.ToStorageKey()).ToArray();
            await using var session = _sessionFunc();
            var results = session.Query<EntryEnvelope>().Where(e => e.Id.IsOneOf(localKeys)).ToAsyncEnumerable();
            await foreach (var result in results)
            {
                yield return Entry.Create(result.Method, Key.Create(result.ResourceType, result.ResourceId, result.VersionId), result.Resource);
            }
        }

        /// <inheritdoc />
        public async Task<bool> Exists(IKey? key)
        {
            if (key == null)
            {
                return false;
            }

            await using var session = _sessionFunc();
            var count = await session.Query<EntryEnvelope>()
                .CountAsync(
                    x => x.ResourceType == key.TypeName
                         && x.ResourceId == key.ResourceId
                         && x.VersionId == key.VersionId)
                .ConfigureAwait(false);
            return count > 0;
        }
    }
}
