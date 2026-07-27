namespace IDEALAKEWMSService.Services;

/// <summary>
/// Eine Zeile der Sage-View <c>dbo.vw_IDEAL_AKE_WMS_FAZusatzinformationen</c>.
/// Die View liefert fertige Texte (Status-CASE-Logik lebt in der View, nicht bei uns).
/// </summary>
public record SageZusatzinfoRow(
    string? WaNummer,
    string? Kaeltemittel,
    string? Ventil,
    string? AusfuehrungEZ,
    string? Maschine,
    string? Status);

/// <summary>
/// <c>ViewExists=false</c>: die View fehlt am Zielsystem — der Sync ueberspringt dann
/// regulaer mit Warn-Zeile (kein throw, keine Fehlermail; Spec §2 View-Guard).
/// </summary>
public record SageZusatzinfoReadResult(bool ViewExists, IReadOnlyList<SageZusatzinfoRow> Rows);

public interface ISageZusatzinfoReader
{
    Task<SageZusatzinfoReadResult> ReadAsync(CancellationToken ct = default);
}
