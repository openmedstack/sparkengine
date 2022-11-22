// --------------------------------------------------------------------------------------------------------------------
// <copyright file="GeneralExtensions.cs" company="Reimers.dk">
//   Copyright © Reimers.dk 2014-2021
//   This source is subject to the Microsoft Public License (Ms-PL).
//   Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//   All other rights reserved.
// </copyright>
// <summary>
//   Defines the GeneralExtensions type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OpenMedStack.Linq2Fhir
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Text;
    using Expression = System.Linq.Expressions.Expression;

    internal static class GeneralExtensions
    {
        private static readonly ConcurrentDictionary<Type, bool> KnownAnonymousTypes = new();

        public static bool IsAnonymousType(this Type type)
        {
            return KnownAnonymousTypes.GetOrAdd(
                type,
                t => Attribute.IsDefined(t, typeof(CompilerGeneratedAttribute), false)
                        && t.IsGenericType
                        && t.Name.Contains("AnonymousType") && (t.Name.StartsWith("<>") || t.Name.StartsWith("VB$"))
                        && (t.Attributes & TypeAttributes.NotPublic) == TypeAttributes.NotPublic);
        }

        public static string Capitalize(this string input)
        {
            return char.ToUpperInvariant(input[0]) + input[1..];
        }

        public static Stream ToStream(this string input)
        {
            return new MemoryStream(Encoding.UTF8.GetBytes(input ?? string.Empty));
        }

        public static IEnumerable<T> Replace<T>(this IEnumerable<T> items, Func<T, bool> predicate, T replacement)
        {
            return items.Select(item => predicate(item) ? replacement : item);
        }
        
        public static IOrderedQueryable<T> OrderBy<T>(this IQueryable<T> source, Expression keySelector)
        {
            var propertyType = keySelector.GetType().GetGenericArguments()[0].GetGenericArguments()[1];
            var orderbyMethod = typeof(Queryable).GetMethods(BindingFlags.Public | BindingFlags.Static).FirstOrDefault(x => x.Name == "OrderBy" && x.GetParameters().Length == 2);

            orderbyMethod = orderbyMethod.MakeGenericMethod(typeof(T), propertyType);

            return (IOrderedQueryable<T>)orderbyMethod.Invoke(null, new object[] { source, keySelector });
        }

        public static IOrderedQueryable<T> OrderByDescending<T>(this IQueryable<T> source, Expression keySelector)
        {
            var propertyType = keySelector.GetType().GetGenericArguments()[0].GetGenericArguments()[1];
            var orderbyMethod = typeof(Queryable).GetMethods(BindingFlags.Public | BindingFlags.Static).FirstOrDefault(x => x.Name == "OrderByDescending" && x.GetParameters().Length == 2);

            orderbyMethod = orderbyMethod.MakeGenericMethod(typeof(T), propertyType);

            return (IOrderedQueryable<T>)orderbyMethod.Invoke(null, new object[] { source, keySelector });
        }

        public static IOrderedQueryable<T> ThenBy<T>(this IOrderedQueryable<T> source, Expression keySelector)
        {
            var propertyType = keySelector.GetType().GetGenericArguments()[0].GetGenericArguments()[1];
            var orderbyMethod = typeof(Queryable).GetMethods(BindingFlags.Public | BindingFlags.Static).FirstOrDefault(x => x.Name == "ThenBy" && x.GetParameters().Length == 2);

            orderbyMethod = orderbyMethod.MakeGenericMethod(typeof(T), propertyType);

            return (IOrderedQueryable<T>)orderbyMethod.Invoke(null, new object[] { source, keySelector });
        }

        public static IOrderedQueryable<T> ThenByDescending<T>(this IOrderedQueryable<T> source, Expression keySelector)
        {
            var propertyType = keySelector.GetType().GetGenericArguments()[0].GetGenericArguments()[1];
            var orderbyMethod = typeof(Queryable).GetMethods(BindingFlags.Public | BindingFlags.Static).FirstOrDefault(x => x.Name == "ThenByDescending" && x.GetParameters().Length == 2);

            orderbyMethod = orderbyMethod.MakeGenericMethod(typeof(T), propertyType);

            return (IOrderedQueryable<T>)orderbyMethod.Invoke(null, new object[] { source, keySelector });
        }

        private static Type GetMemberType(MemberInfo member)
        {
            return member.MemberType switch
            {
                MemberTypes.Field => ((FieldInfo)member).FieldType,
                MemberTypes.Property => ((PropertyInfo)member).PropertyType,
                MemberTypes.Method => ((MethodInfo)member).ReturnType,
                _ => throw new InvalidOperationException(member.MemberType + " is not resolvable")
            };
        }
    }
}