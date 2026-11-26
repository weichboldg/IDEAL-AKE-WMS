using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using IdealAkeWms.Services;

namespace IdealAkeWms.Models.ViewModels;

/// <summary>Zeile der Auftragsliste <c>/Beschichtungsauftraege</c> (Spec 2026-10-06-beschichtungsauftrag-ausbau-teil-4,
/// Anforderung 3). Projektion des gespeicherten Auftrags; die Spaltenfilter laufen ueber <see cref="ColumnMap"/> auf
/// genau dem Text, den die Zelle rendert.</summary>
public sealed class BeschichtungsauftragListRow
{
    public int Id { get; init; }
    public string Nummer { get; init; } = string.Empty;
    public int HauptFA { get; init; }
    public BeschichtungsauftragStatus Status { get; init; }
    public string? Dienstleister { get; init; }
    public string? Ral { get; init; }
    public DateTime? StartBeschichtung { get; init; }
    public DateTime? BeschichtenRetour { get; init; }
    public int PositionCount { get; init; }
    public int? UrsprungId { get; init; }
    public string? UrsprungNummer { get; init; }
    public IReadOnlyList<(int Id, string Nummer)> Folgeauftraege { get; init; } = new List<(int, string)>();
    public DateTime? VersendetAt { get; init; }
    public DateTime CreatedAt { get; init; }
    public string CreatedBy { get; init; } = string.Empty;

    public static string Datum(DateTime? d) => d?.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) ?? string.Empty;
    public static string Zeitpunkt(DateTime d) => d.ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture);
    public string ReklamiertText => string.Join(", ", Folgeauftraege.Select(f => f.Nummer));

    public static BeschichtungsauftragListRow Von(Beschichtungsauftrag a) => new()
    {
        Id = a.Id, Nummer = a.Nummer, HauptFA = a.HauptFA, Status = a.Status, Dienstleister = a.Dienstleister, Ral = a.Ral,
        StartBeschichtung = a.StartBeschichtung, BeschichtenRetour = a.BeschichtenRetour, PositionCount = a.Positionen.Count,
        UrsprungId = a.UrsprungsauftragId, UrsprungNummer = a.Ursprungsauftrag?.Nummer,
        Folgeauftraege = a.Folgeauftraege.OrderBy(f => f.Nummer).Select(f => (f.Id, f.Nummer)).ToList(),
        VersendetAt = a.VersendetAt, CreatedAt = a.CreatedAt, CreatedBy = a.CreatedBy,
    };

    /// <summary>Server-Spaltenfilter (ADR 0005): Col-Key → gerenderter Zelltext; Datumsspalten nach der Projektion.</summary>
    public static readonly IReadOnlyDictionary<string, Func<BeschichtungsauftragListRow, string?>> ColumnMap =
        new Dictionary<string, Func<BeschichtungsauftragListRow, string?>>
        {
            ["nummer"] = r => r.Nummer,
            ["hauptfa"] = r => r.HauptFA.ToString(CultureInfo.InvariantCulture),
            ["status"] = r => BeschichtungsauftragWorkflow.Anzeige(r.Status),
            ["dienstleister"] = r => r.Dienstleister,
            ["ral"] = r => r.Ral,
            ["start"] = r => Datum(r.StartBeschichtung),
            ["retour"] = r => Datum(r.BeschichtenRetour),
            ["versendetam"] = r => r.VersendetAt is DateTime v ? Zeitpunkt(v) : null,
            ["positionen"] = r => r.PositionCount.ToString(CultureInfo.InvariantCulture),
            ["ursprung"] = r => r.UrsprungNummer,
            ["reklamiert"] = r => r.ReklamiertText,
            ["angelegtam"] = r => Zeitpunkt(r.CreatedAt),
            ["angelegtvon"] = r => r.CreatedBy,
        };
}

public sealed class BeschichtungsauftragListViewModel
{
    public List<BeschichtungsauftragListRow> Rows { get; init; } = new();
    public BeschichtungsauftragStatus? Status { get; init; }
    public PaginationState Pagination { get; init; } = new();
}

/// <summary>Detail eines Auftrags (Anforderungen 4, 4a, 5, 6): Kopf, Positionen, Historie, Verweise, Positionsauswahl.</summary>
public sealed class BeschichtungsauftragDetailViewModel
{
    public Beschichtungsauftrag Auftrag { get; init; } = new();

    /// <summary>RowVersion als Base64 fuer die Hidden-Felder der Formulare (Concurrency).</summary>
    public string RowVersion => Convert.ToBase64String(Auftrag.RowVersion);

    public bool DarfSchreiben { get; init; }
    public bool PdfVerfuegbar { get; init; }

    /// <summary>Aktueller Sage-Wert (PPS-View), nur wenn er vom Auftrag abweicht; null = gleich oder HauptFA nicht im Cache.</summary>
    public string? SageDienstleisterAbweichend { get; init; }

    /// <summary>Hinzufuegbare Positionen laut Teil-3-Workflow (Erst-Entwurf: freie HauptFA-Positionen; Reklamations-Entwurf:
    /// noch nicht enthaltene Ursprungspositionen). Leer ausserhalb des Entwurfs.</summary>
    public List<PositionsKandidat> Kandidaten { get; init; } = new();

    /// <summary>v1.99.41 A9: Lackierer-Auswahl (aktive Kreditoren mit IsCoater); leer ausserhalb Entwurf/ohne Schreibrecht.</summary>
    public List<Creditor> Beschichter { get; init; } = new();

    /// <summary>Link „Beschichter pflegen“ nur mit Stammdaten-Schreibrecht.</summary>
    public bool DarfStammdaten { get; init; }

    /// <summary>Anzeige je Beschichter „Kurzname — Name, Ort“ (A9).</summary>
    public static string BeschichterText(Creditor c) =>
        $"{c.ShortName} — {c.Name}" + (string.IsNullOrWhiteSpace(c.City) ? "" : $", {c.City}");

    /// <summary>User-Id → Name fuer die Statushistorie.</summary>
    public IReadOnlyDictionary<int, string> Benutzer { get; init; } = new Dictionary<int, string>();

    public bool IstEntwurf => Auftrag.Status == BeschichtungsauftragStatus.Entwurf;
    public bool IstReklamation => Auftrag.UrsprungsauftragId.HasValue;
    public bool Druckbar => BeschichtungsauftraegeDruck.IstDruckbar(Auftrag.Status);

    public IEnumerable<BeschichtungsauftragStatus> ErlaubteZiele =>
        Enum.GetValues<BeschichtungsauftragStatus>().Where(z => BeschichtungsauftragWorkflow.IstErlaubt(Auftrag.Status, z));

    public string BenutzerName(int? id) =>
        id is int i ? (Benutzer.TryGetValue(i, out var n) ? n : $"Benutzer #{i}") : "—";
}

/// <summary>Teileauswahl der Reklamation (Anforderung 5): alle Ursprungspositionen, nichts vorausgewaehlt.</summary>
public sealed class BeschichtungsauftragReklamationViewModel
{
    public Beschichtungsauftrag Ursprung { get; init; } = new();
}

/// <summary>Aussendokument (Anforderung 7): Briefkopf (Empfaenger = Dienstleister des Auftrags), Kopf, Positionen summiert
/// je Artnr (v1.99.87, <see cref="BeschichtungsauftraegeDruck.Summiert"/>) mit Summenzeile, Barcode = Auftragsnummer, sichtbare Spalten der Detail-Positionstabelle.</summary>
public sealed class BeschichtungsauftragPrintViewModel
{
    public Beschichtungsauftrag Auftrag { get; init; } = new();
    /// <summary>v1.99.87 (Spec 2026-11-26-beschichtung-summe-…-teil-1, hebt v1.99.41 A11 „je Position“ fuer Druck/PDF auf):
    /// immer summiert je Artnr (<see cref="BeschichtungsauftraegeDruck.Summiert"/>); Pos. = Index + 1 je Zeile.</summary>
    public List<BeschichtungsauftragDruckZeile> Zeilen { get; init; } = new();
    /// <summary>Summenzeile „Summe Teile im Auftrag“ (S5): Summe aller Mengen.</summary>
    public decimal Summe => Zeilen.Sum(z => z.Menge);
    public string Barcode { get; init; } = string.Empty;
    public IReadOnlyList<string> VisibleColumns { get; init; } = new List<string>();

    /// <summary>Spalte drucken? Leere Auswahl = alle; Spalten, die die Detail-Tabelle nicht kennt (Anzahl Positionen), immer.</summary>
    public bool ShowCol(string key) =>
        VisibleColumns.Count == 0 || VisibleColumns.Contains(key)
        || ColumnDefinitions.BeschichtungsauftragPositionen.Columns.All(c => c.Key != key);
}

/// <summary>Druckregel (Antwort 8): gedruckt wird ab Uebergeben, nie im Entwurf oder storniert.</summary>
public static class BeschichtungsauftraegeDruck
{
    public static bool IstDruckbar(BeschichtungsauftragStatus s) =>
        s is BeschichtungsauftragStatus.Uebergeben or BeschichtungsauftragStatus.Versendet or BeschichtungsauftragStatus.Erledigt;

    /// <summary>Reihenfolge der Detailseite (je Position) und Grundlage der Druckreihenfolge (<see cref="Summiert"/>):
    /// Position, PositionText natuerlich, SubFA; Positionen ohne FA zuletzt.</summary>
    public static List<BeschichtungsauftragPosition> Sortiert(IEnumerable<BeschichtungsauftragPosition> positionen) =>
        positionen.OrderBy(p => p.OhneFa).ThenBy(p => p.Position ?? int.MaxValue)
            .ThenBy(p => p.PositionText, new NaturalPositionComparer()).ThenBy(p => p.SubFA).ToList();

    /// <summary>v1.99.87 Druck/PDF (S1-S4): je (Artnr getrimmt, OhneFa) eine Zeile in der Reihenfolge der jeweils ersten
    /// Position aus <see cref="Sortiert"/> (GroupBy erhaelt die Quellreihenfolge). Positionen ohne Artnr werden nie
    /// zusammengefasst (Schluessel = Position selbst). Eigenschaften und Maße aus der ersten Position,
    /// <see cref="BeschichtungsauftragDruckZeile.MasseUneinheitlich"/> wie <c>AggregateSummiert</c>.</summary>
    public static List<BeschichtungsauftragDruckZeile> Summiert(IEnumerable<BeschichtungsauftragPosition> positionen) =>
        Sortiert(positionen)
            .GroupBy(p => string.IsNullOrWhiteSpace(p.Artnr) ? (object)p : (p.Artnr.Trim(), p.OhneFa))
            .Select(g =>
            {
                var first = g.First();
                return new BeschichtungsauftragDruckZeile(first.HauptArtnr, first.Artnr?.Trim(), first.Matchcode, first.OhneFa,
                    first.Breite, first.Hoehe, first.Tiefe, g.Sum(p => p.Menge), g.Count(), g.Count(p => p.Verpackt),
                    g.Any(p => p.Breite != first.Breite || p.Hoehe != first.Hoehe || p.Tiefe != first.Tiefe));
            })
            .ToList();
}

/// <summary>Eine gedruckte Zeile des Beschichtungsauftrags (v1.99.87): <see cref="Verpackt"/> = Anzahl verpackter von
/// <see cref="Positionen"/> Auftragspositionen.</summary>
public sealed record BeschichtungsauftragDruckZeile(string? HauptArtnr, string? Artnr, string? Matchcode, bool OhneFa,
    decimal Breite, decimal Hoehe, decimal Tiefe, decimal Menge, int Positionen, int Verpackt, bool MasseUneinheitlich);
