using IdealAkeWms.Services;

namespace IdealAkeWms.Models.ViewModels;

/// <summary>v1.99.38: Vorschau „Arbeitsgänge → Sage“ (Spec 2026-11-17-arbeitsgaenge-aus-wms-in-sage-fa-anlegen).</summary>
public class SageFaArbeitsgaengeViewModel
{
    public int? HauptFa { get; set; }
    public int? Fa { get; set; }
    public bool NurAnlegbar { get; set; }
    public bool Pruefen { get; set; }
    public List<SageFaArbeitsgaenge.Zeile> Zeilen { get; set; } = new();
    /// <summary>Blaetter (SubFA = 0) mit Kuerzeln in der Auswahl — werden nicht angelegt (Freigabe-Antwort 3).</summary>
    public int Blaetter { get; set; }
    /// <summary>AG-Positionen, die nur in Sage stehen (nie geloescht, Freigabe-Antwort 14).</summary>
    public int NurInSage { get; set; }
    /// <summary>Sage nicht lesbar bzw. Auswahl zu gross — sichtbares Banner.</summary>
    public string? SageBanner { get; set; }
    public PaginationState Pagination { get; set; } = new();
    /// <summary>Schalter SageFaArbeitsgaengeAnlegenAktiv: nur dann Knoepfe.</summary>
    public bool SchalterAn { get; set; }
    /// <summary>Schalter an, aber kein Sage-Bearbeiter: der Dienst sendet nichts — Banner.</summary>
    public bool BearbeiterFehlt { get; set; }
    /// <summary>FAs der Seite mit einreihbaren Zeilen (anlegbar oder Fehler) samt Sage-AGs fuer den Bestaetigungsdialog.</summary>
    public List<SageFaEinreihbar> Einreihbar { get; set; } = new();
}

public sealed record SageFaEinreihbar(int BelId, string? Artnr, string? Matchcode, IReadOnlyList<string> SageAgs);
