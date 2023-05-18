// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace OpenMedStack.SparkEngine;

using System;
using System.Diagnostics;
using System.Reflection;
using Hl7.Fhir.Serialization;
using Search;

public class SparkSettings
{
    public Uri Endpoint { get; init; } = null!;
    public ParserSettings? ParserSettings { get; init; }
    public SerializerSettings? SerializerSettings { get; init; }
    public IndexSettings? IndexSettings { get; set; }

    public static string Version
    {
        get
        {
            var asm = Assembly.GetExecutingAssembly();
            var version = FileVersionInfo.GetVersionInfo(asm.Location);
            return $"{version.ProductMajorPart}.{version.ProductMinorPart}";
        }
    }
}
