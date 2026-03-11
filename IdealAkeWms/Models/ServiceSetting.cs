using System.ComponentModel.DataAnnotations;

namespace IdealAkeWms.Models;

public class ServiceSetting
{
    [Key]
    [Required]
    [StringLength(100)]
    [Display(Name = "Schlüssel")]
    public string Key { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    [Display(Name = "Wert")]
    public string Value { get; set; } = string.Empty;

    [StringLength(100)]
    [Display(Name = "Kategorie")]
    public string? Category { get; set; }

    [StringLength(500)]
    [Display(Name = "Beschreibung")]
    public string? Description { get; set; }
}
