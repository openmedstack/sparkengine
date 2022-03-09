namespace Spark.Engine.Service.FhirServiceExtensions
{
    using Hl7.Fhir.Model;

    public interface IPatchService
    {
        Resource Apply(Resource resource, Parameters patch);
    }
}