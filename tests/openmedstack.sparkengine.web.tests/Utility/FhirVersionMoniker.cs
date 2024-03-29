﻿// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace OpenMedStack.SparkEngine.Web.Tests.Utility;

using Hl7.Fhir.Utility;

public enum FhirVersionMoniker
{
    [EnumLiteral("")] None = 0,

    [EnumLiteral(FhirVersionUtility.VERSION_R2)]
    R2 = 2,

    [EnumLiteral(FhirVersionUtility.VERSION_R3)]
    R3 = 3,

    [EnumLiteral(FhirVersionUtility.VERSION_R4)]
    R4 = 4,

    [EnumLiteral(FhirVersionUtility.VERSION_R5)]
    R5 = 5
}