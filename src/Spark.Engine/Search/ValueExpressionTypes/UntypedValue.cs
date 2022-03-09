// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace Spark.Engine.Search.ValueExpressionTypes
{
    using Hl7.Fhir.Model;

    public class UntypedValue : ValueExpression
    {
        public UntypedValue(string value) => Value = value;

        public string Value { get; }

        public override string ToString() => Value;

        public NumberValue AsNumberValue() => NumberValue.Parse(Value);

        public DateValue AsDateValue() => DateValue.Parse(Value);

        public FhirDateTime AsDateTimeValue() => new(Value);

        public StringValue AsStringValue() => StringValue.Parse(Value);

        public TokenValue AsTokenValue() => TokenValue.Parse(Value);

        public QuantityValue AsQuantityValue() => QuantityValue.Parse(Value);

        public ReferenceValue AsReferenceValue() => ReferenceValue.Parse(Value);
    }
}