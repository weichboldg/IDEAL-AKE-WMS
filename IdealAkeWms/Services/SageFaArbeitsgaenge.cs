using System.Text.Json;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services.SageSdata;

namespace IdealAkeWms.Services;

/// <summary>
/// v1.99.38 (Spec 2026-11-17-arbeitsgaenge-aus-wms-in-sage-fa-anlegen, E0/E1a): Arbeitsgaenge (Kuerzel aus der FA-Struktur) als
/// AG-Positionen in Sage-Fertigungsauftraegen anlegen. EINE Stelle fuer Web (Vorschau, Einreihen) und Dienst (Sende-Lauf): Schluessel,
/// Texte, der lesende Sage-Stand (nur GET) und die reine Vorschau-Funktion.
/// </summary>
public static class SageFaArbeitsgaenge
{
    /// <summary>Schalter (ServiceSetting, Default aus): Knopf + Annahme im Web, Senden im Dienst.</summary>
    public const string ToggleKey = "SageFaArbeitsgaengeAnlegenAktiv";
    /// <summary>Bearbeiter-Kuerzel der AG-Position in Sage. Leer = nichts senden (sichtbarer Fehler).</summary>
    public const string BearbeiterKey = "Sync:SageFaArbeitsgaengeBearbeiter";
    public const string MaxWiederholungenKey = "Sync:SageFaArbeitsgaengeMaxWiederholungen";
    public const string WiederholAbstandKey = "Sync:SageFaArbeitsgaengeWiederholAbstandMinuten";
    public const string ServiceActor = "system:sage-fa-arbeitsgaenge";

    // Status-Texte der Vorschau (Spec E0.3 + Freigabe-Antworten 4/14)
    public const string NichtGeprueft = "nicht geprüft";
    public const string Anlegbar = "anlegbar";
    public const string OhneSageAg = "Kürzel ohne Sage-AG";
    public const string InSageVorhanden = "in Sage schon vorhanden";
    public const string FaBeendet = "FA beendet/gesperrt";
    public const string FaBegonnen = "FA begonnen";
    public const string SageNichtLesbar = "Sage nicht lesbar";
    // Status der Warteschlange (E1a) — gewinnen in der Vorschau vor dem Live-Stand
    public const string Eingereiht = "eingereiht";
    public const string Gesendet = "gesendet";
    public const string Angelegt = "angelegt";
    public const string QueueFehler = "Fehler";
    public const string BearbeiterFehlt = "Sage-Bearbeiter für AG-Anlage fehlt (Service-Einstellungen) – nichts gesendet.";
    public const string UnklarText = "Antwort unklar – bitte in Sage prüfen, ob der Arbeitsgang am FA steht; danach erneut einreihen.";
    public const string AusText = "Arbeitsgänge in Sage anlegen ist ausgeschaltet – nichts eingereiht.";

    /// <summary>Last-Grenze der Pruefung: mehr FAs = enger filtern (Haupt-FA). ponytail: feste Grenze, Einstellung erst bei Bedarf.</summary>
    public const int MaxFasJePruefung = 500;

    public const string RessourceKoepfe = "PpsFaBelegeInfo";
    public const string RessourcePositionen = "PpsFaBelegePositionenInfo";

    /// <summary>Kuerzel → wirksame Sage-AG nach der Regel der Pruefliste (getrimmt, ohne Gross/Klein, kleinste Id gewinnt).</summary>
    public static Dictionary<string, OperationConfig> KuerzelLookup(IEnumerable<OperationConfig> configs)
    {
        var byName = new Dictionary<string, OperationConfig>(StringComparer.OrdinalIgnoreCase);
        foreach (var c in configs.OrderBy(c => c.Id)) byName.TryAdd(c.OperationName.Trim(), c);
        return byName;
    }

    // ------------------------------------------------------------------ Sage-Stand (nur GET) ------------------------------------------

    /// <summary>Kopf aus <c>PpsFaBelegeInfo</c>; Namen wie der AKE-Lesetest L1 (Sage-Boolean -1/0 ueber <see cref="SageBoolJsonConverter"/>).
    /// Ohne select gelesen: ein falscher Feldname im select waere HTTP 500, fehlende Felder bleiben hier null.</summary>
    public sealed record KopfZeile(int? BelID, decimal? Sollmenge, decimal? Fertigungmenge, decimal? Istmenge,
        bool? IstBegonnen, bool? IstBeendet, bool? IstGesperrt, bool? Aktiv);

    /// <summary>Position aus <c>PpsFaBelegePositionenInfo</c>. Nummern als <see cref="JsonElement"/>: Sage liefert sie je nach Feld als
    /// Zahl oder Text (TODO P-5: Typ und Inhalt von ArbeitsgangNummer an der IDEAL belegen).</summary>
    public sealed record PositionZeile(int? BelID, string? RessourceTyp, JsonElement? RessourceNummer, JsonElement? ArbeitsgangNummer);

    /// <summary>Stand eines FA in Sage. <see cref="Gefunden"/> = false: nicht in PpsFaBelegeInfo (beendete Haupt-FAs fehlen dort,
    /// fallstricke §40) → nie anlegbar.</summary>
    public sealed record FaStand(bool Gefunden, bool Beendet, bool Begonnen, decimal? Fertigungsmenge, IReadOnlySet<string> AgNummern);

    public static string? Text(JsonElement? e) => e is not { } v ? null : v.ValueKind switch
    {
        JsonValueKind.String => v.GetString()?.Trim() is { Length: > 0 } s ? s : null,
        JsonValueKind.Number => v.GetRawText(),
        _ => null,
    };

    /// <summary>Liest Kopf und AG-Positionen der FAs (in Bloecken zu 50, mit orderBy). Wirft <see cref="SdataLeseException"/> bei jedem
    /// Lesefehler — nie „leer“ statt Fehler. AG-Positionen werden im Code gefiltert (RessourceTyp „AG“): ne/eq-Filter auf Text liefern laut
    /// fallstricke „Sage-SData-Lesen“ teils 0 Zeilen (TODO P-5: Wert an der IDEAL belegen).</summary>
    public static async Task<Dictionary<int, FaStand>> LesenAsync(ISageSdataReadClient client, SdataLeseZiel ziel, IEnumerable<int> belIds,
        CancellationToken ct)
    {
        var ids = belIds.Where(i => i > 0).Distinct().ToList();
        var koepfe = new List<KopfZeile>();
        var positionen = new List<PositionZeile>();
        foreach (var where in SageSdataQuellen.OrBloecke("BelID", ids))
        {
            koepfe.AddRange((await client.QueryAllAsync<KopfZeile>(ziel, SageSdataReadClient.VertragResources, RessourceKoepfe, where, null,
                "BelID", ct)).Zeilen);
            positionen.AddRange((await client.QueryAllAsync<PositionZeile>(ziel, SageSdataReadClient.VertragResources, RessourcePositionen,
                where, null, "BelPosID", ct)).Zeilen);
        }
        return Auswerten(ids, koepfe, positionen);
    }

    internal static Dictionary<int, FaStand> Auswerten(IEnumerable<int> ids, IEnumerable<KopfZeile> koepfe, IEnumerable<PositionZeile> positionen)
    {
        var kopfJeFa = koepfe.Where(k => k.BelID != null).GroupBy(k => k.BelID!.Value).ToDictionary(g => g.Key, g => g.First());
        var agJeFa = positionen
            .Where(p => p.BelID != null && string.Equals(p.RessourceTyp?.Trim(), "AG", StringComparison.OrdinalIgnoreCase))
            .GroupBy(p => p.BelID!.Value)
            // TODO(P-5/P-2): welches Feld traegt an der IDEAL die AG-Nummer? Konservativ beide: ein Treffer in einem davon = vorhanden
            // (lieber einmal zu wenig anlegen als doppelt).
            .ToDictionary(g => g.Key, g => (IReadOnlySet<string>)g.SelectMany(p => new[] { Text(p.ArbeitsgangNummer), Text(p.RessourceNummer) })
                .Where(s => s != null).Select(s => s!).ToHashSet(StringComparer.OrdinalIgnoreCase));
        var leer = (IReadOnlySet<string>)new HashSet<string>();
        return ids.Distinct().ToDictionary(id => id, id =>
        {
            var ag = agJeFa.GetValueOrDefault(id) ?? leer;
            if (!kopfJeFa.TryGetValue(id, out var k)) return new FaStand(false, true, false, null, ag);
            var beendet = k.IstBeendet == true || k.IstGesperrt == true || k.Aktiv == false;
            // TODO(P-5): Rueckmeldung > 0 als Kriterium; heute IstBegonnen oder Istmenge > 0 (Freigabe-Antwort 4).
            var begonnen = k.IstBegonnen == true || k.Istmenge > 0;
            var menge = k.Fertigungmenge is > 0 ? k.Fertigungmenge : k.Sollmenge;
            return new FaStand(true, beendet, begonnen, menge, ag);
        });
    }

    /// <summary>Warteschlangen-Zeilen als Vorschau-Status je (BelID, Sage-AG).</summary>
    public static Dictionary<(int BelId, string SageAg), (string Status, string? Hinweis)> QueueStatus(IEnumerable<SageFaArbeitsgangAnlage> anlagen) =>
        anlagen.GroupBy(a => (a.BelId, a.SageArbeitsgangnummer.Trim()))
            .ToDictionary(g => g.Key, g =>
            {
                var a = g.OrderByDescending(x => x.Id).First();
                return a.Status switch
                {
                    SageFaArbeitsgangStatus.Offen => (Eingereiht, (string?)$"eingereiht von {a.ModifiedBy ?? a.CreatedBy}"),
                    SageFaArbeitsgangStatus.Gesendet => (Gesendet, null),
                    SageFaArbeitsgangStatus.Angelegt => (Angelegt, a.SageAm is { } t ? $"am {t:dd.MM.yyyy HH:mm}" : null),
                    SageFaArbeitsgangStatus.InSageVorhanden => (InSageVorhanden, "vom Dienst vor dem Senden gefunden"),
                    _ => (QueueFehler, a.Fehler),
                };
            }, new BelIdAgComparer());

    private sealed class BelIdAgComparer : IEqualityComparer<(int BelId, string SageAg)>
    {
        public bool Equals((int BelId, string SageAg) x, (int BelId, string SageAg) y) =>
            x.BelId == y.BelId && string.Equals(x.SageAg, y.SageAg, StringComparison.OrdinalIgnoreCase);
        public int GetHashCode((int BelId, string SageAg) o) => HashCode.Combine(o.BelId, StringComparer.OrdinalIgnoreCase.GetHashCode(o.SageAg));
    }

    /// <summary>Zeile darf (erneut) eingereiht werden: anlegbar nach Pruefung, oder Fehler in der Warteschlange.</summary>
    public static bool IstEinreihbar(Zeile z) => z.Status is Anlegbar or QueueFehler;

    // ------------------------------------------------------------------ Vorschau (reine Funktion) -------------------------------------

    /// <summary>Eine Vorschau-Zeile: ein FA × ein Kuerzel (Reihenfolge wie im Feld, Dubletten einmal).</summary>
    public sealed record Zeile(int HauptFa, int BelId, bool IstHauptFa, string? Artnr, string? Matchcode, int Reihenfolge, string Kuerzel,
        string? SageAg, string Status, string? Hinweis, int? AgInSage)
    {
        public bool IstAnlegbar => Status == Anlegbar;
        public string Ebene => IstHauptFa ? "Haupt" : "Unter";
    }

    public sealed record Ergebnis(IReadOnlyList<Zeile> Zeilen, int Blaetter, int NurInSage);

    /// <param name="knoten">aktive Knoten (VerschwundenSeit wird hier nochmals gefiltert).</param>
    /// <param name="sage">null = nicht geprueft; sonst Stand je BelID (fehlender Eintrag = nicht lesbar).</param>
    /// <param name="sageFehler">gesetzt = Sage war nicht lesbar (Banner), Zeilen mit Sage-AG werden „Sage nicht lesbar“.</param>
    /// <param name="queueStatus">Status der Warteschlange je (BelID, Sage-AG); gewinnt vor dem Live-Stand (E1a).</param>
    public static Ergebnis BuildVorschau(IEnumerable<FaHierarchyNode> knoten, IReadOnlyList<OperationConfig> configs,
        IReadOnlyDictionary<int, FaStand>? sage, bool sageFehler = false,
        IReadOnlyDictionary<(int BelId, string SageAg), (string Status, string? Hinweis)>? queueStatus = null)
    {
        var lookup = KuerzelLookup(configs);
        var aktiv = knoten.Where(n => n.VerschwundenSeit == null).ToList();
        var blaetter = aktiv.Count(n => n.SubFA == 0 && FaArbeitsschrittSymbole.Tokenize(n.Arbeitsschritte).Count > 0);
        var zeilen = new List<Zeile>();
        var nurInSage = 0;

        // je FA genau ein Knoten (Wurzel zuerst); Blaetter (SubFA = 0) haben keinen eigenen FA (Freigabe-Antwort 3)
        foreach (var n in aktiv.Where(n => n.SubFA != 0).OrderBy(n => n.HauptFA).ThenBy(n => n.VaterFA.HasValue).ThenBy(n => n.VaterFA)
                     .ThenBy(n => n.Position).DistinctBy(n => n.SubFA))
        {
            var tokens = FaArbeitsschrittSymbole.Tokenize(n.Arbeitsschritte);
            if (tokens.Count == 0) continue;
            var stand = sage?.GetValueOrDefault(n.SubFA);
            var agDesFa = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var reihenfolge = 0;
            foreach (var g in tokens.GroupBy(t => t, StringComparer.OrdinalIgnoreCase))
            {
                reihenfolge++;
                var ag = lookup.GetValueOrDefault(g.Key)?.WirksameSageArbeitsgangnummer?.Trim();
                string? hinweis = g.Count() > 1 ? $"{g.Count()}× im Feld – wird einmal angelegt" : null;
                string status;
                if (!string.IsNullOrEmpty(ag) && !agDesFa.Add(ag))
                    hinweis = Verbinden(hinweis, $"Sage-AG {ag} auch bei einem anderen Kürzel dieses FA – wird einmal angelegt");
                if (string.IsNullOrEmpty(ag)) { status = OhneSageAg; ag = null; }
                else if (queueStatus?.TryGetValue((n.SubFA, ag), out var q) == true) { status = q.Status; hinweis = Verbinden(hinweis, q.Hinweis); }
                else if (sage == null) status = sageFehler ? SageNichtLesbar : NichtGeprueft;
                else if (stand == null) status = SageNichtLesbar;
                else if (stand.AgNummern.Contains(ag)) status = InSageVorhanden;
                else if (stand.Beendet) status = FaBeendet;
                else if (stand.Begonnen) status = FaBegonnen;
                else status = Anlegbar;
                zeilen.Add(new Zeile(n.HauptFA, n.SubFA, n.VaterFA == null, n.Artnr, n.Matchcode, reihenfolge, g.Key, ag, status, hinweis,
                    stand?.AgNummern.Count));
            }
            if (stand != null) nurInSage += stand.AgNummern.Count(a => !agDesFa.Contains(a));
        }
        return new Ergebnis(zeilen, blaetter, nurInSage);
    }

    private static string? Verbinden(string? a, string? b) => a == null ? b : b == null ? a : $"{a}; {b}";
}
