// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace Spark.Engine.Service.FhirServiceExtensions
{
    using System.Collections.Generic;
    using Core;

    public interface ISnapshotPaginationCalculator
    {
        IEnumerable<IKey> GetKeysForPage(Snapshot snapshot, int? start = null);
        int GetIndexForLastPage(Snapshot snapshot);
        int? GetIndexForNextPage(Snapshot snapshot, int? start = null);
        int? GetIndexForPreviousPage(Snapshot snapshot, int? start = null);
    }
}