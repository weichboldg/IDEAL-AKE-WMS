namespace IdealAkeWms.Models.ViewModels;

public record ColumnDef(string Key, string Label, bool Locked = false, int? DefaultWidth = null, bool DefaultHidden = false);

public record ViewConfig(string ViewKey, string DisplayName, bool SupportsReorder, bool SupportsSortDefault)
{
    public List<ColumnDef> Columns { get; init; } = new();
}

public static class ColumnDefinitions
{
    /// <summary>
    /// ProductionOrders/Index.cshtml columns — Phase 2 slim FA-Uebersicht
    /// (Picker/Tracker/Leitstand). Status-Pivot, Bulk-Release und Picker-Assign
    /// wandern nach <see cref="PickingLeitstand"/>.
    /// Conditional columns:
    ///   - "actions"   : rendered only when Model.CanPick (Stueckliste-Button column, width 40px)
    /// </summary>
    public static readonly ViewConfig ProductionOrders = new("ProductionOrders", "Fertigungsauftraege", SupportsReorder: true, SupportsSortDefault: true)
    {
        Columns =
        [
            // Conditional: only when CanPick
            new ColumnDef("actions",     "",                 Locked: true,  DefaultWidth: 40),
            new ColumnDef("order-number","FA Nr.",           Locked: true,  DefaultWidth: 90),
            new ColumnDef("quantity",    "Stk.",             Locked: false, DefaultWidth: 55),
            new ColumnDef("customer",    "Kunde",            Locked: false),
            new ColumnDef("article-number","Artikelnummer",  Locked: false),
            new ColumnDef("description1","Bezeichnung 1",    Locked: false),
            new ColumnDef("description2","Bezeichnung 2",    Locked: false),
            new ColumnDef("workbench",   "Werkbank",         Locked: false),
            // FA-Zusatzinfos aus Sage (v1.26.0) — Default AUSGEBLENDET (Zahnrad blendet ein)
            new ColumnDef("kaeltemittel","Kaeltemittel",    Locked: false, DefaultHidden: true),
            new ColumnDef("ventil",      "Ventil",          Locked: false, DefaultHidden: true),
            new ColumnDef("ausfuehrung", "Ausfuehrung E/Z", Locked: false, DefaultHidden: true),
            new ColumnDef("maschine",    "Maschine",        Locked: false, DefaultHidden: true),
            new ColumnDef("sage-status", "Sage-Status",     Locked: false, DefaultHidden: true),
            new ColumnDef("coating-date","Beschicht.",       Locked: false),
            new ColumnDef("bg-date",     "BG-Termin",        Locked: false),
            new ColumnDef("picking-date","Komm.",            Locked: false),
            new ColumnDef("production-date","Fert.-Termin",  Locked: false),
            new ColumnDef("delivery-date","Liefertermin",    Locked: false),
            new ColumnDef("coating-part","Lack-T",           Locked: false, DefaultWidth: 55),
            // Icon/action column (enaio links)
            new ColumnDef("row-actions", "",                 Locked: true,  DefaultWidth: 80),
        ]
    };

    /// <summary>
    /// PickingLeitstand/Index.cshtml columns — Phase 2 rich Kommissionier-Leitstand-View.
    /// Conditional columns:
    ///   - "bulk-select" : rendered only when Model.LeitstandAktiv &amp;&amp; Model.CanManagePickingRelease
    ///   - "actions"     : rendered only when Model.CanPick
    ///   - "release"     : rendered only when Model.LeitstandAktiv &amp;&amp; Model.CanManagePickingRelease
    ///   - "picker"      : rendered only when Model.PickerAssignmentEnabled
    /// </summary>
    public static readonly ViewConfig PickingLeitstand = new(
        "PickingLeitstand", "Kommissionier-Leitstand",
        SupportsReorder: true, SupportsSortDefault: true)
    {
        Columns =
        [
            new ColumnDef("bulk-select",    "",              Locked: true,  DefaultWidth: 32),
            new ColumnDef("actions",        "",              Locked: true,  DefaultWidth: 40),
            new ColumnDef("order-number",   "FA Nr.",        Locked: true,  DefaultWidth: 90),
            new ColumnDef("quantity",       "Stk.",          Locked: false, DefaultWidth: 55),
            new ColumnDef("customer",       "Kunde",         Locked: false),
            new ColumnDef("article-number", "Artikelnummer", Locked: false),
            new ColumnDef("description1",   "Bezeichnung 1", Locked: false),
            new ColumnDef("description2",   "Bezeichnung 2", Locked: false),
            new ColumnDef("workbench",      "Werkbank",      Locked: false),
            // FA-Zusatzinfos aus Sage (v1.26.0) — Default AUSGEBLENDET (Zahnrad blendet ein)
            new ColumnDef("kaeltemittel","Kaeltemittel",    Locked: false, DefaultHidden: true),
            new ColumnDef("ventil",      "Ventil",          Locked: false, DefaultHidden: true),
            new ColumnDef("ausfuehrung", "Ausfuehrung E/Z", Locked: false, DefaultHidden: true),
            new ColumnDef("maschine",    "Maschine",        Locked: false, DefaultHidden: true),
            new ColumnDef("sage-status", "Sage-Status",     Locked: false, DefaultHidden: true),
            new ColumnDef("coating-date",   "Beschicht.",    Locked: false),
            new ColumnDef("bg-date",        "BG-Termin",     Locked: false),
            new ColumnDef("picking-date",   "Komm.",         Locked: false),
            new ColumnDef("production-date","Fert.-Termin",  Locked: false),
            new ColumnDef("delivery-date",  "Liefertermin",  Locked: false),
            new ColumnDef("coating-part",   "Lack-T",        Locked: false, DefaultWidth: 55),
            new ColumnDef("glass",          "Glas",          Locked: false, DefaultWidth: 45),
            new ColumnDef("purchase",       "Zukauf",        Locked: false, DefaultWidth: 55),
            new ColumnDef("cooling",        "VK",            Locked: false, DefaultWidth: 40),
            new ColumnDef("fan",            "VL",            Locked: false, DefaultWidth: 40),
            new ColumnDef("electric",       "VE",            Locked: false, DefaultWidth: 40),
            new ColumnDef("doors",          "VT",            Locked: false, DefaultWidth: 40),
            new ColumnDef("superstructure", "VA",            Locked: false, DefaultWidth: 40),
            new ColumnDef("status",         "Status",        Locked: false),
            new ColumnDef("row-actions",    "",              Locked: true,  DefaultWidth: 80),
            new ColumnDef("release",        "Freigabe",      Locked: false, DefaultWidth: 160),
            new ColumnDef("picker",         "Kommissionierer", Locked: false),
        ]
    };

    /// <summary>
    /// FaCompletion/Index.cshtml columns — Phase 4 FA-Vervollstaendigung-Liste.
    /// </summary>
    public static readonly ViewConfig FaCompletion = new(
        "FaCompletion", "FA-Vervollstaendigung",
        SupportsReorder: true, SupportsSortDefault: true)
    {
        Columns =
        [
            new ColumnDef("order-number",   "FA Nr.",        Locked: true,  DefaultWidth: 90),
            new ColumnDef("quantity",       "Stk.",          Locked: false, DefaultWidth: 55),
            new ColumnDef("customer",       "Kunde",         Locked: false),
            new ColumnDef("article-number", "Artikelnummer", Locked: false),
            new ColumnDef("description1",   "Bezeichnung 1", Locked: false),
            new ColumnDef("production-date","Fert.-Termin",  Locked: false),
            new ColumnDef("applicable",     "Anwendbar",     Locked: false, DefaultWidth: 100),
            new ColumnDef("completed",      "Vervollstaendigt", Locked: false, DefaultWidth: 130),
            new ColumnDef("spec-count",     "Auspraegungen", Locked: false, DefaultWidth: 110),
            new ColumnDef("row-actions",    "",              Locked: true,  DefaultWidth: 110),
        ]
    };

    /// <summary>
    /// FaWorklist/Index.cshtml columns — FA-Abarbeitungsliste (v1.22.0).
    /// Registrierung seit v1.26.0: vorher kannte GetByViewKey den Key nicht,
    /// die Prefs-API antwortete 400 und Zahnrad-Einstellungen gingen bei jedem
    /// Reload verloren. Dynamische Merkmal-Spalten ("attr-{id}") sind bewusst
    /// NICHT enthalten (die API-Validierung prueft nur den ViewKey).
    /// </summary>
    public static readonly ViewConfig FaWorklist = new(
        "FaWorklist", "FA-Abarbeitungsliste",
        SupportsReorder: true, SupportsSortDefault: true)
    {
        Columns =
        [
            new ColumnDef("order-number",   "FA Nr.",           Locked: true),
            new ColumnDef("workbench",      "Werkbank",         Locked: false),
            new ColumnDef("article-number", "Artikelnummer",    Locked: false),
            new ColumnDef("description1",   "Bezeichnung 1",    Locked: false),
            new ColumnDef("description2",   "Bezeichnung 2",    Locked: false),
            new ColumnDef("quantity",       "Stk.",             Locked: false),
            new ColumnDef("coating-date",   "Beschicht.",       Locked: false),
            new ColumnDef("bg-date",        "BG-Termin",        Locked: false),
            new ColumnDef("picking-date",   "Komm.",            Locked: false),
            new ColumnDef("production-date","Fert.-Termin",     Locked: false),
            new ColumnDef("kaeltemittel",   "Kaeltemittel",     Locked: false),
            new ColumnDef("ventil",         "Ventil",           Locked: false),
            new ColumnDef("ausfuehrung",    "Ausfuehrung E/Z",  Locked: false),
            new ColumnDef("maschine",       "Maschine",         Locked: false),
            new ColumnDef("sage-status",    "Sage-Status",      Locked: false),
            new ColumnDef("done",           "Erledigt",         Locked: true),
        ]
    };

    /// <summary>
    /// Picking/Index.cshtml columns (shown when LeitstandAktiv = true).
    /// Conditional columns:
    ///   - "picker" : rendered only when Model.PickerAssignmentEnabled
    /// Note: col index 5 (Stk.) has no data-col attribute in the view.
    /// </summary>
    public static readonly ViewConfig Picking = new("Picking", "Kommissionierliste", SupportsReorder: true, SupportsSortDefault: true)
    {
        Columns =
        [
            new ColumnDef("priority",    "Prio",             Locked: false, DefaultWidth: 60),
            new ColumnDef("order-number","FA Nr.",           Locked: true),
            new ColumnDef("article-number","Artikelnummer",  Locked: false),
            new ColumnDef("description", "Bezeichnung",      Locked: false),
            new ColumnDef("customer",    "Kunde",            Locked: false),
            new ColumnDef("quantity",    "Stk.",             Locked: false, DefaultWidth: 55),
            new ColumnDef("picking-date","Komm.-Termin",     Locked: false),
            new ColumnDef("status",      "Status",           Locked: false),
            // Conditional: only when PickerAssignmentEnabled
            new ColumnDef("picker",      "Kommissionierer",  Locked: false),
        ]
    };

    /// <summary>
    /// Tracking/OseonIndex.cshtml columns.
    /// This view uses a 3-level tree structure; reorder/sort not supported.
    /// The expand/toggle column (width 30px, no label) is structural/locked.
    /// </summary>
    public static readonly ViewConfig OseonTracking = new("OseonTracking", "OSEON Teileverfolgung", SupportsReorder: false, SupportsSortDefault: false)
    {
        Columns =
        [
            // Expand/collapse toggle (icon-only, structural)
            new ColumnDef("expand",      "",                 Locked: true,  DefaultWidth: 30),
            new ColumnDef("order-number","Auftrag",          Locked: true),
            new ColumnDef("article-number","Artikelnr.",     Locked: false),
            new ColumnDef("description", "Bezeichnung",      Locked: false),
            new ColumnDef("workbench",   "Werkbank",         Locked: false),
            new ColumnDef("status",      "Status",           Locked: false),
            new ColumnDef("progress",    "Soll / Ist",       Locked: false),
            new ColumnDef("end-date",    "Endtermin",        Locked: false),
        ]
    };

    /// <summary>
    /// Picking/Bom.cshtml columns.
    /// Reorder/sort not supported (BOM is a structured tree by position).
    /// Conditional columns:
    ///   - "order" : rendered only when ViewBag.BestellungenAktiv == true
    /// The first unnamed column (width 40px) contains the pick-checkbox/expand control.
    /// The "storage-location" and "source-location" columns have no data-col attribute (not filterable).
    /// </summary>
    public static readonly ViewConfig Bom = new("Bom", "Stueckliste (BOM)", SupportsReorder: false, SupportsSortDefault: false)
    {
        Columns =
        [
            // Pick checkbox / expand control (structural)
            new ColumnDef("pick-control",     "",               Locked: true,  DefaultWidth: 40),
            new ColumnDef("position",         "Pos.",           Locked: true,  DefaultWidth: 55),
            new ColumnDef("assembly-group",   "Baugruppe",      Locked: false),
            new ColumnDef("resource-number",  "Ressourcenummer",Locked: false),
            new ColumnDef("description1",     "Bezeichnung 1",  Locked: false),
            new ColumnDef("description2",     "Bezeichnung 2",  Locked: false),
            new ColumnDef("quantity",         "Menge",          Locked: false),
            new ColumnDef("procurement",      "Beschaffung",    Locked: false),
            new ColumnDef("article-group",    "Artikelgruppe",  Locked: false),
            new ColumnDef("category",         "Kategorie",      Locked: false),
            // Not filterable (no data-filterable) but always present
            new ColumnDef("storage-location", "Lagerplatz",     Locked: false),
            new ColumnDef("source-location",  "Quell-Lagerplatz",Locked: false),
            // Conditional: only when ViewBag.BestellungenAktiv == true
            new ColumnDef("order",            "Bestellen",      Locked: false, DefaultWidth: 80),
        ]
    };

    /// <summary>
    /// BdeBookings/Index.cshtml columns.
    /// Conditional columns:
    ///   - "actions" : rendered only when canAdminBde
    /// </summary>
    public static readonly ViewConfig BdeBookings = new("BdeBookings", "BDE-Buchungen", SupportsReorder: true, SupportsSortDefault: true)
    {
        Columns =
        [
            new ColumnDef("started-at",         "Start",          Locked: false),
            new ColumnDef("ended-at",           "Ende",           Locked: false),
            new ColumnDef("effective-duration", "Effektive Zeit", Locked: false),
            new ColumnDef("operator",           "Operator",       Locked: false),
            new ColumnDef("workplace",    "Werkbank",    Locked: false),
            new ColumnDef("booking-type", "Typ",         Locked: false),
            new ColumnDef("target",       "Ziel",        Locked: false),
            new ColumnDef("good-qty",     "Gut",         Locked: false),
            new ColumnDef("scrap-qty",    "Ausschuss",   Locked: false),
            new ColumnDef("status",       "Status",      Locked: false),
            // Conditional: only when canAdminBde
            new ColumnDef("actions",      "Aktionen",    Locked: true),
        ]
    };

    /// <summary>
    /// WarehousePicking/Details.cshtml — Positionsliste einer Lagerbestellung.
    /// Client-Mode-Tabelle (kleine, vorgefilterte Liste je Bestellung), aber mit
    /// vollem Spalten-Preferences-Muster. pos + article-number sind Locked
    /// (Identitaet der Position). Labels muessen mit den &lt;th&gt; in Details.cshtml
    /// UND dem inline #column-config JSON uebereinstimmen.
    /// </summary>
    public static readonly ViewConfig WarehousePickingDetails = new(
        "WarehousePickingDetails", "Lagerbestellung-Positionen",
        SupportsReorder: true, SupportsSortDefault: true)
    {
        Columns =
        [
            new ColumnDef("pos",            "Pos",             Locked: true),
            new ColumnDef("article-number", "Artikel-Nr",      Locked: true),
            new ColumnDef("description",    "Bezeichnung",     Locked: false),
            new ColumnDef("requested",      "Bestellt",        Locked: false),
            new ColumnDef("picked",         "Ist",             Locked: false),
            new ColumnDef("unit",           "ME",              Locked: false),
            new ColumnDef("storage",        "Lagerplatz",      Locked: false),
            new ColumnDef("note-lager",     "Notiz Lager",     Locked: false),
            new ColumnDef("note-ek",        "Notiz EK",        Locked: false),
            new ColumnDef("shortage",       "Fehlteil-Status", Locked: false),
        ]
    };

    public static ViewConfig? GetByViewKey(string viewKey) => viewKey switch
    {
        "ProductionOrders" => ProductionOrders,
        "PickingLeitstand" => PickingLeitstand,
        "FaCompletion"     => FaCompletion,
        "FaWorklist"       => FaWorklist,
        "Picking"          => Picking,
        "OseonTracking"    => OseonTracking,
        "Bom"              => Bom,
        "BdeBookings"      => BdeBookings,
        "WarehousePickingDetails" => WarehousePickingDetails,
        _                  => null
    };
}
