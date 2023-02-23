/* 
 * Copyright (c) 2016, Furore (info@furore.com) and contributors
 * Copyright (c) 2021, Incendi (info@incendi.no) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/spark/stu3/master/LICENSE
 */
namespace OpenMedStack.SparkEngine.Service.FhirServiceExtensions;

using System.Threading;
using System.Threading.Tasks;
using Core;
using Hl7.Fhir.Rest;

public interface ISearchService
{
    Task<Snapshot> GetSnapshot(string type, SearchParams searchCommand, CancellationToken cancellationToken);

    Task<Snapshot> GetSnapshotForEverything(IKey key, CancellationToken cancellationToken);

    Task<IKey> FindSingle(string type, SearchParams searchCommand, CancellationToken cancellationToken);

    Task<IKey?> FindSingleOrDefault(string type, SearchParams searchCommand, CancellationToken cancellationToken);

    Task<SearchResults> GetSearchResults(string type, SearchParams searchCommand, CancellationToken cancellationToken);
}