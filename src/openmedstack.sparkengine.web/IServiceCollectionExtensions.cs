// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace OpenMedStack.SparkEngine.Web;

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
    public static IServiceCollection AddFhir<T>(
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

        services.AddSingleton(settings);
        services.AddTransient<ElementIndexer>();
        services.AddTransient<IReferenceNormalizationService, ReferenceNormalizationService>();
        services.AddTransient<IIndexService, IndexService>();
        services.AddSingleton<ILocalhost>(new Localhost(settings.Endpoint));
        services.AddSingleton<IFhirModel>(new FhirModel(ModelInfo.SearchParameters));
        services.AddTransient(provider => new FhirPropertyIndex(provider.GetRequiredService<IFhirModel>()));
        services.AddTransient<ITransfer, Transfer>();
        services.AddTransient<ConditionalHeaderFhirResponseInterceptor>();
        services.AddTransient(
            provider => new IFhirResponseInterceptor[]
            {
                provider.GetRequiredService<ConditionalHeaderFhirResponseInterceptor>()
            });
        services.AddTransient<IFhirResponseInterceptorRunner, FhirResponseInterceptorRunner>();
        services.AddTransient<IFhirResponseFactory, FhirResponseFactory>();
        services.AddTransient<IIndexRebuildService, IndexRebuildService>();
        services.AddTransient<ISnapshotPaginationProvider, SnapshotPaginationProvider>();
        services.AddTransient<ISnapshotPaginationCalculator, SnapshotPaginationCalculator>();
        services.AddTransient<IServiceListener, SearchService>(); // searchListener
        services.AddTransient(provider => provider.GetServices<IServiceListener>().ToArray());
        services.AddTransient<ISearchService, SearchService>(); // search
        services.AddTransient<ITransactionService, AsyncTransactionService>(); // transaction
        services.AddTransient<IPagingService, PagingService>(); // paging
        services.TryAddTransient<IResourceStorageService, ResourceStorageService>(); // storage
        services.AddTransient<ICapabilityStatementService, CapabilityStatementService>(); // conformance
        services.AddTransient<ICompositeServiceListener, ServiceListener>();

        services.TryAddSingleton<IFhirService, FhirService>();

        services.AddFhirFormatters(settings, setupAction);

        return services;
    }

    private static IServiceCollection AddFhirFormatters(
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
                }).Services;
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