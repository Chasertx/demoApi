using ModApi.Application.Entities;

namespace ModApi.UnitTests.Application;

public class RouteNamingTests
{
    [Theory]
    [InlineData("Product", "products")]
    [InlineData("Category", "categories")]
    [InlineData("OrderLine", "order-lines")]
    [InlineData("Address", "addresses")]
    [InlineData("Box", "boxes")]
    [InlineData("Batch", "batches")]
    [InlineData("Key", "keys")]
    [InlineData("HTTPRequest", "http-requests")]
    [InlineData("Item2", "item2s")]
    [InlineData("customer_note", "customer-notes")]
    public void ToRoute_produces_kebab_case_plural(string typeName, string expected)
    {
        Assert.Equal(expected, RouteNaming.ToRoute(typeName));
    }

    [Theory]
    [InlineData("City", "Cities")]
    [InlineData("Day", "Days")]
    [InlineData("Bus", "Buses")]
    [InlineData("Wish", "Wishes")]
    [InlineData("Quiz", "Quizes")]
    [InlineData("Car", "Cars")]
    [InlineData("", "")]
    public void Pluralize_applies_english_rules(string word, string expected)
    {
        Assert.Equal(expected, RouteNaming.Pluralize(word));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("___")]
    public void ToRoute_rejects_names_without_letters(string typeName)
    {
        Assert.ThrowsAny<ArgumentException>(() => RouteNaming.ToRoute(typeName));
    }
}
