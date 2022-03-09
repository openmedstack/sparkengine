// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace Spark.Engine
{
    using System;
    using System.Diagnostics;
    using System.Reflection;
    using Hl7.Fhir.Serialization;
    using Search;

    public class SparkSettings
    {
        public Uri Endpoint { get; set; }
        public bool UseAsynchronousIO { get; set; }
        public ParserSettings ParserSettings { get; set; }
        public SerializerSettings SerializerSettings { get; set; }
        public ExportSettings ExportSettings { get; set; }
        public IndexSettings IndexSettings { get; set; }
        public SearchSettings Search { get; set; }
        public string FhirRelease { get; set; }

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
}