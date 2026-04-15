using System.ComponentModel.DataAnnotations;

namespace IdealAkeWms.Models.ViewModels;

public class BdeActivityEditViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Code ist erforderlich")]
    [StringLength(20)]
    [Display(Name = "Code")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Bezeichnung ist erforderlich")]
    [StringLength(100)]
    [Display(Name = "Bezeichnung")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Aktiv")]
    public bool IsActive { get; set; } = true;
}
