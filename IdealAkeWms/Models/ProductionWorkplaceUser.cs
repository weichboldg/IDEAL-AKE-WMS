namespace IdealAkeWms.Models;

public class ProductionWorkplaceUser : AuditableEntity
{
    public int ProductionWorkplaceId { get; set; }
    public ProductionWorkplace ProductionWorkplace { get; set; } = null!;

    public int UserId { get; set; }
    public User User { get; set; } = null!;
}
