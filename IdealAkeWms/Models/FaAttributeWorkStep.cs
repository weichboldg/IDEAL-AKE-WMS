namespace IdealAkeWms.Models;

/// <summary>N:M Junction Merkmal-Definition ↔ Arbeitsgang (analog UserRole: AuditableEntity).</summary>
public class FaAttributeWorkStep : AuditableEntity
{
    public int FaAttributeDefinitionId { get; set; }
    public FaAttributeDefinition Definition { get; set; } = null!;

    public int WorkStepId { get; set; }
    public WorkStep WorkStep { get; set; } = null!;
}
