// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace Spark.Engine.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Net;
    using Core;
    using Hl7.Fhir.Model;

    public static class OperationOutcomeExtensions
    {
        //internal static Func<string, string> pascalToCamelCase = (pascalCase) => $"{char.ToLower(pascalCase[0])}{pascalCase[1..]}";

        //public static OperationOutcome AddValidationProblems(this OperationOutcome outcome, Type resourceType, HttpStatusCode code, ValidationProblemDetails validationProblems)
        //{
        //    if (resourceType == null) throw new ArgumentNullException(nameof(resourceType));
        //    if (validationProblems == null) throw new ArgumentNullException(nameof(ValidationProblemDetails));

        //    OperationOutcome.IssueSeverity severity = IssueSeverityOf(code);
        //    foreach (var error in validationProblems.Errors)
        //    {
        //        var expression = FhirPathUtil.ResolveToFhirPathExpression(resourceType, error.Key);
        //        outcome.Issue.Add(new OperationOutcome.IssueComponent
        //        {
        //            Severity = severity,
        //            Code = OperationOutcome.IssueType.Required,
        //            Diagnostics = error.Value.FirstOrDefault(),
        //            Expression = new[] { expression },
        //            Location = new[] { FhirPathUtil.ConvertToXPathExpression(expression) }
        //        });
        //    }

        //    return outcome;
        //}

        internal static OperationOutcome.IssueSeverity IssueSeverityOf(HttpStatusCode code)
        {
            var range = (int) code / 100;
            return range switch
            {
                1 => OperationOutcome.IssueSeverity.Information,
                2 => OperationOutcome.IssueSeverity.Information,
                3 => OperationOutcome.IssueSeverity.Warning,
                4 => OperationOutcome.IssueSeverity.Error,
                5 => OperationOutcome.IssueSeverity.Fatal,
                _ => OperationOutcome.IssueSeverity.Information
            };
        }

        public static OperationOutcome Init(this OperationOutcome outcome)
        {
            outcome.Issue ??= new List<OperationOutcome.IssueComponent>();
            return outcome;
        }

        public static OperationOutcome AddError(this OperationOutcome outcome, Exception exception)
        {
            string message;

            message = exception is SparkException
                ? exception.Message
                : $"{exception.GetType().Name}: {exception.Message}";

            outcome.AddError(message);

            // Don't add a stacktrace if this is an acceptable logical-level error
            if (Debugger.IsAttached && !(exception is SparkException))
            {
                var stackTrace = new OperationOutcome.IssueComponent
                {
                    Severity = OperationOutcome.IssueSeverity.Information, Diagnostics = exception.StackTrace
                };
                outcome.Issue.Add(stackTrace);
            }

            return outcome;
        }

        public static OperationOutcome AddAllInnerErrors(this OperationOutcome outcome, Exception exception)
        {
            AddError(outcome, exception);
            while (exception.InnerException != null)
            {
                exception = exception.InnerException;
                AddError(outcome, exception);
            }

            return outcome;
        }

        public static OperationOutcome AddError(this OperationOutcome outcome, string message) =>
            outcome.AddIssue(OperationOutcome.IssueSeverity.Error, message);

        private static OperationOutcome AddIssue(
            this OperationOutcome outcome,
            OperationOutcome.IssueSeverity severity,
            string message)
        {
            if (outcome.Issue == null)
            {
                outcome.Init();
            }

            var item = new OperationOutcome.IssueComponent {Severity = severity, Diagnostics = message};
            outcome.Issue.Add(item);
            return outcome;
        }
    }
}