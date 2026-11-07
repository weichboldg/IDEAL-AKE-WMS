using IdealAkeWms.Services.Settings;

namespace IdealAkeWms.Models.ViewModels;

/// <summary>
/// Ein Feld auf /Settings bzw. /ServiceSettings (Spec 2026-11-07-einstellungen-auswahlfelder-und-beschreibung, E2/E3): Katalog-Definition
/// plus DB-Stand. Zeigt den WIRKSAMEN Wert (Bool nach Lesart, F6), Abweichung vom Standard und Ungueltigkeit (F3).
/// </summary>
public sealed class SettingItem
{
    public required SettingDefinition Definition { get; init; }
    /// <summary>Rohwert aus der DB; null = keine Zeile.</summary>
    public string? RohWert { get; init; }
    public bool HatZeile => RohWert != null;
    /// <summary>Was ins Feld kommt: Bool "true"/"false" (wirksamer Zustand), sonst Rohwert bzw. Default ohne Zeile.</summary>
    public required string Wirksam { get; init; }
    public bool WeichtAb { get; init; }
    public bool Ungueltig { get; init; }
    public string? Fehler { get; init; }
    /// <summary>Auswahl bei MasterData (Wert, Anzeigetext); bei Lagerplatz und String Vorschlaege; sonst leer.</summary>
    public IReadOnlyList<(string Wert, string Text)> Optionen { get; init; } = Array.Empty<(string, string)>();

    public string Key => Definition.Key;
    public string Anzeigename => Definition.Anzeigename ?? Definition.Key;

    /// <summary>Wirksamer Bool-Zustand nach Lesart (F6).</summary>
    /// <remarks>Ohne Trim, genau wie die Leser (z. B. <c>RequireLagerbestellungAktivAttribute</c>, <c>ServiceSettings.GetBoolAsync</c>):
    /// „ false“ ist fuer NurFalseSperrt an (QA B3).</remarks>
    public static bool BoolWirksam(SettingDefinition d, string? roh)
    {
        var t = roh ?? "";
        return d.BoolLesart switch
        {
            SettingBoolLesart.NurFalseSperrt => !t.Equals("false", StringComparison.OrdinalIgnoreCase),
            SettingBoolLesart.LeerIstDefault => string.IsNullOrWhiteSpace(t)
                ? d.DefaultValue.Equals("true", StringComparison.OrdinalIgnoreCase)
                : t.Equals("true", StringComparison.OrdinalIgnoreCase),
            _ => t.Equals("true", StringComparison.OrdinalIgnoreCase),
        };
    }

    /// <summary>F4: Wert entspricht dem (wirksamen) Default — ohne DB-Zeile wird dann nichts geschrieben.</summary>
    public static bool EntsprichtDefault(SettingDefinition d, string? wert) => d.Type == SettingType.Bool
        ? BoolWirksam(d, wert) == BoolWirksam(d, d.DefaultValue)
        : string.Equals((wert ?? "").Trim(), d.DefaultValue.Trim(), StringComparison.Ordinal);

    /// <param name="exakt">Choice nur exakt gueltig (ServiceSettings, siehe <see cref="SettingValidator"/>).</param>
    public static SettingItem Aus(SettingDefinition d, string? roh, IReadOnlyList<(string Wert, string Text)>? optionen = null, bool exakt = false)
    {
        optionen ??= Array.Empty<(string, string)>();
        string wirksam, standard;
        if (d.Type == SettingType.Bool)
        {
            // Ohne Zeile wirkt der Default des Lesers.
            wirksam = BoolWirksam(d, roh ?? d.DefaultValue) ? "true" : "false";
            standard = BoolWirksam(d, d.DefaultValue) ? "true" : "false";
        }
        else
        {
            wirksam = roh ?? d.DefaultValue;
            standard = d.DefaultValue;
        }
        // Stammdaten-Existenz nur, wo die Auswahl vollstaendig ist (nicht bei Lagerplatz-Vorschlaegen ohne Liste).
        var schluessel = d.Type == SettingType.MasterData && optionen.Count > 0 ? optionen.Select(o => o.Wert).ToList() : null;
        // v1.79.0 F2: Ohne Zeile wirkt der Default - bei vollstaendiger Stammdaten-Auswahl wird er mitgeprueft (z. B. NegativeBuchungLagerplatz
        // = NAN, Platz umbenannt). Choice/Bool ohne Zeile bleiben ungeprueft (Schreibweisen-Pruefung haengt an roh).
        var pruefWert = roh ?? (schluessel != null ? d.DefaultValue : null);
        var pruefung = pruefWert == null ? null : SettingValidator.Validate(d, pruefWert, schluessel, exakt);
        return new SettingItem
        {
            Definition = d,
            RohWert = roh,
            Wirksam = wirksam,
            WeichtAb = !string.Equals(wirksam.Trim(), standard.Trim(), StringComparison.OrdinalIgnoreCase),
            Ungueltig = pruefung is { Gueltig: false }
                || (d.Type == SettingType.Choice && pruefung is { Gueltig: true } && !string.Equals(pruefung.Kanonisch, roh, StringComparison.Ordinal)),
            Fehler = pruefung?.Fehler ?? (d.Type == SettingType.Choice && pruefung?.Kanonisch is { } k && !string.Equals(k, roh, StringComparison.Ordinal)
                ? $"Schreibweise „{roh}“ – richtig ist „{k}“." : null),
            Optionen = optionen,
        };
    }
}

/// <summary>Eine Gruppe von Feldern in Katalog-Reihenfolge.</summary>
public sealed class SettingGroup
{
    public string Category { get; init; } = string.Empty;
    public List<SettingItem> Items { get; init; } = new();

    /// <summary>Gruppiert in Katalog-Reihenfolge (erste Nennung einer Kategorie bestimmt die Position).</summary>
    public static List<SettingGroup> Bilden(IEnumerable<SettingItem> items) =>
        items.GroupBy(i => i.Definition.Category).Select(g => new SettingGroup { Category = g.Key, Items = g.ToList() }).ToList();
}
