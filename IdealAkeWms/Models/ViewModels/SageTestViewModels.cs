using IdealAkeWms.Services.SageSdata;

namespace IdealAkeWms.Models.ViewModels;

// Spec 2026-11-13-sdata-teil-2-userfelder-stueckliste F1/F3/F4: Register Sage-Test (nur Admin, nur lesen).

/// <summary>Feldliste (F3). Eingaben kommen per GET; <see cref="Ausgefuehrt"/> = es wurde gegen Sage gelesen.</summary>
public sealed class SageTestFeldlisteViewModel
{
    public static readonly string[] Vertraege =
        { SageSdataReadClient.VertragResources, SageSdataReadClient.VertragResourcesSpecific, SageSdataReadClient.ServiceContract };

    /// <summary>Vorschlagsliste = Ressourcen des Lesetests (L1–L9).</summary>
    public static readonly string[] Vorschlaege =
    {
        "PpsFaBelegeInfo", "PpsFaBelegePositionenInfo", "PpsFaBelegePositionenArbeitsgangInfo", "PpsRessourcenListenInfo",
        "PpsRessourcenPositionenResolvedInfo", "PpsRessourcenPositionenInfo", "ArtikelStueckliste", "VerkaufBelegepositionenRawInfo",
        "Artikel", "dtsFaBelegsuche.Sage.PPS",
    };

    public string Vertrag { get; set; } = SageSdataReadClient.VertragResources;
    public string? Ressource { get; set; }
    public string? Where { get; set; }
    public string? OrderBy { get; set; }
    public string? Include { get; set; }
    public int Anzahl { get; set; } = 3;

    public SdataWebStand? Stand { get; set; }
    public bool Ausgefuehrt { get; set; }
    public List<string> Fehler { get; } = new();
    public List<string> Hinweise { get; } = new();
    public SchemaInfo? Schema { get; set; }
    public List<FeldlistenZeile> Felder { get; set; } = new();
    public int Zeilen { get; set; }
    public int? Gesamt { get; set; }
    public long DauerMs { get; set; }
    public string? Url { get; set; }
}

/// <summary>Stueckliste live (F4): Artikelnummer (Ressourcenliste) ODER FA-Nummer (FA-Positionen).</summary>
public sealed class SageTestStuecklisteViewModel
{
    public string? Artikelnummer { get; set; }
    public int? FaNummer { get; set; }
    public int? Mandant { get; set; }

    public SdataWebStand? Stand { get; set; }
    public bool Ausgefuehrt { get; set; }
    public bool FaModus => FaNummer is not null;
    public List<string> Fehler { get; } = new();
    public List<string> Aufrufe { get; } = new();
    public string? Titel { get; set; }
    public List<FlachesFeld> Kopf { get; set; } = new();
    public List<StuecklistenZeile> Zeilen { get; set; } = new();
}
