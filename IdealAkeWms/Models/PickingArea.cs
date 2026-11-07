namespace IdealAkeWms.Models;

/// <summary>
/// v1.88.0 (Spec 2026-11-08-kommissionierung-auftraege-kopf-zeilen, E1): Kommissionierbereich (Stammdaten, flach – Antwort 9).
/// Genau ein aktiver Bereich ist <see cref="IsDefault"/>; Zeilen ohne Regeltreffer landen dort. Seed: Lager (Standard), Glas,
/// Vorkommissionierung.
/// </summary>
public class PickingArea : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string Kuerzel { get; set; } = string.Empty;
    public int Reihenfolge { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDefault { get; set; }

    /// <summary>Antwort 8: Auftraege dieses Bereichs werden mit dem Vorkommissionier-Vorlauf faellig (PrePickingDaysResolver).</summary>
    public bool IstVorkommissionierung { get; set; }

    public List<PickingAreaRule> Regeln { get; set; } = new();
}

/// <summary>v1.88.0: Regeltyp der Bereichszuordnung; Rangfolge Artikel → Kommissionierziel → Lagerort → Artikelgruppe (Antwort 1).</summary>
public enum PickingAreaRuleType : byte
{
    Artikel = 1,
    Kommissionierziel = 2,
    Lagerort = 3,
    Artikelgruppe = 4,
}

/// <summary>v1.88.0: Zuordnungsregel (Typ, Wert) → Bereich; (Typ, Wert) ist eindeutig (UNIQUE).</summary>
public class PickingAreaRule : AuditableEntity
{
    public int PickingAreaId { get; set; }
    public PickingArea PickingArea { get; set; } = null!;
    public PickingAreaRuleType RuleType { get; set; }
    public string Value { get; set; } = string.Empty;
}
