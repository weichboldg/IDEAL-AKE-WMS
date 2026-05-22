using System.ComponentModel.DataAnnotations;

namespace IdealAkeWms.Models;

public class ProductionWorkplaceAssemblyGroup : AuditableEntity
{
    public int ProductionWorkplaceId { get; set; }
    public ProductionWorkplace ProductionWorkplace { get; set; } = null!;

    [Required]
    [StringLength(10)]
    public string GroupKey { get; set; } = string.Empty;
}
