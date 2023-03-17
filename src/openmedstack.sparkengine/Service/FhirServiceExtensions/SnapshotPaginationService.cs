/*
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
using System.Threading;
using System.Threading.Tasks;
using Core;
using Extensions;
using Hl7.Fhir.Model;
using Interfaces;

internal class SnapshotPaginationService : ISnapshotPagination
{
    private readonly IFhirStore _fhirStore;
    private readonly ITransfer _transfer;
    private readonly ILocalhost _localhost;
    private readonly ISnapshotPaginationCalculator _snapshotPaginationCalculator;
    private readonly Snapshot _snapshot;

    public SnapshotPaginationService(
        IFhirStore fhirStore,
        ITransfer transfer,
        ILocalhost localhost,
        ISnapshotPaginationCalculator snapshotPaginationCalculator,
        Snapshot snapshot)
    {
        _fhirStore = fhirStore;
        _transfer = transfer;
        _localhost = localhost;
        _snapshotPaginationCalculator = snapshotPaginationCalculator;
        _snapshot = snapshot;
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

        if (!_snapshot.InRange(index ?? 0))
        {
            throw Error.NotFound(
                "The specified index lies outside the range of available results ({0}) in snapshot {1}",
                _snapshot.Keys.Count(),
                _snapshot.Id);
        }

        return await CreateBundle(cancellationToken, index).ConfigureAwait(false);
    }

    private async Task<Bundle> CreateBundle(CancellationToken cancellationToken, int? start = null)
    {
        var bundle = new Bundle { Type = _snapshot.Type, Total = _snapshot.Count, Id = Guid.NewGuid().ToString("N") };

        var keys = _snapshotPaginationCalculator.GetKeysForPage(_snapshot, start).ToList();
        var infos = await _fhirStore.Get(keys, cancellationToken)
            .ToListAsync(cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        if (_snapshot.SortBy != null)
        {
            infos = infos.Select(e => new { Entry = e, Index = keys.IndexOf(e.GetKey()) })
                .OrderBy(e => e.Index)
                .Select(e => e.Entry)
                .ToList();
        }

        var resources =
            await System.Threading.Tasks.Task.WhenAll(infos.Select(i => _fhirStore.Load(i.GetKey(), cancellationToken)));
        var entries = infos.Select(
                i => Entry.Create(i.Method, i.GetKey(), resources.FirstOrDefault(r => r!.ExtractKey().Equals(i.GetKey()))))
            .ToList();

        foreach (var entry in entries)
        {
            bundle.Append(_transfer.Externalize(entry));
        }
        var included = GetIncludesRecursiveFor(entries, _snapshot.Includes, cancellationToken);
        foreach (var entry in await included.ConfigureAwait(false))
        {
            bundle.Append(_transfer.Externalize(entry));
        }
        
        BuildLinks(bundle, start);

        return bundle;
    }

    private async Task<IEnumerable<Entry>> GetIncludesRecursiveFor(
        IEnumerable<Entry> entries,
        ICollection<string> includes,
        CancellationToken cancellationToken)
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

        return included;
    }

    private IAsyncEnumerable<Entry> GetIncludesFor(IEnumerable<Entry> entries, IEnumerable<string> includes, CancellationToken cancellationToken)
    {
        var paths = includes.SelectMany(IncludeToPath);
        IList<IKey> identifiers = entries.GetResources()
            .GetReferences(paths)
            .Distinct()
            .Select(k => (IKey)Key.ParseOperationPath(k))
            .ToList();

        return _fhirStore.Get(identifiers, cancellationToken)
            .SelectAwait(
                async x =>
                {
                    var resource = await _fhirStore.Load(x.GetKey(), cancellationToken);
                    return Entry.Create(x.Method, x.GetKey(), resource);
                });
    }

    private void BuildLinks(Bundle bundle, int? offset = null)
    {
        bundle.SelfLink = offset == null
            ? _localhost.Absolute(new Uri(_snapshot.FeedSelfLink, UriKind.RelativeOrAbsolute))
            : BuildSnapshotPageLink(offset);
        bundle.FirstLink = BuildSnapshotPageLink(0);
        bundle.LastLink = BuildSnapshotPageLink(_snapshotPaginationCalculator.GetIndexForLastPage(_snapshot));

        var previousPageIndex = _snapshotPaginationCalculator.GetIndexForPreviousPage(_snapshot, offset);
        if (previousPageIndex != null)
        {
            bundle.PreviousLink = BuildSnapshotPageLink(previousPageIndex);
        }

        var nextPageIndex = _snapshotPaginationCalculator.GetIndexForNextPage(_snapshot, offset);
        if (nextPageIndex != null)
        {
            bundle.NextLink = BuildSnapshotPageLink(nextPageIndex);
        }
    }

    private Uri? BuildSnapshotPageLink(int? offset)
    {
        if (offset == null)
        {
            return null;
        }

        Uri baseurl;
        if (string.IsNullOrEmpty(_snapshot.Id) == false)
        {
            //baseUrl for stateful pagination
            baseurl = new Uri(_localhost.DefaultBase + "/" + FhirRestOp.SNAPSHOT).AddParam(
                FhirParameter.SNAPSHOT_ID,
                _snapshot.Id);
        }
        else
        {
            //baseUrl for stateless pagination
            baseurl = new Uri(_snapshot.FeedSelfLink);
        }

        return baseurl.AddParam(FhirParameter.OFFSET, offset.ToString()!);
    }

    private static IEnumerable<string> IncludeToPath(string include)
    {
        var split = include.Split(':');
        var resource = split.FirstOrDefault();
        var paramName = split.Skip(1).FirstOrDefault();
        var param = ModelInfo.SearchParameters.FirstOrDefault(p => p.Resource == resource && p.Name == paramName);
        return param is { Path: { } } ? param.Path : Enumerable.Empty<string>();
    }
}