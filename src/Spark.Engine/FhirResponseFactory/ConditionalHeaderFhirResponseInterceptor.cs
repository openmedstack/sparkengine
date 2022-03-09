// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace Spark.Engine.FhirResponseFactory
{
    using System.Linq;
    using System.Net;
    using Core;
    using Extensions;
    using Interfaces;

    public class ConditionalHeaderFhirResponseInterceptor : IFhirResponseInterceptor
    {
        public bool CanHandle(object input) => input is ConditionalHeaderParameters;

        public FhirResponse GetFhirResponse(Entry entry, object input)
        {
            var parameters = ConvertInput(input);
            if (parameters == null)
            {
                return null;
            }

            var matchTags = parameters.IfNoneMatchTags.Any()
                ? parameters.IfNoneMatchTags.Any(t => t == ETag.Create(entry.Key.VersionId).Tag)
                : (bool?) null;
            var matchModifiedDate = parameters.IfModifiedSince.HasValue
                ? parameters.IfModifiedSince.Value < entry.Resource.Meta.LastUpdated
                : (bool?) null;

            if (!matchTags.HasValue && !matchModifiedDate.HasValue)
            {
                return null;
            }

            return (matchTags ?? true) && (matchModifiedDate ?? true)
                ? Respond.WithCode(HttpStatusCode.NotModified)
                : null;
        }

        private ConditionalHeaderParameters ConvertInput(object input) => input as ConditionalHeaderParameters;
    }
}