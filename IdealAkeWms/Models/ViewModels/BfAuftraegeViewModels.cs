using System.Globalization;
using IdealAkeWms.Services;

namespace IdealAkeWms.Models.ViewModels;

/// <summary>Zeile der Liste <c>/BfAuftraege</c> (Spec 2026-11-13-blechfertigung-bf-auftraege-wicam-csv, Listen-View). Spaltenfilter laufen
/// ueber <see cref="ColumnMap"/> auf genau dem Text, den die Zelle rendert (Datumsspalten nach der Projektion).</summary>
public sealed class BfAuftragListRow
{
    public int Id { get; init; }
    public string Nummer { get; init; } = string.Empty;
    public BfAuftragStatus Status { get; init; }
    public BfCsvStatus CsvStatus { get; init; }
    public string? CsvFehler { get; init; }
    public string Besteller { get; init; } = string.Empty;
    public DateTime? BestelltAm { get; init; }
    public int Positionen { get; init; }
    public DateTime? FreigegebenAm { get; init; }
    public string? FreigegebenVon { get; init; }
    public DateTime? Schachteltermin { get; init; }
    public DateTime? FeTermin { get; init; }
    public DateTime? FertigAm { get; init; }

    public static string Datum(DateTime? d) => d?.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) ?? string.Empty;
    public static string Zeitpunkt(DateTime? d) => d?.ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture) ?? string.Empty;

    public static BfAuftragListRow Von(BfAuftrag a) => new()
    {
        Id = a.Id, Nummer = a.Nummer ?? string.Empty, Status = a.Status, CsvStatus = a.CsvStatus, CsvFehler = a.CsvFehler,
        Besteller = a.BestellerName, BestelltAm = a.BestelltAm, Positionen = a.Positionen.Count,
        FreigegebenAm = a.FreigegebenAm, FreigegebenVon = a.FreigegebenVon, Schachteltermin = a.Schachteltermin, FeTermin = a.FeTermin,
        FertigAm = a.FertigAm,
    };

    public static string StatusText(BfAuftragStatus s) => s switch
    {
        BfAuftragStatus.Entwurf => "Warenkorb",
        BfAuftragStatus.Bestellt => "Bestellt",
        BfAuftragStatus.Freigegeben => "Freigegeben",
        BfAuftragStatus.Fertig => "Fertig",
        BfAuftragStatus.Storniert => "Storniert",
        _ => s.ToString(),
    };

    public static string CsvText(BfCsvStatus s) => s switch
    {
        BfCsvStatus.Keine => "–",
        BfCsvStatus.Offen => "offen",
        BfCsvStatus.Geschrieben => "geschrieben",
        BfCsvStatus.Fehler => "Fehler",
        _ => s.ToString(),
    };

    public static readonly IReadOnlyDictionary<string, Func<BfAuftragListRow, string?>> ColumnMap =
        new Dictionary<string, Func<BfAuftragListRow, string?>>
        {
            ["nummer"] = r => r.Nummer,
            ["status"] = r => StatusText(r.Status),
            ["csv"] = r => CsvText(r.CsvStatus),
            ["besteller"] = r => r.Besteller,
            ["bestelltam"] = r => Zeitpunkt(r.BestelltAm),
            ["positionen"] = r => r.Positionen.ToString(CultureInfo.InvariantCulture),
            ["freigegebenam"] = r => Zeitpunkt(r.FreigegebenAm),
            ["freigegebenvon"] = r => r.FreigegebenVon,
            ["schachteltermin"] = r => Datum(r.Schachteltermin),
            ["fetermin"] = r => Datum(r.FeTermin),
            ["fertigam"] = r => Zeitpunkt(r.FertigAm),
        };
}

public sealed class BfAuftragListViewModel
{
    public List<BfAuftragListRow> Rows { get; init; } = new();
    public BfAuftragStatus? Status { get; init; }
    public DateTime? Von { get; init; }
    public DateTime? Bis { get; init; }
    public bool NurMeine { get; init; }
    /// <summary>Schachtler/admin: sieht alle Auftraege, Checkbox-Spalte und „Fertig melden“.</summary>
    public bool IstSchachtler { get; init; }
    public bool HatWarenkorb { get; init; }
    public PaginationState Pagination { get; init; } = new();
}

/// <summary>Detail eines BF-Auftrags; ohne Id (0) = leerer Warenkorb.</summary>
public sealed class BfAuftragDetailViewModel
{
    public BfAuftrag Auftrag { get; init; } = new();
    public string RowVersion => Convert.ToBase64String(Auftrag.RowVersion);
    public bool IstEigentuemer { get; init; }
    public bool IstSchachtler { get; init; }
    public List<string> Statuswerte { get; init; } = new();
    public List<string> Lagerplaetze { get; init; } = new();
    /// <summary>Abschicken gesperrt (keine Empfaengergruppe) — Text fuer den Hinweis, null = ok.</summary>
    public string? AbschickenGesperrt { get; init; }
    /// <summary>Artikelsuche im Warenkorb (Server-Suche ohne eigenes JS).</summary>
    public string? Suche { get; init; }
    public List<IdealAkeWms.Data.Repositories.BfArtikel> Treffer { get; init; } = new();

    public bool IstWarenkorb => Auftrag.Status == BfAuftragStatus.Entwurf;
    public bool BestellerBearbeitet => IstWarenkorb && IstEigentuemer;
    public bool SchachtlerBearbeitet => IstSchachtler && Auftrag.Status == BfAuftragStatus.Bestellt;
    /// <summary>F6 (Freigabe-Antwort 11): Besteller bis zur Freigabe, Schachtler bis Fertig; Regel wiederholt der Workflow.</summary>
    public bool DarfStornieren =>
        (IstEigentuemer && Auftrag.Status == BfAuftragStatus.Bestellt)
        || (IstSchachtler && Auftrag.Status is BfAuftragStatus.Bestellt or BfAuftragStatus.Freigegeben);
}
