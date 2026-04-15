using System.ComponentModel.DataAnnotations;

namespace IdealAkeWms.Models;

public class BdeBookingQuantity : AuditableEntity
{
    [Required]
    public int BdeBookingId { get; set; }
    public BdeBooking BdeBooking { get; set; } = null!;

    [Required]
    public int BdeOperatorId { get; set; }
    public BdeOperator BdeOperator { get; set; } = null!;

    [Display(Name = "Gutmenge")]
    public decimal GoodQuantity { get; set; }

    [Display(Name = "Ausschuss")]
    public decimal ScrapQuantity { get; set; }

    [Display(Name = "Abschluss-Meldung")]
    public bool IsFinal { get; set; }

    [Required]
    public DateTime ReportedAt { get; set; }
}
