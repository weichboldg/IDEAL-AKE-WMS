using System.ComponentModel.DataAnnotations;
using IdealAkeWms.Models;

namespace IdealAkeWms.Models.ViewModels;

public class ProfileViewModel
{
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Personalnummer")]
    public string? PersonalNumber { get; set; }

    [StringLength(100)]
    [Display(Name = "Standard-Filter Beschaffung")]
    public string? DefaultFilterBeschaffung { get; set; }

    [StringLength(100)]
    [Display(Name = "Standard-Filter Artikelgruppe")]
    public string? DefaultFilterArtikelgruppe { get; set; }

    [Display(Name = "Rekursive Suche bei aktiver Filterung")]
    public bool RecursiveFilterSearch { get; set; }

    [StringLength(200)]
    [EmailAddress]
    [Display(Name = "E-Mail")]
    public string? Email { get; set; }

    [Display(Name = "Meldebestand-Benachrichtigung")]
    public bool NotifyOnReorderLevel { get; set; }

    /// <summary>
    /// User-Default fuer Seitengroesse in Listen. NULL = System-Default (25).
    /// Erlaubt: 25, 50, 100, 0 (= Alle).
    /// </summary>
    [Display(Name = "Eintraege pro Seite (Standard)")]
    public int? DefaultPageSize { get; set; }

    /// <summary>
    /// Vorausgewaehlter Arbeitsgang in der FA-Abarbeitungsliste. NULL = keiner.
    /// </summary>
    [Display(Name = "Standard-Arbeitsgang (FA-Abarbeitungsliste)")]
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
}
