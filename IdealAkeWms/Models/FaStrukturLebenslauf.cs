using System;

namespace IdealAkeWms.Models;

/// <summary>
/// v1.99.37 (Spec 2026-11-17-fa-struktur-abgleich-statt-full-replace, ADR 0032): Lebenslauf einer Zeile der FA-Struktur-Spiegel
/// (<see cref="FaHierarchyNode"/>, <see cref="FaHierarchyOrderInfo"/>, <see cref="ProductionOrderPosition"/>). Der Lauf „FA-Hierarchie“
/// gleicht ab statt zu ersetzen: was die Quelle nicht mehr liefert, bleibt mit <see cref="VerschwundenSeit"/>/<see cref="VerschwundenGrund"/>
/// stehen, bis Sage es als geloescht meldet oder die Aufraeumregel greift.
/// </summary>
public interface IFaStrukturZeile
{
    /// <summary>Erster Lauf, der die Zeile gelesen hat (Lokalzeit).</summary>
    DateTime ErstGesehen { get; set; }
    /// <summary>Letzter Lauf, der die Zeile gelesen hat (Lokalzeit).</summary>
    DateTime ZuletztGesehen { get; set; }
    /// <summary>Erster Lauf, in dem die Zeile fehlte; null = aktiv.</summary>
    DateTime? VerschwundenSeit { get; set; }
    /// <summary><see cref="FaStrukturGrund"/>; null = aktiv.</summary>
    string? VerschwundenGrund { get; set; }
    /// <summary>Zeitpunkt der letzten Wertaenderung (Lokalzeit).</summary>
    DateTime SyncedAt { get; set; }
}

/// <summary>Gruende fuer verschwundene Zeilen und ihr Anzeigetext — die EINE Zuordnung fuer alle Ansichten.</summary>
public static class FaStrukturGrund
{
    public const string Beendet = "beendet";
    public const string Inaktiv = "inaktiv";
    public const string Unklar = "unklar";

    /// <summary>Badge-Text (nie nur Farbe); null = aktiv, kein Badge.</summary>
    public static string? Text(string? grund) => grund switch
    {
        null => null,
        Beendet => "in Sage beendet",
        Inaktiv => "in Sage inaktiv",
        Unklar => "in Sage ungeklärt",
        _ => "in Sage " + grund,
    };
}
