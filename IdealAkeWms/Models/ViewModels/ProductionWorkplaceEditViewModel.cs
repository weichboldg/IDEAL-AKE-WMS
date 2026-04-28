using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

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

    [Display(Name = "Eigener Schichtplan")]
    public bool BdeUseCustomShiftPlan { get; set; }

    [ValidateNever]
    public List<BdeShift> CustomShifts { get; set; } = new();

    [ValidateNever]
    [Display(Name = "Zugeordnete Benutzer")]
    public List<int>? SelectedUserIds { get; set; }

    [ValidateNever]
    public List<User>? AvailableUsers { get; set; }
}
