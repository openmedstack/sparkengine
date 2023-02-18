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
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Core;
    using Store.Interfaces;

    public class ResourceStorageService : IResourceStorageService
    {
        private readonly IFhirStore _fhirStore;
        private readonly ITransfer _transfer;


        public ResourceStorageService(ITransfer transfer, IFhirStore fhirStore)
        {
            _transfer = transfer;
            _fhirStore = fhirStore;
        }

        /// <inheritdoc />
        public Task<bool> Exists(IKey key, CancellationToken cancellationToken) => _fhirStore.Exists(key);

        public async Task<Entry?> Get(IKey key, CancellationToken cancellationToken)
        {
            var entry = await _fhirStore.Get(key, cancellationToken).ConfigureAwait(false);
            if (entry != null)
            {
                _transfer.Externalize(entry);
            }

            return entry;
        }

        public async Task<Entry> Add(Entry entry, CancellationToken cancellationToken)
        {
            if (entry.State != EntryState.Internal)
            {
                await _transfer.Internalize(entry, cancellationToken).ConfigureAwait(false);
            }

            await _fhirStore.Add(entry, cancellationToken).ConfigureAwait(false);
            var result = entry.IsDelete
                ? entry
                : await _fhirStore.Get(entry.Key, cancellationToken).ConfigureAwait(false);

            if (result != null)
            {
                _transfer.Externalize(result);
            }

            return result ?? entry;
        }

        public IAsyncEnumerable<Entry> Get(
            IEnumerable<string> localIdentifiers,
            string? sortBy = null,
            CancellationToken cancellationToken = default)
        {
            return _transfer.Externalize(
                _fhirStore.Get(localIdentifiers.Select(k => (IKey)Key.ParseOperationPath(k)), cancellationToken));
        }
    }
}
