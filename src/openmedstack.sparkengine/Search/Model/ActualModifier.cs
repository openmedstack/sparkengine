// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace OpenMedStack.SparkEngine.Search.Model;

using System;
using System.Collections.Generic;
using System.Linq;
using Hl7.Fhir.Model;
using Support;

public class ActualModifier
{
    private const string MISSINGTRUE = "true";
    private const string MISSINGFALSE = "false";
    private const string MISSING_SEPARATOR = "=";

    private static readonly Dictionary<string, Modifier> Mapping = new()
    {
        { "exact", Modifier.EXACT },
        { "partial", Modifier.PARTIAL },
        { "text", Modifier.TEXT },
        { "contains", Modifier.CONTAINS },
        { "anyns", Modifier.ANYNAMESPACE },
        { "missing", Modifier.MISSING },
        { "below", Modifier.BELOW },
        { "above", Modifier.ABOVE },
        { "in", Modifier.IN },
        { "not-in", Modifier.NOT_IN },
        { "", Modifier.NONE }
    };

    public ActualModifier(string rawModifier)
    {
        RawModifier = rawModifier;
        Missing = TryParseMissing(rawModifier);
        if (Missing.HasValue)
        {
            Modifier = Modifier.MISSING;
            return;
        }

        Modifier = Mapping
            .FirstOrDefault(m => m.Key.Equals(rawModifier, StringComparison.InvariantCultureIgnoreCase))
            .Value;

        if (Modifier == Modifier.UNKNOWN)
        {
            ModifierType = TryGetType(rawModifier);
            if (ModifierType != null)
            {
                Modifier = Modifier.TYPE;
            }
        }
    }

    public string RawModifier { get; }

    public Type? ModifierType { get; }

    public Modifier Modifier { get; }

    public bool? Missing { get; }

    /// <summary>
    ///     Catches missing, missing=true and missing=false
    /// </summary>
    /// <param name="rawModifier"></param>
    /// <returns></returns>
    private bool? TryParseMissing(string rawModifier)
    {
        var missing = Mapping.FirstOrDefault(m => m.Value == Modifier.MISSING).Key;
        var parts = rawModifier.Split([MISSING_SEPARATOR], StringSplitOptions.None);
        if (parts[0].Equals(missing, StringComparison.InvariantCultureIgnoreCase))
        {
            if (parts.Length > 1)
            {
                if (parts[1].Equals(MISSINGTRUE, StringComparison.InvariantCultureIgnoreCase))
                {
                    return true;
                }

                return parts[1].Equals(MISSINGFALSE, StringComparison.InvariantCultureIgnoreCase)
                    ? (bool?)false
                    : throw Error.Argument(
                        "rawModifier",
                        "For the :missing modifier, only values '{0}' and '{1}' are allowed",
                        MISSINGTRUE,
                        MISSINGFALSE);
            }

            return true;
        }

        return null;
    }

    private Type? TryGetType(string rawModifier) => ModelInfo.GetTypeForFhirType(rawModifier);

    public override string ToString()
    {
        var modifierText = Mapping.FirstOrDefault(m => m.Value == Modifier).Key;
        return Modifier switch
        {
            Modifier.MISSING => modifierText + MISSING_SEPARATOR + (Missing!.Value ? MISSINGTRUE : MISSINGFALSE),
            Modifier.TYPE => ModelInfo.GetFhirTypeNameForType(ModifierType!) ?? "",
            _ => modifierText
        };
    }
}
