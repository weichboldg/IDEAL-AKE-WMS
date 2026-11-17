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
    private readonly IBdeBookingService _booking;
    private readonly ICurrentUserService _userSvc;
    private readonly IAppSettingRepository _settings;

    public BdeTeilelisteService(IFaHierarchyNodeRepository nodes, IFaHierarchyOrderInfoRepository orderInfos, ApplicationDbContext ctx,
        IBdeBookingService booking, ICurrentUserService userSvc, IAppSettingRepository settings)
    {
        _nodes = nodes; _orderInfos = orderInfos; _ctx = ctx; _booking = booking; _userSvc = userSvc; _settings = settings;
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

    // ---------------------------------------------------------------- Melden (E3)

    /// <summary>Gut/Ausschuss null = volle Sollmenge, Ausschuss 0 (Block und Summiert, F9).</summary>
    public sealed record MeldeAuftrag(TeilSchluessel Schluessel, decimal? Gut, decimal? Ausschuss);

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
        foreach (var a in auftraege.DistinctBy(a => a.Schluessel))
            await InTransaktionAsync(() => MeldeZeileAsync(a, kuerzel, operatorId, workplaceId, terminalId, ergebnis));
        return ergebnis;
    }

    private async Task MeldeZeileAsync(MeldeAuftrag a, string kuerzel, int operatorId, int workplaceId, int terminalId, MeldeErgebnis e)
    {
        var s = a.Schluessel;
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
        if (gut < 0 || ausschuss < 0)
        {
            e.Abgewiesen.Add($"{bezeichnung}: Mengen dürfen nicht negativ sein");
            return;
        }
        if (gut + ausschuss == 0)
        {
            e.Abgewiesen.Add($"{bezeichnung}: bitte eine Gut- oder Ausschussmenge angeben");
            return;
        }

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
        _ctx.BdeTeilMeldungen.Add(meldung);
        try
        {
            await _ctx.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (Nummernkreis.IstSqlFehler(ex, 2601, 2627))
        {
            // DB-Gleichstand (ADR 0026): der gefilterte UNIQUE hat die zweite Meldung abgewiesen.
            _ctx.Entry(meldung).State = EntityState.Detached;
            var gewinner = await VorhandeneMeldungAsync(s, kuerzel);
            e.BereitsGemeldet.Add(gewinner != null ? BereitsText(bezeichnung, gewinner) : $"{bezeichnung}: bereits gemeldet");
            return;
        }
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

    private Task<BdeTeilMeldung?> VorhandeneMeldungAsync(TeilSchluessel s, string kuerzel) =>
        _ctx.BdeTeilMeldungen.AsNoTracking().Include(m => m.BdeOperator)
            .Where(m => !m.IsCancelled && m.Kuerzel == kuerzel && m.HauptFA == s.HauptFA && m.VaterFA == s.VaterFA && m.SubFA == s.SubFA
                && m.Position == s.Position && (s.Artnr == null ? m.Artnr == null || m.Artnr == "" : m.Artnr == s.Artnr))
            .FirstOrDefaultAsync();

    private static string BereitsText(string bezeichnung, BdeTeilMeldung m) =>
        $"{bezeichnung}: bereits gemeldet von {m.BdeOperator?.DisplayName ?? "?"} um {m.GemeldetAm:HH:mm}";

    /// <summary>sp_getapplock-Ressource (max. 255 Zeichen; Artnr bis 200) → Hash des Schluessels. Die eigentliche Wache ist der UNIQUE-Index.</summary>
    internal static string Sperre(TeilSchluessel s, string kuerzel) =>
        "BDE-Teilmeldung:" + Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(s.Text + "|" + kuerzel.ToUpperInvariant())));

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
