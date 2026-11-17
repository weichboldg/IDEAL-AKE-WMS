using IdealAkeWms.Data;
using IdealAkeWms.Models;
using IdealAkeWms.Services;
using IdealAkeWms.Services.SageSdata;
using IdealAkeWms.Services.SyncLogger;
using IDEALAKEWMSService.Services.Sdata;
using Microsoft.EntityFrameworkCore;

namespace IDEALAKEWMSService.Services;

/// <summary>Sage-Stand der FAs vor dem Senden (nur GET, <see cref="SageFaArbeitsgaenge.LesenAsync"/>). Eigene Schnittstelle nur, damit
/// der Sende-Lauf ohne DB-Konfiguration testbar ist.</summary>
public interface ISageFaStandLookup
{
    Task<IReadOnlyDictionary<int, SageFaArbeitsgaenge.FaStand>> LesenAsync(IReadOnlyCollection<int> belIds, CancellationToken ct);
}

public sealed class SdataFaStandLookup : ISageFaStandLookup
{
    private readonly ISageSdataReadClient _client;
    private readonly IConfiguration _configuration;

    public SdataFaStandLookup(ISageSdataReadClient client, IConfiguration configuration)
    {
        _client = client;
        _configuration = configuration;
    }

    public async Task<IReadOnlyDictionary<int, SageFaArbeitsgaenge.FaStand>> LesenAsync(IReadOnlyCollection<int> belIds, CancellationToken ct) =>
        await SageFaArbeitsgaenge.LesenAsync(_client, await SdataLeseZielLeser.LesenAsync(_configuration, ct), belIds, ct);
}

public sealed record SageFaAgWiederholung(int MaxWiederholungen, int AbstandMinuten);

/// <summary>
/// v1.99.38 (Spec 2026-11-17-arbeitsgaenge-aus-wms-in-sage-fa-anlegen E1a, Freigabe-Antwort 9): ein Sende-Lauf der eingereihten AG-Anlagen.
/// Vor JEDEM Senden wird der FA in Sage gelesen (nur GET): AG schon da = InSageVorhanden ohne POST; FA fehlt/beendet/begonnen = Fehler ohne
/// POST; Lesefehler = nichts senden. Status VOR dem POST auf Gesendet (+1 Versuch); haengt er, wird er „unklar“ und nie automatisch
/// wiederholt. Je AG ein Aufruf (Teilerfolg je AG sichtbar). Voraussetzung: genau EINE Dienst-Instanz (wie SageBookingWorker).
/// </summary>
public class SageFaArbeitsgangService
{
    public const int MaxFasJeLauf = 50;

    private readonly ApplicationDbContext _ctx;
    private readonly ISageFaArbeitsgangClient _client;
    private readonly ISageFaStandLookup _lookup;
    private readonly ISyncErrorNotifier _notifier;
    private readonly ILogger<SageFaArbeitsgangService> _logger;
    private readonly ISyncLogger _syncLogger;

    public SageFaArbeitsgangService(ApplicationDbContext ctx, ISageFaArbeitsgangClient client, ISageFaStandLookup lookup, ISyncErrorNotifier notifier,
        ILogger<SageFaArbeitsgangService> logger, ISyncLogger syncLogger)
    {
        _ctx = ctx;
        _client = client;
        _lookup = lookup;
        _notifier = notifier;
        _logger = logger;
        _syncLogger = syncLogger;
    }

    /// <returns>Anzahl angelegter AGs dieses Laufs.</returns>
    public async Task<int> RunOnceAsync(SageBookingEndpoint endpoint, string? bearbeiter, int stuckMinutes, SageFaAgWiederholung wiederholung,
        CancellationToken ct = default, string? zugangWarnung = null)
    {
        var jetzt0 = DateTime.Now;
        var grenze = jetzt0.AddMinutes(-Math.Max(1, stuckMinutes));
        var abstand = jetzt0.AddMinutes(-Math.Max(1, wiederholung.AbstandMinuten));
        var max = Math.Max(0, wiederholung.MaxWiederholungen);
        var haengend = await _ctx.SageFaArbeitsgangAnlagen
            .Where(a => a.Status == SageFaArbeitsgangStatus.Gesendet && (a.ModifiedAt ?? a.CreatedAt) < grenze).ToListAsync(ct);
        // automatisch wiederholen nur nach einem echten Versuch (Versuche >= 1), hoechstens max-mal, nie eine unklare Antwort
        var kandidaten = (await _ctx.SageFaArbeitsgangAnlagen
            .Where(a => a.Status == SageFaArbeitsgangStatus.Offen
                        || (a.Status == SageFaArbeitsgangStatus.Fehler && a.Versuche >= 1 && a.Versuche <= max && (a.ModifiedAt ?? a.CreatedAt) < abstand))
            .ToListAsync(ct))
            .Where(a => a.Status == SageFaArbeitsgangStatus.Offen || a.Fehler?.StartsWith(SageFaArbeitsgaenge.UnklarText) != true)
            .ToList();
        var faIds = kandidaten.Select(a => a.BelId).Distinct().Order().Take(MaxFasJeLauf).ToHashSet();
        var anlagen = kandidaten.Where(a => faIds.Contains(a.BelId)).OrderBy(a => a.BelId).ThenBy(a => a.Reihenfolge).ThenBy(a => a.Id).ToList();
        if (haengend.Count == 0 && anlagen.Count == 0) return 0;   // kein Protokoll-Eintrag je Takt ohne Arbeit

        await using var run = await _syncLogger.BeginRunAsync(SyncLogServices.SageFaArbeitsgaenge, ct);
        int angelegt = 0, uebersprungen = 0, fehler = 0;
        var fehlerTexte = new List<string>();
        if (zugangWarnung is not null) await run.LogWarningAsync(zugangWarnung, ct: ct);
        try
        {
            foreach (var a in haengend)
            {
                Setze(a, SageFaArbeitsgangStatus.Fehler, SageFaArbeitsgaenge.UnklarText);
                fehler++;
                fehlerTexte.Add(Text(a, SageFaArbeitsgaenge.UnklarText));
                await run.LogWarningAsync(Text(a, SageFaArbeitsgaenge.UnklarText), ct: ct);
            }
            if (haengend.Count > 0) await _ctx.SaveChangesAsync(ct);

            if (anlagen.Count > 0 && string.IsNullOrWhiteSpace(bearbeiter))
            {
                foreach (var a in anlagen) Setze(a, SageFaArbeitsgangStatus.Fehler, SageFaArbeitsgaenge.BearbeiterFehlt);
                await _ctx.SaveChangesAsync(ct);
                fehler += anlagen.Count;
                fehlerTexte.Add(SageFaArbeitsgaenge.BearbeiterFehlt);
                await run.LogWarningAsync($"{anlagen.Count} Arbeitsgang/Arbeitsgänge: {SageFaArbeitsgaenge.BearbeiterFehlt}", ct: ct);
                anlagen.Clear();
            }

            IReadOnlyDictionary<int, SageFaArbeitsgaenge.FaStand> stand = new Dictionary<int, SageFaArbeitsgaenge.FaStand>();
            if (anlagen.Count > 0)
            {
                try { stand = await _lookup.LesenAsync(anlagen.Select(a => a.BelId).Distinct().ToList(), ct); }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogWarning(ex, "Sage-FA-AG: Sage-Stand vor dem Senden nicht lesbar.");
                    var text = $"Sage-Prüfung vor dem Senden fehlgeschlagen ({ex.GetBaseException().Message}) – nicht gesendet, nächster Versuch später.";
                    foreach (var a in anlagen) { Setze(a, SageFaArbeitsgangStatus.Fehler, text); a.Versuche++; }
                    await _ctx.SaveChangesAsync(ct);
                    fehler += anlagen.Count;
                    fehlerTexte.Add(text);
                    await run.LogWarningAsync(Kappen(text, 900)!, ct: ct);
                    anlagen.Clear();
                }
            }

            foreach (var a in anlagen)
            {
                if (ct.IsCancellationRequested) break;
                var st = stand.GetValueOrDefault(a.BelId);
                if (st is { Gefunden: true } && st.AgNummern.Contains(a.SageArbeitsgangnummer.Trim()))
                {
                    Setze(a, SageFaArbeitsgangStatus.InSageVorhanden, null);
                    a.SageAm = DateTime.Now;
                    uebersprungen++;
                    await _ctx.SaveChangesAsync(ct);
                    continue;
                }
                var sperre = st is null || !st.Gefunden ? "FA in Sage nicht unter den offenen FAs – nicht gesendet."
                    : st.Beendet ? "FA in Sage beendet/gesperrt – nicht gesendet."
                    : st.Begonnen ? "FA in Sage begonnen – nicht gesendet (nur nicht begonnene FAs werden beschrieben)."
                    : st.Fertigungsmenge is not > 0 ? "FA ohne Fertigungs-/Sollmenge in Sage – nicht gesendet."
                    : null;
                if (sperre is not null)
                {
                    Setze(a, SageFaArbeitsgangStatus.Fehler, sperre);
                    uebersprungen++;
                    await _ctx.SaveChangesAsync(ct);
                    await run.LogWarningAsync(Text(a, sperre), ct: ct);
                    continue;
                }

                // Status VOR dem HTTP-Call (Timeout/Absturz -> „haengend“ -> unklar statt blind doppelt senden)
                Setze(a, SageFaArbeitsgangStatus.Gesendet, null);
                a.Versuche++;
                await _ctx.SaveChangesAsync(ct);

                var r = await _client.SendAsync(
                    new SageFaArbeitsgangRequest(a.BelId, a.SageArbeitsgangnummer.Trim(), st!.Fertigungsmenge!.Value, bearbeiter!.Trim()), endpoint, ct);
                // QA F3 (v1.99.39): 2xx ohne ErrorText reicht nicht (der Request schickt den Handle selbst mit) -> in Sage nachlesen
                if (r.Success && await NachlesenAsync(a, ct) is { } nachlese) r = nachlese;
                if (r.Success)
                {
                    Setze(a, SageFaArbeitsgangStatus.Angelegt, null);
                    a.SageAm = DateTime.Now;
                    angelegt++;
                    await run.LogInfoAsync($"FA {a.BelId}: Arbeitsgang {a.SageArbeitsgangnummer} ({a.Kuerzel}) in Sage angelegt.", ct: ct);
                }
                else
                {
                    var roh = string.Join(" | ", new[] { r.Error, r.ResponseRaw }.Where(s => !string.IsNullOrWhiteSpace(s)));
                    var text = Kappen(r.Unklar ? $"{SageFaArbeitsgaenge.UnklarText} ({roh})" : roh, SageFaArbeitsgangAnlage.FehlerMaxLaenge);
                    if (string.IsNullOrWhiteSpace(text)) text = "Unbekannter Sende-Fehler";
                    Setze(a, SageFaArbeitsgangStatus.Fehler, text);
                    fehler++;
                    fehlerTexte.Add(Text(a, text));
                    await run.LogWarningAsync(Kappen(Text(a, text), 900)!, ct: ct);
                }
                await _ctx.SaveChangesAsync(ct);
            }

            await run.FinishSuccessAsync(Counts(angelegt, uebersprungen, fehler), ct: ct);
            if (fehler > 0)
                await _notifier.NotifyAsync("Sage-FA-Arbeitsgänge", new InvalidOperationException(
                    Kappen($"{fehler} Arbeitsgang-Anlage(n) nicht angelegt: " + string.Join(" / ", fehlerTexte.Distinct()), 1000)), ct);
            return angelegt;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Sage-FA-AG-Lauf fehlgeschlagen.");
            await run.LogErrorAsync(Kappen(ex.Message, 900)!, ct: ct);
            await run.FinishFailedAsync(Kappen(ex.Message, 900)!, counts: Counts(angelegt, uebersprungen, fehler), ct: ct);
            throw;
        }
    }

    /// <summary>Nach einer 2xx-Antwort: AG am FA nachlesen (nur GET; AG-Nummer = RessourceNummer der AG-Position, P-5).
    /// null = gefunden; sonst Fehler mit Text bzw. „unklar“ bei Lesefehler (nie automatisch wiederholt).</summary>
    private async Task<SageFaArbeitsgangSendResult?> NachlesenAsync(SageFaArbeitsgangAnlage a, CancellationToken ct)
    {
        try
        {
            var st = (await _lookup.LesenAsync(new[] { a.BelId }, ct)).GetValueOrDefault(a.BelId);
            return st is { Gefunden: true } && st.AgNummern.Contains(a.SageArbeitsgangnummer.Trim())
                ? null
                : new(false, false, null, "Sage meldet Erfolg, aber der Arbeitsgang steht beim Nachlesen nicht am FA – nicht angelegt.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Sage-FA-AG: Nachlesen nach dem Senden fehlgeschlagen (FA {BelId}).", a.BelId);
            return new(false, true, null, $"Nachlesen nach dem Senden fehlgeschlagen: {ex.GetBaseException().Message}");
        }
    }

    private static Dictionary<string, int> Counts(int angelegt, int uebersprungen, int fehler) => new()
    {
        ["Angelegt"] = angelegt, ["Übersprungen"] = uebersprungen, ["Fehler"] = fehler,
    };

    private static string Text(SageFaArbeitsgangAnlage a, string text) => $"FA {a.BelId}, Arbeitsgang {a.SageArbeitsgangnummer} ({a.Kuerzel}): {text}";

    private static void Setze(SageFaArbeitsgangAnlage a, SageFaArbeitsgangStatus status, string? fehlerText)
    {
        a.Status = status;
        a.Fehler = status == SageFaArbeitsgangStatus.Fehler ? Kappen(fehlerText, SageFaArbeitsgangAnlage.FehlerMaxLaenge) : null;
        a.ModifiedAt = DateTime.Now;
        a.ModifiedBy = SageFaArbeitsgaenge.ServiceActor;
        a.ModifiedByWindows = SageFaArbeitsgaenge.ServiceActor;
    }

    private static string? Kappen(string? v, int max) => string.IsNullOrEmpty(v) ? v : (v.Length <= max ? v : v[..max]);
}
