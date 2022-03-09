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
    using System.Collections.Generic;
    using Core;
    using Hl7.Fhir.Model;
    using Hl7.Fhir.Rest;

    public static partial class ResourceManipulationOperationFactory
    {
        private class DeleteManipulationOperation : ResourceManipulationOperation
        {
            public DeleteManipulationOperation(
                Resource resource,
                IKey operationKey,
                SearchResults searchResults,
                SearchParams searchCommand = null)
                : base(resource, operationKey, searchResults, searchCommand)
            {
            }

            public static Uri ReadSearchUri(Bundle.EntryComponent entry) =>
                entry.Request != null ? new Uri(entry.Request.Url, UriKind.RelativeOrAbsolute) : null;

            protected override IEnumerable<Entry> ComputeEntries()
            {
                if (SearchResults != null)
                {
                    foreach (var localKeyValue in SearchResults)
                    {
                        yield return Entry.Delete(Key.ParseOperationPath(localKeyValue), DateTimeOffset.UtcNow);
                    }
                }
                else
                {
                    yield return Entry.Delete(OperationKey, DateTimeOffset.UtcNow);
                }
            }
        }
    }
}