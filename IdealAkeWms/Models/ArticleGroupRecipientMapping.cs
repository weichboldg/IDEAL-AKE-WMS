using System.ComponentModel.DataAnnotations;

namespace IdealAkeWms.Models;

public class ArticleGroupRecipientMapping : AuditableEntity
{
    [Required]
    [StringLength(100)]
    public string ArticleGroup { get; set; } = string.Empty;

    public int OrderRecipientGroupId { get; set; }
    public OrderRecipientGroup OrderRecipientGroup { get; set; } = null!;
}
