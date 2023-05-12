// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

using Microsoft.AspNetCore.Authorization;

namespace OpenMedStack.SparkEngine.Web.Controllers;

using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Core;
using Extensions;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Interfaces;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using SparkEngine.Extensions;
using Utility;

[Authorize]
[EnableCors]
public abstract class FhirController : ControllerBase
{
    protected IFhirService FhirService { get; }

    protected FhirController(IFhirService fhirService) =>
        FhirService = fhirService ?? throw new ArgumentNullException(nameof(fhirService));

    /// <summary>
    /// The read interaction accesses the current contents of a resource.
    /// </summary>
    /// <param name="type">The <see cref="Resource"/> type.</param>
    /// <param name="id">The <see cref="Resource"/> id.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> for the async operation.</param>
    /// <para>Reading a <see cref="Resource"/> requires the `read` scope access.</para>
    /// <returns>The requested <see cref="Resource"/> as a <see cref="FhirResponse"/>.</returns>
    [HttpGet("{type}/{id}")]
    public virtual async Task<ActionResult<FhirResponse>> Read(
        string type,
        string id,
        CancellationToken cancellationToken)
    {
        var ifModifiedSince = Request.Headers[HeaderNames.IfModifiedSince]
            .Aggregate(
                default(DateTimeOffset?),
                (d, s) => !d.HasValue && DateTimeOffset.TryParse(s, out var result) ? result : d);

        var parameters = new ConditionalHeaderParameters(Request.Headers[HeaderNames.IfNoneMatch], ifModifiedSince);
        var key = Key.Create(type, id);
        var result = await FhirService.Read(key, parameters, cancellationToken).ConfigureAwait(false);
        return new ActionResult<FhirResponse>(result);
    }

    [HttpGet("{type}/{id}/_history/{vid}")]
    public virtual Task<FhirResponse> VRead(string type, string id, string vid, CancellationToken cancellationToken)
    {
        var key = Key.Create(type, id, vid);
        return FhirService.VersionRead(key, cancellationToken);
    }

    [HttpPut("{type}/{id?}")]
    public virtual async Task<FhirResponse?> Update(
        string type,
        Resource resource,
        string? id = null,
        CancellationToken cancellationToken = default)
    {
        var versionId = Request.GetTypedHeaders().IfMatch.FirstOrDefault()?.Tag.Buffer;
        var key = Key.Create(type, id, versionId);
        if (key.HasResourceId())
        {
            Request.TransferResourceIdIfRawBinary(resource, id);

            var update = await FhirService.Update(key, resource, cancellationToken).ConfigureAwait(false);
            return update;
        }

        var conditionalUpdate = await FhirService.ConditionalUpdate(
                key,
                resource,
                SearchParams.FromUriParamList(
                    Request.TupledParameters().Select(t => Tuple.Create(t.Item1, t.Item2))),
                cancellationToken)
            .ConfigureAwait(false);
        return conditionalUpdate;
    }

    [HttpPost("{type}")]
    public virtual async Task<FhirResponse?> Create(
        string type,
        [FromBody] Resource resource,
        CancellationToken cancellationToken)
    {
        resource.Id = Guid.NewGuid().ToString("N");
        var key = Key.Create(type, resource.Id);

        if (Request.Headers.ContainsKey(FhirHttpHeaders.IfNoneExist))
        {
            var searchQueryString = HttpUtility.ParseQueryString(Request.GetTypedHeaders().IfNoneExist() ?? "");
            var searchValues = searchQueryString.Keys.Cast<string>()
                .Select(k => new Tuple<string, string?>(k, searchQueryString[k]));

            return await FhirService
                .ConditionalCreate(key, resource, SearchParams.FromUriParamList(searchValues), cancellationToken)
                .ConfigureAwait(false);
        }

        var response = await FhirService.Create(key, resource, cancellationToken).ConfigureAwait(false);
        return response;
    }

    [HttpDelete("{type}/{id}")]
    public virtual Task<FhirResponse> Delete(string type, string id, CancellationToken cancellationToken)
    {
        var key = Key.Create(type, id);
        return FhirService.Delete(key, cancellationToken);
    }

    [HttpDelete("{type}")]
    public virtual Task<FhirResponse> ConditionalDelete(string type, CancellationToken cancellationToken)
    {
        var key = Key.Create(type);
        return FhirService.ConditionalDelete(key, Request.TupledParameters(), cancellationToken);
    }

    [HttpGet("{type}/{id}/_history")]
    public virtual Task<FhirResponse> History(string type, string id, CancellationToken cancellationToken)
    {
        var key = Key.Create(type, id);
        var parameters = GetHistoryParameters(Request);
        return FhirService.History(key, parameters, cancellationToken);
    }

    // ============= Validate

    [HttpPost("{type}/{id}/$validate")]
    public virtual Task<FhirResponse> Validate(
        string type,
        string id,
        Resource resource,
        CancellationToken cancellationToken)
    {
        var key = Key.Create(type, id);
        return FhirService.ValidateOperation(key, resource, cancellationToken);
    }

    [HttpPost("{type}/$validate")]
    public virtual Task<FhirResponse> Validate(string type, Resource resource, CancellationToken cancellationToken)
    {
        var key = Key.Create(type);
        return FhirService.ValidateOperation(key, resource, cancellationToken);
    }

    // ============= Type Level Interactions

    [HttpGet("{type}")]
    public virtual Task<FhirResponse> Search(string type, CancellationToken cancellationToken)
    {
        var start = Request.GetParameter(FhirParameter.SNAPSHOT_INDEX)?.ParseIntParameter() ?? 0;
        var searchparams = Request.GetSearchParams();
        var pagesize = Request.GetParameter(FhirParameter.COUNT)?.ParseIntParameter() ?? 100; //Const.DEFAULT_PAGE_SIZE;
        var sortby = Request.GetParameter(FhirParameter.SORT);
        searchparams = searchparams.LimitTo(pagesize).OrderBy(sortby ?? "");
        return FhirService.Search(type, searchparams, start, cancellationToken);
    }

    [HttpPost("{type}/_search")]
    public virtual Task<FhirResponse> SearchWithOperator(
        string type,
        [FromForm(Name = FhirParameter.SNAPSHOT_INDEX)] int? start,
        CancellationToken cancellationToken)
    {
        // TODO: start index should be retrieved from the body.
        //var startIndex = Request.GetParameter(FhirParameter.SNAPSHOT_INDEX)?.ParseIntParameter() ?? 0;
        var searchParams = Request.GetSearchParamsFromBody();

        return FhirService.Search(type, searchParams, start ?? 0, cancellationToken);
    }

    [HttpGet("{type}/_history")]
    public virtual Task<FhirResponse> History(string type, CancellationToken cancellationToken)
    {
        var parameters = GetHistoryParameters(Request);
        return FhirService.History(type, parameters, cancellationToken);
    }

    // ============= Whole System Interactions

    [HttpGet]
    [Route("metadata")]
    public virtual Task<FhirResponse> Metadata() => FhirService.CapabilityStatement(SparkSettings.Version);

    [HttpOptions]
    [Route("")]
    public virtual Task<FhirResponse> Options() => FhirService.CapabilityStatement(SparkSettings.Version);

    [HttpPost]
    [Route("")]
    public virtual Task<FhirResponse> Transaction(Bundle bundle, CancellationToken cancellationToken) =>
        FhirService.Transaction(bundle, cancellationToken);

    //[HttpPost, Route("Mailbox")]
    //public FhirResponse Mailbox(Bundle document)
    //{
    //    Binary b = Request.GetBody();
    //    return service.Mailbox(document, b);
    //}

    [HttpGet]
    [Route("_history")]
    public virtual Task<FhirResponse> History(CancellationToken cancellationToken)
    {
        var parameters = GetHistoryParameters(Request);
        return FhirService.History(parameters, cancellationToken);
    }

    [HttpGet]
    [Route("_snapshot")]
    public virtual Task<FhirResponse> Snapshot(CancellationToken cancellationToken)
    {
        var snapshot = Request.GetParameter(FhirParameter.SNAPSHOT_ID)
         ?? throw new ArgumentException("Missing snapshot id");
        var start = Request.GetParameter(FhirParameter.SNAPSHOT_INDEX)?.ParseIntParameter() ?? 0;
        return FhirService.GetPage(snapshot, start, cancellationToken);
    }

    // Operations

    [HttpPost]
    [Route("${operation}")]
#pragma warning disable CA1822 // Mark members as static
    public FhirResponse ServerOperation(string operation)
#pragma warning restore CA1822 // Mark members as static
    {
        return operation.ToLower() switch
        {
            "error" => throw new Exception("This error is for testing purposes"),
            _ => Respond.WithError(HttpStatusCode.NotFound, "Unknown operation")
        };
    }

    [HttpPost]
    [Route("{type}/{id}/${operation}")]
    public virtual async Task<FhirResponse> InstanceOperation(
        string type,
        string id,
        string operation,
        Parameters parameters,
        CancellationToken cancellationToken)
    {
        var key = Key.Create(type, id);
        return operation.ToLower() switch
        {
            "meta" => await FhirService.ReadMeta(key, cancellationToken).ConfigureAwait(false),
            "meta-add" => await FhirService.AddMeta(key, parameters, cancellationToken).ConfigureAwait(false),
            "meta-delete" => Respond.WithError(HttpStatusCode.NotFound, "Unknown operation"),
            _ => Respond.WithError(HttpStatusCode.NotFound, "Unknown operation")
        };
    }

    [HttpPost]
    [HttpGet]
    [Route("{type}/{id}/$everything")]
    public virtual Task<FhirResponse> Everything(string type, string id, CancellationToken cancellationToken)
    {
        var key = Key.Create(type, id);
        return FhirService.Everything(key, cancellationToken);
    }

    [HttpPost]
    [HttpGet]
    [Route("{type}/$everything")]
    public virtual Task<FhirResponse> Everything(string type, CancellationToken cancellationToken)
    {
        var key = Key.Create(type);
        return FhirService.Everything(key, cancellationToken);
    }

    [HttpPost]
    [HttpGet]
    [Route("Composition/{id}/$document")]
    public virtual Task<FhirResponse> Document(string id, CancellationToken cancellationToken)
    {
        var key = Key.Create("Composition", id);
        return FhirService.Document(key, cancellationToken);
    }

    private HistoryParameters GetHistoryParameters(HttpRequest request)
    {
        var count = request.GetParameter(FhirParameter.COUNT)?.ParseIntParameter();
        var since = request.GetParameter(FhirParameter.SINCE)?.ParseDateParameter();
        var sortBy = Request.GetParameter(FhirParameter.SORT);
        return new HistoryParameters(count, since, sortBy);
    }
}
