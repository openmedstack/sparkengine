// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace OpenMedStack.SparkEngine.FhirResponseFactory;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core;
using Extensions;
using Hl7.Fhir.Model;
using Interfaces;

public class FhirResponseFactory : IFhirResponseFactory
{
    private readonly IFhirResponseInterceptorRunner _interceptorRunner;
    private readonly IFhirStore _fhirStore;

    public FhirResponseFactory(IFhirResponseInterceptorRunner interceptorRunner, IFhirStore fhirStore)
    {
        _interceptorRunner = interceptorRunner;
        _fhirStore = fhirStore;
    }

    public async Task<FhirResponse> GetFhirResponse(ResourceInfo? entry, IKey? key = null, IEnumerable<object>? parameters = null)
    {
        if (entry == null)
        {
            return Respond.NotFound(key);
        }

        if (entry.IsDeleted)
        {
            return Respond.Gone(entry);
        }

        FhirResponse? response = null;

        if (parameters != null)
        {
            response = _interceptorRunner.RunInterceptors(entry, parameters);
        }

        if (response != null)
        {
            return response;
        }

        var resource = await _fhirStore.Load(Key.ParseOperationPath(entry.ResourceKey));
        return Respond.WithResource(resource!);
    }

    public Task<FhirResponse> GetFhirResponse(ResourceInfo? entry, IKey? key = null, params object[] parameters) =>
        GetFhirResponse(entry, key, parameters.AsEnumerable());

    public async Task<FhirResponse> GetMetadataResponse(ResourceInfo? entry, IKey? key = null)
    {
        if (entry == null)
        {
            return Respond.NotFound(key);
        }

        if (entry.IsDeleted)
        {
            return Respond.Gone(entry);
        }

        var resource = await _fhirStore.Load(key!);
        return Respond.WithMeta(resource!.Meta);
    }

    public async Task<FhirResponse> GetFhirResponse(
        IAsyncEnumerable<Tuple<Entry, FhirResponse>> responses,
        Bundle.BundleType bundleType)
    {
        var bundle = new Bundle { Type = bundleType };
        await foreach (var response in responses.ConfigureAwait(false))
        {
            bundle.Append(response.Item1, response.Item2);
        }

        return Respond.WithBundle(bundle);
    }

    public FhirResponse GetFhirResponse(Bundle? bundle) => Respond.WithBundle(bundle);
}