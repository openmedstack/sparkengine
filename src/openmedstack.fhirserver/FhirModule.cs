﻿namespace OpenMedStack.FhirServer;

using System;
using System.Net.Http;
using DotAuth.Client;
using DotAuth.Uma;
using global::Autofac;
using SparkEngine.Interfaces;
using SparkEngine.Service.FhirServiceExtensions;

internal class FhirModule : Module
{
    private readonly DeploymentConfiguration _configuration;

    public FhirModule(DeploymentConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <inheritdoc />
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<FhirEventListener>().AsImplementedInterfaces();
        builder.RegisterType<GuidGenerator>().As<IGenerator>().SingleInstance();
        builder.RegisterType<PatchService>().As<IPatchService>().SingleInstance();
        builder.Register(_ => new DbSourceMap(_configuration.ConnectionString))
            .As<IResourceMap>()
            .InstancePerDependency();
        builder.Register(
                ctx =>
                {
                    return new UmaClient(
                        () =>
                        {
                            var factory = ctx.Resolve<IHttpClientFactory>();
                            return factory.CreateClient();
                        },
                        new Uri(_configuration.TokenService));
                })
            .AsSelf()
            .AsImplementedInterfaces().InstancePerLifetimeScope();
        builder.Register(
                sp => new TokenClient(
                    TokenCredentials.FromClientCredentials(_configuration.ClientId, _configuration.Secret),
                    () =>
                    {
                        var factory = sp.Resolve<IHttpClientFactory>();
                        return factory.CreateClient();
                    },
                    new Uri(_configuration.TokenService)))
            .AsSelf()
            .AsImplementedInterfaces()
            .InstancePerLifetimeScope();

    }
}