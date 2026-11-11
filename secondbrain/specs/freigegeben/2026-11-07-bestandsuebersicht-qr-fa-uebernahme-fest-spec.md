---
type: spec
title: Bestandsübersicht – FA-Übernahme aus dem Artikel-QR an QrMitFaNummer binden
slug: 2026-11-07-bestandsuebersicht-qr-fa-uebernahme-fest-spec
status: Testbereit
created: 2026-11-07
updated: 2026-11-07
source_backlog: "[[2026-10-27-bestandsuebersicht-qr-fa-uebernahme-fest]]"
bug: "[[2026-11-07-bestandsuebersicht-qr-fa-uebernahme-fest-bug]]"
buendel: "[[2026-11-07-buendel-uebersicht]]"
version: v1.76.0
task: "[[2026-11-07-buendel-2026-11-07-goal-lauf]]"
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
standort: beide
affected_code:
  - "IdealAkeWms/Views/StockOverview/Index.cshtml:17-18 (data-qr-fa-enabled hart \"true\" → aus ViewBag)"
  - "IdealAkeWms/Controllers/StockOverviewController.cs:41-95 (ViewBag.QrMitFaNummer setzen, _settingRepository ist schon injiziert :15/:37)"
  - "IdealAkeWms/wwwroot/js/barcode-scanner.js:14, :311-313, :396-403 (nur gelesen, keine Änderung)"
  - "IdealAkeWms/Views/Help/Index.cshtml:120-121, :1775 (Hilfe-Text)"
  - "README.md:247 (QR mit FA-Nummer)"
  - "docs/TESTSZENARIEN.md TS-101.7, TS-101.18 anpassen, TS-101.22–101.25 neu"
  - "IdealAkeWms.Tests (neuer Controller-Test + Quelltext-Wächter)"
  - "Bug-Record [[2026-11-07-bestandsuebersicht-qr-fa-uebernahme-fest-bug]]"
open_questions:
  - "Variante A (Bestandsübersicht an QrMitFaNummer binden) oder B (so lassen, Bug schließen)? Empfehlung A, vorher Prüfabfrage P1 je Standort-DB."
  - "Sammel-Wareneingang: QrMitFaNummer dort NICHT ausdehnen, tote ViewBag-Zuweisungen StockMovementsController.cs:168/:258 entfernen? Empfehlung: nicht ausdehnen, Zuweisungen entfernen."
  - "Stückliste, Artikelinfo, FA-Liste, Leitstand, OSEON: QrMitFaNummer weiterhin NICHT ausdehnen? Empfehlung ja (nicht ausdehnen)."
  - "Zusatzfund OSEON-Teileverfolgung: doppelter Klick-Handler am Knopf btnScanArticle – eigener Backlog-Punkt? Empfehlung ja, nicht hier mitbauen."
epic: false
etappen: []
deploy:
  web: true
  service: true
  migration: false
freigabe_entscheidung: ""
freigabe_von: "Gerald Weichbold"
freigabe_am: "2026-11-07"
---

## Ziel / Nutzen (das Warum)

`QrMitFaNummer` soll eine Regel sein, nicht zwei. Heute übernimmt die Bestandsübersicht die FA aus Feld 3 eines
Artikel-QR **immer**, Ein-/Aus-/Umbuchung nur bei eingeschalteter Einstellung. Weil ein FA-Filter in der
Bestandsübersicht alle anderen Filter übersteuert, zeigt ein Artikel-Scan dort bei Etiketten mit FA den Bestand
**aller Artikel dieses FA** statt des gescannten Artikels, obwohl der Admin die FA-Übernahme ausgeschaltet hat.
Defekt-Register: [[2026-11-07-bestandsuebersicht-qr-fa-uebernahme-fest-bug]].

## Befund am Code (Bündel-Worktree, v1.73.0)

### Wer entscheidet über die FA-Übernahme

Eine einzige Stelle wertet aus: `processScannedValue` in `barcode-scanner.js:290-371`. Die FA wird nur bei
`scanType === 'article'`, `qrFaEnabled` und ≥ 3 Feldern gesetzt (`:311-313`) und über `clearAndFillFaNumber`
(`:396-403`) ins Feld `faTargetId` geschrieben. Das Feld wird **bei jedem Artikel-Scan zuerst geleert**, auch wenn
`qrFaEnabled` falsch ist, sobald der Knopf ein `data-fa-target` hat (`:400-401`).

`qrFaEnabled` kommt aus genau zwei Quellen:
- `initScanner` liest `data-qr-fa-enabled` am Knopf **einmal beim Laden** (`:14`) und gibt den Wert an Kamera (`:18`),
  Hardware-Scanner (`:20` → `:559`), Kamera-Fenster (`:544`) und „Scannen ohne Feld“ (`:583`) weiter.
- `initTextInputScanner` setzt fest `false`, ohne FA-Ziel (`:427`, `:430`).

Eine serverseitige QR-Zerlegung gibt es nicht (Grep `split(` im Web-JS: nur `barcode-scanner.js:296/301/304/312/460`
für Artikel-QR; `bde-terminal.js:182/187` zerlegt FA-/AG-Barcodes `FA,AG` bzw. `FA/AG`, kein Artikel-QR).

### Alle Seiten mit Artikel-QR (vollständig: alle `initScanner`/`initTextInputScanner` mit `article`)

| # | Seite | Init | FA-Übernahme heute | Quelle |
|---|---|---|---|---|
| 1 | Wareneingang | `barcode-scanner.js:593` (Knopf `Inbound.cshtml:39-40`) | nur bei `QrMitFaNummer` | `Inbound.cshtml:40` ← `StockMovementsController.cs:97`, `:118` |
| 2 | Warenausgang | `:593` (`Outbound.cshtml:26-27`) | nur bei `QrMitFaNummer` | `Outbound.cshtml:27` ← `:280`, `:299` |
| 3 | Umbuchung | `:593` (`Transfer.cshtml:26-27`) | nur bei `QrMitFaNummer` | `Transfer.cshtml:27` ← `:367`, `:384` |
| 4 | Sammel-Wareneingang | `InboundBulk.cshtml:293` (`initTextInputScanner`) | **nie** (fest `false`) | ViewBag wird gesetzt (`StockMovementsController.cs:168`, `:258`), View liest ihn nicht (Grep `QrMitFa` in `InboundBulk.cshtml`: kein Treffer) |
| 5 | Stückliste | `Bom.cshtml:722` (`initScanner`, Knopf ohne Attribut) | nie | Attribut fehlt → `false` |
| 6 | Artikelinfo | `barcode-scanner.js:596` | nie | Attribut fehlt |
| 7 | **Bestandsübersicht** | `StockOverview/Index.cshtml:159` | **immer** | **hart `data-qr-fa-enabled="true"`, `:18`**; `StockOverviewController` liest die Einstellung nicht (`:41-95`) |
| 8 | FA-Liste | `ProductionOrders/Index.cshtml:294` | nie | `initTextInputScanner` |
| 9 | Kommissionier-Leitstand | `PickingLeitstand/Index.cshtml:493` | nie | `initTextInputScanner` |
| 10 | OSEON-Teileverfolgung (nur AKE) | `Tracking/OseonIndex.cshtml:312` | nie | `initTextInputScanner` |

FA-**Knöpfe** (`scanType 'productionOrder'`, z. B. `StockOverview/Index.cshtml:160`, `ProductionOrders/Index.cshtml:288`,
`PickingLeitstand/Index.cshtml:487`, `StockMovements/Index.cshtml:162`) nehmen Feld 3 immer (`barcode-scanner.js:298-305`).
Das ist kein Artikel-QR → FA, sondern der Zweck des FA-Knopfs, und bleibt unverändert.

### Der vermeintliche Widerspruch zur Scan-Handler-Spec

[[2026-10-15-scan-handler-hardware-scanner-spec]] `:102` („FA-Übernahme nur bei `QrMitFaNummer`“) steht in
Freigabe-Nachtrag 5 und gilt nur für die Pilotseite **Einbuchung** (`:95`). Für Seite 1 stimmt der Satz
(`barcode-scanner.js:583` reicht `artikel.qrFaEnabled` aus dem Knopf durch). Ein Widerspruch im Code besteht nicht;
die Hilfe beschreibt den Ist-Stand korrekt („Ein-, Aus- und Umbuchung (nur mit `QrMitFaNummer`) und in der
Bestandsübersicht (immer)“, `Help/Index.cshtml:120-121`). Der Konflikt ist fachlich: Eine Einstellung namens
„QR-Code enthält FA-Nummer“ (`Program.cs:472`) wirkt auf einer Seite nicht.

### Folge auf der Bestandsübersicht

Mit gefülltem FA-Filter fragt der Controller nur `GetStockByProductionOrderAsync` ab und ignoriert Artikel,
Lagerplatz und Min/Max (`StockOverviewController.cs:48-54`; dokumentiert `Help/Index.cshtml:1781`). Ein
Artikel-QR mit FA filtert also nach dem FA, nicht nach dem Artikel (Hinweis `Help/Index.cshtml:1775`).

## Umfang (In-Scope / Out-of-Scope)

**In-Scope (Variante A, empfohlen):**
- Bestandsübersicht: FA-Übernahme beim Artikel-QR-Scan folgt `QrMitFaNummer`, für alle Eingabewege (Kamera,
  Bild hochladen, Hardware-Scanner, Kamera-Fenster mit Handscanner-Feld).
- Hilfe, README, Testszenarien, Changelog, Version v1.76.0.
- Abhängig von Rückfrage 2: tote ViewBag-Zuweisungen im Sammel-Wareneingang entfernen.

**Variante B:** keine Code-Änderung; Bug-Record auf `behoben` mit Begründung „gewollt“, Spec entfällt.

**Out-of-Scope:**
- `QrMitFaNummer` auf weitere Seiten ausdehnen (Seiten 4, 5, 6, 8, 9, 10; Rückfragen 2 und 3).
- FA-Filter übersteuert andere Filter (`StockOverviewController.cs:48-54`): alt, dokumentiert, bleibt.
- FA-Knöpfe, Lagerplatz-Scan, `QrErstesFeld`, `QrTrennzeichen`: unverändert.
- Zusatzfund OSEON-Doppel-Handler (Rückfrage 4).

## Fachliche Anforderungen

1. Bestandsübersicht, `QrMitFaNummer` = `true`: Artikel-QR mit Feld 3 setzt Artikelfilter (Feld 1, ggf. per
   `QrErstesFeld` übersetzt) **und** FA-Filter (Feld 3, Komma-Suffix abgeschnitten) – wie heute.
2. Bestandsübersicht, `QrMitFaNummer` = `false`, leer oder Zeile fehlt: Artikel-QR setzt nur den Artikelfilter;
   der FA-Filter wird **geleert** (wie Ein-/Aus-/Umbuchung, README:247 „Bei jedem Scan wird das FA-Feld zuerst
   geleert“). Grund: ein stehengebliebener FA-Filter würde den neuen Artikelfilter sonst übersteuern.
3. Gleiche Lesart der Einstellung wie Ein-/Aus-/Umbuchung: nur `true` (ohne Groß/Klein) schaltet ein
   (`StockMovementsController.cs:97`).
4. Der FA-Knopf der Bestandsübersicht (`btnScanPO`) nimmt Feld 3 weiterhin immer.
5. Eine Änderung der Einstellung wirkt nach Neuladen der Seite (Attribut wird beim Laden gelesen, `barcode-scanner.js:14`).

## Technischer Loesungsentwurf

ponytail-Leiter: Das Muster gibt es schon (Rung 2) – exakt wie `Inbound/Outbound/Transfer`.

1. `StockOverviewController.Index` (`:41-95`): vor `return View(vm)` eine Zeile, identisch zu
   `StockMovementsController.cs:97`:
   `ViewBag.QrMitFaNummer = (await _settingRepository.GetValueAsync(AppSettingKeys.QrMitFaNummer))?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;`
   (`using IdealAkeWms.Models;` ergänzen, falls `AppSettingKeys` nicht erreichbar.) `IAppSettingRepository` ist schon
   injiziert (`:15`, `:37`) und per `CachedSettingRepository` dekoriert (`Program.cs:80`).
2. `Views/StockOverview/Index.cshtml:18`: `data-qr-fa-enabled="@(ViewBag.QrMitFaNummer == true ? "true" : "false")"`
   (Wortlaut wie `Outbound.cshtml:27`). `data-fa-target="filterProductionOrder"` **bleibt** (Anforderung 2).
3. Kein JS-Change: `barcode-scanner.js` liest das Attribut bereits für alle Eingabewege.
4. Nur bei Antwort 2 = „entfernen“: `StockMovementsController.cs:168` und `:258` (`ViewBag.QrMitFaNummer` im
   Sammel-Wareneingang) löschen.
5. Tests:
   - Controller-Test `StockOverviewControllerTests` (neu; es gibt noch keinen, Grep `StockOverviewController` in
     `IdealAkeWms.Tests`: kein Treffer): `GetValueAsync(QrMitFaNummer)` → `"true"`/`"TRUE"` ⇒ `ViewBag.QrMitFaNummer == true`;
     `"false"`/`null` ⇒ `false`. Repositories per Moq.
   - Quelltext-Wächter (z. B. in `HardwareScannerTests`, Muster `Read("Views", …)`, `:25`):
     `StockOverview/Index.cshtml` enthält **nicht** `data-qr-fa-enabled="true"` und enthält `ViewBag.QrMitFaNummer`.
   - Der Klick-/Scan-Ablauf im Browser ist Manual-UAT (TS-101.22–101.25).
6. Doku: `Help/Index.cshtml:120-121` → „bei Ein-, Aus- und Umbuchung sowie in der Bestandsübersicht (nur mit
   `QrMitFaNummer`); sonst nie“; `:1775` → „… wird diese bei eingeschalteter Einstellung `QrMitFaNummer` ebenfalls in das
   FA-Feld übernommen …“; `README.md:247` „Ein/Aus/Umbuchung und Bestandsübersicht“. Changelog-Eintrag v1.76.0.

**frontend-design:** Es ändert sich eine View (`StockOverview/Index.cshtml`, nur ein Attributwert, kein sichtbares
Layout) und die Hilfeseite (Text). Der Skill `frontend-design` ist laut CLAUDE.md trotzdem explizit aufzurufen;
erwartete Wirkung: keine visuelle Änderung.

## Migrations-/SQL-Auswirkungen

**Keine.** Kein Schema, keine Migration, kein `SQL/XX`, `SQL/00_FreshInstall.sql` und `SQL/AgentJobs/` unberührt.
`QrMitFaNummer` existiert (`SQL/28_AddQrMitFaNummer.sql`, `00_FreshInstall.sql:1059`, Laufzeit-Seed `Program.cs:472`).
Dev-Lauf bestätigt `dotnet ef migrations has-pending-model-changes` = leer.

**Prüfabfrage P1 (Mensch, vor Freigabe; je Produktiv-Datenbank, an der AKE bzw. IDEAL läuft):**

```sql
SELECT [Key], [Value] FROM dbo.AppSettings
WHERE [Key] IN (N'QrMitFaNummer', N'QrErstesFeld', N'QrTrennzeichen');
```

Steht `QrMitFaNummer` irgendwo auf `false` (Default), verliert die Bestandsübersicht dort mit Variante A die
FA-Übernahme. Das ist die gewollte Wirkung, muss aber den Anwendern angesagt werden (Changelog). Ungeprüft: ob an
einem Standort jemand die FA-Übernahme in der Bestandsübersicht bewusst nutzt (Frage an die Lagerleitung).

## Einstellungen

Keine neue Einstellung, kein neuer Wert. Geändert wird die **Reichweite** einer bestehenden:

| Key | Typ | Erlaubte Werte | Default | Wirkung | Verwendung | Standort | Warnhinweis |
|---|---|---|---|---|---|---|---|
| `QrMitFaNummer` | Bool (Schalter) | `true` / `false` | `false` (`Program.cs:472`, `00_FreshInstall.sql:1059`) | An: Feld 3 eines Artikel-QR wird als FA übernommen. Aus: FA-Feld wird beim Artikel-Scan geleert. | Ein-, Aus-, Umbuchung **und neu Bestandsübersicht** (Artikel-QR-Knopf). Nicht: Sammel-Wareneingang, Stückliste, Artikelinfo, FA-Liste, Leitstand, OSEON, FA-Knöpfe. | beide | – (keine Außenwirkung; die FA einer Buchung reist erst mit der Buchung) |
| `QrErstesFeld` | Auswahl | `Artikelnummer` / `Matchcode` | `Artikelnummer` | unverändert | unverändert | beide | – |
| `QrTrennzeichen` | Auswahl | `;` / `,` | `;` | unverändert | unverändert | beide | – |

Die Beschreibungszeile in der DB (`Program.cs:472`) wird nicht per SQL angefasst. Liefert der Einstellungs-Katalog nach
ADR 0020 (Backlog `2026-11-07-einstellungen-auswahlfelder-und-beschreibung`) im selben Bündel die Texte „Wirkung“/
„Verwendung“, übernimmt er die Spalte „Verwendung“ oben wörtlich.

## Audit-Feld-Auswirkungen

Keine. Es wird nur gelesen; `AppSetting` erbt nicht `AuditableEntity`. Keine Buchungsänderung.

## Rollen / Zugriff

Unverändert: `StockOverviewController` `[RequireStockReadAccess]` (`:10`). `/Settings` wie bisher (Admin).
Listen-View-Pattern: keine neue Tabelle, Pagination/Spaltenfilter der Bestandsübersicht unberührt.

## Akzeptanzkriterien

1. `QrMitFaNummer` = `true`: Bestandsübersicht, Artikel-QR `102044;;1065141;09` per Kamera/Bild → Artikelfilter `102044`, FA-Filter `1065141`.
2. `QrMitFaNummer` = `false`: derselbe Scan → Artikelfilter `102044`, FA-Filter leer.
3. `false`, FA-Filter vorher von Hand mit `999` gefüllt: Artikel-Scan → FA-Filter leer.
4. Wie 1 und 2 per Hardware-Scanner (Fokus im Artikelfilter) und über das Kamera-Fenster mit Handscanner-Feld.
5. FA-Knopf der Bestandsübersicht mit `102044;;1065141;09` → FA-Filter `1065141`, unabhängig von `QrMitFaNummer`.
6. Ein-, Aus-, Umbuchung verhalten sich unverändert (TS-101.1/101.2).
7. Wert `TRUE` in der DB schaltet ein; fehlende Zeile oder leerer Wert = aus.
8. Quelltext: `StockOverview/Index.cshtml` enthält kein `data-qr-fa-enabled="true"`; Controller-Test und Wächter grün; `dotnet build` + `dotnet test` grün.
9. Hilfe (`:120-121`, `:1775`) und README:247 nennen die Bestandsübersicht als von `QrMitFaNummer` abhängig.
10. Nur bei Antwort 2 = entfernen: kein `ViewBag.QrMitFaNummer` mehr in `InboundBulk`-Actions; Sammel-Wareneingang scannt wie vorher (TS-101.5).

## Test-Szenarien

`docs/TESTSZENARIEN.md`, Kapitel TS-101 (QR-Code), danach `secondbrain/tests/testszenarien-index.md` Zeile 101.

- **TS-101.7 anpassen:** „Bestandsübersicht: Artikelfilter `50002134`, FA-Filter `1065141` **nur bei `QrMitFaNummer` an**, sonst leer.“
- **TS-101.18 anpassen:** „(wo übernommen)“ ersetzen durch die Liste „Ein-, Ausgang, Umbuchung, Bestandsübersicht“.
- **TS-101.22 neu** – `QrMitFaNummer` an, Bestandsübersicht, E2 per Kamera/Bild → Artikel `102044`, FA `1065141`; „Filtern“ → FA-Bestand (Hinweis Hilfe :1781).
- **TS-101.23 neu** – `QrMitFaNummer` aus, Seite neu laden, FA-Filter von Hand `999`, E2 → Artikel `102044`, FA leer; „Filtern“ → nur Artikel 102044.
- **TS-101.24 neu** – 101.22/101.23 per Hardware-Scanner (Tastatur-Emulation und Netum Fokus-Eingabe, Kamera-Fenster) → gleiche Ergebnisse.
- **TS-101.25 neu (Negativ/Regression)** – FA-Knopf mit E2 bei `QrMitFaNummer` aus → FA `1065141`. Ein-/Aus-/Umbuchung 101.1/101.2 wiederholen. `QrMitFaNummer` in der DB auf `TRUE` → wie an.

Vorbedingungen wie TS-101 (Etiketten E1/E2, Testsystem beider Standorte; OSEON nicht betroffen).

## Etappen (nur bei epic: true)

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | entfällt (kein Epic) | – | – |

## Deploy

**Final (QA 2026-11-07, aus `git diff 1f55f35b 2fafd073`).**

- **Web-App:** ja – `IdealAkeWms/` (StockOverviewController, StockMovementsController, StockOverview/Index, Help/Index, Help/Changelog, AppVersion).
- **Service:** ja, nur Versionsstand – `IDEALAKEWMSService/AppVersion.cs` 1.75.0 → 1.76.0, keine Logik. Mitpublizieren, damit Web und Dienst dieselbe Version melden.
- **Migration:** nein (keine Datei unter `*/Migrations/`, kein `SQL/`).
- **Publish-Befehle** (im Worktree; Ablauf: Publish aus dem Worktree → Testsystem → Test → Merge):
  `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`
  `dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService`
- Reihenfolge: kein DB-Schritt; Web tauschen, Dienst tauschen. Kein Backup-Zwang aus dieser Spec.
- Nach dem Merge nur dann erneut aus `main` publizieren, wenn der Merge getestete Dateien mit parallelen `main`-Änderungen zusammengeführt hat.
- P1 laut Freigabe: `QrMitFaNummer` an AKE und IDEAL aktiv → keine sichtbare Verhaltensänderung im Betrieb.

## Zusatzfunde (nicht mitbauen)

1. **OSEON-Teileverfolgung, doppelter Klick-Handler.** `OseonIndex.cshtml:26` nennt den Knopf `btnScanArticle` und
   lädt `barcode-scanner.js` (`:235`). Der globale Start `barcode-scanner.js:592-597` ruft
   `initScanner('btnScanArticle', 'ArticleId', 'article')`; `initScanner` prüft nur den Knopf, nicht das Ziel (`:11-12`),
   und hängt einen Klick-Handler an, der das Kamera-Fenster für das dort nicht vorhandene `ArticleId` öffnet (`:17-18`).
   Zusätzlich hängt `OseonIndex.cshtml:312` (`initTextInputScanner`) einen zweiten an. Ein Klick ruft
   `openScannerModal` zweimal; welches Fenster bleibt, hängt vom Ende von `requestCameraPermission` ab (`:63`, `:69-70`).
   Laufzeitwirkung **ungeprüft** (nur AKE): Manual-Check „OSEON, Artikel-QR-Knopf, Scan → Filter gesetzt + Submit?“.
   Kein FA-Bezug. Rückfrage 4.
2. **Toter ViewBag im Sammel-Wareneingang** (`StockMovementsController.cs:168`, `:258`) – hat zum Befund „Wert wird
   übergeben, aber nie gelesen“ geführt. Rückfrage 2.

## Offene Rueckfragen

1. **Variante A oder B?** A = Bestandsübersicht an `QrMitFaNummer` binden (2 Zeilen Code, FA-Filter wird bei „aus“
   beim Artikel-Scan geleert). B = so lassen, Bug als „gewollt“ schließen. **Empfehlung A**: eine Einstellung, eine Regel;
   das „immer an“ kostet bei FA-Etiketten den Artikelfilter. Vorher P1 je Standort-DB laufen lassen.
2. **Sammel-Wareneingang:** `QrMitFaNummer` dort ausdehnen? Die Seite hat **eine** Kopf-FA für alle Zeilen
   (`StockMovementsController.cs:238`); jeder Artikel-Scan würde sie überschreiben. **Empfehlung: nicht ausdehnen,
   tote Zuweisungen `:168`/`:258` entfernen.**
3. **Stückliste, Artikelinfo, FA-Liste, Leitstand, OSEON:** weiterhin ohne FA-Übernahme? Stückliste/Artikelinfo haben
   kein FA-Ziel; OSEONs zweites Feld ist der Kundenauftrag (`OseonIndex.cshtml:321`, `filterCustomerOrder`), nicht die FA;
   in FA-Liste/Leitstand würde FA + Artikel gemeinsam filtern. **Empfehlung: nicht ausdehnen.**
4. **OSEON-Doppel-Handler** (Zusatzfund 1): eigener Backlog-Punkt? **Empfehlung ja**, nicht in diese Spec.

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
  Bei Varianten-Specs zusaetzlich freigabe_entscheidung im Frontmatter setzen.
-->

1. Variante A (binden) oder B (so lassen)? (Empfehlung A, vorher P1) → **Variante A (Gerald 2026-11-07). P1: `QrMitFaNummer` ist an AKE und IDEAL aktiv – an beiden Standorten keine Verhaltensänderung nach dem Deploy.**
2. Sammel-Wareneingang nicht ausdehnen, tote ViewBag-Zuweisungen :168/:258 entfernen? (Empfehlung ja) → **ja (Gerald 2026-11-07)**
3. Stückliste, Artikelinfo, FA-Liste, Leitstand, OSEON nicht ausdehnen? (Empfehlung ja) → **ja (Gerald 2026-11-07)**
4. OSEON-Doppel-Handler als eigener Backlog-Punkt? (Empfehlung ja) → **ja, eigener Backlog-Punkt (Gerald 2026-11-07)**

## QA-Nachweis (qa-agent, 2026-11-07)

Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, Branch `feature/2026-08-07-ideal-teile-1-5`, HEAD `2fafd073`.
Review-Bereich `git diff 1f55f35b 2fafd073` (ein Commit, 11 Dateien, +87/−12). Build und Tests über den ganzen Worktree.

### 1. Build und Tests

- `dotnet build IdealAkeWms.slnx`: 0 Fehler, 28 Warnungen (bestehend, keine aus dem Diff).
- `dotnet test --no-build`:
  - `IdealAkeWms.Tests`: 2402 bestanden, 1 übersprungen, 0 fehlgeschlagen (2403).
  - `IDEALAKEWMSService.Tests`: 497 bestanden, 0 übersprungen, 0 fehlgeschlagen (497).

### 2. Code-Review (selbst, in diesem Lauf)

Geprüft: `StockOverviewController.Index` komplett (ein einziger `return View(vm)`, kein früher Ausstieg, der die ViewBag-Zuweisung
umgeht), `StockOverview/Index.cshtml` (Knopf + Init-Skript :152-165), `barcode-scanner.js` `initScanner` :10-21, `processScannedValue`
:290-371, `clearAndFillFaNumber` :396-403, Hardware-Scan-Zustellung :535-560 (alle Wege lesen `qrFaEnabled` aus `_hwScanTargets`, also
aus dem Attribut), alle `QrMitFaNummer`-Fundstellen in `IdealAkeWms` (nur noch Ein-/Aus-/Umbuchung GET+POST und Bestandsübersicht),
die Razor-Ausgabe `ViewBag.QrMitFaNummer == true ? "true" : "false"` (dynamic `null == true` → `"false"`; Wortlaut identisch zu
`Inbound/Outbound/Transfer.cshtml`), entfernte Zuweisungen im Sammel-Wareneingang (`InboundBulk.cshtml` liest `QrMitFa` nicht – Grep).
Bekannte Defektklassen: kein Wert in `<script>` eingebettet (nur HTML-Attribut mit festem String); kein `hidden`/`d-*`; keine stille
Umdeutung (fehlend/leer → aus ist die spezifizierte Regel, Anforderung 3/AK 7); keine parallelen Arrays; keine reine
Browser-Validierung; Lesart der Einstellung in identischem Wortlaut an allen Stellen (H2).

Befunde:
- **Keine Befunde**, die eine Korrektur am Diff verlangen.
- **H1 (Hinweis, Folge-Spec im selben Bündel):** [[2026-11-07-einstellungen-auswahlfelder-und-beschreibung-spec]] `:282` führt für
  `QrMitFaNummer` als Fundstellen noch `StockMovementsController.cs:168`, `:258` (jetzt entfernt) und nennt die Bestandsübersicht nicht.
  Beim Umsetzen jener Spec die Spalte „Verwendung“ aus dem Abschnitt „Einstellungen“ dieser Spec übernehmen (dort bereits angekündigt).
  Außerhalb des Review-Bereichs, hier nicht behandelt.
- **H2 (Hinweis, nicht mitbauen):** Die Lesart von `QrMitFaNummer` steht jetzt 7× als identische Zeile (6× `StockMovementsController`,
  1× `StockOverviewController`). Kein Verhaltensunterschied; eine Hilfsmethode lohnt erst bei einer Regeländerung.
- Nachgezogen (QA, Brain): Satzzeichen in Index-Zeile 101 (`P1–P6. Nachtrag v1.76.0:`).

### 3. Nachweisart je Akzeptanzkriterium

| AK | Nachweis | Ergebnis |
|---|---|---|
| 1 `true` → Artikel + FA | Manual-UAT (Browser-JS, Kamera/Bild). DEV: Attribut `data-qr-fa-enabled="true"` gerendert (6c). JS-Pfad unverändert, gelesen. | DOM OK, Scan offen |
| 2 `false` → nur Artikel, FA leer | Manual-UAT; DEV: Attribut `"false"` gerendert; `clearAndFillFaNumber` leert bei `faNumber=null` (:400-401) | DOM OK, Scan offen |
| 3 FA `999` wird geleert | Manual-UAT (Browser-JS) | offen |
| 4 Hardware-Scanner / Kamera-Fenster | Manual-UAT | offen (6c Schritt 7) |
| 5 FA-Knopf nimmt Feld 3 immer | Code unverändert (`btnScanPO` ohne Attribut, Zweig `productionOrder` :298-305); Manual-UAT | offen |
| 6 Ein/Aus/Umbuchung unverändert | Code: nur Sammel-Wareneingang-Zuweisungen entfernt; DEV: Inbound/Outbound folgen der Einstellung (6c) | DOM OK, Buchung offen |
| 7 `TRUE` an, leer/fehlend aus | automatisiert `StockOverviewControllerTests` (true/TRUE/false/""/null) + DEV-DOM (6c); fehlende Zeile = `null` nur im Test | grün |
| 8 Quelltext + Tests | automatisiert `HardwareScannerTests.Bestandsuebersicht_QrFaUebernahme_FolgtQrMitFaNummer_NichtFest`, Build/Test | grün |
| 9 Hilfe + README | Diff gelesen (`Help/Index.cshtml:120-121`, `:1775`, `README.md:247`) | OK |
| 10 kein ViewBag im InboundBulk | automatisiert (Wächter zählt 6 Zuweisungen); Scan im Sammel-Wareneingang (TS-101.5) Manual-UAT | grün / UAT offen |

### 4. Geänderte bestehende Tests

Keine. Der Diff fügt nur hinzu: `StockOverviewControllerTests.cs` (neu) und einen neuen `[Fact]` in `HardwareScannerTests.cs`.

### 5. Änderungs-Checkliste (CLAUDE.md) und ADR 0020

- Migration/SQL/FreshInstall/AgentJobs: entfällt (keine Schemaänderung). Audit-Felder: entfällt (nur Lesen).
- Version: beide `AppVersion.cs` 1.76.0; Anwender-Changelog `Help/Changelog.cshtml` v1.76.0; Brain-Changelog
  [[2026-11-07-v1-76-0-bestandsuebersicht-qr-fa-uebernahme]] vorhanden.
- `docs/TESTSZENARIEN.md` TS-101.7/.18 angepasst, TS-101.22–101.25 neu; Index-Zeile 101 nachgezogen. Hilfe + README gepflegt. Keine neue Rolle.
- ADR 0020: keine neue Einstellung, kein neuer Wert. `QrMitFaNummer` ist Bool-Schalter in `/Settings` (fester Wertebereich).
  Wirkung/Verwendung im DB-Beschreibungstext fehlen weiterhin – laut Abschnitt „Einstellungen“ bewusst dem Katalog-Spec überlassen
  (akzeptierte Lücke, s. H1). Kein Warnhinweis nötig (keine Außenwirkung).

### 6. DEV-Deploy und Testlauf

a. Standort laut Frontmatter `beide`.
b. Elevated Shell bestätigt. `dev-deploy.ps1 -Standort AKE -Quelle <worktree>`: Exit 0, „HTTP 200, Fusszeile v1.76.0“, „Fertig: AKE
   v1.76.0 auf http://localhost:8081/“. `-Standort IDEAL`: Exit 0, „HTTP 200, Fusszeile v1.76.0“, „Fertig: IDEAL v1.76.0 auf
   http://localhost:8082/“. Fußzeile = AppVersion 1.76.0 an beiden Standorten.
c. Testschritte (Ausgangswert `QrMitFaNummer` = `true` an AKE und IDEAL; AKE am Ende wiederhergestellt, IDEAL nicht verändert):

| # | Schritt | Ergebnis | Status |
|---|---|---|---|
| 1 | AKE + IDEAL `/StockOverview` gerendert (Wert `true`) | `btnScanArticleFilter` … `data-qr-fa-enabled="true" data-fa-target="filterProductionOrder"` an beiden | OK |
| 2 | AKE `UPDATE AppSettings SET Value='false'`, App-Pool gestoppt/gestartet (Cache 2 min umgangen) | Bestandsübersicht, Wareneingang, Warenausgang: `data-qr-fa-enabled="false"` | OK |
| 3 | AKE Wert `TRUE` | alle drei Seiten `"true"` (AK 7) | OK |
| 4 | AKE Wert `''` | alle drei Seiten `"false"` (AK 7) | OK |
| 5 | AKE Wert `NULL` | nicht setzbar (Spalte `Value` NOT NULL, Msg 515); fehlender Wert nur im Unit-Test | NICHT MOEGLICH (Schema) |
| 6 | AKE zurück auf `true` | alle drei Seiten `"true"`; DB-Wert `true` geprüft | OK |
| 7 | Browser AKE: FA-Filter `999`, Artikel-QR-Knopf → Kamera-Fenster („Kamera nicht verfügbar“, Handscanner-Feld fokussiert), `102044;;1065141;09` bzw. `~09102044;;1065141;09` + Enter eingetippt | nicht als Hardware-Scan erkannt (Text blieb im Feld, Präfix nicht entfernt); die Tool-Tastatureingabe trifft das Scanner-Ereignismuster nicht. Ohne Kamera und QR-Bilddatei kein weiterer Weg | NICHT MOEGLICH |
| 8 | TS-101.22–101.25 Scan-Abläufe (Kamera/Bild/Netum/Tastatur-Emulation), FA-Knopf, Ein-/Aus-/Umbuchung | Gerät/Kamera nötig | NICHT MOEGLICH → Mensch |

Keine Außenschalter berührt. Testdaten: nur `QrMitFaNummer` in `AKE_WMS_DEV` kurzzeitig geändert, auf `true` zurückgesetzt.

### Gesamturteil

Build und Tests grün, Review ohne korrekturpflichtigen Befund, DEV beide Standorte v1.76.0, serverseitige Wirkung (Attribut folgt der
Einstellung inkl. `TRUE`/leer) auf DEV belegt. Scan-Abläufe im Browser sind Manual-UAT. → **Testbereit.**

### Manuelle Test-Checkliste (Mensch, Schranke 2)

1. Testsystem AKE und IDEAL: Web **und** Dienst aus dem Worktree publizieren (s. Deploy), Fußzeile v1.76.0.
2. TS-101.22: `QrMitFaNummer` an, Bestandsübersicht, E2 per Kamera bzw. Bild hochladen → Artikel `102044`, FA `1065141`.
3. TS-101.23: `QrMitFaNummer` aus (Seite neu laden; Einstellungs-Cache bis 2 min), FA-Filter `999`, E2 scannen → FA-Filter leer, „Filtern“ → nur Artikel.
4. TS-101.24: 2 und 3 per Hardware-Scanner (Tastatur-Emulation, Netum Fokus-Eingabe, Kamera-Fenster mit Handscanner-Feld).
5. TS-101.25: FA-Knopf mit E2 bei „aus“ → FA `1065141`; Ein-/Aus-/Umbuchung 101.1/101.2; Sammel-Wareneingang 101.5; Wert `TRUE` wirkt wie an.
6. TS-101.7 und TS-101.18 (angepasst) einmal mit „an“ durchspielen.
7. IDEAL: dort `QrErstesFeld`/`QrTrennzeichen` beachten und das passende Etikett wählen (DEV IDEAL: Matchcode + `,` → E1k statt E2).
8. Nach dem Test `QrMitFaNummer` auf den Produktivwert (`true`, P1) zurückstellen; dann Merge.
