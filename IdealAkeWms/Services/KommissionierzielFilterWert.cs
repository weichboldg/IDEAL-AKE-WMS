namespace IdealAkeWms.Services;

/// <summary>
/// Normalisiert den gespeicherten Standardfilter „Komm.-Ziel“ der Stueckliste
/// (User.DefaultFilterBomKommissionierziel, Freitext). <c>!(leer)</c> = „Alle Ziele“ (Ziel nicht leer)
/// ist exklusiv: kommt es — in beliebiger Schreibweise — vor, wird genau <see cref="AlleZiele"/>
/// gespeichert. <c>DroppedTargets</c> ist nur true, wenn dabei Einzelziele verworfen wurden
/// (Bedeutungsaenderung -> Hinweis); reine Schreibweisen-Korrektur meldet nichts.
/// </summary>
public static class KommissionierzielFilterWert
{
    public const string AlleZiele = "!(leer)";

    public static (string? Value, bool DroppedTargets) Normalize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return (null, false);

        var parts = input.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (!parts.Any(p => string.Equals(p, AlleZiele, StringComparison.OrdinalIgnoreCase)))
            return (input.Trim(), false);

        var dropped = parts.Any(p => !string.Equals(p, AlleZiele, StringComparison.OrdinalIgnoreCase));
        return (AlleZiele, dropped);
    }
}
