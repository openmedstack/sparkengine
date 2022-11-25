//// --------------------------------------------------------------------------------------------------------------------
//// <copyright file="ModelFilter.cs" company="Reimers.dk">
////   Copyright © Reimers.dk 2014-2021
////   This source is subject to the Microsoft Public License (Ms-PL).
////   Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
////   All other rights reserved.
//// </copyright>
//// <summary>
////   Defines the ModelFilter type.
//// </summary>
//// --------------------------------------------------------------------------------------------------------------------

//namespace OpenMedStack.Linq2Fhir
//{
//    using System;
//    using System.Collections.Generic;
//    using System.Linq;
//    using System.Linq.Expressions;
//    using Hl7.Fhir.Rest;

//    internal class ModelFilter<T> : IModelFilter<T>
//    {
//        private readonly Expression<Func<T, object>> _selectExpression;

//        public ModelFilter(Expression<Func<T, bool>> filterExpression, Expression<Func<T, object>> selectExpression, IEnumerable<(SortOrder, Expression)>? sortDescriptions = null, int skip = 0, int top = -1)
//        {
//            SkipCount = skip;
//            TakeCount = top;
//            FilterExpression = filterExpression;
//            _selectExpression = selectExpression;
//            SortDescriptions = sortDescriptions ?? Enumerable.Empty<(SortOrder, Expression)>();
//        }

//        /// <summary>
//        /// Gets the amount of items to take.
//        /// </summary>
//        public int TakeCount { get; }

//        /// <summary>
//		/// Gets the filter expression.
//		/// </summary>
//		public Expression<Func<T, bool>> FilterExpression { get; }

//        /// <summary>
//		/// Gets the amount of items to skip.
//		/// </summary>
//		public int SkipCount { get; }

//        /// <summary>
//		/// Gets the sort descriptions for the sequence.
//		/// </summary>
//		public IEnumerable<(SortOrder, Expression)> SortDescriptions { get; }

//        public IQueryable<object> Filter(IEnumerable<T> model)
//        {
//            var result = model.AsQueryable().Where(FilterExpression);

//            if (SortDescriptions.Any())
//            {
//                var isFirst = true;
//                foreach (var sortDescription in SortDescriptions)
//                {
//                    if (isFirst)
//                    {
//                        isFirst = false;
//                        result = sortDescription.Item1 == SortOrder.Ascending
//                            ? result.OrderBy(sortDescription.Item2)
//                            : result.OrderByDescending(sortDescription.Item2);
//                    }
//                    else
//                    {
//                        var orderedEnumerable = (result as IOrderedQueryable<T>)!;

//                        result = sortDescription.Item1 == SortOrder.Ascending
//                                    ? orderedEnumerable.ThenBy(sortDescription.Item2)
//                                    : orderedEnumerable.ThenByDescending(sortDescription.Item2);
//                    }
//                }
//            }

//            if (SkipCount > 0)
//            {
//                result = result.Skip(SkipCount);
//            }

//            if (TakeCount > -1)
//            {
//                result = result.Take(TakeCount);
//            }

//            return new UntypedQueryable<T>(result, _selectExpression);
//        }
//    }
//}