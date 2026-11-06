using IdealAkeWms.Data;
using IdealAkeWms.Models;
using Microsoft.EntityFrameworkCore;

namespace IdealAkeWms.Data.Repositories;

public class BdeOperatorRepository : IBdeOperatorRepository
{
    private readonly ApplicationDbContext _ctx;
    public BdeOperatorRepository(ApplicationDbContext ctx) { _ctx = ctx; }

    public Task<List<BdeOperator>> GetAllAsync() =>
        _ctx.BdeOperators.OrderBy(o => o.LastName).ThenBy(o => o.FirstName).ToListAsync();

    public Task<List<BdeOperator>> GetAllActiveAsync() =>
        _ctx.BdeOperators.Where(o => o.IsActive).OrderBy(o => o.LastName).ThenBy(o => o.FirstName).ToListAsync();

    public Task<BdeOperator?> GetByIdAsync(int id) =>
        _ctx.BdeOperators.FirstOrDefaultAsync(o => o.Id == id);

    public Task<BdeOperator?> GetByPersonnelNumberAsync(string personnelNumber) =>
        _ctx.BdeOperators.FirstOrDefaultAsync(o => o.PersonnelNumber == personnelNumber && o.IsActive);

    /// <summary>E9/F11: bei Uebergabe <c>Offen</c> (im WMS angelegt) Outbox-Eintrag vormerken – nicht senden. Zweites SaveChanges wie
    /// <see cref="StorageLocationRepository.AddAsync"/> (Schluessel = WMS-Id).</summary>
    public async Task AddAsync(BdeOperator op)
    {
        _ctx.BdeOperators.Add(op);
        await _ctx.SaveChangesAsync();
        if (op.SageUebergabeStatus == SageUebergabeStatus.Offen)
        {
            SageStammdatenOutbox.Vormerken(_ctx, SageStammdatenOutbox.TypBdeMitarbeiter, op,
                new { op.Id, op.PersonnelNumber, op.FirstName, op.LastName });
            await _ctx.SaveChangesAsync();
        }
    }

    /// <summary>E9/F11: steht der Mitarbeiter auf <c>Uebergeben</c> (Sage-Zuordnung), werden offene Outbox-Eintraege im selben
    /// SaveChanges erledigt – ein kuenftiger Worker sendet ihn dann nicht mehr.</summary>
    public async Task UpdateAsync(BdeOperator op)
    {
        _ctx.BdeOperators.Update(op);
        if (op.SageUebergabeStatus == SageUebergabeStatus.Uebergeben)
            await SageStammdatenOutbox.ErledigenAsync(_ctx, SageStammdatenOutbox.TypBdeMitarbeiter, op.Id,
                op.ModifiedBy ?? op.CreatedBy, op.ModifiedByWindows ?? op.CreatedByWindows);
        await _ctx.SaveChangesAsync();
    }

    public Task<List<SagePpsMitarbeiter>> GetSagePpsMitarbeiterAsync() =>
        _ctx.SagePpsMitarbeiter.AsNoTracking().OrderBy(m => m.Nummer).ToListAsync();

    public Task<BdeOperator?> GetBySageMitarbeiternummerAsync(string sageMitarbeiternummer) =>
        _ctx.BdeOperators.FirstOrDefaultAsync(o => o.SageMitarbeiternummer == sageMitarbeiternummer);
}
