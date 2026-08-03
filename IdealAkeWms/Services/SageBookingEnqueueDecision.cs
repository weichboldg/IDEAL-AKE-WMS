using IdealAkeWms.Models;

namespace IdealAkeWms.Services;

/// <summary>
/// Reine, DB-freie Entscheidungslogik: darf eine gespeicherte Buchung an Sage gemeldet
/// (in die Queue geschrieben) werden? Kumulative UND-Verknuepfung aus globalem Toggle und
/// Lagerplatz-Flag, plus harter Bewegungsart-Filter (nur manuelle Ein-/Ausbuchung; NIE
/// Sage-Korrekturen oder Umbuchung — Feedback-Loop-Schutz). Isoliert unit-testbar.
/// </summary>
public static class SageBookingEnqueueDecision
{
    /// <summary>Nur diese Bewegungsarten duerfen ausgehend an Sage gemeldet werden.</summary>
    public static bool IsBookableType(MovementType type)
        => type is MovementType.Einbuchung or MovementType.Ausbuchung;

    /// <summary>
    /// True, wenn die Buchung in die Sage-Queue geschrieben werden soll.
    /// </summary>
    /// <param name="type">Bewegungsart der gespeicherten Buchung.</param>
    /// <param name="globalToggleAktiv">Globaler ServiceSetting <c>SageLagerbuchungAktiv</c>.</param>
    /// <param name="locationSageBuchungErlaubt">Flag <c>SageBuchungErlaubt</c> des beteiligten Lagerplatzes.</param>
    public static bool ShouldEnqueue(MovementType type, bool globalToggleAktiv, bool locationSageBuchungErlaubt)
        => IsBookableType(type) && globalToggleAktiv && locationSageBuchungErlaubt;

    /// <summary>Ueberladung mit der Buchung selbst (AK6: direkter Aufruf mit einer SageEinbuchung-Instanz).</summary>
    public static bool ShouldEnqueue(StockMovement movement, bool globalToggleAktiv, bool locationSageBuchungErlaubt)
        => ShouldEnqueue(movement.MovementType, globalToggleAktiv, locationSageBuchungErlaubt);
}
