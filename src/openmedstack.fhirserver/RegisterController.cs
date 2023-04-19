using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using DotAuth.Client;
using DotAuth.Shared;
using DotAuth.Shared.Models;
using DotAuth.Shared.Responses;
using DotAuth.Uma;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OpenMedStack.SparkEngine.Core;
using OpenMedStack.SparkEngine.Extensions;
using OpenMedStack.SparkEngine.Interfaces;

namespace OpenMedStack.FhirServer;

[Authorize]
[Route("register")]
public class RegisterController : ControllerBase
{
    private readonly IUmaResourceSetClient _resourceSetClient;
    private readonly IResourceMapper _resourceMap;
    private readonly IProvideApplicationName _applicationNameProvider;
    private readonly ILogger<RegisterController> _logger;

    public RegisterController(
        IUmaResourceSetClient resourceSetClient,
        IResourceMapper resourceMap,
        IProvideApplicationName applicationNameProvider,
        ILogger<RegisterController> logger)
    {
        _resourceSetClient = resourceSetClient;
        _resourceMap = resourceMap;
        _applicationNameProvider = applicationNameProvider;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Register([FromForm] string key, CancellationToken cancellationToken)
    {
        var k = Key.ParseOperationPath(key);
        var id = k.ToStorageKey();
        var token = Request.Headers.Authorization[0];
        const string fhirResource = "FHIR Resource";
        var option = await _resourceSetClient.AddResourceSet(new ResourceSet
            {
                AuthorizationPolicies = new[]
                {
                    new PolicyRule
                    {
                        ClientIdsAllowed = new[] { _applicationNameProvider.ClientId },
                        OpenIdProvider = _applicationNameProvider.Authority,
                        Scopes = new[] { "read" },
                        IsResourceOwnerConsentNeeded = true
                    }
                },
                Description = fhirResource,
                Type = k.TypeName ?? fhirResource,
                Name = id,
                Scopes = new[] { "read" }
            }, token!,
            cancellationToken);
        switch (option)
        {
            case Option<AddResourceSetResponse>.Result result:
                await _resourceMap.MapResource(id, result.Item.Id, cancellationToken);
                return Ok(result.Item);
            case Option<AddResourceSetResponse>.Error error:
                _logger.LogError("{title}: {detail}", error.Details.Title, error.Details.Detail);
                return Problem(error.Details.Detail, statusCode: (int)HttpStatusCode.InternalServerError,
                    title: error.Details.Title);
            default: throw new ArgumentOutOfRangeException();
        }
    }
}