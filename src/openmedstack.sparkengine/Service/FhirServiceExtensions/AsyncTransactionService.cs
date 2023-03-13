/*
 * Copyright (c) 2021, Incendi (info@incendi.no) and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/spark/stu3/master/LICENSE
 */

namespace OpenMedStack.SparkEngine.Service.FhirServiceExtensions;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Core;
using Extensions;
using Hl7.Fhir.Model;
using Interfaces;

public class AsyncTransactionService : ITransactionService
{
    private readonly ILocalhost _localhost;
    private readonly ITransfer _transfer;
    private readonly ISearchService _searchService;

    public AsyncTransactionService(ILocalhost localhost, ITransfer transfer, ISearchService searchService)
    {
        _localhost = localhost;
        _transfer = transfer;
        _searchService = searchService;
    }

    private FhirResponse MergeFhirResponse(FhirResponse? previousResponse, FhirResponse response)
    {
        if (previousResponse == null)
        {
            return response;
        }

        if (!response.IsValid)
        {
            return response;
        }

        if (response.StatusCode != previousResponse.StatusCode)
        {
            throw new Exception("Incompatible responses");
        }

        if (response.Key != null
            && previousResponse.Key != null
            && response.Key.Equals(previousResponse.Key) == false)
        {
            throw new Exception("Incompatible responses");
        }

        if (response.Key != null && previousResponse.Key == null
            || response.Key == null && previousResponse.Key != null)
        {
            throw new Exception("Incompatible responses");
        }

        return response;
    }

    private void AddMappingsForOperation(
        Mapper<string, IKey>? mapper,
        ResourceManipulationOperation operation,
        IList<Entry> interactions)
    {
        if (mapper == null || interactions.Count != 1)
        {
            return;
        }

        var entry = interactions.First();
        if (entry.Key?.Equals(operation.OperationKey) != true)
        {
            mapper.Remap(
                _localhost.GetKeyKind(operation.OperationKey) == KeyKind.Temporary
                    ? operation.OperationKey.ResourceId!
                    : operation.OperationKey.ToString()!,
                entry.Key!.WithoutVersion());
        }
    }

    private async IAsyncEnumerable<Tuple<Entry, FhirResponse>> HandleTransaction(
        IEnumerable<Entry> interactions,
        IInteractionHandler interactionHandler,
        Mapper<string, IKey>? mapper,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var interaction in _transfer.Internalize(interactions, mapper, cancellationToken).ConfigureAwait(false))
        {
            var response = await interactionHandler.HandleInteraction(interaction, cancellationToken)
                .ConfigureAwait(false);
            if (!response.IsValid)
            {
                throw new Exception($"Unsuccessful response to interaction {interaction}: {response}");
            }

            interaction.Resource = response.Resource;
            //response.Resource = null;

            //_transfer.Externalize(interactions);
            yield return Tuple.Create(_transfer.Externalize(interaction), response);
            //responses.Add(new Tuple<Entry, FhirResponse>(interaction, response));
        }

        //_transfer.Externalize(interactions);
        //return responses;
    }

    public Task<FhirResponse?> HandleTransaction(
        ResourceManipulationOperation operation,
        IInteractionHandler interactionHandler,
        CancellationToken cancellationToken)
    {
        return HandleOperation(operation, interactionHandler, cancellationToken: cancellationToken);
    }

    public async Task<FhirResponse?> HandleOperation(
        ResourceManipulationOperation operation,
        IInteractionHandler interactionHandler,
        Mapper<string, IKey>? mapper = null,
        CancellationToken cancellationToken = default)
    {
        var interactions = operation.GetEntries();

        FhirResponse? response = null;
        await foreach (var interaction in _transfer.Internalize(interactions, mapper, cancellationToken).ConfigureAwait(false))
        {
            response = MergeFhirResponse(
                response,
                await interactionHandler.HandleInteraction(interaction, cancellationToken).ConfigureAwait(false));
            if (!response.IsValid)
            {
                throw new Exception();
            }

            interaction.Resource = response.Resource;
        }

        //_transfer.Externalize(interactions);

        return response;
    }

    public async IAsyncEnumerable<Tuple<Entry, FhirResponse>> HandleTransaction(
        Bundle bundle,
        IInteractionHandler interactionHandler,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (interactionHandler == null)
        {
            throw new InvalidOperationException("Unable to run transaction operation");
        }

        // Create a new list of EntryComponent according to FHIR transaction processing rules
        var entryComponents = new List<Bundle.EntryComponent>();
        entryComponents.AddRange(bundle.Entry.Where(e => e.Request.Method == Bundle.HTTPVerb.DELETE));
        entryComponents.AddRange(bundle.Entry.Where(e => e.Request.Method == Bundle.HTTPVerb.POST));
        entryComponents.AddRange(bundle.Entry.Where(e => e.Request.Method == Bundle.HTTPVerb.PUT));
        entryComponents.AddRange(bundle.Entry.Where(e => e.Request.Method == Bundle.HTTPVerb.GET));

        var entries = new List<Entry>();
        var mapper = new Mapper<string, IKey>();

        foreach (var task in entryComponents.Select(
                     e => ResourceManipulationOperationFactory.GetManipulationOperation(
                         e,
                         _localhost,
                         _searchService,
                         cancellationToken)))
        {
            var operation = await task.ConfigureAwait(false);
            IList<Entry> atomicOperations = operation.GetEntries().ToList();
            AddMappingsForOperation(mapper, operation, atomicOperations);
            entries.AddRange(atomicOperations);
        }

        await foreach (var transaction in HandleTransaction(entries, interactionHandler, mapper, cancellationToken)
                           .ConfigureAwait(false))
        {
            yield return transaction;
        }
    }

    public IAsyncEnumerable<Tuple<Entry, FhirResponse>> HandleTransaction(
        IList<Entry> interactions,
        IInteractionHandler interactionHandler,
        CancellationToken cancellationToken)
    {
        return interactionHandler == null
            ? throw new InvalidOperationException("Unable to run transaction operation")
            : HandleTransaction(interactions, interactionHandler, null, cancellationToken);
    }
}