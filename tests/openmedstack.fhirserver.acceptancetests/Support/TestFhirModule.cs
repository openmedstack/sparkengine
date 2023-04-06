namespace OpenMedStack.FhirServer.AcceptanceTests.Support;

using System;
using System.Net.Http;
using DotAuth.Client;
using DotAuth.Uma;
using global::Autofac;
using OpenMedStack.FhirServer.Handlers;
using OpenMedStack.SparkEngine.Interfaces;
using OpenMedStack.SparkEngine.Service.FhirServiceExtensions;

internal class TestFhirModule<T> : Module
where T: DeploymentConfiguration
{
    private readonly T _configuration;
    private readonly TestResourceMap _map;

    public TestFhirModule(T configuration, TestResourceMap map)
    {
        _configuration = configuration;
        _map = map;
    }

    /// <inheritdoc />
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<T>().AsSelf().As<DeploymentConfiguration>();
        builder.RegisterType<FhirEventListener>().AsImplementedInterfaces();
        builder.RegisterType<GuidGenerator>().As<IGenerator>().SingleInstance();
        builder.RegisterType<PatchService>().As<IPatchService>().SingleInstance();
        builder.Register(_ => _map)
            .As<IResourceMap>()
            .As<IResourceMapper>()
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
        builder.Register(_ => new TestTokenClient(_configuration))
            .AsSelf()
            .AsImplementedInterfaces()
            .InstancePerLifetimeScope();
    }
}
