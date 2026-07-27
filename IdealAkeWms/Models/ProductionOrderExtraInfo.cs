using System.ComponentModel.DataAnnotations;

namespace IdealAkeWms.Models;

/// <summary>
/// FA-Zusatzinfos aus Sage (v1.26.0) — 1:1-Satellit zu <see cref="ProductionOrder"/>
/// (Muster ProductionOrderPickingStatus: UNIQUE-FK + Cascade). Sage ist Master:
/// Zeilen entstehen/aendern sich AUSSCHLIESSLICH im FaZusatzinfoSyncService,
/// in der App read-only. Kein Loeschen — verschwindet ein WA aus der Sage-View,
/// bleibt der letzte bekannte Stand stehen. Kein AgentJob-Eager-Create.
/// Deutsche Property-Namen = Sage-Domaenenvokabular (1:1 zur View nachvollziehbar).
/// </summary>
public class ProductionOrderExtraInfo : AuditableEntity
{
    public int ProductionOrderId { get; set; }
    public ProductionOrder ProductionOrder { get; set; } = null!;

    [StringLength(200)]
    [Display(Name = "Kältemittel")]
    public string? Kaeltemittel { get; set; }

    [StringLength(200)]
    [Display(Name = "Ventil")]
    public string? Ventil { get; set; }

    [StringLength(200)]
    [Display(Name = "Ausführung E/Z")]
    public string? AusfuehrungEZ { get; set; }

    [StringLength(200)]
    [Display(Name = "Maschine")]
    public string? Maschine { get; set; }

    [StringLength(200)]
    [Display(Name = "Sage-Status")]
    public string? SageStatus { get; set; }
}
