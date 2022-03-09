// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace Spark.Engine.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Hl7.Fhir.Model;

    public class Snapshot
    {
        //public const int NOCOUNT = -1;
        public const int MAX_PAGE_SIZE = 100;
        public ICollection<string> Includes;

        public string Id { get; set; }
        public Bundle.BundleType Type { get; set; }

        public IEnumerable<string> Keys { get; set; }

        //public string FeedTitle { get; set; }
        public string FeedSelfLink { get; set; }
        public int Count { get; set; }
        public int? CountParam { get; set; }
        public DateTimeOffset WhenCreated { get; set; }
        public string SortBy { get; set; }
        public ICollection<string> ReverseIncludes { get; set; }

        public static Snapshot Create(
            Bundle.BundleType type,
            Uri selflink,
            IList<string> keys,
            string sortby,
            int? count,
            IList<string> includes,
            IList<string> reverseIncludes)
        {
            var snapshot = new Snapshot
            {
                Type = type,
                Id = CreateKey(),
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