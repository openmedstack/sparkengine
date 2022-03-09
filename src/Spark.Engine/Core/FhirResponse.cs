// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace Spark.Engine.Core
{
    using System.Net;
    using Hl7.Fhir.Model;

    public class FhirResponse
    {
        public IKey Key;
        public Resource Resource;
        public HttpStatusCode StatusCode;

        public FhirResponse(HttpStatusCode code, IKey key, Resource resource)
        {
            StatusCode = code;
            Key = key;
            Resource = resource;
        }

        public FhirResponse(HttpStatusCode code, Resource resource)
        {
            StatusCode = code;
            Key = null;
            Resource = resource;
        }

        public FhirResponse(HttpStatusCode code)
        {
            StatusCode = code;
            Key = null;
            Resource = null;
        }

        public bool IsValid
        {
            get
            {
                var code = (int) StatusCode;
                return code <= 300;
            }
        }

        public bool HasBody => Resource != null;

        public override string ToString()
        {
            var details = Resource != null ? $"({Resource.TypeName})" : null;
            var location = Key?.ToString();
            return $"{(int) StatusCode}: {StatusCode} {details} ({location})";
        }
    }
}