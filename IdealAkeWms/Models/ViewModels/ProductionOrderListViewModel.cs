namespace IdealAkeWms.Models.ViewModels;

public class ProductionOrderListViewModel
{
    public List<ProductionOrderListItem> Items { get; set; } = new();
    public string? FilterOrderNumber { get; set; }
    public string? FilterArticleNumber { get; set; }
    public string? FilterCustomer { get; set; }
    public bool ShowDone { get; set; }
    public int KommissionierTage { get; set; }
    public int VorkommissionierTage { get; set; }
    public int BeschichtungTage { get; set; }
    public bool CanPick { get; set; }

    /// <summary>vorbau-Zugriff (read-only Stueckliste-Button in der FA-Liste, v1.25.0).</summary>
    public bool HasVorbauAccess { get; set; }

    /// <summary>enaio DMS-Links pro FA-Nummer (Key=OrderNumber, Value=Liste von DMS-Dokumenten)</summary>
    public Dictionary<string, List<Data.Repositories.EnaioDmsDocumentLink>> EnaioDmsLinks { get; set; } = new();

    public PaginationState Pagination { get; set; } = new();

    // IDEAL FA-Liste-Hierarchie (Teil 7/8): nur bei Master ProduktionsauftragHierarchisch = true.
    /// <summary>Hierarchischer Modus aktiv → Zwei-Ebenen-Gruppierung (Groups) statt Items.</summary>
    public bool Hierarchical { get; set; }
    /// <summary>HauptFA-Gruppen (Gruppen-Kopfzeile = OrderNumber, darunter alle Nachfahren-Sub-FAs).</summary>
    public List<ProductionOrderListGroup> Groups { get; set; } = new();
    /// <summary>Anzahl aller Sub-FA-Zeilen auf der Filterauswahl (Z4: „N Aufträge · M Sub-FAs").</summary>
    public int HierarchicalRowCount { get; set; }
}

/// <summary>Eine HauptFA-Gruppe der FA-Liste im hierarchischen Modus.</summary>
public class ProductionOrderListGroup
{
    public string OrderNumber { get; set; } = string.Empty;
    public List<ProductionOrderListItem> Items { get; set; } = new();
}

/// <summary>
/// Kontext fuer das Zeilen-Partial <c>_ProductionOrderRow.cshtml</c> — traegt die Zeile plus den
/// gemeinsamen View-Kontext, damit flache und gruppierte Darstellung dasselbe Zeilen-Markup teilen.
/// </summary>
public class ProductionOrderRowContext
{
    public ProductionOrderListItem Item { get; set; } = new();
    public bool CanPick { get; set; }
    public bool HasVorbauAccess { get; set; }
    public int VorkommissionierTage { get; set; }
    public Dictionary<string, List<Data.Repositories.EnaioDmsDocumentLink>> EnaioDmsLinks { get; set; } = new();
    public string? LackierteilKategorieName { get; set; }
    /// <summary>true → order-number-Zelle zeigt SubOrderNumber, Elternspalte + SageMissingSince-Badge aktiv.</summary>
    public bool Hierarchical { get; set; }
}

public class ProductionOrderListItem
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string? Customer { get; set; }
    public string? ArticleNumber { get; set; }
    public string? Description1 { get; set; }
    public string? Description2 { get; set; }
    public DateTime? ProductionDate { get; set; }
    public DateTime? DeliveryDate { get; set; }
    public bool IsDone { get; set; }
    public bool IsCancelled { get; set; }
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

    // Calculated dates
    public DateTime? KommissionierTermin { get; set; }
    public DateTime? VorkommissionierTermin { get; set; }
    public DateTime? BeschichtungTermin { get; set; }

    // Cross-cutting from PickingStatus (siehe Spec 6.1) — fuer Beschichtungstermin-Logik
    public bool HasCoatingParts { get; set; }
    public bool IsCoatingDone { get; set; }

    // IDEAL Hierarchie (Teil 7) — nur im hierarchischen Modus befuellt.
    /// <summary>Sub-FA-Nummer (= Sage BelID). Im Flachmodus == OrderNumber (Backfill).</summary>
    public string? SubOrderNumber { get; set; }
    /// <summary>Elternzeiger (Sub-FA des Vaters); Spalte parent-sub-order-number, defaultHidden.</summary>
    public string? ParentSubOrderNumber { get; set; }
    /// <summary>Zeitstempel „aus der Struktur vermisst" (Sync-Regel 2); eigener Badge, kein IsCancelled.</summary>
    public DateTime? SageMissingSince { get; set; }
}
