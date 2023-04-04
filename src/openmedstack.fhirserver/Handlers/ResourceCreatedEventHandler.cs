namespace OpenMedStack.FhirServer.Handlers;

using System;
using System.Threading;
using System.Threading.Tasks;
using DotAuth.Client;
using DotAuth.Shared;
using DotAuth.Shared.Models;
using DotAuth.Shared.Responses;
using Events;
using Microsoft.Extensions.Logging;
using OpenMedStack.Events;
using SparkEngine.Core;

public class ResourceCreatedEventHandler : EventHandlerBase<ResourceCreatedEvent>
{
    private readonly FhirServerConfiguration _configurationValues;
    private readonly ILogger<ResourceCreatedEventHandler> _logger;
    private readonly IUmaResourceSetClient _resourceSetClient;
    private readonly IResourceMapper _resourceMapper;

    /// <inheritdoc />
    public ResourceCreatedEventHandler(
        IUmaResourceSetClient resourceSetClient,
        IResourceMapper resourceMapper,
        FhirServerConfiguration configurationValues,
        ILogger<ResourceCreatedEventHandler> logger)
        : base(logger)
    {
        _resourceSetClient = resourceSetClient;
        _resourceMapper = resourceMapper;
        _configurationValues = configurationValues;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task HandleInternal(
        ResourceCreatedEvent domainEvent,
        IMessageHeaders headers,
        CancellationToken cancellationToken = default)
    {
        var key = Key.ParseOperationPath(domainEvent.Source);
        var scopes = new[] { "read" };
        var response = await _resourceSetClient.AddResourceSet(
            new ResourceSet
            {
                AuthorizationPolicies = new[]
                {
                    new PolicyRule
                    {
                        ClientIdsAllowed = new[] { _configurationValues.ClientId },
                        OpenIdProvider = _configurationValues.TokenService,
                        IsResourceOwnerConsentNeeded = true,
                        Scopes = scopes,
                    }
                },
                Scopes = scopes,
                Created = DateTimeOffset.UtcNow,
                Description = $"FHIR {key.TypeName} resource",
                Name = domainEvent.Source,
                Type = "FHIR Resource"
            },
            domainEvent.UserToken,
            cancellationToken);
        if (response is Option<AddResourceSetResponse>.Result result)
        {
            var resourceSetId = result.Item.Id;
            _logger.LogInformation(
                "Resource: {resourceId} registered as resource set: {resourceSetId}",
                domainEvent.ResourceId,
                resourceSetId);
            await _resourceMapper.MapResource(domainEvent.ResourceId, resourceSetId, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
