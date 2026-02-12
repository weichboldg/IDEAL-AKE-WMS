using System.ComponentModel.DataAnnotations;

namespace AKEBDELight.Models.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "Bitte wählen Sie einen Benutzer")]
    [Display(Name = "Benutzer")]
    public int UserId { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "Passwort")]
    public string? Password { get; set; }

    public List<User> AvailableUsers { get; set; } = new();
    public string? ErrorMessage { get; set; }
}
