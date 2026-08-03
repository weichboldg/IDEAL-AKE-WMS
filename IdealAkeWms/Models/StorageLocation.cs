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

    /// <summary>
    /// User-controlled Opt-in: duerfen manuelle Ein-/Ausbuchungen dieses Platzes an Sage
    /// gemeldet werden? Default false, kumulativ (UND) zum globalen Toggle
    /// <c>SageLagerbuchungAktiv</c> und unabhaengig von <see cref="IstBuchbar"/>.
    /// </summary>
    [Display(Name = "Sage-Buchung erlaubt")]
    public bool SageBuchungErlaubt { get; set; }

    /// <summary>
    /// Volle Sage-Kurzbezeichnung des Platzes inkl. Ebenen (Format
    /// <c>Lagerkennung;Reihe;Platz;Ebene</c>, z.B. "LL;1;4;0") — identisch zu
    /// <see cref="Code"/> fuer Sage-Plaetze. Wird vom Lagerplatz-Sync befuellt und als
    /// <c>Herkunft-/ZielLagerkennung</c> im SData-Payload gesendet (NICHT das frei
    /// editierbare <see cref="Zone"/>). Bei manuellen Plaetzen null.
    /// </summary>
    [StringLength(50)]
    [Display(Name = "Sage-Lagerkennung")]
    public string? SageLagerkennung { get; set; }

    /// <summary>
    /// Sage-interne numerische Lagerplatz-Id (<c>KHKLagerplaetze.PlatzID</c>), von der SData-API
    /// als <c>Herkunft-/ZielLagerplatzId</c> erwartet. Vom Lagerplatz-Sync befuellt, bei
    /// manuellen Plaetzen null.
    /// </summary>
    [Display(Name = "Sage-Lagerplatz-Id")]
    public int? SageLagerplatzId { get; set; }

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
