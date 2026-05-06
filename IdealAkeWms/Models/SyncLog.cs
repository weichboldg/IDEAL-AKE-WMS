using System.ComponentModel.DataAnnotations;

namespace IdealAkeWms.Models;

public class SyncLog
{
    public int Id { get; set; }

    [Display(Name = "Zeitpunkt")]
    public DateTime Timestamp { get; set; } = DateTime.Now;

    [Required]
    [StringLength(50)]
    [Display(Name = "Service")]
    public string Service { get; set; } = string.Empty;

    [Required]
    [StringLength(10)]
    [Display(Name = "Stufe")]
    public string Level { get; set; } = SyncLogLevel.Info;

    [Required]
    [StringLength(1000)]
    [Display(Name = "Meldung")]
    public string Message { get; set; } = string.Empty;

    [StringLength(100)]
    [Display(Name = "Referenz")]
    public string? Reference { get; set; }
}
