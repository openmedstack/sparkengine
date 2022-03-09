// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace Spark.Engine.Extensions
{
    using System.Collections.Generic;
    using Core;
    using Hl7.Fhir.Model;

    public static class FhirModelExtensions
    {
        public static void Append(this Bundle bundle, Bundle.HTTPVerb method, Resource resource)
        {
            var entry = CreateEntryForResource(resource);

            entry.Request ??= new Bundle.RequestComponent();

            entry.Request.Method = method;
            bundle.Entry.Add(entry);
        }

        private static Bundle.EntryComponent CreateEntryForResource(Resource resource)
        {
            var entry = new Bundle.EntryComponent
            {
                Resource = resource,
                //            entry.FullUrl = resource.ResourceIdentity().ToString();
                FullUrl = resource.ExtractKey().ToUriString()
            };
            return entry;
        }

        public static Bundle Append(this Bundle bundle, Entry entry, FhirResponse response = null)
        {
            // API: The api should have a function for this. AddResourceEntry doesn't cut it.
            // Might TransactionBuilder be better suitable?

            var bundleEntry = bundle.Type switch
            {
                Bundle.BundleType.History => entry.ToTransactionEntry(),
                Bundle.BundleType.Searchset => entry.TranslateToSparseEntry(),
                Bundle.BundleType.BatchResponse => entry.TranslateToSparseEntry(response),
                Bundle.BundleType.TransactionResponse => entry.TranslateToSparseEntry(response),
                _ => entry.TranslateToSparseEntry()
            };

            bundle.Entry.Add(bundleEntry);

            return bundle;
        }

        public static Bundle Append(this Bundle bundle, IEnumerable<Entry> entries)
        {
            foreach (var entry in entries)
            {
                // BALLOT: whether to send transactionResponse components... not a very clean solution
                bundle.Append(entry);
            }

            // NB! Total can not be set by counting bundle elements, because total is about the snapshot total
            // bundle.Total = bundle.Entry.Count();

            return bundle;
        }
    }
}