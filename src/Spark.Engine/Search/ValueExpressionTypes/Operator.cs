// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace Spark.Engine.Search.ValueExpressionTypes
{
    /// <summary>
    ///     Types of comparison operator applicable to searching on integer values
    /// </summary>
    public enum Operator
    {
        LT, // less than
        LTE, // less than or equals
        EQ, // equals (default)
        APPROX, // approximately equals
        GTE, // greater than or equals
        GT, // greater than

        ISNULL, // has no value
        NOTNULL, // has value
        IN, // equals one of a set of values
        CHAIN, // chain to subexpression
        NOT_EQUAL, // not equal
        STARTS_AFTER,
        ENDS_BEFORE
    }
}