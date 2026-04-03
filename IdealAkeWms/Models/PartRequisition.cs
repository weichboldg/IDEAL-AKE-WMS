using System.ComponentModel.DataAnnotations;

namespace IdealAkeWms.Models;

public class PartRequisition : AuditableEntity
{
    public int ProductionOrderId { get; set; }

    [Required]
    [StringLength(100)]
    public string ArticleNumber { get; set; } = string.Empty;

    [StringLength(500)]
    public string? ArticleDescription { get; set; }

    [StringLength(100)]
    public string? ArticleGroup { get; set; }

    [StringLength(50)]
    public string? Position { get; set; }

    public decimal Quantity { get; set; }

    [StringLength(20)]
    public string? Unit { get; set; }

    [Required]
    [StringLength(20)]
    public string Status { get; set; } = PartRequisitionStatus.Offen;

    [Required]
    [StringLength(20)]
    public string Priority { get; set; } = PartRequisitionPriority.Normal;

    [StringLength(1000)]
    public string? Notes { get; set; }

    public int? OrderRecipientGroupId { get; set; }

    [StringLength(1000)]
    public string? SentToEmails { get; set; }

    public DateTime? EmailSentAt { get; set; }

    public int? FulfilledByStockMovementId { get; set; }
    public DateTime? FulfilledAt { get; set; }

    public DateTime? CancelledAt { get; set; }

    [StringLength(200)]
    public string? CancelledBy { get; set; }

    // Navigation
    public ProductionOrder ProductionOrder { get; set; } = null!;
    public OrderRecipientGroup? OrderRecipientGroup { get; set; }
    public StockMovement? FulfilledByStockMovement { get; set; }
}
