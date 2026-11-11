---
typ: feature
---
# BDE: Abgleich Lastenheft (`anhaenge/bde_ideen.md`) gegen den Ist-Stand + mehrere Werkbänke je Arbeitsgang

**Aufgenommen 2026-10-19 (Gerald), Abgleich durch die Koordination** gegen den Bündel-Worktree v1.62.0, nur gelesen.
Quelle: [[bde_ideen]] (Lastenheft „Einführung BDE-System“, Abschnitte 5 Soll-Konzept, 6 Anforderungen 1–22, NFA, Schnittstellen).

## Neue Anforderung (Gerald 2026-10-19)

**Derselbe Arbeitsgang (Kürzel) soll an mehreren Werkbänken gestempelt werden können.**

Ist (am Code belegt):
- `WorkOperation` hängt an genau **einer** Werkbank (`ProductionWorkplaceId`, `Models/WorkOperation.cs:21`).
- Das Terminal zeigt nur Arbeitsgänge dieser einen Werkbank (`WorkOperationRepository.GetOpenByWorkplaceIdAsync:65`),
  der Scan sucht werkbank-gescopt (`/api/bde/resolve-scan`).
- Die Struktur-Erkennung legt bei einem Kürzel an mehreren Werkbänken **gar keinen** Arbeitsgang an („mehrdeutig“,
  `WorkOperationStructureDetectionService.cs:118-122`) – heute 8 Kürzel, u. a. EG 93 / KA 94 / VM 58 Aufträge.
- Die Buchung selbst speichert die tatsächliche Werkbank schon (`BdeBooking.ProductionWorkplaceId`, `Models/BdeBooking.cs:12`).

Vorschlag Koordination (für die Spec):
- Der Arbeitsgang gehört zum **Kürzel**, nicht zu einer Werkbank. Die Struktur-Erkennung legt ihn auch bei mehreren
  passenden Werkbänken an (Werkbank leer oder „bevorzugt“), statt „mehrdeutig“ zu überspringen.
- Das Terminal zeigt an jeder BDE-aktiven Werkbank alle offenen Arbeitsgänge, deren Kürzel zur Werkbank passt; der Scan
  `FA,AG` findet sie dort ebenso. Gebucht wird mit der tatsächlichen Werkbank (vorhanden).
- Das entschärft auch die veralteten W1-Werkbänke ([[2026-10-19-werkbank-katalog-veraltete-sage-arbeitsplaetze]]):
  sie blockieren nicht mehr, sind nur noch Stammdaten-Hygiene. **Beide Notizen in einer Spec behandeln.**
- Offene Frage: Reicht ein Kürzel je Werkbank (`ArbeitsschrittCode`), oder braucht eine Werkbank mehrere Kürzel
  (N:M wie `ProductionWorkplaceWorkStep` für Vorbau-AG)? Empfehlung: zuerst ein Kürzel je Werkbank, N:M bei Bedarf.

## Abgleich Lastenheft -> Ist-Stand

Legende: **ja** vorhanden · **teilw.** teilweise · **nein** fehlt. Belege Bündel v1.62.0.

| Nr. | Anforderung (Lastenheft) | Stand | Beleg / Lücke |
|---|---|---|---|
| 1 | Start/Stop-Zeiten erfassen | ja | `BdeBooking` (StartedAt/EndedAt, Pause/Finish, `BdeBookingService.cs:142`, `:242`), Typen Rüsten/Produktion/Tätigkeit (`BdeBookingType`) |
| 2 | Teileverfolgung | teilw. | AKE: OSEON-Teileverfolgung (`TrackingController`). IDEAL: nur BDE-Buchungen je Arbeitsgang, keine Teileverfolgung über Lagerplätze |
| 3 | Maschinenzeiten übernehmen (Oseon/WiCAM) | teilw./nein | Oseon: Tracking/Arbeitsgänge werden gelesen, keine Übernahme als BDE-Zeiten belegt. WiCAM: nichts |
| 4 | Erfassung manuelle Arbeitsbereiche | ja | Terminal je Werkbank (`BdeTerminal`), Operatoren, Schichtkalender, Auto-Pause |
| 5 | Hardware Terminals/MDE | teilw. | Terminal im Browser; MDE-Scanner: Scan-Handler-Spec ([[2026-10-15-scan-handler-hardware-scanner-spec]], Entwurf) |
| 6 | PPS Grob-/Feinplanung | teilw. | Arbeitsvorrat je Werkbank nach Fertigungstermin (`BdeTerminalAuftraege.cs:46`, `:64`), Soll-Termin per Offset; keine Grob-/Feinplanung, keine Kapazität |
| 7 | Mehrzeiten mit Grund | nein | Nur Storno-Grund (`BdeBooking.CancellationReason`); kein Mehrzeit-Grund |
| 8 | Mehrere Arbeiter am Arbeitsgang | ja (per Schalter) | AppSetting `BdeMehrfachBuchungProArbeitsgang` (IDEAL-Test: **aus**), geprüft in `BdeBookingService.cs:51`, `:183`; fremd gebuchte Arbeitsgänge bleiben sichtbar, `CloseOtherBookingsOnWorkOperationAsync` |
| 9 | Sammelauftrag | teilw. (per Schalter) | `BdeMehrfachBuchungProOperator` + `BdeGleichzeitigerAbschlussBeiMehrfachStart` (beide aus): parallele Buchungen, gemeinsamer Abschluss (`FinishGroupAsync`); keine Sammelpalette, Zeitaufteilung ungeprüft |
| 10 | Etikettendruck bei Fertigmeldung | nein | Barcode-Druck nur für Listen/Aufträge (`BarcodeService`) |
| 11 | Priorisierte Aufträge anzeigen | teilw. | Reihung nach Termin + Überfällig-Status; `Prio` aus FAInfos wird am Terminal nicht verwendet (ungeprüft im Detail) |
| 12 | Rückmelden mit einem Scan (SOLL=IST) | teilw. | NurFA-Modus (`AppSettingKeys.BdeNurFaMeldung`) meldet je FA; **keine SOLL-Zeiten** im Modell (`WorkOperation` ohne Soll-Zeit) -> SOLL=IST nicht möglich |
| 13 | Stücklisten mit Arbeitsgängen + SOLL-Zeiten | teilw. | Arbeitsgänge aus FA-Struktur (Kürzel); SOLL-Zeiten fehlen in Quelle und Modell |
| 14 | Fertigungsgrad in % | teilw. | Arbeitsgang-Status-Symbole je FA (FA-Struktur Teil 1–3); keine %-Summe |
| 15 | Chargen/Rückverfolgbarkeit | nein | kein Chargenfeld |
| 16 | FIFO bei mehreren Lagerplätzen | nein | Quell-Vorschlag: Hauptlagerplatz, dann Menge (v1.62.0) – kein Einlagerdatum |
| 17 | Transportlisten | teilw. | Kommissionierlisten je Arbeitsbereich (IDEAL); keine Transportlisten zwischen Bereichen |
| 18 | Rüstzeit-Optimierung (Biegeprogramm) | nein | Rüsten als Buchungstyp vorhanden, keine Werkzeug-Daten |
| 19 | Dokumente am Terminal | nein | Vault-Link nur in WMS-Listen, nicht im Terminal |
| 20 | Revisionierung Artikelnr./FA-Änderungen | teilw. | Audit-Felder (`AuditableEntity`), keine fachliche Änderungshistorie |
| 21 | Qualifikationsmatrix | nein | nur Benutzer je Werkbank (`ProductionWorkplaceUsers`), nicht Operatoren |
| 22 | Verbrauch außerhalb Stückliste | teilw. | Ausbuchung im WMS möglich, nicht aus dem BDE |
| NFA 1 | Grafische PPS (Fortschritt, Auslastung, Drag&Drop) | nein | Cockpit/Leitstand tabellarisch |
| S1 | BDE <-> Sage | nein | Setting `SageRueckmeldungAktiv` existiert (`Program.cs:281`), **keine Verwendung im Code** |
| S3.2/3.4 | WiCAM/Oseon -> BDE | nein/teilw. | s. Nr. 3 |
| Soll | Reporting (IST/Anwesenheit, SOLL-IST, Termintreue, Ausschuss) | teilw. | Gut-/Ausschussmenge je Buchung (`BdeBookingQuantity`), OSEON-Reporting; kein SOLL-IST, keine Anwesenheit |

## Entscheidungen / Angaben Gerald 2026-10-19

- **SOLL-Zeiten gibt es derzeit nicht.** Nr. 12 (SOLL=IST), 14 (Fertigungsgrad %), SOLL-IST-Reporting ruhen, bis eine
  Quelle existiert.
- **Neue Werkbank-Option „BDE nur AG-Fertigmeldung“** (ohne Beginn/Ende): An so markierten Werkbänken meldet der Werker
  einen Arbeitsgang nur **fertig** (Scan `FA,AG` bzw. Klick), ohne Start/Stop. Entspricht Lastenheft 3 „Rückmelden“/Nr. 12
  ohne SOLL-Zeit: festgehalten werden wer, wann, wo, Menge. Unterschied zum globalen `BdeNurFaMeldung`: je Werkbank, und
  auf Arbeitsgang- statt FA-Ebene.
- **Reihenfolge:** zuerst die vorhandenen Punkte ausbauen, danach die teilweisen.
- Ist-Settings (Screenshot IDEAL-Test): `BdeAktiv` an; `BdeNurFaMeldung`, `BdeMehrfachBuchungProOperator`,
  `BdeMehrfachBuchungProArbeitsgang`, `BdeGleichzeitigerAbschlussBeiMehrfachStart`, `BdeSchichtkalenderAktiv` aus.

## Plan nach Gerald (Koordination)

- **Stufe 1 – Vorhandenes ausbauen (eine Spec):** (a) Arbeitsgang an mehreren Werkbänken stempelbar, (b) veraltete
  W1-Werkbänke kennzeichnen ([[2026-10-19-werkbank-katalog-veraltete-sage-arbeitsplaetze]]), (c) Werkbank-Option
  „nur AG-Fertigmeldung“.
- **Stufe 2 – Teilweise Punkte:** 11 Priorität am Terminal, 9 Sammelauftrag, 2 Teileverfolgung IDEAL, 17 Transportlisten,
  6 Arbeitsvorrat/Planung – je eigene Backlog-Notiz/Spec, Reihenfolge mit Gerald.

## Empfohlene Reihenfolge (Koordination, überholt durch „Plan nach Gerald“ oben – SOLL-Zeiten gibt es nicht)

1. **Mehrere Werkbänke je Arbeitsgang + veraltete Werkbänke** (blockiert heute IDEAL-BDE) – eine Spec.
2. SOLL-Zeiten je Arbeitsgang (Quelle Sage/Struktur klären) – Voraussetzung für Nr. 12, 14, SOLL-IST.
3. Mehrzeit-Gründe (Nr. 7) und Priorität am Terminal (Nr. 11) – klein.
4. BDE -> Sage Rückmeldung (S1) – eigenes Epic, Sage-Schnittstelle klären.
5. Rest (Chargen, FIFO, Etiketten, Dokumente, Qualifikation, grafische PPS) als Backlog-Ideen.
