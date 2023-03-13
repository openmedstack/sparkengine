// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace OpenMedStack.SparkEngine.Service.FhirServiceExtensions;

using System.Threading;
using System.Threading.Tasks;
using Core;
using Interfaces;

public class PagingService : IPagingService
{
    private readonly ISnapshotPaginationProvider _paginationProvider;
    private readonly ISnapshotStore _snapshotStore;

    public PagingService(ISnapshotStore snapshotStore, ISnapshotPaginationProvider paginationProvider)
    {
        _snapshotStore = snapshotStore;
        _paginationProvider = paginationProvider;
    }

    public async Task<ISnapshotPagination> StartPagination(Snapshot snapshot, CancellationToken cancellationToken)
    {
        await _snapshotStore.AddSnapshot(snapshot, cancellationToken).ConfigureAwait(false);

        return _paginationProvider.StartPagination(snapshot);
    }

    public async Task<ISnapshotPagination> StartPagination(string snapshotKey, CancellationToken cancellationToken)
    {
        var snapshot = await _snapshotStore.GetSnapshot(snapshotKey, cancellationToken).ConfigureAwait(false);
        return _paginationProvider.StartPagination(snapshot!);
    }
}