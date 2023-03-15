namespace OpenMedStack.SparkEngine.Service.FhirServiceExtensions;

using Hl7.Fhir.Model;

public interface ICapabilityStatementService
{
    CapabilityStatement GetSparkCapabilityStatement(string sparkVersion);
}