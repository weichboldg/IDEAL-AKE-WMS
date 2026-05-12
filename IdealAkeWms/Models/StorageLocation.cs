using System.ComponentModel.DataAnnotations;

namespace IdealAkeWms.Models;

public class StorageLocation : AuditableEntity
{
    [Required(ErrorMessage = "Lagerplatz-Code ist erforderlich")]
    [StringLength(12, ErrorMessage = "Code darf maximal 12 Zeichen lang sein (Barcode-Lesbarkeit).")]
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
}
