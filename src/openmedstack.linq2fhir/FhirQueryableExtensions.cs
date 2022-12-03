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
                Expression.Constant(time, typeof(DateTimeOffset)),
                Expression.Constant(cancellationToken, typeof(CancellationToken))));
    }

    private static MethodInfo? _reverseIncludeMethod;

    private static MethodInfo GetReverseIncludeMethod(Type source, Type include) =>
        (_reverseIncludeMethod ??= new Func<IAsyncQueryable<object>, Expression<Func<object, object>>, IncludeModifier, CancellationToken, IAsyncQueryable<object>>(ReverseInclude).GetMethodInfo().GetGenericMethodDefinition()).MakeGenericMethod(source, include);

    /// <summary>
    /// Includes resources which reference the queried results.
    /// </summary>
    /// <typeparam name="T">The <see cref="Type"/> of <see cref="Fhir.Resource"/> being queried.</typeparam>
    /// <typeparam name="TRevInclude">The <see cref="Type"/> of <see cref="Fhir.Resource"/> to reverse include.</typeparam>
    /// <param name="source">The source <see cref="IAsyncQueryable{T}"/>.</param>
    /// <param name="selector">The selection of what to reverse include.</param>
    /// <param name="modifier">Whether to include references recursively or iteratively.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> for the request.</param>
    /// <returns>An <see cref="IAsyncQueryable{T}"/> instance.</returns>
    public static IAsyncQueryable<T> ReverseInclude<T, TRevInclude>(this IAsyncQueryable<T> source, Expression<Func<TRevInclude, object>> selector, IncludeModifier modifier = IncludeModifier.None, CancellationToken cancellationToken = default)
    {
        return source.Provider.CreateQuery<T>(
            Expression.Call(
                GetReverseIncludeMethod(typeof(T), typeof(TRevInclude)),
                source.Expression,
                selector,
                Expression.Constant(modifier, typeof(IncludeModifier)),
                Expression.Constant(cancellationToken, typeof(CancellationToken))));
    }

    private static MethodInfo? _includeMethod;

    private static MethodInfo GetIncludeMethod(Type source) =>
        (_includeMethod ??= new Func<IAsyncQueryable<object>, Expression<Func<object, object>>, CancellationToken, IAsyncQueryable<object>>(Include).GetMethodInfo().GetGenericMethodDefinition()).MakeGenericMethod(source);

    /// <summary>
    /// Includes resources which reference the queried results.
    /// </summary>
    /// <typeparam name="T">The <see cref="Type"/> of <see cref="Fhir.Resource"/> being queried.</typeparam>
    /// <param name="source">The source <see cref="IAsyncQueryable{T}"/>.</param>
    /// <param name="selector">The selection of what to reverse include.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> for the request.</param>
    /// <returns>An <see cref="IAsyncQueryable{T}"/> instance.</returns>
    public static IAsyncQueryable<T> Include<T>(this IAsyncQueryable<T> source, Expression<Func<T, object>> selector, CancellationToken cancellationToken = default)
    {
        return source.Provider.CreateQuery<T>(
            Expression.Call(
                GetIncludeMethod(typeof(T)),
                source.Expression,
                selector,
                Expression.Constant(cancellationToken, typeof(CancellationToken))));
    }

    private static MethodInfo? _elementMethod;

    private static MethodInfo GetElementsMethod(Type source) =>
        (_elementMethod ??= new Func<IAsyncQueryable<object>, Expression<Func<object, object>>, CancellationToken, IAsyncQueryable<object>>(Elements).GetMethodInfo().GetGenericMethodDefinition()).MakeGenericMethod(source);

    /// <summary>
    /// Selects the elements to include in the response.
    /// </summary>
    /// <typeparam name="T">The <see cref="Type"/> of <see cref="Fhir.Resource"/> being queried.</typeparam>
    /// <param name="source">The source <see cref="IAsyncQueryable{T}"/>.</param>
    /// <param name="selector">The selection of what elements to include.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> for the request.</param>
    /// <returns>An <see cref="IAsyncQueryable{T}"/> instance.</returns>
    public static IAsyncQueryable<T> Elements<T>(this IAsyncQueryable<T> source, Expression<Func<T, object>> selector, CancellationToken cancellationToken = default)
    {
        return source.Provider.CreateQuery<T>(
            Expression.Call(
                GetElementsMethod(typeof(T)),
                source.Expression,
                selector,
                Expression.Constant(cancellationToken, typeof(CancellationToken))));
    }

    /// <summary>
    /// Queries for a reference to a specific resource type.
    /// </summary>
    /// <typeparam name="TResource">The <see cref="Type"/> of <see cref="Fhir.Resource"/> to query for.</typeparam>
    /// <param name="resource">The <see cref="Fhir.ResourceReference"/> being queried.</param>
    /// <returns>A <see cref="Fhir.Resource"/> specific reference.</returns>
    public static TResource ReferringResource<TResource>(this Fhir.Resource resource) where TResource : Fhir.Resource
    {
        return (resource as TResource)!;
    }

    /// <summary>
    /// Treats the data type as an object to match any value.
    /// </summary>
    /// <param name="data">The attribute value to match</param>
    /// <param name="match">The value to match</param>
    /// <returns>The attribute value as an <see cref="object"/>.</returns>
    public static bool MatchAnyAttribute(this Fhir.DataType data, object match)
    {
        return data == match;
    }

    /// <summary>
    /// Treats the data type as an object to match any value.
    /// </summary>
    /// <param name="data">The attribute value to match</param>
    /// <param name="match">The value to match</param>
    /// <returns>The attribute value as an <see cref="object"/>.</returns>
    public static bool DoNotMatchAnyAttribute(this Fhir.DataType data, object match)
    {
        return data != match;
    }
}