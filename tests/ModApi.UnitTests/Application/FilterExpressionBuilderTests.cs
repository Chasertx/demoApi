using System.Linq.Expressions;
using ModApi.Application.Exceptions;
using ModApi.Application.Querying;
using ModApi.UnitTests.TestDoubles;

namespace ModApi.UnitTests.Application;

public class FilterExpressionBuilderTests
{
    private static readonly Guid KnownGuid = Guid.Parse("8f14e45f-ceea-467f-a8d6-4f1f1a1b2c3d");

    private static readonly Widget[] Widgets =
    [
        new() { Id = 1, Name = "Alpha Laptop", Notes = "Fast", Quantity = 5, Price = 999.99m, IsEnabled = true, Color = WidgetColor.Red, ExternalId = KnownGuid },
        new() { Id = 2, Name = "Beta Mouse", Notes = null, Quantity = 50, Price = 19.99m, IsEnabled = true, Color = WidgetColor.Green, ShippedAtUtc = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
        new() { Id = 3, Name = "Gamma Keyboard", Notes = "Mechanical LAPTOP friendly", Quantity = 0, Price = 79m, IsEnabled = false, Color = WidgetColor.Blue, ShippedAtUtc = new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc) },
    ];

    private static int[] Apply(string? search, params FilterCriterion[] filters)
    {
        var predicate = FilterExpressionBuilder.Build<Widget>(filters, search);
        var query = Widgets.AsQueryable();
        return (predicate is null ? query : query.Where(predicate)).Select(w => w.Id).OrderBy(id => id).ToArray();
    }

    private static int[] Apply(params FilterCriterion[] filters) => Apply(null, filters);

    private static FilterCriterion F(string property, FilterOperator op, string value) => new(property, op, value);

    [Fact]
    public void No_filters_and_no_search_returns_null()
    {
        Assert.Null(FilterExpressionBuilder.Build<Widget>([], null));
        Assert.Null(FilterExpressionBuilder.Build<Widget>([], "   "));
    }

    [Fact]
    public void Equal_matches_exact_values()
    {
        Assert.Equal([2], Apply(F("quantity", FilterOperator.Equal, "50")));
        Assert.Equal([1], Apply(F("Name", FilterOperator.Equal, "Alpha Laptop")));
    }

    [Fact]
    public void NotEqual_excludes_values()
    {
        Assert.Equal([1, 3], Apply(F("quantity", FilterOperator.NotEqual, "50")));
    }

    [Theory]
    [InlineData(FilterOperator.GreaterThan, "5", new[] { 2 })]
    [InlineData(FilterOperator.GreaterThanOrEqual, "5", new[] { 1, 2 })]
    [InlineData(FilterOperator.LessThan, "5", new[] { 3 })]
    [InlineData(FilterOperator.LessThanOrEqual, "5", new[] { 1, 3 })]
    public void Range_operators_compare_numbers(FilterOperator op, string value, int[] expected)
    {
        Assert.Equal(expected, Apply(F("quantity", op, value)));
    }

    [Fact]
    public void Range_operators_work_on_decimals()
    {
        Assert.Equal([1, 3], Apply(F("price", FilterOperator.GreaterThan, "50.5")));
    }

    [Fact]
    public void Range_operators_work_on_nullable_dates()
    {
        Assert.Equal([3], Apply(F("shippedAtUtc", FilterOperator.GreaterThan, "2025-03-01")));
    }

    [Fact]
    public void Filters_are_combined_with_and()
    {
        Assert.Equal([1], Apply(F("isEnabled", FilterOperator.Equal, "true"), F("quantity", FilterOperator.LessThan, "10")));
    }

    [Fact]
    public void Enum_equality_accepts_names()
    {
        Assert.Equal([3], Apply(F("color", FilterOperator.Equal, "blue")));
    }

    [Fact]
    public void Guid_equality_is_supported()
    {
        Assert.Equal([1], Apply(F("externalId", FilterOperator.Equal, KnownGuid.ToString())));
    }

    [Fact]
    public void Null_comparisons_are_supported()
    {
        Assert.Equal([2], Apply(F("notes", FilterOperator.Equal, "null")));
        Assert.Equal([1, 3], Apply(F("notes", FilterOperator.NotEqual, "null")));
        Assert.Equal([1], Apply(F("shippedAtUtc", FilterOperator.Equal, "null")));
    }

    [Theory]
    [InlineData(FilterOperator.Contains, "LAPTOP", new[] { 1 })]
    [InlineData(FilterOperator.StartsWith, "beta", new[] { 2 })]
    [InlineData(FilterOperator.EndsWith, "BOARD", new[] { 3 })]
    public void Text_operators_are_case_insensitive(FilterOperator op, string value, int[] expected)
    {
        Assert.Equal(expected, Apply(F("name", op, value)));
    }

    [Fact]
    public void Text_operators_skip_null_values()
    {
        Assert.Equal([1], Apply(F("notes", FilterOperator.Contains, "fast")));
    }

    [Fact]
    public void Search_matches_any_text_property_case_insensitively()
    {
        Assert.Equal([1, 3], Apply("laptop"));
    }

    [Fact]
    public void Search_is_combined_with_filters()
    {
        Assert.Equal([3], Apply("laptop", F("isEnabled", FilterOperator.Equal, "false")));
    }

    [Fact]
    public void Search_on_entity_without_text_properties_is_rejected()
    {
        var exception = Assert.Throws<RequestValidationException>(() => FilterExpressionBuilder.Build<Measurement>([], "x"));

        Assert.True(exception.Errors.ContainsKey("search"));
    }

    [Fact]
    public void Unknown_properties_are_rejected()
    {
        var exception = Assert.Throws<RequestValidationException>(() => Apply(F("doesNotExist", FilterOperator.Equal, "1")));

        Assert.True(exception.Errors.ContainsKey("doesNotExist"));
    }

    [Fact]
    public void Navigation_and_not_mapped_properties_are_not_filterable()
    {
        Assert.Throws<RequestValidationException>(() => Apply(F("gadgets", FilterOperator.Equal, "1")));
        Assert.Throws<RequestValidationException>(() => Apply(F("computed", FilterOperator.Equal, "x")));
    }

    [Fact]
    public void Invalid_values_are_rejected()
    {
        var exception = Assert.Throws<RequestValidationException>(() => Apply(F("quantity", FilterOperator.Equal, "lots")));

        Assert.Contains("lots", Assert.Single(exception.Errors["quantity"]));
    }

    [Fact]
    public void Null_with_range_operator_is_rejected()
    {
        Assert.Throws<RequestValidationException>(() => Apply(F("shippedAtUtc", FilterOperator.GreaterThan, "null")));
    }

    [Theory]
    [InlineData("name", FilterOperator.GreaterThan, "a")]
    [InlineData("isEnabled", FilterOperator.LessThan, "true")]
    [InlineData("color", FilterOperator.GreaterThan, "Red")]
    public void Range_operators_on_unordered_types_are_rejected(string property, FilterOperator op, string value)
    {
        Assert.Throws<RequestValidationException>(() => Apply(F(property, op, value)));
    }

    [Fact]
    public void Text_operators_on_non_strings_are_rejected()
    {
        Assert.Throws<RequestValidationException>(() => Apply(F("quantity", FilterOperator.Contains, "5")));
    }

    [Fact]
    public void Every_error_is_reported_at_once()
    {
        var exception = Assert.Throws<RequestValidationException>(() =>
            Apply(F("unknown", FilterOperator.Equal, "1"), F("quantity", FilterOperator.Equal, "x")));

        Assert.Equal(2, exception.Errors.Count);
    }

    [Fact]
    public void Values_are_captured_as_members_not_constants_so_ef_core_parameterizes_them()
    {
        var predicate = FilterExpressionBuilder.Build<Widget>([F("name", FilterOperator.Equal, "x")], null)!;

        var comparison = Assert.IsAssignableFrom<BinaryExpression>(predicate.Body);
        var right = Assert.IsAssignableFrom<MemberExpression>(comparison.Right);
        Assert.IsAssignableFrom<ConstantExpression>(right.Expression);
    }
}
