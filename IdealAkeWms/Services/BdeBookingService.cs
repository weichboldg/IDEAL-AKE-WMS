using IdealAkeWms.Data;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace IdealAkeWms.Services;

/// <summary>
/// State-Machine fuer BDE-Buchungen. Regeln:
/// - Max. 1 aktive Buchung pro Operator (EndedAt IS NULL AND !IsCancelled)
/// - Max. 1 aktive Buchung pro WorkOperation
/// - Paused-Buchungen haben EndedAt gesetzt (terminal bis Resume)
/// - Resume legt NEUE Buchung mit ParentBookingId an
/// - Transaktionen kapseln Auto-Close + neues Insert (filtered unique index).
/// </summary>
public class BdeBookingService : IBdeBookingService
{
    private readonly ApplicationDbContext _ctx;
    private readonly ICurrentUserService _userSvc;
    private readonly IAppSettingRepository _settings;

    public BdeBookingService(ApplicationDbContext ctx, ICurrentUserService userSvc, IAppSettingRepository settings)
    {
        _ctx = ctx;
        _userSvc = userSvc;
        _settings = settings;
    }

    public Task<BdeBookingResult> StartSetupAsync(int operatorId, int workOperationId, int workplaceId, int terminalId)
        => StartPlannedAsync(operatorId, workOperationId, workplaceId, terminalId, BdeBookingType.Setup);

    public Task<BdeBookingResult> StartProductionAsync(int operatorId, int workOperationId, int workplaceId, int terminalId)
        => StartPlannedAsync(operatorId, workOperationId, workplaceId, terminalId, BdeBookingType.Production);

    private Task<BdeBookingResult> StartPlannedAsync(int operatorId, int workOperationId, int workplaceId, int terminalId, BdeBookingType type)
    {
        return InTransactionAsync(async () =>
        {
            // Phase-2.1-Gate: Werkbank muss BDE-aktiv sein
            var gateError = await EnsureWorkplaceIsBdeActiveAsync(workplaceId);
            if (gateError != null) return gateError;

            // Settings lesen
            var multiMa = await ReadBoolSettingAsync(AppSettingKeys.BdeMehrfachBuchungProArbeitsgang);
            var multiOp = await ReadBoolSettingAsync(AppSettingKeys.BdeMehrfachBuchungProOperator);

            // Collision-Check (Rule 0): Setup immer strikt, Production nur wenn multiMa=false
            // Queries über alle Operatoren → bleibt separate DB-Abfrage
            if (type == BdeBookingType.Setup || !multiMa)
            {
                var existingOnWo = await _ctx.BdeBookings
                    .Include(b => b.BdeOperator).Include(b => b.ProductionWorkplace)
                    .FirstOrDefaultAsync(b => b.WorkOperationId == workOperationId && b.EndedAt == null && !b.IsCancelled);

                if (existingOnWo != null && existingOnWo.BdeOperatorId != operatorId)
                    return BdeBookingResult.Collision(existingOnWo);
            }

            // Einmalige DB-Abfrage: alle aktiven Buchungen dieses Operators (Rules 1-4)
            var activeOwn = await _ctx.BdeBookings
                .Where(b => b.BdeOperatorId == operatorId && b.EndedAt == null && !b.IsCancelled)
                .ToListAsync();

            var setupSameWo  = activeOwn.FirstOrDefault(b => b.BookingType == BdeBookingType.Setup   && b.WorkOperationId == workOperationId);
            var setupOtherWo = activeOwn.FirstOrDefault(b => b.BookingType == BdeBookingType.Setup   && b.WorkOperationId != workOperationId);
            var activity     = activeOwn.FirstOrDefault(b => b.BookingType == BdeBookingType.Activity);
            var production   = activeOwn.FirstOrDefault(b => b.BookingType == BdeBookingType.Production);

            // Rule 1 (Terminal): Setup auf demselben AG + type=Production → Transition
            if (type == BdeBookingType.Production && setupSameWo != null)
            {
                await FinishAndSaveAsync(setupSameWo, null, null);
                return await CreatePlannedAsync(operatorId, workOperationId, workplaceId, terminalId, type, setupSameWo.Id);
            }

            // Rule 2 (Cumulative): Setup auf anderem AG schließen
            if (setupOtherWo != null)
                await FinishAndSaveAsync(setupOtherWo, null, null);

            // Rule 3 (Cumulative): Activity schließen
            if (activity != null)
                await FinishAndSaveAsync(activity, null, null);

            // Rule 4 (Terminal): Production-Prüfung
            if (production != null)
            {
                if (type == BdeBookingType.Setup)
                    return BdeBookingResult.QuantityRequired(production);

                // type == Production
                if (!multiOp)
                    return BdeBookingResult.QuantityRequired(production);
                // multiOp=true → weiter zum Create
            }

            return await CreatePlannedAsync(operatorId, workOperationId, workplaceId, terminalId, type, null);
        });
    }

    public Task<BdeBookingResult> StartActivityAsync(int operatorId, int activityId, int workplaceId, int terminalId)
    {
        return InTransactionAsync(async () =>
        {
            var gateError = await EnsureWorkplaceIsBdeActiveAsync(workplaceId);
            if (gateError != null) return gateError;

            var existingOwn = await _ctx.BdeBookings
                .FirstOrDefaultAsync(b => b.BdeOperatorId == operatorId && b.EndedAt == null && !b.IsCancelled);

            if (existingOwn != null)
            {
                if (existingOwn.BookingType == BdeBookingType.Production)
                    return BdeBookingResult.QuantityRequired(existingOwn);
                await FinishAndSaveAsync(existingOwn, null, null);
            }

            var now = DateTime.Now;
            var booking = new BdeBooking
            {
                BdeOperatorId = operatorId,
                BdeActivityId = activityId,
                ProductionWorkplaceId = workplaceId,
                BdeTerminalId = terminalId,
                BookingType = BdeBookingType.Activity,
                Status = BdeBookingStatus.Running,
                StartedAt = now
            };
            SetAudit(booking);
            _ctx.BdeBookings.Add(booking);
            await _ctx.SaveChangesAsync();
            return BdeBookingResult.Success(booking);
        });
    }

    public async Task<BdeBookingResult> PauseAsync(int bookingId, decimal? goodQty = null, decimal? scrapQty = null)
    {
        var b = await _ctx.BdeBookings.FindAsync(bookingId);
        if (b == null) return BdeBookingResult.NotFound();
        if (b.Status != BdeBookingStatus.Running)
            return BdeBookingResult.Invalid("Buchung ist nicht aktiv.");

        b.Status = BdeBookingStatus.Paused;
        b.EndedAt = DateTime.Now;
        SetAuditModified(b);

        if (b.BookingType == BdeBookingType.Production && (goodQty.HasValue || scrapQty.HasValue))
        {
            _ctx.BdeBookingQuantities.Add(CreatePartialQuantity(b, goodQty ?? 0, scrapQty ?? 0));
        }

        await _ctx.SaveChangesAsync();
        return BdeBookingResult.Success(b);
    }

    public Task<BdeBookingResult> ResumeAsync(int pausedBookingId, int operatorId, BdeBookingType resumeAs, int workplaceId, int terminalId)
    {
        return InTransactionAsync(async () =>
        {
            var gateError = await EnsureWorkplaceIsBdeActiveAsync(workplaceId);
            if (gateError != null) return gateError;

            var parent = await _ctx.BdeBookings.FindAsync(pausedBookingId);
            if (parent == null) return BdeBookingResult.NotFound();
            if (parent.Status != BdeBookingStatus.Paused)
                return BdeBookingResult.Invalid("Ziel-Buchung ist nicht pausiert.");

            if (parent.WorkOperationId.HasValue)
            {
                var collision = await _ctx.BdeBookings
                    .Include(b => b.BdeOperator).Include(b => b.ProductionWorkplace)
                    .FirstOrDefaultAsync(b => b.WorkOperationId == parent.WorkOperationId && b.EndedAt == null && !b.IsCancelled);
                if (collision != null)
                    return BdeBookingResult.Collision(collision);
            }

            var existingOwn = await _ctx.BdeBookings
                .FirstOrDefaultAsync(b => b.BdeOperatorId == operatorId && b.EndedAt == null && !b.IsCancelled);
            if (existingOwn != null)
            {
                if (existingOwn.BookingType == BdeBookingType.Production)
                    return BdeBookingResult.QuantityRequired(existingOwn);
                await FinishAndSaveAsync(existingOwn, null, null);
            }

            var now = DateTime.Now;
            var newBooking = new BdeBooking
            {
                BdeOperatorId = operatorId,
                ProductionWorkplaceId = workplaceId,
                BdeTerminalId = terminalId,
                WorkOperationId = parent.WorkOperationId,
                BdeActivityId = parent.BdeActivityId,
                BookingType = resumeAs,
                Status = BdeBookingStatus.Running,
                StartedAt = now,
                ParentBookingId = parent.Id
            };
            SetAudit(newBooking);
            _ctx.BdeBookings.Add(newBooking);
            await _ctx.SaveChangesAsync();
            return BdeBookingResult.Success(newBooking);
        });
    }

    public async Task<BdeBookingResult> FinishAsync(int bookingId, decimal? goodQty = null, decimal? scrapQty = null)
    {
        var b = await _ctx.BdeBookings.FindAsync(bookingId);
        if (b == null) return BdeBookingResult.NotFound();
        if (b.Status == BdeBookingStatus.Finished)
            return BdeBookingResult.Invalid("Bereits beendet.");

        if (b.BookingType == BdeBookingType.Production && !goodQty.HasValue && !scrapQty.HasValue)
            return BdeBookingResult.QuantityRequired(b);

        await FinishAndSaveAsync(b, goodQty, scrapQty);
        return BdeBookingResult.Success(b);
    }

    public async Task<BdeBookingResult> ReportPartialQuantityAsync(int bookingId, decimal goodQty, decimal scrapQty)
    {
        var b = await _ctx.BdeBookings.FindAsync(bookingId);
        if (b == null) return BdeBookingResult.NotFound();
        if (b.Status != BdeBookingStatus.Running || b.BookingType != BdeBookingType.Production)
            return BdeBookingResult.Invalid("Teilmengen nur auf laufender Production.");

        _ctx.BdeBookingQuantities.Add(CreatePartialQuantity(b, goodQty, scrapQty));
        await _ctx.SaveChangesAsync();
        return BdeBookingResult.Success(b);
    }

    // --- Interne Helfer ---

    private async Task<BdeBookingResult> CreatePlannedAsync(int operatorId, int workOperationId, int workplaceId, int terminalId, BdeBookingType type, int? parentId)
    {
        var now = DateTime.Now;
        var booking = new BdeBooking
        {
            BdeOperatorId = operatorId,
            WorkOperationId = workOperationId,
            ProductionWorkplaceId = workplaceId,
            BdeTerminalId = terminalId,
            BookingType = type,
            Status = BdeBookingStatus.Running,
            StartedAt = now,
            ParentBookingId = parentId
        };
        SetAudit(booking);
        _ctx.BdeBookings.Add(booking);
        await _ctx.SaveChangesAsync();
        return BdeBookingResult.Success(booking);
    }

    private Task FinishInternalAsync(BdeBooking b, decimal? goodQty, decimal? scrapQty)
    {
        b.Status = BdeBookingStatus.Finished;
        b.EndedAt = DateTime.Now;
        SetAuditModified(b);

        if (b.BookingType == BdeBookingType.Production && (goodQty.HasValue || scrapQty.HasValue))
        {
            var q = CreatePartialQuantity(b, goodQty ?? 0, scrapQty ?? 0);
            q.IsFinal = true;
            _ctx.BdeBookingQuantities.Add(q);
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// Schliesst eine Buchung UND speichert direkt. Muss vor dem Add einer neuen
    /// Running-Buchung auf derselben WorkOperationId/BdeOperatorId aufgerufen werden,
    /// um Reihenfolge-Kollisionen mit den gefilterten Unique-Indizes zu vermeiden.
    /// </summary>
    private async Task FinishAndSaveAsync(BdeBooking b, decimal? goodQty, decimal? scrapQty)
    {
        await FinishInternalAsync(b, goodQty, scrapQty);
        await _ctx.SaveChangesAsync();
    }

    private BdeBookingQuantity CreatePartialQuantity(BdeBooking b, decimal good, decimal scrap)
    {
        var q = new BdeBookingQuantity
        {
            BdeBookingId = b.Id,
            BdeOperatorId = b.BdeOperatorId,
            GoodQuantity = good,
            ScrapQuantity = scrap,
            IsFinal = false,
            ReportedAt = DateTime.Now
        };
        SetAudit(q);
        return q;
    }

    private void SetAudit(AuditableEntity e)
    {
        e.CreatedAt = DateTime.Now;
        e.CreatedBy = _userSvc.GetDisplayName();
        e.CreatedByWindows = _userSvc.GetWindowsUserName();
    }

    private void SetAuditModified(AuditableEntity e)
    {
        e.ModifiedAt = DateTime.Now;
        e.ModifiedBy = _userSvc.GetDisplayName();
        e.ModifiedByWindows = _userSvc.GetWindowsUserName();
    }

    /// <summary>
    /// Gate-Check: verifiziert, dass die Ziel-Werkbank BDE-aktiv ist.
    /// Rueckgabe null bedeutet "OK, weiter", sonst ein Invalid-Result das der Aufrufer zurueckgeben muss.
    /// </summary>
    private async Task<BdeBookingResult?> EnsureWorkplaceIsBdeActiveAsync(int workplaceId)
    {
        var workplace = await _ctx.ProductionWorkplaces.FindAsync(workplaceId);
        if (workplace == null || !workplace.BdeAktiv)
            return BdeBookingResult.Invalid("Werkbank ist nicht für BDE aktiviert.");
        return null;
    }

    /// <summary>
    /// Fuehrt action in einer Transaktion aus. Fuer InMemory-DB (Tests) wird die
    /// Transaktion uebersprungen, da InMemory keine Transaktionen unterstuetzt.
    /// </summary>
    private async Task<BdeBookingResult> InTransactionAsync(Func<Task<BdeBookingResult>> action)
    {
        // InMemory provider does not support transactions
        if (_ctx.Database.ProviderName?.Contains("InMemory", StringComparison.OrdinalIgnoreCase) == true)
            return await action();

        using IDbContextTransaction tx = await _ctx.Database.BeginTransactionAsync();
        var result = await action();
        await tx.CommitAsync();
        return result;
    }

    private async Task<bool> ReadBoolSettingAsync(string key)
    {
        var value = await _settings.GetValueAsync(key);
        return value?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;
    }
}
