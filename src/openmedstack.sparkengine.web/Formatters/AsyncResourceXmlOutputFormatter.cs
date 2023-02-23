// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace OpenMedStack.SparkEngine.Web.Formatters
{
    using System;
    using System.Linq;
    using System.Text;
    using Core;
    using Extensions;
    using Hl7.Fhir.Model;
    using Hl7.Fhir.Serialization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Formatters;
    using Microsoft.Net.Http.Headers;
    using Task = System.Threading.Tasks.Task;

    public class AsyncResourceXmlOutputFormatter : TextOutputFormatter
    {
        private readonly FhirXmlSerializer _serializer;

        public AsyncResourceXmlOutputFormatter(FhirXmlSerializer serializer)
        {
            _serializer = serializer;
            SupportedEncodings.Clear();
            SupportedEncodings.Add(Encoding.UTF8);

            foreach (var mediaType in FhirMediaType.XmlMimeTypes)
            {
                SupportedMediaTypes.Add(mediaType);
            }
        }

        /// <inheritdoc />
        public override bool CanWriteResult(OutputFormatterCanWriteContext context)
        {
            var contentTypes = context.ContentType.Value?.Split(';', StringSplitOptions.TrimEntries);
            return SupportedMediaTypes.Intersect(contentTypes ?? Array.Empty<string>()).Any() && CanWriteType(context.ObjectType);
        }

        protected override bool CanWriteType(Type? type)
        {
            return
                typeof(Resource).IsAssignableFrom(type)
                || typeof(FhirResponse).IsAssignableFrom(type)
                || typeof(ValidationProblemDetails).IsAssignableFrom(type);
        }

        /// <inheritdoc />
        public override void WriteResponseHeaders(OutputFormatterWriteContext context)
        {
            context.HttpContext.Response.ContentType =
                new MediaTypeHeaderValue("application/fhir+xml") { Encoding = Encoding.UTF8 }.ToString();
        }

        public override async Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (selectedEncoding == null)
            {
                throw new ArgumentNullException(nameof(selectedEncoding));
            }

            if (selectedEncoding != Encoding.UTF8)
            {
                throw Error.BadRequest($"FHIR supports UTF-8 encoding exclusively, not {selectedEncoding.WebName}");
            }

            var responseBody = context.HttpContext.Response.Body;
            var writeBodyString = string.Empty;
            var summaryType = context.HttpContext.Request.RequestSummary();

            if (context.Object is FhirResponse response)
            {
                context.HttpContext.Response.AcquireHeaders(response);
                context.HttpContext.Response.StatusCode = (int)response.StatusCode;

                if (response.Resource != null)
                {
                    writeBodyString = await _serializer.SerializeToStringAsync(response.Resource, summaryType).ConfigureAwait(false);
                }
            }
            else if (context.ObjectType == typeof(OperationOutcome) || typeof(Resource).IsAssignableFrom(context.ObjectType))
            {
                if (context.Object != null)
                {
                    writeBodyString = await _serializer.SerializeToStringAsync((context.Object as Resource)!, summaryType).ConfigureAwait(false);
                }
            }
            else if (context.Object is ValidationProblemDetails)
            {
                var outcome = new OperationOutcome();
                //outcome.AddValidationProblems(context.HttpContext.GetResourceType(), (HttpStatusCode)context.HttpContext.Response.StatusCode, validationProblems);
                writeBodyString = await _serializer.SerializeToStringAsync(outcome, summaryType).ConfigureAwait(false);
            }

            if (!string.IsNullOrWhiteSpace(writeBodyString))
            {
                var writeBuffer = selectedEncoding.GetBytes(writeBodyString);
                await responseBody.WriteAsync(writeBuffer.AsMemory(0, writeBuffer.Length)).ConfigureAwait(false);
                await responseBody.FlushAsync().ConfigureAwait(false);
            }
        }
    }
}