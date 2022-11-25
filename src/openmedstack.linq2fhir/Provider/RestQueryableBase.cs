// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RestQueryableBase.cs" company="Reimers.dk">
//   Copyright © Reimers.dk 2014-2021
//   This source is subject to the Microsoft Public License (Ms-PL).
//   Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//   All other rights reserved.
// </copyright>
// <summary>
//   Defines the RestQueryableBase type.
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

    internal abstract class RestQueryableBase<T> : IFhirQueryable<T> where T : Resource
    {
        protected RestQueryableBase(FhirClient client, Expression? expression = null)
        {
            Client = client;
            Expression = expression ?? Expression.Constant(this);
        }

        /// <summary>
        /// 	<see cref="Type"/> of T in IQueryable of T.
        /// </summary>
        public Type ElementType
        {
            get { return typeof(T); }
        }

        /// <summary>
        /// 	The expression tree.
        /// </summary>
        public Expression Expression { get; }

        /// <summary>
        /// 	IQueryProvider part of RestQueryable.
        /// </summary>
        public IAsyncQueryProvider Provider { get; protected init; } = null!;

        internal FhirClient Client { get; }

        public ValueTask DisposeAsync()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
            return ValueTask.CompletedTask;
        }

        /// <inheritdoc />
        public async IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            var enumerable = await Provider.ExecuteAsync<T>(Expression, cancellationToken);
            yield return enumerable;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Client.Dispose();
            }
        }
    }
}
