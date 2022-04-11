// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace OpenMedStack.SparkEngine.Search.ValueExpressionTypes
{
    public abstract class ValueExpression : Expression
    {
        public string ToUnescapedString()
        {
            ValueExpression value = this;
            if (value is UntypedValue untyped)
            {
                value = untyped.AsStringValue();

                return StringValue.UnescapeString(value.ToString()!);
            }

            return value.ToString()!;
        }
    }
}