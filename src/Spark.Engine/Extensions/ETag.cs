// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace Spark.Engine.Extensions
{
    using System.Net.Http.Headers;

    public static class ETag
    {
        public static EntityTagHeaderValue Create(string value)
        {
            var tag = "\"" + value + "\"";
            return new EntityTagHeaderValue(tag, true);
        }
    }
}