/* 
 * Copyright (c) 2021, Incendi (info@incendi.no) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/spark/stu3/master/LICENSE
 */

using static Hl7.Fhir.Model.CapabilityStatement;

namespace OpenMedStack.SparkEngine.Tests.Core.Builders;

using System.Collections.Generic;
using System.Linq;
using Hl7.Fhir.Model;
using SparkEngine.Core;
using SparkEngine.Core.Builders;
using Xunit;

public class CapabilityStatementBuilderTests
{
    [Fact]
    public void CapabilityStatementStatusIsActive()
    {
        var builder = new CapabilityStatementBuilder();
        var capabilityStatement = builder
            .WithStatus(PublicationStatus.Active)
            .Build();
        Assert.Equal(PublicationStatus.Active, capabilityStatement.Status);
    }

    [Fact]
    public void CapabilityStatementStatusIsDraft()
    {
        var builder = new CapabilityStatementBuilder();
        var capabilityStatement = builder
            .WithStatus(PublicationStatus.Draft)
            .Build();
        Assert.Equal(PublicationStatus.Draft, capabilityStatement.Status);
    }

    [Fact]
    public void CapabilityStatementNameIsCS_SPARK()
    {
        var builder = new CapabilityStatementBuilder();
        var capabilityStatement = builder
            .WithName("CS_SPARK")
            .Build();
        Assert.Equal("CS_SPARK", capabilityStatement.Name);
    }

    [Fact]
    public void CapabilityStatementTitleIsSpark_Capability_Statement()
    {
        var builder = new CapabilityStatementBuilder();
        var capabilityStatement = builder
            .WithTitle("Spark Capability Statement")
            .Build();
        Assert.Equal("Spark Capability Statement", capabilityStatement.Title);
    }

    [Fact]
    public void CapabilityStatementCanBuildRestComponent()
    {
        var builder = new CapabilityStatementBuilder();
        var capabilityStatement = builder
            .WithPublisher("Incendi")
            .WithVersion("1.5.7")
            .WithDate(new FhirDateTime(2021, 7, 4))
            .WithDescription("This FHIR SERVER is a reference Implementation server built in C# on HL7.Fhir.Core (nuget) by Firely, Incendi and others")
            .WithKind(CapabilityStatementKind.Instance)
            .WithFhirVersion(FHIRVersion.N4_0_1)
            .WithAcceptFormat(FhirMediaType.JsonMimeTypes)
            .WithAcceptFormat(FhirMediaType.XmlMimeTypes)
            .WithRest(() =>
                new RestComponentBuilder()
                    .WithResource(() => new ResourceComponent
                    {
                        Type = ModelInfo.ResourceTypeToFhirTypeName(ResourceType.Patient),
                        Profile = "http://hl7.no/fhir/StructureDefinition/no-helseapi-Patient",
                        Interaction = new List<ResourceInteractionComponent>
                        {
                            new() {Code = TypeRestfulInteraction.Read},
                            new() {Code = TypeRestfulInteraction.SearchType},
                        },
                        SearchParam = new List<SearchParamComponent>
                        {
                            new() {Name = "identifier", Type = SearchParamType.Token, Documentation = new Markdown("A patient identifier")},
                            new() {Name = "name", Type = SearchParamType.String, Documentation = new Markdown("A server defined search that may match any of the string fields in the HumanName, including family, give, prefix, suffix, suffix, and/or text")},
                            new() {Name = "family", Type = SearchParamType.String},
                            new() {Name = "given", Type = SearchParamType.String},
                            new() {Name = "gender", Type = SearchParamType.Token},
                        },
                    })
                    .WithResource(() => new ResourceComponent
                    {
                        Type = ModelInfo.ResourceTypeToFhirTypeName(ResourceType.Practitioner),
                        Profile = "http://hl7.no/fhir/StructureDefinition/no-helseapi-Practitioner",
                        Interaction = new List<ResourceInteractionComponent>
                        {
                            new() {Code = TypeRestfulInteraction.Read},
                            new() {Code = TypeRestfulInteraction.SearchType},
                        },
                        SearchParam = new List<SearchParamComponent>
                        {
                            new() {Name = "identifier", Type = SearchParamType.Token, Documentation = new Markdown("A patient identifier")},
                            new() {Name = "name", Type = SearchParamType.String, Documentation = new Markdown("A server defined search that may match any of the string fields in the HumanName, including family, give, prefix, suffix, suffix, and/or text")},
                            new() {Name = "family", Type = SearchParamType.String},
                            new() {Name = "given", Type = SearchParamType.String},
                        },
                    })
                    .WithResource(() => new ResourceComponent
                    {
                        Type = ModelInfo.ResourceTypeToFhirTypeName(ResourceType.DocumentReference),
                        Profile = "http://hl7.no/fhir/StructureDefinition/no-helseapi-DocumentReference",
                        Interaction = new List<ResourceInteractionComponent>
                        {
                            new() {Code = TypeRestfulInteraction.Create},
                            new() {Code = TypeRestfulInteraction.Read},
                            new() {Code = TypeRestfulInteraction.SearchType},
                        },
                        SearchParam = new List<SearchParamComponent>
                        {
                            new() {Name = "patient", Type = SearchParamType.Reference, Documentation = new Markdown("The Person links to this Patient")},
                            new() {Name = "type", Type = SearchParamType.Token, Documentation = new Markdown("Kind of document")},
                        },
                    })
                    .WithInteraction(SystemRestfulInteraction.Transaction)
                    .Build()
            )
            .Build();

        Assert.Equal(1, capabilityStatement.Rest?.Count);
        Assert.Equal(3, capabilityStatement.Rest?.FirstOrDefault()?.Resource.Count);
        Assert.Equal(3, capabilityStatement.Rest?.FirstOrDefault()?.Resource.Count);
        Assert.Equal(5, capabilityStatement.Rest?.FirstOrDefault()?.Resource.Find(rest => rest.Type == ModelInfo.ResourceTypeToFhirTypeName(ResourceType.Patient))?.SearchParam?.Count);
        Assert.NotNull(capabilityStatement.Rest?.FirstOrDefault()?.Resource.Find(rest => rest.Type == ModelInfo.ResourceTypeToFhirTypeName(ResourceType.DocumentReference))?.Interaction.Find(interaction => interaction.Code == TypeRestfulInteraction.Create));
    }
}