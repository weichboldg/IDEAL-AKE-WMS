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

    public string? ErrorMessage { get; set; }
}
