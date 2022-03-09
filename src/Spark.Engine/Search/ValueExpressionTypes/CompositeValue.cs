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

    public class CompositeValue : ValueExpression
    {
        private const char _tupleseparator = '$';

        public CompositeValue(ValueExpression[] components) =>
            Components = components ?? throw Error.ArgumentNull("components");

        public CompositeValue(IEnumerable<ValueExpression> components)
        {
            if (components == null)
            {
                throw Error.ArgumentNull("components");
            }

            Components = components.ToArray();
        }

        public ValueExpression[] Components { get; }

        public override string ToString()
        {
            var values = Components.Select(v => v.ToString());
            return string.Join(_tupleseparator.ToString(), values);
        }


        public static CompositeValue Parse(string text)
        {
            if (text == null)
            {
                throw Error.ArgumentNull("text");
            }

            var values = text.SplitNotEscaped(_tupleseparator);

            return new CompositeValue(values.Select(v => new UntypedValue(v)));
        }
    }
}