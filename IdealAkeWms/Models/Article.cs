using System.ComponentModel.DataAnnotations;

namespace IdealAkeWms.Models;

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

    [StringLength(100)]
    [Display(Name = "Artikelgruppe")]
    public string? ArticleGroup { get; set; }

    [Display(Name = "Meldebestand")]
    public decimal? ReorderLevel { get; set; }

    [Display(Name = "Kategorie")]
    public int? ArticleCategoryId { get; set; }
    public ArticleCategory? ArticleCategory { get; set; }

    [Display(Name = "Hauptlagerplatz")]
    public int? PrimaryStorageLocationId { get; set; }
    public StorageLocation? PrimaryStorageLocation { get; set; }

    /// <summary>Sage-Rohcode (KHKLagerplaetze.Kurzbezeichnung). Nicht leer ⇒ aus Sage ⇒ in der App gesperrt.</summary>
    [StringLength(100)]
    public string? SagePrimaryStorageLocation { get; set; }

    public ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
    public ICollection<ArticleAttributeValue> AttributeValues { get; set; } = new List<ArticleAttributeValue>();
}
