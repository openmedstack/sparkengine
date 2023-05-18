/*
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * Copyright (c) 2021, Incendi (info@incendi.no) and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

namespace OpenMedStack.SparkEngine.Service;

using System;
using System.Linq;
using System.Net;
using Extensions;
using Hl7.Fhir.Model;
using Hl7.Fhir.Utility;
using Interfaces;
using Error = Core.Error;

public static class Validate
{
    public static void TypeName(string name)
    {
        if (ModelInfo.SupportedResources.Contains(name))
        {
            return;
        }

        //  Test for the most common mistake first: wrong casing of the resource name
        var correct =
            ModelInfo.SupportedResources.FirstOrDefault(s => s.ToUpperInvariant() == name.ToUpperInvariant());
        if (correct != null)
        {
            throw Error.NotFound("Wrong casing of collection name, try '{0}' instead", correct);
        }

        throw Error.NotFound("Unknown resource collection '{0}'", name);
    }

    public static void ResourceType(IKey key, Resource? resource)
    {
        if (resource == null)
        {
            throw Error.BadRequest("Request did not contain a body");
        }

        if (key.TypeName != resource.TypeName)
        {
            throw Error.BadRequest(
                "Received a body with a '{0}' resource, which does not match the indicated collection '{1}' in the url.",
                resource.TypeName,
                key.TypeName ?? "unknown");
        }
    }

    public static void ValidateKey(IKey key, bool withVersion = false)
    {
        Validate.HasTypeName(key);
        Validate.HasResourceId(key);
        if (withVersion)
        {
            Validate.HasVersion(key);
        }
        else
        {
            Validate.HasNoVersion(key);
        }
    }

    public static void Key(IKey? key)
    {
        if (key.HasResourceId())
        {
            ResourceId(key!.ResourceId!);
        }

        if (key.HasVersionId())
        {
            VersionId(key!.VersionId!);
        }

        if (!string.IsNullOrEmpty(key?.TypeName))
        {
            TypeName(key.TypeName);
        }
    }

    public static void HasTypeName(IKey key)
    {
        if (string.IsNullOrEmpty(key.TypeName))
        {
            throw Error.BadRequest("Resource type is missing: {0}", key);
        }
    }

    public static void HasResourceId(IKey key)
    {
        if (key.HasResourceId())
        {
            ResourceId(key.ResourceId!);
        }
        else
        {
            throw Error.BadRequest("The request should have a resource id.");
        }
    }

    public static void HasResourceId(Resource resource)
    {
        if (string.IsNullOrEmpty(resource.Id))
        {
            throw Error.BadRequest("The resource MUST contain an Id.");
        }
    }

    public static void IsResourceIdEqual(IKey key, Resource resource)
    {
        if (key.ResourceId != resource.Id)
        {
            throw Error.BadRequest(
                "The Id in the request '{0}' is not the same is the Id in the resource '{1}'.",
                key.ResourceId ?? "null",
                resource.Id);
        }
    }

    public static void HasVersion(IKey key)
    {
        if (key.HasVersionId())
        {
            VersionId(key.VersionId!);
        }
        else
        {
            throw Error.BadRequest("The request should contain a version id.");
        }
    }

    public static void HasNoVersion(IKey key)
    {
        if (key.HasVersionId())
        {
            throw Error.BadRequest("Resource should not contain a version.");
        }
    }

    public static void HasNoResourceId(IKey key)
    {
        if (key.HasResourceId())
        {
            throw Error.BadRequest("The request should not contain an id");
        }
    }

    public static void VersionId(string versionId)
    {
        if (string.IsNullOrEmpty(versionId))
        {
            throw Error.BadRequest("Must pass history id in url.");
        }
    }

    public static void ResourceId(string resourceId)
    {
        if (string.IsNullOrEmpty(resourceId))
        {
            throw Error.BadRequest("Logical ID is empty");
        }

        if (!Id.IsValidValue(resourceId))
        {
            throw Error.BadRequest($"{resourceId} is not a valid value for an id");
        }

        if (resourceId.Length > 64)
        {
            throw Error.BadRequest("Logical ID is too long.");
        }
    }

    public static void IsSameVersion(IKey? original, IKey? replacement)
    {
        if (original?.VersionId != replacement?.VersionId)
        {
            throw Error.Create(
                HttpStatusCode.Conflict,
                "The current resource on this server '{0}' doesn't match the required version '{1}'",
                original?.VersionId ?? "null",
                replacement?.VersionId ?? "null");
        }
    }

    public static OperationOutcome AgainstSchema(Resource resource) =>
        throw
            //var result = new OperationOutcome {Issue = new List<OperationOutcome.IssueComponent>()};
            new NotImplementedException();

    public static void HasResourceType(IKey key, ResourceType type)
    {
        if (key.TypeName != type.GetLiteral())
        {
            throw Error.BadRequest("Operation only valid for {0} resource type");
        }
    }
}
