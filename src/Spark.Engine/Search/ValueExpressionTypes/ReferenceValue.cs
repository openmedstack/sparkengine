// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace Spark.Engine.Search.ValueExpressionTypes
{
    using System;
    using Hl7.Fhir.Model;
    using Support;

    public class ReferenceValue : ValueExpression
    {
        public ReferenceValue(string value)
        {
            if (!Uri.IsWellFormedUriString(value, UriKind.Absolute) && !Id.IsValidValue(value))
            {
                throw Error.Argument("text", "Reference is not a valid Id nor a valid absolute Url");
            }

            Value = value;
        }

        public string Value { get; }

        public override string ToString() => StringValue.EscapeString(Value);

        public static ReferenceValue Parse(string text)
        {
            var value = StringValue.UnescapeString(text);

            return new ReferenceValue(value);
        }
    }
}