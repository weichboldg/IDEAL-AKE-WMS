namespace IdealAkeWms.Models;

/// <summary>
/// Erledigt-Status eines FA-Vorbau-Arbeitsgangs (FA-Abarbeitungsliste + Leitstand-VK-VA).
/// Ersetzt das frühere bool IsCompleted (v1.24.0). Nur Fertig blendet die FA aus der
/// Abarbeitungsliste aus; InBearbeitung bleibt sichtbar.
/// </summary>
public enum FaWorkStepStatus
{
    Offen = 0,
    InBearbeitung = 1,
    Fertig = 2
}
