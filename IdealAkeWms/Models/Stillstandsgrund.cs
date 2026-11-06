namespace IdealAkeWms.Models;

/// <summary>
/// Sage-Stillstandsbegruendung (<c>PpsStillstandsbegruendungenInfo</c>, Spec 2026-11-06-sage-sdata-stammdaten-sync, E6b/F8).
/// Nur gelesen (keine Oberflaeche bis zum Rueckmelde-Epic). Nie geloescht: fehlt ein Grund in Sage, setzt der Abgleich
/// <see cref="SageMissingSince"/>. Laengen laut SData-<c>$schema</c> (P-S1): Stillstandnummer string(31), Bezeichnung string(50).
/// Kein Aktiv-Feld: die Ressource liefert keins (P-S1).
/// </summary>
public class Stillstandsgrund : AuditableEntity
{
    public string Nummer { get; set; } = string.Empty;
    public string? Bezeichnung { get; set; }
    public DateTime? SageMissingSince { get; set; }
}
