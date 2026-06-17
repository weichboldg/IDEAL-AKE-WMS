using System.ComponentModel.DataAnnotations;
using IdealAkeWms.Models;

namespace IdealAkeWms.Models.ViewModels;

public class UserEditViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Name ist erforderlich")]
    [StringLength(200)]
    [Display(Name = "Name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(50)]
    [Display(Name = "Personalnummer")]
    public string? PersonalNumber { get; set; }

    [Display(Name = "Aktiv")]
    public bool IsActive { get; set; } = true;

    [StringLength(200)]
    [EmailAddress]
    [Display(Name = "E-Mail")]
    public string? Email { get; set; }

    [Display(Name = "Meldebestand-Benachrichtigung")]
    public bool NotifyOnReorderLevel { get; set; }

    [StringLength(100)]
    [Display(Name = "Standard-Filter Beschaffung")]
    public string? DefaultFilterBeschaffung { get; set; }

    [StringLength(100)]
    [Display(Name = "Standard-Filter Artikelgruppe")]
    public string? DefaultFilterArtikelgruppe { get; set; }

    [Display(Name = "Rekursive Suche bei aktiver Filterung")]
    public bool RecursiveFilterSearch { get; set; }

    [Display(Name = "Ist Kommissionierer")]
    public bool IsPicker { get; set; }

    [Display(Name = "Eintraege pro Seite (Standard)")]
    public int? DefaultPageSize { get; set; }

    /// <summary>
    /// Vorausgewaehlter Arbeitsgang in der FA-Abarbeitungsliste. NULL = keiner.
    /// </summary>
    [Display(Name = "Standard FA-Vorbau-AG (FA-Abarbeitungsliste)")]
    public int? DefaultWorkStepId { get; set; }

    /// <summary>Aktive Arbeitsgaenge fuer das Dropdown.</summary>
    public List<WorkStep> AvailableWorkSteps { get; set; } = new();

    /// <summary>
    /// Vorausgewaehlter Werkbank-Zusatzfilter in der FA-Abarbeitungsliste. NULL = alle.
    /// </summary>
    [Display(Name = "Standard-Werkbank (FA-Abarbeitungsliste)")]
    public int? DefaultWorkplaceId { get; set; }

    /// <summary>Werkbaenke fuer das Dropdown.</summary>
    public List<ProductionWorkplace> AvailableWorkplaces { get; set; } = new();

    public List<RoleCheckboxItem> AvailableRoles { get; set; } = new();
    public List<int> SelectedRoleIds { get; set; } = new();

    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string CreatedByWindows { get; set; } = string.Empty;
}

public class RoleCheckboxItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public bool IsSelected { get; set; }
}
