namespace ModApi.Domain.Entities;

/// <summary>Lifecycle status of a <see cref="Product"/>.</summary>
public enum ProductStatus
{
    /// <summary>Not yet published.</summary>
    Draft = 0,

    /// <summary>Available for sale.</summary>
    Active = 1,

    /// <summary>No longer sold.</summary>
    Discontinued = 2,
}
