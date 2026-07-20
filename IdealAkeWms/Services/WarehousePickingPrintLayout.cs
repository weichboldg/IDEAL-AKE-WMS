using System.Globalization;
using System.Text.Json;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;

namespace IdealAkeWms.Services;

/// <summary>
/// Reine Druck-Layout-Logik fuer WarehousePicking/Print: Spalten-Reihenfolge/-Sichtbarkeit
/// aus User-Preferences, gerenderter Zelltext je Col-Key (EINE Quelle fuer Filter/Sort/Druck),
/// und GUI-gleiche Sortierung (repliziert table-filter.js sortTable).
/// </summary>
public static class WarehousePickingPrintLayout
{
    /// <summary>Deserialisierungs-Ziel fuer das in UserViewPreferences.SettingsJson gespeicherte Format.</summary>
    public sealed class PrintPrefs
    {
        public List<PrintPrefsColumn>? Columns { get; set; }
        public string? DefaultSortColumn { get; set; }
        public string? DefaultSortDirection { get; set; }
    }

    public sealed class PrintPrefsColumn
    {
        public string Key { get; set; } = string.Empty;
        public bool Visible { get; set; } = true;
        public int Order { get; set; }
    }

    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public static PrintPrefs? ParsePrefs(string? settingsJson)
    {
        if (string.IsNullOrWhiteSpace(settingsJson)) return null;
        try { return JsonSerializer.Deserialize<PrintPrefs>(settingsJson, JsonOpts); }
        catch (JsonException) { return null; }
    }

    /// <summary>Sichtbare Spalten in finaler Reihenfolge. Locked-Spalten bleiben immer sichtbar.</summary>
    public static List<PrintColumn> ResolveColumns(string? settingsJson)
        => ResolveColumns(ParsePrefs(settingsJson));

    public static List<PrintColumn> ResolveColumns(PrintPrefs? prefs)
    {
        var defs = ColumnDefinitions.WarehousePickingDetails.Columns;
        if (prefs?.Columns == null || prefs.Columns.Count == 0)
            return defs.Select(d => new PrintColumn(d.Key, d.Label)).ToList();

        var byKey = new Dictionary<string, PrintPrefsColumn>();
        foreach (var c in prefs.Columns)
            if (!string.IsNullOrEmpty(c.Key)) byKey[c.Key] = c; // letzter gewinnt -> dup-sicher

        return defs
            .Select((d, idx) =>
            {
                byKey.TryGetValue(d.Key, out var p);
                var visible = d.Locked || (p?.Visible ?? true);
                var order = p?.Order ?? idx;
                return (def: d, visible, order);
            })
            .Where(x => x.visible)
            .OrderBy(x => x.order)
            .Select(x => new PrintColumn(x.def.Key, x.def.Label))
            .ToList();
    }

    /// <summary>Gerenderter Zelltext je Col-Key — identisch fuer Filter, Sort und Druck.</summary>
    public static string CellText(WarehouseRequisitionDetailItemViewModel i, string key) => key switch
    {
        "pos"            => i.Position.ToString(CultureInfo.InvariantCulture),
        "article-number" => i.ArticleNumber ?? string.Empty,
        "description"    => i.ArticleDescription ?? string.Empty,
        "requested"      => Round(i.QuantityRequested).ToString(CultureInfo.InvariantCulture),
        "picked"         => i.QuantityPicked.HasValue
                                ? Round(i.QuantityPicked.Value).ToString(CultureInfo.InvariantCulture)
                                : string.Empty,
        "unit"           => i.Unit ?? string.Empty,
        "storage"        => i.StorageLocations ?? string.Empty,
        "note-lager"     => i.Note ?? string.Empty,
        "note-ek"        => i.NoteEinkauf ?? string.Empty,
        "shortage"       => i.ShortageStatus switch
        {
            ShortageStatus.WillBeRestocked => "Fehlteil",
            ShortageStatus.NoRestock       => "Wird nicht nachgeliefert",
            _                              => string.Empty
        },
        _                => string.Empty
    };

    private static int Round(decimal d) => (int)Math.Round(d, MidpointRounding.AwayFromZero);

    /// <summary>Col-Key -> Zelltext, fuer ColumnFilterHelper.Apply.</summary>
    public static readonly IReadOnlyDictionary<string, Func<WarehouseRequisitionDetailItemViewModel, string?>> ColumnMap =
        ColumnDefinitions.WarehousePickingDetails.Columns
            .ToDictionary(d => d.Key, d => (Func<WarehouseRequisitionDetailItemViewModel, string?>)(i => CellText(i, d.Key)));

    /// <summary>
    /// Stabile Sortierung wie der Bildschirm: effektive Spalte = sortCol ?? defaultSortColumn ?? "pos".
    /// Vergleich repliziert table-filter.js (numerisch wenn beide Zahlen, sonst de-Culture-String).
    /// </summary>
    public static List<WarehouseRequisitionDetailItemViewModel> SortItems(
        IEnumerable<WarehouseRequisitionDetailItemViewModel> items,
        string? sortCol, string? sortDir, string? defaultSortColumn, string? defaultSortDirection)
    {
        var list = items.ToList();

        string col;
        string dir;
        if (!string.IsNullOrWhiteSpace(sortCol))
        {
            col = sortCol!;
            dir = string.IsNullOrWhiteSpace(sortDir) ? "asc" : sortDir!;
        }
        else if (!string.IsNullOrWhiteSpace(defaultSortColumn))
        {
            col = defaultSortColumn!;
            dir = string.IsNullOrWhiteSpace(defaultSortDirection) ? "asc" : defaultSortDirection!;
        }
        else
        {
            col = "pos";
            dir = "asc";
        }

        if (!ColumnMap.ContainsKey(col)) { col = "pos"; dir = "asc"; }

        var cmp = Comparer<WarehouseRequisitionDetailItemViewModel>.Create((a, b) =>
            CompareLikeGui(CellText(a, col), CellText(b, col)));

        // OrderBy ist stabil -> gleiche Werte behalten Position-Reihenfolge (Eingangsreihenfolge).
        return dir.Equals("desc", StringComparison.OrdinalIgnoreCase)
            ? list.OrderByDescending(x => x, cmp).ToList()
            : list.OrderBy(x => x, cmp).ToList();
    }

    private static readonly CultureInfo De = CultureInfo.GetCultureInfo("de-DE");

    /// <summary>Numerisch wenn beide als Zahl (de-Format: Tausender '.', Dezimal ',') parsen, sonst de-String.</summary>
    internal static int CompareLikeGui(string a, string b)
    {
        if (TryNum(a, out var na) && TryNum(b, out var nb))
            return na.CompareTo(nb);
        return string.Compare(a, b, De, CompareOptions.None);
    }

    private static bool TryNum(string s, out double value)
    {
        var cleaned = (s ?? string.Empty).Replace(".", string.Empty).Replace(',', '.');
        return double.TryParse(cleaned, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }
}
