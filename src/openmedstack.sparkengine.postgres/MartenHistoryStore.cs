// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace OpenMedStack.SparkEngine.Postgres
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Core;
    using Hl7.Fhir.Model;
    using Hl7.Fhir.Rest;
    using Marten;
    using SparkEngine.Extensions;
    using Store.Interfaces;

    public class MartenHistoryStore : IHistoryStore
    {
        private readonly Func<IDocumentSession> _sessionFunc;

        public MartenHistoryStore(Func<IDocumentSession> sessionFunc) => _sessionFunc = sessionFunc;

        /// <inheritdoc />
        public async Task<Snapshot> History(string typename, HistoryParameters parameters)
        {
            await using var session = _sessionFunc();
            var query = session.Query<EntryEnvelope>().Where(e => e.ResourceType == typename);
            if (parameters.Since.HasValue)
            {
                query = query.Where(x => x.When > parameters.Since.Value);
            }

            // TODO: Handle sort

            if (parameters.Count.HasValue)
            {
                query = query.Take(parameters.Count.Value);
            }

            var result = await query.Select(x => new { x.Key.TypeName, x.Key.Base, x.Key.ResourceId, x.Key.VersionId })
                .ToListAsync()
                .ConfigureAwait(false);
            var keys = result.Select(x => new Key(x.Base, x.TypeName, x.ResourceId, x.VersionId).ToString()).ToList();
            return CreateSnapshot(keys, result.Count);
        }

        /// <inheritdoc />
        public async Task<Snapshot> History(IKey key, HistoryParameters parameters)
        {
            var storageKey = key.ToStorageKey();
            await using var session = _sessionFunc();
            var query = session.Query<EntryEnvelope>().Where(e => e.ResourceKey == storageKey);
            if (parameters.Since.HasValue)
            {
                query = query.Where(x => x.When > parameters.Since.Value);
            }

            // TODO: Handle sort

            if (parameters.Count.HasValue)
            {
                query = query.Take(parameters.Count.Value);
            }

            var result = await query.Select(x => new { x.Key.TypeName, x.Key.Base, x.Key.ResourceId, x.Key.VersionId })
                .ToListAsync()
                .ConfigureAwait(false);
            return CreateSnapshot(
                result.Select(x => new Key(x.Base, x.TypeName, x.ResourceId, x.VersionId).ToString()).ToList(),
                result.Count);
        }

        /// <inheritdoc />
        public async Task<Snapshot> History(HistoryParameters parameters)
        {
            await using var session = _sessionFunc();
            IQueryable<EntryEnvelope> query = session.Query<EntryEnvelope>();
            if (parameters.Since.HasValue)
            {
                query = query.Where(x => x.When > parameters.Since.Value);
            }

            // TODO: Handle sort

            if (parameters.Count.HasValue)
            {
                query = query.Take(parameters.Count.Value);
            }

            var result = await query.Select(x => new { x.Key.TypeName, x.Key.Base, x.Key.ResourceId, x.Key.VersionId })
                .ToListAsync()
                .ConfigureAwait(false);
            return CreateSnapshot(
                result.Select(x => new Key(x.Base, x.TypeName, x.ResourceId, x.VersionId).ToString()).ToList(),
                result.Count);
        }

        private static Snapshot CreateSnapshot(
            IList<string> keys,
            int? count = null,
            IList<string>? includes = null,
            IList<string>? reverseIncludes = null) =>
            Snapshot.Create(
                Bundle.BundleType.History,
                new Uri(TransactionBuilder.HISTORY, UriKind.Relative),
                keys,
                "history",
                count,
                includes ?? Array.Empty<string>(),
                reverseIncludes ?? Array.Empty<string>());
    }
}