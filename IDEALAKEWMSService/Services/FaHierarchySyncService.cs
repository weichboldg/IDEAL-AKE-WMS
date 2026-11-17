using IdealAkeWms.Data;
using IdealAkeWms.Models;
using IdealAkeWms.Services.SyncLogger;
using IDEALAKEWMSService.Common;
using IDEALAKEWMSService.Services.Sdata;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace IDEALAKEWMSService.Services;

/// <summary>
/// Import der IDEAL-FA-Hierarchie aus den Sage-Views/SData in die lokalen Tabellen. Seit v1.99.37 (ADR 0032) ein Abgleich mit Lebenslauf
/// statt DELETE + Neubefuellung: neue Zeilen anlegen, vorhandene uebernehmen (Id bleibt), fehlende nach Sage-Zustand gekennzeichnet behalten
/// oder entfernen. Knoten + OrderInfos weiter in EINER Transaktion.
///
/// Absicherung: View-Namen aus ServiceSettings, Whitelist + QUOTENAME (FaHierarchySql). Empty-Guard
/// auf den ungefilterten Roh-Zeilenzahlen BEIDER Views VOR jeder Mutation. Fehlermail
/// (ISyncErrorNotifier) explizit bei ungueltigem View-Namen UND bei leerem View-Read, da
/// RunResilientAsync nur bei geworfener Exception mailt (ADR 0010, S-4).
///
/// Gelesen wird ueber <see cref="ISageFaStrukturReader"/> (v1.73.0 E4a): der bisherige SQL-Lese-Code steckt unveraendert in
/// <see cref="SageFaStrukturReader"/>, davor die Quellen-Weiche <c>Sync:Quelle:FaStruktur</c> (Default View).
/// </summary>
public class FaHierarchySyncService : IFaHierarchySyncService
{
    private const string SyncStep = "FaHierarchy-Sync";
    internal const string AufbewahrungKey = "Sync:FaStrukturAufbewahrungTage";

    private readonly ApplicationDbContext _ctx;
    private readonly ISageFaStrukturReader _reader;
    private readonly ISyncErrorNotifier _errorNotifier;
    private readonly ILogger<FaHierarchySyncService> _logger;
    private readonly IConfiguration _config;
    private readonly ISyncLogger _syncLogger;

    public FaHierarchySyncService(
        ApplicationDbContext ctx,
        ISageFaStrukturReader reader,
        ISyncErrorNotifier errorNotifier,
        ILogger<FaHierarchySyncService> logger,
        IConfiguration config,
        ISyncLogger syncLogger) // ISyncLogger als LETZTER Ctor-Parameter (ADR 0010)
    {
        _ctx = ctx;
        _reader = reader;
        _errorNotifier = errorNotifier;
        _logger = logger;
        _config = config;
        _syncLogger = syncLogger;
    }

    private static readonly string[] CountKeys =
    {
        "neu", "geaendert", "wieder_da", "verschwunden_beendet", "verschwunden_inaktiv", "verschwunden_unklar", "entfernt_position",
        "entfernt_in_sage", "aufgeraeumt", "schluessel_doppelt", "verschwunden_gesperrt", "sage_abfrage_fehler", "hauptfa_mehrdeutig",
    };

    /// <summary>Lebenslauf-Felder: aendern sie sich allein, gilt die Zeile nicht als geaendert.</summary>
    private static readonly HashSet<string> LebenslaufFelder = new()
    {
        nameof(IFaStrukturZeile.ErstGesehen), nameof(IFaStrukturZeile.ZuletztGesehen), nameof(IFaStrukturZeile.VerschwundenSeit),
        nameof(IFaStrukturZeile.VerschwundenGrund), nameof(IFaStrukturZeile.SyncedAt),
    };

    public async Task<FaHierarchySyncResult> RunAsync(bool dryRun, CancellationToken ct = default)
    {
        await using var run = await _syncLogger.BeginRunAsync(SyncLogServices.FaHierarchy, ct);

        try
        {
            // 1)–3) View-Namen, Existenzpruefung, Roh-Zeilenzahlen, Knoten + OrderInfos (SageFaStrukturReader bzw. Weiche).
            var lesung = await _reader.LesenAsync(ct);
            await LogQuelleAsync(run, ct);

            // 2) Empty-Guard: ungefilterte Roh-Zeilenzahlen BEIDER Views VOR jeder Mutation.
            if (lesung.ListeAnzahl == 0 || lesung.InfosAnzahl == 0)
            {
                var msg = $"Leerer View-Read (FAListe={lesung.ListeAnzahl}, FAInfos={lesung.InfosAnzahl}) — " +
                          "kein Abgleich, bestehende Zieltabellen bleiben unangetastet.";
                _logger.LogWarning("FaHierarchy-Sync: {Msg}", msg);
                await run.LogWarningAsync(msg, ct: ct);
                await _errorNotifier.NotifyAsync(
                    "FaHierarchy-Sync: leerer View-Read",
                    new InvalidOperationException($"FaHierarchy-Sync uebersprungen: {msg}"),
                    ct);
                await run.FinishSuccessAsync(CountKeys.ToDictionary(k => k, _ => 0), messageSuffix: "[uebersprungen: leerer View-Read]", ct: ct);
                return new FaHierarchySyncResult(0, 0, 0, 0, Skipped: true, dryRun);
            }

            var a = await AbgleichenAsync(run, lesung, dryRun, ct);
            var ambiguousCount = await LogAmbiguousHauptFasAsync(run, lesung.Infos, ct);
            a.Counts["hauptfa_mehrdeutig"] = ambiguousCount;
            await run.FinishSuccessAsync(a.Counts, messageSuffix: dryRun ? "[DryRun]" : null, ct: ct);
            return new FaHierarchySyncResult(lesung.Knoten.Count, lesung.Infos.Count, a.EntfernteKnoten, a.EntfernteInfos, Skipped: false, dryRun);
        }
        catch (FaHierarchyViewNameException ex)
        {
            // KEIN SQL wird ausgefuehrt (Reader validiert vor der Verbindung). FinishFailedAsync + explizite Fehlermail (S-4).
            _logger.LogError(ex, "FaHierarchy-Sync: ungueltiger View-Name.");
            await run.LogErrorAsync($"Ungueltiger View-Name: {ex.Message}", ct: ct);
            await run.FinishFailedAsync($"Ungueltiger View-Name: {ex.Message}", ct: ct);
            await _errorNotifier.NotifyAsync(
                "FaHierarchy-Sync: ungueltiger View-Name",
                new InvalidOperationException(
                    $"FaHierarchy-Sync abgebrochen — ungueltiger View-Name (kein SQL ausgefuehrt): {ex.Message}"),
                ct);
            return new FaHierarchySyncResult(0, 0, 0, 0, Skipped: true, dryRun);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Unerwarteter Fehler: FinishFailedAsync + rethrow. Die Fehlermail schickt der
            // umgebende RunResilientAsync-Block im SyncWorker (mailt bei geworfener Exception).
            _logger.LogError(ex, "FaHierarchy-Sync fehlgeschlagen.");
            await run.LogErrorAsync(ex.Message, ct: ct);
            await run.FinishFailedAsync(ex.Message, ct: ct);
            throw;
        }
    }

    private sealed record Ergebnis(Dictionary<string, int> Counts, int EntfernteKnoten, int EntfernteInfos);

    private sealed record Fehlend(IFaStrukturZeile Zeile, int SubFA, int Owner);

    /// <summary>
    /// v1.99.37 (Spec 2026-11-17-fa-struktur-abgleich-statt-full-replace, ADR 0032): Abgleich statt DELETE + Insert. Neu anlegen, Vorhandenes
    /// uebernehmen, Fehlendes nach Sage-Zustand behalten (gekennzeichnet) oder entfernen, danach aufraeumen. DryRun rechnet alles, schreibt nichts.
    /// </summary>
    private async Task<Ergebnis> AbgleichenAsync(ISyncRun run, FaStrukturLesung lesung, bool dryRun, CancellationToken ct)
    {
        var jetzt = DateTime.Now; // Lokalzeit wie SyncedAt (ADR 0025)
        var c = CountKeys.ToDictionary(k => k, _ => 0);
        var entfernt = new HashSet<object>(ReferenceEqualityComparer.Instance);
        foreach (var n in lesung.Knoten) n.Artnr = FaStrukturAbgleich.LeerAlsNull(n.Artnr);
        foreach (var i in lesung.Infos) { i.ABNr = FaStrukturAbgleich.LeerAlsNull(i.ABNr); i.Pos = FaStrukturAbgleich.LeerAlsNull(i.Pos); }

        // Bestand getrackt (einige tausend Zeilen; ExecuteUpdate bewusst nicht, InMemory testet es nicht).
        var knoten = await _ctx.FaHierarchyNodes.ToListAsync(ct);
        var infos = await _ctx.FaHierarchyOrderInfos.ToListAsync(ct);
        var lp = lesung.Positionen is { Count: > 0 } x ? x : null;
        var positionen = lp is null ? null : await _ctx.ProductionOrderPositions.ToListAsync(ct);

        // Dubletten-Waechter: eine HauptFA mit doppeltem Schluessel im Read bleibt in diesem Lauf in allen Tabellen unberuehrt.
        var (gesperrt, doppelt) = FaStrukturAbgleich.Doppelt(lesung.Knoten, FaStrukturAbgleich.Schluessel, n => n.HauptFA);
        var (gesperrtI, doppeltI) = FaStrukturAbgleich.Doppelt(lesung.Infos, FaStrukturAbgleich.Schluessel, i => i.HauptFA);
        gesperrt.UnionWith(gesperrtI);
        doppelt.AddRange(doppeltI);
        if (lp is not null)
        {
            var (gesperrtP, doppeltP) = FaStrukturAbgleich.Doppelt(lp, p => p.BelPosID, p => p.StrukturID);
            gesperrt.UnionWith(gesperrtP);
            doppelt.AddRange(doppeltP.Select(d => "BelPosID " + d));
        }
        var pk = FaStrukturAbgleich.Planen(knoten, lesung.Knoten, FaStrukturAbgleich.Schluessel, n => n.HauptFA, gesperrt);
        var pi = FaStrukturAbgleich.Planen(infos, lesung.Infos, FaStrukturAbgleich.Schluessel, i => i.HauptFA, gesperrt);
        var pp = lp is null ? null : FaStrukturAbgleich.Planen(positionen!, lp, p => p.BelPosID, p => p.StrukturID, gesperrt);

        c["schluessel_doppelt"] = gesperrt.Count;
        if (gesperrt.Count > 0)
        {
            var msg = $"Doppelte FA-Struktur-Schlüssel im Read – {gesperrt.Count} HauptFA in diesem Lauf nicht abgeglichen " +
                      $"({string.Join(", ", gesperrt.Order().Take(20))}); Schlüssel: {string.Join("; ", doppelt.Take(20))}.";
            await run.LogWarningAsync(Kurz(msg), ct: ct);
            await _errorNotifier.NotifyAsync("FaHierarchy-Sync: doppelte Schlüssel", new InvalidOperationException(msg), ct);
        }

        // Guard „alles verschwunden“ (Anforderung 4): keine einzige bisher aktive HauptFA im Read = defekter Read.
        var gelesenHaupt = lesung.Knoten.Select(n => n.HauptFA).Concat(lesung.Infos.Select(i => i.HauptFA)).ToHashSet();
        var aktivHaupt = knoten.Where(Aktiv).Select(n => n.HauptFA).Concat(infos.Where(Aktiv).Select(i => i.HauptFA)).ToHashSet();
        var sperre = aktivHaupt.Count > 0 && !aktivHaupt.Overlaps(gelesenHaupt);
        if (sperre)
        {
            c["verschwunden_gesperrt"] = 1;
            var msg = $"Keine der {aktivHaupt.Count} bisher aktiven HauptFA im Read – Read gilt als defekt; neue und geänderte Zeilen übernommen, " +
                      "nichts als verschwunden markiert oder entfernt.";
            await run.LogWarningAsync(msg, ct: ct);
            await _errorNotifier.NotifyAsync("FaHierarchy-Sync: Read ohne bisherige HauptFA", new InvalidOperationException(msg), ct);
        }

        // FAs, deren Stueckliste der Lauf gelesen hat (Owner einer gelesenen Zeile), und die Sage-Einzelabfrage fuer den Rest (nur SELECT).
        // QA 2026-11-17: NICHT SubFA eines gelesenen FA-Knotens und NICHT HauptFA aus FAInfos — ein beendeter Sub-FA steht weiter in der
        // Stueckliste des offenen Vaters, seine eigene Stueckliste fehlt aber; sie galt sonst als „Position entfernt“ und ging verloren (AK 4/5).
        var gelesen = lesung.Knoten.Select(FaStrukturAbgleich.Owner)
            .Concat(lp?.Select(p => p.BelID) ?? Enumerable.Empty<int>())
            .Where(id => id != 0).ToHashSet();
        var fehlend = pk.Fehlend.Select(n => new Fehlend(n, n.SubFA, FaStrukturAbgleich.Owner(n)))
            .Concat(pi.Fehlend.Select(i => new Fehlend(i, 0, i.HauptFA)))
            .Concat(pp?.Fehlend.Select(p => new Fehlend(p, 0, p.BelID)) ?? Enumerable.Empty<Fehlend>())
            .ToList();
        IReadOnlyDictionary<int, FaSageStatus> status = new Dictionary<int, FaSageStatus>();
        var sageFehler = false;
        if (!sperre)
        {
            var frage = fehlend.Where(f => !gelesen.Contains(f.Owner)).Select(f => f.Owner)
                .Concat(fehlend.Where(f => f.SubFA != 0).Select(f => f.SubFA)).ToHashSet();
            if (frage.Count > 0)
            {
                try
                {
                    status = await _reader.FaStatusLesenAsync(frage, ct);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    sageFehler = true;
                    c["sage_abfrage_fehler"] = 1;
                    _logger.LogError(ex, "FaHierarchy-Sync: Sage-Einzelabfrage fehlgeschlagen.");
                    await run.LogWarningAsync(Kurz($"Sage-Abfrage der verschwundenen FAs fehlgeschlagen – nichts markiert oder entfernt: {ex.Message}"), ct: ct);
                    await _errorNotifier.NotifyAsync("FaHierarchy-Sync: Sage-Abfrage fehlgeschlagen", ex, ct);
                }
            }
        }
        var entscheiden = !sperre && !sageFehler;

        // Knoten + OrderInfos in EINER Transaktion (wie bisher): anlegen, uebernehmen, markieren, entfernen.
        int entfKnoten = 0, entfInfos = 0;
        await using (var tx = !dryRun && _ctx.Database.IsRelational() ? await _ctx.Database.BeginTransactionAsync(ct) : null)
        {
            c["neu"] = Anlegen(_ctx.FaHierarchyNodes, pk.Neu, jetzt) + Anlegen(_ctx.FaHierarchyOrderInfos, pi.Neu, jetzt);
            Uebernehmen(pk.Paare, jetzt, c);
            Uebernehmen(pi.Paare, jetzt, c);
            if (entscheiden)
                foreach (var f in fehlend.Where(f => f.Zeile is not ProductionOrderPosition))
                    if (!Entscheiden(f, gelesen, status, jetzt, c, entfernt))
                    {
                        if (f.Zeile is FaHierarchyNode) entfKnoten++; else entfInfos++;
                    }
            if (!dryRun)
            {
                await _ctx.SaveChangesAsync(ct);
                if (tx is not null) await tx.CommitAsync(ct);
            }
        }

        // Positionen (nur aus SData, E4): dieselbe Logik in eigener Transaktion; im View-Betrieb (null) unberuehrt.
        if (lesung.Positionen is not null)
        {
            c["positionen_neu"] = 0;
            c["positionen_entfernt"] = 0;
            if (pp is null)
                await run.LogWarningAsync("ProductionOrderPositions: SData liefert keine Positionen – Tabelle bleibt unverändert.", ct: ct);
            else
                await PositionenAbgleichenAsync(run, pp, entscheiden ? fehlend : new(), gelesen, status, jetzt, dryRun, c, entfernt, ct);
        }

        if (c["verschwunden_unklar"] > 0)
        {
            var msg = $"{c["verschwunden_unklar"]} Zeilen fehlen im Read, ihr FA ist in Sage aber offen und aktiv – als „ungeklärt“ behalten.";
            await run.LogWarningAsync(msg, ct: ct);
            await _errorNotifier.NotifyAsync("FaHierarchy-Sync: ungeklärt verschwundene Zeilen", new InvalidOperationException(msg), ct);
        }

        // Aufraeumen (Anforderung 5) – nie nach einem defekten Read oder einem Sage-Lesefehler.
        if (entscheiden)
            c["aufgeraeumt"] = await AufraeumenAsync(knoten, infos, positionen, entfernt, jetzt, dryRun, ct);

        if (dryRun) _ctx.ChangeTracker.Clear();
        return new Ergebnis(c, entfKnoten, entfInfos);
    }

    /// <summary>E4: <c>ProductionOrderPositions</c> mit derselben Logik (ersetzt das fruehere Full-Replace). Fehlertoleranz wie bisher: ein Fehler
    /// laesst den Lauf erfolgreich, mit Protokoll-Fehler und Mail; der alte Stand bleibt.</summary>
    private async Task PositionenAbgleichenAsync(ISyncRun run, AbgleichPlan<ProductionOrderPosition> plan, List<Fehlend> fehlend,
        IReadOnlySet<int> gelesen, IReadOnlyDictionary<int, FaSageStatus> status, DateTime jetzt, bool dryRun, Dictionary<string, int> c,
        HashSet<object> entfernt, CancellationToken ct)
    {
        var entfernteVorher = new HashSet<object>(entfernt, ReferenceEqualityComparer.Instance);
        try
        {
            await using var tx = !dryRun && _ctx.Database.IsRelational() ? await _ctx.Database.BeginTransactionAsync(ct) : null;
            var neu = Anlegen(_ctx.ProductionOrderPositions, plan.Neu, jetzt);
            Uebernehmen(plan.Paare, jetzt, c);
            var weg = fehlend.Where(f => f.Zeile is ProductionOrderPosition).Count(f => !Entscheiden(f, gelesen, status, jetzt, c, entfernt));
            if (!dryRun)
            {
                await _ctx.SaveChangesAsync(ct);
                if (tx is not null) await tx.CommitAsync(ct);
            }
            c["positionen_neu"] = neu;
            c["positionen_entfernt"] = weg;
            await run.LogInfoAsync($"{(dryRun ? "[DryRun] " : "")}ProductionOrderPositions: {neu} neu, {weg} entfernt, {plan.Paare.Count} abgeglichen.", ct: ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _ctx.ChangeTracker.Clear();
            entfernt.IntersectWith(entfernteVorher);
            _logger.LogError(ex, "FaHierarchy-Sync: ProductionOrderPositions nicht abgeglichen.");
            await run.LogErrorAsync(Kurz($"ProductionOrderPositions nicht abgeglichen (alter Stand bleibt): {ex.Message}"), ct: ct);
            await _errorNotifier.NotifyAsync("FaHierarchy-Sync: FA-Positionen nicht abgeglichen", ex, ct);
        }
    }

    /// <summary>
    /// Anforderung 5: eine Haupt-FA-Struktur, deren Zeilen in allen drei Tabellen verschwunden sind, wird entfernt, wenn alle ihre WMS-Auftraege
    /// (<c>OrderNumber = HauptFA</c>) erledigt oder storniert sind — ein leeres „alle“ zaehlt nicht — oder ihr juengstes Verschwinden aelter als
    /// <c>Sync:FaStrukturAufbewahrungTage</c> ist. Eigene Transaktion. Liefert die Zahl der entfernten Strukturen.
    /// </summary>
    private async Task<int> AufraeumenAsync(List<FaHierarchyNode> knoten, List<FaHierarchyOrderInfo> infos, List<ProductionOrderPosition>? positionen,
        HashSet<object> entfernt, DateTime jetzt, bool dryRun, CancellationToken ct)
    {
        var je = knoten.Where(n => !entfernt.Contains(n)).Select(n => (n.HauptFA, Z: (IFaStrukturZeile)n))
            .Concat(infos.Where(i => !entfernt.Contains(i)).Select(i => (i.HauptFA, Z: (IFaStrukturZeile)i)))
            .GroupBy(t => t.HauptFA)
            .Where(g => g.All(t => !Aktiv(t.Z)))
            .ToDictionary(g => g.Key, g => g.Select(t => t.Z).ToList());
        if (je.Count == 0) return 0;

        var ids = je.Keys.ToList();
        var pos = positionen?.Where(p => !entfernt.Contains(p) && je.ContainsKey(p.StrukturID)).ToList()
                  ?? await _ctx.ProductionOrderPositions.Where(p => ids.Contains(p.StrukturID)).ToListAsync(ct);
        foreach (var p in pos) je[p.StrukturID].Add(p);

        var nummern = ids.Select(i => i.ToString(CultureInfo.InvariantCulture)).ToList();
        var auftraege = (await _ctx.ProductionOrders.AsNoTracking().Where(o => nummern.Contains(o.OrderNumber))
                .Select(o => new { o.OrderNumber, Fertig = o.IsDone || o.IsCancelled }).ToListAsync(ct))
            .ToLookup(o => o.OrderNumber, o => o.Fertig);
        var frist = Math.Clamp(await ServiceSettings.GetIntSafeAsync(_config, AufbewahrungKey, 90, ct), 1, 3650);
        var grenze = jetzt.AddDays(-frist);

        var weg = je.Where(kv =>
        {
            if (kv.Value.Any(Aktiv)) return false;
            var a = auftraege[kv.Key.ToString(CultureInfo.InvariantCulture)].ToList();
            return (a.Count > 0 && a.All(f => f)) || kv.Value.Max(z => z.VerschwundenSeit!.Value) < grenze;
        }).ToList();
        if (weg.Count == 0 || dryRun) return weg.Count;

        await using var tx = _ctx.Database.IsRelational() ? await _ctx.Database.BeginTransactionAsync(ct) : null;
        foreach (var z in weg.SelectMany(kv => kv.Value)) _ctx.Remove(z);
        await _ctx.SaveChangesAsync(ct);
        if (tx is not null) await tx.CommitAsync(ct);
        return weg.Count;
    }

    private static bool Aktiv(IFaStrukturZeile z) => z.VerschwundenSeit is null;

    private static string Kurz(string s) => s.Length <= 600 ? s : s[..600]; // SyncLogs.Message = 1000

    private static int Anlegen<T>(DbSet<T> set, List<T> neue, DateTime jetzt) where T : class, IFaStrukturZeile
    {
        foreach (var n in neue)
        {
            n.ErstGesehen = n.ZuletztGesehen = n.SyncedAt = jetzt;
            n.VerschwundenSeit = null;
            n.VerschwundenGrund = null;
        }
        set.AddRange(neue);
        return neue.Count;
    }

    /// <summary>Werte des Reads auf die Bestandszeile (<c>SetValues</c>); „geaendert“ = EF meldet eine geaenderte Eigenschaft ausser dem Lebenslauf.</summary>
    private void Uebernehmen<T>(List<(T Alt, T Neu)> paare, DateTime jetzt, Dictionary<string, int> c) where T : class, IFaStrukturZeile
    {
        foreach (var (alt, neu) in paare)
        {
            var e = _ctx.Entry(alt);
            var werte = _ctx.Entry(neu).CurrentValues.Clone();
            foreach (var p in e.Metadata.FindPrimaryKey()!.Properties) werte[p] = e.CurrentValues[p];
            werte[nameof(IFaStrukturZeile.ErstGesehen)] = alt.ErstGesehen;
            werte[nameof(IFaStrukturZeile.SyncedAt)] = alt.SyncedAt;
            werte[nameof(IFaStrukturZeile.ZuletztGesehen)] = jetzt;
            werte[nameof(IFaStrukturZeile.VerschwundenSeit)] = null;
            werte[nameof(IFaStrukturZeile.VerschwundenGrund)] = null;
            var wiederDa = alt.VerschwundenSeit is not null;
            e.CurrentValues.SetValues(werte);
            if (e.Properties.Any(p => p.IsModified && !LebenslaufFelder.Contains(p.Metadata.Name)))
            {
                alt.SyncedAt = jetzt;
                c["geaendert"]++;
            }
            if (wiederDa) c["wieder_da"]++;
        }
    }

    /// <summary>Anforderung 3 fuer eine fehlende Zeile; false = entfernt. Der erste <c>VerschwundenSeit</c> bleibt, der Grund folgt Sage.</summary>
    private bool Entscheiden(Fehlend f, IReadOnlySet<int> gelesen, IReadOnlyDictionary<int, FaSageStatus> status, DateTime jetzt,
        Dictionary<string, int> c, HashSet<object> entfernt)
    {
        var z = f.Zeile;
        var grund = FaStrukturAbgleich.Entscheiden(f.SubFA, f.Owner, gelesen, status);
        if (grund is null)
        {
            _ctx.Remove(z);
            entfernt.Add(z);
            if (z is not ProductionOrderPosition) c[gelesen.Contains(f.Owner) ? "entfernt_position" : "entfernt_in_sage"]++;
            return false;
        }
        if (z.VerschwundenGrund == grund) return true;
        z.VerschwundenSeit ??= jetzt;
        z.VerschwundenGrund = grund;
        c["verschwunden_" + grund]++;
        return true;
    }

    /// <summary>Info-Zeile „Quelle: View|SData|Vergleich“ (F1) + Hinweise des SData-Readers, nur wenn der Reader die Quellen-Weiche ist.</summary>
    private async Task LogQuelleAsync(ISyncRun run, CancellationToken ct)
    {
        if (_reader is not SageQuelleWeiche { GenutzteQuelle: not null } weiche)
            return;
        await weiche.LogQuelleAsync(run, ct);
        if (_reader is FaStrukturQuelleSwitch w)
            foreach (var h in w.SdataHinweise)
                if (h.Warnung) await run.LogWarningAsync(h.Text, ct: ct);
                else await run.LogInfoAsync(h.Text, ct: ct);
    }

    /// <summary>
    /// Kombigeraete: derselbe HauptFA mit mehreren Montage-Abteilungen. Eine Eigenschaft der
    /// DATEN, nicht der Anzeige — deshalb einmal je Lauf gemeldet statt je Seitenaufruf
    /// (Rueckfrage 7). Die Kennzeichnung in der Oberflaeche bleibt davon unberuehrt.
    /// Internal statt private: <see cref="RunAsync"/> braucht eine echte SqlConnection nach
    /// Sage und ist deshalb ohne Live-DB nicht InMemory-testbar — die reine Meldelogik wird
    /// hier separat gehalten, damit sie ueber InternalsVisibleTo direkt getestet werden kann.
    /// </summary>
    internal static async Task<int> LogAmbiguousHauptFasAsync(
        ISyncRun run, IReadOnlyCollection<FaHierarchyOrderInfo> infos, CancellationToken ct)
    {
        var ambiguousHauptFas = infos.GroupBy(i => i.HauptFA)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .OrderBy(k => k)
            .ToList();
        if (ambiguousHauptFas.Count > 0)
            await run.LogWarningAsync(
                $"Kombigeraete: {ambiguousHauptFas.Count} HauptFA mit mehreren Auftragskoepfen " +
                $"({string.Join(", ", ambiguousHauptFas)}). Kopfdaten werden in der FA-Liste als " +
                "mehrdeutig gekennzeichnet, es wird keine Variante ausgewaehlt.", ct: ct);
        return ambiguousHauptFas.Count;
    }
}
