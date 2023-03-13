// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace OpenMedStack.SparkEngine.Postgres;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Core;
using Interfaces;
using Marten;
using SparkEngine.Extensions;
using Task = System.Threading.Tasks.Task;

public class MartenFhirStore : IFhirStore
{
    private readonly Func<IDocumentSession> _sessionFunc;
    private readonly IResourcePersistence _persistence;

    public MartenFhirStore(Func<IDocumentSession> sessionFunc, IResourcePersistence persistence)
    {
        _sessionFunc = sessionFunc;
        _persistence = persistence;
    }

    /// <inheritdoc />
    public async Task<Entry> Add(Entry entry, CancellationToken cancellationToken = default)
    {
        if (entry.Key == null || entry.Resource == null)
        {
            return entry;
        }

        var session = _sessionFunc();
        await using var _ = session.ConfigureAwait(false);
        if (entry.IsPresent)
        {
            var existing = await session.Query<EntryEnvelope>()
                .Where(x => x.Id == entry.Key.ToStorageKey())
                .ToListAsync(token: cancellationToken).ConfigureAwait(false);
            foreach (var envelope in existing)
            {
                envelope.IsPresent = false;
            }

            session.Store<EntryEnvelope>(existing);
            await session.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        var persisted = await _persistence.Store(entry.Resource, cancellationToken).ConfigureAwait(false);
        var entryEnvelope = new EntryEnvelope
        {
            Id = entry.Key.ToStorageKey(),
            ResourceId = entry.Resource.Id,
            VersionId = entry.Resource.VersionId,
            ResourceType = entry.Resource.TypeName,
            ResourceKey = entry.Key.WithoutVersion().ToStorageKey(),
            State = entry.State,
            Method = entry.Method,
            When = entry.When ?? DateTimeOffset.MinValue,
            Resource = persisted ? null : entry.Resource,
            StorageKey = persisted ? entry.Key.ToStorageKey() : null,
            IsPresent = entry.IsPresent,
            Deleted = entry.IsDelete
        };
        session.Store(entryEnvelope);
        await session.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return entry;
    }

    /// <inheritdoc />
    public async Task<Entry?> Get(IKey key, CancellationToken cancellationToken = default)
    {
        if (key == null)
        {
            return null;
        }

        var session = _sessionFunc();
        await using var _ = session.ConfigureAwait(false);
        var result = key.HasVersionId()
            ? await session.LoadAsync<EntryEnvelope>(key.ToStorageKey(), cancellationToken).ConfigureAwait(false)
            : await session.Query<EntryEnvelope>()
                .Where(
                    x => x.ResourceType == key.TypeName
                         && x.Deleted == false
                         && x.IsPresent
                         && x.ResourceKey == key.WithoutVersion().ToStorageKey())
                .OrderByDescending(x => x.When)
                .FirstOrDefaultAsync(token: cancellationToken)
                .ConfigureAwait(false);

        var resource = result?.Resource;
        if (resource == null && result?.StorageKey != null)
        {
            resource = await _persistence.Get(Key.ParseOperationPath(result.StorageKey), cancellationToken).ConfigureAwait(false);
        }
        return result == null
            ? null
            : Entry.Create(
                result.Method,
                Key.Create(result.ResourceType, result.ResourceId, result.VersionId),
                resource);
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<Entry> Get(
        IEnumerable<IKey> localIdentifiers,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var localKeys = localIdentifiers.Select(x => x.ToStorageKey()).ToArray();
        var session = _sessionFunc();
        await using var _ = session.ConfigureAwait(false);
        var results = session.Query<EntryEnvelope>()
            .Where(e => e.Id.IsOneOf(localKeys))
            .ToAsyncEnumerable(token: cancellationToken);
        await foreach (var result in results.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            var resource = result.Resource;
            if (resource == null && result.StorageKey != null)
            {
                resource = await _persistence.Get(Key.ParseOperationPath(result.StorageKey), cancellationToken).ConfigureAwait(false);
            }
            yield return Entry.Create(
                result.Method,
                Key.Create(result.ResourceType, result.ResourceId, result.VersionId),
                resource);
        }
    }

    /// <inheritdoc />
    public async Task<bool> Exists(IKey key, CancellationToken cancellationToken = default)
    {
        if (key == null)
        {
            return false;
        }

        var session = _sessionFunc();
        await using var _ = session.ConfigureAwait(false);
        var count = await session.Query<EntryEnvelope>()
            .CountAsync(
                x => x.ResourceType == key.TypeName
                     && x.ResourceId == key.ResourceId
                     && x.VersionId == key.VersionId,
                token: cancellationToken)
            .ConfigureAwait(false);
        return count > 0;
    }
}