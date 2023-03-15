// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace OpenMedStack.SparkEngine.Core;

using System.Net;
using Hl7.Fhir.Model;
using Interfaces;

public class FhirResponse
{
    public IKey? Key { get; }
    public Resource? Resource { get; }
    public HttpStatusCode StatusCode { get; }

    public FhirResponse(HttpStatusCode code, IKey? key = null, Resource? resource = null)
    {
        StatusCode = code;
        Key = key;
        Resource = resource;
    }

    public bool IsValid
    {
        get
        {
            var code = (int)StatusCode;
            return code <= 300;
        }
    }

    public bool HasBody => Resource != null;

    public override string ToString()
    {
        var details = Resource != null ? $"({Resource.TypeName})" : null;
        var location = Key?.ToString();
        return $"{(int)StatusCode}: {StatusCode} {details} ({location})";
    }
}