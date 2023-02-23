// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace OpenMedStack.SparkEngine.Service
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Core;
    using Interfaces;

    /// <summary>
    ///     Transfer maps between local id's and references and absolute id's and references upon incoming or outgoing
    ///     Interactions.
    ///     It uses an Import or Export to do de actual work for incoming or outgoing Interactions respectively.
    /// </summary>
    public class Transfer : ITransfer
    {
        private readonly Export _export;
        private readonly Import _import;

        public Transfer(IGenerator generator, ILocalhost localhost, SparkSettings? sparkSettings = null)
        {
            _export = new(localhost, sparkSettings?.ExportSettings ?? new ExportSettings());
            _import = new(localhost, generator);
        }

        public Task Internalize(Entry entry, CancellationToken cancellationToken)
        {
            return _import.Internalize(entry, null, cancellationToken);
        }


        public async IAsyncEnumerable<Entry> Internalize(IEnumerable<Entry> interactions, Mapper<string, IKey>? mapper, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            foreach (var interaction in interactions)
            {
                yield return await _import.Internalize(interaction, mapper, cancellationToken).ConfigureAwait(false);
            }
        }

        public Entry Externalize(Entry interaction)
        {
            return _export.Externalize(interaction);
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<Entry> Externalize(IAsyncEnumerable<Entry> interactions)
        {
            await foreach (var item in interactions.ConfigureAwait(false))
            {
                yield return _export.Externalize(item);
            }
        }

        public IEnumerable<Entry> Externalize(IEnumerable<Entry> interactions)
        {
            return interactions.Select(_export.Externalize);
        }
    }
}