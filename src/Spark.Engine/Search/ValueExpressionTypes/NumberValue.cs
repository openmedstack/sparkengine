// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace Spark.Engine.Search.ValueExpressionTypes
{
    using Hl7.Fhir.Serialization;

    public class NumberValue : ValueExpression
    {
        public NumberValue(decimal value) => Value = value;

        public decimal Value { get; }

        public override string ToString() => PrimitiveTypeConverter.ConvertTo<string>(Value);

        public static NumberValue Parse(string text) =>
            new(PrimitiveTypeConverter.ConvertTo<decimal>(text));
    }
}