using System.ComponentModel.DataAnnotations;

namespace AKEBDELight.Models;

public class Workstation : AuditableEntity
{
    [Required(ErrorMessage = "Bezeichnung ist erforderlich")]
    [StringLength(200)]
    [Display(Name = "Bezeichnung")]
    public string Name { get; set; } = string.Empty;

    [StringLength(200)]
    [Display(Name = "Standort")]
    public string? Location { get; set; }

    [Display(Name = "Standard-Benutzer")]
    public int? DefaultUserId { get; set; }

    public User? DefaultUser { get; set; }
    public ICollection<WorkstationUser> WorkstationUsers { get; set; } = new List<WorkstationUser>();
}
