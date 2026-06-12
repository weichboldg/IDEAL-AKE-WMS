namespace IdealAkeWms.Models;

/// <summary>N:M Junction Werkbank ↔ Arbeitsgang (analog UserRole: AuditableEntity).</summary>
public class ProductionWorkplaceWorkStep : AuditableEntity
{
    public int ProductionWorkplaceId { get; set; }
    public ProductionWorkplace ProductionWorkplace { get; set; } = null!;

    public int WorkStepId { get; set; }
    public WorkStep WorkStep { get; set; } = null!;
}
