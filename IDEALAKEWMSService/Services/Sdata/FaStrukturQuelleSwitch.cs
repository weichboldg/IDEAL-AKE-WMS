using IdealAkeWms.Models;
using IdealAkeWms.Services.SyncLogger;

namespace IDEALAKEWMSService.Services.Sdata;

/// <summary>
/// Spec 2026-11-06-sage-sdata-stammdaten-sync (E4a/F6): Quellen-Weiche fuer <see cref="ISageFaStrukturReader"/> (Schalter
/// <c>Sync:Quelle:FaStruktur</c>, Default View). View = <see cref="SageFaStrukturReader"/> (bisheriger Lese-Code unveraendert),
/// SData = <see cref="SdataFaStrukturReader"/> (Knoten + Felder, hybrid mit FAInfos aus der View, E5). <c>SData</c> ist gesperrt
/// (<see cref="SageQuelleSperre"/>) und wirkt wie <c>View</c> mit Warnung; <c>Vergleich</c> ist erlaubt, vergleicht die Kanten und gibt die View-Daten zurueck — ergaenzt um die FA-Positionen aus
/// SData (E4b, <see cref="FaStrukturLesung.Positionen"/>).
/// Registrierung: <see cref="SdataRegistrierung.AddFaStrukturQuelle"/>.
/// </summary>
public class FaStrukturQuelleSwitch : SageQuelleWeiche, ISageFaStrukturReader
{
    /// <summary>
    /// Vergleich je Knoten (<see cref="Schluessel"/>) auf der Eltern-Kante und den aus SData belegten Feldern (F6).
    /// Wurzel: <c>Artnr</c> und Mengen werden nicht verglichen — ihre Herkunft in der View ist unbelegt (FAListe.sql: Positionsfelder, P-F1).
    /// </summary>
    public static readonly SdataVergleich.Feld<FaHierarchyNode>[] VergleichsFelder =
    {
        new("HauptFA", k => k.HauptFA),
        new("VaterFA", k => k.VaterFA),
        new("Position", k => k.Position),
        new("SubFA", k => k.SubFA),
        new("HauptArtnr", k => k.HauptArtnr?.Trim()),
        new("Artnr", k => k.VaterFA is null ? null : k.Artnr?.Trim()),
        new("Sollmenge", k => k.VaterFA is null ? null : k.Sollmenge),
        new("Fertigungmenge", k => k.VaterFA is null ? null : k.Fertigungmenge),
        // Spec 2026-11-16-fa-struktur-ideal-ueber-sdata E4: alle Knotenfelder (Arbeitshypothese bis P-FS1, Freigabe-Antwort 2). Blattfelder
        // ohne SData-Quelle (Beschichtet, Breite, Hoehe, Tiefe, EKBedarf; Rueckfrage 4) werden bewusst mitverglichen - Abweichung sichtbar.
        new("Matchcode", k => T(k.Matchcode)),
        new("Bezeichnung1", k => T(k.Bezeichnung1)),
        new("Bezeichnung2", k => T(k.Bezeichnung2)),
        new("Artikelgruppe", k => T(k.Artikelgruppe)),
        new("Beschaffungsartikel", k => k.Beschaffungsartikel),
        new("Hauptlagerplatz", k => T(k.Hauptlagerplatz)),
        new("BemerkungPN", k => T(k.BemerkungPN)),
        new("FertigungsInfoPN", k => T(k.FertigungsInfoPN)),
        new("Kommissionieren", k => T(k.Kommissionieren)),
        new("Arbeitsbereich", k => T(k.Arbeitsbereich)),
        new("Arbeitsschritte", k => T(k.Arbeitsschritte)),
        new("Artikeltyp", k => T(k.Artikeltyp)),
        new("Material", k => T(k.Material)),
        new("VMBedarf", k => T(k.VMBedarf)),
        new("Beschichtet", k => k.Beschichtet),
        new("Breite", k => k.Breite),
        new("Hoehe", k => k.Hoehe),
        new("Tiefe", k => k.Tiefe),
        new("EKBedarf", k => k.EKBedarf),
    };

    /// <summary>Text fuer den Vergleich: getrimmt, leer = null (View liefert '' wo SData nichts liefert).</summary>
    private static string? T(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private readonly ISageFaStrukturReader _view;
    private readonly ISageFaStrukturReader _sdata;

    public FaStrukturQuelleSwitch(ISageFaStrukturReader view, ISageFaStrukturReader sdata, IConfiguration configuration,
        ISyncErrorNotifier errorNotifier, ISyncLogger syncLogger)
        : base(configuration, errorNotifier, syncLogger)
    {
        _view = view;
        _sdata = sdata;
    }

    protected override string Bereich => SageQuelleSchalter.FaStruktur;

    // Spec 2026-11-16-fa-struktur-ideal-ueber-sdata E5: Hybrid gebaut (HybridAsync); die Sperre (SageQuelleSperre.Grund "FaStruktur")
    // bleibt bis DPS-Freigabe RealTimeData/P-FS3 und Rueckfragen 4/5 - Auflage Gerald 2026-11-16. Aufheben nur in SageQuelleSperre.

    /// <summary>Hinweise des SData-Readers — leer, wenn diesmal nur die View gelesen wurde.</summary>
    public IReadOnlyList<SdataHinweis> SdataHinweise =>
        GenutzteQuelle is SageQuelle.SData or SageQuelle.Vergleich && _sdata is SdataFaStrukturReader r ? r.Hinweise : Array.Empty<SdataHinweis>();

    public async Task<FaStrukturLesung> LesenAsync(CancellationToken ct = default)
    {
        // Der View-Knoten-Select nimmt nur Strukturen mit FAInfos-Zeile (EXISTS, FaHierarchySql.BuildNodeSelectSql); dieselbe Grenze
        // gilt fuer die SData-Seite des Vergleichs, sonst erschiene jede Struktur ohne PPS-Daten als „fehlt in View“.
        HashSet<int>? mitInfos = null;
        List<ProductionOrderPosition>? positionen = null;
        var lesung = await LeseAsync<FaStrukturLesung, FaHierarchyNode>(
            async c =>
            {
                var v = await _view.LesenAsync(c);
                mitInfos = v.InfosAnzahl == 0 ? null : v.Infos.Select(i => i.HauptFA).ToHashSet();
                return v;
            },
            async c =>
            {
                // Freigabe-Antwort 1 (Spec 2026-11-16-fa-struktur-ideal-ueber-sdata): RealTimeData im Dienst nur im Modus Vergleich.
                if (_sdata is SdataFaStrukturReader r) r.RealTimeDataErlaubt = RealTimeDataErlaubt(GenutzteQuelle);
                var s = await _sdata.LesenAsync(c);
                positionen = s.Positionen;
                return GenutzteQuelle == SageQuelle.SData ? await HybridAsync(s, c) : s;
            },
            l => mitInfos is null ? l.Knoten : l.Knoten.Where(k => mitInfos.Contains(k.HauptFA)),
            Schluessel, VergleichsFelder, ct);
        // E4b/F6: Positionen gibt es nur aus SData (keine View-Quelle) — sie werden geschrieben, sobald SData gelesen wurde, also auch im
        // Vergleichsmodus. View und gesperrtes SData lesen SData nicht; scheitert der SData-Read im Vergleich, bleibt positionen null.
        return positionen is null || ReferenceEquals(lesung.Positionen, positionen) ? lesung : lesung with { Positionen = positionen };
    }

    /// <summary>
    /// E5/F6 (SData-Modus): Knoten aus SData, FAInfos aus der View (<see cref="SageFaStrukturReader.InfosLesenAsync"/>), geschrieben nur
    /// Strukturen mit FAInfos-Zeile - dieselbe Grenze wie das EXISTS des View-Selects. Ohne FAInfos-Zeilen bleibt <c>InfosAnzahl</c> 0 und
    /// der Empty-Guard des Abgleichs greift wie bisher. Erreichbar erst, wenn <see cref="SageQuelleSperre"/> den Bereich freigibt.
    /// </summary>
    private async Task<FaStrukturLesung> HybridAsync(FaStrukturLesung sdata, CancellationToken ct)
    {
        var v = _view is SageFaStrukturReader sql ? await sql.InfosLesenAsync(ct) : await _view.LesenAsync(ct);
        var mit = v.Infos.Select(i => i.HauptFA).ToHashSet();
        return sdata with { Knoten = sdata.Knoten.Where(k => mit.Contains(k.HauptFA)).ToList(), Infos = v.Infos, InfosAnzahl = v.InfosAnzahl };
    }

    /// <summary>Freigabe-Antwort 1: RealTimeData (dtsFaBelegsuche) im Dienst nur lesend und nur im Modus Vergleich - bis DPS-Antwort bzw.
    /// P-FS3. Erst danach darf diese Regel (an genau dieser Stelle) auch SData zulassen.</summary>
    internal static bool RealTimeDataErlaubt(SageQuelle? quelle) => quelle == SageQuelle.Vergleich;

    /// <summary>FA-Knoten (Wurzel, Baugruppe) ueber die eigene BelID (<c>SubFA</c>, global eindeutig) — so vergleicht das Feld
    /// <c>VaterFA</c>/<c>Position</c> die Eltern-Kante. Blaetter (<c>SubFA = 0</c>) ueber ihren Platz im Vater-FA.</summary>
    internal static string Schluessel(FaHierarchyNode k) =>
        k.SubFA != 0 ? $"FA {k.SubFA}" : $"Blatt {k.VaterFA}/{k.Position}";
}
