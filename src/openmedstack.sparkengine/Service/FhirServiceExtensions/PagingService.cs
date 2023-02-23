// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace OpenMedStack.SparkEngine.Service.FhirServiceExtensions;

using System;
using System.Threading.Tasks;
using Core;
using Store.Interfaces;

public class PagingService : IPagingService
{
    private readonly ISnapshotPaginationProvider _paginationProvider;
    private readonly ISnapshotStore _snapshotstore;

    public PagingService(ISnapshotStore snapshotstore, ISnapshotPaginationProvider paginationProvider)
    {
        _snapshotstore = snapshotstore;
        _paginationProvider = paginationProvider;
    }

    public async Task<ISnapshotPagination> StartPagination(Snapshot snapshot)
    {
        await _snapshotstore.AddSnapshot(snapshot).ConfigureAwait(false);

        return _paginationProvider.StartPagination(snapshot);
    }

    public async Task<ISnapshotPagination> StartPagination(string snapshotkey)
    {
        var snapshot = await _snapshotstore.GetSnapshot(snapshotkey).ConfigureAwait(false);
        return _paginationProvider.StartPagination(snapshot!);
    }
}