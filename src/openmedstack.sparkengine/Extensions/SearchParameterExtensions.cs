﻿// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace OpenMedStack.SparkEngine.Extensions;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Hl7.Fhir.Utility;

public static class SearchParameterExtensions
{
    private const string XPATH_SEPARATOR = "/";
    private const string PATH_SEPARATOR = ".";

    private const string GENERAL_PATH_PATTERN =
        @"(?<chainPart>(?<element>[^{0}\(]+)(?<predicate>\((?<propname>[^=]*)=(?<filterValue>[^\)]*)\))?((?<separator>{0})|(?<endofinput>$)))+";

    public static readonly Regex XpathPattern =
        new(string.Format(@"(?<root>^//)" + GENERAL_PATH_PATTERN, XPATH_SEPARATOR));

    public static readonly Regex
        PathPattern = new(string.Format(GENERAL_PATH_PATTERN, @"\" + PATH_SEPARATOR));

    public static void SetPropertyPath(this SearchParameter searchParameter, string[] paths)
    {
        string[] workingPaths;
        //if (paths != null)
        //{
        // TODO: Added FirstOrDefault to searchParameter.Base.GetLiteral() could possibly generate a bug

        //A searchparameter always has a Resource as focus, so we don't need the name of the resource to be at the start of the Path.
        //See also: https://github.com/ewoutkramer/fhirpath/blob/master/fhirpath.md
        workingPaths = paths.Select(
                pp => StripResourceNameFromStart(pp, searchParameter.Base.First()!.GetLiteral()!))
            .ToArray();
        var xpaths = workingPaths.Select(
            pp => "//" + PathPattern.ReplaceGroup(pp, "separator", XPATH_SEPARATOR));
        //searchParameter.Xpath = string.Join(" | ", xpaths);
        //}
        //else
        //{
        //    searchParameter.Xpath = string.Empty;
        //    //Null is not an error, for example Composite parameters don't have a path.
        //}
    }

    private static string StripResourceNameFromStart(string path, string resourceName)
    {
        if (path == null || resourceName == null)
        {
            throw new ArgumentException("path and resourceName are both mandatory.");
        }

        if (path.StartsWith(resourceName, StringComparison.CurrentCultureIgnoreCase))
        {
            //Path is like "Patient.birthdate", but "Patient." is superfluous. Ignore it.
            return path.Remove(0, resourceName.Length + 1);
        }

        return path;
    }

    //public static string[] GetPropertyPath(this SearchParameter searchParameter)
    //{
    //    if (searchParameter.Xpath != null)
    //    {
    //        var xpaths = searchParameter.Xpath.Split(new[] {" | "}, StringSplitOptions.None);
    //        return xpaths.Select(
    //                xp => XpathPattern.ReplaceGroups(
    //                    xp,
    //                    new Dictionary<string, string> {{"separator", PATH_SEPARATOR}, {"root", string.Empty}}))
    //            .ToArray();
    //    }

    //    return Array.Empty<string>();
    //}

    public static ModelInfo.SearchParamDefinition GetOriginalDefinition(this SearchParameter searchParameter) =>
        searchParameter.Annotation<ModelInfo.SearchParamDefinition>();

    public static void SetOriginalDefinition(
        this SearchParameter searchParameter,
        ModelInfo.SearchParamDefinition definition)
    {
        searchParameter.AddAnnotation(definition);
    }

    public static SearchParams AddAll(this SearchParams self, List<Tuple<string, string>> @params)
    {
        foreach (var (item1, item2) in @params)
        {
            self.Add(item1, item2);
        }

        return self;
    }
}
