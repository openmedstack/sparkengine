// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace Spark.Engine.Model
{
    using System.Collections.Generic;
    using System.Linq;
    using Search.ValueExpressionTypes;

    public class IndexValue : ValueExpression
    {
        private readonly List<Expression> _values;

        private IndexValue() => _values = new List<Expression>();

        public IndexValue(string name)
            : this() =>
            Name = name;

        public IndexValue(string name, List<Expression> values)
            : this(name) =>
            Values = values;

        public IndexValue(string name, params Expression[] values)
            : this(name) =>
            Values = values.ToList();

        public string Name { get; set; }

        public List<Expression> Values
        {
            get => _values;
            private set => _values.AddRange(value);
        }
    }
}