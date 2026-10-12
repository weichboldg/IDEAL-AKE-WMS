---
type: spec
title: "Beschichtungsauftrag-Ausbau Teil 3 — Beschichtungsauftrag als eigenes Objekt"
slug: 2026-10-06-beschichtungsauftrag-ausbau-teil-3-spec
status: Testbereit
created: 2026-10-06
updated: 2026-10-12
source_backlog: "[[2026-10-06-beschichtungsauftrag-ausbau]]"
depends_on: ""
task: "[[2026-10-12-beschichtungsauftrag-ausbau-teil-3-umsetzung]]"
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
affected_code:
  - IdealAkeWms/Models/Beschichtungsauftrag.cs (neu, erbt AuditableEntity — Kopf mit genau einem HauptFA, Nummer, Status, Statuszeitpunkte, Dienstleister als Freitext, RowVersion; Muster WarehouseRequisition.cs)
  - IdealAkeWms/Models/BeschichtungsauftragPosition.cs (neu, erbt AuditableEntity — Herkunftsschluessel + Snapshot; Muster WarehouseRequisitionItem.cs:5)
  - IdealAkeWms/Models/BeschichtungsauftragStatus.cs (neu, enum byte mit Luecken; Wert 20 nur als Kommentar reserviert, Antwort 6; Muster WarehouseRequisitionStatus.cs)
  - IdealAkeWms/Data/ApplicationDbContext.cs (DbSets + Konfiguration: Laengen, decimal(18,4), Indizes, UNIQUE Nummer, RowVersion; Muster :1164-1189)
  - IdealAkeWms/Data/Repositories/IBeschichtungsauftragRepository.cs + BeschichtungsauftragRepository.cs (neu, ADR 0001; Registrierung in Program.cs; Abfrage 'sperrende Vergaben je HauptFA' = Positionen aus Auftraegen mit Status != Storniert)
  - IdealAkeWms/Services/BeschichtungsauftragWorkflow.cs (neu — Anlegen je HauptFA aus den FREIEN beschichteten Positionen, Nummernvergabe, Statuswechsel inkl. Dienstleister-Pflicht nach Uebergeben (Teil-4-Antwort 9), Entwurf bearbeiten, PositionenHinzufuegenAsync (Antwort 12/14: Erst-Entwurf = freie HauptFA-Positionen aus dem Cache, Reklamations-Entwurf = nur noch nicht enthaltene Positionen des Ursprungs), Bemerkung, Doppelvergabe-Guard nur fuer Erstauftraege (Antwort 15), Reklamations-Folgeauftrag mit Kopf vom Ursprung (Antwort 13) ohne Vergabe-Pruefung gegen andere Auftraege (Antwort 15); Name im Dev-Lauf frei)
  - IdealAkeWms/Data/Repositories/IFaHierarchyNodeRepository.cs (nur Nutzung, unveraendert — GetByHauptFaAsync, FaHierarchyNodeRepository.cs:18-26, liefert die Knoten frisch fuer Anlegen und Hinzufuegen)
  - IdealAkeWms/Migrations/<Zeitstempel>_AddBeschichtungsauftrag.cs (neu)
  - SQL/XX_AddBeschichtungsauftrag.sql (neu, XX = naechste freie Nummer in BEIDEN Zweigen zum Umsetzungszeitpunkt; heute Buendel SQL/93, main SQL/88)
  - SQL/00_FreshInstall.sql (zwei Stellen — Schema-Objekte UND MigrationId)
  - IdealAkeWms.Tests/Services/BeschichtungsauftragWorkflowTests.cs (neu, xUnit + EF InMemory — Nummernkreis, Statusautomat, Dienstleister-Pflicht, Doppelvergabe/Freigabe nach Storno, Anlegen nur freie Positionen, PositionenHinzufuegenAsync (Erst- und Reklamations-Entwurf), Reklamation nur aus Erledigt + Kopf vom Ursprung, Reklamation einer Reklamation + zweite Reklamation derselben Position, Audit-Felder, Snapshot)
  - IdealAkeWms.Tests/Repositories/ (DI-Aufloesung, Muster BomDiResolutionTests)
  - secondbrain/codebase/datenmodell.md (zwei neue Tabellen)
  - IdealAkeWms/Views/Help/Changelog.cshtml + beide AppVersion.cs
  - docs/TESTSZENARIEN.md + secondbrain/tests/testszenarien-index.md
open_questions: []
beantwortete_rueckfragen:
  - "1 Nummernformat -> BA-JJJJ-NNNNN, getrennte Server/Datenbanken je Standort, kein Kuerzel (Keyuser + Nachtrag Gerald 2026-10-07, Freigabe-Block Nr. 1)"
  - "2 Status-Automat -> Entwurf -> Uebergeben -> Erledigt, Storno nur im Entwurf, Reklamation als Folgeauftrag mit Verweis (Keyuser 2026-10-07, Freigabe-Block Nr. 2)"
  - "3 Schreibrolle -> neue Berechtigungsgruppe 'Beschichtungsauftraege', bestehende Rolle bleibt Lesen (Keyuser 2026-10-07, Freigabe-Block Nr. 3)"
  - "4 Doppelvergabe -> erneute Vergabe nur ueber Reklamation (Keyuser 2026-10-07, Freigabe-Block Nr. 4)"
  - "5 Mehrere HauptFA -> nein, ein HauptFA je Auftrag, Kombigeraete mehrdeutig gekennzeichnet (Keyuser 2026-10-07, Freigabe-Block Nr. 5)"
  - "6 Freigabe-Vorsorge -> bestaetigt: Statuswert 20 nur als Kommentar reserviert, keine Spalten, kein Code (Gerald 2026-10-09, Freigabe-Block Nr. 6)"
  - "7 Vorgemerktes-Vorsorge -> bestaetigt: Positionen eigene Zeilen mit eigener Id, Folgeauftrag verweist auf Ursprung; nichts weiter bauen (Gerald 2026-10-09, Freigabe-Block Nr. 7)"
  - "8 Snapshot/Menge -> Snapshot bei Anlage, Sollmenge, im Entwurf aenderbar, keine Nachfuehrung (Keyuser 2026-10-07, Freigabe-Block Nr. 8)"
  - "9 Termine -> aus Start_Beschichtung/Beschichten_Retour; Aenderbarkeit im Entwurf ueber Teil-4-Antwort 7 (Keyuser 2026-10-07, Freigabe-Block Nr. 9)"
  - "10 Storno -> Storniert sperrt nicht, Positionen wieder frei (Gerald 2026-10-09, Freigabe-Block Nr. 10)"
  - "11 Reklamation -> nur aus Erledigt (Gerald 2026-10-09, Freigabe-Block Nr. 11)"
  - "12 Restpositionen -> entfernte und neu in Sage beschichtete Positionen sind frei; (a) im bestehenden Entwurf desselben HauptFA hinzufuegbar oder (b) weiterer Auftrag nur mit freien Positionen (Gerald 2026-10-09, Freigabe-Block Nr. 12)"
  - "13 Reklamations-Kopf -> 1:1 vom Ursprung (Dienstleister, RAL, Termine), im Entwurf aenderbar; 'nichts vorbelegen' nur Teileauswahl; PPS-View-Vorbelegung nur fuer Erstauftraege (Gerald 2026-10-09, Freigabe-Block Nr. 13)"
  - "14 Hinzufuegen im Reklamations-Entwurf -> (b) nur weitere Positionen des Ursprungs, freie HauptFA-Positionen gehen in einen Erstauftrag (Gerald 2026-10-12, Freigabe-Block Nr. 14)"
  - "15 Reichweite Reklamations-Ausnahme -> Reklamations-Anlage prueft keine Doppelvergabe gegen andere Auftraege; Reklamation einer Reklamation und zweite Reklamation derselben Position moeglich (Gerald 2026-10-12, Freigabe-Block Nr. 15)"
epic: false
etappen: []
deploy:
  web: true
  service: false
  migration: true
freigabe_entscheidung: "Freigegeben wie Antworten 1-15: BA-JJJJ-NNNNN je DB, Entwurf->Uebergeben->Erledigt, Storno nur Entwurf (gibt frei), Reklamation als Folgeauftrag nur aus Erledigt (Kopf vom Ursprung, nur Ursprungspositionen, keine Doppelvergabe-Pruefung), Snapshot/Sollmenge, Hinzufuegen freier Positionen im Erstentwurf. Umsetzung direkt im Buendel, vor Teil 4."
freigabe_von: "Gerald Weichbold"
freigabe_am: 2026-10-12
---

> **Umsetzungsort (Entscheidung Gerald 2026-10-09):** direkt im Bündel-Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5` auf Zweig `feature/2026-08-07-ideal-teile-1-5` (kein eigener Worktree, ein Lauf zur Zeit). SQL-Nummer, TS-Nummer und Version: naechste freie zum Umsetzungszeitpunkt (SQL in **beiden**
> Zweigen pruefen — heute Buendel `SQL/93`, main `SQL/88`). Code-Belege beziehen sich auf den Buendel-Stand
> `.claude/worktrees/2026-08-07-ideal-teile-1-5` (v1.47.1).

> [!info] Einarbeitung 2026-10-09
> Antworten 6, 7, 10, 11, 12 und 13 (Gerald 2026-10-09) sind in Rumpf, `affected_code`, Akzeptanzkriterien und
> Testszenarien eingearbeitet; ausserdem die Dienstleister-Pflicht beim Wechsel nach `Uebergeben` aus
> [[2026-10-06-beschichtungsauftrag-ausbau-teil-4-spec]] Antwort 9 — sie gehoert in den Workflow (Statuswechsel
> laeuft nur hier), Teil 4 zeigt nur die Meldung. Neu offen: Rueckfragen 14 und 15 (Reklamations-Sonderfaelle
> der Doppelvergabe). Widerspruchspruefung: siehe Abschnitt „Widerspruchspruefung 2026-10-09" unter den
> fachlichen Anforderungen.

> [!info] Einarbeitung 2026-10-12
> Antworten 14 und 15 (Gerald 2026-10-12) sind eingearbeitet: Im Reklamations-Entwurf sind nur weitere
> Positionen des Ursprungs hinzufuegbar (Anforderung 10a, AK 8g); Reklamations-Anlage und -Hinzufuegen pruefen
> keine Doppelvergabe gegen andere Auftraege, Reklamation einer Reklamation und zweite Reklamation derselben
> Position sind moeglich (Anforderung 6, 11, AK 8h). Die „freie Position"-Definition gilt fuer Erstauftraege.
> Ob eine offene Reklamation Erstauftraege sperrt, ist aus Antwort 2/4/10/11/15 hergeleitet (Anforderung 6,
> „Herleitung 2026-10-12") — keine neue Rueckfrage. Der Hinweis „Neu offen: Rueckfragen 14 und 15" im
> Kasten oben ist damit ueberholt.

Teil 3 von 4 — Uebersicht: [[2026-10-06-beschichtungsauftrag-ausbau-uebersicht]]. Standort: **IDEAL**.

**Keine Abhaengigkeit zu Teil 1:** Teil 3 speichert Positionen **je Strukturknoten** (das braucht der
Doppelvergabe-Guard, Anforderung 6). Die Summierung ist Darstellung — der gedruckte Auftrag wird summiert
(Teil-4-Antwort 6), das macht Teil 4 beim Druck. Fachlich ist Teil 1 daher nicht Voraussetzung.

**Schnitt:** Teil 3 ist der **Backend-Baustein ohne Oberflaeche** (Modell, Migration, Repository,
Workflow-Service, Tests). Er ist eigenstaendig mergbar („dunkel": nichts ist fuer Anwender sichtbar) und
ueber Unit-Tests plus das SQL-Skript abnehmbar. Alle Bedienelemente kommen in Teil 4.

## Ziel / Nutzen (das Warum)

Heute ist der Beschichtungsauftrag eine gefilterte Ansicht auf die Sage-Cache-Tabellen — kein gespeichertes
Objekt (`FaHierarchyBeschichtungController.cs:55-136`: nur Lese-Actions). Es gibt keine Nummer, keinen
Status und keine Spur, was wann an welchen Beschichter ging. Teil 3 fuehrt das Objekt ein: Nummernkreis,
Kopf, Positionen mit Bezug auf HauptFA/Strukturknoten, Ablauf-Status mit Audit.

**Warum Positionen nicht per Fremdschluessel auf `FaHierarchyNode` zeigen duerfen:** Der Sync loescht bei
jedem Lauf **beide** Cache-Tabellen und fuegt neu ein (`FaHierarchySyncService.cs:131-136`,
`DELETE FROM dbo.FaHierarchyNodes` / `dbo.FaHierarchyOrderInfos` + `AddRangeAsync`). Die `Id` einer Zeile
ist damit nach dem naechsten Sync eine andere; ein FK wuerde den Sync brechen oder ins Leere zeigen.
Ausserdem importiert der Sync nur Knoten, deren HauptFA in der FAInfos-View steht (`FaHierarchySql.cs:129-131`)
— verschwindet ein HauptFA aus der View, verschwinden seine Knoten aus dem Cache. Ein erledigter
Beschichtungsauftrag muss trotzdem lesbar bleiben. Deshalb: **Herkunftsschluessel + Snapshot** je Position.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**
- Entitaeten `Beschichtungsauftrag` (Kopf) und `BeschichtungsauftragPosition`, Status-Enum.
- Nummernkreis mit Eindeutigkeit unter Parallelitaet.
- Status-Automat `Entwurf → Uebergeben → Erledigt`, Storno nur aus `Entwurf` (Antwort 2), Statuszeitpunkt +
  Benutzer je Uebergang; Wechsel nach `Uebergeben` nur mit Dienstleister (Teil-4-Antwort 9).
- Anlegen **je HauptFA** aus allen aktuell beschichteten **freien** Positionen dieses HauptFA (Teil-4-Antwort 2,
  Antwort 5, Antwort 12 b), mit Doppelvergabe-Guard und Snapshot. ~~Anlegen aus einer Menge von
  Positionsschluesseln~~ *(verworfen mit Teil-4-Antwort 2: keine Einzelauswahl)*
- Bearbeiten im `Entwurf` (Kopf, Positionsmenge, Position entfernen, Positionen hinzufuegen — im
  Erst-Entwurf **freie Positionen desselben HauptFA** (Antwort 12 a), im Reklamations-Entwurf **nur weitere
  Positionen des Ursprungs** (Antwort 14 b)) und Bemerkung in jedem Status ausser `Storniert` (Teil-4-Antwort 7,
  Antwort 8).
- Reklamations-Folgeauftrag mit Verweis auf den Ursprungsauftrag (Antwort 2), nur aus `Erledigt` (Antwort 11),
  aus ausgewaehlten Positionen des Ursprungs (Teil-4-Antwort 3), Kopf 1:1 vom Ursprung (Antwort 13), ohne
  Vergabe-Pruefung gegen andere Auftraege (Antwort 15).
- Migration dreifach (ADR 0004), Repository (ADR 0001), Unit-Tests.

**Out-of-Scope**
- Jede Oberflaeche (Liste, Detail, Buttons, Druck des Auftrags) → Teil 4.
- Dienstleister aus dem Kreditoren-Stamm (Fremdschluessel) — ~~Teil 4, nach [[2026-10-06-kreditoren-stammdaten-spec]]~~
  *(ueberholt mit Teil-4-Antwort 4: Ueberschreiben als Freitext genuegt, Kreditoren-Auswahl spaeter, ausserhalb
  dieses Pakets)*. Teil 3 speichert den Dienstleister als Freitext (vorbelegt aus Sage, ueberschreibbar).
- **Freigabe-Schritt** (Notiz Punkt 6): wird **nicht** gebaut. Vorsorge ausschliesslich: Statuswert 20 im Enum
  als Kommentar reserviert, kein Enum-Mitglied, keine Spalten, kein Code (Antwort 6).
- **Vorgemerktes** (Notiz Punkt 7: Positions-Haekchen Reklamation/Neubeschichtung, Ursprungs-HauptFA bei
  Reklamation): wird **nicht** gebaut. Vorsorge ausschliesslich: Positionen als eigene Zeilen mit eigener Id,
  Folgeauftrag verweist per `UrsprungsauftragId` auf den Ursprung (Antwort 7).
- Rueckschreiben nach Sage (Integrationsgrenze: Sage bleibt Quelle der FAInfos-Werte, das WMS schreibt dort
  nichts).
- Mail/enaio-Ablage.

## Fachliche Anforderungen

1. **Nummer** `BA-JJJJ-NNNNN` (fuenfstellig, fuehrende Nullen, Zaehler beginnt jedes Jahr bei 1), **ohne**
   Standortkuerzel — AKE und IDEAL laufen auf getrennten Servern mit eigenen Datenbanken (Antwort 1).
   Eindeutig, nie wiederverwendet (auch nicht nach Storno).
2. **Kopf:** genau **ein** `HauptFA` (Antwort 5: pro HauptFA ein Auftrag), Dienstleister (Freitext), RAL (Text),
   Start Beschichtung, Retour, Bemerkung, Status, ggf. Verweis auf Ursprungsauftrag. Vorbelegung **bei
   Erstauftraegen** (ohne `UrsprungsauftragId`; Reklamations-Folgeauftraege → Anforderung 11, Antwort 13) aus
   `FaHierarchyOrderInfo` des HauptFA: Dienstleister = „Standard-Beschichter aus der PPS-View"
   (`FaHierarchyOrderInfo.Dienstleister`, `FaHierarchyOrderInfo.cs:27`), RAL (`:28`), Termine aus
   `Start_Beschichtung`/`Beschichten_Retour` (`:33-34`, Antwort 9). Dienstleister, RAL und Termine sind im
   `Entwurf` aenderbar (Teil-4-Antwort 7: Kopf nur im Entwurf; Teil-4-Antwort 4: Dienstleister am Auftrag
   ueberschreibbar, als Freitext); kein Rueckschreiben nach Sage.
   Feldbedeutung belegt: `Dienstleister`, `RAL`, `Start_Beschichtung`, `Beschichten_Retour` werden nur vom Sync
   aus der Sage-View FAInfos geschrieben (`FaHierarchySyncService.cs:282-288`, Quelle `FaHierarchySql.cs:139-140`)
   und im WMS nur gelesen (Beschichtungs-Views; `Start_Beschichtung` zusaetzlich in der FA-Liste,
   `ProductionOrdersController.cs:307`). Was sie in Sage fachlich bedeuten (Plan- oder Ist-Termin), ist am Code
   nicht ablesbar; Antwort 9 legt fest, dass sie als Beschichtungstermin bzw. „Beschichten retour" uebernommen
   werden.
3. **Position:** Herkunftsschluessel `HauptFA`, `VaterFA`, `SubFA`, `Position` (die Struktur-Felder aus
   `FaHierarchyNode.cs:19-28`) + Snapshot `HauptArtnr`, `Artnr`, `Matchcode`, Menge, `Breite`, `Hoehe`, `Tiefe`.
   **Unverifiziert:** dass dieser Schluessel eine FAListe-Zeile eindeutig bestimmt — weder das Modell
   (`ApplicationDbContext.cs:1032-1033`, nur nicht-eindeutige Indizes auf HauptFA und VaterFA) noch der Sync erzwingen das.
   Pruefabfrage in den Test-Szenarien; davon haengt der Doppelvergabe-Guard ab.
4. **Kopf-Eindeutigkeit** (Antwort 5): Ein Auftrag enthaelt nur Positionen **eines** HauptFA; ein Aufruf mit
   Positionen mehrerer HauptFA wird abgelehnt. Kombigeraet (mehrere `FaHierarchyOrderInfo`-Zeilen je HauptFA,
   `FaHierarchyOrderInfo.cs:7-9`): je Kopffeld wird nur vorbelegt, wenn alle Kopfzeilen denselben Wert
   liefern; sonst bleibt das Feld leer und das Anlege-Ergebnis traegt den Hinweis „Kopf aus Sage mehrdeutig
   (n Kopfzeilen)" mit den Varianten. Nie still eine Kopfvariante waehlen (Grundsatz aus
   [[2026-07-29-standort-ideal-teil-4-spec]], Antwort zu B-2). ~~mehrere HauptFA je Auftrag~~ *(verworfen mit
   Antwort 5)*
5. **Statuswechsel** nur entlang `Entwurf → Uebergeben → Erledigt` und `Entwurf → Storniert` (Antwort 2);
   keine Rueckwaertsschritte. Jeder Wechsel setzt Zeitpunkt und Benutzer des Uebergangs sowie die Audit-Felder.
   Unerlaubter Wechsel → Fehler mit Klartext, kein stiller No-op.
   **Dienstleister-Pflicht** (Teil-4-Antwort 9): `Entwurf → Uebergeben` wird abgelehnt, wenn `Dienstleister`
   leer oder nur Leerzeichen ist — Klartext „Dienstleister fehlt — vor dem Uebergeben am Auftrag eintragen";
   Status und `Modified*` bleiben unveraendert. Im `Entwurf` bleibt das Feld optional (z. B. Kombigeraet mit
   mehrdeutigem Kopf, Anforderung 4).
6. **Doppelvergabe** (Antwort 4, Antwort 10, Antwort 12, Antwort 15): Eine Position (Herkunftsschluessel) darf
   nur ueber eine Reklamation erneut in einen Auftrag; sonst ist Doppelvergabe ausgeschlossen. Die Vergabe-Pruefung
   gilt fuer **Erstauftraege** (Anlegen, Anforderung 7; Hinzufuegen im Erst-Entwurf, Anforderung 10a). Ob eine
   Position fuer einen Erstauftrag vergeben ist, bestimmt allein der Status der Auftraege, die sie enthalten:

   | Status des Auftrags, der die Position enthaelt | sperrt die Position fuer Erstauftraege? | Beleg |
   |---|---|---|
   | `Entwurf` | ja | Antwort 4 |
   | `Uebergeben` | ja | Antwort 4 |
   | `Erledigt` | ja | Antwort 4 |
   | `Storniert` | **nein** — Positionen wieder frei | Antwort 10 |
   | „Reklamation" | kein eigener Status: ein Reklamations-Folgeauftrag ist ein Auftrag mit eigenem Status und sperrt nach derselben Tabelle — ohne praktische Wirkung, siehe Herleitung 2026-10-12 | Antwort 2, 15 |

   **Freie Position** eines HauptFA = Knoten mit `Beschichtet == true` im **aktuellen** Cache dieses HauptFA,
   dessen Herkunftsschluessel in **keinem** sperrenden Auftrag steckt. Darunter fallen ohne Sonderlogik: im
   Entwurf entfernte Positionen (die Positionszeile ist geloescht), Positionen stornierter Auftraege und
   Positionen, die nach einer Anlage in Sage neu als beschichtet markiert wurden (Antwort 12). Das WMS muss
   „neu in Sage beschichtet" nicht eigens erkennen — und kann es auch nicht: `FaHierarchyNode.Beschichtet`
   (`FaHierarchyNode.cs:56`) wird nur vom Sync aus der Sage-View geschrieben (`FaHierarchySyncService.cs:243`,
   Quelle `FaHierarchySql.cs:126`), der Cache wird je Lauf komplett ersetzt (`FaHierarchySyncService.cs:131-134`),
   es gibt keine Flag-Historie; der Web-Teil liest den Cache nur (`FaHierarchyNodeRepository.cs:18-36`, kein
   weiterer Schreiber im Repo). Bei einer vergebenen Position lehnt der Workflow ab bzw. meldet sie (Anforderung
   7) und nennt die betroffenen Positionen samt Auftragsnummer(n) — nicht still ueberspringen.
   ~~Ausnahme: ein Reklamations-Folgeauftrag darf Positionen seines Ursprungsauftrags enthalten (Reichweite der
   Ausnahme: Rueckfrage 15).~~ *(ueberholt mit Antwort 15, ersetzt durch den folgenden Absatz)*
   **Reklamationen** (Antwort 15): Reklamations-Anlage (Anforderung 11) und Hinzufuegen im Reklamations-Entwurf
   (Anforderung 10a) pruefen **keine** Doppelvergabe gegen andere Auftraege — ihre Positionen stammen
   ausschliesslich aus dem Ursprungsauftrag, eine Fremdvergabe ist dadurch ausgeschlossen. Damit sind moeglich:
   eine **Reklamation einer Reklamation** (Ursprung ist ein `Erledigt`-Reklamationsauftrag) und eine **zweite
   Reklamation** derselben Position aus demselben Ursprung, auch solange die erste noch `Entwurf`/`Uebergeben` ist.
   **Herleitung 2026-10-12 — sperrt eine offene Reklamation (`Entwurf`/`Uebergeben`) Erstauftraege derselben
   Position?** Nach der Tabelle ja (Antwort 2: Reklamation ist ein Auftrag mit eigenem Status; Antwort 4: sperrend
   ist jeder Auftrag ausser `Storniert`, Antwort 10). Die Frage hat aber kein abweichendes Ergebnis: Jede Position
   eines Reklamationsauftrags steckt auch im `Erledigt`-Erstauftrag am Anfang seiner Ursprungskette — Reklamation
   nur aus `Erledigt` (Antwort 11), Positionen nur aus dem Ursprung (Anforderungen 10a, 11; Antwort 14/15), ein
   `Erledigt`-Auftrag wechselt den Status nicht mehr (Anforderung 5, Storno nur aus `Entwurf`) und aendert keine
   Positionen (Anforderung 10), der Workflow hat keine Loesch-Operation fuer Auftraege und `UrsprungsauftragId`
   ist `OnDelete Restrict`. Dieser Erstauftrag sperrt die Position also dauerhaft; ob die offene Reklamation
   zusaetzlich mitzaehlt, aendert „frei/vergeben" nie. Folge fuer den Bau: die Vergabe-Abfrage bleibt einheitlich
   „Status != `Storniert`" inkl. Reklamationsauftraegen; Meldung und Markierung (Teil 4) nennen dann alle
   enthaltenden Auftragsnummern (Erstauftrag und Reklamationen). Keine Sonderlogik, keine neue Rueckfrage.
   Diese Herleitung gilt nur, solange es keinen Weg gibt, einen `Erledigt`-Auftrag zu loeschen oder zurueckzusetzen;
   wer einen solchen Weg baut, muss sie neu pruefen.
7. **Anlegen je HauptFA prueft gegen den aktuellen Cache:** Der Workflow liest beim Anlegen alle Knoten des
   HauptFA mit `Beschichtet == true` frisch aus dem Cache (nicht aus dem Seitenaufbau,
   `IFaHierarchyNodeRepository.GetByHauptFaAsync`). Kein Treffer → Ablehnung mit Klartext. Der neue Auftrag
   uebernimmt **nur die freien** Positionen (Antwort 12 b — ein weiterer Auftrag fuer denselben HauptFA ist
   damit erlaubt). Sind einzelne Positionen bereits vergeben, traegt das Anlege-Ergebnis den sichtbaren Hinweis
   „n Positionen bereits in BA-… vergeben, nicht uebernommen" mit Positionsliste (nicht still). Sind **alle**
   beschichteten Positionen vergeben → Ablehnung mit Klartext und den Auftragsnummern.
   ~~Sind einzelne dieser Positionen bereits vergeben, gilt Rueckfrage 12; bis zur Antwort lehnt der Workflow mit
   der Liste der vergebenen Positionen ab (sicherste Variante).~~ *(ueberholt mit Antwort 12)*
8. **Snapshot-Regel** (Antwort 8): Positionsdaten werden bei der Anlage eingefroren; Menge = `Sollmenge`;
   die Menge ist im `Entwurf` aenderbar; keine automatische Nachfuehrung bei spaeteren Sage-Aenderungen.
9. **Zugriff** (Antwort 3): neue Berechtigungsgruppe „Beschichtungsaufträge" als **Schreibrolle** (ein Recht
   fuer alle Schreibvorgaenge); die bestehende Rolle `beschichtungsauftrag` (`RoleKeys.cs:24`) bleibt reine
   Lese-Ebene. Teil 3 hat keine Actions; Rolle, Filter und die drei Pflichtstellen setzt Teil 4 um.
   Class-Level bleiben `[RequireBeschichtungsauftragAccess]` + `[RequireFaHierarchyBeschichtungAktiv]`
   (`FaHierarchyBeschichtungController.cs:27-28`). Hinweis fuer Teil 4: der Anzeigename der Leserolle lautet
   heute „Beschichtungsauftrag (IDEAL)" (`RoleOverview.cshtml:194`) — der neue Name „Beschichtungsaufträge"
   muss in der Rollenuebersicht klar als Schreibrecht erkennbar sein.
10. **Bearbeiten** (Teil-4-Antwort 7, Antwort 12 a): Kopf und Positionen (Menge aendern, Position entfernen,
    Positionen hinzufuegen) nur im `Entwurf`; danach nur Statuswechsel und Bemerkung. Eine entfernte Position
    ist sofort frei (Anforderung 6). ~~Ob Positionen im Entwurf hinzugefuegt werden duerfen und ob entfernte
    Positionen frei werden, ist Rueckfrage 12.~~ *(entschieden mit Antwort 12)*
10a. **Positionen hinzufuegen** (Antwort 12 a, Antwort 14): nur im Status `Entwurf`. Die zulaessige
    Kandidatenmenge haengt von der Art des Entwurfs ab.
    **Erst-Entwurf** (ohne `UrsprungsauftragId`): nur **freie** Positionen (Anforderung 6)
    **dieses** HauptFA (`Beschichtungsauftrag.HauptFA`); der Workflow liest die Knoten frisch aus dem Cache und
    prueft jede uebergebene Position dort erneut (Seitenaufbau kann veraltet sein). Snapshot wie bei der Anlage
    (Antwort 8): `HauptArtnr`, `Artnr`, `Matchcode`, `Breite`/`Hoehe`/`Tiefe` aus dem Cache-Knoten, `Menge` =
    `Sollmenge`; `Created*` an der neuen Position, `Modified*` am Kopf. Ablehnung mit Klartext und Positionsliste,
    wenn eine Position nicht (mehr) im Cache steht, nicht beschichtet ist, zu einem anderen HauptFA gehoert, bereits
    vergeben ist (mit Auftragsnummer) oder schon in diesem Auftrag steht; leere Auswahl → Ablehnung. Eine
    teilweise gueltige Auswahl wird **ganz** abgelehnt (kein stilles Teil-Hinzufuegen).
    ~~Im Reklamations-Entwurf: Rueckfrage 14 — bis zur Antwort lehnt der Workflow das Hinzufuegen dort mit
    Klartext ab.~~ *(ueberholt mit Antwort 14)*
    **Reklamations-Entwurf** (mit `UrsprungsauftragId`, Antwort 14 b): nur Positionen des **direkten**
    Ursprungsauftrags, deren Herkunftsschluessel noch **nicht** in diesem Entwurf steht; freie Positionen des
    HauptFA gehen in einen Erstauftrag. Quelle ist der Ursprungsauftrag, **nicht** der Cache (der HauptFA kann
    dort schon fehlen, Anforderung 11); Snapshot einschliesslich `Menge` wird aus der Ursprungsposition kopiert
    wie bei der Reklamations-Anlage; keine Vergabe-Pruefung gegen andere Auftraege (Antwort 15, Anforderung 6).
    `Created*` an der neuen Position, `Modified*` am Kopf. Ablehnung mit Klartext und Positionsliste, wenn eine
    Position nicht im Ursprung steht (z. B. freie HauptFA-Position — Klartext „Im Reklamationsauftrag nur
    Positionen des Ursprungs BA-… — freie Positionen ueber einen neuen Auftrag vergeben") oder schon in diesem
    Entwurf steht; leere Auswahl → Ablehnung; teilweise gueltige Auswahl → ganz abgelehnt.
    Identifikation in beiden Faellen ueber den Herkunftsschluessel (`VaterFA`, `SubFA`, `Position`; `HauptFA` aus
    dem Auftrag) — ein Eingabeformat fuer Teil 4. Der Vergleich „schon enthalten" und die Zuordnung zur
    Ursprungsposition setzen voraus, dass der Schluessel eindeutig ist (Pruefschritt aus Anforderung 3; ergibt er
    Treffer, waechst der Schluessel hier mit).
11. **Reklamation** (Antwort 2, Antwort 11, Antwort 13, Teil-4-Antwort 3): Folgeauftrag mit `UrsprungsauftragId`,
    **nur aus `Erledigt`** (Antwort 11; aus jedem anderen Status Ablehnung mit Klartext). ~~(ob auch aus
    `Uebergeben`: Rueckfrage 11)~~ *(entschieden mit Antwort 11)* Die Positionen waehlt der Anwender aus den
    Positionen des Ursprungs; **nichts** wird vorausgewaehlt; leere Auswahl → Ablehnung. Snapshot-Werte kommen aus
    den Ursprungspositionen (nicht aus dem Cache — der HauptFA kann dort schon fehlen). **Keine** Vergabe-Pruefung
    gegen andere Auftraege (Antwort 15, Anforderung 6): der Ursprung darf selbst ein Reklamationsauftrag sein
    (Reklamation einer Reklamation), und dieselbe Position darf aus demselben Ursprung mehrfach reklamiert werden.
    Weitere Ursprungspositionen kommen spaeter ueber das Hinzufuegen im Reklamations-Entwurf dazu (Anforderung 10a,
    Antwort 14). Der „Vermerk am
    Ursprungsauftrag" ergibt sich aus dem Selbstverweis (Ursprung kennt seine Folgeauftraege ueber
    `UrsprungsauftragId`); keine eigene Spalte, der Ursprung behaelt seinen Status `Erledigt`.
    **Kopf des Folgeauftrags** (Antwort 13): `Dienstleister`, `Ral`, `StartBeschichtung`, `BeschichtenRetour`
    werden 1:1 aus dem Ursprungsauftrag kopiert — **nicht** aus der PPS-View (`FaHierarchyOrderInfo` wird bei
    der Reklamation nicht gelesen, daher auch kein Kombigeraet-Hinweis); im `Entwurf` aenderbar wie jeder Kopf.
    `Bemerkung` wird nicht kopiert (Antwort 13 nennt nur Dienstleister, RAL, Termine). „Nichts vorbelegen"
    (Teil-4-Antwort 3) gilt nur fuer die Teileauswahl. ~~Kopf-Vorbelegung des Folgeauftrags: Rueckfrage 13 (bis
    zur Antwort wie beim Anlegen aus dem Cache des HauptFA; fehlt der HauptFA dort, bleibt der Kopf leer mit
    Hinweis).~~ *(ueberholt mit Antwort 13)*

### Widerspruchspruefung 2026-10-09

- **Teil-4-Antwort 3 „nichts vorbelegen" vs. Antwort 13 „Kopf vom Ursprung":** aufgeloest, kein Konflikt.
  Antwort 13 sagt woertlich: „‚Nichts vorbelegen' gilt nur fuer die Teileauswahl." Teil-4-Antwort 3 („Teile
  auswaehlen, nichts vorbelegen") steht im Satz ueber die Teileauswahl.
- **Antwort 4 „nur ueber Reklamation erneut" vs. Antwort 10 (Storno gibt frei) und Antwort 12 (entfernte und neu
  beschichtete Positionen frei):** aufgeloest, kein Konflikt. Antwort 4 betrifft Positionen, die in einem
  **sperrenden** Auftrag stecken; Antwort 10 legt fest, dass `Storniert` nicht sperrt; die Positionen aus Antwort
  12 stecken in keinem sperrenden Auftrag (entfernt = Zeile weg; neu beschichtet = nie vergeben). Ergebnis:
  Status-Tabelle und Definition „freie Position" in Anforderung 6. „Neu in Sage beschichtet" braucht keine
  eigene Erkennung (Beleg dort).
- **Antwort 5 „pro HauptFA ein Auftrag" vs. Antwort 12 b „weiterer Auftrag fuer denselben HauptFA":** aufgeloest.
  Antwort 5 heisst „ein Auftrag enthaelt genau einen HauptFA" (Anforderung 4); mehrere Auftraege je HauptFA
  waren ohnehin moeglich (Index nicht UNIQUE, Reklamationen) und sind mit Antwort 12 b ausdruecklich erlaubt.
- **Nicht aufloesbar, als Rueckfragen gemeldet:** Hinzufuegen im Reklamations-Entwurf (Rueckfrage 14 — Antwort
  12 „im bestehenden Entwurf desselben HauptFA" schliesst Reklamations-Entwuerfe dem Wortlaut nach ein, die
  Empfehlung zu Teil-4-Rueckfrage 3 sah dort nur Ursprungspositionen vor) und Reichweite der Reklamations-Ausnahme
  (Rueckfrage 15). *(ueberholt — beantwortet mit Antworten 14 und 15, 2026-10-12; siehe unten)*

### Widerspruchspruefung 2026-10-12

- **Antwort 12 a „im bestehenden Entwurf desselben HauptFA hinzufuegen" vs. Antwort 14 b:** aufgeloest. Antwort
  14 schraenkt 12 a fuer Reklamations-Entwuerfe ein (nur Ursprungspositionen); fuer Erst-Entwuerfe gilt 12 a
  unveraendert (Anforderung 10a).
- **Antwort 4 „nur ueber Reklamation erneut" vs. Antwort 15 „keine Pruefung, mehrfache Reklamation":**
  aufgeloest. Antwort 15 ist genau der Reklamationsweg aus Antwort 4; ausserhalb davon gilt die Sperre
  unveraendert (Anforderung 6).
- **Sperrt eine offene Reklamation Erstauftraege?** Aus Antworten 2, 4, 10, 11, 15 hergeleitet, ohne
  abweichendes Ergebnis (Anforderung 6, „Herleitung 2026-10-12") — keine Rueckfrage noetig.

## Technischer Loesungsentwurf

**Modell (nach den Antworten 1, 2, 5, 9 und Teil-4-Antwort 4):**

`Beschichtungsauftrag : AuditableEntity`
- `Nummer` (string, max 20, Pflicht, UNIQUE), `Jahr` (int), `LaufendeNummer` (int), UNIQUE (`Jahr`, `LaufendeNummer`)
- `HauptFA` (int, Pflicht, Index **nicht** UNIQUE — Reklamations-Folgeauftraege und stornierte Entwuerfe teilen
  den HauptFA) — Antwort 5; auch Grundlage fuer Liste/Filter in Teil 4
- `Status` (`BeschichtungsauftragStatus`, byte)
- `Dienstleister` (string?, 200 wie `ApplicationDbContext.cs:1044`) — Freitext, vorbelegt aus Sage, im Entwurf
  ueberschreibbar. ~~Teil 4 ergaenzt den Kreditor-Fremdschluessel~~ *(verworfen mit Teil-4-Antwort 4)*
- `Ral` (string?, 200 wie `:1045`), `StartBeschichtung` (DateTime?), `BeschichtenRetour` (DateTime?)
- `Bemerkung` (string?, 1000)
- `UrsprungsauftragId` (int?, Selbstverweis, `OnDelete Restrict`) — Reklamations-Folgeauftrag (Antwort 2)
- Statuszeitpunkte je Uebergang: `UebergebenAt`/`UebergebenByUserId`, `ErledigtAt`/`ErledigtByUserId`,
  `StorniertAt`/`StorniertByUserId` — Muster `WarehouseRequisition.cs:21-28`, User-Id aus
  `ICurrentUserService.GetCurrentAppUserId()` (`ICurrentUserService.cs:7`)
- `RowVersion` (`[Timestamp]`, Muster `WarehouseRequisition.cs:43-44`) gegen parallele Statuswechsel
- `Positionen` (Collection)

`BeschichtungsauftragPosition : AuditableEntity` (Muster `WarehouseRequisitionItem.cs:5`)
- `BeschichtungsauftragId` (FK, Cascade)
- `HauptFA` (int), `VaterFA` (int?), `SubFA` (int), `Position` (int?) — Herkunftsschluessel
- `HauptArtnr`, `Artnr`, `Matchcode` (string?, 200), `Menge` (decimal 18,4), `Breite`/`Hoehe`/`Tiefe`
  (decimal 18,4) — Typen wie `ApplicationDbContext.cs:1010-1030`
- Index (`HauptFA`, `VaterFA`, `SubFA`, `Position`) **nicht** UNIQUE: dieselbe Position darf in einem
  stornierten Auftrag und einem neuen stehen bzw. im Reklamations-Folgeauftrag erneut vorkommen. Die
  Doppelvergabe pruefen deshalb der Workflow (statusabhaengig) — und Tests muessen das am App-Layer
  nachweisen, weil EF InMemory UNIQUE-Indizes nicht erzwingt.

`BeschichtungsauftragStatus : byte` — Werte mit Luecken, z. B. `Entwurf = 10`, `Uebergeben = 30`,
`Erledigt = 40`, `Storniert = 90` (Antwort 2). **Wert 20 bleibt fuer „Freigegeben"
reserviert und wird nicht angelegt** — nur ein Kommentar im Enum (Antwort 6).

**Nummernvergabe:** `MAX(LaufendeNummer) + 1` fuer das laufende Jahr, Speichern; scheitert das Insert am
UNIQUE-Index (paralleles Anlegen), neu lesen und erneut versuchen (wenige Versuche, danach sichtbarer
Fehler). Muster fuer „UNIQUE-Index entscheidet, Verlierer faengt `DbUpdateException`":
`SageBookingQueueRepository.cs:163-173`. Im Repo gibt es keinen bestehenden Nummernkreis und keine
SQL-`SEQUENCE` (Grep `nummernkreis|NumberRange|CREATE SEQUENCE|HasSequence|NEXT VALUE FOR` in
`*.cs`/`*.sql`/`*.cshtml`: kein Treffer) — daher bewusst die einfachste tragende Loesung statt einer
Sequence je Jahr.

**Workflow-Service** (scoped, ueber `IBeschichtungsauftragRepository`, `IFaHierarchyNodeRepository`,
`IFaHierarchyOrderInfoRepository`, `ICurrentUserService`):
- `AnlegenAsync(hauptFa)` → liest die beschichteten Knoten und die Kopfzeilen des HauptFA frisch aus dem
  Cache, prueft Anforderungen 4/6/7, uebernimmt nur freie Positionen (Hinweis auf vergebene im Ergebnis), bildet
  Snapshot, belegt den Kopf aus der PPS-View vor, vergibt Nummer, setzt `Entwurf`, `CreatedAt/By/ByWindows`;
  alles in einer Transaktion. ~~`AnlegenAsync(positionsSchluessel, kopfVorgaben)`~~ *(verworfen mit
  Teil-4-Antwort 2)*
- `EntwurfBearbeitenAsync(id, kopf, positionsMengen, entfernteIds, rowVersion)` → nur im `Entwurf`; setzt
  `ModifiedAt/By/ByWindows` an Kopf und geaenderten Positionen; entfernte Positionszeilen werden geloescht
  (damit frei, Anforderung 6).
- `PositionenHinzufuegenAsync(id, positionsSchluessel, rowVersion)` (Antwort 12 a, Antwort 14, Anforderung 10a)
  → `positionsSchluessel` = Liste aus (`VaterFA`, `SubFA`, `Position`); `HauptFA` kommt aus dem Auftrag, nicht
  vom Aufrufer. **Keine** Knoten-`Id` als Eingabe — sie aendert sich mit jedem Sync (Ziel/Nutzen). Verzweigt
  nach `UrsprungsauftragId`:
  - **Erst-Entwurf:** liest `GetByHauptFaAsync(auftrag.HauptFA)` frisch, prueft alle Regeln aus 10a (Erst-Entwurf)
    inkl. Vergabe-Pruefung, Snapshot wie `AnlegenAsync` (gemeinsamer privater Snapshot-Baustein, kein Duplikat).
  - **Reklamations-Entwurf:** liest die Positionen des Ursprungsauftrags, Kandidaten = Ursprungspositionen,
    deren Schluessel nicht im Entwurf steht; jede uebergebene Position muss darunter sein; Kopie wie
    `ReklamationAnlegenAsync` (gemeinsamer Kopier-Baustein); keine Vergabe-Pruefung (Antwort 15); kein Cache-Zugriff.
  RowVersion des Kopfes gegen parallele Bearbeitung (deckt auch zweimal dieselbe Ursprungsposition aus zwei
  Sessions ab). Teil 4 ruft die Methode aus der Positionsauswahl im Detail auf — fuer beide Entwurfsarten mit
  demselben Eingabeformat. Fuer die Anzeige liefert der Workflow (oder das Repository) je Entwurf die
  Kandidatenmenge nach derselben Regel, damit Teil 4 keine zweite Logik baut.
- `BemerkungAendernAsync(id, text, rowVersion)` → in jedem Status ausser `Storniert`.
- `StatusWechselnAsync(id, zielStatus, rowVersion)` → prueft Uebergang und bei Ziel `Uebergeben` die
  Dienstleister-Pflicht (Anforderung 5), setzt Statuszeitpunkt + `ModifiedAt/By/ByWindows`;
  Concurrency-Konflikt → verstaendliche Meldung.
- Abfrage fuer Teil 4 und fuer die Vergabe-Pruefung: „Herkunftsschluessel → Auftragsnummer(n)" je HauptFA-Menge,
  nur Auftraege mit Status != `Storniert` (Anforderung 6, Reklamationsauftraege eingeschlossen — Herleitung
  2026-10-12), im Repository, ohne N+1. Liefert zugleich die Menge der freien Positionen fuer die
  Positionsauswahl eines Erst-Entwurfs in Teil 4.
- `ReklamationAnlegenAsync(ursprungId, positionIds)` → nur wenn Ursprung `Erledigt` (Antwort 11); neuer Auftrag
  `Entwurf` mit `UrsprungsauftragId` und demselben `HauptFA`, Positionen = Kopie der ausgewaehlten
  Ursprungspositionen (`positionIds` = Ids der **Auftragspositionen** des Ursprungs, die stabil sind;
  ~~Doppelvergabe-Ausnahme, Anforderung 6 / Rueckfrage 15~~ *(ueberholt)* **keine** Vergabe-Pruefung gegen
  andere Auftraege, Antwort 15 — Ursprung darf ein Reklamationsauftrag sein, Mehrfach-Reklamation erlaubt);
  Kopf 1:1 vom Ursprung (Antwort 13, Anforderung 11).
- **Grenze Parallelitaet (Vorschlag Spec-Agent):** Der Positionsindex ist bewusst nicht UNIQUE (siehe Modell),
  die Vergabe-Pruefung ist App-Layer. Zwei gleichzeitige Vergaben derselben freien Position (Anlegen bzw.
  Hinzufuegen in zwei Auftraege) koennten beide durchgehen. Pruefung + Insert laufen deshalb in **einer**
  Transaktion mit `IsolationLevel.Serializable` (SQL-Server-Key-Range-Locks auf dem Positionsindex); der
  Verlierer erhaelt eine Klartext-Meldung „bitte erneut versuchen". Nicht InMemory-testbar → Manual-UAT. Wird
  das im Dev-Lauf als unverhaeltnismaessig verworfen, als `ponytail:`-Kommentar mit benannter Grenze festhalten.
  Betrifft nur die Erstauftrags-Pfade; Reklamations-Anlage und -Hinzufuegen pruefen keine Vergabe (Antwort 15)
  und brauchen dafuer keine Serializable-Transaktion.
- Ergebnisse als Ergebnisobjekt mit Klartext-Fehlerliste (Controller in Teil 4 uebersetzt in
  `ModelState`/`TempData["WarningMessage"]`; TempData kennt kein `ErrorMessage`).

**Kein** Aktivitaets-Protokoll (ADR 0010 gilt fuer Hintergrund-Services); Nachvollziehbarkeit kommt aus den
Audit- und Statusfeldern. Kein Service-Anteil.

## Migrations-/SQL-Auswirkungen

Neue Tabellen `dbo.Beschichtungsauftraege` und `dbo.BeschichtungsauftragPositionen` (Namen im Dev-Lauf final).
Reihenfolge nach ADR 0004 / CLAUDE.md:
1. Model + DbContext-Konfiguration.
2. `dotnet ef migrations add AddBeschichtungsauftrag --project IdealAkeWms`.
3. `SQL/XX_AddBeschichtungsauftrag.sql`, idempotent: `IF OBJECT_ID(N'dbo.Beschichtungsauftraege', N'U') IS NULL`
   bzw. je Tabelle; jede DDL in eigenem Batch (`GO`); Indizes/Constraints mit eigener Existenzpruefung
   (`sys.indexes`). XX = naechste freie Nummer in **beiden** Zweigen zum Umsetzungszeitpunkt.
4. `__EFMigrationsHistory`-Insert in separatem Batch, mit Existenzpruefung auf die `MigrationId`.
5. `SQL/00_FreshInstall.sql` an **zwei** Stellen: Tabellen/Indizes **und** `MigrationId`.
- Nicht daten-destruktiv (nur neue Tabellen) — kein Pflicht-Backup-Hinweis ueber das uebliche Mass hinaus.
- `SQL/AgentJobs/` unberuehrt (Grep `beschicht|FaHierarchy` in `SQL/AgentJobs`: kein Treffer).
- Fallstrick Buendel: Migrationen 90/91 liessen sich auf der Ziel-DB nicht per `Migrate()` anwenden
  (`fallstricke.md` §8, Hinweis in [[2026-08-06-pdf-erzeugung-fahierarchy-druck-spec]], Deploy). Fuer diese
  Migration im Dev-Lauf pruefen, ob der automatische `Migrate()`-Aufruf ausreicht oder das SQL-Skript der
  Deploy-Weg ist; im Deploy-Abschnitt festhalten.

## Audit-Feld-Auswirkungen

- Beide Entitaeten erben `AuditableEntity` (`AuditableEntity.cs:3-12`, ADR 0003).
- Anlage: `CreatedAt`, `CreatedBy` (`GetDisplayName()`), `CreatedByWindows` (`GetWindowsUserName()`).
- **Jeder** Update (Kopf-Aenderung, Statuswechsel, Positionsaenderung, Position entfernen oder hinzufuegen):
  `ModifiedAt`, `ModifiedBy`, `ModifiedByWindows` am Kopf (und an geaenderten Positionen). Hinzugefuegte
  Positionen tragen `Created*`. Statuswechsel setzen zusaetzlich `<Status>At` + `<Status>ByUserId`. Eine
  abgelehnte Aktion (z. B. Uebergeben ohne Dienstleister) aendert keine Audit-Felder.
- Grenze, bewusst benannt: `AuditableEntity` haelt nur die **letzte** Aenderung. Eine lueckenlose
  Statushistorie ergibt sich, weil jeder Status hoechstens einmal erreicht wird (Antwort 2: keine
  Rueckwaertsschritte, Reklamation = neuer Auftrag) — keine Historientabelle. ~~Entscheidet Antwort 2
  „Reklamation als Status desselben Auftrags", wiederholen sich Uebergaenge — dann braucht es eine eigene
  Historientabelle.~~ *(entfaellt mit Antwort 2)* Aenderungen an Kopf/Menge im Entwurf sind nur als letzte
  Aenderung nachvollziehbar (`Modified*`) — fuer den Entwurf ausreichend, weil erst ab `Uebergeben` gedruckt
  wird (Teil-4-Antwort 8).

## Akzeptanzkriterien

1. Migration und `SQL/XX_*.sql` legen beide Tabellen an; das Skript laeuft zweimal hintereinander ohne
   Fehler (idempotent); `SQL/00_FreshInstall.sql` enthaelt Tabellen **und** `MigrationId`.
2. Anlegen fuer einen HauptFA ohne bestehende Vergaben erzeugt einen Auftrag im Status `Entwurf` mit Nummer
   `BA-JJJJ-NNNNN`, `HauptFA` gesetzt, **allen** beschichteten Positionen dieses HauptFA mit Snapshot-Werten
   (Menge = `Sollmenge`), vorbelegtem Kopf (Dienstleister, RAL, Start, Retour aus `FaHierarchyOrderInfo`) und
   gesetzten `Created*`-Feldern.
2a. Anlegen fuer einen HauptFA, von dem einzelne beschichtete Positionen bereits in einem sperrenden Auftrag
    stecken, erzeugt einen weiteren Auftrag mit **nur den freien** Positionen; das Ergebnis meldet die nicht
    uebernommenen Positionen mit Auftragsnummer. Sind alle vergeben, wird mit Klartext abgelehnt und nichts
    angelegt (keine Nummer verbraucht).
3. Zwei Anlagen im selben Jahr erhalten aufeinanderfolgende Nummern (`BA-2026-00001`, `BA-2026-00002`); im
   neuen Jahr beginnt die Folge bei `00001`; die Nummer enthaelt kein Standortkuerzel.
4. Ein Insert, der am UNIQUE-Index der Nummer scheitert, wird mit neuer Nummer wiederholt; erschoepfte
   Versuche enden mit Klartext-Fehler (Unit-Test ueber simulierten Konflikt; echter Parallelfall Manual-UAT
   bzw. SQL-Test).
5. `Entwurf → Uebergeben`, `Uebergeben → Erledigt` und `Entwurf → Storniert` gelingen und setzen
   Statuszeitpunkt, Statusbenutzer und `Modified*`; jeder andere Uebergang (z. B. `Uebergeben → Storniert`,
   `Erledigt → Entwurf`) wird mit Klartext abgelehnt und aendert nichts.
5a. `Entwurf → Uebergeben` mit leerem oder nur aus Leerzeichen bestehendem `Dienstleister` wird mit Klartext
    „Dienstleister fehlt …" abgelehnt; Status, Statusfelder und `Modified*` bleiben unveraendert. Mit
    Dienstleister gelingt derselbe Wechsel. Im `Entwurf` ist Speichern ohne Dienstleister moeglich.
6. Eine Position, die in einem Auftrag mit Status `Entwurf`, `Uebergeben` oder `Erledigt` steckt, kann nicht in
   einen zweiten **Erst**auftrag uebernommen werden — weder per Anlegen noch per Hinzufuegen im Erst-Entwurf;
   die Ablehnung nennt Position und Auftragsnummer(n). Reklamations-Pfade pruefen keine Vergabe (AK 8g, 8h).
   ~~(Ausnahme: Reklamations-Folgeauftrag des Ursprungs)~~ *(ueberholt mit Antwort 15)*
6a. Nach Storno eines Entwurfs sind dessen Positionen frei: ein neuer Auftrag fuer denselben HauptFA uebernimmt
    sie, und sie lassen sich einem anderen Entwurf desselben HauptFA hinzufuegen (Antwort 10).
6b. Eine im Entwurf entfernte Position ist danach frei (Anlegen bzw. Hinzufuegen moeglich).
7. Anlegen fuer einen HauptFA ohne beschichtete Positionen im Cache wird mit Klartext abgelehnt; Positionen
   werden beim Anlegen frisch aus dem Cache gelesen.
8. Kombigeraet: Kopffelder mit abweichenden Werten in den Kopfzeilen bleiben leer, das Ergebnis meldet „Kopf
   aus Sage mehrdeutig" mit den Varianten; nie wird still eine Variante gewaehlt.
8a. Ein Aufruf mit Positionen mehrerer HauptFA in einem Auftrag ist nicht moeglich bzw. wird abgelehnt.
8b. Kopf, Menge und Positionsentfernung aendern sich nur im `Entwurf`; in `Uebergeben`/`Erledigt` wird nur die
    Bemerkung angenommen, alles andere mit Klartext abgelehnt; jede Aenderung setzt `Modified*`.
8c. Reklamation aus `Erledigt` mit ausgewaehlten Positionen erzeugt einen `Entwurf` mit `UrsprungsauftragId`,
    demselben `HauptFA` und genau diesen Positionen; leere Auswahl wird abgelehnt; der Ursprung zeigt den
    Folgeauftrag ueber den Selbstverweis und bleibt `Erledigt`.
8d. Reklamation aus `Entwurf`, `Uebergeben` oder `Storniert` wird mit Klartext abgelehnt (Antwort 11).
8e. Der Kopf des Reklamations-Folgeauftrags traegt `Dienstleister`, `Ral`, `StartBeschichtung`,
    `BeschichtenRetour` exakt wie der Ursprung — auch wenn `FaHierarchyOrderInfo` des HauptFA inzwischen andere
    Werte hat oder fehlt; `Bemerkung` ist leer; die Kopffelder sind im Entwurf aenderbar (Antwort 13).
8f. `PositionenHinzufuegenAsync` auf einen Erst-Entwurf fuegt freie beschichtete Positionen desselben HauptFA mit
    Snapshot (Menge = `Sollmenge`, Maße/Artnr/Matchcode aus dem Cache) und `Created*` hinzu und setzt `Modified*`
    am Kopf. Abgelehnt (mit Klartext, nichts geaendert) werden: Status ≠ `Entwurf`; Position eines anderen
    HauptFA; Position nicht im Cache oder nicht beschichtet; bereits vergebene Position (mit Auftragsnummer);
    Position schon in diesem Auftrag; leere Auswahl; teilweise gueltige Auswahl (nichts wird hinzugefuegt).
    ~~Aufruf auf einen Reklamations-Entwurf (bis Rueckfrage 14 beantwortet ist).~~ *(ueberholt mit Antwort 14 →
    AK 8g)*
8g. `PositionenHinzufuegenAsync` auf einen Reklamations-Entwurf fuegt Positionen des Ursprungs hinzu, die noch
    nicht im Entwurf stehen — Snapshot inkl. `Menge` aus der Ursprungsposition, auch wenn der HauptFA im Cache
    fehlt oder dort andere Werte hat; `Created*` an der neuen Position, `Modified*` am Kopf. Abgelehnt (Klartext,
    nichts geaendert): eine freie HauptFA-Position, die nicht im Ursprung steht; eine schon enthaltene Position;
    leere Auswahl; teilweise gueltige Auswahl. Eine Ursprungsposition, die zugleich in einer anderen offenen
    Reklamation steckt, wird **nicht** abgelehnt (Antwort 15).
8h. Reklamation eines `Erledigt`-Reklamationsauftrags gelingt (Reklamation einer Reklamation); eine zweite
    Reklamation derselben Position aus demselben Ursprung gelingt, waehrend die erste `Entwurf` oder
    `Uebergeben` ist. Anlegen eines Erstauftrags fuer denselben HauptFA uebernimmt diese Position in keinem der
    Faelle; der Hinweis nennt die enthaltenden Auftragsnummern (Ursprung und Reklamation(en)).
9. Nach einem simulierten Sync (Cache geloescht und neu befuellt, andere Ids) ist ein bestehender Auftrag
   vollstaendig lesbar und seine Positionen unveraendert.
10. Paralleler Statuswechsel auf denselben Auftrag fuehrt beim Verlierer zu einer Concurrency-Meldung, nicht
    zu einem stillen Ueberschreiben (RowVersion; Manual-UAT, InMemory prueft RowVersion nicht).
11. Das Enum enthaelt keinen „Freigegeben"-Wert; Wert 20 ist frei und kommentiert; es gibt keine
    Freigabe-Spalten und keinen Freigabe-Code (Antwort 6). Positionen haben eine eigene Id (Antwort 7).
12. Keine Oberflaeche aendert sich (Liste/Druck/PDF des Beschichtungsauftrags wie vorher, Regression TS-60/TS-74).

## Test-Szenarien

Neues Kapitel (naechste freie Nummer), Schwerpunkt Datenbank und Workflow; Bedienung folgt in Teil 4:
- **Pruefschritt Schluessel-Eindeutigkeit (vor Abnahme, Ergebnis im Kapitel notieren):** Sage-DB IDEAL
  ```sql
  SELECT [HauptFA], [VaterFA], [SubFA], [Position], COUNT(*) AS Anzahl
  FROM [vw_IDEAL-AKE_Kommissionierung_FAListe]
  WHERE [Beschichtet] <> 0
  GROUP BY [HauptFA], [VaterFA], [SubFA], [Position]
  HAVING COUNT(*) > 1;
  ```
  und gegen den WMS-Cache dieselbe Abfrage auf `dbo.FaHierarchyNodes` mit `WHERE Beschichtet = 1`. Leer ⇒
  Schluessel traegt. Treffer ⇒ melden; dann gehoert ein weiteres Feld (z. B. `Artnr`) in den Schluessel.
- **Pruefschritt View-Filter (Sage):** `EXEC sp_helptext 'vw_IDEAL-AKE_Kommissionierung_FAInfos';` — nach
  welchem Kriterium faellt ein HauptFA aus der View (und damit aus dem Cache)? Bestimmt, wie oft Auftraege
  auf nicht mehr vorhandene Knoten zeigen.
- SQL-Skript zweimal auf Test-DB ausfuehren; FreshInstall auf leerer DB.
- Unit-Tests (xUnit + FluentAssertions, EF InMemory, `BeschichtungsauftragWorkflowTests`): Nummernkreis inkl.
  Jahreswechsel und Format `BA-JJJJ-NNNNN`, Uebergangsmatrix (alle erlaubten und eine Auswahl verbotener
  Uebergaenge), Dienstleister-Pflicht (`null`, `""`, `"   "` → Ablehnung ohne Aenderung; gefuellt → Erfolg),
  Doppelvergabe je Status der Status-Tabelle (`Entwurf`/`Uebergeben`/`Erledigt` sperren, `Storniert` gibt frei)
  (Reklamationsauftraege sperren Erstauftraege mit; Meldung nennt Ursprung und Reklamation), Anlegen je HauptFA
  (alle Positionen, teilweise vergeben → nur freie + Hinweis, alle vergeben → Ablehnung ohne Nummernverbrauch,
  leerer HauptFA, Kombigeraet-Kopf), Bearbeiten nur im Entwurf, entfernte Position ist frei,
  `PositionenHinzufuegenAsync` im **Erst-Entwurf** (Erfolg mit Snapshot/Audit; Ablehnungen: falscher Status,
  fremder HauptFA, nicht im Cache, nicht beschichtet, vergeben, schon enthalten, leer; teilweise gueltige Auswahl
  → nichts hinzugefuegt) und im **Reklamations-Entwurf** (Ursprungsposition hinzufuegen mit Snapshot aus dem
  Ursprung bei fehlendem Cache; Ablehnungen: freie HauptFA-Position, schon enthalten, leer, teilweise gueltig;
  Ursprungsposition in anderer offener Reklamation → Erfolg), Bemerkung danach, Reklamation (Auswahl, leere
  Auswahl, nur aus `Erledigt`, Kopf 1:1 vom Ursprung bei abweichender/fehlender `FaHierarchyOrderInfo`,
  Reklamation einer Reklamation, zweite Reklamation derselben Position bei offener erster), Audit-Felder,
  Snapshot nach simuliertem Sync. ~~inkl. Reklamations-Ausnahme~~ *(ueberholt mit Antwort 15)* Vergabe-Pruefung am App-Layer testen —
  InMemory erzwingt keine UNIQUE-Indizes (und der Positionsindex ist ohnehin nicht UNIQUE).
- Manual-UAT Concurrency (zwei Sessions, gleicher Auftrag; zwei Sessions, dieselbe freie Position in zwei
  Entwuerfe) — sobald Teil 4 Bedienelemente liefert, dort.

## Etappen (nur bei epic: true)

Nicht zutreffend (`epic: false`).

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| 1 | — | — | — |

## Deploy

> [!info] Finalisiert vom qa-agent 2026-10-12 gegen den echten Diff `020bf8af..a1f7fcfd`; im Nachlauf gegen
> `020bf8af..a5b27550` bestaetigt (a5b27550 aendert nur `IdealAkeWms/` Repository+Workflow, Tests, `docs/` — keine neue
> Migration, kein `IDEALAKEWMSService/`; Flags und Ablauf unveraendert)
> Geaendert: `IdealAkeWms/` (Modelle, DbContext, Repository, Workflow, `Program.cs`, Changelog-View, `AppVersion.cs`),
> **neu** `IdealAkeWms/Migrations/20261012073551_AddBeschichtungsauftrag*.cs` + Snapshot, `SQL/94_AddBeschichtungsauftrag.sql`,
> `SQL/00_FreshInstall.sql`, `IDEALAKEWMSService/AppVersion.cs` (**nur** Versionsnummer), Tests, `docs/`.
> ⇒ **deploy.web: true, deploy.service: false, deploy.migration: true** (Frontmatter stimmt). Der Service referenziert
> das Web-Projekt, nutzt die neuen Tabellen aber nicht und ruft kein `Migrate()` (Grep `Migrate()` in
> `IDEALAKEWMSService`: kein Treffer) — der Versions-Bump allein verlangt keinen Service-Deploy (Praezedenz Teil 2).
> Migration rein additiv (2× `CreateTable`, 7× `CreateIndex`, keine Drop/Alter) — **nicht daten-destruktiv**.

**Ablauf Mensch** (Publish aus dem Worktree → Testsystem IDEAL → TS-83 → Merge):
1. DB-Backup der Ziel-DB (ueblicher Standard vor jedem Schema-Deploy; nicht wegen Datenverlust-Risiko).
2. Voraussetzung Buendel: `SQL/90`–`SQL/93` des Buendels sind auf der Ziel-DB bereits eingespielt (sonst zuerst diese,
   siehe Deploy-Hinweise der Vorgaenger-Specs; `Migrate()` beim Start wuerde sonst die offenen Buendel-Migrationen
   ziehen, fallstricke „EF-Migration UND idempotentes SQL-Skript").
3. `SQL/94_AddBeschichtungsauftrag.sql` auf der Ziel-DB ausfuehren (idempotent; legt beide Tabellen + 7 Indizes an und
   schreibt `20261012073551_AddBeschichtungsauftrag` in `__EFMigrationsHistory`) — **vor** dem IIS-Recycle. Danach legt
   `Migrate()` beim App-Start nichts mehr an.
4. Web publizieren (im Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`):
   `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`, auf den IIS-Pfad kopieren,
   App-Pool recyceln. Kein Service-Stop, kein Service-Publish.
5. Die Web-App aus dem Worktree **nicht lokal starten** (`Migrate()` gegen AKESQL20, fallstricke „Lokaler `dotnet run`
   der Web-App ist ein Deploy gegen AKESQL20").

Nach dem Merge nur dann erneut aus `main` publizieren, wenn der Merge getestete Dateien mit parallelen `main`-Aenderungen
zusammengefuehrt hat.

## Offene Rueckfragen

*Stand 2026-10-12: alle Rueckfragen 1–15 beantwortet und in den Rumpf eingearbeitet (14, 15 am 2026-10-12).
Keine offene Rueckfrage.* ~~*Stand 2026-10-09: 1–13 beantwortet und in den Rumpf eingearbeitet (6, 7, 10–13 am
2026-10-09). Offen: 14, 15 (neu aus der Einarbeitung).*~~ *(ueberholt mit der Einarbeitung 2026-10-12)* ~~*Stand 2026-10-09: alle beantwortet; 6, 7, 10–13 (2026-10-09) noch nicht in den
Rumpf eingearbeitet.*~~ *(ueberholt mit der Einarbeitung 2026-10-09)*

1. **Nummernformat (Notiz Punkt 3):** z. B. `BA-JJJJ-laufend`, je Standort getrennt?
   *Beantwortet mit Antwort 1: `BA-JJJJ-NNNNN`, eigene Datenbank je Standort, kein Kuerzel.*
   *Empfehlung:* `BA-JJJJ-NNNNN` (fuenfstellig, fuehrende Nullen), Zaehler beginnt jedes Jahr bei 1.
   Trennung je Standort ohne Standortkennung in der Nummer — **unter der Annahme**, dass AKE und IDEAL je
   eine eigene Datenbank haben (Hinweis darauf: der Master-Schalter `ProduktionsauftragHierarchisch` ist ein
   Wert je Datenbank, `StandortSettingsCatalog.cs:61`). Das ist am Code nicht beweisbar — bitte bestaetigen.
   Teilen sich beide Standorte eine Datenbank, braucht die Nummer ein Standortkuerzel.
2. **Status-Automat (Notiz Punkt 4):** Anfangsstatus, dann uebergeben → erledigt; Reklamation als Status des
   Auftrags oder als neuer Folgeauftrag mit Verweis? Welche Uebergaenge sind erlaubt? Gibt es Storno?
   *Beantwortet mit Antwort 2: wie vorgeschlagen (der Klammerteil „ggf. auch aus Uebergeben?" → Rueckfrage 11).*
   *Empfehlung der Koordination:* Folgeauftrag mit Verweis auf den Ursprungsauftrag (bereitet „urspruenglicher
   HauptFA ersichtlich" vor). *Ergaenzung:* `Entwurf → Uebergeben → Erledigt`; `Storniert` nur aus `Entwurf`;
   keine Rueckwaertsschritte (Korrektur = Storno + neuer Auftrag); Reklamation nur aus `Erledigt` (ggf. auch
   aus `Uebergeben`?). Mit Folgeauftrag reichen Statuszeitpunkt-Spalten als Audit (siehe Audit-Abschnitt).
3. **Wer darf anlegen und Status setzen (Notiz Punkt 4, Pflicht Edit-Split ADR 0006):** Die Rolle
   `beschichtungsauftrag` ist bewusst reine Lese-Ebene (`RequireBeschichtungsauftragAccessAttribute.cs:7-12`,
   `CurrentUserService.cs:137-138`). Optionen: (a) neue Schreibrolle (z. B. `beschichtungsauftrag_edit`),
   bestehende bleibt Lesen — Muster `masterdata`/`masterdata_read` (`RoleKeys.cs:6-7`); (b) bestehende Rolle
   wird Schreibrolle, neue Leserolle. Getrennte Rechte je Uebergang (z. B. nur Lager setzt „Erledigt")?
   *Beantwortet mit Antwort 3: Variante (a), neue Gruppe „Beschichtungsaufträge", bestehende bleibt Lesen.*
   *Empfehlung:* (a), ein Recht fuer alle Schreibvorgaenge. Neue Rolle = drei Stellen (Attribut,
   `secondbrain/codebase/controller.md`, `Views/Users/RoleOverview.cshtml`) — umgesetzt in Teil 4.
4. **Doppelvergabe (Notiz Punkt 5):** Positionen, die schon in einem Auftrag stecken — in der Vorschlagsliste
   ausblenden oder markieren? Welche Status sperren?
   *Beantwortet mit Antwort 4: erneute Vergabe nur ueber Reklamation (Storno-Fall → Rueckfrage 10; Darstellung
   → Teil 4).*
   *Empfehlung:* sperrend sind `Entwurf`, `Uebergeben`, `Erledigt`; `Storniert` gibt frei. Reklamations-
   Folgeauftraege duerfen Positionen eines `Erledigt`-Ursprungs erneut enthalten. In der Vorschlagsliste
   **markieren** (mit Auftragsnummer) statt ausblenden — sonst sieht niemand, warum ein Teil fehlt (Umsetzung
   Teil 4).
5. **Kombigeraete / mehrere HauptFA (Notiz Punkt 8):** Bei mehreren FAInfos-Zeilen je HauptFA sind mehrere
   Dienstleister/RAL moeglich ([[2026-08-06-kombinationsgeraete-montageabteilung]]). Darf ein Auftrag
   Positionen mehrerer HauptFA enthalten, und wie entsteht der Kopf?
   *Beantwortet mit Antwort 5: nein, pro HauptFA ein Auftrag; Kombigeraete bleiben mehrdeutig gekennzeichnet.*
   *Empfehlung:* ~~mehrere HauptFA erlaubt (eine Fahrt zum Beschichter), aber **ein** Dienstleister und **ein**
   RAL je Auftrag.~~ *(verworfen mit Antwort 5)* Vorbelegung nur, wenn alle beteiligten Kopfzeilen dasselbe
   liefern; sonst bleibt der Kopf leer, der Anwender waehlt, und der Auftrag zeigt sichtbar „Kopf aus Sage
   mehrdeutig". ~~Positionen mit abweichendem Sage-RAL im selben Auftrag → Warnung, nicht still.~~ *(entfaellt
   mit Antwort 5 — ein HauptFA je Auftrag)*
6. **Freigabe-Schritt (Notiz Punkt 6) — OUT OF SCOPE:** wird nicht gebaut. Vorsorge nur: Statuswert 20 im
   Enum reserviert (Kommentar, kein Enum-Mitglied), keine Spalten, kein Code. Der spaetere Schritt ergaenzt
   Enum-Wert, `FreigegebenAt/ByUserId` und den Uebergang — additiv, ohne Umbau. Bestaetigen? (Die Frage „wer
   prueft das Dokument vor dem Versand" aus [[2026-09-17-druck-briefkopf-logo-adresse]] gehoert dorthin.)
   *Eingearbeitet (Antwort 6, 2026-10-09).*
7. **Vorgemerktes (Notiz Punkt 7) — OUT OF SCOPE:** Positions-Haekchen Reklamation/Neubeschichtung und
   „urspruenglicher HauptFA ersichtlich" werden nicht gebaut. Vorsorge nur: Positionen sind eigene Zeilen mit
   eigener Id (ein Haekchen ist spaeter eine additive Spalte) und der Folgeauftrag verweist auf den Ursprung
   (dessen Positionen tragen `HauptFA`). Bestaetigen?
   *Eingearbeitet (Antwort 7, 2026-10-09).*
8. **Snapshot und Menge:** Werden Positionsdaten bei der Anlage eingefroren, und was gilt bei spaeteren
   Sage-Aenderungen oder wenn die Position aus der FAListe verschwindet? Welche Menge (die Liste zeigt heute
   `Sollmenge`, `Index.cshtml:111`; `Fertigungmenge` existiert ebenfalls, `FaHierarchyNode.cs:47`) — und darf
   sie im Auftrag geaendert werden (Teilmenge)?
   *Beantwortet mit Antwort 8: wie vorgeschlagen.*
   *Empfehlung:* Snapshot bei Anlage gilt; keine automatische Nachfuehrung. `Sollmenge` uebernehmen, im
   Status `Entwurf` aenderbar. Abweichungshinweis (Sage-Wert geaendert/Position weg) erst bei Bedarf als
   eigener Punkt.
9. **Termine im Kopf:** `Start_Beschichtung`/`Beschichten_Retour` kommen aus Sage (Bedeutung dort nicht am Code
   belegbar). Als Vorbelegung uebernehmen und im WMS aenderbar (ohne Rueckschreiben nach Sage) — oder eigene
   WMS-Termine (z. B. Uebergabe-/Rueckgabe-Soll) unabhaengig von Sage?
   *Empfehlung:* Vorbelegung aus Sage, im Entwurf aenderbar, kein Rueckschreiben.
   *Beantwortet mit Antwort 9 (Uebernahme aus `Start_Beschichtung`/`Beschichten_Retour`); die dort offene
   Aenderbarkeit im Entwurf ist durch Teil-4-Antwort 7 entschieden („Kopf und Positionen nur im Entwurf").*
10. **Storno und Doppelvergabe (neu 2026-10-07):** Antwort 4 erlaubt eine erneute Vergabe nur ueber eine
    Reklamation. Ein stornierter Entwurf (Storno nur im Entwurf, Antwort 2) ging nie an den Beschichter — gibt er
    seine Positionen frei, sodass ein neuer Auftrag fuer denselben HauptFA moeglich ist?
    *Empfehlung:* ja, `Storniert` sperrt nicht (sonst waeren die Positionen nach einem Fehlanlage-Storno nur noch
    ueber eine Reklamation vergebbar, die einen `Erledigt`-Ursprung braucht).
    *Eingearbeitet (Antwort 10, 2026-10-09) — Anforderung 6, AK 6a.*
11. **Reklamation aus welchem Status (neu 2026-10-07):** Die Empfehlung zu Rueckfrage 2 nannte „nur aus
    Erledigt (ggf. auch aus Uebergeben?)"; Antwort 2 „wie vorgeschlagen" klaert den Klammerteil nicht.
    *Empfehlung:* nur aus `Erledigt`.
    *Eingearbeitet (Antwort 11, 2026-10-09) — Anforderung 11, AK 8d.*
12. **Restpositionen eines HauptFA (neu 2026-10-07):** Teil-4-Antwort 2 uebernimmt automatisch alle Positionen
    des HauptFA und erlaubt Bearbeiten im Entwurf; Antwort 5 sagt „pro HauptFA ein Auftrag". Was gilt fuer
    Positionen, die im Entwurf entfernt wurden oder nach der Anlage in Sage neu als beschichtet erscheinen —
    frei fuer einen weiteren Auftrag desselben HauptFA (nur diese Positionen) oder nur ueber den bestehenden
    Auftrag? Duerfen im Entwurf Positionen (wieder) hinzugefuegt werden?
    *Empfehlung:* entfernte und neue Positionen sind frei; ein weiterer Auftrag fuer denselben HauptFA nimmt nur
    freie Positionen; im Entwurf kein Hinzufuegen (kein Positions-Picker noetig).
    *Eingearbeitet (Antwort 12, 2026-10-09, abweichend von der Empfehlung: Hinzufuegen im Entwurf erlaubt) —
    Anforderungen 6, 7, 10, 10a, `PositionenHinzufuegenAsync`, AK 2a, 6b, 8f. Rest fuer Reklamations-Entwuerfe
    → Rueckfrage 14.* *(Rueckfrage 14 beantwortet und eingearbeitet 2026-10-12.)*
13. **Reklamations-Kopf (neu 2026-10-07):** Teil-4-Antwort 3 sagt „Teile auswaehlen, nichts vorbelegen",
    Teil-4-Antwort 4 „Standard-Beschichter aus der PPS-View, am Auftrag ueberschreibbar — besonders bei
    Reklamation". Gilt „nichts vorbelegen" nur fuer die Teileauswahl (Kopf dann aus PPS-View vorbelegt, Fallback
    Ursprungskopf) oder auch fuer den Kopf?
    *Empfehlung:* nur fuer die Teileauswahl; Kopf aus der PPS-View, fehlt der HauptFA im Cache, aus dem Ursprung.
    *Eingearbeitet (Antwort 13, 2026-10-09, abweichend von der Empfehlung: Kopf 1:1 vom Ursprung, PPS-View nur
    fuer Erstauftraege) — Anforderungen 2, 11, AK 8e.*
14. **Hinzufuegen im Reklamations-Entwurf (neu 2026-10-09):** Antwort 12 erlaubt das Hinzufuegen freier
    Positionen „im bestehenden Entwurf desselben HauptFA". Dem Wortlaut nach ist auch ein Reklamations-
    Folgeauftrag im `Entwurf` ein solcher Entwurf — dann mischte er Erstbeschichtung und Reklamation in einem
    Aussendokument. Die Empfehlung zu Teil-4-Rueckfrage 3 („Positionen hinzufuegen nur aus dem Ursprung") wurde
    mit Teil-4-Antwort 3 weder bestaetigt noch verworfen. Was darf im Reklamations-Entwurf hinzugefuegt werden:
    (a) nichts, (b) nur weitere Positionen des Ursprungs, (c) auch freie Positionen des HauptFA?
    *Empfehlung:* (b) — eine Reklamation bleibt eine Reklamation; freie Positionen gehen in einen Erstauftrag.
    ~~Bis zur Antwort lehnt der Workflow das Hinzufuegen im Reklamations-Entwurf ab (AK 8f).~~ *(ueberholt)*
    *Eingearbeitet (Antwort 14, 2026-10-12) — (b): Anforderung 10a, `PositionenHinzufuegenAsync`, AK 8f, 8g.*
15. **Reichweite der Doppelvergabe-Ausnahme bei Reklamationen (neu 2026-10-09):** Anforderung 6 erlaubt einem
    Reklamations-Folgeauftrag nur „Positionen seines Ursprungsauftrags". Streng gelesen (nur der direkte
    Ursprung ist ausgenommen) waere (1) eine **Reklamation einer Reklamation** unmoeglich, weil der
    Erst-Ursprung (`Erledigt`) die Position weiter sperrt, und (2) eine **zweite Reklamation** derselben Position
    aus demselben Ursprung gesperrt, solange die erste nicht storniert ist. Antwort 4 („nur ueber eine
    Reklamation erneut") laesst beides dem Wortlaut nach zu. Was gilt?
    *Empfehlung:* Reklamations-Anlage prueft keine Doppelvergabe gegen andere Auftraege — die Positionen stammen
    ausschliesslich aus dem Ursprung, eine Fremdvergabe ist dadurch ausgeschlossen; (1) und (2) sind moeglich.
    ~~Bis zur Antwort gilt die strenge Lesart (nur der direkte Ursprung ist ausgenommen).~~ *(ueberholt)*
    *Eingearbeitet (Antwort 15, 2026-10-12) — wie empfohlen: Anforderungen 6 (inkl. Herleitung „offene
    Reklamation sperrt Erstauftraege" ohne abweichendes Ergebnis), 10a, 11, `ReklamationAnlegenAsync`, AK 6, 8g, 8h.*

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
-->

1. Nummernformat / Standort → **`BA-JJJJ-NNNNN` ja (Keyuser 2026-10-07). Standortfrage (eigene Datenbank je Standort) offen bei Gerald.** **Nachtrag (Gerald 2026-10-07): AKE und IDEAL laufen auf zwei getrennten Servern mit eigenen Datenbanken – Nummer ohne Standortkürzel.**
2. Status-Automat / Reklamation / Storno → **Wie vorgeschlagen (Keyuser 2026-10-07): Entwurf → Übergeben → Erledigt, Storno nur im Entwurf, Reklamation als Folgeauftrag mit Verweis.**
3. Schreibrolle / Edit-Split → **Neue Berechtigungsgruppe „Beschichtungsaufträge“ (Schreibrolle) (Keyuser 2026-10-07); bestehende Rolle bleibt Lesen.**
4. Doppelvergabe / sperrende Status → **Eine Position darf nur über eine Reklamation erneut in einen Auftrag; sonst ist Doppelvergabe ausgeschlossen (Keyuser 2026-10-07). Darstellung in der Vorschlagsliste: siehe Teil 4.**
5. Mehrere HauptFA / Kopf bei Kombigeraet → **Nein – pro HauptFA ein Auftrag (Keyuser 2026-10-07) (abweichend vom Vorschlag). Kopf (Dienstleister/RAL) damit je HauptFA; Kombigeräte bleiben mehrdeutig gekennzeichnet.**
6. Freigabe-Vorsorge bestaetigen → **Bestaetigt (Gerald 2026-10-09): Freigabe-Schritt jetzt nicht bauen; Statuswert 20 bleibt reserviert (Kommentar, kein Enum-Mitglied), keine Spalten, kein Code.**
7. Vorgemerktes-Vorsorge bestaetigen → **Bestaetigt (Gerald 2026-10-09): Haekchen Reklamation/Neubeschichtung und „urspruenglicher HauptFA ersichtlich“ jetzt nicht bauen; Vorsorge: Positionen als eigene Zeilen mit eigener Id, Folgeauftrag verweist auf den Ursprung.**
8. Snapshot / Menge → **Wie vorgeschlagen (Keyuser 2026-10-07): Snapshot bei Anlage, Sollmenge, im Entwurf änderbar, keine automatische Nachführung.**
9. Termine im Kopf → **Beschichtungstermin und „Beschichten retour“ aus den vorhandenen Terminen übernehmen (Keyuser 2026-10-07) – am Code `FaHierarchyOrderInfo.Start_Beschichtung` / `Beschichten_Retour` (Z. 33-34, aus der PPS-View). Änderbarkeit im Entwurf: Vorschlag, nicht ausdrücklich beantwortet.**
10. Storno gibt Positionen frei → **Ja (Gerald 2026-10-09): `Storniert` sperrt nicht; die Positionen sind wieder frei, ein neuer Auftrag fuer denselben HauptFA ist moeglich.**
11. Reklamation aus welchem Status → **Nur aus `Erledigt` (Gerald 2026-10-09).**
12. Restpositionen eines HauptFA / Hinzufuegen im Entwurf → **Beides (Gerald 2026-10-09, abweichend vom Vorschlag): Im Entwurf entfernte und nach der Anlage neu in Sage beschichtete Positionen sind frei. Sie koennen (a) im bestehenden Entwurf desselben HauptFA wieder hinzugefuegt werden – nur freie Positionen dieses HauptFA, braucht eine Positionsauswahl (UI in Teil 4) – oder (b) in einen weiteren Auftrag fuer denselben HauptFA, der nur freie Positionen nimmt.**
13. Reklamations-Kopf vorbelegen → **Kopf vom Ursprung (Gerald 2026-10-09, abweichend vom Vorschlag): Der Kopf des Reklamations-Folgeauftrags (Dienstleister, RAL, Termine) wird 1:1 aus dem Ursprungsauftrag uebernommen, nicht aus der PPS-View; im Entwurf aenderbar wie jeder Kopf (Teil-4-Antwort 7). „Nichts vorbelegen“ gilt nur fuer die Teileauswahl. Teil-4-Antwort 4 (Standard-Beschichter aus PPS-View) gilt damit nur fuer Erstauftraege.**
14. Hinzufuegen im Reklamations-Entwurf (a nichts / b nur Ursprungspositionen / c auch freie HauptFA-Positionen) → **(b) nur weitere Positionen des Ursprungs (Gerald 2026-10-12); freie Positionen gehen in einen Erstauftrag.**
15. Reichweite der Reklamations-Ausnahme (Reklamation einer Reklamation, zweite Reklamation derselben Position) → **Beides erlaubt (Gerald 2026-10-12): Reklamations-Anlage prueft keine Doppelvergabe gegen andere Auftraege, Positionen stammen nur aus dem Ursprung; Reklamation einer Reklamation und zweite Reklamation derselben Position sind moeglich.**

## Umsetzung (Dev-Lauf 2026-10-12)

Direkt im Bündel-Worktree (Zweig `feature/2026-08-07-ideal-teile-1-5`), Start `020bf8af` (v1.49.0). Plan
`docs/superpowers/plans/2026-10-12-beschichtungsauftrag-ausbau-teil-3.md`. Commits `1410fc5c` (Modell, Repository,
Workflow, Tests), `51c6bc79` (Migration + SQL/94 + FreshInstall), `a1f7fcfd` (feature-complete, v1.50.0, TS-83).
Spec-Datei vor dem Lauf von `specs/entwurf/` nach `specs/freigegeben/` verschoben (Status stand schon auf Freigegeben).

**SQL-Nummer und Migration:** `SQL/94_AddBeschichtungsauftrag.sql` (94 frei in allen lokalen Zweigen: Bündel endete bei
93, `main` bei 88, übrige Feature-Zweige ≤ 88), EF-Migration `20261012073551_AddBeschichtungsauftrag`. Rein additiv
(`CreateTable`/`CreateIndex`, keine ungeschützten Drop-/Alter-Operationen) → `Migrate()` beim App-Start reicht technisch;
Deploy-Weg trotzdem das SQL-Skript vor dem Recycle (sein History-Insert verhindert eine Doppelanlage). Objektnamen in
SQL/94 und FreshInstall exakt wie EF (Drift-Wächter `BeschichtungsauftragMigrationSqlTests`).

**Was gebaut ist** (`affected_code` wie geplant, Abweichungen begründet):
- Modelle `Beschichtungsauftrag`, `BeschichtungsauftragPosition`, `BeschichtungsauftragStatus` (20 nur Kommentar),
  DbContext-Konfiguration, `IBeschichtungsauftragRepository`/`BeschichtungsauftragRepository`, DI in `Program.cs`.
- `Services/BeschichtungsauftragWorkflow.cs`: `AnlegenAsync`, `EntwurfBearbeitenAsync`, `GetHinzufuegbarePositionenAsync`
  (Kandidaten für Teil 4, dieselbe Regel wie das Hinzufügen), `PositionenHinzufuegenAsync` (Erst-/Reklamations-Entwurf),
  `BemerkungAendernAsync`, `StatusWechselnAsync`, `ReklamationAnlegenAsync`; Ergebnisobjekt mit Klartext-Fehlern und
  Hinweisen.
- **Abweichung `affected_code`:** Der Workflow liest Knoten über die **ungecachte** `FaHierarchyNodeRepository`, nicht über
  `IFaHierarchyNodeRepository` — die ist in `Program.cs` der `CachedFaHierarchyNodeRepository` mit 5 Minuten
  `IMemoryCache`; „frisch aus dem Cache" (Anforderung 7) heißt die DB-Cache-Tabelle, nicht ein Speicherstand. Die
  Vergabe-Prüfung liest ohnehin live aus den neuen Tabellen.
- **Nummernkreis:** `MAX(LaufendeNummer)+1` im Jahr aus `TimeProvider` (neu registriert: `TimeProvider.System`);
  `DbUpdateException` (UNIQUE-Index) **oder** SQL-Deadlock 1205 → verwerfen, neu lesen, erneut versuchen, nach 3 Versuchen
  Klartext. Abgelehnte Anlage verbraucht keine Nummer.
- **Parallelität Erstauftrag** (Spec-Vorschlag umgesetzt): Anlegen und Hinzufügen im Erst-Entwurf in einer
  `IsolationLevel.Serializable`-Transaktion (nur relationaler Provider; InMemory-Tests direkt). Reklamations-Pfade ohne.
- **Zusätzlich gemeldet (Hausregel „melden statt still behandeln"):** fehlende Kopfdaten in der PPS-View → Hinweis;
  nicht eindeutiger Herkunftsschlüssel im Cache → Hinweis (Anforderung 3, unverifiziert; Prüfschritt TS-83.1).
  Menge ≤ 0 beim Bearbeiten → Ablehnung (Position entfernen statt 0). Bemerkung > 1000 und Kopftexte > 200 → Ablehnung.
- Version 1.50.0, Anwender-Changelog (Vorbereitung, nichts sichtbar), TS-83. Keine Hilfeseite (nichts bedienbar).

**Nachweis (Dev-Lauf, vor QA):**
```
dotnet build IdealAkeWms.slnx         -> 0 Fehler
dotnet test IdealAkeWms.Tests         -> Bestanden! Fehler: 0, erfolgreich: 1566, übersprungen: 1, gesamt: 1567
dotnet test IDEALAKEWMSService.Tests  -> Bestanden! Fehler: 0, erfolgreich: 272, gesamt: 272
```
Neu 45 Tests (`BeschichtungsauftragWorkflowTests` 43, DI 1, Migration/SQL-Drift 1; vorher 1521). **Rot-vor-Grün:** die
Workflow-Tests liefen nicht separat rot (Testdatei war ohne Workflow nicht kompilierbar); stattdessen Mutationsprobe:
„Storniert sperrt" eingebaut → 1 Test rot, „Reklamation aus jedem Status" → 3 Tests rot, danach zurückgesetzt und grün.
Drift-Wächter: erst rot (falsch gezählte Erwartung 10 statt 11 Objektnamen), korrigiert. Nicht InMemory-testbar:
SQL-Skript/FreshInstall, RowVersion-Konflikt, Serializable/Deadlock → TS-83.3–83.6.

## QA-Nachweis (qa-agent, 2026-10-12)

Synchroner Lauf im Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, Diff `020bf8af..a1f7fcfd` (3 Commits,
21 Dateien). Review selbst durchgefuehrt, kein Unteragent. **Ergebnis: nicht Testbereit** — Build und Tests gruen,
aber ein offener Important-Befund (I-1). Status bleibt `InUmsetzung`.

### 1. Build und Tests (eigener Lauf)

```
dotnet build IdealAkeWms.slnx              -> 0 Fehler, 12 Warnungen (vorbestehend, z. B. CS8321 FaWorklist/Index.cshtml)
dotnet test IdealAkeWms.Tests --no-build   -> Bestanden! Fehler: 0, erfolgreich: 1566, übersprungen: 1, gesamt: 1567
                                              (Skip = ProductionOrderEagerCreateAgentJobTests…, vorbestehend)
dotnet test IDEALAKEWMSService.Tests       -> Bestanden! Fehler: 0, erfolgreich: 272, übersprungen: 0, gesamt: 272
dotnet ef migrations has-pending-model-changes --project IdealAkeWms
                                           -> "No changes have been made to the model since the last migration."
```

Eigene Pruefungen (temporaer, danach entfernt bzw. per `git checkout` zurueckgesetzt; `git status` sauber):
- **Mutationsprobe** (ergaenzt die Dev-Probe): (a) `if (!r.Erfolg) return r;` im Erst-Entwurf-Hinzufuegen
  (`BeschichtungsauftragWorkflow.cs:267`) entfernt, (b) `Geaendert(a, jetzt)` im Statuswechsel (`:312`) entfernt →
  2 Tests rot (`Hinzufuegen_ErstEntwurf_Ablehnungen_NichtsGeaendert`, `Status_ErlaubteUebergaenge_SetzenZeitpunktBenutzerUndModified`),
  danach zurueckgesetzt. Bewertung der Dev-Probe: tragfaehig als Ersatz fuer Rot-vor-Gruen — die Tests erkennen Regel- und
  Audit-Ausfaelle.
- **Probe Reklamations-Wiederholung:** `ReklamationAnlegenAsync` mit simuliertem `DbUpdateException` beim ersten
  `SaveChanges` → genau **ein** neuer Auftrag (`BA-…-00002`, `UrsprungsauftragId` gesetzt), keine Geisterzeile ueber die
  `Folgeauftraege`-Navigation des Ursprungs. Kein Befund.
- **Probe RowVersion auf InMemory:** `StatusWechselnAsync` mit falscher RowVersion → Klartext „wurde inzwischen von jemand
  anderem geändert" (InMemory prueft Concurrency-Tokens, siehe M-3); danach steht der Status am getrackten Objekt
  trotzdem auf `Storniert` (siehe M-1).

### 2. Code-Review (Befunde und Behandlung)

**Critical:** keine.

**Important**
- **I-1 — Verlierer der Serializable-Transaktion beim Hinzufuegen bekommt eine Ausnahme statt Klartext.**
  `BeschichtungsauftragWorkflow.cs:254-272` ruft `SpeichernAsync` (`:326-344`) **innerhalb** von
  `InSerializableTransactionAsync` auf. `SpeichernAsync` faengt `DbUpdateException` inkl. Deadlock 1205 und gibt ein
  Fehler-Ergebnis zurueck; danach ruft `BeschichtungsauftragRepository.cs:65` trotzdem `tx.CommitAsync()`. Ist die Sitzung
  Deadlock-Opfer, hat SQL Server die Transaktion bereits zurueckgerollt — `SqlTransaction.Commit` auf einer abgeschlossenen
  Transaktion wirft `InvalidOperationException` („This SqlTransaction has completed; it is no longer usable"). Ein
  Deadlock schon bei den Lesezugriffen (`:256-257`) laeuft ebenfalls ungefangen durch (anders als `AnlegenAsync`, das ueber
  `MitNummernVersuchenAsync` faengt). Genau das ist der Fall, fuer den die Transaktion gebaut wurde (zwei Sessions, dieselbe
  freie Position in zwei Erst-Entwuerfe: beide halten RangeS-S auf dem Positionsindex des HauptFA, beide wollen
  einfuegen → Deadlock). Die Spec verlangt fuer den Verlierer „Klartext-Meldung ‚bitte erneut versuchen'"
  (Technischer Loesungsentwurf, „Grenze Parallelitaet"). Datenintegritaet bleibt gewahrt (Verlierer zurueckgerollt), aber
  in Teil 4 waere das eine Fehlerseite. Hergeleitet aus dem SqlClient-Verhalten, nicht gemessen (kein SQL Server im
  QA-Lauf; InMemory kennt keine Transaktion). **Behandlung: offen — Fix im Dev-Lauf.** Vorschlag (klein): im
  Erst-Entwurf-Pfad innerhalb der Transaktion ohne Fang speichern und `DbUpdateConcurrencyException`/`DbUpdateException`/
  Deadlock **ausserhalb** von `InSerializableTransactionAsync` fangen (Rollback ueber `DisposeAsync`, wie bei
  `AnlegenAsync`), oder in `InSerializableTransactionAsync` nur bei Erfolg committen. Dazu TS-83.6 um den parallelen
  Hinzufuegen-Fall ergaenzen (M-4).

**Minor** (blockieren nicht; Behandlung jeweils angegeben)
- **M-1 — Fehlerpfade hinterlassen einen „schmutzigen" DbContext.** Nach `DbUpdateConcurrencyException` bleibt der
  getrackte Auftrag mit den neuen Werten `Modified` (Status, Kopf, geloeschte Positionen); `VerwerfeNeue`
  (`BeschichtungsauftragRepository.cs:47-52`) loest nur `Added`, hinzugefuegte Positionen bleiben zudem in
  `a.Positionen`. Probe: nach gescheitertem Storno meldet ein zweiter Aufruf im selben Scope „steht auf Storniert",
  obwohl nichts gespeichert wurde. Unkritisch bei einem Aufruf je Request. **Behandlung:** an Teil 4 weitergeben (ein
  Workflow-Aufruf je Request, danach Redirect) oder im Fehlerpfad `ChangeTracker.Clear()`.
- **M-2 — Sammel-Fang ohne Protokoll.** `:339` und `:356` fangen jede `DbUpdateException` (nicht nur 2601/2627/1205) und
  melden sie als „gleichzeitige Anlage/Änderung"; die Ausnahme wird nirgends geloggt (kein `ILogger`). Ein anderer
  DB-Fehler wuerde dreimal wiederholt und falsch benannt. **Behandlung:** offen, empfohlen beim I-1-Fix mitzunehmen
  (Log-Zeile mit Ausnahme).
- **M-3 — „InMemory prueft RowVersion nicht" stimmt nur halb** (Dev-Abschnitt, TS-83-Einleitung, Testkopf
  `BeschichtungsauftragWorkflowTests.cs:21`). EF InMemory prueft Concurrency-Tokens; der Fang-Pfad ist mit falscher
  RowVersion unit-testbar (Probe oben). Echte `rowversion`-Erzeugung und der Parallelfall bleiben Manual-UAT.
  **Behandlung:** optionaler Test, keine Blockade.
- **M-4 — TS-83-Luecken.** TS-83.5 fuegt `LaufendeNummer 99999` ein und sagt nur „Testzeile loeschen" — ohne
  `DELETE FROM dbo.Beschichtungsauftraege WHERE Nummer = 'BA-2026-99999';` ergibt die naechste Anlage `BA-2026-100000`
  (Formatbruch). TS-83.6 enthaelt nicht den von der Spec geforderten Fall „zwei Sessions, dieselbe freie Position in zwei
  Entwuerfe" (= I-1). **Behandlung:** mit dem I-1-Fix in `docs/TESTSZENARIEN.md` nachziehen; die Checkliste unten nennt
  das DELETE bereits.
- **M-5 — Brain-Checkliste:** `secondbrain/feature-map.md` hat keinen Eintrag fuer v1.50.0/Teil 3 (Grep `1.50`,
  `ausbau-teil-3`: kein Treffer). Brain-Changelog `2026-10-12-v1-50-0-beschichtungsauftrag-objekt`, `datenmodell.md`
  und Testindex (Kapitel 83) sind vorhanden. **Behandlung:** Dev-Lauf ergaenzt.
- **M-6 — Kosmetik Tests.** `IdealAkeWms.Tests/Data/BeschichtungsauftragMigrationSqlTests.cs` liegt in `Data/`, Namespace
  `…Tests.Repositories`. Der DI-Test spiegelt `Program.cs`, statt es zu lesen (anerkanntes Muster `BomDiResolutionTests`);
  `Program.cs:116-118` von Hand geprueft. **Behandlung:** keine.
- **M-7 — Eine Regel, zwei Stellen.** Kandidaten (`GetHinzufuegbarePositionenAsync`, `:200-222`) und Pruefung
  (`PositionenHinzufuegenAsync`, `:226-273`) setzen dieselbe Regel getrennt um; heute deckungsgleich (Test
  `Storno_GibtFrei_…` nutzt Kandidaten → Hinzufuegen). **Behandlung:** akzeptiert, Drift-Risiko benannt.

**Ohne Befund geprueft:** Statusautomat (`:51-57`, `:292-314`; Pruefung vor jeder Mutation), Dienstleister-Pflicht
(`:300-301`, `IsNullOrWhiteSpace`), Vergabe-Abfrage (`BeschichtungsauftragRepository.cs:32-41`: eine Abfrage, Join ueber
Navigation, `Status != Storniert` inkl. Reklamationen, fuer SQL Server uebersetzbar), Anlegen nur freie Positionen + Hinweis
mit Nummern, Ablehnung ohne Nummernverbrauch (Nummer erst in `NummerVergebenUndSpeichernAsync`), Hinzufuegen
ganz-oder-gar-nicht (alle Pruefungen vor dem ersten `Add`, `:233-267`), Reklamation (nur `Erledigt`, Kopf 1:1, Bemerkung
nicht kopiert, keine Vergabe-Pruefung, Ketten/Mehrfach), Audit-Felder bei jeder Aenderung inkl. Positionen
(`:181-185`, `:441-453`), Nummernkreis (MAX+1, neue Transaktion je Versuch, Rollback des Deadlock-Opfers ueber
`DisposeAsync` wirft nicht, `VerwerfeNeue` vor dem neuen Versuch, Jahr aus `TimeProvider`), keine Retry-Execution-Strategy
(`Program.cs:19-27`, eigene Transaktionen damit zulaessig), ungecachtes `FaHierarchyNodeRepository` (konkret registriert
`Program.cs:107`, vorbestehend) — **Abweichung `affected_code` sachlich richtig**: der Decorator haelt 5 min
`IMemoryCache`, „frisch" verlangt die DB-Cache-Tabelle; `TimeProvider.System` (`Program.cs:118`, keine andere Registrierung
im Repo; mehrfach registriert waere es dieselbe Instanz), Migration/SQL/94/FreshInstall (Typen `tinyint`, `datetime2`,
`rowversion`, `decimal(18,4)`, Laengen und Nullbarkeit identisch; je DDL eigener Batch mit `OBJECT_ID`-/`sys.indexes`-Guard;
History separat; `ProductVersion 10.0.2` = Designer; FreshInstall an beiden Stellen; SQL-Nr. 94 in keinem lokalen Zweig
belegt), `SQL/AgentJobs` unberuehrt. Klassen aus der QA-Liste (Script-Einbettung, `hidden`+`d-*`, Client-Validierung,
Parallel-Array-Binding): nicht anwendbar, keine Oberflaeche. Stille Fallbacks: `?? 0` in `GetMaxLaufendeNummerAsync` ist
fachlich richtig (keine Nummer = 0); `u?.Positionen ?? new()` unerreichbar wegen FK `Restrict`.

### 3. Beweisart je Akzeptanzkriterium

| AK | Beweis | Test / Szenario |
|---|---|---|
| 1 | teilweise automatisiert (Namen-Drift, Model-Snapshot) — **Manual-UAT** fuer Ausfuehrung/Idempotenz | `BeschichtungsauftragMigrationSqlTests`, `has-pending-model-changes`; TS-83.3, TS-83.4 |
| 2 | automatisiert | `Anlegen_ErzeugtEntwurf_MitNummer_Snapshot_Kopf_Audit` |
| 2a | automatisiert | `Anlegen_TeilweiseVergeben_NurFreie_UndHinweisMitNummer`, `Anlegen_AlleVergeben_Abgelehnt_OhneNummernverbrauch` |
| 3 | automatisiert | `Anlegen_Nummern_Fortlaufend_UndNeuesJahrBeginntBei1_OhneStandortkuerzel` |
| 4 | automatisiert (simulierter Konflikt) — echter UNIQUE-Index **Manual-UAT** | `Anlegen_NummernKonflikt_WirdMitNeuerNummerWiederholt`, `…_Erschoepft_Klartext_NichtsGespeichert`; TS-83.5 |
| 5 | automatisiert | `Status_ErlaubteUebergaenge_…`, `Status_VerboteneUebergaenge_Abgelehnt_NichtsGeaendert` (8 Faelle) |
| 5a | automatisiert | `Uebergeben_OhneDienstleister_Abgelehnt_NichtsGeaendert` (null, "", "   ") |
| 6 | automatisiert (App-Layer) — Parallelfall **Manual-UAT** (I-1) | `Doppelvergabe_SperrendeStatus_ZweiterErstauftragAbgelehnt` (3), `Hinzufuegen_ErstEntwurf_Ablehnungen_NichtsGeaendert`; TS-83.6 |
| 6a | automatisiert | `Storno_GibtFrei_EntfernteSindFrei_HinzufuegenInAnderenEntwurf` |
| 6b | automatisiert | dto. + `Anlegen_TeilweiseVergeben_…` |
| 7 | automatisiert (Ablehnung); „frisch" per Konstruktion (ungecachtes Repo) | `Anlegen_OhneBeschichtetePositionen_Abgelehnt`, `BeschichtungsauftragDiResolutionTests` |
| 8 | automatisiert | `Anlegen_Kombigeraet_AbweichendeKopffelderLeer_HinweisMitVarianten` |
| 8a | automatisiert (indirekt: Signatur ein HauptFA, fremder HauptFA abgelehnt) | `Hinzufuegen_ErstEntwurf_Ablehnungen_NichtsGeaendert` |
| 8b | automatisiert | `Bearbeiten_NurImEntwurf_SetztModified_DanachNurBemerkung`, `Bemerkung_ImStorniert_Abgelehnt`, `Bearbeiten_UngueltigeMenge_Abgelehnt` |
| 8c | automatisiert | `Reklamation_AusErledigt_KopfVomUrsprung_NurAuswahl_UrsprungBleibtErledigt`, `Reklamation_LeereAuswahl_UndFremdePositionsId_Abgelehnt` |
| 8d | automatisiert | `Reklamation_NurAusErledigt` (3) |
| 8e | automatisiert | `Reklamation_AusErledigt_KopfVomUrsprung_…` |
| 8f | automatisiert | `Hinzufuegen_ErstEntwurf_NeuBeschichtetePosition_MitSnapshotUndAudit`, `Hinzufuegen_ErstEntwurf_Ablehnungen_NichtsGeaendert` |
| 8g | automatisiert | `Hinzufuegen_ReklamationsEntwurf_NurUrsprungspositionen_SnapshotAusUrsprung_OhneCache` |
| 8h | automatisiert (zweite Reklamation bei erster im `Entwurf`; `Uebergeben`-Variante nicht eigens) | `Reklamation_EinerReklamation_UndZweiteReklamation_ErstauftragNimmtPositionNicht` |
| 9 | automatisiert | `NachSimuliertemSync_AuftragUnveraendert` |
| 10 | **Manual-UAT** (Fang-Pfad per QA-Probe auf InMemory belegt, M-3) | TS-83.6 |
| 11 | automatisiert + Review (keine Freigabe-Spalten in Modell/Migration) | `Enum_KeinFreigegeben_Wert20Frei` |
| 12 | Review (einzige View-Aenderung: `Help/Changelog.cshtml`) + **Manual-UAT** | TS-83.7 |

### 4. Geaenderte bestehende Tests

Keine. Der Diff aendert keinen vorbestehenden Test; neu sind nur `BeschichtungsauftragWorkflowTests.cs`,
`BeschichtungsauftragDiResolutionTests.cs`, `BeschichtungsauftragMigrationSqlTests.cs`.

### 5. CLAUDE.md-Checkliste

- Migration + `SQL/94` mit Guards, History separat: ja. FreshInstall an beiden Stellen: ja. `SQL/AgentJobs`: unberuehrt.
- Audit-Felder: beide Entitaeten erben `AuditableEntity`, `Created*`/`Modified*` gesetzt (s. o.).
- Version 1.50.0 in beiden `AppVersion.cs`, Anwender-Changelog: ja. Brain-Changelog: ja. Feature-Map: **fehlt** (M-5).
- `docs/TESTSZENARIEN.md` TS-83 + Testindex: ja (Luecken M-4).
- Keine neue Rolle, kein Filter (Teil 4). Kein `SQL/Einmalig`-Skript.
- Zeilenenden (byte-genau, `git cat-file blob` + Python-Zaehlung `b'\r\n'`): alle 21 Dateien reines LF (CRLF=0); die
  sieben vorbestehenden Dateien waren vorher ebenfalls reines LF — keine Mischung, keine Konvertierung.
- TS-83 ohne Rueckfragen abarbeitbar: ja, bis auf das fehlende `DELETE` in TS-83.5 und den fehlenden Hinzufuegen-Parallelfall
  in TS-83.6 (M-4).

### 6. Manuelle Test-Checkliste (Mensch, Testsystem IDEAL — nach dem I-1-Fix)

1. TS-83.1 auf Sage-DB IDEAL und WMS-DB ausfuehren; Ergebnis eintragen. Treffer → melden, nicht abnehmen.
2. TS-83.2 `sp_helptext` auf `vw_IDEAL-AKE_Kommissionierung_FAInfos`; Filterkriterium notieren.
3. DB-Backup, dann `SQL/94_AddBeschichtungsauftrag.sql` **zweimal** ausfuehren: erster Lauf „Tabelle … angelegt", zweiter
   ohne Fehler und ohne Meldung; genau eine History-Zeile `20261012073551_AddBeschichtungsauftrag`.
4. Web publizieren und recyceln; Log beim Start ohne Migrationsfehler.
5. TS-83.4 FreshInstall auf leerer DB: beide Tabellen, `IX_Beschichtungsauftraege_Nummer` und `…_Jahr_LaufendeNummer`
   UNIQUE, History-Eintrag vorhanden.
6. TS-83.5 doppelter Insert scheitert am UNIQUE-Index; **danach**
   `DELETE FROM dbo.Beschichtungsauftraege WHERE Nummer = 'BA-2026-99999';` — sonst lautet die naechste Nummer
   `BA-2026-100000`.
7. TS-83.7 Regression: Beschichtungsauftrag Liste/Summiert, Druck, PDF (TS-60, TS-74, TS-81, TS-82) unveraendert; kein
   neuer Menuepunkt; Hilfe → Changelog zeigt v1.50.0.
8. TS-83.6 (mit Teil 4): paralleler Statuswechsel → Concurrency-Meldung; paralleles Anlegen desselben HauptFA → einer
   uebernimmt, der andere meldet „vergeben" bzw. „erneut versuchen"; **paralleles Hinzufuegen derselben freien Position in
   zwei Erst-Entwuerfe → der Verlierer bekommt Klartext, keine Fehlerseite** (Pruefstein fuer I-1).

### 7. Nachlauf nach a5b27550 (qa-agent, 2026-10-12)

Synchron, ohne Unteragent, im Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5` (HEAD `a5b27550`, 7 Dateien,
+122/−28). **Ergebnis: Testbereit** — Build und Tests gruen, I-1 geschlossen, keine offenen Critical/Important.

**Build und Tests (eigener Lauf)**

```
dotnet build IdealAkeWms.slnx   -> 0 Fehler, 12 Warnungen (vorbestehend, CS8321 u. a.)
dotnet test                     -> IdealAkeWms.Tests:        Bestanden! Fehler: 0, erfolgreich: 1568, übersprungen: 1, gesamt: 1569
                                   IDEALAKEWMSService.Tests: Bestanden! Fehler: 0, erfolgreich:  272, übersprungen: 0, gesamt:  272
```
(+2 gegenueber dem ersten Lauf = die zwei neuen Tests; Skip unveraendert vorbestehend.)

**Mutationsproben** (temporaer, per `git checkout` zurueckgesetzt, `git status` danach sauber):
- Erst-Entwurf wieder mit `SpeichernAsync` **innerhalb** der Transaktion → `Hinzufuegen_ErstEntwurf_SpeicherFehler_
  KlartextStattAusnahme_NichtsGeaendert` rot. Der Test erkennt den I-1-Fehler.
- `VerwerfeAenderungen` als No-op → `VeralteteRowVersion_ConcurrencyMeldung_UndContextSauber` und beide
  `Anlegen_NummernKonflikt_*` rot. Das Verwerfen ist abgesichert.

**Proben zu den Folgen von `ChangeTracker.Clear()`** (temporaere Testdatei, danach geloescht, alle 3 gruen):
- *Reklamation, erstes Speichern scheitert, zweiter Versuch*: genau 2 Auftraege in der DB, Reklamation `BA-2026-00002`,
  `UrsprungsauftragId` = Ursprung, Positionen 1+2 mit den Mengen des Ursprungs, insgesamt 6 Positionen (4 + 2, keine
  Geisterzeilen), Ursprung bleibt `Erledigt`. Begruendung im Code: `ursprung` ist nach `Clear()` detached, wird im
  Versuch aber nur noch **gelesen** (`ursprung.Id`, Kopffelder, `Kopie(p)` kopiert Skalare); der neue Auftrag haengt nur
  ueber die FK-Spalte, nicht ueber die Navigation am Ursprung (`BeschichtungsauftragWorkflow.cs:141-153`) — das
  `Add` zieht den Ursprung also nicht in den Context.
- *Anlegen, zwei Fehlschlaege, dritter Versuch*: 1 Auftrag, 4 Positionen. `AnlegenAsync` liest je Versuch alles neu
  (`:92`, `:100`), haelt keine Objekte ueber `Clear()` hinweg.
- *Entwurf bearbeiten mit Entfernen + veralteter RowVersion*: Klartext, ChangeTracker leer, DB hat noch alle 4
  Positionen und den alten Kopf; derselbe Aufruf mit frischer RowVersion **im selben Context** gelingt (3 Positionen).
- Weitere Pfade, die nach `Clear()` getrackte Objekte benutzen: keine. In `MitFehlerbehandlungAsync` (`:351-373`) wird
  nach dem Verwerfen nur noch `nummer` (string, vorher gelesen) verwendet; `MitNummernVersuchenAsync` (`:377-397`)
  baut jeden Versuch neu auf. `VerwerfeAenderungen` hat genau drei Aufrufer (`:359`, `:364`, `:390`); der Workflow hat
  ausser DI (`Program.cs:117`) noch keinen Aufrufer.

**Ergebnis je Befund**
- **I-1 — geschlossen.** `PositionenHinzufuegenAsync` Erst-Entwurf (`:260-279`): `MitFehlerbehandlungAsync` umschliesst
  `InSerializableTransactionAsync`; innen `SpeichernRohAsync` ohne Fang. Scheitert ein Lesen (Deadlock 1205 ueber
  `IstDeadlock`, `:410`) oder das Speichern, laeuft die Ausnahme aus `action()` heraus, `CommitAsync`
  (`BeschichtungsauftragRepository.cs:61`) wird nicht erreicht, `await using` rollt zurueck; aussen Klartext. Ablehnung
  durch Pruefung (`:273`) kehrt vor jedem `Add` zurueck → Commit einer reinen Lesetransaktion, unkritisch. Der neue Test
  bildet die SQL-Server-Semantik (Commit nach gescheitertem Speichern wirft) nach und ist per Mutation als
  unterscheidend belegt. Echter Deadlock bleibt **Manual-UAT** (TS-83.6, Checkliste Punkt 8).
- **Fehlerbehandlung einheitlich:** alle fuenf Speicherpfade laufen ueber genau zwei Fang-Stellen —
  `MitFehlerbehandlungAsync` (Bearbeiten `:196`, Hinzufuegen Reklamation `:255`, Hinzufuegen Erst-Entwurf `:260`,
  Bemerkung `:296`, Status `:320`) und `MitNummernVersuchenAsync` (Anlegen, Reklamation). Beide fangen
  `DbUpdateException` (inkl. Concurrency) und Deadlock. Kein Workflow-Pfad wirft bei diesen Fehlern noch.
  *Hinweis (kein Befund):* Andere Infrastrukturfehler, z. B. Timeout oder Verbindungsabbruch, laufen bei den Lesezugriffen
  ausserhalb der Fang-Stellen (`LadenAsync` `:327`, `GetHinzufuegbarePositionenAsync`) und als
  Nicht-Deadlock-`SqlException` weiterhin als Ausnahme durch. Das ist gewollt, denn ein nicht erreichbarer DB-Server ist
  kein Fall fuer „bitte erneut versuchen". Teil 4 deckt das mit der globalen Fehlerseite ab.
- **M-1 — erledigt.** `VerwerfeAenderungen() => _db.ChangeTracker.Clear()` (`BeschichtungsauftragRepository.cs:48`)
  nach jedem gescheiterten Speichern, auch im Nummern-Retry. *Hinweis fuer Teil 4:* `Clear()` leert den **ganzen**
  scoped `ApplicationDbContext`. Laedt oder aendert ein Controller im selben Request vor dem Workflow-Aufruf andere
  Entitaeten, sind diese nach einem Fehler detached. Deshalb gilt die Regel „ein Workflow-Aufruf je Request, danach
  Redirect" (Code-Kommentar `:47`). Teil 4 muss sie einhalten.
- **M-2 — erledigt.** `ILogger<BeschichtungsauftragWorkflow>` (DI ueber `AddLogging`, DI-Test ergaenzt). Deadlock →
  `LogWarning` + „gleichzeitige Änderung", sonstige `DbUpdateException` → `LogError` + „Datenbankfehler, Details im
  Log", Concurrency ohne Log (fachlich erwartet). Retry loggt jeden Versuch als Warning, Endmeldung nennt „gleichzeitige
  Anlage oder Datenbankfehler". Die Grenze (jede `DbUpdateException` wird wiederholt) steht im `ponytail:`-Kommentar
  (`:388-389`). Akzeptiert.
- **M-3 — erledigt (Test).** `VeralteteRowVersion_ConcurrencyMeldung_UndContextSauber` belegt den Fang-Pfad auf
  InMemory. Die Formulierung in TS-83-Einleitung (`docs/TESTSZENARIEN.md:9411`, „RowVersion-Konflikt nicht
  InMemory-testbar") bleibt unscharf. Das ist Kosmetik, denn die echte `rowversion` auf SQL Server ist weiterhin
  Manual-UAT. Behandlung: keine.
- **M-4 — erledigt.** TS-83.5 nennt jetzt `DELETE FROM dbo.Beschichtungsauftraege WHERE Nummer = 'BA-2026-99999';`.
  TS-83.6 enthaelt den parallelen Hinzufuegen-Fall: dieselbe freie Position in zwei Entwuerfe, erwartet ist Klartext und
  keine Fehlerseite.
- **M-5 — offen (Minor, Brain).** `secondbrain/feature-map.md` hat weiterhin keinen Eintrag fuer v1.50.0/Teil 3 (Grep
  `1.50` und `ausbau-teil-3`: kein Treffer). Der Dev traegt ihn nach. Das blockiert Testbereit nicht.
- **M-6 — erledigt.** `BeschichtungsauftragMigrationSqlTests.cs` nach `IdealAkeWms.Tests/Repositories/` verschoben
  (100 % Rename, Namespace passt jetzt).
- **M-7 — unveraendert akzeptiert** (Hinweis, Drift-Risiko benannt).

**Geaenderte bestehende Tests (in a5b27550):** `BeschichtungsauftragWorkflowTests.Fx` reicht jetzt einen
`NullLogger` an den Konstruktor durch, `BeschichtungsauftragDiResolutionTests` ruft zusaetzlich `services.AddLogging()`
auf. Beides ist eine reine Typ- bzw. Compile-Anpassung an den neuen Konstruktor-Parameter. `BeschichtungsauftragMigrationSqlTests`
wurde nur verschoben. Keine Fixture wurde inhaltlich veraendert, keine Assertion abgeschwaecht.

**Beweisart:** unveraendert wie Abschnitt 3. AK 10 hat jetzt einen automatisierten Fang-Pfad
(`VeralteteRowVersion_…`, `Hinzufuegen_ErstEntwurf_SpeicherFehler_…`). Der echte Parallelfall bleibt **Manual-UAT**
(TS-83.6).

**Manuelle Checkliste:** Abschnitt 6 gilt unveraendert, Punkt 8 ist der Pruefstein fuer I-1. Zusaetzlich:
9. Nach Punkt 8 im Web-Log (Serilog) pruefen: Ein provozierter Deadlock erscheint als Warning
   „Beschichtungsauftrag … Deadlock-Opfer", nicht als Error und nicht als unbehandelte Ausnahme.
