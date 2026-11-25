using System.Globalization;
using IdealAkeWms.Data;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace IdealAkeWms.Services;

/// <summary>v1.99.32 (Spec 2026-11-13-bde-vereinfacht-teileliste-blechfertigung, ADR 0030): fachlicher Schluessel einer Strukturzeile.
/// <c>FaHierarchyNode.Id</c> ist KEIN Schluessel (der Struktur-Sync loescht und legt neu an). Leere Artnr = null (wie im UNIQUE-Index).</summary>
public sealed record TeilSchluessel(int HauptFA, int? VaterFA, int SubFA, int? Position, string? Artnr)
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public static TeilSchluessel Von(FaHierarchyNode n) => new(n.HauptFA, n.VaterFA, n.SubFA, n.Position, string.IsNullOrEmpty(n.Artnr) ? null : n.Artnr);
    public static TeilSchluessel Von(BdeTeilMeldung m) => new(m.HauptFA, m.VaterFA, m.SubFA, m.Position, string.IsNullOrEmpty(m.Artnr) ? null : m.Artnr);

    /// <summary>Formularwert "HauptFA|VaterFA|SubFA|Position|Artnr" (Artnr zuletzt, darf selbst "|" enthalten).</summary>
    public string Text => string.Join('|', HauptFA.ToString(Inv), VaterFA?.ToString(Inv) ?? "", SubFA.ToString(Inv), Position?.ToString(Inv) ?? "", Artnr ?? "");

    public static TeilSchluessel? Parse(string? text)
    {
        var t = text?.Split('|', 5);
        if (t is not { Length: 5 } || !int.TryParse(t[0], NumberStyles.None, Inv, out var haupt) || !int.TryParse(t[2], NumberStyles.None, Inv, out var sub))
            return null;
        int? Opt(string s) => int.TryParse(s, NumberStyles.None, Inv, out var v) ? v : null;
        if ((t[1] != "" && Opt(t[1]) == null) || (t[3] != "" && Opt(t[3]) == null)) return null;
        return new TeilSchluessel(haupt, Opt(t[1]), sub, Opt(t[3]), t[4] == "" ? null : t[4]);
    }
}

/// <summary>v1.99.72 (Spec 2026-11-20-bde-liste-isolierungsfraese-pfad-material, Loesung 1): Artikel-Zusatzwerte je Artikelnummer –
/// Sage-Merkmale der Blechteile-Nachlese (logischer Name) und, nur bei <c>FraesenPfadAktiv</c>, der Fraesen-Pfad (<c>Articles.CamPath</c>).</summary>
public sealed record ArtikelZusatz(string? Material, string? FertigungsInfo, string? Arbeitsschritte, string? Blechdicke, string? Pfad);

/// <summary>v1.99.72 (F7, ADR 0038): Herkunft einer Lagerbestell-Zeile. <see cref="Termin"/> = SubmittedAt + N Arbeitstage (ohne SubmittedAt: kein Termin).</summary>
public sealed record LagerPosition(int ItemId, int RequisitionId, int Position, string? Kommentar, string? Werkbank, DateTime? Termin);

/// <summary>Eine Zeile der Teileliste: Blechteil (Blatt) oder Baugruppe (eigene Sub-FA) mit dem Kuerzel der Werkbank – seit v1.99.72
/// auch eine Lagerbestell-Position (<see cref="Lager"/> gesetzt; <see cref="Node"/> ist dann eine nicht gespeicherte Projektion mit HauptFA 0).</summary>
public sealed class TeilelisteZeile
{
    public required FaHierarchyNode Node { get; init; }
    public required TeilSchluessel Schluessel { get; init; }
    public LagerPosition? Lager { get; init; }
    /// <summary>Formularwert: FA-Zeile = <see cref="TeilSchluessel.Text"/>, Lagerbestell-Zeile = <c>LB|&lt;ItemId&gt;</c>.</summary>
    public string FormSchluessel => Lager is { } l ? BdeTeilelisteService.LagerSchluessel(l.ItemId) : Schluessel.Text;
    public bool IstBaugruppe => Node.SubFA != 0;
    /// <summary>F16: Schluessel kommt in der Struktur mehrfach vor → angezeigt, nicht meldbar.</summary>
    public bool Eindeutig { get; init; } = true;
    /// <summary>Antwort 8: zum besitzenden Auftrag gibt es keinen WMS-Auftrag (meldbar, gekennzeichnet).</summary>
    public bool KeinWmsAuftrag { get; init; }
    public BdeTeilMeldung? Meldung { get; init; }
    /// <summary>v1.99.73 (E1-2): direkte Kinder einer Baugruppe als eigene Zeilen (nur Anzeige, dieselben Getter wie die Eltern).</summary>
    public IReadOnlyList<TeilelisteZeile> Kinder { get; init; } = Array.Empty<TeilelisteZeile>();
    /// <summary>v1.99.73 (E1-1): AG-Status je Arbeitsschritt-Token (WorkOperation oder Teilmeldung); ohne Lauf neutral.</summary>
    public IReadOnlyList<FaArbeitsschrittSymbol> Arbeitsgaenge { get; init; } = Array.Empty<FaArbeitsschrittSymbol>();
    /// <summary>v1.99.73 (E1-3): Kinderzeile traf den Spaltenfilter (die Eltern selbst nicht oder ebenfalls).</summary>
    public bool Treffer { get; set; }
    /// <summary>v1.99.73 (E1-3): mindestens ein Kind ist Treffer → Baugruppe aufgeklappt.</summary>
    public bool Aufgeklappt => Kinder.Any(k => k.Treffer);
    /// <summary>v1.99.37 (ADR 0032, Rueckfrage 3): in Sage verschwundene Zeile → angezeigt mit Badge, nicht meldbar.</summary>
    public bool Verschwunden => Node.VerschwundenSeit != null;
    public bool Meldbar => Meldung == null && Eindeutig && !Verschwunden;
}

/// <summary>Sicht „Summiert“: je Matchcode, mit den beitragenden meldbaren Schluesseln (F7).</summary>
public sealed class TeilelisteAggregat
{
    public string Matchcode { get; init; } = string.Empty;
    public decimal SummeSollmenge { get; init; }
    public int Anzahl { get; init; }
    public string? HauptFas { get; init; }
    public string? Abteilungen { get; init; }
    public string? FeTermine { get; init; }
    /// <summary>Formularwerte (<see cref="TeilelisteZeile.FormSchluessel"/>) der offenen, meldbaren Zeilen beider Arten (F10).</summary>
    public IReadOnlyList<string> OffeneSchluessel { get; init; } = Array.Empty<string>();
    public decimal SummeSollmengeOffen { get; init; }
    // v1.99.72 (F2): verschiedene Werte der beitragenden Positionen, sortiert, kommagetrennt (FormatTexte wie Abteilungen).
    public string? Materialien { get; init; }
    public string? Blechdicken { get; init; }
    public string? BemerkungenPn { get; init; }
    public string? FertigungsinfosPn { get; init; }
    /// <summary>Verschiedene Fraesen-Pfade (je Wert ein Baustein <c>_FraesenPfad</c>); Zelltext = kommagetrennt.</summary>
    public IReadOnlyList<string> Pfade { get; init; } = Array.Empty<string>();
}

/// <param name="Waisen">F15: offene Meldungen dieses Kuerzels, deren Schluessel keine Zeile mehr trifft (HauptFA noch in der Struktur).</param>
/// <param name="Zusatz">v1.99.72: Artikel-Zusatzwerte (Blechdicke, Pfad) fuer die Zellen der View – dieselben Getter wie der Filter.</param>
public sealed record TeilelisteDaten(IReadOnlyList<TeilelisteZeile> Zeilen, int Waisen, IReadOnlyDictionary<string, ArtikelZusatz>? Zusatz = null);

/// <summary>
/// v1.99.32 (Spec 2026-11-13-bde-vereinfacht-teileliste-blechfertigung, ADR 0030): BDE-Teileliste der Blechfertigung. Zeile = jeder
/// <see cref="FaHierarchyNode"/>, dessen Arbeitsschritte das Kuerzel der Werkbank tragen (Blatt und Baugruppe). Wiederverwendet
/// <see cref="FaArbeitsschrittSymbole.Tokenize"/>, <see cref="VormontageService"/> (Kopfwerte, Spalten-Getter, Formate) und
/// <see cref="IsoWeekRange"/>. Kein Interface (eine Implementierung). Kein Schreiben nach Sage.
/// </summary>
public class BdeTeilelisteService
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    private readonly IFaHierarchyNodeRepository _nodes;
    private readonly IFaHierarchyOrderInfoRepository _orderInfos;
    private readonly ApplicationDbContext _ctx;
    private readonly IBdeBookingService _booking;
    private readonly ICurrentUserService _userSvc;
    private readonly IAppSettingRepository _settings;
    private readonly IBdeShiftCalendarService _schichten;
    private readonly IArticleRepository _articles;
    private readonly IBusinessDayService _days;
    private readonly IHolidayRepository _holidays;
    private readonly FaAgStatusService? _agStatus;

    public BdeTeilelisteService(IFaHierarchyNodeRepository nodes, IFaHierarchyOrderInfoRepository orderInfos, ApplicationDbContext ctx,
        IBdeBookingService booking, ICurrentUserService userSvc, IAppSettingRepository settings, IBdeShiftCalendarService schichten,
        IArticleRepository articles, IBusinessDayService days, IHolidayRepository holidays, FaAgStatusService? agStatus = null)
    {
        _agStatus = agStatus; // v1.99.73; null (nur Tests ohne Status-Lauf) = Symbole neutral
        _nodes = nodes; _orderInfos = orderInfos; _ctx = ctx; _booking = booking; _userSvc = userSvc; _settings = settings; _schichten = schichten;
        _articles = articles; _days = days; _holidays = holidays;
    }

    /// <summary>F2: ein Token (Leerzeichen-Trennung wie Dienst und FA-Baum) ist das Kuerzel, Gross/Klein egal.</summary>
    public static bool Traegt(FaHierarchyNode n, string kuerzel) => TraegtText(n.Arbeitsschritte, kuerzel);

    /// <summary>v1.99.72 (F7): dieselbe Token-Regel auf den Text des Artikel-Merkmals Arbeitsschritte (Lagerbestell-Zeilen).</summary>
    public static bool TraegtText(string? arbeitsschritte, string kuerzel) =>
        FaArbeitsschrittSymbole.Tokenize(arbeitsschritte).Contains(kuerzel, StringComparer.OrdinalIgnoreCase);

    /// <summary>v1.99.72: Formular-Schluessel einer Lagerbestell-Zeile (der Controller trennt ihn vor <see cref="TeilSchluessel.Parse"/>).</summary>
    public const string LagerPraefix = "LB|";
    public static string LagerSchluessel(int itemId) => LagerPraefix + itemId.ToString(Inv);

    /// <summary>"LB|&lt;ItemId&gt;" → ItemId; alles andere null (dann FA-Schluessel oder ungueltig).</summary>
    public static int? ParseLagerSchluessel(string? text) =>
        text != null && text.StartsWith(LagerPraefix, StringComparison.Ordinal)
        && int.TryParse(text.AsSpan(LagerPraefix.Length), NumberStyles.None, Inv, out var id) && id > 0 ? id : null;

    /// <summary>Besitzender Auftrag (Antwort 8): Baugruppe = eigene Sub-FA, Blechteil = Vater-Sub-FA.</summary>
    public static string? Besitzer(FaHierarchyNode n) => n.SubFA != 0 ? n.SubFA.ToString(Inv) : n.VaterFA?.ToString(Inv);

    /// <summary>
    /// v1.99.73 (E1-1, Antwort 21): zweite Statusquelle nur der Teileliste – nach <see cref="FaArbeitsgangStatus.Zuordnen"/> aufgerufen.
    /// Token mit nicht stornierter Teilmeldung (Schluessel, Kuerzel gross) = erledigt (ODER zur WorkOperation). Token ohne WorkOperation-Status,
    /// dessen Kuerzel eine Teileliste-Werkbank traegt (Teilmeldung moeglich), = offen/ueberfaellig ueber <see cref="ArbeitsgangStatusRegel"/>.
    /// Sonst bleibt der Status aus dem WorkOperation-Lauf („kein BDE-Arbeitsgang“ bzw. neutral).
    /// </summary>
    public static void TeilmeldungenAnwenden(IEnumerable<FaHierarchyStructure> structures, IReadOnlySet<(TeilSchluessel, string)> gemeldet,
        IReadOnlySet<string> teilelisteKuerzel, IReadOnlyDictionary<string, int> offsets, Func<DateTime, int, DateTime> rechner, DateTime heute)
    {
        foreach (var s in structures)
        {
            var bezug = s.OrderInfos.Select(o => o.FE_Termin).Min();
            var offen = new Stack<FaHierarchyTreeNode>(s.Roots);
            while (offen.Count > 0)
            {
                var t = offen.Pop();
                foreach (var c in t.Children) offen.Push(c);
                var schluessel = TeilSchluessel.Von(t.Node);
                for (var i = 0; i < t.Arbeitsgaenge.Count; i++)
                {
                    var sym = t.Arbeitsgaenge[i];
                    var k = sym.Kuerzel.Trim().ToUpperInvariant();
                    var erledigt = gemeldet.Contains((schluessel, k));
                    var moeglich = (sym.Status is null or FaAgStatus.KeinBdeArbeitsgang) && teilelisteKuerzel.Contains(k);
                    if (!erledigt && !moeglich) continue;
                    var soll = sym.Soll ?? ArbeitsgangStatusRegel.Soll(bezug, k, offsets, rechner, out _);
                    t.Arbeitsgaenge[i] = sym with
                    {
                        Soll = soll,
                        Status = erledigt ? FaAgStatus.Erledigt : ArbeitsgangStatusRegel.Ableiten(false, false, soll, heute),
                    };
                }
            }
        }
    }

    /// <summary>v1.99.73: Symbole ohne Status (neutral wie die FA-Struktur bei BDE aus) – Lagerbestell-Zeilen und Aufrufe ohne Status-Lauf.</summary>
    public static IReadOnlyList<FaArbeitsschrittSymbol> NeutraleSymbole(string? arbeitsschritte) =>
        FaArbeitsschrittSymbole.Tokenize(arbeitsschritte).Select(k => new FaArbeitsschrittSymbol(k, null)).ToList();

    /// <summary>Spalten der Sicht „Einzeln“ (Col-Key → Zelltext, Getter = Zelle, fallstricke §26).</summary>
    public static IReadOnlyDictionary<string, Func<FaHierarchyNode, string?>> SpaltenEinzeln(
        IReadOnlyDictionary<int, VormontageService.KopfWerte> kopf, Func<FaHierarchyNode, string?>? beschichtet = null)
    {
        var map = new Dictionary<string, Func<FaHierarchyNode, string?>>(VormontageService.BuildPositionsSpalten(kopf))
        {
            ["matchcode"] = n => n.Matchcode,
            ["sollmenge"] = n => VormontageService.FormatMenge(n.Sollmenge),
            ["material"] = n => n.Material,
            ["artnr"] = n => n.Artnr,
            ["subfa"] = n => n.SubFA == 0 ? null : n.SubFA.ToString(Inv),
            ["vaterfa"] = n => n.VaterFA?.ToString(Inv),
            ["position"] = n => n.Position?.ToString(Inv),
            ["arbeitsschritte"] = n => n.Arbeitsschritte,
            ["bezeichnung"] = n => n.Bezeichnung1,
        };
        if (beschichtet != null) map["beschichtet"] = beschichtet;
        return map;
    }

    /// <summary>v1.99.72: Zeilen-Getter (Zelle = Filter) fuer beide Zeilenarten: FA-Zeilen ueber <see cref="SpaltenEinzeln"/>, Lagerbestell-Zeilen
    /// mit leerem Hauptauftrag/Kunde/Beschichtet, Abteilung = bestellende Werkbank, FE-Termin = Termin (F7); Blechdicke/Pfad aus dem Artikel.</summary>
    public static Dictionary<string, Func<TeilelisteZeile, string?>> SpaltenZeile(IReadOnlyDictionary<int, VormontageService.KopfWerte> kopf,
        Func<FaHierarchyNode, string?>? beschichtet, IReadOnlyDictionary<string, ArtikelZusatz>? zusatz)
    {
        var map = SpaltenEinzeln(kopf, beschichtet).ToDictionary(e => e.Key, e => (Func<TeilelisteZeile, string?>)(z => e.Value(z.Node)));
        void Lager(string key, Func<LagerPosition, string?> g)
        {
            if (!map.TryGetValue(key, out var fa)) return;
            map[key] = z => z.Lager is { } l ? g(l) : fa(z);
        }
        Lager("hauptfa", _ => null);
        Lager("kunde", _ => null);
        Lager("beschichtet", _ => null);
        Lager("montage-abteilung", l => l.Werkbank);
        Lager("fe-termin", l => VormontageService.FormatDaten(new[] { l.Termin }));
        map["blechdicke"] = z => Zusatz(zusatz, z.Node.Artnr)?.Blechdicke;
        map["fraesen-pfad"] = z => Zusatz(zusatz, z.Node.Artnr)?.Pfad;
        // v1.99.73 (E1-1): Zelle zeigt Zeichen + Kuerzel, der Filter sieht den Screenreader-Text („KA – Kanten – erledigt“).
        map["ag-status"] = z => z.Arbeitsgaenge.Count == 0 ? null : string.Join(", ", z.Arbeitsgaenge.Select(a => a.AriaLabel));
        return map;
    }

    public static ArtikelZusatz? Zusatz(IReadOnlyDictionary<string, ArtikelZusatz>? zusatz, string? artnr) =>
        zusatz != null && !string.IsNullOrWhiteSpace(artnr) && zusatz.TryGetValue(artnr.Trim(), out var z) ? z : null;

    /// <summary>Summiert: Filter auf der Aggregat-Projektion; alle anderen Spalten filtern je Position VOR dem Zusammenzaehlen.</summary>
    public static readonly IReadOnlyDictionary<string, Func<TeilelisteAggregat, string?>> SpaltenSummiert =
        new Dictionary<string, Func<TeilelisteAggregat, string?>>
        {
            ["matchcode"] = a => a.Matchcode,
            ["sollmenge"] = a => VormontageService.FormatMenge(a.SummeSollmenge),
            ["anzahl"] = a => a.Anzahl.ToString(Inv),
        };

    /// <summary>Laedt Struktur, Kopfwerte, offene Meldungen des Kuerzels und Auftragsstatus; Rest in <see cref="Bauen"/>.</summary>
    /// <param name="mitPfad">v1.99.72: <c>FraesenPfadAktiv</c> (sonst bleibt der Pfad leer, F15).</param>
    /// <param name="mitLagerbestellungen">v1.99.72 (F6): Werkbank-Option wirksam und <c>LagerbestellungAktiv</c>.</param>
    public async Task<TeilelisteDaten> LadenAsync(string kuerzel, (DateTime StartInclusive, DateTime EndExclusive)? kw, bool gemeldeteZeigen,
        IReadOnlyDictionary<string, string>? colf, Func<FaHierarchyNode, string?>? beschichtet = null, bool mitPfad = false,
        bool mitLagerbestellungen = false)
    {
        var nodes = await _nodes.GetAllAsync();
        var infos = await _orderInfos.GetAllAsync();
        var hauptFas = nodes.Select(n => n.HauptFA).Distinct().ToList();
        // v1.99.72 (ADR 0038): FA-Lesewege filtern Lagerbestell-Meldungen ausdruecklich aus (nicht auf „HauptFA 0 kommt nie vor“ verlassen).
        var meldungen = await _ctx.BdeTeilMeldungen.AsNoTracking().Include(m => m.BdeOperator)
            .Where(m => !m.IsCancelled && m.WarehouseRequisitionItemId == null && m.Kuerzel == kuerzel && hauptFas.Contains(m.HauptFA)).ToListAsync();
        var besitzer = nodes.Where(n => Traegt(n, kuerzel)).Select(Besitzer).OfType<string>().Distinct().ToList();
        var zusatz = await ZusatzAsync(mitPfad);
        var lager = mitLagerbestellungen ? await LagerZeilenAsync(kuerzel, zusatz) : null;
        var agStatus = await AgStatusAsync(nodes, infos, kuerzel, hauptFas);
        return Bauen(nodes, infos, kuerzel, meldungen, await AuftragOffenAsync(besitzer), kw, gemeldeteZeigen, colf, beschichtet, zusatz, lager, agStatus);
    }

    /// <summary>
    /// v1.99.73 (E1-1): AG-Status je Knoten der Haupt-FAs mit Zeilen dieses Kuerzels. Je Haupt-FA eine flache Struktur aller Knoten (die
    /// VaterFA-Zuordnung von <see cref="FaArbeitsgangStatus.Zuordnen"/> braucht alle Sub-FAs), derselbe Lauf wie die FA-Struktur
    /// (<see cref="FaAgStatusService"/>), danach die Teilmeldungen ALLER Kuerzel (<see cref="TeilmeldungenAnwenden"/>). Vor dem Filter geladen,
    /// damit die Spalte AG-Status serverseitig filterbar ist.
    /// </summary>
    private async Task<Dictionary<FaHierarchyNode, IReadOnlyList<FaArbeitsschrittSymbol>>?> AgStatusAsync(IReadOnlyCollection<FaHierarchyNode> nodes,
        IEnumerable<FaHierarchyOrderInfo> infos, string kuerzel, List<int> hauptFas)
    {
        if (_agStatus == null) return null;
        var relevant = nodes.Where(n => Traegt(n, kuerzel)).Select(n => n.HauptFA).ToHashSet();
        var infoJeFa = infos.ToLookup(o => o.HauptFA);
        var structures = nodes.Where(n => relevant.Contains(n.HauptFA)).GroupBy(n => n.HauptFA)
            .Select(g => new FaHierarchyStructure
            {
                HauptFA = g.Key, OrderInfos = infoJeFa[g.Key].ToList(),
                Roots = g.Select(n => new FaHierarchyTreeNode { Node = n }).ToList(),
            }).ToList();
        var lauf = await _agStatus.ZuordnenAsync(structures);
        if (lauf != null)
        {
            var gemeldet = (await _ctx.BdeTeilMeldungen.AsNoTracking()
                    .Where(m => !m.IsCancelled && m.WarehouseRequisitionItemId == null && hauptFas.Contains(m.HauptFA)).ToListAsync())
                .Select(m => (TeilSchluessel.Von(m), m.Kuerzel.Trim().ToUpperInvariant())).ToHashSet();
            var teileliste = lauf.Werkbaenke.Where(w => WerkbankKuerzelFamilie.TeilelisteWirksam(w, false))
                .Select(w => WerkbankKuerzelFamilie.Kuerzel(w)!.ToUpperInvariant()).ToHashSet();
            TeilmeldungenAnwenden(structures, gemeldet, teileliste, lauf.Offsets, lauf.Rechner, DateTime.Today);
        }
        return structures.SelectMany(s => s.Roots)
            .ToDictionary(t => t.Node, t => (IReadOnlyList<FaArbeitsschrittSymbol>)t.Arbeitsgaenge, ReferenceEqualityComparer.Instance as IEqualityComparer<FaHierarchyNode>);
    }

    /// <summary>v1.99.72 (Loesung 1): eine Abfrage je Quelle, ohne Artikelnummer-Liste (keine 2100-Parameter-Grenze). Schluessel getrimmt,
    /// Gross/Klein egal (B18).</summary>
    public async Task<Dictionary<string, ArtikelZusatz>> ZusatzAsync(bool mitPfad)
    {
        string[] felder = { BfArtikelFelder.Material, BfArtikelFelder.FertigungsInfoPN, BfArtikelFelder.Arbeitsschritte, BfArtikelFelder.Blechdicke };
        var werte = await _ctx.ArticleAttributeValues.AsNoTracking()
            .Where(v => v.ArticleAttributeDefinition.SyncSource == "Sage" && felder.Contains(v.ArticleAttributeDefinition.SyncFieldName!)
                        && v.TextValue != null && v.TextValue != "")
            .Select(v => new { v.Article.ArticleNumber, v.ArticleAttributeDefinition.SyncFieldName, v.TextValue })
            .ToListAsync();
        var pfade = mitPfad ? await _articles.GetFraesenPfadeAsync() : new Dictionary<string, string>();
        var ergebnis = new Dictionary<string, ArtikelZusatz>(StringComparer.OrdinalIgnoreCase);
        foreach (var g in werte.GroupBy(w => w.ArticleNumber.Trim(), StringComparer.OrdinalIgnoreCase))
        {
            string? F(string feld) => g.FirstOrDefault(w => w.SyncFieldName == feld)?.TextValue?.Trim() is { Length: > 0 } t ? t : null;
            ergebnis[g.Key] = new ArtikelZusatz(F(BfArtikelFelder.Material), F(BfArtikelFelder.FertigungsInfoPN), F(BfArtikelFelder.Arbeitsschritte),
                F(BfArtikelFelder.Blechdicke), pfade.GetValueOrDefault(g.Key));
        }
        foreach (var (nr, pfad) in pfade)
            ergebnis.TryAdd(nr, new ArtikelZusatz(null, null, null, null, pfad));
        return ergebnis;
    }

    /// <summary>
    /// v1.99.72 (F7, ADR 0038): Positionen abgeschickter/teilgelieferter Lagerbestellungen, deren Artikel das Kuerzel im Merkmal Arbeitsschritte
    /// traegt, als Teileliste-Zeilen (Projektion, nicht gespeichert). Termin = <c>AddBusinessDays(SubmittedAt.Date, N, Feiertage)</c>.
    /// </summary>
    public async Task<List<TeilelisteZeile>> LagerZeilenAsync(string kuerzel, IReadOnlyDictionary<string, ArtikelZusatz> zusatz)
    {
        var items = await _ctx.WarehouseRequisitionItems.AsNoTracking()
            .Where(i => i.WarehouseRequisition.Status == WarehouseRequisitionStatus.Submitted
                        || i.WarehouseRequisition.Status == WarehouseRequisitionStatus.PartiallyDelivered)
            .Select(i => new
            {
                i.Id, i.WarehouseRequisitionId, i.Position, i.ArticleNumber, i.ArticleDescription, i.QuantityRequested,
                i.WarehouseRequisition.Comment, i.WarehouseRequisition.SubmittedAt, Werkbank = i.WarehouseRequisition.ProductionWorkplace.Name,
                Matchcode = _ctx.Articles.Where(a => a.ArticleNumber == i.ArticleNumber).Select(a => a.Matchcode).FirstOrDefault(),
            })
            .ToListAsync();
        var treffer = items.Where(i => TraegtText(Zusatz(zusatz, i.ArticleNumber)?.Arbeitsschritte, kuerzel)).ToList();
        if (treffer.Count == 0) return new List<TeilelisteZeile>();

        var meldungen = await _ctx.BdeTeilMeldungen.AsNoTracking().Include(m => m.BdeOperator)
            .Where(m => !m.IsCancelled && m.WarehouseRequisitionItemId != null && m.Kuerzel == kuerzel).ToListAsync();
        var tage = await _settings.GetIntValueAsync(AppSettingKeys.BdeTeilelisteLagerbestellungArbeitstage, 10);
        var feiertage = await _holidays.GetHolidayDatesAsync() ?? new HashSet<DateTime>();
        return treffer.Select(i =>
        {
            var z = Zusatz(zusatz, i.ArticleNumber);
            var node = new FaHierarchyNode
            {
                HauptFA = 0, SubFA = 0, Artnr = i.ArticleNumber, Matchcode = i.Matchcode ?? i.ArticleNumber, Sollmenge = i.QuantityRequested,
                Material = z?.Material, FertigungsInfoPN = z?.FertigungsInfo, Arbeitsschritte = z?.Arbeitsschritte, Bezeichnung1 = i.ArticleDescription,
            };
            return new TeilelisteZeile
            {
                Node = node, Schluessel = TeilSchluessel.Von(node),
                Lager = new LagerPosition(i.Id, i.WarehouseRequisitionId, i.Position, i.Comment, i.Werkbank,
                    i.SubmittedAt is DateTime s ? _days.AddBusinessDays(s.Date, tage, feiertage) : null),
                Meldung = meldungen.FirstOrDefault(m => m.WarehouseRequisitionItemId == i.Id),
                Arbeitsgaenge = NeutraleSymbole(node.Arbeitsschritte),
            };
        }).ToList();
    }

    /// <summary>Besitzer-Nummer → mindestens ein WMS-Auftrag weder erledigt noch storniert. Fehlt die Nummer = kein WMS-Auftrag.
    /// Haupt-FA-Auftrag: SubOrderNumber leer oder gleich OrderNumber (<see cref="ProductionOrder.IsHauptFa"/>-Invariante).</summary>
    public async Task<IReadOnlyDictionary<string, bool>> AuftragOffenAsync(IReadOnlyCollection<string> nummern)
    {
        var liste = nummern.ToList();
        var auftraege = await _ctx.ProductionOrders.AsNoTracking()
            .Where(o => liste.Contains(o.SubOrderNumber) || (liste.Contains(o.OrderNumber) && (o.SubOrderNumber == "" || o.SubOrderNumber == o.OrderNumber)))
            .Select(o => new { Nr = o.SubOrderNumber == "" ? o.OrderNumber : o.SubOrderNumber, Offen = !o.IsDone && !o.IsCancelled })
            .ToListAsync();
        return auftraege.GroupBy(a => a.Nr).ToDictionary(g => g.Key, g => g.Any(a => a.Offen));
    }

    /// <summary>Reine Zeilenbildung (DB-frei, testbar): Kuerzel → Sichtbarkeit Auftrag → KW → gemeldete → Spaltenfilter.</summary>
    public static TeilelisteDaten Bauen(IReadOnlyCollection<FaHierarchyNode> nodes, IEnumerable<FaHierarchyOrderInfo> infos, string kuerzel,
        IReadOnlyCollection<BdeTeilMeldung> meldungen, IReadOnlyDictionary<string, bool> auftragOffen,
        (DateTime StartInclusive, DateTime EndExclusive)? kw, bool gemeldeteZeigen, IReadOnlyDictionary<string, string>? colf,
        Func<FaHierarchyNode, string?>? beschichtet = null, IReadOnlyDictionary<string, ArtikelZusatz>? zusatz = null,
        IReadOnlyList<TeilelisteZeile>? lager = null,
        IReadOnlyDictionary<FaHierarchyNode, IReadOnlyList<FaArbeitsschrittSymbol>>? agStatus = null)
    {
        IReadOnlyList<FaArbeitsschrittSymbol> Symbole(FaHierarchyNode n) => agStatus?.GetValueOrDefault(n) ?? NeutraleSymbole(n.Arbeitsschritte);
        var infoListe = infos.ToList();
        var kopf = VormontageService.BuildKopfLookup(infoListe);
        var doppelt = nodes.GroupBy(TeilSchluessel.Von).Where(g => g.Count() > 1).Select(g => g.Key).ToHashSet();
        var kinder = nodes.Where(n => n.VaterFA.HasValue).ToLookup(n => n.VaterFA!.Value);
        meldungen = meldungen.Where(m => m.WarehouseRequisitionItemId == null).ToList(); // v1.99.72: nur FA-Meldungen (ADR 0038)
        var meldungJeSchluessel = meldungen.GroupBy(TeilSchluessel.Von).ToDictionary(g => g.Key, g => g.First());
        var kandidaten = nodes.Where(n => Traegt(n, kuerzel)).ToList();

        var treffer = kandidaten.Select(TeilSchluessel.Von).ToHashSet();
        var hauptFas = nodes.Select(n => n.HauptFA).ToHashSet();
        var waisen = meldungen.Count(m => hauptFas.Contains(m.HauptFA) && !treffer.Contains(TeilSchluessel.Von(m)));

        IEnumerable<TeilelisteZeile> zeilen = kandidaten
            .Where(n => Besitzer(n) is not string b || !auftragOffen.TryGetValue(b, out var offen) || offen)
            .Select(n =>
            {
                var s = TeilSchluessel.Von(n);
                return new TeilelisteZeile
                {
                    Node = n, Schluessel = s, Eindeutig = !doppelt.Contains(s),
                    KeinWmsAuftrag = Besitzer(n) is not string b || !auftragOffen.ContainsKey(b),
                    Meldung = meldungJeSchluessel.GetValueOrDefault(s),
                    Arbeitsgaenge = Symbole(n),
                    Kinder = n.SubFA != 0
                        ? kinder[n.SubFA].OrderBy(k => k.Position)
                            .Select(k => new TeilelisteZeile { Node = k, Schluessel = TeilSchluessel.Von(k), Arbeitsgaenge = Symbole(k) }).ToList()
                        : Array.Empty<TeilelisteZeile>(),
                };
            })
            .Concat(lager ?? Array.Empty<TeilelisteZeile>());

        if (kw is not null)
        {
            var inKw = IsoWeekRange.HauptFasInRange(infoListe, o => o.FE_Termin, kw.Value);
            var (von, bis) = kw.Value;
            // F7: Lagerbestell-Zeilen ueber ihren Termin; ohne Termin fallen sie weg (wie ein Auftragsteil ohne FE-Termin).
            zeilen = zeilen.Where(z => z.Lager is { } l ? l.Termin >= von && l.Termin < bis : inKw.Contains(z.Node.HauptFA));
        }
        if (!gemeldeteZeigen) zeilen = zeilen.Where(z => z.Meldung == null);

        // E1-3: Eltern bleiben, wenn sie selbst ODER ein Kind passt. Kind-Treffer nur, wenn die Eltern selbst nicht passen (sonst
        // waere z. B. ein Haupt-FA-Filter an jedem Kind ein „Treffer“); Pagination zaehlt weiter nur Elternzeilen.
        var typen = ColumnFilterTypes.For("BdeTeileliste/Index");
        if (ColumnFilterHelper.HasAny(colf, typen))
        {
            var liste = zeilen.ToList();
            var passt = ColumnFilterHelper.Apply(liste.Concat(liste.SelectMany(z => z.Kinder)), colf, SpaltenZeile(kopf, beschichtet, zusatz), typen)
                .ToHashSet(ReferenceEqualityComparer.Instance);
            foreach (var z in liste.Where(z => !passt.Contains(z)))
                foreach (var k in z.Kinder) k.Treffer = passt.Contains(k);
            zeilen = liste.Where(z => passt.Contains(z) || z.Aufgeklappt);
        }

        return new TeilelisteDaten(zeilen
            .OrderBy(z => z.Node.Matchcode, StringComparer.OrdinalIgnoreCase).ThenBy(z => z.Node.HauptFA)
            .ThenBy(z => z.Node.VaterFA).ThenBy(z => z.Node.Position).ThenBy(z => z.Lager?.ItemId).ToList(), waisen, zusatz);
    }

    /// <summary>F7: je Matchcode (getrimmt, ordinal wie Vormontage). Die Zeilen sind schon je Position gefiltert (Einzeln-Spalten);
    /// hier wirken nur Matchcode, Summe und Anzahl auf der Aggregat-Projektion.</summary>
    public static List<TeilelisteAggregat> Summieren(IEnumerable<TeilelisteZeile> zeilen, IEnumerable<FaHierarchyOrderInfo> infos,
        IReadOnlyDictionary<string, string>? colf, IReadOnlyDictionary<string, ArtikelZusatz>? zusatz = null)
    {
        var infosJeHauptFa = infos.GroupBy(o => o.HauptFA).ToDictionary(g => g.Key, g => g.ToList());
        var kopf = infosJeHauptFa.ToDictionary(e => e.Key, e => VormontageService.BuildKopfWerte(e.Value));
        var aggregate = zeilen
            .GroupBy(z => (z.Node.Matchcode ?? string.Empty).Trim(), StringComparer.Ordinal)
            .Select(g =>
            {
                var hauptFas = g.Where(z => z.Lager == null).Select(z => z.Node.HauptFA).Distinct().OrderBy(h => h).ToList();
                var lager = g.Select(z => z.Lager).OfType<LagerPosition>().ToList();
                var offen = g.Where(z => z.Meldbar).ToList();
                var zus = g.Select(z => Zusatz(zusatz, z.Node.Artnr)).ToList();
                return new TeilelisteAggregat
                {
                    Matchcode = g.Key,
                    SummeSollmenge = g.Sum(z => z.Node.Sollmenge),
                    Anzahl = g.Count(),
                    // F10: Lagerbestellungen zusaetzlich als „LB <Nr>“
                    HauptFas = string.Join(", ", hauptFas.Select(h => h.ToString(Inv))
                        .Concat(lager.Select(l => l.RequisitionId).Distinct().OrderBy(n => n).Select(n => "LB " + n.ToString(Inv)))),
                    Abteilungen = VormontageService.FormatTexte(hauptFas.Select(h => kopf.TryGetValue(h, out var k) ? k.Abteilung : null)
                        .Concat(lager.Select(l => l.Werkbank))),
                    FeTermine = VormontageService.FormatDaten(hauptFas.SelectMany(h => infosJeHauptFa.TryGetValue(h, out var l) ? l : new List<FaHierarchyOrderInfo>())
                        .Select(i => i.FE_Termin).Concat(lager.Select(l => l.Termin))),
                    OffeneSchluessel = offen.Select(z => z.FormSchluessel).ToList(),
                    SummeSollmengeOffen = offen.Sum(z => z.Node.Sollmenge),
                    Materialien = VormontageService.FormatTexte(g.Select(z => z.Node.Material)),
                    Blechdicken = VormontageService.FormatTexte(zus.Select(z => z?.Blechdicke)),
                    BemerkungenPn = VormontageService.FormatTexte(g.Select(z => z.Node.BemerkungPN)),
                    FertigungsinfosPn = VormontageService.FormatTexte(g.Select(z => z.Node.FertigungsInfoPN)),
                    Pfade = zus.Select(z => z?.Pfad).OfType<string>().Distinct(StringComparer.OrdinalIgnoreCase)
                        .OrderBy(p => p, StringComparer.OrdinalIgnoreCase).ToList(),
                };
            })
            .OrderBy(a => a.Matchcode, StringComparer.OrdinalIgnoreCase);
        return ColumnFilterHelper.Apply(aggregate, colf, SpaltenSummiert, ColumnFilterTypes.For("BdeTeileliste/Summiert")).ToList();
    }

    // ---------------------------------------------------------------- Melden (E3)

    /// <summary>Gut/Ausschuss null = volle Sollmenge, Ausschuss 0 (Block und Summiert, F9). v1.99.72: <paramref name="LagerItemId"/> gesetzt =
    /// Lagerbestell-Position (dann ist <paramref name="Schluessel"/> null).</summary>
    public sealed record MeldeAuftrag(TeilSchluessel? Schluessel, decimal? Gut, decimal? Ausschuss, int? LagerItemId = null);

    /// <summary>Sammelmeldung (F9): nie still — jede nicht gemeldete Zeile mit Grund.</summary>
    public sealed class MeldeErgebnis
    {
        public int Gemeldet { get; set; }
        public List<string> BereitsGemeldet { get; } = new();
        public List<string> Abgewiesen { get; } = new();
        public List<string> Hinweise { get; } = new();
        public bool AllesGut => BereitsGemeldet.Count == 0 && Abgewiesen.Count == 0 && Hinweise.Count == 0;

        public string Text()
        {
            var teile = new List<string> { $"Gemeldet: {Gemeldet}." };
            if (BereitsGemeldet.Count > 0) teile.Add($"Bereits gemeldet: {BereitsGemeldet.Count} – nichts geändert ({string.Join("; ", BereitsGemeldet)}).");
            if (Abgewiesen.Count > 0) teile.Add($"Abgewiesen: {Abgewiesen.Count} ({string.Join("; ", Abgewiesen)}).");
            if (Hinweise.Count > 0) teile.Add($"Hinweis: {string.Join("; ", Hinweise)}.");
            return string.Join(" ", teile);
        }
    }

    /// <summary>
    /// F8–F11 (ADR 0026/0030): je Zeile eigene Transaktion mit Anwendungssperre auf den Schluessel, Pruefung gegen den AKTUELLEN Struktur-Stand
    /// (nicht gegen die Seite), Teilmeldung speichern (gefilterter UNIQUE = letzte Wache), bei Baugruppe den eigenen Arbeitsgang
    /// (Sub-FA, OperationNumber = Kuerzel) ueber den Kern von <see cref="IBdeBookingService.ReportCompletionAsync"/> fertig melden.
    /// </summary>
    public async Task<MeldeErgebnis> MeldenAsync(int operatorId, int workplaceId, int terminalId, IReadOnlyList<MeldeAuftrag> auftraege)
    {
        var ergebnis = new MeldeErgebnis();
        var wp = await _ctx.ProductionWorkplaces.FindAsync(workplaceId);
        var nurFa = (await _settings.GetValueAsync(AppSettingKeys.BdeNurFaMeldung))?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;
        if (wp == null || !WerkbankKuerzelFamilie.TeilelisteWirksam(wp, nurFa))
        {
            ergebnis.Abgewiesen.Add("Werkbank meldet nicht über die BDE-Teileliste");
            return ergebnis;
        }
        if (!await _ctx.BdeOperators.AnyAsync(o => o.Id == operatorId && o.IsActive))
        {
            ergebnis.Abgewiesen.Add("Werker nicht angemeldet oder inaktiv");
            return ergebnis;
        }
        var kuerzel = WerkbankKuerzelFamilie.Kuerzel(wp)!;
        var lagerWirksam = wp.BdeTeilelisteLagerbestellungen && await LagerbestellungAktivAsync();
        foreach (var a in auftraege.DistinctBy(a => (a.Schluessel, a.LagerItemId)))
            await InTransaktionAsync(() => a.LagerItemId is int itemId
                ? MeldeLagerZeileAsync(itemId, a, kuerzel, lagerWirksam, operatorId, workplaceId, terminalId, ergebnis)
                : MeldeZeileAsync(a, kuerzel, operatorId, workplaceId, terminalId, ergebnis));
        return ergebnis;
    }

    /// <summary>Default-Semantik wie <c>RequireLagerbestellungAktivFilter</c>: nur "false" sperrt.</summary>
    private async Task<bool> LagerbestellungAktivAsync() =>
        !string.Equals(await _settings.GetValueAsync(AppSettingKeys.LagerbestellungAktiv), "false", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// v1.99.72 (F8, ADR 0038): Lagerbestell-Position fertig melden – wie ein Blechteil, ohne Lagerbuchung, ohne Statusaenderung der Bestellung,
    /// ohne Arbeitsgang, nichts an Sage. Pruefung am Schreibort gegen den aktuellen Stand; App-Guard + gefilterter UNIQUE
    /// <c>UX_BdeTeilMeldungen_Lagerbestellung</c> als letzte Wache.
    /// </summary>
    private async Task MeldeLagerZeileAsync(int itemId, MeldeAuftrag a, string kuerzel, bool lagerWirksam, int operatorId, int workplaceId,
        int terminalId, MeldeErgebnis e)
    {
        var bezeichnung = $"Lagerbestell-Position {itemId.ToString(Inv)}";
        if (!lagerWirksam)
        {
            e.Abgewiesen.Add($"{bezeichnung}: Lagerbestellungen sind in der Teileliste dieser Werkbank nicht aktiv");
            return;
        }
        if (!await BdeBookingService.AnwendungssperreAsync(_ctx, LagerSperre(itemId, kuerzel)))
        {
            e.Abgewiesen.Add($"{bezeichnung}: wird gerade an einem anderen Terminal gemeldet – bitte erneut versuchen");
            return;
        }
        var item = await _ctx.WarehouseRequisitionItems.AsNoTracking().Where(i => i.Id == itemId)
            .Select(i => new
            {
                i.ArticleNumber, i.QuantityRequested, i.Position, i.WarehouseRequisitionId, i.WarehouseRequisition.Status,
                Matchcode = _ctx.Articles.Where(x => x.ArticleNumber == i.ArticleNumber).Select(x => x.Matchcode).FirstOrDefault(),
            })
            .FirstOrDefaultAsync();
        if (item == null)
        {
            e.Abgewiesen.Add($"{bezeichnung}: nicht mehr vorhanden");
            return;
        }
        bezeichnung = $"{item.Matchcode ?? item.ArticleNumber} (Lagerbestellung #{item.WarehouseRequisitionId.ToString(Inv)} Pos. {item.Position.ToString(Inv)})";
        if (item.Status is not (WarehouseRequisitionStatus.Submitted or WarehouseRequisitionStatus.PartiallyDelivered))
        {
            e.Abgewiesen.Add($"{bezeichnung}: Lagerbestellung ist nicht mehr abgeschickt oder teilgeliefert");
            return;
        }
        var nr = item.ArticleNumber.Trim();
        var arbeitsschritte = await _ctx.ArticleAttributeValues.AsNoTracking()
            .Where(v => v.Article.ArticleNumber == nr && v.ArticleAttributeDefinition.SyncSource == "Sage"
                        && v.ArticleAttributeDefinition.SyncFieldName == BfArtikelFelder.Arbeitsschritte)
            .Select(v => v.TextValue).FirstOrDefaultAsync();
        if (!TraegtText(arbeitsschritte, kuerzel))
        {
            e.Abgewiesen.Add($"{bezeichnung}: Artikel trägt das Kürzel {kuerzel} nicht (mehr)");
            return;
        }
        var gut = a.Gut ?? item.QuantityRequested;
        var ausschuss = a.Ausschuss ?? 0;
        if (!MengenOk(gut, ausschuss, bezeichnung, e)) return;
        if (await VorhandeneLagerMeldungAsync(itemId, kuerzel) is { } vorhanden)
        {
            e.BereitsGemeldet.Add(BereitsText(bezeichnung, vorhanden));
            return;
        }
        var meldung = new BdeTeilMeldung
        {
            HauptFA = 0, VaterFA = null, SubFA = 0, Position = null, Artnr = item.ArticleNumber, Matchcode = item.Matchcode,
            WarehouseRequisitionItemId = itemId,
            Kuerzel = kuerzel, ProductionWorkplaceId = workplaceId, BdeOperatorId = operatorId, BdeTerminalId = terminalId,
            GoodQuantity = gut, ScrapQuantity = ausschuss, SollmengeBeiMeldung = item.QuantityRequested, GemeldetAm = DateTime.Now,
            CreatedAt = DateTime.Now, CreatedBy = _userSvc.GetDisplayName(), CreatedByWindows = _userSvc.GetWindowsUserName(),
        };
        if (await SpeichernAsync(meldung, bezeichnung, e, () => VorhandeneLagerMeldungAsync(itemId, kuerzel))) e.Gemeldet++;
    }

    private static bool MengenOk(decimal gut, decimal ausschuss, string bezeichnung, MeldeErgebnis e)
    {
        if (gut < 0 || ausschuss < 0)
        {
            e.Abgewiesen.Add($"{bezeichnung}: Mengen dürfen nicht negativ sein");
            return false;
        }
        if (gut + ausschuss == 0)
        {
            e.Abgewiesen.Add($"{bezeichnung}: bitte eine Gut- oder Ausschussmenge angeben");
            return false;
        }
        return true;
    }

    /// <summary>Insert; DB-Gleichstand (ADR 0026): der gefilterte UNIQUE weist die zweite Meldung ab → „bereits gemeldet“ statt Fehler.</summary>
    private async Task<bool> SpeichernAsync(BdeTeilMeldung meldung, string bezeichnung, MeldeErgebnis e, Func<Task<BdeTeilMeldung?>> gewinnerLaden)
    {
        _ctx.BdeTeilMeldungen.Add(meldung);
        try
        {
            await _ctx.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException ex) when (Nummernkreis.IstSqlFehler(ex, 2601, 2627))
        {
            _ctx.Entry(meldung).State = EntityState.Detached;
            var gewinner = await gewinnerLaden();
            e.BereitsGemeldet.Add(gewinner != null ? BereitsText(bezeichnung, gewinner) : $"{bezeichnung}: bereits gemeldet");
            return false;
        }
    }

    private async Task MeldeZeileAsync(MeldeAuftrag a, string kuerzel, int operatorId, int workplaceId, int terminalId, MeldeErgebnis e)
    {
        var s = a.Schluessel!;
        var bezeichnung = $"Haupt-FA {s.HauptFA} Pos. {s.Position?.ToString(Inv) ?? "–"}";
        if (!await BdeBookingService.AnwendungssperreAsync(_ctx, Sperre(s, kuerzel)))
        {
            e.Abgewiesen.Add($"{bezeichnung}: wird gerade an einem anderen Terminal gemeldet – bitte erneut versuchen");
            return;
        }

        var treffer = (await _ctx.FaHierarchyNodes.AsNoTracking()
                .Where(n => n.HauptFA == s.HauptFA && n.SubFA == s.SubFA && n.VaterFA == s.VaterFA && n.Position == s.Position).ToListAsync())
            .Where(n => TeilSchluessel.Von(n) == s).ToList();
        if (treffer.Count == 0 || !Traegt(treffer[0], kuerzel))
        {
            e.Abgewiesen.Add($"{bezeichnung}: Zeile mit Kürzel {kuerzel} nicht mehr in der FA-Struktur");
            return;
        }
        if (treffer.Count > 1)
        {
            e.Abgewiesen.Add($"{bezeichnung}: Strukturzeile nicht eindeutig – bitte AV");
            return;
        }
        var node = treffer[0];
        bezeichnung = $"{node.Matchcode} ({bezeichnung})";
        if (node.VerschwundenSeit != null) // v1.99.37 (ADR 0032, Rueckfrage 3): verschwundene Zeilen sind sichtbar, aber nicht meldbar
        {
            e.Abgewiesen.Add($"{bezeichnung}: FA {FaStrukturGrund.Text(node.VerschwundenGrund) ?? "in Sage nicht gelesen"} – nicht meldbar");
            return;
        }

        var besitzer = Besitzer(node);
        if (besitzer != null)
        {
            var auftraege = await _ctx.ProductionOrders.AsNoTracking()
                .Where(o => o.SubOrderNumber == besitzer || (o.OrderNumber == besitzer && (o.SubOrderNumber == "" || o.SubOrderNumber == o.OrderNumber)))
                .Select(o => new { o.OrderNumber, o.IsDone, o.IsCancelled, SageStatus = o.ExtraInfo != null ? o.ExtraInfo.SageStatus : null })
                .ToListAsync();
            if (auftraege.FirstOrDefault(o => FaZusatzinfoStatus.IstVerpacktOderAbgeholt(o.SageStatus)) is { } verpackt)
            {
                e.Abgewiesen.Add($"{bezeichnung}: FA {verpackt.OrderNumber} ist bereits {verpackt.SageStatus!.Trim()}");
                return;
            }
            if (auftraege.Count > 0 && auftraege.All(o => o.IsDone || o.IsCancelled))
            {
                e.Abgewiesen.Add($"{bezeichnung}: Auftrag {besitzer} ist erledigt oder storniert");
                return;
            }
        }

        var gut = a.Gut ?? node.Sollmenge;
        var ausschuss = a.Ausschuss ?? 0;
        if (!MengenOk(gut, ausschuss, bezeichnung, e)) return;

        if (await VorhandeneMeldungAsync(s, kuerzel) is { } vorhanden)
        {
            e.BereitsGemeldet.Add(BereitsText(bezeichnung, vorhanden));
            return;
        }

        var meldung = new BdeTeilMeldung
        {
            HauptFA = s.HauptFA, VaterFA = s.VaterFA, SubFA = s.SubFA, Position = s.Position, Artnr = s.Artnr, Matchcode = node.Matchcode,
            Kuerzel = kuerzel, ProductionWorkplaceId = workplaceId, BdeOperatorId = operatorId, BdeTerminalId = terminalId,
            GoodQuantity = gut, ScrapQuantity = ausschuss, SollmengeBeiMeldung = node.Sollmenge, GemeldetAm = DateTime.Now,
            CreatedAt = DateTime.Now, CreatedBy = _userSvc.GetDisplayName(), CreatedByWindows = _userSvc.GetWindowsUserName(),
        };
        if (!await SpeichernAsync(meldung, bezeichnung, e, () => VorhandeneMeldungAsync(s, kuerzel))) return;
        e.Gemeldet++;

        if (node.SubFA == 0) return; // Blechteil: nur Teilmeldung (F10)

        // Baugruppe: eigener Arbeitsgang der Sub-FA mit OperationNumber = Kuerzel (F10). Vater-Sub-FA und Haupt-FA bleiben unberuehrt.
        var subFa = node.SubFA.ToString(Inv);
        // OperationNumber = Token aus Arbeitsschritte (Dienst), Gross/Klein wie dort egal.
        var kuerzelGross = kuerzel.ToUpperInvariant();
        var agId = await _ctx.WorkOperations.Where(w => w.OperationNumber.ToUpper() == kuerzelGross && w.ProductionOrder.SubOrderNumber == subFa)
            .Select(w => (int?)w.Id).FirstOrDefaultAsync();
        if (agId == null)
        {
            e.Hinweise.Add($"Arbeitsgang {kuerzel} an Sub-FA {subFa} nicht vorhanden – nur die Teilmeldung gespeichert");
            return;
        }
        var r = await _booking.ReportCompletionCoreAsync(operatorId, agId.Value, workplaceId, terminalId, gut, ausschuss);
        if (r.Outcome == BdeBookingOutcome.Success)
        {
            meldung.BdeBookingId = r.Booking!.Id;
            await _ctx.SaveChangesAsync();
        }
        else
        {
            var grund = r.Message ?? (r.CollidingBooking != null ? "läuft gerade bei einem anderen Werker" : r.Outcome.ToString());
            e.Hinweise.Add($"Arbeitsgang {kuerzel} an Sub-FA {subFa}: {grund} – nur die Teilmeldung gespeichert");
        }
    }

    // ---------------------------------------------------------------- Ruecknahme (E4)

    /// <summary>F13: Frist der Werker-Ruecknahme = Schichtende der Meldung (<see cref="IBdeShiftCalendarService.GetShiftEndForBookingAsync"/>),
    /// ohne Schicht (Schichtkalender aus, Feiertag, keine Schicht) Ende des Meldetags 23:59:59 (Freigabe-Antwort 3, P-TL5: IDEAL ohne Kalender).</summary>
    public async Task<DateTime> RuecknahmeFristAsync(BdeTeilMeldung m) =>
        await _schichten.GetShiftEndForBookingAsync(m.ProductionWorkplaceId, m.GemeldetAm) ?? m.GemeldetAm.Date.AddDays(1).AddSeconds(-1);

    /// <summary>
    /// F13: Teilmeldung zuruecknehmen. <paramref name="werkerId"/> gesetzt = Werker am Terminal (nur eigene, nur bis zur Frist);
    /// null = BDE-Admin (Grund Pflicht, keine Frist). Die verknuepfte Baugruppen-AG-Buchung wird mitstorniert. Modified* aus dem
    /// angemeldeten Web-Benutzer (Terminal bzw. Admin), Muster <c>BdeBookingsController.Cancel</c>.
    /// </summary>
    public async Task<(bool Ok, string Text)> ZuruecknehmenAsync(int meldungId, int? werkerId, string? grund)
    {
        var m = await _ctx.BdeTeilMeldungen.Include(x => x.BdeBooking).FirstOrDefaultAsync(x => x.Id == meldungId);
        if (m == null) return (false, "Meldung nicht gefunden – nichts geändert.");
        if (m.IsCancelled) return (false, $"Meldung {m.Matchcode} ist bereits zurückgenommen – nichts geändert.");
        if (werkerId != null)
        {
            if (m.BdeOperatorId != werkerId) return (false, "Nur eigene Meldungen können zurückgenommen werden – nichts geändert.");
            var frist = await RuecknahmeFristAsync(m);
            if (DateTime.Now >= frist)
                return (false, $"Frist abgelaufen ({frist:dd.MM. HH:mm}) – Rücknahme nur noch über den BDE-Admin (BDE-Buchungen › Teilmeldungen).");
            grund = "vom Werker am Terminal zurückgenommen";
        }
        else if (string.IsNullOrWhiteSpace(grund))
        {
            return (false, "Bitte einen Grund angeben – nichts geändert.");
        }

        var now = DateTime.Now;
        m.IsCancelled = true;
        m.CancellationReason = grund!.Trim();
        SetModified(m, now);
        if (m.BdeBooking is { IsCancelled: false } b)
        {
            b.IsCancelled = true;
            b.CancellationReason = "Teilmeldung zurückgenommen";
            SetModified(b, now);
        }
        await _ctx.SaveChangesAsync();
        return (true, $"Meldung {m.Matchcode} zurückgenommen" + (m.BdeBooking != null ? " (Arbeitsgang-Buchung storniert)." : "."));
    }

    private void SetModified(AuditableEntity e, DateTime now)
    {
        e.ModifiedAt = now;
        e.ModifiedBy = _userSvc.GetDisplayName();
        e.ModifiedByWindows = _userSvc.GetWindowsUserName();
    }

    private Task<BdeTeilMeldung?> VorhandeneLagerMeldungAsync(int itemId, string kuerzel) =>
        _ctx.BdeTeilMeldungen.AsNoTracking().Include(m => m.BdeOperator)
            .Where(m => !m.IsCancelled && m.Kuerzel == kuerzel && m.WarehouseRequisitionItemId == itemId)
            .FirstOrDefaultAsync();

    private Task<BdeTeilMeldung?> VorhandeneMeldungAsync(TeilSchluessel s, string kuerzel) =>
        _ctx.BdeTeilMeldungen.AsNoTracking().Include(m => m.BdeOperator)
            .Where(m => !m.IsCancelled && m.WarehouseRequisitionItemId == null && m.Kuerzel == kuerzel && m.HauptFA == s.HauptFA && m.VaterFA == s.VaterFA && m.SubFA == s.SubFA
                && m.Position == s.Position && (s.Artnr == null ? m.Artnr == null || m.Artnr == "" : m.Artnr == s.Artnr))
            .FirstOrDefaultAsync();

    private static string BereitsText(string bezeichnung, BdeTeilMeldung m) =>
        $"{bezeichnung}: bereits gemeldet von {m.BdeOperator?.DisplayName ?? "?"} um {m.GemeldetAm:HH:mm}";

    /// <summary>sp_getapplock-Ressource (max. 255 Zeichen; Artnr bis 200) → Hash des Schluessels. Die eigentliche Wache ist der UNIQUE-Index.</summary>
    internal static string Sperre(TeilSchluessel s, string kuerzel) =>
        "BDE-Teilmeldung:" + Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(s.Text + "|" + kuerzel.ToUpperInvariant())));

    /// <summary>v1.99.72: Sperre je Lagerbestell-Position und Kuerzel (kurz, kein Hash noetig).</summary>
    internal static string LagerSperre(int itemId, string kuerzel) =>
        "BDE-Teilmeldung-LB:" + itemId.ToString(Inv) + "|" + kuerzel.ToUpperInvariant();

    private async Task InTransaktionAsync(Func<Task> aktion)
    {
        if (_ctx.Database.ProviderName?.Contains("InMemory", StringComparison.OrdinalIgnoreCase) == true)
        {
            await aktion();
            return;
        }
        await using var tx = await _ctx.Database.BeginTransactionAsync();
        await aktion();
        await tx.CommitAsync();
    }
}
