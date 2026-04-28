using IdealAkeWms.Data;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using Microsoft.EntityFrameworkCore;

namespace IdealAkeWms.Services;

public class BdeShiftCalendarService : IBdeShiftCalendarService
{
    private readonly ApplicationDbContext _ctx;
    private readonly IAppSettingRepository _settings;

    public BdeShiftCalendarService(ApplicationDbContext ctx, IAppSettingRepository settings)
    {
        _ctx = ctx;
        _settings = settings;
    }

    public async Task<DateTime?> GetShiftEndForBookingAsync(int workplaceId, DateTime startedAt)
    {
        var enabled = (await _settings.GetValueAsync(AppSettingKeys.BdeSchichtkalenderAktiv))?
            .Equals("true", StringComparison.OrdinalIgnoreCase) == true;
        if (!enabled) return null;

        var workplace = await _ctx.ProductionWorkplaces.AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == workplaceId);
        if (workplace == null) return null;

        // Feiertag pruefen
        var date = startedAt.Date;
        var isHoliday = await _ctx.Holidays.AsNoTracking().AnyAsync(h => h.Date == date);
        if (isHoliday) return null;

        // Schichten laden — Werkbank-Override hat Vorrang wenn Toggle EIN
        var shifts = workplace.BdeUseCustomShiftPlan
            ? await _ctx.BdeShifts.AsNoTracking().Where(s => s.ProductionWorkplaceId == workplaceId && s.DayOfWeek == startedAt.DayOfWeek).ToListAsync()
            : await _ctx.BdeShifts.AsNoTracking().Where(s => s.ProductionWorkplaceId == null && s.DayOfWeek == startedAt.DayOfWeek).ToListAsync();

        if (shifts.Count == 0) return null;

        var startTimeOfDay = startedAt.TimeOfDay;

        // 1) Schicht in der startedAt liegt (EndTime exklusiv: an exakter Schichtgrenze faellt durch zur naechsten Schicht)
        var current = shifts.FirstOrDefault(s => s.StartTime <= startTimeOfDay && startTimeOfDay < s.EndTime);
        if (current != null)
            return date + current.EndTime;

        // 2) Naechstfolgende Schicht des Tages
        var nextLater = shifts.Where(s => s.StartTime > startTimeOfDay).OrderBy(s => s.StartTime).FirstOrDefault();
        if (nextLater != null)
            return date + nextLater.EndTime;

        // 3) Alle Schichten des Tages liegen vor startedAt → kein Auto-Pause
        return null;
    }

    public async Task<IReadOnlyList<BdeShift>> GetShiftsAsync(int workplaceId, DayOfWeek day)
    {
        var workplace = await _ctx.ProductionWorkplaces.AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == workplaceId);
        if (workplace == null) return Array.Empty<BdeShift>();

        return workplace.BdeUseCustomShiftPlan
            ? await _ctx.BdeShifts.AsNoTracking().Where(s => s.ProductionWorkplaceId == workplaceId && s.DayOfWeek == day).ToListAsync()
            : await _ctx.BdeShifts.AsNoTracking().Where(s => s.ProductionWorkplaceId == null && s.DayOfWeek == day).ToListAsync();
    }
}
