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
using System.Threading.Tasks;
using Core;
using Hl7.Fhir.Model;
using Interfaces;

public interface IFhirResponseFactory
{
    Task<FhirResponse> GetFhirResponse(ResourceInfo? entry, IKey? key = null, IEnumerable<object>? parameters = null);
    Task<FhirResponse> GetFhirResponse(ResourceInfo? entry, IKey? key = null, params object[] parameters);
    Task<FhirResponse> GetMetadataResponse(ResourceInfo? entry, IKey? key = null);
    FhirResponse GetFhirResponse(Bundle? bundle);

    Task<FhirResponse> GetFhirResponse(
        IAsyncEnumerable<Tuple<Entry, FhirResponse>> responses,
        Bundle.BundleType bundleType);
}