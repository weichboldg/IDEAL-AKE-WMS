namespace IdealAkeWms.Models;

public class WorkstationUser : AuditableEntity
{
    public int WorkstationId { get; set; }
    public Workstation Workstation { get; set; } = null!;

    public int UserId { get; set; }
    public User User { get; set; } = null!;
}
