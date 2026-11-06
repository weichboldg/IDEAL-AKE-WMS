using IdealAkeWms.Data;
using IdealAkeWms.Models;
using IdealAkeWms.Services.SageSdata;
using IdealAkeWms.Services.SyncLogger;
using Microsoft.EntityFrameworkCore;

namespace IDEALAKEWMSService.Services.Sdata;

/// <summary>Zeile aus <c>PpsStillstandsbegruendungenInfo</c> (Namen exakt wie in der SData-Antwort, Typen P-S1).
/// Kein Aktiv-Feld: im Schema nicht vorhanden (P-S1) — ein Feld ausserhalb des Schemas bricht die Abfrage (HTTP 500).</summary>
public sealed record SdataStillstandZeile(string? Stillstandnummer, string? Bezeichnung);

/// <summary>
/// Spec 2026-11-06-sage-sdata-stammdaten-sync (E6b/F8): liest die Sage-Stillstandsbegruendungen aus SData in die Stammtabelle
/// <c>Stillstandsgruende</c> (anlegen, Bezeichnung aktualisieren, nie loeschen — fehlende bekommen <c>SageMissingSince</c>,
/// wieder auftauchende verlieren es). Gate <c>Sync:StillstandsgruendeEnabled</c> im SyncWorker. Keine Oberflaeche (Rueckmelde-Epic).
/// Empty-Guard: leere Antwort kennzeichnet nichts. Lesefehler: Lauf Fehler + throw (Mail ueber RunResilientAsync).
/// </summary>
public class StillstandsgruendeSyncService
{
    public const string ServiceUser = "StillstandsgruendeSync";
    public const string Ressource = "PpsStillstandsbegruendungenInfo";
    public const string Select = "Stillstandnummer,Bezeichnung";

    private readonly ApplicationDbContext _ctx;
    private readonly ISageSdataReadClient _client;
    private readonly IConfiguration _config;
    private readonly ILogger<StillstandsgruendeSyncService> _logger;
    private readonly ISyncLogger _syncLogger;

    public StillstandsgruendeSyncService(ApplicationDbContext ctx, ISageSdataReadClient client, IConfiguration config,
        ILogger<StillstandsgruendeSyncService> logger, ISyncLogger syncLogger) // ISyncLogger als LETZTER Ctor-Parameter (ADR 0010)
    {
        _ctx = ctx;
        _client = client;
        _config = config;
        _logger = logger;
        _syncLogger = syncLogger;
    }

    /// <param name="ziel">Nur fuer Tests; im Betrieb aus ServiceSettings (<see cref="SdataLeseZielLeser"/>).</param>
    public async Task<SyncResult> RunAsync(bool dryRun, CancellationToken ct = default, SdataLeseZiel? ziel = null)
    {
        await using var run = await _syncLogger.BeginRunAsync(SyncLogServices.Stillstandsgruende, ct);

        IReadOnlyList<SdataStillstandZeile> zeilen;
        try
        {
            ziel ??= await SdataLeseZielLeser.LesenAsync(_config, ct);
            var seiten = await _client.QueryAllAsync<SdataStillstandZeile>(ziel, SageSdataReadClient.VertragResources, Ressource,
                null, Select, "Stillstandnummer", ct);
            zeilen = seiten.Zeilen;
            await run.LogInfoAsync(seiten.Zusammenfassung, ct: ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Stillstandsbegruendungen aus Sage (SData) nicht lesbar.");
            await run.LogErrorAsync($"Sage (SData) nicht lesbar: {ex.Message}", ct: ct);
            await run.FinishFailedAsync($"Sage (SData) nicht lesbar: {ex.Message}", ct: ct);
            throw;
        }

        try
        {
            var (eindeutig, uebersprungen) = await SdataSchluessel.EindeutigAsync(zeilen, z => z.Stillstandnummer, run, "Stillstandnummer", ct);
            int neu = 0, aktualisiert = 0, unveraendert = 0, nichtInSage = 0, wiederDa = 0;
            var neuMarkiert = new List<Stillstandsgrund>();
            var now = DateTime.Now;
            var bestand = await _ctx.Stillstandsgruende.ToListAsync(ct);
            var byNummer = bestand.ToDictionary(g => g.Nummer, StringComparer.Ordinal);

            void Geaendert(Stillstandsgrund g) { g.ModifiedAt = now; g.ModifiedBy = ServiceUser; g.ModifiedByWindows = ServiceUser; }

            foreach (var z in eindeutig)
            {
                var nummer = z.Stillstandnummer!.Trim();
                var bezeichnung = z.Bezeichnung?.Trim();
                if (!byNummer.TryGetValue(nummer, out var g))
                {
                    neu++;
                    if (!dryRun)
                        _ctx.Stillstandsgruende.Add(new Stillstandsgrund
                        {
                            Nummer = nummer, Bezeichnung = bezeichnung,
                            CreatedAt = now, CreatedBy = ServiceUser, CreatedByWindows = ServiceUser,
                        });
                    continue;
                }
                // F1: null = Quelle liefert das Feld nicht = nicht schreiben.
                var bezAnders = bezeichnung != null && !string.Equals(bezeichnung, g.Bezeichnung, StringComparison.Ordinal);
                var warWeg = g.SageMissingSince != null;
                if (warWeg) wiederDa++;
                if (bezAnders) aktualisiert++;
                if (!bezAnders && !warWeg) { unveraendert++; continue; }
                if (dryRun) continue;
                if (bezAnders) g.Bezeichnung = bezeichnung;
                g.SageMissingSince = null;
                Geaendert(g);
            }

            if (eindeutig.Count == 0)
                await run.LogWarningAsync("Sage liefert keine Stillstandsbegruendungen — keine Kennzeichnung \"nicht in Sage\" (Empty-Guard).", ct: ct);
            else
            {
                var present = eindeutig.Select(z => z.Stillstandnummer!.Trim()).ToHashSet(StringComparer.Ordinal);
                foreach (var g in bestand.Where(g => !present.Contains(g.Nummer)))
                {
                    nichtInSage++;
                    if (g.SageMissingSince != null) continue;
                    neuMarkiert.Add(g);
                    if (dryRun) continue;
                    g.SageMissingSince = now;
                    Geaendert(g);
                }
                if (neuMarkiert.Count > 0)
                    await run.LogWarningAsync(SdataSchluessel.Kappen($"Nicht mehr in Sage ({neuMarkiert.Count} neu gekennzeichnet): " +
                        string.Join(", ", neuMarkiert.Take(20).Select(g => $"{g.Nummer} {g.Bezeichnung}"))), ct: ct);
            }

            if (!dryRun) await _ctx.SaveChangesAsync(ct);

            await run.FinishSuccessAsync(new Dictionary<string, int>
            {
                ["gelesen"] = eindeutig.Count,
                ["neu"] = neu,
                ["aktualisiert"] = aktualisiert,
                ["unveraendert"] = unveraendert,
                ["uebersprungen"] = uebersprungen,
                ["nicht_in_sage"] = nichtInSage,
                ["nicht_in_sage_neu"] = neuMarkiert.Count,
                ["wieder_in_sage"] = wiederDa,
            }, messageSuffix: dryRun ? "[DryRun]" : null, ct: ct);
            return new SyncResult(neu, aktualisiert, uebersprungen);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Stillstandsgruende-Abgleich fehlgeschlagen.");
            await run.LogErrorAsync(ex.Message, ct: ct);
            await run.FinishFailedAsync(ex.Message, ct: ct);
            throw;
        }
    }
}
