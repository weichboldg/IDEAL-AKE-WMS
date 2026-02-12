using System.ComponentModel.DataAnnotations;

namespace AKEBDELight.Models;

public class User : AuditableEntity
{
    [Required(ErrorMessage = "Name ist erforderlich")]
    [StringLength(200)]
    [Display(Name = "Name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(50)]
    [Display(Name = "Personalnummer")]
    public string? PersonalNumber { get; set; }

    [Display(Name = "Aktiv")]
    public bool IsActive { get; set; } = true;

    [StringLength(500)]
    public string? PasswordHash { get; set; }

    public ICollection<WorkstationUser> WorkstationUsers { get; set; } = new List<WorkstationUser>();
    public ICollection<Workstation> DefaultWorkstations { get; set; } = new List<Workstation>();
    public ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
}
