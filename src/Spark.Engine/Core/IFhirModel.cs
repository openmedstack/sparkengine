﻿// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace Spark.Engine.Core
{
    using System;
    using System.Collections.Generic;
    using Hl7.Fhir.Model;
    using Model;

    public interface IFhirModel
    {
        List<SearchParameter> SearchParameters { get; }

        /// <summary>
        ///     "Patient" -> Hl7.Fhir.Model.Patient
        /// </summary>
        /// <param name="name"></param>
        /// <returns>type belonging to the name, if known (otherwise null)</returns>
        Type GetTypeForResourceName(string name);

        /// <summary>
        ///     Hl7.Fhir.Model.Patient -> "Patient"
        /// </summary>
        /// <param name="type"></param>
        /// <returns>name of the type as it is used in the REST interface, if known (otherwise null)</returns>
        string GetResourceNameForType(Type type);

        /// <summary>
        ///     "Patient" -> Hl7.Fhir.Model.ResourceType.Patient
        /// </summary>
        /// <param name="name"></param>
        /// <returns>Enum value of ResourceType matching the name</returns>
        ResourceType GetResourceTypeForResourceName(string name);

        /// <summary>
        ///     Hl7.Fhir.Model.ResourceType.Patient -> "Patient"
        /// </summary>
        /// <param name="type"></param>
        /// <returns>string representation of ResourceType</returns>
        string GetResourceNameForResourceType(ResourceType type);

        IEnumerable<SearchParameter> FindSearchParameters(ResourceType resourceType);

        IEnumerable<SearchParameter> FindSearchParameters(Type resourceType);

        IEnumerable<SearchParameter> FindSearchParameters(string resourceTypeName);

        SearchParameter FindSearchParameter(ResourceType resourceType, string parameterName);

        SearchParameter FindSearchParameter(Type resourceType, string parameterName);

        SearchParameter FindSearchParameter(string resourceTypeName, string parameterName);

        /// <summary>
        ///     Get the string value for an enum as specified in the EnumLiteral attribute.
        /// </summary>
        /// <param name="enumType"></param>
        /// <param name="value"></param>
        /// <returns>String for the enum value if found, otherwise null</returns>
        string GetLiteralForEnum(Enum value);

        CompartmentInfo FindCompartmentInfo(ResourceType resourceType);
        CompartmentInfo FindCompartmentInfo(string resourceType);
    }
}