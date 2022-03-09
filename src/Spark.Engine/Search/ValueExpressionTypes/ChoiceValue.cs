// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace Spark.Engine.Search.ValueExpressionTypes
{
    using System.Collections.Generic;
    using System.Linq;
    using Support;

    public class ChoiceValue : ValueExpression
    {
        private const char VALUESEPARATOR = ',';

        public ChoiceValue(ValueExpression[] choices)
        {
            if (choices == null)
            {
                Error.ArgumentNull("choices");
            }

            Choices = choices;
        }

        public ChoiceValue(IEnumerable<ValueExpression> choices)
        {
            if (choices == null)
            {
                Error.ArgumentNull("choices");
            }

            Choices = choices.ToArray();
        }

        public ValueExpression[] Choices { get; }

        public override string ToString()
        {
            var values = Choices.Select(v => v.ToString());
            return string.Join(VALUESEPARATOR.ToString(), values);
        }

        public static ChoiceValue Parse(string text)
        {
            if (text == null)
            {
                Error.ArgumentNull("text");
            }

            var values = text.SplitNotEscaped(VALUESEPARATOR);

            return new ChoiceValue(values.Select(SplitIntoComposite));
        }

        private static ValueExpression SplitIntoComposite(string text)
        {
            var composite = CompositeValue.Parse(text);

            // If there's only one component, this really was a single value
            return composite.Components.Length == 1 ? composite.Components[0] : composite;
        }
    }
}