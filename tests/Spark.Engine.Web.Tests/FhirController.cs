namespace Spark.Engine.Web.Tests
{
    using System;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;
    using System.Web;
    using Core;
    using Engine.Extensions;
    using Engine.Service;
    using Engine.Utility;
    using Extensions;
    using Hl7.Fhir.Model;
    using Hl7.Fhir.Rest;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;

    //[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route("fhir")]
    [ApiController]
    public class FhirController : ControllerBase
    {
        private readonly IAsyncFhirService _fhirService;

        public FhirController(IAsyncFhirService fhirService)
        {
            _fhirService = fhirService ?? throw new ArgumentNullException(nameof(fhirService));
        }

        [HttpGet("{type}/{id}")]
        public async Task<ActionResult<FhirResponse>> Read(string type, string id)
        {
            var parameters = Request.ToConditionalHeaderParameters();
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

                var result = await _fhirService.Update(key, resource).ConfigureAwait(false);
                return new ActionResult<FhirResponse>(result);
            }

            var value = await _fhirService.ConditionalUpdate(
                    key,
                    resource,
                    SearchParams.FromUriParamList(Request.TupledParameters()))
                .ConfigureAwait(false);
            return new ActionResult<FhirResponse>(value);
        }

        [HttpPost("{type}")]
        public async Task<FhirResponse> Create(string type, Resource resource)
        {
            var key = Key.Create(type, resource?.Id);

            if (Request.Headers.ContainsKey(FhirHttpHeaders.IfNoneExist))
            {
                var searchQueryString = HttpUtility.ParseQueryString(Request.GetTypedHeaders().IfNoneExist());
                var searchValues = searchQueryString.Keys.Cast<string>()
                    .Select(k => new Tuple<string, string>(k, searchQueryString[k]));

                return await _fhirService.ConditionalCreate(key, resource, SearchParams.FromUriParamList(searchValues))
                    .ConfigureAwait(false);
            }

            return await _fhirService.Create(key, resource).ConfigureAwait(false);
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
            var parameters = Request.ToHistoryParameters();
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
            var searchParams = Request.GetSearchParams();

            return _fhirService.Search(type, searchParams, start);
        }

        [HttpPost("{type}/_search")]
        public Task<FhirResponse> SearchWithOperator(string type)
        {
            // todo: get tupled parameters from post.
            return Search(type);
        }

        [HttpGet("{type}/_history")]
        public Task<FhirResponse> History(string type)
        {
            var parameters = Request.ToHistoryParameters();
            return _fhirService.History(type, parameters);
        }

        // ============= Whole System Interactions

        //[HttpGet, Route("metadata")]
        //public FhirResponse Metadata()
        //{
        //    return _fhirService.Conformance(_settings.Version);
        //}

        //[HttpOptions, Route("")]
        //public FhirResponse Options()
        //{
        //    return _fhirService.Conformance(_settings.Version);
        //}

        [HttpPost]
        [Route("")]
        public Task<FhirResponse> Transaction(Bundle bundle)
        {
            return _fhirService.Transaction(bundle);
        }

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
            var parameters = Request.ToHistoryParameters();
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

        //[HttpPost]
        //[Route("${operation}")]
        //public FhirResponse ServerOperation(string operation)
        //{
        //    return operation.ToLower() switch
        //    {
        //        "error" => throw new Exception("This error is for testing purposes"),
        //        _ => Respond.WithError(HttpStatusCode.NotFound, "Unknown operation")
        //    };
        //}

        [HttpPost]
        [Route("{type}/{id}/${operation}")]
        public Task<FhirResponse> InstanceOperation(string type, string id, string operation, Parameters parameters)
        {
            var key = Key.Create(type, id);
            return operation.ToLower() switch
            {
                "meta" => _fhirService.ReadMeta(key),
                "meta-add" => _fhirService.AddMeta(key, parameters),
                _ => System.Threading.Tasks.Task.FromResult(
                    Respond.WithError(HttpStatusCode.NotFound, "Unknown operation"))
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
    }
}
