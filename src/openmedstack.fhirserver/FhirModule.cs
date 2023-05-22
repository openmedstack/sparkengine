using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace OpenMedStack.FhirServer;

using System;
using System.Net.Http;
using DotAuth.Client;
using DotAuth.Uma;
using global::Autofac;
using SparkEngine.Interfaces;
using SparkEngine.Service.FhirServiceExtensions;

internal class FhirModule : Module
{
    private readonly FhirServerConfiguration _configuration;

    public FhirModule(FhirServerConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <inheritdoc />
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterInstance(new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            DateTimeZoneHandling = DateTimeZoneHandling.RoundtripKind,
            NullValueHandling = NullValueHandling.Include,
            DefaultValueHandling = DefaultValueHandling.Include,
            TypeNameHandling = TypeNameHandling.Auto,
            Formatting = Formatting.None,
            DateParseHandling = DateParseHandling.DateTimeOffset
        }).AsSelf().SingleInstance();
        builder.RegisterInstance(_configuration)
            .AsSelf()
            .As<DeploymentConfiguration>()
            .IfNotRegistered(typeof(DeploymentConfiguration));
        builder.RegisterType<ConfigurationTenantProvider>().As<IProvideTenant>().InstancePerRequest();
        builder.RegisterType<FhirEventListener>().AsImplementedInterfaces();
        builder.RegisterType<GuidGenerator>().As<IGenerator>().SingleInstance();
        builder.RegisterType<PatchService>().As<IPatchService>().SingleInstance();
        builder.Register(_ => new DbSourceMap(_configuration.ConnectionString))
            .As<IResourceMap>()
            .As<IResourceMapper>()
            .InstancePerDependency();
        builder.Register<Func<HttpClient>>(ctx =>
        {
            var factory = ctx.Resolve<IHttpClientFactory>();
            return factory.CreateClient;
        }).As<Func<HttpClient>>().InstancePerLifetimeScope();
        builder.Register(
                ctx => new UmaClient(
                    ctx.Resolve<Func<HttpClient>>(),
                    new Uri(_configuration.TokenService)))
            .AsSelf()
            .AsImplementedInterfaces()
            .InstancePerLifetimeScope();
        builder.Register(
                sp => new TokenClient(
                    TokenCredentials.FromClientCredentials(_configuration.ClientId, _configuration.Secret),
                    sp.Resolve<Func<HttpClient>>(),
                    new Uri(_configuration.TokenService)))
            .AsSelf()
            .AsImplementedInterfaces()
            .InstancePerLifetimeScope();
        builder.RegisterType<TokenCache>().AsSelf().AsImplementedInterfaces().SingleInstance();
        builder.RegisterInstance(new ApplicationNameProvider(_configuration))
            .AsImplementedInterfaces()
            .SingleInstance();
    }
}
