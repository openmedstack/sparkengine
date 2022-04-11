// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace Spark.Mongo.Store
{
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using Engine.Core;
    using Engine.Store.Interfaces;
    using MongoDB.Bson;
    using MongoDB.Driver;
    using Search.Infrastructure;

    public class MongoFhirStorePagedReader : IFhirStorePagedReader
    {
        private readonly IMongoCollection<BsonDocument> _collection;

        public MongoFhirStorePagedReader(string mongoUrl)
        {
            var database = MongoDatabaseFactory.GetMongoDatabase(mongoUrl);
            _collection = database.GetCollection<BsonDocument>(Collection.RESOURCE);
        }

        public async IAsyncEnumerable<Entry> ReadAsync(
            FhirStorePageReaderOptions options,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            options ??= new FhirStorePageReaderOptions();

            var filter = Builders<BsonDocument>.Filter.Eq(Field.STATE, Value.CURRENT);

            var totalRecords = await _collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            for (var offset = 0; offset < totalRecords; offset += options.PageSize)
            {
                var data = await _collection.Find(filter)
                    .Sort(Builders<BsonDocument>.Sort.Ascending(Field.PRIMARYKEY))
                    .Skip(offset)
                    .Limit(options.PageSize)
                    .ToListAsync(cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
                foreach (var envelope in data)
                {
                    yield return envelope.ToEntry();
                }
            }
        }
    }
}
