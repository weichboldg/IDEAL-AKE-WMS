using System.ComponentModel.DataAnnotations;

namespace AKEBDELight.Models.ViewModels;

public class WorkstationEditViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Bezeichnung ist erforderlich")]
    [StringLength(200)]
    [Display(Name = "Bezeichnung")]
    public string Name { get; set; } = string.Empty;

    [StringLength(200)]
    [Display(Name = "Standort")]
    public string? Location { get; set; }

    [StringLength(500)]
    [Display(Name = "Standard-Drucker")]
    public string? DefaultPrinter { get; set; }

    [Display(Name = "Standard-Benutzer")]
    public int? DefaultUserId { get; set; }

    [Display(Name = "Zugeordnete Benutzer")]
    public List<int> SelectedUserIds { get; set; } = new();

    public List<User> AvailableUsers { get; set; } = new();
}
