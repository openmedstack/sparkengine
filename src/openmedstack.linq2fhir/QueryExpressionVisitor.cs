namespace OpenMedStack.Linq2Fhir;

using System.Collections.Generic;
using System.Linq.Expressions;
using Hl7.Fhir.Rest;
using Expression = System.Linq.Expressions.Expression;

internal class QueryExpressionVisitor : ExpressionVisitor
{
    private readonly CriteriaVisitor _criteriaVisitor = new();
    private readonly List<(string, SortOrder)> _sortOrders = new();

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
        return p;
    }

    /// <inheritdoc />
    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        var nodeArgument = node.Arguments[1];
        switch (node.Method.Name)
        {
            case "Where":
                _criteriaVisitor.Visit(nodeArgument);
                break;
            case "Order":
                _sortOrders.Add(("_id", SortOrder.Ascending));
                break;
            case "OrderBy":
                var member = (((nodeArgument as UnaryExpression).Operand as LambdaExpression).Body as MemberExpression).Member.Name;
                _sortOrders.Add((member, SortOrder.Ascending));
                break;
            case "OrderDescending":
                _sortOrders.Add(("_id", SortOrder.Descending));
                break;
        }

        return Visit(node.Arguments[0]);
    }
}