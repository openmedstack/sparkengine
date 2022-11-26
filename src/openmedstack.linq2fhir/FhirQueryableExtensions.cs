namespace OpenMedStack.Linq2Fhir;

using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Hl7.Fhir.Rest;
using Provider;
using Fhir = Hl7.Fhir.Model;

/// <summary>
/// Defines the FHIR specific query extension methods.
/// </summary>
public static class FhirQueryableExtensions
{
    /// <summary>
    /// Creates an <see cref="IAsyncQueryable{T}"/> to generate searches using the <see cref="FhirClient"/>.
    /// </summary>
    /// <typeparam name="T">The <see cref="Type"/> of <see cref="Fhir.Resource"/> being queried.</typeparam>
    /// <param name="client">The <see cref="FhirClient"/> to use to execute the query.</param>
    /// <returns></returns>
    public static IAsyncQueryable<T> Query<T>(this FhirClient client)
        where T : Fhir.Resource, new()
    {
        return new RestGetQueryable<T>(client);
    }

    /// <summary>
    /// Gets the requested resources as a <see cref="Fhir.Bundle"/>.
    /// </summary>
    /// <typeparam name="T">The <see cref="Type"/> of <see cref="Fhir.Resource"/> being queried.</typeparam>
    /// <param name="queryable">The source <see cref="IAsyncQueryable{T}"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> for the request.</param>
    /// <returns></returns>
    public static ValueTask<Fhir.Bundle> GetBundle<T>(this IAsyncQueryable<T> queryable, CancellationToken cancellationToken = default)
    {
        return queryable.Provider.ExecuteAsync<Fhir.Bundle>(queryable.Expression, cancellationToken);
    }

    private static MethodInfo? _updatedSinceMethod;

    private static MethodInfo GetUpdatedSinceMethod(Type source) =>
        (_updatedSinceMethod ??= new Func<IAsyncQueryable<object>, DateTimeOffset, CancellationToken, IAsyncQueryable<object>>(UpdatedSince).GetMethodInfo().GetGenericMethodDefinition()).MakeGenericMethod(source);

    /// <summary>
    /// Queries for resources updated after the given <see cref="DateTimeOffset"/>.
    /// </summary>
    /// <typeparam name="T">The <see cref="Type"/> of <see cref="Fhir.Resource"/> being queried.</typeparam>
    /// <param name="queryable">The source <see cref="IAsyncQueryable{T}"/>.</param>
    /// <param name="time">The update time limit.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> for the request.</param>
    /// <returns>An <see cref="IAsyncQueryable{T}"/> instance.</returns>
    public static IAsyncQueryable<T> UpdatedSince<T>(
        this IAsyncQueryable<T> queryable,
        DateTimeOffset time,
        CancellationToken cancellationToken = default)
    {
        return queryable.Provider.CreateQuery<T>(
            Expression.Call(
                GetUpdatedSinceMethod(typeof(T)),
                queryable.Expression,
                Expression.Constant(time, typeof(DateTimeOffset))));
    }
}