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

/// <summary>Eine Zeile der Teileliste: Blechteil (Blatt) oder Baugruppe (eigene Sub-FA) mit dem Kuerzel der Werkbank.</summary>
public sealed class TeilelisteZeile
{
    public required FaHierarchyNode Node { get; init; }
    public required TeilSchluessel Schluessel { get; init; }
    public bool IstBaugruppe => Node.SubFA != 0;
    /// <summary>F16: Schluessel kommt in der Struktur mehrfach vor → angezeigt, nicht meldbar.</summary>
    public bool Eindeutig { get; init; } = true;
    /// <summary>Antwort 8: zum besitzenden Auftrag gibt es keinen WMS-Auftrag (meldbar, gekennzeichnet).</summary>
    public bool KeinWmsAuftrag { get; init; }
    public BdeTeilMeldung? Meldung { get; init; }
    public IReadOnlyList<FaHierarchyNode> Kinder { get; init; } = Array.Empty<FaHierarchyNode>();
    public bool Meldbar => Meldung == null && Eindeutig;
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
    public IReadOnlyList<TeilSchluessel> OffeneSchluessel { get; init; } = Array.Empty<TeilSchluessel>();
    public decimal SummeSollmengeOffen { get; init; }
}

/// <param name="Waisen">F15: offene Meldungen dieses Kuerzels, deren Schluessel keine Zeile mehr trifft (HauptFA noch in der Struktur).</param>
public sealed record TeilelisteDaten(IReadOnlyList<TeilelisteZeile> Zeilen, int Waisen);

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

    public BdeTeilelisteService(IFaHierarchyNodeRepository nodes, IFaHierarchyOrderInfoRepository orderInfos, ApplicationDbContext ctx)
    {
        _nodes = nodes; _orderInfos = orderInfos; _ctx = ctx;
    }

    /// <summary>F2: ein Token (Leerzeichen-Trennung wie Dienst und FA-Baum) ist das Kuerzel, Gross/Klein egal.</summary>
    public static bool Traegt(FaHierarchyNode n, string kuerzel) =>
        FaArbeitsschrittSymbole.Tokenize(n.Arbeitsschritte).Contains(kuerzel, StringComparer.OrdinalIgnoreCase);

    /// <summary>Besitzender Auftrag (Antwort 8): Baugruppe = eigene Sub-FA, Blechteil = Vater-Sub-FA.</summary>
    public static string? Besitzer(FaHierarchyNode n) => n.SubFA != 0 ? n.SubFA.ToString(Inv) : n.VaterFA?.ToString(Inv);

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

    /// <summary>Summiert: Filter auf der Aggregat-Projektion; alle anderen Spalten filtern je Position VOR dem Zusammenzaehlen.</summary>
    public static readonly IReadOnlyDictionary<string, Func<TeilelisteAggregat, string?>> SpaltenSummiert =
        new Dictionary<string, Func<TeilelisteAggregat, string?>>
        {
            ["matchcode"] = a => a.Matchcode,
            ["sollmenge"] = a => VormontageService.FormatMenge(a.SummeSollmenge),
            ["anzahl"] = a => a.Anzahl.ToString(Inv),
        };

    /// <summary>Laedt Struktur, Kopfwerte, offene Meldungen des Kuerzels und Auftragsstatus; Rest in <see cref="Bauen"/>.</summary>
    public async Task<TeilelisteDaten> LadenAsync(string kuerzel, (DateTime StartInclusive, DateTime EndExclusive)? kw, bool gemeldeteZeigen,
        IReadOnlyDictionary<string, string>? colf, Func<FaHierarchyNode, string?>? beschichtet = null)
    {
        var nodes = await _nodes.GetAllAsync();
        var infos = await _orderInfos.GetAllAsync();
        var hauptFas = nodes.Select(n => n.HauptFA).Distinct().ToList();
        var meldungen = await _ctx.BdeTeilMeldungen.AsNoTracking().Include(m => m.BdeOperator)
            .Where(m => !m.IsCancelled && m.Kuerzel == kuerzel && hauptFas.Contains(m.HauptFA)).ToListAsync();
        var besitzer = nodes.Where(n => Traegt(n, kuerzel)).Select(Besitzer).OfType<string>().Distinct().ToList();
        return Bauen(nodes, infos, kuerzel, meldungen, await AuftragOffenAsync(besitzer), kw, gemeldeteZeigen, colf, beschichtet);
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
        Func<FaHierarchyNode, string?>? beschichtet = null)
    {
        var infoListe = infos.ToList();
        var kopf = VormontageService.BuildKopfLookup(infoListe);
        var doppelt = nodes.GroupBy(TeilSchluessel.Von).Where(g => g.Count() > 1).Select(g => g.Key).ToHashSet();
        var kinder = nodes.Where(n => n.VaterFA.HasValue).ToLookup(n => n.VaterFA!.Value);
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
                    Kinder = n.SubFA != 0 ? kinder[n.SubFA].OrderBy(k => k.Position).ToList() : Array.Empty<FaHierarchyNode>(),
                };
            });

        if (kw is not null)
        {
            var inKw = IsoWeekRange.HauptFasInRange(infoListe, o => o.FE_Termin, kw.Value);
            zeilen = zeilen.Where(z => inKw.Contains(z.Node.HauptFA));
        }
        if (!gemeldeteZeigen) zeilen = zeilen.Where(z => z.Meldung == null);

        var spalten = SpaltenEinzeln(kopf, beschichtet).ToDictionary(e => e.Key, e => (Func<TeilelisteZeile, string?>)(z => e.Value(z.Node)));
        zeilen = ColumnFilterHelper.Apply(zeilen, colf, spalten, ColumnFilterTypes.For("BdeTeileliste/Index"));

        return new TeilelisteDaten(zeilen
            .OrderBy(z => z.Node.Matchcode, StringComparer.OrdinalIgnoreCase).ThenBy(z => z.Node.HauptFA)
            .ThenBy(z => z.Node.VaterFA).ThenBy(z => z.Node.Position).ToList(), waisen);
    }

    /// <summary>F7: je Matchcode (getrimmt, ordinal wie Vormontage). Die Zeilen sind schon je Position gefiltert (Einzeln-Spalten);
    /// hier wirken nur Matchcode, Summe und Anzahl auf der Aggregat-Projektion.</summary>
    public static List<TeilelisteAggregat> Summieren(IEnumerable<TeilelisteZeile> zeilen, IEnumerable<FaHierarchyOrderInfo> infos,
        IReadOnlyDictionary<string, string>? colf)
    {
        var infosJeHauptFa = infos.GroupBy(o => o.HauptFA).ToDictionary(g => g.Key, g => g.ToList());
        var kopf = infosJeHauptFa.ToDictionary(e => e.Key, e => VormontageService.BuildKopfWerte(e.Value));
        var aggregate = zeilen
            .GroupBy(z => (z.Node.Matchcode ?? string.Empty).Trim(), StringComparer.Ordinal)
            .Select(g =>
            {
                var hauptFas = g.Select(z => z.Node.HauptFA).Distinct().OrderBy(h => h).ToList();
                var offen = g.Where(z => z.Meldbar).ToList();
                return new TeilelisteAggregat
                {
                    Matchcode = g.Key,
                    SummeSollmenge = g.Sum(z => z.Node.Sollmenge),
                    Anzahl = g.Count(),
                    HauptFas = string.Join(", ", hauptFas.Select(h => h.ToString(Inv))),
                    Abteilungen = VormontageService.FormatTexte(hauptFas.Select(h => kopf.TryGetValue(h, out var k) ? k.Abteilung : null)),
                    FeTermine = VormontageService.FormatDaten(hauptFas.SelectMany(h => infosJeHauptFa.TryGetValue(h, out var l) ? l : new List<FaHierarchyOrderInfo>())
                        .Select(i => i.FE_Termin)),
                    OffeneSchluessel = offen.Select(z => z.Schluessel).ToList(),
                    SummeSollmengeOffen = offen.Sum(z => z.Node.Sollmenge),
                };
            })
            .OrderBy(a => a.Matchcode, StringComparer.OrdinalIgnoreCase);
        return ColumnFilterHelper.Apply(aggregate, colf, SpaltenSummiert, ColumnFilterTypes.For("BdeTeileliste/Summiert")).ToList();
    }
}
