// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UntypedQueryable.cs" company="Reimers.dk">
//   Copyright © Reimers.dk 2014-2021
//   This source is subject to the Microsoft Public License (Ms-PL).
//   Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//   All other rights reserved.
// </copyright>
// <summary>
//   Defines the UntypedQueryable type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OpenMedStack.Linq2Fhir;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

internal class UntypedQueryable<T> : IQueryable<object>
{
    private readonly IQueryable _source;

    public UntypedQueryable(IQueryable<T> source, Expression<Func<T, object>> projection)
    {
        _source = projection == null ? (IQueryable) source : source.Select(projection);
    }

    public Expression Expression => _source.Expression;

    public Type ElementType => typeof(T);

    public IQueryProvider Provider => _source.Provider;

    public IEnumerator<object> GetEnumerator()
    {
        return Enumerable.Cast<object>(_source).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}