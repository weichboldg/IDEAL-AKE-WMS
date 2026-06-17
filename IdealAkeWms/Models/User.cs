using System.ComponentModel.DataAnnotations;

namespace IdealAkeWms.Models;

public class User : AuditableEntity
{
    [Required(ErrorMessage = "Name ist erforderlich")]
    [StringLength(200)]
    [Display(Name = "Name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(50)]
    [Display(Name = "Personalnummer")]
    public string? PersonalNumber { get; set; }

    [Display(Name = "Aktiv")]
    public bool IsActive { get; set; } = true;

    [StringLength(500)]
    public string? PasswordHash { get; set; }

    [Display(Name = "Stammdaten-Zugriff")]
    public bool HasMasterDataAccess { get; set; }

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

    [Display(Name = "Administrator")]
    public bool IsAdmin { get; set; }

    [Display(Name = "Meldebestand-Benachrichtigung")]
    public bool NotifyOnReorderLevel { get; set; }

    [Display(Name = "Kommissionierung")]
    public bool CanPick { get; set; }

    [Display(Name = "Teileverfolgung anzeigen")]
    public bool CanViewTracking { get; set; }

    [Display(Name = "Arbeitsgänge rückmelden")]
    public bool CanReportOperations { get; set; }

    [Display(Name = "Ist Kommissionierer")]
    public bool IsPicker { get; set; }

    /// <summary>
    /// User-spezifischer Default fuer die Seitengroesse in Listen.
    /// Erlaubte Werte: 25, 50, 100, 0 (= Alle, gecappt auf 5000). NULL = System-Default 25.
    /// </summary>
    [Display(Name = "Eintraege pro Seite (Standard)")]
    public int? DefaultPageSize { get; set; }

    /// <summary>Vorausgewaehlter Arbeitsgang in der FA-Abarbeitungsliste (NULL = keiner).</summary>
    [Display(Name = "Standard FA-Vorbau-AG (FA-Abarbeitungsliste)")]
    public int? DefaultWorkStepId { get; set; }
    public WorkStep? DefaultWorkStep { get; set; }

    /// <summary>Vorausgewaehlter Werkbank-Zusatzfilter in der FA-Abarbeitungsliste (NULL = alle).</summary>
    [Display(Name = "Standard-Werkbank (FA-Abarbeitungsliste)")]
    public int? DefaultWorkplaceId { get; set; }
    public ProductionWorkplace? DefaultWorkplace { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

    public ICollection<WorkstationUser> WorkstationUsers { get; set; } = new List<WorkstationUser>();
    public ICollection<ProductionWorkplaceUser> ProductionWorkplaceUsers { get; set; } = new List<ProductionWorkplaceUser>();
    public ICollection<Workstation> DefaultWorkstations { get; set; } = new List<Workstation>();
    public ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
}
