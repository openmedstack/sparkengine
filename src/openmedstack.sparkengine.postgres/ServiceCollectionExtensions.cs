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
        services.AddSingleton<IDocumentStore>(
            sp => DocumentStore.For(
                o =>
                {
                    o.Serializer(sp.GetRequiredService<ISerializer>());
                    o.Connection(settings.ConnectionString);
                    o.DatabaseSchemaName = settings.Schema;
                    o.Schema.Include<FhirRegistry>();
                    /*
                     
    DROP INDEX IF EXISTS public.mt_doc_indexentry_idx_data_values;
    
    CREATE INDEX IF NOT EXISTS mt_doc_indexentry_idx_data_values
        ON public.mt_doc_indexentry USING gin
        ((data -> 'values'::text) jsonb_ops)
        TABLESPACE pg_default;
        
    REINDEX INDEX public.mt_doc_indexentry_idx_data_values;
    
                     */
                    o.AutoCreateSchemaObjects = AutoCreate.None;
                }));
        services.TryAddSingleton(settings);
        services.TryAddTransient(sp => sp.GetRequiredService<StoreSettings>().SerializerSettings);
        services.TryAddTransient<ISerializer, CustomSerializer>();
        services.AddTransient<Func<IDocumentSession>>(
            sp => () => sp.GetRequiredService<IDocumentStore>().OpenSession());
        services.TryAddTransient<IResourcePersistence, MartenResourcePersistence>();
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
