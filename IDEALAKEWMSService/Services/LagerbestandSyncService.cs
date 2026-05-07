using IdealAkeWms.Data;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IDEALAKEWMSService.Services;

public class LagerbestandSyncService : ILagerbestandSyncService
{
    private const string ServiceName = "Lagerbestand";
    private const string SyncUser = "system:sync";

    private readonly ApplicationDbContext _ctx;
    private readonly ISageBestandReader _reader;
    private readonly IStockMovementRepository _stockRepo;
    private readonly ISyncLogRepository _syncLogs;
    private readonly ILogger<LagerbestandSyncService> _logger;

    public LagerbestandSyncService(
        ApplicationDbContext ctx,
        ISageBestandReader reader,
        IStockMovementRepository stockRepo,
        ISyncLogRepository syncLogs,
        ILogger<LagerbestandSyncService> logger)
    {
        _ctx = ctx;
        _reader = reader;
        _stockRepo = stockRepo;
        _syncLogs = syncLogs;
        _logger = logger;
    }

    public async Task<LagerbestandSyncResult> RunAsync(bool dryRun, CancellationToken ct = default)
    {
        int tuples = 0, plus = 0, minus = 0, noChange = 0, skipped = 0, errors = 0;

        List<SageBestandDto> sageRows;
        try
        {
            sageRows = await _reader.GetAllAsync(ct);
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
            return new LagerbestandSyncResult(0, 0, 0, 0, 0, 1, dryRun);
        }

        // Pre-loading
        var articleByNumber = await _ctx.Articles
            .ToDictionaryAsync(a => a.ArticleNumber, a => a.Id, StringComparer.OrdinalIgnoreCase, ct);
        var locationByCode = await _ctx.StorageLocations
            .ToDictionaryAsync(
                l => l.Code,
                l => (l.Id, l.Source, l.IsActive),
                StringComparer.OrdinalIgnoreCase, ct);
        var wmsStock = await _stockRepo.GetCurrentStockByArticleAndLocationAsync();

        foreach (var dto in sageRows)
        {
            tuples++;

            if (string.IsNullOrWhiteSpace(dto.Artikelnummer) || string.IsNullOrWhiteSpace(dto.Lagerplatz))
            {
                skipped++;
                continue;
            }

            if (!articleByNumber.TryGetValue(dto.Artikelnummer, out var articleId))
            {
                await _syncLogs.AddAsync(new SyncLog
                {
                    Service = ServiceName, Level = SyncLogLevel.Warning,
                    Message = $"Artikel {dto.Artikelnummer} nicht im WMS, uebersprungen.",
                    Reference = dto.Artikelnummer
                });
                skipped++;
                continue;
            }

            if (!locationByCode.TryGetValue(dto.Lagerplatz, out var loc) ||
                loc.Source != StorageLocationSource.Sage ||
                !loc.IsActive)
            {
                skipped++;
                continue;
            }

            var wmsBestand = wmsStock.GetValueOrDefault((articleId, loc.Id), 0m);
            var sageBestand = dto.Bestand ?? 0m;
            var delta = sageBestand - wmsBestand;

            if (delta == 0m) { noChange++; continue; }

            if (!dryRun)
            {
                _ctx.StockMovements.Add(new StockMovement
                {
                    ArticleId = articleId,
                    StorageLocationId = loc.Id,
                    Quantity = Math.Abs(delta),
                    MovementType = delta > 0 ? MovementType.SageEinbuchung : MovementType.SageAusbuchung,
                    Note = $"Sage-Korrektur: WMS={wmsBestand}, Sage={sageBestand}, Diff={(delta > 0 ? "+" : "")}{delta}",
                    Timestamp = DateTime.Now,
                    UserId = null,
                    WindowsUser = SyncUser,
                    CreatedAt = DateTime.Now,
                    CreatedBy = SyncUser,
                    CreatedByWindows = Environment.MachineName
                });
            }

            if (delta > 0) plus++; else minus++;
        }

        if (!dryRun) await _ctx.SaveChangesAsync(ct);

        var prefix = dryRun ? "[DryRun] " : "";
        await _syncLogs.AddAsync(new SyncLog
        {
            Service = ServiceName,
            Level = SyncLogLevel.Info,
            Message = $"{prefix}Sync OK: {tuples} Tupel, {plus} Plus, {minus} Minus, {noChange} ohne Aenderung, {skipped} uebersprungen, {errors} Fehler."
        });

        return new LagerbestandSyncResult(tuples, plus, minus, noChange, skipped, errors, dryRun);
    }
}
