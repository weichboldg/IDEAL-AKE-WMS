using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;

namespace IdealAkeWms.Models.ViewModels;

/// <summary>
/// FA-Abarbeitungsliste je Arbeitsgang (v1.22.0): pro offenem FA mit aktivem
/// FaWorkStep des gewaehlten Arbeitsgangs eine Zeile — ueber ALLE Werkbaenke.
/// Spalten: Werkbank (Info), Merkmal-Spalten des AGs, EIN Status-Auswahlfeld
/// (der gewaehlte AG, 3-Wert, AJAX <c>/api/fa-work-steps/set-status</c>).
/// </summary>
public class FaWorklistViewModel
{
    public int? SelectedWorkStepId { get; set; }
    public List<WorkStep> AvailableWorkSteps { get; set; } = new();
    public WorkStep? SelectedWorkStep { get; set; }                            // Header der Erledigt-Spalte
    public string? Workbenches { get; set; }                                   // Komma-Werkbank-Filter (NULL/leer = alle)
    public List<ProductionWorkplace> AvailableWorkplaces { get; set; } = new();
    public List<FaAttributeDefinition> AttributeColumns { get; set; } = new(); // Merkmale des gewaehlten AG
    public bool ShowDone { get; set; }

    /// <summary>Globaler Settings-Wert <c>VorkommissionierTage</c> — Standard-Referenz im Override-Tooltip.</summary>
    public int VorkommissionierTage { get; set; }

    public List<FaWorklistRow> Items { get; set; } = new();
    public Dictionary<string, List<EnaioDmsDocumentLink>> EnaioDmsLinks { get; set; } = new();
    public PaginationState Pagination { get; set; } = new();

    // IDEAL FA-Liste-Hierarchie (Teil 7/8): nur bei Master ProduktionsauftragHierarchisch = true.
    /// <summary>Hierarchischer Modus aktiv → Zwei-Ebenen-Gruppierung (Groups) statt Items.</summary>
    public bool Hierarchical { get; set; }
    /// <summary>HauptFA-Gruppen (Gruppen-Kopfzeile = OrderNumber, darunter alle Nachfahren-Sub-FAs).</summary>
    public List<FaWorklistGroup> Groups { get; set; } = new();
    /// <summary>Anzahl aller Sub-FA-Zeilen auf der Filterauswahl (Z4: „N Aufträge · M Sub-FAs").</summary>
    public int HierarchicalRowCount { get; set; }
}

/// <summary>Eine HauptFA-Gruppe der FA-Abarbeitungsliste im hierarchischen Modus.</summary>
public class FaWorklistGroup
{
    public string OrderNumber { get; set; } = string.Empty;
    public List<FaWorklistRow> Items { get; set; } = new();
}

/// <summary>
/// Kontext fuer das Zeilen-Partial <c>_FaWorklistRow.cshtml</c> — traegt die Zeile plus den gemeinsamen
/// View-Kontext, damit flache und gruppierte Darstellung dasselbe Zeilen-Markup teilen.
/// </summary>
public class FaWorklistRowContext
{
    public FaWorklistRow Item { get; set; } = new();
    public List<FaAttributeDefinition> AttributeColumns { get; set; } = new();
    public int VorkommissionierTage { get; set; }
    public Dictionary<string, List<EnaioDmsDocumentLink>> EnaioDmsLinks { get; set; } = new();
    /// <summary>true → order-number-Zelle zeigt SubOrderNumber, Elternspalte + SageMissingSince-Badge aktiv.</summary>
    public bool Hierarchical { get; set; }
}

public class FaWorklistRow
{
    public int ProductionOrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string? ArticleNumber { get; set; }
    public string? Description1 { get; set; }
    public string? Description2 { get; set; }
    public decimal Quantity { get; set; }
    public string? WorkplaceName { get; set; }                  // Werkbank als Info-Spalte

    /// <summary>
    /// Werkbank-Override „Abweichende Vorkommissioniertage" (v1.27.0) — nur gesetzt, wenn fuer
    /// diese Zeile tatsaechlich ein Override statt des globalen Werts griff (UI-Rueckmeldung).
    /// </summary>
    public int? PrePickingDaysOverride { get; set; }
    // FA-Zusatzinfos aus Sage (v1.26.0) — read-only Anzeige-Spalten.
    public string? Kaeltemittel { get; set; }
    public string? Ventil { get; set; }
    public string? AusfuehrungEZ { get; set; }
    public string? Maschine { get; set; }
    public string? SageStatus { get; set; }
    public DateTime? VorkommissionierTermin { get; set; }  // BG-Termin
    public DateTime? BeschichtungTermin { get; set; }
    public DateTime? KommissionierTermin { get; set; }
    public DateTime? ProductionDate { get; set; }
    public Dictionary<int, string?> AttributeValues { get; set; } = new();   // DefinitionId -> Anzeigetext
    public FaWorklistCell? WorkStepCell { get; set; }                        // genau EIN AG (der gewaehlte)

    // IDEAL Hierarchie (Teil 7) — nur im hierarchischen Modus befuellt.
    /// <summary>Sub-FA-Nummer (= Sage BelID). Im Flachmodus == OrderNumber (Backfill).</summary>
    public string? SubOrderNumber { get; set; }
    /// <summary>Elternzeiger (Sub-FA des Vaters); Spalte parent-sub-order-number, defaultHidden.</summary>
    public string? ParentSubOrderNumber { get; set; }
    /// <summary>Zeitstempel „aus der Struktur vermisst" (Sync-Regel 2); eigener Badge, kein IsCancelled.</summary>
    public DateTime? SageMissingSince { get; set; }
}

public class FaWorklistCell
{
    public int FaWorkStepId { get; set; }
    public FaWorkStepStatus Status { get; set; }
}
