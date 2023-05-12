using System.Linq;
using DotAuth.Shared;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Microsoft.Extensions.DependencyInjection;
using OpenMedStack.Linq2Fhir;

namespace OpenMedStack.FhirServer.Configuration;

using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.Extensions.Options;
using Task = System.Threading.Tasks.Task;

public class ConfigureOAuthOptions : IPostConfigureOptions<OAuthOptions>
{
    private readonly DeploymentConfiguration _configuration;

    public ConfigureOAuthOptions(DeploymentConfiguration configuration)
    {
        _configuration = configuration;
    }

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
        options.Events.OnTicketReceived = _ => Task.CompletedTask;
        options.Events.OnRedirectToAuthorizationEndpoint = ctx =>
        {
            ctx.Response.Redirect(ctx.RedirectUri);
            return Task.CompletedTask;
        };
        options.Events.OnAccessDenied = ctx => Task.CompletedTask;
        options.Events.OnRemoteFailure = ctx => Task.CompletedTask;
    }
}
