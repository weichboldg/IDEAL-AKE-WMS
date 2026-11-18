using IdealAkeWms.Models;

namespace IdealAkeWms.Data.Repositories;

/// <summary>Artikel fuer den BF-Warenkorb (Spec 2026-11-13-blechfertigung-bf-auftraege-wicam-csv F1): Felder aus den Sage-Merkmalen
/// (<see cref="Services.BfArtikelFelder"/>), Bestellmenge aus <c>SageDetail</c> vor dem Merkmal.</summary>
public sealed record BfArtikel(string ArticleNumber, string? Description, string? Matchcode, string? Teileart, string? Material,
    string? Materialnummer, string? FertigungsInfoPN, string? Hauptlagerplatz, decimal? Bestellmenge);

/// <summary>Datenzugriff fuer BF-Auftraege (ADR 0001). Regeln im <c>BfAuftragWorkflow</c>; hier nur Laden, Suchen, Speichern.</summary>
public interface IBfAuftragRepository
{
    /// <summary>Entwurf (Warenkorb) des Benutzers inkl. Positionen, getrackt; null = keiner.</summary>
    Task<BfAuftrag?> GetEntwurfAsync(int userId);
    /// <summary>Auftrag inkl. Positionen, getrackt.</summary>
    Task<BfAuftrag?> GetMitPositionenAsync(int id);
    /// <summary>Mehrere Auftraege getrackt (Fertig melden per Mehrfachauswahl).</summary>
    Task<List<BfAuftrag>> GetMehrereAsync(IReadOnlyCollection<int> ids);
    /// <summary>Ungetrackt mit Positionen und Empfaengergruppe (Detail).</summary>
    Task<BfAuftrag?> GetDetailAsync(int id);
    /// <summary>Alle abgeschickten Auftraege (Nummer gesetzt) ungetrackt mit Positionen (Liste).</summary>
    Task<List<BfAuftrag>> GetAlleAsync();
    /// <summary>Hoechste vergebene laufende Nummer (0 = noch keine); feste Breite, daher Textmaximum = Zahlmaximum.</summary>
    Task<int> GetMaxLaufendeNummerAsync();

    /// <summary>Lagerteile BG/FT (Merkmal IstLagerartikel ≠ 0, Artikeltyp BG/FT) — Filter in SQL, dann <paramref name="limit"/>.</summary>
    Task<List<BfArtikel>> SucheArtikelAsync(string? q, int limit);
    /// <summary>Ein Artikel nur, wenn er den Warenkorb-Filter erfuellt.</summary>
    Task<BfArtikel?> GetArtikelAsync(string articleNumber);

    /// <summary>v1.99.42: aktuelles Kuerzel des Bestellers (null = kein Benutzer/kein Kuerzel).</summary>
    Task<string?> GetBenutzerKuerzelAsync(int? userId);

    void Add(BfAuftrag auftrag);
    void Remove(BfAuftragPosition position);
    void VerwerfeAenderungen();
    void SetOriginalRowVersion(BfAuftrag auftrag, byte[] rowVersion);
    Task SaveChangesAsync();
    Task<T> InSerializableTransactionAsync<T>(Func<Task<T>> action);
}
