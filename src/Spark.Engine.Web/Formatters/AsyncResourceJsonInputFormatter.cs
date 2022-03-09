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
    using System.Net.Http.Headers;
    using System.Text;
    using System.Threading.Tasks;
    using Core;
    using Hl7.Fhir.Model;
    using Hl7.Fhir.Serialization;
    using Microsoft.AspNetCore.Mvc.Formatters;

    public class AsyncResourceJsonInputFormatter : TextInputFormatter
    {
        private readonly FhirJsonParser _parser;

        public AsyncResourceJsonInputFormatter(FhirJsonParser parser)
        {
            _parser = parser ?? throw new ArgumentNullException(nameof(parser));

            SupportedEncodings.Clear();
            SupportedEncodings.Add(Encoding.UTF8);

            foreach (var mediaType in FhirMediaType.JsonMimeTypes)
            {
                SupportedMediaTypes.Add(mediaType);
            }
        }

        protected override bool CanReadType(Type type)
        {
            return typeof(Resource).IsAssignableFrom(type);
        }

        /// <inheritdoc />
        public override bool CanRead(InputFormatterContext context)
        {
            var result = MediaTypeHeaderValue.TryParse(context.HttpContext.Request.ContentType, out var mediaType);
            return result && SupportedMediaTypes.Contains(mediaType.MediaType) && base.CanRead(context);
        }

        public override async Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context, Encoding encoding)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (encoding == null) throw new ArgumentNullException(nameof(encoding));
            if (!encoding.Equals(Encoding.UTF8))
                throw Error.BadRequest("FHIR supports UTF-8 encoding exclusively, not " + encoding.WebName);

            try
            {
                using var reader = new StreamReader(context.HttpContext.Request.Body, Encoding.UTF8, leaveOpen: true);
                var body = await reader.ReadToEndAsync().ConfigureAwait(false);
                var resource = _parser.Parse<Resource>(body);
                context.HttpContext.AddResourceType(resource.GetType());

                return await InputFormatterResult.SuccessAsync(resource).ConfigureAwait(false);
            }
            catch (FormatException exception)
            {
                throw Error.BadRequest($"Body parsing failed: {exception.Message}");
            }
        }
    }
}