// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace Spark.Mongo.Extensions
{
    using Engine;
    using Engine.Interfaces;
    using Engine.Store.Interfaces;
    using Hl7.Fhir.Model;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using Search.Common;
    using Search.Indexer;
    using Search.Infrastructure;
    using Search.Searcher;
    using Spark.Search.Mongo;
    using Spark.Store.Mongo;
    using Store;
    using Store.Extensions;

    public static class ServiceCollectionExtensions
    {
        public static void AddMongoFhirStore(this IServiceCollection services, StoreSettings settings)
        {
            services.TryAddSingleton(settings);
            services.TryAddTransient<IGenerator>(provider => new MongoIdGenerator(settings.ConnectionString));
            services.TryAddTransient<IFhirStore>(provider => new MongoFhirStore(settings.ConnectionString));
            services.TryAddTransient<IFhirStorePagedReader>(
                provider => new MongoFhirStorePagedReader(settings.ConnectionString));
            services.TryAddTransient<IHistoryStore>(provider => new HistoryStore(settings.ConnectionString));
            services.TryAddTransient<ISnapshotStore>(provider => new MongoSnapshotStore(settings.ConnectionString));
            services.TryAddTransient<IFhirStoreAdministration>(
                provider => new MongoStoreAdministration(settings.ConnectionString));
            services.TryAddTransient<MongoIndexMapper>();
            services.TryAddTransient<IIndexStore>(
                provider => new MongoIndexStore(
                    settings.ConnectionString,
                    provider.GetRequiredService<MongoIndexMapper>()));
            services.TryAddTransient(
                provider => new MongoIndexStore(
                    settings.ConnectionString,
                    provider.GetRequiredService<MongoIndexMapper>()));
            services.TryAddTransient(provider => DefinitionsFactory.Generate(ModelInfo.SearchParameters));
            services.TryAddTransient<MongoSearcher>();
            services.TryAddTransient<IFhirIndex, MongoFhirIndex>();
        }
    }
}