// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace Spark.Engine.Core
{
    using System;
    using System.Net;
    using Hl7.Fhir.Model;

    // Placed in a sub-namespace because you must be explicit about it if you want to throw this error directly

    // todo: Can this be replaced by a FhirOperationException ?

    public class SparkException : Exception
    {
        public SparkException(HttpStatusCode statuscode, string message = null)
            : base(message) =>
            StatusCode = statuscode;

        public SparkException(HttpStatusCode statuscode, string message, params object[] values)
            : base(string.Format(message, values)) =>
            StatusCode = statuscode;

        public SparkException(HttpStatusCode statuscode, OperationOutcome outcome, string message = null)
            : this(statuscode, message) =>
            Outcome = outcome;

        public HttpStatusCode StatusCode { get; set; }

        public OperationOutcome Outcome { get; set; }
    }
}