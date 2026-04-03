using System.ComponentModel.DataAnnotations;

namespace IdealAkeWms.Models;

public class OrderRecipientGroup : AuditableEntity
{
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    public ICollection<OrderRecipient> Recipients { get; set; } = new List<OrderRecipient>();
    public ICollection<ArticleGroupRecipientMapping> ArticleGroupMappings { get; set; } = new List<ArticleGroupRecipientMapping>();
}
