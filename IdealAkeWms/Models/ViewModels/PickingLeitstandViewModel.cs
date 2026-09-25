namespace IdealAkeWms.Models.ViewModels;

public class PickingLeitstandViewModel
{
    public List<PickingLeitstandItem> Items { get; set; } = new();
    public string? FilterOrderNumber { get; set; }
    public string? FilterArticleNumber { get; set; }
    public string? FilterCustomer { get; set; }
    public bool ShowDone { get; set; }
    public int KommissionierTage { get; set; }
    public int VorkommissionierTage { get; set; }
    public int BeschichtungTage { get; set; }
    public bool CanPick { get; set; }
    public bool CanManagePickingRelease { get; set; }
    public bool LeitstandAktiv { get; set; }
    public bool PickerAssignmentEnabled { get; set; }

    public Dictionary<string, List<Data.Repositories.EnaioDmsDocumentLink>> EnaioDmsLinks { get; set; } = new();

    public PaginationState Pagination { get; set; } = new();

    // IDEAL FA-Liste-Hierarchie (Teil 7/8): nur bei Master ProduktionsauftragHierarchisch = true.
    /// <summary>Hierarchischer Modus aktiv → Zwei-Ebenen-Gruppierung (Groups) statt Items.</summary>
    public bool Hierarchical { get; set; }
    /// <summary>HauptFA-Gruppen (Gruppen-Kopfzeile = OrderNumber, darunter alle Nachfahren-Sub-FAs).</summary>
    public List<PickingLeitstandGroup> Groups { get; set; } = new();
    /// <summary>Anzahl aller Sub-FA-Zeilen auf der Filterauswahl (Z4: „N Aufträge · M Sub-FAs").</summary>
    public int HierarchicalRowCount { get; set; }
}

/// <summary>Eine HauptFA-Gruppe des Leitstands im hierarchischen Modus.</summary>
public class PickingLeitstandGroup
{
    public string OrderNumber { get; set; } = string.Empty;
    public List<PickingLeitstandItem> Items { get; set; } = new();
}

/// <summary>
/// Kontext fuer das Zeilen-Partial <c>_PickingLeitstandRow.cshtml</c> — traegt die Zeile plus den
/// gemeinsamen View-Kontext, damit flache und gruppierte Darstellung dasselbe Zeilen-Markup teilen.
/// </summary>
public class PickingLeitstandRowContext
{
    public PickingLeitstandItem Item { get; set; } = new();
    public bool CanPick { get; set; }
    public bool LeitstandAktiv { get; set; }
    public bool CanManagePickingRelease { get; set; }
    public bool PickerAssignmentEnabled { get; set; }
    public int VorkommissionierTage { get; set; }
    public Dictionary<string, List<Data.Repositories.EnaioDmsDocumentLink>> EnaioDmsLinks { get; set; } = new();
    public string? LackierteilKategorieName { get; set; }
    /// <summary>true → order-number-Zelle zeigt SubOrderNumber, Elternspalte + SageMissingSince-Badge aktiv.</summary>
    public bool Hierarchical { get; set; }
}

public class PickingLeitstandItem
{
    // Sage-Master (identisch zu ProductionOrderListItem)
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string? Customer { get; set; }
    public string? ArticleNumber { get; set; }

    /// <summary>IDEAL-Matchcode (Task 1/2), materialisierte Zusatzinfo je FA.</summary>
    public string? Matchcode { get; set; }
    public string? Description1 { get; set; }
    public string? Description2 { get; set; }
    public DateTime? ProductionDate { get; set; }
    public DateTime? DeliveryDate { get; set; }
    public bool IsDone { get; set; }
    public string? WorkplaceName { get; set; }

    /// <summary>
    /// Werkbank-Override „Abweichende Vorkommissioniertage" (v1.27.0) — nur gesetzt, wenn fuer
    /// diese Zeile tatsaechlich ein Override statt des globalen Werts griff (UI-Rueckmeldung).
    /// </summary>
    public int? PrePickingDaysOverride { get; set; }

    // FA-Zusatzinfos aus Sage (v1.26.0) — read-only, Default-ausgeblendete Spalten.
    public string? Kaeltemittel { get; set; }
    public string? Ventil { get; set; }
    public string? AusfuehrungEZ { get; set; }
    public string? Maschine { get; set; }
    public string? SageStatus { get; set; }

    public DateTime? KommissionierTermin { get; set; }
    public DateTime? VorkommissionierTermin { get; set; }
    public DateTime? BeschichtungTermin { get; set; }

    // PickingStatus-Felder (aus ProductionOrderPickingStatus)
    public string? PickingStatus { get; set; }
    public bool HasGlass { get; set; }
    public bool HasExternalPurchase { get; set; }
    public bool HasCoatingParts { get; set; }
    public bool IsCoatingDone { get; set; }
    public bool IsReleasedForPicking { get; set; }
    public int? PickingPriority { get; set; }
    public DateTime? ReleasedAt { get; set; }
    public string? ReleasedBy { get; set; }
    public int? AssignedPickerId { get; set; }
    public string? AssignedPickerName { get; set; }

    // FaWorkStep-Detail-Pivot: Code (VK/VL/VE/VT/VA) -> Zelle (FaWorkStepId + Status).
    // Nur aktive AGs sind enthalten; fehlender Key = AG nicht anwendbar (leere Zelle).
    // Das Auswahlfeld zeigt/setzt Status (3-Wert-Erledigt) — dasselbe Flag wie die FA-Abarbeitungsliste.
    public Dictionary<string, Data.Repositories.FaWorkStepPivotCell> WorkSteps { get; set; } = new();

    // IDEAL Hierarchie (Teil 7) — nur im hierarchischen Modus befuellt.
    /// <summary>Sub-FA-Nummer (= Sage BelID). Im Flachmodus == OrderNumber (Backfill).</summary>
    public string? SubOrderNumber { get; set; }
    /// <summary>Elternzeiger (Sub-FA des Vaters); Spalte parent-sub-order-number, defaultHidden.</summary>
    public string? ParentSubOrderNumber { get; set; }
    /// <summary>Zeitstempel „aus der Struktur vermisst" (Sync-Regel 2); eigener Badge, kein IsCancelled.</summary>
    public DateTime? SageMissingSince { get; set; }
    /// <summary>Kommissionierung nur am HauptFA (Spec 2026-09-25) — aus ProductionOrder.IsSubFaOf, keine eigene Formel.</summary>
    public bool IsSubFa { get; set; }
}
