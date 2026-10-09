# Beschichtungsauftrag Teil 2 — Briefkopf / CD-Druck Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Briefkopf aus einer IT-gepflegten `briefkopf.html` (Branding-Ordner) auf Seite 1 jedes Beschichtungsauftrag-Dokuments (Einzel- und Summiert-Druck, Bildschirm + PDF), mit Ersatz-Kopf bei jedem Fehler.

**Architecture:** Ein scoped `BriefkopfVorlageService` liest den Pfad aus AppSettings, laedt und bereinigt die Vorlage (schlanke Regex-Bereinigung, ponytail-Grenze), bettet Bilder/Schriften als Data-URI ein und liefert `{Styles, Markup, Hinweise, IstErsatz, Firmenname, Firmenanschrift}` — einmal je Anfrage gemerkt, kein Cache darueber hinaus. Zwei Partials (`_PrintBriefkopfStyles` im `<head>`, `_PrintBriefkopf` je `.fa-group`) holen das Ergebnis per `@inject`. Die Lade-/Bereinigungslogik ist eine statische, dateibasierte Methode (`Load`) und damit mit Temp-Ordnern unit-testbar.

**Tech Stack:** ASP.NET Core 10 MVC/Razor, System.Text.RegularExpressions, xUnit/FluentAssertions/Moq.

**Spec:** `C:\git\IDEAL-AKE-WMS\secondbrain\specs\freigegeben\2026-10-06-beschichtungsauftrag-ausbau-teil-2-spec.md`

## Global Constraints

- Direkt im Buendel (`feature/2026-08-07-ideal-teile-1-5`, Start `cc86d373`); Version **1.49.0** in beiden `AppVersion.cs`.
- Grenzen als Konstanten: Vorlage 256 KB, je Datei 1 MB, Summe 3 MB. Vorlage zu gross ⇒ Ersatz-Kopf; Ressource zu gross ⇒ nicht eingebettet (Rumpf AK 20; Ruling zu Antwort 8 in der Spec).
- Erlaubt: png/jpg/jpeg (Bilder), woff/woff2/ttf/otf (Schriften). **Kein SVG**. Kein Cache ueber Anfragen. Keine neue Abhaengigkeit.
- Vorlagen-Endung `.html`/`.htm`, sonst Ersatz. Nur relative Verweise unterhalb des Branding-Ordners; `..`, absolut, UNC, Schema, `//` abgelehnt.
- Entfernen: `script`/`iframe`/`object`/`embed`/`svg` (mit Inhalt), `link`/`base`/`meta`, `on*`, `srcset`, `javascript:`/`vbscript:` in href/src, CSS `@import`, HTML-Kommentare.
- Ersatz-Kopf = Firmenname (IDEAL Blau, fett) + Firmenanschrift einzeilig, Razor-kodiert. Hinweise nur Bildschirm (`@media print` aus) + `LogWarning` mit Pfad und Grund. Nie Ausnahme in die View.
- Empfaenger: 1 Kopfzeile → Dienstleister bzw. „Empfänger fehlt — in Sage nachpflegen"; >1 → „mehrdeutig — siehe Kopfzeilen"; 0 → „Empfänger fehlt — in Sage nachpflegen".
- Kopf nur in `FaHierarchyBeschichtung/Print.cshtml` und `PrintSummiert.cshtml`, direkt nach `<div class="fa-group">`; **keine** Kommissionierlisten. Inhalt darunter unveraendert.
- Neues AppSettings-Feld `BriefkopfVorlagePfad`, Katalog-Eintrag in Firmendaten mit Hint (Wortlaut Spec Anforderung 10).
- Muster `Branding/briefkopf.html` + `Branding/IDEAL_AKE_Logo_rgb_klein.jpg` in der Repo-Wurzel; Adresse „In der Schörihub 28, 4810 Gmunden"; keine Schriftdateien.
- „Seite X von Y": weglassen (Zaehler laeuft im Mehrgruppen-Bildschirmdruck ueber alle Dokumente, AK 14) → Aufgabe im Brain.

## Review Focus

1. Vorlage ohne `<body>`/`<html>` (nur ein Fragment) — Markup = Fragment ohne `<style>`. Test `Load_FragmentWithoutBody_UsesWholeFile`.
2. Derselbe Logo-Verweis zweimal in der Vorlage zaehlt nur einmal zur Summengrenze. Test `Load_SameFileTwice_CountsOnceAgainstSum`.
3. Verweis mit Grossbuchstaben-Endung (`LOGO.JPG`) wird eingebettet. Test `Load_UpperCaseExtension_Embedded`.
4. Bereits vorhandene `data:`-URI mit erlaubtem MIME bleibt, mit `image/svg+xml` wird entfernt. Test `Load_DataUri_AllowedKept_SvgRemoved`.
5. Unlesbare/fehlerhafte Vorlage (Ausnahme beim Lesen) ⇒ Ersatz statt Absturz. Test `Load_PathIsDirectory_FallsBackToErsatz`.

---

### Task 1: BriefkopfVorlageService (Laden, Bereinigen, Einbetten) + Einstellung

**Files:** Create `IdealAkeWms/Services/BriefkopfVorlageService.cs`; Modify `Models/AppSettingKeys.cs`, `Models/Standort/StandortSettingsCatalog.cs`, `Program.cs`; Test `IdealAkeWms.Tests/Services/BriefkopfVorlageServiceTests.cs`.

**Produces:**
```csharp
public sealed record BriefkopfVorlage(bool IstErsatz, string Styles, string Markup, IReadOnlyList<string> Hinweise)
{ public string? Firmenname { get; init; } public string? Firmenanschrift { get; init; } }
public class BriefkopfVorlageService {
  public const int MaxVorlageBytes = 256*1024, MaxDateiBytes = 1024*1024, MaxSummeBytes = 3*1024*1024;
  public BriefkopfVorlageService(IAppSettingRepository settings, ILogger<BriefkopfVorlageService> logger);
  public Task<BriefkopfVorlage> GetAsync();                 // je Anfrage einmal, loggt Hinweise als Warnung
  public static BriefkopfVorlage Load(string? vorlagePfad); // DB-frei, testbar
}
```
- [ ] Tests (rot): vorhanden (Logo + Schrift eingebettet), Pfad leer, Datei fehlt, falsche Endung (Inhalt nicht im Markup), Vorlage > 256 KB, Bild fehlt, Schrift fehlt (`url` → `none`), Script/on*/javascript:/iframe/link/base/meta/@import/srcset/svg entfernt, Ausbruch `../`, `C:\`, `\\server`, `https:`, `//`, Datei > 1 MB, Summe > 3 MB, SVG-Datei abgelehnt, Review-Focus 1–5, `GetAsync` liefert Firmenname/Anschrift + loggt.
- [ ] Implementieren, gruen, Commit `wip: …-teil-2 BriefkopfVorlageService`.

### Task 2: Partials + Einbindung in beide Drucke

**Files:** Create `Views/Shared/_PrintBriefkopf.cshtml`, `Views/Shared/_PrintBriefkopfStyles.cshtml`, `Models/ViewModels/PrintBriefkopfModel.cs` (`record PrintBriefkopfModel(string Dokument, IReadOnlyList<FaHierarchyOrderInfo> OrderInfos)` mit `Empfaenger`-Property); Modify `Views/FaHierarchyBeschichtung/Print.cshtml`, `PrintSummiert.cshtml`; Test `IdealAkeWms.Tests/Views/BriefkopfEinbindungTests.cs` (Quelltext-Waechter: beide Beschichtung-Drucke binden beide Partials ein, Kommissionier-Drucke nicht; Empfaenger-Logik als Unit-Test).
- [ ] frontend-design-Skill; Tests rot; Partials + Einbindung; Build (Razor) + Tests gruen; Commit.

### Task 3: Muster-Vorlage, Doku, Version

- `Branding/briefkopf.html` (CD-Satzspiegel, alle Regeln unter `.briefkopf`, Kommentar mit IT-Regeln, `@font-face` mit Platzhalter-Dateinamen), `Branding/IDEAL_AKE_Logo_rgb_klein.jpg`; Test: Muster laedt ohne Hinweise ausser fehlender Schrift (Muster-Waechter).
- README (AppSettings-Zeile + Abschnitt „Briefkopf-Vorlage"), Hilfe, Changelog-Karte, 1.49.0, TS-82 (LF pruefen), Commit `wip: …-teil-2 feature-complete`.
- Brain: testszenarien-index, Changelog, feature-map, Aufgabe „Seitenzaehlung", Spec (Ruling, Nachweis), qa-agent synchron.
