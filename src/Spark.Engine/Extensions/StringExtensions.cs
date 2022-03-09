// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace Spark.Engine.Extensions
{
    public static class StringExtensions
    {
        public static string FirstUpper(this string input) =>
            string.IsNullOrWhiteSpace(input)
                ? input
                : string.Concat(input.Substring(0, 1).ToUpperInvariant(), input.Remove(0, 1));
    }
}