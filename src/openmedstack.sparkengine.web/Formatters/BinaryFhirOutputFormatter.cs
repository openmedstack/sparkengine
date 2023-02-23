// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace OpenMedStack.SparkEngine.Web.Formatters;

using System;
using System.Linq;
using Core;
using Hl7.Fhir.Model;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;

public class BinaryFhirOutputFormatter : OutputFormatter
{
    public BinaryFhirOutputFormatter()
    {
        SupportedMediaTypes.Add(new MediaTypeHeaderValue(FhirMediaType.OctetStreamMimeType));
    }

    /// <inheritdoc />
    public override bool CanWriteResult(OutputFormatterCanWriteContext context)
    {
        var contentTypes = context.ContentType.Value?.Split(';', StringSplitOptions.TrimEntries);
        return SupportedMediaTypes.Intersect(contentTypes ?? Array.Empty<string>()).Any() && base.CanWriteResult(context);
    }

    /// <inheritdoc />
    protected override bool CanWriteType(Type? type) => typeof(Resource).IsAssignableFrom(type);

    /// <inheritdoc />
    public override async System.Threading.Tasks.Task WriteResponseBodyAsync(OutputFormatterWriteContext context)
    {
        if (context.Object is not Binary binary)
        {
            return;
        }

        context.HttpContext.Response.Headers[HeaderNames.ContentType] = binary.ContentType;
        var responseBody = context.HttpContext.Response.Body;
        await responseBody.WriteAsync(binary.Data).ConfigureAwait(false);
        await responseBody.FlushAsync().ConfigureAwait(false);
    }
}