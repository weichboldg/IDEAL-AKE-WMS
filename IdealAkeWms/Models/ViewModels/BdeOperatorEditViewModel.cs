using System.ComponentModel.DataAnnotations;

namespace IdealAkeWms.Models.ViewModels;

public class BdeOperatorEditViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Personalnummer ist erforderlich")]
    [StringLength(50)]
    [Display(Name = "Personalnummer")]
    public string PersonnelNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vorname ist erforderlich")]
    [StringLength(100)]
    [Display(Name = "Vorname")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nachname ist erforderlich")]
    [StringLength(100)]
    [Display(Name = "Nachname")]
    public string LastName { get; set; } = string.Empty;

    [Display(Name = "Aktiv")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "WMS-Benutzer")]
    public int? UserId { get; set; }
}
