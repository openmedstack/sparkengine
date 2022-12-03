namespace OpenMedStack.Linq2Fhir;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using global::System.Linq.Expressions;
using Hl7.Fhir.Rest;
using Expression = System.Linq.Expressions.Expression;

internal class CriteriaVisitor : ExpressionVisitor
{
    private readonly SearchParams _query = new();
    private readonly StringBuilder _builder = new();

    public IEnumerable<Tuple<string, string>> Query
    {
        get { return _query.Parameters; }
    }

    /// <inheritdoc />
    protected override Expression VisitLambda<T>(Expression<T> node)
    {
        Visit(node.Body);
        return node;
    }

    /// <inheritdoc />
    protected override Expression VisitBinary(BinaryExpression node)
    {
        switch (node.NodeType)
        {
            case ExpressionType.AndAlso:
                Visit(node.Left);
                Visit(node.Right);
                break;
            case ExpressionType.OrElse:
                throw new Exception("You should perform a separate query for each condition");
            default:
                {
                    Visit(node.Left);
                    var op = GetOperation(node.NodeType);
                    if (node.Right is ConstantExpression { Value: null })
                    {
                        _query.Add($"{_builder}:missing", op == ":not" ? "false" : "true");
                        _builder.Clear();
                    }
                    else if (node.NodeType == ExpressionType.NotEqual)
                    {
                        var left = _builder + op;
                        _builder.Clear();
                        Visit(node.Right);
                        _query.Add(left, _builder.ToString());
                        _builder.Clear();
                    }
                    else
                    {
                        var left = _builder.ToString();
                        _builder.Clear();
                        Visit(node.Right);
                        _query.Add(left, op + _builder);
                        _builder.Clear();
                    }

                    break;
                }
        }

        return node;
    }

    /// <inheritdoc />
    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        switch (node.Method.Name)
        {
            case nameof(string.Contains) when node.Method.DeclaringType == typeof(string):
                {
                    Visit(node.Object);
                    _builder.Append(":contains");
                    var left = _builder.ToString();
                    _builder.Clear();
                    Visit(node.Arguments[0]);
                    _query.Add(left, _builder.ToString());
                    _builder.Clear();
                }
                break;
            case nameof(Enumerable.Any) when node.Method.DeclaringType == typeof(Enumerable):
                {
                    Visit(node.Arguments[0]);
                    var lambda = (node.Arguments[1] as LambdaExpression)!;
                    var body = lambda.Body;
                    switch (body)
                    {
                        case BinaryExpression:
                            _builder.Append('.');
                            Visit(body);
                            break;
                        case MethodCallExpression methodCall:
                            Visit(methodCall);
                            break;
                    }
                }
                break;
            case nameof(FhirQueryableExtensions.MatchAnyAttribute)
                when node.Method.DeclaringType == typeof(FhirQueryableExtensions):
                {
                    var name = _builder.ToString();
                    _builder.Clear();
                    Visit(node.Arguments[1]);
                    _query.Add(name, _builder.ToString());
                    _builder.Clear();
                }
                break;
            case nameof(FhirQueryableExtensions.DoNotMatchAnyAttribute)
                when node.Method.DeclaringType == typeof(FhirQueryableExtensions):
                {
                    _builder.Append(":not");
                    var name = _builder.ToString();
                    _builder.Clear();
                    Visit(node.Arguments[1]);
                    _query.Add(name, _builder.ToString());
                    _builder.Clear();
                }
                break;
            case nameof(List<object>.Contains) when node.Method.DeclaringType?.IsGenericType == true
                                                    && node.Method.DeclaringType.GetGenericTypeDefinition()
                                                        .IsAssignableTo(typeof(IEnumerable<>)):
                throw new NotSupportedException($"Use {nameof(Enumerable.Any)} to find matches.");
        }

        return node;
    }

    /// <inheritdoc />
    protected override Expression VisitMember(MemberExpression node)
    {
        _builder.Append(node.Member.Name.Replace("Element", "").ToLowerInvariant());
        return base.VisitMember(node);
    }

    /// <inheritdoc />
    protected override Expression VisitConstant(ConstantExpression node)
    {
        _builder.Append(node.Value);
        return base.VisitConstant(node);
    }

    private static string GetOperation(ExpressionType nodeType)
    {
        return nodeType switch
        {
            ExpressionType.Equal => "",
            ExpressionType.NotEqual => ":not",
            ExpressionType.GreaterThan => "gt",
            ExpressionType.GreaterThanOrEqual => "ge",
            ExpressionType.LessThan => "lt",
            ExpressionType.LessThanOrEqual => "le",
            _ => throw new Exception($"Unhandled type: {nodeType}")
        };
    }
}