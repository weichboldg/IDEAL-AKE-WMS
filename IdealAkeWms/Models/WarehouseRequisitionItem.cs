using System.ComponentModel.DataAnnotations;

namespace IdealAkeWms.Models;

public class WarehouseRequisitionItem : AuditableEntity
{
    [Required]
    public int WarehouseRequisitionId { get; set; }
    public WarehouseRequisition WarehouseRequisition { get; set; } = null!;

    [Required, StringLength(100)]
    public string ArticleNumber { get; set; } = string.Empty;

    [Required, StringLength(500)]
    public string ArticleDescription { get; set; } = string.Empty;

    [StringLength(20)]
    public string? Unit { get; set; }

    public decimal QuantityRequested { get; set; }
    public decimal? QuantityPicked { get; set; }

    public int Position { get; set; }

    /// <summary>
    /// Notiz vom Lagermitarbeiter zur Position. Wird auf dem Druck angezeigt.
    /// </summary>
    [StringLength(500)]
    public string? Note { get; set; }

    /// <summary>
    /// True = Lagermitarbeiter hat dieses Item als endgueltigen Fehlteil markiert.
    /// Wird beim Status-Ableitungs-Helper geprueft; treibt Status auf Closed wenn
    /// alle "kurzen" Items markiert sind.
    /// </summary>
    public bool IsFinalShortage { get; set; }
}
