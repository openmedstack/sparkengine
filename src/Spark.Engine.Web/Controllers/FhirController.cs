// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace Spark.Engine.Web.Controllers
{
    using System;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;
    using System.Web;
    using Core;
    using Engine.Extensions;
    using Extensions;
    using Hl7.Fhir.Model;
    using Hl7.Fhir.Rest;
    using Microsoft.AspNetCore.Cors;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Net.Http.Headers;
    using Service;
    using Utility;

    [Route("fhir")]
    [ApiController]
    [EnableCors]
    public class FhirController : ControllerBase
    {
        private readonly IAsyncFhirService _fhirService;

        public FhirController(IAsyncFhirService fhirService) =>
            _fhirService = fhirService ?? throw new ArgumentNullException(nameof(fhirService));

        [HttpGet("{type}/{id}")]
        public async Task<ActionResult<FhirResponse>> Read(string type, string id)
        {
            var ifModifiedSince = Request.Headers[HeaderNames.IfModifiedSince]
                .Aggregate(
                    default(DateTimeOffset?),
                    (d, s) => !d.HasValue && DateTimeOffset.TryParse(s, out var result) ? result : d);

            var parameters = new ConditionalHeaderParameters(Request.Headers[HeaderNames.IfNoneMatch], ifModifiedSince);
            var key = Key.Create(type, id);
            var result = await _fhirService.Read(key, parameters).ConfigureAwait(false);
            return new ActionResult<FhirResponse>(result);
        }

        [HttpGet("{type}/{id}/_history/{vid}")]
        public Task<FhirResponse> VRead(string type, string id, string vid)
        {
            var key = Key.Create(type, id, vid);
            return _fhirService.VersionRead(key);
        }

        [HttpPut("{type}/{id?}")]
        public async Task<ActionResult<FhirResponse>> Update(string type, Resource resource, string id = null)
        {
            var versionId = Request.GetTypedHeaders().IfMatch?.FirstOrDefault()?.Tag.Buffer;
            var key = Key.Create(type, id, versionId);
            if (key.HasResourceId())
            {
                Request.TransferResourceIdIfRawBinary(resource, id);

                var update = await _fhirService.Update(key, resource).ConfigureAwait(false);
                return new ActionResult<FhirResponse>(update);
            }

            var conditionalUpdate = await _fhirService.ConditionalUpdate(
                    key,
                    resource,
                    SearchParams.FromUriParamList(
                        Request.TupledParameters().Select(t => Tuple.Create(t.Item1, t.Item2))))
                .ConfigureAwait(false);
            return new ActionResult<FhirResponse>(conditionalUpdate);
        }

        [HttpPost("{type}")]
        public Task<FhirResponse> Create(string type, Resource resource)
        {
            var key = Key.Create(type, resource?.Id);

            if (Request.Headers.ContainsKey(FhirHttpHeaders.IfNoneExist))
            {
                var searchQueryString = HttpUtility.ParseQueryString(Request.GetTypedHeaders().IfNoneExist());
                var searchValues = searchQueryString.Keys.Cast<string>()
                    .Select(k => new Tuple<string, string>(k, searchQueryString[k]));

                return _fhirService.ConditionalCreate(key, resource, SearchParams.FromUriParamList(searchValues));
            }

            return _fhirService.Create(key, resource);
        }

        [HttpDelete("{type}/{id}")]
        public Task<FhirResponse> Delete(string type, string id)
        {
            var key = Key.Create(type, id);
            return _fhirService.Delete(key);
        }

        [HttpDelete("{type}")]
        public Task<FhirResponse> ConditionalDelete(string type)
        {
            var key = Key.Create(type);
            return _fhirService.ConditionalDelete(key, Request.TupledParameters());
        }

        [HttpGet("{type}/{id}/_history")]
        public Task<FhirResponse> History(string type, string id)
        {
            var key = Key.Create(type, id);
            var parameters = GetHistoryParameters(Request);
            return _fhirService.History(key, parameters);
        }

        // ============= Validate

        [HttpPost("{type}/{id}/$validate")]
        public Task<FhirResponse> Validate(string type, string id, Resource resource)
        {
            var key = Key.Create(type, id);
            return _fhirService.ValidateOperation(key, resource);
        }

        [HttpPost("{type}/$validate")]
        public Task<FhirResponse> Validate(string type, Resource resource)
        {
            var key = Key.Create(type);
            return _fhirService.ValidateOperation(key, resource);
        }

        // ============= Type Level Interactions

        [HttpGet("{type}")]
        public Task<FhirResponse> Search(string type)
        {
            var start = Request.GetParameter(FhirParameter.SNAPSHOT_INDEX).ParseIntParameter() ?? 0;
            var searchparams = Request.GetSearchParams();
            //int pagesize = Request.GetIntParameter(FhirParameter.COUNT) ?? Const.DEFAULT_PAGE_SIZE;
            //string sortby = Request.GetParameter(FhirParameter.SORT);

            return _fhirService.Search(type, searchparams, start);
        }

        [HttpPost("{type}/_search")]
        public Task<FhirResponse> SearchWithOperator(string type)
        {
            // TODO: start index should be retrieved from the body.
            var start = Request.GetParameter(FhirParameter.SNAPSHOT_INDEX).ParseIntParameter() ?? 0;
            var searchparams = Request.GetSearchParamsFromBody();

            return _fhirService.Search(type, searchparams, start);
        }

        [HttpGet("{type}/_history")]
        public Task<FhirResponse> History(string type)
        {
            var parameters = GetHistoryParameters(Request);
            return _fhirService.History(type, parameters);
        }

        // ============= Whole System Interactions

        [HttpGet]
        [Route("metadata")]
        public Task<FhirResponse> Metadata() => _fhirService.CapabilityStatement(SparkSettings.Version);

        [HttpOptions]
        [Route("")]
        public Task<FhirResponse> Options() => _fhirService.CapabilityStatement(SparkSettings.Version);

        [HttpPost]
        [Route("")]
        public Task<FhirResponse> Transaction(Bundle bundle) => _fhirService.Transaction(bundle);

        //[HttpPost, Route("Mailbox")]
        //public FhirResponse Mailbox(Bundle document)
        //{
        //    Binary b = Request.GetBody();
        //    return service.Mailbox(document, b);
        //}

        [HttpGet]
        [Route("_history")]
        public Task<FhirResponse> History()
        {
            var parameters = GetHistoryParameters(Request);
            return _fhirService.History(parameters);
        }

        [HttpGet]
        [Route("_snapshot")]
        public Task<FhirResponse> Snapshot()
        {
            var snapshot = Request.GetParameter(FhirParameter.SNAPSHOT_ID);
            var start = Request.GetParameter(FhirParameter.SNAPSHOT_INDEX).ParseIntParameter() ?? 0;
            return _fhirService.GetPage(snapshot, start);
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
        public async Task<FhirResponse> InstanceOperation(
            string type,
            string id,
            string operation,
            Parameters parameters)
        {
            var key = Key.Create(type, id);
            return operation.ToLower() switch
            {
                "meta" => await _fhirService.ReadMeta(key).ConfigureAwait(false),
                "meta-add" => await _fhirService.AddMeta(key, parameters).ConfigureAwait(false),
                "meta-delete" => Respond.WithError(HttpStatusCode.NotFound, "Unknown operation"),
                _ => Respond.WithError(HttpStatusCode.NotFound, "Unknown operation")
            };
        }

        [HttpPost]
        [HttpGet]
        [Route("{type}/{id}/$everything")]
        public Task<FhirResponse> Everything(string type, string id)
        {
            var key = Key.Create(type, id);
            return _fhirService.Everything(key);
        }

        [HttpPost]
        [HttpGet]
        [Route("{type}/$everything")]
        public Task<FhirResponse> Everything(string type)
        {
            var key = Key.Create(type);
            return _fhirService.Everything(key);
        }

        [HttpPost]
        [HttpGet]
        [Route("Composition/{id}/$document")]
        public Task<FhirResponse> Document(string id)
        {
            var key = Key.Create("Composition", id);
            return _fhirService.Document(key);
        }

        private HistoryParameters GetHistoryParameters(HttpRequest request)
        {
            var count = request.GetParameter(FhirParameter.COUNT).ParseIntParameter();
            var since = request.GetParameter(FhirParameter.SINCE).ParseDateParameter();
            var sortBy = Request.GetParameter(FhirParameter.SORT);
            return new HistoryParameters(count, since, sortBy);
        }
    }
}