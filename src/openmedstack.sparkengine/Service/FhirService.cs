namespace OpenMedStack.SparkEngine.Service;

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Core;
using Extensions;
using FhirResponseFactory;
using FhirServiceExtensions;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Interfaces;
using Task = System.Threading.Tasks.Task;

public class FhirService : IFhirService, IInteractionHandler
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

    public FhirService(
        IResourceStorageService storageService,
        IPagingService pagingService,
        ISearchService searchService,
        ITransactionService transactionService,
        ICapabilityStatementService capabilityStatementService,
        IHistoryStore historyService,
        IFhirResponseFactory responseFactory,
        IPatchService patchService,
        ILocalhost localhost,
        IServiceListener[] listeners)
    {
        _storageService = storageService;
        _pagingService = pagingService;
        _searchService = searchService;
        _transactionService = transactionService;
        _capabilityStatementService = capabilityStatementService;
        _historyService = historyService;
        _responseFactory = responseFactory;
        _patchService = patchService;
        _serviceListener = new ServiceListener(localhost, listeners);
    }

    public async Task<FhirResponse> AddMeta(IKey key, Parameters parameters, CancellationToken cancellationToken)
    {
        var entry = await _storageService.Get(key, cancellationToken).ConfigureAwait(false);
        if (entry == null || entry.IsDeleted || !entry.HasResource)
        {
            return await _responseFactory.GetMetadataResponse(entry, key).ConfigureAwait(false);
        }

        var resource = await _storageService.Load(key, cancellationToken).ConfigureAwait(false);
        resource!.AffixTags(parameters);
        await Store(Entry.Post(key, resource!), cancellationToken).ConfigureAwait(false);

        return await _responseFactory.GetMetadataResponse(entry, key).ConfigureAwait(false);
    }

    public Task<FhirResponse?> ConditionalCreate(
        IKey key,
        Resource resource,
        IEnumerable<Tuple<string, string>> parameters,
        CancellationToken cancellationToken)
    {
        return ConditionalCreate(key, resource, SearchParams.FromUriParamList(parameters), cancellationToken);
    }

    public async Task<FhirResponse?> ConditionalCreate(
        IKey key,
        Resource resource,
        SearchParams parameters,
        CancellationToken cancellationToken)
    {
        var operation = await resource
            .CreatePost(key, _searchService, parameters, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        return await _transactionService.HandleTransaction(operation, this, cancellationToken).ConfigureAwait(false);
    }

    public async Task<FhirResponse> ConditionalDelete(
        IKey key,
        IEnumerable<Tuple<string, string>> parameters,
        CancellationToken cancellationToken)
    {
        var operation = await ResourceManipulationOperationFactory
            .CreateDelete(key, _searchService, SearchParams.FromUriParamList(parameters), cancellationToken)
            .ConfigureAwait(false);
        return await _transactionService.HandleTransaction(operation, this, cancellationToken).ConfigureAwait(false)
               ?? Respond.WithCode(HttpStatusCode.NotFound);
    }

    public async Task<FhirResponse?> ConditionalUpdate(
        IKey key,
        Resource resource,
        SearchParams parameters,
        CancellationToken cancellationToken)
    {
        // FIXME: if update receives a key with no version how do we handle concurrency?

        var operation = await resource.CreatePut(key, _searchService, parameters, cancellationToken: cancellationToken).ConfigureAwait(false);
        return await _transactionService.HandleTransaction(operation, this, cancellationToken).ConfigureAwait(false);
    }

    public Task<FhirResponse> CapabilityStatement(string sparkVersion)
    {
        var response = Respond.WithResource(_capabilityStatementService.GetSparkCapabilityStatement(sparkVersion));
        return Task.FromResult(response);
    }

    public async Task<FhirResponse> Create(IKey key, Resource resource, CancellationToken cancellationToken)
    {
        Validate.Key(key);
        Validate.HasTypeName(key);
        Validate.ResourceType(key, resource);

        key = key.CleanupForCreate();
        var result = await Store(Entry.Post(key, resource), cancellationToken).ConfigureAwait(false);
        return Respond.WithResource(HttpStatusCode.Created, result);
    }

    public async Task<FhirResponse> Delete(IKey key, CancellationToken cancellationToken)
    {
        Validate.Key(key);
        Validate.HasNoVersion(key);

        var current = await _storageService.Get(key, cancellationToken).ConfigureAwait(false);
        return current is { IsPresent: true }
            ? await Delete(Entry.Delete(key, DateTimeOffset.UtcNow), cancellationToken).ConfigureAwait(false)
            : Respond.WithCode(HttpStatusCode.NotFound);
    }

    public async Task<FhirResponse> Delete(Entry entry, CancellationToken cancellationToken)
    {
        Validate.Key(entry.Key);
        await Store(entry, cancellationToken).ConfigureAwait(false);
        return Respond.WithCode(HttpStatusCode.NoContent);
    }

    public async Task<FhirResponse> GetPage(string snapshotKey, int index, CancellationToken cancellationToken)
    {
        var snapshot = await _pagingService.StartPagination(snapshotKey, cancellationToken).ConfigureAwait(false);
        var page = await snapshot.GetPage(cancellationToken, index).ConfigureAwait(false);
        return _responseFactory.GetFhirResponse(page);
    }

    public async Task<FhirResponse> History(HistoryParameters parameters, CancellationToken cancellationToken)
    {
        var snapshot = await _historyService.History(parameters).ConfigureAwait(false);
        return await CreateSnapshotResponse(snapshot, 0, cancellationToken).ConfigureAwait(false);
    }

    public async Task<FhirResponse> History(string type, HistoryParameters parameters, CancellationToken cancellationToken)
    {
        var snapshot = await _historyService.History(type, parameters).ConfigureAwait(false);
        return await CreateSnapshotResponse(snapshot, 0, cancellationToken).ConfigureAwait(false);
    }

    public async Task<FhirResponse> History(
        IKey key,
        HistoryParameters parameters,
        CancellationToken cancellationToken)
    {
        if (await _storageService.Get(key, cancellationToken).ConfigureAwait(false) == null)
        {
            return Respond.NotFound(key);
        }

        var snapshot = await _historyService.History(key, parameters).ConfigureAwait(false);
        return await CreateSnapshotResponse(snapshot, 0, cancellationToken).ConfigureAwait(false);
    }

    public Task<FhirResponse> Mailbox(Bundle bundle, Binary body) => throw new NotImplementedException();

    public Task<FhirResponse> Put(IKey key, Resource resource, CancellationToken cancellationToken)
    {
        Validate.HasResourceId(resource);
        Validate.IsResourceIdEqual(key, resource);
        return Put(Entry.Put(key, resource), cancellationToken);
    }

    public async Task<FhirResponse> Put(Entry entry, CancellationToken cancellationToken)
    {
        Validate.Key(entry.Key);
        var entryKey = entry.Key;
        Validate.ResourceType(entryKey, entry.Resource);
        Validate.HasTypeName(entryKey);
        Validate.HasResourceId(entryKey);

        //return Transaction(entry);
        var result = await Store(Entry.Put(entryKey, entry.Resource), cancellationToken).ConfigureAwait(false);
        return Respond.WithResource(HttpStatusCode.Created, result);
    }

    public async Task<FhirResponse> Read(
        IKey key,
        ConditionalHeaderParameters? parameters = null,
        CancellationToken cancellationToken = default)
    {
        Validate.ValidateKey(key);
        var entry = await _storageService.Get(key, cancellationToken).ConfigureAwait(false);
        return parameters == null
            ? await _responseFactory.GetFhirResponse(entry, key).ConfigureAwait(false)
            : await _responseFactory.GetFhirResponse(entry, key, parameters).ConfigureAwait(false);
    }

    public async Task<FhirResponse> ReadMeta(IKey key, CancellationToken cancellationToken)
    {
        Validate.ValidateKey(key);
        var entry = await _storageService.Get(key, cancellationToken).ConfigureAwait(false);
        return await _responseFactory.GetMetadataResponse(entry, key).ConfigureAwait(false);
    }

    public async Task<FhirResponse> Search(
        string type,
        SearchParams searchCommand,
        int pageIndex = 0,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await _searchService.GetSnapshot(type, searchCommand, cancellationToken).ConfigureAwait(false);
        return await CreateSnapshotResponse(snapshot, pageIndex, cancellationToken).ConfigureAwait(false);
    }

    public async Task<FhirResponse> Transaction(IList<Entry> interactions, CancellationToken cancellationToken)
    {
        var responses = _transactionService.HandleTransaction(interactions, this, cancellationToken);
        return await _responseFactory.GetFhirResponse(responses, Bundle.BundleType.TransactionResponse).ConfigureAwait(false);
    }

    public async Task<FhirResponse> Transaction(Bundle bundle, CancellationToken cancellationToken)
    {
        var responses = _transactionService.HandleTransaction(bundle, this, cancellationToken);
        return await _responseFactory.GetFhirResponse(responses, Bundle.BundleType.TransactionResponse).ConfigureAwait(false);
    }

    public async Task<FhirResponse> Update(IKey key, Resource resource, CancellationToken cancellationToken)
    {
        return key.HasVersionId()
            ? await VersionSpecificUpdate(key, resource, cancellationToken).ConfigureAwait(false)
            : await Put(key, resource, cancellationToken).ConfigureAwait(false);
    }

    public async Task<FhirResponse> Patch(IKey key, Parameters parameters, CancellationToken cancellationToken)
    {
        var current = await _storageService.Get(key.WithoutVersion(), cancellationToken).ConfigureAwait(false);
        if (current is not { IsPresent: true })
        {
            return Respond.WithCode(HttpStatusCode.NotFound);
        }

        try
        {
            var entry = await _storageService.Load(key, cancellationToken).ConfigureAwait(false);
            var resource = _patchService.Apply(entry!, parameters);
            return await Patch(Entry.Patch(current.GetKey().WithoutVersion(), resource), cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            return new FhirResponse(HttpStatusCode.BadRequest);
        }
    }

    public async Task<FhirResponse> Patch(Entry entry, CancellationToken cancellationToken)
    {
        Validate.Key(entry.Key);
        var entryKey = entry.Key;
        Validate.ResourceType(entryKey, entry.Resource);
        Validate.HasTypeName(entryKey);
        Validate.HasResourceId(entryKey);

        var result = await Store(entry, cancellationToken).ConfigureAwait(false);

        return Respond.WithResource(HttpStatusCode.OK, result);
    }

    public Task<FhirResponse> ValidateOperation(IKey key, Resource resource, CancellationToken cancellationToken)
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

    public async Task<FhirResponse> VersionRead(IKey key, CancellationToken cancellationToken)
    {
        Validate.ValidateKey(key, true);
        var entry = await _storageService.Get(key, cancellationToken).ConfigureAwait(false);
        return await _responseFactory.GetFhirResponse(entry, key).ConfigureAwait(false);
    }

    public async Task<FhirResponse> VersionSpecificUpdate(
        IKey versionedKey,
        Resource resource,
        CancellationToken cancellationToken)
    {
        Validate.HasTypeName(versionedKey);
        Validate.HasVersion(versionedKey);
        var key = versionedKey.WithoutVersion();
        var current = await _storageService.Get(key, cancellationToken).ConfigureAwait(false);
        Validate.IsSameVersion(current?.GetKey(), versionedKey);
        return await Put(key, resource, cancellationToken).ConfigureAwait(false);
    }

    public async Task<FhirResponse> Everything(IKey key, CancellationToken cancellationToken)
    {
        var snapshot = await _searchService.GetSnapshotForEverything(key, cancellationToken).ConfigureAwait(false);
        return await CreateSnapshotResponse(snapshot, 0, cancellationToken).ConfigureAwait(false);
    }

    public async Task<FhirResponse> Document(IKey key, CancellationToken cancellationToken)
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

        return await Search(key.TypeName ?? "", searchCommand, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    public async Task<FhirResponse> HandleInteraction(Entry interaction, CancellationToken cancellationToken)
    {
        return (interaction.Method) switch
        {
            Bundle.HTTPVerb.PUT => await Put(interaction, cancellationToken).ConfigureAwait(false),
            Bundle.HTTPVerb.POST => await Create(interaction, cancellationToken).ConfigureAwait(false),
            Bundle.HTTPVerb.DELETE => (await _storageService.Get(interaction.Key.WithoutVersion(), cancellationToken)
                .ConfigureAwait(false)) is { IsPresent: true }
                ? await Delete(interaction, cancellationToken).ConfigureAwait(false)
                : Respond.WithCode(HttpStatusCode.NotFound),
            Bundle.HTTPVerb.GET when (interaction.Key.HasVersionId()) => await VersionRead(
                    interaction.Key,
                    cancellationToken)
                .ConfigureAwait(false),
            Bundle.HTTPVerb.GET => await Read(interaction.Key, null, cancellationToken).ConfigureAwait(false),
            Bundle.HTTPVerb.PATCH => await Patch(
                    interaction.Key,
                    (interaction.Resource as Parameters)!,
                    cancellationToken)
                .ConfigureAwait(false),
            _ => Respond.Success
        };
    }

    private async Task<FhirResponse> Create(Entry entry, CancellationToken cancellationToken)
    {
        Validate.Key(entry.Key);
        var entryKey = entry.Key;
        Validate.HasTypeName(entryKey);
        Validate.ResourceType(entryKey, entry.Resource);

        if (entry.State != EntryState.Internal)
        {
            Validate.HasNoResourceId(entryKey);
            Validate.HasNoVersion(entryKey);
        }

        var result = await Store(entry, cancellationToken).ConfigureAwait(false);

        return Respond.WithResource(HttpStatusCode.Created, result);
    }

    private async Task<Entry> Store(Entry entry, CancellationToken cancellationToken)
    {
        var result = await _storageService.Add(entry, cancellationToken).ConfigureAwait(false);
        await _serviceListener.Inform(result).ConfigureAwait(false);

        return result;
    }
    private async Task<FhirResponse> CreateSnapshotResponse(Snapshot snapshot, int pageIndex, CancellationToken cancellationToken)
    {
        var pagination = await _pagingService.StartPagination(snapshot, cancellationToken).ConfigureAwait(false);
        var bundle = await pagination.GetPage(cancellationToken, pageIndex).ConfigureAwait(false);
        return _responseFactory.GetFhirResponse(bundle);
    }
}