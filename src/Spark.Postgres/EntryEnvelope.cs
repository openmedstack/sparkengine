// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace Spark.Postgres
{
    using System;
    using Engine.Core;
    using Hl7.Fhir.Model;

    public class EntryEnvelope
    {
        public string Id { get; init; }

        public string ResourceType { get; init; }

        public EntryState State { get; init; }

        public IKey Key { get; init; }

        public string ResourceKey { get; init; }

        public Bundle.HTTPVerb Method { get; init; }

        public DateTimeOffset? When { get; init; }

        public Resource Resource { get; init; }

        public bool IsPresent { get; set; }

        public bool Deleted { get; init; }
    }
}