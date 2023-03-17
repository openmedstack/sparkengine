// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace OpenMedStack.SparkEngine.Core;

using System.Net;
using Extensions;
using Hl7.Fhir.Model;
using Interfaces;

// This class serves instances of "Response"
public static class Respond
{
    public static FhirResponse Success => new(HttpStatusCode.OK);

    public static FhirResponse WithCode(HttpStatusCode code) => new(code);

    public static FhirResponse WithError(HttpStatusCode code, string message, params object?[] args)
    {
        var outcome = new OperationOutcome();
        outcome.AddError(string.Format(message, args));
        return new FhirResponse(code, null, outcome);
    }

    public static FhirResponse WithResource(int code, Resource resource) =>
        new((HttpStatusCode)code, null, resource);

    public static FhirResponse WithResource(Resource resource) => new(HttpStatusCode.OK, null, resource);

    public static FhirResponse WithBundle(Bundle? bundle) => new(HttpStatusCode.OK, null, bundle);

    public static FhirResponse WithMeta(Meta meta)
    {
        var parameters = new Parameters { { nameof(Meta), meta } };
        return WithResource(parameters);
    }

    public static FhirResponse WithMeta(Entry? entry) =>
        entry?.Resource?.Meta != null
            ? WithMeta(entry.Resource.Meta)
            : WithError(
                HttpStatusCode.InternalServerError,
                "Could not retrieve meta. Meta was not present on the resource");

    public static FhirResponse WithResource(HttpStatusCode code, Entry entry) =>
        new(code, entry.Key, entry.Resource!);

    public static FhirResponse WithResource(Entry entry) => new(HttpStatusCode.OK, entry.Key, entry.Resource!);

    public static FhirResponse NotFound(IKey? key) =>
        key?.VersionId == null
            ? WithError(
                HttpStatusCode.NotFound,
                "No {0} resource with id {1} was found.",
                key?.TypeName ?? "unknown",
                key?.ResourceId ?? "")
            : WithError(
                HttpStatusCode.NotFound,
                "There is no {0} resource with id {1}, or there is no version {2}",
                key.TypeName,
                key.ResourceId,
                key.VersionId);

    // For security reasons (leakage): keep message in sync with Error.NotFound(key)
    public static FhirResponse Gone(ResourceInfo entry)
    {
        var key = Key.ParseOperationPath(entry.ResourceKey);
        var message =
            $"A {key.TypeName ?? "unknown"} resource with id {key.ResourceId ?? "unknown"} existed, but was deleted on {entry.When} (version {key.ToRelativeUri().AbsoluteUri}).";

        return WithError(HttpStatusCode.Gone, message);
    }
}