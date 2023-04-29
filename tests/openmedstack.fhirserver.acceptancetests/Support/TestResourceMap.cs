using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Security.Claims;
using DotAuth.Client;
using DotAuth.Shared;
using DotAuth.Shared.Requests;
using DotAuth.Shared.Responses;
using DotAuth.Uma;
using DotAuth.Uma.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace OpenMedStack.FhirServer.AcceptanceTests.Support;

internal class TestResourceMap: StaticResourceMap, IResourceMapper
{
    public TestResourceMap(IReadOnlySet<KeyValuePair<string, string>> mappings) : base(mappings)
    {
    }

    public Task MapResource(string resourceId, string resourceSetId, CancellationToken cancellationToken = default)
    {
        MappedResourcesCount++;
        return Task.CompletedTask;
    }

    public int MappedResourcesCount { get; private set; }
}
