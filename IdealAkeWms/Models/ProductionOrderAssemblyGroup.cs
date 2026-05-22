using System.ComponentModel.DataAnnotations;

namespace IdealAkeWms.Models;

public class ProductionOrderAssemblyGroup : AuditableEntity
{
    public int ProductionOrderId { get; set; }
    public ProductionOrder ProductionOrder { get; set; } = null!;

    /// <summary>VK / VL / VE / VT / VA</summary>
    [Required]
    [StringLength(10)]
    public string GroupKey { get; set; } = string.Empty;

    public bool IsApplicable { get; set; }

    /// <summary>Phase 4 — bleibt in Phase 1 immer false.</summary>
    public bool IsCompleted { get; set; }

    public DateTime? CompletedAt { get; set; }

    [StringLength(200)]
    public string? CompletedBy { get; set; }

    public ICollection<ProductionOrderAssemblyGroupSpec> Specs { get; set; } =
        new List<ProductionOrderAssemblyGroupSpec>();
}
