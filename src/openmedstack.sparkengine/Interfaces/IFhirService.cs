// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace OpenMedStack.SparkEngine.Interfaces;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Core;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;

public interface IFhirService
{
    Task<FhirResponse> AddMeta(IKey key, Parameters parameters, CancellationToken cancellationToken);
    Task<FhirResponse?> ConditionalCreate(IKey key, Resource resource, IEnumerable<Tuple<string, string>> parameters, CancellationToken cancellationToken);
    Task<FhirResponse?> ConditionalCreate(IKey key, Resource resource, SearchParams parameters, CancellationToken cancellationToken);
    Task<FhirResponse> ConditionalDelete(IKey key, IEnumerable<Tuple<string, string>> parameters, CancellationToken cancellationToken);
    Task<FhirResponse?> ConditionalUpdate(IKey key, Resource resource, SearchParams parameters, CancellationToken cancellationToken);
    Task<FhirResponse> CapabilityStatement(string sparkVersion);
    Task<FhirResponse> Create(IKey key, Resource resource, CancellationToken cancellationToken);
    Task<FhirResponse> Delete(IKey key, CancellationToken cancellationToken);
    Task<FhirResponse> Delete(Entry entry, CancellationToken cancellationToken);
    Task<FhirResponse> GetPage(string snapshotKey, int index, CancellationToken cancellationToken);
    Task<FhirResponse> History(HistoryParameters parameters, CancellationToken cancellationToken);
    Task<FhirResponse> History(string type, HistoryParameters parameters, CancellationToken cancellationToken);
    Task<FhirResponse> History(IKey key, HistoryParameters parameters, CancellationToken cancellationToken);
    Task<FhirResponse> Put(IKey key, Resource resource, CancellationToken cancellationToken);
    Task<FhirResponse> Put(Entry entry, CancellationToken cancellationToken);
    Task<FhirResponse> Read(IKey key, ConditionalHeaderParameters? parameters = null, CancellationToken cancellationToken = default);
    Task<FhirResponse> ReadMeta(IKey key, CancellationToken cancellationToken);
    Task<FhirResponse> Search(string type, SearchParams searchCommand, int pageIndex = 0, CancellationToken cancellationToken = default);
    Task<FhirResponse> Transaction(IList<Entry> interactions, CancellationToken cancellationToken);
    Task<FhirResponse> Transaction(Bundle bundle, CancellationToken cancellationToken);
    Task<FhirResponse> Update(IKey key, Resource resource, CancellationToken cancellationToken);
    Task<FhirResponse> Patch(IKey key, Parameters patch, CancellationToken cancellationToken);
    Task<FhirResponse> Patch(Entry entry, CancellationToken cancellationToken);
    Task<FhirResponse> ValidateOperation(IKey key, Resource resource, CancellationToken cancellationToken);
    Task<FhirResponse> VersionRead(IKey key, CancellationToken cancellationToken);
    Task<FhirResponse> VersionSpecificUpdate(IKey versionedKey, Resource resource, CancellationToken cancellationToken);
    Task<FhirResponse> Everything(IKey key, CancellationToken cancellationToken);
    Task<FhirResponse> Document(IKey key, CancellationToken cancellationToken);
}