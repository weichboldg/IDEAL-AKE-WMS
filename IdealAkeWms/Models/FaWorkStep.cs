namespace IdealAkeWms.Models;

public static class FaWorkStepSources
{
    public const string Sync = "Sync";
    public const string Manual = "Manual";
}

public class FaWorkStep : AuditableEntity
{
    public int ProductionOrderId { get; set; }
    public ProductionOrder ProductionOrder { get; set; } = null!;

    public int WorkStepId { get; set; }
    public WorkStep WorkStep { get; set; } = null!;

    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? CompletedBy { get; set; }

    public string Source { get; set; } = FaWorkStepSources.Manual; // NVARCHAR(20)

    /// <summary>Manuell abgewaehlt — Sync darf NICHT re-adden.</summary>
    public bool IsRemoved { get; set; }

    public ICollection<FaWorkStepSpec> Specs { get; set; } = new List<FaWorkStepSpec>();
}
