---
type: spec
title: Sage-PPS-Abgleich FA/BDE – Lesemodell, Reconciliation-Korrektur und BDE-Rückmeldung an Sage (Epic)
slug: 2026-11-02-sage-pps-fa-bde-abgleich-spec
status: Entwurf
created: 2026-11-02
updated: 2026-11-02
source_backlog: "[[2026-11-02-sage100-pps-fa-bde-recherche]]"
task: ""
worktree: ".claude/worktrees/sage-pps-fa-bde"
branch: "spec/sage-pps-fa-bde-abgleich"
standort: beide
affected_code:
  - "IDEALAKEWMSService/Services/FaMaterializationPlanner.cs (K1: Grund 'in Material umgewandelt')"
  - "IDEALAKEWMSService/Services/FaMaterializationSyncService.cs (K1: Grund + SagePosition schreiben, Sammelmeldung; K3: Verursacher-Plausibilität)"
  - "IdealAkeWms/Models/ProductionOrder.cs (K1: SageMissingReason, SagePosition)"
  - "IdealAkeWms/Views/ProductionOrders/_ProductionOrderRow.cshtml (K1: Badge je Grund)"
  - "IdealAkeWms/Services/BdeBookingService.cs (K1: Buchungssperre für umgewandelte Sub-FAs, vor jedem Schreiben)"
  - "IdealAkeWms/Models/SageFaState.cs (neu, K2 Lesemodell) + ApplicationDbContext"
  - "IDEALAKEWMSService/Services/SageFaStateSyncService.cs (neu, K2) + Workers/SyncWorker.cs (Block + K5-Guard)"
  - "IdealAkeWms/Services/SageSdata/SageSdataReadClient.cs (Wiederverwendung im Service, ggf. Felder ergänzen)"
  - "IdealAkeWms/Models/ServiceSettingDefinitions.cs (Sage:Mandant, Sync:SageFaStatusEnabled, SageRueckmeldung*)"
  - "IDEALAKEWMSService/Services/SageBestandReader.cs:33, SageLagerplatzReader.cs:27 (K4: Mandant aus Setting, s. Rückfrage 11)"
  - "IdealAkeWms/Models/SageRueckmeldungQueueItem.cs (neu, Etappe 2) + Repository"
  - "IDEALAKEWMSService/Workers/SageRueckmeldungWorker.cs (neu, Etappe 2) + ISageRueckmeldungClient (eine Implementierung je Freigabe A oder C)"
  - "IdealAkeWms/Controllers/SageRueckmeldungQueueController.cs + Views (neu, Listen-View-Pattern)"
  - "IdealAkeWms/Program.cs:290-291 + Views/Settings/Index.cshtml:39 (AppSetting SageRueckmeldungAktiv entfernen)"
  - "IdealAkeWms/Services/SageBookingEnqueueDecision.cs:14-15 (nur Etappe 3, je Rückfrage 3)"
  - "IdealAkeWms/Services/SyncLogger/SyncLogServices.cs (SageFaStatus, SageRueckmeldung)"
  - "SQL/98_*.sql, SQL/99_*.sql (Nummern bei Umsetzung prüfen), SQL/00_FreshInstall.sql (zwei Stellen je Migration)"
  - "docs/TESTSZENARIEN.md, secondbrain/tests/testszenarien-index.md, README.md, Views/Help/*"
open_questions:
  - "G1: Schreibweg Variante A (Partner-Service via SData) oder C (SageBridge .NET 4.8 x86)? Entscheidung = freigabe_entscheidung"
  - "G2: Materialentnahme in Sage: retrograd je AG, explizit als Material-RM aus dem WMS, oder über die Gesamtfertigmeldung?"
  - "G3: Sollen FA-bezogene WMS-Ausbuchungen ab Etappe 3 als Material-RM statt als Lagerbuchung laufen (K6)?"
  - "G4: K5 (Guard flacher FA-Sync im hierarchischen Modus) vorziehen als eigener Kleinfix oder in Etappe 1?"
  - "G5: Wann laufen die Phase-2-Prüfabfragen Q0, Q3, Q4/Q4b/Q5, Q7, Q11, Q12 je Installation (Gate vor Etappe 1)?"
  - "G6: 'Arbeitsbereit' nur anzeigen oder nicht eingelastete/beendete/gesperrte FAs im BDE-Terminal und in Listen ausblenden?"
  - "G7: Was löst im WMS die Sage-Fertigmeldung eines (Sub-)FA aus, und mit welcher Menge?"
  - "G8: Korrektur nach Versand: Bearbeiten/Stornieren einer an Sage gemeldeten BDE-Buchung sperren oder als Konflikt melden? Haltefrist-Default?"
  - "G9: Ist BdeOperator.PersonnelNumber die Sage-Mitarbeiternummer? Zeit-RM von Werkbänken ohne SageArbeitsplatznummer senden oder zurückhalten?"
  - "G10: Basis des Epic-Worktrees (Bündel-Branch oder main nach dessen Merge) und Branch-Präfix spec/ beibehalten?"
  - "G11: Neuer Key Sage:Mandant oder vorhandenen Sync:ProductionWorkplaceSyncMandant mitverwenden; die zwei harten '= 1' mit umstellen?"
  - "G12: Statusquelle für das Lesemodell: Sage-View-Spalten per SQL oder SData dtsFaBelegsuche?"
  - "G13: AKE: Ist [WA Nummer] der WAListe die BelID des FA (Rückmeldeadresse)? Erst relevant, wenn AKE-BDE aktiv wird"
  - "G14: Rollen: Queue lesen ab bde_shiftlead, Requeue/Klären nur bde_admin – passt das?"
  - "D1: Grundlageneinstellungen Produktion je Installation: Ist-Werte und verbindliche Zielwerte"
  - "D2: Welche Lizenz (Third-Party/Partner) braucht eine Rückmeldung über die Sage-Engine für Weg A bzw. C?"
  - "D3: Kann DPS einen Rückmelde-Service im Stil LagerbuchungService liefern (Vertrag unten)? Alternativ SDK-Zugang + Signaturen RueckmeldungEngine"
  - "D4: Meldet DPS-MDE heute schon Rückmeldungen oder Unterbaugruppen an Sage (R33)?"
  - "D5: SData-Host und Dataset der IDEAL-Sage-Installation, gleicher Vertragsstand wie AKE?"
  - "D6: Gibt es Trigger/Partnerlogik auf KHKPps*/KHKLager*, die bei Rückmeldungen mitläuft (Q10)?"
  - "D7: In welchem Sage-Feld kann die WMS-GUID je Rückmeldung abgelegt und gesucht werden (R25)?"
  - "D8: Wie unterscheidet die Engine transiente Fehler (FA gesperrt) von endgültigen (beendet, gelöscht)?"
  - "D9: Bedarfsprüfung beim Einlasten: Wird der gelöschte Unter-FA protokolliert, und wird die Vaterposition zu Material mit BelIDNachfolger = 0 (Q12)?"
  - "D10: Zeit-RM geplant auf AG-Position (BelPosID) oder ungeplant mit Position 0, wenn die FAs keine AG-Positionen haben (Q6b)?"
epic: true
etappen:
  - "1 Lesemodell + Reconciliation-Korrektur (K1–K5), nur lesend"
  - "2 Einstufig: Zeit-RM + Fertigmeldung über Rückmelde-Queue"
  - "3 Material-RM (ggf. mit Charge)"
  - "4 Mehrstufig: Fertigmeldung bottom-up über Unterbaugruppen"
deploy:
  web: true
  service: true
  migration: true
freigabe_entscheidung: ""
freigabe_von: ""
freigabe_am: ""
---

> [!info] Abstimmung mit [[2026-11-06-sage-sdata-stammdaten-sync-spec]] (Gerald 2026-11-06, G15)
> Das SData-Sync-Epic wird zuerst umgesetzt, erhält SQL/98 ff. und legt `SageFaStates` (E4b) in der hier geplanten Form an. Dieses Epic nutzt die
> Tabelle und vergibt seine SQL-Nummern beim Start neu. Außerdem: Rüst-/Stückzeit und Standard-Arbeitsplatz der Arbeitsgänge gehören hierher (G12 dort).


# Sage-PPS-Abgleich FA/BDE – Lesemodell, Reconciliation-Korrektur und BDE-Rückmeldung an Sage

**Grundlagen:** Recherche [[2026-11-02-sage100-pps-fa-bde-recherche]] · Abgleichsbericht [[2026-11-02-sage-pps-abgleich]]
(Befundnummern R/W/Z/B, Code-Stand Bündel-Worktree v1.71.0) · Prüfabfragen [[2026-11-02-sage-pps-pruefabfragen]] (Q0–Q14) ·
Vor-Analyse [[2026-10-30-fa-rueckmeldung-an-sage]] mit Anhang [[sdata-befund-ake-test-2026-10-30]] · Lastenheft
[[2026-10-19-bde-lastenheft-abgleich]] / [[bde_ideen]] (S1 „BDE ↔ Sage“).
Vorgänger: [[2026-07-29-sage-lagerbuchungen-spec]] (Queue/Worker), [[2026-08-20-materialisierung-fachliche-felder-spec]],
[[2026-10-19-bde-stufe-1-werkbaenke-ag-fertigmeldung-spec]], [[2026-10-30-sage-sdata-fa-ansicht-spec]], Reconciliation-Design
`docs/superpowers/specs/2026-07-07-fa-reconciliation-design.md`. ADRs: [[0004-migrations-und-sql-disziplin]],
[[0005-listen-view-pattern-mit-server-side-spaltenfilter]], [[0008-servicesettings-db-first-mit-typisiertem-katalog]],
[[0009-app-status-in-satelliten-tabellen-neben-sage-master]], [[0010-aktivitaets-protokoll-mit-isolierten-dbcontexts]],
[[0012-fa-hierarchie-einweg-migrationstor]], [[0014-werkbank-datenhoheit-sage-fuehrend-mit-abweichungsmeldung]].

> [!warning] Worktree und Branch sind noch NICHT angelegt
> `worktree`/`branch` im Frontmatter sind Vorschläge. In diesem Lauf wurde kein Git-Befehl ausgeführt. Der Code, auf dem die Spec
> aufsetzt (Materialisierung, `SageSdataReadClient`, BDE Stufe 1), existiert nur im Bündel-Branch
> `feature/2026-08-07-ideal-teile-1-5` (v1.71.0). Die Basis des Epic-Worktrees ist Rückfrage G10. Während der Arbeit den Branch mit
> `scripts/sync-worktree.ps1` aktuell halten.

**Standort:** `beide`. Die Etappen 1 und 2 wirken zuerst an **IDEAL**, weil dort das BDE im Einsatz ist (Master
`ProduktionsauftragHierarchisch`). An AKE ist das BDE derzeit nicht aktiv. K4 und K5 gelten standortneutral. Die
Rückmeldeadresse an AKE ist ungeprüft (G13).

## Ziel / Nutzen (das Warum)

Heute fließt aus dem BDE nichts an Sage (R28, `SageLagerbuchungClient.cs:45` ist der einzige POST). FAs bleiben in Sage offen,
die Nachkalkulation bleibt leer, und die Dispo sieht Bedarf und Zugang doppelt. Außerdem behandelt das WMS zwei Sage-Vorgänge falsch
oder gar nicht: einen Unter-FA, den die Bedarfsprüfung in Material umwandelt (R8/R9), und den FA-Status (R7). Das Epic

1. macht den Sage-Zustand je FA im WMS sichtbar (Status, Rückmeldestand), nur lesend,
2. korrigiert die Abgleiche (K1–K6),
3. meldet BDE-Ergebnisse über **genau einen** freigegebenen Schreibweg an Sage zurück: Zeit, Fertigmeldung, Material.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope:** K1–K5 (Etappe 1), Rückmelde-Queue mit Adapter (Etappen 2–4), Queue-Liste mit Requeue/Klären, Schalter und
DEV-Absicherung, Brain/Doku/Testszenarien.

**Out-of-Scope (ausdrücklich):**
- Die Stückliste in Sage ändern. Mehrverbrauch geht als Material-RM, nicht als Ressourcenlisten-Änderung (Vor-Analyse,
  „Fachliche Empfehlung“).
- In KHK-Tabellen schreiben, egal ob per SQL-INSERT, UPDATE oder Prozedur (R10, Recherche §6 „Nicht tun“, Weg D der Vor-Analyse).
- Sage-Grundlageneinstellungen aus dem WMS ändern. Sie sind Voraussetzung (a) und werden mit DPS abgestimmt.
- FA in Sage anlegen, einlasten, reaktivieren oder löschen.
- Weg B (`strFaBeleg.Sage.PPS` `Execute`, 403, MethodNames undokumentiert; R11).
- AKE-BDE-Rückmeldung, solange G13 offen ist.
- **Brain-Korrektur B-1** ist bereits erledigt (Korrekturnotiz in [[integrationen]]). Sie wird hier nur erwähnt.

## Fachliche Anforderungen

### Korrekturen am Bestand

**K1 – „In Material umgewandelt“ erkennen (R8, R9, Z-4; Annahme A-1).**
Der Ort ist die IDEAL-Materialisierung, nicht (nur) die Reconciliation aus Migration 80. Diese läuft nur flach per `OrderNumber`
über die WAListe (`ProductionOrderReconciler.cs:36-68`, `SageImportService.cs:215-327`). Verschwundene Sub-FAs markiert
`FaMaterializationPlanner.cs:68-86` mit `SageMissingSince` (`FaMaterializationSyncService.cs:161-182`). Einen Grund erfasst der
Code dabei nicht.
- Regel: Ein Sub-FA *X* fehlt in der Struktur (Regel 2 des Planers). In der aktuellen Struktur gibt es unter seinem Vater
  (`VaterFA = X.ParentSubOrderNumber`) eine Blattzeile (`SubFA = 0`) an derselben Position (`Position = X.SagePosition`) mit
  demselben Artikel (`Artnr = X.ArticleNumber`). Dann ist der Grund **„in Material umgewandelt“**, sonst **„vermisst“**.
- `ProductionOrder` speichert die Position heute nicht (`FaHierarchyNode.Position`, `FaHierarchyNode.cs:28`, wird nicht
  materialisiert, `FaMaterializationSyncService.cs:144-156`). Deshalb gibt es die neue Spalte `SagePosition`. Die Materialisierung
  füllt sie im Anlege- und im Update-Zweig. Sub-FAs, die schon vor dem Deploy vermisst waren, haben keine Position. Für sie gilt
  ersatzweise `VaterFA + Artnr`, und das Protokoll weist diesen Fall als „Abgleich nur über Artikel“ aus.
- Neues Feld `ProductionOrder.SageMissingReason` (int?, 1 = Vermisst, 2 = InMaterialUmgewandelt). Es wird zusammen mit
  `SageMissingSince` gesetzt und beim Wiederauftauchen zusammen mit ihm geleert. Der Fallstrick „`SageMissingSince` ist ein
  Zeitstempel, nicht `IsCancelled`“ gilt weiter: **kein Storno**.
- Bereits vermisste Zeilen mit Grund `NULL` stuft der nächste Lauf einmal ein (Backfill im Lauf, nicht per SQL).
- **Melden statt still behandeln:** Je Lauf schreibt das Protokoll eine Sammelwarnung je Grund mit den Sub-FA-Nummern. Neue
  Counts-Keys sind `in_material_umgewandelt` und `vermisst_neu` (heute vorhanden). Das Badge in `_ProductionOrderRow.cshtml:57-59`
  unterscheidet die Gründe im Text, nicht nur in der Farbe: „in Material umgewandelt seit …“ bzw. „vermisst seit …“.
- **Rückmeldung sperren (Z-4):** Ein vermisster Sub-FA bleibt wie heute rückmeldefähig (Tooltip „Rückmeldung weiterhin möglich“
  ist gewollt). Ein *umgewandelter* Sub-FA nimmt keine BDE-Buchung mehr an. Die Prüfung steht in `BdeBookingService` **vor jedem
  Schreiben**, in der Reihenfolge aus [[fallstricke]] §28. Offene Queue-Einträge eines umgewandelten FA gehen ab Etappe 2 auf
  „Konflikt“.
- **Annahme A-1 (ungeprüft, W2/R8):** Die Bedarfsprüfung löscht den Unter-FA. Die Vaterposition bleibt als MA-Position mit
  `BelIDNachfolger = 0` stehen, und die View liefert sie als Blattzeile. Prüfung: Q4b und Q12 vor/nach einem Einlasten, D9. Zeigt
  Q12 ein anderes Bild (Position entfällt, oder der Unter-FA steht im Protokoll), wird die Regel vor der Umsetzung angepasst.

**K2 – Statusableitung (R4–R7).**
- Lesemodell je BelID: `IstEingelastet`, `IstBegonnen`, `IstBeendet`, `IstGesperrt`, dazu `IstReaktiviert`, sofern die Quelle es
  liefert. `dtsFaBelegsuche` liefert es heute nicht (`SageSdataFaModels.cs:70-71`). Abgeleitet wird an **einer** Stelle
  (`SageFaStatusRegel`, Muster `ArbeitsgangStatusRegel`) zu Neu / Eingelastet / Begonnen / Beendet / Gesperrt (/ Reaktiviert).
  Sage-Boolean `-1` gilt als wahr (R22, `SageBool.IsTrue`).
- „Arbeitsbereit“ heißt eingelastet, nicht beendet und nicht gesperrt. **Etappe 1 zeigt den Status nur an.** Ob das WMS danach
  filtert oder ob die Views das schon tun, entscheidet sich erst nach Q3/Q11 (G6). Kein WMS-Lesepfad filtert heute auf Status
  (`SageImportService.cs:47-59`, `FaHierarchySql.cs:139-163`).
- Quelle (G12): Variante **S-SQL** liest Sage-View- oder Tabellenspalten über `SageConnection`, sofern Q3/Q11 sie liefern. Variante
  **S-SData** liest `dtsFaBelegsuche.Sage.PPS` über den vorhandenen `SageSdataReadClient`.
  > [!note] Abweichung vom Auftrag
  > Der Auftrag nennt `dtsFaUebersicht` als „bereits gelesen“. Gelesen wird tatsächlich `dtsFaBelegsuche`
  > (`SageSdataFaModels.cs:8,65`). `dtsFaUebersicht` wurde nur per Postman getestet ([[sdata-befund-ake-test-2026-10-30]] §6).
  > Außerdem liegt der Client im **Web**-Projekt (v1.71.0, nur Anzeige). Für das Lesemodell muss ihn der Service mitregistrieren.
  > S-SData setzt einen bekannten IDEAL-SData-Host voraus (D5). Heute zeigt IDEAL-DEV auf den AKE-Sage-Test
  > ([[2026-10-29-dev-umgebung-lokal]], Nachtrag 2026-11-02).
- Sage-Rückmeldestand je BelID: Summe `MengeGut` aus `dtsRueckKorrektur` und `dtsFertigmeldungen` (`SageSdataQuellen`,
  `SageSdataFaModels.cs:21-22`). Am AKE-Test waren beide Quellen **leer**. Erprobt ist damit nur der Aufruf, der Inhalt nicht.

**K3 – Hierarchie-Sync bleibt bei BelIDNachfolger (R2, W1, W2).**
Die Kanten kommen weiter aus der Struktur-View (`VaterFA`/`SubFA`, laut Doku `KHKPpsFaBelegePositionen.BelIDNachfolger`,
`SQL/sage-views/vw_IDEAL-AKE_Kommissionierung_FAListe.sql:23-27`). Der Viewtext liegt nicht im Repo, das sagt erst Q11.
`KHKPpsFaBelegeBedarfVerursacher` (Typ 70) dient nur der **Plausibilitätsprüfung**: Weicht eine Verursacher-Kante von der
Struktur-Kante ab, schreibt der Materialisierungslauf eine Invarianten-Warnung mit beiden BelIDs. Kanten werden nie geändert. Die
Deutung von `VerursacherID` ist [A] (Recherche §6). Gebaut wird K3 erst, wenn Q5 die Spalten belegt hat. Liefern Q4b und Q5 an
beiden Installationen identische Kanten, entfällt K3 (YAGNI) und wird hier als „entfallen“ vermerkt.

**K4 – Mandant und Standort-Schlüssel (R23, R24, W5).**
- **Kein** Standort-Feld. Jede WMS-Installation spricht genau eine Sage-Installation an (`IDEALAKEWMSService/appsettings.json:3-6`,
  `StandortSettingsCatalog.cs:29,48-55`). Ein Standort-Feld käme erst mit einer Mehr-Sage-Installation.
- Mandant als ServiceSetting `Sage:Mandant` (Int, Default `1`) statt hartem `= 1` (`SageBestandReader.cs:33`,
  `SageLagerplatzReader.cs:27`). Es gibt schon `Sync:ProductionWorkplaceSyncMandant` (`ServiceSettingDefinitions.cs:45`); ob der
  neue Key ihn ersetzt, ist G11. Alle neuen Sage-Leser dieses Epics filtern mit dem Setting.
- Das Rückmelde-Mapping (Etappe 2) speichert `Mandant` + `BelID` je Eintrag.
- W5 (Mandant = 1 an beiden Installationen) ist ungeprüft und wird mit Q0 belegt.

**K5 – Guard: flacher WAListe-Sync nicht im hierarchischen Modus (Z-1).**
Storno, Reaktivieren und Upsert arbeiten mit `WHERE OrderNumber = @x` (`SageImportService.cs:284,320`). Im IDEAL-Schema trifft das
die ganze Gruppe ([[fallstricke]] §9). Heute schützt nur die Betriebsvorgabe `Sync:ProductionOrdersEnabled = false`. Der Default
des Schalters ist zudem **`true`** (`SyncWorker.cs:43`): Fehlt die Zeile, läuft der Sync. Neu: Ist der Master
`ProduktionsauftragHierarchisch = true`, überspringt `SyncWorker` den Block „Produktionsauftraege-Sync“ samt eingebetteter
Reconciliation. Jeder Lauf schreibt eine sichtbare Warnung („übersprungen: hierarchischer Modus“). Für AKE (Master false) ändert
sich nichts. Ob K5 vorgezogen wird, ist G4.

**K6 – Doppel-Entnahme (R19).**
WMS-Ausbuchungen gehen heute als „Entnahme“ **ohne FA-Bezug** an Sage (`SageLagerbuchungPayloadBuilder.cs:34-54`,
`SageBookingEnqueueDecision.cs:14-15`, `StockMovement.ProductionOrder` wird gespeichert, aber nicht gesendet,
`StockMovementsController.cs:130`). Solange keine Material-RM aktiv ist, bleibt das **unverändert**. Mit Etappe 3 gilt genau eine
Regel (G2/G3): Entweder läuft eine FA-bezogene Ausbuchung als Material-RM statt als Lagerbuchung, oder sie bleibt Lagerbuchung und
das WMS sendet keine Material-RM. Beides parallel ist ausgeschlossen.

### Neue Implementierung

**Lesemodell (Etappe 1).** Neue Cache-Tabelle `SageFaStates`, eine Zeile je (Mandant, BelID). Spalten: StrukturID, Status-Bits,
Sage-MengeGut rückgemeldet/fertiggemeldet, `SyncedAt`. Sie ist kein `AuditableEntity`, wie `FaHierarchyNode` (`FaHierarchyNode.cs:7-11`).
Neuer Service-Schritt `SageFaStateSyncService` läuft nach der Materialisierung. Gates: Master und `Sync:SageFaStatusEnabled`
(Default false). Er liest nur BelIDs offener, materialisierter FAs (`SubOrderNumber`, je 50 BelIDs ein Aufruf,
`SageSdataQuellen.OrBloecke`) und macht ein Upsert. **Empty-Guard:** Bei Lesefehler oder leerer Antwort bleiben die alten Werte
stehen, das Protokoll warnt, und es wird nie geleert. Angezeigt wird der Status in der FA-Struktur und in der FA-Liste (IDEAL):
Status-Chip in Textform plus „Sage gut / WMS gut“.

**Rückmeldungen (Etappen 2–4)** stammen aus `BdeBooking` (Zeiten, `BdeBooking.cs:31-36`), `BdeBookingQuantity` (Gut/Ausschuss,
`IsFinal`, `BdeBookingQuantity.cs:16-25`) und `ReportCompletionAsync` (`BdeBookingService.cs:268`, Fertigmeldung = Buchung mit
`StartedAt == EndedAt`, [[fallstricke]] §28). Material kommt aus `StockMovement` mit FA-Bezug (Etappe 3). Nicht verwendet wird
`WorkOperation.IsReported`: Das ist der WMS-Rückmeldestatus der Teileverfolgung (`TrackingController.cs:254,283`), nicht „an Sage
gemeldet“.

**Schreibpfad: zwei Varianten, eine WMS-Schnittstelle (Entscheidung bei Schranke 1 = `freigabe_entscheidung`).**

| | **A – Partner-Service (DPS)** | **C – SageBridge (eigen)** |
|---|---|---|
| Was | DPS stellt einen SData-Service im Stil des `LagerbuchungService` bereit | eigener .NET-4.8-x86-Dienst auf einem Rechner mit Sage-Client, ruft `Sagede.OfficeLine.Pps.RueckmeldungEngine` (Session `ApplicationEngine.CreateSession`, Dispose zwingend; R15) |
| WMS-Adapter | `ISageRueckmeldungClient` → HTTP-POST an SData (Muster `SageLagerbuchungClient`, `Accept: application/json` Pflicht, TLS-Policy) | dieselbe Schnittstelle → HTTP-POST an die Bridge |
| Voraussetzungen | D2, D3 (Vertrag), D5, D7, D8 | D2, D3 (SDK + Signaturen, R13/R14), Bridge-Host, **neuer ADR** (erstes Nicht-.NET-10-Projekt, 32 Bit) |
| Risiko | Abhängigkeit von DPS-Lieferung | Engine-API undokumentiert (R14); Lizenz- und Session-Handling liegen bei uns; Betrieb eines zweiten Dienstes |
| Wiederverwendung | Queue/Worker/Idempotenz-Muster vollständig | dasselbe auf WMS-Seite, dazu eine eigene Bridge |

**Empfehlung: A.** Gründe: Vor-Analyse Weg A; DPS kennt Struktur und Pakete auf dem Server (`DPSBS_MDE*`, `AKE_InterneFA`); kein
eigener 32-Bit-Dienst mit Sage-Session-/Lizenzpflichten; der Betrieb bleibt wie bei der Lagerbuchung. C ist der Rückfall, wenn DPS
nicht liefert. Für beide gilt **derselbe** Vertrag. Bei A bauen wir ihn als Anforderung an DPS, bei C als Bridge-Endpunkt:
- `POST rueckmeldung`: `{ CorrelationId (GUID), Mandant, BelID, BelPosID? , Typ (Zeit|Material|Fertigmeldung|Beenden), MengeGut,
  MengeAusschuss, Start, Ende, Mitarbeiternummer, Arbeitsplatznummer, Artikelnummer?, Lagerplatz?, Charge? }` →
  `{ Ergebnis: Ok|Transient|Konflikt, RueckmeldungID?, BatchID?, Meldung }`
- `GET rueckmeldung/{CorrelationId}` → vorhanden ja/nein (+ IDs), für die Recovery hängender Einträge.

Das Interface hat später nur eine Produktiv-Implementierung. Es ist trotzdem gerechtfertigt: wegen Fake im Worker-Test (Muster
`ISageLagerbuchungClient`) und wegen des Variantentauschs. Weitere Abstraktion (Factory, Laufzeitumschaltung A/C) gibt es nicht.
**Kein SQL-INSERT in KHK*** (R10).

**Rückmelde-Queue (Etappe 2), eigene Tabelle statt Erweiterung von `SageBookingQueueItems`.** Begründung: Die Lagerbuchungs-Queue
hängt per Pflicht-FK an `StockMovement` (`SageBookingQueueItem.cs:15-17`) und kennt weder Typ, BelID, Struktur noch Backoff.
Ihr Worker ist auf Lagerbuchung + Memo-Lookup zugeschnitten (`SageBookingWorker.cs:85-170`). Eine Erweiterung würde beide Pfade
koppeln und die laufende Lagerbuchung gefährden.
- `SageRueckmeldungQueueItem : AuditableEntity`: `CorrelationId` (GUID, UNIQUE), `Mandant`, `BelID`, `StrukturID`,
  `ProductionOrderId`, `Typ`, Quelle (`QuelleArt` + `QuelleId`, UNIQUE zusammen mit `Typ`; zusätzlich App-Guard, weil InMemory
  UNIQUE nicht erzwingt), `PayloadJson` (Snapshot beim Einreihen), `Status` (Offen / Gesendet / Bestätigt / Fehler / Konflikt /
  Geklärt), `AttemptCount`, `NextAttemptAt`, `LastAttemptAt`, `LastError`, `SageResponseRaw`, `SentAt`, `ConfirmedAt`,
  `SageRueckmeldungId`, `SageBatchId`.
- **Einreihen per Sweep im Worker**, nicht per Decorator: beendete, nicht stornierte Buchungen, älter als die Haltefrist
  (`Sync:SageRueckmeldungHaltefristMinuten`), beendet nach `SageRueckmeldungAb` (verhindert, dass beim Einschalten die Historie
  nachgesendet wird), ohne Queue-Eintrag. Die Haltefrist lässt Korrekturen in `BdeBookingsController.Edit/Cancel`
  (`:206-220`, `:252-256`) vor dem Versand zu. Was danach passiert, ist G8.
- **Idempotenz:** Status vor dem HTTP-Call auf „Gesendet“. Hängende „Gesendet“-Einträge prüft `GET …/{CorrelationId}`, blind neu
  gesendet wird nie. Das ist das Muster `SageBookingCorrelation`/`SageBuchungLookupReader`. Wo Sage die GUID ablegt, ist offen
  (R25/W4 ungeprüft, D7). Bei Bestätigung werden `RueckmeldungID`/`BatchID` gespeichert. Voraussetzung bleibt **genau eine**
  Worker-Instanz ([[fallstricke]] §8).
- **Serialisierung je StrukturID:** Je Struktur ist höchstens ein Eintrag gleichzeitig „Gesendet“. Reihenfolge: Zeit → Material →
  Fertigmeldung, bei Fertigmeldung tiefere Ebene vor höherer, sonst `CreatedAt`. Steht ein früherer Eintrag der Struktur auf Fehler
  oder Konflikt, wartet der Rest sichtbar („blockiert durch #…“).
- **Vorprüfung aus dem Lesemodell:** nicht eingelastet → bleibt offen („wartet auf Einlastung“, ohne Versuchszählung); gesperrt →
  Transient; beendet, gelöscht oder umgewandelt (K1) → Konflikt. Maßgeblich bleibt die Antwort des Adapters.
- **Automatischer Retry mit Backoff (Z-3):** Bei Transient bekommt der Eintrag wieder Status Offen, `AttemptCount + 1` und
  `NextAttemptAt = now + min(Basis · 2^n, Max)`. Erst nach `Sync:SageRueckmeldungMaxRetries` wird er zu „Fehler“, und es geht eine
  Fehlermail raus (Schwelle je Lauf). Der Ist-Fehler des Lagerbuchungs-Workers (setzt sofort Fehler, `SageBookingWorker.cs:150`,
  `SageBookingQueueRepository.cs:48,131`) wird **nicht** kopiert.
- **Konflikt – manuelle Klärung:** kein automatischer Retry. In der Liste stehen „Erneut senden“ und „Als geklärt markieren“, mit
  Pflichtnotiz und Audit-Feldern.
- **Protokoll:** `ISyncLogger` als letzter Ctor-Parameter, neuer Name `SyncLogServices.SageRueckmeldung` in `SyncLogServices.All`,
  deutsche Counts-Keys (`eingereiht`, `gesendet`, `bestaetigt`, `wiederholung`, `konflikt`, `fehler`). Siehe
  [[0010-aktivitaets-protokoll-mit-isolierten-dbcontexts]].
- **Liste/Requeue-UI** nach [[0005-listen-view-pattern-mit-server-side-spaltenfilter]]: `PageSize.Resolve` + `_Pagination`,
  Filterkarte (Status, Typ, Struktur), Server-Spaltenfilter (`data-col-key` an allen `<th>`, `ColumnFilterTypes`-Register, viewKey in
  `ColumnDefinitions.GetByViewKey`). Vorlage ist `SageBookingQueueController`.
- **Service-Keys** in `ServiceSettingDefinitions.All` (Drift-Guard): `SageRueckmeldungAktiv` (Bool, false), `SageRueckmeldungAb`
  (String/Datum), `Sync:SageRueckmeldungIntervalSeconds` (20), `…BatchSize` (50), `…MaxRetries` (8), `…BackoffMaxMinutes` (60),
  `…HaltefristMinuten` (30, G8), `…MaxErrorsPerRun` (20), dazu bei A die Service-URL-Segmente. Zugangsdaten bleiben appsettings-only.
- **Schalter `SageRueckmeldungAktiv`: ServiceSetting statt AppSetting.** Heute ist er ein AppSetting (Seed `Program.cs:291`,
  Anzeige `Views/Settings/Index.cshtml:39`), das niemand liest (R28). Gründe für die Umstellung: (1) Leser ist der Service-Worker, wie
  bei `SageLagerbuchungAktiv` (`SageBookingWorker.cs:66`); (2) die DEV-Absicherung arbeitet auf `ServiceSettings`; (3) Service-Keys
  gehören nach ADR 0008, AppSettings sind fachliche Web-Toggles (ADR 0011). Das AppSetting wird entfernt (Seed, Anzeige, verwaiste
  Zeile per Skript), damit es keine zwei gleichnamigen Schalter gibt.
- **DEV-Sicherheit:** DEV darf nie buchen ([[2026-10-29-dev-umgebung-lokal]], Entscheidung 2). `SageRueckmeldungAktiv = false`
  kommt in `D:\Dev\scripts\dev-absicherung-{ake,ideal}.sql` (Schalter-Set + Prüfzeile `NichtFalse`) und in die „Hard limits on DEV“
  des qa-agent. Das Skript liegt außerhalb des Repos und wird von Gerald bzw. vom qa-agent gepflegt. Bei C gilt zusätzlich: keine
  Bridge-URL in `appsettings.DEV.json`.

## Voraussetzungen

**(a) Verbindliche Grundlageneinstellungen je Installation (mit DPS, D1).** Ist-Werte ungeprüft (Q7, R16). Zielwerte nur als
Empfehlung [A] aus Recherche §4/§5:

| Einstellung | Empfehlung [A] | Warum |
|---|---|---|
| Materialentnahmen buchen | „Manuell bzw. Gesamtfertigmeldung“ (Pflicht bei Chargen, R17) | keine automatische Doppelentnahme |
| Verf. Mengen aus Lager rückmelden | „Keine“ | „bei Einlastung“ + „automatisch bei FM AG“ = zwei Entnahmen |
| Gesamtfertigmeldung bucht | „Rest zu Soll“ | Achtung Schwund-Fehler „zu viel zurückgemeldet“ (R29, Version Q0) |
| Zugang Fertig-/Halbfertigteile buchen | mit DPS festlegen (manuelle RM letzter AG / Gesamtfertigmeldung) | bestimmt G7 |
| Letzter Arbeitsschritt löst Fertigmeldung aus | mit DPS | dto. |
| Übernahme Lager in FA | nicht „keines“ ohne Hauptlager | sonst fehlerhafte RM |
| Bedarfsprüfung Unterbaugruppen | Ist-Wert dokumentieren | erzeugt K1-Fälle (R8) |
| Unterbaugruppen rückmelden (DPS-Hb.: „entfällt, da Nutzung BDE“) | Ist-Wert prüfen | R33, D4 |

**(b) SDK/Lizenz (R13):** Third-Party-/Partnerlizenz für Rückmeldungen über die Engine klären (D2). Ohne Klärung gibt es keine
Etappe 2.
**(c) Schnittstelle (R14/R15):** Bei A der unterschriebene Vertrag oben (D3, D7, D8). Bei C SDK-Zugang, Signaturen der
`RueckmeldungEngine` und Assemblies auf dem Bridge-Host.
**(d) Gate vor Etappe 1:** Q0, Q3, Q4/Q4b/Q5, Q7, Q11, Q12 sind je Installation gelaufen und liegen unter
`secondbrain/tests/anhaenge/2026-11-02-sage-pps-abgleich/`. Die Spalte „Befund Sage“ im Bericht ist nachgetragen (G5).

## Technischer Lösungsentwurf

- **Muster:** reiner Planer + Sync-Service (Materialisierung), Repository-Interfaces, Satelliten-/Cache-Tabellen
  ([[0009-app-status-in-satelliten-tabellen-neben-sage-master]]), Queue + eigener `BackgroundService` (Vorbild `SageBookingWorker`),
  Listen-View-Pattern.
- **K1:** `FaMaterializationPlanner.Plan` bekommt als zusätzlichen Eingang die Blattzeilen (VaterFA, Position, Artnr) und liefert je
  vermisstem Key den Grund. Der Planer bleibt DB-frei und unit-getestet. Der Service schreibt Grund und `SagePosition`.
- **K2:** `SageFaStateSyncService` im `SyncWorker` nach der Materialisierung, `RunResilientAsync`, eigener Protokoll-Lauf.
- **K5:** eine Bedingung im `SyncWorker` um den Block `Sync:ProductionOrdersEnabled` (Master-Read wie `SyncWorker.cs:357`).
- **Etappe 2:** `SageRueckmeldungWorker` (Kurztakt, gated `SageRueckmeldungAktiv`). Ablauf je Tick: Sweep → Recovery „Gesendet“ →
  Senden je Struktur → Fehlermail-Schwelle.
- Neue Sage-Leser sind nur lesend (`SELECT` bzw. SData-GET) und filtern mit `Sage:Mandant`.

## Migrations-/SQL-Auswirkungen

Nach [[0004-migrations-und-sql-disziplin]]: Model → `dotnet ef migrations add` → `SQL/XX_*.sql` (DDL mit
`COL_LENGTH`/`OBJECT_ID`-Guard im eigenen Batch) → `__EFMigrationsHistory`-Insert im separaten Batch → `SQL/00_FreshInstall.sql`
**an zwei Stellen**. Die EF-Migration braucht dieselben Guards wie das Skript ([[fallstricke]] §9). Die Nummern sind vorläufig: Der
Bündel-Worktree endet bei `SQL/97`, main hat eine eigene Reihe, also bei Umsetzung prüfen.

| Etappe | Objekt | Skript | Art |
|---|---|---|---|
| 1 | `ProductionOrders.SageMissingReason` (int NULL), `ProductionOrders.SagePosition` (int NULL) | `SQL/98_…` | additiv, nicht destruktiv |
| 1 | Tabelle `SageFaStates` (UNIQUE Mandant+BelID) | `SQL/98_…` | additiv |
| 2 | Tabelle `SageRueckmeldungQueueItems` (UNIQUE `CorrelationId`, UNIQUE `QuelleArt+QuelleId+Typ`, Index `Status, NextAttemptAt`, Index `StrukturID`) | `SQL/99_…` | additiv |
| 2 | `DELETE AppSettings WHERE [Key]='SageRueckmeldungAktiv'` (guarded) | `SQL/99_…` | Datenbereinigung, nicht fachlich destruktiv |
| 3 | Charge-Felder nur, wenn Q8/Q13 Chargenpflicht belegen | offen | – |

`SQL/AgentJobs/` ist nicht betroffen. Job 01 ist archiviert, aber Z-2 gilt: Ein auf AKESQL20 eingerichteter Job bleibt aktiv, bis
er manuell entfernt wird. Das wird gemeldet, nicht mitgebaut. Kein Backup-Zwang, da keine Konvertierung. Der übliche
Backup-Hinweis bleibt.

## Audit-Feld-Auswirkungen

- `ProductionOrder`: Die Materialisierung setzt bei jeder Grund-/Positionsänderung `ModifiedAt/By/ByWindows` = Service-Name, wie
  heute (`FaMaterializationSyncService.cs:178-181`).
- `SageFaStates`: Cache, kein `AuditableEntity`, `SyncedAt` (Präzedenz `FaHierarchyNode`). Nachvollziehbarkeit über das Protokoll.
- `SageRueckmeldungQueueItem`: `AuditableEntity`. Worker-Updates setzen den Service-Akteur (Muster
  `SageBookingQueueRepository.ServiceActor`), Requeue/Klären setzen den Benutzer aus `ICurrentUserService`.
- `ServiceSettings` hat keine Audit-Spalten (bekannt, [[2026-10-29-dev-umgebung-lokal]] Befund S-2). Keine Änderung.

**Rollen/Filter:** keine neue Rolle. Queue-Liste `[RequireBdeActive]` + `[RequireBdeShiftleadAccess]` (Klasse),
Requeue/Klären `[RequireBdeAdminAccess]` (Action), siehe [[controller]] Z. 73–75 und G14. Die Status-Anzeige erbt die Filter der
FA-Liste/FA-Struktur.

## Akzeptanzkriterien

Kennzeichnung **[Annahme]** = hängt an einer ungeprüften Annahme (Prüfbefund-Bezug unten).

*Etappe 1*
1. [Annahme A-1] Ein Sub-FA verschwindet aus der Struktur. Unter seinem Vater steht an derselben Position eine Blattzeile mit
   demselben Artikel. Dann ist `SageMissingSince` gesetzt, `SageMissingReason = 2`, `IsCancelled` bleibt false.
2. Ohne solche Blattzeile ist `SageMissingReason = 1`. Das Verhalten ist sonst wie heute.
3. Taucht der Sub-FA wieder auf, sind `SageMissingSince` und `SageMissingReason` NULL.
4. Ein vor dem Deploy vermisster Sub-FA (Grund NULL) wird im ersten Lauf eingestuft. Das Protokoll kennzeichnet ihn als
   „Abgleich nur über Artikel“.
5. Der Materialisierungslauf protokolliert je Grund eine Sammelwarnung mit den Sub-FA-Nummern und dem Count
   `in_material_umgewandelt`.
6. Die FA-Liste zeigt „in Material umgewandelt seit TT.MM.JJJJ“ bzw. „vermisst seit TT.MM.JJJJ“ als Text, mit Kontrast nach WCAG AA.
7. Start, Fortsetzen und AG-Fertigmeldung an einem umgewandelten Sub-FA werden mit Meldung abgewiesen. Es entsteht keine Buchung,
   und eine laufende Tätigkeit wird nicht beendet. An einem nur vermissten Sub-FA bleibt Buchen möglich.
8. Für jeden offenen materialisierten FA hat `SageFaStates` nach einem Lauf Status-Bits, abgeleiteten Status und
   Sage-Rückmeldestand. `-1` wird als wahr gelesen.
9. Ist die Quelle nicht erreichbar oder leer, bleiben die vorhandenen Zeilen unverändert. Das Protokoll enthält eine Warnung.
10. FA-Struktur und FA-Liste (IDEAL) zeigen den Sage-Status als Text-Chip und „Sage gut / WMS gut“.
11. [nur wenn K3 gebaut] Eine abweichende Verursacher-Kante erzeugt eine Warnung mit beiden BelIDs. Keine Kante ändert sich.
12. `Sage:Mandant` steht in `ServiceSettingDefinitions.All`, der Drift-Guard-Test ist grün, und alle neuen Sage-Leser filtern damit.
13. Master = true und `Sync:ProductionOrdersEnabled` = true oder fehlend: Der FA-Sync samt Reconciliation läuft nicht, jeder Lauf
    protokolliert „übersprungen: hierarchischer Modus“, und keine `ProductionOrders`-Zeile ändert sich.
14. Master = false: Das FA-Sync-Verhalten ist unverändert (bestehende `SyncWorkerTests` grün).
15. Etappe 1 enthält keinen schreibenden Aufruf an Sage. Das Suchmuster aus dem Bericht-Anhang findet keinen neuen Treffer.

*Etappe 2*
16. `SageRueckmeldungAktiv` (ServiceSetting) ist aus: Es wird nichts eingereiht und nichts gesendet. Das AppSetting gleichen
    Namens ist aus Seed, `/Settings` und DB entfernt.
17. Eine beendete Zeitbuchung (beendet nach `SageRueckmeldungAb`, älter als die Haltefrist) erzeugt genau einen Queue-Eintrag mit
    neuer GUID, Mandant, BelID, StrukturID und Payload-Snapshot.
18. Ein zweiter Sweep über dieselbe Buchung erzeugt keinen zweiten Eintrag (DB-UNIQUE + App-Guard, unit-getestet).
19. [Annahme R25/D7] Ein hängender „Gesendet“-Eintrag, den Sage laut Lookup kennt, wird ohne erneutes Senden „Bestätigt“.
20. [Annahme D8] Eine Transient-Antwort (FA gesperrt) setzt den Eintrag auf Offen, erhöht `AttemptCount` und setzt
    `NextAttemptAt` nach Backoff. Nach `MaxRetries` steht er auf Fehler, und es geht eine Fehlermail raus.
21. Eine Konflikt-Antwort oder ein Vorprüfungsbefund (beendet, gelöscht, umgewandelt) setzt den Eintrag auf „Konflikt“ ohne Retry.
    Spätere Einträge derselben StrukturID zeigen „blockiert durch #…“.
22. Je StrukturID ist höchstens ein Eintrag gleichzeitig „Gesendet“. Für denselben FA wird Zeit vor Fertigmeldung gesendet.
23. Für FAs mit Unter-FAs erzeugt Etappe 2 keine Fertigmeldung. Das Protokoll enthält einen Hinweis.
24. Bei Bestätigung sind `SageRueckmeldungId`/`SageBatchId` gespeichert.
25. Die Queue-Liste erfüllt das Listen-View-Pattern (Pagination, Filterkarte, Server-Spaltenfilter, viewKey registriert, Spaltenwahl
    bleibt nach Reload). Requeue/Klären darf nur `bde_admin`/`admin`. Klären verlangt eine Notiz und setzt Audit-Felder.
26. Das Aktivitäts-Protokoll führt `SageRueckmeldung` im Filter mit deutschen Counts.
27. DEV: Nach `dev-deploy.ps1` steht `SageRueckmeldungAktiv = false` (Prüfzeile `NichtFalse = 0`), und kein Eintrag erreicht „Gesendet“.

*Etappe 3/4 (vorläufig, nach Gate präzisieren)*
28. [G2/G3] Eine FA-bezogene Ausbuchung erzeugt **entweder** eine Material-RM **oder** eine Lagerbuchung, nie beides
    (unit-getestete Entscheidung).
29. [G7, R3] Eine Fertigmeldung eines FA mit Unter-FAs wird erst gesendet, wenn alle Unter-FAs fertiggemeldet (bestätigt) oder
    umgewandelt sind. Sonst wartet sie mit sichtbarem Hinweis.

## Test-Szenarien

Skizze für die Browser-Abnahme auf DEV (IDEAL zuerst). Die TS-Nummer ist noch nicht vergeben. Bei der Umsetzung kommt das Kapitel in
`docs/TESTSZENARIEN.md` und in [[testszenarien-index]]. Testdaten sind Platzhalter: `{FA-mehrstufig}`, `{FA-einstufig}`,
`{Sub-FA-umgewandelt}`, `{Werkbank}`, `{Operator}`, `{Charge}`.

1. **Umgewandelt erkennen:** In Sage `{FA-mehrstufig}` einlasten, sodass die Bedarfsprüfung `{Sub-FA-umgewandelt}` löscht (Q12
   vorher/nachher). Sync laufen lassen. Erwartet: Badge „in Material umgewandelt“, Sammelwarnung im Protokoll, kein Storno.
   *Negativ:* Den Sub-FA in Sage manuell löschen, ohne dass Material entsteht → „vermisst“.
2. **Buchungssperre:** An `{Werkbank}` mit `{Operator}` einen AG von `{Sub-FA-umgewandelt}` starten → abgewiesen, keine Buchung.
   Derselbe Versuch an einem vermissten Sub-FA → möglich.
3. **Status/Lesemodell:** `{FA-einstufig}` in Sage neu / eingelastet / begonnen / gesperrt setzen → Chip folgt nach dem Lauf.
   *Negativ:* SData-Host falsch → alte Werte bleiben, Warnung.
4. **K5:** Master = true, `Sync:ProductionOrdersEnabled` = true → Protokoll „übersprungen“, FA-Liste unverändert.
5. **Zeit-RM einstufig:** An `{Werkbank}` einen AG von `{FA-einstufig}` buchen und beenden, Haltefrist abwarten → ein Eintrag,
   „Bestätigt“, Rückmeldung in Sage (`dtsRueckKorrektur`) sichtbar.
6. **FA gesperrt → Retry:** `{FA-einstufig}` in Sage geöffnet lassen → Eintrag Offen mit steigender Versuchszahl. Nach dem Schließen
   → Bestätigt.
7. **FA beendet → Konflikt:** `{FA-einstufig}` in Sage beenden, dann im WMS buchen → Konflikt, Struktur blockiert, „Als geklärt
   markieren“ mit Notiz.
8. **Doppelsendung:** Worker während „Gesendet“ stoppen (Eintrag hängt), neu starten → Lookup findet die Buchung, kein zweiter
   Satz in Sage.
9. **DEV-Schalter aus:** `SageRueckmeldungAktiv = false` → Buchungen erzeugen weder Einträge noch Sendungen.
10. **(Etappe 3) Material mit `{Charge}`** und **(Etappe 4) bottom-up** nach Gate ausformulieren.

## Etappen (nur bei epic: true)

Ein Worktree, ein Merge am Ende. Jede Etappe endet mit einem eigenen Commit. QA und Schranke 2 kommen erst nach Etappe 4, außer
Gerald entscheidet bei G4 anders. Den Branch während der Arbeit mit `scripts/sync-worktree.ps1` aktuell halten.

| # | Etappe | Gate (vor Start) | Status | Commit |
|---|--------|------------------|--------|--------|
| 1 | Lesemodell + Reconciliation-Korrektur K1–K5, nur lesend, kein Sage-Schreiben | Voraussetzung (d); D9 für K1; G12 | offen | |
| 2 | Einstufig: Zeit-RM + Fertigmeldung (Queue, Worker, Adapter A **oder** C, Liste, Schalter, DEV-Absicherung) | Schranke-1-Entscheidung A/C; (a), (b), (c); G7, G8, G9; D8, D10 | offen | |
| 3 | Material-RM (Charge nur bei Chargenpflicht), K6-Umschaltung | G2, G3; Q8/Q13 | offen | |
| 4 | Mehrstufig: Fertigmeldung bottom-up über Unterbaugruppen | R3 bestätigt (DPS), D4 | offen | |

## Deploy

**Vorläufig.** Die QA bestätigt es am echten Diff.
- **Web-App:** ja (Badges, Status-Chips, Queue-Liste, Entfernen des AppSettings).
- **Service:** ja (Materialisierung, `SageFaStateSyncService`, K5-Guard, `SageRueckmeldungWorker`).
- **Migration:** ja (Etappe 1 `SQL/98`, Etappe 2 `SQL/99`). Reihenfolge: SQL-Skript + History-Insert → Service stoppen → Web und
  Service publishen → Service starten. Vor dem ersten Start `SageRueckmeldungAktiv = false` prüfen.
- **Publish-Befehle** (im Worktree):
  `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb` ·
  `dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService`
- Bei Variante C kommt der Deploy der Bridge auf den Sage-Host als eigener Schritt hinzu (ADR).

## Prüfbefund-Bezug

| Spec-Punkt | hängt an | Status laut Bericht |
|---|---|---|
| K1 Regel „umgewandelt“ | R8, R9, W2, Q4b/Q12 | R9 bestätigt; R8, W2 ungeprüft |
| K1 Sperre / Z-4 | Z-4 | Zusatzfund |
| K2 Status | R4, R5, R6, R7, R22, Q3/Q11 | R4, R6, R22 bestätigt; R5, R7 ungeprüft |
| K2 Rückmeldestand | R20, R33 | ungeprüft (Quellen am AKE-Test leer) |
| K3 Kanten | R2, W1, W2, Q4/Q4b/Q5 | ungeprüft |
| K4 Mandant | R23, R24, W5, Q0 | R24 bestätigt; R23, W5 ungeprüft |
| K5 Guard | Z-1, W3 | Zusatzfund; W3 ungeprüft |
| K6 Doppel-Entnahme | R19 | ungeprüft (Risiko am Code belegt) |
| Kein KHK-Schreiben | R10, R28 | bestätigt |
| Schreibweg A/C | R11, R12, R13, R14, R15 | R11 widerlegt (Absolutheit); Rest ungeprüft |
| Idempotenz | R25, W4 | ungeprüft |
| Reihenfolge/Serialisierung | R3, R26 | ungeprüft (Entwurfsentscheidung) |
| Retry | Z-3 | Zusatzfund |
| Grundlageneinstellungen | R16, R29, R30 | ungeprüft |
| Material/Charge/Rundung | R17, R18, R21 | ungeprüft |
| Zeit-RM-Adressierung | R27, Q6b | ungeprüft |
| Trigger | R32, Q10 | ungeprüft |
| Brain | B-1 | erledigt |

## Offene Rueckfragen

**An Gerald**
1. **G1:** Schreibweg Variante A (Partner-Service via SData) oder C (SageBridge)? Empfehlung A. Die Antwort gehört in
   `freigabe_entscheidung`.
2. **G2:** Materialentnahme in Sage retrograd je AG, explizit als Material-RM aus dem WMS, oder über die Gesamtfertigmeldung (mit DPS)?
3. **G3:** Sollen FA-bezogene WMS-Ausbuchungen ab Etappe 3 als Material-RM statt als Lagerbuchung laufen (K6)?
4. **G4:** K5 vorziehen (eigener Kleinfix vor dem Epic, früher Merge) oder in Etappe 1 belassen?
5. **G5:** Wann laufen die Phase-2-Prüfabfragen (Q0, Q3, Q4/Q4b/Q5, Q7, Q11, Q12) je Installation? Sie sind das Gate vor Etappe 1.
6. **G6:** „Arbeitsbereit“ nur anzeigen, oder nicht eingelastete/beendete/gesperrte FAs im BDE-Terminal und in den Listen ausblenden?
7. **G7:** Was löst die Sage-Fertigmeldung eines (Sub-)FA aus: die `IsFinal`-Menge am letzten AG oder das BDE-Erledigt-Flag des FA?
   Welche Menge wird gemeldet?
8. **G8:** Bearbeiten/Stornieren einer bereits gemeldeten BDE-Buchung sperren, oder zulassen und als Konflikt „in Sage korrigieren“
   melden? Haltefrist-Default 30 min?
9. **G9:** Ist `BdeOperator.PersonnelNumber` (`BdeOperator.cs:10`) die Sage-Mitarbeiternummer? Zeit-RM von Werkbänken ohne
   `SageArbeitsplatznummer` (`ProductionWorkplace.cs:36`) senden oder zurückhalten?
10. **G10:** Basis des Epic-Worktrees: Bündel-Branch `feature/2026-08-07-ideal-teile-1-5` oder main nach dessen Merge? Präfix `spec/`
    für einen Umsetzungs-Branch beibehalten?
11. **G11:** Neuer Key `Sage:Mandant` oder den vorhandenen `Sync:ProductionWorkplaceSyncMandant` mitverwenden? Die zwei harten `= 1`
    gleich mit umstellen?
12. **G12:** Statusquelle des Lesemodells: Sage-View-/Tabellenspalten per SQL (nach Q3/Q11) oder SData `dtsFaBelegsuche` (braucht D5)?
13. **G13:** AKE: Ist `[WA Nummer]` der WAListe die BelID (Rückmeldeadresse)? Prüfung: Q11-Definition von
    `vw_AKE_Kommissionierung_WAListe`. Erst nötig, wenn das AKE-BDE aktiv wird.
14. **G14:** Rollen: Queue lesen ab `bde_shiftlead`, Requeue/Klären nur `bde_admin`/`admin`. Passt das?

**An DPS (Sage-Partner)**
15. **D1:** Grundlageneinstellungen Produktion je Installation (Tabelle in Voraussetzung a): Ist-Werte und verbindliche Zielwerte?
16. **D2:** Welche Lizenz braucht eine Rückmeldung über die Sage-Engine, bei Weg A (euer Service) bzw. C (unsere Bridge)?
17. **D3:** Könnt ihr einen Rückmelde-Service im Stil `LagerbuchungService` nach dem Vertrag oben liefern (Aufwand, Termin)?
    Alternativ: SDK-Zugang und Signaturen der `RueckmeldungEngine`.
18. **D4:** Meldet DPS-MDE heute schon Rückmeldungen oder Unterbaugruppen an Sage (`USER_DPSBSMDEFreigabe`, Pakete `DPSBS_MDE*`)?
19. **D5:** SData-Host und Dataset der IDEAL-Installation? Gleicher Vertragsstand wie AKE?
20. **D6:** Gibt es Trigger oder Partnerlogik auf `KHKPps*`/`KHKLager*`, die bei Rückmeldungen mitläuft (Q10)?
21. **D7:** In welchem Feld kann die WMS-GUID je Rückmeldung abgelegt und gesucht werden?
22. **D8:** Wie meldet die Engine „FA gesperrt“ (vorübergehend) gegenüber „beendet/gelöscht“ (endgültig)?
23. **D9:** Bedarfsprüfung beim Einlasten: Wird der gelöschte Unter-FA protokolliert? Bleibt die Vaterposition als Material mit
    `BelIDNachfolger = 0`?
24. **D10:** Zeit-RM geplant auf die AG-Position (BelPosID) oder ungeplant mit Position 0, wenn die FAs keine AG-Positionen haben (Q6b)?

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
  Bei Varianten-Specs zusaetzlich freigabe_entscheidung im Frontmatter setzen.
-->

1. G1 Weg A oder C →
2. G2 Materialentnahme →
3. G3 FA-Ausbuchung als Material-RM →
4. G4 K5 vorziehen →
5. G5 Prüfabfragen-Termin →
6. G6 Arbeitsbereit anzeigen/filtern →
7. G7 Auslöser + Menge Fertigmeldung →
8. G8 Korrektur nach Versand / Haltefrist →
9. G9 Mitarbeiter-/Arbeitsplatz-Mapping →
10. G10 Worktree-Basis / Branch-Präfix →
11. G11 Mandant-Key →
12. G12 Statusquelle →
13. G13 AKE-Rückmeldeadresse →
14. G14 Rollen →
15. D1 Grundlageneinstellungen →
16. D2 Lizenz →
17. D3 Partner-Service / SDK →
18. D4 DPS-MDE heute →
19. D5 IDEAL-SData-Host →
20. D6 Trigger →
21. D7 GUID-Feld →
22. D8 Fehlerunterscheidung →
23. D9 Bedarfsprüfung/Protokoll →
24. D10 Zeit-RM-Adressierung →

> [!note] Hinweis 2026-11-06 (aus [[2026-11-06-sage-sdata-stammdaten-sync-spec]], G15)
> `SageFaStates` ist im Bündel mit **SQL/101** (`20261106191438_AddProductionOrderPositions`, Commit `1da971b7`) angelegt und bleibt leer, bis dieses
> Epic sie befüllt. Spalten: Mandant, BelID, StrukturID, IstEingelastet, IstBegonnen, IstBeendet, IstGesperrt, `IstReaktiviert` (nullable),
> `MengeGutRueckgemeldet`, `MengeGutFertiggemeldet`, SyncedAt; UNIQUE Mandant+BelID. Dieses Epic nummeriert seine SQL-Dateien ab der nächsten
> freien Nummer (nach SQL/107).
