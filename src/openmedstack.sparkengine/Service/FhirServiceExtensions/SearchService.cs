/* 
 * Copyright (c) 2016, Furore (info@furore.com) and contributors
 * Copyright (c) 2021, Incendi (info@incendi.no) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/spark/stu3/master/LICENSE
 */
namespace OpenMedStack.SparkEngine.Service.FhirServiceExtensions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;
    using Core;
    using Extensions;
    using Hl7.Fhir.Model;
    using Hl7.Fhir.Rest;
    using Interfaces;
    using Task = System.Threading.Tasks.Task;

    public class SearchService : ISearchService
    {
        private readonly IFhirIndex _fhirIndex;
        private readonly IFhirModel _fhirModel;
        private readonly IIndexService _indexService;
        private readonly ILocalhost _localhost;

        public SearchService(
            ILocalhost localhost,
            IFhirModel fhirModel,
            IFhirIndex fhirIndex,
            IIndexService indexService)
        {
            _fhirModel = fhirModel;
            _localhost = localhost;
            _indexService = indexService;
            _fhirIndex = fhirIndex;
        }

        public async Task<Snapshot> GetSnapshot(string type, SearchParams searchCommand)
        {
            Validate.TypeName(type);
            var results = await _fhirIndex.Search(type, searchCommand).ConfigureAwait(false);

            if (results.HasErrors)
            {
                throw new SparkException(HttpStatusCode.BadRequest, results.Outcome!);
            }

            var builder = new UriBuilder(_localhost.Uri(type)) { Query = results.UsedParameters };
            var link = builder.Uri;

            return CreateSnapshot(link, results, searchCommand);
        }

        public async Task<Snapshot> GetSnapshotForEverything(IKey key)
        {
            var searchCommand = new SearchParams();
            if (string.IsNullOrEmpty(key.ResourceId) == false)
            {
                searchCommand.Add("_id", key.ResourceId);
            }

            var compartment = _fhirModel.FindCompartmentInfo(key.TypeName);
            if (compartment != null)
            {
                foreach (var ri in compartment.ReverseIncludes)
                {
                    searchCommand.RevInclude.Add((ri, IncludeModifier.None));
                }
            }

            return await GetSnapshot(key.TypeName!, searchCommand).ConfigureAwait(false);
        }

        public async Task<IKey> FindSingle(string type, SearchParams searchCommand) =>
            Key.ParseOperationPath((await GetSearchResults(type, searchCommand).ConfigureAwait(false)).Single());

        public async Task<IKey?> FindSingleOrDefault(string type, SearchParams searchCommand)
        {
            var value = (await GetSearchResults(type, searchCommand).ConfigureAwait(false)).SingleOrDefault();
            return value != null ? Key.ParseOperationPath(value) : null;
        }

        public async Task<SearchResults> GetSearchResults(string type, SearchParams searchCommand)
        {
            Validate.TypeName(type);
            var results = await _fhirIndex.Search(type, searchCommand).ConfigureAwait(false);

            return results.HasErrors ? throw new SparkException(HttpStatusCode.BadRequest, results.Outcome!) : results;
        }
        
        private static Snapshot CreateSnapshot(Uri selflink, IList<string> keys, SearchParams searchCommand)
        {
            var sort = GetFirstSort(searchCommand);

            var count = searchCommand.Count;
            if (count.HasValue)
            {
                //TODO: should we change count?
                //count = Math.Min(searchCommand.Count.Value, MAX_PAGE_SIZE);
                selflink = selflink.AddParam(SearchParams.SEARCH_PARAM_COUNT, count.Value.ToString());
            }

            if (searchCommand.Sort.Any())
            {
                foreach (var (item1, sortOrder) in searchCommand.Sort)
                {
                    selflink = selflink.AddParam(
                        SearchParams.SEARCH_PARAM_SORT,
                        $"{item1}:{(sortOrder == SortOrder.Ascending ? "asc" : "desc")}");
                }
            }

            if (searchCommand.Include.Any())
            {
                selflink = selflink.AddParam(
                    SearchParams.SEARCH_PARAM_INCLUDE,
                    searchCommand.Include.Select(inc => inc.Item1).ToArray());
            }

            if (searchCommand.RevInclude.Any())
            {
                selflink = selflink.AddParam(
                    SearchParams.SEARCH_PARAM_REVINCLUDE,
                    searchCommand.RevInclude.Select(inc => inc.Item1).ToArray());
            }

            return Snapshot.Create(
                Bundle.BundleType.Searchset,
                selflink,
                keys,
                sort,
                count,
                searchCommand.Include.Select(inc => inc.Item1).ToList(),
                searchCommand.RevInclude.Select(inc => inc.Item1).ToList());
        }

        private static string? GetFirstSort(SearchParams searchCommand)
        {
            string? firstSort = null;
            if (searchCommand.Sort != null && searchCommand.Sort.Any())
            {
                firstSort = searchCommand.Sort[0].Item1; //TODO: Support sortorder and multiple sort arguments.
            }

            return firstSort;
        }
    }
}