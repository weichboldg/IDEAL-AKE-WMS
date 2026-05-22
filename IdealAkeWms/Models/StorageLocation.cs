using System.ComponentModel.DataAnnotations;

namespace IdealAkeWms.Models;

public class StorageLocation : AuditableEntity, IValidatableObject
{
    /// <summary>
    /// DB-Spalte ist NVARCHAR(50) — manuelle Eintraege werden zusaetzlich per
    /// <see cref="Validate"/> auf 12 Zeichen begrenzt (Barcode-Lesbarkeit).
    /// Sage-Codes nutzen den vollen Platz.
    /// </summary>
    [Required(ErrorMessage = "Lagerplatz-Code ist erforderlich")]
    [StringLength(50, ErrorMessage = "Code darf maximal 50 Zeichen lang sein.")]
    [Display(Name = "Code")]
    public string Code { get; set; } = string.Empty;

    [StringLength(200)]
    [Display(Name = "Bezeichnung")]
    public string? Description { get; set; }

    [StringLength(100)]
    [Display(Name = "Bereich/Zone")]
    public string? Zone { get; set; }

    [Display(Name = "Kapazität")]
    [Range(0, double.MaxValue, ErrorMessage = "Kapazität muss positiv sein")]
    public decimal? Capacity { get; set; }

    [StringLength(50)]
    [Display(Name = "Barcode-Wert")]
    public string? BarcodeValue { get; set; }

    [Display(Name = "Kommissionierwagen")]
    public bool IsPickingTransport { get; set; }

    [StringLength(20)]
    [Display(Name = "Quelle")]
    public string Source { get; set; } = StorageLocationSource.Manual;

    [Display(Name = "Aktiv")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Buchbar")]
    public bool IstBuchbar { get; set; } = true;

    public ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // Manuelle Eintraege bleiben auf 12 Zeichen begrenzt; Sage-Codes duerfen bis 50.
        if (Source == StorageLocationSource.Manual && !string.IsNullOrEmpty(Code) && Code.Length > 12)
        {
            yield return new ValidationResult(
                "Manuelle Lagerplatz-Codes duerfen maximal 12 Zeichen lang sein (Barcode-Lesbarkeit). Sage-synchronisierte Codes duerfen bis 50 Zeichen.",
                new[] { nameof(Code) });
        }
    }
}
