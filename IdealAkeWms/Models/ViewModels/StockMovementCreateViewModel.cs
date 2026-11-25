using System.ComponentModel.DataAnnotations;

namespace IdealAkeWms.Models.ViewModels;

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

    /// <summary>v1.99.68: Werkbank-Name, nur in der Maske Ausbuchung gerendert und ausgewertet; leer = keine.</summary>
    [StringLength(200)]
    [Display(Name = "Abteilung")]
    public string? Abteilung { get; set; }

    /// <summary>v1.99.68: Auswahlliste Abteilung = Werkbank-Namen ohne „nicht in Sage“.</summary>
    public List<string> Abteilungen { get; set; } = new();

    public List<Article> Articles { get; set; } = new();
    public List<StorageLocation> StorageLocations { get; set; } = new();
    public List<User> Users { get; set; } = new();
}
