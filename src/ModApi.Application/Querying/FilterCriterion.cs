namespace ModApi.Application.Querying;

public sealed record FilterCriterion(string Property, FilterOperator Operator, string Value);
