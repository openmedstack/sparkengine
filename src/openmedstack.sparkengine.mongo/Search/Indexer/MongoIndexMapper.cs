// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace Spark.Mongo.Search.Indexer
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Common;
    using Engine.Model;
    using Engine.Search.ValueExpressionTypes;
    using MongoDB.Bson;

    //Maps IndexValue elements to BsonElements.
    public class MongoIndexMapper
    {
        /// <summary>
        ///     Meant for mapping the root IndexValue (and all the stuff below it)
        /// </summary>
        /// <param name="indexValue"></param>
        /// <returns>List of BsonDocuments, one for the root and one for each contained index in it.</returns>
        public List<BsonDocument> MapEntry(IndexValue indexValue)
        {
            var result = new List<BsonDocument>();

            if (indexValue.Name == "root")
            {
                EntryToDocument(indexValue, 0, result);
                return result;
            }

            throw new ArgumentException("MapEntry is only meant for mapping a root IndexValue.", nameof(indexValue));
        }

        private void EntryToDocument(IndexValue indexValue, int level, List<BsonDocument> result)
        {
            //Add the real values (not contained) to a document and add that to the result.
            var notNestedValues = indexValue.Values.OfType<IndexValue>().Where(exp => exp.Name != "contained").ToList();
            var doc = new BsonDocument(new BsonElement(InternalField.LEVEL, level));
            doc.AddRange(notNestedValues.Select(IndexValueToElement));
            result.Add(doc);

            //Then do that recursively for all contained indexed resources.
            var containedValues = indexValue.Values.OfType<IndexValue>().Where(exp => exp.Name == "contained").ToList();
            foreach (var contained in containedValues)
            {
                EntryToDocument(contained, level + 1, result);
            }
        }

        private BsonValue Map(Expression expression) => MapExpression((dynamic) expression);

        private BsonValue MapExpression(IndexValue indexValue) => new BsonDocument(IndexValueToElement(indexValue));

        private BsonElement IndexValueToElement(IndexValue indexValue)
        {
            if (indexValue.Name == "_id")
            {
                indexValue.Name = "fhir_id"; //_id is reserved in Mongo for the primary key and must be unique.
            }

            if (indexValue.Values.Count == 1)
            {
                return new BsonElement(indexValue.Name, Map(indexValue.Values[0]));
            }

            var values = new BsonArray();
            foreach (var value in indexValue.Values)
            {
                values.Add(Map(value));
            }

            return new BsonElement(indexValue.Name, values);
        }

        private BsonValue MapExpression(CompositeValue composite)
        {
            var compositeDocument = new BsonDocument();
            foreach (var component in composite.Components)
            {
                if (component is IndexValue value)
                {
                    compositeDocument.Add(IndexValueToElement(value));
                }
                else
                {
                    throw new ArgumentException("All Components of composite are expected to be of type IndexValue");
                }
            }

            return compositeDocument;
        }

        private static BsonValue MapExpression(StringValue stringValue) => BsonValue.Create(stringValue.Value);

        private static BsonValue MapExpression(DateTimeValue datetimeValue) =>
            BsonValue.Create(datetimeValue.Value.UtcDateTime);

        private static BsonValue MapExpression(DateValue dateValue) => BsonValue.Create(dateValue.Value);

        private static BsonValue MapExpression(NumberValue numberValue) => BsonValue.Create((double) numberValue.Value);
        //TODO: double is not as accurate as decimal, but MongoDB has no support for decimal.
        //https://docs.mongodb.org/v2.6/tutorial/model-monetary-data/#monetary-value-exact-precision.
    }
}