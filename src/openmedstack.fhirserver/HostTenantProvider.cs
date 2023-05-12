namespace OpenMedStack.FhirServer;

using Microsoft.AspNetCore.Http;

internal class HostTenantProvider : IProvideTenant
{
    private readonly IHttpContextAccessor _contextAccessor;

    public HostTenantProvider(IHttpContextAccessor contextAccessor)
    {
        _contextAccessor = contextAccessor;
    }

    public string GetTenantName()
    {
        return _contextAccessor.HttpContext?.Request.Host.Host ?? "default";
    }
}
