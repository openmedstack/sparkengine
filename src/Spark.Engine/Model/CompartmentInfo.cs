// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace Spark.Engine.Model
{
    using System.Collections.Generic;
    using Hl7.Fhir.Model;

    /// <summary>
    ///     Class for holding information as present in a CompartmentDefinition resource.
    ///     This is a (hopefully) temporary solution, since the Hl7.Fhir api does not containt CompartmentDefinition yet.
    /// </summary>
    public class CompartmentInfo
    {
        public CompartmentInfo(ResourceType resourceType) => ResourceType = resourceType;

        public ResourceType ResourceType { get; set; }

        public List<string> ReverseIncludes { get; } = new();

        public void AddReverseInclude(string revInclude)
        {
            ReverseIncludes.Add(revInclude);
        }

        public void AddReverseIncludes(IEnumerable<string> revIncludes)
        {
            ReverseIncludes.AddRange(revIncludes);
        }
    }
}