// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace Spark.Engine.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Fhir.Metrics;
    using Model;
    using Search.ValueExpressionTypes;
    using FM = Hl7.Fhir.Model;

    public static class QuantityExtensions
    {
        public static readonly string UcumUriString = "http://unitsofmeasure.org";
        public static readonly SystemOfUnits System = UCUM.Load();

        public static Quantity ToUnitsOfMeasureQuantity(this FM.Quantity input)
        {
            var metric = input.Code != null ? System.Metric(input.Code) : new Metric(new List<Metric.Axis>());
            Exponential value = input.Value ?? 1; //todo: is this assumption correct?
            return new Quantity(value, metric);
        }
        
        public static Expression ToExpression(this Quantity quantity)
        {
            quantity = quantity.Canonical();
            var searchable = quantity.LeftSearchableString();

            var values = new List<ValueExpression>
            {
                new IndexValue("system", new StringValue(UcumUriString)),
                new IndexValue("value", new NumberValue(quantity.Value.ToDecimal())),
                new IndexValue("decimals", new StringValue(searchable)),
                new IndexValue("unit", new StringValue(quantity.Metric.ToString()))
            };

            return new CompositeValue(values);
        }

        public static Expression NonUcumIndexedExpression(this FM.Quantity quantity)
        {
            var values = new List<ValueExpression>();
            if (quantity.System != null)
            {
                values.Add(new IndexValue("system", new StringValue(quantity.System)));
            }

            if (quantity.Unit != null)
            {
                values.Add(new IndexValue("unit", new StringValue(quantity.Unit)));
            }

            if (quantity.Value.HasValue)
            {
                values.Add(new IndexValue("value", new NumberValue(quantity.Value.Value)));
            }

            return values.Any() ? new CompositeValue(values) : null;
        }

        public static Expression ToExpression(this FM.Quantity quantity)
        {
            if (quantity.IsUcum())
            {
                var q = quantity.ToUnitsOfMeasureQuantity();
                return q.ToExpression();
            }

            return quantity.NonUcumIndexedExpression();
        }

        public static bool IsUcum(this FM.Quantity quantity) =>
            quantity.System != null && new Uri(UcumUriString).IsBaseOf(new Uri(quantity.System));

        public static Quantity Canonical(this Quantity input)
        {
            var output = input.Metric.Symbols switch
            {
                // TODO: Conversion of Celsius to its base unit Kelvin fails using the method SystemOfUnits::Canoncial
                // Waiting for feedback on issue: https://github.com/FirelyTeam/Fhir.Metrics/issues/7
                "Cel" => new Quantity(input.Value + 273.15m, System.Metric("K")),
                _ => System.Canonical(input)
            };

            return output;
        }

        public static string SearchableString(this Quantity quantity) =>
            quantity.LeftSearchableString(); // extension access
    }
}