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
    using System.Linq;

    public static class UriUtils
    {
        public static Tuple<string, string> SplitParam(string s)
        {
            var a = s.Split(new[] { '=' }, 2);
            return new Tuple<string, string>(a.First(), a.Skip(1).FirstOrDefault());
        }

        public static ICollection<Tuple<string, string>> SplitParams(string query)
        {
            return query.TrimStart('?')
                .Split(new[] { '&' }, 2, StringSplitOptions.RemoveEmptyEntries)
                .Select(SplitParam)
                .ToList();
        }

        public static string JoinParams(IEnumerable<Tuple<string, string>> query)
        {
            return string.Join("&", query.Select(t => t.Item1 + "=" + t.Item2));
        }
    }
}