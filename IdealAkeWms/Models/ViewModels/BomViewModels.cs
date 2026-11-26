namespace IdealAkeWms.Models.ViewModels;

public record BomQueryResult(List<BomItem> Items, string DataSource, bool MengeIstAuftragsmenge = false);

/// <summary>Bekannte Werte von <see cref="BomQueryResult.DataSource"/>.</summary>
public static class BomDataSources
{
    public const string Sage = "SAGE";
    public const string Oseon = "OSEON";
    public const string NoData = "KEINE_DATEN";

    /// <summary>BOM-Bridge (Spec 2026-09-08): Quelle ist die lokale FA-Struktur (FaHierarchyNode).</summary>
    public const string FaHierarchie = "FA-HIERARCHIE";
}

public class BomItem
{
    public string Artikelnummer { get; set; } = string.Empty;
    public string? Position { get; set; }
    public string? Baugruppe { get; set; }
    public string? Ressourcenummer { get; set; }
    public string? Bezeichnung1 { get; set; }
    public string? Bezeichnung2 { get; set; }
    public decimal Menge { get; set; }
    public string? Beschaffungsartikel { get; set; }
    public string? Artikelgruppe { get; set; }
}

public class StockLocationInfo
{
    public string Code { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public int StorageLocationId { get; set; }
    public bool IsPrimaryStorageLocation { get; set; }

    /// <summary>Lagerplatz-Text der Lager-/Glasbestellung „Code (Menge), …“: nur positive Mengen, Hauptlagerplatz zuerst, dann Code
    /// (v1.99.45, Entscheidung Gerald 2026-11-19).</summary>
    public static string LagerplatzText(IEnumerable<StockLocationInfo> plaetze) => string.Join(", ", plaetze
        .Where(p => p.Quantity > 0)
        .OrderBy(p => p.IsPrimaryStorageLocation ? 0 : 1).ThenBy(p => p.Code)
        .Select(p => $"{p.Code} ({p.Quantity:N3})"));
}

public class BomItemViewModel
{
    /// <summary>Sichtbare Bestandsplaetze in der Stuecklisten-Zelle, der Rest per „+n weitere“ (v1.62.0).</summary>
    // ponytail: Konstante, AppSetting erst bei belegtem Bedarf (Spec 2026-10-16 Antwort 5a).
    public const int MaxSichtbareLagerplaetze = 3;

    public string Artikelnummer { get; set; } = string.Empty;
    public string? Position { get; set; }
    public string? Baugruppe { get; set; }
    public string? Ressourcenummer { get; set; }
    public string? Bezeichnung1 { get; set; }
    public string? Bezeichnung2 { get; set; }
    /// <summary>Matchcode der Komponente (Ressourcenummer): IDEAL Strukturknoten, sonst Artikelstamm (Variante B).</summary>
    public string? Matchcode { get; set; }
    public decimal Menge { get; set; }
    public string? Beschaffungsartikel { get; set; }
    public string? Artikelgruppe { get; set; }
    public string? KategorieName { get; set; }
    public List<StockLocationInfo> StockLocations { get; set; } = new();

    // Tree structure
    public int TreeLevel { get; set; }
    public bool IsBaugruppe { get; set; }

    // BOM-Bridge (hierarchisch, nur befuellt wenn die Zeile ein FaHierarchyBomItem ist)
    public string? Kommissionieren { get; set; }
    public string? Hauptlagerplatz { get; set; }
    public int Ebene { get; set; }
    public string? VaterSubFa { get; set; }
    public string? SagePosition { get; set; }
    public bool IsWaise { get; set; }
    public bool PositionsKollision { get; set; }

    // Picking fields
    public int? PickingItemId { get; set; }
    public bool IsPicked { get; set; }
    public int? SourceStorageLocationId { get; set; }
    public int? SuggestedSourceStorageLocationId { get; set; }
    public bool IsTransferred { get; set; }
    /// <summary>v1.88.0: Ist-Menge der Auftragszeile (nach dem Umbuchen) – Eingabe-Vorbelegung ist das Soll.</summary>
    public decimal? IstMenge { get; set; }
    /// <summary>v1.88.0: Status der Auftragszeile (Text in der Zeile, Farbe nie allein); null ohne Auftrag.</summary>
    public PickingOrderLineStatus? ZeilenStatus { get; set; }
    /// <summary>v1.99.89: Arbeitsbereich aus der FA-Struktur (nur Maske „Liste“ des Kommissionierauftrags).</summary>
    public string? Arbeitsbereich { get; set; }
}

/// <summary>
/// v1.99.89 (Spec 2026-11-26 Teil 3, K4): eine Zeile der Maske „Liste“ des Kommissionierauftrags = Summe der Auftragszeilen je
/// Ressourcenummer + Komm.-Ziel + BG-Kennzeichen. Gebaut von <see cref="IdealAkeWms.Services.KommissionierauftragListe.Summiere"/>.
/// </summary>
public class AuftragSummenzeile
{
    public string Ressourcenummer { get; set; } = string.Empty;
    public string? Kommissionieren { get; set; }
    public bool IsBaugruppe { get; set; }
    /// <summary>Auftragszeilen der Gruppe (je Zeile eine Position).</summary>
    public List<PickingOrderLine> Zeilen { get; set; } = new();
    /// <summary>Alle Positionen, natuerlich sortiert, leere zuletzt.</summary>
    public List<string> Positionen { get; set; } = new();
    public string PositionenText => string.Join(", ", Positionen);
    public decimal SollSumme { get; set; }
    /// <summary>Σ Soll der offenen Zeilen (Offen/Gepickt) – Vorbelegung der Ist-Menge (Etappe B).</summary>
    public decimal OffenSoll { get; set; }
    /// <summary>v1.99.89 (K8): Ids der offenen Zeilen in Positionsreihenfolge – Haken <c>data-ids</c>, Verteilung serverseitig.</summary>
    public List<int> OffeneZeilenIds => Zeilen.Where(z => z.Status is PickingOrderLineStatus.Offen or PickingOrderLineStatus.Gepickt).Select(z => z.Id).ToList();
    /// <summary>v1.99.89 (K6): alle offenen Zeilen schon gepickt (Haken vorbelegt wie <c>BomItemViewModel.IsPicked</c>).</summary>
    public bool AlleOffenenGepickt => Zeilen.Any(z => z.Status == PickingOrderLineStatus.Gepickt)
        && Zeilen.All(z => z.Status is not PickingOrderLineStatus.Offen);
    /// <summary>v1.99.89 (K9): Quell-Lagerplatz-Vorschlag je Artikel (aus der ersten Stuecklisten-Zeile der Gruppe); K4c-Zeilen ohne.</summary>
    public int? QuellVorschlagId { get; set; }
    /// <summary>Σ Ist der umgebuchten Zeilen; null, wenn keine umgebucht ist.</summary>
    public decimal? GebuchtIst { get; set; }
    /// <summary>Keine Zeile mehr offen: keine Eingaben, <see cref="StatusText"/> zeigen (K4b).</summary>
    public bool Erledigt { get; set; }
    /// <summary>Zeilenstatus als Text: ein Status = <c>ZeilenStatusText</c>, gemischt mit Umgebucht = „teilweise umgebucht (n von m Pos.)“.</summary>
    public string StatusText { get; set; } = string.Empty;
    /// <summary>K4b: „Rest x aus Pos. y geht beim Abschluss in den Folgeauftrag“, sonst null.</summary>
    public string? RestHinweis { get; set; }
    /// <summary>K4c: Auftragszeile(n) ohne Position in der aktuellen Stueckliste – Artikeleigenschaften leer.</summary>
    public bool OhneStuecklistenposition { get; set; }

    public string? Matchcode { get; set; }
    public string? Bezeichnung1 { get; set; }
    public string? Bezeichnung2 { get; set; }
    public string? Beschaffungsartikel { get; set; }
    public string? Artikelgruppe { get; set; }
    public string? KategorieName { get; set; }
    public string? Hauptlagerplatz { get; set; }
    public List<StockLocationInfo> StockLocations { get; set; } = new();
    public string? Baugruppe { get; set; }
    public string? SagePosition { get; set; }
    public string? Ebene { get; set; }
    public string? VaterSubFa { get; set; }
    public string? Arbeitsbereich { get; set; }
    public string? Abteilung { get; set; }
    /// <summary>Nur Druck (K11/S-6): Quell-Lagerplatz je Zeile (gespeichert bzw. Vorschlag), verschiedene „, “-getrennt.</summary>
    public string? QuellLagerplatzText { get; set; }
}

public class BomViewModel
{
    public int ProductionOrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    /// <summary>
    /// Eigene FA-Nummer des Auftrags (Z4-Konvention): im hierarchischen Modus die Sub-FA,
    /// im Flachmodus identisch mit <see cref="OrderNumber"/>. Null nur bei Alt-Aufrufern.
    /// </summary>
    public string? SubOrderNumber { get; set; }
    public string? ArticleNumber { get; set; }
    public string? Description1 { get; set; }
    public List<BomItemViewModel> Items { get; set; } = new();

    private HashSet<string>? _positionen;

    /// <summary>
    /// v1.99.23 (N-1): naechster VORHANDENER Vorfahre der Zeile in <see cref="Items"/> (Punkt-Praefixe von hinten), sonst null. Im
    /// Kommissionierauftrag fehlen Vater-Zeilen, deren Baugruppe in einem anderen Auftrag liegt – die Zeile haengt dann am Grossvater
    /// bzw. steht oben und bleibt sichtbar. Die volle Stueckliste hat keine Luecken: Ergebnis = direkter Vater wie bisher.
    /// </summary>
    public string? VaterPosition(BomItemViewModel item)
    {
        var pos = item.Position;
        if (string.IsNullOrEmpty(pos)) return null;
        _positionen ??= Items.Where(i => !string.IsNullOrEmpty(i.Position)).Select(i => i.Position!).ToHashSet();
        for (var i = pos.LastIndexOf('.'); i > 0; i = pos.LastIndexOf('.', i - 1))
        {
            var vater = pos[..i];
            if (_positionen.Contains(vater)) return vater;
        }
        return null;
    }

    public string? FilterText { get; set; }
    public string? DefaultFilterBeschaffung { get; set; }
    public string? DefaultFilterArtikelgruppe { get; set; }
    public string? DefaultFilterBomDescription1 { get; set; }
    public string? DefaultFilterBomKommissionierziel { get; set; }
    public List<StorageLocation> AllStorageLocations { get; set; } = new();
    public List<StorageLocation> TargetStorageLocations { get; set; } = new();
    public string? DataSource { get; set; }
    public bool RecursiveFilterSearch { get; set; }

    /// <summary>Quelle ist die FA-Hierarchie (DataSource FA-HIERARCHIE): hierarchische Spalten rendern.</summary>
    public bool Hierarchical { get; set; }
    /// <summary>Vollstruktur (HauptFA): zusaetzlich Ebene/Vater-Sub-FA rendern.</summary>
    public bool FullStructure { get; set; }

    /// <summary>
    /// Read-only-Modus (FA-Abarbeitungsliste, v1.22.0): reine BOM-Anzeige ohne
    /// Picking-Checkboxen, Lagerplatz-Dropdowns, Umbuchen, Fotos und Bedarfsmeldungen.
    /// </summary>
    public bool ReadOnly { get; set; }

    /// <summary>
    /// Ziel-Controller des "Zurueck"-Buttons im Read-only-Modus. Default
    /// <c>FaWorklist</c> (Abarbeitungsliste); FaCompletion setzt <c>FaCompletion</c>,
    /// damit fa_completion-User nicht auf die nicht-erreichbare FaWorklist-Index geleitet
    /// werden. Im Picking-Modus ungenutzt.
    /// </summary>
    public string BackController { get; set; } = "FaWorklist";

    /// <summary>Beschriftung des "Zurueck"-Buttons im Read-only-Modus.</summary>
    public string BackText { get; set; } = "Zurück zur Abarbeitungsliste";

    /// <summary>v1.88.0: Auftragsmodus – der Kommissionierauftrag dieser Seite (null = FA-Stueckliste, lesend).</summary>
    public PickingOrder? Auftrag { get; set; }
    /// <summary>v1.88.0: Kommissionierauftraege des FA (nur FA-Stueckliste, als Links).</summary>
    public List<PickingOrder> FaAuftraege { get; set; } = new();
    /// <summary>v1.99.70 (F8): live berechnete Ueberdeckung (Stueckliste kleiner als offene Kommissionierzeilen); leer = kein Hinweis.</summary>
    public List<IdealAkeWms.Services.PickingOrderRestmengen.Ueberdeckung> StuecklisteKleiner { get; set; } = new();

    /// <summary>v1.99.89 (K2/K3): Umschalter „Stückliste | Liste“ sichtbar (Auftrag, Quelle FA-Struktur, Kommissionierlisten an).</summary>
    public bool KannListe { get; set; }
    /// <summary>v1.99.89: Maske „Liste“ aktiv (summiert je Artikel, Partial <c>_AuftragListeTabelle</c>).</summary>
    public bool IstListe { get; set; }
    public List<AuftragSummenzeile> Summenzeilen { get; set; } = new();
    /// <summary>K4c: Anzahl Auftragszeilen ohne Stuecklistenposition (Banner).</summary>
    public int OhneStuecklistenposition { get; set; }
    /// <summary>K11: PDF-Knopf in der Liste nur bei verfuegbarem PDF-Dienst.</summary>
    public bool PdfVerfuegbar { get; set; }
}

/// <summary>v1.99.89 (K11): summierter Auftragsdruck aus der Maske „Liste“ (View <c>PrintAuftragListe</c>, auch als PDF).</summary>
public class PrintAuftragListeViewModel
{
    public PrintPickingViewModel Kopf { get; set; } = new();
    public List<AuftragSummenzeile> Zeilen { get; set; } = new();
    public int OhneStuecklistenposition { get; set; }
    public bool FullStructure { get; set; }
    public IReadOnlyList<string> VisibleColumns { get; set; } = Array.Empty<string>();
}

public class PrintPickingViewModel
{
    // v1.88.0: Druck je Kommissionierauftrag
    public string AuftragNummer { get; set; } = string.Empty;
    public string? AuftragBarcode { get; set; }
    public string? Bereich { get; set; }
    public string? Status { get; set; }
    public DateTime? Faelligkeit { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string? ArticleNumber { get; set; }
    /// <summary>v1.99.8: Matchcode des FA-Artikels im Druckkopf.</summary>
    public string? Matchcode { get; set; }
    public string? Description1 { get; set; }
    public decimal Quantity { get; set; }
    public DateTime? ProductionDate { get; set; }
    public string? PickedBy { get; set; }
    public string? PrintedBy { get; set; }
    public List<PrintPickingItem> Items { get; set; } = new();
}

public class PrintPickingItem
{
    public string Artikelnummer { get; set; } = string.Empty;
    public string? Bezeichnung1 { get; set; }
    public decimal Menge { get; set; }
    // v1.88.0
    public string? Position { get; set; }
    public decimal? IstMenge { get; set; }
    public string? Lagerplatz { get; set; }
    public string? Status { get; set; }
}

public class PrintBomViewModel
{
    public string OrderNumber { get; set; } = string.Empty;
    public string? ArticleNumber { get; set; }
    public string? Description1 { get; set; }
    public decimal Quantity { get; set; }
    public DateTime? ProductionDate { get; set; }
    public string? PrintedBy { get; set; }
    public List<PrintBomItem> Items { get; set; } = new();
    /// <summary>Aktive Filterinfo für Header-Anzeige (z.B. "Artikelgruppe=960")</summary>
    public string? FilterInfo { get; set; }
    /// <summary>Sichtbare Spalten-Keys aus der interaktiven Ansicht. Leer = alle Spalten anzeigen.</summary>
    public List<string> VisibleColumns { get; set; } = new();
}

public class PrintBomItem
{
    public string? Position { get; set; }
    public string? Baugruppe { get; set; }
    public string? Ressourcenummer { get; set; }
    public string? Bezeichnung1 { get; set; }
    public string? Bezeichnung2 { get; set; }
    public string? Matchcode { get; set; }
    public decimal Menge { get; set; }
    public string? Beschaffungsartikel { get; set; }
    public string? Artikelgruppe { get; set; }
    public string? Lagerplatz { get; set; }
    public int TreeLevel { get; set; }
    public bool IsBaugruppe { get; set; }
    // Hierarchische Spalten (nur IDEAL, aus FaHierarchyBomItem) — fuer den Druck-Durchschlag.
    public string? Kommissionieren { get; set; }
    public string? Hauptlagerplatz { get; set; }
    public int Ebene { get; set; }
}
