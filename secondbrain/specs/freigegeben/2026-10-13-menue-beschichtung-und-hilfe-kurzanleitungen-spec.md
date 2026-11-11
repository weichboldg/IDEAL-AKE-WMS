---
type: spec
title: "Menüpunkt „Beschichtung“ bündeln + Kurzanleitungen in der App-Hilfe"
slug: 2026-10-13-menue-beschichtung-und-hilfe-kurzanleitungen-spec
status: Testbereit
created: 2026-10-13
updated: 2026-10-13
source_backlog: "[[2026-10-13-menue-beschichtung-und-hilfe-kurzanleitungen]]"
depends_on: ""
task: "[[2026-10-13-menue-beschichtung-und-hilfe-kurzanleitungen-umsetzung]]"
worktree: ""
branch: ""
affected_code:
  - "IdealAkeWms/Views/Shared/_Layout.cshtml Z.112-125 — zwei Top-Level-Eintraege (Beschichtungsauftrag Z.116-119, Beschichtungsaufträge Z.121-124) unter EINER Bedingung Z.114 werden EIN Dropdown „Beschichtung“; Muster Dropdown „Bestellungen“ Z.212-236 bzw. „Fertigungsaufträge“ Z.138-159; Bedingungs-Variablen Z.58-61 bleiben unveraendert"
  - "IdealAkeWms/Views/Help/Index.cshtml — neue Karte „Beschichtung (IDEAL)“ mit Kurzanleitung; Abschnitte Z.489-514 (v1.48), Z.516-535 (v1.49), Z.557-585 (v1.51) werden in diese Karte ueberfuehrt (Antwort 3 vorausgesetzt); Z.639-656 (v1.52 Kuerzel) und Z.222-247 (v1.53 CSV) erweitert"
  - "IdealAkeWms/Views/FaHierarchyBeschichtung/Index.cshtml Z.5 + Z.15 und Summiert.cshtml Z.5 + Z.14 — Seitentitel nur falls Rueckfrage 2 = ja (Druck-Views unveraendert)"
  - "IdealAkeWms.Tests/Views/BeschichtungsauftraegeViewTests.cs — neuer Quelltext-Waechter fuer das Dropdown (Muster derselben Klasse, Z.18-26)"
  - "IdealAkeWms/AppVersion.cs + IDEALAKEWMSService/AppVersion.cs (naechste freie Version, heute 1.54.0 -> AppVersion.cs:5), IdealAkeWms/Views/Help/Changelog.cshtml"
  - "docs/TESTSZENARIEN.md — neues Kapitel (naechste freie TS-Nummer, heute TS-88) + Menue-Wortlaut in TS-81.2 (Z.9298), TS-81.13 (Z.9334-9336), TS-84.14 (Z.9514-9518) nachziehen; secondbrain/tests/testszenarien-index.md"
open_questions: []
epic: false
etappen: []
deploy:
  web: true
  service: true
  migration: false
freigabe_entscheidung: "Freigegeben wie Antworten 1-4 inkl. Nachtrag: Dropdown Beschichtung mit zwei Eintraegen (Vorschlagsliste, Auftraege), Seitentitel Vorschlagsliste Beschichtung (IDEAL), neue Hilfe-Karte Beschichtung (IDEAL), keine Screenshots. Umsetzung direkt im Buendel vor FA-Struktur Teil 2."
freigabe_von: "Gerald Weichbold"
freigabe_am: 2026-10-13
# Flache Schluessel mit Absicht: Obsidians Property-Editor kann verschachtelte
# YAML-Objekte NICHT bearbeiten - und genau diesen Block fuellt der Mensch aus.
---

> **Umsetzungsort:** direkt im Bündel-Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5` auf Zweig
> `feature/2026-08-07-ideal-teile-1-5` (kein eigener Worktree, ein Lauf zur Zeit). `worktree`/`branch` im Frontmatter
> bleiben leer — der Ort steht hier fest. Code-Belege dieser Spec: Bündel-Stand v1.54.0 (`IdealAkeWms/AppVersion.cs:5`),
> HEAD laut Auftrag `d56a340e`; alle Zeilen am 2026-10-13 nachgelesen. **Version und TS-Nummer:** die zum
> Umsetzungszeitpunkt nächste freie im Bündel (heute wären es 1.55.0 und TS-88 — vor dem Start neu messen, weil
> parallele Specs im Bündel dieselben Nummern beanspruchen können).

## Ziel / Nutzen (das Warum)

Seit Beschichtungsauftrag Teil 4 (v1.51.0) gibt es für ein Thema zwei Top-Level-Menüpunkte mit fast gleichem Namen
(„Beschichtungsauftrag“ = Vorschlagsliste, „Beschichtungsaufträge“ = gespeicherte Aufträge, `_Layout.cshtml:116-124`).
Das ist verwechselbar und verbraucht Platz in der Navigationsleiste. Ein Dropdown „Beschichtung“ (Name entschieden,
Gerald 2026-10-13) bündelt beides nach dem Muster „Bestellungen“.

Die Hilfe zu Beschichtung, Arbeitsgang-Kürzeln und CSV-Export ist in v1.48–v1.53 als Änderungsnotizen je Version
entstanden („seit v1.xx“). Wer die Funktion zum ersten Mal bedient, braucht eine Schritt-für-Schritt-Kurzanleitung
(Gerald 2026-10-13: ausführliches Dokument macht die Koordination, Kurzfassung in die App-Hilfe).

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**
1. `_Layout.cshtml`: zwei Einträge → ein Dropdown „Beschichtung“ mit Einträgen laut Rückfrage 1, Active-Markierung
   für beide Controller.
2. `Help/Index.cshtml`: Kurzanleitungen (a) Beschichtungsauftrag, (b) Arbeitsgang-Kürzel im FA-Baum, (c) CSV-Export;
   vorhandene Abschnitte zusammenführen statt doppeln (Plan unten).
3. Seitentitel der Vorschlagsliste — nur falls Rückfrage 2 = ja.
4. Quelltext-Wächter für das Dropdown, Version, Anwender-Changelog, TS-Kapitel, Menü-Wortlaut in TS-81/TS-84.

**Out-of-Scope**
- Rollen, Filter, Feature-Toggle, Routen, Controller — unverändert.
- Druck-/PDF-Views (`Print*.cshtml`) und deren Dokumenttitel.
- Inhaltsverzeichnis/Anker für die Hilfeseite: gibt es heute nicht (`Help/Index.cshtml` hat keine `id=`/`href="#"`,
  Aufbau = Karten in `col-lg-8`, Z.7-8) — wird nicht eingeführt.
- Hervorheben des aktiven Eintrags *im* Dropdown: kein bestehendes Dropdown macht das (Z.141, Z.180, Z.215 markieren
  nur den Toggle) — Muster beibehalten.
- Historische Einträge in `Help/Changelog.cshtml` (Z.78, Z.82, Z.138 nennen die alten Menünamen) bleiben —
  sie beschreiben den damaligen Stand.
- Andere Hilfe-Abschnitte, z. B. „Arbeitsgang-Stammdaten (seit v1.54.0)“ (Z.537-555, steht inhaltsfremd in der
  FA-Karte): nicht verschieben, ist nicht Teil dieses Auftrags.

## Fachliche Anforderungen

### F1 — Dropdown „Beschichtung“

**Heute (belegt):**
- `_Layout.cshtml:114` `@if (hasBeschichtungsauftragAccess && faBeschichtungAktiv)` umschließt **beide** Einträge
  (Kommentar Z.120: „dieselbe Bedingung (Lesen + Toggle)“).
- `faBeschichtungAktiv` = AppSetting `FaHierarchyBeschichtungAktiv` exakt `"true"` (`_Layout.cshtml:58-60`);
  `hasBeschichtungsauftragAccess` = `HasBeschichtungsauftragAccessAsync()` (`_Layout.cshtml:61`) =
  Rollen `admin` ODER `beschichtungsauftrag` ODER `beschichtungsauftrag_schreiben` (`Services/CurrentUserService.cs:137-138`,
  Test `CurrentUserServiceRoleTests.cs:457-469` „die Schreibrolle schliesst die Leseebene ein“).
- Beide Controller tragen dieselben Class-Level-Filter `[RequireBeschichtungsauftragAccess]` +
  `[RequireFaHierarchyBeschichtungAktiv]` (`Controllers/FaHierarchyBeschichtungController.cs:28-29`,
  `Controllers/BeschichtungsauftraegeController.cs:28-29`). Die Schreibrolle wirkt nur auf Action-Ebene
  (`[RequireBeschichtungsauftragWriteAccess]` an `Create`, `Bearbeiten`, `PositionenHinzufuegen`, `Bemerkung`, `Status`,
  `Reklamation` — `BeschichtungsauftraegeController.cs:141-234`), keine Listen-Action ist schreibgeschützt.
- Vorhandene GET-Einstiege: `FaHierarchyBeschichtung/Index` (Einzelpositionen, Z.62), `FaHierarchyBeschichtung/Summiert`
  (Standard seit v1.48.0, Z.152), `Beschichtungsauftraege/Index` (Z.72). Detail/Reklamation/Print/Pdf brauchen eine Id
  bzw. kommen aus der Liste — keine Menü-Kandidaten.
- Der Begriff „Vorschlagsliste“ ist in der Oberfläche schon eingeführt (`Beschichtungsauftraege/Index.cshtml:13-14`,
  Knopf „Zur Vorschlagsliste“; Hilfe Z.559, Z.563). Der Umschalter auf der Seite heißt „Liste | Summiert“
  (`FaHierarchyBeschichtung/Summiert.cshtml:24-31`, `Index.cshtml:25-32`).

**Folgerung:** Lese- und Schreibrolle sehen **dieselben** Einträge — eine Bedingung je Eintrag ist nicht nötig. Weil alle
Einträge dieselbe Bedingung haben, ist „Dropdown nur rendern, wenn mind. ein Eintrag sichtbar“ gleichbedeutend mit
der heutigen Bedingung Z.114. Die Schreibrolle unterscheidet sich nur durch Knöpfe *auf* den Seiten (unverändert).

**Soll:**
- An derselben Stelle der Navigationsleiste (nach „FA-Struktur“, vor „Vormontage“) ein `nav-item dropdown` mit Toggle
  „Beschichtung“, gerendert genau unter `hasBeschichtungsauftragAccess && faBeschichtungAktiv`.
- Einträge, Texte und Reihenfolge laut Rückfrage 1 (Empfehlung: „Vorschlagsliste (summiert)“ → `Summiert`,
  „Vorschlagsliste (Einzelpositionen)“ → `Index`, Trennlinie, „Aufträge“ → `Beschichtungsauftraege/Index`).
- Toggle ist `active`, wenn der Controller `FaHierarchyBeschichtung` **oder** `Beschichtungsauftraege` ist
  (Muster `is "…" or "…"`, `_Layout.cshtml:215`). Damit ist auch das Auftragsdetail und die Reklamationsseite markiert
  (gleicher Controller).
- Die beiden alten Top-Level-`nav-link`s entfallen ersatzlos.
- AKE: Toggle ist dort aus (Default „nur exakt true“, `_Layout.cshtml:56-60`) → kein Menüpunkt, wie heute.

### F2 — Kurzanleitungen in der App-Hilfe

Hilfe-Detail-Regel: konkrete Schritte, Rollen/Rechte, was bei Fehlern passiert. Anwendersprache, deutsch, keine
Klassen-/Controller-Namen. Rollen mit ihrem Anzeigenamen; Rollen-Schlüssel und AppSetting-Name nur in **einem**
abgesetzten Satz „Einrichtung (Admin)“ — der Admin braucht sie zum Anlegen (`README.md:414`: „Rolle unter `/Roles`
mit genau diesem Key anlegen“). Inhaltliche Quelle: `docs/TESTSZENARIEN.md` TS-81 (Z.9283-9338), TS-82 (Z.9342-9402),
TS-84 (Z.9445-9523), TS-85 (Z.9525-9565), TS-86 (Z.9567-9626) — daraus ableiten, nicht neu erfinden; jede Aussage
muss dort oder im bestehenden Hilfetext gedeckt sein.

**Inventar vorhandener Abschnitte (v1.39–v1.54, `Help/Index.cshtml`) und Plan:**

| Z. | Abschnitt (Karte) | Plan |
|---|---|---|
| 420-436 | IDEAL: PDF-Download der Druckdokumente (v1.39, FA-Karte; gilt Kommissionierliste **und** Beschichtung) | **behalten** (gemeinsam); neue Karte verweist darauf in einem Satz |
| 438-487 | IDEAL: Kommissionierliste — Summiert (v1.47) | **behalten** (anderes Thema) |
| 489-514 | IDEAL: Beschichtungsauftrag — Summiert als Standard (v1.48) | **ersetzen**: in neue Karte, Abschnitt „Vorschlagsliste“; Satz „Der Menuepunkt **Beschichtungsauftrag** fuehrt jetzt direkt …“ (Z.491) wird zu „*Beschichtung → Vorschlagsliste (summiert)*“; Details (Maße, Kopf, Filter) bleiben als Unterpunkte |
| 516-535 | IDEAL: Briefkopf (v1.49) | **ersetzen**: in neue Karte, Abschnitt „Druck und PDF“; Admin-Teil (Vorlage, Pfad) als „Einrichtung (Admin)“ |
| 537-555 | Arbeitsgang-Stammdaten (v1.54) | **unverändert** (out of scope) |
| 557-585 | IDEAL: Beschichtungsaufträge (v1.51) | **ersetzen**: in neue Karte, aufgeteilt auf Kurzanleitung (Schritte) + Abschnitte Status / Reklamation / Rechte |
| 606-637 | IDEAL: Arbeitsgaenge aus der FA-Struktur (v1.41) | **behalten** (Einrichtung/Service, kein Bedienthema) |
| 639-656 | IDEAL: Arbeitsgaenge als Kuerzel im FA-Struktur-Baum (v1.52) | **erweitern**: vorne Kurzanleitung „So lesen Sie die Kürzel“ (Schritte), wer die Seite sieht, Verweis auf den Knopf *Arbeitsgänge* (v1.45-Abschnitt); bestehende Punkte bleiben |
| 658-677 | IDEAL: Arbeitsgaenge einer FA-Struktur ansehen (v1.45) | **behalten** |
| 222-247 | Liste als CSV exportieren (v1.53, Karte „Listen-Pagination & Filter“) | **erweitern**: vorne Kurzanleitung (Schritte), Rechte, „wo es keinen Knopf gibt“; bestehende Punkte bleiben (decken Fehlerfälle schon ab, Z.235-245) |
| 1523-1528 | Stammdaten → Einstellungen (Hinweis Arbeitsgang-Stammdaten) | **unverändert** |

Gefundene Beschichtungs-Hilfe vor v1.48 (Teil 4 v1.31) gibt es auf der Hilfeseite nicht als eigenen Abschnitt —
`grep` über `<dt>` (Z.14-1743) findet nur Z.420, 489, 516, 557. v1.50.0 (Teil 3) hatte keine Oberfläche und keine Hilfe.

**(a) Neue Karte „Beschichtung (IDEAL)“** (Platz: direkt nach der Karte „Fertigungsaufträge“, also nach Z.727;
Aufbau nach dem bestehenden Muster der BDE-Karte Z.973-1009: `card` → `card-header` `<strong>` → `card-body` mit
`<h6 class="mt-3">`-Unterabschnitten, Schrittfolgen als `<ol>`, Details als `<dl>`/`<ul>`). Inhalt:
1. **Wofür, wer, wo** — Menü *Beschichtung*; sichtbar mit Rolle „Beschichtungsauftrag (IDEAL)“ (Lesen, Drucken) oder
   „Beschichtungsaufträge (Schreibrecht)“ (zusätzlich anlegen/bearbeiten), nur am Standort IDEAL eingeschaltet.
2. **Kurzanleitung (Schritt für Schritt)** — Vorschlagsliste öffnen → HauptFA suchen (Spaltenfilter) → „Aus
   Vorschlagsliste erstellen“ → im Entwurf Dienstleister/RAL/Start/Retour/Mengen prüfen, „Entwurf speichern“ → „Auf
   ‚Übergeben‘ setzen“ → Drucken/PDF → nach Rückkehr „Erledigt“ (Quelle TS-84.1, .4, .6–.8).
3. **Vorschlagsliste** — Inhalt aus Z.489-514 (summiert/Einzelpositionen, Maße uneinheitlich, Kopf je HauptFA,
   Filter, Umschalter), plus Gruppenkopf mit vorhandenen Aufträgen und „(n Positionen frei)“ (TS-84.1, .2).
4. **Status** — Entwurf → Übergeben → Erledigt; Storno nur im Entwurf, gibt Positionen frei; was nach Übergeben noch
   änderbar ist (Z.560-574, TS-84.7, .12).
5. **Reklamation** — Z.575-578, TS-84.9–.11.
6. **Druck und PDF** — ab Übergeben; Briefkopf, Empfänger = Dienstleister des Auftrags, Barcode = Auftragsnummer,
   ausgeblendete Spalten fehlen auch im Druck; Vorschlagsliste druckt je HauptFA; Verweis auf PDF-Abschnitt Z.420
   (Z.516-535, TS-82.2, TS-84.8).
7. **Wenn etwas nicht klappt** — gelbe Meldungen und was zu tun ist: „Dienstleister fehlt“, „inzwischen von jemand
   anderem geändert — neu laden“, „bereits vergeben“, „Im Entwurf nicht druckbar“, kein PDF-Knopf (Edge fehlt),
   Briefkopf-Hinweis (nur am Bildschirm), „Empfänger fehlt — in Sage nachpflegen“, Menü fehlt (Rolle/Schalter)
   (Quellen Z.527-533, Z.579-580, TS-84.3, .6, .7, .13, TS-81.11).
8. **Einrichtung (Admin)** — ein Absatz: Rollen-Schlüssel `beschichtungsauftrag` / `beschichtungsauftrag_schreiben`,
   Einstellung `FaHierarchyBeschichtungAktiv`, Briefkopf-Vorlage (Pfad) unter Standorteinstellungen → Firmendaten
   (Z.523-526, Z.581-583).

**(b) Arbeitsgang-Kürzel** — Erweiterung von Z.639-656 um eine `<ol>`: *FA-Struktur* öffnen (sichtbar mit Kommissionierung,
Teileverfolgung, Leitstand oder Vorbau — `_Layout.cshtml:72`, `:105-110`) → Struktur aufklappen → Spalte *Arbeitsgänge*
lesen (Reihenfolge wie Sage) → Werkbank-Name per Mauszeiger oder Tab → nach Kürzel filtern (`ka`) → Details über den
Knopf *Arbeitsgänge* an der Kopfzeile (Abschnitt v1.45). Fehlerfälle: Kürzel ohne Namen (keiner/mehrere Werkbänke),
Spalte ausgeblendet (Zahnrad), Strich = keine Kürzel (TS-85.1–.3, .7).

**(c) CSV-Export** — Erweiterung von Z.222-247 um eine `<ol>`: Liste öffnen → Filter setzen → Spalten über das Zahnrad
wählen/ordnen → Knopf *CSV* neben dem Zahnrad → Datei per Doppelklick in Excel. Rechte: kein eigenes Recht — exportiert
wird, was die Liste dem Benutzer zeigt (TS-86.8). Kein Knopf in Baumansichten (FA-Struktur, Tracking-Baum,
OSEON-Tracking) und auf Formular-/Druckseiten (TS-86.1). Bestehende Punkte (5.000er-Kappung, rote Meldung ohne Datei)
bleiben unverändert.

## Technischer Loesungsentwurf

- **Layout** (`_Layout.cshtml:112-125` ersetzen): ein `<li class="nav-item dropdown">` mit
  `<a class="nav-link dropdown-toggle @((ViewContext.RouteData.Values["controller"]?.ToString() is "FaHierarchyBeschichtung" or "Beschichtungsauftraege") ? "active" : "")" href="#" role="button" data-bs-toggle="dropdown" aria-expanded="false">Beschichtung</a>`
  und `<ul class="dropdown-menu">` mit `dropdown-item`-Links (`asp-controller`/`asp-action`), Trennlinie wie Z.229
  (`<li><hr class="dropdown-divider" style="border-color: rgba(255,255,255,0.2);" /></li>`). Bedingung Z.114 und die
  Variablen Z.56-61 bleiben wortgleich. Kommentar über dem Block anpassen (IDEAL Teil 4 + Ausbau Teil 4, ein
  Dropdown seit dieser Version). Keine neue Variable, kein neuer Service (ponytail: alles existiert).
- **Hilfe**: reines Razor-Markup in `Help/Index.cshtml`, keine Logik, keine Rollen-Gates (die Seite hat heute keine:
  kein `@if`/`@inject`). Fallstrick `fallstricke.md` „Razor: `@` im Hilfetext ist Code“ — `@@` schreiben, vor dem
  Commit auf „0 Fehler“ prüfen. Umlaute: die Seite mischt Umschreibung („Menuepunkt“) und echte Umlaute
  („Beschichtungsaufträge“, Z.557) — neue Texte mit echten Umlauten und HTML-Entities wie im Bestand (`&bdquo;`,
  `&rarr;`); bestehende Abschnitte nicht nur deswegen umschreiben.
- **Seitentitel** (nur bei Rückfrage 2 = ja): `FaHierarchyBeschichtung/Index.cshtml:5`, `:15`,
  `Summiert.cshtml:5`, `:14`. Der CSV-Dateiname hängt nicht am Titel (`csv-export.js:181` nimmt `data-view-key`).
- **Frontend-Pflicht:** Der Dev-Lauf ruft den Skill `frontend-design` **explizit** auf (Views geändert). Leitplanken:
  Bootstrap-Dropdown-Muster der vorhandenen Menüs, keine neuen Icons/Bibliotheken; Hilfe-Karte im bestehenden
  Karten-/`h6`-Muster; Kontrast WCAG AA (Toggle-Active-Zustand wie die anderen Dropdowns); Tastaturbedienung des
  Dropdowns (Bootstrap-Standard: Enter/Leertaste öffnet, Pfeiltasten, Esc) bleibt erhalten; im eingeklappten
  Mobil-Menü (`navbar-collapse`, Z.28) bedienbar.
- **Test (Quelltext-Wächter):** Es gibt im Testprojekt **keine** Razor-Render-Tests: `TestServer`/`WebApplicationFactory`
  kommen nicht vor; `NullableIntArrayBindingTests.cs:20-21` hält fest, dass `Microsoft.AspNetCore.TestHost` ein neues
  NuGet-Paket wäre. Layout-Prüfungen laufen heute als Quelltext-Prüfung (`ResponsiveCssTests.cs:33`,
  `CsvExportCoverageTests.cs:44-51`). Kleinster tragfähiger Ansatz: neue Facts in der bestehenden
  `IdealAkeWms.Tests/Views/BeschichtungsauftraegeViewTests.cs` (hat schon den Views-Root-Helfer Z.18-26), z. B.
  `Navigation_EinDropdownBeschichtung`:
  1. `_Layout.cshtml` enthält genau ein `dropdown-toggle` mit Text „Beschichtung“;
  2. dessen Active-Ausdruck nennt `"FaHierarchyBeschichtung"` **und** `"Beschichtungsauftraege"`;
  3. zwischen `@if (hasBeschichtungsauftragAccess && faBeschichtungAktiv)` und dem Ende des Dropdowns stehen die
     Einträge aus Rückfrage 1 in der beschlossenen Reihenfolge (Controller/Action je Eintrag);
  4. kein `nav-link` (Top-Level) verweist mehr auf `asp-controller="FaHierarchyBeschichtung"` oder
     `"Beschichtungsauftraege"`;
  5. die Bedingung kommt genau einmal vor.
  Plus ein Fact für die Hilfe: `Help/Index.cshtml` enthält die Karte „Beschichtung (IDEAL)“ und **nicht** mehr den
  Satz „Der Menuepunkt <strong>Beschichtungsauftrag</strong>“. Grenze (in der Testklasse dokumentieren): der Wächter
  beweist die Markup-Struktur, nicht die Sichtbarkeit je Rolle zur Laufzeit — die ist durch
  `CurrentUserServiceRoleTests` (Rollenlogik) + Manual-UAT (TS neu) abgedeckt.

## Migrations-/SQL-Auswirkungen

Keine. Geändert werden nur Razor-Views, ein Test und Versions-/Doku-Dateien; kein Model, kein `DbContext`, keine
Rolle, kein AppSetting-Key (Toggle `FaHierarchyBeschichtungAktiv` und beide Rollen existieren:
`Models/RoleKeys.cs:24`, `:26`; `_Layout.cshtml:59`). Damit: keine EF-Migration, kein `SQL/XX_*.sql`, kein
`00_FreshInstall.sql`-Nachtrag, keine `SQL/AgentJobs/*`.

## Audit-Feld-Auswirkungen

Keine — es wird nichts gespeichert; keine Entität wird angelegt oder geändert.

## Akzeptanzkriterien

1. In der Navigationsleiste gibt es für die Beschichtung genau **einen** Eintrag, das Dropdown „Beschichtung“, an der
   bisherigen Stelle (nach „FA-Struktur“, vor „Vormontage“); die Top-Level-Einträge „Beschichtungsauftrag“ und
   „Beschichtungsaufträge“ gibt es nicht mehr.
2. Das Dropdown enthält die Einträge aus Rückfrage 1 in der beschlossenen Reihenfolge und mit den beschlossenen Texten;
   jeder Eintrag öffnet die zugehörige Seite (`/FaHierarchyBeschichtung/Summiert`, `/FaHierarchyBeschichtung`,
   `/Beschichtungsauftraege` — vorbehaltlich Rückfrage 1).
3. Sichtbarkeit unverändert: Dropdown erscheint genau dann, wenn der Benutzer `admin`, `beschichtungsauftrag` oder
   `beschichtungsauftrag_schreiben` hat **und** `FaHierarchyBeschichtungAktiv = true`; Lese- und Schreibrolle sehen
   dieselben Einträge.
4. Der Toggle „Beschichtung“ ist als aktiv markiert auf jeder Seite der Controller `FaHierarchyBeschichtung`
   (Liste, Summiert) und `Beschichtungsauftraege` (Liste, Detail, Reklamation) und auf keiner anderen Seite.
5. Zugriff unverändert: Direktaufrufe der drei Seiten ohne Rolle bzw. mit Schalter aus enden wie bisher auf
   AccessDenied bzw. gesperrt (keine Filteränderung).
6. Das Dropdown ist per Tastatur und im eingeklappten Mobil-Menü bedienbar; Toggle-Text und Aktiv-Zustand erfüllen
   WCAG AA wie die übrigen Dropdowns.
7. `Help/Index.cshtml` enthält eine Karte „Beschichtung (IDEAL)“ mit: Wofür/wer/wo, Kurzanleitung als nummerierte
   Schritte, Vorschlagsliste, Status, Reklamation, Druck und PDF, „Wenn etwas nicht klappt“, Einrichtung (Admin).
8. Die Abschnitte v1.48 (Z.489-514), v1.49 (Z.516-535) und v1.51 (Z.557-585) stehen nicht mehr zusätzlich in der Karte
   „Fertigungsaufträge“; ihr Inhalt ist in der neuen Karte vollständig enthalten (keine Aussage geht verloren; Abgleich
   im Review gegen die alte Fassung). *(Gilt bei Rückfrage 3 = ja; bei „nein“ werden die Abschnitte an Ort und Stelle
   gekürzt/ergänzt und die Kurzanleitung steht vorne im v1.51-Abschnitt.)*
9. Keine Hilfestelle nennt mehr den alten Menüweg („Menuepunkt Beschichtungsauftrag“ o. ä.); der Menüweg lautet
   *Beschichtung → …* mit den beschlossenen Eintragstexten.
10. Der Abschnitt „Arbeitsgaenge als Kuerzel im FA-Struktur-Baum“ beginnt mit einer nummerierten Kurzanleitung und nennt,
    wer die FA-Struktur sieht; die bisherigen Punkte bleiben.
11. Der Abschnitt „Liste als CSV exportieren“ beginnt mit einer nummerierten Kurzanleitung, nennt die Rechte-Regel und
    die Ansichten ohne Knopf; die bisherigen Punkte bleiben.
12. Kurzanleitungen enthalten keine Klassen-, Controller- oder Action-Namen; Rollen-Schlüssel und Schalter-Name
    erscheinen nur im Absatz „Einrichtung (Admin)“.
13. Jede inhaltliche Aussage der neuen Hilfetexte ist durch TS-81/82/84/85/86 oder den bisherigen Hilfetext gedeckt
    (keine neuen Behauptungen über Verhalten).
14. Nur falls Rückfrage 2 = ja: Bildschirm-Überschrift und Browsertitel beider Vorschlagslisten-Views lauten wie
    beschlossen; Druck/PDF unverändert „Beschichtungsauftrag …“.
15. Der Quelltext-Wächter (Facts 1–5 + Hilfe-Fact, siehe Lösungsentwurf) ist grün; `dotnet build` 0 Fehler,
    `dotnet test` grün.
16. Version in **beiden** `AppVersion.cs` auf die nächste freie Bündel-Version, Eintrag in `Help/Changelog.cshtml`.
17. `docs/TESTSZENARIEN.md`: neues Kapitel (nächste freie TS-Nummer); TS-81.2, TS-81.13 und TS-84.14 nennen den
    neuen Menüweg (Altwortlaut nicht stillschweigend löschen, sondern mit „(seit v1.xx: Beschichtung → …)“ ersetzen);
    `secondbrain/tests/testszenarien-index.md` nachgezogen.

## Test-Szenarien

Neues Kapitel **TS-<nächste freie>** (heute TS-88) „IDEAL: Menü Beschichtung + Kurzanleitungen in der Hilfe
(v<nächste freie>)“. Vorbedingungen: IDEAL-Testsystem, Benutzer **S** (`beschichtungsauftrag_schreiben`),
**L** (nur `beschichtungsauftrag`), **N** (keine der beiden, aber z. B. Kommissionierung), **A** (`admin`);
`FaHierarchyBeschichtungAktiv` umschaltbar. Automatisiert: Quelltext-Wächter in `BeschichtungsauftraegeViewTests`,
Rollenlogik `CurrentUserServiceRoleTests`. Sichtbarkeit, Active-Zustand und Hilfetexte sind Manual-UAT.

- **.1 Dropdown sichtbar (AK 1, 2).** L meldet sich an → ein Menüpunkt „Beschichtung“ zwischen „FA-Struktur“ und
  „Vormontage“, keine Einträge „Beschichtungsauftrag“/„Beschichtungsaufträge“ mehr in der Leiste. Aufklappen → Einträge
  in beschlossener Reihenfolge; jeder öffnet die erwartete URL.
- **.2 Active-Markierung (AK 4).** Nacheinander Vorschlagsliste (summiert), Umschalter „Liste“, Aufträge, ein
  Auftragsdetail, (S) Reklamationsseite öffnen → „Beschichtung“ jeweils markiert. FA-Struktur, FA-Liste öffnen →
  „Beschichtung“ nicht markiert.
- **.3 Nur Schreibrolle (AK 3).** S sieht dasselbe Dropdown wie L (gleiche Einträge); auf den Seiten zusätzlich die
  Schreib-Knöpfe (Regression TS-84.14).
- **.4 Nur Leserolle (AK 3, 5).** L sieht das Dropdown, auf den Seiten keine Schreib-Knöpfe (Regression TS-84.14).
- **.5 Negativ — Rolle ohne Rechte (AK 3, 5).** N: kein „Beschichtung“ in der Leiste (auch nicht im Mobil-Menü);
  `/FaHierarchyBeschichtung/Summiert` und `/Beschichtungsauftraege` direkt → AccessDenied.
- **.6 Negativ — Schalter aus (AK 3, 5).** A setzt `FaHierarchyBeschichtungAktiv = false` → für A, S, L kein
  „Beschichtung“; Direktaufrufe gesperrt wie bisher. Schalter wieder an → Dropdown zurück.
- **.7 AKE (AK 3).** AKE-Testsystem (Schalter aus/nicht gesetzt) → kein Menüpunkt „Beschichtung“, Navigationsleiste
  sonst unverändert.
- **.8 Bedienung (AK 6).** Nur Tastatur: Tab bis „Beschichtung“, Enter öffnet, Pfeiltasten wählen, Esc schließt.
  Schmales Fenster/Tablet: Hamburger-Menü → „Beschichtung“ aufklappbar, Einträge erreichbar. Kontrast des markierten
  Toggles wie bei „Bestellungen“.
- **.9 Hilfe Beschichtung (AK 7–9, 12, 13).** Hilfe öffnen → Karte „Beschichtung (IDEAL)“ nach „Fertigungsaufträge“;
  Kurzanleitung mit S am Testsystem einmal Schritt für Schritt nachgehen (anlegen bis Druck) — jeder Schritt passt zur
  Oberfläche (Knopf-/Menütexte gleich). Suche (Strg+F) nach „Menuepunkt Beschichtungsauftrag“ → kein Treffer; die
  alten v1.48/v1.49/v1.51-Abschnitte stehen nicht mehr in der FA-Karte (bei Rückfrage 3 = ja).
- **.10 Hilfe Kürzel und CSV (AK 10, 11).** Beide Abschnitte beginnen mit nummerierten Schritten; Schritte am
  Testsystem nachvollziehen (FA-Struktur: Kürzel, Tooltip, Filter `ka`; FA-Liste: CSV mit Filter und ausgeblendeter
  Spalte).
- **.11 Seitentitel (AK 14, nur bei Rückfrage 2 = ja).** Vorschlagsliste: Überschrift und Browser-Tab mit neuem Titel;
  „Drucken“ → Druckdokument weiter „Beschichtungsauftrag …“.
- **.12 Regression.** TS-81.2 (mit neuem Menüweg), TS-84.14, TS-86.4 (CSV auf beiden Vorschlagslisten und
  Aufträgen) — unverändert bis auf den Menüweg; übrige Navigationspunkte (Lager, FA, Kommissionierung, Bestellungen)
  unverändert.

## Etappen (nur bei epic: true)

Entfällt (`epic: false`) — ein Lauf, ein Commit-Paket im Bündel.

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | – | – | – |

## Deploy

*Final (QA 2026-10-13) aus dem echten Diff `627c992a..d3746373`.*

- **Web-App:** ja — `Views/Shared/_Layout.cshtml`, `Views/Help/Index.cshtml`, `Views/Help/Changelog.cshtml`,
  `Views/FaHierarchyBeschichtung/Index.cshtml` + `Summiert.cshtml` (Titel), `AppVersion.cs`.
- **Service:** nur Versions-Bump `IDEALAKEWMSService/AppVersion.cs` (1.55.0 → 1.56.0), keine fachliche Änderung.
  Publish nur nötig, wenn die angezeigte Service-Version gleichziehen soll.
- **Migration:** nein (kein Eintrag unter `*/Migrations/`, kein `SQL/`). Kein Einmal-Skript.
- **Ablauf:** aus dem Bündel-Worktree publishen → Testsystem → testen → Merge. Kein DB-Backup-/DB-Update-Schritt nötig.
  ```
  dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb
  dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService   # optional, nur Versionsanzeige
  ```
- Kein Rollen-/Einstellungs-Schritt (alles existiert). Kein JS/CSS geändert → kein Cache-Thema.
- Nach dem Merge nur dann erneut aus `main` publishen, wenn der Merge getestete Dateien mit parallelen `main`-Änderungen
  zusammengeführt hat.

## Offene Rueckfragen

1. **Menü-Einträge: Texte und Reihenfolge** im Dropdown „Beschichtung“? Vorhanden sind drei GET-Einstiege
   (`FaHierarchyBeschichtung/Summiert`, `FaHierarchyBeschichtung/Index`, `Beschichtungsauftraege/Index`).
   **Empfehlung:** „Vorschlagsliste (summiert)“ → Summiert, „Vorschlagsliste (Einzelpositionen)“ → Index, Trennlinie,
   „Aufträge“ → Beschichtungsauftraege. Gründe: Arbeitsreihenfolge (erst Vorschlag, dann Auftrag); „Vorschlagsliste“
   ist in der Oberfläche schon eingeführt (`Beschichtungsauftraege/Index.cshtml:14`); „Einzelpositionen“ ist der
   Hilfe-Begriff (Z.495) und eindeutiger als „einzeln“. Schlankere Alternative: nur zwei Einträge („Vorschlagsliste“ →
   Summiert, „Aufträge“) — die Einzelpositionen sind über den Umschalter „Liste“ auf der Seite erreichbar.
2. **Seitentitel der Vorschlagsliste angleichen?** Heute heißen beide Listen-Views „Beschichtungsauftrag (IDEAL)“
   (`FaHierarchyBeschichtung/Index.cshtml:5,15`, `Summiert.cshtml:5,14`), obwohl „Beschichtungsauftrag“ seit v1.51 der
   gespeicherte Auftrag ist. **Empfehlung:** ja — Überschrift + Browsertitel auf „Vorschlagsliste Beschichtung (IDEAL)“,
   je zwei Zeilen; Druck/PDF-Dokumente bleiben „Beschichtungsauftrag …“ (gehen an den Dienstleister). „Nein“ ist
   möglich, dann zeigt das Menü „Vorschlagsliste“ und die Seite „Beschichtungsauftrag“.
3. **Hilfe umsortieren?** Die drei Beschichtungs-Abschnitte (v1.48 Z.489, v1.49 Z.516, v1.51 Z.557) stehen in der Karte
   „Fertigungsaufträge“. **Empfehlung:** ja — in eine neue Karte „Beschichtung (IDEAL)“ nach „Fertigungsaufträge“
   verschieben und dort zur Kurzanleitung + Details zusammenführen (sonst zwei Fundstellen für dasselbe Thema). Alle
   anderen Karten und Abschnitte bleiben, wo sie sind.
4. **Screenshots in der App-Hilfe?** **Empfehlung:** nein — die Hilfeseite enthält heute keine (nur kleine Inline-SVG-Icons,
   Z.269-282), Bilder veralten mit jeder Oberflächenänderung und müssten mit ins Web-Publish; Screenshots gehören ins
   ausführliche Dokument der Koordination.

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
  Bei Varianten-Specs zusaetzlich freigabe_entscheidung im Frontmatter setzen.
-->

1. Menü-Einträge (Texte, Reihenfolge) → **Zwei Eintraege (Gerald 2026-10-13, abweichend von der Empfehlung): „Vorschlagsliste“ → FaHierarchyBeschichtung/Summiert, „Auftraege“ → Beschichtungsauftraege/Index. Einzelpositionen nur ueber den Umschalter „Liste“ auf der Seite.**
2. Seitentitel der Vorschlagsliste angleichen → **Wie empfohlen (Gerald 2026-10-13).**
3. Hilfe-Abschnitte in neue Karte „Beschichtung (IDEAL)“ verschieben → **Wie empfohlen (Gerald 2026-10-13).**
4. Screenshots in der App-Hilfe → **Wie empfohlen (Gerald 2026-10-13).**

> **FREIGABE-NACHTRAG (Gerald 2026-10-13, Antwort 1):** Dropdown „Beschichtung“ mit nur ZWEI Eintraegen –
> „Vorschlagsliste“ (FaHierarchyBeschichtung/Summiert) und „Auftraege“ (Beschichtungsauftraege/Index), keine Trennlinie.
> Active-Markierung des Dropdowns fuer beide Controller (auch FaHierarchyBeschichtung/Index). Geht dem Rumpf vor.

## Umsetzung (2026-10-13, Dev-Lauf)

Direkt im Bündel `feature/2026-08-07-ideal-teile-1-5`, Start `627c992a` (Endstand FA-Struktur Teil 2, v1.55.0; Worktree
sauber, keine offenen wip-Commits). Plan `docs/superpowers/plans/2026-10-13-menue-beschichtung-und-hilfe-kurzanleitungen.md`.
Commits `14d1600d` (Dropdown, Titel, Hilfe, Wächter), `699586af` (feature-complete: v1.56.0, Changelog, TS-89, Menüweg in
TS-81.2/81.13/84.14). **Version 1.56.0, TS-89** (neu gemessen nach FA Teil 2). Keine Migration.
Aufgabe [[2026-10-13-menue-beschichtung-und-hilfe-kurzanleitungen-umsetzung]].

**Umgesetzt:** `_Layout.cshtml` — ein `nav-item dropdown` „Beschichtung“ unter der wortgleichen Bedingung, Einträge
„Vorschlagsliste“ (`FaHierarchyBeschichtung/Summiert`) und „Aufträge“ (`Beschichtungsauftraege/Index`), keine Trennlinie
(Nachtrag), Active per `is "FaHierarchyBeschichtung" or "Beschichtungsauftraege"` (deckt auch `FaHierarchyBeschichtung/Index`,
Detail, Reklamation). Titel + Überschrift beider Vorschlagslisten-Views „Vorschlagsliste Beschichtung (IDEAL)“ (Summiert hatte
vorher „Beschichtungsauftrag — Summiert (IDEAL)“ als Browsertitel; jetzt beide gleich, wie Antwort 2), Druck/PDF unverändert.
Hilfe: neue Karte „Beschichtung (IDEAL)“ direkt nach „Fertigungsaufträge“ (Wofür/wer/wo, Kurzanleitung `<ol>`, Vorschlagsliste,
Status, Reklamation, Druck und PDF, Wenn etwas nicht klappt, Einrichtung (Admin)); v1.48/v1.49/v1.51-Abschnitte aus der
FA-Karte entfernt, Inhalt Satz für Satz übernommen; Kürzel-Abschnitt (inkl. v1.55.0-Statuspunkten, nicht gedoppelt) und
CSV-Abschnitt mit vorangestellter `<ol>`, Rechte-Regel bzw. „wer sieht die FA-Struktur“, Ansichten ohne CSV-Knopf.
PDF-Abschnitt v1.39: Listenname „Beschichtungsauftrag“ → „Vorschlagsliste Beschichtung“ (Konsistenz zum neuen Titel).

**Rulings:** R1 — Menütext „Aufträge“ mit echtem Umlaut (Antwort schreibt „Auftraege“ in ASCII; die Oberfläche nutzt Umlaute,
z. B. „Beschichtungsaufträge“). R2 — Browser-Testprotokoll: in `secondbrain/tests/` gibt es keins für TS-81/TS-84 mit
Menü-Schritten (`2026-09-08-uat-protokoll-ideal-buendel-chrome.md` ruft `/FaHierarchyBeschichtung` per URL auf) — nichts
nachzuziehen. R3 — Einrichtung (Admin) nennt `/Roles` als Ort (README:414).

**Zusatzfund (nicht gebaut):** `ColumnDefinitions.cs:377`/`:402` führen als Anzeigename der Spalten-Einstellungen
„Beschichtungsauftrag (IDEAL)“ bzw. „Beschichtungsauftrag — Summiert (IDEAL)“ (sichtbar in `Users/Edit.cshtml:196`,
gespeicherte Spalteneinstellungen) — außerhalb des Spec-Umfangs, bei Bedarf in einer Folge-Spec angleichen.

**Beweis:** `dotnet build IdealAkeWms.slnx` → 0 Fehler; `dotnet test` → Web 1831 bestanden / 1 übersprungen (+5 Wächter),
Service 272 bestanden. Wächter vorher rot (5 Fehler), danach grün.

## QA-Nachweis (2026-10-13, qa-agent)

Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, Zweig `feature/2026-08-07-ideal-teile-1-5`, geprüfter Bereich
`627c992a..699586af` (2 Commits) + QA-Fix `d3746373`. Web-App nicht gestartet (Auto-Migration).

### 1. Build und Tests (nach dem QA-Fix erneut gelaufen)

- `dotnet build IdealAkeWms.slnx` → **0 Fehler**, 12 Warnungen (Bestand, z. B. CS8321 `FaWorklist/Index.cshtml`).
- `dotnet test` → **IdealAkeWms.Tests: 1831 bestanden / 1 übersprungen / 0 Fehler (1832)**;
  **IDEALAKEWMSService.Tests: 272 bestanden / 0 übersprungen / 0 Fehler (272)**.

### 2. Code-Review (selbst, in diesem Lauf)

Geprüft: kompletter Diff (`_Layout.cshtml`, beide Vorschlagslisten-Views, `Help/Index.cshtml`, `Help/Changelog.cshtml`,
beide `AppVersion.cs`, `BeschichtungsauftraegeViewTests.cs`, `docs/TESTSZENARIEN.md`, Plan) plus Umgebung.

- **Layout:** Bedingung `@if (hasBeschichtungsauftragAccess && faBeschichtungAktiv)` wortgleich und einmalig; Toggle
  1:1 nach Muster „Fertigungsaufträge“/„Bestellungen“ (`nav-link dropdown-toggle`, `href="#"`, `role="button"`,
  `data-bs-toggle="dropdown"`, `aria-expanded="false"`, `is "…" or "…"`-Active-Ausdruck mit beiden Controllern); zwei
  `dropdown-item` (Summiert, Beschichtungsauftraege/Index), keine Trennlinie (Nachtrag); keine Top-Level-Links mehr;
  Position nach FA-Struktur, vor Vormontage. Tastatur/Mobil = Bootstrap-Standard wie die übrigen Dropdowns (Manual-UAT TS-89.8).
- **Titel:** beide Vorschlagslisten-Views Title + `<h2>` „Vorschlagsliste Beschichtung (IDEAL)“; `Print*.cshtml` nicht im
  Diff (Druck unverändert, Wächter prüft den `Print.cshtml`-Titel).
- **AK 8 (Satz-für-Satz-Abgleich gegen `git show 627c992a:IdealAkeWms/Views/Help/Index.cshtml`):** v1.48 (Menüweg →
  Kurzanleitung Schritt 1/„Summiert (Standard)“, Summen-/Positionssatz, Umschalter „Liste“, Maße, Kopf je HauptFA,
  Spaltenfilter, Druck/PDF je HauptFA mit Barcode + sichtbare Spalten + PDF/Edge), v1.49 (Briefkopf Logo/Absender/Kontakt,
  erste Seite, Empfänger/Dokument/Datum, Liste + summiert, keine Kommissionierlisten, Vorlage/Pfad, Fallback-Kopf,
  Logo/Schrift-Teilausfall, gelber Hinweis nur Bildschirm, Empfänger fehlt/mehrdeutig), v1.51 (BA-Nummer, Ablauf, Storno,
  Anlegen inkl. „nur solange frei“ und „nur über Reklamation“, Entwurf wörtlich, Übergeben, Reklamation wörtlich,
  Gleichzeitig bearbeitet, Rechte → Intro (Anzeigenamen) + Einrichtung (Admin)) — **jede Aussage wiedergefunden**.
- **AK 13 (neue Aussagen):** Gruppenkopf-Link/„(n Positionen frei)“/„in BA-…“ TS-84.1/.2; Statusverlauf mit Zeit+Benutzer
  TS-84.7 (`Details.cshtml:67-75`); Storno mit Rückfrage TS-84.12 (`Details.cshtml:108`); „Reklamation zu BA-…“,
  Mehrfach-/Ketten-Reklamation TS-84.9/.11; Empfänger = Dienstleister des Auftrags, ausgeblendete Spalte fehlt im Druck
  TS-84.8; PDF-Fehler gelb TS-81.11; Meldungstexte im Code belegt (`Details.cshtml:121`, Controller-/Service-Texte
  „bereits vergeben“, „Im Entwurf nicht druckbar — erst übergeben.“, „inzwischen von jemand anderem geändert — bitte neu
  laden …“); Knopftext „Auf „Übergeben“ setzen“ (`Details.cshtml:112`); Anlegen → Detail
  (`BeschichtungsauftraegeController.cs:154`); FA-Struktur-Sichtbarkeit = `showFaHierarchy` (`_Layout.cshtml:72`);
  Strich/Spalte ausgeblendet TS-85; BDE-Status im Tooltip TS-88 + Bestandshilfe v1.55; CSV-Rechte und Ansichten ohne Knopf
  TS-86.1/.8, Doppelklick in Excel TS-86. **Gedeckt.**
- **AK 12:** Rollen-Schlüssel und `FaHierarchyBeschichtungAktiv` nur im Absatz „Einrichtung (Admin)“; Intro nutzt die
  Anzeigenamen aus `RoleOverview.cshtml:194/207`. Keine Klassen-/Controller-/Action-Namen in den Kurzanleitungen.
- **AK 9:** `grep Beschichtungsauftr Help/Index.cshtml` → nur noch Karten-Intro (Objektname + Rollen-Anzeigenamen); kein
  alter Menüweg.
- Defektklassen-Check: keine `<script>`-Einbettung, kein `hidden`+`d-*`, keine stillen Fallbacks, kein Binding, keine
  Client-only-Validierung, keine Regel-Duplikate — der Diff ist reines Markup/Doku.
- `docs/TESTSZENARIEN.md`: TS-89 vorhanden, TS-81.2/81.13/84.14 mit „(seit v1.56.0: …)“ ohne stilles Löschen;
  `git show HEAD:docs/TESTSZENARIEN.md` enthält 0 CR (LF). Version 1.56.0 in beiden `AppVersion.cs`, Changelog-Karte v1.56.0.

**Befunde:**
- Critical: keine. Important: keine.
- **M1 (Minor, behoben `d3746373`):** CSV-Hilfe „**Kein Knopf** gibt es in Baumansichten …“ → „**Keinen Knopf** gibt es …“
  (Grammatik). Build + alle Tests danach erneut grün (Zahlen oben).
- **M2 (Minor, offen, Zusatzfund des Dev-Laufs bestätigt):** Spalten-Einstellungs-Anzeigenamen `ColumnDefinitions.cs:377/402`
  noch „Beschichtungsauftrag (IDEAL)“ / „Beschichtungsauftrag — Summiert (IDEAL)“ (sichtbar unter Benutzer bearbeiten) —
  Folge-Spec bei Bedarf.
- **M3 (Minor, akzeptiert):** Kurzanleitung Schritt 5 zitiert den Knopf mit verschachtelten gleichen Anführungszeichen
  („Auf „Übergeben“ setzen“) — entspricht wörtlich dem Knopftext. Übernommene Altsätze behalten Umschreibungen
  („Hoehe“, „Arbeitsgaenge“), wie die Spec es erlaubt.

### 3. Nachweisart je AK

| AK | Nachweis |
|---|---|
| 1, 2 | Wächter `Navigation_EinDropdownBeschichtung_…`, `Navigation_KeineTopLevelLinksMehrAufBeschichtung` (Markup) + Manual-UAT TS-89.1 |
| 3 | Bedingung wortgleich (Wächter) + `CurrentUserServiceRoleTests` (Rollenlogik); Laufzeit-Sichtbarkeit **Manual-UAT** TS-89.3–.7 |
| 4 | Active-Ausdruck per Wächter; Laufzeit-Markierung **Manual-UAT** TS-89.2 |
| 5 | keine Filteränderung (Diff); **Manual-UAT** TS-89.5/.6 |
| 6 | **Manual-UAT** TS-89.8 (Tastatur, Mobil, Kontrast) |
| 7, 8, 9 | Wächter `Hilfe_KarteBeschichtung_…` (Struktur, Altabschnitte weg) + QA-Abgleich oben; Inhalt **Manual-UAT** TS-89.10 |
| 10, 11 | Wächter `Hilfe_KuerzelUndCsv_BeginnenMitNummeriertenSchritten` + **Manual-UAT** TS-89.11 |
| 12, 13 | QA-Review (oben) + **Manual-UAT** TS-89.10 |
| 14 | Wächter `Vorschlagslisten_TitelVorschlagsliste_DruckUnveraendert` + **Manual-UAT** TS-89.9 |
| 15 | Build-/Test-Ausgabe oben |
| 16, 17 | Diff geprüft (AppVersion ×2, Changelog, TS-89, TS-81.2/.13, TS-84.14); Testindex zieht der Aufrufer nach |

### 4. Geänderte bestehende Tests

Keine. `BeschichtungsauftraegeViewTests.cs` erhält nur 5 neue Facts (reine Ergänzung, bestehende Facts unverändert).

### 5. CLAUDE.md-Checkliste

Migration/SQL/FreshInstall/AgentJobs: entfällt (keine Schema-Änderung). Audit-Felder: entfällt. Version in beiden
`AppVersion.cs` + Anwender-Changelog: ja. `docs/TESTSZENARIEN.md`: ja (TS-89). Testindex, Brain-Changelog, feature-map:
beim Aufrufer.

## Manuelle Test-Checkliste (Schranke 2)

1. Web aus dem Worktree aufs IDEAL-Testsystem publishen (Befehl im Deploy-Abschnitt); Version 1.56.0 sichtbar.
2. TS-89.1: als L anmelden — Menü „Beschichtung“ zwischen „FA-Struktur“ und „Vormontage“, aufgeklappt genau
   „Vorschlagsliste“ und „Aufträge“, keine Trennlinie; beide öffnen die richtige URL.
3. TS-89.2: Vorschlagsliste, Umschalter „Liste“, Aufträge, Auftragsdetail, (S) Reklamationsseite → Toggle markiert;
   FA-Struktur, FA-Liste → nicht markiert.
4. TS-89.3/.4: S und L sehen dasselbe Dropdown; Schreib-Knöpfe nur bei S.
5. TS-89.5: N (ohne Beschichtungsrolle) — kein Menü (auch im Hamburger-Menü), Direktaufrufe → AccessDenied.
6. TS-89.6: `FaHierarchyBeschichtungAktiv = false` → Menü weg für A/S/L, Seiten gesperrt; wieder `true` → zurück.
7. TS-89.7: AKE-Testsystem → kein Menü „Beschichtung“, Leiste sonst unverändert.
8. TS-89.8: nur Tastatur (Tab, Enter, Pfeile, Esc); schmales Fenster → Hamburger → Dropdown bedienbar; Kontrast wie
   „Bestellungen“.
9. TS-89.9: Überschrift + Browser-Tab beider Vorschlagslisten „Vorschlagsliste Beschichtung (IDEAL)“; Druck/PDF weiter
   „Beschichtungsauftrag …“.
10. TS-89.10: Hilfe → Karte „Beschichtung (IDEAL)“ direkt nach „Fertigungsaufträge“; Kurzanleitung mit S einmal komplett
    nachgehen (anlegen → Entwurf speichern → Übergeben → Drucken/PDF → Erledigt), Knopf-/Menütexte stimmen; Strg+F
    „Menuepunkt Beschichtungsauftrag“ ohne Treffer; Rollen-Schlüssel nur unter „Einrichtung (Admin)“.
11. TS-89.11: Hilfe Kürzel und CSV beginnen mit nummerierten Schritten; FA-Struktur (Tooltip, Filter `ka`, Knopf
    Arbeitsgänge) und FA-Liste-CSV mit Filter + ausgeblendeter Spalte nachvollziehen; v1.55-Statuspunkte nicht doppelt.
12. TS-89.12: Regression TS-81.2 (neuer Menüweg), TS-84.14, TS-86.4; übrige Menüs unverändert.
