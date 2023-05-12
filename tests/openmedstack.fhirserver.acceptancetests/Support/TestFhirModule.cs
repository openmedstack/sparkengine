namespace OpenMedStack.FhirServer.AcceptanceTests.Support;

using DotAuth.Shared;
using DotAuth.Shared.Models;
using DotAuth.Shared.Requests;
using DotAuth.Shared.Responses;
using System;
using DotAuth.Client;
using DotAuth.Uma;
using global::Autofac;
using OpenMedStack.SparkEngine.Interfaces;
using OpenMedStack.SparkEngine.Service.FhirServiceExtensions;

internal class TestFhirModule : Module
{
    private readonly FhirServerConfiguration _configuration;
    private readonly TestResourceMap _map;

    public TestFhirModule(FhirServerConfiguration configuration, TestResourceMap map)
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
        builder.RegisterInstance(_map)
            .As<IResourceMap>()
            .As<IResourceMapper>()
            .SingleInstance();
        builder.Register(_ => new TestTokenClient(_configuration))
            .AsSelf()
            .AsImplementedInterfaces()
            .InstancePerLifetimeScope();
//        builder.RegisterType<TestAccessTokenCache>().AsImplementedInterfaces().InstancePerDependency();
        builder.RegisterType<TestUmaResourceSetClient>().AsImplementedInterfaces().InstancePerDependency();
        builder.RegisterType<TestUmaPermissionsClient>().AsImplementedInterfaces().InstancePerDependency();

        builder.RegisterInstance(new ApplicationNameProvider(_configuration)).AsImplementedInterfaces()
            .SingleInstance();
    }
}

internal class TestUmaPermissionsClient : IUmaPermissionClient
{
    public async Task<Option<TicketResponse>> RequestPermission(
        string token,
        CancellationToken cancellationToken = new CancellationToken(),
        params PermissionRequest[] requests)
    {
        await Task.Yield();
        return new TicketResponse { TicketId = "ticket" };
    }

    public Task<UmaConfiguration> GetUmaDocument(CancellationToken cancellationToken = new CancellationToken())
    {
        throw new NotImplementedException();
    }

    public Uri Authority { get; } = new ("https://localhost");
}

internal class TestUmaResourceSetClient : IUmaResourceSetClient
{
    public Task<Option<UpdateResourceSetResponse>> UpdateResourceSet(
        ResourceSet request,
        string token,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<Option<AddResourceSetResponse>> AddResourceSet(
        ResourceSet request,
        string token,
        CancellationToken cancellationToken = default)
    {
        var response = new AddResourceSetResponse
            { Id = Guid.NewGuid().ToString(), UserAccessPolicyUri = "http://localhost" };
        return Task.FromResult<Option<AddResourceSetResponse>>(new Option<AddResourceSetResponse>.Result(response));
    }

    public Task<Option> DeleteResource(
        string resourceSetId,
        string token,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<Option<string[]>> GetAllOwnResourceSets(
        string token,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<Option<ResourceSet>> GetResourceSet(
        string resourceSetId,
        string token,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<Option<PagedResult<ResourceSetDescription>>> SearchResources(
        SearchResourceSet parameter,
        string? token = null,
        CancellationToken cancellationToken = new CancellationToken())
    {
        throw new NotImplementedException();
    }
}
