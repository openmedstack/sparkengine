/* 
 * Copyright (c) 2016, Furore (info@furore.com) and contributors
 * Copyright (c) 2021, Incendi (info@incendi.no) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/spark/stu3/master/LICENSE
 */
namespace OpenMedStack.SparkEngine.Service.FhirServiceExtensions;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Core;
using Hl7.Fhir.Model;
using Interfaces;

public interface IResourceStorageService
{
    Task<bool> Exists(IKey key, CancellationToken cancellationToken);

    Task<ResourceInfo?> Get(IKey key, CancellationToken cancellationToken);

    Task<Resource?> Load(IKey key, CancellationToken cancellationToken);

    Task<Entry> Add(Entry entry, CancellationToken cancellationToken);

    IAsyncEnumerable<ResourceInfo> Get(IEnumerable<string> localIdentifiers, string? sortBy = null, CancellationToken cancellationToken = default);
}