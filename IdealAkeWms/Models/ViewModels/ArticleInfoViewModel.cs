using IdealAkeWms.Services;

namespace IdealAkeWms.Models.ViewModels;

public class ArticleInfoViewModel
{
    public string ArticleNumber { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Matchcode { get; set; }
    public string? Unit { get; set; }
    public string? ArticleGroup { get; set; }
    public decimal ReorderLevel { get; set; }
    public List<StockOverviewItem> StockByLocation { get; set; } = new();
    public decimal TotalStock => StockByLocation.Sum(s => s.CurrentQuantity);

    /// <summary>
    /// v1.99.30 (Spec 2026-11-16-bestellvorschlaege-nachbesserung F4, Antwort 7): Bedarfe nach derselben Regel wie die Bestellvorschlaege
    /// (FA-Rest, Bedarfsmeldungen, Lagerbestellungen, angerechnet). Ersetzt <c>PlannedConsumption</c> (voller FA-Verbrauch ohne Rest).
    /// </summary>
    public ArtikelBedarf Bedarf { get; set; } = ArtikelBedarf.Leer;
    /// <summary>Einstellung <c>EinkaufOffeneBedarfe</c>: zaehlen Lagerbestellungen / Bedarfsmeldungen (Anzeige „nicht gezaehlt“).</summary>
    public bool BedarfeMitLagerbestellungen { get; set; } = true;
    public bool BedarfeMitBedarfsmeldungen { get; set; } = true;

    /// <summary>Gesamtbestand abzueglich der angerechneten Bedarfe.</summary>
    public decimal AvailableStock => TotalStock - Bedarf.Angerechnet;

    public string? CategoryName { get; set; }
    public List<AttributeDisplayValue> AttributeDisplayValues { get; set; } = new();
    public List<ArticleUsageItem> UsedInOrders { get; set; } = new();

    // IDEAL FA-Liste-Hierarchie (Teil 7/8, Z4): nur bei Master ProduktionsauftragHierarchisch = true.
    /// <summary>Hierarchischer Modus → „Teil enthalten in"-Zaehlung als „N Aufträge · M Sub-FAs".</summary>
    public bool Hierarchical { get; set; }

    /// <summary>v1.99.72: Fraesen-Pfad (<c>Articles.CamPath</c>); angezeigt nur bei <c>FraesenPfadAktiv</c>.</summary>
    public string? CamPath { get; set; }

    /// <summary>Sage-Zusatzfelder (v1.73.0 E3a, nur lesend); null = Artikel wurde nie ueber SData gelesen → seit v1.80.0 Hinweis statt Karte.</summary>
    public ArticleSageDetail? SageDetail { get; set; }

    /// <summary>v1.80.0: Kreditor zu <c>SageDetail.Hauptlieferant</c> bzw. <c>.LetzterLieferant</c> (getrimmt, Gross/Klein egal); null = kein Kreditor im WMS.</summary>
    public Creditor? HauptlieferantKreditor { get; set; }
    public Creditor? LetzterLieferantKreditor { get; set; }
    /// <summary>v1.80.0: Artikel-Lieferanten aus Sage (unabhaengig vom Satelliten).</summary>
    public List<ArticleSupplierRow> Lieferanten { get; set; } = new();
    /// <summary>Z4: Anzahl distinkter HauptFAs (OrderNumber) unter <see cref="UsedInOrders"/> (Sub-FAs sind die Zeilen).</summary>
    public int UsedInHauptFaCount => UsedInOrders.Select(o => o.OrderNumber).Distinct().Count();
}

public class ArticleUsageItem
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public DateTime? ProductionDate { get; set; }
    public DateTime? DeliveryDate { get; set; }

    /// <summary>Sub-FA-Nummer (IDEAL Teil 7). Im Flachmodus == OrderNumber (Backfill); hierarchisch die Zeilen-Nummer.</summary>
    public string? SubOrderNumber { get; set; }

    /// <summary>v1.99.30: BOM-Menge dieses Bauteils im FA und Rest nach Ausbuchungen (Spalte „Rest“).</summary>
    public decimal Soll { get; set; }
    public decimal Rest { get; set; }
}

public class AttributeDisplayValue
{
    public string Name { get; set; } = string.Empty;
    public string DisplayValue { get; set; } = string.Empty;
}

/// <summary>v1.99.72 (F14): Model des Pflegeformulars <c>Views/Articles/_FraesenPfadPflegen.cshtml</c> (Artikelinfo, „Artikel bearbeiten“).</summary>
public record FraesenPfadPflegenModel(string ArticleNumber, string? CamPath, string ReturnUrl);
