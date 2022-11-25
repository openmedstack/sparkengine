// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RestGetQueryable.cs" company="Reimers.dk">
//   Copyright © Reimers.dk 2014-2021
//   This source is subject to the Microsoft Public License (Ms-PL).
//   Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//   All other rights reserved.
// </copyright>
// <summary>
//   Defines the RestGetQueryable type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OpenMedStack.Linq2Fhir.Provider
{
    using Hl7.Fhir.Model;
    using Hl7.Fhir.Rest;
    using Expression = System.Linq.Expressions.Expression;

    internal class RestGetQueryable<T> : RestQueryableBase<T> where T : Resource, new()
    {
        public RestGetQueryable(FhirClient client)
            : base(client)
        {
            Provider = new RestGetQueryProvider<T>(client);
        }

        public RestGetQueryable(FhirClient client, Expression expression)
            : base(client, expression)
        {
            Provider = new RestGetQueryProvider<T>(client);
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing)
                {
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }
    }
}
