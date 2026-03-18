using System.ComponentModel.DataAnnotations;

namespace IdealAkeWms.Models;

public class OseonWorkOperation : AuditableEntity
{
    public int OseonProductionOrderId { get; set; }
    public OseonProductionOrder OseonProductionOrder { get; set; } = null!;

    [Required]
    [MaxLength(50)]
    public string PositionNumber { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public int OseonStatus { get; set; }

    public bool IsFirstOperation { get; set; }
    public bool IsLastOperation { get; set; }
}
