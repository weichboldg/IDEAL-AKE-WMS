namespace IDEALAKEWMSService.Services;

public enum SageLagerplatzAnlageArt { Angelegt, Vorhanden, Abgelehnt }

/// <summary>v1.99.71 (Spec 2026-11-24-lagerplatz-anlage-wms-nach-sage, ADR 0036): Ergebnis eines Sage-Schreibbefehls. S6: Sperr-/Masswerte der
/// Sage-Zeile fuer die Uebernahme; <see cref="SageDb"/> = "&lt;@@SERVERNAME&gt;/&lt;DB_NAME()&gt;" fuer die Protokoll-Info (Freigabe-Antwort 8).</summary>
public sealed record SageLagerplatzAnlageErgebnis(SageLagerplatzAnlageArt Art, int? PlatzId, string? Platzbezeichnung, string? Grund,
    bool? IstGesperrt = null, bool? IstGesperrtInventur = null, decimal? Laenge = null, decimal? Breite = null, decimal? Hoehe = null,
    decimal? Volumen = null, decimal? Tragkraft = null, string? SageDb = null)
{
    public static SageLagerplatzAnlageErgebnis Abgelehnt(string grund) => new(SageLagerplatzAnlageArt.Abgelehnt, null, null, grund);
}

/// <summary>
/// Schreibweg „Lager/Lagerplatz in Sage anlegen“. Eine Implementierung (Direkt-SQL, <see cref="SqlSageLagerplatzAnlage"/>); die Schnittstelle
/// ist die angekuendigte Austauschstelle gegen einen SData-Service von DPS (Entscheid 2, ADR 0036). Outbox, Status-Automat und Oberflaeche
/// haengen nur an diesem Vertrag.
/// Fachliche Ablehnung = Ergebnis <see cref="SageLagerplatzAnlageArt.Abgelehnt"/> mit Grund; technische Fehler (Fehlerklassen F3) = Exception.
/// </summary>
public interface ISageLagerplatzAnlage
{
    Task<SageLagerplatzAnlageErgebnis> AnlegenAsync(string lagerkennung, int d1, int d2, int d3, CancellationToken ct);

    /// <summary>F3a: PlatzId = Basisplatz, Platzbezeichnung = Lagerbezeichnung. <paramref name="neuanlageErlaubt"/> (H4): false = nur
    /// „vorhanden“ uebernehmen, eine Neuanlage wird abgelehnt (50006).</summary>
    Task<SageLagerplatzAnlageErgebnis> LagerAnlegenAsync(string lagerkennung, string bezeichnung, string lagertechnik, bool neuanlageErlaubt,
        CancellationToken ct);
}
