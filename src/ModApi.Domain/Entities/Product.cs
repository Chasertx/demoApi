using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using ModApi.Domain.Common;

namespace ModApi.Domain.Entities;

/// <summary>A sellable product. Sample entity exposed at <c>api/products</c>.</summary>
public class Product : EntityBase<int>
{
    /// <summary>Display name.</summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Stock keeping unit; unique across all products.</summary>
    [Required]
    [MaxLength(64)]
    public string Sku { get; set; } = string.Empty;

    /// <summary>Optional long description.</summary>
    [MaxLength(2000)]
    public string? Description { get; set; }

    /// <summary>Unit price.</summary>
    [Range(0d, 1_000_000d)]
    public decimal Price { get; set; }

    /// <summary>Units currently in stock.</summary>
    [Range(0, int.MaxValue)]
    public int StockQuantity { get; set; }

    /// <summary>Lifecycle status.</summary>
    public ProductStatus Status { get; set; } = ProductStatus.Active;

    /// <summary>Optional category foreign key.</summary>
    public int? CategoryId { get; set; }

    /// <summary>The category navigation (not serialized by the generic API).</summary>
    [JsonIgnore]
    public Category? Category { get; set; }
}
