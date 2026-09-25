namespace IdealAkeWms.Services;

/// <summary>
/// Kommissionier-Relevanz einer Stuecklisten-Position (Spec 2026-09-25-kommissionierung-nur-hauptfa):
/// heute genau ein Kriterium — Feld <c>Kommissionieren</c> nicht leer. Einziger Verbraucher:
/// <see cref="KommissionierListenService.BuildFlagPredicate"/>. Ort fuer kuenftige Zusatzkriterien.
/// Die Stueckliste prueft dieselbe Semantik client-seitig ueber den Filterwert <c>!(leer)</c>
/// (Views/Picking/Bom.cshtml, bomMatchesFilter) — Aenderungen hier dort mitziehen.
/// </summary>
public static class KommissionierRelevanzFilter
{
    public static bool IsRelevant(string? kommissionieren) => !string.IsNullOrWhiteSpace(kommissionieren);
}
