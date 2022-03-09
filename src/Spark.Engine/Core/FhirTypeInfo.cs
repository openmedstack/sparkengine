// /*
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
    using System.Linq;

    /// <summary>
    ///     Class with info about a Fhir Type (Resource or Element).
    ///     Works on other types as well, but is not intended for it.
    /// </summary>
    public class FhirTypeInfo
    {
        internal List<FhirPropertyInfo> Properties;
        public string TypeName { get; internal set; }

        public Type FhirType { get; internal set; }

        public IEnumerable<FhirPropertyInfo> FindPropertyInfos(Predicate<FhirPropertyInfo> propertyPredicate)
        {
            return Properties?.Where(pi => propertyPredicate(pi));
        }

        /// <summary>
        ///     Find the first property that matches the <paramref name="propertyPredicate" />.
        ///     Properties that are FhirElements are preferred over properties that are not.
        /// </summary>
        /// <param name="propertyPredicate"></param>
        /// <returns>PropertyInfo for property that matches the predicate. Null if none matches.</returns>
        public FhirPropertyInfo FindPropertyInfo(Predicate<FhirPropertyInfo> propertyPredicate)
        {
            var allMatches = FindPropertyInfos(propertyPredicate);
            IEnumerable<FhirPropertyInfo> preferredMatches;
            if (allMatches != null && allMatches.Count() > 1)
            {
                preferredMatches = allMatches.Where(pi => pi.IsFhirElement);
            }
            else
            {
                preferredMatches = allMatches;
            }

            return preferredMatches?.FirstOrDefault();
        }

        /// <summary>
        ///     Find the first property with the name <paramref name="propertyName" />, or where one of the TypedNames matches
        ///     <paramref name="propertyName" />.
        ///     Properties that are FhirElements are preferred over properties that are not.
        /// </summary>
        /// <param name="propertyName"></param>
        /// <returns>PropertyInfo for property that matches this name.</returns>
        public FhirPropertyInfo FindPropertyInfo(string propertyName)
        {
            var result = FindPropertyInfo(pi => pi.PropertyName == propertyName)
                         ?? FindPropertyInfo(pi => pi.TypedNames.Contains(propertyName));

            return result;
        }
    }
}
