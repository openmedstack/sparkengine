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
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Threading;
    using System.Threading.Tasks;
    using Hl7.Fhir.Rest;

    internal class RestQueryableBase<T> : IOrderedAsyncQueryable<T>, IDisposable
    {
        protected RestQueryableBase(FhirClient client)
        {
            Client = client;
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
        public Expression Expression { get; protected init; }

        /// <summary>
        /// 	IQueryProvider part of RestQueryable.
        /// </summary>
        public IAsyncQueryProvider Provider { get; protected init; } = null!;

        internal FhirClient Client { get; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc />
        public async IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = new CancellationToken())
        {
            var enumerable = await Provider.ExecuteAsync<T>(Expression, cancellationToken);
            //foreach (var item in enumerable)
            //{
                yield return enumerable;
            //}
        }
        
        //IAsyncEnumerator IAsyncEnumerable.GetAsyncEnumerator()
        //{
        //    return Provider.ExecuteAsync<IEnumerable>(Expression).GetAsyncEnumerator();
        //}

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Client.Dispose();
            }
        }
    }
}
