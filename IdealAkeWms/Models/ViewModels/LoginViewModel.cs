using System.ComponentModel.DataAnnotations;

namespace IdealAkeWms.Models.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "Bitte Benutzernamen eingeben")]
    [Display(Name = "Benutzer")]
    public string UserName { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "Passwort")]
    public string? Password { get; set; }

    /// <summary>Pflichtwechsel (ADR 0027, Regel P): neues Kennwort. Nur im Schritt „Neues Kennwort festlegen“.</summary>
    [DataType(DataType.Password)]
    [Display(Name = "Neues Kennwort")]
    public string? NewPassword { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "Neues Kennwort wiederholen")]
    public string? NewPasswordRepeat { get; set; }

    /// <summary>Schritt „Neues Kennwort festlegen“ anzeigen bzw. wurde gepostet. Steuert nur die Anzeige – der Server prueft
    /// Kennwort und Haken bei jedem POST erneut.</summary>
    public bool KennwortFestlegen { get; set; }

    public string? ErrorMessage { get; set; }
}
