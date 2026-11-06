using ModApi.Application.Querying;
using ModApi.UnitTests.TestDoubles;

namespace ModApi.UnitTests.Application;

public class ValueParserTests
{
    [Theory]
    [InlineData("42", typeof(int), 42)]
    [InlineData("-7", typeof(long), -7L)]
    [InlineData("1.5", typeof(double), 1.5d)]
    [InlineData("true", typeof(bool), true)]
    [InlineData("FALSE", typeof(bool), false)]
    [InlineData("hello", typeof(string), "hello")]
    public void Parses_primitives(string raw, Type type, object expected)
    {
        Assert.True(ValueParser.TryParse(raw, type, out var value));
        Assert.Equal(expected, value);
    }

    [Fact]
    public void Parses_decimals_with_invariant_culture()
    {
        Assert.True(ValueParser.TryParse("19.99", typeof(decimal), out var value));
        Assert.Equal(19.99m, value);
    }

    [Theory]
    [InlineData("Green")]
    [InlineData("green")]
    [InlineData("1")]
    public void Parses_enums_by_name_or_number(string raw)
    {
        Assert.True(ValueParser.TryParse(raw, typeof(WidgetColor), out var value));
        Assert.Equal(WidgetColor.Green, value);
    }

    [Theory]
    [InlineData("Purple")]
    [InlineData("99")]
    public void Rejects_undefined_enum_values(string raw)
    {
        Assert.False(ValueParser.TryParse(raw, typeof(WidgetColor), out _));
    }

    [Fact]
    public void Parses_guids()
    {
        var guid = Guid.NewGuid();
        Assert.True(ValueParser.TryParse(guid.ToString(), typeof(Guid), out var value));
        Assert.Equal(guid, value);
    }

    [Fact]
    public void Dates_without_offset_are_treated_as_utc()
    {
        Assert.True(ValueParser.TryParse("2025-03-01T12:00:00", typeof(DateTime), out var value));
        var date = Assert.IsType<DateTime>(value);
        Assert.Equal(DateTimeKind.Utc, date.Kind);
        Assert.Equal(new DateTime(2025, 3, 1, 12, 0, 0, DateTimeKind.Utc), date);
    }

    [Fact]
    public void Dates_with_offset_are_converted_to_utc()
    {
        Assert.True(ValueParser.TryParse("2025-03-01T12:00:00+02:00", typeof(DateTime), out var value));
        Assert.Equal(new DateTime(2025, 3, 1, 10, 0, 0, DateTimeKind.Utc), value);
    }

    [Fact]
    public void Parses_date_time_offset_date_only_time_only_and_time_span()
    {
        Assert.True(ValueParser.TryParse("2025-03-01T12:00:00Z", typeof(DateTimeOffset), out var offset));
        Assert.Equal(new DateTimeOffset(2025, 3, 1, 12, 0, 0, TimeSpan.Zero), offset);

        Assert.True(ValueParser.TryParse("2025-03-01", typeof(DateOnly), out var date));
        Assert.Equal(new DateOnly(2025, 3, 1), date);

        Assert.True(ValueParser.TryParse("13:45", typeof(TimeOnly), out var time));
        Assert.Equal(new TimeOnly(13, 45), time);

        Assert.True(ValueParser.TryParse("01:30:00", typeof(TimeSpan), out var span));
        Assert.Equal(TimeSpan.FromMinutes(90), span);
    }

    [Theory]
    [InlineData(typeof(int?))]
    [InlineData(typeof(string))]
    [InlineData(typeof(Guid?))]
    public void Null_literal_is_accepted_for_nullable_targets(Type type)
    {
        Assert.True(ValueParser.TryParse("null", type, out var value));
        Assert.Null(value);
        Assert.True(ValueParser.TryParse(null, type, out value));
        Assert.Null(value);
    }

    [Fact]
    public void Null_literal_is_rejected_for_non_nullable_value_types()
    {
        Assert.False(ValueParser.TryParse("NULL", typeof(int), out _));
    }

    [Fact]
    public void Nullable_targets_parse_their_underlying_type()
    {
        Assert.True(ValueParser.TryParse("5", typeof(int?), out var value));
        Assert.Equal(5, value);
    }

    [Theory]
    [InlineData("abc", typeof(int))]
    [InlineData("1.5", typeof(int))]
    [InlineData("99999999999", typeof(int))]
    [InlineData("yes", typeof(bool))]
    [InlineData("not-a-guid", typeof(Guid))]
    [InlineData("not-a-date", typeof(DateTime))]
    [InlineData("not-a-date", typeof(DateTimeOffset))]
    [InlineData("not-a-date", typeof(DateOnly))]
    [InlineData("25:99", typeof(TimeOnly))]
    [InlineData("nope", typeof(TimeSpan))]
    public void Invalid_values_are_rejected(string raw, Type type)
    {
        Assert.False(ValueParser.TryParse(raw, type, out var value));
        Assert.Null(value);
    }

    [Fact]
    public void Types_without_a_string_converter_are_rejected()
    {
        Assert.False(ValueParser.TryParse("x", typeof(Widget), out _));
    }
}
