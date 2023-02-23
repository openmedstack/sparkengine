// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RestQueryProvider.cs" company="Reimers.dk">
//   Copyright © Reimers.dk 2014-2021
//   This source is subject to the Microsoft Public License (Ms-PL).
//   Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//   All other rights reserved.
// </copyright>
// <summary>
//   Defines the RestQueryProvider type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OpenMedStack.Linq2Fhir.Provider;

using System;
using System.Linq;
using System.Reflection;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Expression = System.Linq.Expressions.Expression;

internal abstract class RestQueryProvider<T> : RestQueryProviderBase<T> where T : Resource, new()
{
    public RestQueryProvider(FhirClient client)
    {
        Client = client;
    }

    protected FhirClient Client { get; }

    protected abstract Func<FhirClient, Expression, Type, IAsyncQueryable<TResult>> CreateQueryable<TResult>() where TResult : Resource, new();
        
    public override IAsyncQueryable<TResult> CreateQuery<TResult>(Expression expression)
    {
        if (expression == null)
        {
            throw new ArgumentNullException(nameof(expression));
        }

        var methodInfo = GetType().GetMethod(nameof(CreateQueryable), BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Instance);
        var method = (Func<FhirClient, Expression, Type, IAsyncQueryable<TResult>>)methodInfo!.MakeGenericMethod(typeof(TResult)).Invoke(this, Array.Empty<object>())!;
        return method(Client, expression, typeof(T));
    }
        
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Client.Dispose();
        }
    }
}