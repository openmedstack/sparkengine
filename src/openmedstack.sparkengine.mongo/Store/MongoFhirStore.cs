/*
 * Copyright (c) 2016, Furore (info@furore.com) and contributors
 * Copyright (c) 2021, Incendi (info@incendi.no) and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/spark/stu3/master/LICENSE
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using Spark.Engine.Core;
using Spark.Engine.Store.Interfaces;

namespace Spark.Store.Mongo
{
    using Engine.Extensions;
    using Spark.Mongo.Search.Infrastructure;
    using Spark.Mongo.Store;

    public class MongoFhirStore : IFhirStore
    {
        private readonly IMongoDatabase _database;
        private readonly IMongoCollection<BsonDocument> _collection;

        public MongoFhirStore(string mongoUrl)
        {
            _database = MongoDatabaseFactory.GetMongoDatabase(mongoUrl);
            _collection = _database.GetCollection<BsonDocument>(Collection.RESOURCE);
        }

        public async Task Add(Entry entry)
        {
            BsonDocument document = SparkBsonHelper.ToBsonDocument(entry);
            await Supercede(entry.Key).ConfigureAwait(false);
            await _collection.InsertOneAsync(document).ConfigureAwait(false);
        }

        public async Task<Entry> Get(IKey key)
        {
            var clauses = new List<FilterDefinition<BsonDocument>>
            {
                Builders<BsonDocument>.Filter.Eq(Field.TYPENAME, key.TypeName),
                Builders<BsonDocument>.Filter.Eq(Field.RESOURCEID, key.ResourceId)
            };

            if (key.HasVersionId())
            {
                clauses.Add(Builders<BsonDocument>.Filter.Eq(Field.VERSIONID, key.VersionId));
            }
            else
            {
                clauses.Add(Builders<BsonDocument>.Filter.Eq(Field.STATE, Value.CURRENT));
            }

            FilterDefinition<BsonDocument> query = Builders<BsonDocument>.Filter.And(clauses);

            return (await _collection.FindAsync(query).ConfigureAwait(false))
                .FirstOrDefault()
                ?.ToEntry();
        }

        public async Task<IList<Entry>> Get(IEnumerable<IKey> identifiers)
        {
            if (!identifiers.Any())
                return new List<Entry>();

            IList<IKey> identifiersList = identifiers.ToList();
            var versionedIdentifiers = GetBsonValues(identifiersList, k => k.HasVersionId());
            var unversionedIdentifiers = GetBsonValues(identifiersList, k => k.HasVersionId() == false);

            var queries = new List<FilterDefinition<BsonDocument>>();
            if (versionedIdentifiers.Any())
                queries.Add(GetSpecificVersionQuery(versionedIdentifiers));
            if (unversionedIdentifiers.Any())
                queries.Add(GetCurrentVersionQuery(unversionedIdentifiers));
            FilterDefinition<BsonDocument> query = Builders<BsonDocument>.Filter.Or(queries);

            IEnumerable<BsonDocument> cursor = (await _collection.FindAsync(query).ConfigureAwait(false)).ToEnumerable();

            return cursor.ToEntries().ToList();
        }

        /// <inheritdoc />
        public Task<bool> Exists(IKey key)
        {
            return Task.FromResult(false);
        }

        private static IEnumerable<BsonValue> GetBsonValues(IEnumerable<IKey> identifiers, Func<IKey, bool> keyCondition)
        {
            return identifiers.Where(keyCondition).Select(k => (BsonValue)k.ToString());
        }

        private static FilterDefinition<BsonDocument> GetCurrentVersionQuery(IEnumerable<BsonValue> ids)
        {
            var clauses = new List<FilterDefinition<BsonDocument>>
            {
                Builders<BsonDocument>.Filter.In(Field.REFERENCE, ids),
                Builders<BsonDocument>.Filter.Eq(Field.STATE, Value.CURRENT)
            };
            return Builders<BsonDocument>.Filter.And(clauses);
        }

        private static FilterDefinition<BsonDocument> GetSpecificVersionQuery(IEnumerable<BsonValue> ids)
        {
            var clauses = new List<FilterDefinition<BsonDocument>>
            {
                Builders<BsonDocument>.Filter.In(Field.PRIMARYKEY, ids)
            };

            return Builders<BsonDocument>.Filter.And(clauses);
        }

        private async Task Supercede(IKey key)
        {
            var pk = key.ToBsonReferenceKey();
            FilterDefinition<BsonDocument> query = Builders<BsonDocument>.Filter.And(
                Builders<BsonDocument>.Filter.Eq(Field.REFERENCE, pk),
                Builders<BsonDocument>.Filter.Eq(Field.STATE, Value.CURRENT)
            );

            UpdateDefinition<BsonDocument> update = Builders<BsonDocument>.Update.Set(Field.STATE, Value.SUPERCEDED);
            // A single delete on a sharded collection must contain an exact match on _id (and have the collection default collation) or contain the shard key (and have the simple collation).
            await _collection.UpdateManyAsync(query, update).ConfigureAwait(false);
        }
    }
}
