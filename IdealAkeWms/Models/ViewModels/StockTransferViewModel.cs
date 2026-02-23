using System.ComponentModel.DataAnnotations;

namespace IdealAkeWms.Models.ViewModels;

public class StockTransferViewModel
{
    [Required(ErrorMessage = "Artikel ist erforderlich")]
    [Display(Name = "Artikel")]
    public int ArticleId { get; set; }

    [Required(ErrorMessage = "Menge ist erforderlich")]
    [Range(0.001, double.MaxValue, ErrorMessage = "Menge muss größer als 0 sein")]
    [Display(Name = "Menge")]
    public decimal Quantity { get; set; }

    [Required(ErrorMessage = "Quell-Lagerplatz ist erforderlich")]
    [Display(Name = "Von Lagerplatz")]
    public int SourceStorageLocationId { get; set; }

    [Required(ErrorMessage = "Ziel-Lagerplatz ist erforderlich")]
    [Display(Name = "Auf Lagerplatz")]
    public int StorageLocationId { get; set; }

    [StringLength(100)]
    [Display(Name = "Fertigungsauftrag")]
    public string? ProductionOrder { get; set; }

    public string? ArticleDisplay { get; set; }
    public List<StorageLocation> StorageLocations { get; set; } = new();
}
