using System.ComponentModel.DataAnnotations;

namespace IdealAkeWms.Models;

public enum AttributeType
{
    Boolean = 0,
    Dropdown = 1
}

public class ArticleAttributeDefinition : AuditableEntity
{
    [Required(ErrorMessage = "Name ist erforderlich")]
    [StringLength(200)]
    [Display(Name = "Name")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Typ")]
    public AttributeType AttributeType { get; set; }

    [Display(Name = "Reihenfolge")]
    public int SortOrder { get; set; }

    [Display(Name = "Aktiv")]
    public bool IsActive { get; set; } = true;

    [StringLength(50)]
    public string? SyncSource { get; set; }

    [StringLength(200)]
    public string? SyncFieldName { get; set; }

    public ICollection<ArticleAttributeOption> Options { get; set; } = new List<ArticleAttributeOption>();
    public ICollection<ArticleAttributeValue> Values { get; set; } = new List<ArticleAttributeValue>();
}
