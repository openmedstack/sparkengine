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
    using System.Reflection;
    using Extensions;
    using Hl7.Fhir.Introspection;
    using Hl7.Fhir.Model;
    using Hl7.Fhir.Validation;

    /// <summary>
    ///     Singleton class to hold a reference to every property of every type of resource that may be of interest in
    ///     evaluating a search or indexing a resource for search.
    ///     This keeps the buildup of ElementQuery clean and more performing.
    ///     For properties with the attribute FhirElement, the values of that attribute are also cached.
    /// </summary>
    public class FhirPropertyIndex
    {
        private readonly IFhirModel _fhirModel;
        private readonly IEnumerable<FhirTypeInfo> _fhirTypeInfoList;

        /// <summary>
        ///     Build up an index of properties in the <paramref name="supportedFhirTypes" />.
        /// </summary>
        /// <param name="fhirModel">IFhirModel that can provide mapping from resource names to .Net types</param>
        /// <param name="supportedFhirTypes">List of (resource and element) types to be indexed.</param>
        public FhirPropertyIndex(
            IFhirModel fhirModel,
            IEnumerable<Type> supportedFhirTypes) //Hint: supply all Resource and Element types from an assembly
        {
            _fhirModel = fhirModel;
            _fhirTypeInfoList = supportedFhirTypes?.Select(CreateFhirTypeInfo).ToList();
            foreach (var ti in _fhirTypeInfoList)
            {
                ti.Properties = ti.FhirType.GetProperties().Select(CreateFhirPropertyInfo).ToList();
            }
        }

        /// <summary>
        ///     Build up an index of properties in the Resource and Element types in
        ///     <param name="fhirAssembly" />
        ///     .
        /// </summary>
        /// <param name="fhirModel">IFhirModel that can provide mapping from resource names to .Net types</param>
        public FhirPropertyIndex(IFhirModel fhirModel, Assembly fhirAssembly)
            : this(fhirModel, LoadSupportedTypesFromAssembly(fhirAssembly))
        {
        }

        /// <summary>
        ///     Build up an index of properties in the Resource and Element types in Hl7.Fhir.Core.
        /// </summary>
        /// <param name="fhirModel">IFhirModel that can provide mapping from resource names to .Net types</param>
        public FhirPropertyIndex(IFhirModel fhirModel)
            : this(fhirModel, Assembly.GetAssembly(typeof(Resource)))
        {
        }

        private static IEnumerable<Type> LoadSupportedTypesFromAssembly(Assembly fhirAssembly)
        {
            var result = new List<Type>();
            foreach (var fhirType in fhirAssembly.GetTypes())
            {
                if (typeof(Resource).IsAssignableFrom(fhirType) || typeof(Element).IsAssignableFrom(fhirType)
                ) //It is derived of Resource or Element, so we should support it.
                {
                    result.Add(fhirType);
                }
            }

            return result;
        }

        internal FhirTypeInfo FindFhirTypeInfo(Predicate<FhirTypeInfo> typePredicate) =>
            FindFhirTypeInfos(typePredicate)?.FirstOrDefault();

        internal IEnumerable<FhirTypeInfo> FindFhirTypeInfos(Predicate<FhirTypeInfo> typePredicate)
        {
            return _fhirTypeInfoList?.Where(fti => typePredicate(fti));
        }

        /// <summary>
        ///     Find info about the property with the supplied name in the supplied resource.
        ///     Can also be called directly for the Type instead of the resourceTypeName,
        ///     <see cref="FindPropertyInfo(System.Type,string)" />.
        /// </summary>
        /// <param name="resourceTypeName">Name of the resource type that should contain a property with the supplied name.</param>
        /// <param name="propertyName">Name of the property within the resource type.</param>
        /// <returns>FhirPropertyInfo for the specified property. Null if not present.</returns>
        public FhirPropertyInfo FindPropertyInfo(string resourceTypeName, string propertyName)
        {
            return FindFhirTypeInfo(r => r.TypeName == resourceTypeName)?.FindPropertyInfo(propertyName);
        }

        /// <summary>
        ///     Find info about the property with the name <paramref name="propertyName" /> in the resource of type
        ///     <paramref name="fhirType" />.
        ///     Can also be called for the resourceTypeName instead of the Type, <see cref="FindPropertyInfo" />.
        /// </summary>
        /// <param name="fhirType">Type of resource that should contain a property with the supplied name.</param>
        /// <param name="propertyName">Name of the property within the resource type.</param>
        /// <returns><see cref="FhirPropertyInfo" /> for the specified property. Null if not present.</returns>
        public FhirPropertyInfo FindPropertyInfo(Type fhirType, string propertyName)
        {
            FhirPropertyInfo propertyInfo;
            if (fhirType.IsGenericType)
            {
                propertyInfo = FindFhirTypeInfo(r => r.FhirType.Name == fhirType.Name)?.FindPropertyInfo(propertyName);
                if (propertyInfo != null)
                {
                    propertyInfo.PropInfo = fhirType.GetProperty(propertyInfo.PropInfo.Name);
                }
            }
            else
            {
                propertyInfo = FindFhirTypeInfo(r => r.FhirType == fhirType)?.FindPropertyInfo(propertyName);
            }

            return propertyInfo;
        }

        //CK: Function to create FhirTypeInfo instead of putting this knowledge in the FhirTypeInfo constructor, 
        //because I don't want to pass an IFhirModel to all instances of FhirTypeInfo and FhirPropertyInfo.
        private static FhirTypeInfo CreateFhirTypeInfo(Type fhirType)
        {
            if (fhirType == null)
            {
                return null;
            }

            var result = new FhirTypeInfo {FhirType = fhirType, TypeName = fhirType.Name};
            var attFhirType = fhirType.GetCustomAttribute<FhirTypeAttribute>(false);
            if (attFhirType != null)
            {
                result.TypeName = attFhirType.Name;
            }

            return result;
        }

        private FhirPropertyInfo CreateFhirPropertyInfo(PropertyInfo prop)
        {
            var result = new FhirPropertyInfo
            {
                PropertyName = prop.Name, PropInfo = prop, AllowedTypes = new List<Type>()
            };

            ExtractDataChoiceTypes(prop, result);

            ExtractReferenceTypes(prop, result);

            if (!result.AllowedTypes.Any())
            {
                result.AllowedTypes.Add(prop.PropertyType);
            }

            result.TypedNames = result.AllowedTypes.Select(
                    at => result.PropertyName + FindFhirTypeInfo(fti => fti.FhirType == at)?.TypeName.FirstUpper())
                .ToList();

            return result;
        }

        private void ExtractReferenceTypes(MemberInfo prop, FhirPropertyInfo target)
        {
            var attReferenceAttribute = prop.GetCustomAttribute<ReferencesAttribute>(false);
            if (attReferenceAttribute == null)
            {
                return;
            }

            target.IsReference = true;
            target.AllowedTypes.AddRange(
                attReferenceAttribute.Resources.Select(r => _fhirModel.GetTypeForResourceName(r))
                    .Where(at => at != null));
        }

        private static void ExtractDataChoiceTypes(PropertyInfo prop, FhirPropertyInfo target)
        {
            var attFhirElement = prop.GetCustomAttribute<FhirElementAttribute>(false);
            if (attFhirElement != null)
            {
                target.PropertyName = attFhirElement.Name;
                target.IsFhirElement = true;
                if (attFhirElement.Choice == ChoiceType.DatatypeChoice
                    || attFhirElement.Choice == ChoiceType.ResourceChoice)
                {
                    var attChoiceAttribute = prop.GetCustomAttribute<AllowedTypesAttribute>(false);
                    //CK: Nasty workaround because Element.Value is specified wit AllowedTypes(Element) instead of the list of exact types.
                    //TODO: Solve this, preferably in the Hl7.Api
                    if (prop.DeclaringType == typeof(Extension) && prop.Name == "Value")
                    {
                        target.AllowedTypes.AddRange(
                            new List<Type>
                            {
                                typeof(Integer),
                                typeof(FhirDecimal),
                                typeof(FhirDateTime),
                                typeof(Date),
                                typeof(Instant),
                                typeof(FhirString),
                                typeof(FhirUri),
                                typeof(FhirBoolean),
                                typeof(Code),
                                typeof(Markdown),
                                typeof(Base64Binary),
                                typeof(Coding),
                                typeof(CodeableConcept),
                                typeof(Attachment),
                                typeof(Identifier),
                                typeof(Quantity),
                                typeof(Hl7.Fhir.Model.Range),
                                typeof(Period),
                                typeof(Ratio),
                                typeof(HumanName),
                                typeof(Address),
                                typeof(ContactPoint),
                                typeof(Timing),
                                typeof(Signature),
                                typeof(ResourceReference)
                            });
                    }
                    else if (attChoiceAttribute != null)
                    {
                        target.AllowedTypes.AddRange(attChoiceAttribute.Types);
                    }
                }
            }
        }
    }
}
