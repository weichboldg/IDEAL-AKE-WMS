using System.ComponentModel.DataAnnotations;

namespace IdealAkeWms.Models;

public class Holiday : AuditableEntity
{
    [Required(ErrorMessage = "Datum ist erforderlich")]
    [DataType(DataType.Date)]
    [Display(Name = "Datum")]
    public DateTime Date { get; set; }

    [StringLength(200)]
    [Display(Name = "Bezeichnung")]
    public string? Description { get; set; }

    [Required]
    [Display(Name = "Quelle")]
    public HolidaySource Source { get; set; } = HolidaySource.Manual;
}
