using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Spark.Engine.Core;
using Spark.Engine.Extensions;

namespace Spark.Engine.Service.FhirServiceExtensions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Core;
    using Extensions;
    using Hl7.Fhir.Model;
    using Hl7.Fhir.Rest;

    public static partial class ResourceManipulationOperationFactory
    {
        private static readonly Dictionary<Bundle.HTTPVerb, Func<Resource, IKey, ISearchService, SearchParams, Task<ResourceManipulationOperation>>> _asyncBuilders;

        static ResourceManipulationOperationFactory()
        {
            _asyncBuilders = new Dictionary<Bundle.HTTPVerb, Func<Resource, IKey, ISearchService, SearchParams, Task<ResourceManipulationOperation>>>
            {
                { Bundle.HTTPVerb.POST, CreatePost },
                { Bundle.HTTPVerb.PUT, CreatePut },
                { Bundle.HTTPVerb.PATCH, CreatePatch },
                { Bundle.HTTPVerb.DELETE, CreateDelete },
                { Bundle.HTTPVerb.GET, CreateGet },
            };
        }
        
        private static async Task<SearchResults> GetSearchResult(IKey key, ISearchService searchService, SearchParams searchParams = null)
        {
            if (searchParams == null || searchParams.Parameters.Count == 0)
                return null;
            if (searchParams != null && searchService == null)
                throw new InvalidOperationException("Unallowed operation");
            return await searchService.GetSearchResults(key.TypeName, searchParams).ConfigureAwait(false);
        }
        
        public static async Task<ResourceManipulationOperation> CreatePost(this Resource resource, IKey key, ISearchService searchService = null, SearchParams searchParams = null)
        {
            return new PostManipulationOperation(resource, key, await GetSearchResult(key, searchService, searchParams).ConfigureAwait(false), searchParams);
        }
        
        public static async Task<ResourceManipulationOperation> CreatePut(this Resource resource, IKey key, ISearchService searchService = null, SearchParams searchParams = null)
        {
            return new PutManipulationOperation(resource, key, await GetSearchResult(key, searchService, searchParams).ConfigureAwait(false), searchParams);
        }
        
        private static async Task<ResourceManipulationOperation> CreatePatch(Resource resource, IKey key, ISearchService searchService = null, SearchParams searchParams = null)
        {
            return new PatchManipulationOperation(resource, key, await GetSearchResult(key, searchService, searchParams), searchParams);
        }
        
        public static async Task<ResourceManipulationOperation> CreateDelete(IKey key, ISearchService searchService = null, SearchParams searchParams = null)
        {
            return new DeleteManipulationOperation(null, key, await GetSearchResult(key, searchService, searchParams).ConfigureAwait(false), searchParams);
        }
        
        private static async Task<ResourceManipulationOperation> CreateDelete(Resource resource, IKey key, ISearchService searchService = null, SearchParams searchParams = null)
        {
            return new DeleteManipulationOperation(null, key, await GetSearchResult(key, searchService, searchParams).ConfigureAwait(false), searchParams);
        }
        
        private static async Task<ResourceManipulationOperation> CreateGet(Resource resource, IKey key, ISearchService searchService, SearchParams searchParams)
        {
            return new GetManipulationOperation(resource, key, await GetSearchResult(key, searchService, searchParams), searchParams);
        }
        
        public static async Task<ResourceManipulationOperation> GetManipulationOperation(Bundle.EntryComponent entryComponent, ILocalhost localhost, ISearchService searchService = null)
        {
            Bundle.HTTPVerb method = localhost.ExtrapolateMethod(entryComponent, null);
            Key key = localhost.ExtractKey(entryComponent);
            var searchUri = GetSearchUri(entryComponent, method);

            var searchParams = searchUri != null ? ParseQueryString(localhost, searchUri) : null;
            return await _asyncBuilders[method](entryComponent.Resource, key, searchService, searchParams)
                .ConfigureAwait(false);
        }

        private static Uri GetSearchUri(Bundle.EntryComponent entryComponent, Bundle.HTTPVerb method)
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
            var query = uri?.Query ?? throw new ArgumentNullException(nameof(uri));
            return query.Trim('?')
                .Split('&')
                .Select(x => x.Split('='))
                .Where(x => x.Length == 2)
                .Select(x => Tuple.Create(Uri.UnescapeDataString(x[0]), Uri.UnescapeDataString(x[1])));
        }
    }
}