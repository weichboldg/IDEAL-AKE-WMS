using System.ComponentModel.DataAnnotations;

namespace IdealAkeWms.Models.ViewModels;

public class RoleEditViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Schlüssel ist erforderlich")]
    [StringLength(50)]
    [Display(Name = "Schlüssel")]
    public string Key { get; set; } = string.Empty;

    [Required(ErrorMessage = "Name ist erforderlich")]
    [StringLength(100)]
    [Display(Name = "Name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    [Display(Name = "Beschreibung")]
    public string? Description { get; set; }

    [StringLength(200)]
    [Display(Name = "AD-Gruppe")]
    public string? AdGroup { get; set; }

    [Display(Name = "Sortierung")]
    public int SortOrder { get; set; }

    public bool IsSystem { get; set; }
    public int UserCount { get; set; }
}
