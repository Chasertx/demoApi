namespace ModApi.Application.Querying;

/// <summary>A single property filter, e.g. <c>price:gte=10</c>.</summary>
/// <param name="Property">Entity property name (case-insensitive).</param>
/// <param name="Operator">Comparison operator.</param>
/// <param name="Value">Raw value; converted to the property type when the query is built. <c>null</c> matches null.</param>
public sealed record FilterCriterion(string Property, FilterOperator Operator, string Value);
