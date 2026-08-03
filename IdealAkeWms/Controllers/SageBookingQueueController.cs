using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using Microsoft.AspNetCore.Mvc;

namespace IdealAkeWms.Controllers;

/// <summary>
/// Read-only-Monitoring der ausgehenden Sage-Lagerbuchungen (Queue) mit manueller
/// Requeue-Aktion fuer Fehler-Eintraege. Lesen unter <see cref="RequireStockReadAccessAttribute"/>,
/// Requeue unter <see cref="RequireStockKeyUserAccessAttribute"/> (bestehende Rollen, keine neue).
/// </summary>
[RequireStockReadAccess]
public class SageBookingQueueController : Controller
{
    private readonly ISageBookingQueueRepository _queue;
    private readonly ICurrentUserService _currentUserService;

    public SageBookingQueueController(
        ISageBookingQueueRepository queue,
        ICurrentUserService currentUserService)
    {
        _queue = queue;
        _currentUserService = currentUserService;
    }

    public static string StatusText(SageBookingQueueStatus s) => s switch
    {
        SageBookingQueueStatus.Offen => "Offen",
        SageBookingQueueStatus.Gesendet => "Gesendet",
        SageBookingQueueStatus.Bestaetigt => "Bestätigt",
        SageBookingQueueStatus.Fehler => "Fehler",
        _ => s.ToString()
    };

    public static string MovementText(MovementType t) => t switch
    {
        MovementType.Einbuchung => "Einbuchung",
        MovementType.Ausbuchung => "Ausbuchung",
        _ => t.ToString()
    };

    /// <summary>Server-Side-Spaltenfilter: Col-Key -> gerenderter Zell-Text.</summary>
    private static readonly Dictionary<string, Func<SageBookingQueueItem, string?>> ColumnMap = new()
    {
        ["id"] = q => q.Id.ToString(),
        ["status"] = q => StatusText(q.Status),
        ["movement-type"] = q => MovementText(q.StockMovement.MovementType),
        ["article"] = q => q.StockMovement.Article.ArticleNumber,
        ["storage-location"] = q => q.StockMovement.StorageLocation.Code,
        ["quantity"] = q => q.StockMovement.Quantity.ToString("0.###"),
        ["created-at"] = q => q.CreatedAt.ToString("dd.MM.yyyy HH:mm"),
        ["sent-at"] = q => q.SentAt?.ToString("dd.MM.yyyy HH:mm"),
        ["confirmed-at"] = q => q.ConfirmedAt?.ToString("dd.MM.yyyy HH:mm"),
        ["attempts"] = q => q.AttemptCount.ToString(),
        ["error"] = q => q.LastError,
    };

    public async Task<IActionResult> Index(SageBookingQueueStatus? status, int page = 1, int? pageSize = null)
    {
        if (page < 1) page = 1;
        var userDefaultPageSize = await _currentUserService.GetDefaultPageSizeAsync();
        var effectivePageSize = Services.PageSize.Resolve(pageSize, userDefaultPageSize);
        var rawPageSize = Services.PageSize.ResolveRaw(pageSize, userDefaultPageSize);

        var all = await _queue.GetForMonitoringAsync(status);

        // Server-Side-Spaltenfilter ueber ALLE Eintraege, vor der Pagination.
        var columnFilters = ColumnFilterHelper.ReadFromQuery(HttpContext?.Request);
        var filtered = ColumnFilterHelper.Apply(all.AsQueryable(), columnFilters, ColumnMap).ToList();

        var vm = new SageBookingQueueViewModel
        {
            Items = filtered.Skip((page - 1) * effectivePageSize).Take(effectivePageSize).ToList(),
            FilterStatus = status,
            Pagination = new PaginationState
            {
                CurrentPage = page,
                PageSize = effectivePageSize,
                PageSizeRaw = rawPageSize,
                TotalCount = filtered.Count
            }
        };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequireStockKeyUserAccess]
    public async Task<IActionResult> Requeue(int id)
    {
        var item = await _queue.GetByIdWithMovementAsync(id);
        if (item == null)
            return NotFound();

        if (item.Status != SageBookingQueueStatus.Fehler)
        {
            TempData["WarningMessage"] = $"Eintrag #{id} ist nicht im Status Fehler und wird nicht erneut eingereiht.";
            return RedirectToAction(nameof(Index));
        }

        // Zuruecksetzen auf Offen. Der Worker prueft VOR dem erneuten Senden ueber den Sage-Memo-Lookup,
        // ob die Buchung dort bereits existiert (Doppelbuchungs-Schutz, B2) — kein blindes Resend.
        await _queue.RequeueAsync(id, _currentUserService.GetDisplayName());
        TempData["SuccessMessage"] = $"Eintrag #{id} wurde erneut eingereiht. Der Dienst prüft vor dem Senden, ob die Buchung in Sage bereits existiert.";
        return RedirectToAction(nameof(Index));
    }
}
