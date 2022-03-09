// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace Spark.Postgres
{
    using System;
    using System.Threading.Tasks;
    using Engine.Core;
    using Engine.Interfaces;
    using Marten;

    public class MartenFhirStoreAdministration : IFhirStoreAdministration
    {
        private readonly Func<IDocumentSession> _sessionFunc;

        public MartenFhirStoreAdministration(Func<IDocumentSession> sessionFunc) => _sessionFunc = sessionFunc;

        public Task Clean()
        {
            using var session = _sessionFunc();
            var documentCleaner = session.DocumentStore.Advanced.Clean;
            var tasks = new[]
            {
                documentCleaner.DeleteDocumentsByTypeAsync(typeof(IndexEntry)),
                documentCleaner.DeleteDocumentsByTypeAsync(typeof(EntryEnvelope)),
                documentCleaner.DeleteDocumentsByTypeAsync(typeof(Snapshot))
            };
            return Task.WhenAll(tasks);
        }
    }
}