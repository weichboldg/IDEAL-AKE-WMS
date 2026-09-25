using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;

namespace IdealAkeWms.Models;

public class ProductionOrder : AuditableEntity
{
    [Required(ErrorMessage = "FA Nummer ist erforderlich")]
    [StringLength(100)]
    [Display(Name = "FA Nummer")]
    public string OrderNumber { get; set; } = string.Empty;

    // --- Hierarchie-Felder (IDEAL Teil 7, Schema-Inversion) ---
    // OrderNumber (= Sage StrukturID/HauptFA) ist nach der Inversion NICHT mehr unique; die
    // Eindeutigkeit wandert auf SubOrderNumber (= Sage BelID). Im flachen AKE-Modus gilt die
    // Invariante SubOrderNumber == OrderNumber (Backfill), Verhalten unveraendert.

    [Required]
    [StringLength(100)]
    [Display(Name = "Sub-FA Nummer")]
    public string SubOrderNumber { get; set; } = string.Empty;

    // EINE Sub-FA-Definition (Spec 2026-09-25-kommissionierung-nur-hauptfa). Quelle ist die
    // Expression (EF uebersetzt sie in SQL); IsSubFa/IsSubFaOf sind daraus kompiliert — keine
    // zweite Formel. Leer gilt als HauptFA (AKE-sichere Richtung). Grenze: InMemory-Tests belegen
    // nur C#-Semantik; SQL Server vergleicht case-insensitiv und ignoriert Trailing Spaces —
    // folgenlos, weil beide Spalten aus demselben Ursprungswert stammen.
    public static readonly Expression<Func<ProductionOrder, bool>> IsHauptFa =
        p => p.SubOrderNumber == "" || p.SubOrderNumber == p.OrderNumber;

    private static readonly Func<ProductionOrder, bool> IsHauptFaCompiled = IsHauptFa.Compile();

    [NotMapped]
    public bool IsSubFa => !IsHauptFaCompiled(this);

    /// <summary>Fuer Projektionen ohne ProductionOrder-Objekt (z. B. LeitstandOrderRow).
    /// null gilt wie "" als HauptFA.</summary>
    public static bool IsSubFaOf(string orderNumber, string? subOrderNumber) =>
        !IsHauptFaCompiled(new ProductionOrder { OrderNumber = orderNumber, SubOrderNumber = subOrderNumber ?? string.Empty });

    // Echter Elternzeiger (= Sage VaterFA). NULL bei der Wurzel bzw. im flachen Modus; noetig,
    // weil OrderNumber/SubOrderNumber allein nur zwei Ebenen ausdruecken koennen, die Struktur
    // aber mehrstufig ist (Sub-FA kann unter einem anderen Sub-FA haengen).
    [StringLength(100)]
    [Display(Name = "Vater-Sub-FA")]
    public string? ParentSubOrderNumber { get; set; }

    // Zeitstempel (kein Bool!): gesetzt, sobald ein materialisierter Sub-FA nicht mehr in der
    // Struktur-Quelle auftaucht (Sync-Regel 2); wieder auf NULL, sobald er erneut auftaucht
    // (selbstheilend). Bewusst NICHT IsCancelled — "storniert" und "aus der Quelle verschwunden"
    // sind fachlich verschieden. Bei AKE (flacher Modus) durchgehend NULL.
    [Display(Name = "In Sage vermisst seit")]
    public DateTime? SageMissingSince { get; set; }

    [Display(Name = "Stückzahl")]
    public decimal Quantity { get; set; }

    [StringLength(200)]
    [Display(Name = "Kunde")]
    public string? Customer { get; set; }

    [StringLength(100)]
    [Display(Name = "Artikelnummer")]
    public string? ArticleNumber { get; set; }

    [StringLength(500)]
    [Display(Name = "Bezeichnung 1")]
    public string? Description1 { get; set; }

    [StringLength(500)]
    [Display(Name = "Bezeichnung 2")]
    public string? Description2 { get; set; }

    [Display(Name = "Fertigungstermin")]
    [DataType(DataType.Date)]
    public DateTime? ProductionDate { get; set; }

    [Display(Name = "Liefertermin")]
    [DataType(DataType.Date)]
    public DateTime? DeliveryDate { get; set; }

    [Display(Name = "Erledigt")]
    public bool IsDone { get; set; }

    [Display(Name = "Werkbank")]
    public int? ProductionWorkplaceId { get; set; }
    public ProductionWorkplace? ProductionWorkplace { get; set; }

    public ICollection<WorkOperation> WorkOperations { get; set; } = new List<WorkOperation>();

    // Phase 1 — neue Nav-Properties (siehe Spec 5.1)
    // AssemblyGroups-Collection entfernt in v1.22.0 (ersetzt durch FaWorkSteps)
    public ProductionOrderPickingStatus? PickingStatus { get; set; }
    public ProductionOrderBdeStatus? BdeStatus { get; set; }

    /// <summary>FA-Zusatzinfos aus Sage (v1.26.0, read-only Satellit).</summary>
    public ProductionOrderExtraInfo? ExtraInfo { get; set; }

    // FA-Reconciliation (v1.25.0): verwaiste FAs, die in Sage geloescht wurden.
    // IsCancelled verhaelt sich in allen Offen-Queries wie IsDone (raus aus offenen Sichten).
    [Display(Name = "Storniert")]
    public bool IsCancelled { get; set; }

    [Display(Name = "Storniert am")]
    public DateTime? CancelledAt { get; set; }

    [StringLength(256)]
    [Display(Name = "Storniert von")]
    public string? CancelledBy { get; set; }
}
