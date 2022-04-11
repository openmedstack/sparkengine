// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace Spark.Mongo.Search.Infrastructure
{
    using System.Collections.Generic;
    using System.Linq;
    using MongoDB.Driver;

    public static class MongoDatabaseFactory
    {
        private static Dictionary<string, IMongoDatabase> _instances;

        public static IMongoDatabase GetMongoDatabase(string url)
        {
            IMongoDatabase result;

            if (_instances == null) //instances dictionary is not at all initialized
            {
                _instances = new Dictionary<string, IMongoDatabase>();
            }

            if (!_instances.Where(i => i.Key == url).Any()) //there is no instance for this url yet
            {
                result = CreateMongoDatabase(url);
                _instances.Add(url, result);
            }

            ;
            return _instances.First(i => i.Key == url).Value; //now there must be one.
        }

        private static IMongoDatabase CreateMongoDatabase(string url)
        {
            var mongourl = new MongoUrl(url);
            var client = new MongoClient(mongourl);
            return client.GetDatabase(mongourl.DatabaseName);
        }
    }
}