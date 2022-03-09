// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace Spark.Mongo.Search.Common
{
    public static class MetaField
    {
        public const string
            COUNT = "_count", INCLUDE = "_include", LIMIT = "_limit"; // Limit is geen onderdeel vd. standaard

        public static string[] All = {COUNT, INCLUDE, LIMIT};
    }
}