using System;

namespace IdealAkeWms.Models;

/// <summary>
/// Projektion einer Zeile aus der IDEAL-Sage-View <c>vw_IDEAL-AKE_Kommissionierung_FAInfos</c>
/// (PPS-/Auftragsdaten). Granularitaet: ein Datensatz = ein Auftrag (ABNr + Pos). Kombinationsgeraete
/// tragen denselben <see cref="HauptFA"/> mit mehreren <see cref="MontageAbteilung"/>-Zeilen —
/// diese Tabelle behaelt bewusst ALLE Zeilen (kein Join in <see cref="FaHierarchyNode"/> hinein).
/// NICHT <see cref="AuditableEntity"/> — reine Cache-Tabelle (Praezedenzfall <see cref="CachedBomHeader"/>).
/// </summary>
public class FaHierarchyOrderInfo
{
    public int Id { get; set; }

    /// <summary>Verknuepfungs-Schluessel zur FAListe-View. Index.</summary>
    public int HauptFA { get; set; }

    /// <summary>Montage-Abteilung — Teil des fachlichen Schluessels bei Kombinationsgeraeten
    /// (informativ, kein Struktur-Schluessel).</summary>
    public string? MontageAbteilung { get; set; }

    public string? ABNr { get; set; }
    public string? Pos { get; set; }
    public string? Kunde { get; set; }
    public string? Status { get; set; }
    public string? Dienstleister { get; set; }
    public string? RAL { get; set; }
    public string? Bemerkung_Uhrzeit { get; set; }

    public DateTime? KO_Termin { get; set; }
    public DateTime? FE_Termin { get; set; }
    public DateTime? Start_Beschichtung { get; set; }
    public DateTime? Beschichten_Retour { get; set; }
    public DateTime? Neuer_PT_PPS { get; set; }
    public DateTime? Verladetermin_Vsl { get; set; }

    public decimal? Montagestunden { get; set; }
    public int? Prio { get; set; }

    /// <summary>Zeitpunkt des letzten Full-Refresh (Lokalzeit).</summary>
    public DateTime SyncedAt { get; set; }
}
