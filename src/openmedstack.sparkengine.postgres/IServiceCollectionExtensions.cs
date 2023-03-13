// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace OpenMedStack.SparkEngine.Postgres;

using System;
using Interfaces;
using Marten;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Weasel.Core;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPostgresFhirStore(this IServiceCollection services, StoreSettings settings)
    {
        var store = DocumentStore.For(
            o =>
            {
                o.Serializer<CustomSerializer>();
                o.Connection(settings.ConnectionString);
                o.Schema.Include<FhirRegistry>();
                o.AutoCreateSchemaObjects = AutoCreate.CreateOrUpdate;
            });
        services.AddSingleton<IDocumentStore>(store);
        services.TryAddSingleton(settings);
        services.AddTransient<Func<IDocumentSession>>(
            sp => () => sp.GetRequiredService<IDocumentStore>().OpenSession());
        services.TryAddTransient<IResourcePersistence>(_ => NoOpPersistence.Get());
        services.TryAddTransient<IFhirStore, MartenFhirStore>();
        services.TryAddTransient<IFhirStorePagedReader, MartenFhirStorePagedReader>();
        services.TryAddTransient<IHistoryStore, MartenHistoryStore>();
        services.TryAddTransient<ISnapshotStore, MartenSnapshotStore>();
        services.TryAddTransient<IFhirStoreAdministration, MartenFhirStoreAdministration>();
        services.TryAddTransient<IIndexStore, MartenFhirIndex>();
        services.TryAddTransient<IFhirIndex, MartenFhirIndex>();
        return services;
    }
}
