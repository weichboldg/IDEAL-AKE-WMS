using System.ComponentModel.DataAnnotations;

namespace IdealAkeWms.Models;

public class WorkStep : AuditableEntity
{
    [Required(ErrorMessage = "Code ist erforderlich")]
    [StringLength(20)]
    [Display(Name = "Code")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Name ist erforderlich")]
    [StringLength(100)]
    [Display(Name = "Name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    [Display(Name = "Suchbegriffe (kommasepariert)")]
    public string? SearchString { get; set; }

    [Display(Name = "Reihenfolge")]
    public int SortOrder { get; set; }

    [Display(Name = "Aktiv")]
    public bool IsActive { get; set; } = true;
}
