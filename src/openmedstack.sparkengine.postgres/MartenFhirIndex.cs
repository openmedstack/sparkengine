﻿// /*
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
using System.Threading;
using System.Threading.Tasks;
using Core;
using Hl7.Fhir.Rest;
using Interfaces;
using Marten;
using Marten.Linq.MatchesSql;
using Microsoft.Extensions.Logging;
using Model;
using Search.ValueExpressionTypes;
using SparkEngine.Extensions;

public class MartenFhirIndex : IFhirIndex, IIndexStore
{
    private readonly ILogger<MartenFhirIndex> _logger;
    private readonly Func<IDocumentSession> _sessionFunc;

    public MartenFhirIndex(ILogger<MartenFhirIndex> logger, Func<IDocumentSession> sessionFunc)
    {
        _logger = logger;
        _sessionFunc = sessionFunc;
    }

    /// <inheritdoc />
    Task IFhirIndex.Clean()
    {
        _logger.LogDebug("Clean requested");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<SearchResults> Search(
        string resource,
        SearchParams searchCommand,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("{resource} search requested with {searchCommand}", resource, searchCommand.ToUriParamList().ToQueryString());

        var session = _sessionFunc();
        await using var _ = session.ConfigureAwait(false);

        var resourceQuery = session.Query<IndexEntry>()
            .Where(x => x.ResourceType == resource);

        resourceQuery = searchCommand.Parameters.Select(tuple => Criterium.Parse(resource, tuple.Item1, tuple.Item2))
            .Select(criterium => $"(data -> 'values') @? '$.{criterium.ParamName} ? ({GetComparison(criterium)})'")
            .Aggregate(resourceQuery, (current, sql) => current.Where(x => x.MatchesSql(sql)));

        if (searchCommand.Count is > 0)
        {
            resourceQuery = resourceQuery.Take(searchCommand.Count.Value);
        }
        var resources = await resourceQuery.Select(x => new { x.Id, x.CanonicalId })
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var count = resources.Count;

        var results = new SearchResults
        {
            MatchCount = count,
            UsedCriteria = searchCommand.Parameters.Select(t => Criterium.Parse(resource, t.Item1, t.Item2)).ToArray()
        };

        results.AddRange(resources.SelectMany(x => new[] { x.Id, x.CanonicalId }).Where(x => x != null).Distinct());

        return results;
    }

    /// <inheritdoc />
    public async Task<Key?> FindSingle(
        string resource,
        SearchParams searchCommand,
        CancellationToken cancellationToken = default)
    {
        await Task.Yield();
        _logger.LogDebug("Find single {resource} key", resource);

        // TODO: Fix
        var entries = Array.Empty<string>();// await GetIndexValues(resource, searchCommand).ConfigureAwait(false);

        return entries.Length > 0 ? Key.ParseOperationPath(entries[0]) : null;
    }

    /// <inheritdoc />
    public Task<SearchResults> GetReverseIncludes(
        IList<IKey> keys,
        IList<string> revIncludes,
        CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    /// <inheritdoc />
    public async Task Save(IndexValue indexValue)
    {
        var session = _sessionFunc();
        await using var _ = session.ConfigureAwait(false);
        var entry = indexValue.BuildIndexEntry();

        session.DeleteWhere<IndexEntry>(x => x.Id == entry.Id);
        session.Store(entry);
        await session.SaveChangesAsync().ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task Delete(Entry entry)
    {
        var session = _sessionFunc();
        await using var _ = session.ConfigureAwait(false);
        session.Delete<IndexEntry>(entry.Key.ToStorageKey());
        await session.SaveChangesAsync().ConfigureAwait(false);
    }

    /// <inheritdoc />
    async Task IIndexStore.Clean()
    {
        _logger.LogDebug("Clean requested");
        var session = _sessionFunc();
        await using var _ = session.ConfigureAwait(false);
        session.DeleteWhere<IndexEntry>(e => e.Id != "");
        await session.SaveChangesAsync().ConfigureAwait(false);
    }

    private static string GetComparison(Criterium criterium)
    {
        return criterium.Operator switch
        {
            Operator.EQ => $"@ == \"{criterium.Operand!.GetValue()}\"",
            Operator.LT => $"@ < {criterium.Operand!.GetValue()}",
            Operator.LTE => $"@ <= {criterium.Operand!.GetValue()}",
            Operator.APPROX => $"@ == \"{criterium.Operand!.GetValue()}\"",
            Operator.GTE => $"@ >= {criterium.Operand!.GetValue()}",
            Operator.GT => $"@ > {criterium.Operand!.GetValue()}",
            Operator.ISNULL => "@ IS NULL",
            Operator.NOTNULL => "@ IS NOT NULL",
            Operator.IN => $"@ ? {criterium.Operand!.GetValue()}",
            Operator.CHAIN => "@ is null",
            Operator.NOT_EQUAL => $"@ != \"{criterium.Operand!.GetValue()}\"",
            Operator.STARTS_AFTER =>
                $"@.start[0] > {criterium.Operand!.GetValue()}",
            Operator.ENDS_BEFORE => $"@.end[0] < {criterium.Operand!.GetValue()}",
            _ => throw new ArgumentOutOfRangeException(nameof(criterium))
        };
    }
}