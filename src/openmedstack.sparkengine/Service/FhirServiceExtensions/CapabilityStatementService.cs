﻿// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace OpenMedStack.SparkEngine.Service.FhirServiceExtensions;

using Hl7.Fhir.Model;
using Interfaces;

public class CapabilityStatementService : ICapabilityStatementService
{
    private readonly ILocalhost _localhost;

    public CapabilityStatementService(ILocalhost localhost) => _localhost = localhost;

    public CapabilityStatement GetSparkCapabilityStatement(string sparkVersion) =>
        CapabilityStatementBuilder.GetSparkCapabilityStatement(sparkVersion, _localhost);
}