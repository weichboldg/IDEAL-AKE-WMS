using System.ComponentModel.DataAnnotations;

namespace IdealAkeWms.Models;

public class OseonProductionOrder : AuditableEntity
{
    public long OseonId { get; set; }

    [Required]
    [MaxLength(100)]
    public string OseonOrderNumber { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? CustomerOrderNumber { get; set; }

    public int OseonStatus { get; set; }

    [MaxLength(100)]
    public string? ArticleNumber { get; set; }

    [MaxLength(500)]
    public string? Description1 { get; set; }

    [MaxLength(500)]
    public string? Description2 { get; set; }

    [MaxLength(200)]
    public string? WorkplaceName { get; set; }

    public int? ProductionWorkplaceId { get; set; }
    public ProductionWorkplace? ProductionWorkplace { get; set; }

    public decimal QuantityTarget { get; set; }
    public decimal QuantityActual { get; set; }

    public DateTime? DueDate { get; set; }

    public ICollection<OseonWorkOperation> WorkOperations { get; set; } = new List<OseonWorkOperation>();
}
