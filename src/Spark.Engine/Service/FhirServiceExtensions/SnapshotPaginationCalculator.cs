// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace Spark.Engine.Service.FhirServiceExtensions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core;

    public class SnapshotPaginationCalculator : ISnapshotPaginationCalculator
    {
        public const int DEFAULT_PAGE_SIZE = 20;

        public IEnumerable<IKey> GetKeysForPage(Snapshot snapshot, int? start = null)
        {
            var keysInBundle = snapshot.Keys;
            if (start.HasValue)
            {
                keysInBundle = keysInBundle.Skip(start.Value);
            }

            return keysInBundle.Take(snapshot.CountParam ?? DEFAULT_PAGE_SIZE)
                .Select(k => (IKey) Key.ParseOperationPath(k))
                .ToList();
        }

        public int GetIndexForLastPage(Snapshot snapshot)
        {
            var countParam = snapshot.CountParam ?? DEFAULT_PAGE_SIZE;
            if (snapshot.Count <= countParam)
            {
                return 0;
            }

            var numberOfPages = snapshot.Count / countParam;
            var lastPageIndex = snapshot.Count % countParam == 0 ? numberOfPages - 1 : numberOfPages;
            return lastPageIndex * countParam;
        }

        public int? GetIndexForNextPage(Snapshot snapshot, int? start = null)
        {
            var countParam = snapshot.CountParam ?? DEFAULT_PAGE_SIZE;

            return (start ?? 0) + countParam >= snapshot.Count ? null : (int?) ((start ?? 0) + countParam);
        }

        public int? GetIndexForPreviousPage(Snapshot snapshot, int? start = null)
        {
            var countParam = snapshot.CountParam ?? DEFAULT_PAGE_SIZE;
            return start.HasValue == false || start.Value == 0 ? null : (int?) Math.Max(0, start.Value - countParam);
        }
    }
}