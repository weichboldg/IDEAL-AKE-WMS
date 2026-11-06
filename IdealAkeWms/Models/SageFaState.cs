using System;

namespace IdealAkeWms.Models;

/// <summary>
/// Sage-Status je Fertigungsauftrag (Lesemodell des Rueckmelde-Epics 2026-11-02-sage-pps-fa-bde-abgleich, Etappe 1): eine Zeile je
/// (<see cref="Mandant"/>, <see cref="BelID"/>), UNIQUE. Die Tabelle legt laut G15 das SData-Epic (Etappe E4b) in der dort geplanten Form
/// an; <b>befuellt</b> wird sie erst vom <c>SageFaStateSyncService</c> des Rueckmelde-Epics (Upsert, Empty-Guard) — bis dahin bleibt sie leer.
/// Den abgeleiteten Status (Neu/Eingelastet/Begonnen/Beendet/Gesperrt) bildet dort <c>SageFaStatusRegel</c>; er wird nicht gespeichert.
/// NICHT <see cref="AuditableEntity"/> — Cache wie <see cref="FaHierarchyNode"/>, Nachvollziehbarkeit ueber <see cref="SyncedAt"/> + Protokoll.
/// </summary>
public class SageFaState
{
    public int Id { get; set; }

    public int Mandant { get; set; }
    public int BelID { get; set; }
    public int StrukturID { get; set; }

    // Sage-Booleans (-1 = wahr, SageBool.IsTrue).
    public bool IstEingelastet { get; set; }
    public bool IstBegonnen { get; set; }
    public bool IstBeendet { get; set; }
    public bool IstGesperrt { get; set; }

    /// <summary>NULL = Quelle liefert das Kennzeichen nicht (dtsFaBelegsuche hat es nicht).</summary>
    public bool? IstReaktiviert { get; set; }

    /// <summary>Summe MengeGut der Sage-Rueckmeldungen (dtsRueckKorrektur); NULL = nicht gelesen.</summary>
    public decimal? MengeGutRueckgemeldet { get; set; }

    /// <summary>Summe MengeGut der Sage-Fertigmeldungen (dtsFertigmeldungen); NULL = nicht gelesen.</summary>
    public decimal? MengeGutFertiggemeldet { get; set; }

    public DateTime SyncedAt { get; set; }
}
