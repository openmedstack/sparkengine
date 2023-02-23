﻿// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace OpenMedStack.SparkEngine.Web
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Controllers;
    using Core;
    using FhirResponseFactory;
    using Formatters;
    using Hl7.Fhir.Model;
    using Hl7.Fhir.Serialization;
    using Interfaces;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using OpenMedStack.SparkEngine.Store.Interfaces;
    using OpenMedStack.SparkEngine.Web.Persistence;
    using Search;
    using Service;
    using Service.FhirServiceExtensions;
    
    [Route("fhir")]
    public class DefaultFhirController : FhirController
    {
        /// <inheritdoc />
        public DefaultFhirController(IFhirService fhirService)
            : base(fhirService)
        {
        }
    }

    public static class ServiceCollectionExtensions
    {
        public static IMvcCoreBuilder AddFhir<T>(
            this IServiceCollection services,
            SparkSettings settings,
            Action<MvcOptions>? setupAction = null)
        where T : FhirController
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }
            
            services.RemoveAll(typeof(FhirController));
            services.AddTransient<T>();
            if (typeof(T).GetCustomAttribute<RouteAttribute>()?.Template != "fhir")
            {
                services.AddTransient<DefaultFhirController>();
            }
            services.AddTransient<ControllerBase>(sp => sp.GetRequiredService<T>());
            AddFhirHttpSearchParameters();
            services.SetContentTypeAsFhirMediaTypeOnValidationError();

            services.TryAddSingleton(settings);
            services.TryAddTransient<ElementIndexer>();

            services.TryAddTransient<IReferenceNormalizationService, ReferenceNormalizationService>();

            services.TryAddTransient<IIndexService, IndexService>();
            services.AddSingleton<ILocalhost>(new Localhost(settings.Endpoint));
            services.AddSingleton<IFhirModel>(new FhirModel(ModelInfo.SearchParameters));
            services.TryAddTransient(provider => new FhirPropertyIndex(provider.GetRequiredService<IFhirModel>()));
            services.TryAddTransient<ITransfer, Transfer>();
            services.TryAddTransient<ConditionalHeaderFhirResponseInterceptor>();
            services.TryAddTransient(
                provider => new IFhirResponseInterceptor[]
                {
                    provider.GetRequiredService<ConditionalHeaderFhirResponseInterceptor>()
                });
            services.TryAddTransient<IFhirResponseInterceptorRunner, FhirResponseInterceptorRunner>();
            services.TryAddTransient<IFhirResponseFactory, FhirResponseFactory>();
            services.TryAddTransient<IIndexRebuildService, IndexRebuildService>();
            services.TryAddTransient<ISnapshotPaginationProvider, SnapshotPaginationProvider>();
            services.TryAddTransient<ISnapshotPaginationCalculator, SnapshotPaginationCalculator>();
            services.TryAddTransient<IServiceListener, SearchService>(); // searchListener
            services.TryAddTransient(provider => new[] { provider.GetRequiredService<IServiceListener>() });
            services.TryAddTransient<ISearchService, SearchService>(); // search
            services.TryAddTransient<ITransactionService, AsyncTransactionService>(); // transaction
            services.TryAddTransient<IPagingService, PagingService>(); // paging
            services.TryAddTransient<IResourceStorageService, ResourceStorageService>(); // storage
            services.TryAddTransient<ICapabilityStatementService, CapabilityStatementService>(); // conformance
            services.TryAddTransient<ICompositeServiceListener, ServiceListener>();

            services.TryAddSingleton<IFhirService, FhirService>();

            var builder = services.AddFhirFormatters(settings, setupAction);

            return builder;
        }

        private static IMvcCoreBuilder AddFhirFormatters(
            this IServiceCollection services,
            SparkSettings settings,
            Action<MvcOptions>? setupAction = null)
        {
            return settings == null
                ? throw new ArgumentNullException(nameof(settings))
                : services.AddMvcCore(
                    options =>
                    {
                        options.InputFormatters.Insert(0,
                            new AsyncResourceJsonInputFormatter(new FhirJsonParser(settings.ParserSettings)));
                        options.InputFormatters.Insert(0,
                            new AsyncResourceXmlInputFormatter(new FhirXmlParser(settings.ParserSettings)));
                        options.InputFormatters.Insert(0, new BinaryFhirInputFormatter());
                        options.OutputFormatters.Insert(0,
                            new AsyncResourceJsonOutputFormatter(new FhirJsonSerializer(settings.SerializerSettings)));
                        options.OutputFormatters.Insert(0,
                            new AsyncResourceXmlOutputFormatter(new FhirXmlSerializer(settings.SerializerSettings)));
                        options.OutputFormatters.Insert(0, new BinaryFhirOutputFormatter());

                        options.RespectBrowserAcceptHeader = true;

                        setupAction?.Invoke(options);
                    });
        }

        public static IServiceCollection AddInMemoryPersistence(this IServiceCollection services)
        {
            return services.AddSingleton<InMemoryFhirIndex>()
                .AddSingleton<IFhirIndex>(sp => sp.GetRequiredService<InMemoryFhirIndex>())
                .AddSingleton<ISnapshotStore, InMemorySnapshotStore>()
                .AddSingleton<IHistoryStore, InMemoryHistoryStore>()
                .AddSingleton<IFhirStore, InMemoryFhirStore>()
                .AddSingleton<IIndexStore>(sp => sp.GetRequiredService<InMemoryFhirIndex>());
        }

        public static void AddCustomSearchParameters(
            this IServiceCollection _,
            IEnumerable<ModelInfo.SearchParamDefinition> searchParameters)
        {
            // Add any user-supplied SearchParameters
            ModelInfo.SearchParameters.AddRange(searchParameters);
        }

        private static void AddFhirHttpSearchParameters()
        {
            ModelInfo.SearchParameters.AddRange(
                new[]
                {
                    new ModelInfo.SearchParamDefinition
                    {
                        Resource = "Resource",
                        Name = "_id",
                        Type = SearchParamType.String,
                        Path = new[] {"Resource.id"}
                    },
                    new ModelInfo.SearchParamDefinition
                    {
                        Resource = "Resource",
                        Name = "_lastUpdated",
                        Type = SearchParamType.Date,
                        Path = new[] {"Resource.meta.lastUpdated"}
                    },
                    new ModelInfo.SearchParamDefinition
                    {
                        Resource = "Resource",
                        Name = "_tag",
                        Type = SearchParamType.Token,
                        Path = new[] {"Resource.meta.tag"}
                    },
                    new ModelInfo.SearchParamDefinition
                    {
                        Resource = "Resource",
                        Name = "_profile",
                        Type = SearchParamType.Uri,
                        Path = new[] {"Resource.meta.profile"}
                    },
                    new ModelInfo.SearchParamDefinition
                    {
                        Resource = "Resource",
                        Name = "_security",
                        Type = SearchParamType.Token,
                        Path = new[] {"Resource.meta.security"}
                    }
                });
        }

        private static void SetContentTypeAsFhirMediaTypeOnValidationError(this IServiceCollection services)
        {
            static IActionResult ProcessObjectResult(ObjectResult actionResult)
            {
                actionResult.ContentTypes.Clear();
                foreach (var mediaType in FhirMediaType.JsonMimeTypes.Concat(FhirMediaType.XmlMimeTypes))
                {
                    actionResult.ContentTypes.Add(mediaType);
                }

                return actionResult;
            }

            // Validation errors need to be returned as application/json or application/xml
            // instead of application/problem+json and application/problem+xml.
            // (https://github.com/FirelyTeam/spark/issues/282)
            services.Configure<ApiBehaviorOptions>(options =>
            {
                var defaultInvalidModelStateResponseFactory = options.InvalidModelStateResponseFactory;
                options.InvalidModelStateResponseFactory = context =>
                {
                    var result = defaultInvalidModelStateResponseFactory(context);
                    return result switch
                    {
                        ObjectResult actionResult => ProcessObjectResult(actionResult),
                        _ => result
                    };
                };
            });
        }
    }
}