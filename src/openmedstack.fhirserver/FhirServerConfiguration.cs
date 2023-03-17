namespace OpenMedStack.FhirServer;

using System;
using Web.Autofac;

internal class FhirServerConfiguration : WebDeploymentConfiguration
{
    public required string AccessKey { get; init; }
    public required string AccessSecret { get; init; }
    public required Uri StorageServiceUrl { get; init; }
    public required string FhirRoot { get; init; }
    public required bool CompressStorage { get; init; }
}