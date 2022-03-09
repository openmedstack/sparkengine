// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace Spark.Engine.Service.FhirServiceExtensions
{
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
            if (_snapshotstore != null)
            {
                await _snapshotstore.AddSnapshot(snapshot).ConfigureAwait(false);
            }
            else
            {
                snapshot.Id = null;
            }

            return _paginationProvider.StartPagination(snapshot);
        }

        public async Task<ISnapshotPagination> StartPagination(string snapshotkey) =>
            _snapshotstore == null
                ? throw new NotSupportedException("Stateful pagination is not currently supported.")
                : _paginationProvider.StartPagination(
                    await _snapshotstore.GetSnapshot(snapshotkey).ConfigureAwait(false));
    }
}