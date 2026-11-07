namespace IdealAkeWms.Services.SageSdata;

/// <summary>
/// Spec 2026-11-06-sage-sdata-stammdaten-sync, Entscheidungs-Nachtrag 2026-11-07 (G16 neu, B6): EINE Regel, welcher Quellenschalter
/// <c>Sync:Quelle:&lt;Bereich&gt;</c> wegen einer offenen Pruefabfrage nicht auf <c>SData</c> darf. Gelesen von der Dienst-Weiche
/// (<c>SageQuelleWeiche</c>: SData wirkt wie View + Warnung im Protokoll) und von <c>/ServiceSettings</c> (Warnhinweis an der Einstellung,
/// ADR 0020) — damit Protokoll und Maske nie auseinanderlaufen.
/// </summary>
public static class SageQuelleSperre
{
    /// <summary>Gesetzt = IDEAL-Artikel-View (Filter im Viewtext, P-R1); leer = AKE-Filter, den der SData-Reader nachbaut.</summary>
    public const string ArtikelViewKey = "Sync:ArticlesViewName";

    /// <summary>Grund der Sperre (beginnt mit der Pruefabfrage, z. B. „P-F2 …“) oder null = SData erlaubt.</summary>
    /// <param name="bereich">Key-Suffix, z. B. <c>Artikel</c>.</param>
    /// <param name="artikelView">Wert von <see cref="ArtikelViewKey"/> (nur fuer <c>Artikel</c> relevant).</param>
    public static string? Grund(string bereich, string? artikelView) => bereich switch
    {
        "FaStruktur" => "P-F2 (IDEAL-Benutzerfelder am FA nicht belegt, G11)",
        "Artikel" when !string.IsNullOrWhiteSpace(artikelView) =>
            $"P-R1 (Artikel-View '{artikelView.Trim()}' gesetzt = IDEAL-Filter, im SData-Reader nicht nachgebaut, G16)",
        _ => null,
    };
}
