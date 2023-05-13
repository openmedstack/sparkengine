namespace OpenMedStack.SparkEngine.Web.Tests;

using Controllers;
using Interfaces;
using Microsoft.AspNetCore.Mvc;

[Route("fhir")]
[ApiController]
public class TestFhirController : FhirController
{
    public TestFhirController(IFhirService fhirService, IFhirModel model) : base(fhirService, model)
    {
    }
}
