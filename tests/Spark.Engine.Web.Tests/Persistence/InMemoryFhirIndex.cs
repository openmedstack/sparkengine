namespace Spark.Engine.Web.Tests.Persistence
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Core;
    using Engine.Extensions;
    using Hl7.Fhir.Rest;
    using Interfaces;
    using Microsoft.Extensions.Logging;
    using Model;
    using Search.ValueExpressionTypes;
    using Store.Interfaces;

    public class InMemoryFhirIndex : IFhirIndex, IIndexStore
    {
        // private static readonly Regex IdentifierRegex = new Regex(@"[person|patient]\.Identifier", RegexOptions.Compiled);
        private readonly ILogger<InMemoryFhirIndex> _logger;
        private readonly List<IndexValue> _indexValues = new();

        public InMemoryFhirIndex(ILogger<InMemoryFhirIndex> logger)
        {
            _logger = logger;
        }

        private void Clean()
        {
            _logger.LogDebug("Clean requested");
        }

        /// <inheritdoc />
        Task IIndexStore.Clean()
        {
            Clean();
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task Save(IndexValue indexValue)
        {
            lock (_indexValues)
            {
                _indexValues.Add(indexValue);
            }
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task Delete(Entry entry)
        {
            lock (_indexValues)
            {
                _ = _indexValues.RemoveAll(
                    x => x.Values.OfType<IndexValue>()
                        .Any(
                            v => v.Name == "internal_id"
                                 && v.Values.OfType<StringValue>()
                                     .All(sv => sv.Value == entry.Key.WithoutVersion().ToStorageKey())));
            }
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task<SearchResults> Search(string resource, SearchParams searchCommand)
        {
            _logger.LogDebug(
                "{resource} search requested with {searchCommand}",
                resource,
                searchCommand.ToUriParamList().ToQueryString());

            var resources = GetIndexValues(resource, searchCommand).ToArray();

            var count = resources.Length;

            if (searchCommand.Count is > 0)
            {
                resources = resources.Take(searchCommand.Count.Value).ToArray();
            }

            var results = new SearchResults
            {
                MatchCount = count,
                UsedCriteria = searchCommand.Parameters.Select(t => Criterium.Parse(resource, t.Item1, t.Item2))
                    .ToList()
            };

            results.AddRange(resources);

            return Task.FromResult(results);
        }

        /// <inheritdoc />
        public Task<Key> FindSingle(string resource, SearchParams searchCommand)
        {
            _logger.LogDebug($"Find single {resource} key");
            var indexValue = GetIndexValues(resource, searchCommand).FirstOrDefault();

            var key = indexValue == null ? null : Key.ParseOperationPath(indexValue);
            return Task.FromResult(key);
        }

        /// <inheritdoc />
        public Task<SearchResults> GetReverseIncludes(IList<IKey> keys, IList<string> revIncludes)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        Task IFhirIndex.Clean()
        {
            Clean();
            return Task.CompletedTask;
        }

        private IEnumerable<string> GetIndexValues(string resource, SearchParams searchCommand)
        {
            lock (_indexValues)
            {
                return _indexValues
                    .Where(
                        x => x.Values.OfType<IndexValue>()
                            .Any(
                                v => v.Name == "internal_resource"
                                     && v.Values.OfType<StringValue>().First().Value == resource))
                    .Where(
                        x => searchCommand.Parameters.All(
                            kv => x.Values.OfType<IndexValue>()
                                .Any(
                                    // TODO: Apply proper criteria evaluation
                                    v => v.Name == kv.Item1
                                         && v.Values.OfType<StringValue>().Any(v2 => v2.Value.Equals(kv.Item2)))))
                    .SelectMany(
                        iv => iv.Values.OfType<IndexValue>()
                            .Where(v => v.Name == "internal_id" || v.Name == "internal_selflink")
                            .Select(v => v.Values.OfType<StringValue>().First().Value))
                    .Distinct();
            }
        }
    }
}