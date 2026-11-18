namespace IdealAkeWms.Models;

/// <summary>Status des BF-Auftrags (Spec 2026-11-13-blechfertigung-bf-auftraege-wicam-csv). Werte als int gespeichert, nie umnummerieren.</summary>
public enum BfAuftragStatus
{
    Entwurf = 0,
    Bestellt = 1,
    Freigegeben = 2,
    Fertig = 3,
    Storniert = 4,
}

/// <summary>Stand der WiCAM-CSV (Muster <see cref="WareneingangCsvStatus"/>); <c>Keine</c> bis zur Freigabe.</summary>
public enum BfCsvStatus
{
    Keine = 0,
    Offen = 1,
    Geschrieben = 2,
    Fehler = 3,
}

/// <summary>
/// BF-Auftrag (Blechfertigung, IDEAL): Warenkorb des Bestellers (Entwurf, ohne Nummer), nach dem Abschicken mit Nummer <c>BF</c> + 8 Ziffern,
/// Freigabe durch den Schachtler schreibt Termine/Bemerkungen fest und gibt die CSV an den Dienst (WiCAM-Auftragspool). Stufe 1 ohne Sage.
/// </summary>
public class BfAuftrag : AuditableEntity
{
    public const int NummerLaenge = 10;

    /// <summary><c>BF00000001</c> ff., vergeben beim Abschicken; null im Entwurf (gefilterter UNIQUE-Index).</summary>
    public string? Nummer { get; set; }
    public BfAuftragStatus Status { get; set; }

    public int? BestellerUserId { get; set; }
    /// <summary>Snapshot fuer <c>ORDER_REMARK_4</c> und Anzeige.</summary>
    public string BestellerName { get; set; } = string.Empty;
    /// <summary>v1.99.42: Kuerzel des Bestellers (<see cref="User.Kuerzel"/>), bei der Freigabe festgeschrieben → <c>ORDER_REMARK_4</c>.</summary>
    public string? BestellerKuerzel { get; set; }
    public DateTime? BestelltAm { get; set; }

    public int? OrderRecipientGroupId { get; set; }
    public OrderRecipientGroup? OrderRecipientGroup { get; set; }
    public DateTime? EmailSentAt { get; set; }

    public DateTime? FreigegebenAm { get; set; }
    public string? FreigegebenVon { get; set; }
    /// <summary>Bei der Freigabe festgeschrieben (Bezugstag = Freigabetag, Arbeitstage + Feiertage).</summary>
    public DateTime? Schachteltermin { get; set; }
    public DateTime? FeTermin { get; set; }

    public BfCsvStatus CsvStatus { get; set; }
    public int CsvVersuche { get; set; }
    public string? CsvFehler { get; set; }
    public string? CsvDatei { get; set; }
    public DateTime? CsvGeschriebenAm { get; set; }

    public DateTime? FertigAm { get; set; }
    public string? FertigVon { get; set; }
    public DateTime? StorniertAm { get; set; }
    public string? StorniertVon { get; set; }
    public string? StornoGrund { get; set; }

    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public List<BfAuftragPosition> Positionen { get; set; } = new();
}

/// <summary>Position eines BF-Auftrags; Artikelfelder als Snapshot beim Hinzufuegen.</summary>
public class BfAuftragPosition : AuditableEntity
{
    public int BfAuftragId { get; set; }
    public BfAuftrag BfAuftrag { get; set; } = null!;

    /// <summary>Fortlaufend ab 1 je Auftrag (UNIQUE mit <see cref="BfAuftragId"/>).</summary>
    public int Position { get; set; }
    public string ArticleNumber { get; set; } = string.Empty;
    public string? Matchcode { get; set; }
    /// <summary>Artikeltyp BG/FT (Sage <c>USER_OSArtikeltyp</c>).</summary>
    public string? Teileart { get; set; }
    public string? Material { get; set; }
    /// <summary>Sage <c>USER_OSMaterialnummerPN</c>; leer = Position wird nicht an WiCAM uebergeben (Freigabe-Antwort 2).</summary>
    public string? Materialnummer { get; set; }
    public string? FertigungsInfoPN { get; set; }
    /// <summary>Voller Sage-Lagerplatz (z. B. <c>K-02;0;0;0</c>); CSV: ORDER_POSITION mit Kommas, ORDER_REMARK_2 = Teil vor dem ersten Trenner.</summary>
    public string? Hauptlagerplatz { get; set; }
    /// <summary>v1.99.42: Sage <c>USER_OSArbeitsschritte</c> (Merkmal, Userfeld-Einstellung Blechfertigung) als Snapshot → ORDER_REMARK_1.</summary>
    public string? Arbeitsschritte { get; set; }
    public decimal Menge { get; set; }
    /// <summary>Snapshot der Sage-Bestellmenge fuer die Mengenregel; null/0 = Menge frei.</summary>
    public decimal? SageBestellmenge { get; set; }
    /// <summary>Etikett aus <c>BfStatuswerte</c> (z. B. EILT), steht vorne in der WiCAM-Bemerkung.</summary>
    public string? Status { get; set; }
    /// <summary>Bei der Freigabe berechnet (ORDER_REMARK).</summary>
    public string? Bemerkung { get; set; }
    /// <summary>Vom Schachtler gestrichen: bleibt sichtbar, geht nicht in die CSV.</summary>
    public bool Gestrichen { get; set; }
    /// <summary>Stufe 2 (Sage-FA je Position); in Stufe 1 nie geschrieben.</summary>
    public string? SageFa { get; set; }

    public bool Uebergeben => !Gestrichen && !string.IsNullOrWhiteSpace(Materialnummer);
}
