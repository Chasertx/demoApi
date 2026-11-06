using ModApi.Application.Exceptions;
using ModApi.Application.Querying;

namespace ModApi.UnitTests.Application;

public class FilterParserTests
{
    private static IReadOnlyList<FilterCriterion> Parse(params (string Key, string Value)[] pairs) =>
        FilterParser.Parse(pairs.Select(p => KeyValuePair.Create(p.Key, p.Value)));

    [Fact]
    public void A_bare_property_means_equality()
    {
        var criterion = Assert.Single(Parse(("name", "Laptop")));

        Assert.Equal(new FilterCriterion("name", FilterOperator.Equal, "Laptop"), criterion);
    }

    [Theory]
    [InlineData("eq", FilterOperator.Equal)]
    [InlineData("ne", FilterOperator.NotEqual)]
    [InlineData("gt", FilterOperator.GreaterThan)]
    [InlineData("gte", FilterOperator.GreaterThanOrEqual)]
    [InlineData("lt", FilterOperator.LessThan)]
    [InlineData("lte", FilterOperator.LessThanOrEqual)]
    [InlineData("contains", FilterOperator.Contains)]
    [InlineData("startswith", FilterOperator.StartsWith)]
    [InlineData("endswith", FilterOperator.EndsWith)]
    [InlineData("GTE", FilterOperator.GreaterThanOrEqual)]
    public void Operators_are_parsed_case_insensitively(string token, FilterOperator expected)
    {
        var criterion = Assert.Single(Parse(($"price:{token}", "10")));

        Assert.Equal("price", criterion.Property);
        Assert.Equal(expected, criterion.Operator);
        Assert.Equal("10", criterion.Value);
    }

    [Fact]
    public void Whitespace_around_property_and_operator_is_trimmed()
    {
        var criterion = Assert.Single(Parse((" price : lt ", "5")));

        Assert.Equal("price", criterion.Property);
        Assert.Equal(FilterOperator.LessThan, criterion.Operator);
    }

    [Fact]
    public void Multiple_filters_are_preserved_in_order()
    {
        var criteria = Parse(("price:gte", "10"), ("price:lt", "100"), ("name", "x"));

        Assert.Equal(3, criteria.Count);
        Assert.Equal(FilterOperator.GreaterThanOrEqual, criteria[0].Operator);
        Assert.Equal(FilterOperator.LessThan, criteria[1].Operator);
        Assert.Equal("name", criteria[2].Property);
    }

    [Fact]
    public void No_pairs_produce_no_filters()
    {
        Assert.Empty(Parse());
    }

    [Fact]
    public void Unknown_operators_are_reported()
    {
        var exception = Assert.Throws<RequestValidationException>(() => Parse(("price:between", "1")));

        Assert.Contains("between", Assert.Single(exception.Errors["price:between"]));
    }

    [Fact]
    public void Missing_property_names_are_reported()
    {
        var exception = Assert.Throws<RequestValidationException>(() => Parse((":eq", "1")));

        Assert.True(exception.Errors.ContainsKey(":eq"));
    }

    [Fact]
    public void All_errors_are_reported_together()
    {
        var exception = Assert.Throws<RequestValidationException>(() => Parse(("a:bad", "1"), (":gt", "2")));

        Assert.Equal(2, exception.Errors.Count);
    }

    [Fact]
    public void Supported_operators_are_exposed()
    {
        Assert.Contains("contains", FilterParser.SupportedOperators);
        Assert.Equal(9, FilterParser.SupportedOperators.Count);
    }
}
