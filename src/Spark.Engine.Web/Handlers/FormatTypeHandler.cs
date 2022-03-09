// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace Spark.Engine.Web.Handlers
{
    using System.Threading.Tasks;
    using Core;
    using Engine.Extensions;
    using Extensions;
    using Hl7.Fhir.Rest;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Primitives;

    public class FormatTypeHandler : IMiddleware
    {
        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            var format = context.Request.GetParameter("_format");
            if (!string.IsNullOrEmpty(format))
            {
                var accepted = ContentType.GetResourceFormatFromFormatParam(format);
                if (accepted != ResourceFormat.Unknown)
                {
                    if (context.Request.Headers.ContainsKey("Accept"))
                    {
                        context.Request.Headers.Remove("Accept");
                    }

                    context.Request.Headers.Add(
                        "Accept",
                        accepted == ResourceFormat.Json
                            ? new StringValues(ContentType.JSON_CONTENT_HEADER)
                            : new StringValues(ContentType.XML_CONTENT_HEADER));
                }
            }

            if (context.Request.IsRawBinaryPostOrPutRequest())
            {
                if (!context.Request.ContentType.IsContentTypeHeaderFhirMediaType())
                {
                    var contentType = context.Request.ContentType;
                    context.Request.Headers.Add("X-Content-Type", contentType);
                    context.Request.ContentType = FhirMediaType.OctetStreamMimeType;
                }
            }
            //else if(context.Request.IsRawBinaryRequest())
            //{
            //    if (context.Request.Headers.ContainsKey("Accept")) context.Request.Headers.Remove("Accept");
            //    context.Request.Headers.Add("Accept", new StringValues(FhirMediaType.OCTET_STREAM_CONTENT_HEADER));
            //}

            await next(context).ConfigureAwait(false);
        }
    }
}