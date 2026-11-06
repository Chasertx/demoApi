using System.ComponentModel.DataAnnotations;
using ModApi.Domain.Common;

namespace ModApi.Domain.Entities;

public class Customer : EntityBase<Guid>
{
    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Phone]
    [MaxLength(32)]
    public string? PhoneNumber { get; set; }

    public bool IsActive { get; set; } = true;
}
