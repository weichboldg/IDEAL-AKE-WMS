using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace IdealAkeWms.Models;

/// <summary>
/// Persistent BOM cache header — one entry per device article number.
/// NOT an AuditableEntity — pure cache table.
/// </summary>
public class CachedBomHeader
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Artikelnummer { get; set; } = string.Empty;

    /// <summary>"SAGE" or "OSEON"</summary>
    [Required]
    [MaxLength(20)]
    public string Source { get; set; } = string.Empty;

    public int ItemCount { get; set; }

    /// <summary>SHA256 hex of the sorted BOM item list.</summary>
    [Required]
    [MaxLength(64)]
    public string ContentHash { get; set; } = string.Empty;

    public DateTime CachedAt { get; set; }

    public ICollection<CachedBomItem> Items { get; set; } = new List<CachedBomItem>();
}
