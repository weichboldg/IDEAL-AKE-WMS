using System.ComponentModel.DataAnnotations;

namespace AKEBDELight.Models;

public class AppSetting
{
    [Required]
    [StringLength(100)]
    [Display(Name = "Schlüssel")]
    public string Key { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    [Display(Name = "Wert")]
    public string Value { get; set; } = string.Empty;

    [StringLength(500)]
    [Display(Name = "Beschreibung")]
    public string? Description { get; set; }
}
