/* 
 * Copyright (c) 2016, Furore (info@furore.com) and contributors
 * Copyright (c) 2021, Incendi (info@incendi.no) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/spark/stu3/master/LICENSE
 */

namespace OpenMedStack.SparkEngine.Store.Interfaces
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Core;
    using Hl7.Fhir.Model;
    using Task = System.Threading.Tasks.Task;

    public interface IFhirStore
    {
        Task Add(Entry entry, CancellationToken cancellationToken = default);

        Task<Entry?> Get(IKey? key, CancellationToken cancellationToken = default);

        IAsyncEnumerable<Entry> Get(IEnumerable<IKey> localIdentifiers, CancellationToken cancellationToken = default);

        Task<bool> Exists(IKey? key, CancellationToken cancellationToken = default);
    }

    public interface IResourcePersistence
    {
        Task<bool> Store(Resource resource, CancellationToken cancellationToken);
        Task<Resource?> Get(IKey key, CancellationToken cancellationToken);
    }
}