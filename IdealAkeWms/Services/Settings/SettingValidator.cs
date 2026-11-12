using System.Globalization;
using IdealAkeWms.Models;

namespace IdealAkeWms.Services.Settings;

/// <summary>Ergebnis einer Pruefung: gueltig mit kanonischem Wert, oder eine deutsche Fehlermeldung.</summary>
public sealed record SettingPruefung(bool Gueltig, string? Kanonisch, string? Fehler)
{
    public static SettingPruefung Ok(string kanonisch) => new(true, kanonisch, null);
}

/// <summary>
/// Spec 2026-11-07-einstellungen-auswahlfelder-und-beschreibung F5 (ADR 0020 Punkt 2): EINE Pruefung je Typ fuer beide Masken
/// (/Settings, /ServiceSettings) — beim Speichern (E4a) und fuer die Anzeige „ungueltig“ (E2/E3) sowie die Bestandspruefung (E4b).
/// Rein, ohne DB: Stammdaten-Schluessel reicht der Aufrufer herein (null = nicht pruefen).
/// </summary>
public static class SettingValidator
{
    /// <param name="exakt">Choice nur in exakter Schreibweise (ServiceSettings: der Dienst liest ordinal, <c>SageQuelleSchalter</c>);
    /// sonst ohne Gross/Klein und auf die Katalog-Schreibweise kanonisiert (AppSettings, z. B. QrErstesFeld).</param>
    public static SettingPruefung Validate(SettingDefinition d, string? roh, IReadOnlyCollection<string>? stammdaten = null, bool exakt = false)
    {
        var name = d.Anzeigename ?? d.Key;
        var t = roh?.Trim() ?? "";
        switch (d.Type)
        {
            case SettingType.Bool:
                // Ohne Trim wie die Leser (QA B3): „ false“ ist kein gueltiger Wert.
                if (string.IsNullOrEmpty(roh)) return SettingPruefung.Ok("");
                if (roh.Equals("true", StringComparison.OrdinalIgnoreCase)) return SettingPruefung.Ok("true");
                if (roh.Equals("false", StringComparison.OrdinalIgnoreCase)) return SettingPruefung.Ok("false");
                return Fehler(name, roh, "an oder aus (true/false)");

            case SettingType.Int:
                if (t.Length == 0)
                    return d.LeerErlaubt ? SettingPruefung.Ok("") : Fehler(name, roh, Bereich(d, "eine ganze Zahl"));
                if (!int.TryParse(t, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i)
                    || (d.Min is { } min && i < min) || (d.Max is { } max && i > max))
                    return Fehler(name, roh, Bereich(d, "eine ganze Zahl"));
                return SettingPruefung.Ok(i.ToString(CultureInfo.InvariantCulture));

            case SettingType.Choice:
            {
                var werte = d.Werte ?? Array.Empty<(string Wert, string Text)>();
                var treffer = werte.Select(w => w.Wert).FirstOrDefault(w => exakt
                    ? string.Equals(w, roh, StringComparison.Ordinal)
                    : string.Equals(w, t, StringComparison.OrdinalIgnoreCase));
                return treffer != null ? SettingPruefung.Ok(treffer) : Fehler(name, roh, Liste(werte.Select(w => w.Wert)));
            }

            case SettingType.MultiChoice:
            {
                var werte = (d.Werte ?? Array.Empty<(string Wert, string Text)>()).Select(w => w.Wert).ToList();
                var teile = t.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (teile.Length == 0)
                    return d.LeerErlaubt ? SettingPruefung.Ok("") : Fehler(name, roh, "mindestens ein Wert aus " + Liste(werte));
                if (teile.Any(p => !werte.Contains(p, StringComparer.OrdinalIgnoreCase))) return Fehler(name, roh, Liste(werte));
                return SettingPruefung.Ok(string.Join(",", werte.Where(w => teile.Contains(w, StringComparer.OrdinalIgnoreCase))));
            }

            case SettingType.MasterData:
                if (t.Length == 0)
                    return d.LeerErlaubt ? SettingPruefung.Ok("") : Fehler(name, roh, "ein vorhandener Stammdaten-Eintrag");
                if (stammdaten == null) return SettingPruefung.Ok(t);
                var s = stammdaten.FirstOrDefault(x => string.Equals(x, t, StringComparison.OrdinalIgnoreCase));
                return s != null ? SettingPruefung.Ok(s) : Fehler(name, roh, "ein vorhandener Stammdaten-Eintrag");

            case SettingType.String when d.Muster is { } muster:
                // v1.99.9: festes Format (System-Kuerzel); gespeichert wird der getrimmte Wert
                return System.Text.RegularExpressions.Regex.IsMatch(t, muster.Regex) ? SettingPruefung.Ok(t) : Fehler(name, roh, muster.Erlaubt);

            default:
                return SettingPruefung.Ok(roh ?? "");
        }
    }

    private static string Bereich(SettingDefinition d, string basis)
    {
        var leer = d.LeerErlaubt ? " oder leer" : "";
        return (d.Min, d.Max) switch
        {
            ({ } min, { } max) => $"{basis} von {min} bis {max}{leer}",
            ({ } min, null) => $"{basis} ab {min}{leer}",
            (null, { } max) => $"{basis} bis {max}{leer}",
            _ => basis + leer,
        };
    }

    private static string Liste(IEnumerable<string> werte) => string.Join(", ", werte.Select(w => $"„{w}“"));

    private static SettingPruefung Fehler(string name, string? roh, string erlaubt) =>
        new(false, null, $"„{name}“: „{roh}“ ist nicht erlaubt – erlaubt: {erlaubt}.");
}
