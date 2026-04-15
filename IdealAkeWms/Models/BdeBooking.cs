using System.ComponentModel.DataAnnotations;

namespace IdealAkeWms.Models;

public class BdeBooking : AuditableEntity
{
    [Required]
    public int BdeOperatorId { get; set; }
    public BdeOperator BdeOperator { get; set; } = null!;

    [Required]
    public int ProductionWorkplaceId { get; set; }
    public ProductionWorkplace ProductionWorkplace { get; set; } = null!;

    [Required]
    public int BdeTerminalId { get; set; }
    public BdeTerminal BdeTerminal { get; set; } = null!;

    public int? WorkOperationId { get; set; }
    public WorkOperation? WorkOperation { get; set; }

    public int? BdeActivityId { get; set; }
    public BdeActivity? BdeActivity { get; set; }

    [Required]
    public BdeBookingType BookingType { get; set; }

    [Required]
    public BdeBookingStatus Status { get; set; }

    [Required]
    public DateTime StartedAt { get; set; }

    public DateTime? EndedAt { get; set; }

    public bool IsCancelled { get; set; }

    [StringLength(500)]
    public string? CancellationReason { get; set; }

    public int? ParentBookingId { get; set; }
    public BdeBooking? ParentBooking { get; set; }

    public ICollection<BdeBookingQuantity> Quantities { get; set; } = new List<BdeBookingQuantity>();
}
