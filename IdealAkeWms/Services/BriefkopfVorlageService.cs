using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using Microsoft.Extensions.Logging;

namespace IdealAkeWms.Services;

/// <summary>Ergebnis fuer den Briefkopf einer Druckseite. <see cref="IstErsatz"/>: Vorlage nicht nutzbar, der
/// Kopf-Partial zeigt Firmenname + Firmenanschrift. <see cref="Hinweise"/> erscheinen nur am Bildschirm.</summary>
public sealed record BriefkopfVorlage(bool IstErsatz, string Styles, string Markup, IReadOnlyList<string> Hinweise)
{
    public string? Firmenname { get; init; }
    public string? Firmenanschrift { get; init; }
}

/// <summary>
/// Briefkopf aus der IT-Vorlage <c>briefkopf.html</c> (Spec 2026-10-06-beschichtungsauftrag-ausbau-teil-2, Antworten
/// 1, 6, 8–11). Der Pfad kommt aus der Einstellung „Briefkopf-Vorlage (Pfad)" (<see cref="AppSettingKeys.BriefkopfVorlagePfad"/>,
/// nur Admin). Die Vorlage wird gelesen, von aktiven Inhalten befreit, Bilder und Schriften aus demselben Ordner
/// (Branding) werden als Data-URI eingebettet — Bildschirmdruck UND Headless-Edge-PDF sehen so dasselbe, ohne Netz-
/// oder Dateizugriff. Fehlt/unlesbar/zu gross/falsche Endung ⇒ Ersatz-Kopf; der Druck bricht nie ab, jeder Fall wird
/// gemeldet (Hinweis am Bildschirm + <c>LogWarning</c>). Scoped: je Anfrage einmal gelesen, bewusst kein Cache
/// darueber hinaus (Antwort 10 — Aenderungen der IT wirken sofort).
/// </summary>
public class BriefkopfVorlageService
{
    public const int MaxVorlageBytes = 256 * 1024;
    public const int MaxDateiBytes = 1024 * 1024;
    public const int MaxSummeBytes = 3 * 1024 * 1024;

    /// <summary>Obergrenze der eingebetteten Data-URI-Zeichen insgesamt (jede Verwendung zaehlt): schuetzt vor einer
    /// Vorlage, die dieselbe 1-MB-Datei hundertfach referenziert (Summe zaehlt jede Datei nur einmal).</summary>
    private const int MaxAusgabeZeichen = 8 * 1024 * 1024;

    /// <summary>Erlaubte eingebettete Dateitypen (Endung → MIME). Kein SVG (Antwort 9).</summary>
    private static readonly IReadOnlyDictionary<string, string> Mime = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        [".png"] = "image/png", [".jpg"] = "image/jpeg", [".jpeg"] = "image/jpeg",
        [".woff"] = "font/woff", [".woff2"] = "font/woff2", [".ttf"] = "font/ttf", [".otf"] = "font/otf",
    };

    private const RegexOptions Opt = RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant;

    private readonly IAppSettingRepository _settings;
    private readonly ILogger<BriefkopfVorlageService> _logger;
    private BriefkopfVorlage? _current;

    public BriefkopfVorlageService(IAppSettingRepository settings, ILogger<BriefkopfVorlageService> logger)
    {
        _settings = settings;
        _logger = logger;
    }

    /// <summary>Liefert den Briefkopf dieser Anfrage (einmal geladen, danach gemerkt) und loggt jeden Hinweis.</summary>
    public async Task<BriefkopfVorlage> GetAsync()
    {
        if (_current != null) return _current;

        var pfad = await _settings.GetValueAsync(AppSettingKeys.BriefkopfVorlagePfad);
        var vorlage = Load(pfad) with
        {
            Firmenname = await _settings.GetValueAsync(AppSettingKeys.Firmenname),
            Firmenanschrift = await _settings.GetValueAsync(AppSettingKeys.Firmenanschrift),
        };
        foreach (var h in vorlage.Hinweise)
            _logger.LogWarning("Briefkopf: {Hinweis}", h);

        return _current = vorlage;
    }

    /// <summary>
    /// Laedt und bereinigt die Vorlage (DB-frei, unit-testbar). Wirft nie.
    /// </summary>
    public static BriefkopfVorlage Load(string? vorlagePfad)
    {
        if (string.IsNullOrWhiteSpace(vorlagePfad))
            return Ersatz("Briefkopf-Vorlage nicht konfiguriert (Standorteinstellungen → Firmendaten → „Briefkopf-Vorlage (Pfad)\") — Ersatz-Kopf aus Firmenname und Anschrift.");

        string html, brandingDir;
        try
        {
            var full = Path.GetFullPath(vorlagePfad.Trim().Trim('"').Trim());
            var ext = Path.GetExtension(full);
            if (!ext.Equals(".html", StringComparison.OrdinalIgnoreCase) && !ext.Equals(".htm", StringComparison.OrdinalIgnoreCase))
                return Ersatz($"Briefkopf-Vorlage ist keine .html/.htm-Datei, wird nicht gelesen: {full} — Ersatz-Kopf.");

            var fi = new FileInfo(full);
            if (!fi.Exists)
                return Ersatz($"Briefkopf-Vorlage nicht gefunden: {full} — Ersatz-Kopf.");
            if (fi.Length > MaxVorlageBytes)
                return Ersatz($"Briefkopf-Vorlage zu groß ({fi.Length} Bytes, höchstens {MaxVorlageBytes / 1024} KB): {full} — Ersatz-Kopf.");

            html = File.ReadAllText(full, Encoding.UTF8);
            brandingDir = fi.DirectoryName!;
        }
        catch (Exception ex)
        {
            return Ersatz($"Briefkopf-Vorlage nicht lesbar: {vorlagePfad} ({ex.Message}) — Ersatz-Kopf.");
        }

        var hinweise = new List<string>();

        // Unausgeglichenes Markup zerstoert sonst die ganze Druckseite still (offener Kommentar/Rawtext-Block
        // verschluckt den Rest, ueberzaehliges </div> schliesst die .fa-group) — dann lieber Ersatz-Kopf + Hinweis.
        html = Regex.Replace(html, "<!--.*?-->", "", Opt);
        if (html.Contains("<!--", StringComparison.Ordinal))
            return Ersatz($"Briefkopf-Vorlage hat einen nicht geschlossenen Kommentar („<!--\" ohne „-->\"): {vorlagePfad} — Ersatz-Kopf.");
        html = Sanitize(html);

        var styles = new StringBuilder();
        html = Regex.Replace(html, @"<style\b[^>]*>(.*?)</style\s*>", m => { styles.AppendLine(m.Groups[1].Value); return ""; }, Opt);
        if (styles.ToString().Contains("</style", StringComparison.OrdinalIgnoreCase))
            return Ersatz($"Briefkopf-Vorlage: Style-Block mit fehlerhaftem Ende („</style …>\"): {vorlagePfad} — Ersatz-Kopf.");

        var body = Regex.Match(html, @"<body\b[^>]*>(.*)</body\s*>", Opt);
        var markup = body.Success
            ? body.Groups[1].Value
            : Regex.Replace(html, @"<head\b[^>]*>.*?</head\s*>|<!doctype[^>]*>|</?(html|body)\b[^>]*>", "", Opt);

        var offen = Regex.Match(markup, @"<(style|title|textarea|xmp|plaintext|noembed|noframes)\b", Opt);
        if (offen.Success)
            return Ersatz($"Briefkopf-Vorlage: unvollständiges Element <{offen.Groups[1].Value.ToLowerInvariant()}> (ohne End-Tag): {vorlagePfad} — Ersatz-Kopf.");
        int auf = Regex.Matches(markup, @"<div\b", Opt).Count, zu = Regex.Matches(markup, @"</div\s*>", Opt).Count;
        if (auf != zu)
            return Ersatz($"Briefkopf-Vorlage: ungleich viele <div> ({auf}) und </div> ({zu}) — würde die Druckseite verschieben: {vorlagePfad} — Ersatz-Kopf.");

        var embedder = new Embedder(brandingDir, hinweise);
        var css = embedder.EmbedCssUrls(Regex.Replace(styles.ToString(), @"@import\b[^;]*;?", "", Opt), "CSS");
        markup = embedder.EmbedCssUrls(markup, "style-Attribut");
        markup = embedder.EmbedSrcAttributes(markup);

        return new BriefkopfVorlage(false, css.Trim(), markup.Trim(), hinweise);
    }

    private static BriefkopfVorlage Ersatz(string grund) => new(true, string.Empty, string.Empty, new[] { grund });

    /// <summary>
    /// Entfernt aktive Inhalte aus der Vorlage.
    /// ponytail: schlanke Regex-Bereinigung ohne HTML-Parser (Antwort 11) — Schutz vor Versehen in einer Datei der IT
    /// (Pfad nur durch Admins setzbar), NICHT gegen boeswillig verschachteltes/verschleiertes Markup gebaut. Wer
    /// Dateizugriff auf den Server hat, braucht diesen Weg nicht. Upgrade-Pfad: HtmlSanitizer (AngleSharp), falls die
    /// Vorlage je aus Anwender-Hand kommt.
    /// </summary>
    private static string Sanitize(string html)
    {
        html = Regex.Replace(html, @"<(script|iframe|object|embed|svg|noscript|template|title|textarea|xmp|noembed|noframes)\b.*?</\1\s*>", "", Opt);
        html = Regex.Replace(html, @"</?(script|iframe|object|embed|svg|noscript|template|link|base|meta)\b[^>]*>", "", Opt);
        // Attribut-Regeln nur INNERHALB von Tags — sonst wuerde Fliesstext wie „online = 24/7" verstuemmelt.
        return Regex.Replace(html, @"<[a-z][^>]*>", tag =>
        {
            var t = Regex.Replace(tag.Value, @"\s+(on[a-z]+|srcset)\s*=\s*(""[^""]*""|'[^']*'|[^\s>]+)", "", Opt);
            return Regex.Replace(t, @"\s+(href|src|action|formaction|xlink:href)\s*=\s*(""([^""]*)""|'([^']*)'|([^\s>]+))",
                m => IsScriptUrl(m.Groups[3].Value + m.Groups[4].Value + m.Groups[5].Value) ? "" : m.Value, Opt);
        }, Opt);
    }

    private static bool IsScriptUrl(string value)
    {
        // Browser ignorieren Leer- und Steuerzeichen im Schema ("java\tscript:"), deshalb vor dem Vergleich entfernen.
        var v = new string(value.Where(c => !char.IsWhiteSpace(c) && !char.IsControl(c)).ToArray());
        return v.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase)
            || v.StartsWith("vbscript:", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Loest Verweise gegen den Branding-Ordner auf und bettet sie als Data-URI ein (je Load ein Exemplar:
    /// jede Datei wird einmal gelesen und einmal auf die Summengrenze angerechnet).</summary>
    private sealed class Embedder(string brandingDir, List<string> hinweise)
    {
        private readonly string _root = brandingDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        private readonly Dictionary<string, string> _embedded = new(StringComparer.OrdinalIgnoreCase);
        private long _summe;
        private long _ausgabe;

        public string EmbedCssUrls(string text, string ort) =>
            Regex.Replace(text, @"url\(\s*(?:""([^""]*)""|'([^']*)'|([^)\s]*))\s*\)", m =>
            {
                var data = Resolve(m.Groups[1].Value + m.Groups[2].Value + m.Groups[3].Value, ort);
                return data is null ? "none" : $"url(\"{data}\")";
            }, Opt);

        public string EmbedSrcAttributes(string markup) =>
            Regex.Replace(markup, @"<([a-z][a-z0-9]*)\b[^>]*>", tag =>
            {
                var src = Regex.Match(tag.Value, @"\ssrc\s*=\s*(""([^""]*)""|'([^']*)'|([^\s>]+))", Opt);
                if (!src.Success) return tag.Value;
                var isImg = tag.Groups[1].Value.Equals("img", StringComparison.OrdinalIgnoreCase);
                var data = Resolve(src.Groups[2].Value + src.Groups[3].Value + src.Groups[4].Value, isImg ? "Bild" : "src-Attribut");
                if (data is null)
                    return isImg ? "" : tag.Value.Remove(src.Index, src.Length); // Bild ohne Quelle: ganz weg (kein Broken-Image)
                return tag.Value.Remove(src.Index, src.Length).Insert(src.Index, $" src=\"{data}\"");
            }, Opt);

        private string? Resolve(string reference, string ort)
        {
            var r = reference.Trim();
            if (r.Length == 0) return null;

            if (r.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                var mime = r.Substring(5).Split(';', ',')[0].Trim();
                if (Mime.Values.Contains(mime, StringComparer.OrdinalIgnoreCase)) return r;
                return Reject($"{ort} abgelehnt: data:-URI mit nicht erlaubtem Typ „{mime}\".");
            }
            if (r.Contains(':') || r.StartsWith('/') || r.StartsWith('\\') || Path.IsPathRooted(r))
                return Reject($"{ort} abgelehnt (nur Dateien im Branding-Ordner, kein Schema/absoluter Pfad): {r}");

            string full;
            try { full = Path.GetFullPath(Path.Combine(_root, r)); }
            catch (Exception) { return Reject($"{ort} abgelehnt (ungültiger Pfad): {r}"); }
            if (!full.StartsWith(_root, StringComparison.OrdinalIgnoreCase))
                return Reject($"{ort} abgelehnt (außerhalb des Branding-Ordners): {r}");

            if (!Mime.TryGetValue(Path.GetExtension(full), out var type))
                return Reject($"{ort} abgelehnt (Dateityp nicht erlaubt, nur png/jpg/woff/woff2/ttf/otf): {r}");
            if (_embedded.TryGetValue(full, out var known)) return Charge(known, r, ort);

            var fi = new FileInfo(full);
            if (!fi.Exists)
                return Reject($"{ort} nicht gefunden: {r} (in {_root})");
            if (fi.Length > MaxDateiBytes)
                return Reject($"{ort} zu groß ({fi.Length} Bytes, höchstens {MaxDateiBytes / 1024 / 1024} MB): {r}");
            if (_summe + fi.Length > MaxSummeBytes)
                return Reject($"{ort} nicht eingebettet — Summe aller eingebetteten Dateien wäre über {MaxSummeBytes / 1024 / 1024} MB: {r}");

            try
            {
                var data = $"data:{type};base64,{Convert.ToBase64String(File.ReadAllBytes(full))}";
                _summe += fi.Length;
                _embedded[full] = data;
                return Charge(data, r, ort);
            }
            catch (Exception ex)
            {
                return Reject($"{ort} nicht lesbar: {r} ({ex.Message})");
            }
        }

        private string? Charge(string data, string r, string ort)
        {
            if (_ausgabe + data.Length > MaxAusgabeZeichen)
                return Reject($"{ort} nicht eingebettet — zu oft verwendet (eingebettete Daten insgesamt über {MaxAusgabeZeichen / 1024 / 1024} MB): {r}");
            _ausgabe += data.Length;
            return data;
        }

        private string? Reject(string hinweis)
        {
            hinweise.Add(hinweis);
            return null;
        }
    }
}
