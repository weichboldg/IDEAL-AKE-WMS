---
type: aufgabe
status: offen
created: 2026-11-23
quelle: Gerald, Chat 2026-11-23 („alles was spec ok ist mittels goal run und qa etc verifizieren“)
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
---
# /goal-Lauf 2026-11-23 abends: Mengenfelder, Tabellen, Beschichtung, NAN-Hinweis, Menge IST (v1.99.54–1.99.58)

Basis `b7e2d5a4` (v1.99.53 + Nachbesserung Laufzeit Lagerbestellung). QA prüft die Nachbesserung mit (TS-178.12).
Übersicht: [[2026-11-23-mengenfelder-tabellen-beschichtung-kommissionierung-uebersicht]]. Keine SQL-Nummer nötig.
Grenzen: nichts an Sage schreiben, keine Einstellungen auf Prod, kein Merge, kein Push.

## Übersicht

| Spec | Typ | SQL | Version | TS | Status | Commits | Blocker |
|---|---|---|---|---|---|---|---|
| [[2026-11-23-mengenfelder-tabellen-beschichtung-kommissionierung-teil-1-spec]] | Bug | – | 1.99.54 | TS-179 | Testbereit | `96b60901` | |
| [[2026-11-23-mengenfelder-tabellen-beschichtung-kommissionierung-teil-2-spec]] | Spec | – | 1.99.55 | TS-180 | Testbereit | `f2fb8b43` | |
| [[2026-11-23-mengenfelder-tabellen-beschichtung-kommissionierung-teil-3-spec]] | Bug | – | 1.99.56 | TS-181 | Testbereit | `01a6429a` | |
| [[2026-11-23-mengenfelder-tabellen-beschichtung-kommissionierung-teil-4-spec]] | Bug | – | 1.99.57 | TS-182 | Testbereit | `8bac07bd` | |
| [[2026-11-23-mengenfelder-tabellen-beschichtung-kommissionierung-teil-5-spec]] | Spec | – | 1.99.58 | TS-183 | Testbereit | `0f853eed`, QA `b79a4b1a` | 183.2 HTTP 400 offen (Mensch)  |
| Nachbesserung v1.99.53 `b7e2d5a4` ([[2026-11-23-lagerbestellung-verbrauchsbuchung-bei-lieferung-spec]]) | QA | – | 1.99.53 | TS-178.12 | QA ok (Testbereit) | `b7e2d5a4` | |

**Nicht im Lauf (Blocker beim Menschen, unverändert):**
- [[2026-11-08-oseon-teileverfolgung-doppelter-klickhandler-spec]] – Schritt 1 „Nachstellen mit Kamera“ offen (Mensch).
- [[2026-11-16-fa-struktur-ideal-ueber-sdata-spec]] – E3/E4 Abnahme-Blocker R2/R4/R5 (Fachseite, Leserecht DEV-Dienstkonto).

## Protokoll

- 2026-11-23: Lauf gestartet, Vorprüfung: Teile 1–5 `Freigegeben`, worktree leer, Freigabe-Antworten vollständig (Gerald 2026-11-23),
  keine BLOCKER. Status → InUmsetzung.
- 2026-11-23: Teil 1 `96b60901` (frontend-design geladen): `site.js` delegierter Handler (pointerdown/focusin → step="any", focusout zurück,
  wheel → blur). Nur Browser-Verhalten, `node --check` ok.
- 2026-11-23: Teil 2 `f2fb8b43`: `supportsReorder` true in 7 Views + Katalog, Kommentar Receive, `ViewKey_Registriert` vergleicht jetzt
  `#view-config.supportsReorder` mit dem Katalog (alle Views gleich, 31 Tests grün).
- 2026-11-23: Teil 3 `01a6429a`: Status-Knöpfe (außer Storno) `form="entwurf-form" name="ziel"` bei bearbeitbarem Auftrag; `Bearbeiten(…, ziel)`
  speichert, dann `StatusWechselnAsync` mit `r.Auftrag.RowVersion`; Teilerfolg = „Gespeichert.“ + Warnung. `beschichtung-bemerkung.js` fragt
  bei Druck/PDF auch nach ungespeicherten entwurf-form-Daten (Rückfrage 2). Hilfe Kurzanleitung. 4 neue Controller-Tests.
- 2026-11-23: Teil 4 `8bac07bd`: `VorschlagHinweis(…, erlaubteIds)` nennt Bestand auf nicht wählbaren Plätzen (max. 3, +n). Hilfe Lagerbestellung.
- 2026-11-23: Teil 5 `0f853eed`: Spalte `ist-menge` „Menge IST“ in `Picking/Bom.cshtml` (th/td/#column-config, nur `Model.Auftrag != null`),
  `ColumnDefinitions.Bom`, Drift-Test. Hilfe Kommissionierung.
- 2026-11-23: Build 0 Fehler; Tests Web 3480 + 1 übersprungen, Service 790. Testindex TS-179–183 nachgezogen. Nächster Schritt: qa-agent.
- 2026-11-23 18:05: qa-agent: Build 0 Fehler, Tests Web 3480/1/0, Service 790/0/0; DEV AKE + IDEAL v1.99.58 (Web+Dienst, Exit 0, Footer ok);
  Review `1cfb92d7..HEAD` mit einem Befund (Leerzeilen-colspan Menge IST) → QA-Fix `b79a4b1a`; DEV-Tests TS-178.12, 179, 180.1, 181, 182, 183.1 OK,
  TS-183.2 Umbuchen → HTTP 400 mit Browser-Dialog (Ursache offen, Mensch); Publish `<worktree>\publish` v1.99.58 (Commit b79a4b1a). Teile 1–5 Testbereit.
  Offen beim Orchestrator: Brain-Changelog + feature-map 1.99.54–1.99.58, Doku-Nachzug, Paket-ZIP auf Shares.
- 2026-11-23: TS-183.2-Abbruch (HTTP 400) untersucht: Session-/Antiforgery-Cookie trug Schlüssel `2d88eb0d…` aus `D:\Dev\IDEAL\keys` →
  Cookie-Kollision 8081/8082 (Cookies kennen keine Ports), kein Code-Befund. Nachtrag in [[2026-10-29-dev-umgebung-lokal]]. 183.2 im UAT wiederholen.
- 2026-11-23: Brain-Changelogs `2026-11-23-v1-99-54…58-*`, feature-map (5 Zeilen), Testindex TS-179–183.
- 2026-11-23: Doku `a536de12` (Worktree): Kapitel 01 (Spalte „Menge IST“), 03 (Hinweis Sage-Plätze), 10 (Umsortieren 7 Listen, Mengenfelder
  Pfeil/Mausrad), 15 (Statusknopf speichert mit), README-Stand + Tabelle, betrieb 00-index Stand v1.99.58 / Paket 14.
  **Fotos ausstehend:** Chrome-Tab hing an einem Browser-Dialog aus dem QA-Lauf; Foto 01-09 zeigt noch die Ist-Menge unter Soll (Vermerk im Kapitel).
- 2026-11-23: Paket `publish\paket14-v1.99.58.zip` (Publish-Commit `b79a4b1a`, Doku-Commit danach ändert keine Binärdateien),
  237 041 687 Bytes, SHA-256 `986B3A57D9E205FF52BCA5B606E2895EF326B8A8D4019ACA21D53CB25F5E5DE8`.
  - `\\AKENET01.ake.at\Installsourcen`: Größe + SHA-256 gleich ✔
  - `\\IDEALWEB01.ideal.ideal-ake.at\InstallSourcen`: Größe + SHA-256 gleich ✔

- 2026-11-23 abends: **Nachtest im Browser (Chrome neu gestartet), DEV AKE 8081, nur 8081 offen (keine Cookie-Kollision):**
  - TS-183.2 ✔ Auftrag 47 (KO-2026-00014, FA 2607170): Pos. 00725315 Ist 1,5 → Wagen 2, `PickingOrderLines` 2184 IstMenge 1.500, Bewegung 19595;
    nach Neuladen Zelle „Menge IST“ 1,500 ohne Eingabe. (Auftrag 49 lehnt ab: „Artikel He266522 ist im WMS unbekannt“ – Testdaten, kein Befund.)
  - TS-183.3 ✔ Ist 0 / 4 (Soll 2) / „abc“ → jeweils Meldung, keine Bewegung.
  - TS-183.4 ✔ Spalte aus-/einblenden bleibt nach Neuladen; „Menge IST“ vor „Menge“ gezogen → Buchung Pos. 00725321 Ist 1 (Zeile 2206, Bewegung 19596).
  - TS-183.5 ✔ FA-Stückliste ohne Auftrag (`/Picking/Bom/143`): keine Spalte, kein `#column-config`-Eintrag, keine `.ist-menge`; CSV-Wert `data-csv-value` 1,500/1,000.
  - TS-181 ✔ BA-2026-00001 (Entwurf, ohne Dienstleister): Menge 1→2, „Auf Übergeben setzen“ → „Gespeichert.“ + Warnung „Dienstleister fehlt“, Status bleibt Entwurf,
    DB Menge 2.0000 mit `ModifiedAt/By`; danach über „Speichern“ auf 1 zurückgesetzt.
  - TS-182 ✔ (Zweig „Kein Bestand – Vorschlag NAN“, Lagerbestellung #70 bei Schalter an). Zweig „nur nicht wählbare Plätze“ nur per Unit-Test/QA belegt (Testdaten fehlen).
    Schalter auf DEV AKE nur kurz an, danach Zeile wieder entfernt (Ausgangszustand: Zeile fehlte).
    Beobachtung: Ein SQL-Insert in `AppSettings` greift erst nach bis zu 2 min bzw. nach Speichern in /Settings (`CachedSettingRepository`, null wird mitgecacht) – erwartetes Verhalten, kein Befund.
  - DEV-Rest: Auftrag 47 jetzt In Arbeit, Pos. 00725315 (1,5) und 00725321 (1) auf Wagen 2; Spalten-Präferenz Stückliste zurückgesetzt;
    Spaltenfilter „Bezeichnung 1 = Verdampfer“ ist **Standard-Vorbelegung** der Stückliste (bomDefaultFilterConfig), kein QA-Rest.
  - Nicht im Browser nachgetestet: TS-179.2/179.4/179.5 (Schritt/Dialoge/Ganzzahl), TS-180.1–180.4 (weitere Listen, WE-Buchung nach Umsortieren), TS-182.3 (manipulierter POST), IDEAL 8082 → UAT.
- 2026-11-23 abends: Fotos `36a6f518` (Worktree): 01-09 neu (Spalte „Menge IST“, Vermerk entfernt), 10-06 Spalten verschieben, 15-07 Entwurf mit Statusknöpfen. Paket 14 unverändert gültig (nur Doku).
- 2026-11-23 spät: **Testprotokoll abgearbeitet (Auftrag Gerald) + v1.99.59 Hover hellblau** (`b139ff5b`, TS-184), DEV AKE + IDEAL auf v1.99.59.
  - Deploy-Panne: `dev-deploy.ps1 -Standort AKE` ohne `-Quelle` veröffentlicht **main** → HTTP 500, Site gestoppt; sofort mit `-Quelle <Bündel-Worktree>` neu, beide 200.
    Merke: bei AKE immer `-Quelle` angeben (Skript-Default AKE = main). Dienste AKE/IDEAL DEV stehen (Starttyp manuell, nicht gestartet).
  - Tests: 3480 Web + 790 Service grün.
  - TS-184.1 ✔ Hover #A1D9F7 (BelID 363221) · 184.2 ✔ Fokus-Balken bleibt (Fokus-Zeile selbst grau → Rückfrage) · 184.3 ✔ `table-danger` bleibt rot („abc“ → „Nichts gebucht“, keine Bewegung) · 184.4 ✔ Offene Bestellungen weiter `rgba(0,0,0,.075)`.
  - TS-180.1 ✔ `#view-config.supportsReorder` true in Artikel, Lagerplätze, Kreditoren, Debitoren, Offene Bestellungen, Bestellvorschläge, WE; Griffe im Zahnrad (Artikel 29).
  - TS-180.2 ✔ BelID 363197, Ist-Menge vor Artikel: EK019894 2 → `1;1;1`, EK023404 1 → `asdfasdf`, EK023400 1 → `QA173-L` (Bewegungen 19597–19599, Sage-Schalter DEV alle aus, Dienst steht). Präferenz danach gelöscht.
  - TS-180.3 ✔ Artikel (Matchcode nach vorn, Filter Matchcode, CSV-Kopf in neuer Reihenfolge) und Offene Bestellungen (Artikel nach vorn, Filter EK02: 25 Zeilen, CSV 224 Zeilen alle EK02). Präferenzen gelöscht.
  - TS-180.4 ✔ OSEON Zahnrad ohne Griffe; Einbuchung Mehrfach ohne Zahnrad.
  - TS-179.1/179.2 ✔ 2,5 + Pfeil → 3,5; `step` im Fokus `any`, danach `0.001`; `1,0005` → stepMismatch (Browser weist ab). 179.4 ✔ Stückliste Dialog Lagerbestellung: Pfeil 1 → 2, Mausrad ohne Wirkung, Fokus weg. 179.5 ✔ Lagerbestellung #70 Ganzzahlfeld Pfeil ±1 (`step=1`), keine Konsolenfehler (Lagerbestellung, Startseite, Stückliste).
  - Nicht live: TS-179.4 BDE-Terminal-Gruppenmeldung, TS-182.3 manipulierter POST (Unit-Test `PlatzAusserhalbDerListe_UndSagePlatzOhneSageBuchung_WerdenAbgewiesen_AK4`). IDEAL 8082 nur lesend (v1.99.59, Seiten 200, Hover-Regel, Reorder Artikel).
  - v1.99.59 ohne `qa-agent`-Lauf (CSS-Zweizeiler auf Zuruf); Paket 14 enthält v1.99.59 **nicht** – neues Paket erst auf Anforderung.
    → nachgeholt, siehe „QA v1.99.59 (qa-agent)“.

## QA v1.99.59 (qa-agent)

2026-11-23, Worktree `feature/2026-08-07-ideal-teile-1-5`, Commits `b139ff5b` + `c9624562` (Diff gegen `36a6f518`). Keine Spec, kein Statuswechsel.

**1 Build/Tests:** `dotnet build IdealAkeWms.slnx` 0 Fehler (30 Warnungen, Bestand). `dotnet test`: IdealAkeWms.Tests 3480 bestanden / 1 übersprungen / 0 Fehler (3481);
IDEALAKEWMSService.Tests 790 / 0 / 0.

**DEV-Deploy** (`dev-deploy.ps1 -Quelle <Bündel-Worktree> -OhneDienst`, erhöhte Shell): AKE Exit 0, „HTTP 200, Fusszeile v1.99.59“ (8081);
IDEAL Exit 0, „HTTP 200, Fusszeile v1.99.59“ (8082). Dienst bewusst nicht (nur CSS; `-OhneDienst` laut Auftrag).

**2 Review** (site.css-Regel, Wächter, Version, Changelog, Doku):
- Spezifität: `.table.we-positionen` (0,2,0) schlägt Bootstraps `.table` (0,1,0) unabhängig von der Ladereihenfolge. `table-danger` (5.3.3) setzt
  `--bs-table-hover-bg`/`--bs-table-active-bg` am `tr` → Zellen erben vom näheren Vorfahren, Rot bleibt (gemessen, s. u.). Hover auf der Fokus-Zeile selbst
  zeigt die Fokus-Farbe (`tr:focus-within`-Regel gleich spezifisch wie `.table-hover … tr:hover`, steht später) – gewollt, Balken bleibt.
- Kontrast (WCAG, berechnet): Fließtext #212529 auf #A1D9F7 **10,1:1**, auf #D9F4FF **13,5:1**; `text-body-secondary` (rgba(33,37,41,.75) gemischt) 5,3:1 bzw. 6,3:1;
  Fokus-Balken #052C65 8,9:1 bzw. 11,8:1. Alles ≥ 4,5:1.
- **Befund B-1 (niedrig, nicht behoben, gemeldet):** Links im Block „Offener Bedarf zu diesem Artikel“ (`Receive.cshtml`, Bootstrap-Linkfarbe #0D6EFD) liegen auf
  Hover-Hellblau bei **2,95:1**, auf der Fokus-Zeile bei 3,93:1 (vorher Grau-Hover 3,81:1, Weiß 4,5:1) – unter AA. Tritt nur in Zeilen mit offenem Bedarf auf;
  Hover ist flüchtig, die Fokus-Zeile nicht. Vorschlag (Entscheid Gerald): `.we-positionen a { color: var(--bs-primary-text-emphasis); }` (#052C65, 8,9:1).
- **Befund B-2 (Doku, niedrig):** Bildunterschrift 05-06 in `docs/anwender/05-wareneingang-zu-bestellung.md` sagt noch „grau“; Foto stammt aus 1.99.53. Foto neu gegen DEV.
- Wächter `ViewKeyDriftGuardTests.WareneingangPositionen_…`: nur Erwartung erweitert (2 → 3 Regeln, dritte prüft beide Variablen) – Verhaltensänderung gewollt (TS-184), kein Fixture-Biegen.
- Sonst keine Befunde: keine Script-/`hidden`-/Binder-/Fallback-Themen berührt; keine Migration, keine Einstellung (ADR 0020 n/a), keine SQL; Version in beiden `AppVersion.cs`,
  Anwender-Changelog-Karte, Brain-Changelog `[[2026-11-23-v1-99-59-wareneingang-hover-hellblau]]`, TS-184 in `docs/TESTSZENARIEN.md` + Testindex Zeile 184 vorhanden.

**3 Nachweisart:** Regel-Existenz automatisch (Wächter-Test); Farbwirkung, Spezifität, `table-danger` = Manual-UAT (Browser, hier auf DEV AKE erbracht).

**6c TS-184 auf DEV AKE** (BelID 363221, Pixel aus gespeicherten Screenshots gemessen, `C:\Users\GERALD~1.WEI\AppData\Local\Temp\claude-chrome-screenshots-ZoOUCf\`):
| Nr | Ergebnis | Status |
|---|---|---|
| 184.1 | Hover Zeile 3 #A1D9F7; ohne Hover weiß #FFFFFF / gestreift #F2F2F2 unverändert (`…-4.png`) | OK |
| 184.2 | Fokus Zeile 2 (Ist-Menge) #D9F4FF mit Balken links, gleichzeitig Hover Zeile 4 #A1D9F7 – unterscheidbar (`…-5.png`); Fokus in roter Zeile (Memo) #DFC2C4 = rot, Balken da, Hover Zeile 3 #A1D9F7 (`…-7.png`) | OK |
| 184.3 | „abc“ + Einbuchen → „Nichts gebucht …“, Zeile 1 `table-danger`; Hover #E5C7CA (Bootstrap-Rot-Hover), kein Blau (`…-6.png`) | OK |
| 184.4 | Beschichtungsaufträge (`table-hover`): Hover #ECECEC grau (`…-9.png`). Hinweis: Offene Bestellungen und Artikel haben gar kein `table-hover` – TS-184.4 nennt besser eine `table-hover`-Liste | OK |
DEV-Rest: keine Bewegung gebucht (Fehlerzeile). IDEAL 8082: Deploy + Fußzeile geprüft, Browser-Schritte nicht wiederholt (gleiche CSS-Datei; Cookie-Kollision).

**7 Publish:** nicht ausgeführt – laut Protokoll oben neues Paket erst auf Anforderung (Paket 14 = v1.99.58). Offen für den Menschen.

**Manuell offen (Mensch):**
1. B-1 entscheiden (Linkfarbe in WE-Positionen) – nur relevant mit offenem Bedarf in einer Zeile.
2. TS-184.1–184.3 einmal am Terminal (Lichtverhältnisse) und auf DEV/Test IDEAL ansehen.
3. Foto 05-06 neu (B-2), Paket mit v1.99.59 bei Bedarf anfordern.

## Geänderte Kapitel (für spätere Confluence-Übernahme)

- Anwender: 01 Kommissionierung, 03 Lager-/Glasbestellung, 10 Bestände und Spaltenfilter, 15 Beschichtungsauftrag, 05 Wareneingang zu Bestellung (v1.99.59), README (Stand/Tabelle)
- Betrieb: 00-index (Stand)

## QA v1.99.59 Anmeldemaske (qa-agent)

Auf Zuruf Gerald, keine eigene Spec. Geprüft: Commit `b5354e51` (Diff gegen `c9624562`) im Worktree `C:\Git\IDEAL-AKE-WMS\.claude\worktrees\2026-08-07-ideal-teile-1-5`.
Geändert: `IdealAkeWms/Views/Account/Login.cshtml` (+1 Zeile), `Views/Help/Changelog.cshtml`, `docs/TESTSZENARIEN.md` (TS-184.5 neu, 184.4 präzisiert), `docs/anwender/13` + `README.md`.

**1 Build + Tests (2026-11-23):** `dotnet build IdealAkeWms.slnx` 0 Fehler (30 Warnungen, Bestand). `dotnet test`:
IdealAkeWms.Tests 3480 bestanden / 1 übersprungen / 0 Fehler (3481); IDEALAKEWMSService.Tests 790 / 0 / 0.

**6b DEV-Deploy** (`dev-deploy.ps1 -Quelle <Worktree> -OhneDienst`, Dienst bewusst nicht mit, nur View geändert):
AKE Exit 0, „HTTP 200, Fusszeile v1.99.59“, DEV-Absicherung gelaufen. IDEAL Exit 0, „HTTP 200, Fusszeile v1.99.59“, DEV-Absicherung gelaufen. Footer = AppVersion 1.99.59 (Web + Service).

**2 Review:**
- Platzierung: Zeile 139 steht nach dem schließenden `}` des `if (Model.KennwortFestlegen) … else`, innerhalb `.login-body` → in beiden Zweigen sichtbar. Kein Script-Block, kein `@Html.Raw`, Razor-Encoding des Konstanten-Strings.
- Kontrast: Bootstrap 5.3.3, `.text-muted` = `--bs-secondary-color` rgba(33,37,41,.75) auf Weiß ≈ #595C5F → **6,7:1** (AA erfüllt, auch für `small`).
- Anonym erreichbar / Preisgabe: Die Seite zeigt nur die Produktversion (Konstante `AppVersion.Version`), keine Standort-, Server-, DB- oder Benutzerangaben. Neu ist, dass die Version **ohne Anmeldung** lesbar ist (Fußzeile nur nach Anmeldung). Bewertung: geringes Fingerprinting-Risiko, Anwendung nur im Intranet, Version ist ohnehin Teil der Paketnamen/Shares – **akzeptiert, kein Befund**; wer das nicht will, entscheidet Gerald.
- **Q-1 (Brain, niedrig, nicht behoben):** Testindex Zeile 184 nennt „TS-184.1 – 184.4“, nicht 184.5; Brain-Changelog `[[2026-11-23-v1-99-59-wareneingang-hover-hellblau]]` erwähnt die Anmeldemaske nicht. Nachziehen (Auftrag erlaubte hier nur diese Notiz).
- **Q-2 (Doku, niedrig):** `docs/anwender/README.md` Stand-Satz führt Kapitel 13 jetzt bei 1.99.59 **und** noch bei 1.99.43 (gleiches Muster wie 01/15 bei 1.99.58/1.99.43 – wohl Text vs. Fotos, aber ungesagt). Optional „(Fotos)“ ergänzen.
- Sonst keine Befunde: keine Migration, keine Einstellung (ADR 0020 n/a), keine SQL, keine geänderten Tests.

**3 Nachweisart:** TS-184.5 = Manual-UAT (Razor-View, kein Test); hier im Browser auf DEV erbracht (Login-Zweig). Kennwort-ändern-Zweig nur per Code-Lesung belegt.

**4 Geänderte Bestandstests:** keine.

**6c TS-184.5 auf DEV** (eigener Tab, nicht angemeldet, nichts abgeschickt):
| Nr | Ergebnis | Status |
|---|---|---|
| 184.5 AKE | http://localhost:8081/Account/Login: unten in der weißen Karte grau „IDEAL-AKE WMS v1.99.59“ unter „Mit Windows anmelden“ (`C:\Users\GERALD~1.WEI\AppData\Local\Temp\claude-chrome-screenshots-ZoOUCf\screenshot-1795458990226-10.jpg`) | OK |
| 184.5 IDEAL | http://localhost:8082/Account/Login: gleiche Zeile v1.99.59 (`…\screenshot-1795458995919-11.jpg`; Felder vom Browser-Autofill vorbelegt, nicht abgeschickt) | OK |
| 184.5 Kennwort-ändern-Ansicht | nur nach Anmeldung mit vom Admin vergebenem Kennwort erreichbar – nicht ausgeführt (keine echten Kennwörter) | NICHT MOEGLICH |

**7 Publish:** nicht ausgeführt (neues Paket nur auf Anforderung, wie oben).

**Manuell offen (Mensch):**
1. TS-184.5 Kennwort-ändern-Ansicht: Benutzer mit Admin-Kennwort anmelden → Versionszeile unten sichtbar.
2. Q-1/Q-2 nachziehen (Testindex 184.5, Brain-Changelog, README-Stand-Satz).
3. Entscheiden, ob Version anonym sichtbar sein darf (QA: vertretbar).

## QA v1.99.59 Linkfarbe + Publish (qa-agent)

Auftrag: Nachbesserung zu QA-Befund B-1 (von Gerald freigegeben), Commit `41f6ee1f` (Diff gegen `b5354e51`), keine eigene Spec.
Geändert: `IdealAkeWms/wwwroot/css/site.css` (neue Regel `.we-positionen a:not(.btn) { color: var(--bs-primary-text-emphasis); }`),
`IdealAkeWms.Tests/Views/ViewKeyDriftGuardTests.cs` (4 statt 3 Regeln, Regel 4 prüft die Farbe), `docs/TESTSZENARIEN.md` (TS-184.6), `Views/Help/Changelog.cshtml`.

**1 Build + Tests** (Worktree, HEAD `41f6ee1f`): `dotnet build IdealAkeWms.slnx` 0 Fehler (30 Warnungen, bekannt).
`dotnet test`: IdealAkeWms.Tests 3480 bestanden / 1 übersprungen / 0 Fehler; IDEALAKEWMSService.Tests 790 / 0 / 0.

**6a/b DEV-Deploy** (`dev-deploy.ps1 -Quelle <Bündel-Worktree> -OhneDienst`, Dienste wie zuvor nicht gestartet):
AKE „HTTP 200, Fusszeile v1.99.59 … Fertig: AKE v1.99.59 auf http://localhost:8081/“, Exit 0; IDEAL dito auf 8082, Exit 0.

**2 Review** (`b5354e51..41f6ee1f`):
- Spezifität `.we-positionen a:not(.btn)` = (0,2,1) schlägt Bootstraps `a { color: rgba(var(--bs-link-color-rgb),…) }` (0,0,1); `a:hover` setzt in Bootstrap
  nur die Variable `--bs-link-color-rgb`, die Regel hier eine feste Farbe → beim Überfahren kein Farbwechsel, Unterstreichung bleibt (Bedeutung nicht nur über Farbe). Unkritisch.
- `.btn` ausgenommen; `.link-*`-Hilfsklassen (0,1,0 mit `!important`) würden weiter gewinnen – in der Tabelle keine verwendet. `.dropdown-item` (0,1,0) würde überstimmt,
  die WE-Tabelle hat aber weder Spaltenfilter noch Dropdowns; kein JS fügt `<a>` in diese Tabelle ein (nur `csv-export.js`/`print-visible-columns.js` erzeugen Anker, außerhalb).
- Betroffene Links: genau die zwei im Block „Offener Bedarf zu diesem Artikel“ (`Receive.cshtml` Z.162 Lagerbestellung, Z.167 Bedarfsmeldung). Beide gewollt; kein weiteres `<a>` in `tbody`/`thead` (Laufzeit: einziger Link in der Tabelle „Nr. 41“).
- Kontrast #052C65 (WCAG-Formel nachgerechnet): Weiß 13,51:1 · Streifen #F2F2F2 12,07:1 · Hover #A1D9F7 8,87:1 · Fokus #D9F4FF 11,79:1 ·
  table-danger #F8D7DA 10,11:1 / Streifen #ECCCCF 9,07:1 / Hover #E5C7CA 8,59:1 / Aktiv #DFC2C4 8,14:1. Vorher #0D6EFD auf #A1D9F7 2,95:1.
- Kein Dark-Mode im Projekt (`data-bs-theme` nirgends in Views) → Variable bleibt #052C65.
- Wächter-Test: Regel 4 prüft die Farbe; `OnlyContain(!background)` gilt weiter.
- **Befunde: keine.** (Untersucht: CSS-Diff, Receive.cshtml Tabelle Z.118–262, Bootstrap-Linkregeln, JS-Anker-Erzeuger, Wächter-Test.)

**3 Nachweisart:** Farbregel per Wächter-Test (statisch, CSS-Text); tatsächliche Darstellung nur Manual/Browser (unten).

**4 Geänderte Bestandstests:** `ViewKeyDriftGuardTests` (WE-CSS-Regeln) – Anzahl 3→4 und neue Prüfung Regel 4: Verhalten absichtlich geändert (B-1). Kein Fixture angepasst.

**6c TS-184.6 auf DEV AKE** (eigener Tab, nichts gebucht; Fokus nur ins Positionsmemo gesetzt, nichts abgeschickt):
Daten per SQL gefunden: `SagePurchaseOrderLines` × `WarehouseRequisitionItems` → BelId 363134 (Bestellung 2027-168939, Pos. 1 EK005782, Lagerbestellung Nr. 41).
Kein JS-Werkzeug im Browser verfügbar → statt `getComputedStyle` Pixelmessung in den gespeicherten Zoom-Screenshots (PNG, häufigste dunkle Farbe im Bereich „Nr. 41“):
| Nr | Zustand | Link-Pixel | Hintergrund | Screenshot | Status |
|---|---|---|---|---|---|
| 184.6a | ruhend (Streifenzeile) | #052C65 | #F2F2F2 | `C:\Users\GERALD~1.WEI\AppData\Local\Temp\claude-chrome-screenshots-ZoOUCf\screenshot-1795459924054-12.png` | OK |
| 184.6b | Hover | #052C65 | #A1D9F7 | `…\screenshot-1795459926205-13.png` | OK |
| 184.6c | Fokus-Zeile (focus-within) | #052C65 | #D9F4FF | `…\screenshot-1795459946790-14.png` | OK |
| 184.6d | table-danger-Zeile mit Link | nicht hergestellt (bräuchte Fehlbuchung) – nur rechnerisch 8,14–10,11:1 | – | – | NICHT MOEGLICH |
| 184.6 IDEAL | 8082 nicht im Browser geprüft (gleiches CSS, Deploy v1.99.59 belegt) | – | – | – | NICHT MOEGLICH |

**7 Publish:** `<worktree>\publish\IDEALAKEWMSWeb` + `IDEALAKEWMSWebService` geleert und neu (`dotnet publish -c Release`, beide Exit 0; Regel im publizierten `site.css` vorhanden),
`SQL\` + `scripts\deploy\` kopiert, `publish-info.txt` Version 1.99.59 / Commit `41f6ee1f5345a8096436b36ebeea404279f7224b` / Erstellt 2026-11-23 19:53.
Paket `publish\paket15-v1.99.59.zip`: 595 Dateien (gleiche Struktur wie Paket 14: 346 Web, 93 Dienst, 150 SQL, 5 deploy, publish-info.txt),
**237 037 743 Bytes, SHA-256 `DB053BCD0747D8B7986E13685A867EE2DFAB9914501389ECDD55BC6D5F4C6858`**. Shares nicht angefasst (Hauptagent).
Deploy-Umfang: nur Web nötig (CSS + Changelog-View), keine Migration, kein Nachlauf-Schritt.

**Manuell offen (Mensch):**
1. TS-184.6 an einer `table-danger`-Zeile mit offenem Bedarf (z. B. „abc“ als Ist-Menge) – Link dunkelblau lesbar.
2. TS-184.6 auf IDEAL 8082 bzw. Testsystem, am Fertigungsterminal ansehen.

## QA v1.99.60 Menü FA-Struktur (qa-agent)

Auftrag: Wunsch Gerald 2026-11-23 auf Zuruf, keine eigene Spec. Commit `bcb7a357` (Diff gegen `f08f9c7c`):
`Views/Shared/_Layout.cshtml` (eigenständiger Punkt „FA-Struktur“ entfernt, als Dropdown-Eintrag unter „Fertigungsaufträge“ nach „FA-Liste“; `ZeigtEines` um `NavZiele.FaStruktur`, Aktiv-Markierung um Controller `FaHierarchy`),
beide `AppVersion.cs` 1.99.60, `Views/Help/Changelog.cshtml`, `docs/TESTSZENARIEN.md` (TS-185). Kein Publish/Paket (Auftrag).

**1 Build + Tests** (Worktree, HEAD `bcb7a357`): `dotnet build IdealAkeWms.slnx` 0 Fehler (30 Warnungen, bekannt).
`dotnet test`: IdealAkeWms.Tests 3480 bestanden / 1 übersprungen / 0 Fehler; IDEALAKEWMSService.Tests 790 / 0 / 0.

**6a/b DEV-Deploy** (`dev-deploy.ps1 -Quelle <Bündel-Worktree> -OhneDienst`, Dienste nicht gestartet):
IDEAL „HTTP 200, Fusszeile v1.99.60 … Fertig: IDEAL v1.99.60 auf http://localhost:8082/“, Exit 0; AKE „Fertig: AKE v1.99.60 auf http://localhost:8081/“, Exit 0.

**2 Review** (`f08f9c7c..bcb7a357`):
- Rechte-Regel unverändert: `NavigationKatalog.cs:265` `"FaListe" or "FaStruktur" => faLesen` (`CanViewProductionOrdersAsync`), kein Standort-/Toggle-Anteil.
  Controller-Filter identisch: `FaHierarchyController.cs:24` und `ProductionOrdersController.cs:13` je `[RequirePickingOrTrackingOrLeitstandAccess]`.
  → Ein Benutzer, der FA-Struktur, aber nicht FA-Liste sieht, ist nicht möglich (eine Regel für beide). Die Erweiterung von `ZeigtEines` um FaStruktur ist daher fachlich redundant, aber konsistent mit dem Wächter (jeder Link hängt an seinem NavZiel).
- Wächter `NavigationSichtTests.Layout_JederMenuelinkHaengtAnSeinemNavZiel_OhneEigeneRechteabfrage` grün; neuer Link steht in `@if (nav.Zeigt(NavZiele.FaStruktur))`.
- Standort: FA-Struktur war und ist auch auf AKE sichtbar (Regel ohne Standort). Auf DEV AKE zeigt die Seite „FA-Struktur (IDEAL)“ mit „Keine Strukturen gefunden“ – Altbestand, nicht durch diesen Commit; nur Hinweis (H-1), kein Befund.
- Aktiv-Markierung: `FaHierarchy` in die `is`-Liste des Dropdown-Toggles aufgenommen; der alte Einzellink samt eigener Aktiv-Logik ist vollständig entfernt (im Browser nur ein FA-Struktur-Menülink gefunden).
- Startseiten-Kachel unverändert (NavigationKatalog nicht im Diff).
- Hilfe/Doku mit Menüweg: `docs/anwender/18-startseite-und-menue.md`, `Views/Help/Index.cshtml`, `README.md` nennen keinen Menüweg zur FA-Struktur (grep) → nichts nachzuziehen. Fotos `docs/anwender/bilder/18-0x` können die alte Menüleiste zeigen – kosmetisch (H-2).
- Klassen aus der QA-Liste (Script-Einbettung, `hidden`+`d-*`, stille Fallbacks, Arrays, nur-Client-Validierung, Mehrfachregel) nicht berührt.
- **Befund B-1:** `secondbrain/tests/testszenarien-index.md` hat keine Zeile für TS-185 (letzte ist 184). Nicht nachgezogen, weil der Auftrag nur diese Notiz zu stagen erlaubt → Hauptagent.

**3 Nachweisart:** Link-an-NavZiel per Wächter-Test (statisch); Reihenfolge/Aktiv-Markierung/Sichtbarkeit nur Browser (unten). 185.5 per Code-Beleg.

**4 Geänderte Bestandstests:** keine.

**5 Checkliste:** Version beide AppVersion.cs + Anwender-Changelog ✓, TS-185 in TESTSZENARIEN ✓, Testindex ✗ (B-1), keine Migration/SQL/Einstellung (ADR 0020 n/a), kein Nachlauf-Schritt.

**6c TS-185 im Browser** (eigener Tab, Admin-Benutzer, nichts gebucht):
| Nr | Standort | Ergebnis | Screenshot | Status |
|---|---|---|---|---|
| 185.1 | IDEAL 8082 | Leiste: Lager, Beschichtung, Vormontage, Fertigungsaufträge, Leitstand, … – kein eigener „FA-Struktur“ | `C:\Users\GERALD~1.WEI\AppData\Local\Temp\claude-chrome-screenshots-ZoOUCf\screenshot-1795460828144-16.png` | OK |
| 185.2 | IDEAL 8082 | Dropdown: FA-Liste, FA-Struktur (FA-Vervollständigen/Abarbeitungsliste dort nicht freigeschaltet) | wie 185.1 | OK |
| 185.3 | IDEAL 8082 | `/FaHierarchy` öffnet „FA-Struktur (IDEAL)“, „Fertigungsaufträge“ hervorgehoben | `…\screenshot-1795460834132-17.png` | OK |
| 185.4 | IDEAL 8082 | Kachel „FA-Struktur“ im Bereich Produktion/BDE nach FA-Liste | `…\screenshot-1795460845988-18.jpg` | OK |
| 185.1/185.2 | AKE 8081 | Kein eigener Punkt; Dropdown: FA-Liste, FA-Struktur, FA-Vervollständigen, FA-Abarbeitungsliste | `…\screenshot-1795460860052-19.png` | OK |
| 185.3 | AKE 8081 | `/FaHierarchy` öffnet, „Fertigungsaufträge“ hervorgehoben (Seite leer, H-1) | `…\screenshot-1795460866544-20.jpg` | OK |
| 185.4 | AKE 8081 | Kachel „FA-Struktur“ im Reiter „Alle“ vorhanden (Accessibility-Baum) | – | OK |
| 185.5 | beide | Kein Benutzer ohne Recht genutzt; per Code: gleiche Regel `faLesen` wie FA-Liste, Dropdown nur bei `ZeigtEines(...)`, `/FaHierarchy` hinter `RequirePickingOrTrackingOrLeitstandAccess` | – | OK (Code-Beleg), Browser NICHT MOEGLICH |

**7 Publish:** laut Auftrag nicht ausgeführt. Deploy-Umfang: nur Web (Layout, Changelog), keine Migration.

**Manuell offen (Mensch):**
1. TS-185.5 mit einem Benutzer ohne FA-Leserecht im Browser.
2. B-1: TS-185 in `secondbrain/tests/testszenarien-index.md` aufnehmen.
3. TS-185.1–185.4 auf dem Testsystem.

## QA v1.99.61 FA-Struktur Titel/Gate (qa-agent)

Auftrag: Wunsch Gerald 2026-11-23 auf Zuruf („Fa-Struktur (ideal) nur auf fa-struktur. Aber wenn das Feature deaktiviert ist, soll es auch nicht angezeigt werden.“), keine eigene Spec. Commit `22c118ff` (Diff gegen `bcb7a357`):
`NavigationKatalog.cs` (Variable `hierarchisch` aus `ProduktionsauftragHierarchisch`, `"FaStruktur" => faLesen && hierarchisch`), `Views/FaHierarchy/Index.cshtml` + `ColumnDefinitions.cs:563` + `Help/Index.cshtml` „FA-Struktur“ ohne „(IDEAL)“, Test `NavigationSichtTests.FaStruktur_NurBeiHierarchischerStruktur`, beide `AppVersion.cs` 1.99.61, Anwender-Changelog, TS-186. Kein Publish/Paket (Auftrag).

**1 Build + Tests** (Worktree, HEAD `22c118ff`): `dotnet build IdealAkeWms.slnx` 0 Fehler (30 Warnungen, bekannt).
`dotnet test`: IdealAkeWms.Tests 3482 bestanden / 1 übersprungen / 0 Fehler (3483); IDEALAKEWMSService.Tests 790 / 0 / 0. Gezielt `NavigationSichtTests`: 66/66 grün.

**6a/b DEV-Deploy** (Shell elevated: True; `dev-deploy.ps1 -Standort <X> -Quelle <Bündel-Worktree> -OhneDienst`, Dienste nicht gestartet):
IDEAL „HTTP 200, Fusszeile v1.99.61 … Fertig: IDEAL v1.99.61 auf http://localhost:8082/“, Exit 0; AKE „HTTP 200, Fusszeile v1.99.61 … Fertig: AKE v1.99.61 auf http://localhost:8081/“, Exit 0.
DEV-Werte (sqlcmd): AKE_WMS_DEV `ProduktionsauftragHierarchisch`=false, `Sync:HierarchicalFaEnabled`=false, FaHierarchyNodes 0; IDEAL_WMS_DEV Master=true, `Sync:HierarchicalFaEnabled`=false, FaHierarchyNodes 755, FaHierarchyOrderInfos 5. Nichts umgeschaltet.

**2 Review** (`bcb7a357..22c118ff`):
- **Richtiger Schalter?** Die Seite liest `FaHierarchyNodes`/`FaHierarchyOrderInfos` (`FaHierarchyController.cs:85-86`). Gefüllt werden sie vom Dienst `FaHierarchySyncService`, gegatet durch `Sync:HierarchicalFaEnabled` (`IDEALAKEWMSService/Workers/SyncWorker.cs:352`, Katalog `ServiceSettingDefinitions.cs:282` „FA-Struktur-Import (IDEAL)“, Standortmaske `StandortSettingsCatalog.cs:65`). Der Controller-Kommentar sagt ausdrücklich, er berühre den Master `ProduktionsauftragHierarchisch` nicht (`FaHierarchyController.cs:17-21`). Der Master ist dagegen der Schalter, den die FA-Links schon heute für „Ziel FA-Struktur“ nutzen (`Views/Shared/_FaLink.cshtml:14`, `StockApiController.cs:80-86`, gelesen in `StockMovements/Index.cshtml:9`, `StockOverview/Index.cshtml:82`).
  → **H-1 (Rückfrage, kein Defekt):** Menü/Kachel folgen jetzt dem Master, die Daten dem Import-Schalter. Fall „Import an, Master aus“ (IDEAL vor der Umstellung Teil 7) blendet eine gefüllte FA-Struktur aus; Fall „Master an, Import aus“ (DEV IDEAL heute) zeigt sie mit Altdaten. Für Gerald: Ist „Feature“ = Master (konsistent mit den FA-Links, so umgesetzt) oder = `Sync:HierarchicalFaEnabled` bzw. beides? Die Umsetzung ist mit dem Master in sich stimmig; die Wahl bleibt fachlich zu bestätigen.
- **B-1 (Befund, niedrig): Direktaufruf bei Schalter aus erreichbar.** `FaHierarchyController` hat nur `[RequirePickingOrTrackingOrLeitstandAccess]` (`FaHierarchyController.cs:24`), kein Gate auf den Master. Browser: `http://localhost:8081/FaHierarchy` auf AKE (Master=false) öffnet „FA-Struktur“ mit „Keine Strukturen gefunden“ (Screenshot unten). ADR 0011 (Zeile 44-46) verlangt den Schalter an drei Stellen inkl. Controller-Zugriff, kumulativ zum Rechtefilter. Nicht umgebaut (Auftrag); Folge wäre ein Gate (z.B. Redirect auf FA-Liste bei Master≠true). Wirkung gering (leere Seite, keine Schreibaktion).
- **Andere Links auf FA-Struktur:** einzige Url-Erzeuger `_FaLink.cshtml:14` und `StockApiController.cs:84` – beide nur bei Master=true auf FaHierarchy, sonst FA-Liste → kein Link ins Leere bei Schalter aus. `FaHierarchy/Index.cshtml:275` (WorkOperations) nur innerhalb der Seite. In `wwwroot/js` keine Links auf `/FaHierarchy`. Beschichtung/Kommissionierlisten/Vormontage verlinken eigene Controller (`FaHierarchyBeschichtung` usw.).
- **Wirkzeit nach Umschalten:** `NavigationSicht` ist scoped (`Program.cs:171`) und cacht nur je Request (`_ergebnis`); `IServiceSettingRepository` = `GuardedServiceSettingRepository` → `ServiceSettingRepository` ohne Cache-Decorator (`Program.cs:89-92`) → wirkt ab dem nächsten Seitenaufruf.
- **H-2 (Hinweis, Mehrfachregel):** Die Master-Auswertung existiert mehrfach; `NavigationKatalog.cs:248` vergleicht ohne `Trim()`, `FaBuchungsText.IstHierarchisch` (`FaBuchungsText.cs:74`) mit `Trim()`. Der einzige UI-Schreibweg schreibt exakt „true“/„false“ (`HierarchieUmstellungController.cs:87`) → praktisch ohne Wirkung; sauberer wäre `FaBuchungsText.IstHierarchisch(...)` auch im Katalog. Nicht angefasst.
- `hierarchisch` wird jetzt für alle Rollen gelesen (vorher nur über `master = admin && …`); Regel `faLesen && hierarchisch` rollenunabhängig – korrekt. Test deckt Admin ab, FA-Liste bleibt unabhängig.
- QA-Defektklassen (Script-Einbettung, `hidden`+`d-*`, stille Fallbacks, Arrays, nur-Client-Validierung) nicht berührt; Mehrfachregel s. H-2.
- **B-2 (Befund, Doku):** `secondbrain/tests/testszenarien-index.md` hat weder TS-185 noch TS-186. Nicht nachgezogen (Auftrag: nur diese Notiz stagen) → Hauptagent.

**3 Nachweisart:** 186.4 (Regel Master an/aus, FA-Liste unabhängig) automatisch per `NavigationSichtTests.FaStruktur_NurBeiHierarchischerStruktur`. 186.1–186.3 (Titel, Menü, Kachel je Standort) Browser.

**4 Geänderte Bestandstests:** keine (nur neuer Test).

**5 Checkliste:** Version beide AppVersion.cs + Anwender-Changelog ✓, TS-186 in TESTSZENARIEN ✓, Testindex ✗ (B-2), keine Migration/SQL. ADR 0020: keine neue/geänderte Einstellung (Master nur gelesen) → n/a. Kein Nachlauf-Schritt.

**6c TS-186 im Browser** (eigener Tab, Admin-Benutzer, nichts gebucht, nichts umgeschaltet; IDEAL dann AKE):
| Nr | Standort | Ergebnis | Screenshot | Status |
|---|---|---|---|---|
| 186.1 | IDEAL 8082 | Tab-Titel „FA-Struktur - IDEAL-AKE WMS“, Überschrift „FA-Struktur“, Baum mit Strukturen; Dropdown „Fertigungsaufträge“: FA-Liste, FA-Struktur | `C:\Users\GERALD~1.WEI\AppData\Local\Temp\claude-chrome-screenshots-ZoOUCf\screenshot-1795461405839-21.jpg`, `…\screenshot-1795461414466-22.png` | OK |
| 186.2 | IDEAL 8082 | Kachel „FA-Struktur“ im Bereich Produktion/BDE nach FA-Liste | `…\screenshot-1795461423083-23.jpg` | OK |
| 186.3 | AKE 8081 | Dropdown: FA-Liste, FA-Vervollständigen, FA-Abarbeitungsliste – kein „FA-Struktur“; Startseite (auch Reiter „Alle“, Accessibility-Baum) ohne FA-Struktur-Kachel/-Link | `…\screenshot-1795461433340-24.png` | OK |
| 186.3 neg. | AKE 8081 | Direktaufruf `/FaHierarchy` öffnet weiterhin „FA-Struktur“, „Keine Strukturen gefunden“ | `…\screenshot-1795461438724-25.jpg` | Befund B-1 |
| 186.4 | – | Unit-Test grün (s. 1) | – | OK |

**7 Publish:** laut Auftrag nicht ausgeführt. Deploy-Umfang: nur Web (NavigationKatalog, Views, ColumnDefinitions); Dienst nur Versionsnummer; keine Migration.

**Bewertung:** Build/Tests grün, DEV beide Standorte v1.99.61, TS-186.1–186.4 OK. Offen: H-1 (Schalterwahl fachlich bestätigen), B-1 (Controller-Gate fehlt, ADR 0011), B-2 (Testindex).

**Manuell offen (Mensch):**
1. H-1 entscheiden: Master `ProduktionsauftragHierarchisch` oder `Sync:HierarchicalFaEnabled` als „Feature“ für Menü/Kachel.
2. B-1 entscheiden: Controller-Gate für `/FaHierarchy` bei Schalter aus nachziehen (ADR 0011) oder als bewusste Lücke festhalten.
3. B-2: TS-185 und TS-186 in `secondbrain/tests/testszenarien-index.md` aufnehmen.
4. TS-186.1–186.3 auf dem Testsystem (IDEAL mit Master an, AKE mit Master aus).
