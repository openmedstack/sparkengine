// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RestQueryProviderBase.cs" company="Reimers.dk">
//   Copyright © Reimers.dk 2014-2021
//   This source is subject to the Microsoft Public License (Ms-PL).
//   Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//   All other rights reserved.
// </copyright>
// <summary>
//   Defines the RestQueryProviderBase type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OpenMedStack.Linq2Fhir.Provider
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Hl7.Fhir.Model;
    using Hl7.Fhir.Rest;
    using Expression = System.Linq.Expressions.Expression;

    internal abstract class RestQueryProviderBase<T> : IAsyncQueryProvider, IDisposable where T : Resource, new()
    {
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public abstract IAsyncQueryable<TElement> CreateQuery<TElement>(Expression expression);

        protected abstract Task<Bundle?> GetResults(SearchParams builder);

        /// <inheritdoc />
        public async ValueTask<TResult> ExecuteAsync<TResult>(Expression expression, CancellationToken token)
        {
            var visitor = new QueryExpressionVisitor();
            visitor.Visit(expression);
            var p = visitor.GetParams();
            var bundle = await GetResults(p);
            if (typeof(TResult) == typeof(Bundle))
            {
                return (TResult)(bundle as object)!;
            }
            var enumerable = bundle.GetResources().OfType<T>();
            object? o = typeof(TResult) switch
            {
                not null when typeof(TResult) == typeof(T) && Nullable.GetUnderlyingType(typeof(TResult)) != null => enumerable.FirstOrDefault(),
                not null when typeof(TResult) == typeof(T) && Nullable.GetUnderlyingType(typeof(TResult)) == null => enumerable.First(),
                not null when typeof(TResult).IsAssignableTo(typeof(List<T>)) => enumerable.ToList(),
                not null when typeof(TResult).IsAssignableTo(typeof(T[])) => enumerable.ToArray(),
                not null when typeof(TResult).IsAssignableTo(typeof(IEnumerable<T>)) => enumerable.AsEnumerable(),
                _ => throw new Exception($"Unexpected type {nameof(TResult)}")
            };
            return (TResult)o!;
        }

        protected abstract void Dispose(bool disposing);
    }
}