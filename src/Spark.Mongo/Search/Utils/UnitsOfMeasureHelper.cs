// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace Spark.Mongo.Search.Utils
{
    using FM = Hl7.Fhir.Model;

    using Engine.Extensions;
    using Engine.Search.ValueExpressionTypes;
    using Fhir.Metrics;
    using MongoDB.Bson;

    public static class UnitsOfMeasureHelper
    {
        public static BsonDocument ToBson(this Quantity quantity)
        {
            quantity = QuantityExtensions.System.Canonical(quantity);
            var searchable = quantity.LeftSearchableString();

            var block = new BsonDocument
            {
                {"system", UCUM.Uri.ToString()},
                {"value", quantity.GetValueAsBson()},
                {"decimals", searchable},
                {"unit", quantity.Metric.ToString()}
            };
            return block;
        }

        public static BsonDocument NonUcumIndexed(this FM.Quantity quantity)
        {
            var system = quantity.System != null ? (BsonValue)quantity.System : BsonNull.Value;
            var code = quantity.Code != null ? (BsonValue)quantity.Code : BsonNull.Value;

            var block = new BsonDocument
            {
                {"system", system},
                {"value", quantity.GetValueAsBson()},
                {"unit", (BsonValue) quantity.Code ?? BsonNull.Value}
            };
            return block;
        }

        public static BsonDocument ToBson(this FM.Quantity quantity)
        {
            if (quantity.IsUcum())
            {
                var q = quantity.ToUnitsOfMeasureQuantity();
                return ToBson(q);
            }

            return quantity.NonUcumIndexed();
        }


        public static BsonDouble GetValueAsBson(this FM.Quantity quantity)
        {
            var value = (double)quantity.Value;
            return new BsonDouble(value);
        }

        public static BsonDouble GetValueAsBson(this Quantity quantity)
        {
            var value = (double)quantity.Value.ToDecimal();
            return new BsonDouble(value);
        }

        // This code might have a better place somewhere else: //mh
        public static FM.Quantity ToModelQuantity(this ValueExpression expression)
        {
            var q = QuantityValue.Parse(expression.ToString());
            var quantity = new FM.Quantity { Value = q.Number, System = q.Namespace, Unit = q.Unit, Code = q.Unit };
            return quantity;
        }
    }
}