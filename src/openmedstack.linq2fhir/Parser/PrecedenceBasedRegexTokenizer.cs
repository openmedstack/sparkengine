namespace OpenMedStack.Linq2Fhir.Parser;

using System;
using System.Collections.Generic;
#if NET7_0
using System.Diagnostics.CodeAnalysis;
#endif
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using Hl7.Fhir.Model;
using Expression = System.Linq.Expressions.Expression;

internal class PrecedenceBasedRegexTokenizer
{
    private readonly List<TokenDefinition> _tokenDefinitions;

    public PrecedenceBasedRegexTokenizer()
    {
        _tokenDefinitions = new List<TokenDefinition>
        {
                new (TokenType.OpenParenthesis, "\\(", 1),
                new (TokenType.CloseParenthesis, "\\)", 1),
                new (TokenType.AssignedValue, "=", 1),
                new (TokenType.Equals, "eq", 1),
                new (TokenType.NotEquals, "ne", 1),
                new (TokenType.GreaterThan, "gt", 1),
                new (TokenType.LessThan, "lt", 1),
                new (TokenType.GreaterThanOrEqual, "ge", 1),
                new (TokenType.LessThanOrEqual, "le", 1),
                new (TokenType.StartsAfter, "sa", 1),
                new (TokenType.EndsBefore, "eb", 1),
                new (TokenType.Approximately, "ap", 1),
                new (TokenType.StringValue, "'([^']*)'", 2),
                new (TokenType.Value, "((?<!:)\\b\\w+\\b(?!:(not|missing|exact|contains)))", 2),
                new (TokenType.NotValue, "\\b\\w+\\b:not", 2),
                new (TokenType.MissingValue, "\\b\\w+\\b:missing", 2),
                new (TokenType.ExactValue, "\\b\\w+\\b:exact", 2),
                new (TokenType.ContainsValue, "\\b\\w+\\b:contains", 2),
                new (TokenType.Number, "\\d+", 2)
            };
    }

    public IEnumerable<DslToken> Tokenize(string input)
    {
        var tokenMatches = FindTokenMatches(input);

        var groupedByIndex = tokenMatches.GroupBy(x => x.StartIndex)
            .OrderBy(x => x.Key)
            .ToArray();

        TokenMatch? lastMatch = null;
        foreach (var group in groupedByIndex)
        {
            var bestMatch = group.OrderBy(x => x.Precedence).First();
            if (lastMatch != null && bestMatch.StartIndex < lastMatch.EndIndex)
                continue;

            yield return new DslToken(bestMatch.TokenType, bestMatch.Value);

            lastMatch = bestMatch;
        }

        yield return new DslToken(TokenType.SequenceTerminator);
    }

    private IEnumerable<TokenMatch> FindTokenMatches(string lqlText)
    {
        return _tokenDefinitions.SelectMany(tokenDefinition => tokenDefinition.FindMatches(lqlText));
    }
}


internal class Parser
{
    private static readonly Dictionary<string, PropertyInfo> PropertyInfos = new();

    public Expression<Func<T, bool>> Parse<T>(IReadOnlyList<DslToken> tokens) where T: Resource
    {
        var tokenSequence = LoadSequenceStack(tokens);

        var parameter = Expression.Parameter(typeof(T), "x");
        var body = CreateExpression<T>(tokenSequence, parameter);
        return Expression.Lambda<Func<T, bool>>(body, false, parameter);
    }

    private Expression CreateExpression<T>(Stack<DslToken> tokens, ParameterExpression parameter)
    {
        var first = tokens.Pop();

        var op = tokens.Pop();
        if (op.TokenType != TokenType.AssignedValue)
        {
            throw new Exception($"Malformed expression: {first.Value} {op.Value}");
        }

        var second = tokens.Pop();


        var (property, firstExpression) = CreatePropertyExpression<T>(parameter, first);


        if (!IsPrefix(second.TokenType))
        {
            var secondExpression = Expression.Constant(
                Convert.ChangeType(second.Value, property.PropertyType),
                property.PropertyType);
            return first.TokenType == TokenType.NotValue
                ? Expression.NotEqual(firstExpression, secondExpression)
                : Expression.Equal(firstExpression, secondExpression);
        }
        else
        {
            var prefix = second.TokenType;
            second = tokens.Pop();
            var secondExpression = Expression.Constant(
                Convert.ChangeType(second.Value, property.PropertyType),
                property.PropertyType);
            return BuildExpression(prefix, first.TokenType, firstExpression, secondExpression);
        }
    }

    private static (PropertyInfo property, Expression expression) CreatePropertyExpression<T>(
        ParameterExpression parameter,
        DslToken first)
    {
        var propertyName = first.Value.IndexOf(':') > 0 ? first.Value[..first.Value.IndexOf(':')] : first.Value;
        lock (PropertyInfos)
        {
            var key = $"{typeof(T).AssemblyQualifiedName}-{propertyName}";
            if (!PropertyInfos.TryGetValue(key, out var info))
            {
                info = typeof(T).GetProperties()
                    .First(
                        p => string.Equals(p.Name, propertyName, StringComparison.InvariantCultureIgnoreCase));
                PropertyInfos[key] = info;
            }

            return (info, Expression.Property(parameter, info));
        }
    }

    private static Expression BuildExpression(TokenType prefix, TokenType suffix, Expression first, Expression second)
    {
        return prefix switch
        {
            TokenType.NotEquals when suffix == TokenType.NotValue => Expression.Equal(first, second),
            TokenType.NotEquals => Expression.NotEqual(first, second),
            TokenType.GreaterThan when suffix == TokenType.NotValue => Expression.LessThanOrEqual(first, second),
            TokenType.GreaterThan => Expression.GreaterThan(first, second),
            TokenType.GreaterThanOrEqual when suffix == TokenType.NotValue => Expression.LessThan(first, second),
            TokenType.GreaterThanOrEqual => Expression.GreaterThanOrEqual(first, second),
            TokenType.LessThan when suffix == TokenType.NotValue => Expression.GreaterThanOrEqual(first, second),
            TokenType.LessThan => Expression.LessThan(first, second),
            TokenType.LessThanOrEqual when suffix == TokenType.NotValue => Expression.GreaterThan(first, second),
            TokenType.LessThanOrEqual => Expression.LessThanOrEqual(first, second),
            _ => throw new ArgumentOutOfRangeException(nameof(prefix))
        };
    }

    private static bool IsPrefix(TokenType tokenType)
    {
        return tokenType is TokenType.NotEquals or TokenType.GreaterThan or TokenType.LessThan
            or TokenType.GreaterThanOrEqual or TokenType.LessThanOrEqual or TokenType.StartsAfter
            or TokenType.EndsBefore or TokenType.Approximately;
    }

    private Stack<DslToken> LoadSequenceStack(IReadOnlyList<DslToken> tokens)
    {
        var tokenSequence = new Stack<DslToken>();
        var count = tokens.Count;
        for (var i = count - 1; i >= 0; i--)
        {
            tokenSequence.Push(tokens[i]);
        }

        return tokenSequence;
    }

}

internal class TokenDefinition
{
    private readonly Regex _regex;
    private readonly TokenType _returnsToken;
    private readonly int _precedence;

#if NET7_0
    public TokenDefinition(TokenType returnsToken, [StringSyntax(StringSyntaxAttribute.Regex)] string regexPattern, int precedence)
#else
    public TokenDefinition(TokenType returnsToken, string regexPattern, int precedence)
#endif
    {
        _regex = new Regex(regexPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
        _returnsToken = returnsToken;
        _precedence = precedence;
    }

    public IEnumerable<TokenMatch> FindMatches(string inputString)
    {
        var matches = _regex.Matches(inputString);
        for (var i = 0; i < matches.Count; i++)
        {
            yield return new TokenMatch()
            {
                StartIndex = matches[i].Index,
                EndIndex = matches[i].Index + matches[i].Length,
                TokenType = _returnsToken,
                Value = matches[i].Value,
                Precedence = _precedence
            };
        }
    }
}


internal class TokenMatch
{
    public TokenType TokenType { get; init; }
    public string Value { get; init; } = null!;
    public int StartIndex { get; init; }
    public int EndIndex { get; init; }
    public int Precedence { get; init; }
}

internal enum TokenType
{
    CloseParenthesis,
    Equals,
    NotEquals,
    Number,
    OpenParenthesis,
    StringValue,
    SequenceTerminator,
    Value,
    NotValue,
    MissingValue,
    ExactValue,
    ContainsValue,
    AssignedValue,
    GreaterThan,
    LessThan,
    GreaterThanOrEqual,
    LessThanOrEqual,
    StartsAfter,
    EndsBefore,
    Approximately
}

internal class DslToken
{
    public DslToken(TokenType tokenType)
    {
        TokenType = tokenType;
        Value = string.Empty;
    }

    public DslToken(TokenType tokenType, string value)
    {
        TokenType = tokenType;
        Value = value;
    }

    public TokenType TokenType { get; set; }
    public string Value { get; set; }

    public DslToken Clone()
    {
        return new DslToken(TokenType, Value);
    }
}
