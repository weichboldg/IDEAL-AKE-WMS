using System.ComponentModel.DataAnnotations;

namespace IdealAkeWms.Models.ViewModels;

public class BdeTerminalEditViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Terminal-Benutzer ist erforderlich")]
    [Display(Name = "Terminal-Benutzer")]
    public int UserId { get; set; }

    [Required(ErrorMessage = "Default-Werkbank ist erforderlich")]
    [Display(Name = "Default-Werkbank")]
    public int DefaultProductionWorkplaceId { get; set; }

    [StringLength(200)]
    [Display(Name = "Beschreibung")]
    public string? Description { get; set; }
}
