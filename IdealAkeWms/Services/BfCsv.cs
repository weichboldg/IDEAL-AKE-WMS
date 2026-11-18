using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using IdealAkeWms.Models;

namespace IdealAkeWms.Services;

/// <summary>Kopfwerte der WiCAM-CSV aus den ServiceSettings (Defaults = Spec-Vorlage).</summary>
public sealed record BfCsvKopf(string CreateNewOrder = "YES", int Priority = 1, string Release = "YES",
    string Remark1 = "*HEADER_LINE_17", string Remark2 = "*HEADER_LINE_18", string Remark3 = "*HEADER_LINE_19");

/// <summary>
/// v1.99.35 (Spec 2026-11-13-blechfertigung-bf-auftraege-wicam-csv): gemeinsame Keys und Regeln der BF-Auftraege fuer Web (Freigabe)
/// und Dienst (CSV, Mail), Muster <see cref="WareneingangCsv"/>. Trenner, Kodierung und Datumsformate sind fest (Freigabe-Antwort 9).
/// <para>v1.99.42: Aufbau exakt nach der echten WiCAM-Datei <c>BF00000002.csv</c> (Befund P-1 (2), Walter 2026-11-18 „Zielform“),
/// Testvorlage <c>IDEALAKEWMSService.Tests/TestData/BF00000002.csv</c> (Byte-Vergleich).</para>
/// </summary>
public static class BfCsv
{
    public const string AktivKey = "BfCsvAktiv";
    public const string PfadKey = "BfCsvPfad";
    public const string MaxWiederholungenKey = "Sync:BfCsvMaxWiederholungen";
    public const string WiederholAbstandKey = "Sync:BfCsvWiederholAbstandMinuten";
    public const string CreateNewOrderKey = "BfCsvCreateNewOrder";
    public const string OrderPriorityKey = "BfCsvOrderPriority";
    public const string OrderReleaseKey = "BfCsvOrderRelease";
    public const string Remark1Key = "BfCsvRemark1";
    public const string Remark2Key = "BfCsvRemark2";
    public const string Remark3Key = "BfCsvRemark3";

    /// <summary>ServiceSetting (Default aus): Dienst sendet die Bestell-Mail je abgeschicktem BF-Auftrag.</summary>
    public const string EmailEnabledKey = "Sync:BfAuftragEmailEnabled";

    /// <summary>Audit-Kennungen der Dienst-Schritte.</summary>
    public const string ServiceActor = "system:bf-csv";
    public const string MailActor = "system:bf-mail";

    /// <summary>Namenskollision im Pool: nie automatisch wiederholen (nur „CSV erneut schreiben“ nach Klaerung in WiCAM).</summary>
    public const string KollisionText = "Datei existiert bereits im Pool – nicht überschrieben.";

    /// <summary>Kopf-Remarks: ohne <c>;</c>, <c>"</c>, Zeilenumbruch, hoechstens 100 Zeichen.</summary>
    public const string RemarkMuster = @"^[^;""\r\n]{0,100}$";
    public const string RemarkMusterText = "höchstens 100 Zeichen, ohne ; \" und Zeilenumbruch";
    public static readonly (string Wert, string Text)[] JaNein = { ("YES", "YES"), ("NO", "NO") };

    /// <summary>Windows-1252, das nie still <c>?</c> einsetzt. Der Provider wird hier registriert (idempotent), nicht in einem statischen
    /// Konstruktor: Feldinitialisierer liefen sonst vor der Registrierung.</summary>
    public static Encoding Kodierung()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        return Encoding.GetEncoding(1252, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
    }

    /// <summary>Excel <c>TRIM(CLEAN(UPPER(x)))</c>: Steuerzeichen &lt; 32 weg, Leerzeichenfolgen auf eines, trimmen, gross.</summary>
    public static string MatchcodeBereinigen(string? s) =>
        Regex.Replace(new string((s ?? "").Where(c => c >= 32).ToArray()), " {2,}", " ").Trim().ToUpperInvariant();

    /// <summary>Bemerkung je Position (Freigabe-Antworten 3/4): <c>[Status - ][FertigungsInfoPN - ]dd.MM(Bezugstag)[ - Position bei BG]</c>.</summary>
    public static string Bemerkung(string? status, string? fertigungsInfo, DateTime bezugstag, string? teileart, int position)
    {
        var sb = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(status)) sb.Append(status.Trim()).Append(" - ");
        if (!string.IsNullOrWhiteSpace(fertigungsInfo)) sb.Append(fertigungsInfo.Trim()).Append(" - ");
        sb.Append(bezugstag.ToString("dd.MM", CultureInfo.InvariantCulture));
        if (string.Equals(teileart?.Trim(), "BG", StringComparison.OrdinalIgnoreCase)) sb.Append(" - ").Append(position.ToString(CultureInfo.InvariantCulture));
        return sb.ToString();
    }

    /// <summary>Melden statt still ersetzen: Beschreibung des ersten unzulaessigen Zeichens (<c>;</c>, <c>"</c>, Zeilenumbruch, nicht in
    /// Windows-1252), sonst null. Seit v1.99.42 wieder ohne <c>;</c> - die Vorlage fasst nichts ein; der Lagerplatz geht vorher durch
    /// <see cref="Lagerplatz"/>.</summary>
    public static string? UnzulaessigesZeichen(string? wert)
    {
        if (string.IsNullOrEmpty(wert)) return null;
        if (wert.Contains(';')) return "Semikolon";
        if (wert.Contains('"')) return "Anführungszeichen";
        if (wert.IndexOfAny(new[] { '\r', '\n' }) >= 0) return "Zeilenumbruch";
        var enc = Kodierung();
        foreach (var c in wert.EnumerateRunes())
        {
            try { enc.GetByteCount(c.ToString()); }
            catch (EncoderFallbackException) { return $"Zeichen „{c}“ (nicht Windows-1252)"; }
        }
        return null;
    }

    /// <summary>ORDER_POSITION: voller Sage-Lagerplatz, <c>;</c> → <c>,</c> (<c>K-02;0;0;0</c> → <c>K-02,0,0,0</c>, Walter 2026-11-18).</summary>
    public static string Lagerplatz(string? lp) => (lp ?? "").Trim().Replace(';', ',');

    /// <summary>ORDER_REMARK_2: Hauptlagerplatz = Teil vor dem ersten Trenner (<c>K-02;0;0;0</c> → <c>K-02</c>).</summary>
    public static string Hauptlagerplatz(string? lp) => Lagerplatz(lp).Split(',')[0].Trim();

    /// <summary>ORDER_MATERIAL_NO ganzzahlig (<c>1</c>, auch aus <c>1.000</c>/<c>1,0</c>); nicht ganzzahlig/leer = null (Freigabe-Sperre).</summary>
    public static string? MaterialNr(string? s) =>
        decimal.TryParse((s ?? "").Trim().Replace(',', '.'), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var d)
        && d == decimal.Truncate(d) ? d.ToString("0", CultureInfo.InvariantCulture) : null;

    /// <summary>Kopfwerte aus den ServiceSettings (leer bzw. nicht lesbar = Default), gemeinsam fuer Dienst und Test-CSV-Download.</summary>
    public static async Task<BfCsvKopf> KopfAsync(Func<string, Task<string?>> lesen)
    {
        var d = new BfCsvKopf();
        async Task<string> S(string key, string fallback) => await lesen(key) is { } v && v.Trim().Length > 0 ? v.Trim() : fallback;
        var prio = int.TryParse(await lesen(OrderPriorityKey), NumberStyles.Integer, CultureInfo.InvariantCulture, out var p) ? p : d.Priority;
        return new BfCsvKopf(await S(CreateNewOrderKey, d.CreateNewOrder), prio, await S(OrderReleaseKey, d.Release),
            await S(Remark1Key, d.Remark1), await S(Remark2Key, d.Remark2), await S(Remark3Key, d.Remark3));
    }

    /// <summary>Menge ganzzahlig ohne Nachkommastellen, sonst mit Dezimalkomma (de-AT).</summary>
    public static string Menge(decimal m) =>
        m == decimal.Truncate(m) ? decimal.Truncate(m).ToString("0", CultureInfo.InvariantCulture) : m.ToString("0.######", CultureInfo.GetCultureInfo("de-AT"));

    /// <summary>Feste Feldanzahl je Zeile wie <c>BF00000002.csv</c> (12 <c>;</c>, auch Leerzeilen).</summary>
    public const int Spalten = 13;

    /// <summary>Dateiinhalt exakt wie <c>BF00000002.csv</c>: CRLF, jede Zeile <see cref="Spalten"/> Felder; Kopf Schluessel in Spalte B/Wert C,
    /// PART_START/PART_END in A; Z10 leer, Z11 die 10 ORDER_*-Spalten, Z12 leer, Z13 deutsche Ueberschrift, ab Z14 die uebergebenen
    /// Positionen (<see cref="BfAuftragPosition.Uebergeben"/>) aufsteigend; keine Auffuellzeilen, keine Anfuehrungszeichen.</summary>
    public static string Inhalt(BfAuftrag a, BfCsvKopf kopf)
    {
        var sb = new StringBuilder();
        void Z(params string[] f) => sb.Append(string.Join(';', f.Concat(Enumerable.Repeat("", Spalten - f.Length)))).Append("\r\n");
        Z("PART_START");
        Z("", "CREATE_NEW_ORDER", kopf.CreateNewOrder);
        Z("", "ORDER_NUMBER", a.Nummer);
        Z("", "ORDER_REMARK_3", kopf.Remark3);
        Z("", "ORDER_REMARK_1", kopf.Remark1);
        Z("", "ORDER_REMARK_2", kopf.Remark2);
        Z("", "ORDER_PRIORITY", kopf.Priority.ToString(CultureInfo.InvariantCulture));
        Z("", "ORDER_RELEASE", kopf.Release);
        Z("PART_END");
        Z();
        Z("ORDER_NC_FILE", "ORDER_MATERIAL_NO", "ORDER_AMOUNT", "ORDER_REMARK", "ORDER_POSITION", "ORDER_REMARK_2", "ORDER_REMARK_4",
            "ORDER_DATE", "ORDER_REMARK_5", "ORDER_REMARK_1");
        Z();
        Z("Matchcode", "Mat.Nr.", "Menge", "Bemerkung", "Lagerplatz", "Hauptlagerplatz", "Besteller", "Schachteltermin", "FE-Termin", "Arbeitsschritte");
        var schachtel = a.Schachteltermin?.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) ?? "";
        var fe = a.FeTermin?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "";
        foreach (var p in a.Positionen.Where(p => p.Uebergeben).OrderBy(p => p.Position))
            Z(MatchcodeBereinigen(p.Matchcode), MaterialNr(p.Materialnummer) ?? p.Materialnummer!.Trim(), Menge(p.Menge), p.Bemerkung ?? "",
                Lagerplatz(p.Hauptlagerplatz), Hauptlagerplatz(p.Hauptlagerplatz), a.BestellerKuerzel ?? "", schachtel, fe, p.Arbeitsschritte?.Trim() ?? "");
        return sb.ToString();
    }
}
