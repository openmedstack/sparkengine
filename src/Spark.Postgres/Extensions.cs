// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace Spark.Postgres
{
    using System.Collections.Generic;

    internal static class Extensions
    {
        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> source) => new(source);
    }
}