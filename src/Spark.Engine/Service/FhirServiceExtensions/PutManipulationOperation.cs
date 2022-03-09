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
    using System.Linq;
    using System.Net;
    using Core;
    using Hl7.Fhir.Model;
    using Hl7.Fhir.Rest;

    public static partial class ResourceManipulationOperationFactory
    {
        private class PutManipulationOperation : ResourceManipulationOperation
        {
            public PutManipulationOperation(
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
                Entry entry = null;

                if (SearchResults != null)
                {
                    if (SearchResults.Count > 1)
                    {
                        throw new SparkException(
                            HttpStatusCode.PreconditionFailed,
                            "Multiple matches found when trying to resolve conditional update. Client's criteria were not selective enough");
                    }

                    var localKeyValue = SearchResults.SingleOrDefault();
                    if (localKeyValue != null)
                    {
                        IKey localKey = Key.ParseOperationPath(localKeyValue);

                        entry = Entry.Put(localKey, Resource);
                    }
                    else
                    {
                        entry = Entry.Post(OperationKey, Resource);
                    }
                }

                entry ??= Entry.Put(OperationKey, Resource);
                yield return entry;
            }
        }
    }
}