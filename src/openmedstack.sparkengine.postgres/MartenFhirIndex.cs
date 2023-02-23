// /*
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
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Core;
    using Hl7.Fhir.Rest;
    using Interfaces;
    using Marten;
    using Microsoft.Extensions.Logging;
    using Model;
    using Search.ValueExpressionTypes;
    using SparkEngine.Extensions;
    using Store.Interfaces;

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
            var resources = await GetIndexValues(resource, searchCommand).ConfigureAwait(false);

            var count = resources.Count;

            if (searchCommand.Count is > 0)
            {
                resources = resources.Take(searchCommand.Count.Value).ToList();
            }

            var keys = resources.ToList();
            var results = new SearchResults
            {
                MatchCount = count,
                UsedCriteria = searchCommand.Parameters.Select(t => Criterium.Parse(resource, t.Item1, t.Item2)).ToList()
            };

            results.AddRange(keys);

            return results;
        }

        /// <inheritdoc />
        public async Task<Key?> FindSingle(
            string resource,
            SearchParams searchCommand,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Find single {resource} key", resource);
            var entries = await GetIndexValues(resource, searchCommand).ConfigureAwait(false);

            return entries.Count > 0 ? Key.ParseOperationPath(entries[0]) : null;
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
            var values = indexValue.IndexValues().ToArray();
            var id = (StringValue)values.First(x => x.Name == "internal_forResource").Values[0];
            var resource = (StringValue)values.First(x => x.Name == "internal_resource").Values[0];
            var canonicalId = (StringValue)values.First(x => x.Name == "internal_id").Values[0];
            var entry = new IndexEntry(id.Value, canonicalId.Value, resource.Value, new Dictionary<string, object>());
            foreach (var value in values)
            {
                var array = value.Values.Select(GetValue).ToHashSet();
                var o = array.Count == 1 ? array.First() : array;
                if (value.Name != null && entry.Values.TryGetValue(value.Name, out var existing))
                {
                    switch (existing)
                    {
                        case HashSet<object> l:
                            switch (o)
                            {
                                case HashSet<object> list:
                                    foreach (var item in list)
                                    {
                                        l.Add(item);
                                    }

                                    break;
                                default:
                                    l.Add(o);
                                    break;
                            }

                            entry.Values[value.Name] = l;
                            break;
                        case { }:
                            entry.Values[value.Name] = new HashSet<object> { existing, o };
                            break;
                    }
                }
                else if (value.Name != null)
                {
                    entry.Values.Add(value.Name, o);
                }
            }

            session.DeleteWhere<IndexEntry>(x => x.CanonicalId == canonicalId.Value);
            session.Store(entry);
            await session.SaveChangesAsync().ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task Delete(Entry entry)
        {
            if (entry.Key == null)
            {
                return;
            }

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

        private static object GetValue(Expression expression)
        {
            return expression switch
            {
                StringValue stringValue => stringValue.Value,
                IndexValue indexValue => indexValue.Values.Count == 1
                    ? GetValue(indexValue.Values[0])
                    : indexValue.Values.Select(GetValue).ToArray(),
                CompositeValue compositeValue =>
                    compositeValue.Components.OfType<IndexValue>().All(x => x.Name == "code")
                        ? compositeValue.Components.OfType<IndexValue>()
                            .SelectMany(x => x.Values.Select(GetValue))
                            .First()
                        : compositeValue.Components.OfType<IndexValue>()
                            .Where(x => x.Name != null)
                            .ToDictionary(
                                component => component.Name!,
                                component =>
                                {
                                    var a = component.Values.Select(GetValue).ToArray();
                                    return a.Length == 1 ? a[0] : a;
                                }),
                _ => expression.ToString() ?? ""
            };
        }

        private async Task<List<string>> GetIndexValues(string resource, SearchParams searchCommand)
        {
            var criteria = searchCommand.Parameters.Select(t => Criterium.Parse(resource, t.Item1, t.Item2));
            var session = _sessionFunc();
            await using var _ = session.ConfigureAwait(false);
            var queryBuilder = new StringBuilder();
            queryBuilder.AppendFormat(@"where data -> 'Values' ->> 'internal_resource' = '{0}'", resource);
            queryBuilder = criteria.Aggregate(
                queryBuilder,
                (sb, c) => sb.AppendFormat(@" and data -> 'Values' {0}", GetComparison(c)));

            var sql = queryBuilder.ToString();
            _logger.LogDebug($"Executing query: {sql}");
            var result = await session.QueryAsync<IndexEntry>(sql).ConfigureAwait(false);

            return result.SelectMany(iv => iv.Values.Where(v => v.Key == "internal_id" || v.Key == "internal_selflink"))
                .Select(v => v.Value as string)
                .Where(x => x is not null)
                .Distinct()
                .Select(x => x!)
                .ToList();
        }

        private static string GetComparison(Criterium criterium)
        {
            return criterium.Operator switch
            {
                Operator.EQ => $"->> '{criterium.ParamName}' = '{GetValue(criterium.Operand!)}'",
                Operator.LT => $"->> '{criterium.ParamName}' < '{GetValue(criterium.Operand!)}'",
                Operator.LTE => $"->> '{criterium.ParamName}' <= '{GetValue(criterium.Operand!)}'",
                Operator.APPROX => $"->> '{criterium.ParamName}' = '{GetValue(criterium.Operand!)}'",
                Operator.GTE => $"->> '{criterium.ParamName}' >= '{GetValue(criterium.Operand!)}'",
                Operator.GT => $"->> '{criterium.ParamName}' > '{GetValue(criterium.Operand!)}'",
                Operator.ISNULL => $"->'{criterium.ParamName}' IS NULL",
                Operator.NOTNULL => $"-> '{criterium.ParamName}' IS NOT NULL",
                Operator.IN => $"-> '{criterium.ParamName}' ? {GetValue(criterium.Operand!)}",
                Operator.CHAIN => $"-> {criterium.ParamName} is null",
                Operator.NOT_EQUAL => $"->> '{criterium.ParamName}' != '{GetValue(criterium.Operand!)}'",
                Operator.STARTS_AFTER =>
                    $"-> '{criterium.ParamName}' -> 'start' ->> 0 > '{GetValue(criterium.Operand!)}'",
                Operator.ENDS_BEFORE => $"-> '{criterium.ParamName}' -> 'end' ->> 0 < '{GetValue(criterium.Operand!)}'",
                _ => throw new ArgumentOutOfRangeException(nameof(criterium))
            };
        }
    }
}