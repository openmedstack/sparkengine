// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PredicateConverter.cs" company="Reimers.dk">
//   Copyright © Reimers.dk 2014-2021
//   This source is subject to the Microsoft Public License (Ms-PL).
//   Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//   All other rights reserved.
// </copyright>
// <summary>
//   Defines the PredicateConverter type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OpenMedStack.Linq2Fhir
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;

    internal class PredicateConverter<TSource, TResult> : IPredicateConverter
	{
        public Type SourceType => typeof(TSource);

        public Type TargetType => typeof(TResult);

        public IDictionary<MemberInfo, MemberInfo> Substitutions { get; } = new Dictionary<MemberInfo, MemberInfo>();
    }
}