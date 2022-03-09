namespace Spark.Engine.Web.Tests.Persistence
{
    using System;
    using Core;
    using Hl7.Fhir.Model;

    public class EntryEnvelope
    {
        public string Id { get; set; }

        public string ResourceType { get; set; }

        public EntryState State { get; set; }
        public IKey Key { get; set; }
        public string ResourceKey { get; set; }
        public Bundle.HTTPVerb Method { get; set; }
        public DateTimeOffset? When { get; set; }
        public Resource Resource { get; set; }
        public bool IsPresent { get; set; }
        public bool Deleted { get; set; }
    }
}