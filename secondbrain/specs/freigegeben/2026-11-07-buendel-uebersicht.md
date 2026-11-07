---
type: uebersicht
title: Bündel 2026-11-07 – ein /goal-Dev-Lauf (OSEON-Bug, Fehlteile, Bestandsübersicht-QR, Einstellungen ADR 0020, EAN-Scan)
status: Entwurf
created: 2026-11-07
updated: 2026-11-07
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
specs:
  - "[[2026-11-07-oseon-sync-neu-ausgeschriebener-auftrag-spec]]"
  - "[[2026-11-07-fehlteile-nur-meine-werkbaenke-trefferzahl-spec]]"
  - "[[2026-11-07-bestandsuebersicht-qr-fa-uebernahme-fest-spec]]"
  - "[[2026-11-07-einstellungen-auswahlfelder-und-beschreibung-spec]]"
  - "[[2026-11-07-ean-einkaufsartikel-scan-spec]]"
nachbesserung: "[[2026-11-06-sage-sdata-stammdaten-sync-spec]]"
---

# Bündel 2026-11-07

Ein gemeinsamer `/goal`-Lauf ([[goal]], Befehl `/goal-run`) im **bestehenden** Bündel-Worktree
`.claude/worktrees/2026-08-07-ideal-teile-1-5` (Stand v1.73.0, letzte SQL-Datei `SQL/107`). Schranke 1 gilt je Spec
(alle fünf sind Entwurf); der Lauf startet erst, wenn Gerald die Freigabe-Antworten gefüllt hat bzw. ausdrücklich eine Ausnahme nennt.

## Reihenfolge, Version, SQL, Testszenario

| # | Spec | Typ | Version | SQL | Testszenario (Vergabe hier) | Standort | Deploy |
|---|---|---|---|---|---|---|---|
| 0 | Nachbesserung SData-Epic (unten, kein eigener Spec-Lauf) | Fix | bleibt 1.73.0 | – | TS-106.3 / 106.12 ergänzen | beide | Dienst |
| 1 | [[2026-11-07-oseon-sync-neu-ausgeschriebener-auftrag-spec]] | **Bug, Prio 1** | 1.74.0 | – | TS-107 | AKE | Dienst (+ Web für Version/Changelog) |
| 2 | [[2026-11-07-fehlteile-nur-meine-werkbaenke-trefferzahl-spec]] | Bug | 1.75.0 | – | TS-108 | beide | Web |
| 3 | [[2026-11-07-bestandsuebersicht-qr-fa-uebernahme-fest-spec]] | Bug | 1.76.0 | – | TS-101.22 – 101.25 (Ergänzung) | beide | Web |
| 4 | [[2026-11-07-einstellungen-auswahlfelder-und-beschreibung-spec]] | **Epic** E1, E2, E3, E4a, E4b | 1.77.0 | – | TS-109 | beide | Web |
| 5 | [[2026-11-07-ean-einkaufsartikel-scan-spec]] | Feature | 1.78.0 | – | TS-110 | beide (wirkt nur AKE mit `Sync:Quelle:Artikel = SData`) | Web |

- **SQL:** Keine Spec braucht eine Schemaänderung. Die reservierten Blöcke 108–109, 110–113, 114–115 sind **zurückgegeben**; nächste freie
  Nummer bleibt `SQL/108`. Braucht eine Spec im Lauf doch eine, vergibt der Lauf sie fortlaufend ab 108 (ADR 0004, [[goal]] Schritt 4)
  und trägt sie hier und in der Spec nach.
- **Testszenario-Nummern:** In einigen Specs steht noch „TS-1xx“ bzw. „TS-107 (vorläufig)“ – maßgeblich ist die Spalte oben; der Dev-Lauf
  zieht die Spec nach.
- **Versionen:** je Spec die nächste (ein Epic = eine Version). Verschiebt sich die Reihenfolge, wandern die Versionen mit.

## Abhängigkeiten

- 1 → keine; zuerst, weil die AKE-Produktion ungeschützt ist (Rückfrage 3 der OSEON-Spec: zusätzlicher Hotfix aus `main`?).
- 2, 3 → keine fachliche; 3 liefert die Spalte „Verwendung“ für `QrMitFaNummer`, die 4 in den Katalog übernimmt.
- 4 → nach 1–3 (deren Einstellungen landen gleich im Katalog); intern E1 → E2 → E3 → E4a → E4b. Übernimmt die Quellenschalter-Tabelle der
  SData-Spec und schließt deren „bewusste Lücke“ (Auswahlliste `Sync:Quelle:*`) in E3.
- 5 → `depends_on` [[2026-11-06-sage-sdata-stammdaten-sync-spec]] (E3a: `ArticleSageDetails.EANNummer` wird schon über SData geschrieben)
  und nach 4 (ADR-0020-Katalog). Nutzt die Scan-Kette aus [[2026-10-15-scan-handler-hardware-scanner-spec]].
- Gemeinsame Dateien (Konfliktgefahr im selben Zweig): `barcode-scanner.js` (3, 5), `Views/Help/*`, `Views/Help/Changelog.cshtml`,
  `docs/TESTSZENARIEN.md`, beide `AppVersion.cs` (alle) – deshalb streng nacheinander, je Spec eigene Commits.

## 0 – Nachbesserung SData-Epic (gehört zu [[2026-11-06-sage-sdata-stammdaten-sync-spec]], kein eigener Spec-Lauf)

Vor Spec 1 im selben Worktree; Commit `fix 2026-11-06-sage-sdata-stammdaten-sync: …`, Etappentabelle der SData-Spec um Zeile N4 ergänzen,
danach kurzer qa-agent-Lauf über den Commit (Status der SData-Spec bleibt Testbereit, wenn ohne Befund).

- **Q5 Dublettenprüfung Kreditoren/Debitoren gegen Name1 (+ Name2) statt Matchcode.** Heute vergleicht `KontokorrentSyncService` den
  WMS-Namen eines Datensatzes ohne Sage-Konto mit dem **Sage-Matchcode**; seit A13 heißen Sage-Konten aber Name1 (+ Name2). Folge: Dubletten
  werden übersehen (nie falsch gemeldet). Fix: gegen den Namen aus `Adressen` vergleichen (Rückfall Matchcode, wenn die Adresse fehlt oder
  `Adressen` nicht lesbar ist), Vergleich getrimmt + Groß/Klein egal wie bisher; Unit-Test; TS-106.12.e anpassen. Weiterhin nie automatisch
  verknüpfen.
- **Testartikel 86003871 in TS-106.3 ersetzen.** Er liegt außerhalb des AKE-Artikelfilters (Fertigungsartikel, `IstBestellartikel = 0`, in keiner
  PPS-Ressourcenposition; lesend geprüft 2026-11-07). Ersatz: **86006871** „Durchführung Kälteleitungen Ø50 x 76mm“ (Fertigungsartikel, in 427
  Ressourcenpositionen → in der AKE-Menge; Sage-DB `ake_TEST2026`). Vermutlich war diese Nummer gemeint (eine Ziffer Unterschied) – bitte
  bestätigen. TS-106.3 Vorbedingung, Nachtrag-Absatz und 106.3.g sowie die Spec-Abschnitte (Test-Szenarien, Checkliste) nachziehen.

## Freigabe-Nachtrag 2026-11-07 (Gerald, geht allem oben vor)

- Alle 18 Rückfragen **wie empfohlen**, mit einer Abweichung: **OSEON (Spec 1) als AKE-Hotfix aus `main`**, eigener Worktree, nicht im
  Bündel-Lauf. Im Bündel entfällt Version 1.74.0; das Bündel holt den Fix über `scripts/sync-worktree.ps1` (Mensch).
- P1 (QR): `QrMitFaNummer` ist an AKE und IDEAL aktiv – Variante A ändert an beiden Standorten nichts.
- Ersatz-Testartikel **86006871** bestätigt (Punkt 0).
- **Vor Punkt 0 im Bündel:** Fix SData-Filter `ne`/`gt` auf Textfeldern (`ArtikelVariantenExtended`, [[fallstricke]] „Sage-SData-Lesen“);
  EAN (Spec 5) liest dieselbe Ressource und kommt danach.
- Specs 2–5 freigegeben (2026-11-07); P-E1–P-E4 (EAN) führt der Dev am AKE-Test nur lesend aus.
- **Spec 5 EAN zurückgestellt (Gerald 2026-11-07 10:08):** EAN wird in Sage nicht gepflegt (P-E2: 2 von 279.529) – im Lauf überspringen.
- Nachzügler: `backlog/2026-11-07-artikeldetails-lieferanten-anzeigen.md` hat noch keine Spec – nicht in diesem Lauf.

## Offene Rückfragen über alle Specs (je mit Empfehlung, Details in der Spec)

| Spec | Frage | Empfehlung |
|---|---|---|
| 1 OSEON | Auswahlregel bei mehreren OSEON-Zeilen je Auftragsnummer | höchste ID (OSEON-Prüfabfrage zur ID-Vergabe) |
| 1 OSEON | Nicht mehr gelieferte Arbeitsgänge nach Neuausschreibung | stehen lassen (wie bisher) |
| 1 OSEON | Nur im Bündel ausliefern oder zusätzlich AKE-Hotfix aus `main` | Bündel; Hotfix nur, wenn der Merge sich verzögert |
| 1 OSEON | OSEON-Sync auf der IDEAL-Testinstanz aktiv – gewollt? | QA auf AKE-Test; Prüfabfrage `ServiceSettings` |
| 2 Fehlteile | Tab-Badges zählen bei „Nur meine Werkbänke“ nur die eigenen | ja (Spec so geschrieben) |
| 2 Fehlteile | Karte „Meine Fehlteile“ zählt Lager + Glas, Link filtert nach Typ | eigener Backlog-Eintrag |
| 3 QR | Variante A (an `QrMitFaNummer` binden) oder B (so lassen) | A, vorher Prüfabfrage P1 je Standort |
| 3 QR | Sammel-Wareneingang nicht ausdehnen, tote ViewBag-Zuweisungen entfernen | ja |
| 3 QR | Stückliste/Artikelinfo/FA-Liste/Leitstand/OSEON weiter ohne FA-Übernahme | ja |
| 3 QR | OSEON-Doppel-Klickhandler `btnScanArticle` | eigener Backlog-Eintrag |
| 4 Einstellungen | Prüfung ungültiger Werte beim Web-Start statt Umbau von ~100 Lesestellen | ja |
| 4 Einstellungen | `BdeDefaultArbeitsgang` Freitext mit Vorschlagsliste | ja |
| 4 Einstellungen | `OseonRueckmeldungAktiv`/`SageRueckmeldungAktiv` ohne Leser: als „ohne Wirkung“ führen | ja, Aufräumen als Aufgabe |
| 5 EAN | Quelle: SData-Satellit statt IDEAL-View-Spalte; IDEAL-Lücke als Folge-Spec | ja |
| 5 EAN | Mehrere EANs je Artikel: erste Variante genügt vorerst | ja, nach P-E2/P-E3 neu entscheiden |
| 5 EAN | Mehrere Artikel je EAN: Auswahlfenster bis 5 Treffer | ja |
| 5 EAN | EAN von Hand in Suchfeldern | nein, nur Scan |
| 0 SData | Ersatzartikel 86006871 statt 86003871 | bestätigen |

## Zusatzfunde (nicht spezifiziert, als Backlog-Kandidaten)

- Fehlteile: Werkbank-Dropdown im Modus „Nur meine Werkbänke“ gesperrt und listet alle Werkbänke (`MissingPartsController.cs:134`, `Views/MissingParts/Index.cshtml:70`).
- Fehlteile: Karte „Meine Fehlteile“ auf `/WarehouseRequisitions` zählt Lager + Glas zusammen (`WarehouseRequisitionRepository.cs:477-488`).
- OSEON-Teileverfolgung: doppelter Klickhandler am Knopf `btnScanArticle` (`OseonIndex.cshtml:26`, `barcode-scanner.js:593`).
- Einstellungen: `/ServiceSettings` speichert bei einem Zahlenfehler die übrigen Werte trotzdem; Service-Schalter mit Default `true` und leerem
  DB-Wert kippt beim Speichern auf `false`; `/Settings` schreibt unbekannte Keys – diese drei behebt Spec 4 (E3/E4a).
