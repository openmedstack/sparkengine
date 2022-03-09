// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace Spark.Engine.Web
{
    using System.Threading.Tasks;
    using Maintenance;
    using Microsoft.AspNetCore.Http;
    internal static class HttpHeaderName
    {
        public const string ACCEPT = "Accept";
        public const string CONTENT_DISPOSITION = "Content-Disposition";
        public const string CONTENT_LOCATION = "Content-Location";
        public const string CONTENT_TYPE = "Content-Type";
        public const string ETAG = "ETag";
        public const string LOCATION = "Location";
        public const string LAST_MODIFIED = "Last-Modified";

        public const string X_CONTENT_TYPE = "X-Content-Type";
    }

    public class MaintenanceModeHandler
    {
        private readonly RequestDelegate _next;

        public MaintenanceModeHandler(RequestDelegate next) => _next = next;

        public async Task InvokeAsync(HttpContext context)
        {
            if (MaintenanceMode.IsEnabledForHttpMethod(context.Request.Method))
            {
                context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                return;
            }

            await _next(context).ConfigureAwait(false);
        }
    }
}