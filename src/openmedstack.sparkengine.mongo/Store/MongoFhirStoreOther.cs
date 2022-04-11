// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace Spark.Mongo.Store
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Engine.Core;
    using Engine.Store.Interfaces;
    using MongoDB.Bson;
    using MongoDB.Driver;
    using Search.Infrastructure;

    //TODO: decide if we still need this
    [Obsolete("Don't use it at all")]
    public class MongoFhirStoreOther
    {
        private readonly IMongoCollection<BsonDocument> _collection;
        private readonly IMongoDatabase _database;
        private readonly IFhirStore _mongoFhirStoreOther;

        public MongoFhirStoreOther(string mongoUrl, IFhirStore mongoFhirStoreOther)
        {
            _mongoFhirStoreOther = mongoFhirStoreOther;
            _database = MongoDatabaseFactory.GetMongoDatabase(mongoUrl);
            _collection = _database.GetCollection<BsonDocument>(Collection.RESOURCE);
            //this.transaction = new MongoSimpleTransaction(collection);
        }

        //TODO: I've commented this. Do we still need it?
        //public IList<string> List(string resource, DateTimeOffset? since = null)
        //{
        //    var clauses = new List<FilterDefinition<BsonDocument>>();

        //    clauses.Add(Builders<BsonDocument>.Filter.Eq(Field.TYPENAME, resource));
        //    if (since != null)
        //    {
        //        clauses.Add(Builders<BsonDocument>.Filter.GT(Field.WHEN, BsonDateTime.Create(since)));
        //    }
        //    clauses.Add(Builders<BsonDocument>.Filter.Eq(Field.STATE, Value.CURRENT));

        //    return FetchPrimaryKeys(clauses);
        //}


        public async Task<bool> Exists(IKey key)
        {
            // PERF: efficiency
            var existing = await _mongoFhirStoreOther.Get(key).ConfigureAwait(false);
            return existing != null;
        }

        //public Interaction Get(string primarykey)
        //{
        //    FilterDefinition<BsonDocument> query = MonQ.Query.Eq(Field.PRIMARYKEY, primarykey);
        //    BsonDocument document = collection.FindOne(query);
        //    if (document != null)
        //    {
        //        Interaction entry = document.ToInteraction();
        //        return entry;
        //    }
        //    else
        //    {
        //        return null;
        //    }
        //}

        public IList<Entry> GetCurrent(IEnumerable<string> identifiers, string sortby = null)
        {
            var clauses = new List<FilterDefinition<BsonDocument>>();
            var ids = identifiers.Select(i => (BsonValue) i);

            clauses.Add(Builders<BsonDocument>.Filter.In(Field.REFERENCE, ids));
            clauses.Add(Builders<BsonDocument>.Filter.Eq(Field.STATE, Value.CURRENT));
            var query = Builders<BsonDocument>.Filter.And(clauses);

            var cursor = _collection.Find(query);

            if (sortby != null)
            {
                cursor = cursor.Sort(Builders<BsonDocument>.Sort.Ascending(sortby));
            }
            else
            {
                cursor = cursor.Sort(Builders<BsonDocument>.Sort.Descending(Field.WHEN));
            }

            return cursor.ToEnumerable().ToEntries().ToList();
        }

        private void Supercede(IEnumerable<IKey> keys)
        {
            var pks = keys.Select(k => k.ToBsonReferenceKey());
            var query = Builders<BsonDocument>.Filter.And(
                Builders<BsonDocument>.Filter.In(Field.REFERENCE, pks),
                Builders<BsonDocument>.Filter.Eq(Field.STATE, Value.CURRENT));
            var update = Builders<BsonDocument>.Update.Set(Field.STATE, Value.SUPERCEDED);
            _collection.UpdateMany(query, update);
        }

        public void Add(IEnumerable<Entry> entries)
        {
            var keys = entries.Select(i => i.Key);
            Supercede(keys);
            IList<BsonDocument> documents = entries.Select(SparkBsonHelper.ToBsonDocument).ToList();
            _collection.InsertMany(documents);
        }
    }
}