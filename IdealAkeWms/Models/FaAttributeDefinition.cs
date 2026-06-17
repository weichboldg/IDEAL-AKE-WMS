using System.ComponentModel.DataAnnotations;

namespace IdealAkeWms.Models;

/// <summary>AttributeType-Enum aus ArticleAttributeDefinition.cs wird wiederverwendet.</summary>
public class FaAttributeDefinition : AuditableEntity
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

    public ICollection<FaAttributeOption> Options { get; set; } = new List<FaAttributeOption>();
    public ICollection<FaAttributeWorkStep> WorkSteps { get; set; } = new List<FaAttributeWorkStep>();
}
