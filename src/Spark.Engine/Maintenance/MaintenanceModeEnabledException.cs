// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace Spark.Engine.Maintenance
{
    using System.Net;
    using Core;

    internal class MaintenanceModeEnabledException : SparkException
    {
        public MaintenanceModeEnabledException()
            : base(HttpStatusCode.ServiceUnavailable)
        {
        }
    }
}