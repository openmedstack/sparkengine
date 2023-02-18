namespace OpenMedStack.Linq2Fhir;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
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
    private readonly List<(string, IncludeModifier)> _revIncludes = new();
    private readonly List<(string, IncludeModifier)> _includes = new();
    private readonly HashSet<string> _elements = new();

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

        foreach (var item in _includes)
        {
            p.Include.Add(item);
        }

        foreach (var item in _revIncludes)
        {
            p.RevInclude.Add(item);
        }

        foreach (var element in _elements)
        {
            p.Elements.Add(element);
        }

        return p;
    }

    /// <inheritdoc />
    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        var nodeArgument = node.Arguments[1];
        var declaringType = node.Method.DeclaringType;
        switch (node.Method.Name)
        {
            case nameof(AsyncQueryable.Where) when declaringType == typeof(AsyncQueryable):
                _criteriaVisitor.Visit(nodeArgument);
                break;
            case nameof(AsyncQueryable.Select) when declaringType == typeof(AsyncQueryable):
            case nameof(AsyncQueryable.SelectAwait) when declaringType == typeof(AsyncQueryable):
            case nameof(AsyncQueryable.SelectAwaitWithCancellation) when declaringType == typeof(AsyncQueryable):
            case nameof(AsyncQueryable.SelectMany) when declaringType == typeof(AsyncQueryable):
            case nameof(AsyncQueryable.SelectManyAwait) when declaringType == typeof(AsyncQueryable):
            case nameof(AsyncQueryable.SelectManyAwaitWithCancellation) when declaringType == typeof(AsyncQueryable):
                throw new NotSupportedException("Use Elements method to reduce data transfer.");
            case nameof(AsyncQueryable.OrderBy) when declaringType == typeof(AsyncQueryable):
                {
                    var member = GetMemberName(nodeArgument);
                    _sortOrders.Add((member, SortOrder.Ascending));
                }
                break;
            case nameof(AsyncQueryable.OrderByDescending) when declaringType == typeof(AsyncQueryable):
                {
                    var memberName = GetMemberName(nodeArgument);
                    _sortOrders.Add((memberName, SortOrder.Descending));
                }
                break;
            case nameof(FhirQueryableExtensions.UpdatedSince) when declaringType == typeof(FhirQueryableExtensions):
                _updatedSince = (DateTimeOffset?)(nodeArgument as ConstantExpression)?.Value;
                break;
            case nameof(FhirQueryableExtensions.ReverseInclude) when declaringType == typeof(FhirQueryableExtensions):
                {
                    var modifier = node.Arguments[2] as ConstantExpression;
                    var member = GetMemberName(nodeArgument);
                    _revIncludes.Add((member, (IncludeModifier)(modifier?.Value ?? IncludeModifier.None)));
                }
                break;
            case nameof(FhirQueryableExtensions.Include) when declaringType == typeof(FhirQueryableExtensions):
                {
                    var member = GetMemberName(nodeArgument, true);
                    _includes.Add((member, IncludeModifier.None));
                }
                break;
            case nameof(FhirQueryableExtensions.Elements) when declaringType == typeof(FhirQueryableExtensions):
                {
                    var lambdaExpression = GetLambdaExpression(nodeArgument);
                    var newExpression = lambdaExpression!.Body as NewExpression;
                    if (newExpression?.Arguments != null)
                    {
                        foreach (var member in newExpression.Arguments.OfType<MemberExpression>())
                        {
                            _elements.Add(GetCleanMemberName(member.Member));
                        }
                    }
                }
                break;
            case nameof(AsyncQueryable.ToListAsync) when declaringType == typeof(AsyncQueryable):
            case nameof(AsyncQueryable.ToArrayAsync) when declaringType == typeof(AsyncQueryable):
                break;
            default:
                throw new NotSupportedException("Not supported (yet)");
        }

        return Visit(node.Arguments[0]);
    }

    private static string GetMemberName(Expression nodeArgument, bool includeDeclaringType = false)
    {
        var lambdaExpression = GetLambdaExpression(nodeArgument);
        if (lambdaExpression!.Body is MemberExpression { Expression: MethodCallExpression { Method.Name: nameof(FhirQueryableExtensions.ReferringResource) } methodCall } memberAccess 
            && methodCall.Method.DeclaringType == typeof(FhirQueryableExtensions))
        {
            var resourceType = methodCall.Method.ReturnType;
            return $"{resourceType.Name}:{GetCleanMemberName(memberAccess.Member)}";
        }

        var memberExpression = lambdaExpression.Body as MemberExpression;
        return includeDeclaringType
            ? $"{ModelInfo.GetFhirTypeNameForType(memberExpression!.Expression!.Type)}:{GetCleanMemberName(memberExpression.Member)}"
            : GetCleanMemberName(memberExpression!.Member);
    }

    private static string GetCleanMemberName(MemberInfo member)
    {
        return member.Name.ToLowerInvariant().Replace("element", "");
    }

    private static LambdaExpression? GetLambdaExpression(Expression nodeArgument)
    {
        var unaryExpression = nodeArgument as UnaryExpression;
        var lambdaExpression = unaryExpression!.Operand as LambdaExpression;
        return lambdaExpression;
    }
}
