using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using ModApi.Domain.Common;

namespace ModApi.Domain.Entities;

/// <summary>A product category. Sample entity exposed at <c>api/categories</c>.</summary>
[CrudResource("categories")]
public class Category : EntityBase<int>
{
    /// <summary>Display name of the category.</summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Optional free-text description.</summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>Products in this category (not serialized by the generic API).</summary>
    [JsonIgnore]
    public ICollection<Product> Products { get; set; } = new List<Product>();
}
