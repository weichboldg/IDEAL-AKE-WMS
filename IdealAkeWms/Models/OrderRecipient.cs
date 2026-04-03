using System.ComponentModel.DataAnnotations;

namespace IdealAkeWms.Models;

public class OrderRecipient : AuditableEntity
{
    public int OrderRecipientGroupId { get; set; }

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(300)]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public OrderRecipientGroup OrderRecipientGroup { get; set; } = null!;
}
