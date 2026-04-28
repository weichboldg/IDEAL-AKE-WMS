using System.ComponentModel.DataAnnotations;

namespace IdealAkeWms.Models.ViewModels;

public class BdeShiftEditViewModel
{
    public int Id { get; set; }

    [Required]
    [Display(Name = "Wochentag")]
    public DayOfWeek DayOfWeek { get; set; }

    [Required]
    [Display(Name = "Beginn")]
    [DataType(DataType.Time)]
    public TimeSpan StartTime { get; set; }

    [Required]
    [Display(Name = "Ende")]
    [DataType(DataType.Time)]
    public TimeSpan EndTime { get; set; }

    [StringLength(50)]
    [Display(Name = "Bezeichnung")]
    public string? Name { get; set; }

    public int? ProductionWorkplaceId { get; set; }
}
