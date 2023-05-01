using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
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

namespace OpenMedStack.FhirServer;
//
///// <summary>
///// Defines the UMA filter attribute
///// </summary>
//[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Interface)]
//public class FhirUmaFilterAttribute : Attribute, IFilterFactory
//{
//    private const string ID_TOKEN_PARAMETER = "id_token";
//    private readonly string? _allowedScope;
//    private readonly string[] _resourceIdParameters;
//    private readonly string _idTokenHeader;
//    private readonly string? _resourceIdFormat;
//    private readonly string? _realm;
//    private readonly string[] _resourceAccessScope;
//
//    /// <summary>
//    /// Initializes a new instance of the <see cref="UmaFilter"/> class.
//    /// </summary>
//    /// <param name="resourceIdParameter">The parameter name identifying the resource id.</param>
//    /// <param name="idTokenHeader">The request parameter for the id token.</param>
//    /// <param name="realm">The resource realm</param>
//    /// <param name="allowedScope">Scope allowed to access resource.</param>
//    /// <param name="resourceAccessScope">The resource access scopes.</param>
//    /// <summary>
//    /// <para>Filters the incoming request to check permission using the UMA2 standard.</para>
//    /// <para>If required, the id token if retrieved from either a query parameter or a request header (in that order) with the given <paramref name="idTokenHeader"/> name.</para>
//    /// </summary>
//    public FhirUmaFilterAttribute(
//        string resourceIdParameter,
//        string idTokenHeader = ID_TOKEN_PARAMETER,
//        string? realm = null,
//        string? allowedScope = null,
//        params string[] resourceAccessScope)
//        : this(null, new[] { resourceIdParameter }, idTokenHeader, realm, allowedScope, resourceAccessScope)
//    {
//        _allowedScope = allowedScope;
//    }
//
//    /// <summary>
//    /// Initializes a new instance of the <see cref="UmaFilter"/> class.
//    /// </summary>
//    /// <param name="resourceIdFormat">The format string setting how the parameters build the identifier.</param>
//    /// <param name="resourceIdParameters">The names of the parameters identifying the resource id.</param>
//    /// <param name="idTokenHeader"></param>
//    /// <param name="realm">The resource realm</param>
//    /// <param name="allowedScope">Scope allowed to access resource.</param>
//    /// <param name="resourceAccessScope">The resource access scopes.</param>
//    public FhirUmaFilterAttribute(
//        [StringSyntax(StringSyntaxAttribute.CompositeFormat)]
//        string? resourceIdFormat,
//        string[] resourceIdParameters,
//        string idTokenHeader = ID_TOKEN_PARAMETER,
//        string? realm = null,
//        string? allowedScope = null,
//        params string[] resourceAccessScope)
//    {
//        _resourceIdParameters = resourceIdParameters;
//        _idTokenHeader = idTokenHeader;
//        _resourceIdFormat = resourceIdFormat;
//        _realm = realm;
//        _allowedScope = allowedScope;
//        _resourceAccessScope = resourceAccessScope;
//    }
//
//    /// <inheritdoc />
//    public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
//    {
//        return new FhirUmaAuthorizationFilter(
//            serviceProvider.GetRequiredService<ITokenClient>(),
//            serviceProvider.GetRequiredService<IUmaPermissionClient>(),
//            serviceProvider.GetRequiredService<IResourceMap>(),
//            serviceProvider.GetRequiredService<ILogger<UmaFilter>>(),
//            _resourceIdParameters,
//            realm: _realm,
//            idTokenHeader: _idTokenHeader,
//            resourceIdFormat: _resourceIdFormat,
//            allowedScope: _allowedScope,
//            scopes: _resourceAccessScope);
//    }
//
//    /// <inheritdoc />
//    public bool IsReusable
//    {
//        get { return true; }
//    }
//
//    private class FhirUmaAuthorizationFilter : IAsyncAuthorizationFilter
//    {
//        private readonly ITokenClient _tokenClient;
//        private readonly IUmaPermissionClient _permissionClient;
//        private readonly IResourceMap _resourceMap;
//        private readonly ILogger _logger;
//        private readonly string? _realm;
//        private readonly string[] _resourceIdParameters;
//        private readonly string _idTokenHeader;
//        private readonly string? _resourceIdFormat;
//        private readonly string? _allowedScope;
//        private readonly string[] _scopes;
//
//        public FhirUmaAuthorizationFilter(
//            ITokenClient tokenClient,
//            IUmaPermissionClient permissionClient,
//            IResourceMap resourceMap,
//            ILogger logger,
//            string[] resourceIdParameters,
//            string idTokenHeader,
//            string? realm = null,
//            string? resourceIdFormat = null,
//            string? allowedScope = null,
//            params string[] scopes)
//        {
//            _tokenClient = tokenClient;
//            _permissionClient = permissionClient;
//            _resourceMap = resourceMap;
//            _logger = logger;
//            _realm = realm;
//            _resourceIdParameters = resourceIdParameters;
//            _idTokenHeader = idTokenHeader;
//            _resourceIdFormat = resourceIdFormat;
//            _allowedScope = allowedScope;
//            _scopes = scopes;
//        }
//
//        /// <inheritdoc />
//        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
//        {
//            var token = context.HttpContext.Request.Headers.Authorization.FirstOrDefault();
//            var user = context.HttpContext.User;
//            if (CheckHasScopeAccess(user, _allowedScope))
//            {
//                return;
//            }
//
//            var values = _resourceIdParameters.Select(x => context.RouteData.Values[x]).ToArray();
//            var resourceId = _resourceIdFormat == null
//                ? string.Join("", values.Select(v => (v ?? "").ToString()).ToArray())
//                : string.Format(_resourceIdFormat, values);
//            _logger.LogDebug("Attempting to map {resourceId}", resourceId);
//            var resourceSetId = await _resourceMap.GetResourceSetId(resourceId).ConfigureAwait(false);
//            if (resourceSetId == null)
//            {
//                _logger.LogError("Failed to map {resourceId} to resource set", resourceId);
//                context.Result = new UnauthorizedResult();
//                return;
//            }
//
//            if (CheckResourceAccess(user, resourceSetId, _scopes))
//            {
//                var subject = user.GetSubject();
//                var scopes = string.Join(",", _scopes);
//                _logger.LogDebug(
//                    "Received valid token for {resourceId}, scopes {scopes} from {subject}",
//                    resourceId,
//                    scopes,
//                    subject);
//                return;
//            }
//
//            var serverToken = await HasServerAccessToken(context).ConfigureAwait(false);
//            if (serverToken == null)
//            {
//                return;
//            }
//
//            if (!HasIdToken(context, out var idToken))
//            {
//                _logger.LogError("No valid id token to request permission for {resourceId}", resourceId);
//                return;
//            }
//
//            var permission = await _permissionClient.RequestPermission(
//                    serverToken.AccessToken,
//                    CancellationToken.None,
//                    new PermissionRequest { IdToken = idToken, ResourceSetId = resourceSetId, Scopes = _scopes })
//                .ConfigureAwait(false);
//            switch (permission)
//            {
//                case Option<TicketResponse>.Error error:
//                    _logger.LogError("Title: {title}, Details: {detail}", error.Details.Title, error.Details.Detail);
//                    context.Result = new UmaServerUnreachableResult();
//                    break;
//                case Option<TicketResponse>.Result result:
//                    _logger.LogDebug(
//                        "Ticket {ticketId} received from {uri}",
//                        result.Item.TicketId,
//                        _permissionClient.Authority.AbsoluteUri);
//                    context.Result = new UmaTicketResult(
//                        new UmaTicketInfo(result.Item.TicketId, _permissionClient.Authority.AbsoluteUri, _realm));
//                    break;
//            }
//        }
//
//        private bool HasIdToken(AuthorizationFilterContext context, out string? idToken)
//        {
//            var request = context.HttpContext.Request;
//            var hasIdToken = request.Query.TryGetValue(_idTokenHeader, out var token);
//            idToken = token;
//            if (hasIdToken)
//            {
//                return true;
//            }
//
//            if (AuthenticationHeaderValue.TryParse(request.Headers[_idTokenHeader], out var idTokenHeader))
//            {
//                idToken = idTokenHeader.Parameter;
//                return true;
//            }
//
//            context.Result = new UmaServerUnreachableResult();
//            return false;
//        }
//
//        private async Task<GrantedTokenResponse?> HasServerAccessToken(AuthorizationFilterContext context)
//        {
//            // No valid permissions found. Request a permission ticket.
//            var option = await _tokenClient.GetToken(TokenRequest.FromScopes("uma_protection")).ConfigureAwait(false);
//            if (option is Option<GrantedTokenResponse>.Result accessToken)
//            {
//                return accessToken.Item;
//            }
//
//            _logger.LogError("Could not retrieve access token for server");
//            context.Result = new UmaServerUnreachableResult();
//            return null;
//        }
//
//        private bool CheckHasScopeAccess(ClaimsPrincipal user, string? allowedOauthScope)
//        {
//            if (allowedOauthScope == null
//             || !user.HasClaim(
//                    c => c.Type == StandardClaimNames.Scopes
//                     && c.Value.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
//                            .Contains(allowedOauthScope)))
//            {
//                return false;
//            }
//
//            _logger.LogDebug(
//                "Allowing access for user {subject} in role {allowedScope}",
//                user.GetSubject(),
//                allowedOauthScope);
//            return true;
//        }
//
//        private static bool CheckResourceAccess(
//            IPrincipal principal,
//            string? resourceSetId,
//            params string[] scope)
//        {
//            if (string.IsNullOrWhiteSpace(resourceSetId))
//            {
//                return false;
//            }
//
//            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
//
//            bool IsValidTicket(DotAuth.Shared.Models.Permission l)
//            {
//                if (l.ResourceSetId == resourceSetId)
//                {
//                    var nullable = l.NotBefore;
//                    var num1 = now;
//                    if (nullable.GetValueOrDefault() <= num1 & nullable.HasValue)
//                    {
//                        nullable = l.Expiry;
//                        var num2 = now;
//                        if (nullable.GetValueOrDefault() > num2 & nullable.HasValue) return scope.All(l.Scopes.Contains<string>);
//                    }
//                }
//
//                return false;
//            }
//
//            return principal.Identity is ClaimsIdentity identity
//             && identity.TryGetUmaTickets(out var tickets)
//             && tickets.Any(IsValidTicket);
//        }
//    }
//}
