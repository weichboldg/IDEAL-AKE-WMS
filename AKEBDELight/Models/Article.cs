using System.ComponentModel.DataAnnotations;

namespace AKEBDELight.Models;

public class Article : AuditableEntity
{
    [Required(ErrorMessage = "Artikelnummer ist erforderlich")]
    [StringLength(100)]
    [Display(Name = "Artikelnummer")]
    public string ArticleNumber { get; set; } = string.Empty;

    [StringLength(500)]
    [Display(Name = "Bezeichnung")]
    public string? Description { get; set; }

    [StringLength(20)]
    [Display(Name = "Einheit")]
    public string? Unit { get; set; }

    [Display(Name = "Meldebestand")]
    public decimal? ReorderLevel { get; set; }

    public ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
}
