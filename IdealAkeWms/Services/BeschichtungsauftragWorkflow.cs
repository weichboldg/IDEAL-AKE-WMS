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
/// <remarks>v1.99.41 A9: <c>CreditorId</c> = Lackierer aus den Kreditoren (aktiv + IsCoater, Server prueft); gesetzt =&gt;
/// <c>Dienstleister</c> = Kreditor-Name, sonst gilt der Freitext (Rueckfall).</remarks>
public sealed record KopfDaten(string? Dienstleister, string? Ral, DateTime? StartBeschichtung, DateTime? BeschichtenRetour,
    int? CreditorId = null);

/// <summary>v1.99.41 (A1-A4): Auswahl aus der Vorschlagsliste — angehakte Positionen (Liste), Artnr (Summiert) und freie Positionen.</summary>
public sealed record VorschlagAuswahl(IReadOnlyCollection<PositionsSchluessel> Schluessel, IReadOnlyCollection<string> ArtnrSummiert,
    IReadOnlyCollection<FreiePosition> Frei)
{
    public bool IstLeer => Schluessel.Count == 0 && ArtnrSummiert.Count == 0 && Frei.Count == 0;
}

/// <summary>v1.99.41 (A4/A7): freie Position ohne FA — Artikel-Id aus der Artikelsuche und Menge (&gt; 0).</summary>
public sealed record FreiePosition(int ArticleId, decimal Menge);

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
            (BeschichtungsauftragStatus.Uebergeben, BeschichtungsauftragStatus.Versendet),
            (BeschichtungsauftragStatus.Versendet, BeschichtungsauftragStatus.Erledigt),
            (BeschichtungsauftragStatus.Entwurf, BeschichtungsauftragStatus.Storniert),
        };

    private readonly IBeschichtungsauftragRepository _repo;
    private readonly FaHierarchyNodeRepository _nodes;
    private readonly IFaHierarchyOrderInfoRepository _infos;
    private readonly Beschichtung.BeschichtungFlacheQuelle _flach;
    private readonly ICurrentUserService _user;
    private readonly TimeProvider _time;
    private readonly ILogger<BeschichtungsauftragWorkflow> _logger;

    public BeschichtungsauftragWorkflow(
        IBeschichtungsauftragRepository repo,
        FaHierarchyNodeRepository nodes,
        IFaHierarchyOrderInfoRepository infos,
        Beschichtung.BeschichtungFlacheQuelle flach,
        ICurrentUserService user,
        TimeProvider time,
        ILogger<BeschichtungsauftragWorkflow> logger)
    {
        _repo = repo;
        _nodes = nodes;
        _infos = infos;
        _flach = flach;
        _user = user;
        _time = time;
        _logger = logger;
    }

    private DateTime Jetzt => _time.GetLocalNow().DateTime;

    /// <summary>v1.99.34: Positionen eines HauptFA je Quelle — flach (AKE) aus der WA-Stueckliste, sonst die ungecachten Knoten.</summary>
    private async Task<List<FaHierarchyNode>> KnotenAsync(int hauptFa) => await _flach.IstFlachAsync()
        ? (await _flach.LadenAsync(new[] { hauptFa })).Knoten
        : await _nodes.GetByHauptFaAsync(hauptFa);

    // ===================== Anlegen (Anforderungen 2, 4, 6, 7, 8) =====================

    /// <summary>Legt einen Erstauftrag fuer den HauptFA an. <paramref name="auswahl"/> null oder leer (v1.99.65 B1) = alle aktuell
    /// beschichteten FREIEN Positionen, offen; sonst (v1.99.41 A3) genau die angehakten Positionen, Summiert-Artnr (= ihre freien
    /// Positionen) und freien Positionen ohne FA, verpackt. Teilweise ungueltige Auswahl = nichts angelegt (keine Nummer verbraucht).</summary>
    public Task<BeschichtungsauftragErgebnis> AnlegenAsync(int hauptFa, VorschlagAuswahl? auswahl = null) =>
        MitNummernVersuchenAsync(serializable: true, async () =>
        {
            var r = new BeschichtungsauftragErgebnis();
            var alle = auswahl is null || auswahl.IstLeer;
            var flach = await _flach.IstFlachAsync();
            var flachePositionen = flach ? await _flach.LadenAsync(new[] { hauptFa }) : null;
            var beschichtet = (flachePositionen?.Knoten ?? await _nodes.GetByHauptFaAsync(hauptFa)).Where(n => n.Beschichtet).ToList();
            var positionen = alle
                ? AlleFreien(hauptFa, beschichtet, flach, VergabenJeSchluessel(await _repo.GetVergabenAsync(hauptFa)), r)
                : await AuswahlAsync(hauptFa, beschichtet, auswahl!, r);
            if (!r.Erfolg) return r;

            var auftrag = new Beschichtungsauftrag { HauptFA = hauptFa, Status = BeschichtungsauftragStatus.Entwurf };
            Erstellt(auftrag);
            if (flachePositionen is not null)
                // AKE (Freigabe-Antwort 11): keine PPS-View; Start = berechneter Termin, Dienstleister/RAL/Retour bleiben leer.
                auftrag.StartBeschichtung = flachePositionen.Auftraege.Count == 1
                    ? await _flach.StartBeschichtungAsync(flachePositionen.Auftraege[0]) : null;
            else
            {
                KopfVorbelegen(auftrag, await _infos.GetByHauptFaAsync(hauptFa), r);
                KreditorVorbelegen(auftrag, await _repo.GetBeschichterAsync(), r);
            }
            // A3: angehakte Auswahl ist verpackt (Haken = Auswahl). v1.99.65 B1: ohne Auswahl alle freien offen — abgehakt wird
            // im Entwurf. Spaeter hinzugefuegte Positionen sind ebenfalls offen.
            foreach (var p in positionen) { p.Verpackt = !alle; auftrag.Positionen.Add(p); }
            await NummerVergebenUndSpeichernAsync(auftrag);
            r.Auftrag = auftrag;
            return r;
        });

    /// <summary>Teil 3/4: alle freien beschichteten Positionen; bereits vergebene als Hinweis.</summary>
    private List<BeschichtungsauftragPosition> AlleFreien(int hauptFa, List<FaHierarchyNode> beschichtet, bool flach,
        Dictionary<PositionsSchluessel, List<string>> vergaben, BeschichtungsauftragErgebnis r)
    {
        if (beschichtet.Count == 0)
        {
            r.Fehler.Add(flach
                ? $"Für WA {hauptFa} gibt es keine offenen Lackierteil-Positionen in der Stückliste — kein Auftrag angelegt."
                : $"Für HauptFA {hauptFa} gibt es im Sage-Cache keine beschichteten Positionen — kein Auftrag angelegt.");
            return new();
        }
        MeldeDoppelteSchluessel(beschichtet, r);

        var vergeben = beschichtet.Where(n => vergaben.ContainsKey(PositionsSchluessel.Von(n))).ToList();
        var frei = beschichtet.Except(vergeben).ToList();
        if (frei.Count == 0)
        {
            r.Fehler.Add($"Alle {beschichtet.Count} beschichteten Positionen von HauptFA {hauptFa} sind bereits vergeben — kein " +
                         $"neuer Auftrag: {Liste(vergeben, vergaben)}");
            return new();
        }
        if (vergeben.Count > 0)
            r.Hinweise.Add($"{vergeben.Count} Positionen bereits in {string.Join(", ", vergeben.SelectMany(n => vergaben[PositionsSchluessel.Von(n)]).Distinct())} " +
                           $"vergeben, nicht übernommen: {Liste(vergeben, vergaben)}");
        return frei.Select(Snapshot).ToList();
    }

    /// <summary>v1.99.41 A2/A3: angehakte Schluessel + Summiert-Artnr (= alle ihre freien Positionen; keine mehr frei = Fehler) +
    /// freie Positionen ohne FA. Pruefung je Schluessel wie beim Hinzufuegen (<see cref="FreieKnotenPruefenAsync"/>).</summary>
    private async Task<List<BeschichtungsauftragPosition>> AuswahlAsync(int hauptFa, List<FaHierarchyNode> beschichtet,
        VorschlagAuswahl auswahl, BeschichtungsauftragErgebnis r)
    {
        var keys = auswahl.Schluessel.ToHashSet();
        if (auswahl.ArtnrSummiert.Count > 0)
        {
            var vergaben = VergabenJeSchluessel(await _repo.GetVergabenAsync(hauptFa));
            foreach (var artnr in auswahl.ArtnrSummiert.Distinct())
            {
                var frei = beschichtet.Where(n => n.Artnr == artnr).Select(PositionsSchluessel.Von).Where(k => !vergaben.ContainsKey(k)).ToList();
                if (frei.Count == 0) r.Fehler.Add($"Artnr {artnr}: keine freie Position mehr in HauptFA {hauptFa} (inzwischen vergeben oder nicht mehr im Cache).");
                keys.UnionWith(frei);
            }
        }
        var knoten = keys.Count == 0 ? new() : await FreieKnotenPruefenAsync(hauptFa, keys, new HashSet<PositionsSchluessel>(), r);
        var freie = await FreiePositionenAsync(hauptFa, auswahl.Frei, r);
        return r.Erfolg ? keys.Select(k => Snapshot(knoten[k])).Concat(freie).ToList() : new();
    }

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
                CreditorId = ursprung.CreditorId,
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

    // ===================== Bearbeiten bis vor Versendet (Anforderung 10; v1.99.41 A7, A8) =====================

    /// <summary>Status, in denen Positionen (Haken, Menge, Entfernen, Hinzufuegen) aenderbar sind (A8). v1.99.65: Entfernen in
    /// Uebergeben nur fuer nicht verpackte Positionen (<see cref="Entfernbar"/>).</summary>
    public static bool PositionenAenderbar(BeschichtungsauftragStatus s) =>
        s is BeschichtungsauftragStatus.Entwurf or BeschichtungsauftragStatus.Uebergeben;

    /// <summary>v1.99.65 (Antwort R-2): Entfernen im Entwurf immer, in Uebergeben nur mit gespeichertem Haken „offen“. Eine Regel
    /// fuer Server (<see cref="BearbeitenAsync"/>) und Detailseite.</summary>
    public static bool Entfernbar(BeschichtungsauftragStatus s, bool verpackt) =>
        s == BeschichtungsauftragStatus.Entwurf || (s == BeschichtungsauftragStatus.Uebergeben && !verpackt);

    /// <summary>
    /// Ein Speichern am Auftrag (A8): Kopf (nur Entwurf, Rueckfrage 10 — <paramref name="kopf"/> null = unveraendert), Mengen,
    /// Entfernen, Haken (<paramref name="verpacktIds"/> = angehakte Ids; null = Haken unveraendert), Kandidaten
    /// (<paramref name="hinzu"/>) und freie Positionen (<paramref name="frei"/>). Alles oder nichts: ein Fehler lehnt die ganze
    /// Aenderung ab. Hinzufuegen prueft die Vergabe in der Serializable-Transaktion (wie die Anlage).
    /// v1.99.67 F1: <paramref name="ziel"/> = der Statuswechsel, der nach dem Speichern folgt; bei Uebergeben wird abgelehnt, wenn
    /// danach 0 Positionen uebrig blieben — vor dem Speichern, damit nichts entfernt ist.
    /// </summary>
    public async Task<BeschichtungsauftragErgebnis> BearbeitenAsync(int id, KopfDaten? kopf,
        IReadOnlyDictionary<int, decimal> mengen, IReadOnlyCollection<int> entfernteIds, byte[] rowVersion,
        IReadOnlyCollection<int>? verpacktIds = null, IReadOnlyCollection<PositionsSchluessel>? hinzu = null,
        IReadOnlyCollection<FreiePosition>? frei = null, int? kreditorNachtragen = null, BeschichtungsauftragStatus? ziel = null)
    {
        var r = new BeschichtungsauftragErgebnis();
        var a = await LadenAsync(id, r);
        if (a is null) return r;
        if (!PositionenAenderbar(a.Status))
            return Fehler($"Positionen sind nur in Entwurf und Übergeben änderbar — {a.Nummer} steht auf {Anzeige(a.Status)}; " +
                          "möglich ist nur noch Statuswechsel und Bemerkung.");
        if (kopf is not null && a.Status != BeschichtungsauftragStatus.Entwurf)
            return Fehler($"Der Kopf (Dienstleister, RAL, Termine) ist nur im Entwurf änderbar — {a.Nummer} steht auf {Anzeige(a.Status)}.");

        foreach (var pid in mengen.Keys.Concat(entfernteIds).Concat(verpacktIds ?? Array.Empty<int>()).Distinct()
                     .Where(pid => a.Positionen.All(p => p.Id != pid)))
            r.Fehler.Add($"Position {pid} gehört nicht zu {a.Nummer}.");
        foreach (var (pid, menge) in mengen.Where(m => m.Value <= 0))
            r.Fehler.Add($"Menge muss größer 0 sein (Position {pid}: {menge}) — zum Weglassen die Position entfernen.");
        foreach (var p in a.Positionen.Where(p => entfernteIds.Contains(p.Id)))
        {
            // v1.99.65 B4: entweder verpackt oder entfernen — Widerspruch melden statt still entscheiden
            if (verpacktIds?.Contains(p.Id) == true)
                r.Fehler.Add($"Position {PositionBezeichnung(p)}: „Verpackt“ und „Entfernen“ zugleich angehakt — nur eines wählen.");
            // v1.99.65 (Antwort R-2): ab Uebergeben nur nicht verpackte entfernbar; massgeblich ist der GESPEICHERTE Haken
            else if (!Entfernbar(a.Status, p.Verpackt))
                r.Fehler.Add($"Position {PositionBezeichnung(p)} ist verpackt — in {Anzeige(a.Status)} lassen sich nur nicht verpackte " +
                             $"Positionen entfernen ({a.Nummer}).");
        }
        if (ziel == BeschichtungsauftragStatus.Uebergeben
            && a.Positionen.Count(p => !entfernteIds.Contains(p.Id)) + (hinzu?.Count ?? 0) + (frei?.Count ?? 0) == 0)
            r.Fehler.Add(OhnePositionen(a));
        if (kopf is not null)
            foreach (var (feld, wert) in new[] { ("Dienstleister", kopf.Dienstleister), ("RAL", kopf.Ral) })
                if (wert?.Trim().Length > MaxKopfTextLaenge)
                    r.Fehler.Add($"{feld} höchstens {MaxKopfTextLaenge} Zeichen.");
        Creditor? kreditor = null;
        // v1.99.42 (Befund 15:05 b): in Uebergeben ist der Kopf fest, der Lackierer aber nachtragbar, solange keiner gesetzt ist (Altauftraege).
        if (kopf is null && kreditorNachtragen is int nid)
        {
            if (a.CreditorId is not null)
                r.Fehler.Add($"{a.Nummer} hat schon einen Kreditor — nachtragen geht nur, solange keiner gesetzt ist.");
            else if ((kreditor = (await _repo.GetBeschichterAsync()).FirstOrDefault(c => c.Id == nid)) is null)
                r.Fehler.Add($"Kreditor {nid} ist kein aktiver Beschichter — Lackierer aus der Liste wählen oder in den Kreditoren „Beschichter“ setzen.");
        }
        if (kopf?.CreditorId is int cid)
        {
            kreditor = (await _repo.GetBeschichterAsync()).FirstOrDefault(c => c.Id == cid);
            if (kreditor is null)
                r.Fehler.Add($"Kreditor {cid} ist kein aktiver Beschichter — Lackierer aus der Liste wählen oder in den Kreditoren „Beschichter“ setzen.");
        }
        var neueFreie = await FreiePositionenAsync(a.HauptFA, frei ?? Array.Empty<FreiePosition>(), r);
        if (!r.Erfolg) return r;

        var keys = (hinzu ?? Array.Empty<PositionsSchluessel>()).Distinct().ToList();
        void Anwenden(IReadOnlyList<BeschichtungsauftragPosition> neuAusSchluessel)
        {
            var jetzt = Jetzt;
            if (kopf is not null)
            {
                a.CreditorId = kreditor?.Id;
                a.Dienstleister = kreditor is not null ? Leer(kreditor.Name) ?? kreditor.ShortName : Leer(kopf.Dienstleister);
                a.Ral = Leer(kopf.Ral);
                a.StartBeschichtung = kopf.StartBeschichtung;
                a.BeschichtenRetour = kopf.BeschichtenRetour;
            }
            else if (kreditor is not null)
            {
                a.CreditorId = kreditor.Id;
                a.Dienstleister = Leer(kreditor.Name) ?? kreditor.ShortName;
            }
            foreach (var p in a.Positionen)
            {
                var geaendert = false;
                if (mengen.TryGetValue(p.Id, out var m) && m != p.Menge) { p.Menge = m; geaendert = true; }
                if (verpacktIds is not null && verpacktIds.Contains(p.Id) != p.Verpackt) { p.Verpackt = !p.Verpackt; geaendert = true; }
                if (geaendert) Geaendert(p, jetzt);
            }
            foreach (var p in a.Positionen.Where(p => entfernteIds.Contains(p.Id)).ToList())
            {
                a.Positionen.Remove(p);
                _repo.Remove(p);   // geloescht = sofort frei (Anforderung 6)
            }
            foreach (var p in neuAusSchluessel.Concat(neueFreie)) a.Positionen.Add(p);   // neu = offen (Verpackt false)
            Geaendert(a, jetzt);
        }
        // Hinweise (z. B. Masse 0) ueberleben das Speichern
        BeschichtungsauftragErgebnis MitHinweisen(BeschichtungsauftragErgebnis e)
        {
            if (!ReferenceEquals(e, r)) e.Hinweise.InsertRange(0, r.Hinweise);
            return e;
        }

        if (keys.Count == 0)
        {
            Anwenden(Array.Empty<BeschichtungsauftragPosition>());
            return MitHinweisen(await SpeichernAsync(a, rowVersion));
        }
        if (a.UrsprungsauftragId is int)
        {
            var kopien = await ReklamationsKopienAsync(a, keys, r);
            if (!r.Erfolg) return r;
            Anwenden(kopien);
            return MitHinweisen(await SpeichernAsync(a, rowVersion));
        }
        // Fehlerbehandlung AUSSERHALB der Transaktion: scheitert ein Lesen oder Speichern darin (z. B. Deadlock-Opfer), ist
        // die SQL-Server-Transaktion beendet — ein Commit danach wuerde selbst werfen (QA I-1).
        return MitHinweisen(await MitFehlerbehandlungAsync(a.Nummer, () => _repo.InSerializableTransactionAsync(async () =>
        {
            var knoten = await FreieKnotenPruefenAsync(a.HauptFA, keys, EigeneSchluessel(a), r);
            if (!r.Erfolg) return r;
            Anwenden(keys.Select(k => Snapshot(knoten[k])).ToList());
            await SpeichernRohAsync(a, rowVersion);
            return new BeschichtungsauftragErgebnis { Auftrag = a };
        })));
    }

    /// <summary>Kandidaten fuer das Hinzufuegen (Entwurf und Uebergeben) — Erstauftrag: freie beschichtete Positionen des
    /// HauptFA aus dem aktuellen Cache; Reklamation: Positionen des direkten Ursprungs, die noch nicht enthalten sind.
    /// Sonst oder nicht gefunden: leer.</summary>
    public async Task<List<PositionsKandidat>> GetHinzufuegbarePositionenAsync(int id)
    {
        var a = await _repo.GetMitPositionenAsync(id);
        if (a is null || !PositionenAenderbar(a.Status)) return new();
        var eigene = EigeneSchluessel(a);

        if (a.UrsprungsauftragId is int uid)
        {
            var u = await _repo.GetMitPositionenAsync(uid);
            return (u?.Positionen ?? new List<BeschichtungsauftragPosition>())
                .Where(p => !p.OhneFa && !eigene.Contains(PositionsSchluessel.Von(p)))
                .OrderBy(p => p.Position ?? int.MaxValue).ThenBy(p => p.PositionText, new NaturalPositionComparer())
                .DistinctBy(PositionsSchluessel.Von)
                .Select(p => new PositionsKandidat(PositionsSchluessel.Von(p), p.HauptArtnr, p.Artnr, p.Matchcode, p.Menge, p.Breite, p.Hoehe, p.Tiefe))
                .ToList();
        }

        var vergaben = VergabenJeSchluessel(await _repo.GetVergabenAsync(a.HauptFA));
        return (await KnotenAsync(a.HauptFA))
            .Where(n => n.Beschichtet && !vergaben.ContainsKey(PositionsSchluessel.Von(n)))
            // v1.99.36 (Nachtrag 4 Punkt 3, QA-F1): Auswahl natuerlich nach Ebenen (51.5.2 vor 51.10)
            .OrderBy(n => n.Position ?? int.MaxValue).ThenBy(n => n.PositionText, new NaturalPositionComparer())
            .DistinctBy(PositionsSchluessel.Von)
            .Select(n => new PositionsKandidat(PositionsSchluessel.Von(n), n.HauptArtnr, n.Artnr, n.Matchcode, n.Sollmenge, n.Breite, n.Hoehe, n.Tiefe))
            .ToList();
    }

    /// <summary>Fuegt Positionen hinzu (Herkunftsschluessel; HauptFA aus dem Auftrag). Teilweise gueltige Auswahl = ganz abgelehnt.</summary>
    public Task<BeschichtungsauftragErgebnis> PositionenHinzufuegenAsync(int id, IReadOnlyCollection<PositionsSchluessel> schluessel, byte[] rowVersion) =>
        schluessel.Count == 0
            ? Task.FromResult(Fehler("Positionen hinzufügen: keine Positionen ausgewählt."))
            : BearbeitenAsync(id, null, new Dictionary<int, decimal>(), Array.Empty<int>(), rowVersion, hinzu: schluessel);

    /// <summary>Schluessel der FA-Positionen eines Auftrags — freie Positionen haben keinen bedeutsamen Schluessel (A7).</summary>
    private static HashSet<PositionsSchluessel> EigeneSchluessel(Beschichtungsauftrag a) =>
        a.Positionen.Where(p => !p.OhneFa).Select(PositionsSchluessel.Von).ToHashSet();

    /// <summary>Gemeinsame Pruefung fuer Anlage (A3) und Hinzufuegen: je Schluessel im Cache, beschichtet, nicht vergeben, nicht
    /// schon im Auftrag. Fehler in <paramref name="r"/>; Rueckgabe = Knoten je Schluessel. Nur in der Serializable-Transaktion aufrufen.</summary>
    private async Task<Dictionary<PositionsSchluessel, FaHierarchyNode>> FreieKnotenPruefenAsync(int hauptFa,
        IReadOnlyCollection<PositionsSchluessel> keys, IReadOnlySet<PositionsSchluessel> eigene, BeschichtungsauftragErgebnis r)
    {
        var knoten = (await KnotenAsync(hauptFa)).GroupBy(PositionsSchluessel.Von).ToDictionary(g => g.Key, g => g.First());
        var vergaben = VergabenJeSchluessel(await _repo.GetVergabenAsync(hauptFa));
        foreach (var k in keys)
        {
            if (eigene.Contains(k))
                r.Fehler.Add($"Position {k} steht bereits in diesem Auftrag.");
            else if (!knoten.TryGetValue(k, out var n))
                r.Fehler.Add($"Position {k} ist nicht im Cache von HauptFA {hauptFa} (nicht mehr vorhanden oder anderer HauptFA).");
            else if (!n.Beschichtet)
                r.Fehler.Add($"Position {k} ist nicht beschichtet.");
            else if (vergaben.TryGetValue(k, out var nummern))
                r.Fehler.Add($"Position {k} ist bereits vergeben in {string.Join(", ", nummern)}.");
        }
        return knoten;
    }

    /// <summary>Reklamations-Entwurf: nur Ursprungspositionen, Kopie aus dem Ursprung, keine Vergabe-Pruefung (Antworten 14, 15).</summary>
    private async Task<List<BeschichtungsauftragPosition>> ReklamationsKopienAsync(Beschichtungsauftrag a,
        IReadOnlyCollection<PositionsSchluessel> keys, BeschichtungsauftragErgebnis r)
    {
        var u = await _repo.GetMitPositionenAsync(a.UrsprungsauftragId!.Value);
        var ausUrsprung = (u?.Positionen ?? new List<BeschichtungsauftragPosition>()).Where(p => !p.OhneFa)
            .GroupBy(PositionsSchluessel.Von).ToDictionary(g => g.Key, g => g.First());
        var eigene = EigeneSchluessel(a);
        foreach (var k in keys)
        {
            if (eigene.Contains(k)) r.Fehler.Add($"Position {k} steht bereits in diesem Auftrag ({a.Nummer}).");
            else if (!ausUrsprung.ContainsKey(k))
                r.Fehler.Add($"Im Reklamationsauftrag nur Positionen des Ursprungs {u?.Nummer} — Position {k} über einen neuen Auftrag vergeben.");
        }
        return r.Erfolg ? keys.Select(k => Kopie(ausUrsprung[k])).ToList() : new();
    }

    /// <summary>
    /// Freie Positionen ohne FA (A7): Artikel aus dem Stamm, Menge &gt; 0, nie Vergabe. Masse (Rueckfrage 9): flache Quelle (AKE) =
    /// <see cref="ArticleSageDetail"/> cm × 10 wie <see cref="Beschichtung.BeschichtungFlacheQuelle"/>-Projektion; sonst (IDEAL, bis P-3
    /// bestaetigt) 0 mit Hinweis und Log-Warnung. Fehlende Masse = 0 mit Hinweis — nie still.
    /// </summary>
    private async Task<List<BeschichtungsauftragPosition>> FreiePositionenAsync(int hauptFa, IReadOnlyCollection<FreiePosition> frei,
        BeschichtungsauftragErgebnis r)
    {
        if (frei.Count == 0) return new();
        foreach (var f in frei.Where(f => f.Menge <= 0))
            r.Fehler.Add($"Position ohne FA: Menge muss größer 0 sein (Artikel {f.ArticleId}: {f.Menge}).");
        var artikel = (await _repo.GetArtikelMitMassenAsync(frei.Select(f => f.ArticleId).Distinct().ToList())).ToDictionary(x => x.Id);
        foreach (var f in frei.Where(f => !artikel.ContainsKey(f.ArticleId)))
            r.Fehler.Add($"Position ohne FA: Artikel {f.ArticleId} nicht gefunden.");
        if (!r.Erfolg) return new();

        var flach = await _flach.IstFlachAsync();
        var ohneMasse = new List<string>();
        var liste = frei.Select(f =>
        {
            var x = artikel[f.ArticleId];
            var d = flach ? x.SageDetail : null;
            if (d?.Breite is null && d?.Hoehe is null && d?.Laenge is null) ohneMasse.Add(x.ArticleNumber);
            var p = new BeschichtungsauftragPosition
            {
                HauptFA = hauptFa, VaterFA = null, SubFA = 0, Position = null, PositionText = null, HauptArtnr = null,
                Artnr = x.ArticleNumber, Matchcode = x.Matchcode, Menge = f.Menge, OhneFa = true,
                Breite = (d?.Breite ?? 0m) * 10m, Hoehe = (d?.Hoehe ?? 0m) * 10m, Tiefe = (d?.Laenge ?? 0m) * 10m,
            };
            Erstellt(p);
            return p;
        }).ToList();
        if (ohneMasse.Count > 0)
        {
            var text = flach
                ? $"Maße 0 bei Positionen ohne FA (Artikel ohne Sage-Maße): {string.Join(", ", ohneMasse.Distinct())}."
                : $"Maße 0 bei Positionen ohne FA (IDEAL: Maße aus dem Artikelstamm erst nach Prüfschritt P-3): {string.Join(", ", ohneMasse.Distinct())}.";
            r.Hinweise.Add(text);
            _logger.LogWarning("Beschichtungsauftrag HauptFA {HauptFa}: {Text}", hauptFa, text);
        }
        return liste;
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
                          "Entwurf → Übergeben → Versendet → Erledigt sowie Entwurf → Storniert.");
        if (ziel == BeschichtungsauftragStatus.Uebergeben && string.IsNullOrWhiteSpace(a.Dienstleister))
            return Fehler("Dienstleister fehlt — vor dem Uebergeben am Auftrag eintragen.");
        if (ziel == BeschichtungsauftragStatus.Uebergeben && a.Positionen.Count == 0)
            return Fehler(OhnePositionen(a));
        if (ziel == BeschichtungsauftragStatus.Versendet)
        {
            // A6: Versendet nur mit mindestens einer Position und allen Haken „verpackt“ — fehlende werden genannt, nie still uebergangen.
            if (a.Positionen.Count == 0)
                return Fehler($"{a.Nummer} hat keine Positionen — Versendet nicht möglich.");
            var offen = a.Positionen.Where(p => !p.Verpackt).ToList();
            if (offen.Count > 0)
                return Fehler($"Versendet nur, wenn alle Positionen verpackt sind — nicht verpackt ({offen.Count}): " +
                              string.Join(", ", offen.Select(PositionBezeichnung)) + ". Bitte anhaken oder entfernen.");
        }

        var jetzt = Jetzt;
        var userId = _user.GetCurrentAppUserId();
        a.Status = ziel;
        switch (ziel)
        {
            case BeschichtungsauftragStatus.Uebergeben: a.UebergebenAt = jetzt; a.UebergebenByUserId = userId; break;
            case BeschichtungsauftragStatus.Versendet: a.VersendetAt = jetzt; a.VersendetByUserId = userId; break;
            case BeschichtungsauftragStatus.Erledigt: a.ErledigtAt = jetzt; a.ErledigtByUserId = userId; break;
            case BeschichtungsauftragStatus.Storniert: a.StorniertAt = jetzt; a.StorniertByUserId = userId; break;
        }
        Geaendert(a, jetzt);
        return await SpeichernAsync(a, rowVersion);
    }

    // ===================== Bausteine =====================

    /// <summary>v1.99.67 F1 (Gerald 2026-11-25): Uebergeben ohne Positionen ergibt keinen Sinn — der Weg ist Stornieren.</summary>
    private static string OhnePositionen(Beschichtungsauftrag a) =>
        $"Übergeben mit 0 Positionen ist nicht möglich ({a.Nummer}) — nichts gespeichert. Ist nichts zu beschichten, " +
        "den Entwurf stornieren; die Positionen werden wieder frei.";

    /// <summary>Kurzbezeichnung einer Position fuer Meldungen, Uebergeben-Fenster und Aria-Labels: Artnr, sonst Matchcode; freie
    /// Positionen mit „(ohne FA)“. v1.99.67 B-1: mit VaterFA, wenn gesetzt — Artnr + Pos. allein ist an IDEAL nicht eindeutig.</summary>
    internal static string PositionBezeichnung(BeschichtungsauftragPosition p)
    {
        var name = string.IsNullOrWhiteSpace(p.Artnr) ? p.Matchcode ?? $"#{p.Id}" : p.Artnr;
        if (p.OhneFa) return name + " (ohne FA)";
        var pos = p.Position is int nr ? $"Pos. {nr}" : string.IsNullOrWhiteSpace(p.PositionText) ? null : $"Pos. {p.PositionText}";
        var teile = new[] { pos, p.VaterFA is int v ? $"VaterFA {v}" : null }.Where(t => t is not null).ToList();
        return teile.Count == 0 ? name : $"{name} ({string.Join(", ", teile)})";
    }

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
        // v1.88.0: Wiederhol-Schleife im gemeinsamen Helfer (auch fuer Kommissionierauftraege „KO“).
        var (ok, ergebnis) = await Nummernkreis.MitVersuchenAsync(MaxNummerVersuche,
            () => serializable ? _repo.InSerializableTransactionAsync(anlage) : anlage(),
            _repo.VerwerfeAenderungen, _logger, "Beschichtungsauftrag anlegen");
        return ok
            ? ergebnis!
            : Fehler($"Konnte nach {MaxNummerVersuche} Versuchen keine freie Nummer vergeben (gleichzeitige Anlage oder " +
                     "Datenbankfehler, Details im Log) — bitte erneut versuchen.");
    }

    private async Task NummerVergebenUndSpeichernAsync(Beschichtungsauftrag auftrag)
    {
        var jahr = Jetzt.Year;
        var lfd = await _repo.GetMaxLaufendeNummerAsync(jahr) + 1;
        auftrag.Jahr = jahr;
        auftrag.LaufendeNummer = lfd;
        auftrag.Nummer = Nummernkreis.Format("BA", jahr, lfd);
        _repo.Add(auftrag);
        await _repo.SaveChangesAsync();
    }

    private static bool IstDeadlock(Exception ex) => Nummernkreis.IstDeadlock(ex);

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

    /// <summary>v1.99.41 A10 (Rueckfrage 17): Sage-Dienstleistertext = Kurzname GENAU eines aktiven Beschichter-Kreditors
    /// (Vergleich wie <see cref="CreditorAssignment"/>: getrimmt, ohne Gross/Klein) =&gt; CreditorId + Name. Sonst nur der Text mit
    /// Hinweis — nie still raten.</summary>
    internal static void KreditorVorbelegen(Beschichtungsauftrag a, IReadOnlyCollection<Creditor> beschichter, BeschichtungsauftragErgebnis r)
    {
        var text = a.Dienstleister?.Trim();
        if (string.IsNullOrEmpty(text)) return;
        var treffer = beschichter.Where(c => string.Equals(c.ShortName?.Trim(), text, StringComparison.OrdinalIgnoreCase)).ToList();
        if (treffer.Count == 1)
        {
            a.CreditorId = treffer[0].Id;
            a.Dienstleister = Leer(treffer[0].Name) ?? text;
            return;
        }
        r.Hinweise.Add(treffer.Count == 0
            ? $"Kein Beschichter-Kreditor zu „{text}“ — Kreditor wählen oder Kurzname pflegen."
            : $"Mehrere Beschichter-Kreditoren mit Kurzname „{text}“ — Kreditor wählen.");
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
            HauptFA = n.HauptFA, VaterFA = n.VaterFA, SubFA = n.SubFA, Position = n.Position, PositionText = n.PositionText,
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
            HauptFA = u.HauptFA, VaterFA = u.VaterFA, SubFA = u.SubFA, Position = u.Position, PositionText = u.PositionText,
            HauptArtnr = u.HauptArtnr, Artnr = u.Artnr, Matchcode = u.Matchcode,
            Menge = u.Menge, Breite = u.Breite, Hoehe = u.Hoehe, Tiefe = u.Tiefe,
            OhneFa = u.OhneFa,   // A7; Verpackt bewusst false — die Reklamationsteile werden neu verpackt
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

    /// <summary>Erlaubter Statuswechsel? (Teil 4 zeigt nur diese Knoepfe; der Workflow prueft ohnehin.)</summary>
    public static bool IstErlaubt(BeschichtungsauftragStatus von, BeschichtungsauftragStatus nach) => ErlaubteUebergaenge.Contains((von, nach));

    /// <summary>Anzeigetext eines Status (Umlaut statt Enum-Name).</summary>
    public static string Anzeige(BeschichtungsauftragStatus s) => s switch
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
