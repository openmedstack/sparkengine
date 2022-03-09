// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace Spark.Engine.Web
{
    using System;
    using Handlers;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Routing;

    public static class ApplicationBuilderExtensions
    {
        public static void UseFhir(this IApplicationBuilder app, Action<IRouteBuilder> configureRoutes = null)
        {
            app.UseMiddleware<ErrorHandler>();
            app.UseMiddleware<FormatTypeHandler>();
            app.UseMiddleware<MaintenanceModeHandler>();

            if (configureRoutes == null)
            {
                app.UseMvc();
            }
            else
            {
                app.UseMvc(configureRoutes);
            }
        }
    }
}