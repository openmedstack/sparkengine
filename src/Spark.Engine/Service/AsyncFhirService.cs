using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Spark.Engine.Core;
using Spark.Engine.FhirResponseFactory;
using Spark.Engine.Service.FhirServiceExtensions;
using Spark.Service;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Spark.Engine.Extensions;
using Task = System.Threading.Tasks.Task;

namespace Spark.Engine.Service
{
    using Store.Interfaces;

    public class AsyncFhirService : IAsyncFhirService, IInteractionHandler
    {
        private readonly IResourceStorageService _storageService;
        private readonly IPagingService _pagingService;
        private readonly ISearchService _searchService;
        private readonly ITransactionService _transactionService;
        private readonly ICapabilityStatementService _capabilityStatementService;
        private readonly IHistoryStore _historyService;
        private readonly IFhirResponseFactory _responseFactory;
        private readonly IPatchService _patchService;
        private readonly ICompositeServiceListener _serviceListener;

        public AsyncFhirService(
            IResourceStorageService storageService,
            IPagingService pagingService,
            ISearchService searchService,
            ITransactionService transactionService,
            ICapabilityStatementService capabilityStatementService,
            IHistoryStore historyService,
            IFhirResponseFactory responseFactory,
            IPatchService patchService,
            ICompositeServiceListener serviceListener = null)
        {
            _storageService = storageService;
            _pagingService = pagingService;
            _searchService = searchService;
            _transactionService = transactionService;
            _capabilityStatementService = capabilityStatementService;
            _historyService = historyService;
            _responseFactory = responseFactory;
            _patchService = patchService;
            _serviceListener = serviceListener;
        }

        public async Task<FhirResponse> AddMeta(IKey key, Parameters parameters)
        {
            var entry = await _storageService.Get(key).ConfigureAwait(false);
            if (entry != null && !entry.IsDeleted())
            {
                entry.Resource.AffixTags(parameters);
                await _storageService.Add(entry).ConfigureAwait(false);
            }

            return _responseFactory.GetMetadataResponse(entry, key);
        }

        public Task<FhirResponse> ConditionalCreate(
            IKey key,
            Resource resource,
            IEnumerable<Tuple<string, string>> parameters)
        {
            return ConditionalCreate(key, resource, SearchParams.FromUriParamList(parameters));
        }

        public async Task<FhirResponse> ConditionalCreate(IKey key, Resource resource, SearchParams parameters)
        {
            var operation = await ResourceManipulationOperationFactory
                .CreatePost(resource, key, _searchService, parameters)
                .ConfigureAwait(false);
            return await _transactionService.HandleTransaction(operation, this).ConfigureAwait(false);
        }

        public async Task<FhirResponse> ConditionalDelete(IKey key, IEnumerable<Tuple<string, string>> parameters)
        {
            var operation = await ResourceManipulationOperationFactory
                .CreateDelete(key, _searchService, SearchParams.FromUriParamList(parameters))
                .ConfigureAwait(false);
            return await _transactionService.HandleTransaction(operation, this).ConfigureAwait(false)
                   ?? Respond.WithCode(HttpStatusCode.NotFound);
        }

        public async Task<FhirResponse> ConditionalUpdate(IKey key, Resource resource, SearchParams parameters)
        {
            // FIXME: if update receives a key with no version how do we handle concurrency?

            var operation = await resource.CreatePut(key, _searchService, parameters).ConfigureAwait(false);
            return await _transactionService.HandleTransaction(operation, this).ConfigureAwait(false);
        }

        public Task<FhirResponse> CapabilityStatement(string sparkVersion)
        {
            var response = Respond.WithResource(_capabilityStatementService.GetSparkCapabilityStatement(sparkVersion));
            return Task.FromResult(response);
        }

        public async Task<FhirResponse> Create(IKey key, Resource resource)
        {
            Validate.Key(key);
            Validate.HasTypeName(key);
            Validate.ResourceType(key, resource);

            key = key.CleanupForCreate();
            var result = await Store(Entry.Post(key, resource)).ConfigureAwait(false);
            return Respond.WithResource(HttpStatusCode.Created, result);
        }

        public async Task<FhirResponse> Delete(IKey key)
        {
            Validate.Key(key);
            Validate.HasNoVersion(key);

            var current = await _storageService.Get(key).ConfigureAwait(false);
            return current is { IsPresent: true }
                ? await Delete(Entry.Delete(key, DateTimeOffset.UtcNow)).ConfigureAwait(false)
                : Respond.WithCode(HttpStatusCode.NotFound);
        }

        public async Task<FhirResponse> Delete(Entry entry)
        {
            Validate.Key(entry.Key);
            await Store(entry).ConfigureAwait(false);
            return Respond.WithCode(HttpStatusCode.NoContent);
        }

        public async Task<FhirResponse> GetPage(string snapshotKey, int index)
        {
            var snapshot = await _pagingService.StartPagination(snapshotKey).ConfigureAwait(false);
            var page = await snapshot.GetPage(index).ConfigureAwait(false);
            return _responseFactory.GetFhirResponse(page);
        }

        public async Task<FhirResponse> History(HistoryParameters parameters)
        {
            var snapshot = await _historyService.History(parameters).ConfigureAwait(false);
            return await CreateSnapshotResponse(snapshot).ConfigureAwait(false);
        }

        public async Task<FhirResponse> History(string type, HistoryParameters parameters)
        {
            var snapshot = await _historyService.History(type, parameters).ConfigureAwait(false);
            return await CreateSnapshotResponse(snapshot).ConfigureAwait(false);
        }

        public async Task<FhirResponse> History(IKey key, HistoryParameters parameters)
        {
            if (await _storageService.Get(key).ConfigureAwait(false) == null)
            {
                return Respond.NotFound(key);
            }

            var snapshot = await _historyService.History(key, parameters).ConfigureAwait(false);
            return await CreateSnapshotResponse(snapshot).ConfigureAwait(false);
        }

        public Task<FhirResponse> Mailbox(Bundle bundle, Binary body) => throw new NotImplementedException();

        public Task<FhirResponse> Put(IKey key, Resource resource)
        {
            Validate.HasResourceId(resource);
            Validate.IsResourceIdEqual(key, resource);
            return Put(Entry.Put(key, resource));
        }

        public async Task<FhirResponse> Put(Entry entry)
        {
            Validate.Key(entry.Key);
            Validate.ResourceType(entry.Key, entry.Resource);
            Validate.HasTypeName(entry.Key);
            Validate.HasResourceId(entry.Key);

            //return Transaction(entry);
            var result = await Store(Entry.Put(entry.Key, entry.Resource)).ConfigureAwait(false);
            return Respond.WithResource(HttpStatusCode.Created, result);
        }

        public async Task<FhirResponse> Read(IKey key, ConditionalHeaderParameters parameters = null)
        {
            Validate.ValidateKey(key);
            var entry = await _storageService.Get(key).ConfigureAwait(false);
            return _responseFactory.GetFhirResponse(entry, key, parameters);
        }

        public async Task<FhirResponse> ReadMeta(IKey key)
        {
            Validate.ValidateKey(key);
            var entry = await _storageService.Get(key).ConfigureAwait(false);
            return _responseFactory.GetMetadataResponse(entry, key);
        }

        public async Task<FhirResponse> Search(string type, SearchParams searchCommand, int pageIndex = 0)
        {
            var snapshot = await _searchService.GetSnapshot(type, searchCommand).ConfigureAwait(false);
            return await CreateSnapshotResponse(snapshot, pageIndex).ConfigureAwait(false);
        }

        public async Task<FhirResponse> Transaction(IList<Entry> interactions)
        {
            var responses = _transactionService.HandleTransaction(interactions, this);
            return await _responseFactory.GetFhirResponse(responses, Bundle.BundleType.TransactionResponse);
        }

        public async Task<FhirResponse> Transaction(Bundle bundle)
        {
            var responses = _transactionService.HandleTransaction(bundle, this);
            return await _responseFactory.GetFhirResponse(responses, Bundle.BundleType.TransactionResponse);
        }

        public async Task<FhirResponse> Update(IKey key, Resource resource)
        {
            return key.HasVersionId()
                ? await VersionSpecificUpdate(key, resource).ConfigureAwait(false)
                : await Put(key, resource).ConfigureAwait(false);
        }

        public async Task<FhirResponse> Patch(IKey key, Parameters parameters)
        {
            if (parameters == null)
            {
                return new FhirResponse(HttpStatusCode.BadRequest);
            }

            var current = await _storageService.Get(key.WithoutVersion()).ConfigureAwait(false);
            if (current is { IsPresent: true })
            {
                try
                {
                    var resource = _patchService.Apply(current.Resource, parameters);
                    return await Patch(Entry.PATCH(current.Key.WithoutVersion(), resource)).ConfigureAwait(false);
                }
                catch
                {
                    return new FhirResponse(HttpStatusCode.BadRequest);
                }
            }

            return Respond.WithCode(HttpStatusCode.NotFound);
        }

        public async Task<FhirResponse> Patch(Entry entry)
        {
            Validate.Key(entry.Key);
            Validate.ResourceType(entry.Key, entry.Resource);
            Validate.HasTypeName(entry.Key);
            Validate.HasResourceId(entry.Key);

            var result = await Store(entry).ConfigureAwait(false);

            return Respond.WithResource(HttpStatusCode.OK, result);
        }

        public Task<FhirResponse> ValidateOperation(IKey key, Resource resource)
        {
            if (resource == null)
            {
                throw Error.BadRequest("Validate needs a Resource in the body payload");
            }

            Validate.ResourceType(key, resource);

            var outcome = Validate.AgainstSchema(resource);
            return Task.FromResult(
                outcome == null ? Respond.WithCode(HttpStatusCode.OK) : Respond.WithResource(422, outcome));
        }

        public async Task<FhirResponse> VersionRead(IKey key)
        {
            Validate.ValidateKey(key, true);
            var entry = await _storageService.Get(key).ConfigureAwait(false);
            return _responseFactory.GetFhirResponse(entry, key);
        }

        public async Task<FhirResponse> VersionSpecificUpdate(IKey versionedKey, Resource resource)
        {
            Validate.HasTypeName(versionedKey);
            Validate.HasVersion(versionedKey);
            var key = versionedKey.WithoutVersion();
            var current = await _storageService.Get(key).ConfigureAwait(false);
            Validate.IsSameVersion(current.Key, versionedKey);
            return await Put(key, resource).ConfigureAwait(false);
        }

        public async Task<FhirResponse> Everything(IKey key)
        {
            var snapshot = await _searchService.GetSnapshotForEverything(key).ConfigureAwait(false);
            return await CreateSnapshotResponse(snapshot).ConfigureAwait(false);
        }

        public async Task<FhirResponse> Document(IKey key)
        {
            Validate.HasResourceType(key, ResourceType.Composition);

            var searchCommand = new SearchParams();
            searchCommand.Add("_id", key.ResourceId);
            var includes = new List<string>
            {
                "Composition:subject",
                "Composition:author",
                "Composition:attester" //Composition.attester.party
                ,
                "Composition:custodian",
                "Composition:eventdetail" //Composition.event.detail
                ,
                "Composition:encounter",
                "Composition:entry" //Composition.section.entry
            };
            foreach (var inc in includes)
            {
                searchCommand.Include.Add((inc, IncludeModifier.None));
            }

            return await Search(key.TypeName, searchCommand).ConfigureAwait(false);
        }

        public async Task<FhirResponse> HandleInteraction(Entry interaction)
        {
            switch (interaction.Method)
            {
                case Bundle.HTTPVerb.PUT:
                    return await Put(interaction).ConfigureAwait(false);
                case Bundle.HTTPVerb.POST:
                    return await Create(interaction).ConfigureAwait(false);
                case Bundle.HTTPVerb.DELETE:
                {
                    var current = await _storageService.Get(interaction.Key.WithoutVersion()).ConfigureAwait(false);
                    return current is { IsPresent: true }
                        ? await Delete(interaction).ConfigureAwait(false)
                        : Respond.WithCode(HttpStatusCode.NotFound);
                }
                case Bundle.HTTPVerb.GET:
                    if (interaction.Key.HasVersionId())
                    {
                        return await VersionRead((Key)interaction.Key).ConfigureAwait(false);
                    }
                    else
                    {
                        return await Read(interaction.Key).ConfigureAwait(false);
                    }
                case Bundle.HTTPVerb.PATCH:
                    return await Patch(interaction.Key, interaction.Resource as Parameters).ConfigureAwait(false);
                default:
                    return Respond.Success;
            }
        }

        private async Task<FhirResponse> Create(Entry entry)
        {
            Validate.Key(entry.Key);
            Validate.HasTypeName(entry.Key);
            Validate.ResourceType(entry.Key, entry.Resource);

            if (entry.State != EntryState.Internal)
            {
                Validate.HasNoResourceId(entry.Key);
                Validate.HasNoVersion(entry.Key);
            }

            var result = await _storageService.Add(entry).ConfigureAwait(false);
            await _serviceListener.Inform(entry).ConfigureAwait(false);
            return Respond.WithResource(HttpStatusCode.Created, result);
        }

        private static void ValidateKey(IKey key, bool withVersion = false)
        {
            Validate.HasTypeName(key);
            Validate.HasResourceId(key);
            if (withVersion)
            {
                Validate.HasVersion(key);
            }
            else
            {
                Validate.HasNoVersion(key);
            }

            Validate.Key(key);
        }

        private async Task<Entry> Store(Entry entry)
        {
            Entry result = await _storageService.Add(entry).ConfigureAwait(false);
            await _serviceListener.Inform(entry).ConfigureAwait(false);
            return result;
        }
        private async Task<FhirResponse> CreateSnapshotResponse(Snapshot snapshot, int pageIndex = 0)
        {
            var pagination = await _pagingService.StartPagination(snapshot).ConfigureAwait(false);
            var bundle = await pagination.GetPage(pageIndex).ConfigureAwait(false);
            return _responseFactory.GetFhirResponse(bundle);
        }
    }
}
