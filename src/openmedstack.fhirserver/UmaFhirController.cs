﻿namespace OpenMedStack.FhirServer;

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
using Hl7.Fhir.Model;
using Microsoft.AspNetCore.Mvc;
using OpenMedStack.SparkEngine.Core;
using OpenMedStack.SparkEngine.Interfaces;
using OpenMedStack.SparkEngine.Web.Controllers;
using Task = System.Threading.Tasks.Task;

[Route("uma")]
public class UmaFhirController : FhirController
{
    private readonly IAccessTokenCache _tokenCache;
    private readonly IUmaResourceSetClient _resourceSetClient;
    private readonly IResourceMap _resourceMap;

    public UmaFhirController(
        IFhirService fhirService,
        IAccessTokenCache tokenCache,
        IUmaResourceSetClient resourceSetClient,
        IResourceMap resourceMap)
        : base(fhirService)
    {
        _tokenCache = tokenCache;
        _resourceSetClient = resourceSetClient;
        _resourceMap = resourceMap;
    }

    /// <inheritdoc />
    [UmaFilter("{0}/{1}", new[] { "type", "id" }, resourceSetAccessScope: "read")]
    public override Task<ActionResult<FhirResponse>> Read(string type, string id, CancellationToken cancellationToken)
    {
        return base.Read(type, id, cancellationToken);
    }

    /// <inheritdoc />
    [UmaFilter("{0}/{1}/_history/{2}", new[] { "type", "id", "vid" }, resourceSetAccessScope: "read")]
    public override Task<FhirResponse> VRead(string type, string id, string vid, CancellationToken cancellationToken)
    {
        return base.VRead(type, id, vid, cancellationToken);
    }

    // [UmaFilter("{0}", new[] { "type" }, allowedScope: "create")]
    [OwnResourceFilter]
    public override Task<FhirResponse?> Create(string type, Resource resource, CancellationToken cancellationToken)
    {
        return base.Create(type, resource, cancellationToken);
    }

    [UmaFilter("{0}/{1}", new[] { "type", "id" }, resourceSetAccessScope: "delete")]
    public override Task<FhirResponse> Delete(string type, string id, CancellationToken cancellationToken)
    {
        return base.Delete(type, id, cancellationToken);
    }

    [UmaFilter("{0}/{1}/$everything", new[] { "type", "id" }, resourceSetAccessScope: "read")]
    public override Task<FhirResponse> Everything(string type, string id, CancellationToken cancellationToken)
    {
        return base.Everything(type, id, cancellationToken);
    }

    /// <inheritdoc />
    [UmaFilter("{0}", new[] { "type" }, resourceSetAccessScope: "search")]
    public override async Task<FhirResponse> Search(string type, CancellationToken cancellationToken)
    {
        var idToken = Request.Headers["X-ID-TOKEN"].FirstOrDefault();
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
            (await Task.WhenAll(
                    resources.Item.Content.Select(d => _resourceMap.GetResourceId(d.Id, cancellationToken)))
                .ConfigureAwait(false)).Where(s => s != null)
            .Select(s => s!));
        var entries = bundle.Entry.Where(x => availableIds.Contains(x.Resource.Id));
        var resultingBundle = new Bundle { Type = bundle.Type, Total = availableIds.Count };
        resultingBundle.Entry.AddRange(entries);
        return new FhirResponse(response.StatusCode, response.Key, resultingBundle);
    }
}
