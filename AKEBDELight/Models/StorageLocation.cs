using System.ComponentModel.DataAnnotations;

namespace AKEBDELight.Models;

public class StorageLocation : AuditableEntity
{
    [Required(ErrorMessage = "Lagerplatz-Code ist erforderlich")]
    [StringLength(50)]
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

    public ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
}
