// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace Spark.Engine.Core
{
    using System.Net;

    public static class Error
    {
        public static SparkException Internal(string message, params object[] values) =>
            new(HttpStatusCode.InternalServerError, message, values);

        public static SparkException NotFound(string message, params object[] values) =>
            new(HttpStatusCode.NotFound, message, values);

        public static SparkException Create(HttpStatusCode code, string message, params object[] values) =>
            new(code, message, values);

        public static SparkException BadRequest(string message, params object[] values) =>
            new(HttpStatusCode.BadRequest, message, values);
    }
}
