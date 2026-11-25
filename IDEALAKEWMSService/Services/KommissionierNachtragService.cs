using IdealAkeWms.Data;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Services;
using IdealAkeWms.Services.SyncLogger;
using IDEALAKEWMSService.Common;
using IDEALAKEWMSService.Services.Sdata;
using Microsoft.EntityFrameworkCore;

namespace IDEALAKEWMSService.Services;

/// <summary>
/// v1.99.70 (Spec 2026-11-25-kommissionierauftraege-nachtrag-nachts F1–F5, F8): Lauf „Kommissionier-Nachtrag“ – einmal taeglich ab
/// <c>Sync:KommissionierNachtragUhrzeit</c> fuer alle freigegebenen, offenen HauptFAs
/// (<see cref="IProductionOrderPickingStatusRepository.GetReleasedForPickingAsync"/>) <see cref="PickingOrderWorkflow.ErzeugeOffeneAsync"/>
/// aufrufen (idempotent, dieselbe Restmengen-Logik wie Freigabe und Leitstand-Knopf). Je FA ein eigener Scope: kein Change-Tracker ueber
/// Tausende Zeilen, und ein Fehler trifft den naechsten FA nicht mit verschmutztem Kontext. Offene Zeilen werden nie gekuerzt (Antwort 2).
/// </summary>
public class KommissionierNachtragService
{
    public const string ServiceUser = "KommissionierNachtrag";
    public const string KeyEnabled = "Sync:KommissionierNachtragEnabled";
    public const string KeyUhrzeit = "Sync:KommissionierNachtragUhrzeit";
    public const int DefaultUhrzeit = 2;
    private const int MeldungMax = 1000; // SyncLog.Message NVARCHAR(1000) – laengere Zeilen gingen sonst still verloren

    private readonly ApplicationDbContext _ctx;
    private readonly IProductionOrderPickingStatusRepository _status;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<KommissionierNachtragService> _logger;
    private readonly ISyncLogger _syncLogger;

    public KommissionierNachtragService(ApplicationDbContext ctx, IProductionOrderPickingStatusRepository status, IServiceScopeFactory scopeFactory,
        IConfiguration config, ILogger<KommissionierNachtragService> logger, ISyncLogger syncLogger) // ISyncLogger als LETZTER Ctor-Parameter (ADR 0010)
    {
        _ctx = ctx;
        _status = status;
        _scopeFactory = scopeFactory;
        _config = config;
        _logger = logger;
        _syncLogger = syncLogger;
    }

    /// <summary>Registrierung im Dienst (Program.cs und DI-Test): FA-Repository, gemeinsame Kommissionierauftrags-Registrierung der Web-App,
    /// <see cref="DienstBenutzer"/> als <see cref="ICurrentUserService"/>, dieser Lauf. <see cref="IProductionOrderPickingStatusRepository"/>
    /// registriert Program.cs schon vorher.</summary>
    public static IServiceCollection Registrieren(IServiceCollection services)
    {
        services.AddScoped<IProductionOrderRepository, ProductionOrderRepository>();
        services.AddKommissionierauftraege();
        services.AddScoped<ICurrentUserService>(_ => new DienstBenutzer(ServiceUser));
        services.AddScoped<KommissionierNachtragService>();
        return services;
    }

    /// <summary>F2/F3: Lauf nur, wenn eingeschaltet, nicht im DryRun und <see cref="LetzteBestellungPlaner.Faellig"/>. Merker = juengste
    /// Startmeldung dieses Laufs im Aktivitaetsprotokoll (verpasster Termin wird nach dem Dienststart einmal nachgeholt). true = gelaufen.</summary>
    public async Task<bool> RunWennFaelligAsync(bool dryRun, DateTime jetzt, CancellationToken ct = default)
    {
        if (!await ServiceSettings.GetBoolSafeAsync(_config, KeyEnabled, true, ct)) return false;
        if (dryRun)
        {
            // ErzeugeOffeneAsync kennt keinen Trockenmodus – also gar nicht laufen und keinen Merker setzen.
            _logger.LogInformation("Kommissionier-Nachtrag im DryRun übersprungen.");
            return false;
        }
        var uhrzeit = await ServiceSettings.GetIntSafeAsync(_config, KeyUhrzeit, DefaultUhrzeit, ct);
        var letzterStart = await _ctx.SyncLogs.AsNoTracking()
            .Where(l => l.Service == SyncLogServices.KommissionierNachtrag && l.Message == "Run gestartet")
            .MaxAsync(l => (DateTime?)l.Timestamp, ct);
        if (!LetzteBestellungPlaner.Faellig(jetzt, letzterStart, new[] { uhrzeit })) return false;
        await RunAsync(ct);
        return true;
    }

    public async Task RunAsync(CancellationToken ct = default)
    {
        await using var run = await _syncLogger.BeginRunAsync(SyncLogServices.KommissionierNachtrag, ct);
        try
        {
            var counts = new Dictionary<string, int>
            {
                ["fa_geprueft"] = 0, ["fa_mit_nachtrag"] = 0, ["auftraege_angelegt"] = 0, ["zeilen_angelegt"] = 0, ["ohne_rest"] = 0,
                ["altbestand_pruefen"] = 0, ["ohne_stueckliste"] = 0, ["fehler"] = 0, ["stueckliste_kleiner"] = 0,
            };
            foreach (var fa in (await _status.GetReleasedForPickingAsync()).Select(o => (o.Id, o.OrderNumber)).ToList())
            {
                ct.ThrowIfCancellationRequested();
                counts["fa_geprueft"]++;
                PickingOrderErzeugung e;
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    e = await scope.ServiceProvider.GetRequiredService<PickingOrderWorkflow>().ErzeugeOffeneAsync(fa.Id);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // z. B. Stueckliste nicht lesbar (Sage-View/OSEON) – zaehlen, melden, naechster FA.
                    _logger.LogError(ex, "Kommissionier-Nachtrag FA {Fa} fehlgeschlagen.", fa.OrderNumber);
                    counts["fehler"]++;
                    await run.LogWarningAsync(Kurz($"FA {fa.OrderNumber}: Fehler – {ex.Message}"), fa.OrderNumber, ct);
                    continue;
                }

                if (e.Fehler != null) counts["fehler"]++;
                if (e.AltbestandPruefen) counts["altbestand_pruefen"]++;
                if (e.OhneStueckliste) counts["ohne_stueckliste"]++;
                if (e.OhneRest) counts["ohne_rest"]++;
                if (e.Ueberdeckt.Count > 0) counts["stueckliste_kleiner"]++;
                if (e.Nummern.Count > 0)
                {
                    counts["fa_mit_nachtrag"]++;
                    counts["auftraege_angelegt"] += e.Nummern.Count;
                    counts["zeilen_angelegt"] += e.ZeilenAngelegt;
                    await run.LogInfoAsync(Kurz($"FA {fa.OrderNumber}: {string.Join(", ", e.Nummern)} ({e.ZeilenAngelegt} Zeilen)"), fa.OrderNumber, ct);
                }
                foreach (var m in e.Meldungen.Concat(e.Fehler != null ? new[] { e.Fehler } : Array.Empty<string>()))
                    await run.LogWarningAsync(Kurz(m.StartsWith($"FA {fa.OrderNumber}", StringComparison.Ordinal) ? m : $"FA {fa.OrderNumber}: {m}"),
                        fa.OrderNumber, ct);
            }
            await run.FinishSuccessAsync(counts, ct: ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // FA-Liste nicht lesbar o. ae.: Lauf scheitert als Ganzes, RunResilientAsync meldet per Fehlermail.
            _logger.LogError(ex, "Kommissionier-Nachtrag fehlgeschlagen.");
            await run.LogErrorAsync(Kurz(ex.Message), ct: ct);
            await run.FinishFailedAsync(Kurz(ex.Message), ct: ct);
            throw;
        }
    }

    private static string Kurz(string s) => s.Length <= MeldungMax ? s : s[..(MeldungMax - 1)] + "…";
}
