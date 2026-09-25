using IdealAkeWms.Models;

namespace IdealAkeWms.Data.Repositories;

public record LeitstandOrderRow(
    int Id,
    string OrderNumber,
    decimal Quantity,
    string? Customer,
    string? ArticleNumber,
    string? Description1,
    string? Description2,
    DateTime? ProductionDate,
    DateTime? DeliveryDate,
    bool IsDone,
    bool IsDonePicking,
    bool IsCancelled,
    string? WorkplaceName,
    // FA-Zusatzinfos aus Sage (v1.26.0) — aus der ExtraInfo-Projektion,
    // Defaults halten bestehende Konstruktor-Aufrufe kompatibel.
    string? Kaeltemittel = null,
    string? Ventil = null,
    string? AusfuehrungEZ = null,
    string? Maschine = null,
    string? SageStatus = null,
    // Werkbank-Override „Abweichende Vorkommissioniertage" (v1.27.0) — null = globaler
    // Settings-Wert gilt. Aufloesung ueber PrePickingDaysResolver.
    int? WorkplaceOverridePrePickingDays = null,
    // Hierarchie-Felder (IDEAL Teil 7). Im Flachmodus: SubOrderNumber == OrderNumber (Backfill),
    // ParentSubOrderNumber/SageMissingSince == null — die hierarchische Anzeige ignoriert sie dann.
    string? SubOrderNumber = null,
    string? ParentSubOrderNumber = null,
    DateTime? SageMissingSince = null,
    // IDEAL-Matchcode (Task 1/2) — materialisiert, Zusatzinfo je FA. Default haelt bestehende
    // Konstruktor-Aufrufe kompatibel.
    string? Matchcode = null);

public record LeitstandOrderPage(List<LeitstandOrderRow> Rows, int TotalCount);

/// <summary>Eine HauptFA-Gruppe (Gruppen-Kopfzeile = OrderNumber) mit ihren Sub-FA-Zeilen.</summary>
public record LeitstandOrderGroup(string OrderNumber, List<LeitstandOrderRow> Rows);

/// <summary>
/// Gruppierte, seitenweise Leitstand-/FA-Liste fuer den hierarchischen Modus (IDEAL Teil 7/8).
/// Paginierung ueber GRUPPEN: <see cref="TotalGroupCount"/> = Anzahl HauptFA-Gruppen auf der
/// aktuellen Filterauswahl (nie eine Gruppe ueber Seiten getrennt), <see cref="TotalRowCount"/> =
/// Anzahl aller passenden Sub-FA-Zeilen (fuer die Z4-Zaehlung „N Auftraege · M Sub-FAs").
/// </summary>
public record LeitstandOrderGroupedPage(List<LeitstandOrderGroup> Groups, int TotalGroupCount, int TotalRowCount);

public interface IProductionOrderRepository : IRepository<ProductionOrder>
{
    Task<List<ProductionOrder>> GetAllOrderedAsync();
    Task<List<ProductionOrder>> GetOpenOrdersAsync();
    Task<ProductionOrder?> GetByOrderNumberAsync(string orderNumber);

    /// <summary>
    /// IDEAL Teil 7/8 (Kaskade): Anzahl aller <see cref="ProductionOrder"/> mit dieser HauptFA-
    /// <c>OrderNumber</c> (= alle materialisierten Nachfahren-Sub-FAs einer Gruppe). Fuer die
    /// Gesamtzahl im Bestaetigungsdialog „Alle Sub-FAs fertigmelden" (unabhaengig von Listen-Filtern).
    /// </summary>
    Task<int> CountByOrderNumberAsync(string orderNumber);
    Task<List<ProductionOrder>> SearchAsync(string? query, int limit = 20);

    /// <summary>
    /// Live-Prüfung der Einwegtor-Sperrbedingung: existiert mindestens eine Sub-FA-Zeile (siehe
    /// <see cref="ProductionOrder.IsHauptFa"/> — EINE Sub-FA-Definition, keine eigene Formel hier;
    /// = hierarchische Materialisierung hat stattgefunden)?
    /// Nur auf dem Schreibpfad des Wächters verwenden — für die Anzeige den gecachten
    /// <c>HierarchischeStrukturStatus</c> nutzen (Freigabe-Antwort 3).
    /// </summary>
    Task<bool> HierarchicalDataExistsAsync();

    /// <summary>
    /// Returns the top-N open production orders with ProductionDate in the next
    /// weeksAhead weeks, ordered by ProductionDate ASC. Orders without ProductionDate are excluded.
    /// </summary>
    Task<List<ProductionOrder>> GetOpenOrdersInWindowAsync(int weeksAhead, int maxCount);

    /// <summary>
    /// Returns production orders whose ArticleNumber is in the given list.
    /// </summary>
    Task<List<ProductionOrder>> GetByArticleNumbersAsync(List<string> articleNumbers);

    /// <summary>Auftraege zu Sub-FA-Nummern (BOM-Bridge Artikelinfo, hierarchisch), inkl. PickingStatus.</summary>
    Task<List<ProductionOrder>> GetBySubOrderNumbersAsync(IReadOnlyCollection<string> subOrderNumbers);

    /// <summary>
    /// FA-Zusatzinfos (Sage, v1.26.0): liest den 1:1-Satelliten zu einem FA
    /// (ein gezielter Read, AsNoTracking). Null wenn (noch) keine Sage-Daten da sind.
    /// </summary>
    Task<ProductionOrderExtraInfo?> GetExtraInfoAsync(int productionOrderId);

    /// <summary>
    /// FA-/Leitstand-Liste mit Server-Side-Filterung, Projection und Pagination.
    /// Filter laufen in SQL; nur die in der View angezeigten Spalten werden
    /// materialisiert. AsNoTracking. <paramref name="page"/> ist 1-basiert.
    /// </summary>
    /// <param name="columnFilters">
    /// Optionale Spalten-Filter aus der URL (<c>colf_&lt;col-key&gt;=value</c>).
    /// Bekannte Keys: order-number, customer, article-number, description1,
    /// description2, workbench, kaeltemittel, ventil, ausfuehrung, maschine,
    /// sage-status. OR-/NOT-Syntax wie clientseitig.
    /// </param>
    Task<LeitstandOrderPage> GetForLeitstandAsync(
        string? filterOrderNumber,
        string? filterArticleNumber,
        string? filterCustomer,
        bool showDone,
        int page,
        int pageSize,
        IReadOnlyDictionary<string, string>? columnFilters = null);

    /// <summary>
    /// Wie <see cref="GetForLeitstandAsync"/>, aber im hierarchischen Modus (Master
    /// <c>ProduktionsauftragHierarchisch</c> = true) GRUPPIERT nach HauptFA (<c>OrderNumber</c>) und
    /// ueber GRUPPEN paginiert. Dieselben Filter wirken auf Zeilen (Z3b: eine Gruppe erscheint,
    /// solange mindestens eine Zeile passt; die order-number-Suche trifft SubOrderNumber ODER
    /// OrderNumber). Sortierung je Gruppe: <c>SubOrderNumber</c> aufsteigend (Z2, Vorbelegung).
    /// </summary>
    Task<LeitstandOrderGroupedPage> GetForLeitstandGroupedAsync(
        string? filterOrderNumber,
        string? filterArticleNumber,
        string? filterCustomer,
        bool showDone,
        int page,
        int pageSize,
        IReadOnlyDictionary<string, string>? columnFilters = null);
}
