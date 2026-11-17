using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IdealAkeWms.Models;

namespace IdealAkeWms.Data.Repositories;

/// <summary>Herkunftsschluessel einer Position innerhalb eines HauptFA (die Struktur-Felder von <see cref="FaHierarchyNode"/>;
/// der HauptFA steht am Auftrag). Bewusst ohne Knoten-Id — die wechselt mit jedem Sync.
/// <para>v1.99.34 (Spec 2026-11-16-beschichtungslogik-je-standort, Freigabe-Antwort 20): flache Quelle (AKE) = <see cref="Artnr"/> +
/// <see cref="PositionText"/> (Stuecklisten-Position als Text, <c>5.2</c> ≠ <c>5.20</c>), normalisiert wie
/// <see cref="Services.PickingOrderRestmengen.Schluessel"/>. Ohne Positionstext (IDEAL, Altzeilen) bleiben beide null.</para></summary>
public readonly record struct PositionsSchluessel(int? VaterFA, int SubFA, int? Position, string? Artnr = null, string? PositionText = null)
{
    public static PositionsSchluessel Von(FaHierarchyNode n) => Bilden(n.VaterFA, n.SubFA, n.Position, n.Artnr, n.PositionText);
    public static PositionsSchluessel Von(BeschichtungsauftragPosition p) => Bilden(p.VaterFA, p.SubFA, p.Position, p.Artnr, p.PositionText);

    public static PositionsSchluessel Bilden(int? vaterFa, int subFa, int? position, string? artnr, string? positionText)
    {
        if (positionText is null) return new(vaterFa, subFa, position);
        var (a, t) = Services.PickingOrderRestmengen.Schluessel(artnr ?? "", positionText);
        return new(vaterFa, subFa, position, a, t);
    }

    public override string ToString() => PositionText is not null
        ? $"Artnr {Artnr} / Pos {(PositionText.Length == 0 ? "—" : PositionText)}"
        : $"Vater {VaterFA?.ToString() ?? "—"} / SubFA {SubFA} / Pos {Position?.ToString() ?? "—"}";
}

/// <summary>v1.99.34: eine Lackierteil-Zeile der flachen Quelle — offener WA (Haupt-FA) und seine Stuecklisten-Cache-Zeile.
/// v1.99.36 (Nachtrag 4): Masse des Lackierteils aus <see cref="ArticleSageDetail"/> in Sage-Einheit cm (null = keine Sage-Zeile).</summary>
public sealed record FlacheLackierteilZeile(ProductionOrder Auftrag, string Source, CachedBomItem Item,
    decimal? BreiteCm = null, decimal? HoeheCm = null, decimal? LaengeCm = null);

/// <summary>v1.99.34: flache Quelle samt offener WA mit Beschichtungskennzeichen OHNE Lackierteil-Zeile im Cache (Sammelmeldung A15).
/// v1.99.36 (Nachtrag 4): <see cref="OhneLackierteile"/> nur nicht kommissionierte WA, kommissionierte nur als Anzahl.</summary>
public sealed record FlacheLackierteilDaten(List<FlacheLackierteilZeile> Zeilen, List<string> OhneLackierteile, int OhneLackierteileKommissioniert = 0);

/// <summary>Eine Position steckt in einem sperrenden Auftrag (Status ≠ Storniert).</summary>
public sealed record Vergabe(PositionsSchluessel Schluessel, int AuftragId, string Nummer, int HauptFA);

/// <summary>
/// Datenzugriff fuer den gespeicherten Beschichtungsauftrag (ADR 0001; Spec 2026-10-06-beschichtungsauftrag-ausbau-teil-3).
/// Die fachlichen Regeln liegen im <c>BeschichtungsauftragWorkflow</c>; hier nur Laden, Speichern, Vergabe-Abfrage.
/// </summary>
public interface IBeschichtungsauftragRepository
{
    /// <summary>Auftrag inkl. Positionen, getrackt (fuer Aenderungen). Null = nicht gefunden.</summary>
    Task<Beschichtungsauftrag?> GetMitPositionenAsync(int id);

    /// <summary>Hoechste vergebene laufende Nummer des Jahres (0 = noch keine).</summary>
    Task<int> GetMaxLaufendeNummerAsync(int jahr);

    /// <summary>v1.99.34 (Spec 2026-11-16-beschichtungslogik-je-standort E5): flache Positionsquelle der AKE. Offene WA
    /// (<c>IsDone = 0 AND IsCancelled = 0</c>, Haupt-FA, <c>PickingStatus.HasCoatingParts = 1</c>; <c>IsDonePicking</c> bewusst
    /// nicht gefiltert, A13) und ihre Stuecklisten-Cache-Zeilen der Kategorie <paramref name="lackierteilKategorie"/> — dieselbe
    /// Kette wie die Lackierteil-Erkennung des Dienstes. <paramref name="orderNumbers"/> null = alle offenen WA.</summary>
    Task<FlacheLackierteilDaten> GetFlacheLackierteileAsync(string? lackierteilKategorie, IReadOnlyCollection<string>? orderNumbers = null);

    /// <summary>Alle Positionen des HauptFA in Auftraegen mit Status ≠ Storniert — inkl. Reklamationen
    /// (Spec Anforderung 6, Herleitung 2026-10-12) — mit Auftragsnummer. Eine Abfrage, kein N+1.</summary>
    Task<List<Vergabe>> GetVergabenAsync(int hauptFa);

    /// <summary>Wie <see cref="GetVergabenAsync(int)"/>, fuer mehrere HauptFA in EINER Abfrage (Markierung der
    /// Vorschlagsliste je Seite, Teil 4).</summary>
    Task<List<Vergabe>> GetVergabenAsync(IReadOnlyCollection<int> hauptFas);

    /// <summary>Alle Auftraege ohne Positionen-Tracking (Liste, Teil 4) — inkl. Positionen fuer die Anzahl und
    /// Ursprung/Folgeauftraege fuer die Verweise.</summary>
    Task<List<Beschichtungsauftrag>> GetAlleAsync();

    /// <summary>Ein Auftrag ungetrackt mit Positionen, Ursprung und Folgeauftraegen (Detail, Reklamationsauswahl, Druck).</summary>
    Task<Beschichtungsauftrag?> GetDetailAsync(int id);

    void Add(Beschichtungsauftrag auftrag);
    void Remove(BeschichtungsauftragPosition position);

    /// <summary>Verwirft alle ungespeicherten Aenderungen des Contexts nach einem gescheiterten Speichern (Nummern-Konflikt,
    /// Concurrency, Deadlock): neue Zeilen fallen weg, geaenderte Werte werden beim naechsten Laden neu gelesen.</summary>
    void VerwerfeAenderungen();

    /// <summary>Optimistische Nebenlaeufigkeit: der Aufrufer meldet die RowVersion, die er gesehen hat.</summary>
    void SetOriginalRowVersion(Beschichtungsauftrag auftrag, byte[] rowVersion);

    Task SaveChangesAsync();

    /// <summary>Fuehrt Pruefung + Schreiben in EINER Serializable-Transaktion aus (SQL Server: Key-Range-Locks gegen zwei
    /// gleichzeitige Vergaben derselben freien Position). Nicht-relationaler Provider (InMemory-Tests): direkt.</summary>
    Task<T> InSerializableTransactionAsync<T>(Func<Task<T>> action);
}
