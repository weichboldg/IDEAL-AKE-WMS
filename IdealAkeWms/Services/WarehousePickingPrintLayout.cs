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
        // v1.99.72: der Druck kennt keinen Fraesen-Pfad (F13 nennt nur die Bildschirm-Spalte); v1.99.85 ebenso keinen Liefertermin
        var defs = ColumnDefinitions.WarehousePickingDetails.Columns.Where(d => d.Key is not ("fraesen-pfad" or "liefertermin")).ToList();
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
        string? sortCol, string? sortDir, string? defaultSortColumn, string? defaultSortDirection,
        IReadOnlyDictionary<string, Func<WarehouseRequisitionDetailItemViewModel, string?>>? colMap = null)
    {
        var list = items.ToList();
        colMap ??= ColumnMap; // v1.99.85: Print reicht die um Bildschirm-Spalten (Liefertermin, Fräsen-Pfad) ergaenzte Map durch

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

        if (!colMap.TryGetValue(col, out var text)) { col = "pos"; dir = "asc"; text = colMap["pos"]; }

        var cmp = Comparer<WarehouseRequisitionDetailItemViewModel>.Create((a, b) =>
            CompareLikeGui(text(a) ?? string.Empty, text(b) ?? string.Empty));

        // OrderBy ist stabil -> gleiche Werte behalten Position-Reihenfolge (Eingangsreihenfolge).
        return dir.Equals("desc", StringComparison.OrdinalIgnoreCase)
            ? list.OrderByDescending(x => x, cmp).ToList()
            : list.OrderBy(x => x, cmp).ToList();
    }

    private static readonly CultureInfo De = CultureInfo.GetCultureInfo("de-DE");

    /// <summary>Wie <c>compareValues</c> in table-filter.js: Datum am Anfang (dd.MM.yyyy, auch „… überfällig (+N)“) chronologisch,
    /// Werte ohne Datum aufsteigend ans Ende; sonst numerisch wenn beide als Zahl (de-Format) parsen, sonst de-String.</summary>
    internal static int CompareLikeGui(string a, string b)
    {
        var da = LeadingDate(a);
        var db = LeadingDate(b);
        if (da != null && db != null) return da.Value.CompareTo(db.Value);
        if (da != null) return -1;
        if (db != null) return 1;
        if (TryNum(a, out var na) && TryNum(b, out var nb))
            return na.CompareTo(nb);
        return string.Compare(a, b, De, CompareOptions.None);
    }

    private static DateTime? LeadingDate(string s)
        => s.Length >= 10 && DateTime.TryParseExact(s[..10], "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d : null;

    private static bool TryNum(string s, out double value)
    {
        var cleaned = (s ?? string.Empty).Replace(".", string.Empty).Replace(',', '.');
        return double.TryParse(cleaned, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }
}
