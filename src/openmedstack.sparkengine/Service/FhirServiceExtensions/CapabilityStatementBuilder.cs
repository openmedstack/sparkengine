// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace OpenMedStack.SparkEngine.Service.FhirServiceExtensions
{
    using System;
    using System.Linq;
    using Core;
    using Hl7.Fhir.Model;
    using Hl7.Fhir.Utility;

    public static class CapabilityStatementBuilder
    {
        public static CapabilityStatement GetSparkCapabilityStatement(string sparkVersion, ILocalhost localhost)
        {
            var vsn = FHIRVersion.N4_0_1;
            var capabilityStatement = CreateServer("Spark", sparkVersion, "Incendi", fhirVersion: vsn);

            capabilityStatement.AddAllCoreResources(
                true,
                true,
                CapabilityStatement.ResourceVersionPolicy.VersionedUpdate);
            capabilityStatement.AddAllSystemInteractions()
                .AddAllInteractionsForAllResources()
                .AddCoreSearchParamsAllResources();
            capabilityStatement.AddSummaryForAllResources();
            capabilityStatement.AddOperation(
                "Fetch Patient Record",
                localhost.Absolute(new Uri("OperationDefinition/Patient-everything", UriKind.Relative)).ToString());
            capabilityStatement.AddOperation(
                "Generate a Document",
                localhost.Absolute(new Uri("OperationDefinition/Composition-document", UriKind.Relative)).ToString());
            //capabilityStatement.AcceptUnknown = CapabilityStatement.UnknownContentCode.Both;
            capabilityStatement.Experimental = true;
            capabilityStatement.Kind = CapabilityStatementKind.Capability;
            capabilityStatement.Format = new[] { "xml", "json" };
            capabilityStatement.Description = new Markdown("This FHIR SERVER is a reference Implementation server built in C# on HL7.Fhir.Core (nuget) by Firely, Incendi and others");

            return capabilityStatement;
        }

        public static CapabilityStatement CreateServer(
            string server,
            string serverVersion,
            string publisher,
            FHIRVersion fhirVersion)
        {
            var capabilityStatement = new CapabilityStatement
            {
                Name = server,
                Publisher = publisher,
                Version = serverVersion,
                FhirVersion = fhirVersion,
                //capabilityStatement.AcceptUnknown = CapabilityStatement.UnknownContentCode.No;
                Date = Date.Today().Value
            };
            capabilityStatement.AddServer();
            return capabilityStatement;
        }

        public static CapabilityStatement.RestComponent AddRestComponent(
            this CapabilityStatement capabilityStatement,
            bool isServer,
            Markdown? documentation = null)
        {
            var server = new CapabilityStatement.RestComponent
            {
                Mode = isServer
                    ? CapabilityStatement.RestfulCapabilityMode.Server
                    : CapabilityStatement.RestfulCapabilityMode.Client
            };

            if (documentation != null)
            {
                server.Documentation = documentation;
            }

            capabilityStatement.Rest.Add(server);
            return server;
        }

        public static CapabilityStatement AddServer(this CapabilityStatement capabilityStatement)
        {
            capabilityStatement.AddRestComponent(true);
            return capabilityStatement;
        }

        public static CapabilityStatement.RestComponent Server(this CapabilityStatement capabilityStatement)
        {
            var server =
                capabilityStatement.Rest.FirstOrDefault(
                    r => r.Mode == CapabilityStatement.RestfulCapabilityMode.Server);
            return server ?? capabilityStatement.AddRestComponent(true);
        }

        public static CapabilityStatement.RestComponent? Rest(this CapabilityStatement capabilityStatement) =>
            capabilityStatement.Rest.FirstOrDefault();

        public static CapabilityStatement AddAllCoreResources(
            this CapabilityStatement capabilityStatement,
            bool readhistory,
            bool updatecreate,
            CapabilityStatement.ResourceVersionPolicy versioning)
        {
            foreach (var resource in ModelInfo.SupportedResources)
            {
                capabilityStatement.AddSingleResourceComponent(resource, readhistory, updatecreate, versioning);
            }

            return capabilityStatement;
        }

        public static CapabilityStatement AddSingleResourceComponent(
            this CapabilityStatement capabilityStatement,
            string resourcetype,
            bool readhistory,
            bool updatecreate,
            CapabilityStatement.ResourceVersionPolicy versioning,
            Canonical? profile = null)
        {
            var resource = new CapabilityStatement.ResourceComponent
            {
                Type = EnumUtility.ParseLiteral<ResourceType>(resourcetype),
                Profile = profile ?? new Canonical(),
                ReadHistory = readhistory,
                UpdateCreate = updatecreate,
                Versioning = versioning
            };
            capabilityStatement.Server().Resource.Add(resource);
            return capabilityStatement;
        }

        public static CapabilityStatement AddSummaryForAllResources(this CapabilityStatement capabilityStatement)
        {
            foreach (var resource in capabilityStatement.Rest.First().Resource)
            {
                var p = new CapabilityStatement.SearchParamComponent
                {
                    Name = "_summary",
                    Type = SearchParamType.String,
                    Documentation = new Markdown("Summary for resource")
                };
                resource.SearchParam.Add(p);
            }

            return capabilityStatement;
        }

        public static CapabilityStatement AddCoreSearchParamsAllResources(this CapabilityStatement capabilityStatement)
        {
            foreach (var r in capabilityStatement.Rest.First().Resource)
            {
                var restComponent = capabilityStatement.Rest();
                if (restComponent != null)
                {
                    restComponent.Resource.Remove(r);
                    restComponent.Resource.Add(AddCoreSearchParamsResource(r));
                }
            }

            return capabilityStatement;
        }


        public static CapabilityStatement.ResourceComponent AddCoreSearchParamsResource(
            CapabilityStatement.ResourceComponent resourcecomp)
        {
            var parameters = ModelInfo.SearchParameters.Where(sp => sp.Resource == resourcecomp.Type!.GetLiteral())
                .Select(
                    sp => new CapabilityStatement.SearchParamComponent
                    {
                        Name = sp.Name,
                        Type = sp.Type,
                        Documentation = sp.Description
                    });

            resourcecomp.SearchParam.AddRange(parameters);
            return resourcecomp;
        }

        public static CapabilityStatement AddAllInteractionsForAllResources(
            this CapabilityStatement capabilityStatement)
        {
            foreach (var r in capabilityStatement.Rest.First().Resource)
            {
                var restComponent = capabilityStatement.Rest();
                if (restComponent != null)
                {
                    restComponent.Resource.Remove(r);
                    restComponent.Resource.Add(AddAllResourceInteractions(r));
                }
            }

            return capabilityStatement;
        }

        public static CapabilityStatement.ResourceComponent AddAllResourceInteractions(
            CapabilityStatement.ResourceComponent resourcecomp)
        {
            foreach (CapabilityStatement.TypeRestfulInteraction type in Enum.GetValues(
                typeof(CapabilityStatement.TypeRestfulInteraction)))
            {
                var interaction = AddSingleResourceInteraction(type);
                resourcecomp.Interaction.Add(interaction);
            }

            return resourcecomp;
        }

        public static CapabilityStatement.ResourceInteractionComponent AddSingleResourceInteraction(
            CapabilityStatement.TypeRestfulInteraction type)
        {
            var interaction = new CapabilityStatement.ResourceInteractionComponent { Code = type };
            return interaction;
        }

        public static CapabilityStatement AddAllSystemInteractions(this CapabilityStatement capabilityStatement)
        {
            foreach (CapabilityStatement.SystemRestfulInteraction code in Enum.GetValues(
                typeof(CapabilityStatement.SystemRestfulInteraction)))
            {
                capabilityStatement.AddSystemInteraction(code);
            }

            return capabilityStatement;
        }

        public static void AddSystemInteraction(
            this CapabilityStatement capabilityStatement,
            CapabilityStatement.SystemRestfulInteraction code)
        {
            var restComponent = capabilityStatement.Rest();
            if (restComponent != null)
            {
                restComponent.Interaction.Add(new CapabilityStatement.SystemInteractionComponent { Code = code });
            }
        }

        public static void AddOperation(this CapabilityStatement capabilityStatement, string name, string definition)
        {
            var operation = new CapabilityStatement.OperationComponent { Name = name, Definition = definition };

            capabilityStatement.Server().Operation.Add(operation);
        }
    }
}