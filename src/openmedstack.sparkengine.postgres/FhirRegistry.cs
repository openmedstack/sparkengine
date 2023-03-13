// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace OpenMedStack.SparkEngine.Postgres;

using Marten;
using Marten.Schema;
using Marten.Schema.Indexing.Unique;
using Weasel.Postgresql.Tables;

public class FhirRegistry : MartenRegistry
{
    public FhirRegistry()
    {
        For<EntryEnvelope>()
            .Index(x => x.Id)
            .Duplicate(x => x.ResourceType)
            .Duplicate(x => x.ResourceId!, notNull: false)
            .Duplicate(x => x.VersionId!, notNull: false)
            .Duplicate(x => x.ResourceKey)
            .Duplicate(x => x.Deleted)
            .Duplicate(x => x.IsPresent)
            .Index(x => x.When)
            .GinIndexJsonData();
        For<IndexEntry>()
            .Identity(x => x.Id)
            .Index(x => x.CanonicalId)
            .Index(x => x.ResourceType)
            .GinIndexJsonData(
                idx =>
                {
                    idx.Mask = "? jsonb_ops";
                });
    }
}