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
        public Task<bool> Exists(IKey key) => _fhirStore.Exists(key);

        public async Task<Entry?> Get(IKey key)
        {
            var entry = await _fhirStore.Get(key).ConfigureAwait(false);
            if (entry != null)
            {
                _transfer.Externalize(entry);
            }

            return entry;
        }

        public async Task<Entry> Add(Entry entry)
        {
            if (entry.State != EntryState.Internal)
            {
                await _transfer.Internalize(entry).ConfigureAwait(false);
            }

            await _fhirStore.Add(entry).ConfigureAwait(false);
            var result = entry.IsDelete ? entry : await _fhirStore.Get(entry.Key).ConfigureAwait(false);

            if (result != null)
            {
                _transfer.Externalize(result);
            }

            return result ?? entry;
        }

        public IAsyncEnumerable<Entry> Get(IEnumerable<string> localIdentifiers, string? sortBy = null)
        {
            return _transfer.Externalize(_fhirStore.Get(localIdentifiers.Select(k => (IKey)Key.ParseOperationPath(k))));
        }
    }
}