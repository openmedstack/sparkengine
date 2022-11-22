namespace OpenMedStack.Linq2Fhir.Tests;

using System.Linq.Expressions;
using Hl7.Fhir.Model;
using Expression = System.Linq.Expressions.Expression;

public class QueryVisitorTests
{
    [Theory]
    [MemberData(nameof(QueryExpressions))]
    public void CanConvertQueries(Expression query, string path)
    {
        var visitor = new CriteriaVisitor();
        _ = visitor.Visit(query);

        var parameter = string.Join(
            "&",
            visitor.Query.Select(parameter => $"{parameter.Item1}={parameter.Item2}"));
        Assert.Equal(path, parameter);
    }

    [Fact]
    public void ThrowsForOrElseExpressions()
    {
        var visitor = new CriteriaVisitor();

        Assert.Throws<Exception>(() => visitor.Visit((TestType t) => t.Subject == "a" || t.Number > 1));
    }

    public static IEnumerable<object[]> QueryExpressions()
    {
        yield return new object[] { (Expression<Func<Encounter, bool>>)(t => t.PlannedEndDate.Contains("jan")), "plannedenddate:contains=jan" };
        yield return new object[] { (Expression<Func<TestType, bool>>)(t => t.Subject != null), "subject:missing=false" };
        yield return new object[] { (Expression<Func<TestType, bool>>)(t => t.Subject == "Lucy"), "subject=Lucy" };
        yield return new object[] { (Expression<Func<TestType, bool>>)(t => t.Subject != "Lucy"), "subject:not=Lucy" };
        yield return new object[] { (Expression<Func<TestType, bool>>)(t => t.Number == 1), "number=1" };
        yield return new object[] { (Expression<Func<TestType, bool>>)(t => t.Number > 1), "number=gt1" };
        yield return new object[] { (Expression<Func<TestType, bool>>)(t => t.Number > 1 && t.Subject == "Lucy"), "number=gt1&subject=Lucy" };
    }

}