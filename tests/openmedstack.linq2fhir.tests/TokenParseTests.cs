namespace OpenMedStack.Linq2Fhir.Tests;

using Parser;
using Xunit;

public class TokenParseTests
{
    [Theory]
    [InlineData("subject=lucy", "x => (x.Subject == \"lucy\")")]
    [InlineData("subject:not=lucy", "x => (x.Subject != \"lucy\")")]
    [InlineData("number=1", "x => (x.Number == 1)")]
    [InlineData("number:not=1", "x => (x.Number != 1)")]
    [InlineData("number=gt1", "x => (x.Number > 1)")]
    [InlineData("number:not=gt1", "x => (x.Number <= 1)")]
    [InlineData("number=lt1", "x => (x.Number < 1)")]
    [InlineData("number:not=lt1", "x => (x.Number >= 1)")]
    [InlineData("number=ge1", "x => (x.Number >= 1)")]
    [InlineData("number:not=ge1", "x => (x.Number < 1)")]
    [InlineData("number=le1", "x => (x.Number <= 1)")]
    [InlineData("number:not=le1", "x => (x.Number > 1)")]
    public void CanParse(string query, string expected)
    {
        var tokenizer = new PrecedenceBasedRegexTokenizer();
        var tokens = tokenizer.Tokenize(query).ToArray();
        var parser = new Parser();

        var expression = parser.Parse<TestType>(tokens);

        Assert.Equal(expected, expression.ToString());
    }
}