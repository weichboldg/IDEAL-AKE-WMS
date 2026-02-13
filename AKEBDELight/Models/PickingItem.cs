using System.ComponentModel.DataAnnotations;

namespace AKEBDELight.Models;

public class PickingItem : AuditableEntity
{
    [Required]
    public int ProductionOrderId { get; set; }

    [Required]
    [StringLength(100)]
    public string BomArticleNumber { get; set; } = string.Empty;

    [StringLength(50)]
    public string? BomPosition { get; set; }

    public decimal Quantity { get; set; }

    public int? SourceStorageLocationId { get; set; }

    public bool IsPicked { get; set; }
    public DateTime? PickedAt { get; set; }

    [StringLength(200)]
    public string? PickedBy { get; set; }

    [StringLength(200)]
    public string? PickedByWindows { get; set; }

    public bool IsTransferred { get; set; }
    public DateTime? TransferredAt { get; set; }

    public ProductionOrder ProductionOrder { get; set; } = null!;
    public StorageLocation? SourceStorageLocation { get; set; }
}
