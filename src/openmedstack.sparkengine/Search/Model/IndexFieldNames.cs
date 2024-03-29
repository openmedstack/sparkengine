﻿// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace OpenMedStack.SparkEngine.Search.Model;

public static class IndexFieldNames
{
    public const string
        // Internally stored search fields
        ID = "internal_id",
        JUSTID = "internal_justid",
        SELFLINK = "internal_selflink",
        CONTAINER = "internal_container",
        RESOURCE = "internal_resource",
        FOR_RESOURCE = "internal_forResource",
        LEVEL = "internal_level",
        TAG = "internal_tag",
        TAGSCHEME = "scheme",
        TAGTERM = "term",
        TAGLABEL = "label",
        LASTUPDATED = "lastupdated";

    public static string[] All = [ID, JUSTID, SELFLINK, CONTAINER, RESOURCE, LEVEL, TAG, LASTUPDATED];
}
