// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace OpenMedStack.SparkEngine.Postgres;

using Core;
using Hl7.Fhir.Model;
using Marten;
using Weasel.Postgresql.Tables;

public class FhirRegistry : MartenRegistry
{
    public FhirRegistry()
    {
        For<Resource>()
            .AddSubClassHierarchy()
            .Identity(x => x.Id)
            .Index(x => x.TypeName)
            .Index(x => x.VersionId);
        For<ResourceInfo>()
            .Index(x => x.Id)
            .Index(x => x.ResourceId!)
            .Index(x => x.ResourceType!)
            .Index(x => x.VersionId!)
            .Index(x => x.IsDeleted)
            .Index(x => x.IsPresent)
            .Index(x => x.When!)
            .GinIndexJsonData();
        For<IndexEntry>()
            .Identity(x => x.Id)
            .Index(x => x.CanonicalId)
            .Index(x => x.ResourceType)
            .Index(
                x => x.Values,
                idx =>
                {
                    idx.Method = IndexMethod.gin;
                    idx.Mask = "? jsonb_ops";
                })
            .GinIndexJsonData(idx => { idx.Mask = "? jsonb_ops"; });
    }
}
