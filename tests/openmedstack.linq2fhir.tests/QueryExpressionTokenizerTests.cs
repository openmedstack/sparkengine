namespace OpenMedStack.Linq2Fhir.Tests
{
    using OpenMedStack.Linq2Fhir.Parser;
    using Xunit;

    public class QueryExpressionTokenizerTests
    {
        [Theory]
        [InlineData("subject=lucy", 4)]
        [InlineData("gender:not=female", 4)]
        [InlineData("name:exact=Jon", 4)]
        [InlineData("address:contains=Meadow", 4)]
        [InlineData("address:missing=true", 4)]
        public void CanTokenize(string query, int count)
        {
            var parser = new PrecedenceBasedRegexTokenizer();
            var tokens = parser.Tokenize(query).ToArray();

            Assert.Equal(count, tokens.Length);
        }
    }
}