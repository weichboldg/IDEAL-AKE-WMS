using System.ComponentModel.DataAnnotations;

namespace AKEBDELight.Models.ViewModels;

public class StockMovementCreateViewModel
{
    [Required(ErrorMessage = "Artikel ist erforderlich")]
    [Display(Name = "Artikel")]
    public int ArticleId { get; set; }

    [Required(ErrorMessage = "Menge ist erforderlich")]
    [Range(0.001, double.MaxValue, ErrorMessage = "Menge muss größer als 0 sein")]
    [Display(Name = "Menge")]
    public decimal Quantity { get; set; }

    [Required(ErrorMessage = "Lagerplatz ist erforderlich")]
    [Display(Name = "Lagerplatz")]
    public int StorageLocationId { get; set; }

    [StringLength(100)]
    [Display(Name = "Fertigungsauftrag")]
    public string? ProductionOrder { get; set; }

    [Display(Name = "Benutzer")]
    public int? UserId { get; set; }

    public string? ArticleDisplay { get; set; }

    public List<Article> Articles { get; set; } = new();
    public List<StorageLocation> StorageLocations { get; set; } = new();
    public List<User> Users { get; set; } = new();
}
