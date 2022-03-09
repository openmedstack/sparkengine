// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace Spark.Postgres
{
    using Marten;

    public class FhirRegistry : MartenRegistry
    {
        public FhirRegistry()
        {
            For<EntryEnvelope>()
                .Index(x => x.Id)
                .Duplicate(x => x.ResourceType)
                .Duplicate(x => x.Resource.Id)
                .Duplicate(x => x.Resource.VersionId)
                .Duplicate(x => x.ResourceKey)
                .Duplicate(x => x.Deleted)
                .Duplicate(x => x.IsPresent)
                .Index(x => x.When)
                .GinIndexJsonData();
            For<IndexEntry>()
                .Identity(x => x.Id)
                .Index(x => x.Id)
                .Duplicate(x => x.ResourceType)
                .GinIndexJsonData();
        }
    }
}
