// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ModelFilterExtensions.cs" company="Reimers.dk">
//   Copyright © Reimers.dk 2014-2021
//   This source is subject to the Microsoft Public License (Ms-PL).
//   Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//   All other rights reserved.
// </copyright>
// <summary>
//   Defines extension methods for model filters.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OpenMedStack.Linq2Fhir
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Hl7.Fhir.Model;
    using Hl7.Fhir.Rest;
    using Parser;
    using Expression = System.Linq.Expressions.Expression;

    /// <summary>
    /// Defines extension methods for model filters.
    /// </summary>
    public static class ModelFilterExtensions
    {
        /// <summary>
        /// Filters the source collection using the passed query parameters.
        /// </summary>
        /// <param name="source">The source items to filter.</param>
        /// <param name="query">The query parameters defining the filter.</param>
        /// <typeparam name="T">The <see cref="Type"/> of items in the source collection.</typeparam>
        /// <returns>A filtered and projected enumeration of the source collection.</returns>
        public static IQueryable<object> Filter<T>(this IEnumerable<T> source, string query) where T : Resource
        {
            var tokenizer = new PrecedenceBasedRegexTokenizer();
            var tokens = tokenizer.Tokenize(query).ToArray();
            var parser = new Parser.Parser();
            var filter = new ModelFilter<T>(parser.Parse<T>(tokens), x => x!, Enumerable.Empty<(SortOrder, Expression)>());
            return Filter(source, filter);
        }

        /// <summary>
        /// Filters the source collection using the passed query parameters.
        /// </summary>
        /// <param name="source">The source items to filter.</param>
        /// <param name="filter">The filter to apply.</param>
        /// <typeparam name="T">The <see cref="Type"/> of items in the source collection.</typeparam>
        /// <returns>A filtered and projected enumeration of the source collection.</returns>
        public static IQueryable<object> Filter<T>(this IEnumerable<T> source, IModelFilter<T> filter)
        {
            return filter.Filter(source);
        }
    }
}
