using System.ComponentModel.DataAnnotations;

namespace IdealAkeWms.Models;

public class ProductionOrderBdeStatus : AuditableEntity
{
    public int ProductionOrderId { get; set; }
    public ProductionOrder ProductionOrder { get; set; } = null!;

    [Display(Name = "BDE abgeschlossen")]
    public bool IsDoneBde { get; set; }
}
