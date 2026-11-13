using System.Globalization;
using System.Text.Json;
using System.Xml.Linq;

namespace IdealAkeWms.Services.SageSdata;

/// <summary>Ein Feld laut <c>$schema</c>.</summary>
public sealed record SchemaFeld(string Name, string Typ, int? Laenge, bool Pflicht)
{
    public string TypText => Laenge is { } l ? $"{Typ} ({l})" : Typ;
}

/// <summary>Schema einer Ressource: Felder in Schema-Reihenfolge und die Rechte laut <c>sme:canGet/Post/Put/Delete</c> (Vertrag, nicht Benutzer).</summary>
public sealed record SchemaInfo(IReadOnlyList<SchemaFeld> Felder, bool Lesen, bool Anlegen, bool Aendern, bool Loeschen);

/// <summary>Ein flaches Feld einer Datenzeile: Pfad, Wert (gekuerzt), Typ aus den Daten (nur Benutzerfeld-Listen).</summary>
public sealed record FlachesFeld(string Feld, string? Wert, string? TypDaten)
{
    public bool Benutzerfeld => SageTestAufbereitung.IstBenutzerfeld(Feld);
}

/// <summary>Zeile der Feldliste (F3).</summary>
public sealed class FeldlistenZeile
{
    public required string Feld { get; init; }
    public string? TypLaenge { get; set; }
    public bool ImSchema { get; set; }
    public bool InDaten { get; set; }
    public bool Benutzerfeld => SageTestAufbereitung.IstBenutzerfeld(Feld);
    /// <summary>Beispielwert je Datenzeile (null = Feld fehlt in dieser Zeile bzw. Wert null).</summary>
    public string?[] Beispiele { get; set; } = Array.Empty<string?>();
}

/// <summary>Zeile der Stueckliste live (F4): eingerueckt nach <see cref="Tiefe"/>; FA-Zeilen (<see cref="IstFa"/>) im FA-Modus.</summary>
public sealed class StuecklistenZeile
{
    public int Tiefe { get; init; }
    public bool IstFa { get; init; }
    public string? Ebene { get; init; }
    public string? Position { get; init; }
    public string? Ressourcetyp { get; init; }
    public string? Ressourcenummer { get; init; }
    public string? Bezeichnung { get; init; }
    public string? Menge { get; init; }
    public string? Arbeitsgang { get; init; }
    public string? ArtikelnummerUbg { get; init; }
    public string? Platz { get; init; }
    public string? BelPosId { get; init; }
    public string? Sollmenge { get; init; }
    public string? Istmenge { get; init; }
    public IReadOnlyList<FlachesFeld> AlleFelder { get; init; } = Array.Empty<FlachesFeld>();
}

/// <summary>
/// Spec 2026-11-13-sdata-teil-2-userfelder-stueckliste F3/F4: reine Aufbereitung fuer das Register Sage-Test (Muster
/// <see cref="SageSdataFaTreeBuilder"/>) — Schema-XML → Felder/Rechte, JSON → flache Felder, Positionen → Baum. Ohne DB, ohne HTTP.
/// Feldnamen am AKE-Test belegt (Lesetest 2026-11-13, [[sage-sdata-benutzerfelder-lesetest]]).
/// </summary>
public static class SageTestAufbereitung
{
    public const int MaxWertLaenge = 200;
    private static readonly XNamespace Xs = "http://www.w3.org/2001/XMLSchema";
    private static readonly XNamespace Sme = "http://schemas.sage.com/sdata/sme/2007";

    /// <summary>Benutzerfeld: Name enthaelt <c>USER_</c> (auch RealTimeData <c>Tabelle_USER_…</c>) oder stammt aus <c>SpecificUserfields</c>.</summary>
    public static bool IstBenutzerfeld(string feld)
    {
        var letzter = feld[(feld.LastIndexOf('.') + 1)..];
        return letzter.Contains("USER_", StringComparison.OrdinalIgnoreCase)
            || feld.Contains("SpecificUserfields.", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Felder + Rechte der Ressource aus einem Ressourcen- ODER Vertrags-<c>$schema</c>; null, wenn die Ressource nicht darin steht
    /// bzw. der Text kein XML ist.</summary>
    public static SchemaInfo? SchemaLesen(string? xml, string ressource)
    {
        if (string.IsNullOrWhiteSpace(xml)) return null;
        XDocument doc;
        try { doc = XDocument.Parse(xml); }
        catch (System.Xml.XmlException) { return null; }

        var element = doc.Descendants(Xs + "element").FirstOrDefault(e =>
            string.Equals((string?)e.Attribute("name"), ressource, StringComparison.OrdinalIgnoreCase)
            && (string?)e.Attribute(Sme + "role") == "resourceKind");
        if (element is null) return null;
        bool Recht(string a) => string.Equals((string?)element.Attribute(Sme + a), "true", StringComparison.OrdinalIgnoreCase);

        var typ = OhnePraefix((string?)element.Attribute("type"));
        var complex = typ is null ? null : doc.Descendants(Xs + "complexType").FirstOrDefault(c => (string?)c.Attribute("name") == typ);
        var felder = complex?.Elements().Where(x => x.Name == Xs + "all" || x.Name == Xs + "sequence" || x.Name == Xs + "choice")
            .Elements(Xs + "element")
            .Select(f => new SchemaFeld(
                (string?)f.Attribute("name") ?? "?",
                OhnePraefix((string?)f.Attribute("type")) ?? "?",
                int.TryParse((string?)f.Attribute(Sme + "maxLength"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var l) ? l : null,
                string.Equals((string?)f.Attribute(Sme + "isMandatory"), "true", StringComparison.OrdinalIgnoreCase)))
            .ToList() ?? new List<SchemaFeld>();
        return new SchemaInfo(felder, Recht("canGet"), Recht("canPost"), Recht("canPut"), Recht("canDelete"));
    }

    private static string? OhnePraefix(string? typ) => typ is null ? null : typ[(typ.IndexOf(':') + 1)..];

    /// <summary>
    /// Flache Felder einer Zeile. SData-Metadaten (<c>$key</c>, <c>$url</c> …) entfallen. <c>SpecificUserfields</c>-Listen
    /// (<c>DatafieldName</c>/<c>DatafieldValue</c>) werden zu <c>SpecificUserfields.&lt;Name&gt;</c>, andere Kind-Listen zu <c>Kind[n].Feld</c>.
    /// </summary>
    public static List<FlachesFeld> Flach(JsonElement zeile, params string[] ohne)
    {
        var liste = new List<FlachesFeld>();
        Sammeln(zeile, "", liste, ohne);
        return liste;
    }

    private static void Sammeln(JsonElement obj, string praefix, List<FlachesFeld> liste, string[] ohne)
    {
        if (obj.ValueKind != JsonValueKind.Object) return;
        foreach (var p in obj.EnumerateObject())
        {
            if (p.Name.StartsWith('$') || (praefix.Length == 0 && ohne.Contains(p.Name, StringComparer.OrdinalIgnoreCase))) continue;
            var pfad = praefix + p.Name;
            var wert = p.Value;
            if (wert.ValueKind == JsonValueKind.Object && wert.TryGetProperty("$resources", out var res) && res.ValueKind == JsonValueKind.Array)
                wert = res;
            switch (wert.ValueKind)
            {
                case JsonValueKind.Array when IstBenutzerfeldListe(wert):
                    foreach (var u in wert.EnumerateArray())
                    {
                        var typ = Text(Prop(u, "DatafieldType"));
                        var laenge = Text(Prop(u, "DatafieldLength"));
                        liste.Add(new FlachesFeld($"{pfad}.{Text(Prop(u, "DatafieldName"))}", Kurz(Text(Prop(u, "DatafieldValue"))),
                            typ is null ? null : laenge is null ? typ : $"{typ} ({laenge})"));
                    }
                    break;
                case JsonValueKind.Array when wert.EnumerateArray().Any(e => e.ValueKind == JsonValueKind.Object):
                    var i = 0;
                    foreach (var e in wert.EnumerateArray()) Sammeln(e, $"{pfad}[{i++}].", liste, ohne);
                    break;
                case JsonValueKind.Object:
                    Sammeln(wert, pfad + ".", liste, ohne);
                    break;
                default:
                    liste.Add(new FlachesFeld(pfad, Kurz(Text(wert)), null));
                    break;
            }
        }
    }

    private static bool IstBenutzerfeldListe(JsonElement array) =>
        array.GetArrayLength() > 0 && array.EnumerateArray().All(e => e.ValueKind == JsonValueKind.Object && Prop(e, "DatafieldName") is not null);

    /// <summary>Feldliste: Schemafelder in Schema-Reihenfolge, danach Felder nur aus den Daten in Fundreihenfolge.</summary>
    public static List<FeldlistenZeile> Feldliste(SchemaInfo? schema, IReadOnlyList<JsonElement> zeilen)
    {
        var ergebnis = new List<FeldlistenZeile>();
        var index = new Dictionary<string, FeldlistenZeile>(StringComparer.OrdinalIgnoreCase);
        FeldlistenZeile Hole(string feld)
        {
            if (index.TryGetValue(feld, out var z)) return z;
            z = new FeldlistenZeile { Feld = feld, Beispiele = new string?[zeilen.Count] };
            index[feld] = z;
            ergebnis.Add(z);
            return z;
        }
        foreach (var f in schema?.Felder ?? Array.Empty<SchemaFeld>())
        {
            var z = Hole(f.Name);
            z.ImSchema = true;
            z.TypLaenge = f.TypText + (f.Pflicht ? ", Pflicht" : "");
        }
        for (var i = 0; i < zeilen.Count; i++)
            foreach (var f in Flach(zeilen[i]))
            {
                var z = Hole(f.Feld);
                z.InDaten = true;
                z.Beispiele[i] = f.Wert;
                if (!z.ImSchema && z.TypLaenge is null && f.TypDaten is not null) z.TypLaenge = f.TypDaten + " (Daten)";
            }
        return ergebnis;
    }

    /// <summary>Mehrstufige Ressourcenliste (<c>PpsRessourcenPositionenResolvedInfo</c>) als Baum ueber <c>RowGuid</c>/<c>RowGuidParent</c>;
    /// ohne RowGuid in Lieferreihenfolge, eingerueckt nach <c>Ebene</c>.</summary>
    public static List<StuecklistenZeile> RessourcenBaum(IReadOnlyList<JsonElement> positionen)
    {
        var ergebnis = new List<StuecklistenZeile>();
        var guids = positionen.Select(p => Text(Prop(p, "RowGuid"))).Where(g => !string.IsNullOrEmpty(g)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (guids.Count == 0)
        {
            foreach (var p in positionen)
                ergebnis.Add(Zeile(p, int.TryParse(Text(Prop(p, "Ebene")), out var e) ? Math.Max(0, e - 1) : 0, false));
            return ergebnis;
        }
        var kinder = positionen.ToLookup(p => Text(Prop(p, "RowGuidParent")) is { Length: > 0 } g && guids.Contains(g) ? g.ToLowerInvariant() : "");
        var besucht = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        void Ab(string eltern, int tiefe)
        {
            foreach (var p in kinder[eltern])
            {
                var g = Text(Prop(p, "RowGuid")) ?? "";
                if (g.Length > 0 && !besucht.Add(g)) continue; // Zyklus-Schutz
                ergebnis.Add(Zeile(p, tiefe, false));
                if (g.Length > 0) Ab(g.ToLowerInvariant(), tiefe + 1);
            }
        }
        Ab("", 0);
        return ergebnis;
    }

    /// <summary>FA-Struktur mit Positionen (<c>PpsFaBelegeInfo</c> + <c>PpsFaBelegePositionenInfo</c>): FA-Zeile, darunter ihre Positionen;
    /// eine Position mit <c>BelIDNachfolger</c> traegt die Sub-FA darunter. FAs ohne Platz im Baum folgen am Ende (Tiefe 0).</summary>
    public static List<StuecklistenZeile> FaBaum(IReadOnlyList<JsonElement> fas, IReadOnlyList<JsonElement> positionen, int strukturId)
    {
        var ergebnis = new List<StuecklistenZeile>();
        var faJeId = new Dictionary<int, JsonElement>();
        foreach (var f in fas) if (Zahl(Prop(f, "BelID")) is { } id) faJeId.TryAdd(id, f);
        var posJeFa = positionen.ToLookup(p => Zahl(Prop(p, "BelID")) ?? 0);
        var besucht = new HashSet<int>();
        void Fa(int id, int tiefe)
        {
            if (!besucht.Add(id) || !faJeId.TryGetValue(id, out var fa)) return;
            ergebnis.Add(Zeile(fa, tiefe, true));
            foreach (var p in posJeFa[id])
            {
                ergebnis.Add(Zeile(p, tiefe + 1, false));
                if (Zahl(Prop(p, "BelIDNachfolger")) is { } nach and > 0) Fa(nach, tiefe + 2);
            }
        }
        Fa(strukturId, 0);
        foreach (var id in faJeId.Keys.Where(id => !besucht.Contains(id)).ToList()) Fa(id, 0);
        return ergebnis;
    }

    private static StuecklistenZeile Zeile(JsonElement e, int tiefe, bool istFa) => new()
    {
        Tiefe = tiefe,
        IstFa = istFa,
        Ebene = Text(Prop(e, "Ebene")),
        Position = istFa ? Text(Prop(e, "BelID")) : Text(Prop(e, "Position")),
        Ressourcetyp = istFa ? "FA" : Text(Prop(e, "Ressourcetyp")),
        Ressourcenummer = istFa ? Text(Prop(e, "Artikelnummer")) : Text(Prop(e, "Ressourcenummer")),
        Bezeichnung = Text(Prop(e, "Matchcode")) ?? Text(Prop(e, "Bezeichnung1")),
        Menge = istFa ? Text(Prop(e, "Sollmenge")) : Text(Prop(e, "Menge")),
        Arbeitsgang = Text(Prop(e, "Arbeitsgangnummer")),
        ArtikelnummerUbg = Text(Prop(e, "ArtikelnummerUBG")),
        Platz = Text(Prop(e, "Platzbezeichnung")) ?? Text(Prop(e, "Kurzbezeichnung")) ?? Text(Prop(e, "PlatzID")),
        BelPosId = Text(Prop(e, "BelPosID")),
        Sollmenge = Text(Prop(e, "Sollmenge")),
        Istmenge = Text(Prop(e, "Istmenge")),
        AlleFelder = Flach(e, "PpsRessourcenPositionenResolvedInfo"),
    };

    /// <summary>Mandant = Zahl nach dem letzten <c>;</c> im Dataset (<c>ake_TEST2026;1</c> → 1), sonst null.</summary>
    public static int? MandantAusDataset(string? dataset)
    {
        var i = dataset?.LastIndexOf(';') ?? -1;
        return i >= 0 && int.TryParse(dataset![(i + 1)..], NumberStyles.Integer, CultureInfo.InvariantCulture, out var m) ? m : null;
    }

    /// <summary>Feld ohne Gross/Klein (Sage schreibt z. B. <c>RessourceTyp</c> in FA-Positionen, <c>Ressourcetyp</c> in der Ressourcenliste).</summary>
    public static JsonElement? Prop(JsonElement e, string name)
    {
        if (e.ValueKind != JsonValueKind.Object) return null;
        foreach (var p in e.EnumerateObject())
            if (string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)) return p.Value;
        return null;
    }

    public static string? Text(JsonElement? e) => e?.ValueKind switch
    {
        JsonValueKind.String => e.Value.GetString(),
        JsonValueKind.Number => e.Value.GetRawText(),
        JsonValueKind.True => "true",
        JsonValueKind.False => "false",
        JsonValueKind.Array or JsonValueKind.Object => e.Value.GetRawText(),
        _ => null,
    };

    public static int? Zahl(JsonElement? e) =>
        int.TryParse(Text(e), NumberStyles.Integer, CultureInfo.InvariantCulture, out var i) ? i : null;

    private static string? Kurz(string? s) => s is null || s.Length <= MaxWertLaenge ? s : s[..MaxWertLaenge] + "…";
}
