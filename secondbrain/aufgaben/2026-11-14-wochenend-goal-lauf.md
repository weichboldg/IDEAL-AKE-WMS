---
type: aufgabe
status: erledigt
created: 2026-11-13
auftrag: "[[2026-11-14-wochenend-review-test-anwenderdoku]]"
---
# /goal-Lauf Wochenende: Code-Review, Regression DEV, Anwender-Doku

Auftrag [[2026-11-14-wochenend-review-test-anwenderdoku]] (Gerald 2026-11-13 19:35, /goal). Bündel-Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`,
Branch `feature/2026-08-07-ideal-teile-1-5`, Stand v1.99.22 `90bbbbd7`. Reihenfolge A → B → C, Kommissionierung und Kommissionierbereiche jeweils
zuerst und am gründlichsten. Grenzen: nichts an Sage schreiben, keine Mails an externe Adressen, keine Einstellungen außerhalb DEV, kein
Anwendungscode-Fix ohne Spec (außer Tippfehler), kein Merge, kein Push.

| Teil | Inhalt | Status | Ergebnis |
|---|---|---|---|
| A1 | Review Kommissionierung + Kommissionierbereiche | **erledigt** | 20 Befunde – Kommissionierung K-1–K-15 (1 hoch K-1 doppelter Folgeauftrag, 5 mittel, 6 niedrig, 3 Hinweis), Bereiche B-1–B-5 (1 mittel B-1 Standardwechsel ohne Transaktion, 2 niedrig, 2 Hinweis): [[2026-11-14-review-befunde-kommissionierung]], [[2026-11-14-review-befunde-kommissionierbereiche]]. Tippfehler behoben Worktree `433da6c4` (Sage-Stückliste, Kommissionierer ändern) |
| A2 | Review Wareneingang 1–3, Sage-WE/CSV, Lieferverzug, Bestellvorschläge, Offene Bestellungen | **erledigt** | 11 Befunde (1 hoch WE-1, 2 mittel WE-2/LV-1, 5 niedrig, 3 Hinweis): [[2026-11-14-review-befunde-wareneingang]], [[2026-11-14-review-befunde-lieferverzug]], [[2026-11-14-review-befunde-offene-bestellungen]], [[2026-11-14-review-befunde-bestellvorschlaege]] |
| A3 | Review SData Teil 2, Update-Skript, UTC/Lokalzeit, übrige Bündel-Module | **erledigt** | 14 Befunde (0 hoch, 4 mittel SD-1 Kennwort an freie Adresse im Verbindungstest, SD-5 Klartext-Kennwort im Install-Skript, UP-1 Neustart-Update verliert Daten-Einträge, BDE-1 doppelte Fertigmeldung; 5 niedrig, 5 Hinweis): [[2026-11-14-review-befunde-sdata]], [[2026-11-14-review-befunde-update-skript]], [[2026-11-14-review-befunde-lokalzeit]], [[2026-11-14-review-befunde-bde]], [[2026-11-14-review-befunde-einstellungen]]. Gezielter `dotnet test` 261/0. „Fertigungmenge“ (UI, = DB-Spaltenname) nicht geändert → Frage an Gerald |
| B | Regression DEV AKE + IDEAL, Bericht `secondbrain/tests/2026-11-14-wochenend-regression-dev/` | **erledigt** (B1–B4): IDEAL-Hierarchie/Beschichtung/BDE, B4 übrige Module folgen | B1 [[01-kommissionierung]]: 41 OK / 5 FEHLER / 11 NICHT PRUEFBAR; neu N-1 (hoch, unsichtbare Unterpositionen), N-2 (Deadlock-500); K-2/K-3/K-4/K-6/B-1 reproduziert, K-1 durch N-2 verdeckt. B2 [[02-wareneingang-einkauf]]: 79 OK / 1 FEHLER (WE-1 reproduziert) / 19 NICHT PRUEFBAR; WE-3 reproduziert; neu N-7–N-10 (Glasbestellung als „Lagerbestellung“, Einheit fehlt, Querscrollen, IDEAL-DEV Wareneingang an ohne Abgleich); Lieferverzug-Lauf nur Pickup (91 .eml). B3 [[03-ideal-hierarchie-beschichtung-bde]]: 82 OK / 1 FEHLER (BDE-1 serverseitig reproduziert) / 24 NICHT PRUEFBAR, TS-79/80 vervollständigt; neu N-B3-1–6 in [[2026-11-14-review-befunde-ideal-hierarchie]]; Chrome-Screenshot-Werkzeug ab 21:26 ausgefallen, Ersatz Edge headless. B4 [[04-uebrige-module]]: 74 OK / 2 FEHLER (N-B4-2 CSV Tausender mit Leerzeichen → Excel-Text, N-B4-1 Scanner-Karte bei Schalter aus) / 20 NICHT PRUEFBAR; SD-3/SD-4/LZ-2 bestätigt, SD-1 live nicht belegbar (keine Web-Zugangsdaten). **Gesamt [[README]] der Regression: 276 OK / 9 FEHLER / 74 NICHT PRUEFBAR, 20 neue Befunde** |
| C | Anwender-Doku `docs/anwender/`, README, Brain | **erledigt** (C1 erledigt `494747d3`: 01 Kommissionierung 18 Bilder, 02 Kommissionierbereiche 3 Bilder, Index; C2 erledigt `15833616`: 03–08, 25 Bilder; C3 erledigt `1539c0c1`: 09–12, 21 Bilder – zusammen 12 Anleitungen, 66 Bilder, Links geprüft; README + Brain erledigt) | README Worktree `e7ac143c` (AppSettings/ServiceSettings seit v1.99, Deploy-Hinweise v1.99.x); Brain: glossar (24 Begriffe + 3 Rollen), codebase datenmodell/controller/services/module, testszenarien-index (Regression 2026-11-14). Unstimmigkeit: `Lieferverzug:TageNachTermin` Katalog-Default 3 vs Manifest-Prüfung erwartet 1 → Gerald |

**Teil A Summe:** 45 Befunde (2 hoch: K-1 doppelter Folgeauftrag bei „Teilweise“, WE-1 doppeltes Absenden „Einbuchen“ bucht doppelt), alle als
Backlog-Notizen `2026-11-14-review-befunde-*`, nichts behoben (Schranke 1). Gemeinsames Muster: fehlende Sperre gegen Doppel-Absenden
(K-1, WE-1, BDE-1, WE-4) – Kandidat für eine Querschnitts-Spec.

**C1 offene fachliche Fragen (an Gerald):** Zahl am Menüpunkt „Kommissionierung“ (FAs oder Aufträge?); Bedarfsmeldung ohne Empfänger (N-5);
FA in Sage verpackt/abgeholt mit offenen KO-Aufträgen; auf AKE-DEV nur NAN als Quellplatz wählbar (Bild 01-09); produktive Glas-/VK-Regeln je Standort (B-5).
Hilfeseiten-Verweis auf `docs/anwender/` nur als Vorschlag (cshtml = Code; Auslieferung der Markdown-Dateien beim Deploy offen).
C1-Testdaten AKE-DEV: KO-2026-00053–00060 (storniert außer KO-2026-00057 „Teilweise“), Umbuchungen 19543–19545 NAN → Wagen 2, FA 2638001 teilkommissioniert.

## Antworten auf die Fragen des Laufs (Gerald 2026-11-13 22:47, ergänzt durch Koordination)
1. **Anleitungen aus der Hilfeseite verlinken:** Gerald: „wäre nice“ → **ja, aber nicht in diesem Lauf** (Code-Änderung + Auslieferung der
   Markdown-Dateien). Als Folge-Spec vorschlagen (Backlog-Notiz `2026-11-14-hilfe-verlinkt-anwender-anleitungen`); dieser Lauf schreibt die
   Anleitungen so, dass sie später 1:1 eingebunden werden können (relative Bildpfade, keine Obsidian-Syntax in `docs/anwender/`).
2. **Zahl am Menüpunkt „Kommissionierung“:** Gerald: **offene Aufträge**. Heute: freigegebene FAs (Changelog v1.14.0). → Befund/Folge-Spec:
   Badge = offene Kommissionieraufträge (Offen/In Arbeit); Vorschlag Koordination: für Kommissionierer die eigenen, für Leitstand/Admin alle.
3. **Glas- und VK-Regeln je Standort:** offen – Montag mit Gerald (und Lager/Keyuser) klären; im Bericht als Entscheidungsbedarf führen.
4. **`Lieferverzug:TageNachTermin`:** Katalog-Default **bleibt 3** (neutral); AKE setzt **1** per Einstellung (Keyuser Till). Erwartet die
   Prüfung im Update-Skript fest 1, ist das ein Befund → Prüfung darf keinen Wert erzwingen (nur Hinweis). In die Review-Befunde Update-Skript.
5. **„Fertigungmenge“:** Spaltenname aus der Sage-View/DB – **nicht umbenennen** (Belegen statt deuten). Nur sichtbare Texte in der Oberfläche
   und Doku dürfen „Fertigungsmenge“ heißen; Prüfabfrage für Gerald (Spaltenliste der View) in den Bericht.
6. **Tausendertrennzeichen:** auf akenet01:4445 belegt **Punkt** („4.500,000“, Abnahme 13.11.); idealweb01 vermutlich gleich (nicht geprüft).
   Unabhängig davon: **CSV-Export darf nicht von der Server-Kultur abhängen** – ohne Tausendertrennzeichen, Dezimalkomma, fest de-AT bzw.
   invariant formatiert → Befund (mittel) in die Review-Befunde; feste App-Kultur (BV4-Härtung) gehört dazu.


**Antworten übernommen (2026-11-13 23:xx):** (1) Backlog [[2026-11-14-hilfe-verlinkt-anwender-anleitungen]]; Anleitungen ohne Obsidian-Syntax.
(2) K-16 Badge = offene Aufträge in [[2026-11-14-review-befunde-kommissionierung]]. (3) Glas-/VK-Regeln als Entscheidungsbedarf Montag (dort + Regression-README).
(4) UP-4 in [[2026-11-14-review-befunde-update-skript]]. (5) Sichtbare Texte „Fertigungsmenge“ korrigiert (Worktree `95ea581b`), Spaltenname bleibt,
Prüfabfrage in [[2026-11-14-review-befunde-tabellen]]. (6) N-B4-2 präzisiert (CSV kulturunabhängig + feste App-Kultur) ebenda.

**C2 – Panne:** Ein `requestSubmit()` traf das Abmelde-Formular der Navigationsleiste und **meldete Gerald im gemeinsamen Browser ab** (22:38:38);
der Agent meldete über „Mit Windows anmelden“ (ohne Kennwort) wieder an. Nichts gebucht. C3 hat die Anweisung, Formulare nur mit geprüfter `action` abzusenden.
**C2 offene Fragen:** Abschließen einer Lagerbestellung bucht keinen Bestand – gewollt?; Glasbestellungen als offene Bedarfe in Bestellvorschlägen +
Hinweistext „Glasbestellung“ (N-7)?; Meldung „Standard-Werkbank im Profil hinterlegen“ irreführend (nimmt erste zugeordnete Werkbank,
`WarehouseRequisitionsApiController.cs:252/271`); Status leer bei Teilgeliefert (`WarehouseRequisitions/Edit.cshtml:22-28`); Bedarfsmeldungs-Dialog
„Fehler beim Laden der Empfänger“ bei leerer Artikelgruppe; IDEAL „Wareneingang zu Sage-Bestellungen“ aus lassen (N-10)?
C2-Testdaten AKE-DEV: Lagerbestellung #52 (Entwurf) + 2 Positionen.

## Abschluss (2026-11-14)

- **A Review:** 45 Befunde in Backlog-Notizen `2026-11-14-review-befunde-*` (2 hoch: K-1, WE-1); Tippfehler korrigiert `433da6c4`, sichtbare Texte
  „Fertigungsmenge“ `95ea581b`.
- **B Regression DEV AKE + IDEAL:** 276 OK / 9 FEHLER / 74 NICHT PRUEFBAR, 20 neue Befunde; Bericht + Übersicht in `secondbrain/tests/2026-11-14-wochenend-regression-dev/`.
- **C Doku:** `docs/anwender/` 01–12 + Index (Worktree `494747d3`, `15833616`, `1539c0c1`), README `e7ac143c`, Brain (glossar, codebase, testindex);
  Folge-Specs: [[2026-11-14-hilfe-verlinkt-anwender-anleitungen]], [[2026-11-14-review-befunde-lager]] (neu aus C3).
- Kein Sage-Schreiben, keine Mail an externe Adressen (Lieferverzug nur `.eml` im DEV-Pickup), keine Einstellung außerhalb DEV, kein Merge, kein Push.
- **Panne:** C2 hat Gerald einmal im gemeinsamen Browser abgemeldet (22:38) und per Windows-Anmeldung wieder angemeldet; nichts gebucht.
- **Offen für Gerald:** Folge-Specs aus den Review-Notizen priorisieren (Vorschlag zuerst: Doppel-Absenden-Schutz K-1/N-2/WE-1/BDE-1/K-6 als Querschnitt,
  N-1 unsichtbare Unterpositionen, SD-1 Verbindungstest); Glas-/VK-Regeln (Montag); offene Fragen aus C2/C3 (Lagerbestellung bucht keinen Bestand,
  Glasbestellungen als Bedarf, Werkbank-Meldung, Status Teilgeliefert, Empfänger-Fehlertext, IDEAL Wareneingang aus, Kommissionierer darf
  Lagerplatz aus-/umbuchen?, Ausbuchung Menge 0, Produktivwerte QR/Scanner/Minusbuchungen).
- **DEV-Testdaten (Spuren in den Berichten):** AKE KO-2026-00020–00060 (meist storniert, KO-2026-00057 Teilweise), Umbuchungen 19432–19545 auf Wagen
  (NAN negativ), Receipts 13–20, Lagerbestellung #52 (Entwurf), FA-Prios/IsDonePicking; IDEAL BDE-Terminal 2 + Buchungen 7–17 (storniert), BA-2026-00005.
