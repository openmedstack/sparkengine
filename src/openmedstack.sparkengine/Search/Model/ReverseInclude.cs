// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace OpenMedStack.SparkEngine.Search.Model;

using System;
using System.Text.RegularExpressions;

public partial class ReverseInclude
{
    private static readonly Regex Pattern = ResourceSearchPathRegex();

    public string ResourceType { get; private set; } = null!;
    public string? SearchPath { get; private set; }

    /// <summary>
    ///     Expected format: ResourceType.searchParameter[.searchParameter]*
    /// </summary>
    /// <param name="reverseInclude"></param>
    /// <returns>
    ///     ReverseInclude instance with ResourceType is everything before the first dot, and SearchPath everything after
    ///     it.
    /// </returns>
    public static ReverseInclude Parse(string reverseInclude)
    {
        //_revinclude should have the following format: ResourceType.searchParameter[.searchParameter]*
        //so we simply split in on the first dot.
        if (reverseInclude == null)
        {
            throw new ArgumentNullException(nameof(reverseInclude), "reverseInclude cannot be null");
        }

        var result = new ReverseInclude();
        var match = Pattern.Match(reverseInclude);
        if (match.Groups.Count < 2)
        {
            throw new ArgumentException(
                $"reverseInclude '{reverseInclude}' does not adhere to the format 'ResourceType.searchParameter[.searchParameter]*'");
        }

        result.ResourceType = match.Groups["resourcetype"].Captures[0].Value;
        result.SearchPath = match.Groups["searchpath"].Captures[0].Value;

        return result;
    }

    [GeneratedRegex("(?<resourcetype>[^\\.]+)\\.(?<searchpath>.*)")]
    private static partial Regex ResourceSearchPathRegex();
}
