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
## Geänderte Kapitel (für spätere Confluence-Übernahme)

- Anwender: 01 Kommissionierung, 03 Lager-/Glasbestellung, 10 Bestände und Spaltenfilter, 15 Beschichtungsauftrag, 05 Wareneingang zu Bestellung (v1.99.59), README (Stand/Tabelle)
- Betrieb: 00-index (Stand)
