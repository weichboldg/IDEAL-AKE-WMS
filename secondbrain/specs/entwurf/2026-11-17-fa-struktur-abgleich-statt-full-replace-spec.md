---
type: spec
title: FA-Struktur mit Abgleich und Lebenslauf statt Full-Replace
slug: 2026-11-17-fa-struktur-abgleich-statt-full-replace-spec
status: Entwurf
created: 2026-11-17
updated: 2026-11-17
source_backlog: "[[2026-11-17-fa-struktur-abgleich-statt-full-replace]]"
task: ""
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
standort: IDEAL
affected_code:
  - IDEALAKEWMSService/Services/FaHierarchySyncService.cs
  - IDEALAKEWMSService/Services/FaStrukturAbgleich.cs (neu, reiner Planer)
  - IDEALAKEWMSService/Services/ISageFaStrukturReader.cs
  - IDEALAKEWMSService/Services/SageFaStrukturReader.cs
  - IDEALAKEWMSService/Services/FaHierarchySql.cs
  - IDEALAKEWMSService/Services/Sdata/FaStrukturQuelleSwitch.cs
  - IDEALAKEWMSService/Services/Sdata/SdataFaStrukturReader.cs
  - IDEALAKEWMSService/Services/FaMaterializationSyncService.cs
  - IDEALAKEWMSService/Services/WorkOperationStructureDetectionService.cs
  - IdealAkeWms/Models/FaHierarchyNode.cs
  - IdealAkeWms/Models/FaHierarchyOrderInfo.cs
  - IdealAkeWms/Models/ProductionOrderPosition.cs
  - IdealAkeWms/Models/ServiceSettingDefinitions.cs
  - IdealAkeWms/Data/ApplicationDbContext.cs
  - IdealAkeWms/Services/BdeTeilelisteService.cs
  - IdealAkeWms/Services/KommissionierListenService.cs
  - IdealAkeWms/Services/VormontageService.cs
  - IdealAkeWms/Controllers/FaHierarchyController.cs
  - IdealAkeWms/Views (FaHierarchy, FaHierarchyKommissionierListen, FaHierarchyVormontage, FaHierarchyBeschichtung, BdeTeileliste)
  - SQL/158_FaStrukturAbgleich.sql (Nummer beim Dev-Start pruefen)
  - SQL/00_FreshInstall.sql
  - scripts/deploy/nachlauf.psd1
open_questions:
  - "Sage IDEAL: Was heisst gelöscht und was storniert bei einem FA? Wird ein gelöschter FA physisch aus KHKPpsFaBelege entfernt, und bedeutet Aktiv = 0 storniert (entfernen) oder nur inaktiv (behalten)?"
  - "Default der Aufbewahrungsfrist Sync:FaStrukturAufbewahrungTage: Vorschlag 90 Tage – passt das?"
  - "Arbeitslisten (Kommissionierliste, Vormontage, Beschichtung, BDE-Teileliste): verschwundene Zeilen gekennzeichnet anzeigen (wie im Backlog) oder standardmäßig ausblenden mit Schalter? Und darf man in der BDE-Teileliste auf eine Zeile eines in Sage beendeten FA noch melden?"
  - "Nur falls P-AB4 mehr als einen Mandanten in KHKPpsFaBelege zeigt: Mandant der Sage-Einzelabfrage aus dem bestehenden Sync:ProductionWorkplaceSyncMandant nehmen oder eigener Key?"
epic: true
etappen:
  - "E0 Vorarbeit: View-Definitionen und Prüfabfragen lesen (kein Code)"
  - "E1 Datenmodell, Migration SQL/158, FreshInstall, Setting-Definition"
  - "E2 Abgleich-Planer (rein) und Knoten-Sync ohne Sage-Abfrage"
  - "E3 Sage-Einzelabfrage und Verschwunden-Gründe"
  - "E4 OrderInfos und ProductionOrderPositions mit derselben Logik"
  - "E5 Aufräumen nach Erledigt oder Frist"
  - "E6 R4 Artikeltyp vom Artikel"
  - "E7 Konsumenten: aktiv-Filter im Dienst, Kennzeichnung in der Web-App"
  - "E8 Doku, Testszenarien, Manifest, Version, Brain"
deploy:
  web: true
  service: true
  migration: true
freigabe_entscheidung: ""
freigabe_von: ""
freigabe_am: ""
---

# FA-Struktur mit Abgleich und Lebenslauf statt Full-Replace

Löst R2 und R4 aus [[2026-11-16-fa-struktur-ideal-ueber-sdata-spec]]. Entscheidungen (a) bis (f) von Gerald vom 2026-11-17 im Backlog
[[2026-11-17-fa-struktur-abgleich-statt-full-replace]] sind verbindlich und werden hier nicht neu verhandelt.

**Standort:** Die Logik wirkt nur an der **IDEAL** (Lauf „FA-Hierarchie“ nur bei `Sync:HierarchicalFaEnabled = true`, ServiceSettingDefinitions
:230). Das Schema (SQL/158) läuft an beiden Firmen. An der AKE sind die drei Tabellen leer, dort ändert sich nichts sichtbar.

## Ziel / Nutzen (das Warum)

Heute ersetzt der Lauf „FA-Hierarchie“ bei jedem Durchlauf alle drei Tabellen vollständig:

- `DELETE FROM dbo.FaHierarchyNodes` und `DELETE FROM dbo.FaHierarchyOrderInfos`, danach Neuanlage in einer Transaktion (`FaHierarchySyncService.cs:97-106`).
- `DELETE FROM dbo.ProductionOrderPositions`, danach Neuanlage in einer eigenen Transaktion (`FaHierarchySyncService.cs:157-183`, Löschung :167).

Was die Quelle nicht mehr liefert, ist danach im WMS weg. Die Ressource `PpsFaBelegeInfo` liest `vwDPSBS_CWS_PPS_FaBelegeInfo` mit
`WHERE Fa.IstBeendet = 0 AND Fa.Aktiv = -1` ([[2026-11-17-pruefabfragen-gerald]], Abschnitt „View FaBelegeInfo“). Beendete FAs fehlen deshalb
auf allen Ebenen (R2, R3). Bei einer Umstellung auf SData würde das WMS in Sage beendete Strukturen sofort verlieren, obwohl im WMS noch
kommissioniert, beschichtet oder gemeldet wird.

Nutzen:
- Beendete FAs bleiben sichtbar und gekennzeichnet (a).
- Jede Zeile trägt einen Lebenslauf (b).
- Gelöschte FAs verschwinden gezielt, nicht pauschal (b).
- Aufgeräumt wird nachvollziehbar (c).
- Ein fachlicher Schlüssel macht Zeilen über Läufe hinweg wiedererkennbar (d). Das ist die Grundlage für die AG-Rückmeldung an Sage
  ([[2026-10-30-fa-rueckmeldung-an-sage]], f).

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**
- Abgleich neu, geändert, verschwunden statt DELETE und Insert, für `FaHierarchyNodes`, `FaHierarchyOrderInfos` und `ProductionOrderPositions`.
  Gilt für die View-Quelle und die SData-Quelle gleich (a, f).
- Vier Lebenslauf-Spalten an allen drei Tabellen (b).
- Eindeutige Schlüssel mit NULL-Behandlung (d).
- Lesende Einzelabfrage in Sage nur für verschwundene FAs (b).
- Aufräumregel mit neuem ServiceSetting (c).
- R4: Artikeltyp der FA-Knoten vom Artikel (e).
- Konsumenten:
  - Der Dienst liest nur aktive Zeilen.
  - Die Web-App zeigt verschwundene Zeilen gekennzeichnet.
- Aktivitätsprotokoll, Prüfabfragen, Testszenarien, Manifest.

**Out-of-Scope**
- Schreiben nach Sage. Die Einzelabfrage ist ein reines `SELECT`.
- Weitere Artikelfelder umstellen. Grundsatz (e) gilt, umgestellt wird hier nur `Artikeltyp`. Die übrigen `USER_OS*`-Felder bleiben, wie
  `FelderSetzen` sie heute zuordnet (`SdataFaStrukturReader.cs:228-252`), bis P-FS1 der SData-Spec sie klärt. Bestehende Spalten bleiben.
- Das 30-Tage-Fenster des SData-Kopf-Reads (`SdataFaStrukturReader.cs:47`, :76-77).
- Freigabe des SData-Modus (`SageQuelleSperre`).
- WMS-Aufträge beim Verschwinden automatisch auf erledigt setzen. Die Materialisierung bleibt, wie sie ist (siehe E7). Als Folgeidee notieren.
- Datenmigration: IDEAL-PROD hat 0 Knoten ([[2026-11-17-pruefabfragen-gerald]]). Die Bestandszeilen auf WMS TEST bekommen nur Startwerte (SQL/158).
- Einmal-Skripte unter `SQL/Einmalig`, die `FaHierarchyNodes` lesen (v1.47.4, Bereinigung00erArtikel, v1.99.32 Teileliste), sind Altbestand und
  werden nicht angepasst. Künftig sehen sie auch verschwundene Zeilen. Hinweis in `fallstricke.md`.

## Fachliche Anforderungen

1. **Abgleich je Lauf.** Jede gelesene Zeile wird über ihren fachlichen Schlüssel mit dem Bestand verglichen:
   - **Neu:** anlegen, `ErstGesehen = ZuletztGesehen = jetzt`.
   - **Vorhanden:** Werte übernehmen, `ZuletztGesehen = jetzt`. War die Zeile verschwunden, werden `VerschwundenSeit` und `VerschwundenGrund`
     geleert („wieder da“, selbstheilend wie `SageMissingSince`, ADR 0012).
   - **Im Bestand, aber nicht gelesen:** Die Zeile ist verschwunden. Wie es weitergeht, regeln die Anforderungen 2 und 3.
2. **Ursache klären, nur für verschwundene FAs.** Die Owner-FA einer Zeile ist der FA, in dessen Stückliste sie steht:
   `VaterFA ?? HauptFA` (Knoten), `HauptFA` (OrderInfo), `BelID` (Position). Für verschwundene FA-Knoten (`SubFA != 0`) zählt zusätzlich der
   Sub-FA selbst. Die Einzelabfrage geht nur an FAs, deren Zustand der Lauf nicht selbst kennt.
3. **Entscheidung je verschwundener Zeile:**

   | Lage | Ergebnis | VerschwundenGrund | Count |
   |---|---|---|---|
   | FA-Knoten, Sub-FA in Sage `IstBeendet = -1` | behalten | `beendet` | `verschwunden_beendet` |
   | Owner-FA wurde in diesem Lauf gelesen, Zeile fehlt trotzdem (Position in Sage entfernt) | entfernen (heute schon so) | – | `entfernt_position` |
   | Owner-FA nicht gelesen, in Sage `IstBeendet = -1` | behalten | `beendet` | `verschwunden_beendet` |
   | Owner-FA nicht gelesen, in Sage nicht gefunden | entfernen | – | `entfernt_in_sage` |
   | Owner-FA nicht gelesen, in Sage `Aktiv = 0` | bis Rückfrage 1 **behalten** | `inaktiv` | `verschwunden_inaktiv` |
   | Owner-FA nicht gelesen, in Sage offen und aktiv | behalten, Warnung | `unklar` | `verschwunden_unklar` |

   Ist eine Zeile schon verschwunden, behalten `VerschwundenSeit` und `VerschwundenGrund` ihren ersten Wert. Der Grund wird nur
   aktualisiert, wenn die Abfrage einen anderen Zustand liefert (z. B. `unklar` → `beendet`). Bereits verschwundene Zeilen werden bei jedem Lauf
   erneut abgefragt, damit „gelöscht“ auch nachträglich greift.
4. **Schutz vor defektem Read.** Nie wird alles als verschwunden markiert, stattdessen wird gemeldet:
   - Der bestehende Empty-Guard bleibt (`FaHierarchySyncService.cs:59-75`).
   - **Neu:** Gibt es aktive Zeilen und taucht keine einzige der bisher aktiven Haupt-FAs im Read auf, gilt der Read als defekt.
     Neue und geänderte Zeilen werden übernommen, es wird nichts markiert und nichts entfernt. Dazu Warnung, Fehlermail und
     Count `verschwunden_gesperrt = 1`.
   - Wirft die Sage-Einzelabfrage einen Fehler, gilt dasselbe für Markieren und Entfernen in diesem Lauf, Count `sage_abfrage_fehler = 1`.
     Ein Lesefehler entfernt nie etwas.
5. **Aufräumen (c).** Eine Haupt-FA-Struktur wird vollständig entfernt (Knoten, OrderInfos, Positionen mit `StrukturID = HauptFA`), wenn
   - alle ihre Zeilen in allen drei Tabellen verschwunden sind, **und**
   - eine der beiden Bedingungen gilt:
     - es gibt mindestens einen WMS-Auftrag mit `OrderNumber = HauptFA`, und alle diese Aufträge sind `IsDone` oder `IsCancelled`;
     - der jüngste `VerschwundenSeit` der Struktur ist älter als `Sync:FaStrukturAufbewahrungTage`.

   Ohne WMS-Auftrag (an IDEAL-PROD ist die Materialisierung aus) gilt nur die Frist. Ein leeres „alle erledigt“ zählt nicht.
6. **Eindeutiger Schlüssel (d):**
   - Knoten: `(HauptFA, VaterFA, SubFA, Position, Artnr)`, wie `UX_BdeTeilMeldungen_Schluessel` ohne `Kuerzel` (`ApplicationDbContext.cs:1554-1574`).
   - OrderInfos: `(HauptFA, ABNr, Pos)`. Bestätigt wird das per P-AB2. Liefert P-AB2 Dubletten, wird `MontageAbteilung` angehängt und
     das in der Spec vermerkt.
   - Positionen: `BelPosID` (UNIQUE besteht, `ApplicationDbContext.cs:1169-1185`).
   - BelID und StrukturID sind schon vorhanden. Am Knoten sind `VaterFA` die BelID des Stücklisten-FA und `SubFA` die BelID des Nachfolgers.
     Belegt in `SdataFaStrukturReader.cs:376-379`, Wurzel `SubFA = HauptFA` :362. `HauptFA` ist die StrukturID. `ProductionOrderPosition` trägt
     `BelID` und `StrukturID`. Es sind keine neuen BelID-Spalten nötig.
7. **R4 (e):** `Artikeltyp` kommt für alle Knoten vom Artikel (`USER_OSArtikeltyp` am Artikel), auch für FA-Knoten. Heute nimmt die SData-Seite
   bei `SubFA != 0` den FA-Kopf (`SdataFaStrukturReader.cs:231`, :244), und dort ist der Wert leer. Auf der View-Seite ist der Wert schon „BG“.
   Ob er dort aus dem Artikel kommt, zeigt E0.
8. **Konsumenten:**
   - Materialisierung und Arbeitsgang-Erkennung lesen nur aktive Zeilen. Das ist das heutige Verhalten, weil verschwundene Zeilen heute
     gelöscht sind.
   - Sichtbare Ansichten zeigen verschwundene Zeilen mit Text-Kennzeichnung („in Sage beendet“, „in Sage inaktiv“, „in Sage ungeklärt“), nie nur
     mit Farbe. Ob Arbeitslisten sie standardmäßig ausblenden, klärt Rückfrage 3.

## Technischer Loesungsentwurf

### Erster Dev-Schritt (E0, kein Code)

Auf Sage `IDEAL` (IDEALSQL2\SAGE100TEST) die Abfragen P-AB6 und P-AB8 ausführen (Abschnitt Prüfabfragen) und das Ergebnis hier unter „Befund
E0“ eintragen:
- Liefert `vw_IDEAL_AKE_WMS_FAListe` beendete FAs?
- Woher kommt `Artikeltyp`?
- Woher kommt `FAInfos`? Laut ADR 0029 aus `[PPSImport].[dbo].[vw_PPSData]`.

Wichtig: Knoten werden nur gelesen, wenn ihre HauptFA in FAInfos steht (`FaHierarchySql.cs:139-152`, `WHERE EXISTS … Infos`).
Filtert FAInfos beendete FAs, fehlen auch die Knoten. Die Abgleich-Logik ist davon unabhängig. Der Befund bestimmt nur, wie oft der Fall
`beendet` im View-Betrieb vorkommt.

### Muster

- Reiner, DB-freier Planer nach dem Muster der Materialisierung (Planner + Service, ADR 0012), testbar ohne SQL.
- Schlüsselvergleich über das vorhandene `TeilSchluessel` (`IdealAkeWms/Services/BdeTeilelisteService.cs:12-17`). Leere Artnr wird dort schon zu `null`.
  Ladder-Stufe 2: kein zweiter Schlüsseltyp.
- Wertübernahme mit `_ctx.Entry(alt).CurrentValues.SetValues(neu)`, nachdem `neu.Id`, `ErstGesehen` und die Lebenslauf-Felder vom Bestand
  übernommen wurden. „Geändert“ heißt: EF meldet eine geänderte Eigenschaft außer `ZuletztGesehen`. Kein eigener Feldvergleich.
  `FaStrukturQuelleSwitch.VergleichsFelder` (:20-51) taugt dafür nicht, weil es trimmt und an der Wurzel Felder auslässt.
  `PositionText` ist `[NotMapped]` (`FaHierarchyNode.cs:30-33`), `SetValues` ignoriert es, und es zählt nicht zum Schlüssel.

### Kollation: C#-Vergleich gleich SQL-Index (neuer Fallstrick)

Der UNIQUE-Index vergleicht nach der Spalten-Kollation (Groß/Klein egal, nachlaufende Leerzeichen egal). Ein Ordinal-Vergleich in C# würde
`"A1"` und `"a1 "` als zwei Zeilen sehen, beim INSERT verletzt das dann den Index. Der Planer vergleicht deshalb `Artnr` mit `TrimEnd()` und
`OrdinalIgnoreCase`. Für `ABNr` und `Pos` gilt dasselbe. Leer und NULL werden als NULL behandelt.

### NULL im eindeutigen Index

- SQL Server behandelt NULL in einem UNIQUE-Index als einen Wert, nicht als „unbekannt“.
- Die Wurzel hat `VaterFA = NULL` und `Position = NULL`. Je HauptFA gibt es genau eine Wurzel, das passt.
- Blätter haben `SubFA = 0` (`FaHierarchyNode.cs:24`) und eine Position. Zwei gleiche Blätter in derselben Stücklistenposition wären eine
  echte Dublette.
- Kein gefilterter Index, keine berechneten Spalten. Ladder: Der native Index genügt.

### Dubletten im Read

EF InMemory erzwingt keinen UNIQUE-Index (MEMORY-Notiz), deshalb gibt es einen App-Wächter im Planer:
- Enthält der Read zu einem Schlüssel mehrere Zeilen, wird die **ganze HauptFA in diesem Lauf nicht angefasst**. Der Bestand bleibt; ist
  die HauptFA neu, wird sie nicht angelegt.
- Count `schluessel_doppelt` (Anzahl HauptFAs), Warnung mit den ersten 20 Schlüsseln, Fehlermail.
- Grund: Mengen nicht still verlieren. Dass das vorkommen kann, legt `TeilelisteZeile.Eindeutig` nahe (`BdeTeilelisteService.cs:39-40`).
  P-AB1 misst es.

### Ablauf `FaHierarchySyncService.RunAsync` (ersetzt :77-123)

1. `LesenAsync`, `LogQuelleAsync`, Empty-Guard: unverändert (:55-75).
2. Bestand der Knoten und OrderInfos laden, getrackt. Bei einigen tausend Zeilen ist das unkritisch. `ExecuteUpdate` wird bewusst nicht
   genutzt, weil InMemory es nicht testet.
3. `FaStrukturAbgleich.Planen(bestand, read, jetzt)` liefert: Neu, Übernehmen (mit „wieder da“), Fehlend (mit Owner-FA und Sub-FA),
   gesperrte HauptFAs (Dubletten).
4. Guard „alles verschwunden“ (Anforderung 4).
5. Abfrage-Menge bilden: Owner-FAs fehlender Zeilen, die im Read nicht als Owner vorkommen, plus `SubFA` fehlender FA-Knoten, plus
   Owner-FAs schon verschwundener Zeilen. Dann `ISageFaStrukturReader.FaStatusLesenAsync(belIds)` aufrufen (neu).
6. Klassifizieren nach der Tabelle in Anforderung 3.
7. **Eine Transaktion** für Knoten und OrderInfos wie heute (:97-106): anlegen, übernehmen, markieren, entfernen, `SaveChanges`, `Commit`.
8. Positionen nur, wenn `lesung.Positionen` gesetzt ist: dieselbe Logik in eigener Transaktion. Die heutige Fehlertoleranz von
   `PositionenErsetzenAsync` (:157-183) bleibt: Ein Fehler lässt den Lauf erfolgreich, mit Warnung und Mail. Im View-Betrieb wird die
   Tabelle nicht angefasst, wie heute.
9. Aufräumen (Anforderung 5), eigene Transaktion je Lauf.
10. `LogAmbiguousHauptFasAsync` (:112), Kombigeräte-Meldung (:205-219) und Counts.
11. **DryRun** (:80-93): Planer und Sage-Abfrage laufen lesend, Counts werden mit Suffix `[DryRun]` geschrieben, es gibt keine Mutation.

`SyncedAt` behält die Spalte. Die Bedeutung wird „Zeitpunkt der letzten Wertänderung“: Gesetzt wird es nur bei Neu oder Geändert.
Gelesen wird `FaHierarchyNode.SyncedAt` in der App nirgends; grep `\.SyncedAt` über `*.cs`/`*.cshtml` zeigt nur andere Entitäten und Tests.
Doku-Kommentar in `FaHierarchyNode.cs:5-12` und :66-68 anpassen („Full-Refresh-Cache“ ist überholt).

### Sage-Einzelabfrage (nur lesend)

- Neue Methode `Task<IReadOnlyDictionary<int, FaSageStatus>> FaStatusLesenAsync(IReadOnlyCollection<int> belIds, CancellationToken ct)` an
  `ISageFaStrukturReader`. Heute hat das Interface nur `LesenAsync`, `ISageFaStrukturReader.cs:20-22`.
- Implementiert in `SageFaStrukturReader`, das die `SageConnection` schon nutzt. `FaStrukturQuelleSwitch` delegiert immer an den View-Reader,
  auch im SData-Modus.
- Grund: `dtsFaBelegsuche` (RealTimeData) liefert beendete FAs, ist aber nur im Modus Vergleich erlaubt (ADR 0029,
  `FaStrukturQuelleSwitch.cs:118`). SQL ist der eine Weg für beide Quellen.
- SQL (Spaltennamen per P-AB3 bestätigen, Mandant per P-AB4/Rückfrage 4):

  ```sql
  SELECT BelID, IstBeendet, Aktiv FROM dbo.KHKPpsFaBelege WHERE BelID IN (@p0, @p1, ...)
  ```

  In Blöcken zu 1000 Parametern (Grenze 2100). Nur `SELECT`, keine Schreibrechte nötig.
- `FaSageStatus`: `Beendet` (IstBeendet = -1), `Inaktiv` (Aktiv = 0), `Offen`. BelID nicht im Ergebnis = nicht gefunden.
- Das Leserecht des Logins der `SageConnection` auf `KHKPpsFaBelege` prüft P-AB5. Fehlt es, wirft die Abfrage, und Anforderung 4 greift.

### Konsumenten

**Dienst, nur aktive Zeilen (`VerschwundenSeit == null`):**
- `FaMaterializationSyncService.cs:70`. Sonst würden beendete Knoten neue WMS-Aufträge anlegen oder das heutige `SageMissingSince`-Markieren
  verhindern.
- `WorkOperationStructureDetectionService.cs:72`.

**Web, alle Zeilen, gekennzeichnet:**
- Repositories laden weiter alle Zeilen (`FaHierarchyNodeRepository`, `FaHierarchyOrderInfoRepository.cs:18-47`, Cache-Decorator 5 min
  unverändert). Die Entität trägt `VerschwundenSeit` und `VerschwundenGrund`, also braucht es keine neue Repository-Methode.
- Kennzeichnung als Badge mit Text an Strukturkopf bzw. HauptFA-Zelle. Keine neue Spalte, damit keine neue Spaltenfilter-Pflicht.
  Badge-Text aus einer statischen Zuordnung Grund → deutscher Text (eine Stelle). Kontrast nach WCAG AA, Skill `frontend-design` ist Pflicht.

| Ansicht | Leser (Beleg) | Verhalten |
|---|---|---|
| FA-Struktur | `FaHierarchyController.cs:83-84`, :187 | Badge am Strukturkopf, verschwundene Unterknoten mit Badge |
| Kommissionierliste Liste/Summiert, Ziel-Dropdown | `KommissionierListenService.cs:102-103`, :125-126, :252 | Badge in der Gruppenzeile (Rückfrage 3) |
| Vormontage | `VormontageService.cs:207`, :236-237, :270-271 | Badge in der Gruppenzeile (Rückfrage 3) |
| Beschichtung | `BeschichtungsauftragService.cs:68`, `BeschichtungsauftragVergabeAnzeige.cs:46`, `BeschichtungsauftragWorkflow.cs:91`, :102 | Badge (Rückfrage 3) |
| BDE-Teileliste | `BdeTeilelisteService.cs:128-129`, Melden :285-287 | Badge. Melden auf verschwundene Zeilen bis Rückfrage 3 **abgewiesen** mit „FA in Sage beendet – nicht meldbar“ |
| BOM-Brücke | `FaHierarchyBomRepository.cs:68`, :70, :208 | unverändert (Stückliste eines Auftrags bleibt lesbar) |
| Leitstand / FA-Liste | `ProductionOrderRepository.cs:131`, :353, :355 (OrderInfos für Kunde-Filter) | unverändert. Beendete Infos bleiben, der Kunde-Filter trifft auch erledigte Aufträge |
| Kommissionierziele, Prüfliste Arbeitsschritte | `PickingAreaRepository.cs:33`, `OperationConfigsController.cs:93` | unverändert (Wertelisten) |

Dass der Badge-Text nicht nur Farbe ist, gilt für alle Zeilen der Tabelle.

### R4

`SdataFaStrukturReader.FelderSetzen` (:228-252): `n.Artikeltyp = F(artikel, "USER_OSArtikeltyp")` statt `F(os, …)`, eine Zeile.
`USER_OSArtikeltyp` steht schon in `FelderArtikel` (:156-161). View-Weg: gemäß Befund E0. Liest die View den Typ nicht vom Artikel, wird das als
Befund gemeldet, nicht still umgebaut (View gehört Sage-Seite).

### ADR und Fallstricke

- **Neuer ADR 0032:** „FA-Struktur als Abgleich mit Lebenslauf statt Full-Replace“. Ergänzt ADR 0029. ADR 0030 bleibt gültig:
  `FaHierarchyNode.Id` ist weiterhin kein fachlicher Schlüssel, auch wenn er jetzt stabil bleibt.
- **Fallstrick 47:** C#-Schlüsselvergleich muss der Spalten-Kollation folgen. Einmal-Skripte sehen jetzt verschwundene Zeilen.

## Migrations-/SQL-Auswirkungen

Nummer **SQL/158** (Stand heute, letzte Datei `SQL/157_AddBeschichtungsauftragPositionText.sql`; beim Dev-Start prüfen). Migration
`<ts>_FaStrukturAbgleich` nach `20261117141308_AddBeschichtungsauftragPositionText`. Nicht datenvernichtend.

Je Tabelle `FaHierarchyNodes`, `FaHierarchyOrderInfos`, `ProductionOrderPositions`, jede DDL in eigenem Batch mit Guard:

1. `IF COL_LENGTH(N'dbo.<T>', N'ErstGesehen') IS NULL ALTER TABLE dbo.<T> ADD ErstGesehen datetime2 NULL;`, ebenso `ZuletztGesehen`.
2. `VerschwundenSeit datetime2 NULL`, `VerschwundenGrund nvarchar(20) NULL`.
3. Startwerte für Bestandszeilen (WMS TEST, 755 Knoten): `UPDATE dbo.<T> SET ErstGesehen = SyncedAt, ZuletztGesehen = SyncedAt WHERE ErstGesehen IS NULL;`
4. `ALTER TABLE dbo.<T> ALTER COLUMN ErstGesehen datetime2 NOT NULL;`, ebenso `ZuletztGesehen`. Kein Default-Constraint, der Dienst setzt
   die Werte immer.
5. Knoten: Vor dem Index Leer zu NULL: `UPDATE dbo.FaHierarchyNodes SET Artnr = NULL WHERE LTRIM(RTRIM(Artnr)) = N'';`. Dann Dubletten-Check: gibt es
   Dubletten (Abfrage P-AB1), folgt `RAISERROR(... 16 ...)` mit Text „Doppelte FA-Struktur-Schlüssel – Pruefung/FaStrukturSchluesselDoppelt“
   und Abbruch.
   Sonst:
   `IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_FaHierarchyNodes_Schluessel' AND object_id = OBJECT_ID(N'dbo.FaHierarchyNodes'))
   CREATE UNIQUE INDEX [UX_FaHierarchyNodes_Schluessel] ON [dbo].[FaHierarchyNodes]([HauptFA],[VaterFA],[SubFA],[Position],[Artnr]);`
   Schlüssellänge 4·4 + 400 Byte, unter der Grenze von 1700.
6. OrderInfos: analog `UX_FaHierarchyOrderInfos_Schluessel (HauptFA, ABNr, Pos)`, gegebenenfalls `+ MontageAbteilung` gemäß P-AB2. Leer
   wird vorher zu NULL.
7. Index `IX_<T>_VerschwundenSeit` ist nicht nötig, die Tabellen werden vollständig gelesen (YAGNI).
8. `__EFMigrationsHistory`-Insert in separatem Batch.

Weitere Punkte:
- **EF-Migration:** dieselben Statements per `migrationBuilder.Sql` (Fallstrick §9: kein strukturiertes `CreateIndex` auf manuell geführten
  Indizes). Das Modell bekommt `HasIndex(...).IsUnique().HasDatabaseName(...)` für den Snapshot.
- **FreshInstall an zwei Stellen:**
  - Spalten und Indizes in Abschnitt 17g (`SQL/00_FreshInstall.sql:1927-1993`) und 17g2 (:1996-2028), direkt in `CREATE TABLE` und als
    Index-Batches.
  - `MigrationId` im History-Block (Muster :3292-3294).
- **AgentJobs:** keine. `SQL/AgentJobs` referenziert die Tabellen nicht (grep).
- **Backup:** nicht nötig (additiv). An PROD sind die Tabellen leer.

## Einstellungen

| Key | Typ | Erlaubte Werte | Default | Wirkung | Verwendung | Standort | Warnhinweis |
|---|---|---|---|---|---|---|---|
| `Sync:FaStrukturAufbewahrungTage` | Int (`IntMeta` 1–3650, Einheit „Tage“, Server validiert) | 1–3650 | 90 (Rückfrage 2) | So viele Tage nach dem letzten Verschwinden wird eine vollständig verschwundene Haupt-FA-Struktur aus dem WMS entfernt, auch wenn ihre WMS-Aufträge nicht erledigt sind | Dienst: Lauf „FA-Hierarchie“ | IDEAL | „Kleinere Werte entfernen beendete Strukturen früher aus FA-Struktur, Kommissionierliste und BDE-Teileliste. Sage bleibt unberührt.“ |

Eintrag in `ServiceSettingDefinitions.All` (Gruppe „FA-Hierarchie“, Muster :236), Anzeigename in `Meta` („FA-Struktur: Aufbewahrung verschwundener
FAs“), Grenzen in `IntMeta` (:177-195), Drift-Guard-Test grün (ADR 0008, 0020). Keine weitere Einstellung. Einen Mengen-Deckel wie
`Sync:ReconcileMaxCancelPerRun` braucht es nicht: Entfernt wird nur, was Sage als nicht vorhanden meldet. Ein Teil-Read markiert höchstens
`unklar`, das heilt sich selbst.

## Audit-Feld-Auswirkungen

Keine. Die drei Tabellen sind keine `AuditableEntity`, sondern Dienst-Spiegel (`FaHierarchyNode.cs:8-10`). Nachvollziehbarkeit kommt aus
`ErstGesehen`, `ZuletztGesehen`, `VerschwundenSeit` und `VerschwundenGrund` sowie dem Aktivitätsprotokoll. Neue Rollen oder Filter gibt es
nicht. Die Ansichten behalten ihre `RequireXxxAccess`-Filter.

### Aktivitätsprotokoll (ADR 0010)

`SyncLogServices.FaHierarchy` bleibt. Counts deutsch, nur Werte > 0 sind interessant, alle Keys werden geschrieben:

| Key | Bedeutung |
|---|---|
| `neu` | neu angelegte Zeilen (Knoten + Infos), wie bisher |
| `geaendert` | Zeilen mit geänderten Werten |
| `wieder_da` | vorher verschwundene, jetzt wieder gelesene Zeilen |
| `verschwunden_beendet` / `verschwunden_inaktiv` / `verschwunden_unklar` | neu markierte Zeilen je Grund |
| `entfernt_position` | in Sage aus der Stückliste entfernte Zeilen |
| `entfernt_in_sage` | Zeilen von FAs, die Sage nicht mehr kennt |
| `aufgeraeumt` | Strukturen (HauptFAs), die die Aufräumregel entfernt hat |
| `schluessel_doppelt` | HauptFAs mit Dubletten im Read (nicht angefasst) |
| `verschwunden_gesperrt` / `sage_abfrage_fehler` | 1 = Markieren/Entfernen in diesem Lauf ausgesetzt |
| `hauptfa_mehrdeutig` | wie bisher |
| `positionen_neu` / `positionen_entfernt` | nur bei SData-Positionen (ersetzt `positionen`) |

`geloescht` entfällt (Summe der `entfernt_*`). Fehlermail bei `schluessel_doppelt > 0`, `verschwunden_gesperrt`, `sage_abfrage_fehler`. Bei
`verschwunden_unklar` gibt es nur dann eine Mail, wenn der Lauf eine Zeile neu als `unklar` markiert hat (keine Mail-Flut).

## Akzeptanzkriterien

1. Ein Lauf mit unverändertem Read ändert keine Zeile außer `ZuletztGesehen`, `Id` bleibt gleich, Counts `neu = 0`, `geaendert = 0`.
2. Eine neue Stücklistenzeile in Sage erscheint nach dem Lauf mit `ErstGesehen = ZuletztGesehen = Laufzeit`, Count `neu` steigt.
3. Eine geänderte Sollmenge wird übernommen, `ErstGesehen` bleibt, `SyncedAt` wird neu gesetzt, Count `geaendert = 1`.
4. Ein in Sage beendeter Unter-FA, der nicht mehr gelesen wird, bleibt mit `VerschwundenGrund = beendet` und gesetztem `VerschwundenSeit` erhalten.
   Das gilt auch für seine Stücklistenzeilen.
5. Eine beendete Haupt-FA bleibt vollständig erhalten (Knoten und OrderInfos), gekennzeichnet `beendet`.
6. Ein FA, den `KHKPpsFaBelege` nicht mehr enthält, wird mit allen seinen Zeilen entfernt, Count `entfernt_in_sage`.
7. Eine aus der Stückliste eines gelesenen FA entfernte Position verschwindet aus dem WMS, Count `entfernt_position`.
8. Taucht eine verschwundene Zeile wieder auf, sind `VerschwundenSeit` und `VerschwundenGrund` leer, `ErstGesehen` unverändert, Count `wieder_da`.
9. Leerer Read: keine Mutation, Warnung, Mail (wie bisher).
10. Read ohne eine einzige bisher aktive Haupt-FA: neue Zeilen werden übernommen, nichts markiert oder entfernt, Warnung, Mail,
    `verschwunden_gesperrt = 1`.
11. Fehlschlag der Sage-Einzelabfrage (z. B. fehlendes Recht): nichts markiert oder entfernt, `sage_abfrage_fehler = 1`, Mail, Lauf erfolgreich.
12. Dubletten-Schlüssel im Read: diese HauptFA bleibt unverändert, `schluessel_doppelt = 1`, Warnung nennt den Schlüssel, Mail. Die anderen
    HauptFAs werden abgeglichen.
13. `Artnr` „A1“ im Bestand und „a1 “ im Read gelten als dieselbe Zeile (kein Indexfehler).
14. Aufräumen: Eine vollständig verschwundene Struktur, deren WMS-Aufträge alle erledigt oder storniert sind, ist nach dem Lauf weg
    (alle drei Tabellen), Count `aufgeraeumt`. Ohne WMS-Auftrag erst nach Ablauf der Frist.
15. Eine teilweise verschwundene Struktur (Haupt-FA aktiv, ein Unter-FA beendet) wird nie aufgeräumt.
16. `Sync:FaStrukturAufbewahrungTage` erscheint in `/ServiceSettings` mit Anzeigename, Wirkung, Warnhinweis. Werte außerhalb 1–3650 weist der
    Server beim Speichern ab.
17. DryRun schreibt nichts, auch nicht in die Lebenslauf-Felder, und liefert die Counts mit `[DryRun]`.
18. SData-Modus (Vergleich): FA-Knoten bekommen `Artikeltyp` vom Artikel (z. B. „BG“), nicht leer.
19. Materialisierung legt für verschwundene Knoten keinen WMS-Auftrag an und markiert einen Auftrag, dessen Knoten verschwunden ist, wie
    bisher mit `SageMissingSince`.
20. FA-Struktur, Kommissionierliste, Vormontage, Beschichtung, BDE-Teileliste zeigen verschwundene Zeilen mit lesbarem Text-Badge (nicht nur
    Farbe). Verhalten in den Arbeitslisten gemäß Antwort auf Rückfrage 3.
21. BDE-Teileliste: Melden auf eine verschwundene Zeile wird mit Text abgewiesen (bis Antwort auf Rückfrage 3 anders entscheidet).
22. Im View-Betrieb bleibt `ProductionOrderPositions` unberührt.
23. SQL/158 läuft zweimal hintereinander fehlerfrei (idempotent). Bei Dubletten bricht es mit lesbarer Meldung ab, ohne halbe Änderung am
    Index.
24. Es wird nichts an Sage geschrieben. Die Einzelabfrage ist nur `SELECT`.

## Test-Szenarien

Neues Kapitel **TS-157** (nächste freie Nummer, zuletzt TS-156; beim Dev-Start prüfen) „FA-Struktur: Abgleich mit Lebenslauf“ in
`docs/TESTSZENARIEN.md`, Index `secondbrain/tests/testszenarien-index.md`. Testsystem: DEV IDEAL (WMS-DB TEST mit Sage `IDEAL` auf
IDEALSQL2\SAGE100TEST, Dienstkonto mit Leserecht, siehe R5 der SData-Spec).

**Vorbedingungen:**
- `Sync:HierarchicalFaEnabled = true`.
- `Sync:Quelle:FaStruktur = View`.
- Ein Lauf ist durchgelaufen.
- Bekannte FAs: eine offene Haupt-FA mit Unter-FAs, eine beendete Haupt-FA (z. B. 1059463) und eine Haupt-FA mit beendetem Unter-FA.

| Nr | Schritt | Erwartet |
|---|---|---|
| 157.1 | Lauf zweimal hintereinander, dazwischen nichts ändern | Zweiter Lauf: `neu 0`, `geaendert 0`. `SELECT MIN(Id), MAX(Id)` unverändert |
| 157.2 | In Sage-Test eine Sollmenge ändern, Lauf | `geaendert ≥ 1`, Wert übernommen, `ErstGesehen` alt |
| 157.3 | In Sage-Test einen Unter-FA beenden, Lauf | Zeilen bleiben, Grund `beendet`. FA-Struktur zeigt Badge „in Sage beendet“ |
| 157.4 | Beendete Haupt-FA ansehen | Struktur und Kopfdaten sichtbar, Badge am Kopf |
| 157.5 | In Sage-Test eine Stücklistenposition löschen, Lauf | Zeile weg, `entfernt_position 1` |
| 157.6 | In Sage-Test einen ungenutzten FA löschen, Lauf | Struktur weg, `entfernt_in_sage ≥ 1` |
| 157.7 | Beendung zurücknehmen, Lauf | `wieder_da`, Badge weg |
| 157.8 | `Sync:FaHierarchyListeViewName` auf eine leere Test-View | Empty-Guard: nichts geändert, Mail |
| 157.9 | Dem Dienst-Login das SELECT auf `KHKPpsFaBelege` entziehen (nur DEV), Szenario 157.3 wiederholen | `sage_abfrage_fehler 1`, nichts markiert, Mail. Recht zurückgeben |
| 157.10 | Aufräumen: Frist auf 1 setzen, beendete Struktur ohne WMS-Auftrag, Lauf am Folgetag | `aufgeraeumt 1`, Struktur weg |
| 157.11 | Aufräumen über Erledigt: alle WMS-Aufträge der beendeten Haupt-FA erledigen, Lauf | Struktur weg |
| 157.12 | BDE-Teileliste: Zeile eines beendeten FA melden | Abweisung mit Text, keine Meldung angelegt |
| 157.13 | Kommissionierliste und Vormontage mit beendeter HauptFA | Badge in der Gruppenzeile; Verhalten gemäß Rückfrage 3 |
| 157.14 | `/ServiceSettings`: Aufbewahrung 0 und 4000 speichern | Abgewiesen mit Meldung; 90 wird angenommen |
| 157.15 | Modus Vergleich: FA-Knoten-Artikeltyp | „BG“ (vom Artikel), Vergleichsprotokoll ohne Artikeltyp-Abweichung |
| 157.16 | DryRun-Lauf | keine Änderung an den Lebenslauf-Feldern, Counts mit `[DryRun]` |

**Negativfälle:** 157.8, 157.9, 157.12, 157.14. AKE-Regression: An DEV AKE startet die App ohne Fehler, FA-Liste und Kommissionierung sind
unverändert (Tabellen leer).

**Automatische Tests (xUnit):**
- Planer: alle Zeilen der Tabelle in Anforderung 3, Dubletten, Kollation (Akzeptanzkriterium 13), Wieder-da, Guard „alles verschwunden“.
- Sync-Service mit InMemory und gemocktem Reader (`FaStatusLesenAsync` wirft bzw. liefert).
- Aufräumregel.
- `FelderSetzen`-Artikeltyp.
- Drift-Guard Settings.

UNIQUE-Index und Kollation sind nur im Manual-UAT prüfbar (InMemory).

## Etappen (nur bei epic: true)

Ein Worktree, ein Merge am Ende. Je Etappe ein Commit, Build und Tests grün. Den Branch während der Arbeit mit
`scripts/sync-worktree.ps1 -Slug 2026-08-07-ideal-teile-1-5` auf main-Stand halten.

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| 0 | Vorarbeit: P-AB6/P-AB8 (View-Definitionen) und P-AB1/P-AB2 (WMS TEST) lesen, Befund in die Spec (Hauptcheckout). Kein Code-Commit. **Hängt an Gerald (Sage-Zugang).** Fehlt der Befund, laufen E1–E8 trotzdem, Schlüssel der OrderInfos dann `(HauptFA, ABNr, Pos)` | offen | – |
| 1 | Modell (4 Felder × 3 Tabellen, Unique-Indizes), Migration, SQL/158, FreshInstall 2×, `Sync:FaStrukturAufbewahrungTage` in Definitions + Meta + IntMeta | offen | |
| 2 | `FaStrukturAbgleich` (rein) + Knoten/Infos-Abgleich im Sync, Guard „alles verschwunden“, Dubletten-Wächter. Verschwundene ohne Sage-Abfrage vorläufig `unklar`, nichts entfernt außer `entfernt_position` | offen | |
| 3 | `FaStatusLesenAsync` (SQL, Blöcke) + Klassifizierung + Entfernen bei „nicht gefunden“. `Aktiv = 0` → `inaktiv` behalten bis Rückfrage 1. **Spaltennamen/Mandant gemäß P-AB3/P-AB4 – ohne Befund mit `BelID, IstBeendet, Aktiv` ohne Mandant-Filter bauen und als Blocker vermerken** | offen | |
| 4 | `ProductionOrderPositions` mit derselben Logik (ersetzt `PositionenErsetzenAsync`), Counts `positionen_*` | offen | |
| 5 | Aufräumregel + Setting-Nutzung | offen | |
| 6 | R4: `FelderSetzen` Artikeltyp vom Artikel (+ Test) | offen | |
| 7 | Konsumenten: Materialisierung/Erkennung nur aktiv; Badges in FA-Struktur, Kommissionierliste, Vormontage, Beschichtung, BDE-Teileliste; Melde-Abweisung. **Arbeitslisten-Verhalten gemäß Rückfrage 3 – ohne Antwort: anzeigen mit Badge** | offen | |
| 8 | TS-157 + Index, Hilfeseite, Changelog (Anwender + Brain), Version beide `AppVersion.cs` (nächste freie, heute v1.99.35), README Settings, Manifest, ADR 0032, Fallstrick 47, codebase/datenmodell + services, feature-map | offen | |

## Prüfabfragen (für den Menschen, nur SELECT)

Ergebnisse unter „Befund E0“ eintragen. Aussagen, die davon abhängen, sind bis dahin **unbelegt**.

**P-AB1 – Dubletten Knotenschlüssel (WMS `IDEAL_AKE_WMS_TEST`):**
```sql
SELECT HauptFA, VaterFA, SubFA, Position, NULLIF(LTRIM(RTRIM(Artnr)), N'') AS Artnr, COUNT(*) AS Anzahl
FROM dbo.FaHierarchyNodes
GROUP BY HauptFA, VaterFA, SubFA, Position, NULLIF(LTRIM(RTRIM(Artnr)), N'')
HAVING COUNT(*) > 1;
```

**P-AB2 – Dubletten OrderInfo-Schlüssel (WMS `IDEAL_AKE_WMS_TEST`):**
```sql
SELECT HauptFA, NULLIF(LTRIM(RTRIM(ABNr)), N'') AS ABNr, NULLIF(LTRIM(RTRIM(Pos)), N'') AS Pos, COUNT(*) AS Anzahl
FROM dbo.FaHierarchyOrderInfos
GROUP BY HauptFA, NULLIF(LTRIM(RTRIM(ABNr)), N''), NULLIF(LTRIM(RTRIM(Pos)), N'')
HAVING COUNT(*) > 1;
```

**P-AB3 – Spalten und Primärschlüssel `KHKPpsFaBelege` (Sage `IDEAL`):**
```sql
SELECT c.name, t.name AS typ, c.is_nullable
FROM sys.columns c JOIN sys.types t ON t.user_type_id = c.user_type_id
WHERE c.object_id = OBJECT_ID(N'dbo.KHKPpsFaBelege')
  AND c.name IN (N'BelID', N'StrukturID', N'IstBeendet', N'Aktiv', N'Mandant', N'Istendtermin');

SELECT i.name AS pk, c.name AS spalte
FROM sys.indexes i
JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
WHERE i.object_id = OBJECT_ID(N'dbo.KHKPpsFaBelege') AND i.is_primary_key = 1;
```

**P-AB4 – Verteilung Mandant/Beendet/Aktiv (Sage `IDEAL`):**
```sql
SELECT Mandant, IstBeendet, Aktiv, COUNT(*) AS Anzahl
FROM dbo.KHKPpsFaBelege
GROUP BY Mandant, IstBeendet, Aktiv
ORDER BY Mandant, IstBeendet, Aktiv;
```

**P-AB5 – Leserecht des Dienst-Logins (Sage `IDEAL`, angemeldet als das Login aus der `SageConnection` des Dienstes, je TEST und PROD):**
```sql
SELECT SUSER_SNAME() AS login, HAS_PERMS_BY_NAME(N'dbo.KHKPpsFaBelege', N'OBJECT', N'SELECT') AS darf_lesen;
```

**P-AB6 – View-Definitionen (Sage `IDEAL`), erster Dev-Schritt:**
```sql
SELECT OBJECT_DEFINITION(OBJECT_ID(N'dbo.vw_IDEAL_AKE_WMS_FAListe')) AS FAListe;
SELECT OBJECT_DEFINITION(OBJECT_ID(N'dbo.vw_IDEAL_AKE_WMS_FAInfos')) AS FAInfos;
```
Zu beantworten: Filtert eine der Views auf `IstBeendet`/`Aktiv`? Woher kommt die Spalte `Artikeltyp`, aus `KHKArtikel` oder `KHKPpsFaBelege`?

**P-AB8 – Enthält die FAListe beendete FAs? (Sage `IDEAL`):**
```sql
SELECT
  (SELECT COUNT(DISTINCT f.HauptFA) FROM dbo.vw_IDEAL_AKE_WMS_FAListe f
     JOIN dbo.KHKPpsFaBelege b ON b.BelID = f.HauptFA WHERE b.IstBeendet = -1) AS beendete_hauptfa,
  (SELECT COUNT(DISTINCT f.SubFA) FROM dbo.vw_IDEAL_AKE_WMS_FAListe f
     JOIN dbo.KHKPpsFaBelege b ON b.BelID = f.SubFA WHERE f.SubFA <> 0 AND b.IstBeendet = -1) AS beendete_subfa;
```

### Befund E0

(vom Dev-Lauf bzw. Gerald auszufüllen)

## Deploy

**Vorläufig, wird vom QA-Lauf gegen den echten Diff bestätigt.**

- **Web-App:** ja (Badges, Melde-Abweisung, Settings-Anzeige).
- **Service:** ja (Sync, Materialisierung, Erkennung).
- **Migration:** ja, SQL/158, additiv. Reihenfolge: Dienst stoppen, SQL/158, Web, Dienst starten. Der Dienst darf nicht zwischen Schema und
  neuem Code laufen, weil der alte Code `DELETE` und `Insert` ohne die NOT-NULL-Spalten `ErstGesehen`/`ZuletztGesehen` ausführen würde.
- **Manifest-Einträge** (`scripts/deploy/nachlauf.psd1`):
  - `@{ Id='Pruefung/FaStrukturSchluesselDoppelt'; Version='<neu>'; Typ='Pruefung'; Firma='beide'; Phase='Vorher'; Ausfuehrung='anzeigen'; Pruefung='SELECT COUNT(*) FROM (SELECT 1 AS x FROM dbo.FaHierarchyNodes GROUP BY HauptFA, VaterFA, SubFA, Position, NULLIF(LTRIM(RTRIM(Artnr)), N'''') HAVING COUNT(*) > 1) d'; Text='Doppelte FA-Struktur-Schlüssel verhindern SQL/158 – vorher klären (Spec 2026-11-17-fa-struktur-abgleich)' }`
  - `@{ Id='SQL/158'; Version='<neu>'; Typ='Schema'; Firma='beide'; Phase='Schema'; Ausfuehrung='automatisch'; Datei='158_FaStrukturAbgleich.sql'; MigrationId='<ts>_FaStrukturAbgleich'; Voraussetzung='additiv; bricht bei doppelten Schlüsseln ab'; Quelle='<brain-changelog>' }`
    (Schema-Reihenfolge: nach SQL/157 einreihen, nie nach Dateisortierung).
  - `@{ Id='Einstellung/FaStrukturAufbewahrungTage'; Version='<neu>'; Typ='Einstellung'; Firma='IDEAL'; Phase='Nachher'; Ausfuehrung='anzeigen'; Text='/ServiceSettings: Sync:FaStrukturAufbewahrungTage prüfen (Default 90 Tage)' }`
  - `@{ Id='Info/FaStrukturBeendetSichtbar'; Version='<neu>'; Typ='Info'; Firma='IDEAL'; Phase='Nachher'; Text='In Sage beendete FAs bleiben in FA-Struktur, Kommissionierliste, Vormontage, Beschichtung und BDE-Teileliste sichtbar und sind gekennzeichnet.' }`
  - Kein Daten- oder Einmal-Skript, also kein Trockenlauf.
- **Publish-Befehle** (im Worktree):
  - `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`
  - `dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService`

## Offene Rueckfragen

1. **Sage IDEAL, gelöscht und storniert:** Wird ein gelöschter FA physisch aus `KHKPpsFaBelege` entfernt? Bedeutet `Aktiv = 0` „storniert“
   (→ entfernen) oder nur „inaktiv“ (→ behalten)? Bis zur Antwort werden nur nicht gefundene FAs entfernt, `Aktiv = 0` wird mit Grund
   `inaktiv` behalten.
2. **Default der Aufbewahrungsfrist** `Sync:FaStrukturAufbewahrungTage`: Vorschlag 90 Tage. Passt das?
3. **Arbeitslisten** (Kommissionierliste, Vormontage, Beschichtung, BDE-Teileliste): Verschwundene Zeilen gekennzeichnet anzeigen, wie
   im Backlog, oder standardmäßig ausblenden mit Schalter „Beendete einblenden“? Und darf man in der BDE-Teileliste noch auf eine Zeile eines in
   Sage beendeten FA melden? Vorschlag: anzeigen mit Badge, nicht meldbar.
4. **Nur falls P-AB4 mehr als einen Mandanten zeigt:** Soll der Mandant der Sage-Einzelabfrage aus dem bestehenden
   `Sync:ProductionWorkplaceSyncMandant` kommen (Beschreibung dann erweitern), oder gibt es einen eigenen Key?

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
-->

1. Gelöscht/storniert in Sage IDEAL (physisch entfernt? Aktiv = 0?) →
2. Default Aufbewahrungsfrist (Vorschlag 90 Tage) →
3. Arbeitslisten: anzeigen mit Badge oder ausblenden mit Schalter; Teileliste meldbar ja/nein →
4. Mandant der Einzelabfrage (nur falls P-AB4 mehrere zeigt) →
