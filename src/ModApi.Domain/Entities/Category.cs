using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using ModApi.Domain.Common;

namespace ModApi.Domain.Entities;

[CrudResource("categories")]
public class Category : EntityBase<int>
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [JsonIgnore]
    public ICollection<Product> Products { get; set; } = new List<Product>();
}
