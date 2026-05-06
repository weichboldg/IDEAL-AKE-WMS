using IdealAkeWms.Data;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IDEALAKEWMSService.Services;

public class LagerplatzSyncService : ILagerplatzSyncService
{
    private const string ServiceName = "Lagerplatz";
    private const int MaxCodeLength = 50;
    private const int MaxDescriptionLength = 200;
    private const string SyncUser = "system:sync";

    private readonly ApplicationDbContext _ctx;
    private readonly ISageLagerplatzReader _reader;
    private readonly ISyncLogRepository _syncLogs;
    private readonly ILogger<LagerplatzSyncService> _logger;

    public LagerplatzSyncService(
        ApplicationDbContext ctx,
        ISageLagerplatzReader reader,
        ISyncLogRepository syncLogs,
        ILogger<LagerplatzSyncService> logger)
    {
        _ctx = ctx;
        _reader = reader;
        _syncLogs = syncLogs;
        _logger = logger;
    }

    public async Task<LagerplatzSyncResult> RunAsync(CancellationToken ct = default)
    {
        int inserted = 0, updated = 0, conflicts = 0, deactivated = 0, skipped = 0, errors = 0;

        List<SageLagerplatzDto> sageRecords;
        try
        {
            sageRecords = await _reader.GetAllActiveAsync(ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Sage-Connection fehlgeschlagen.");
            await _syncLogs.AddAsync(new SyncLog
            {
                Service = ServiceName,
                Level = SyncLogLevel.Error,
                Message = $"Sage-Connection fehlgeschlagen: {ex.Message}"
            });
            return new LagerplatzSyncResult(0, 0, 0, 0, 0, 1);
        }

        var existing = await _ctx.StorageLocations.ToListAsync(ct);
        var byCode = existing.ToDictionary(x => x.Code, x => x, StringComparer.OrdinalIgnoreCase);

        foreach (var dto in sageRecords)
        {
            if (string.IsNullOrWhiteSpace(dto.Kurzbezeichnung))
            {
                skipped++;
                continue;
            }

            var code = dto.Kurzbezeichnung.Trim();
            var zone = string.IsNullOrWhiteSpace(dto.Lagerkennung) ? null : dto.Lagerkennung.Trim();
            var description = string.IsNullOrWhiteSpace(dto.Platzbezeichnung) ? null : dto.Platzbezeichnung.Trim();

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
            }
        }

        await _ctx.SaveChangesAsync(ct);

        await _syncLogs.AddAsync(new SyncLog
        {
            Service = ServiceName,
            Level = SyncLogLevel.Info,
            Message = $"Sync OK: {inserted} neu, {updated} aktualisiert, {conflicts} Konflikte, {deactivated} deaktiviert."
        });

        return new LagerplatzSyncResult(inserted, updated, conflicts, deactivated, skipped, errors);
    }
}
