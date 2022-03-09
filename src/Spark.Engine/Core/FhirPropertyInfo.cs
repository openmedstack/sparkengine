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
    using System.Reflection;

    /// <summary>
    ///     Class with info about properties in Fhir Types (Resource or Element)
    /// </summary>
    public class FhirPropertyInfo
    {
        /// <summary>
        ///     Name of the property, either drawn from FhirElementAttribute.Name or PropertyInfo.Name (in that order).
        /// </summary>
        public string PropertyName { get; internal set; }

        /// <summary>
        ///     True if the property has the FhirElementAttribute.
        /// </summary>
        public bool IsFhirElement { get; internal set; }

        /// <summary>
        ///     Some elements are multi-typed.
        ///     This is the list of types that this property may contain, or refer to (in case of <see cref="IsReference" /> =
        ///     true).
        ///     Contains at least 1 type.
        /// </summary>
        public List<Type> AllowedTypes { get; internal set; }

        /// <summary>
        ///     True if the property has the ResourceReferenceAttribute.
        /// </summary>
        public bool IsReference { get; internal set; }

        public IEnumerable<string> TypedNames { get; internal set; }

        /// <summary>
        /// A path in a searchparameter denotes a specific type, as propertyname + Typename, e.g. ClinicalImpression.triggerReference.
        /// (ClinicalImpression.trigger can also be a CodeableConcept.)
        /// Use this property to find this ResourcePropertyInfo by this typed name.
        /// </summary>
        //public IEnumerable<string> TypedNames
        //{
        //    get
        //    {
        //        return AllowedTypes.Select(t => PropertyName + t.Name);
        //    }
        //}

        /// <summary>
        ///     Normal .Net PropertyInfo for this property.
        /// </summary>
        public PropertyInfo PropInfo { get; internal set; }
    }
}