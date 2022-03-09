﻿// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace Spark.Engine.Search
{
    using System;
    using System.Linq;
    using Core;
    using Extensions;
    using ValueExpressionTypes;

    public class ReferenceNormalizationService : IReferenceNormalizationService
    {
        private readonly ILocalhost _localhost;

        public ReferenceNormalizationService(ILocalhost localhost) =>
            _localhost = localhost ?? throw new ArgumentNullException(nameof(localhost));

        public ValueExpression GetNormalizedReferenceValue(ValueExpression originalValue, string resourceType)
        {
            if (originalValue == null)
            {
                return null;
            }

            var value = originalValue.ToString();
            if (string.IsNullOrWhiteSpace(value))
            {
                return originalValue;
            }

            if (!value.Contains("/") && !string.IsNullOrWhiteSpace(resourceType))
            {
                return new StringValue($"{resourceType}/{value}");
            }

            if (Uri.TryCreate(value, UriKind.RelativeOrAbsolute, out var uri))
            {
                var key = KeyExtensions.ExtractKey(uri);
                if (_localhost.GetKeyKind(key) != KeyKind.Foreign
                ) // Don't normalize external references (https://github.com/FirelyTeam/spark/issues/244).
                {
                    var refUri = _localhost.RemoveBase(uri);
                    return new StringValue(refUri.ToString().TrimStart('/'));
                }
            }

            return originalValue;
        }

        public Criterium GetNormalizedReferenceCriteria(Criterium c)
        {
            if (c == null)
            {
                throw new ArgumentNullException(nameof(c));
            }

            Expression operand;

            if (c.Operand is ChoiceValue choiceOperand)
            {
                var normalizedChoicesList = new ChoiceValue(
                    choiceOperand.Choices
                        .Select(choice => GetNormalizedReferenceValue(choice as UntypedValue, c.Modifier))
                        .Where(normalizedValue => normalizedValue != null)
                        .ToList());

                if (!normalizedChoicesList.Choices.Any())
                {
                    return null; // Choice operator without choices: ignore it.
                }

                operand = normalizedChoicesList;
            }
            else
            {
                var normalizedValue = GetNormalizedReferenceValue(c.Operand as UntypedValue, c.Modifier);
                if (normalizedValue == null)
                {
                    return null;
                }

                operand = normalizedValue;
            }

            var cloned = c.Clone();
            cloned.Modifier = null;
            cloned.Operand = operand;
            cloned.SearchParameters.AddRange(c.SearchParameters);
            return cloned;
        }
    }
}