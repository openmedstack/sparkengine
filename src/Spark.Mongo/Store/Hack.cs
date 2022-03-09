// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace Spark.Mongo.Store
{
    using Engine.Auxiliary;
    using Hl7.Fhir.Model;

    public static class Hack
    {
        // HACK: json extensions
        // Since WGM Chicago, extensions in json have their url in the json-name.
        // because MongoDB doesn't allow dots in the json-name, this hack will remove all extensions for now.
        public static void RemoveExtensions(Resource resource)
        {
            if (resource is DomainResource domain)
            {
                domain.Extension = null;
                domain.ModifierExtension = null;
                RemoveExtensionsFromElements(domain);
                foreach (var r in domain.Contained)
                {
                    RemoveExtensions(r);
                }
            }
        }

        public static void ElementExtensionRemover(Element element, string path)
        {
            element.Extension = null;
        }

        public static void RemoveExtensionsFromElements(Resource resource)
        {
            ResourceVisitor.VisitByType(resource, ElementExtensionRemover, typeof(Element));
        }
    }
}