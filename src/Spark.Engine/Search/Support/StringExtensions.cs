// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace Spark.Engine.Search.Support
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public static class StringExtensions
    {
        public static string[] SplitNotEscaped(this string value, char separator)
        {
            var word = string.Empty;
            var result = new List<string>();
            var seenEscape = false;

            foreach (var t in value)
            {
                if (t == '\\')
                {
                    seenEscape = true;
                    continue;
                }

                if (t == separator && !seenEscape)
                {
                    result.Add(word);
                    word = string.Empty;
                    continue;
                }

                if (seenEscape)
                {
                    word += '\\';
                    seenEscape = false;
                }

                word += t;
            }

            result.Add(word);

            return result.ToArray<string>();
        }

        public static Tuple<string, string> SplitLeft(this string text, char separator)
        {
            var pos = text.IndexOf(separator);

            if (pos == -1)
            {
                return Tuple.Create(text, (string) null); // Nothing to split
            }

            var key = text.Substring(0, pos);
            var value = text[(pos + 1)..];

            return Tuple.Create(key, value);
        }
    }
}