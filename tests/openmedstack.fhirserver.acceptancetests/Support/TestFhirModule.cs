using Baseline.ImTools;
using DotAuth.Shared;
using DotAuth.Shared.Models;
using DotAuth.Shared.Requests;
using DotAuth.Shared.Responses;

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
        builder.RegisterInstance(_configuration).As<DeploymentConfiguration>();
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
        builder.RegisterType<TestAccessTokenCache>().AsImplementedInterfaces().InstancePerDependency();
        builder.RegisterType<TestUmaResourceSetClient>().AsImplementedInterfaces().InstancePerDependency();
    }
}

internal class TestUmaResourceSetClient : IUmaResourceSetClient
{
    public async Task<Option<UpdateResourceSetResponse>> UpdateResourceSet(ResourceSet request, string token,
        CancellationToken cancellationToken = new CancellationToken())
    {
        throw new NotImplementedException();
    }

    public Task<Option<AddResourceSetResponse>> AddResourceSet(ResourceSet request, string token, CancellationToken cancellationToken = new CancellationToken())
    {
        var response = new AddResourceSetResponse { Id  = Guid.NewGuid().ToString(), UserAccessPolicyUri = "http://localhost" };
        return Task.FromResult<Option<AddResourceSetResponse>>(new Option<AddResourceSetResponse>.Result(response));
    }

    public async Task<Option> DeleteResource(string resourceSetId, string token, CancellationToken cancellationToken = new CancellationToken())
    {
        throw new NotImplementedException();
    }

    public async Task<Option<string[]>> GetAllOwnResourceSets(string token, CancellationToken cancellationToken = new CancellationToken())
    {
        throw new NotImplementedException();
    }

    public async Task<Option<ResourceSet>> GetResourceSet(string resourceSetId, string token, CancellationToken cancellationToken = new CancellationToken())
    {
        throw new NotImplementedException();
    }

    public async Task<Option<PagedResult<ResourceSetDescription>>> SearchResources(SearchResourceSet parameter, string? token = null,
        CancellationToken cancellationToken = new CancellationToken())
    {
        throw new NotImplementedException();
    }
}

 internal class TestAccessTokenCache : IAccessTokenCache
 {
     private readonly ITokenClient _tokenClient;

     public TestAccessTokenCache(ITokenClient tokenClient)
     {
         _tokenClient = tokenClient;
     }

     public async ValueTask<GrantedTokenResponse?> GetAccessToken(params string[] scopes)
     {
         var token = await _tokenClient.GetToken(TokenRequest.FromScopes(scopes)) as Option<GrantedTokenResponse>.Result;
         return token!.Item;
     }
 }
