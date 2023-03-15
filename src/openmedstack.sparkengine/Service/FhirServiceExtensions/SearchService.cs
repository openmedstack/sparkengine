/* 
 * Copyright (c) 2016, Furore (info@furore.com) and contributors
 * Copyright (c) 2021, Incendi (info@incendi.no) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/spark/stu3/master/LICENSE
 */
namespace OpenMedStack.SparkEngine.Service.FhirServiceExtensions;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Core;
using Extensions;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Interfaces;
using Task = System.Threading.Tasks.Task;

public class SearchService : ISearchService, IServiceListener
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

    public async Task<Snapshot> GetSnapshot(string type, SearchParams searchCommand, CancellationToken cancellationToken)
    {
        Validate.TypeName(type);
        var results = await _fhirIndex.Search(type, searchCommand, cancellationToken).ConfigureAwait(false);

        if (results.HasErrors)
        {
            throw new SparkException(HttpStatusCode.BadRequest, results.Outcome!);
        }

        var builder = new UriBuilder(_localhost.Uri(type)) { Query = results.UsedParameters };
        var link = builder.Uri;

        return CreateSnapshot(link, results, searchCommand);
    }

    public async Task<Snapshot> GetSnapshotForEverything(IKey key, CancellationToken cancellationToken)
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

        return await GetSnapshot(key.TypeName!, searchCommand, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IKey> FindSingle(string type, SearchParams searchCommand, CancellationToken cancellationToken) =>
        Key.ParseOperationPath((await GetSearchResults(type, searchCommand, cancellationToken).ConfigureAwait(false)).Single());

    public async Task<IKey?> FindSingleOrDefault(
        string type,
        SearchParams searchCommand,
        CancellationToken cancellationToken)
    {
        searchCommand.Count = 1;
        var value = (await GetSearchResults(type, searchCommand, cancellationToken).ConfigureAwait(false)).SingleOrDefault();
        return value != null ? Key.ParseOperationPath(value) : null;
    }

    public async Task<SearchResults> GetSearchResults(
        string type,
        SearchParams searchCommand,
        CancellationToken cancellationToken)
    {
        Validate.TypeName(type);
        var results = await _fhirIndex.Search(type, searchCommand, cancellationToken).ConfigureAwait(false);

        return results.HasErrors ? throw new SparkException(HttpStatusCode.BadRequest, results.Outcome!) : results;
    }

    public Task Inform(Uri location, Entry interaction) => _indexService.Process(interaction);

    private static Snapshot CreateSnapshot(Uri selfLink, IList<string> keys, SearchParams searchCommand)
    {
        var sort = GetFirstSort(searchCommand);

        var count = searchCommand.Count;
        if (count.HasValue)
        {
            //TODO: should we change count?
            //count = Math.Min(searchCommand.Count.Value, MAX_PAGE_SIZE);
            selfLink = selfLink.AddParam(SearchParams.SEARCH_PARAM_COUNT, count.Value.ToString());
        }

        if (searchCommand.Sort.Any())
        {
            foreach (var (item1, sortOrder) in searchCommand.Sort)
            {
                selfLink = selfLink.AddParam(
                    SearchParams.SEARCH_PARAM_SORT,
                    $"{item1}:{(sortOrder == SortOrder.Ascending ? "asc" : "desc")}");
            }
        }

        if (searchCommand.Include.Any())
        {
            selfLink = selfLink.AddParam(
                SearchParams.SEARCH_PARAM_INCLUDE,
                searchCommand.Include.Select(inc => inc.Item1).ToArray());
        }

        if (searchCommand.RevInclude.Any())
        {
            selfLink = selfLink.AddParam(
                SearchParams.SEARCH_PARAM_REVINCLUDE,
                searchCommand.RevInclude.Select(inc => inc.Item1).ToArray());
        }

        return Snapshot.Create(
            Bundle.BundleType.Searchset,
            selfLink,
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