using System.ComponentModel.DataAnnotations;
using ModApi.Domain.Common;

namespace ModApi.Domain.Entities;

/// <summary>A customer. Sample entity with a <see cref="Guid"/> key, exposed at <c>api/customers</c>.</summary>
public class Customer : EntityBase<Guid>
{
    /// <summary>Given name.</summary>
    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    /// <summary>Family name.</summary>
    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    /// <summary>Email address; unique across all customers.</summary>
    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    /// <summary>Optional phone number.</summary>
    [Phone]
    [MaxLength(32)]
    public string? PhoneNumber { get; set; }

    /// <summary>Whether the customer account is active.</summary>
    public bool IsActive { get; set; } = true;
}
