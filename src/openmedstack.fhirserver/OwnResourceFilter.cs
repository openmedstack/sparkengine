namespace OpenMedStack.FhirServer;

using System.Linq;
using DotAuth.Shared;
using Hl7.Fhir.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Task = System.Threading.Tasks.Task;

internal class OwnResourceFilter : ActionFilterAttribute
{
    public override Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        if (!context.ActionArguments.TryGetValue("resource", out var resource))
        {
            context.Result = new BadRequestResult();
            return Task.CompletedTask;
        }

        var subject = context.HttpContext.User.GetSubject();
        if (string.IsNullOrWhiteSpace(subject))
        {
            context.Result = new ForbidResult();
            return Task.CompletedTask;
        }

        bool HasUserReference(ResourceReference reference)
        {
            return reference.Reference == $"{ModelInfo.ResourceTypeToFhirTypeName(ResourceType.Patient)}/{subject}";
        }

        var allowed = resource switch
        {
            Patient p => p.Contact.Any(
                x => x.Telecom.Any(t => t.System == ContactPoint.ContactPointSystem.Url && t.Value == subject)),
            NutritionIntake n => HasUserReference(n.Subject),
            Observation o => HasUserReference(o.Subject),
            Procedure p => HasUserReference(p.Subject),
            MedicationDispense m => m.Performer.Any(performer =>
                performer?.Actor?.Reference?.EndsWith($"/{subject}") == true),
            _ => false
        };
        if (!allowed)
        {
            context.Result = new ForbidResult();
        }

        return next();
    }
}
