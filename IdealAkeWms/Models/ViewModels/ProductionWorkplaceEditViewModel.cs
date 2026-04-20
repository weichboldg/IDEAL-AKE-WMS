using System.ComponentModel.DataAnnotations;

namespace IdealAkeWms.Models.ViewModels;

public class ProductionWorkplaceEditViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Bezeichnung ist erforderlich.")]
    [MaxLength(200)]
    [Display(Name = "Bezeichnung")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(200)]
    [Display(Name = "Halle")]
    public string? Hall { get; set; }

    [Display(Name = "Abweichende Vorkommissioniertage")]
    [Range(0, 365, ErrorMessage = "Muss zwischen 0 und 365 liegen.")]
    public int? OverridePrePickingDays { get; set; }

    [Display(Name = "BDE aktiv")]
    public bool BdeAktiv { get; set; }

    [MaxLength(200)]
    [Display(Name = "Default-Arbeitsgang (BDE)")]
    public string? BdeDefaultArbeitsgang { get; set; }

    [Display(Name = "Zugeordnete Benutzer")]
    public List<int> SelectedUserIds { get; set; } = new();

    public List<User> AvailableUsers { get; set; } = new();
}
