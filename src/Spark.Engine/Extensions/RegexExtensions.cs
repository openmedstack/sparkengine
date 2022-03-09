// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace Spark.Engine.Extensions
{
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    public static class RegexExtensions
    {
        public static string ReplaceGroup(this Regex regex, string input, string groupName, string replacement) =>
            ReplaceGroups(regex, input, new Dictionary<string, string> {{groupName, replacement}});

        public static string ReplaceGroups(this Regex regex, string input, Dictionary<string, string> replacements)
        {
            return regex.Replace(input, m => ReplaceNamedGroups(m, replacements));
        }

        private static string ReplaceNamedGroups(Match m, Dictionary<string, string> replacements)
        {
            var result = m.Value;
            foreach (var (groupName, replaceWith) in replacements)
            {
                foreach (Capture cap in m.Groups[groupName].Captures)
                {
                    result = result.Remove(cap.Index - m.Index, cap.Length);
                    result = result.Insert(cap.Index - m.Index, replaceWith);
                }
            }

            return result;
        }
    }
}