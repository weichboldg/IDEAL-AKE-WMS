using System.Globalization;
using System.Text;
using static System.Net.WebUtility;

namespace IDEALAKEWMSService.Services;

/// <summary>Fertige Mail: Betreff, HTML (Logo per <c>cid:</c><see cref="LieferverzugMailHtml.LogoCid"/>) und Text-Teil ohne Markup.</summary>
public sealed record LieferverzugMailInhalt(string Betreff, string Html, string Text);

/// <summary>
/// v1.99.10 (Spec 2026-11-12-offene-bestellungen-mahnmail-lieferverzug, Entwurf 3, AK 14/15): Kreditor- und Einkauf-Mail als reine Funktion.
/// Tabellenbasierte Huelle wie <c>NotificationWorker.BuildMeldebestandHtml</c>; CI: Hausfarbe des gewaehlten Standorts, Fliesstext #4a494a,
/// Arial, Fusszeile mit Firmenadresse (Quelle Branding/briefkopf.html) und www.ideal-ake.at. Kontrast (WCAG AA): auf AKE-Blau #43a6e2 nur
/// dunkle Schrift #053153 (≈5:1), nie Weiss (≈2,7:1); Testbetrieb-Banner #ef7d00 mit schwarzer Schrift (≈7:1), Text ausgeschrieben.
/// Alle Werte gehen durch HtmlEncode. Text-Teil: nackte URL (fallstricke „Mails immer multipart/alternative“).
/// </summary>
public static class LieferverzugMailHtml
{
    public const string LogoCid = "logo-ideal-ake";
    private const string Fliesstext = "#4a494a";
    private const string IdealBlau = "#053153";
    private const string AkeBlau = "#43a6e2";
    private const string TestOrange = "#ef7d00";
    private static readonly CultureInfo De = CultureInfo.GetCultureInfo("de-AT");

    private sealed record Firma(string Name, string Adresse, string Telefon, string Band, string BandText);

    // Branding/briefkopf.html:63-75 (Fusszeile der Druck-Vorlage)
    private static readonly Firma Ake = new("AKE Ausseer Kälte- und Edelstahltechnik GmbH", "Pichl 66, 8984 Bad Mitterndorf, AT", "+43 3624 211 00", AkeBlau, IdealBlau);
    private static readonly Firma Ideal = new("IDEAL Kältetechnik GmbH", "In der Schörihub 28, 4810 Gmunden, AT", "+43 7612 660 61", IdealBlau, "#ffffff");
    private static Firma FirmaVon(string standort) => string.Equals(standort, "IDEAL", StringComparison.Ordinal) ? Ideal : Ake;

    public static string FirmenName(string standort) => FirmaVon(standort).Name;

    /// <summary>Betreff-Praefix im Testbetrieb (F4); fehlt die Adresse: „(keine Adresse)“.</summary>
    public static string TestbetriebText(string name, string? adresse) =>
        $"TESTBETRIEB – wäre an {name}, {(string.IsNullOrWhiteSpace(adresse) ? "(keine Adresse)" : adresse)} gegangen";

    /// <summary>F3 / Freigabe-Antworten 6 und 11: eine Mail Deutsch und Englisch; Mengen in der Basiseinheit, bei abweichender Einheit
    /// „siehe Bestellung“. Keine internen Werte (BelPosId, Lagerplatz, WMS-Erfassung).</summary>
    public static LieferverzugMailInhalt Kreditor(LieferverzugGruppe g, IReadOnlyList<LieferverzugPosition> positionen, string standort, bool testbetrieb)
    {
        var f = FirmaVon(standort);
        var betreff = $"Erinnerung: Lieferverzug / Reminder: overdue delivery – {f.Name}";
        string? banner = testbetrieb ? TestbetriebText(g.Name, g.Adresse) : null;
        if (banner != null) betreff = $"{banner} – {betreff}";

        var html = new StringBuilder();
        Kopf(html, f, "Erinnerung: Lieferverzug · Reminder: overdue delivery", banner);
        Absatz(html, "Guten Tag,");
        Absatz(html, "zu den folgenden Bestellpositionen ist der Liefertermin überschritten. Bitte liefern Sie die offene Menge oder nennen Sie "
            + "uns per Antwort auf diese Mail einen neuen Liefertermin.");
        Absatz(html, "Good day, the delivery date for the following order items has passed. Please deliver the open quantity or reply to this "
            + "e-mail with a new delivery date.", kursiv: true);
        TabelleStart(html, f, "Bestellung<br>Order", "Artikel<br>Item", "Bezeichnung<br>Description", "Bestellt<br>Ordered", "Offen<br>Open",
            "Liefertermin<br>Delivery date");
        var i = 0;
        foreach (var p in positionen)
        {
            var l = p.Line;
            var (bestellt, offen) = l.EinheitAbweichend
                ? ("siehe Bestellung / see order", "siehe Bestellung / see order")
                : (Menge(l.MengeBestellt, l.MengeBasiseinheit), Menge(p.Rest, l.MengeBasiseinheit));
            Zeile(html, i++, l.Belegnummer, l.Artikelnummer, Bezeichnung(l), bestellt, offen, Datum(l.Liefertermin));
        }
        TabelleEnde(html);
        Absatz(html, "Vielen Dank. · Thank you.");
        Fuss(html, f, null);

        var text = new StringBuilder();
        if (banner != null) text.AppendLine(banner).AppendLine();
        text.AppendLine("Guten Tag,").AppendLine()
            .AppendLine("zu den folgenden Bestellpositionen ist der Liefertermin überschritten. Bitte liefern Sie die offene Menge oder nennen Sie uns per Antwort auf diese Mail einen neuen Liefertermin.")
            .AppendLine()
            .AppendLine("Good day, the delivery date for the following order items has passed. Please deliver the open quantity or reply to this e-mail with a new delivery date.")
            .AppendLine();
        foreach (var p in positionen)
        {
            var l = p.Line;
            var mengen = l.EinheitAbweichend ? "Menge siehe Bestellung / quantity see order"
                : $"bestellt/ordered {Menge(l.MengeBestellt, l.MengeBasiseinheit)}, offen/open {Menge(p.Rest, l.MengeBasiseinheit)}";
            text.AppendLine($"- {l.Belegnummer} | {l.Artikelnummer} | {Bezeichnung(l)} | {mengen} | Liefertermin/delivery date {Datum(l.Liefertermin)}");
        }
        text.AppendLine().AppendLine("Vielen Dank. / Thank you.");
        TextFuss(text, f, null);
        return new(betreff, html.ToString(), text.ToString());
    }

    /// <summary>F2: Sammelliste an den Einkauf, gruppiert nach Kreditor mit Statuszeile; F5 Hinweis bei veraltetem Stand.</summary>
    public static LieferverzugMailInhalt Einkauf(LieferverzugPlan plan, string standort, DateTime? stand, string? appBaseUrl, DateTime heute, int tageNachTermin)
    {
        var f = FirmaVon(standort);
        var betreff = $"Lieferverzug: {plan.AnzahlFaellig} Bestellposition(en) bei {plan.Gruppen.Count} Lieferant(en) – {heute:dd.MM.yyyy}";
        var link = string.IsNullOrWhiteSpace(appBaseUrl) ? null : appBaseUrl.TrimEnd('/') + "/PurchaseOrders?nurUeberfaellig=true";
        var hinweis = plan.StandAlt
            ? $"Bestelldaten von {(stand is { } s ? s.ToString("dd.MM.yyyy HH:mm", De) : "(Cache leer)")} – Kreditor-Mails ausgesetzt."
            : null;

        var html = new StringBuilder();
        Kopf(html, f, "Lieferverzug offener Bestellungen", null);
        if (hinweis != null)
            html.Append($"<tr><td style=\"padding:16px 32px 0;\"><p style=\"margin:0;padding:10px 12px;background:#fff3cd;border:1px solid #8a6d00;color:#000000;font-size:14px;\"><strong>Hinweis:</strong> {HtmlEncode(hinweis)}</p></td></tr>");
        Absatz(html, $"Folgende Bestellpositionen sind {tageNachTermin} oder mehr Tage nach dem Liefertermin noch offen (Rest wie in „Wareneingang zu Bestellung“). Stand der Bestelldaten: {(stand is { } st ? st.ToString("dd.MM.yyyy HH:mm", De) : "–")}.");
        foreach (var g in plan.Gruppen)
        {
            html.Append($"<tr><td style=\"padding:8px 32px 4px;\"><p style=\"margin:0;font-size:15px;font-weight:bold;color:{IdealBlau};\">{HtmlEncode(g.Name)}{(g.Kto != null ? $" ({HtmlEncode(g.Kto)})" : "")}</p>"
                + $"<p style=\"margin:2px 0 0;font-size:13px;color:{Fliesstext};\">Kreditor-Mail: {HtmlEncode(g.Status)}</p></td></tr>");
            TabelleStart(html, f, "Bestellung", "Artikel", "Bezeichnung", "Bestellt", "Geliefert", "Rest", "Liefertermin", "Tage überfällig");
            var i = 0;
            foreach (var p in g.Positionen)
            {
                var l = p.Line;
                var einheit = l.EinheitAbweichend ? $"{l.MengeBasiseinheit} (Basis, Einheit abweichend)" : l.MengeBasiseinheit;
                Zeile(html, i++, l.Belegnummer, l.Artikelnummer, Bezeichnung(l), Menge(l.MengeBestellt, einheit), Geliefert(l, einheit),
                    Menge(p.Rest, einheit), Datum(l.Liefertermin), p.TageUeberfaellig.ToString(De));
            }
            TabelleEnde(html);
        }
        if (link != null)
            html.Append($"<tr><td style=\"padding:0 32px 24px;\"><a href=\"{HtmlEncode(link)}\" style=\"display:inline-block;background:{IdealBlau};color:#ffffff;padding:10px 20px;border-radius:4px;text-decoration:none;font-size:14px;font-weight:bold;\">Zu den offenen Bestellungen</a></td></tr>");
        Fuss(html, f, "Diese Nachricht wurde automatisch vom IDEAL AKE WMS Service erzeugt.");

        var text = new StringBuilder();
        if (hinweis != null) text.AppendLine("HINWEIS: " + hinweis).AppendLine();
        text.AppendLine($"Lieferverzug offener Bestellungen – Stand der Bestelldaten: {(stand is { } st2 ? st2.ToString("dd.MM.yyyy HH:mm", De) : "–")}").AppendLine();
        foreach (var g in plan.Gruppen)
        {
            text.AppendLine($"{g.Name}{(g.Kto != null ? $" ({g.Kto})" : "")} – Kreditor-Mail: {g.Status}");
            foreach (var p in g.Positionen)
            {
                var l = p.Line;
                text.AppendLine($"  - {l.Belegnummer} | {l.Artikelnummer} | {Bezeichnung(l)} | bestellt {Menge(l.MengeBestellt, l.MengeBasiseinheit)}, "
                    + $"geliefert {Geliefert(l, l.MengeBasiseinheit)}, Rest {Menge(p.Rest, l.MengeBasiseinheit)}{(l.EinheitAbweichend ? " (Basis, Einheit abweichend)" : "")} | "
                    + $"Liefertermin {Datum(l.Liefertermin)} | {p.TageUeberfaellig} Tage überfällig");
            }
            text.AppendLine();
        }
        if (link != null) text.AppendLine("Zu den offenen Bestellungen: " + link).AppendLine();
        TextFuss(text, f, "Diese Nachricht wurde automatisch vom IDEAL AKE WMS Service erzeugt.");
        return new(betreff, html.ToString(), text.ToString());
    }

    private static string Menge(decimal m, string? einheit) => $"{m.ToString("0.###", De)} {einheit}".Trim();
    /// <summary>v1.99.15 (Teil 3, F5): Sage liefert fuer Rahmenabrufe keine gelieferte Menge je Abruf → „–“, nie „0“.</summary>
    private static string Geliefert(IdealAkeWms.Models.SagePurchaseOrderLine l, string? einheit) => l.IstRahmenabruf ? "–" : Menge(l.MengeGeliefert, einheit);
    private static string Datum(DateTime? d) => d?.ToString("dd.MM.yyyy", De) ?? "–";
    private static string Bezeichnung(IdealAkeWms.Models.SagePurchaseOrderLine l) =>
        string.Join(" ", new[] { l.Bezeichnung1, l.Bezeichnung2 }.Where(s => !string.IsNullOrWhiteSpace(s)));

    private static void Kopf(StringBuilder sb, Firma f, string titel, string? banner)
    {
        sb.Append("<!DOCTYPE html><html lang=\"de\"><head><meta charset=\"utf-8\" /><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\" />"
            + $"<title>{HtmlEncode(titel)}</title></head>"
            + $"<body style=\"margin:0;padding:0;font-family:Arial,Helvetica,sans-serif;background:#f4f6f9;color:{Fliesstext};\">"
            + "<table width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"background:#f4f6f9;padding:20px 0;\"><tr><td align=\"center\">"
            + "<table width=\"680\" cellpadding=\"0\" cellspacing=\"0\" style=\"max-width:680px;background:#ffffff;\">");
        if (banner != null)
            sb.Append($"<tr><td style=\"background:{TestOrange};padding:12px 32px;color:#000000;font-size:15px;font-weight:bold;\">{HtmlEncode(banner)}</td></tr>");
        sb.Append($"<tr><td style=\"padding:20px 32px 12px;\"><img src=\"cid:{LogoCid}\" alt=\"IDEAL AKE\" height=\"48\" style=\"display:block;height:48px;border:0;\" /></td></tr>");
        sb.Append($"<tr><td style=\"background:{f.Band};padding:14px 32px;\"><h1 style=\"margin:0;color:{f.BandText};font-size:19px;font-weight:bold;\">{HtmlEncode(titel)}</h1></td></tr>");
    }

    private static void Absatz(StringBuilder sb, string text, bool kursiv = false) =>
        sb.Append($"<tr><td style=\"padding:16px 32px 0;\"><p style=\"margin:0;color:{Fliesstext};font-size:14px;line-height:1.5;{(kursiv ? "font-style:italic;" : "")}\">{HtmlEncode(text)}</p></td></tr>");

    /// <summary>Kopfzellen duerfen &lt;br&gt; enthalten (feste Texte, kein Eingabewert).</summary>
    private static void TabelleStart(StringBuilder sb, Firma f, params string[] koepfe)
    {
        sb.Append("<tr><td style=\"padding:12px 32px 16px;\"><table width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"border-collapse:collapse;font-size:13px;\"><thead>"
            + $"<tr style=\"background:{IdealBlau};color:#ffffff;\">");
        foreach (var k in koepfe) sb.Append($"<th style=\"padding:8px 8px;text-align:left;font-weight:bold;\">{k}</th>");
        sb.Append("</tr></thead><tbody>");
    }

    private static void Zeile(StringBuilder sb, int i, params string[] zellen)
    {
        sb.Append($"<tr style=\"background:{(i % 2 == 0 ? "#ffffff" : "#f4f6f9")};border-bottom:1px solid #dddddd;\">");
        foreach (var z in zellen) sb.Append($"<td style=\"padding:8px 8px;color:{Fliesstext};vertical-align:top;\">{HtmlEncode(z)}</td>");
        sb.Append("</tr>");
    }

    private static void TabelleEnde(StringBuilder sb) => sb.Append("</tbody></table></td></tr>");

    private static void Fuss(StringBuilder sb, Firma f, string? zusatz)
    {
        sb.Append($"<tr><td style=\"padding:16px 32px 20px;border-top:4px solid {f.Band};\"><p style=\"margin:0;font-size:12px;line-height:1.5;color:{Fliesstext};\">"
            + $"<strong style=\"color:{IdealBlau};\">{HtmlEncode(f.Name)}</strong><br />{HtmlEncode(f.Adresse)}<br />{HtmlEncode(f.Telefon)} · office@ideal-ake.at<br />"
            + $"<a href=\"https://www.ideal-ake.at\" style=\"color:{IdealBlau};\">www.ideal-ake.at</a>"
            + (zusatz != null ? $"<br /><br />{HtmlEncode(zusatz)}" : "")
            + "</p></td></tr></table></td></tr></table></body></html>");
    }

    private static void TextFuss(StringBuilder sb, Firma f, string? zusatz)
    {
        sb.AppendLine("--").AppendLine(f.Name).AppendLine(f.Adresse).AppendLine($"{f.Telefon} · office@ideal-ake.at").AppendLine("https://www.ideal-ake.at");
        if (zusatz != null) sb.AppendLine().AppendLine(zusatz);
    }
}
