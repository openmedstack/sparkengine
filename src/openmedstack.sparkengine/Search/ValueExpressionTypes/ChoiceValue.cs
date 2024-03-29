﻿// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace OpenMedStack.SparkEngine.Search.ValueExpressionTypes;

using System.Collections.Generic;
using System.Linq;
using Support;

public class ChoiceValue : ValueExpression
{
    private const char ValueSeparator = ',';

    public ChoiceValue(ValueExpression[] choices)
    {
        Choices = choices;
    }

    public ChoiceValue(IEnumerable<ValueExpression> choices) : this(choices.ToArray())
    {
    }

    public ValueExpression[] Choices { get; }

    public override string ToString()
    {
        var values = Choices.Select(v => v.ToString());
        return string.Join(ValueSeparator.ToString(), values);
    }

    public static ChoiceValue Parse(string text)
    {
        var values = text.SplitNotEscaped(ValueSeparator);

        return new ChoiceValue(values.Select(SplitIntoComposite));
    }

    private static ValueExpression SplitIntoComposite(string text)
    {
        var composite = CompositeValue.Parse(text);

        // If there's only one component, this really was a single value
        return composite.Components.Length == 1 ? composite.Components[0] : composite;
    }
}