using System.ComponentModel.DataAnnotations;

namespace IdealAkeWms.Models.ViewModels;

public class ProfileViewModel
{
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Personalnummer")]
    public string? PersonalNumber { get; set; }

    [StringLength(100)]
    [Display(Name = "Standard-Filter Beschaffung")]
    public string? DefaultFilterBeschaffung { get; set; }

    [StringLength(100)]
    [Display(Name = "Standard-Filter Artikelgruppe")]
    public string? DefaultFilterArtikelgruppe { get; set; }
}
