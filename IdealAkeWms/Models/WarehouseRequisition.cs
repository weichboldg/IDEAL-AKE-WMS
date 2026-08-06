using System.ComponentModel.DataAnnotations;

namespace IdealAkeWms.Models;

public class WarehouseRequisition : AuditableEntity
{
    [Required]
    public int ProductionWorkplaceId { get; set; }
    public ProductionWorkplace ProductionWorkplace { get; set; } = null!;

    public WarehouseRequisitionStatus Status { get; set; } = WarehouseRequisitionStatus.Draft;

    /// <summary>Bestelltyp (Lager/Glas, v1.25.0). Wird bei Anlage gesetzt, kein Wechsel.</summary>
    public WarehouseRequisitionType Type { get; set; } = WarehouseRequisitionType.Lager;

    public int? CreatedByUserId { get; set; }

    public int? OrderRecipientGroupId { get; set; }
    public OrderRecipientGroup? OrderRecipientGroup { get; set; }

    public DateTime? SubmittedAt { get; set; }
    public int? SubmittedByUserId { get; set; }

    public DateTime? ClosedAt { get; set; }
    public int? ClosedByUserId { get; set; }

    public DateTime? CancelledAt { get; set; }
    public int? CancelledByUserId { get; set; }

    [StringLength(500)]
    public string? CancellationReason { get; set; }

    /// <summary>
    /// Freitext-Kommentar auf Kopf-Ebene der Bestellung (v1.29). Nur vom Besteller/Ersteller
    /// im Draft editierbar; read-only fuer das Lager (Eingehende Listen + Details) und im Submit-Mail.
    /// </summary>
    [StringLength(1000)]
    public string? Comment { get; set; }

    public DateTime? EmailSentAt { get; set; }
    public DateTime? CancellationEmailSentAt { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public ICollection<WarehouseRequisitionItem> Items { get; set; } = new List<WarehouseRequisitionItem>();
}
