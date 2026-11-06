using IdealAkeWms.Models;
using IdealAkeWms.Services.SyncLogger;

namespace IDEALAKEWMSService.Services.Sdata;

/// <summary>
/// Spec 2026-11-06-sage-sdata-stammdaten-sync (E4a/F6): Quellen-Weiche fuer <see cref="ISageFaStrukturReader"/> (Schalter
/// <c>Sync:Quelle:FaStruktur</c>, Default View). View = <see cref="SageFaStrukturReader"/> (bisheriger Lese-Code unveraendert),
/// SData = <see cref="SdataFaStrukturReader"/> (nur Kanten, G11). <c>SData</c> ist gesperrt bis P-F2 und wirkt wie <c>View</c> mit
/// Warnung; <c>Vergleich</c> ist erlaubt, vergleicht die Kanten und gibt immer die View-Daten zurueck.
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
    };

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

    // TODO(P-F2): Sperre aufheben erst, wenn FAInfos (SQL) und die Knotenfelder im SData-Weg belegt sind (G11).
    protected override string? SdataGesperrtBis => "P-F2";

    /// <summary>Hinweise des SData-Readers — leer, wenn diesmal nur die View gelesen wurde.</summary>
    public IReadOnlyList<SdataHinweis> SdataHinweise =>
        GenutzteQuelle is SageQuelle.SData or SageQuelle.Vergleich && _sdata is SdataFaStrukturReader r ? r.Hinweise : Array.Empty<SdataHinweis>();

    public Task<FaStrukturLesung> LesenAsync(CancellationToken ct = default)
    {
        // Der View-Knoten-Select nimmt nur Strukturen mit FAInfos-Zeile (EXISTS, FaHierarchySql.BuildNodeSelectSql); dieselbe Grenze
        // gilt fuer die SData-Seite des Vergleichs, sonst erschiene jede Struktur ohne PPS-Daten als „fehlt in View“.
        HashSet<int>? mitInfos = null;
        return LeseAsync<FaStrukturLesung, FaHierarchyNode>(
            async c =>
            {
                var v = await _view.LesenAsync(c);
                mitInfos = v.InfosAnzahl == 0 ? null : v.Infos.Select(i => i.HauptFA).ToHashSet();
                return v;
            },
            _sdata.LesenAsync,
            l => mitInfos is null ? l.Knoten : l.Knoten.Where(k => mitInfos.Contains(k.HauptFA)),
            Schluessel, VergleichsFelder, ct);
    }

    /// <summary>FA-Knoten (Wurzel, Baugruppe) ueber die eigene BelID (<c>SubFA</c>, global eindeutig) — so vergleicht das Feld
    /// <c>VaterFA</c>/<c>Position</c> die Eltern-Kante. Blaetter (<c>SubFA = 0</c>) ueber ihren Platz im Vater-FA.</summary>
    internal static string Schluessel(FaHierarchyNode k) =>
        k.SubFA != 0 ? $"FA {k.SubFA}" : $"Blatt {k.VaterFA}/{k.Position}";
}
