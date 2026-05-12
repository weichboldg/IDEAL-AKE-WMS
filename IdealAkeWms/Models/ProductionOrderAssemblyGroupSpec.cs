using System.ComponentModel.DataAnnotations;

namespace IdealAkeWms.Models;

public class ProductionOrderAssemblyGroupSpec : AuditableEntity
{
    public int AssemblyGroupId { get; set; }
    public ProductionOrderAssemblyGroup AssemblyGroup { get; set; } = null!;

    public int? ArticleId { get; set; }
    public Article? Article { get; set; }

    [Required]
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    public decimal? Quantity { get; set; }

    public string? Notes { get; set; }

    public int SortOrder { get; set; }
}
