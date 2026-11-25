using IdealAkeWms.Models;

namespace IdealAkeWms.Data.Repositories;

public interface IStorageLocationRepository : IRepository<StorageLocation>
{
    Task<List<StorageLocation>> GetAllOrderedAsync();
    Task<List<StorageLocation>> GetAllOrderedExcludingPickingTransportAsync();
    Task<List<StorageLocation>> GetActiveOrderedExcludingPickingTransportAsync();
    Task<List<StorageLocation>> GetPickingTransportLocationsAsync();
    Task<List<StorageLocation>> GetActivePickingTransportLocationsAsync();
    Task<StorageLocation?> GetByCodeAsync(string code);

    // ----- v1.99.71 Lagerplatz-Anlage in Sage (Spec 2026-11-24-lagerplatz-anlage-wms-nach-sage) -----
    /// <summary>Alle Lagerorte (auch inaktive), sortiert nach Lagerkennung - Auswahl und Eindeutigkeitspruefung des Formulars.</summary>
    Task<List<StorageArea>> GetStorageAreasAsync();
    /// <summary>Alle Platz-Codes (auch inaktive) - Ueberspringen vorhandener Codes ohne Gross/Klein.</summary>
    Task<List<string>> GetAllCodesAsync();
    /// <summary>Lager-Outbox-Eintraege mit Status != Bestaetigt, je StorageArea-Id der juengste.</summary>
    Task<Dictionary<int, SageStammdatenOutboxItem>> GetOffeneLagerUebergabenAsync();
    /// <summary>Lager (optional) + Plaetze + je ein Outbox-Eintrag in EINER Transaktion (S2). Wirft bei Fehler, dann ist nichts angelegt.</summary>
    Task AddSageAnlageAsync(StorageArea? neuesLager, IReadOnlyList<StorageLocation> plaetze);
    /// <summary>„Erneut senden“: Outbox-Eintrag (Typ, Id) mit Status Fehler -> Offen, AttemptCount 0, LastError leer; beim Platz zusaetzlich
    /// SageUebergabeStatus Fehler -> Offen, Fehlertext leer. Audit des Anwenders. false = nichts im Status Fehler gefunden.</summary>
    Task<bool> ErneutSendenAsync(string typ, int id, string modifiedBy, string modifiedByWindows);
}
