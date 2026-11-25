using System.Globalization;
using IdealAkeWms.Data;
using IdealAkeWms.Models;
using IdealAkeWms.Services;
using IdealAkeWms.Services.SyncLogger;
using Microsoft.EntityFrameworkCore;

namespace IDEALAKEWMSService.Services;

/// <summary>
/// v1.99.71 (Spec 2026-11-24-lagerplatz-anlage-wms-nach-sage F3/F3a/F5, ADR 0036): ein Sende-Lauf der im WMS erfassten Lager und Lagerplaetze
/// nach Sage. Zuerst die Lager, danach die Plaetze (in Reihenfolge der Outbox-Id); Plaetze eines noch nicht bestaetigten Lagers warten.
/// Status VOR dem Sage-Aufruf auf Gesendet (+1 Versuch). Hängende Gesendet-Eintraege werden nach <c>stuckMinutes</c> erneut verarbeitet –
/// bewusste Abweichung vom Hausmuster „nie blind neu senden“, weil der Sage-Befehl idempotent ist (H12).
/// Fachlicher Fehler = Fehler am Eintrag, weiter mit dem naechsten, keine Mail. Technischer Fehler = Eintrag zurueck auf Offen, Rest des
/// Takts ueberspringen; Protokoll-Lauf nur bei NEUEM Text (S7, Mail entscheidet der Worker). Voraussetzung: genau EINE Dienst-Instanz.
/// </summary>
public class SageLagerplatzAnlageService
{
    internal const string TechnischPrefix = "Letzter Versuch fehlgeschlagen: ";
    private const string Actor = SageLagerplatzAnlage.ServiceActor;
    private const int MaxFehlerLaenge = 1000;
    private const int MaxDescriptionLength = 200;   // wie LagerplatzSyncService

    private readonly ApplicationDbContext _ctx;
    private readonly ISageLagerplatzAnlage _sage;
    private readonly ILogger<SageLagerplatzAnlageService> _logger;
    private readonly ISyncLogger _syncLogger;

    public SageLagerplatzAnlageService(ApplicationDbContext ctx, ISageLagerplatzAnlage sage, ILogger<SageLagerplatzAnlageService> logger,
        ISyncLogger syncLogger)
    {
        _ctx = ctx;
        _sage = sage;
        _logger = logger;
        _syncLogger = syncLogger;
    }

    /// <param name="stufe">Wert von <c>SageLagerAnlageLagertechniken</c> (Neuanlage nur fuer Techniken der Stufe, H4).</param>
    /// <param name="letzterTechnischerFehler">vom Worker gemerkter Text des letzten technischen Fehlers (S7).</param>
    /// <returns>Text des technischen Fehlers dieses Takts, sonst null (Worker setzt damit seinen Merker und mailt nur beim Wechsel).</returns>
    public async Task<string?> RunOnceAsync(int stuckMinutes, string? stufe, string? letzterTechnischerFehler, CancellationToken ct = default)
    {
        var grenze = DateTime.Now.AddMinutes(-Math.Max(1, stuckMinutes));
        var lagerKandidaten = await KandidatenAsync(SageStammdatenOutbox.TypLagerort, grenze, ct);
        var areaIds = Ids(lagerKandidaten);
        var areas = await _ctx.StorageAreas.Where(a => areaIds.Contains(a.Id) && a.IsActive).ToDictionaryAsync(a => a.Id, ct);
        var lagerArbeit = lagerKandidaten.Where(o => areas.ContainsKey(Id(o))).ToList();

        var platzKandidaten = await KandidatenAsync(SageStammdatenOutbox.TypLagerplatz, grenze, ct);
        var platzIds = Ids(platzKandidaten);
        var plaetze = await _ctx.StorageLocations.Include(l => l.StorageArea)
            .Where(l => platzIds.Contains(l.Id) && l.Source == StorageLocationSource.Manual && l.StorageAreaId != null && l.IsActive)
            .ToDictionaryAsync(l => l.Id, ct);
        var platzArbeit = platzKandidaten.Where(o => plaetze.ContainsKey(Id(o))).ToList();

        var wartend = await WartendeLagerAsync(ct);
        // kein Protokoll-Lauf je Takt ohne Arbeit; Plaetze eines wartenden Lagers sind keine Arbeit (S7), ausser das Lager wird jetzt gesendet
        if (lagerArbeit.Count == 0 && !platzArbeit.Any(o => !wartend.Contains(plaetze[Id(o)].StorageAreaId!.Value)))
            return null;

        var lauf = new Lauf(_syncLogger);
        try
        {
            foreach (var o in lagerArbeit)
            {
                if (ct.IsCancellationRequested) break;
                if (await LagerAsync(o, areas[Id(o)], stufe, lauf, ct) is { } technisch)
                    return await TechnischAsync(lauf, technisch, letzterTechnischerFehler, ct);
            }

            wartend = await WartendeLagerAsync(ct);
            foreach (var o in platzArbeit)
            {
                if (ct.IsCancellationRequested) break;
                var platz = plaetze[Id(o)];
                if (wartend.Contains(platz.StorageAreaId!.Value)) { lauf.Uebersprungen++; continue; }
                if (await PlatzAsync(o, platz, lauf, ct) is { } technisch)
                    return await TechnischAsync(lauf, technisch, letzterTechnischerFehler, ct);
            }

            if (lauf.Run is not null) await lauf.Run.FinishSuccessAsync(lauf.Counts(), ct: ct);
            return null;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Lagerplatz-Anlage-Lauf fehlgeschlagen.");
            var run = await lauf.RunAsync(ct);
            await run.LogErrorAsync(Kappen(ex.Message, 900), ct: ct);
            await run.FinishFailedAsync(Kappen(ex.Message, 900), counts: lauf.Counts(), ct: ct);
            throw;
        }
        finally
        {
            if (lauf.Run is not null) await lauf.Run.DisposeAsync();
        }
    }

    /// <returns>Text eines technischen Fehlers (Takt abbrechen) oder null.</returns>
    private async Task<string?> LagerAsync(SageStammdatenOutboxItem o, StorageArea area, string? stufe, Lauf lauf, CancellationToken ct)
    {
        Setze(o, SageBookingQueueStatus.Gesendet, o.LastError);
        o.AttemptCount++;
        await _ctx.SaveChangesAsync(ct);

        var erlaubt = SageLagerplatzAnlage.TechnikenDerStufe(stufe).Contains(area.Lagertechnik ?? "");
        SageLagerplatzAnlageErgebnis r;
        try
        {
            r = await _sage.LagerAnlegenAsync(area.Lagerkennung, area.Bezeichnung ?? "", area.Lagertechnik ?? "", erlaubt, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            var text = TechnischPrefix + $"Lager {area.Lagerkennung}: " + ex.GetBaseException().Message;
            Setze(o, SageBookingQueueStatus.Offen, text);
            await _ctx.SaveChangesAsync(ct);
            return text;
        }

        var run = await lauf.RunAsync(ct, r.SageDb);
        if (r.Art == SageLagerplatzAnlageArt.Abgelehnt)
        {
            var text = $"Lager {area.Lagerkennung} nicht in Sage angelegt: {r.Grund}";
            Setze(o, SageBookingQueueStatus.Fehler, r.Grund);
            lauf.LagerFehler++;
            await run.LogWarningAsync(Kappen(text, 900), reference: area.Lagerkennung, ct: ct);
        }
        else
        {
            await SageStammdatenOutbox.ErledigenAsync(_ctx, SageStammdatenOutbox.TypLagerort, area.Id, Actor, Actor, ct);
            if (r.Art == SageLagerplatzAnlageArt.Angelegt)
            {
                lauf.LagerAngelegt++;
                await run.LogInfoAsync($"Lager {area.Lagerkennung} in Sage angelegt (Basisplatz PlatzID {r.PlatzId})", reference: area.Lagerkennung, ct: ct);
            }
            else
            {
                lauf.LagerVorhanden++;
                await run.LogInfoAsync($"Lager {area.Lagerkennung} war in Sage schon vorhanden – übernommen", reference: area.Lagerkennung, ct: ct);
            }
        }
        await _ctx.SaveChangesAsync(ct);
        return null;
    }

    private async Task<string?> PlatzAsync(SageStammdatenOutboxItem o, StorageLocation platz, Lauf lauf, CancellationToken ct)
    {
        Setze(o, SageBookingQueueStatus.Gesendet, o.LastError);
        o.AttemptCount++;
        await _ctx.SaveChangesAsync(ct);

        var lager = platz.StorageArea!.Lagerkennung;
        SageLagerplatzAnlageErgebnis r;
        if (!SageLagerplatzAnlage.TryParseCode(platz.Code, lager, out var d1, out var d2, out var d3))
            r = SageLagerplatzAnlageErgebnis.Abgelehnt($"Code passt nicht zum Muster {lager};D1;D2;D3 – nichts an Sage gesendet");
        else
        {
            try
            {
                r = await _sage.AnlegenAsync(lager, d1, d2, d3, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                var text = TechnischPrefix + $"Lagerplatz {platz.Code}: " + ex.GetBaseException().Message;
                Setze(o, SageBookingQueueStatus.Offen, text);
                platz.SageUebergabeFehler = Kappen(text, MaxFehlerLaenge);   // Status bleibt Offen
                Audit(platz);
                await _ctx.SaveChangesAsync(ct);
                return text;
            }
        }

        var run = await lauf.RunAsync(ct, r.SageDb);
        if (r.Art == SageLagerplatzAnlageArt.Abgelehnt)
        {
            Setze(o, SageBookingQueueStatus.Fehler, r.Grund);
            platz.SageUebergabeStatus = SageUebergabeStatus.Fehler;
            platz.SageUebergabeFehler = Kappen(r.Grund, MaxFehlerLaenge);
            Audit(platz);
            lauf.Fehler++;
            await run.LogWarningAsync(Kappen($"Lagerplatz {platz.Code} nicht in Sage angelegt: {r.Grund}", 900), reference: platz.Code, ct: ct);
        }
        else
        {
            Uebernehmen(platz, r);
            await SageStammdatenOutbox.ErledigenAsync(_ctx, SageStammdatenOutbox.TypLagerplatz, platz.Id, Actor, Actor, ct);
            if (r.Art == SageLagerplatzAnlageArt.Angelegt)
            {
                lauf.Angelegt++;
                await run.LogInfoAsync($"Lagerplatz {platz.Code} in Sage angelegt (PlatzID {r.PlatzId})", reference: platz.Code, ct: ct);
            }
            else
            {
                lauf.Vorhanden++;
                await run.LogInfoAsync($"Lagerplatz {platz.Code} war in Sage schon vorhanden (PlatzID {r.PlatzId}) – übernommen", reference: platz.Code, ct: ct);
            }
        }
        await _ctx.SaveChangesAsync(ct);
        return null;
    }

    /// <summary>F3 Punkt 3 + S6: ab jetzt ein Sage-Platz; Sperr- und Masswerte aus der Sage-Zeile, damit der erste Abgleich keinen Diff sieht.</summary>
    internal static void Uebernehmen(StorageLocation platz, SageLagerplatzAnlageErgebnis r)
    {
        var jetzt = DateTime.Now;
        platz.Source = StorageLocationSource.Sage;
        platz.SageLagerplatzId = r.PlatzId;
        platz.SageLagerkennung = platz.Code;
        var bez = string.IsNullOrWhiteSpace(r.Platzbezeichnung) ? null : r.Platzbezeichnung.Trim();
        platz.Description = bez is { Length: > MaxDescriptionLength } ? bez[..MaxDescriptionLength] : bez;
        platz.SageIstGesperrt = r.IstGesperrt;
        platz.SageIstGesperrtInventur = r.IstGesperrtInventur;
        platz.SageLaenge = r.Laenge;
        platz.SageBreite = r.Breite;
        platz.SageHoehe = r.Hoehe;
        platz.SageVolumen = r.Volumen;
        platz.SageTragkraft = r.Tragkraft;
        platz.SageUebergabeStatus = SageUebergabeStatus.Uebergeben;
        platz.SageUebergabeAm = jetzt;
        platz.SageUebergabeFehler = null;
        Audit(platz);
    }

    /// <summary>S7: gleicher technischer Text und sonst nichts geschehen = kein Lauf; sonst ein fehlgeschlagener Lauf mit dem Text.</summary>
    private static async Task<string> TechnischAsync(Lauf lauf, string text, string? letzter, CancellationToken ct)
    {
        if (lauf.Run is null && text == letzter) return text;
        var run = await lauf.RunAsync(ct);
        await run.LogErrorAsync(Kappen(text, 900), ct: ct);
        await run.FinishFailedAsync(Kappen(text, 900), counts: lauf.Counts(), ct: ct);
        return text;
    }

    private Task<List<SageStammdatenOutboxItem>> KandidatenAsync(string typ, DateTime grenze, CancellationToken ct) =>
        _ctx.SageStammdatenOutbox
            .Where(o => o.EntityType == typ && (o.Status == SageBookingQueueStatus.Offen
                        || (o.Status == SageBookingQueueStatus.Gesendet && (o.ModifiedAt ?? o.CreatedAt) < grenze)))
            .OrderBy(o => o.Id).ToListAsync(ct);

    /// <summary>StorageArea-Ids mit Lager-Outbox != Bestaetigt (Plaetze warten, F3).</summary>
    private async Task<HashSet<int>> WartendeLagerAsync(CancellationToken ct) =>
        Ids(await _ctx.SageStammdatenOutbox.AsNoTracking()
            .Where(o => o.EntityType == SageStammdatenOutbox.TypLagerort && o.Status != SageBookingQueueStatus.Bestaetigt).ToListAsync(ct));

    private static HashSet<int> Ids(IEnumerable<SageStammdatenOutboxItem> eintraege) =>
        eintraege.Select(Id).Where(id => id > 0).ToHashSet();

    private static int Id(SageStammdatenOutboxItem o) =>
        int.TryParse(o.EntityKey, NumberStyles.None, CultureInfo.InvariantCulture, out var id) ? id : 0;

    private static void Setze(SageStammdatenOutboxItem o, SageBookingQueueStatus status, string? lastError)
    {
        o.Status = status;
        o.LastError = Kappen(lastError, MaxFehlerLaenge);
        o.ModifiedAt = DateTime.Now;
        o.ModifiedBy = Actor;
        o.ModifiedByWindows = Actor;
    }

    private static void Audit(StorageLocation l)
    {
        l.ModifiedAt = DateTime.Now;
        l.ModifiedBy = Actor;
        l.ModifiedByWindows = Actor;
    }

    private static string Kappen(string? v, int max) => string.IsNullOrEmpty(v) ? v ?? "" : (v.Length <= max ? v : v[..max]);

    /// <summary>Protokoll-Lauf, erst beim ersten Ergebnis geoeffnet (S7); schreibt je Lauf einmal die Ziel-DB (Freigabe-Antwort 8).</summary>
    private sealed class Lauf(ISyncLogger syncLogger)
    {
        public ISyncRun? Run { get; private set; }
        private bool _dbGemeldet;
        public int Angelegt, Vorhanden, Fehler, Uebersprungen, LagerAngelegt, LagerVorhanden, LagerFehler;

        public async Task<ISyncRun> RunAsync(CancellationToken ct, string? sageDb = null)
        {
            Run ??= await syncLogger.BeginRunAsync(SyncLogServices.SageLagerplatzAnlage, ct);
            if (!_dbGemeldet && !string.IsNullOrWhiteSpace(sageDb))
            {
                await Run.LogInfoAsync($"Sage-Datenbank: {sageDb}", ct: ct);
                _dbGemeldet = true;
            }
            return Run;
        }

        public Dictionary<string, int> Counts() => new()
        {
            ["angelegt"] = Angelegt, ["vorhanden"] = Vorhanden, ["fehler"] = Fehler, ["uebersprungen"] = Uebersprungen,
            ["lager_angelegt"] = LagerAngelegt, ["lager_vorhanden"] = LagerVorhanden, ["lager_fehler"] = LagerFehler,
        };
    }
}
