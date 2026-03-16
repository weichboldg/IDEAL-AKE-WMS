using System.ComponentModel.DataAnnotations;

namespace IdealAkeWms.Models;

public class WorkOperation : AuditableEntity
{
    public int ProductionOrderId { get; set; }
    public ProductionOrder ProductionOrder { get; set; } = null!;

    [Required(ErrorMessage = "Arbeitsgangsnummer ist erforderlich")]
    [MaxLength(50)]
    [Display(Name = "Arbeitsgangsnummer")]
    public string OperationNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Bezeichnung ist erforderlich")]
    [MaxLength(200)]
    [Display(Name = "Bezeichnung")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Werkbank")]
    public int? ProductionWorkplaceId { get; set; }
    public ProductionWorkplace? ProductionWorkplace { get; set; }

    [Display(Name = "Reihenfolge")]
    public int Sequence { get; set; }

    [Display(Name = "Rückmeldbar")]
    public bool IsReportable { get; set; }

    [Display(Name = "Fremdsystem-Rückmeldung")]
    public bool IsExternalSystem { get; set; }

    [Display(Name = "Rückgemeldet")]
    public bool IsReported { get; set; }

    [Display(Name = "Rückgemeldet am")]
    public DateTime? ReportedAt { get; set; }

    [MaxLength(200)]
    [Display(Name = "Rückgemeldet von")]
    public string? ReportedBy { get; set; }

    [MaxLength(200)]
    [Display(Name = "Rückgemeldet von (Windows)")]
    public string? ReportedByWindows { get; set; }

    [MaxLength(100)]
    [Display(Name = "Quellsystem")]
    public string? ExternalSource { get; set; }
}
