using System.ComponentModel.DataAnnotations;

namespace IdealAkeWms.Models;

public class BdeShift : AuditableEntity
{
    [Required]
    [Display(Name = "Wochentag")]
    public DayOfWeek DayOfWeek { get; set; }

    [Required]
    [Display(Name = "Beginn")]
    public TimeSpan StartTime { get; set; }

    [Required]
    [Display(Name = "Ende")]
    public TimeSpan EndTime { get; set; }

    [Display(Name = "Werkbank")]
    public int? ProductionWorkplaceId { get; set; }
    public ProductionWorkplace? ProductionWorkplace { get; set; }

    [StringLength(50)]
    [Display(Name = "Bezeichnung")]
    public string? Name { get; set; }
}
