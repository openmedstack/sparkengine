// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace OpenMedStack.SparkEngine.Core;

using System.Collections.Generic;
using System.Linq;
using Hl7.Fhir.Model;
using Search.ValueExpressionTypes;

/// <summary>
/// Describes the result of a search operation.
/// </summary>
public class SearchResults : List<string>
{
    private readonly OperationOutcome _outcome = new() { Issue = [] };

    public required Criterium[] UsedCriteria { get; init; }

    /// <summary>
    /// Gets the total number of matches for the search operation, excluding any includes and reverse includes.
    /// </summary>
    public required int MatchCount { get; init; }

    /// <summary>
    /// Gets the outcome of the search operation.
    /// </summary>
    public OperationOutcome? Outcome => _outcome.Issue.Any() ? _outcome : null;

    /// <summary>
    /// Gets whether the search operation has any errors.
    /// </summary>
    public bool HasErrors
    {
        get { return Outcome != null && Outcome.Issue.Any(i => i.Severity <= OperationOutcome.IssueSeverity.Error); }
    }

    /// <summary>
    /// Gets a description of the parameters used in the search.
    /// </summary>
    public string UsedParameters
    {
        get
        {
            var used = UsedCriteria.Select(c => c.ToString()).ToArray();
            return string.Join("&", used);
        }
    }
}
