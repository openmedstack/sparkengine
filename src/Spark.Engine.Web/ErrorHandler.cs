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
    using System.Net;
    using System.Threading.Tasks;
    using Core;
    using Engine.Extensions;
    using Microsoft.AspNetCore.Http;

    // https://stackoverflow.com/a/38935583
    public class ErrorHandler : IMiddleware
    {
        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            try
            {
                await next(context).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                await HandleExceptionAsync(context, exception).ConfigureAwait(false);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var code = HttpStatusCode.InternalServerError;
            Hl7.Fhir.Model.OperationOutcome outcome;
            if (exception is SparkException ex1)
            {
                code = ex1.StatusCode;
                outcome = GetOperationOutcome(ex1);
            }
            else
            {
                outcome = GetOperationOutcome(exception);
            }

            // Set HTTP status code
            context.Response.StatusCode = (int) code;
            var writeContext = context.GetOutputFormatterWriteContext(outcome);
            var formatter = context.SelectFormatter(writeContext);
            // Write the OperationOutcome to the Response using an OutputFormatter from the request pipeline
            await formatter.WriteAsync(writeContext).ConfigureAwait(false);
        }

        private static Hl7.Fhir.Model.OperationOutcome GetOperationOutcome(SparkException exception) =>
            exception == null
                ? null
                : (exception.Outcome ?? new Hl7.Fhir.Model.OperationOutcome()).AddAllInnerErrors(exception);

        private static Hl7.Fhir.Model.OperationOutcome GetOperationOutcome(Exception exception) =>
            exception == null ? null : new Hl7.Fhir.Model.OperationOutcome().AddAllInnerErrors(exception);
    }
}