// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace Spark.Mongo.Tests.Indexer
{
    using Engine.Model;
    using Engine.Search.ValueExpressionTypes;
    using Mongo.Search.Indexer;
    using Xunit;

    /// <summary>
    ///     Summary description for MongoIndexMapperTest
    /// </summary>
    public class MongoIndexMapperTest
    {
        private readonly MongoIndexMapper _sut;

        public MongoIndexMapperTest() => _sut = new MongoIndexMapper();

        [Fact]
        public void TestMapRootIndexValue()
        {
            //"root" element should be skipped.
            var iv = new IndexValue("root");
            iv.Values.Add(new IndexValue("internal_resource", new StringValue("Patient")));

            var results = _sut.MapEntry(iv);
            Assert.Single(results);
            var result = results[0];
            Assert.True(result.IsBsonDocument);
            Assert.Equal(2, result.AsBsonDocument.ElementCount);
            var firstElement = result.AsBsonDocument.GetElement(0);
            Assert.Equal("internal_level", firstElement.Name);
            var secondElement = result.GetElement(1);
            Assert.Equal("internal_resource", secondElement.Name);
            Assert.True(secondElement.Value.IsString);
            Assert.Equal("Patient", secondElement.Value.AsString);
        }
    }
}