namespace OpenMedStack.Linq2Fhir;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Hl7.Fhir.Support;
using ConstantExpression = System.Linq.Expressions.ConstantExpression;
using Expression = System.Linq.Expressions.Expression;
using UnaryExpression = System.Linq.Expressions.UnaryExpression;

internal class QueryExpressionVisitor : ExpressionVisitor
{
    private readonly CriteriaVisitor _criteriaVisitor = new();
    private readonly List<(string, SortOrder)> _sortOrders = new();
    private DateTimeOffset? _updatedSince;

    public SearchParams GetParams()
    {
        var p = new SearchParams();
        foreach (var tuple in _criteriaVisitor.Query)
        {
            p.Add(tuple.Item1, tuple.Item2);
        }

        foreach (var sortOrder in _sortOrders)
        {
            p.Sort.Add(sortOrder);
        }

        if (_updatedSince != null)
        {
            p.Add("_lastUpdated", $"gt{_updatedSince.ToFhirDateTime()}");
        }
        return p;
    }

    /// <inheritdoc />
    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        var nodeArgument = node.Arguments[1];
        switch (node.Method.Name)
        {
            case nameof(AsyncQueryable.Where) when node.Method.DeclaringType == typeof(AsyncQueryable):
                _criteriaVisitor.Visit(nodeArgument);
                break;
            case nameof(AsyncQueryable.OrderBy) when node.Method.DeclaringType == typeof(AsyncQueryable):
                var member = GetMemberName(nodeArgument);
                _sortOrders.Add((member, SortOrder.Ascending));
                break;
            case nameof(AsyncQueryable.OrderByDescending) when node.Method.DeclaringType == typeof(AsyncQueryable):
                var memberName = GetMemberName(nodeArgument);
                _sortOrders.Add((memberName, SortOrder.Descending));
                break;
            case nameof(FhirQueryableExtensions.UpdatedSince) when node.Method.DeclaringType == typeof(FhirQueryableExtensions):
                _updatedSince = (DateTimeOffset?)(nodeArgument as ConstantExpression)?.Value;
                break;
        }

        return Visit(node.Arguments[0]);
    }

    private static string GetMemberName(Expression nodeArgument)
    {
        var unaryExpression = nodeArgument as UnaryExpression;
        var unaryExpressionOperand = unaryExpression!.Operand as LambdaExpression;
        var memberExpression = unaryExpressionOperand!.Body as MemberExpression;
        var member = memberExpression!.Member.Name.ToLowerInvariant();
        return member;
    }
}
