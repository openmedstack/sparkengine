namespace OpenMedStack.SparkEngine.Service.FhirServiceExtensions
{
    using System;
    using System.Collections.Generic;
    using Core;
    using Hl7.Fhir.Model;
    using Hl7.Fhir.Rest;

    public class GetManipulationOperation : ResourceManipulationOperation
    {
        public GetManipulationOperation(Resource resource, IKey operationKey, SearchResults? searchResults, SearchParams? searchCommand = null) 
            : base(resource, operationKey, searchResults, searchCommand)
        {
        }
        
        public static Uri? ReadSearchUri(Bundle.EntryComponent entry)
        {
            return entry.Request != null
                ? new Uri(entry.Request.Url, UriKind.RelativeOrAbsolute)
                : null;
        }

        protected override IEnumerable<Entry> ComputeEntries()
        {
            if (SearchResults != null)
            {
                foreach (var localKeyLiteral in SearchResults)
                {
                    yield return Entry.Create(Bundle.HTTPVerb.GET, Key.ParseOperationPath(localKeyLiteral));
                }
            }
            else
            {
                yield return Entry.Create(Bundle.HTTPVerb.GET, OperationKey);
            }
        }
    }
}