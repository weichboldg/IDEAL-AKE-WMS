using System.ComponentModel.DataAnnotations;

namespace IdealAkeWms.Models;

public class FaAttributeOption : AuditableEntity
{
    public int FaAttributeDefinitionId { get; set; }
    public FaAttributeDefinition Definition { get; set; } = null!;

    [Required]
    [StringLength(200)]
    [Display(Name = "Wert")]
    public string Value { get; set; } = string.Empty;

    [Display(Name = "Reihenfolge")]
    public int SortOrder { get; set; }

    [Display(Name = "Aktiv")]
    public bool IsActive { get; set; } = true;
}
