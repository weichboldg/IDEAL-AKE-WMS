using IdealAkeWms.Data;
using IdealAkeWms.Models;
using IdealAkeWms.Services.SyncLogger;
using IDEALAKEWMSService.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace IDEALAKEWMSService.Services;

/// <inheritdoc cref="IProductionWorkplaceSyncService"/>
public class ProductionWorkplaceSyncService : IProductionWorkplaceSyncService
{
    private const string ServiceUser = "ProductionWorkplaceSync";

    private readonly ApplicationDbContext _ctx;
    private readonly IConfiguration _config;
    private readonly IMailService _mail;
    private readonly ISageArbeitsplatzReader _reader;
    private readonly IUnknownArbeitsplatzState _problemState;
    private readonly ILogger<ProductionWorkplaceSyncService> _logger;
    private readonly ISyncLogger _syncLogger;

    public ProductionWorkplaceSyncService(
        ApplicationDbContext ctx,
        IConfiguration config,
        IMailService mail,
        ISageArbeitsplatzReader reader,
        IUnknownArbeitsplatzState problemState,
        ILogger<ProductionWorkplaceSyncService> logger,
        ISyncLogger syncLogger) // ISyncLogger als LETZTER Ctor-Parameter (ADR 0010)
    {
        _ctx = ctx;
        _config = config;
        _mail = mail;
        _reader = reader;
        _problemState = problemState;
        _logger = logger;
        _syncLogger = syncLogger;
    }

    public async Task<SyncResult> RunAsync(bool dryRun, CancellationToken ct = default)
    {
        await using var run = await _syncLogger.BeginRunAsync(SyncLogServices.ProductionWorkplaceSync, ct);

        // Safe-Varianten: ein transienter Settings-DB-Fehler soll den Sync nicht abwuergen (faellt auf
        // Default zurueck) — und macht die Upsert-Logik ohne echte DB testbar.
        var mandant = await ServiceSettings.GetIntSafeAsync(_config, "Sync:ProductionWorkplaceSyncMandant", 1, ct);
        var ausschluss = (await ServiceSettings.GetValueSafeAsync(_config, "Sync:ProductionWorkplaceSyncAusschlussliste", ct) ?? string.Empty)
            .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        List<SageArbeitsplatzDto> sageRows;
        try
        {
            sageRows = await _reader.GetActiveAsync(mandant, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Sage-Connection (Arbeitsplaetze) fehlgeschlagen.");
            await run.LogErrorAsync($"Sage-Connection fehlgeschlagen: {ex.Message}", ct: ct);
            await run.FinishFailedAsync($"Sage-Connection fehlgeschlagen: {ex.Message}", ct: ct);
            throw;
        }

        try
        {
            int inserted = 0, updated = 0, skipped = 0, deviations = 0;
            var problems = new HashSet<string>(StringComparer.Ordinal); // mehrdeutig / Insert-Verstoss

            var existing = await _ctx.ProductionWorkplaces
                .Where(w => w.SageArbeitsplatznummer != null)
                .ToListAsync(ct);
            var byNummer = new Dictionary<string, List<ProductionWorkplace>>(StringComparer.Ordinal);
            foreach (var w in existing)
            {
                var k = (w.SageArbeitsplatznummer ?? string.Empty).Trim();
                if (k.Length == 0) continue;
                (byNummer.TryGetValue(k, out var l) ? l : byNummer[k] = new List<ProductionWorkplace>()).Add(w);
            }

            var now = DateTime.Now;
            var pendingUpdates = false;

            foreach (var row in sageRows)
            {
                ct.ThrowIfCancellationRequested();

                var nummer = (row.Arbeitsplatznummer ?? string.Empty).Trim();
                var code = (row.Arbeitsschritt ?? string.Empty).Trim();
                // Pflicht-Filter: Nummer + Kuerzel gefuellt, Kuerzel nicht auf der Ausschlussliste.
                if (nummer.Length == 0 || code.Length == 0 || ausschluss.Contains(code)) { skipped++; continue; }
                var name = string.IsNullOrWhiteSpace(row.Bezeichnung1) ? nummer : row.Bezeichnung1.Trim();

                if (!byNummer.TryGetValue(nummer, out var matches))
                {
                    // Kein Treffer -> neue Werkbank anlegen (BdeAktiv bleibt false).
                    inserted++;
                    if (!dryRun)
                    {
                        _ctx.ProductionWorkplaces.Add(new ProductionWorkplace
                        {
                            SageArbeitsplatznummer = nummer,
                            Name = name,
                            ArbeitsschrittCode = code,
                            CreatedAt = now, CreatedBy = ServiceUser, CreatedByWindows = ServiceUser,
                        });
                        try
                        {
                            await _ctx.SaveChangesAsync(ct); // je Insert, damit der eindeutige Index EINE Zeile isoliert
                            pendingUpdates = false;
                        }
                        catch (DbUpdateException ex)
                        {
                            // Insert am eindeutigen Index gescheitert (Schluesselregel verletzt): melden,
                            // Entity abhaengen, mit der naechsten Zeile weiter — Lauf NICHT abbrechen.
                            inserted--;
                            problems.Add(nummer);
                            foreach (var e in _ctx.ChangeTracker.Entries<ProductionWorkplace>()
                                         .Where(e => e.State == EntityState.Added).ToList())
                                e.State = EntityState.Detached;
                            await run.LogWarningAsync(
                                $"Werkbank-Anlage fehlgeschlagen (Arbeitsplatz {nummer} '{name}'): {ex.GetBaseException().Message}. " +
                                "Schluesselregel verletzt — uebersprungen.", reference: nummer, ct: ct);
                        }
                    }
                    continue;
                }

                if (matches.Count > 1)
                {
                    // Durch den eindeutigen Index ausgeschlossen — Verteidigung in der Tiefe.
                    problems.Add(nummer);
                    await run.LogWarningAsync(
                        $"Arbeitsplatznummer {nummer} mehrfach im WMS ({matches.Count}) — keine Zuweisung.",
                        reference: nummer, ct: ct);
                    continue;
                }

                var wp = matches[0];
                if (!string.Equals(wp.Name, name, StringComparison.Ordinal)
                    || !string.Equals(wp.ArbeitsschrittCode, code, StringComparison.Ordinal))
                {
                    await run.LogWarningAsync(
                        $"Arbeitsplatz {nummer}: Sage weicht ab — Name '{wp.Name}'→'{name}', Kuerzel '{wp.ArbeitsschrittCode}'→'{code}' (Sage fuehrend, ueberschrieben).",
                        reference: nummer, ct: ct);
                    deviations++;
                    if (!dryRun)
                    {
                        wp.Name = name;
                        wp.ArbeitsschrittCode = code;
                        wp.ModifiedAt = now; wp.ModifiedBy = ServiceUser; wp.ModifiedByWindows = ServiceUser;
                        pendingUpdates = true;
                    }
                    updated++;
                }
            }

            if (!dryRun && pendingUpdates) await _ctx.SaveChangesAsync(ct);

            // Sammelmail (S1) nur bei Aenderung der problematischen Menge.
            if (problems.Count > 0 && _problemState.HasChanged(problems.ToList()))
                await SendProblemDigestAsync(problems, ct);
            else
                _problemState.HasChanged(problems.ToList()); // Zustand IMMER fortschreiben (auch bei leerer Menge)

            _logger.LogInformation(
                "ProductionWorkplace-Sync: {Ins} neu, {Upd} aktualisiert, {Skip} uebersprungen, {Prob} problematisch{Dry}.",
                inserted, updated, skipped, problems.Count, dryRun ? " [DryRun]" : "");

            await run.FinishSuccessAsync(new Dictionary<string, int>
            {
                ["neu"] = inserted,
                ["aktualisiert"] = updated,
                ["abweichung"] = deviations,
                ["uebersprungen"] = skipped,
                ["problematisch"] = problems.Count,
            }, messageSuffix: dryRun ? "[DryRun]" : null, ct: ct);

            return new SyncResult(inserted, updated, problems.Count);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "ProductionWorkplace-Sync fehlgeschlagen.");
            await run.LogErrorAsync(ex.Message, ct: ct);
            await run.FinishFailedAsync(ex.Message, ct: ct);
            throw;
        }
    }

    private async Task SendProblemDigestAsync(IReadOnlyCollection<string> problems, CancellationToken ct)
    {
        if (problems.Count == 0) return;
        if (!await ServiceSettings.GetBoolAsync(_config, "ErrorNotification:Enabled", false, ct)) return;
        var recipientsRaw = await ServiceSettings.GetValueAsync(_config, "ErrorNotification:Recipients", ct);
        var recipients = (recipientsRaw ?? string.Empty)
            .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (recipients.Length == 0) return;

        var list = string.Join(", ", problems.OrderBy(x => x, StringComparer.Ordinal));
        var subject = $"Werkbank-Sync: {problems.Count} problematische Arbeitsplaetze";
        var html = $"<p>Beim Werkbank-Sync aus den Sage-Arbeitsplaetzen wurden {problems.Count} Arbeitsplatznummern " +
                   $"nicht angelegt (mehrdeutig oder Schluesselregel verletzt):</p><p>{list}</p>";
        var text = $"Werkbank-Sync: {problems.Count} problematische Arbeitsplaetze:\n{list}";
        await _mail.SendAsync(subject, html, recipients, text, ct);
    }
}
