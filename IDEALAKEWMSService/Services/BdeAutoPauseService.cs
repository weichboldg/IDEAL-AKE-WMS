using IdealAkeWms.Data;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IDEALAKEWMSService.Services;

public class BdeAutoPauseService : IBdeAutoPauseService
{
    private readonly ApplicationDbContext _ctx;
    private readonly IBdeShiftCalendarService _calendar;
    private readonly IAppSettingRepository _settings;
    private readonly ILogger<BdeAutoPauseService> _logger;

    public BdeAutoPauseService(ApplicationDbContext ctx, IBdeShiftCalendarService calendar,
        IAppSettingRepository settings, ILogger<BdeAutoPauseService> logger)
    {
        _ctx = ctx;
        _calendar = calendar;
        _settings = settings;
        _logger = logger;
    }

    public async Task<AutoPauseResult> RunAsync(CancellationToken ct)
    {
        var enabled = (await _settings.GetValueAsync(AppSettingKeys.BdeSchichtkalenderAktiv))?
            .Equals("true", StringComparison.OrdinalIgnoreCase) == true;
        if (!enabled)
            return new AutoPauseResult(0, 0, new());

        var active = await _ctx.BdeBookings
            .Where(b => b.Status == BdeBookingStatus.Running && b.EndedAt == null && !b.IsCancelled)
            .ToListAsync(ct);

        var errors = new List<string>();
        var now = DateTime.Now;
        var paused = 0;

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
                paused++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Auto-Pause failed for bookingId={BookingId}", booking.Id);
                errors.Add($"Booking {booking.Id}: {ex.GetType().Name} — {ex.Message}");
            }
        }

        _logger.LogInformation("BdeAutoPause: checked={Checked} paused={Paused} errors={Errors}",
            active.Count, paused, errors.Count);

        return new AutoPauseResult(active.Count, paused, errors);
    }
}
