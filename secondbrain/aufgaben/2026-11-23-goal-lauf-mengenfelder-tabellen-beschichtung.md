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
