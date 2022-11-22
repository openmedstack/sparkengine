// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IModelFilter.cs" company="Reimers.dk">
//   Copyright © Reimers.dk 2014-2021
//   This source is subject to the Microsoft Public License (Ms-PL).
//   Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//   All other rights reserved.
// </copyright>
// <summary>
//   Defines the public interface for a model filter.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OpenMedStack.Linq2Fhir
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using Parser;

    /// <summary>
    /// Defines the public interface for a model filter.
    /// </summary>
    /// <typeparam name="T">The <see cref="Type"/> of item to filter.</typeparam>
    public interface IModelFilter<T>
    {
        /// <summary>
        /// Filters the passed collection with the defined filter.
        /// </summary>
        /// <param name="source">The source items to filter.</param>
        /// <returns>A filtered enumeration and projected of the source items.</returns>
        IQueryable<object> Filter(IEnumerable<T> source);
    }
}
