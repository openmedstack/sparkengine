// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace Spark.Mongo.Search.Common
{
    using System;

    public static class MetaField
    {
        public const string
            COUNT = "_count",
            INCLUDE = "_include",
            LIMIT = "_limit"; // Limit is geen onderdeel vd. standaard

        public static string[] All = { COUNT, INCLUDE, LIMIT };
    }

    public static class Modifier
    {
        [Obsolete]
        public const string
            BEFORE = "before",
            AFTER = "after",
            Separator = ":";

        public const string
            EXACT = "exact",
            CONTAINS = "contains",
            PARTIAL = "partial",
            TEXT = "text",
            CODE = "code",
            ANYNAMESPACE = "anyns",
            MISSING = "missing",
            BELOW = "below",
            ABOVE = "above",
            NOT = "not",
            NONE = "",
            IDENTIFIER = "identifier";
    }
    
    public static class Config
    {
        public const string PARAM_TRUE = "true", PARAM_FALSE = "false";

        public const int PARAM_NOLIMIT = -1;

        public const int MaxSearchResults = 5000;

        public const string LuceneIndexPath = @"C:\Index", Mongoindexcollection = "searchindex";

        public static bool Equal(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
    }
}