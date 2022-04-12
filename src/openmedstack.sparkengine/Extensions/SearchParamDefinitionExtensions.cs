﻿// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

using static Hl7.Fhir.Model.ModelInfo;

namespace OpenMedStack.SparkEngine.Extensions
{
    using System.Collections.Generic;
    using Hl7.Fhir.Model;

    internal static class SearchParamDefinitionExtensions
    {
        /// <summary>
        ///     Returns true if the search parameter is one of the following types: Number, Date or Quantity.
        ///     See https://www.hl7.org/fhir/stu3/search.html#prefix for more information.
        /// </summary>
        /// <param name="searchParamDefinitions">
        ///     A List of <see cref="SearchParamDefinition" />, since this is an extension this is usually a reference
        ///     to ModelInfo.SearchParameters.
        /// </param>
        /// <param name="resourceType"></param>
        /// <param name="name">A <see cref="string" /> representing the name of the search parameter.</param>
        /// <returns>Returns true if the search parameter is of type Number, Date or Quantity, otherwise false.</returns>
        internal static bool CanHaveOperatorPrefix(
            this List<SearchParamDefinition> searchParamDefinitions,
            string? resourceType,
            string name)
        {
            var searchParamDefinition = searchParamDefinitions.Find(
                p => (p.Resource == resourceType || p.Resource == nameof(Resource)) && p.Name == name);
            return searchParamDefinition != null
                   && (searchParamDefinition.Type == SearchParamType.Number
                       || searchParamDefinition.Type == SearchParamType.Date
                       || searchParamDefinition.Type == SearchParamType.Quantity);
        }
    }
}