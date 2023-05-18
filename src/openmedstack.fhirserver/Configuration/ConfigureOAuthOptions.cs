namespace OpenMedStack.FhirServer.Configuration;

using DotAuth.Shared;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.Extensions.Options;
using Task = System.Threading.Tasks.Task;

public class ConfigureOAuthOptions : IPostConfigureOptions<OAuthOptions>
{
    public void PostConfigure(string? name, OAuthOptions options)
    {
        options.Events.OnCreatingTicket = async ctx =>
        {
            await Task.Yield();
            if (!string.IsNullOrWhiteSpace(ctx.AccessToken))
            {
                var sub = ctx.Principal?.GetSubject();
                if (string.IsNullOrWhiteSpace(sub))
                {
                    return;
                }
//                var fhirClient = ctx.HttpContext.RequestServices.GetRequiredService<FhirClient>();
//                var bundle = await fhirClient.Query<Patient>().Where(x =>
//                        x.Telecom.Any(y => y.System == ContactPoint.ContactPointSystem.Url && y.Value == sub))
//                    .GetBundle();
//                var patient = bundle.GetResources().OfType<Patient>().FirstOrDefault();
            }
        };
        options.Events.OnRedirectToAuthorizationEndpoint = ctx =>
        {
            ctx.Response.Redirect(ctx.RedirectUri);
            return Task.CompletedTask;
        };
    }
}
