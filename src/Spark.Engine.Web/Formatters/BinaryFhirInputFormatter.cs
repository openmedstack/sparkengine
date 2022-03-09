// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace Spark.Engine.Web.Formatters
{
    using System;
    using System.IO;
    using System.Linq;
    using Core;
    using Hl7.Fhir.Model;
    using Microsoft.AspNetCore.Mvc.Formatters;

    public class BinaryFhirInputFormatter : InputFormatter
    {
        public BinaryFhirInputFormatter()
        {
            SupportedMediaTypes.Add(
                new Microsoft.Net.Http.Headers.MediaTypeHeaderValue(FhirMediaType.OctetStreamMimeType));
        }

        /// <inheritdoc />
        public override bool CanRead(InputFormatterContext context)
        {
            var contentType = context.HttpContext.Request.ContentType;
            return SupportedMediaTypes.Contains(contentType) && base.CanRead(context);
        }

        /// <inheritdoc />
        protected override bool CanReadType(Type type) => typeof(Resource).IsAssignableFrom(type);

        /// <inheritdoc />
        public override async System.Threading.Tasks.Task<InputFormatterResult> ReadRequestBodyAsync(
            InputFormatterContext context)
        {
            var success = context.HttpContext.Request.Headers.ContainsKey("X-Content-Type");
            if (!success)
            {
                return await InputFormatterResult.FailureAsync().ConfigureAwait(false);
                //throw Error.BadRequest("POST to binary must provide a Content-Type header");
            }

            var contentType = context.HttpContext.Request.Headers["X-Content-Type"].First();
            var stream = new MemoryStream();
            await context.HttpContext.Request.Body.CopyToAsync(stream).ConfigureAwait(false);
            var binary = new Binary {Data = stream.ToArray(), ContentType = contentType};

            return await InputFormatterResult.SuccessAsync(binary).ConfigureAwait(false);
        }

        //public override Tasks.Task WriteToStreamAsync(Type type, object value, Stream writeStream, HttpContent content, System.Net.TransportContext transportContext)
        //{
        //    Binary binary = (Binary)value;
        //    var stream = new MemoryStream(binary.Data);
        //    content.Headers.ContentType = new MediaTypeHeaderValue(binary.ContentType);
        //    stream.CopyTo(writeStream);
        //    stream.Flush();

        //    return Tasks.Task.CompletedTask;
        //}
    }
}