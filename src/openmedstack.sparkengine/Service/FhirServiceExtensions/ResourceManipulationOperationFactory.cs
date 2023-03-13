namespace OpenMedStack.SparkEngine.Service.FhirServiceExtensions;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core;
using Extensions;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Interfaces;

public static partial class ResourceManipulationOperationFactory
{
    private static readonly Dictionary<Bundle.HTTPVerb, Func<Resource, IKey, ISearchService, SearchParams?, CancellationToken, Task<ResourceManipulationOperation>>> _asyncBuilders;

    static ResourceManipulationOperationFactory()
    {
        _asyncBuilders = new Dictionary<Bundle.HTTPVerb, Func<Resource, IKey, ISearchService, SearchParams?, CancellationToken, Task<ResourceManipulationOperation>>>
        {
            { Bundle.HTTPVerb.POST, CreatePost },
            { Bundle.HTTPVerb.PUT, CreatePut },
            { Bundle.HTTPVerb.PATCH, CreatePatch },
            { Bundle.HTTPVerb.DELETE, CreateDelete },
            { Bundle.HTTPVerb.GET, CreateGet },
        };
    }

    private static async Task<SearchResults?> GetSearchResult(IKey key, ISearchService searchService, SearchParams? searchParams = null, CancellationToken cancellationToken = default)
    {
        if (key.TypeName == null || searchParams == null || searchParams.Parameters.Count == 0)
        {
            return null;
        }

        return await searchService.GetSearchResults(key.TypeName, searchParams, cancellationToken).ConfigureAwait(false);
    }

    public static async Task<ResourceManipulationOperation> CreatePost(this Resource resource, IKey key, ISearchService searchService, SearchParams? searchParams = null, CancellationToken cancellationToken = default)
    {
        return new PostManipulationOperation(resource, key, await GetSearchResult(key, searchService, searchParams, cancellationToken).ConfigureAwait(false), searchParams);
    }

    public static async Task<ResourceManipulationOperation> CreatePut(this Resource resource, IKey key, ISearchService searchService, SearchParams? searchParams = null, CancellationToken cancellationToken = default)
    {
        return new PutManipulationOperation(resource, key, await GetSearchResult(key, searchService, searchParams, cancellationToken).ConfigureAwait(false), searchParams);
    }

    private static async Task<ResourceManipulationOperation> CreatePatch(Resource resource, IKey key, ISearchService searchService, SearchParams? searchParams = null, CancellationToken cancellationToken = default)
    {
        return new PatchManipulationOperation(resource, key, await GetSearchResult(key, searchService, searchParams, cancellationToken).ConfigureAwait(false), searchParams);
    }

    public static async Task<ResourceManipulationOperation> CreateDelete(IKey key, ISearchService searchService, SearchParams? searchParams = null, CancellationToken cancellationToken = default)
    {
        return new DeleteManipulationOperation(null, key, await GetSearchResult(key, searchService, searchParams, cancellationToken).ConfigureAwait(false), searchParams);
    }

    private static async Task<ResourceManipulationOperation> CreateDelete(Resource resource, IKey key, ISearchService searchService, SearchParams? searchParams = null, CancellationToken cancellationToken = default)
    {
        return new DeleteManipulationOperation(null, key, await GetSearchResult(key, searchService, searchParams, cancellationToken).ConfigureAwait(false), searchParams);
    }

    private static async Task<ResourceManipulationOperation> CreateGet(Resource resource, IKey key, ISearchService searchService, SearchParams? searchParams, CancellationToken cancellationToken = default)
    {
        return new GetManipulationOperation(resource, key, await GetSearchResult(key, searchService, searchParams, cancellationToken).ConfigureAwait(false), searchParams);
    }

    public static async Task<ResourceManipulationOperation> GetManipulationOperation(Bundle.EntryComponent entryComponent, ILocalhost localhost, ISearchService searchService, CancellationToken cancellationToken = default)
    {
        var method = localhost.ExtrapolateMethod(entryComponent, null);
        var key = localhost.ExtractKey(entryComponent) ?? throw new NullReferenceException("Cannot identify key");
        var searchUri = GetSearchUri(entryComponent, method);

        var searchParams = searchUri != null ? ParseQueryString(localhost, searchUri) : null;
        return await _asyncBuilders[method](entryComponent.Resource, key, searchService, searchParams, cancellationToken)
            .ConfigureAwait(false);
    }

    private static Uri? GetSearchUri(Bundle.EntryComponent entryComponent, Bundle.HTTPVerb method)
    {
        return method switch
        {
            Bundle.HTTPVerb.POST => PostManipulationOperation.ReadSearchUri(entryComponent),
            Bundle.HTTPVerb.PUT => PutManipulationOperation.ReadSearchUri(entryComponent),
            Bundle.HTTPVerb.DELETE => DeleteManipulationOperation.ReadSearchUri(entryComponent),
            Bundle.HTTPVerb.GET => FhirServiceExtensions.GetManipulationOperation.ReadSearchUri(entryComponent),
            _ => null
        };
    }

    private static SearchParams ParseQueryString(ILocalhost localhost, Uri searchUri)
    {
        var absoluteUri = localhost.Absolute(searchUri);
        var keysCollection = ParseQueryString(absoluteUri);

        return SearchParams.FromUriParamList(keysCollection);
    }

    private static IEnumerable<Tuple<string, string>> ParseQueryString(this Uri uri)
    {
        var query = uri.Query ?? throw new ArgumentNullException(nameof(uri));
        return query.Trim('?')
            .Split('&')
            .Select(x => x.Split('='))
            .Where(x => x.Length == 2)
            .Select(x => Tuple.Create(Uri.UnescapeDataString(x[0]), Uri.UnescapeDataString(x[1])));
    }
}