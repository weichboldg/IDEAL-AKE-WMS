using IdealAkeWms.Models;

namespace IdealAkeWms.Services;

/// <summary>Sage-Lagerbewegungsart, als die eine WMS-Buchung gesendet wird (SData <c>LagerbuchungService</c>).</summary>
public enum SageBuchungsart { Zugang, Entnahme, Umbuchung }

/// <summary>
/// Reine, DB-freie Entscheidungslogik: darf eine gespeicherte Buchung an Sage gemeldet
/// (in die Queue geschrieben) werden, und als welche Sage-Bewegungsart? Kumulative UND-Verknuepfung
/// aus globalem Toggle und Lagerplatz-Flag(s), plus harter Bewegungsart-Filter (NIE Sage-Korrekturen —
/// Feedback-Loop-Schutz). Umbuchung nur mit eigenem Schalter <c>SageLagerbuchungUmbuchungAktiv</c>
/// (Default aus, v1.73.0 E7 / Spec F9). EINE Stelle fuer Web-Decorator, Nacherfassungs-Lauf und Payload.
/// </summary>
public static class SageBookingEnqueueDecision
{
    /// <summary>ServiceSetting: WMS-Umbuchungen an Sage melden (Default aus = Verhalten wie vor v1.73.0).</summary>
    public const string UmbuchungToggleKey = "SageLagerbuchungUmbuchungAktiv";

    private static readonly MovementType[] EinAus = { MovementType.Einbuchung, MovementType.Ausbuchung };
    private static readonly MovementType[] EinAusUm = { MovementType.Einbuchung, MovementType.Ausbuchung, MovementType.Umbuchung };

    /// <summary>
    /// Die eine Typliste: diese Bewegungsarten duerfen ausgehend an Sage gemeldet werden. Auch die EF-Abfrage des
    /// Nacherfassungs-Laufs nutzt sie (<c>Contains</c>). Sage-Korrekturen (SageEin-/SageAusbuchung) nie.
    /// </summary>
    public static IReadOnlyList<MovementType> BookableTypes(bool umbuchungAktiv) => umbuchungAktiv ? EinAusUm : EinAus;

    /// <summary>Ein-/Ausbuchung (Stand vor E7, Umbuchung-Schalter aus).</summary>
    public static bool IsBookableType(MovementType type) => IsBookableType(type, umbuchungAktiv: false);

    public static bool IsBookableType(MovementType type, bool umbuchungAktiv) => BookableTypes(umbuchungAktiv).Contains(type);

    /// <summary>
    /// Sage-Bewegungsart je WMS-Buchung nach den Platz-Flags (<c>SageBuchungErlaubt</c>); null = nicht an Sage.
    /// Einbuchung → Zugang, Ausbuchung → Entnahme (Platz = <c>StorageLocation</c>). Umbuchung (Spec F9):
    /// Sage → Sage = Umbuchung, Sage → WMS-only = Entnahme an der Herkunft, WMS-only → Sage = Zugang am Ziel,
    /// WMS-only → WMS-only = keine. Der Typfilter (Schalter) liegt in <see cref="IsBookableType(MovementType, bool)"/>.
    /// </summary>
    public static SageBuchungsart? Buchungsart(MovementType type, bool zielErlaubt, bool herkunftErlaubt) => type switch
    {
        MovementType.Einbuchung when zielErlaubt => SageBuchungsart.Zugang,
        MovementType.Ausbuchung when zielErlaubt => SageBuchungsart.Entnahme,
        MovementType.Umbuchung => (herkunftErlaubt, zielErlaubt) switch
        {
            (true, true) => SageBuchungsart.Umbuchung,
            (true, false) => SageBuchungsart.Entnahme,
            (false, true) => SageBuchungsart.Zugang,
            _ => null,
        },
        _ => null,
    };

    /// <summary>
    /// True, wenn die Buchung in die Sage-Queue geschrieben werden soll.
    /// </summary>
    /// <param name="type">Bewegungsart der gespeicherten Buchung.</param>
    /// <param name="globalToggleAktiv">Globaler ServiceSetting <c>SageLagerbuchungAktiv</c>.</param>
    /// <param name="umbuchungAktiv">ServiceSetting <see cref="UmbuchungToggleKey"/>.</param>
    /// <param name="locationSageBuchungErlaubt">Flag <c>SageBuchungErlaubt</c> des Lagerplatzes (<c>StorageLocation</c>, bei Umbuchung das Ziel).</param>
    /// <param name="sourceSageBuchungErlaubt">Flag des Herkunftsplatzes (<c>SourceStorageLocation</c>, nur Umbuchung; fehlt = false).</param>
    /// <param name="hatFaBezug">Buchung traegt einen FA (<c>StockMovement.ProductionOrder</c>). Umbuchungen mit FA-Bezug
    /// gehen NICHT an Sage (Spec F9: Entnahme mit FA-Bezug = Material-Rueckmeldung im Rueckmelde-Epic). Ein-/Ausbuchung unberuehrt.</param>
    /// <param name="hatSageWareneingang">v1.99.4 (Wareneingang Teil 2, F3): Wareneingang mit Sage-WE-Status &lt;&gt; NichtNoetig - geht als ELL-Beleg
    /// (oder nur in Sage) an Sage, NIE zusaetzlich als Lagerbuchung-Zugang. Dieselbe Regel steht als EF-Bedingung in EnqueueMissingAsync.</param>
    public static bool ShouldEnqueue(MovementType type, bool globalToggleAktiv, bool umbuchungAktiv,
        bool locationSageBuchungErlaubt, bool sourceSageBuchungErlaubt, bool hatFaBezug = false, bool hatSageWareneingang = false)
        => globalToggleAktiv && IsBookableType(type, umbuchungAktiv)
           && !IstUmbuchungMitFaBezug(type, hatFaBezug)
           && !hatSageWareneingang
           && Buchungsart(type, locationSageBuchungErlaubt, sourceSageBuchungErlaubt) is not null;

    /// <summary>Umbuchung mit FA-Bezug (z. B. Kommissionier-Transfer) – nie an Sage (Spec F9).</summary>
    public static bool IstUmbuchungMitFaBezug(MovementType type, bool hatFaBezug)
        => type == MovementType.Umbuchung && hatFaBezug;

    /// <summary>Ein-/Ausbuchung (ohne Umbuchung) — Stand vor E7.</summary>
    public static bool ShouldEnqueue(MovementType type, bool globalToggleAktiv, bool locationSageBuchungErlaubt)
        => ShouldEnqueue(type, globalToggleAktiv, umbuchungAktiv: false, locationSageBuchungErlaubt, sourceSageBuchungErlaubt: false);

    /// <summary>Ueberladung mit der Buchung selbst (AK6: direkter Aufruf mit einer SageEinbuchung-Instanz).</summary>
    public static bool ShouldEnqueue(StockMovement movement, bool globalToggleAktiv, bool locationSageBuchungErlaubt)
        => ShouldEnqueue(movement.MovementType, globalToggleAktiv, locationSageBuchungErlaubt);
}
