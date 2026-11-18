using System.Text.RegularExpressions;

namespace IdealAkeWms.Services.SageSdata;

/// <summary>Ein Sage-Benutzerfeld als ServiceSetting (Key = <see cref="SageUserfelder.Key"/>; Default = heutiger, logischer Name).</summary>
public sealed record SageUserfeldDef(string Abgleich, string Default, string Anzeigename, string Wirkung, string? Standort)
{
    public string Key => SageUserfelder.Key(Abgleich, Default);
}

/// <summary>
/// v1.99.40 (Spec 2026-11-18-sage-userfelder-als-einstellung, ADR 0033): jedes Sage-Benutzerfeld (USER_*), das das WMS liest, kommt aus einer
/// ServiceSetting <c>Sync:Userfeld:&lt;Abgleich&gt;:&lt;Default&gt;</c>. Lesart: Zeile fehlt = Default (AKE liest unveraendert), leer = nicht
/// konfiguriert (Feld wird nicht angefragt), ungueltig = wie leer mit Hinweis. Der Default ist zugleich der logische Name im Code
/// (Dictionary-Schluessel, Merkmal-Name) — nur der an Sage geschickte Name wechselt. Eine Quelle fuer Web und Dienst.
/// </summary>
public sealed class SageUserfelder
{
    public const string Praefix = "Sync:Userfeld:";
    /// <summary>Pruefung beim Speichern (leer erlaubt). Der Name landet im select/SQL – nur Buchstaben, Ziffern, Unterstrich.</summary>
    public const string Muster = "^(USER_[A-Za-z0-9_]{1,100})?$";
    public const string MusterText = "leer oder USER_ gefolgt von Buchstaben, Ziffern, Unterstrich (ohne Tabellen-Praefix)";
    private static readonly Regex Gueltig = new("^USER_[A-Za-z0-9_]{1,100}$", RegexOptions.CultureInvariant);

    public const string Kreditoren = "Kreditoren", Debitoren = "Debitoren", Artikel = "Artikel", Bestellungen = "Bestellungen",
        Arbeitsplaetze = "Arbeitsplaetze", Blechfertigung = "Blechfertigung", FaStruktur = "FaStruktur";

    public static string Key(string abgleich, string standard) => Praefix + abgleich + ":" + standard;

    public static string Kategorie(string abgleich) => "Sage-Benutzerfelder: " + abgleich switch
    {
        Bestellungen => "Offene Bestellungen",
        Arbeitsplaetze => "Arbeitsplätze",
        FaStruktur => "FA-Struktur",
        _ => abgleich,
    };

    private const string FaWirkung = "FA-Struktur (SData/Vergleich): Feld am FA-Kopf (dtsFaBelegsuche, mit Praefix KHKPpsFaBelege_) und, falls am Artikel vorhanden, am Artikel.";
    private const string BfWirkung = "Blechfertigung: Artikel-Abgleich liest das Feld per SData nach (Sync:BlechteileFelderNachlesen) und speichert es als Merkmal unter dem Default-Namen; BF-Aufträge lesen das Merkmal.";

    public static readonly IReadOnlyList<SageUserfeldDef> Katalog = new SageUserfeldDef[]
    {
        new(Kreditoren, "USER_EKMindestbestellwert", "Kreditoren: Mindestbestellwert", "Kreditoren-Abgleich (SData Kontokorrent) → Kreditor „Mindestbestellwert (Sage)“.", "AKE"),
        new(Debitoren, "USER_KundenNameFA", "Debitoren: Kundenname FA", "Debitoren-Abgleich (SData Adressen) → Debitor „Kundenname FA (Sage)“.", null),
        new(Artikel, "USER_LagerplatzID", "Artikel: Lagerplatz-ID", "Artikel-Abgleich über SData (ArtikelVariantenExtended) → Artikel-Sage-Details.", "AKE"),
        new(Artikel, "USER_Mindestbestand", "Artikel: Mindestbestand", "Artikel-Abgleich über SData (ArtikelVariantenExtended) → Artikel-Sage-Details, Spalte „Mindestbestand (Sage)“ der Bestellvorschläge.", "AKE"),
        new(Bestellungen, "USER_AB", "Offene Bestellungen: AB-Nummer", "Offene Bestellungen (RealTimeData-Kopf, Praefix KHKEKBelege_) → AB-Nummer.", null),
        new(Bestellungen, "USER_Kom", "Offene Bestellungen: Kommission", "Offene Bestellungen (RealTimeData-Kopf, Praefix KHKEKBelege_) → Kommission.", null),
        new(Arbeitsplaetze, "USER_ArbeitsSchritt", "Arbeitsplätze: Arbeitsschritt-Kürzel", "Werkbank-Anlage und Arbeitsgang-Abgleich aus der Sage-Tabelle KHKPpsArbeitsplaetze (nicht bei View-Quelle).", null),
        new(Blechfertigung, "USER_OSIstLagerartikel", "Blechfertigung: Ist Lagerartikel", BfWirkung + " Filter der Nachlese (ne 0) und der BF-Artikelsuche.", "IDEAL"),
        new(Blechfertigung, "USER_OSArtikeltyp", "Blechfertigung: Artikeltyp", BfWirkung + " Filter der BF-Artikelsuche (BG/FT).", "IDEAL"),
        new(Blechfertigung, "USER_OSMaterial", "Blechfertigung: Material", BfWirkung, "IDEAL"),
        new(Blechfertigung, "USER_OSMaterialnummerPN", "Blechfertigung: Materialnummer", BfWirkung, "IDEAL"),
        new(Blechfertigung, "USER_OSFertigungsInfoPN", "Blechfertigung: Fertigungsinfo", BfWirkung, "IDEAL"),
        new(Blechfertigung, "USER_OSArbeitsschritte", "Blechfertigung: Arbeitsschritte", BfWirkung, "IDEAL"),
        new(Blechfertigung, "USER_OSAbteilung", "Blechfertigung: Abteilung", BfWirkung, "IDEAL"),
        new(FaStruktur, "USER_OSBemerkungPN", "FA-Struktur: Bemerkung", FaWirkung, "IDEAL"),
        new(FaStruktur, "USER_OSFertigungsInfoPN", "FA-Struktur: Fertigungsinfo", FaWirkung, "IDEAL"),
        new(FaStruktur, "USER_OSKommissionieren", "FA-Struktur: Kommissionieren", FaWirkung, "IDEAL"),
        new(FaStruktur, "USER_OSAbteilung", "FA-Struktur: Abteilung (Arbeitsbereich)", FaWirkung, "IDEAL"),
        new(FaStruktur, "USER_OSArbeitsschritte", "FA-Struktur: Arbeitsschritte", FaWirkung, "IDEAL"),
        new(FaStruktur, "USER_OSArtikeltyp", "FA-Struktur: Artikeltyp", FaWirkung, "IDEAL"),
        new(FaStruktur, "USER_OSMaterial", "FA-Struktur: Material", FaWirkung, "IDEAL"),
        new(FaStruktur, "USER_OSVMBedarf", "FA-Struktur: VM-Bedarf", FaWirkung, "IDEAL"),
        new(FaStruktur, "USER_OSBeschichtet", "FA-Struktur: Beschichtet", FaWirkung, "IDEAL"),
        new(FaStruktur, "USER_OSBreite", "FA-Struktur: Breite", FaWirkung, "IDEAL"),
        new(FaStruktur, "USER_OSHoehe", "FA-Struktur: Höhe", FaWirkung, "IDEAL"),
        new(FaStruktur, "USER_OSTiefe", "FA-Struktur: Tiefe", FaWirkung, "IDEAL"),
        new(FaStruktur, "USER_OSEKBedarf", "FA-Struktur: EK-Bedarf", FaWirkung, "IDEAL"),
    };

    private readonly Dictionary<string, string?> _feld = new(StringComparer.Ordinal);
    private readonly List<string> _hinweise = new();

    /// <summary>Hinweise zu ungueltigen Werten (je Einstellung einer) – der Aufrufer schreibt sie als Warnung in seinen Lauf.</summary>
    public IReadOnlyList<string> Hinweise => _hinweise;

    /// <summary><paramref name="roh"/>: Wert der Einstellung je Key, <c>null</c> = Zeile fehlt (→ Default).</summary>
    public static SageUserfelder Aus(Func<string, string?> roh)
    {
        var f = new SageUserfelder();
        foreach (var d in Katalog)
        {
            var wert = roh(d.Key);
            if (wert is null) { f._feld[d.Key] = d.Default; continue; }
            wert = wert.Trim();
            if (wert.Length > 0 && !Gueltig.IsMatch(wert))
                f._hinweise.Add($"Einstellung „{d.Anzeigename}“ ({d.Key}): „{wert}“ ist kein gültiger Feldname – Feld wird nicht gelesen.");
            f._feld[d.Key] = Gueltig.IsMatch(wert) ? wert : null;
        }
        return f;
    }

    public const string NichtKonfiguriert = "nicht konfiguriert";

    /// <summary>Web: Einstellungen aus dem Repository (eine Abfrage).</summary>
    public static async Task<SageUserfelder> AusAsync(IdealAkeWms.Data.Repositories.IServiceSettingRepository repo)
    {
        var werte = (await repo.GetAllAsync()).Where(s => s.Key.StartsWith(Praefix, StringComparison.Ordinal))
            .ToDictionary(s => s.Key, s => s.Value ?? "", StringComparer.Ordinal);
        return Aus(k => werte.TryGetValue(k, out var v) ? v : null);
    }

    /// <summary>
    /// Wert eines Benutzerfelds aus einer SData-Zeile: Hat der Datensatz die Eigenschaft unter dem Default-Namen (typisierte Zeile),
    /// kommt der Wert von dort (<paramref name="typisiert"/>); bei abweichendem Namen aus den nicht zugeordneten Feldern (JsonExtensionData).
    /// </summary>
    public static string? Text(string? feld, string standard, object? typisiert, IReadOnlyDictionary<string, System.Text.Json.JsonElement>? weitere)
    {
        if (feld is null) return null;
        if (string.Equals(feld, standard, StringComparison.OrdinalIgnoreCase))
            return typisiert switch
            {
                null => null,
                string t => string.IsNullOrWhiteSpace(t) ? null : t.Trim(),
                IFormattable z => z.ToString(null, System.Globalization.CultureInfo.InvariantCulture),
                var o => o.ToString(),
            };
        var w = weitere?.FirstOrDefault(kv => string.Equals(kv.Key, feld, StringComparison.OrdinalIgnoreCase));
        return w?.Key is null ? null : w.Value.Value.ValueKind switch
        {
            System.Text.Json.JsonValueKind.String => string.IsNullOrWhiteSpace(w.Value.Value.GetString()) ? null : w.Value.Value.GetString()!.Trim(),
            System.Text.Json.JsonValueKind.Number or System.Text.Json.JsonValueKind.True or System.Text.Json.JsonValueKind.False => w.Value.Value.GetRawText(),
            _ => null,
        };
    }

    public static decimal? Dezimal(string? text) =>
        decimal.TryParse(text, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var d) ? d : null;

    /// <summary>Alle Defaults (keine Einstellungszeilen) – AKE-Stand.</summary>
    public static SageUserfelder Standard => Aus(_ => null);

    /// <summary>Sage-Feldname oder <c>null</c> = nicht konfiguriert. Unbekannter Key = Programmierfehler (wirft).</summary>
    public string? Feld(string abgleich, string standard) => _feld[Key(abgleich, standard)];

    /// <summary>Logischer Name (Default) → Sage-Feldname, nur konfigurierte, in Katalog-Reihenfolge der Eingabe.</summary>
    public List<(string Logisch, string Feld)> Konfiguriert(string abgleich, IEnumerable<string> standards) =>
        standards.Select(s => (s, Feld(abgleich, s))).Where(x => x.Item2 is not null).Select(x => (x.s, x.Item2!)).ToList();

    private const string HinweisAnfang = "Sage kennt das Feld „";

    /// <summary>
    /// Stellt einer Sage-Fehlermeldung „Ungueltiger Spaltenname“ die Einstellung voran, aus der der Name stammt (R4). Sonst unveraendert.
    /// v1.99.42 (Befund 13:25): VORAN statt hinten an - die Meldung wird gekappt (SdataLeseException.Message 600, SyncLogs 1000), bei HTTP 500
    /// steht davor das erzeugte SQL, angehaengt fiel der Hinweis weg. <paramref name="weitere"/> = ungekappte Diagnose; schon erklaerte
    /// Meldungen bleiben, wie sie sind (einmal je Lauf, nie doppelt).
    /// </summary>
    public string Erklaeren(string meldung, string? weitere = null)
    {
        if (meldung.Contains(HinweisAnfang, StringComparison.Ordinal)) return meldung;
        var text = meldung + " " + weitere;
        if (text.IndexOf("Spaltenname", StringComparison.OrdinalIgnoreCase) < 0
            && text.IndexOf("column name", StringComparison.OrdinalIgnoreCase) < 0)
            return meldung;
        var treffer = Katalog.Where(d => _feld[d.Key] is { } n && Regex.IsMatch(text, $@"(?<![A-Za-z0-9]){Regex.Escape(n)}(?![A-Za-z0-9_])", RegexOptions.IgnoreCase))
            .Select(d => $"{HinweisAnfang}{_feld[d.Key]}“ nicht – Einstellung „{d.Anzeigename}“ ({d.Key}) prüfen oder leer setzen")
            .Distinct().ToList();
        return treffer.Count == 0 ? meldung : string.Join("; ", treffer) + ". Sage: " + meldung;
    }

    /// <summary>Wie <see cref="Erklaeren(string, string?)"/> mit der ungekappten SData-Diagnose bzw. der inneren Ausnahme.</summary>
    public string Erklaeren(Exception ex) =>
        Erklaeren(ex.Message, (ex as SdataLeseException)?.Diagnose ?? ex.InnerException?.Message);
}
