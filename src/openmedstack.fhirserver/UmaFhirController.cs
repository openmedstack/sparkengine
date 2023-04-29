using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Security.Claims;
using DotAuth.Shared.Responses;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace OpenMedStack.FhirServer;

using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using DotAuth.Client;
using DotAuth.Shared;
using DotAuth.Shared.Models;
using DotAuth.Shared.Requests;
using DotAuth.Uma;
using DotAuth.Uma.Web;
using Hl7.Fhir.Model;
using Microsoft.AspNetCore.Mvc;
using OpenMedStack.SparkEngine.Core;
using OpenMedStack.SparkEngine.Interfaces;
using OpenMedStack.SparkEngine.Web.Controllers;
using Task = System.Threading.Tasks.Task;

[Route("uma")]
public class UmaFhirController : FhirController
{
    private readonly IAccessTokenCache _tokenCache;
    private readonly IUmaResourceSetClient _resourceSetClient;
    private readonly IResourceMap _resourceMap;

    public UmaFhirController(
        IFhirService fhirService,
        IAccessTokenCache tokenCache,
        IUmaResourceSetClient resourceSetClient,
        IResourceMap resourceMap)
        : base(fhirService)
    {
        _tokenCache = tokenCache;
        _resourceSetClient = resourceSetClient;
        _resourceMap = resourceMap;
    }

    [FhirUmaFilter("{0}/{1}", new[] { "type", "id" }, allowedScope: "read")]
    public override Task<ActionResult<FhirResponse>> Read(string type, string id, CancellationToken cancellationToken)
    {
        return base.Read(type, id, cancellationToken);
    }

    [UmaFilter("{0}/{1}/_history/{2}", new[] { "type", "id", "vid" }, allowedScope: "read")]
    public override Task<FhirResponse> VRead(string type, string id, string vid, CancellationToken cancellationToken)
    {
        return base.VRead(type, id, vid, cancellationToken);
    }

    // [UmaFilter("{0}", new[] { "type" }, allowedScope: "create")]
    [OwnResourceFilter]
    public override Task<FhirResponse?> Create(string type, Resource resource, CancellationToken cancellationToken)
    {
        return base.Create(type, resource, cancellationToken);
    }

    [UmaFilter("{0}/{1}", new[] { "type", "id" }, allowedScope: "delete")]
    public override Task<FhirResponse> Delete(string type, string id, CancellationToken cancellationToken)
    {
        return base.Delete(type, id, cancellationToken);
    }

    [UmaFilter("{0}/{1}/$everything", new[] { "type", "id" }, allowedScope: "read")]
    public override Task<FhirResponse> Everything(string type, string id, CancellationToken cancellationToken)
    {
        return base.Everything(type, id, cancellationToken);
    }

    /// <inheritdoc />
    [UmaFilter("{0}", new[] { "type" }, allowedScope: "search")]
    public override async Task<FhirResponse> Search(string type, CancellationToken cancellationToken)
    {
        var idToken = Request.Headers["X-ID-TOKEN"].FirstOrDefault();
        if (idToken == null)
        {
            return new FhirResponse(HttpStatusCode.Forbidden);
        }

        var response = await base.Search(type, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode != HttpStatusCode.OK)
        {
            return response;
        }

        var bundle = (response.Resource as Bundle)!;
        var ids = bundle.GetResources().Select(x => x.HasVersionId ? x.VersionId : x.Id).ToArray();
        var accessToken = await _tokenCache.GetAccessToken("uma_protection").ConfigureAwait(false);

        var resourceOptions = await _resourceSetClient.SearchResources(
                new SearchResourceSet { IdToken = idToken, Terms = ids },
                accessToken?.AccessToken,
                cancellationToken)
            .ConfigureAwait(false);
        if (resourceOptions is not Option<PagedResult<ResourceSetDescription>>.Result resources)
        {
            return new FhirResponse(HttpStatusCode.BadRequest, Key.Create(type));
        }

        var availableIds = new HashSet<string>(
            (await Task.WhenAll(
                    resources.Item.Content.Select(d => _resourceMap.GetResourceId(d.Id)))
                .ConfigureAwait(false)).Where(s => s != null)
            .Select(s => s!));
        var entries = bundle.Entry.Where(x => availableIds.Contains(x.Resource.Id));
        var resultingBundle = new Bundle { Type = bundle.Type, Total = availableIds.Count };
        resultingBundle.Entry.AddRange(entries);
        return new FhirResponse(response.StatusCode, response.Key, resultingBundle);
    }
}


/// <summary>
/// Defines the UMA filter attribute
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Interface)]
public class FhirUmaFilterAttribute : Attribute, IFilterFactory
{
    private const string IdTokenParameter = "id_token";
    private readonly string? _allowedScope;
    private readonly string[] _resourceIdParameters;
    private readonly string _idTokenHeader;
    private readonly string? _resourceIdFormat;
    private readonly string? _realm;
    private readonly string[] _resourceAccessScope;

    /// <summary>
    /// Initializes a new instance of the <see cref="UmaFilter"/> class.
    /// </summary>
    /// <param name="resourceIdParameter">The parameter name identifying the resource id.</param>
    /// <param name="idTokenHeader">The request parameter for the id token.</param>
    /// <param name="realm">The resource realm</param>
    /// <param name="allowedScope">Scope allowed to access resource.</param>
    /// <param name="resourceAccessScope">The resource access scopes.</param>
    /// <summary>
    /// <para>Filters the incoming request to check permission using the UMA2 standard.</para>
    /// <para>If required, the id token if retrieved from either a query parameter or a request header (in that order) with the given <paramref name="idTokenHeader"/> name.</para>
    /// </summary>
    public FhirUmaFilterAttribute(
        string resourceIdParameter,
        string idTokenHeader = IdTokenParameter,
        string? realm = null,
        string? allowedScope = null,
        params string[] resourceAccessScope)
        : this(null, new[] { resourceIdParameter }, idTokenHeader, realm, allowedScope, resourceAccessScope)
    {
        _allowedScope = allowedScope;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UmaFilter"/> class.
    /// </summary>
    /// <param name="resourceIdFormat">The format string setting how the parameters build the identifier.</param>
    /// <param name="resourceIdParameters">The names of the parameters identifying the resource id.</param>
    /// <param name="idTokenHeader"></param>
    /// <param name="realm">The resource realm</param>
    /// <param name="allowedScope">Scope allowed to access resource.</param>
    /// <param name="resourceAccessScope">The resource access scopes.</param>
    public FhirUmaFilterAttribute(
        [StringSyntax(StringSyntaxAttribute.CompositeFormat)] string? resourceIdFormat,
        string[] resourceIdParameters,
        string idTokenHeader = IdTokenParameter,
        string? realm = null,
        string? allowedScope = null,
        params string[] resourceAccessScope)
    {
        _resourceIdParameters = resourceIdParameters;
        _idTokenHeader = idTokenHeader;
        _resourceIdFormat = resourceIdFormat;
        _realm = realm;
        _allowedScope = allowedScope;
        _resourceAccessScope = resourceAccessScope;
    }

    /// <inheritdoc />
    public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
    {
        return new FhirUmaAuthorizationFilter(
            serviceProvider.GetRequiredService<ITokenClient>(),
            serviceProvider.GetRequiredService<IUmaPermissionClient>(),
            serviceProvider.GetRequiredService<IResourceMap>(),
            serviceProvider.GetRequiredService<ILogger<UmaFilter>>(),
            _resourceIdParameters,
            realm: _realm,
            idTokenHeader: _idTokenHeader,
            resourceIdFormat: _resourceIdFormat,
            allowedScope: _allowedScope,
            scopes: _resourceAccessScope);
    }

    /// <inheritdoc />
    public bool IsReusable
    {
        get { return true; }
    }

    private class FhirUmaAuthorizationFilter : IAsyncAuthorizationFilter
    {
        private readonly ITokenClient _tokenClient;
        private readonly IUmaPermissionClient _permissionClient;
        private readonly IResourceMap _resourceMap;
        private readonly ILogger _logger;
        private readonly string? _realm;
        private readonly string[] _resourceIdParameters;
        private readonly string _idTokenHeader;
        private readonly string? _resourceIdFormat;
        private readonly string? _allowedScope;
        private readonly string[] _scopes;

        public FhirUmaAuthorizationFilter(
            ITokenClient tokenClient,
            IUmaPermissionClient permissionClient,
            IResourceMap resourceMap,
            ILogger logger,
            string[] resourceIdParameters,
            string idTokenHeader,
            string? realm = null,
            string? resourceIdFormat = null,
            string? allowedScope = null,
            params string[] scopes)
        {
            _tokenClient = tokenClient;
            _permissionClient = permissionClient;
            _resourceMap = resourceMap;
            _logger = logger;
            _realm = realm;
            _resourceIdParameters = resourceIdParameters;
            _idTokenHeader = idTokenHeader;
            _resourceIdFormat = resourceIdFormat;
            _allowedScope = allowedScope;
            _scopes = scopes;
        }

        /// <inheritdoc />
        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var token = context.HttpContext.Request.Headers.Authorization.FirstOrDefault();
            var user = context.HttpContext.User;
            if (CheckHasScopeAccess(user))
            {
                return;
            }

            var values = _resourceIdParameters.Select(x => context.RouteData.Values[x]).ToArray();
            var resourceId = _resourceIdFormat == null
                ? string.Join("", values.Select(v => (v ?? "").ToString()).ToArray())
                : string.Format(_resourceIdFormat, values);
            _logger.LogDebug("Attempting to map {resourceId}", resourceId);
            var resourceSetId = await _resourceMap.GetResourceSetId(resourceId).ConfigureAwait(false);
            if (resourceSetId == null)
            {
                _logger.LogError("Failed to map {resourceId} to resource set", resourceId);
                context.Result = new UnauthorizedResult();
                return;
            }

            if (CheckResourceAccess(user, resourceSetId, _allowedScope!))
            {
                var subject = user.GetSubject();
                var scopes = string.Join(",", _scopes);
                _logger.LogDebug(
                    "Received valid token for {resourceId}, scopes {scopes} from {subject}",
                    resourceId,
                    scopes,
                    subject);
                return;
            }

            var serverToken = await HasServerAccessToken(context).ConfigureAwait(false);
            if (serverToken == null)
            {
                return;
            }

            if (!HasIdToken(context, out var idToken))
            {
                _logger.LogError("No valid id token to request permission for {resourceId}", resourceId);
                return;
            }

            var permission = await _permissionClient.RequestPermission(
                    serverToken.AccessToken,
                    CancellationToken.None,
                    new PermissionRequest { IdToken = idToken, ResourceSetId = resourceSetId, Scopes = _scopes })
                .ConfigureAwait(false);
            switch (permission)
            {
                case Option<TicketResponse>.Error error:
                    _logger.LogError("Title: {title}, Details: {detail}", error.Details.Title, error.Details.Detail);
                    context.Result = new UmaServerUnreachableResult();
                    break;
                case Option<TicketResponse>.Result result:
                    _logger.LogDebug(
                        "Ticket {ticketId} received from {uri}",
                        result.Item.TicketId,
                        _permissionClient.Authority.AbsoluteUri);
                    context.Result = new UmaTicketResult(
                        new UmaTicketInfo(result.Item.TicketId, _permissionClient.Authority.AbsoluteUri, _realm));
                    break;
            }
        }

        private bool HasIdToken(AuthorizationFilterContext context, out string? idToken)
        {
            var request = context.HttpContext.Request;
            var hasIdToken = request.Query.TryGetValue(_idTokenHeader, out var token);
            idToken = token;
            if (hasIdToken)
            {
                return true;
            }

            if (AuthenticationHeaderValue.TryParse(request.Headers[_idTokenHeader], out var idTokenHeader))
            {
                idToken = idTokenHeader.Parameter;
                return true;
            }

            context.Result = new UmaServerUnreachableResult();
            return false;
        }

        private async Task<GrantedTokenResponse?> HasServerAccessToken(AuthorizationFilterContext context)
        {
            // No valid permissions found. Request a permission ticket.
            var option = await _tokenClient.GetToken(TokenRequest.FromScopes("uma_protection")).ConfigureAwait(false);
            if (option is Option<GrantedTokenResponse>.Result accessToken)
            {
                return accessToken.Item;
            }

            _logger.LogError("Could not retrieve access token for server");
            context.Result = new UmaServerUnreachableResult();
            return null;

        }

        private bool CheckHasScopeAccess(ClaimsPrincipal user)
        {
            if (_allowedScope == null
                || !user.HasClaim(
                    c => c.Type == StandardClaimNames.Scopes
                         && c.Value.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                             .Contains(_allowedScope)))
            {
                return false;
            }

            _logger.LogDebug(
                "Allowing access for user {subject} in role {allowedScope}",
                user.GetSubject(),
                string.Join(",", _allowedScope));
            return true;
        }

        public static bool CheckResourceAccess(
            ClaimsPrincipal principal,
            string? resourceSetId,
            params string[] scope)
        {
            if (string.IsNullOrWhiteSpace(resourceSetId))
                return false;
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            DotAuth.Shared.Models.Permission[] tickets;
            return principal.Identity is ClaimsIdentity identity && identity.TryGetUmaTickets(out tickets) && tickets.Any(l =>
            {
                if (l.ResourceSetId == resourceSetId)
                {
                    long? nullable = l.NotBefore;
                    long num1 = now;
                    if (nullable.GetValueOrDefault() <= num1 & nullable.HasValue)
                    {
                        nullable = l.Expiry;
                        long num2 = now;
                        if (nullable.GetValueOrDefault() > num2 & nullable.HasValue)
                            return scope.All(l.Scopes.Contains<string>);
                    }
                }
                return false;
            });
        }
    }
}
