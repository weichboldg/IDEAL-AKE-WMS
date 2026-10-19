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

            // v1.63.0: Start-Sperre an Fertigmeldungs-Werkbaenken (FA-c6), dann Familien-Pruefung (FA-a9) — beide VOR
            // jedem Schreiben (Auto-Close der Regeln 1-4).
            var noStartError = await EnsureStartAllowedAsync(workplaceId);
            if (noStartError != null) return noStartError;
            var familyError = await EnsureWorkOperationInFamilyAsync(workOperationId, workplaceId);
            if (familyError != null) return familyError;

            // Fold 2 (Spec §10.6): FA in Sage bereits verpackt/abgeholt -> keine neue
            // Buchung. Liegt VOR der Auto-Close-Transition (Rule 1) — ein laufendes
            // Ruesten bleibt bei abgelehntem Produktion-Start unangetastet.
            var packedError = await EnsureOrderNotPackedAsync(workOperationId);
            if (packedError != null) return packedError;

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

    /// <summary>Mengen-Eingaenge (v1.64.0 FA-5, Q4): negative Gut-/Ausschussmengen weist der Server ab, bevor etwas geschrieben wird.</summary>
    private const string NegativeMengeMeldung = "Mengen dürfen nicht negativ sein.";

    private static bool IstNegativ(decimal? goodQty, decimal? scrapQty) => (goodQty ?? 0) < 0 || (scrapQty ?? 0) < 0;

    public async Task<BdeBookingResult> PauseAsync(int bookingId, decimal? goodQty = null, decimal? scrapQty = null)
    {
        if (IstNegativ(goodQty, scrapQty)) return BdeBookingResult.Invalid(NegativeMengeMeldung);
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
            if (parent.Status != BdeBookingStatus.Paused && parent.Status != BdeBookingStatus.AutoPaused)
                return BdeBookingResult.Invalid("Ziel-Buchung ist nicht pausiert.");

            // Fold 2 (Spec §10.6): Resume erzeugt eine NEUE Buchung -> gleiche Sperre wie
            // Start. Nur fuer geplante Buchungen — pausierte Activity-Buchungen
            // (WorkOperationId NULL, erreichbar ueber das Paused-Panel) NICHT blocken.
            if (parent.WorkOperationId.HasValue)
            {
                // v1.63.0: Start-Sperre (FA-c6) + Familien-Pruefung (FA-a9) nur fuer geplante Buchungen; eine pausierte
                // Taetigkeit bleibt an jeder Werkbank fortsetzbar.
                var noStartError = await EnsureStartAllowedAsync(workplaceId);
                if (noStartError != null) return noStartError;
                var familyError = await EnsureWorkOperationInFamilyAsync(parent.WorkOperationId.Value, workplaceId);
                if (familyError != null) return familyError;

                var packedError = await EnsureOrderNotPackedAsync(parent.WorkOperationId.Value);
                if (packedError != null) return packedError;
            }

            var multiMa = await ReadBoolSettingAsync(AppSettingKeys.BdeMehrfachBuchungProArbeitsgang);
            var multiOp = await ReadBoolSettingAsync(AppSettingKeys.BdeMehrfachBuchungProOperator);

            // Collision-Check: Setup ist immer exklusiv; Production nur wenn multiMa deaktiviert
            if (parent.WorkOperationId.HasValue && (resumeAs == BdeBookingType.Setup || !multiMa))
            {
                var collision = await _ctx.BdeBookings
                    .Include(b => b.BdeOperator).Include(b => b.ProductionWorkplace)
                    .FirstOrDefaultAsync(b => b.WorkOperationId == parent.WorkOperationId && b.EndedAt == null && !b.IsCancelled);
                if (collision != null && collision.BdeOperatorId != operatorId)
                    return BdeBookingResult.Collision(collision);
            }

            // Eigener-Operator-Check: aktive Buchungen des Operators schließen oder ablehnen
            var activeOwn = await _ctx.BdeBookings
                .Where(b => b.BdeOperatorId == operatorId && b.EndedAt == null && !b.IsCancelled)
                .ToListAsync();

            var setupOtherWo = activeOwn.FirstOrDefault(b => b.BookingType == BdeBookingType.Setup && b.WorkOperationId != parent.WorkOperationId);
            if (setupOtherWo != null)
                await FinishAndSaveAsync(setupOtherWo, null, null);

            var activity = activeOwn.FirstOrDefault(b => b.BookingType == BdeBookingType.Activity);
            if (activity != null)
                await FinishAndSaveAsync(activity, null, null);

            var production = activeOwn.FirstOrDefault(b => b.BookingType == BdeBookingType.Production);
            if (production != null)
            {
                if (resumeAs == BdeBookingType.Setup)
                    return BdeBookingResult.QuantityRequired(production);
                if (resumeAs == BdeBookingType.Production && !multiOp)
                    return BdeBookingResult.QuantityRequired(production);
                // multiOp=true → parallele Production erlaubt
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
            // Parent auf Resumed setzen (historische Kontinuitaet, kein "Paused"-Zombie)
            parent.Status = BdeBookingStatus.Resumed;
            SetAuditModified(parent);
            await _ctx.SaveChangesAsync();
            return BdeBookingResult.Success(newBooking);
        });
    }

    /// <summary>
    /// v1.63.0 (FA-c4/c5): Fertigmeldung ohne Start/Stop an einer Werkbank mit wirksamer Option „nur AG-Fertigmeldung".
    /// Speichert EINE Produktionsbuchung Finished mit StartedAt == EndedAt (Dauer 0) und eine End-Menge (IsFinal) — erfuellt
    /// die bestehenden Constraints, jede „erledigt"-Abfrage greift unveraendert. Die Regeln 1-4 der Start-Logik gelten bewusst
    /// nicht: die Buchung ist nie aktiv.
    /// </summary>
    public Task<BdeBookingResult> ReportCompletionAsync(int operatorId, int workOperationId, int workplaceId, int terminalId,
        decimal? goodQty, decimal? scrapQty)
    {
        return InTransactionAsync(async () =>
        {
            var gateError = await EnsureWorkplaceIsBdeActiveAsync(workplaceId);
            if (gateError != null) return gateError;

            var workplace = await _ctx.ProductionWorkplaces.FindAsync(workplaceId);
            if (!WerkbankKuerzelFamilie.NurAgFertigmeldungWirksam(workplace!, await ReadBoolSettingAsync(AppSettingKeys.BdeNurFaMeldung)))
                return BdeBookingResult.Invalid("Diese Werkbank meldet nicht ohne Zeit fertig.");

            var familyError = await EnsureWorkOperationInFamilyAsync(workOperationId, workplaceId);
            if (familyError != null) return familyError;

            var packedError = await EnsureOrderNotPackedAsync(workOperationId);
            if (packedError != null) return packedError;

            if (IstNegativ(goodQty, scrapQty)) return BdeBookingResult.Invalid(NegativeMengeMeldung);
            if ((goodQty ?? 0) == 0 && (scrapQty ?? 0) == 0)
                return BdeBookingResult.Invalid("Bitte eine Gut- oder Ausschussmenge angeben.");

            if (await _ctx.BdeBookingQuantities.AnyAsync(q => q.IsFinal && !q.BdeBooking!.IsCancelled
                    && q.BdeBooking.WorkOperationId == workOperationId))
                return BdeBookingResult.Invalid("Arbeitsgang ist bereits fertig gemeldet.");

            var running = await _ctx.BdeBookings
                .Include(b => b.BdeOperator).Include(b => b.ProductionWorkplace)
                .Where(b => b.WorkOperationId == workOperationId && b.EndedAt == null && !b.IsCancelled)
                .ToListAsync();
            if (running.Any(b => b.BdeOperatorId == operatorId))
                return BdeBookingResult.Invalid("Dieser Arbeitsgang läuft bei Ihnen — bitte dort beenden.");
            // Antwort 7: dieselbe Regel wie beim Produktions-Start — Kollision nur ohne Mehrfachbuchung je Arbeitsgang.
            if (running.Count > 0 && !await ReadBoolSettingAsync(AppSettingKeys.BdeMehrfachBuchungProArbeitsgang))
                return BdeBookingResult.Collision(running[0]);

            var now = DateTime.Now;
            var booking = new BdeBooking
            {
                BdeOperatorId = operatorId,
                WorkOperationId = workOperationId,
                ProductionWorkplaceId = workplaceId,
                BdeTerminalId = terminalId,
                BookingType = BdeBookingType.Production,
                Status = BdeBookingStatus.Finished,
                StartedAt = now,
                EndedAt = now
            };
            SetAudit(booking);
            _ctx.BdeBookings.Add(booking);
            await _ctx.SaveChangesAsync();
            var quantity = CreatePartialQuantity(booking, goodQty ?? 0, scrapQty ?? 0);
            quantity.IsFinal = true;
            quantity.ReportedAt = now;
            _ctx.BdeBookingQuantities.Add(quantity);
            await _ctx.SaveChangesAsync();
            return BdeBookingResult.Success(booking);
        });
    }

    public async Task<BdeBookingResult> FinishAsync(int bookingId, decimal? goodQty = null, decimal? scrapQty = null)
    {
        if (IstNegativ(goodQty, scrapQty)) return BdeBookingResult.Invalid(NegativeMengeMeldung);
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
        if (IstNegativ(goodQty, scrapQty)) return BdeBookingResult.Invalid(NegativeMengeMeldung);
        var b = await _ctx.BdeBookings.FindAsync(bookingId);
        if (b == null) return BdeBookingResult.NotFound();
        if (b.Status != BdeBookingStatus.Running || b.BookingType != BdeBookingType.Production)
            return BdeBookingResult.Invalid("Teilmengen nur auf laufender Production.");

        _ctx.BdeBookingQuantities.Add(CreatePartialQuantity(b, goodQty, scrapQty));
        await _ctx.SaveChangesAsync();
        return BdeBookingResult.Success(b);
    }

    public async Task<GroupFinishResult> FinishGroupAsync(int operatorId, IReadOnlyList<GroupFinishEntry> entries)
    {
        if (entries.Count == 0)
            return new GroupFinishResult(0, "Keine Buchungen angegeben.");
        if (entries.Any(e => IstNegativ(e.GoodQty, e.ScrapQty)))
            return new GroupFinishResult(0, NegativeMengeMeldung);

        var bookingIds = entries.Select(e => e.BookingId).ToList();
        var bookings = await _ctx.BdeBookings
            .Where(b => bookingIds.Contains(b.Id))
            .ToListAsync();

        if (bookings.Count != entries.Count)
            return new GroupFinishResult(0, "Nicht alle Buchungen gefunden.");
        if (bookings.Any(b => b.BdeOperatorId != operatorId))
            return new GroupFinishResult(0, "Buchung gehoert nicht diesem Operator.");
        if (bookings.Any(b => b.BookingType != BdeBookingType.Production))
            return new GroupFinishResult(0, "Gruppen-Abschluss nur fuer Production.");
        if (bookings.Any(b => b.Status == BdeBookingStatus.Finished))
            return new GroupFinishResult(0, "Bereits beendete Buchung enthalten.");

        var now = DateTime.Now;
        foreach (var entry in entries)
        {
            var b = bookings.First(x => x.Id == entry.BookingId);
            b.Status = BdeBookingStatus.Finished;
            b.EndedAt = now;
            SetAuditModified(b);
            if (entry.GoodQty.HasValue || entry.ScrapQty.HasValue)
            {
                var q = CreatePartialQuantity(b, entry.GoodQty ?? 0, entry.ScrapQty ?? 0);
                q.IsFinal = entry.IsFinal;
                _ctx.BdeBookingQuantities.Add(q);
            }
        }
        await _ctx.SaveChangesAsync();
        return new GroupFinishResult(bookings.Count);
    }

    public async Task<CloseOthersResult> CloseOtherBookingsOnWorkOperationAsync(int workOperationId, int exceptOperatorId)
    {
        var others = await _ctx.BdeBookings
            .Where(b => b.WorkOperationId == workOperationId
                     && b.BdeOperatorId != exceptOperatorId
                     && b.EndedAt == null
                     && !b.IsCancelled)
            .ToListAsync();

        if (others.Count == 0)
            return new CloseOthersResult(0);

        var now = DateTime.Now;
        foreach (var b in others)
        {
            b.Status = BdeBookingStatus.Finished;
            b.EndedAt = now;
            SetAuditModified(b);
        }

        await _ctx.SaveChangesAsync();
        return new CloseOthersResult(others.Count);
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

    /// <summary>v1.63.0 (FA-c6): an einer Werkbank mit wirksamer Option „nur AG-Fertigmeldung" kein Ruesten/Produktion.</summary>
    private async Task<BdeBookingResult?> EnsureStartAllowedAsync(int workplaceId)
    {
        var workplace = await _ctx.ProductionWorkplaces.FindAsync(workplaceId);
        if (workplace != null && WerkbankKuerzelFamilie.NurAgFertigmeldungWirksam(workplace, await ReadBoolSettingAsync(AppSettingKeys.BdeNurFaMeldung)))
            return BdeBookingResult.Invalid("An dieser Werkbank wird nur fertig gemeldet.");
        return null;
    }

    /// <summary>
    /// v1.63.0 (FA-a9): der Arbeitsgang muss zu dem gehoeren, was das Terminal anbietet — sonst bucht ein manipulierter POST
    /// jeden AG an jeder BDE-aktiven Werkbank. Normal-Modus: <c>WorkOperation.ProductionWorkplaceId</c> in der Kuerzel-Familie
    /// der Terminal-Werkbank (ohne Kuerzel = nur sie selbst). NurFA-Modus (Antwort 12): <c>ProductionOrder.ProductionWorkplaceId</c>
    /// == Terminal-Werkbank, deckungsgleich mit dem NurFA-Angebot (der Default-AG „01" haengt an der Werkbank des ersten Starters).
    /// Unbekannter AG oder AG/FA ohne Werkbank: abgewiesen. Rueckgabe null bedeutet "OK, weiter".
    /// </summary>
    private async Task<BdeBookingResult?> EnsureWorkOperationInFamilyAsync(int workOperationId, int workplaceId)
    {
        var fremd = BdeBookingResult.Invalid("Dieser Arbeitsgang gehört nicht zu dieser Werkbank.");
        var wo = await _ctx.WorkOperations.AsNoTracking()
            .Where(w => w.Id == workOperationId)
            .Select(w => new { w.ProductionWorkplaceId, OrderWorkplaceId = w.ProductionOrder.ProductionWorkplaceId })
            .FirstOrDefaultAsync();
        if (wo == null) return fremd;

        if (await ReadBoolSettingAsync(AppSettingKeys.BdeNurFaMeldung))
            return wo.OrderWorkplaceId == workplaceId ? null : fremd;

        if (wo.ProductionWorkplaceId is not int woWorkplaceId) return fremd;
        var alle = await _ctx.ProductionWorkplaces.AsNoTracking().ToListAsync();
        var self = alle.FirstOrDefault(w => w.Id == workplaceId);
        return self != null && WerkbankKuerzelFamilie.FamilieIds(self, alle).Contains(woWorkplaceId) ? null : fremd;
    }

    /// <summary>
    /// Fold 2 (v1.26.0, Spec §10.6): BDE-Sperre — ist der FA in Sage bereits
    /// verpackt/abgeholt (ExtraInfo.SageStatus), sind keine NEUEN Buchungen mehr
    /// moeglich (Start + Resume). Laufende Buchungen bleiben unangetastet (bewusst
    /// KEIN Guard in Beenden-/Pausieren-/Mengen-Pfaden); ohne Zusatzinfo-Daten
    /// (Sync aus / kein Satellit) keine Sperre. Rueckgabe null bedeutet "OK, weiter".
    /// </summary>
    private async Task<BdeBookingResult?> EnsureOrderNotPackedAsync(int workOperationId)
    {
        var orderInfo = await _ctx.WorkOperations
            .Where(w => w.Id == workOperationId)
            .Select(w => new
            {
                w.ProductionOrder.OrderNumber,
                SageStatus = w.ProductionOrder.ExtraInfo != null ? w.ProductionOrder.ExtraInfo.SageStatus : null
            })
            .FirstOrDefaultAsync();

        if (orderInfo != null && FaZusatzinfoStatus.IstVerpacktOderAbgeholt(orderInfo.SageStatus))
            return BdeBookingResult.Invalid(
                $"FA {orderInfo.OrderNumber} ist bereits {orderInfo.SageStatus!.Trim()} — keine BDE-Buchung mehr moeglich.");

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
