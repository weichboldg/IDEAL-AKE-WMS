using System.ComponentModel.DataAnnotations;

namespace IdealAkeWms.Models;

public class BdeOperator : AuditableEntity
{
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

    public User? User { get; set; }

    /// <summary>Sage-PPS-Mitarbeiternummer (<c>PpsMitarbeiterInfo.Nummer</c>, string(20)); nur manuell zugeordnet (G14),
    /// eindeutig (gefilterter UNIQUE-Index + App-Guard). Null = nicht zugeordnet.</summary>
    [StringLength(20)]
    [Display(Name = "Sage-Mitarbeiternummer")]
    public string? SageMitarbeiternummer { get; set; }

    public string DisplayName => $"{FirstName} {LastName}";
}
