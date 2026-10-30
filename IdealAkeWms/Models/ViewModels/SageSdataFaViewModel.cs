using IdealAkeWms.Services.SageSdata;

namespace IdealAkeWms.Models.ViewModels;

/// <summary>FA-Struktur live aus Sage (v1.71.0): Eingabe, Ergebnis, Meldungen und Diagnose eines Seitenaufbaus.</summary>
public class SageSdataFaViewModel
{
    public int? BelId { get; set; }

    /// <summary>Fehlende/ungueltige Konfiguration (F-8) — gesetzt heisst: kein Aufruf.</summary>
    public List<string> NichtKonfiguriert { get; } = new();

    public bool ZertifikatPruefungAus { get; set; }

    /// <summary>Hinweis, wenn eine Sub-FA eingegeben wurde.</summary>
    public string? Hinweis { get; set; }

    /// <summary>Fehlerbanner (nicht gefunden, Verbindungsfehler, HTTP-Fehler).</summary>
    public List<string> Fehler { get; } = new();

    public SageFaStruktur? Struktur { get; set; }

    /// <summary>BelIDs, deren Positionen bzw. Rueckmeldungen wegen eines fehlgeschlagenen Aufrufs fehlen (Text = Grund).</summary>
    public Dictionary<int, string> PositionenNichtGeladen { get; } = new();
    public Dictionary<int, string> RueckmeldungenNichtGeladen { get; } = new();

    public IReadOnlyDictionary<int, (int Anzahl, decimal MengeGut)> Rueckmeldungen { get; set; } = new Dictionary<int, (int, decimal)>();
    public IReadOnlyDictionary<int, (int Anzahl, decimal MengeGut)> Fertigmeldungen { get; set; } = new Dictionary<int, (int, decimal)>();

    public List<SdataCall> Aufrufe { get; } = new();
}
