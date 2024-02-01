using Hl7.Fhir.Rest;
using Microsoft.Extensions.Logging;
using OpenMedStack.SparkEngine.Core;
using OpenMedStack.SparkEngine.Extensions;
using OpenMedStack.SparkEngine.Interfaces;
using OpenMedStack.SparkEngine.Model;
using OpenMedStack.SparkEngine.Search.ValueExpressionTypes;

namespace OpenMedStack.SparkEngine.Persistence;

public class InMemoryFhirIndex(ILogger<InMemoryFhirIndex> logger) : IFhirIndex, IIndexStore
{
    private readonly List<IndexValue> _indexValues = new();

    private void Clean()
    {
        logger.LogDebug("Clean requested");
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
    public Task<SearchResults> Search(
        string resource,
        SearchParams searchCommand,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug(
            "{Resource} search requested with {SearchCommand}",
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
                .ToArray()
        };

        results.AddRange(resources);

        return Task.FromResult(results);
    }

    /// <inheritdoc />
    public Task<Key?> FindSingle(
        string resource,
        SearchParams searchCommand,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Find single {Resource} key", resource);
        var indexValue = GetIndexValues(resource, searchCommand).FirstOrDefault();

        var key = indexValue == null ? null : Key.ParseOperationPath(indexValue);
        return Task.FromResult(key);
    }

    /// <inheritdoc />
    public Task<SearchResults> GetReverseIncludes(
        IReadOnlyList<IKey> keys,
        IReadOnlyList<string> revIncludes,
        CancellationToken cancellationToken = default)
    {
        var array = keys.SelectMany(key => revIncludes
                .SelectMany(x =>
                {
                    var indexOf = x.IndexOf('.');
                    var property = x[(indexOf + 1)..];
                    var resource = x[..indexOf];

                    return GetIndexValues(resource, new SearchParams().Add(property, key.ToStorageKey()));
                }))
            .ToArray();
        var results = new SearchResults { MatchCount = array.Length, UsedCriteria = [] };
        results.AddRange(array);
        return Task.FromResult(results);
    }

    public Task<SearchResults> GetIncludes(
        IReadOnlyList<IKey> keys,
        IReadOnlyList<string> includes,
        CancellationToken cancellationToken)
    {
        var array = keys.SelectMany(key => includes
                .SelectMany(x =>
                {
                    var indexOf = x.IndexOf('.');
                    var property = x[(indexOf + 1)..];

                    return GetIndexValues("", new SearchParams().Add(property, key.ToStorageKey()));
                }))
            .ToArray();
        var results = new SearchResults { MatchCount = array.Length, UsedCriteria = [] };
        results.AddRange(array);
        return Task.FromResult(results);
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
                .Where(ResourceSelection)
                .Where(Evaluate).SelectMany(Selector).Distinct();
        }

        bool ResourceSelection(IndexValue x) => resource == ""
          || x.Values.OfType<IndexValue>()
                .Any(v => v.Name == "internal_resource" && v.Values.OfType<StringValue>().First().Value == resource);

        bool Predicate(Expression exp, string value)
        {
            return exp switch
            {
                StringValue stringValue => stringValue.Value == value,
                CompositeValue compositeValue => compositeValue.Components.Any(c => Predicate(c, value)),
                IndexValue indexValue => indexValue.Values.Any(v => Predicate(v, value)),
                _ => false
            };
        }

        bool Evaluate(IndexValue x)
        {
            return searchCommand.Parameters.All(kv => x.Values.OfType<IndexValue>()
                .Any(
                    v => v.Name == kv.Item1 && v.Values.Any(exp => Predicate(exp, kv.Item2))));
        }

        IEnumerable<string> Selector(IndexValue iv) =>
            iv.Values.OfType<IndexValue>()
                .Where(v => v.Name is "internal_id" or "internal_selflink")
                .Select(v => v.Values.OfType<StringValue>().First().Value);
    }
}
