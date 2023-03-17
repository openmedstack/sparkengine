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
using Hl7.Fhir.Model;
using Interfaces;
using Marten;
using Microsoft.Extensions.Logging;
using SparkEngine.Extensions;

public class MartenResourcePersistence : IResourcePersistence
{
    private readonly Func<IDocumentSession> _sessionFunc;
    private readonly ILogger<MartenResourcePersistence> _logger;

    public MartenResourcePersistence(Func<IDocumentSession> sessionFunc, ILogger<MartenResourcePersistence> logger)
    {
        _sessionFunc = sessionFunc;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<bool> Store(Resource resource, CancellationToken cancellationToken)
    {
        try
        {
            var session = _sessionFunc();
            await using var _ = session.ConfigureAwait(false);
            session.Store(resource);
            await session.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{error}", ex.Message);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<Resource?> Get(IKey key, CancellationToken cancellationToken)
    {
        try
        {
            var session = _sessionFunc();
            await using var _ = session.ConfigureAwait(false);
            var resource = await session.LoadAsync<Resource>(key.ResourceId!, cancellationToken);
            return resource;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{error}", ex.Message);
            return null;
        }
    }
}

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
        if (entry.Resource == null)
        {
            return entry;
        }

        var session = _sessionFunc();
        await using var _ = session.ConfigureAwait(false);
        if (entry.IsPresent)
        {
            var existing = await session.Query<ResourceInfo>()
                .Where(x => x.Id == entry.Key.ToStorageKey())
                .ToListAsync(token: cancellationToken).ConfigureAwait(false);
            existing = existing.Select(envelope => envelope with { IsPresent = false }).ToArray();

            session.Store<ResourceInfo>(existing);
            await session.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        var persisted = await _persistence.Store(entry.Resource, cancellationToken).ConfigureAwait(false);
        var entryEnvelope = new ResourceInfo
        {
            Id = entry.Key.ToStorageKey(),
            VersionId = entry.Resource.VersionId,
            ResourceType = entry.Resource.TypeName,
            ResourceKey = entry.Key.ToStorageKey(),
            State = entry.State,
            Method = entry.Method,
            When = entry.When ?? DateTimeOffset.MinValue,
            IsPresent = entry.IsPresent,
            IsDeleted = entry.IsDelete,
            HasResource = entry.HasResource(),
            ResourceId = entry.Resource?.Id
        };
        session.Store(entryEnvelope);
        await session.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return entry;
    }

    /// <inheritdoc />
    public async Task<ResourceInfo?> Get(IKey key, CancellationToken cancellationToken = default)
    {
        var session = _sessionFunc();
        await using var _ = session.ConfigureAwait(false);
        var result = key.HasVersionId()
            ? await session.LoadAsync<ResourceInfo>(key.ToStorageKey(), cancellationToken).ConfigureAwait(false)
            : await session.Query<ResourceInfo>()
                .Where(
                    x => x.ResourceType == key.TypeName
                         && x.IsDeleted == false
                         && x.IsPresent
                         && x.ResourceId == key.ResourceId)
                .OrderByDescending(x => x.When)
                .FirstOrDefaultAsync(token: cancellationToken)
                .ConfigureAwait(false);
        return result;
    }

    /// <inheritdoc />
    public Task<Resource?> Load(IKey key, CancellationToken cancellationToken = default)
    {
        return _persistence.Get(key, cancellationToken);
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<ResourceInfo> Get(
        IEnumerable<IKey> localIdentifiers,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var localKeys = localIdentifiers.Select(x => x.ToStorageKey()).ToArray();
        var session = _sessionFunc();
        await using var _ = session.ConfigureAwait(false);
        var results = session.Query<ResourceInfo>()
            .Where(e => e.Id.IsOneOf(localKeys))
            .ToAsyncEnumerable(token: cancellationToken);
        await foreach (var result in results.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            yield return result;
        }
    }

    /// <inheritdoc />
    public async Task<bool> Exists(IKey key, CancellationToken cancellationToken = default)
    {
        var session = _sessionFunc();
        await using var _ = session.ConfigureAwait(false);
        var count = await session.Query<ResourceInfo>()
            .CountAsync(
                x => x.ResourceType == key.TypeName
                     && x.Id == key.ResourceId
                     && x.VersionId == key.VersionId,
                token: cancellationToken)
            .ConfigureAwait(false);
        return count > 0;
    }
}