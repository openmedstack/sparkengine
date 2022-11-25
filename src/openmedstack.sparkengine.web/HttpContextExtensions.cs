// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace OpenMedStack.SparkEngine.Web
{
    using System;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Http.Features;
    using Microsoft.AspNetCore.Mvc.Formatters;
    using Microsoft.AspNetCore.Mvc.Infrastructure;
    using Microsoft.Extensions.DependencyInjection;

    public static class HttpContextExtensions
    {
        private const string ResourceTypeKey = "resourceType";

        public static IOutputFormatter? SelectFormatter(
            this HttpContext context,
            OutputFormatterWriteContext writeContext)
        {
            var outputFormatterSelector = context.RequestServices.GetRequiredService<OutputFormatterSelector>();
            return outputFormatterSelector.SelectFormatter(
                writeContext,
                Array.Empty<IOutputFormatter>(),
                new MediaTypeCollection());
        }

        public static OutputFormatterWriteContext
            GetOutputFormatterWriteContext<T>(this HttpContext context, T model) where T : notnull =>
            context.GetOutputFormatterWriteContext(typeof(T), model);

        public static OutputFormatterWriteContext GetOutputFormatterWriteContext(
            this HttpContext context,
            Type type,
            object model)
        {
            var writerFactory = context.RequestServices.GetRequiredService<IHttpResponseStreamWriterFactory>();
            return new OutputFormatterWriteContext(context, writerFactory.CreateWriter, type, model);
        }
        
        public static void AddResourceType(this HttpContext context, Type resourceType)
        {
            if (context.Items.ContainsKey(ResourceTypeKey))
            {
                return;
            }

            context.Items.Add(ResourceTypeKey, resourceType);
        }
    }
}