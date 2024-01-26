// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

using Microsoft.Net.Http.Headers;

namespace OpenMedStack.SparkEngine.Web.Handlers;

using System.Threading.Tasks;
using Core;
using Extensions;
using Hl7.Fhir.Rest;
using Microsoft.AspNetCore.Http;

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

                context.Request.GetTypedHeaders().Accept.Add(
                    accepted == ResourceFormat.Json
                        ? new MediaTypeHeaderValue(ContentType.JSON_CONTENT_HEADER)
                        : new MediaTypeHeaderValue(ContentType.XML_CONTENT_HEADER));
            }
        }

        if (context.Request.IsRawBinaryPostOrPutRequest())
        {
            if (!context.Request.ContentType.IsContentTypeHeaderFhirMediaType())
            {
                var contentType = context.Request.ContentType;
                context.Request.Headers["X-Content-Type"] = contentType;
                context.Request.ContentType = FhirMediaType.OctetStreamMimeType;
            }
        }

        await next(context).ConfigureAwait(false);
    }
}
