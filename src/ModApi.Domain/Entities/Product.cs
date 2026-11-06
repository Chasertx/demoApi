using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using ModApi.Domain.Common;

namespace ModApi.Domain.Entities;

public class Product : EntityBase<int>
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(64)]
    public string Sku { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    [Range(0d, 1_000_000d)]
    public decimal Price { get; set; }

    [Range(0, int.MaxValue)]
    public int StockQuantity { get; set; }

    public ProductStatus Status { get; set; } = ProductStatus.Active;

    public int? CategoryId { get; set; }

    [JsonIgnore]
    public Category? Category { get; set; }
}
