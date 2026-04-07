using System.ComponentModel.DataAnnotations;

namespace IdealAkeWms.Models;

/// <summary>
/// Persistent BOM cache item — detail row under a CachedBomHeader.
/// NOT an AuditableEntity — pure cache table.
/// </summary>
public class CachedBomItem
{
    public int Id { get; set; }

    public int CachedBomHeaderId { get; set; }
    public CachedBomHeader? CachedBomHeader { get; set; }

    [MaxLength(50)]
    public string? Position { get; set; }

    [MaxLength(200)]
    public string? Baugruppe { get; set; }

    [MaxLength(100)]
    public string? Ressourcenummer { get; set; }

    [MaxLength(500)]
    public string? Bezeichnung1 { get; set; }

    [MaxLength(500)]
    public string? Bezeichnung2 { get; set; }

    public decimal Menge { get; set; }

    [MaxLength(100)]
    public string? Beschaffungsartikel { get; set; }

    [MaxLength(100)]
    public string? Artikelgruppe { get; set; }

    /// <summary>Original order of items when synced (for stable display).</summary>
    public int SortOrder { get; set; }
}
