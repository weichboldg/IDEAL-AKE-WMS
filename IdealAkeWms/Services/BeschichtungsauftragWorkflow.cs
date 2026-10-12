using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IdealAkeWms.Services;

/// <summary>Ergebnis jeder Workflow-Operation: Klartext-Fehler (Aktion abgelehnt, nichts geaendert) und Hinweise (Aktion
/// ausgefuehrt, aber etwas ist zu melden). Teil 4 uebersetzt in <c>ModelState</c>/<c>TempData["WarningMessage"]</c>.</summary>
public sealed class BeschichtungsauftragErgebnis
{
    public bool Erfolg => Fehler.Count == 0;
    public Beschichtungsauftrag? Auftrag { get; set; }
    public List<string> Fehler { get; } = new();
    public List<string> Hinweise { get; } = new();
}

/// <summary>Aenderbare Kopffelder im Entwurf (Teil-4-Antworten 4, 7).</summary>
public sealed record KopfDaten(string? Dienstleister, string? Ral, DateTime? StartBeschichtung, DateTime? BeschichtenRetour);

/// <summary>Eine Position, die einem Entwurf hinzugefuegt werden darf (fuer die Auswahl in Teil 4) — dieselbe Regel wie
/// <see cref="BeschichtungsauftragWorkflow.PositionenHinzufuegenAsync"/>.</summary>
public sealed record PositionsKandidat(PositionsSchluessel Schluessel, string? HauptArtnr, string? Artnr, string? Matchcode,
    decimal Menge, decimal Breite, decimal Hoehe, decimal Tiefe);

/// <summary>
/// Fachliche Regeln des gespeicherten Beschichtungsauftrags (Spec 2026-10-06-beschichtungsauftrag-ausbau-teil-3,
/// Antworten 1–15). Einziger Schreibweg fuer <see cref="Beschichtungsauftrag"/>:
/// <list type="bullet">
///   <item>Nummer <c>BA-JJJJ-NNNNN</c> je Datenbank: <c>MAX+1</c> im Jahr, der UNIQUE-Index entscheidet bei parallelem
///     Anlegen, der Verlierer wiederholt (<see cref="MaxNummerVersuche"/>).</item>
///   <item>Status nur Entwurf → Uebergeben → Erledigt und Entwurf → Storniert; Uebergeben nur mit Dienstleister.</item>
///   <item>Doppelvergabe nur fuer Erstauftraege geprueft: sperrend ist jeder Auftrag mit Status ≠ Storniert, inkl.
///     Reklamationen. Erstauftrags-Pfade laufen auf SQL Server in einer Serializable-Transaktion (Key-Range-Locks).</item>
///   <item>Reklamation nur aus Erledigt, Kopf 1:1 vom Ursprung, Positionen nur aus dem direkten Ursprung, keine
///     Vergabe-Pruefung (Antworten 11, 13–15).</item>
/// </list>
/// Knoten werden ungecacht gelesen (<see cref="FaHierarchyNodeRepository"/>, nicht der 5-Minuten-Decorator) — Anlegen und
/// Hinzufuegen pruefen gegen den aktuellen Sage-Cache.
/// </summary>
public class BeschichtungsauftragWorkflow
{
    public const int MaxNummerVersuche = 3;
    public const int MaxBemerkungLaenge = 1000;
    public const int MaxKopfTextLaenge = 200;

    private static readonly IReadOnlySet<(BeschichtungsauftragStatus, BeschichtungsauftragStatus)> ErlaubteUebergaenge =
        new HashSet<(BeschichtungsauftragStatus, BeschichtungsauftragStatus)>
        {
            (BeschichtungsauftragStatus.Entwurf, BeschichtungsauftragStatus.Uebergeben),
            (BeschichtungsauftragStatus.Uebergeben, BeschichtungsauftragStatus.Erledigt),
            (BeschichtungsauftragStatus.Entwurf, BeschichtungsauftragStatus.Storniert),
        };

    private readonly IBeschichtungsauftragRepository _repo;
    private readonly FaHierarchyNodeRepository _nodes;
    private readonly IFaHierarchyOrderInfoRepository _infos;
    private readonly ICurrentUserService _user;
    private readonly TimeProvider _time;
    private readonly ILogger<BeschichtungsauftragWorkflow> _logger;

    public BeschichtungsauftragWorkflow(
        IBeschichtungsauftragRepository repo,
        FaHierarchyNodeRepository nodes,
        IFaHierarchyOrderInfoRepository infos,
        ICurrentUserService user,
        TimeProvider time,
        ILogger<BeschichtungsauftragWorkflow> logger)
    {
        _repo = repo;
        _nodes = nodes;
        _infos = infos;
        _user = user;
        _time = time;
        _logger = logger;
    }

    private DateTime Jetzt => _time.GetLocalNow().DateTime;

    // ===================== Anlegen (Anforderungen 2, 4, 6, 7, 8) =====================

    /// <summary>Legt einen Erstauftrag fuer den HauptFA aus allen aktuell beschichteten FREIEN Positionen an.</summary>
    public Task<BeschichtungsauftragErgebnis> AnlegenAsync(int hauptFa) =>
        MitNummernVersuchenAsync(serializable: true, async () =>
        {
            var r = new BeschichtungsauftragErgebnis();
            var beschichtet = (await _nodes.GetByHauptFaAsync(hauptFa)).Where(n => n.Beschichtet).ToList();
            if (beschichtet.Count == 0)
            {
                r.Fehler.Add($"Für HauptFA {hauptFa} gibt es im Sage-Cache keine beschichteten Positionen — kein Auftrag angelegt.");
                return r;
            }
            MeldeDoppelteSchluessel(beschichtet, r);

            var vergaben = VergabenJeSchluessel(await _repo.GetVergabenAsync(hauptFa));
            var vergeben = beschichtet.Where(n => vergaben.ContainsKey(PositionsSchluessel.Von(n))).ToList();
            var frei = beschichtet.Except(vergeben).ToList();
            if (frei.Count == 0)
            {
                r.Fehler.Add($"Alle {beschichtet.Count} beschichteten Positionen von HauptFA {hauptFa} sind bereits vergeben — kein " +
                             $"neuer Auftrag: {Liste(vergeben, vergaben)}");
                return r;
            }
            if (vergeben.Count > 0)
                r.Hinweise.Add($"{vergeben.Count} Positionen bereits in {string.Join(", ", vergeben.SelectMany(n => vergaben[PositionsSchluessel.Von(n)]).Distinct())} " +
                               $"vergeben, nicht übernommen: {Liste(vergeben, vergaben)}");

            var auftrag = new Beschichtungsauftrag { HauptFA = hauptFa, Status = BeschichtungsauftragStatus.Entwurf };
            Erstellt(auftrag);
            KopfVorbelegen(auftrag, await _infos.GetByHauptFaAsync(hauptFa), r);
            foreach (var n in frei) auftrag.Positionen.Add(Snapshot(n));
            await NummerVergebenUndSpeichernAsync(auftrag);
            r.Auftrag = auftrag;
            return r;
        });

    // ===================== Reklamation (Anforderung 11, Antworten 11, 13, 15) =====================

    /// <summary>Reklamations-Folgeauftrag aus einem <c>Erledigt</c>-Auftrag mit den ausgewaehlten Ursprungspositionen
    /// (<paramref name="positionIds"/> = Ids der Auftragspositionen des Ursprungs).</summary>
    public async Task<BeschichtungsauftragErgebnis> ReklamationAnlegenAsync(int ursprungId, IReadOnlyCollection<int> positionIds)
    {
        var pruefung = new BeschichtungsauftragErgebnis();
        var ursprung = await LadenAsync(ursprungId, pruefung);
        if (ursprung is null) return pruefung;
        if (ursprung.Status != BeschichtungsauftragStatus.Erledigt)
            return Fehler($"Reklamation nur aus „Erledigt\" möglich — {ursprung.Nummer} steht auf {Anzeige(ursprung.Status)}.");
        var ids = positionIds.Distinct().ToList();
        if (ids.Count == 0) return Fehler("Reklamation: keine Positionen ausgewählt.");
        var fremd = ids.Where(id => ursprung.Positionen.All(p => p.Id != id)).ToList();
        if (fremd.Count > 0)
            return Fehler($"Position(en) {string.Join(", ", fremd)} gehört nicht zu {ursprung.Nummer} — Reklamation nur aus dessen Positionen.");

        return await MitNummernVersuchenAsync(serializable: false, async () =>
        {
            var neu = new Beschichtungsauftrag
            {
                HauptFA = ursprung.HauptFA,
                Status = BeschichtungsauftragStatus.Entwurf,
                UrsprungsauftragId = ursprung.Id,
                // Kopf 1:1 vom Ursprung, nicht aus der PPS-View (Antwort 13); Bemerkung bewusst nicht.
                Dienstleister = ursprung.Dienstleister,
                Ral = ursprung.Ral,
                StartBeschichtung = ursprung.StartBeschichtung,
                BeschichtenRetour = ursprung.BeschichtenRetour,
            };
            Erstellt(neu);
            foreach (var p in ursprung.Positionen.Where(p => ids.Contains(p.Id))) neu.Positionen.Add(Kopie(p));
            await NummerVergebenUndSpeichernAsync(neu);
            return new BeschichtungsauftragErgebnis { Auftrag = neu };
        });
    }

    // ===================== Entwurf bearbeiten (Anforderung 10) =====================

    public async Task<BeschichtungsauftragErgebnis> EntwurfBearbeitenAsync(int id, KopfDaten kopf,
        IReadOnlyDictionary<int, decimal> mengen, IReadOnlyCollection<int> entfernteIds, byte[] rowVersion)
    {
        var r = new BeschichtungsauftragErgebnis();
        var a = await LadenAsync(id, r);
        if (a is null) return r;
        if (a.Status != BeschichtungsauftragStatus.Entwurf)
            return Fehler($"Kopf und Positionen sind nur im Entwurf änderbar — {a.Nummer} steht auf {Anzeige(a.Status)}; " +
                          "möglich ist nur noch Statuswechsel und Bemerkung.");

        foreach (var pid in mengen.Keys.Concat(entfernteIds).Distinct().Where(pid => a.Positionen.All(p => p.Id != pid)))
            r.Fehler.Add($"Position {pid} gehört nicht zu {a.Nummer}.");
        foreach (var (pid, menge) in mengen.Where(m => m.Value <= 0))
            r.Fehler.Add($"Menge muss größer 0 sein (Position {pid}: {menge}) — zum Weglassen die Position entfernen.");
        foreach (var (feld, wert) in new[] { ("Dienstleister", kopf.Dienstleister), ("RAL", kopf.Ral) })
            if (wert?.Trim().Length > MaxKopfTextLaenge)
                r.Fehler.Add($"{feld} höchstens {MaxKopfTextLaenge} Zeichen.");
        if (!r.Erfolg) return r;

        var jetzt = Jetzt;
        a.Dienstleister = Leer(kopf.Dienstleister);
        a.Ral = Leer(kopf.Ral);
        a.StartBeschichtung = kopf.StartBeschichtung;
        a.BeschichtenRetour = kopf.BeschichtenRetour;
        foreach (var p in a.Positionen.Where(p => mengen.TryGetValue(p.Id, out var m) && m != p.Menge))
        {
            p.Menge = mengen[p.Id];
            Geaendert(p, jetzt);
        }
        foreach (var p in a.Positionen.Where(p => entfernteIds.Contains(p.Id)).ToList())
        {
            a.Positionen.Remove(p);
            _repo.Remove(p);   // geloescht = sofort frei (Anforderung 6)
        }
        Geaendert(a, jetzt);
        return await SpeichernAsync(a, rowVersion);
    }

    // ===================== Positionen hinzufuegen (Anforderung 10a, Antworten 12a, 14) =====================

    /// <summary>Kandidaten fuer das Hinzufuegen in einen Entwurf — Erst-Entwurf: freie beschichtete Positionen des HauptFA aus
    /// dem aktuellen Cache; Reklamations-Entwurf: Positionen des direkten Ursprungs, die noch nicht enthalten sind.
    /// Kein Entwurf oder nicht gefunden: leer.</summary>
    public async Task<List<PositionsKandidat>> GetHinzufuegbarePositionenAsync(int id)
    {
        var a = await _repo.GetMitPositionenAsync(id);
        if (a is null || a.Status != BeschichtungsauftragStatus.Entwurf) return new();
        var eigene = a.Positionen.Select(PositionsSchluessel.Von).ToHashSet();

        if (a.UrsprungsauftragId is int uid)
        {
            var u = await _repo.GetMitPositionenAsync(uid);
            return (u?.Positionen ?? new List<BeschichtungsauftragPosition>())
                .Where(p => !eigene.Contains(PositionsSchluessel.Von(p)))
                .DistinctBy(PositionsSchluessel.Von)
                .Select(p => new PositionsKandidat(PositionsSchluessel.Von(p), p.HauptArtnr, p.Artnr, p.Matchcode, p.Menge, p.Breite, p.Hoehe, p.Tiefe))
                .ToList();
        }

        var vergaben = VergabenJeSchluessel(await _repo.GetVergabenAsync(a.HauptFA));
        return (await _nodes.GetByHauptFaAsync(a.HauptFA))
            .Where(n => n.Beschichtet && !vergaben.ContainsKey(PositionsSchluessel.Von(n)))
            .DistinctBy(PositionsSchluessel.Von)
            .Select(n => new PositionsKandidat(PositionsSchluessel.Von(n), n.HauptArtnr, n.Artnr, n.Matchcode, n.Sollmenge, n.Breite, n.Hoehe, n.Tiefe))
            .ToList();
    }

    /// <summary>Fuegt einem Entwurf Positionen hinzu (Herkunftsschluessel; der HauptFA kommt aus dem Auftrag). Eine teilweise
    /// gueltige Auswahl wird ganz abgelehnt.</summary>
    public async Task<BeschichtungsauftragErgebnis> PositionenHinzufuegenAsync(int id, IReadOnlyCollection<PositionsSchluessel> schluessel, byte[] rowVersion)
    {
        var r = new BeschichtungsauftragErgebnis();
        var a = await LadenAsync(id, r);
        if (a is null) return r;
        if (a.Status != BeschichtungsauftragStatus.Entwurf)
            return Fehler($"Positionen hinzufügen nur im Entwurf — {a.Nummer} steht auf {Anzeige(a.Status)}.");
        var keys = schluessel.Distinct().ToList();
        if (keys.Count == 0) return Fehler("Positionen hinzufügen: keine Positionen ausgewählt.");
        var eigene = a.Positionen.Select(PositionsSchluessel.Von).ToHashSet();
        foreach (var k in keys.Where(eigene.Contains))
            r.Fehler.Add($"Position {k} steht bereits in diesem Auftrag ({a.Nummer}).");

        if (a.UrsprungsauftragId is int uid)
        {
            // Reklamations-Entwurf: nur Ursprungspositionen, Kopie aus dem Ursprung, keine Vergabe-Pruefung (Antworten 14, 15).
            var u = await _repo.GetMitPositionenAsync(uid);
            var ausUrsprung = (u?.Positionen ?? new List<BeschichtungsauftragPosition>())
                .GroupBy(PositionsSchluessel.Von).ToDictionary(g => g.Key, g => g.First());
            foreach (var k in keys.Where(k => !eigene.Contains(k) && !ausUrsprung.ContainsKey(k)))
                r.Fehler.Add($"Im Reklamationsauftrag nur Positionen des Ursprungs {u?.Nummer} — Position {k} über einen neuen Auftrag vergeben.");
            if (!r.Erfolg) return r;

            foreach (var k in keys) a.Positionen.Add(Kopie(ausUrsprung[k]));
            Geaendert(a, Jetzt);
            return await SpeichernAsync(a, rowVersion);
        }

        // Fehlerbehandlung AUSSERHALB der Transaktion: scheitert ein Lesen oder Speichern darin (z. B. Deadlock-Opfer), ist
        // die SQL-Server-Transaktion beendet — ein Commit danach wuerde selbst werfen (QA I-1).
        return await MitFehlerbehandlungAsync(a.Nummer, () => _repo.InSerializableTransactionAsync(async () =>
        {
            var knoten = (await _nodes.GetByHauptFaAsync(a.HauptFA)).GroupBy(PositionsSchluessel.Von).ToDictionary(g => g.Key, g => g.First());
            var vergaben = VergabenJeSchluessel(await _repo.GetVergabenAsync(a.HauptFA));
            foreach (var k in keys.Where(k => !eigene.Contains(k)))
            {
                if (!knoten.TryGetValue(k, out var n))
                    r.Fehler.Add($"Position {k} ist nicht im Cache von HauptFA {a.HauptFA} (nicht mehr vorhanden oder anderer HauptFA).");
                else if (!n.Beschichtet)
                    r.Fehler.Add($"Position {k} ist nicht beschichtet.");
                else if (vergaben.TryGetValue(k, out var nummern))
                    r.Fehler.Add($"Position {k} ist bereits vergeben in {string.Join(", ", nummern)}.");
            }
            if (!r.Erfolg) return r;

            foreach (var k in keys) a.Positionen.Add(Snapshot(knoten[k]));
            Geaendert(a, Jetzt);
            await SpeichernRohAsync(a, rowVersion);
            return new BeschichtungsauftragErgebnis { Auftrag = a };
        }));
    }

    // ===================== Bemerkung, Status (Anforderungen 5, 10) =====================

    public async Task<BeschichtungsauftragErgebnis> BemerkungAendernAsync(int id, string? text, byte[] rowVersion)
    {
        var r = new BeschichtungsauftragErgebnis();
        var a = await LadenAsync(id, r);
        if (a is null) return r;
        if (a.Status == BeschichtungsauftragStatus.Storniert)
            return Fehler($"Bemerkung im Status Storniert nicht änderbar ({a.Nummer}).");
        if (text?.Length > MaxBemerkungLaenge)
            return Fehler($"Bemerkung höchstens {MaxBemerkungLaenge} Zeichen ({text.Length} eingegeben).");

        a.Bemerkung = Leer(text);
        Geaendert(a, Jetzt);
        return await SpeichernAsync(a, rowVersion);
    }

    public async Task<BeschichtungsauftragErgebnis> StatusWechselnAsync(int id, BeschichtungsauftragStatus ziel, byte[] rowVersion)
    {
        var r = new BeschichtungsauftragErgebnis();
        var a = await LadenAsync(id, r);
        if (a is null) return r;
        if (!ErlaubteUebergaenge.Contains((a.Status, ziel)))
            return Fehler($"Statuswechsel {Anzeige(a.Status)} → {Anzeige(ziel)} ist nicht erlaubt ({a.Nummer}). Erlaubt: " +
                          "Entwurf → Übergeben → Erledigt sowie Entwurf → Storniert.");
        if (ziel == BeschichtungsauftragStatus.Uebergeben && string.IsNullOrWhiteSpace(a.Dienstleister))
            return Fehler("Dienstleister fehlt — vor dem Uebergeben am Auftrag eintragen.");

        var jetzt = Jetzt;
        var userId = _user.GetCurrentAppUserId();
        a.Status = ziel;
        switch (ziel)
        {
            case BeschichtungsauftragStatus.Uebergeben: a.UebergebenAt = jetzt; a.UebergebenByUserId = userId; break;
            case BeschichtungsauftragStatus.Erledigt: a.ErledigtAt = jetzt; a.ErledigtByUserId = userId; break;
            case BeschichtungsauftragStatus.Storniert: a.StorniertAt = jetzt; a.StorniertByUserId = userId; break;
        }
        Geaendert(a, jetzt);
        return await SpeichernAsync(a, rowVersion);
    }

    // ===================== Bausteine =====================

    private async Task<Beschichtungsauftrag?> LadenAsync(int id, BeschichtungsauftragErgebnis r)
    {
        var a = await _repo.GetMitPositionenAsync(id);
        if (a is null) r.Fehler.Add($"Beschichtungsauftrag {id} nicht gefunden.");
        return a;
    }

    /// <summary>Speichert eine Aenderung mit optimistischer Nebenlaeufigkeit (RowVersion des Aufrufers); Fehler werden
    /// zu Klartext. Nicht innerhalb einer Transaktion verwenden — dort <see cref="SpeichernRohAsync"/> + aussen
    /// <see cref="MitFehlerbehandlungAsync"/>.</summary>
    private Task<BeschichtungsauftragErgebnis> SpeichernAsync(Beschichtungsauftrag a, byte[] rowVersion) =>
        MitFehlerbehandlungAsync(a.Nummer, async () =>
        {
            await SpeichernRohAsync(a, rowVersion);
            return new BeschichtungsauftragErgebnis { Auftrag = a };
        });

    private async Task SpeichernRohAsync(Beschichtungsauftrag a, byte[] rowVersion)
    {
        _repo.SetOriginalRowVersion(a, rowVersion);
        await _repo.SaveChangesAsync();
    }

    /// <summary>Uebersetzt Speicherfehler in Klartext und verwirft danach ALLE ungespeicherten Aenderungen des Request-Contexts
    /// (QA M-1: sonst stuenden halbgeaenderte Werte im Speicher und ein Folgeaufruf im selben Request saehe sie).
    /// Nicht-Nebenlaeufigkeits-Fehler werden geloggt (QA M-2).</summary>
    private async Task<BeschichtungsauftragErgebnis> MitFehlerbehandlungAsync(string nummer, Func<Task<BeschichtungsauftragErgebnis>> op)
    {
        try
        {
            return await op();
        }
        catch (DbUpdateConcurrencyException)
        {
            _repo.VerwerfeAenderungen();
            return Fehler($"{nummer} wurde inzwischen von jemand anderem geändert — bitte neu laden und erneut versuchen.");
        }
        catch (Exception ex) when (ex is DbUpdateException || IstDeadlock(ex))
        {
            _repo.VerwerfeAenderungen();
            if (IstDeadlock(ex))
            {
                _logger.LogWarning(ex, "Beschichtungsauftrag {Nummer}: Deadlock-Opfer bei gleichzeitiger Änderung.", nummer);
                return Fehler($"{nummer} konnte wegen einer gleichzeitigen Änderung nicht gespeichert werden — bitte erneut versuchen.");
            }
            _logger.LogError(ex, "Beschichtungsauftrag {Nummer}: Speichern fehlgeschlagen.", nummer);
            return Fehler($"{nummer} konnte nicht gespeichert werden (Datenbankfehler, Details im Log) — bitte erneut versuchen.");
        }
    }

    /// <summary>Fuehrt eine Anlage mit Nummernvergabe aus; scheitert das Speichern am UNIQUE-Index (paralleles Anlegen)
    /// oder als Deadlock-Opfer der Serializable-Transaktion, wird alles neu gelesen und erneut versucht.</summary>
    private async Task<BeschichtungsauftragErgebnis> MitNummernVersuchenAsync(bool serializable, Func<Task<BeschichtungsauftragErgebnis>> anlage)
    {
        for (var versuch = 1; ; versuch++)
        {
            try
            {
                return serializable ? await _repo.InSerializableTransactionAsync(anlage) : await anlage();
            }
            catch (Exception ex) when (ex is DbUpdateException || IstDeadlock(ex))
            {
                // Jeder Versuch in einer neuen Transaktion; die alte ist mit der Ausnahme zurueckgerollt (Dispose).
                // ponytail: es wird jeder DbUpdateException wiederholt (Unique-Konflikt der Nummer ist ohne SqlException-
                // Nummer nicht sicher erkennbar); andere DB-Fehler kosten so 3 Versuche — deshalb jeder Versuch im Log.
                _repo.VerwerfeAenderungen();
                _logger.LogWarning(ex, "Beschichtungsauftrag anlegen: Versuch {Versuch}/{Max} gescheitert.", versuch, MaxNummerVersuche);
                if (versuch >= MaxNummerVersuche)
                    return Fehler($"Konnte nach {MaxNummerVersuche} Versuchen keine freie Nummer vergeben (gleichzeitige Anlage oder " +
                                  "Datenbankfehler, Details im Log) — bitte erneut versuchen.");
            }
        }
    }

    private async Task NummerVergebenUndSpeichernAsync(Beschichtungsauftrag auftrag)
    {
        var jahr = Jetzt.Year;
        var lfd = await _repo.GetMaxLaufendeNummerAsync(jahr) + 1;
        auftrag.Jahr = jahr;
        auftrag.LaufendeNummer = lfd;
        auftrag.Nummer = $"BA-{jahr}-{lfd:D5}";
        _repo.Add(auftrag);
        await _repo.SaveChangesAsync();
    }

    private static bool IstDeadlock(Exception ex) => ex is SqlException { Number: 1205 } || ex.InnerException is SqlException { Number: 1205 };

    /// <summary>Kopf aus der PPS-View (nur Erstauftraege): je Feld nur, wenn alle Kopfzeilen denselben Wert liefern;
    /// sonst leer + Hinweis mit den Varianten — nie still eine Variante waehlen (Anforderung 4).</summary>
    private static void KopfVorbelegen(Beschichtungsauftrag a, List<FaHierarchyOrderInfo> infos, BeschichtungsauftragErgebnis r)
    {
        if (infos.Count == 0)
        {
            r.Hinweise.Add($"Keine Auftrags-Kopfdaten (PPS-View) zu HauptFA {a.HauptFA} — Dienstleister, RAL und Termine bleiben leer.");
            return;
        }
        var abweichend = new List<string>();
        T? Einheitlich<T>(string feld, Func<FaHierarchyOrderInfo, T?> wert, Func<T?, string> text)
        {
            var werte = infos.Select(wert).Distinct().ToList();
            if (werte.Count == 1) return werte[0];
            abweichend.Add($"{feld}: {string.Join(" | ", werte.Select(text))}");
            return default;
        }
        a.Dienstleister = Einheitlich("Dienstleister", i => Leer(i.Dienstleister), v => v ?? "(leer)");
        a.Ral = Einheitlich("RAL", i => Leer(i.RAL), v => v ?? "(leer)");
        a.StartBeschichtung = Einheitlich("Start Beschichtung", i => i.Start_Beschichtung, v => v?.ToString("dd.MM.yyyy") ?? "(leer)");
        a.BeschichtenRetour = Einheitlich("Retour", i => i.Beschichten_Retour, v => v?.ToString("dd.MM.yyyy") ?? "(leer)");
        if (abweichend.Count > 0)
            r.Hinweise.Add($"Kopf aus Sage mehrdeutig ({infos.Count} Kopfzeilen, Kombigerät) — abweichende Felder bleiben leer: {string.Join("; ", abweichend)}");
    }

    private static void MeldeDoppelteSchluessel(IEnumerable<FaHierarchyNode> knoten, BeschichtungsauftragErgebnis r)
    {
        var doppelt = knoten.GroupBy(PositionsSchluessel.Von).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        if (doppelt.Count > 0)
            r.Hinweise.Add($"Herkunftsschlüssel im Sage-Cache nicht eindeutig (Doppelvergabe-Prüfung unscharf): {string.Join("; ", doppelt)} — Struktur in Sage prüfen.");
    }

    private static Dictionary<PositionsSchluessel, List<string>> VergabenJeSchluessel(IEnumerable<Vergabe> vergaben) =>
        vergaben.GroupBy(v => v.Schluessel).ToDictionary(g => g.Key, g => g.Select(v => v.Nummer).Distinct().OrderBy(n => n).ToList());

    private static string Liste(IEnumerable<FaHierarchyNode> knoten, Dictionary<PositionsSchluessel, List<string>> vergaben) =>
        string.Join("; ", knoten.Select(n => $"{PositionsSchluessel.Von(n)} ({n.Artnr}) in {string.Join(", ", vergaben[PositionsSchluessel.Von(n)])}"));

    private BeschichtungsauftragPosition Snapshot(FaHierarchyNode n)
    {
        var p = new BeschichtungsauftragPosition
        {
            HauptFA = n.HauptFA, VaterFA = n.VaterFA, SubFA = n.SubFA, Position = n.Position,
            HauptArtnr = n.HauptArtnr, Artnr = n.Artnr, Matchcode = n.Matchcode,
            Menge = n.Sollmenge, Breite = n.Breite, Hoehe = n.Hoehe, Tiefe = n.Tiefe,
        };
        Erstellt(p);
        return p;
    }

    private BeschichtungsauftragPosition Kopie(BeschichtungsauftragPosition u)
    {
        var p = new BeschichtungsauftragPosition
        {
            HauptFA = u.HauptFA, VaterFA = u.VaterFA, SubFA = u.SubFA, Position = u.Position,
            HauptArtnr = u.HauptArtnr, Artnr = u.Artnr, Matchcode = u.Matchcode,
            Menge = u.Menge, Breite = u.Breite, Hoehe = u.Hoehe, Tiefe = u.Tiefe,
        };
        Erstellt(p);
        return p;
    }

    private void Erstellt(AuditableEntity e)
    {
        e.CreatedAt = Jetzt;
        e.CreatedBy = _user.GetDisplayName();
        e.CreatedByWindows = _user.GetWindowsUserName();
    }

    private void Geaendert(AuditableEntity e, DateTime jetzt)
    {
        e.ModifiedAt = jetzt;
        e.ModifiedBy = _user.GetDisplayName();
        e.ModifiedByWindows = _user.GetWindowsUserName();
    }

    private static string? Leer(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private static string Anzeige(BeschichtungsauftragStatus s) => s switch
    {
        BeschichtungsauftragStatus.Uebergeben => "Übergeben",
        _ => s.ToString(),
    };

    private static BeschichtungsauftragErgebnis Fehler(string text)
    {
        var r = new BeschichtungsauftragErgebnis();
        r.Fehler.Add(text);
        return r;
    }
}
