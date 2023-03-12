namespace OpenMedStack.SparkEngine.Web.Tests;

using Controllers;
using Microsoft.AspNetCore.Mvc;
using SparkEngine.Service;

//[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[Route("fhir")]
[ApiController]
public class TestFhirController : FhirController
{
    //private readonly IFhirService _fhirService;

    public TestFhirController(IFhirService fhirService) : base(fhirService)
    {
    }
}