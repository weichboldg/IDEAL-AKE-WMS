using IdealAkeWms.Data;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Services;
using IdealAkeWms.Services.SyncLogger;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IDEALAKEWMSService.Services;

public class BdeAutoPauseService : IBdeAutoPauseService
{
    private readonly ApplicationDbContext _ctx;
    private readonly IBdeShiftCalendarService _calendar;
    private readonly IAppSettingRepository _settings;
    private readonly ILogger<BdeAutoPauseService> _logger;
    private readonly ISyncLogger _syncLogger;

    public BdeAutoPauseService(ApplicationDbContext ctx, IBdeShiftCalendarService calendar,
        IAppSettingRepository settings, ILogger<BdeAutoPauseService> logger,
        ISyncLogger syncLogger)
    {
        _ctx = ctx;
        _calendar = calendar;
        _settings = settings;
        _logger = logger;
        _syncLogger = syncLogger;
    }

    public async Task<AutoPauseResult> RunAsync(CancellationToken ct)
    {
        await using var run = await _syncLogger.BeginRunAsync(SyncLogServices.BdeAutoPause, ct);
        var errors = new List<string>();
        int paused = 0;
        int checkedCount = 0;

        try
        {
            var enabled = (await _settings.GetValueAsync(AppSettingKeys.BdeSchichtkalenderAktiv))?
                .Equals("true", StringComparison.OrdinalIgnoreCase) == true;
            if (!enabled)
            {
                await run.FinishSuccessAsync(new Dictionary<string, int>
                {
                    ["geprueft"] = 0, ["pausiert"] = 0, ["fehler"] = 0,
                }, messageSuffix: "deaktiviert", ct: ct);
                return new AutoPauseResult(0, 0, new());
            }

            var active = await _ctx.BdeBookings
                .Where(b => b.Status == BdeBookingStatus.Running && b.EndedAt == null && !b.IsCancelled)
                .ToListAsync(ct);

            checkedCount = active.Count;
            var now = DateTime.Now;

            foreach (var booking in active)
            {
                try
                {
                    var shiftEnd = await _calendar.GetShiftEndForBookingAsync(booking.ProductionWorkplaceId, booking.StartedAt);
                    if (shiftEnd == null) continue;
                    if (shiftEnd > now) continue; // Schichtende noch in der Zukunft

                    booking.Status = BdeBookingStatus.AutoPaused;
                    booking.EndedAt = shiftEnd;
                    booking.ModifiedAt = DateTime.Now;
                    booking.ModifiedBy = "BDE-AutoPause";
                    booking.ModifiedByWindows = "BDE-AutoPause";
                    await _ctx.SaveChangesAsync(ct);

                    await run.LogInfoAsync($"Booking auto-paused (Schichtende {shiftEnd.Value:HH:mm})",
                                           reference: $"booking/{booking.Id}", ct: ct);
                    paused++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Auto-Pause failed for bookingId={BookingId}", booking.Id);
                    errors.Add($"Booking {booking.Id}: {ex.GetType().Name} — {ex.Message}");
                    await run.LogWarningAsync($"Auto-Pause fehlgeschlagen: {ex.Message}",
                                              reference: $"booking/{booking.Id}", ct: ct);
                }
            }

            _logger.LogInformation("BdeAutoPause: checked={Checked} paused={Paused} errors={Errors}",
                checkedCount, paused, errors.Count);

            await run.FinishSuccessAsync(new Dictionary<string, int>
            {
                ["geprueft"] = checkedCount,
                ["pausiert"] = paused,
                ["fehler"] = errors.Count,
            }, ct: ct);

            return new AutoPauseResult(checkedCount, paused, errors);
        }
        catch (Exception ex)
        {
            await run.LogErrorAsync(ex.Message, ct: ct);
            await run.FinishFailedAsync(ex.Message, ct: ct);
            throw;
        }
    }
}
