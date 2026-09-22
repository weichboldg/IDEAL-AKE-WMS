namespace IDEALAKEWMSService.Services;

/// <summary>
/// IDEAL Baustein a (v1.44.0): legt fehlende <c>ProductionWorkplace</c>-Zeilen aus den Sage-Arbeitsplaetzen
/// (`KHKPpsArbeitsplaetze`) an (Sage fuehrend fuer Nummer/Name/Kuerzel) und zieht Abweichungen bei
/// bestehenden Treffern nach UND meldet sie (ADR-0014-Melde-Muster fuer Abweichungen; die Anlage kehrt
/// sich um: kein Treffer -> anlegen). Verknuepfungsschluessel <c>SageArbeitsplatznummer</c> (eindeutiger
/// gefilterter Index). Voraussetzung fuer die Struktur-Arbeitsgang-Erkennung (Baustein b, Kuerzel-Katalog).
/// </summary>
public interface IProductionWorkplaceSyncService
{
    Task<SyncResult> RunAsync(bool dryRun, CancellationToken ct = default);
}
