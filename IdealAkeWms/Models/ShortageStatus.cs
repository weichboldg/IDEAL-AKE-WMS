namespace IdealAkeWms.Models;

/// <summary>
/// Klassifizierung des Fehlteil-Status eines Items in einer Lagerbestellung.
/// Seit v1.19.0 ersetzt das bisherige IsFinalShortage-Bool.
/// </summary>
public enum ShortageStatus : byte
{
    /// <summary>
    /// Kein Fehlteil. Default fuer Items mit Ist>=Soll oder ungeklaerten Differenzen.
    /// </summary>
    None = 0,

    /// <summary>
    /// "Fehlteil" — Lagermitarbeiter bestaetigt: Position fehlt, Restlieferung
    /// wird erwartet. Treibt Bestell-Status auf PartiallyDelivered.
    /// </summary>
    WillBeRestocked = 1,

    /// <summary>
    /// "Wird nicht nachgeliefert" — Eskalation. Position fehlt endgueltig.
    /// Action durch Werkbank/Disposition noetig.
    /// </summary>
    NoRestock = 2
}
