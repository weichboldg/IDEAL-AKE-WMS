using System;

namespace IdealAkeWms.Models;

/// <summary>
/// Position eines Sage-Fertigungsauftrags aus SData <c>PpsFaBelegePositionenInfo</c> (Spec 2026-11-06-sage-sdata-stammdaten-sync,
/// E4b/F6): je FA der gefilterten Menge (eingelastet + aktiv, offen oder kuerzlich beendet) alle Positionen (MA, AG …) mit
/// <see cref="BelPosID"/> und Lagerplatz — Grundlage der spaeteren Rueckmeldungen. Wird nur aus SData befuellt, sobald
/// <c>Sync:Quelle:FaStruktur</c> SData liest (Vergleich bzw. SData nach P-F2); im View-Betrieb bleibt die Tabelle unberuehrt.
/// NICHT <see cref="AuditableEntity"/> — Cache, Full-Refresh je Lauf durch den <c>FaHierarchySyncService</c> (Praezedenz
/// <see cref="FaHierarchyNode"/>); Nachvollziehbarkeit ueber <see cref="SyncedAt"/> + Aktivitaets-Protokoll.
/// </summary>
public class ProductionOrderPosition
{
    public int Id { get; set; }

    /// <summary>Sage-Positions-ID (KHKPpsFaBelegePositionen.BelPosID), UNIQUE.</summary>
    public int BelPosID { get; set; }

    /// <summary>BelID des FA, zu dem die Position gehoert. Index.</summary>
    public int BelID { get; set; }

    /// <summary>StrukturID (= Haupt-FA) des FA. Index.</summary>
    public int StrukturID { get; set; }

    public int? Position { get; set; }

    /// <summary>MA (Material), AG (Arbeitsgang) … roh aus Sage (getrimmt).</summary>
    public string? RessourceTyp { get; set; }

    /// <summary>Artikel-, Arbeitsgang- oder Arbeitsplatznummer je nach <see cref="RessourceTyp"/>.</summary>
    public string? RessourceNummer { get; set; }

    public string? ArbeitsgangNummer { get; set; }

    /// <summary>Eigener Unter-FA der Position; 0 = keiner (Blatt).</summary>
    public int? BelIDNachfolger { get; set; }

    public decimal? Menge { get; set; }
    public decimal? Sollmenge { get; set; }
    public decimal? Istmenge { get; set; }
    public decimal? SollRuestzeit { get; set; }
    public decimal? IstRuestzeit { get; set; }
    public decimal? SollStueckzeit { get; set; }
    public decimal? IstStueckzeit { get; set; }

    public int? Status { get; set; }

    /// <summary>Sage-PlatzID der Position (0 = keiner).</summary>
    public int? PlatzID { get; set; }

    /// <summary>Kurzbezeichnung des Sage-Lagerplatzes zu <see cref="PlatzID"/> (aus <c>LagerplaetzeInfo</c>).</summary>
    public string? LagerplatzKurzbezeichnung { get; set; }

    public decimal? Schwund { get; set; }
    public DateTime? Bedarfstermin { get; set; }

    /// <summary>Zeitpunkt des letzten Full-Refresh (Lokalzeit, wie <see cref="FaHierarchyNode.SyncedAt"/>).</summary>
    public DateTime SyncedAt { get; set; }
}
