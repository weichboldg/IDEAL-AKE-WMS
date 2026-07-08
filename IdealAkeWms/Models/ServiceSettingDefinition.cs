namespace IdealAkeWms.Models;

/// <summary>Typ einer Service-Einstellung — steuert die UI-Darstellung.</summary>
public enum ServiceSettingType
{
    Bool,
    Int,
    String
}

/// <summary>
/// Definiert eine Service-Einstellung typisiert: Single Source of Truth fuer
/// Typ, Default, Kategorie und Beschreibung. Treibt sowohl das Seeding
/// (<c>Program.cs</c>) als auch die typisierte UI (<c>/ServiceSettings</c>).
/// </summary>
/// <param name="Key">DB-Key (z.B. "Sync:BomCacheEnabled").</param>
/// <param name="Type">Typ fuer UI + Validierung.</param>
/// <param name="DefaultValue">
/// Default IMMER als String im DB-Format: bool -&gt; "true"/"false",
/// int -&gt; "60", string -&gt; Rohwert.
/// </param>
/// <param name="Category">UI-Gruppierung, z.B. "Sync", "BOM-Cache".</param>
/// <param name="Description">Anzeige unter dem Feld.</param>
/// <param name="Multiline">String-Listen (z.B. Empfaenger) als Textarea rendern.</param>
public sealed record ServiceSettingDefinition(
    string Key,
    ServiceSettingType Type,
    string DefaultValue,
    string Category,
    string Description,
    bool Multiline = false);
