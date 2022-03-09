// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace Spark.Engine.Store.Interfaces
{
    public class FhirStorePageReaderOptions
    {
        public int PageSize { get; set; } = 100;

        // TODO: add criteria?
        // TODO: add sorting?
    }
}