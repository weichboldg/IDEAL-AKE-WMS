using System.ComponentModel.DataAnnotations;

namespace IdealAkeWms.Models;

/// <summary>Felder identisch zu bisherigem ProductionOrderAssemblyGroupSpec.</summary>
public class FaWorkStepSpec : AuditableEntity
{
    public int FaWorkStepId { get; set; }
    public FaWorkStep FaWorkStep { get; set; } = null!;

    public int? ArticleId { get; set; }
    public Article? Article { get; set; }

    [Required]
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    public decimal? Quantity { get; set; }
    public string? Notes { get; set; }
    public int SortOrder { get; set; }
}
