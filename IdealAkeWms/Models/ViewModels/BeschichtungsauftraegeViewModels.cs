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
        CreatedAt = a.CreatedAt, CreatedBy = a.CreatedBy,
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
/// je Artnr (Teil-1-Aggregation), Barcode = Auftragsnummer, sichtbare Spalten der Detail-Positionstabelle.</summary>
public sealed class BeschichtungsauftragPrintViewModel
{
    public Beschichtungsauftrag Auftrag { get; init; } = new();
    public List<FaHierarchyBeschichtungSummiertRowViewModel> Rows { get; init; } = new();
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
        s is BeschichtungsauftragStatus.Uebergeben or BeschichtungsauftragStatus.Erledigt;

    /// <summary>Gespeicherte Positionen → Knoten-Form fuer <see cref="BeschichtungsauftragService.AggregateSummiert"/>
    /// (Summe der Auftragsmenge statt Sollmenge; Masse-Invariante wie Teil 1).</summary>
    public static List<FaHierarchyBeschichtungSummiertRowViewModel> Summiere(Beschichtungsauftrag a) =>
        BeschichtungsauftragService.AggregateSummiert(
            a.Positionen.Select(p => new FaHierarchyNode
            {
                HauptFA = a.HauptFA, VaterFA = p.VaterFA, SubFA = p.SubFA, Position = p.Position, Beschichtet = true,
                HauptArtnr = p.HauptArtnr, Artnr = p.Artnr, Matchcode = p.Matchcode, Sollmenge = p.Menge,
                Breite = p.Breite, Hoehe = p.Hoehe, Tiefe = p.Tiefe,
            }),
            null, null, 1, int.MaxValue, paginate: false).Rows;
}
