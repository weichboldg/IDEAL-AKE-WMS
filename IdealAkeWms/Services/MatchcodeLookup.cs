using IdealAkeWms.Data.Repositories;
using Microsoft.Extensions.Logging;

namespace IdealAkeWms.Services;

/// <summary>
/// Matchcode-Join (Artikelstamm, Task 2): gemeinsamer Batch-Lookup fuer alle fuenf FA-Zeilenlisten
/// (ProductionOrders, PickingLeitstand, FaCompletion, FaWorklist, Picking) — eine Artikelnummern-Menge,
/// ein Repository-Call, EIN AK-14-Log-Eintrag je Listenaufbau (nicht je Zeile). Ersetzt fuenf fast
/// identische Kopien desselben Musters (DRY, wie <see cref="CoatingDateCalculator"/>).
/// </summary>
public static class MatchcodeLookup
{
    /// <summary>
    /// Laedt die Matchcodes zu <paramref name="articleNumbers"/> ueber
    /// <see cref="IArticleRepository.GetMatchcodesByArticleNumbersAsync"/> und protokolliert AK-14
    /// (Zeilen ohne Artikelstamm-Treffer) als EINE Zahl je Aufruf. Aufrufer setzt je Zeile
    /// <c>matchcodes.GetValueOrDefault(articleNumber ?? "")</c> (null = kein Treffer oder leerer
    /// Matchcode — beides F7-neutral, nie eine Exception).
    /// </summary>
    public static async Task<Dictionary<string, string?>> BuildAsync(
        IArticleRepository articleRepository,
        IEnumerable<string?> articleNumbers,
        ILogger logger,
        string listName)
    {
        var nums = articleNumbers
            .Where(n => !string.IsNullOrEmpty(n))
            .Select(n => n!)
            .Distinct()
            .ToList();

        if (nums.Count == 0)
            return new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        var matchcodes = await articleRepository.GetMatchcodesByArticleNumbersAsync(nums)
            ?? new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        var missing = nums.Count(n => !matchcodes.ContainsKey(n));
        if (missing > 0)
            logger.LogInformation(
                "Matchcode-Join: {Missing} von {Total} FA-Zeilen ohne Artikelstamm-Treffer ({Liste})",
                missing, nums.Count, listName);

        return matchcodes;
    }
}
