using IdealAkeWms.Data;
using IdealAkeWms.Models;
using IdealAkeWms.Services.SyncLogger;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IDEALAKEWMSService.Services;

public class LagerplatzSyncService : ILagerplatzSyncService
{
    // Sage-Codes duerfen den vollen DB-Platz nutzen (NVARCHAR(50)). Manuelle
    // Codes sind via Model-Validation auf 12 Zeichen begrenzt.
    private const int MaxCodeLength = 50;
    private const int MaxDescriptionLength = 200;
    private const string SyncUser = "system:sync";

    private readonly ApplicationDbContext _ctx;
    private readonly ISageLagerplatzReader _reader;
    private readonly ISyncLogger _syncLogger;
    private readonly ILogger<LagerplatzSyncService> _logger;

    public LagerplatzSyncService(
        ApplicationDbContext ctx,
        ISageLagerplatzReader reader,
        ILogger<LagerplatzSyncService> logger,
        ISyncLogger syncLogger)
    {
        _ctx = ctx;
        _reader = reader;
        _syncLogger = syncLogger;
        _logger = logger;
    }

    public async Task<LagerplatzSyncResult> RunAsync(CancellationToken ct = default)
    {
        await using var run = await _syncLogger.BeginRunAsync(SyncLogServices.Lagerplatz, ct);
        int inserted = 0, updated = 0, conflicts = 0, deactivated = 0, skipped = 0, errors = 0;

        try
        {
            List<SageLagerplatzDto> sageRecords;
            try
            {
                sageRecords = await _reader.GetAllActiveAsync(ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Sage-Connection fehlgeschlagen.");
                await run.LogErrorAsync($"Sage-Connection fehlgeschlagen: {ex.Message}", ct: ct);
                errors++;
                await run.FinishFailedAsync($"Sage-Connection fehlgeschlagen: {ex.Message}", counts: new Dictionary<string, int>
                {
                    ["neu"] = inserted,
                    ["aktualisiert"] = updated,
                    ["konflikte"] = conflicts,
                    ["deaktiviert"] = deactivated,
                    ["uebersprungen"] = skipped,
                    ["fehler"] = errors,
                }, ct: ct);
                return new LagerplatzSyncResult(0, 0, 0, 0, 0, 1);
            }

            // Sage-Duplikate erkennen und entfernen
            var dupGroups = sageRecords
                .Where(r => !string.IsNullOrWhiteSpace(r.Kurzbezeichnung))
                .GroupBy(r => r.Kurzbezeichnung!.Trim(), StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .ToList();

            foreach (var group in dupGroups)
            {
                var bereiche = string.Join(", ", group.Select(g => g.Lagerkennung ?? "?"));
                await run.LogWarningAsync(
                    $"Sage liefert Lagerplatz '{group.Key}' mehrfach (Bereiche {bereiche}). Eintraege uebersprungen.",
                    reference: group.Key, ct: ct);
            }

            var dupCodes = dupGroups.Select(g => g.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
            sageRecords = sageRecords
                .Where(r => string.IsNullOrWhiteSpace(r.Kurzbezeichnung)
                         || !dupCodes.Contains(r.Kurzbezeichnung!.Trim()))
                .ToList();

            var existing = await _ctx.StorageLocations.ToListAsync(ct);
            var byCode = existing.ToDictionary(x => x.Code, x => x, StringComparer.OrdinalIgnoreCase);

            foreach (var dto in sageRecords)
            {
                if (!string.IsNullOrWhiteSpace(dto.Kurzbezeichnung))
                {
                    var rawCode = dto.Kurzbezeichnung.Trim();
                    if (rawCode.Length > MaxCodeLength)
                    {
                        await run.LogWarningAsync(
                            $"Lagerplatz-Code '{rawCode}' ist zu lang ({rawCode.Length} > {MaxCodeLength}), uebersprungen.",
                            reference: rawCode, ct: ct);
                        skipped++;
                        continue;
                    }
                }

                if (string.IsNullOrWhiteSpace(dto.Kurzbezeichnung))
                {
                    skipped++;
                    continue;
                }

                var code = dto.Kurzbezeichnung.Trim();
                var zone = string.IsNullOrWhiteSpace(dto.Lagerkennung) ? null : dto.Lagerkennung.Trim();
                var description = string.IsNullOrWhiteSpace(dto.Platzbezeichnung) ? null : dto.Platzbezeichnung.Trim();

                if (description != null && description.Length > MaxDescriptionLength)
                {
                    await run.LogInfoAsync(
                        $"Beschreibung von '{code}' auf {MaxDescriptionLength} Zeichen gekuerzt.",
                        reference: code, ct: ct);
                    description = description.Substring(0, MaxDescriptionLength);
                }

                if (!byCode.ContainsKey(code))
                {
                    _ctx.StorageLocations.Add(new StorageLocation
                    {
                        Code = code,
                        Zone = zone,
                        Description = description,
                        BarcodeValue = code,
                        Source = StorageLocationSource.Sage,
                        IsActive = true,
                        IstBuchbar = false,                       // NEU: Sage-Plaetze sind by default nicht buchbar
                        Capacity = null,
                        IsPickingTransport = false,
                        CreatedAt = DateTime.Now,
                        CreatedBy = SyncUser,
                        CreatedByWindows = Environment.MachineName
                    });
                    inserted++;
                }
                else
                {
                    var existingLoc = byCode[code];
                    if (existingLoc.Source == StorageLocationSource.Sage)
                    {
                        var diff = existingLoc.Zone != zone
                                || existingLoc.Description != description
                                || existingLoc.BarcodeValue != code
                                || !existingLoc.IsActive;

                        if (diff)
                        {
                            existingLoc.Zone = zone;
                            existingLoc.Description = description;
                            existingLoc.BarcodeValue = code;
                            existingLoc.IsActive = true;
                            existingLoc.ModifiedAt = DateTime.Now;
                            existingLoc.ModifiedBy = SyncUser;
                            existingLoc.ModifiedByWindows = Environment.MachineName;
                            updated++;
                        }
                    }
                    else // Manual
                    {
                        await run.LogWarningAsync(
                            $"Konflikt: Lagerplatz {code} existiert manuell, Sage-Eintrag ignoriert.",
                            reference: code, ct: ct);
                        conflicts++;
                    }
                }
            }

            // Soft-deactivate Sage-Records, die nicht mehr in der Sage-Liste sind
            var sageCodesInFeed = sageRecords
                .Select(r => string.IsNullOrWhiteSpace(r.Kurzbezeichnung) ? null : r.Kurzbezeichnung.Trim())
                .Where(c => c != null)
                .ToHashSet(StringComparer.OrdinalIgnoreCase)!;

            foreach (var loc in existing.Where(l => l.Source == StorageLocationSource.Sage && l.IsActive))
            {
                if (!sageCodesInFeed.Contains(loc.Code))
                {
                    loc.IsActive = false;
                    loc.ModifiedAt = DateTime.Now;
                    loc.ModifiedBy = SyncUser;
                    loc.ModifiedByWindows = Environment.MachineName;
                    deactivated++;

                    await run.LogInfoAsync(
                        $"Lagerplatz {loc.Code} aus Sage entfernt -> deaktiviert.",
                        reference: loc.Code, ct: ct);
                }
            }

            await _ctx.SaveChangesAsync(ct);

            await run.FinishSuccessAsync(new Dictionary<string, int>
            {
                ["neu"] = inserted,
                ["aktualisiert"] = updated,
                ["konflikte"] = conflicts,
                ["deaktiviert"] = deactivated,
                ["uebersprungen"] = skipped,
                ["fehler"] = errors,
            }, ct: ct);

            return new LagerplatzSyncResult(inserted, updated, conflicts, deactivated, skipped, errors);
        }
        catch (Exception ex)
        {
            await run.LogErrorAsync(ex.Message, ct: ct);
            await run.FinishFailedAsync(ex.Message, counts: new Dictionary<string, int>
            {
                ["neu"] = inserted,
                ["aktualisiert"] = updated,
                ["konflikte"] = conflicts,
                ["deaktiviert"] = deactivated,
                ["uebersprungen"] = skipped,
                ["fehler"] = errors,
            }, ct: ct);
            throw;
        }
    }
}
