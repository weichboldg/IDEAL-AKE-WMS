using System.ComponentModel.DataAnnotations;

namespace AKEBDELight.Models;

public class StockMovement : AuditableEntity
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

    [Display(Name = "Bewegungsart")]
    public MovementType MovementType { get; set; }

    [Display(Name = "Zeitpunkt")]
    public DateTime Timestamp { get; set; }

    [Display(Name = "Benutzer")]
    public int? UserId { get; set; }

    [StringLength(200)]
    [Display(Name = "Windows-Benutzer")]
    public string WindowsUser { get; set; } = string.Empty;

    public Article Article { get; set; } = null!;
    public StorageLocation StorageLocation { get; set; } = null!;
    public User? User { get; set; }
}
