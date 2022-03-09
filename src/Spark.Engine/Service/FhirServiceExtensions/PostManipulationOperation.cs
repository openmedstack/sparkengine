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
        private class PostManipulationOperation : ResourceManipulationOperation
        {
            public PostManipulationOperation(
                Resource resource,
                IKey operationKey,
                SearchResults searchResults,
                SearchParams searchCommand = null)
                : base(resource, operationKey, searchResults, searchCommand)
            {
            }

            public static Uri ReadSearchUri(Bundle.EntryComponent entry) =>
                string.IsNullOrEmpty(entry.Request?.IfNoneExist) == false
                    ? new Uri($"{entry.Resource.TypeName}?{entry.Request.IfNoneExist}", UriKind.Relative)
                    : null;

            protected override IEnumerable<Entry> ComputeEntries()
            {
                Entry postEntry = null;
                if (SearchResults != null)
                {
                    if (SearchResults.Count > 1)
                    {
                        throw new SparkException(
                            HttpStatusCode.PreconditionFailed,
                            $"Multiple matches found when trying to resolve conditional create. Client's criteria were not selective enough.{GetSearchInformation()}");
                    }

                    var localKeyValue = SearchResults.SingleOrDefault();
                    //throw exception. probably we should manually throw this in order to add fhir specific details
                    if (string.IsNullOrEmpty(localKeyValue) == false)
                    {
                        var localKey = Key.ParseOperationPath(localKeyValue);
                        postEntry = Entry.Create(Bundle.HTTPVerb.GET, localKey, null);
                    }
                }

                postEntry ??= Entry.Post(OperationKey, Resource);

                yield return postEntry;
            }
        }
    }
}