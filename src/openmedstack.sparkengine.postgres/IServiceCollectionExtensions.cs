// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace OpenMedStack.SparkEngine.Postgres
{
    using System;
    using Interfaces;
    using Marten;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using Microsoft.Extensions.Logging;
    using Store.Interfaces;

    public static class ServiceCollectionExtensions
    {
        public static void AddPostgresFhirStore(this IServiceCollection services, StoreSettings settings)
        {
            var store = DocumentStore.For(
                o =>
                {
                    o.Connection(settings.ConnectionString);
                    //o.PLV8Enabled = false;
                    o.Schema.Include<FhirRegistry>();
                });
            services.AddSingleton<IDocumentStore>(store);
            services.TryAddSingleton(settings);
            services.AddSingleton<IGenerator>(new GuidGenerator());
            services.AddTransient<Func<IDocumentSession>>(
                sp => () => sp.GetRequiredService<IDocumentStore>().OpenSession());
            services.TryAddTransient<IFhirStore>(
                provider => new MartenFhirStore(provider.GetRequiredService<Func<IDocumentSession>>()));
            services.TryAddTransient<IFhirStorePagedReader>(
                provider => new MartenFhirStorePagedReader(provider.GetRequiredService<Func<IDocumentSession>>()));
            services.TryAddTransient<IHistoryStore>(
                provider => new MartenHistoryStore(provider.GetRequiredService<Func<IDocumentSession>>()));
            services.TryAddTransient<ISnapshotStore>(
                provider => new MartenSnapshotStore(
                    provider.GetRequiredService<Func<IDocumentSession>>(),
                    provider.GetRequiredService<ILogger<MartenSnapshotStore>>()));
            services.TryAddTransient<IFhirStoreAdministration>(
                provider => new MartenFhirStoreAdministration(provider.GetRequiredService<Func<IDocumentSession>>()));
            services.TryAddTransient<IIndexStore>(
                sp => new MartenFhirIndex(
                    sp.GetRequiredService<ILogger<MartenFhirIndex>>(),
                    sp.GetRequiredService<Func<IDocumentSession>>()));
            services.TryAddTransient<IFhirIndex>(
                sp => new MartenFhirIndex(
                    sp.GetRequiredService<ILogger<MartenFhirIndex>>(),
                    sp.GetRequiredService<Func<IDocumentSession>>()));
        }
    }
}