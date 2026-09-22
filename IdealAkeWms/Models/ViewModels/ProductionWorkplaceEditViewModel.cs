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

    // Sage fuehrend (Baustein a): rein informativ, nie ueber diese Maske editiert. Gesetzt = die
    // Werkbank stammt aus den Sage-Arbeitsplaetzen und wird beim naechsten Sync ueberschrieben.
    [ValidateNever]
    [Display(Name = "Sage-Arbeitsplatznummer")]
    public string? SageArbeitsplatznummer { get; set; }

    [ValidateNever]
    [Display(Name = "Arbeitsschritt-Kürzel (Sage)")]
    public string? ArbeitsschrittCode { get; set; }

    /// <summary>Sage-gespeiste Werkbank (Sage fuehrend): Bezeichnung ist dann read-only.</summary>
    public bool IstSageGefuehrt => !string.IsNullOrWhiteSpace(SageArbeitsplatznummer);

    [ValidateNever]
    public List<BdeShift> CustomShifts { get; set; } = new();

    [ValidateNever]
    [Display(Name = "Zugeordnete Benutzer")]
    public List<int>? SelectedUserIds { get; set; }

    [ValidateNever]
    public List<User>? AvailableUsers { get; set; }

    /// <summary>Aktive Arbeitsgaenge fuer die Checkbox-Liste (POST-Wert kommt als int[] workStepIds).</summary>
    [ValidateNever]
    public List<WorkStep> AllWorkSteps { get; set; } = new();

    [ValidateNever]
    public List<int> SelectedWorkStepIds { get; set; } = new();
}
