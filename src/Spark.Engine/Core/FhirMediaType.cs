/*
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace Spark.Engine.Core
{
    using System;
    using System.Net.Http.Headers;
    using System.Text;
    using Hl7.Fhir.Model;
    using Hl7.Fhir.Rest;

    public static class FhirMediaType
    {
        public static readonly string DefaultJsonMimeType = ContentType.JSON_CONTENT_HEADER;
        public static readonly string DefaultXmlMimeType = ContentType.XML_CONTENT_HEADER;
        public static readonly string OctetStreamMimeType = "application/octet-stream";
        public static readonly string FormUrlEncodedMimeType = "application/x-www-form-urlencoded";
        public static readonly string AnyMimeType = "*/*";

        public static IEnumerable<string> JsonMimeTypes => ContentType.JSON_CONTENT_HEADERS;
        public static IEnumerable<string> XmlMimeTypes => ContentType.XML_CONTENT_HEADERS;
        public static IEnumerable<string> SupportedMimeTypes => JsonMimeTypes
            .Concat(XmlMimeTypes)
            .Concat(new[] { OctetStreamMimeType, FormUrlEncodedMimeType, AnyMimeType });

        /// <summary>
        /// Transforms loose formats to their strict variant
        /// </summary>
        /// <param name="format">Mime type</param>
        /// <returns></returns>
        public static string Interpret(string format)
        {
            if (format == null) return DefaultJsonMimeType;
            if (XmlMimeTypes.Contains(format)) return DefaultXmlMimeType;
            if (JsonMimeTypes.Contains(format)) return DefaultJsonMimeType;
            return format;
        }

        public static ResourceFormat GetResourceFormat(string format)
        {
            var strict = Interpret(format);
            if (strict == DefaultXmlMimeType) return ResourceFormat.Xml;
            else if (strict == DefaultJsonMimeType) return ResourceFormat.Json;
            else return ResourceFormat.Xml;
        }

        public static string GetContentType(Type type, ResourceFormat format)
        {
            if (typeof(Resource).IsAssignableFrom(type) || type == typeof(Resource))
            {
                return format switch
                {
                    ResourceFormat.Json => DefaultJsonMimeType,
                    ResourceFormat.Xml => DefaultXmlMimeType,
                    _ => DefaultXmlMimeType
                };
            }

            return OctetStreamMimeType;
        }

        public static string GetMediaType(this HttpRequestMessage request)
        {
            var headervalue = request.Content.Headers.ContentType;
            var s = headervalue?.MediaType;
            return Interpret(s);
        }

        public static string GetContentTypeHeaderValue(this HttpRequestMessage request)
        {
            var headervalue = request.Content.Headers.ContentType;
            return headervalue?.MediaType;
        }

        public static string GetAcceptHeaderValue(this HttpRequestMessage request)
        {
            var headers = request.Headers.Accept;
            return headers.FirstOrDefault()?.MediaType;
        }

        public static MediaTypeHeaderValue GetMediaTypeHeaderValue(Type type, ResourceFormat format)
        {
            var mediatype = GetContentType(type, format);
            var header = new MediaTypeHeaderValue(mediatype) {CharSet = Encoding.UTF8.WebName};
            return header;
        }
    }
}