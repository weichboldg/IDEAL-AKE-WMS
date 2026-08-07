using System;

namespace IdealAkeWms.Models;

/// <summary>
/// Projektion einer Zeile aus der IDEAL-Sage-View <c>vw_IDEAL-AKE_Kommissionierung_FAListe</c>
/// (Struktur UND Stueckliste je Fertigungsauftrag, mehrstufig ueber HauptFA/VaterFA/SubFA).
/// NICHT <see cref="AuditableEntity"/> — reine, vom FaHierarchySyncService per Full-Refresh
/// befuellte Cache-Tabelle (Praezedenzfall <see cref="CachedBomHeader"/>). Nachvollziehbarkeit
/// kommt aus dem Aktivitaets-Protokoll (SyncLog) + <see cref="SyncedAt"/>, nicht aus Audit-Feldern.
/// Werte sind roh/unnormalisiert uebernommen (Splits erst beim Konsum, analog BOM-Matching).
/// </summary>
public class FaHierarchyNode
{
    public int Id { get; set; }

    /// <summary>Fertigungsauftragsnummer des kompletten Geraets (StrukturID). Einziger
    /// Produktions-Identifier. Index (nicht unique — mehrere Zeilen je Struktur).</summary>
    public int HauptFA { get; set; }

    /// <summary>BelID des uebergeordneten Sub-FA. NULL nur bei der Wurzel. Index.</summary>
    public int? VaterFA { get; set; }

    /// <summary>Eigene BelID des Nachfolgers. 0 = Blatt (Kaufteil/Endmaterial ohne eigenen FA).</summary>
    public int SubFA { get; set; }

    /// <summary>Position innerhalb der Vater-Stueckliste. NULL bei der Wurzel.</summary>
    public int? Position { get; set; }

    public string? HauptArtnr { get; set; }
    public string? Artnr { get; set; }
    public string? Matchcode { get; set; }
    public string? Bezeichnung1 { get; set; }
    public string? Bezeichnung2 { get; set; }
    public string? Artikelgruppe { get; set; }
    public string? Hauptlagerplatz { get; set; }
    public string? BemerkungPN { get; set; }
    public string? FertigungsInfoPN { get; set; }
    public string? Kommissionieren { get; set; }
    public string? Arbeitsbereich { get; set; }
    public string? Arbeitsschritte { get; set; }
    public string? Artikeltyp { get; set; }
    public string? Material { get; set; }
    public string? VMBedarf { get; set; }

    public decimal Sollmenge { get; set; }
    public decimal Fertigungmenge { get; set; }
    public decimal Breite { get; set; }
    public decimal Hoehe { get; set; }
    public decimal Tiefe { get; set; }

    /// <summary>Sage-Boolean 'Ja'/'Nein' -&gt; bit.</summary>
    public bool Beschaffungsartikel { get; set; }

    /// <summary>Sage-Boolean -1/0 (VB6) -&gt; bit (-1 =&gt; true).</summary>
    public bool Beschichtet { get; set; }

    /// <summary>Sage-Boolean (Bit bzw. Ja/Nein) -&gt; bit.</summary>
    public bool EKBedarf { get; set; }

    /// <summary>Zeitpunkt des letzten Full-Refresh (Lokalzeit, analog <see cref="CachedBomHeader.CachedAt"/>).</summary>
    public DateTime SyncedAt { get; set; }
}
