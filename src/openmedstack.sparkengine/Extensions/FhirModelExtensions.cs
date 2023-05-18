// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace OpenMedStack.SparkEngine.Extensions;

using System.Collections.Generic;
using Core;
using Hl7.Fhir.Model;

public static class FhirModelExtensions
{
    public static Bundle Append(this Bundle bundle, Entry entry, FhirResponse? response = null)
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
}
