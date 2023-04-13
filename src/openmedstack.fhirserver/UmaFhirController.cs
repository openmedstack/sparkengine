using Microsoft.AspNetCore.Mvc.Filters;

namespace OpenMedStack.FhirServer;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using DotAuth.Client;
using DotAuth.Shared;
using DotAuth.Shared.Models;
using DotAuth.Shared.Requests;
using DotAuth.Uma;
using DotAuth.Uma.Web;
using Events;
using Hl7.Fhir.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OpenMedStack.Events;
using SparkEngine.Core;
using SparkEngine.Extensions;
using SparkEngine.Interfaces;
using SparkEngine.Web.Controllers;

[Route("uma")]
public class UmaFhirController : FhirController
{
    private readonly IAccessTokenCache _tokenCache;
    private readonly IUmaResourceSetClient _resourceSetClient;
    private readonly IResourceMap _resourceMap;
    private readonly IPublishEvents _eventPublisher;
    private readonly ILogger<UmaFhirController> _logger;

    public UmaFhirController(
        IFhirService fhirService,
        IAccessTokenCache tokenCache,
        IUmaResourceSetClient resourceSetClient,
        IResourceMap resourceMap,
        IPublishEvents eventPublisher,
        ILogger<UmaFhirController> logger)
        : base(fhirService)
    {
        _tokenCache = tokenCache;
        _resourceSetClient = resourceSetClient;
        _resourceMap = resourceMap;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    [AllowAnonymous]
    [UmaFilter("{0}/{1}", new[] { "type", "id" }, allowedScope: "read")]
    public override Task<ActionResult<FhirResponse>> Read(string type, string id, CancellationToken cancellationToken)
    {
        return base.Read(type, id, cancellationToken);
    }

    [AllowAnonymous]
    [UmaFilter("{0}/{1}/_history/{2}", new[] { "type", "id", "vid" }, allowedScope: "read")]
    public override Task<FhirResponse> VRead(string type, string id, string vid, CancellationToken cancellationToken)
    {
        return base.VRead(type, id, vid, cancellationToken);
    }

    // [UmaFilter("{0}", new[] { "type" }, allowedScope: "create")]
    [OwnResourceFilter]
    public override async Task<FhirResponse?> Create(string type, Resource resource,
        CancellationToken cancellationToken)
    {
        var response = await base.Create(type, resource, cancellationToken).ConfigureAwait(false);

        var token = Request.Headers.Authorization;
        if (response?.StatusCode != HttpStatusCode.Created || token.Count <= 0)
        {
            return response;
        }

        var key = resource.ExtractKey().ToStorageKey();
        _logger.LogInformation("Registering resource {resourceId}", key);
        var cmd = new ResourceCreatedEvent(
            "fhir",
            token[0]!,
            key,
            DateTimeOffset.UtcNow);
        await _eventPublisher.Publish(cmd, cancellationToken: cancellationToken).ConfigureAwait(false);

        return response;
    }

    [AllowAnonymous]
    [UmaFilter("{0}/{1}", new[] { "type", "id" }, allowedScope: "delete")]
    public override Task<FhirResponse> Delete(string type, string id, CancellationToken cancellationToken)
    {
        return base.Delete(type, id, cancellationToken);
    }

    [AllowAnonymous]
    [UmaFilter("{0}/{1}/$everything", new[] { "type", "id" }, allowedScope: "read")]
    public override Task<FhirResponse> Everything(string type, string id, CancellationToken cancellationToken)
    {
        return base.Everything(type, id, cancellationToken);
    }

    /// <inheritdoc />
    [AllowAnonymous]
    [UmaFilter("{0}", new[] { "type" }, allowedScope: "search")]
    public override async Task<FhirResponse> Search(string type, CancellationToken cancellationToken)
    {
        var idToken = Request.Query["id_token"].FirstOrDefault();
        if (idToken == null)
        {
            return new FhirResponse(HttpStatusCode.Forbidden);
        }

        var response = await base.Search(type, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode != HttpStatusCode.OK)
        {
            return response;
        }

        var bundle = (response.Resource as Bundle)!;
        var ids = bundle.GetResources().Select(x => x.HasVersionId ? x.VersionId : x.Id).ToArray();
        var accessToken = await _tokenCache.GetAccessToken("uma_protection").ConfigureAwait(false);

        var resourceOptions = await _resourceSetClient.SearchResources(
                new SearchResourceSet { IdToken = idToken, Terms = ids },
                accessToken?.AccessToken,
                cancellationToken)
            .ConfigureAwait(false);
        if (resourceOptions is not Option<PagedResult<ResourceSetDescription>>.Result resources)
        {
            return new FhirResponse(HttpStatusCode.BadRequest, Key.Create(type));
        }

        var availableIds = new HashSet<string>(
            (await System.Threading.Tasks.Task.WhenAll(
                    resources.Item.Content.Select(d => _resourceMap.GetResourceId(d.Id)))
                .ConfigureAwait(false)).Where(s => s != null)
            .Select(s => s!));
        var entries = bundle.Entry.Where(x => availableIds.Contains(x.Resource.Id));
        var resultingBundle = new Bundle { Type = bundle.Type, Total = availableIds.Count };
        resultingBundle.Entry.AddRange(entries);
        return new FhirResponse(response.StatusCode, response.Key, resultingBundle);
    }
}

internal class OwnResourceFilter : ActionFilterAttribute
{
    public override System.Threading.Tasks.Task OnActionExecutionAsync(ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        if (!context.ActionArguments.TryGetValue("resource", out var resource))
        {
            context.Result = new BadRequestResult();
            return System.Threading.Tasks.Task.CompletedTask;
        }

        var subject = context.HttpContext.User.GetSubject();
        if (string.IsNullOrWhiteSpace(subject))
        {
            context.Result = new ForbidResult();
            return System.Threading.Tasks.Task.CompletedTask;
        }

        bool HasUserReference(ResourceReference reference)
        {
            return reference.Reference == $"{ModelInfo.ResourceTypeToFhirTypeName(ResourceType.Patient)}/{subject}";
        }

        var allowed = resource switch
        {
            Patient p => p.Contact.Any(
                x => x.Telecom.Any(t => t.System == ContactPoint.ContactPointSystem.Url && t.Value == subject)),
            NutritionIntake n => HasUserReference(n.Subject),
            Observation o => HasUserReference(o.Subject),
            Procedure p => HasUserReference(p.Subject),
            MedicationDispense m => m.Performer.Any(performer =>
                performer?.Actor?.Reference?.EndsWith($"/{subject}") == true),
            _ => false
        };
        if (!allowed)
        {
            context.Result = new ForbidResult();
        }

        return next();
    }
}
