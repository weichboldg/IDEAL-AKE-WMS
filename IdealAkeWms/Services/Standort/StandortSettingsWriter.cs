using IdealAkeWms.Data;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace IdealAkeWms.Services.Standort;

/// <summary>
/// IDEAL Teil 6: schreibt AppSettings- UND ServiceSettings-Werte eines Speichervorgangs **atomar**
/// (eine gemeinsame EF-Transaktion). Der EINE bewusst zugelassene Ort mit direktem
/// <see cref="ApplicationDbContext"/>-Zugriff (dünner Unit-of-Work-Baustein statt Context-Zugriff im
/// Controller) — Spec „Technischer Lösungsentwurf" / ADR-0001-Ausnahme, hier begründet.
///
/// Warum nötig: <c>AppSettingRepository.SetValueAsync</c> und <c>ServiceSettingRepository.UpsertAsync</c>
/// rufen jeweils **intern** <c>SaveChangesAsync</c> — es gibt keine Transaktions-Naht. Ohne diesen
/// Baustein wäre ein Teil-Speichern möglich (AppSettings geschrieben, ServiceSettings abgelehnt).
/// </summary>
public interface IStandortSettingsWriter
{
    /// <summary>
    /// Schreibt alle übergebenen Werte in einer Transaktion. Schlägt ein Teil fehl, wird alles
    /// zurückgerollt (relationaler Provider) und die Ausnahme propagiert — kein Partial-Save.
    /// Die AppSettings-Cache-Keys werden erst nach erfolgreichem Commit invalidiert.
    /// </summary>
    Task SaveAtomicAsync(
        IReadOnlyDictionary<string, string?> appSettings,
        IReadOnlyList<(string Key, string Value, string? Category, string? Description)> serviceSettings,
        CancellationToken ct = default);
}

public class StandortSettingsWriter : IStandortSettingsWriter
{
    private readonly ApplicationDbContext _ctx;
    private readonly IMemoryCache _cache;

    public StandortSettingsWriter(ApplicationDbContext ctx, IMemoryCache cache)
    {
        _ctx = ctx;
        _cache = cache;
    }

    public async Task SaveAtomicAsync(
        IReadOnlyDictionary<string, string?> appSettings,
        IReadOnlyList<(string Key, string Value, string? Category, string? Description)> serviceSettings,
        CancellationToken ct = default)
    {
        // InMemory (Tests) kennt keine Transaktionen -> nur beim relationalen Provider eine öffnen;
        // die Atomarität am SaveChanges (ein Commit) bleibt in beiden Fällen gewahrt.
        var useTx = _ctx.Database.IsRelational();
        var tx = useTx ? await _ctx.Database.BeginTransactionAsync(ct) : null;
        try
        {
            foreach (var (key, value) in appSettings)
            {
                var existing = await _ctx.AppSettings.FindAsync(new object[] { key }, ct);
                var safe = value ?? string.Empty; // AppSettings.Value ist NOT NULL (wie SetValueAsync)
                if (existing != null)
                    existing.Value = safe;
                else
                    _ctx.AppSettings.Add(new AppSetting { Key = key, Value = safe });
            }

            foreach (var (key, value, category, description) in serviceSettings)
            {
                var existing = await _ctx.ServiceSettings.FindAsync(new object[] { key }, ct);
                if (existing != null)
                {
                    existing.Value = value;
                    if (category != null) existing.Category = category;
                    if (description != null) existing.Description = description;
                }
                else
                {
                    _ctx.ServiceSettings.Add(new ServiceSetting
                    {
                        Key = key,
                        Value = value,
                        Category = category,
                        Description = description
                    });
                }
            }

            await _ctx.SaveChangesAsync(ct);
            if (tx != null) await tx.CommitAsync(ct);

            // Cache-Invalidierung ERST nach Commit (Fallstrick: CachedSettingRepository entfernt sonst
            // vor dem Commit; bei Rollback bliebe der Cache leer und würde beim nächsten Lesen den
            // ungespeicherten Zwischenstand nachladen).
            foreach (var key in appSettings.Keys)
                _cache.Remove(CachedSettingRepository.CachePrefix + key);
        }
        catch
        {
            if (tx != null) await tx.RollbackAsync(ct);
            // Change-Tracker der geteilten scoped-Instanz sauber zurücksetzen — EF revertet ihn bei
            // einem Rollback NICHT automatisch, sonst driftet der Context im selben Request.
            foreach (var entry in _ctx.ChangeTracker.Entries()
                         .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
                         .ToList())
            {
                if (entry.State == EntityState.Added)
                    entry.State = EntityState.Detached;
                else
                    await entry.ReloadAsync(ct);
            }
            throw;
        }
        finally
        {
            if (tx != null) await tx.DisposeAsync();
        }
    }
}
