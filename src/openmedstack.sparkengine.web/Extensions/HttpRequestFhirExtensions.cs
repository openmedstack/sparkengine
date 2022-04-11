/*
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Spark.Engine.Test")]

namespace OpenMedStack.SparkEngine.Web.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core;
    using Hl7.Fhir.Model;
    using Hl7.Fhir.Rest;
    using Hl7.Fhir.Utility;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Http.Features;
    using Microsoft.AspNetCore.Http.Headers;
    using Microsoft.AspNetCore.Mvc.Formatters;
    using Microsoft.Extensions.Primitives;
    using SparkEngine.Extensions;
    using Utility;

    public static class HttpRequestFhirExtensions
    {
        private static string WithoutQuotes(string s)
        {
            return string.IsNullOrEmpty(s) ? "" : s.Trim('"');
        }

        public static int GetPagingOffsetParameter(this HttpRequest request)
        {
            var offset = request.GetParameter(FhirParameter.OFFSET)?.ParseIntParameter();
            if (!offset.HasValue)
            {
                // This part is kept as backwards compatibility for the "start" parameter which was used as an offset
                // in earlier versions of Spark.
                offset = request.GetParameter(FhirParameter.SNAPSHOT_INDEX)?.ParseIntParameter();
            }

            return offset ?? 0;
        }

        public static string? IfMatchVersionId(this HttpRequest request)
        {
            if (request.Headers.Count == 0)
            {
                return null;
            }

            if (!request.Headers.TryGetValue("If-Match", out var value))
            {
                return null;
            }

            var tag = value.FirstOrDefault();
            if (tag == null)
            {
                return null;
            }

            return WithoutQuotes(tag);
        }

        /// <summary>
        /// Returns true if the Accept header matches any of the FHIR supported Xml or Json MIME types, otherwise false.
        /// </summary>
        private static bool IsAcceptHeaderFhirMediaType(this HttpRequest request)
        {
            var acceptHeader = request.GetTypedHeaders().Accept.FirstOrDefault();
            if (acceptHeader == null || acceptHeader.MediaType == StringSegment.Empty)
            {
                return false;
            }

            var accept = acceptHeader.MediaType.Value;
            return ContentType.XML_CONTENT_HEADERS.Contains(accept)
                   || ContentType.JSON_CONTENT_HEADERS.Contains(accept);
        }

        internal static bool IsRawBinaryRequest(this OutputFormatterCanWriteContext context, Type type)
        {
            if (type == typeof(Binary)
                || (type == typeof(FhirResponse)) && ((FhirResponse)context.Object!).Resource is Binary)
            {
                var request = context.HttpContext.Request;
                var isFhirMediaType = false;
                if (request.Method == "GET")
                {
                    isFhirMediaType = request.IsAcceptHeaderFhirMediaType();
                }
                else if (request.Method is "POST" or "PUT")
                {
                    isFhirMediaType = request.ContentType.IsContentTypeHeaderFhirMediaType();
                }

                var ub = new UriBuilder(request.GetRequestUri());
                // TODO: KM: Path matching is not optimal should be replaced by a more solid solution.
                return ub.Path.Contains("Binary") && !isFhirMediaType;
            }
            else
            {
                return false;
            }
        }

        internal static bool IsRawBinaryRequest(this HttpRequest request)
        {
            var ub = new UriBuilder(request.GetRequestUri());
            return ub.Path.Contains("Binary") && !ub.Path.EndsWith("_search");
        }

        internal static void AcquireHeaders(this HttpResponse response, FhirResponse fhirResponse)
        {
            if (fhirResponse.Key != null)
            {
                response.Headers.Add(HttpHeaderName.ETAG, ETag.Create(fhirResponse.Key?.VersionId)?.ToString());

                var location = fhirResponse.Key!.ToUri();
                response.Headers.Add(HttpHeaderName.LOCATION, location.OriginalString);

                response.Headers.Add(HttpHeaderName.CONTENT_LOCATION, location.OriginalString);
                if (fhirResponse.Resource is { Meta.LastUpdated: { } })
                {
                    response.Headers.Add(
                        HttpHeaderName.LAST_MODIFIED,
                        fhirResponse.Resource.Meta.LastUpdated.Value.ToString("R"));
                }
            }
        }

        public static IEnumerable<Tuple<string, string>> TupledParameters(this HttpRequest request)
        {
            return request.Query.Select(x => Tuple.Create(x.Key, x.Value.ToString()));
        }

        public static HistoryParameters ToHistoryParameters(this HttpRequest request) =>
            new(
                request.GetParameter("_count")?.ParseIntParameter(),
                request.GetParameter("_since")?.ParseDateParameter(),
                request.GetParameter("_sort"));

        public static ConditionalHeaderParameters? ToConditionalHeaderParameters(this HttpRequest request) =>
            new(request.IfNoneMatch(), request.IfModifiedSince());

        internal static string GetRequestUri(this HttpRequest request)
        {
            var httpRequestFeature = request.HttpContext.Features.Get<IHttpRequestFeature>();
            return $"{request.Scheme}://{request.Host}{httpRequestFeature?.RawTarget ?? "/"}";
        }

        internal static DateTimeOffset? IfModifiedSince(this HttpRequest request)
        {
            request.Headers.TryGetValue("If-Modified-Since", out var values);
            if (!DateTimeOffset.TryParse(values.FirstOrDefault(), out var modified))
            {
                return null;
            }

            return modified;
        }

        internal static IEnumerable<string> IfNoneMatch(this HttpRequest request)
        {
            if (!request.Headers.TryGetValue("If-None-Match", out var values))
            {
                return Array.Empty<string>();
            }

            return values.ToArray();
        }

        internal static SummaryType RequestSummary(this HttpRequest request)
        {
            request.Query.TryGetValue("_summary", out var stringValues);
            return GetSummary(stringValues.FirstOrDefault());
        }

        /// <summary>
        /// Transfers the id to the <see cref="Resource"/>.
        /// </summary>
        /// <param name="request">An instance of <see cref="HttpRequest"/>.</param>
        /// <param name="resource">An instance of <see cref="Resource"/>.</param>
        /// <param name="id">A <see cref="string"/> containing the id to transfer to Resource.Id.</param>
        public static void TransferResourceIdIfRawBinary(this HttpRequest request, Resource resource, string? id)
        {
            if (request.Headers.TryGetValue("Content-Type", out var value))
            {
                var contentType = value.FirstOrDefault();
                TransferResourceIdIfRawBinary(contentType, resource, id);
            }
        }

        public static string? IfNoneExist(this RequestHeaders headers)
        {
            string? ifNoneExist = null;
            if (headers.Headers.TryGetValue(FhirHttpHeaders.IfNoneExist, out var values))
            {
                ifNoneExist = values.FirstOrDefault();
            }

            return ifNoneExist;
        }

        internal static bool IsRawBinaryPostOrPutRequest(this HttpRequest request)
        {
            var ub = new UriBuilder(request.GetRequestUri());
            // TODO: KM: Path matching is not optimal should be replaced by a more solid solution.
            return ub.Path.Contains("Binary")
                   && !ub.Path.EndsWith("_search")
                   && !request.ContentType.IsContentTypeHeaderFhirMediaType()
                   && (request.Method == "POST" || request.Method == "PUT");
        }

        private static SummaryType GetSummary(string? summary)
        {
            var summaryType = string.IsNullOrWhiteSpace(summary)
                ? SummaryType.False
                : EnumUtility.ParseLiteral<SummaryType>(summary, true);

            return summaryType ?? SummaryType.False;
        }

        private static void TransferResourceIdIfRawBinary(string? contentType, Resource resource, string? id)
        {
            if (!string.IsNullOrEmpty(contentType) && resource is Binary && resource.Id == null && id != null)
            {
                if (!ContentType.XML_CONTENT_HEADERS.Contains(contentType)
                    && !ContentType.JSON_CONTENT_HEADERS.Contains(contentType))
                {
                    resource.Id = id;
                }
            }
        }
    }
}