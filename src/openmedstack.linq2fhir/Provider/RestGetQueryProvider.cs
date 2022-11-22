// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RestGetQueryProvider.cs" company="Reimers.dk">
//   Copyright © Reimers.dk 2014-2021
//   This source is subject to the Microsoft Public License (Ms-PL).
//   Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//   All other rights reserved.
// </copyright>
// <summary>
//   Defines the RestGetQueryProvider type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OpenMedStack.Linq2Fhir.Provider
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Hl7.Fhir.Model;
    using Hl7.Fhir.Rest;
    using Expression = System.Linq.Expressions.Expression;

    internal class RestGetQueryProvider<T> : RestQueryProvider<T> where T : Resource, new()
    {
        public RestGetQueryProvider(FhirClient client)
            : base(client)
        {
        }

        protected override
            Func<FhirClient, Expression, Type, IAsyncQueryable<TResult>> CreateQueryable<TResult>()
        {
            return InnerCreateQueryable<TResult>;
        }
        
        private static IAsyncQueryable<TResult> InnerCreateQueryable<TResult>(
            FhirClient client,
            Expression expression,
            Type type) where TResult : Resource, new()
        {
            return new RestGetQueryable<TResult>(client, expression);
        }

        /// <inheritdoc />
        protected override Task<Bundle> GetResults(SearchParams builder)
        {
            return Client.SearchAsync<T>(builder);
        }
    }
}
