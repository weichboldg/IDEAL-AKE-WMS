using System.ComponentModel.DataAnnotations;

namespace IdealAkeWms.Models;

public class ProductionOrder : AuditableEntity
{
    [Required(ErrorMessage = "FA Nummer ist erforderlich")]
    [StringLength(100)]
    [Display(Name = "FA Nummer")]
    public string OrderNumber { get; set; } = string.Empty;

    [Display(Name = "Stückzahl")]
    public decimal Quantity { get; set; }

    [StringLength(200)]
    [Display(Name = "Kunde")]
    public string? Customer { get; set; }

    [StringLength(100)]
    [Display(Name = "Artikelnummer")]
    public string? ArticleNumber { get; set; }

    [StringLength(500)]
    [Display(Name = "Bezeichnung 1")]
    public string? Description1 { get; set; }

    [StringLength(500)]
    [Display(Name = "Bezeichnung 2")]
    public string? Description2 { get; set; }

    [Display(Name = "Fertigungstermin")]
    [DataType(DataType.Date)]
    public DateTime? ProductionDate { get; set; }

    [Display(Name = "Liefertermin")]
    [DataType(DataType.Date)]
    public DateTime? DeliveryDate { get; set; }

    [Display(Name = "Erledigt")]
    public bool IsDone { get; set; }

    [StringLength(50)]
    [Display(Name = "Kommissionierung")]
    public string? PickingStatus { get; set; }

    [Display(Name = "Glas")]
    public bool HasGlass { get; set; }

    [Display(Name = "Zukauf")]
    public bool HasExternalPurchase { get; set; }

    /// <summary>
    /// True if the BOM contains at least one article in the Lackierteil category.
    /// Sync-calculated, read-only for users.
    /// </summary>
    public bool HasCoatingParts { get; set; } = false;

    /// <summary>
    /// User-toggleable: coating parts are handled / done.
    /// Reset to false by sync if HasCoatingParts flips to false.
    /// </summary>
    public bool IsCoatingDone { get; set; } = false;

    [Display(Name = "Werkbank")]
    public int? ProductionWorkplaceId { get; set; }
    public ProductionWorkplace? ProductionWorkplace { get; set; }

    [Display(Name = "Freigegeben")]
    public bool IsReleasedForPicking { get; set; }

    [Display(Name = "Priorität")]
    public int? PickingPriority { get; set; }

    [Display(Name = "Freigegeben am")]
    public DateTime? ReleasedAt { get; set; }

    [StringLength(200)]
    [Display(Name = "Freigegeben von")]
    public string? ReleasedBy { get; set; }

    [Display(Name = "Zugewiesener Kommissionierer")]
    public int? AssignedPickerId { get; set; }
    public User? AssignedPicker { get; set; }

    [StringLength(200)]
    [Display(Name = "Kommissionierer")]
    public string? AssignedPickerName { get; set; }

    public ICollection<WorkOperation> WorkOperations { get; set; } = new List<WorkOperation>();
}
