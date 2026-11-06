using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ModApi.Domain.Common;

namespace ModApi.UnitTests.TestDoubles;

public enum WidgetColor
{
    Red,
    Green,
    Blue,
}

public class Widget : EntityBase<int>
{
    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    public string? Notes { get; set; }

    [Range(0, 1000)]
    public int Quantity { get; set; }

    public decimal Price { get; set; }

    public bool IsEnabled { get; set; }

    public WidgetColor Color { get; set; }

    public DateTime? ShippedAtUtc { get; set; }

    public Guid? ExternalId { get; set; }

    [ReadOnly(true)]
    public string? ServerManaged { get; set; }

    [NotMapped]
    public string Computed => $"{Name}:{Quantity}";

    public ICollection<Gadget> Gadgets { get; set; } = new List<Gadget>();
}

public class Gadget : EntityBase<Guid>
{
    [Required]
    public string Label { get; set; } = string.Empty;

    public int? WidgetId { get; set; }
}

public class Tag : IEntity<string>
{
    public string Id { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;
}

public class Measurement : IEntity<long>
{
    public long Id { get; set; }

    public double Value { get; set; }
}

[CrudResource("custom-things")]
public class RoutedThing : EntityBase<int>
{
    public string Name { get; set; } = string.Empty;
}

[CrudResource(Expose = false)]
public class HiddenThing : EntityBase<int>
{
    public string Name { get; set; } = string.Empty;
}

public class OrderLine : EntityBase<int>
{
    public string Sku { get; set; } = string.Empty;
}

[CrudResource("order-lines")]
public class ConflictingOrderLine : EntityBase<int>
{
}

public class TwoKeys : IEntity<int>, IEntity<Guid>
{
    int IEntity<int>.Id { get; set; }

    Guid IEntity<Guid>.Id { get; set; }
}

public abstract class AbstractThing : EntityBase<int>
{
}

public class NotAnEntity
{
    public int Id { get; set; }
}
