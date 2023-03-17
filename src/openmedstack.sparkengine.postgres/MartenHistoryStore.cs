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
using System.Threading.Tasks;
using Core;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Interfaces;
using Marten;
using SparkEngine.Extensions;

public class MartenHistoryStore : IHistoryStore
{
    private readonly Func<IDocumentSession> _sessionFunc;

    public MartenHistoryStore(Func<IDocumentSession> sessionFunc) => _sessionFunc = sessionFunc;

    /// <inheritdoc />
    public async Task<Snapshot> History(string typename, HistoryParameters parameters)
    {
        var session = _sessionFunc();
        await using var _ = session.ConfigureAwait(false);
        var query = session.Query<ResourceInfo>().Where(e => e.ResourceType == typename);
        if (parameters.Since.HasValue)
        {
            query = query.Where(x => x.When > parameters.Since.Value);
        }

        // TODO: Handle sort

        if (parameters.Count.HasValue)
        {
            query = query.Take(parameters.Count.Value);
        }

        var keys = await query.Select(x => x.ResourceKey)
            .ToListAsync()
            .ConfigureAwait(false);
        return CreateSnapshot(keys, keys.Count);
    }

    /// <inheritdoc />
    public async Task<Snapshot> History(IKey key, HistoryParameters parameters)
    {
        var storageKey = key.ToStorageKey();
        var session = _sessionFunc();
        await using var _ = session.ConfigureAwait(false);
        var query = session.Query<ResourceInfo>().Where(e => e.ResourceKey == storageKey);
        if (parameters.Since.HasValue)
        {
            query = query.Where(x => x.When > parameters.Since.Value);
        }

        // TODO: Handle sort

        if (parameters.Count.HasValue)
        {
            query = query.Take(parameters.Count.Value);
        }

        var result = await query.Select(x => x.ResourceKey)
            .ToListAsync()
            .ConfigureAwait(false);
        return CreateSnapshot(
            result,
            result.Count);
    }

    /// <inheritdoc />
    public async Task<Snapshot> History(HistoryParameters parameters)
    {
        var session = _sessionFunc();
        await using var _ = session.ConfigureAwait(false);
        IQueryable<ResourceInfo> query = session.Query<ResourceInfo>();
        if (parameters.Since.HasValue)
        {
            query = query.Where(x => x.When > parameters.Since.Value);
        }

        // TODO: Handle sort

        if (parameters.Count.HasValue)
        {
            query = query.Take(parameters.Count.Value);
        }

        var result = await query.Select(x => x.ResourceKey)
            .ToListAsync()
            .ConfigureAwait(false);
        return CreateSnapshot(
            result,
            result.Count);
    }

    private static Snapshot CreateSnapshot(
        IReadOnlyList<string> keys,
        int? count = null,
        IList<string>? includes = null,
        IList<string>? reverseIncludes = null) =>
        Snapshot.Create(
            Bundle.BundleType.History,
            new Uri(TransactionBuilder.HISTORY, UriKind.Relative),
            keys,
            "history",
            count,
            includes ?? Array.Empty<string>(),
            reverseIncludes ?? Array.Empty<string>());
}