// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace OpenMedStack.SparkEngine.Core
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using Hl7.Fhir.Model;

    public class Snapshot
    {
        private Snapshot(string id)
        {
            Id = id;
            Keys = Enumerable.Empty<string>();
            Includes = new Collection<string>();
            ReverseIncludes = new Collection<string>();
        }

        //public const int NOCOUNT = -1;
        public const int MAX_PAGE_SIZE = 100;

        public ICollection<string> Includes { get; set; }

        public string Id { get; }
        public Bundle.BundleType Type { get; private init; }

        public IEnumerable<string> Keys { get; init; }

        //public string FeedTitle { get; set; }
        public string FeedSelfLink { get; init; } = null!;
        public int Count { get; private init; }
        public int? CountParam { get; private init; }
        public DateTimeOffset WhenCreated { get; set; }
        public string? SortBy { get; private init; }
        public ICollection<string> ReverseIncludes { get; set; }

        public static Snapshot Create(
            Bundle.BundleType type,
            Uri selflink,
            IList<string> keys,
            string? sortby,
            int? count,
            IList<string> includes,
            IList<string> reverseIncludes)
        {
            var snapshot = new Snapshot(CreateKey())
            {
                Type = type,
                WhenCreated = DateTimeOffset.UtcNow,
                FeedSelfLink = selflink.ToString(),
                Includes = includes,
                ReverseIncludes = reverseIncludes,
                Keys = keys,
                Count = keys.Count,
                CountParam = NormalizeCount(count),
                SortBy = sortby
            };

            return snapshot;
        }

        private static int? NormalizeCount(int? count) => count.HasValue ? (int?)Math.Min(count.Value, MAX_PAGE_SIZE) : null;

        public static string CreateKey() => Guid.NewGuid().ToString();

        public bool InRange(int index)
        {
            if (index == 0 && !Keys.Any())
            {
                return true;
            }

            var last = Keys.Count() - 1;
            return index > 0 || index <= last;
        }
    }
}