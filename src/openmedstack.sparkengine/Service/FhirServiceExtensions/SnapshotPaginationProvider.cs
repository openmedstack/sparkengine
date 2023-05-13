// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace OpenMedStack.SparkEngine.Service.FhirServiceExtensions;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Core;
using Extensions;
using Hl7.Fhir.Model;
using Interfaces;

public class SnapshotPaginationProvider : ISnapshotPaginationProvider, ISnapshotPagination
{
    private readonly IFhirStore _fhirStore;
    private readonly ILocalhost _localhost;
    private readonly ISnapshotPaginationCalculator _snapshotPaginationCalculator;
    private readonly ITransfer _transfer;
    private Snapshot? _snapshot;

    public SnapshotPaginationProvider(
        IFhirStore fhirStore,
        ITransfer transfer,
        ILocalhost localhost,
        ISnapshotPaginationCalculator snapshotPaginationCalculator)
    {
        _fhirStore = fhirStore;
        _transfer = transfer;
        _localhost = localhost;
        _snapshotPaginationCalculator = snapshotPaginationCalculator;
    }

    public async Task<Bundle> GetPage(
        CancellationToken cancellationToken,
        int? index = null,
        Action<Entry>? transformElement = null)
    {
        if (_snapshot == null)
        {
            throw Error.NotFound("There is no paged snapshot");
        }

        return !_snapshot.InRange(index ?? 0)
            ? throw Error.NotFound(
                "The specified index lies outside the range of available results ({0}) in snapshot {1}",
                _snapshot.Keys.Count(),
                _snapshot.Id)
            : await CreateBundle(cancellationToken, index).ConfigureAwait(false);
    }

    public ISnapshotPagination StartPagination(Snapshot snapshot)
    {
        _snapshot = snapshot;
        return this;
    }

    private async Task<Bundle> CreateBundle(CancellationToken cancellationToken, int? start = null)
    {
        var bundle = new Bundle { Type = _snapshot!.Type, Total = _snapshot.Count, Id = Guid.NewGuid().ToString() };

        var keys = _snapshotPaginationCalculator.GetKeysForPage(_snapshot, start).ToList();
        var infos = await _fhirStore.Get(keys, cancellationToken).ToListAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        if (_snapshot.SortBy != null)
        {
            infos = infos.Select(e => new { Entry = e, Index = keys.IndexOf(e.GetKey()) })
                .OrderBy(e => e.Index)
                .Select(e => e.Entry)
                .ToList();
        }

        var resources = await System.Threading.Tasks.Task.WhenAll(infos.Select(x => _fhirStore.Load(x.GetKey(), cancellationToken)))
            .ConfigureAwait(false);
        var entries = infos.Select(
                x =>
                {
                    var infoKey = x.GetKey();
                    var resource = resources.Where(resource => resource != null)
                        .FirstOrDefault(r => r!.ExtractKey().WithoutBase().Equals(infoKey));
                    return Entry.Create(
                        x.Method,
                        infoKey,
                        resource);
                })
            .ToArray();
        var included = await GetIncludesRecursiveFor(entries, _snapshot.Includes, cancellationToken)
            .ToListAsync(cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        foreach (var entry in _transfer.Externalize(entries).Concat(_transfer.Externalize(included)))
        {
            bundle.Append(entry);
        }

        BuildLinks(bundle, start);

        return bundle;
    }


    private async IAsyncEnumerable<Entry> GetIncludesRecursiveFor(
        IList<Entry> entries,
        ICollection<string> includes,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        IList<Entry> included = new List<Entry>();

        var latest = await GetIncludesFor(entries, includes, cancellationToken)
            .ToListAsync(cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        int previousCount;
        do
        {
            previousCount = included.Count;
            included.AppendDistinct(latest);
            latest = await GetIncludesFor(latest, includes, cancellationToken)
                .ToListAsync(cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }
        while (included.Count > previousCount);

        foreach (var entry in entries)
        {
            yield return entry;
        }
    }

    private IAsyncEnumerable<Entry> GetIncludesFor(IEnumerable<Entry> entries, IEnumerable<string>? includes, CancellationToken cancellationToken)
    {
        if (includes == null)
        {
            return AsyncEnumerable.Empty<Entry>();
        }

        var paths = includes.SelectMany(IncludeToPath);
        var identifiers = entries.GetResources()
            .GetReferences(paths)
            .Distinct()
            .Select(k => (IKey)Key.ParseOperationPath(k));

        return _fhirStore.Get(identifiers, cancellationToken)
            .SelectAwait(
                async x =>
                {
                    var resource = await _fhirStore.Load(x.GetKey(), cancellationToken).ConfigureAwait(false);
                    return Entry.Create(x.Method, x.GetKey(), resource);
                });
    }

    private void BuildLinks(Bundle bundle, int? start = null)
    {
        bundle.SelfLink = start == null
            ? _localhost.Absolute(new Uri(_snapshot!.FeedSelfLink, UriKind.RelativeOrAbsolute))
            : BuildSnapshotPageLink(0);
        bundle.FirstLink = BuildSnapshotPageLink(0);
        bundle.LastLink = BuildSnapshotPageLink(_snapshotPaginationCalculator.GetIndexForLastPage(_snapshot!));

        var previousPageIndex = _snapshotPaginationCalculator.GetIndexForPreviousPage(_snapshot!, start);
        if (previousPageIndex != null)
        {
            bundle.PreviousLink = BuildSnapshotPageLink(previousPageIndex);
        }

        var nextPageIndex = _snapshotPaginationCalculator.GetIndexForNextPage(_snapshot!, start);
        if (nextPageIndex != null)
        {
            bundle.NextLink = BuildSnapshotPageLink(nextPageIndex);
        }
    }

    private Uri? BuildSnapshotPageLink(int? snapshotIndex)
    {
        if (!snapshotIndex.HasValue)
        {
            return null;
        }

        Uri baseurl;
        if (string.IsNullOrEmpty(_snapshot?.Id) == false)
        {
            //baseUrl for statefull pagination
            baseurl = new Uri(_localhost.DefaultBase + "/" + FhirRestOp.SNAPSHOT).AddParam(
                FhirParameter.SNAPSHOT_ID,
                _snapshot.Id);
        }
        else
        {
            //baseUrl for stateless pagination
            baseurl = new Uri(_snapshot!.FeedSelfLink);
        }

        return baseurl.AddParam(FhirParameter.SNAPSHOT_INDEX, snapshotIndex.Value.ToString());
    }

    private IEnumerable<string> IncludeToPath(string include)
    {
        var split = include.Split(':');
        var resource = split.FirstOrDefault();
        var paramname = split.Skip(1).FirstOrDefault();
        var param = ModelInfo.SearchParameters.FirstOrDefault(p => p.Resource == resource && p.Name == paramname);
        return param is { Path: { } } ? param.Path : Enumerable.Empty<string>();
    }
}
