using System.Text.Json;
using ModApi.Application.Exceptions;
using ModApi.Application.Mapping;
using ModApi.UnitTests.TestDoubles;

namespace ModApi.UnitTests.Application;

public class ReflectionEntityMapperTests
{
    private static readonly DateTime Created = new(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private readonly ReflectionEntityMapper _mapper = new();

    private static Widget Existing() => new()
    {
        Id = 7,
        Name = "Original",
        Notes = "keep",
        Quantity = 1,
        Price = 10m,
        Color = WidgetColor.Red,
        CreatedAtUtc = Created,
        ServerManaged = "server",
    };

    private static Dictionary<string, JsonElement> Patch(string json) =>
        JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)!;

    [Fact]
    public void CopyValues_copies_writable_properties()
    {
        var target = Existing();
        var source = new Widget { Name = "New", Notes = null, Quantity = 9, Price = 2.5m, Color = WidgetColor.Blue, IsEnabled = true };

        _mapper.CopyValues(source, target);

        Assert.Equal("New", target.Name);
        Assert.Null(target.Notes);
        Assert.Equal(9, target.Quantity);
        Assert.Equal(2.5m, target.Price);
        Assert.Equal(WidgetColor.Blue, target.Color);
        Assert.True(target.IsEnabled);
    }

    [Fact]
    public void CopyValues_never_overwrites_keys_audit_fields_or_read_only_properties()
    {
        var target = Existing();
        var source = new Widget { Id = 99, Name = "x", CreatedAtUtc = DateTime.MaxValue, UpdatedAtUtc = DateTime.MaxValue, ServerManaged = "client" };

        _mapper.CopyValues(source, target);

        Assert.Equal(7, target.Id);
        Assert.Equal(Created, target.CreatedAtUtc);
        Assert.Null(target.UpdatedAtUtc);
        Assert.Equal("server", target.ServerManaged);
    }

    [Fact]
    public void ApplyPatch_changes_only_supplied_properties()
    {
        var target = Existing();

        _mapper.ApplyPatch(target, Patch("""{ "quantity": 42, "Color": "Green" }"""));

        Assert.Equal(42, target.Quantity);
        Assert.Equal(WidgetColor.Green, target.Color);
        Assert.Equal("Original", target.Name);
        Assert.Equal("keep", target.Notes);
    }

    [Fact]
    public void ApplyPatch_can_set_nullable_properties_to_null()
    {
        var target = Existing();

        _mapper.ApplyPatch(target, Patch("""{ "notes": null }"""));

        Assert.Null(target.Notes);
    }

    [Fact]
    public void ApplyPatch_rejects_null_for_non_nullable_properties()
    {
        var target = Existing();

        var exception = Assert.Throws<RequestValidationException>(() => _mapper.ApplyPatch(target, Patch("""{ "name": null }""")));

        Assert.True(exception.Errors.ContainsKey("name"));
        Assert.Equal("Original", target.Name);
    }

    [Fact]
    public void ApplyPatch_rejects_unknown_and_read_only_properties_without_changing_anything()
    {
        var target = Existing();

        var exception = Assert.Throws<RequestValidationException>(() =>
            _mapper.ApplyPatch(target, Patch("""{ "id": 5, "createdAtUtc": "2020-01-01", "bogus": 1, "quantity": 3 }""")));

        Assert.Contains("read-only", Assert.Single(exception.Errors["id"]));
        Assert.Contains("read-only", Assert.Single(exception.Errors["createdAtUtc"]));
        Assert.Contains("not a known property", Assert.Single(exception.Errors["bogus"]));
        Assert.Equal(1, target.Quantity);
    }

    [Fact]
    public void ApplyPatch_rejects_values_of_the_wrong_type()
    {
        var target = Existing();

        var exception = Assert.Throws<RequestValidationException>(() => _mapper.ApplyPatch(target, Patch("""{ "quantity": "many" }""")));

        Assert.True(exception.Errors.ContainsKey("quantity"));
    }

    [Fact]
    public void ApplyPatch_runs_data_annotation_validation()
    {
        var target = Existing();

        var exception = Assert.Throws<RequestValidationException>(() =>
            _mapper.ApplyPatch(target, Patch("""{ "quantity": 5000, "name": "" }""")));

        Assert.True(exception.Errors.ContainsKey("quantity"));
        Assert.True(exception.Errors.ContainsKey("name"));
    }

    [Fact]
    public void ApplyPatch_with_an_empty_document_is_a_no_op()
    {
        var target = Existing();

        _mapper.ApplyPatch(target, Patch("{}"));

        Assert.Equal("Original", target.Name);
    }

    [Fact]
    public void Custom_json_options_are_used()
    {
        var mapper = new ReflectionEntityMapper(new JsonSerializerOptions { PropertyNameCaseInsensitive = false });
        var target = Existing();

        // no string enum converter here, so only numbers work
        mapper.ApplyPatch(target, Patch("""{ "color": 2 }"""));

        Assert.Equal(WidgetColor.Blue, target.Color);
    }
}
