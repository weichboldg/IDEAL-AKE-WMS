using IdealAkeWms.Data;
using IdealAkeWms.Filters;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IdealAkeWms.Controllers;

[RequireAdminAccess]
public class BdeShiftCalendarController : Controller
{
    private readonly ApplicationDbContext _ctx;
    private readonly ICurrentUserService _userSvc;

    public BdeShiftCalendarController(ApplicationDbContext ctx, ICurrentUserService userSvc)
    {
        _ctx = ctx;
        _userSvc = userSvc;
    }

    public async Task<IActionResult> Index()
    {
        var shifts = await _ctx.BdeShifts
            .Where(s => s.ProductionWorkplaceId == null)
            .OrderBy(s => s.DayOfWeek).ThenBy(s => s.StartTime)
            .ToListAsync();
        return View(shifts);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BdeShiftEditViewModel vm)
    {
        if (vm.EndTime <= vm.StartTime)
            ModelState.AddModelError(nameof(vm.EndTime), "Ende muss nach dem Beginn liegen.");

        if (!ModelState.IsValid)
        {
            TempData["WarningMessage"] = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            if (vm.ProductionWorkplaceId.HasValue)
                return RedirectToAction("Edit", "ProductionWorkplaces", new { id = vm.ProductionWorkplaceId.Value });
            return RedirectToAction(nameof(Index));
        }

        _ctx.BdeShifts.Add(new BdeShift
        {
            DayOfWeek = vm.DayOfWeek,
            StartTime = vm.StartTime,
            EndTime = vm.EndTime,
            Name = vm.Name,
            ProductionWorkplaceId = vm.ProductionWorkplaceId,
            CreatedAt = DateTime.Now,
            CreatedBy = _userSvc.GetDisplayName(),
            CreatedByWindows = _userSvc.GetWindowsUserName()
        });
        await _ctx.SaveChangesAsync();

        TempData["SuccessMessage"] = "Schicht hinzugefuegt.";
        if (vm.ProductionWorkplaceId.HasValue)
            return RedirectToAction("Edit", "ProductionWorkplaces", new { id = vm.ProductionWorkplaceId.Value });
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var shift = await _ctx.BdeShifts.FindAsync(id);
        if (shift != null)
        {
            var workplaceId = shift.ProductionWorkplaceId;
            _ctx.BdeShifts.Remove(shift);
            await _ctx.SaveChangesAsync();
            TempData["SuccessMessage"] = "Schicht entfernt.";
            if (workplaceId.HasValue)
                return RedirectToAction("Edit", "ProductionWorkplaces", new { id = workplaceId.Value });
        }
        return RedirectToAction(nameof(Index));
    }
}
