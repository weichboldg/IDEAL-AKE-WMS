using IdealAkeWms.Models;
using Microsoft.EntityFrameworkCore;

namespace IdealAkeWms.Data.Repositories;

/// <summary>v1.99.38: Warteschlange der AG-Anlagen in Sage-FAs (Web: lesen + einreihen; gesendet wird im Dienst).</summary>
public interface ISageFaArbeitsgangAnlageRepository
{
    Task<List<SageFaArbeitsgangAnlage>> GetByBelIdsAsync(IReadOnlyCollection<int> belIds);

    /// <summary>Reiht je (BelId, Sage-AG) ein: neue Zeile, oder eine Fehler-Zeile wieder auf Offen. Laufende bzw. erledigte Zeilen
    /// (Offen, Gesendet, Angelegt, in Sage vorhanden) bleiben unberuehrt. Liefert die Anzahl eingereihter Zeilen.</summary>
    Task<int> EinreihenAsync(IReadOnlyList<SageFaArbeitsgangAnlage> neu, string benutzer, string benutzerWindows);

    /// <summary>v1.99.42 (N1): unklare Zeile nach Pruefung in Sage von Hand als „in Sage vorhanden“ bestaetigen. false = keine unklare Zeile.</summary>
    Task<bool> UnklarBestaetigenAsync(int belId, string sageAg, string benutzer, string benutzerWindows);
}

public class SageFaArbeitsgangAnlageRepository : ISageFaArbeitsgangAnlageRepository
{
    private readonly ApplicationDbContext _db;

    public SageFaArbeitsgangAnlageRepository(ApplicationDbContext db) => _db = db;

    public Task<List<SageFaArbeitsgangAnlage>> GetByBelIdsAsync(IReadOnlyCollection<int> belIds) =>
        _db.SageFaArbeitsgangAnlagen.AsNoTracking().Where(a => belIds.Contains(a.BelId)).ToListAsync();

    public async Task<int> EinreihenAsync(IReadOnlyList<SageFaArbeitsgangAnlage> neu, string benutzer, string benutzerWindows)
    {
        var ids = neu.Select(n => n.BelId).Distinct().ToList();
        var vorhanden = await _db.SageFaArbeitsgangAnlagen.Where(a => ids.Contains(a.BelId)).ToListAsync();
        var jetzt = DateTime.Now;
        var anzahl = 0;
        // App-Guard vor dem UNIQUE-Index (InMemory erzwingt ihn nicht); der Index entscheidet bei zwei gleichzeitigen Benutzern.
        foreach (var n in neu.DistinctBy(n => (n.BelId, n.SageArbeitsgangnummer.ToUpperInvariant())))
        {
            var alt = vorhanden.FirstOrDefault(a => a.BelId == n.BelId
                && string.Equals(a.SageArbeitsgangnummer, n.SageArbeitsgangnummer, StringComparison.OrdinalIgnoreCase));
            if (alt == null)
            {
                n.Status = SageFaArbeitsgangStatus.Offen;
                n.CreatedAt = jetzt;
                n.CreatedBy = benutzer;
                n.CreatedByWindows = benutzerWindows;
                _db.SageFaArbeitsgangAnlagen.Add(n);
                anzahl++;
            }
            else if (alt.Status == SageFaArbeitsgangStatus.Fehler)
            {
                alt.Status = SageFaArbeitsgangStatus.Offen;
                alt.Reihenfolge = n.Reihenfolge;
                alt.Kuerzel = n.Kuerzel;
                alt.ModifiedAt = jetzt;
                alt.ModifiedBy = benutzer;
                alt.ModifiedByWindows = benutzerWindows;
                anzahl++;
            }
        }
        await _db.SaveChangesAsync();
        return anzahl;
    }

    public async Task<bool> UnklarBestaetigenAsync(int belId, string sageAg, string benutzer, string benutzerWindows)
    {
        var ag = sageAg.Trim();
        var a = (await _db.SageFaArbeitsgangAnlagen.Where(x => x.BelId == belId).ToListAsync())
            .FirstOrDefault(x => string.Equals(x.SageArbeitsgangnummer.Trim(), ag, StringComparison.OrdinalIgnoreCase));
        if (a is null || !IdealAkeWms.Services.SageFaArbeitsgaenge.IstUnklar(a)) return false;
        a.Status = SageFaArbeitsgangStatus.InSageVorhanden;
        a.Fehler = null;
        a.ModifiedAt = DateTime.Now;
        a.ModifiedBy = benutzer;
        a.ModifiedByWindows = benutzerWindows;
        await _db.SaveChangesAsync();
        return true;
    }
}
