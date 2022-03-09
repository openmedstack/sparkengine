// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace Spark.Engine.Search.Model
{
    public enum Modifier
    {
        UNKNOWN = 0,
        EXACT = 1,
        PARTIAL = 2,
        TEXT = 3,
        CONTAINS = 4,
        ANYNAMESPACE = 5,
        MISSING = 6,
        BELOW = 7,
        ABOVE = 8,
        IN = 9,
        NOT_IN = 10,
        TYPE = 11,
        NONE = 12
    }
}