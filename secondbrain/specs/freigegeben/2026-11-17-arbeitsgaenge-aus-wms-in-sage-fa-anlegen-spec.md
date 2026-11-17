---
type: spec
title: "Arbeitsgänge aus dem WMS in Sage-Fertigungsaufträgen anlegen (IDEAL) – Vorschau + Anlage"
slug: 2026-11-17-arbeitsgaenge-aus-wms-in-sage-fa-anlegen-spec
status: Testbereit
created: 2026-11-17
updated: 2026-11-17
source_backlog: "[[2026-10-30-fa-rueckmeldung-an-sage]]"
task: "[[2026-11-17-goal-lauf-sage-ag-anlage]]"
jira:
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
standort: IDEAL
affected_code:
  - "IdealAkeWms/Controllers/<neu> SageFaArbeitsgaengeController (Name beim Dev-Start festlegen)"
  - "IdealAkeWms/Views/<neu>/Index.cshtml (Vorschau-Liste, Listen-View-Pattern)"
  - "IdealAkeWms/Services/SageSdata/SageSdataReadClient.cs (nur Wiederverwendung, Lesen Sage-AG-Positionen)"
  - "IdealAkeWms/Services/SageSdata/<neu> Client fuer FertigungsauftragService ServiceOption 4 (Etappe 1, nach Muster IDEALAKEWMSService/Services/SageWareneingangClient.cs)"
  - "IdealAkeWms/Services/SageSdata/SdataZugangsdaten.cs (nur Wiederverwendung WebStandAsync)"
  - "IdealAkeWms/Models/ViewModels/FaHierarchyTreeViewModel.cs (nur Wiederverwendung FaArbeitsschrittSymbole.Tokenize)"
  - "IdealAkeWms/Models/OperationConfig.cs (nur Wiederverwendung WirksameSageArbeitsgangnummer)"
  - "IdealAkeWms/Models/ServiceSettingDefinitions.cs (neuer Schalter, Etappe 1)"
  - "IdealAkeWms/Services/SyncLogger/SyncLogServices.cs (neuer Service-Name, Etappe 1)"
  - "IdealAkeWms/Models/<neu> Sende-Status je FA+AG (nur falls Rueckfrage 8 Variante b) + Migration + SQL/<Nr> + 00_FreshInstall"
  - "Views/Shared/_Layout.cshtml bzw. Menue (Eintrag)"
  - "docs/TESTSZENARIEN.md, Views/Help, Views/Help/Changelog.cshtml, beide AppVersion.cs"
open_questions:
  - "1 Ressource: Welche RessourceTyp/RessourceNummer sendet das WMS je Arbeitsgang (nur Arbeitsgangnummer oder zusaetzlich ein Arbeitsplatz; wenn Arbeitsplatz: woher, Werkbank-Nummern taugen nicht, §45)?"
  - "2 Ebene: Werden die Arbeitsgaenge an jedem Unter-FA angelegt, am Haupt-FA oder an beiden (Haupt-FA traegt eigene Kuerzel, z. B. 1059463)?"
  - "3 Blaetter (SubFA = 0, Kuerzel vom Artikel, kein eigener FA): auslassen oder am Vater-FA anlegen?"
  - "4 Begonnene FAs: Nur FAs ohne jede Rueckmeldung beschreiben, oder auch begonnene? Woran gilt ein FA als begonnen (Status-Wert, Ist-Mengen)?"
  - "5 Reihenfolge: Der Request hat kein Positions-/Sortierfeld. Reicht die Reihenfolge der Kuerzel im Feld Arbeitsschritte (Sage vergibt die Position beim Anlegen), oder ist eine bestimmte Positionsnummer Pflicht?"
  - "6 Einlastung und Kosten: Welche Wirkung soll die Anlage in Sage haben (Kapazitaet, Kalkulation, Nachkalkulation)? Muss nach der Anlage eingelastet werden (ServiceOption 5) – oder bewusst nicht?"
  - "7 Bisheriger IDEAL-Test-Schreibweg: Die Notiz sagt nur 'derzeit Fehler' bzw. 'Test idealsage2… hat laut Gerald noch einen Fehler'. Was genau schlug fehl, und welcher Endpunkt/Dataset ist der IDEAL-Test fuer den Schreibtest?"
  - "8 Duplikatschutz: Ein Positions-Userfeld fuer eine WMS-Kennung gibt es laut L7 nicht (FaBelegPositionUserfield leer, PositionProperty1-4 nur Laenge 1). Variante a) Benutzerfeld in Sage anlegen lassen (DPS/Sage-Admin), b) WMS-eigener Sende-Status je FA+AG plus Lookup in Sage vor jedem Senden, c) beides?"
  - "9 Schreibweg: Synchron aus der Web-App (Klick, Bestaetigung, Banner) oder ueber eine Dienst-Warteschlange wie der Sage-Wareneingang?"
  - "10 Rolle: Reicht [RequireAdminAccess] fuer Vorschau und Anlage, oder soll die Fachabteilung (Fertigungsplanung) die Vorschau lesen bzw. anlegen duerfen (dann neue Rolle)?"
  - "11 Menge: Welcher Wert geht in FaBelegArbeitsgangPosition.Menge (Sollmenge des FA, Fertigungsmenge, 1, leer lassen)?"
  - "12 Bearbeiter: Was steht im Feld Bearbeiter (leer, fester Sage-Mitarbeiter, angemeldeter Windows-Benutzer)? Am AKE-Test scheiterte ein Aufruf an ValidateMitarbeiter (Befund §11)."
  - "13 Doppelte Kuerzel im selben Knoten (z. B. 'PR PR'): einmal oder zweimal anlegen?"
  - "14 Ausschlussliste: Gilt Sync:ProductionWorkplaceSyncAusschlussliste auch hier (Kuerzel auf der Liste werden nie angelegt), oder nur die Sage-Zuordnung (OperationConfig) entscheidet?"
  - "15 Vergleich 'in Sage schon vorhanden': genuegt der Abgleich ueber die Sage-Arbeitsgangnummer je FA (auch fuer manuell in Sage angelegte AGs), und wie werden AGs gezeigt, die in Sage stehen, aber nicht im Kuerzel-Feld?"
epic: true
etappen:
  - "E0 Vorschau ohne Schreiben (Soll-AG je FA, Status, Filter, CSV)"
  - "E1 Anlage in Sage per SData FertigungsauftragService ServiceOption 4 (Schalter, je FA mit Bestaetigung, Duplikatschutz, Protokoll) – Schreibtest an EINEM FA am IDEAL-Test erst nach Freigabe Gerald"
deploy:
  web: true
  service: true   # Antwort 9: Dienst-Warteschlange (Koordination 20:12)
  migration: true # Antwort 8: Warteschlangen-Tabelle
freigabe_entscheidung: "Empfehlungen 1–15 (Dienst-Warteschlange, Schalter Default aus), testweise; Schreibtest nur nach Freigabe Gerald"
freigabe_von: "Gerald Weichbold"
freigabe_am: "2026-11-17"
---

# Arbeitsgänge aus dem WMS in Sage-Fertigungsaufträgen anlegen (IDEAL)

> [!info] Quelle
> Es gibt **keine Backlog-Datei.** Die Quelle ist die Ideen-Notiz [[2026-10-30-fa-rueckmeldung-an-sage]], und zwar die Abschnitte
> „IDEAL-Befund 2026-11-16“ (Z. 99-111), „Frage Gerald 2026-11-16 15:44“ (Z. 113-129), „Richtung Gerald 2026-11-17 07:43“
> (Z. 131-132) und „Entscheidung 19:52“ (Z. 134-135). Dazu kommen ihre Anhänge: der SData-Befund vom AKE-Test (2026-10-30) und
> die DPS-Anfrage vom 2026-11-03. Beide Anhänge waren lesbar.
> Der Auftrag kam von Gerald am 2026-11-17: Stufe 0 und Stufe 1 bauen, Stufe 1 hinter einem Schalter mit Default aus. Den
> Schreibtest an einem FA am IDEAL-Test gibt es erst nach seiner Freigabe. Der Dev-Lauf folgt nach dem Sammel-Goal vom 17.11.
> im selben Bündel-Worktree.

**Standort:** nur **IDEAL**, mit hierarchischen FAs aus Sage (`ProduktionsauftragHierarchisch`). AKE ist ausdrücklich nicht
betroffen: Dort kommen die Arbeitsgänge aus OSEON.

## Ziel / Nutzen

An der IDEAL tragen viele Sage-FAs keine Arbeitsgang-Positionen. FA 1059463 und die Unter-FAs 1065141/43/44 haben je 0
AG-Positionen; insgesamt gibt es nur 18.398 AG-Positionen auf einem Teil der FAs (Ideen-Notiz, IDEAL-Befund). Die Soll-Arbeitsgänge
stehen aber als Kürzel im FA-Feld `USER_OSArbeitsschritte`, z. B. „PU ISO VL MK PR PL VP“ an 1059463. Das WMS kennt seit v1.99.27
die Zuordnung Kürzel → Sage-Arbeitsgangnummer ([[2026-11-16-arbeitsgang-mapping-sage-arbeitsgangnummer-spec]], ADR 0031).

- **Etappe 0** zeigt je FA, welche Arbeitsgänge fehlen, ohne etwas zu schreiben. Die Fachabteilung kann so die Zuordnung prüfen
  und die fehlenden Sage-AGs nachpflegen (Mail an Benjamin/Walter/Andi vom 17.11.).
- **Etappe 1** legt die fehlenden AGs in Sage an (Variante A, Richtung Gerald 07:43). Damit werden später Rückmeldungen gegen
  echte Sage-AG-Positionen möglich.

## Umfang

**Teil davon**
- E0: Vorschau-Liste „Soll-Arbeitsgänge je FA“ mit Status je Zeile, Filterkarte, Server-Spaltenfilter, Pagination und CSV-Export.
- E1: Knopf „In Sage anlegen“ je FA, mit Bestätigungsdialog. Dazu der neue Schalter (Default aus), Duplikatschutz, keine Änderung
  an beendeten FAs, ein Protokoll je Aufruf und Fehler als Banner.

**Nicht Teil davon**
- Keine Rückmeldung von Mengen oder Zeiten (Ist-Menge, Ist-Rüst-/Stückzeit). Das bleibt Thema der Ideen-Notiz.
- Kein Löschen und kein Ändern vorhandener Sage-AG-Positionen. Es wird nur angelegt.
- Kein Einlasten (ServiceOption 5), solange Rückfrage 6 das nicht verlangt.
- AKE nicht.
- Keine Massen- oder Automatik-Anlage, kein Hintergrundjob. Angelegt wird nur manuell je FA.
- Keine Pflege der Sage-Arbeitsgänge selbst (fehlende `USER_PNKZ`). Das macht die Fachabteilung in Sage.

## Fachliche Anforderungen

### Begriffe (Glossar)
- **FA** = Sage-Fertigungsauftrag mit der Kennung `BelID`. `Belegnummer` ist an der IDEAL immer 0 (IDEAL-Befund).
- **Haupt-FA** = Wurzel der Struktur (`FaHierarchyNode.VaterFA == null`, `IdealAkeWms/Models/FaHierarchyNode.cs:21-22`). Die
  StrukturID ist gleich der BelID.
- **Unter-FA** = Knoten mit eigener BelID (`SubFA != 0`). Ein **Blatt** hat `SubFA = 0` und keinen eigenen FA
  (`FaHierarchyNode.cs:24-25`).
- **Kürzel** = Token aus `Arbeitsschritte`, getrennt nur an Leerzeichen (`FaArbeitsschrittSymbole.Tokenize`,
  `IdealAkeWms/Models/ViewModels/FaHierarchyTreeViewModel.cs:259-264`, dieselbe Regel wie der Dienst, fallstricke §44).
- **Sage-AG** = `OperationConfig.WirksameSageArbeitsgangnummer` (manuell vor Sage, `IdealAkeWms/Models/OperationConfig.cs:65`).
  Das ist eine Arbeitsgang- und **keine** Arbeitsplatznummer (ADR 0031, fallstricke §45).

### Herkunft der Kürzel (belegt bzw. offen)
- Im SData-Modus von `Sync:Quelle:FaStruktur` stammen die Kürzel für Knoten mit eigenem FA aus dem FA-Feld und für Blätter
  vom Artikel. Beleg: `IDEALAKEWMSService/Services/Sdata/SdataFaStrukturReader.cs:231` (`var os = n.SubFA != 0 ? fa : artikel;`)
  und `:243`.
- Die IDEAL läuft heute im View-Modus (`Sync:Quelle:FaStruktur` = View, [[2026-11-16-fa-struktur-ideal-ueber-sdata-spec]]). Ob die
  View-Spalte `Arbeitsschritte` für Unter-FAs ebenfalls das FA-Feld liefert oder das Artikelfeld, ist **nicht belegt** → P-6.
  Die Vorschau zeigt die Kürzel so, wie sie in `FaHierarchyNodes` stehen. Weicht die Herkunft ab, ist das ein Befund für die
  Fachabteilung und kein stiller Ausgleich.

### E0 – Vorschau (je Zeile: ein FA × ein Kürzel)
1. Datenquelle sind die aktiven Knoten aus `FaHierarchyNodes` (`VerschwundenSeit IS NULL`, ADR 0032, fallstricke §48). Die Knoten
   werden auf die beschreibbaren FAs gemäß Rückfrage 2/3 eingeschränkt. Bis zur Antwort gilt der Vorschlag: Haupt-FA und
   Unter-FAs, keine Blätter.
2. Je Knoten werden die Kürzel per `Tokenize` zerlegt und je Kürzel die `OperationConfig` gesucht. Der Treffer folgt derselben
   Regel wie die Prüfliste: getrimmt, ohne Groß-/Kleinschreibung, die kleinste Id gewinnt
   (`IdealAkeWms/Controllers/OperationConfigsController.cs:211-212`). Diese Regel wird wiederverwendet und nicht neu geschrieben.
3. Status je Zeile, genau einer aus der festen Liste:
   - **anlegbar**: Es gibt eine wirksame Sage-AG, der FA ist offen und die AG fehlt in Sage.
   - **Kürzel ohne Sage-AG**: Es gibt keine `OperationConfig` oder keine wirksame Nummer. Heute bekannt sind z. B. ISO und MK
     (P-AG5 der Mapping-Spec).
   - **in Sage schon vorhanden**: Am FA steht eine AG-Position mit dieser Arbeitsgangnummer (Abgleich siehe Rückfrage 15).
   - **FA beendet/gesperrt**: Der FA ist in Sage beendet oder dort nicht lesbar, siehe Abschnitt „Beendete FAs“.
   - **Sage nicht lesbar**: Der Lesefehler wird sichtbar als Banner gemeldet. Diese Zeilen gelten nicht als „anlegbar“.
4. Spalten: Haupt-FA, FA (BelID), Ebene (Haupt/Unter), Artikelnummer, Matchcode, Kürzel, Position des Kürzels im Feld (1, 2,
   …), Sage-AG-Nummer, Status, in Sage vorhandene AG-Positionen (Anzahl).
5. Der Sage-Stand wird live per SData gelesen und nicht aus `ProductionOrderPositions`. Diese Tabelle wird nur im SData-Modus
   gefüllt (`IdealAkeWms/Models/ProductionOrderPosition.cs:8-9`) und ist auf IDEAL-DEV leer
   ([[2026-11-16-fa-struktur-ideal-ueber-sdata-spec]]).
6. Gelesen wird über `SageSdataReadClient.QueryAllAsync` mit `where` **und** `orderBy`
   (`IdealAkeWms/Services/SageSdata/SageSdataReadClient.cs:182-188`, sonst HTTP 500). Die BelIDs gehen in Blöcken über
   `OrBloecke` (`IdealAkeWms/Services/SageSdata/SageSdataFaModels.cs:96-97`; nur int-Werte, nie Freitext).
7. **Last:** Der Sage-Stand wird nur für die FAs der gefilterten Menge gelesen, und das erst auf ausdrücklichen Knopf „Sage-Stand
   prüfen“. Ohne diesen Knopf zeigt die Status-Spalte „nicht geprüft“. Grund: Ein Live-Lesen aller 18.398 AG-Positionen bei jedem
   Seitenaufruf ist unnötig. Die Fachabteilung filtert ohnehin auf Strukturen. Der CSV-Export ruft dieselbe Action mit
   `pageSize=0` auf (`wwwroot/js/csv-export.js:3-4,204`) und trägt den Prüfparameter in der URL mit.
   - Die Kappung `PageSize.AllCap` (5000) bleibt mit sichtbarer Warnung. Bei mehr als 5000 Zeilen muss enger gefiltert werden.

### E1 – Anlage in Sage
1. Der Knopf „Arbeitsgänge in Sage anlegen“ steht **je FA** und ist nur sichtbar und wirksam, wenn der neue Schalter an ist **und**
   der FA mindestens eine Zeile „anlegbar“ hat. Der Server prüft beides am Schreibort erneut (ADR 0026).
2. Der Bestätigungsdialog nennt FA, Artikel und die Liste der Sage-AG-Nummern, die angelegt werden, sowie den Warnhinweis des
   Schalters. Der Knopf wird gegen doppeltes Absenden gesperrt (fallstricke §38).
3. **Vor dem Senden** liest der Server den Ist-Stand des FA in Sage neu: den FA-Status und die vorhandenen AG-Positionen. Er sendet
   nur die AGs, die dann noch fehlen. Diese Prüfung ist der Duplikatschutz gegen Doppelklick, zwei Benutzer und eine Anlage von Hand
   in Sage. Laut DPS prüft der Service selbst nichts (DPS-Anfrage 2026-11-03).
4. Beendete FAs werden nie beschrieben. Ein FA, der in `PpsFaBelegeInfo` fehlt, wird ebenfalls nicht beschrieben. Begonnene FAs
   behandelt Rückfrage 4.
5. Aufruf: `POST $service/FertigungsauftragService` mit `ServiceOption = 4` (AddOrChangePositionArbeitsgang), FA-Handle und je AG
   eine `FaBelegArbeitsgangPosition` mit den Feldern `RessourceTyp`, `RessourceNummer`, `Menge`, `Bearbeiter` und Userfeldern
   (Befund §10, `$schema` IDEAL Prod laut Ideen-Notiz 15:44). Pflichtfelder und Werte sind **nicht belegt** → P-1 bis P-4,
   Rückfragen 1, 11, 12.
6. Ob alle AGs eines FA in einem Aufruf gesendet werden oder je AG ein Aufruf erfolgt, klärt P-1. Bei Teilerfolg (AG 1 angelegt,
   AG 2 abgelehnt) zeigt das Ergebnis je AG den Erfolg oder den Sage-Fehlertext.
7. Fehler:
   - Ein fachlicher Fehler kommt als HTTP 500 oder 400 mit `ResultNumber`/`ResultText`/`ErrorText` (Befund §11/§14). Er erscheint
     als `TempData["WarningMessage"]`. Ein `ErrorMessage` gibt es nicht.
   - Ist die Antwort unklar (Timeout, kein Handle), lautet der Text „unklar – bitte in Sage prüfen“, und es gibt **keine**
     automatische Wiederholung. Dieselbe Regel gilt im Sage-Wareneingang (`IDEALAKEWMSService/Services/SageWareneingangClient.cs:69-104`).
   - Ein erneuter Klick ist erlaubt, weil Schritt 3 vorher den Ist-Stand liest.
8. Protokoll: Je Aufruf wird ein Lauf über `ISyncLogger` geschrieben, mit neuem Service-Namen in `SyncLogServices.All` und
   `DisplayNames` (`IdealAkeWms/Services/SyncLogger/SyncLogServices.cs:56,71`, Muster `SageWareneingang` `:39`).
   - Inhalt: FA, AG-Nummern, Benutzer, Ergebnis je AG, Sage-ResultNumber/Text.
   - `Message` wird auf 1000 Zeichen gekappt. Ein Stacktrace bzw. `ErrorText` gehört nur ins Serilog-Log, ohne Zugangsdaten.
   - Deutsche Counts-Keys: „Angelegt“, „Übersprungen“, „Fehler“ (ADR 0010).

## Technischer Lösungsentwurf

**Empfehlung zu Rückfrage 9: synchron aus der Web-App.**
- Es geht um eine manuelle Aktion je FA mit Bestätigung, und der Benutzer will das Ergebnis sofort als Banner sehen. Eine
  Warteschlange mit Status-Automat („Gesendet“, „Klären“) wäre hier Aufwand ohne Nutzen.
- Die Bausteine liegen schon im Web-Projekt: der Zugang über `SdataZugangsdaten.WebStandAsync` (Web und Dienst teilen diese Stelle,
  `IdealAkeWms/Services/SageSdata/SdataZugangsdaten.cs:24-33,93-112`), der Lese-Client und der `ISyncLogger`.
- Neu ist, dass die Web-App zum ersten Mal per POST an Sage schreibt. Heute tun das nur `SageLagerbuchungClient` (`:45`) und
  `SageWareneingangClient` im Dienst (Grep `PostAsync` im Web-Projekt: kein Treffer).
- Der neue Client ist eine **Kopie** von `SageWareneingangClient`: POST mit Basic-Auth, Auswertung von XML/JSON und Kappung
  (`IDEALAKEWMSService/Services/SageWareneingangClient.cs:33-106`). Ein gemeinsames Projekt gibt es nicht. Die Kopie folgt
  demselben Muster wie `FaArbeitsschrittSymbole` (`FaHierarchyTreeViewModel.cs:256-258`) und wird mit einem `ponytail:`-Kommentar
  markiert.
- Variante Dienst-Queue: nur, wenn Gerald sie bei Rückfrage 9 wählt. Dann gilt das Muster `SageWareneingangService`: Status vor
  dem POST, Lookup vor erneutem Senden, genau eine Instanz (`IDEALAKEWMSService/Services/SageWareneingangService.cs:10-17,57-120`).

**Bausteine**
- **E0:** Controller-Action `Index` nach dem Muster `ProductionOrdersController.Index`: `PageSize.Resolve`, `PaginationState`,
  `_Pagination`, `ColumnFilterHelper.ReadFromQuery`, `data-server-column-filter="true"`, `data-col-key` an jedem `<th>`, eine
  filterbare Tabelle je Seite (§22).
  - Die Filterkarte bietet: Haupt-FA, FA, Status, Kürzel, Sage-AG und „nur anlegbar“.
  - Reine Funktion `BuildVorschau(knoten, configs, sageStand)` als `internal static`, testbar ohne DB und ohne Sage (wie
    `OperationConfigsController.BuildPruefliste`, `:208-235`).
- **Sage-Lesen:** `PpsFaBelegePositionenInfo` mit `where (BelID eq …) and RessourceTyp eq 'AG'` und `orderBy BelPosID`. Die
  Feldnamen sind am AKE-Test belegt (Befund, Lesetest L1/L2). Für die IDEAL prüft P-5 Feldnamen und Werte für `RessourceTyp`, denn
  `ne`/`gt` liefern laut fallstricke „Sage-SData-Lesen“ 0 Zeilen.
  - Der FA-Status kommt aus `PpsFaBelegeInfo`. Dort fehlen beendete Haupt-FAs (fallstricke §40), daher gilt: kein Treffer heißt
    „FA beendet/gesperrt“, nicht anlegbar.
  - `dtsFaBelegsuche` (RealTimeData) wird **nicht** genutzt, weil DPS sie nicht freigegeben hat.
- **E1:** POST-Action `Anlegen(int belId, string[] erwarteteAgNummern)`. Der Zielzustand geht mit (ADR 0026). Weicht der Stand am
  Server ab, gibt es eine Warnung „Stand hat sich geändert, Vorschau neu laden“ und keinen POST.
- **Duplikatschutz:** Der Empfehlungsweg ist Rückfrage 8 b, der Lookup vor dem Senden aus Anforderung E1.3. Damit ist **keine**
  Migration nötig, und das Protokoll entsteht über SyncLog.
  - Eine WMS-Kennung im Positions-Userfeld (Wunsch aus dem Auftrag) geht erst, wenn P-3 ein beschreibbares Feld belegt. Laut
    Lesetest L7 ist `FaBelegPositionUserfield` leer, und `PositionProperty1–4` haben nur Länge 1.
  - Wird Variante a/c gewählt und ist das Feld da, wird beim Anlegen eine Kennung `<Sync:SageSystemKuerzel>#AG#<BelID>` gesetzt.
    Das Kürzel-Muster folgt fallstricke §31 und `ServiceSettingDefinitions.cs:79-81`. In dem Fall ist ein `GenericUserfield` die
    Alternative (`DatafieldName/Value/Type`, [[sage-sdata-commonwawiservices]]), wenn P-3 es zulässt.
- **Kein Cache** für den Sage-Stand: Er wird je Prüf-Klick frisch gelesen.

## Migrations-/SQL-Auswirkungen

- **Empfehlung: keine Migration.** Der Sende-Status ergibt sich aus Sage selbst (Lookup), und das Protokoll liegt in `SyncLogs`.
- Nur wenn Rückfrage 8 einen WMS-eigenen Sende-Status verlangt, kommt eine neue Tabelle hinzu, z. B. `SageFaArbeitsgangAnlagen`
  (BelID, AG-Nummer, Ergebnis, Sage-Handle, BelPosID) als `AuditableEntity`.
  - Dazu: EF-Migration → `SQL/<Nr>_<Name>.sql` mit `OBJECT_ID`-Guard, DDL in eigenem Batch, `__EFMigrationsHistory` in eigenem
    Batch → `SQL/00_FreshInstall.sql` an **beiden** Stellen (ADR 0004).
  - Die SQL-Nummer wird **beim Dev-Start gegen den Worktree geprüft**. Zuletzt belegt war SQL/159.
- Der neue Schalter ist ein ServiceSetting. Sein Default kommt aus `ServiceSettingDefinitions` (DB-first, ADR 0008) und braucht kein
  Seed-SQL, wenn er fehlt. Ob ein Seed nötig ist, prüft der Dev-Lauf am Muster `SageWareneingangBelegAktiv`.
- `SQL/AgentJobs/` ist nicht betroffen.

## Einstellungen

| Key | Typ | Erlaubte Werte | Default | Wirkung | Verwendung | Standort | Warnhinweis |
|---|---|---|---|---|---|---|---|
| `SageFaArbeitsgaengeAnlegenAktiv` (Name beim Dev-Start final) | Bool (ServiceSetting, `/ServiceSettings`, Eintrag in `ServiceSettingDefinitions.All` + Meta, Drift-Guard-Test) | Auswahl Ja/Nein | `false` | Ein: Auf der Vorschau erscheint je FA der Knopf „Arbeitsgänge in Sage anlegen“, der Server nimmt die Anlage an. Aus: Die Vorschau bleibt lesbar, der Knopf fehlt, und ein POST wird am Server mit Warnung abgewiesen. | Web: Vorschau-Seite (E1). Gelesen ohne Cache bei jedem POST. | IDEAL (AKE: aus lassen, dort ohne Wirkung, weil Arbeitsgänge aus OSEON kommen) | „Schreibt Arbeitsgang-Positionen in Sage-Fertigungsaufträge. Einlastung und Kosten in Sage können sich ändern, und angelegte Positionen löscht das WMS nicht. Nur nach erfolgreichem Schreibtest an einem FA am IDEAL-Test einschalten.“ |

- Die Vorlage für Meta und Warntext ist `SageWareneingangBelegAktiv` (`IdealAkeWms/Models/ServiceSettingDefinitions.cs:71-73`). Die
  Definition folgt dem Muster `:271`.
- Die vorhandenen Keys `SData:BaseUrl/Dataset/Username/Password` (`:286-292`) werden nur gelesen und nicht geändert.
  - An der IDEAL war am 2026-11-13 **kein** `SData:BaseUrl` eingetragen (Lesetest L10). Er muss für Test und Prod gesetzt sein,
    sonst zeigt die Vorschau „Sage nicht lesbar“ als Banner. Das ist ein Deploy-Schritt, keine neue Einstellung.

## Audit-Feld-Auswirkungen

- **Ohne Migration (Empfehlung):** Es gibt keine neue Entität und keine Audit-Felder. Die Nachvollziehbarkeit kommt aus dem
  SyncLog-Lauf je Aufruf, der den Benutzer über `ICurrentUserService` enthält (Anmeldename und Windows-Name).
- **Mit Tabelle (Rückfrage 8 b/c):** Die Tabelle erbt `AuditableEntity`, und `CreatedBy`/`ModifiedBy`/`ModifiedByWindows` kommen
  aus `ICurrentUserService` (ADR 0003).
- In Sage setzt der Service `GeaendertAm` **nicht** (Befund §14 D1). Die Änderung ist in Sage also nicht am FA-Kopf sichtbar,
  sondern nur im WMS-Protokoll. Das gehört auch in die Hilfe.

## Rollen / Zugriff

- Vorschlag: `[RequireAdminAccess]` für die ganze Seite, also Vorschau und Anlage. So halten es auch `OperationConfigsController`,
  `SageTestController` und `SageSdataFaController` (`secondbrain/codebase/controller.md`, Z. 55 und 188-192).
- Feature-Gate kumulativ: Die Seite ist nur bei `ProduktionsauftragHierarchisch` sichtbar, wie die FA-Struktur. Für die Anlage
  gilt zusätzlich der neue Schalter (ADR 0011).
- Soll die Fachabteilung lesen oder anlegen, ist das Rückfrage 10. Eine neue Rolle braucht drei Stellen: Attribut,
  `controller.md` und `RoleOverview.cshtml`.

## Akzeptanzkriterien

**E0**
1. Für FA 1059463 (Kürzel „PU ISO VL MK PR PL VP“) zeigt die Vorschau 7 Zeilen. ISO und MK stehen auf „Kürzel ohne Sage-AG“,
   solange keine wirksame Sage-AG gepflegt ist. Die übrigen 5 tragen ihre Nummer aus `/OperationConfigs` (PU 180, VL 260, PR 310,
   PL 320, VP 330).
2. Ohne den Klick auf „Sage-Stand prüfen“ steht die Status-Spalte auf „nicht geprüft“, und es geht keine SData-Anfrage raus.
   Belegt wird das im Serilog-Log.
3. Nach dem Klick stehen FAs ohne AG-Positionen in Sage (z. B. 1065143) auf „anlegbar“, und eine in Sage schon vorhandene
   AG-Nummer steht auf „in Sage schon vorhanden“.
4. Ein beendeter Haupt-FA (z. B. 1059463, laut Befund beendet) steht auf „FA beendet/gesperrt“ und nie auf „anlegbar“.
5. Verschwundene Knoten (`VerschwundenSeit` gesetzt) erscheinen nicht.
6. Jede Spalte ist per Server-Spaltenfilter filterbar. Die Pagination funktioniert, und die Filter bleiben beim Seitenwechsel
   erhalten.
7. Der CSV-Export liefert alle gefilterten Zeilen (bis 5000, sonst mit sichtbarer Warnung), samt Status, wenn geprüft wurde.
8. Ist `SData:BaseUrl` leer oder Sage nicht erreichbar, erscheint ein Banner, kein Absturz. Die Status-Spalte zeigt „Sage nicht
   lesbar“.
9. In `/OperationConfigs` ist für ein Kürzel eine manuelle Sage-AG gesetzt. Die Vorschau nimmt die manuelle Nummer, nach höchstens
   10 Minuten Cache.

**E1**
10. Ist der Schalter aus, gibt es keinen Knopf. Ein direkter POST wird mit WarningMessage abgewiesen, und in Sage passiert nichts.
11. Ist der Schalter an, legt die Bestätigung an einem FA mit „anlegbar“-Zeilen genau diese AGs in Sage an. Danach zeigt
    „Sage-Stand prüfen“ sie als „in Sage schon vorhanden“, und es gibt die SuccessMessage mit der Anzahl.
12. Ein zweiter Klick oder ein zweiter Benutzer auf denselben FA legt nichts doppelt an. Der Lookup vor dem Senden findet die AGs,
    und das Ergebnis lautet „Übersprungen“.
13. An einem beendeten oder in `PpsFaBelegeInfo` fehlenden FA wird nichts gesendet, und eine Warnung erscheint.
14. Lehnt Sage eine AG ab, zeigt die WarningMessage den Sage-ResultText. Erfolgreiche AGs desselben Aufrufs bleiben gemeldet.
15. Je Aufruf entsteht genau ein Eintrag im Aktivitäts-Protokoll mit deutschem Namen, Counts „Angelegt/Übersprungen/Fehler“, FA und
    Benutzer. `Message` hat höchstens 1000 Zeichen.
16. Ein Benutzer ohne Admin-Rolle (bzw. ohne die Rolle aus Rückfrage 10) bekommt auf Vorschau und POST „Zugriff verweigert“.
17. Der Schalter steht in `/ServiceSettings` mit Anzeigename, Wirkung, Verwendung, Standort und Warnhinweis. Der Drift-Guard-Test
    ist grün.

## Test-Szenarien

Outline für `docs/TESTSZENARIEN.md`. Die TS-Nummer wird **beim Dev-Start gegen den Worktree geprüft**, zuletzt belegt war TS-158.

**TS-x.1 Vorschau (E0), IDEAL-DEV/Test**
- Vorbedingung: FA-Struktur ist synchronisiert, `/OperationConfigs` hat Sage-AGs, `SData:BaseUrl` ist gesetzt, die Admin-Rolle
  ist zugewiesen.
- Schritte: Vorschau öffnen, nach Haupt-FA 1059463 filtern, „Sage-Stand prüfen“ klicken, nach Status „anlegbar“ filtern, CSV
  exportieren.
- Erwartet: wie Akzeptanzkriterien 1-7.
- Negativ:
  - `SData:BaseUrl` leer → Banner.
  - Ein Kürzel ohne OperationConfig → „Kürzel ohne Sage-AG“.
  - Ein Benutzer ohne Rolle → kein Zugriff.
  - Ein verschwundener Knoten → nicht sichtbar.

**TS-x.2 Schalter aus (E1)**
- Schritte: Schalter aus, Vorschau öffnen. Danach den POST per Browser-Devtools nachstellen.
- Erwartet: kein Knopf, Warnung, kein Eintrag in Sage (Gegenprobe per P-5).

**TS-x.3 Schreibtest an EINEM FA am IDEAL-Test (E1)**
- **Erst nach Freigabe durch Gerald.**
- Vorbedingung: P-1 bis P-5 sind beantwortet, die Rückfragen 1, 2, 5, 11, 12 sind entschieden, Schalter nur auf dem Test-System an.
  Der FA wird zusammen mit Gerald gewählt: offen, ohne AG-Positionen.
- Schritte: Sage-Stand prüfen, „Arbeitsgänge anlegen“ klicken, Dialog bestätigen, Sage-Stand erneut prüfen und in der Sage-Oberfläche
  am FA nachsehen (Position, Ressource, Einlastung, Kosten).
- Erwartet: Akzeptanzkriterien 11 und 15. Das Ergebnis wird in der Spec festgehalten.
- Negativ:
  - Gleich danach noch einmal klicken → „Übersprungen“, kein Duplikat.
  - Einen beendeten FA wählen → Warnung, kein POST.
  - Eine ungültige Ressource (Testkopie) → Sage-Fehlertext als Banner.

**TS-x.4 Doppelklick/zwei Sitzungen**
- Schritte: Den Knopf zweimal schnell klicken bzw. aus zwei Browsern absenden.
- Erwartet: Die Knopf-Sperre greift. Der zweite Aufruf führt zu „Übersprungen“, in Sage gibt es keine doppelte Position.

**AKE-Regression**
- Auf AKE-DEV ist die Seite bei `ProduktionsauftragHierarchisch` = aus nicht sichtbar, und der Schalter ist ohne Wirkung.

## Etappen

> **Epic:** ein Worktree, ein finaler Merge. Jede Etappe endet mit einem eigenen Commit, gebaut wird im Bündel-Worktree
> `.claude/worktrees/2026-08-07-ideal-teile-1-5` nach dem Sammel-Goal vom 17.11. Den Zweig während der Arbeit über
> `scripts/sync-worktree.ps1` aktuell halten.
> Version, SQL-, TS- und ADR-Nummer werden **beim Dev-Start gegen den Worktree geprüft**. Zuletzt belegt waren v1.99.37, SQL/159,
> TS-158 und ADR 0032.

| # | Etappe | Status | Commit |
|---|---|---|---|
| E0 | Vorschau ohne Schreiben. Hängt nur an P-5 (Feldnamen/Werte `RessourceTyp` IDEAL) und P-6 (Herkunft der Kürzel), beides lesend und ohne Blocker, weil der Code die Werte konfigurierbar bzw. wie geliefert nimmt. Rückfrage 2/3 hat einen Vorschlag als Default (Haupt + Unter-FA, keine Blätter), Build + Tests grün. | erledigt | `825b8f58` |
| E1a | Schalter, Client (Kopie Wareneingang), POST-Action mit Lookup, Protokoll, Banner. Unit-Tests mit gemocktem HTTP. **Hängt an P-1 bis P-4 und an den Rückfragen 1, 8, 9, 11, 12.** Ohne Antwort baut der /goal-Lauf um sie herum (Feldwerte als offene TODOs, Schalter aus) oder trägt sie als Blocker ein. | erledigt (Fake-HTTP, Feldnamen TODO P-1..P-4) | `52a44b71`, Doku `e5ce82da` |
| E1b | **Schranke:** Schreibtest an genau EINEM FA am IDEAL-Test, **erst nach Freigabe durch Gerald** (TS-x.3). Kein Teil eines automatischen Laufs. | blockiert (Freigabe Gerald, Rückfrage 7) | |

## Prüfabfragen (nur lesen: SData GET/`$schema` oder SQL-SELECT, nichts schreiben)

Ziel ist IDEAL, Dataset laut Ideen-Notiz `IDEAL;1` (Prod) bzw. das Test-Dataset aus Rückfrage 7. Basis-URL:
`https://idealsage100tes.ideal.ideal-ake.at:5493/sdata/ol/CommonWawiServices/<Dataset>/`.

- **P-1:** `GET …/$service/FertigungsauftragService/$template` und `GET …/$service/FertigungsauftragService/$schema`.
  - Gesucht: die Pflichtfelder für ServiceOption 4, die Form des FA-Handles (BelID?) und ob mehrere `FaBelegArbeitsgangPosition`
    pro Aufruf erlaubt sind.
- **P-2:** Im selben `$schema` die erlaubten Werte bzw. den Typ von `RessourceTyp` (Zahl wie `DefaultRessourceTyp 1`, oder
  „AG“/Kürzel?) und was `RessourceNummer` bei einem Arbeitsgang erwartet.
  - Gegenprobe lesend: `GET …/PpsFaBelegePositionenInfo?where=RessourceTyp eq 'AG'&orderBy=BelPosID&count=5&format=json` und
    `GET …/PpsFaBelegePositionenArbeitsgangInfo?orderBy=BelAGPosID&count=5&format=json`. Daraus wird abgelesen, wie vorhandene
    AG-Positionen (z. B. FA 1069418) Ressource, ArbeitsgangNummer und `RessourceNummerAg` tragen.
- **P-3:** Gibt es ein beschreibbares Positions-Userfeld für eine WMS-Kennung?
  - `$schema` von `FaBelegPositionUserfield` und `GenericUserfield` prüfen.
  - SQL auf der IDEAL-DB:
    ```sql
    SELECT c.name, t.name AS typ, c.max_length
    FROM sys.columns c JOIN sys.types t ON t.user_type_id = c.user_type_id
    WHERE c.object_id = OBJECT_ID(N'dbo.KHKPpsFaBelegePositionen') AND c.name LIKE N'USER[_]%'
    ORDER BY c.name;
    ```
- **P-4:** Welcher Wert ist für `Bearbeiter` gültig (ValidateMitarbeiter)? Lesend `GET …/PpsMitarbeiterInfo?orderBy=…&count=5`.
  Den Key-Namen vorher per `$schema` bestimmen, weil `orderBy` Pflicht ist.
- **P-5:** Feldnamen und Status-Werte am IDEAL-Ziel. Sie dienen dazu, „beendet“ und „begonnen“ zu erkennen und die
  E0-/E1-Lese-Abfrage festzulegen.
  - Abfrage: `GET …/PpsFaBelegeInfo?where=BelID eq 1065143&orderBy=BelID&format=json` sowie dasselbe für 1059463 (erwartet: kein
    Treffer, fallstricke §40).
  - Für RealTimeData-Felder gilt das Präfix `KHKPpsFaBelege_` (fallstricke §41 letzter Punkt). Ein falscher Name kommt dort still
    leer zurück, bei Resources als HTTP 500.
- **P-6:** Woher liefert die View `Arbeitsschritte` für Unter-FAs, aus dem FA-Feld oder vom Artikel?
  ```sql
  SELECT OBJECT_DEFINITION(OBJECT_ID(N'dbo.[vw_IDEAL-AKE_Kommissionierung_FAListe]'));
  ```
  Die Stelle der Spalte `Arbeitsschritte` suchen, auf der IDEAL-Sage-DB. Den Viewnamen laut `FaHierarchyNode.cs:6` vorher per
  `SELECT name FROM sys.views WHERE name LIKE N'%FAListe%'` bestätigen.
- **P-7:** Welche Ziel-DB nennt die Diagnose `IDEALSQL2\sage100test; DatabaseName IDEAL` wirklich, Test oder Prod? Danach richtet
  sich, ob ein Schreibtest überhaupt das Test-System trifft.
  ```sql
  SELECT @@SERVERNAME, DB_NAME();
  ```
  Am Endpunkt des Test-Datasets prüfen, alternativ `GET …/$schema` je Dataset vergleichen.

Alle Aussagen in dieser Spec zu Pflichtfeldern, `RessourceTyp`-Werten, Userfeldern und Status-Werten der IDEAL sind bis zu P-1 bis
P-7 **unverifiziert**.

## Umsetzungsnachweis E0 + E1a (2026-11-17, Dev-Lauf, Status bleibt InUmsetzung)

Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, Branch `feature/2026-08-07-ideal-teile-1-5`, Basis `1f0361dd` (v1.99.37).
Commits: `825b8f58` (E0 Vorschau), `52a44b71` (E1a Warteschlange + Dienst-Schritt), `e5ce82da` (Version 1.99.38, Hilfe/Changelog,
README, TS-159, docs/betrieb, Nachlauf-Manifest). **Nichts an Sage geschrieben, kein Schalter eingeschaltet, kein Publish/Deploy.**

**Build/Tests (Worktree, `e5ce82da`):**
- `dotnet build IdealAkeWms.slnx` → 0 Fehler, 27 Warnungen (Bestand).
- `dotnet test IdealAkeWms.Tests` → Bestanden, 3248 erfolgreich, 1 übersprungen, gesamt 3249.
- `dotnet test IDEALAKEWMSService.Tests` → Bestanden, 766 erfolgreich, gesamt 766.
- Neu: `SageFaArbeitsgaengeTests`, `SageFaArbeitsgaengeControllerTests` (6), `SageFaArbeitsgangServiceTests` (15, inkl. Client mit Fake-HTTP
  und DI/`ISyncLogger` letzter Parameter); angepasst `NachlaufManifestDriftTests` (SQL/160 in der Deploy-Reihenfolge).

**Was gebaut ist:**
- E0: Seite *Stammdaten → Arbeitsgänge → Sage* (`SageFaArbeitsgaengeController`, nur Admin + `ProduktionsauftragHierarchisch`), je FA
  (Haupt + Unter, keine Blätter) und Kürzel eine Zeile, doppelte Kürzel einmal, Sage-Stand nur auf Knopf (SData-GET), höchstens 500 FAs.
- E1a: Tabelle `SageFaArbeitsgangAnlagen` (SQL/160, UNIQUE BelId+Sage-AG), Knopf „FA … anlegen“ je FA nur bei Schalter an und nach
  „Sage-Stand prüfen“ (anlegbar) bzw. für Fehler-Zeilen; Schreibort prüft Schalter + Ziel-Stand (ADR 0026, Teilmenge der serverseitigen
  Vorschau). Dienst-Schritt im `SageBookingWorker` (`SageFaArbeitsgangService`): liest den FA in Sage vor jedem Senden (vorhanden →
  „in Sage vorhanden“, beendet/begonnen/ohne Menge → nicht senden), POST `$service/FertigungsauftragService` ServiceOption 4 je AG,
  RessourceTyp 1, RessourceNummer = Sage-AG, Menge = Fertigungsmenge, Bearbeiter aus `Sync:SageFaArbeitsgaengeBearbeiter` (leer = nichts
  senden, Fehler). Protokoll `SageFaArbeitsgaenge` mit „Angelegt“/„Übersprungen“/„Fehler“, Fehlermail. Kein ServiceOption 5.
- ServiceSettings (Kategorie „Sage-FA-Arbeitsgänge (IDEAL)“): `SageFaArbeitsgaengeAnlegenAktiv` (aus), `Sync:SageFaArbeitsgaengeBearbeiter`
  (leer), `Sync:SageFaArbeitsgaengeMaxWiederholungen` (3, 0–10), `Sync:SageFaArbeitsgaengeWiederholAbstandMinuten` (15, 5–1440).

**Abweichungen vom Spec-Text (bewusst):**
1. Ein SData-Aufruf **je AG** statt einer Liste je FA – das Ergebnis ist je AG eindeutig (Status je Zeile).
2. Sage-Lesen im Dienst über ein kleines Interface `ISageFaStandLookup` (für Tests austauschbar), nutzt `SdataLeseZielLeser`.
3. Knopf erst nach „Sage-Stand prüfen“ (sonst gibt es keinen „anlegbar“-Status); Fehler-Zeilen sind ohne Prüfung einreihbar.
4. „Antwort unklar“ (Timeout, 2xx ohne Handle, hängendes „Gesendet“) wird über den Text-Präfix `SageFaArbeitsgaenge.UnklarText` markiert
   und nie automatisch wiederholt – keine eigene Statusspalte.
5. Fehlermail je Lauf mit Fehlern (Muster Wareneingang), nicht je Zeile.
6. Hänge-Grenze nutzt den vorhandenen Key `Sync:SageLagerbuchungStuckMinutes` (kein neuer Key).
7. Kein ADR 0033: Schreibweg ist das Wareneingang-Muster (Queue, Lookup vor Senden, unklar nie automatisch); die fachlichen Entscheidungen
   stehen in den Freigabe-Antworten. Dauerwissen als Fallstrick 49.

**Offen (TODO im Code `SageFaArbeitsgangClient`, vor E1b klären):** P-1 Name der Positionsliste/FA-Handle/`PositionHandle 0` und Antwortform
(Erfolg = Handle in der Antwort), P-2 RessourceTyp 1 am IDEAL-Ziel, P-3 WMS-Kennung im Positions-Userfeld (nicht gebaut), P-4 gültiger
Bearbeiter (ValidateMitarbeiter), P-5 Feldnamen/Werte beim Lesen (Ist-Menge/Status) am IDEAL-Ziel. E1b bleibt blockiert (Freigabe Gerald).

**Für den qa-agent:** Payload-Feldnamen gegen `$template`/`$schema` lesen (nur GET), Auswerte-Regel „Erfolg nur mit Handle“, ADR-0026-Prüfung
in `Anlegen`, JavaScript-Kodierung des Bestätigungstexts in der View, TS-159.1–159.16 auf IDEAL-DEV (Bearbeiter leer lassen, dann sendet der
Dienst nachweislich nichts). TS-159.x nur nach Freigabe Gerald.

## Deploy (vorläufig, der QA-Lauf bestätigt am echten Diff)

> [!warning] Etappe 1 schreibt echte Daten in Sage.
> Angelegte AG-Positionen löscht das WMS nicht. Den Schalter auf Prod erst nach dem Schreibtest (E1b) und nach ausdrücklicher
> Freigabe einschalten.

- **Web:** ja (Controller, View, Client, Menü, Hilfe).
- **Service:** nein (Empfehlung Web-synchron). Bei der Variante Dienst-Queue (Rückfrage 9) ja.
- **Migration:** nein (Empfehlung). Mit Rückfrage 8 b/c als Tabelle ja; die Nummer prüft der Dev-Lauf.
- **Vorbedingung IDEAL:** `SData:BaseUrl`, `SData:Dataset`, `SData:Username` und `SData:Password` sind in `/ServiceSettings`
  gesetzt (Lesetest L10: am 13.11. leer).

**Manifest-Einträge `scripts/deploy/nachlauf.psd1` (ADR 0022). Die Version wird beim Dev-Start geprüft.**

| Id | Version | Typ | Firma | Phase | Ausführung | Text |
|---|---|---|---|---|---|---|
| `sage-fa-ag-schalter` | `<Version>` | Einstellung | IDEAL | Nachher | anzeigen | „`SageFaArbeitsgaengeAnlegenAktiv` bleibt AUS. Einschalten erst nach Schreibtest an einem FA am IDEAL-Test und Freigabe Gerald.“ |
| `sage-fa-ag-sdata-zugang` | `<Version>` | Pruefung | IDEAL | Nachher | anzeigen | Prüft per SELECT auf `ServiceSettings`, ob `SData:BaseUrl` gesetzt ist. Bei leer gibt es einen Hinweis „Vorschau zeigt Sage nicht lesbar“, keine Reparatur. |
| `sage-fa-ag-info` | `<Version>` | Info | IDEAL | Nachher | anzeigen | „Neue Seite Arbeitsgänge → Sage (Admin). Vorschau ohne Schreiben. Die Anlage legt AG-Positionen in Sage an, `GeaendertAm` am FA bleibt unverändert.“ |
| *(nur bei Tabelle)* `<Nr>_SageFaArbeitsgangAnlagen` | `<Version>` | Schema | IDEAL | Schema | automatisch | Neue Tabelle mit Sende-Status (Rückfrage 8 b/c), Deploy-Reihenfolge nach Nummer |

**Stand nach Dev-Lauf (v1.99.38, `e5ce82da`):** Web **und Service** (Dienst-Warteschlange, Freigabe-Antwort 9). **Migration ja:**
`SQL/160_AddSageFaArbeitsgangAnlagen.sql` (additiv, eine Tabelle, `MigrationId 20261117192807_AddSageFaArbeitsgangAnlagen`, beide Firmen,
in `00_FreshInstall.sql` an beiden Stellen). Manifest-Einträge eingetragen: `SQL/160` (Schema), `Pruefung/sage-fa-ag-sdata-zugang`
(Schalter an, aber Bearbeiter oder `SData:BaseUrl` leer), `v1.99.38-sage-fa-ag-schalter` (Einstellung, bleibt aus),
`v1.99.38-sage-fa-ag-info` (Info). Kein Daten-Backfill, kein DB-Backup-Sonderfall. Schalter auf keinem System einschalten.

**Publish-Befehle:** wie im Bündel üblich (Web-Publish aus dem Worktree auf das IDEAL-Test-System). Der QA-Lauf trägt die konkreten
Befehle ein.

## Offene Rueckfragen

1. **Ressource:** Welche `RessourceTyp`/`RessourceNummer` sendet das WMS je Arbeitsgang: nur die Arbeitsgangnummer oder
   zusätzlich einen Arbeitsplatz? Wenn ein Arbeitsplatz: woher? Werkbank-Nummern taugen nicht, weil auf IDEAL TEST 26 dreistellige
   Werkbänke Arbeitsgangnummern tragen (ADR 0031, fallstricke §45). Bekannte AG-Info-Werte sind z. B. 3001/2201/2000 als
   `RessourceNummerAg`.
2. **Ebene:** Werden die Arbeitsgänge an jedem Unter-FA angelegt, am Haupt-FA oder an beiden? Der Haupt-FA trägt eigene Kürzel,
   z. B. 1059463 „PU ISO VL MK PR PL VP“.
3. **Blätter** (`SubFA = 0`, Kürzel vom Artikel, kein eigener FA): auslassen (Vorschlag) oder am Vater-FA anlegen?
4. **Begonnene FAs:** Nur FAs ohne jede Rückmeldung beschreiben, oder auch begonnene? Woran gilt ein FA als begonnen (Status-Wert,
   Ist-Mengen, siehe P-5)?
5. **Reihenfolge:** Der Request hat kein Positions- oder Sortierfeld (Ideen-Notiz 15:44). Reicht die Reihenfolge der Kürzel im
   Feld, wobei Sage die Position beim Anlegen vergibt, oder ist eine bestimmte Positionsnummer Pflicht?
6. **Einlastung und Kosten:** Welche Wirkung soll die Anlage in Sage haben (Kapazität, Kalkulation, Nachkalkulation)? Muss nach
   der Anlage eingelastet werden (ServiceOption 5), oder bewusst nicht?
7. **Bisheriger IDEAL-Test-Schreibweg:** Die Notiz beschreibt den Fehler nicht. Sie sagt nur „Schreibtest nur am IDEAL-Test
   (derzeit Fehler)“ (Ideen-Notiz Z. 129) und „Test `idealsage2…` hat laut Gerald noch einen Fehler“ (Lesetest L10). Was genau
   schlug fehl, und welcher Endpunkt bzw. welches Dataset ist das IDEAL-Test-Ziel für den Schreibtest
   (`IDEAL_TEST_2025_05_06;1` oder `IDEAL_TEST_2026_03_05;1`)?
8. **Duplikatschutz:** Ein Positions-Userfeld für eine WMS-Kennung gibt es laut L7 nicht. Zur Wahl stehen:
   - a) ein Benutzerfeld in Sage anlegen lassen (DPS/Sage-Admin),
   - b) ein WMS-eigener Sende-Status je FA+AG plus Lookup vor jedem Senden (Empfehlung: nur Lookup und SyncLog, ohne Tabelle),
   - c) beides?
9. **Schreibweg:** synchron aus der Web-App (Empfehlung) oder über eine Dienst-Warteschlange wie der Sage-Wareneingang?
10. **Rolle:** Reicht `[RequireAdminAccess]` für Vorschau und Anlage? Oder soll die Fachabteilung die Vorschau lesen bzw. anlegen
    dürfen? Dann braucht es eine neue Rolle.
11. **Menge:** Welcher Wert geht in `FaBelegArbeitsgangPosition.Menge`: Sollmenge des FA, Fertigungsmenge, 1 oder leer?
12. **Bearbeiter:** Was steht im Feld `Bearbeiter`: leer, ein fester Sage-Mitarbeiter oder der angemeldete Benutzer? Am AKE-Test
    scheiterte ein Aufruf an `ValidateMitarbeiter` (Befund §11).
13. **Doppelte Kürzel** im selben Knoten (z. B. „PR PR“): einmal oder zweimal anlegen?
14. **Ausschlussliste:** Gilt `Sync:ProductionWorkplaceSyncAusschlussliste` auch hier? Oder entscheidet nur die Sage-Zuordnung in
    `/OperationConfigs` (die Prüfliste wertet ohne Ausschlussliste aus, `OperationConfigsController.cs:204`)?
15. **Vergleich „in Sage schon vorhanden“:** Genügt der Abgleich über die Sage-Arbeitsgangnummer je FA, auch für manuell in Sage
    angelegte AGs? Und wie werden AGs gezeigt, die in Sage stehen, aber nicht im Kürzel-Feld (Vorschlag: nur als Anzahl, kein
    Löschen)?

## ANTWORTEN auf Rueckfragen - Fachbereich (aus Jira)

*(noch keine)*

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

1. Ressource (RessourceTyp/-Nummer, Arbeitsplatz ja/nein, woher) → **Gerald 2026-11-17 20:13 „Können wir mal so implementieren, testweise“ (Empfehlung Koordination 20:12): nur der Arbeitsgang (Sage-Arbeitsgangnummer, RessourceTyp = DefaultRessourceTyp 1), kein Arbeitsplatz; Schreibtest bestätigt.**
2. Ebene (Unter-FA / Haupt-FA / beide) → **Gerald 2026-11-17 20:13 „Können wir mal so implementieren, testweise“ (Empfehlung Koordination 20:12): an dem FA, dessen Knoten das Kürzel trägt (Unter-FA für seine Kürzel, Haupt-FA für seine eigenen).**
3. Blätter (auslassen / am Vater-FA) → **Gerald 2026-11-17 20:13 „Können wir mal so implementieren, testweise“ (Empfehlung Koordination 20:12): auslassen, in der Vorschau als Anzahl; Vater-FA-Variante später nach Rücksprache Walter.**
4. Begonnene FAs (ja/nein, Kriterium) → **Gerald 2026-11-17 20:13 „Können wir mal so implementieren, testweise“ (Empfehlung Koordination 20:12): nur nicht begonnene FAs; begonnene (Ist-Menge/Rückmeldung > 0 laut P-5) in der Vorschau sichtbar, gesperrt.**
5. Reihenfolge (Kürzel-Reihenfolge reicht / Positionsnummer Pflicht) → **Gerald 2026-11-17 20:13 „Können wir mal so implementieren, testweise“ (Empfehlung Koordination 20:12): Reihenfolge der Kürzel im Feld genügt; Schreibtest prüft.**
6. Einlastung und Kosten (Wirkung, ServiceOption 5 ja/nein) → **Gerald 2026-11-17 20:13 „Können wir mal so implementieren, testweise“ (Empfehlung Koordination 20:12): vorerst nicht einlasten (kein ServiceOption 5); Wirkung mit Fertigungsplanung klären.**
7. Bisheriger IDEAL-Test-Fehler + Test-Endpunkt/Dataset → **Gerald 2026-11-17 20:13 „Können wir mal so implementieren, testweise“ (Empfehlung Koordination 20:12): Gerald 20:12: Schreib-Ziel `IDEAL_TEST_2026_03_05;1` wie in den Service-Einstellungen eingestellt – dort gibt es noch Probleme (vor dem Schreibtest klären, blockiert den Bau nicht, Schalter aus). **Lesend** darf gegen Produktiv `IDEAL;1` geprüft werden (nur GET, z. B. Vorschau/Abgleich „in Sage vorhanden“).**
8. Duplikatschutz (a / b / c) → **Gerald 2026-11-17 20:13 „Können wir mal so implementieren, testweise“ (Empfehlung Koordination 20:12): Warteschlange mit Status je FA+Arbeitsgang (neue Tabelle, Migration) plus Lookup in Sage vor jedem Senden; kein Sage-Benutzerfeld.**
9. Schreibweg (Web synchron / Dienst-Queue) → **Gerald 2026-11-17 20:13 „Können wir mal so implementieren, testweise“ (Empfehlung Koordination 20:12): Dienst-Warteschlange nach Muster Sage-Wareneingang (Wiederholungen, Abstand, Fehlermail); Web-App stellt nur ein und zeigt den Status (Gerald 20:11 „nicht über den Service?“).**
10. Rolle (Admin / neue Rolle, welche Rechte) → **Gerald 2026-11-17 20:13 „Können wir mal so implementieren, testweise“ (Empfehlung Koordination 20:12): nur Admin für Vorschau und Anlage.**
11. Menge → **Gerald 2026-11-17 20:13 „Können wir mal so implementieren, testweise“ (Empfehlung Koordination 20:12): Fertigungsmenge (Sollmenge) des FA; Schreibtest bestätigt.**
12. Bearbeiter → **Gerald 2026-11-17 20:13 „Können wir mal so implementieren, testweise“ (Empfehlung Koordination 20:12): neue ServiceSetting „Sage-Bearbeiter für AG-Anlage“ (Default leer), liest der Dienst.**
13. Doppelte Kürzel (einmal / mehrfach) → **Gerald 2026-11-17 20:13 „Können wir mal so implementieren, testweise“ (Empfehlung Koordination 20:12): einmal anlegen, Hinweis in der Vorschau.**
14. Ausschlussliste (gilt / gilt nicht) → **Gerald 2026-11-17 20:13 „Können wir mal so implementieren, testweise“ (Empfehlung Koordination 20:12): gilt nicht; nur die Sage-Zuordnung (/OperationConfigs) entscheidet.**
15. Vergleich „in Sage schon vorhanden“ + Anzeige fremder AGs → **Gerald 2026-11-17 20:13 „Können wir mal so implementieren, testweise“ (Empfehlung Koordination 20:12): Abgleich über Sage-Arbeitsgangnummer je FA; AGs nur in Sage als Anzahl, nie löschen.**

## Entscheidung Gerald 2026-11-17 19:52 (Umfang)
Beide Etappen werden gebaut: E0 Vorschau und E1 Anlage; E1 hinter neuem Schalter, **Default aus**. Schreibtest an genau einem FA erst nach Freigabe Gerald.

## Deploy (final, QA 2026-11-17 am echten Diff `1f0361dd..ab75c336`)

- **Web:** ja (`IdealAkeWms/`: Controller, View, `SageFaArbeitsgaenge`, Repository, Menü, Hilfe, Settings-Katalog).
- **Service:** ja (`IDEALAKEWMSService/`: `SageFaArbeitsgangService`, `SageFaArbeitsgangClient`, Schritt im `SageBookingWorker`).
- **Migration:** ja, `20261117192807_AddSageFaArbeitsgangAnlagen` = `SQL/160_AddSageFaArbeitsgangAnlagen.sql` (additiv, eine Tabelle,
  idempotent, in `00_FreshInstall.sql` an beiden Stellen, im Manifest). Kein Daten-Skript, kein DB-Backup-Sonderfall.
- Reihenfolge: Dienst stoppen → Web + Dienst kopieren → SQL/160 (bzw. Migration beim ersten Request) → Dienst starten. Der Schalter
  `SageFaArbeitsgaengeAnlegenAktiv` bleibt auf **jedem** System AUS (Manifest `v1.99.38-sage-fa-ag-schalter`).
- **Publish:** vom QA-Lauf **nicht** erstellt (Auftrag Gerald 2026-11-17: kein Publish, macht Gerald später). Gerald publiziert aus dem
  Worktree beide Komponenten:
  `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb` und
  `dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService`, dann `SQL\` nach `publish\SQL\`,
  `scripts\deploy\` nach `publish\deploy\`, `publish-info.txt` (Version 1.99.38, Commit HEAD).
- Nach dem Merge nur dann neu aus `main` publizieren, wenn der Merge getestete Dateien mit parallelen main-Änderungen kombiniert hat.

## QA-Nachweis E0 + E1a (qa-agent, 2026-11-17)

Umfang: E0 + E1a, Commit-Bereich `1f0361dd..e5ce82da` + QA-Fix `ab75c336`. E1b (Schreibtest) ist nicht Teil dieses Laufs und bleibt blockiert.
Grenzen eingehalten: nichts an Sage geschrieben (nur GET), Schalter auf keinem System an, keine Einstellung auf Test/Prod, kein Publish.

### 1. Build und Tests (Worktree, HEAD `ab75c336`)
- `dotnet build IdealAkeWms.slnx`: 0 Fehler, 29 Warnungen (vorbestehend).
- `IdealAkeWms.Tests`: 3249 bestanden, 1 übersprungen (`ProductionOrderEagerCreateAgentJobTests`, vorbestehend), 0 Fehler, gesamt 3250.
- `IDEALAKEWMSService.Tests`: 766 bestanden, 0 übersprungen, 0 Fehler.
- Vor dem QA-Fix: Web 3248/1/0, Service 766/0/0.

### 2. Code-Review (selbst, in diesem Lauf)
Geprüft: Controller (Index, Anlegen, ADR-0026-Prüfung), `SageFaArbeitsgaenge` (Lesen/Auswerten/Vorschau/QueueStatus), View (Confirm-Text,
`hidden`/`d-*`, Knopf-Sperre), Repository (`EinreihenAsync`, App-Guard zum UNIQUE-Index), Modell + Migration + SQL/160 + FreshInstall,
`SageFaArbeitsgangService`, `SageFaArbeitsgangClient` (Payload, Auswertung), `SageBookingWorker` (übrige Schritte unverändert, neuer
Schritt im eigenen try, früher Ausstieg bei Schalter aus), `ServiceSettingDefinitions`, `SyncLogServices`, Manifest, Tests.

| # | Schwere | Befund | Behandlung |
|---|---|---|---|
| F1 | Mittel | FA-Filter + „Sage-Stand prüfen": Gelesen wurde nur der gefilterte FA. Seine Unter-FAs (Knotenfilter `VaterFA == fa`) standen ohne Lesefehler und ohne Banner auf „Sage nicht lesbar". | **Behoben** `ab75c336` (alle SubFAs der angezeigten Knoten lesen) + Test `Index_FaFilter_Pruefen_LiestAuchDieUnterFas` (ohne Fix rot). |
| F2 | Mittel | Der Name der Positionsliste `FaBelegArbeitsgangPositionen` widerspricht dem `$schema` (Lesetest L7: `PositionenArbeitsgang[]`). | **Behoben** `ab75c336`: `PositionenArbeitsgang`. Die Bestätigung am IDEAL-Ziel bleibt P-1. |
| F3 | Mittel (blockiert nur E1b/Einschalten) | Die Erfolgsregel „Handle in der Antwort" ist schwach. Der Request schickt selbst `Handle` = BelID, und die Antwort trägt laut L7 den FA-Handle. Eine 2xx-Antwort ohne ErrorText, die die AG nicht anlegt, gälte deshalb als „angelegt". Weil der Queue-Status in der Vorschau vor dem Live-Stand gewinnt, würde das nie korrigiert. | **Offen**, bewusst nicht spekulativ umgebaut. Vor E1b die Regel an das Positionsergebnis binden (neue `BelPosID`/`PositionHandle` der AG bzw. Nachlesen der AG am FA), mit der echten Antwort aus dem Schreibtest. |
| H1 | Niedrig | Das oberste `Handle`-Feld im Request steht evtl. nicht im Schema. `RessourceTyp` wird als Zahl 1 gesendet, gelesen kommt der Text `AG`. | P-1/P-2, vor E1b per `$template` prüfen. |
| H2 | Niedrig | Bei Schalter aus bleiben schon eingereihte Zeilen `Offen`. Nach dem Einschalten würden sie gesendet (mit Lookup). | Hinweis für das Einschalten, kein Fix. |
| H3 | Niedrig | Das DEV-Sicherheits-SQL (`dev-deploy.ps1`) setzt den neuen Schalter nicht explizit aus (Default false, in beiden DEV-DBs `false` geprüft). | Hinweis. Das Skript liegt außerhalb des Repos. |
| H4 | Niedrig | TS-159.9–159.11 verlangen den Schalter auf DEV. Das widerspricht der Grenze Gerald; die Schritte bleiben für E1b. | Hinweis. |
| H5 | Niedrig, Zusatzfund (nicht dieser Zweig) | Eine Seite jenseits des Endes (`page=2`, 8 Treffer) zeigt „Eintraege 26–8 von 8" – gemeinsames `_Pagination`/`PageSize`-Verhalten. | Nur gemeldet. |

Abweichungen des Umsetzers bewertet, alle akzeptiert:
- Ein SData-Aufruf je AG: liefert ein eindeutiges Ergebnis je AG.
- `ISageFaStandLookup` im Dienst: der Lookup läuft vor jedem Senden, durch Tests belegt.
- Knopf erst nach „Sage-Stand prüfen": sonst „nicht geprüft" = nicht anlegbar, konsistent mit AC2.
- „unklar" als Text-Präfix ohne eigene Spalte: wird nie automatisch wiederholt (Test `Unklar_UndHaengend_NieAutomatischWiederholt`).
- Eine Fehler-Mail je Lauf.
- Hänge-Grenze über `Sync:SageLagerbuchungStuckMinutes`: wiederverwendet, kein neuer Key.
- Kein ADR 0033, nur Fallstrick 49: Muster Wareneingang, akzeptabel.
- TODOs P-1..P-5 im Client: Schalter aus, E1b blockiert.

Spezialprüfungen:
- Confirm-Text per `JavaScriptEncoder.Default.Encode` im `onsubmit` (Anführungszeichen/`<>&` als `\uXXXX`): sicher.
- Kein `hidden` zusammen mit `d-*`.
- Keine stillen `?? 0`-Fallbacks im Schreibpfad: eine fehlende Menge ergibt einen Fehler und zählt als „übersprungen".
- Der Server prüft Schalter und Ziel-Stand am Schreibort (ADR 0026), und der Dienst liest Sage unmittelbar vor jedem Senden.
- Keine Regel, die in mehreren Sprachen umgesetzt ist (Status nur in C#).

**P-5 am IDEAL-Test gelesen (GET, `PpsFaBelegePositionenInfo` BelID 1069418):**
- Die AG-Position Pos. 7 hat `RessourceTyp = "AG"`, `RessourceNummer = "401"` (Montage), `ArbeitsgangNummer` leer, `RessourceTypAg = 1` und `RessourceNummerAg = "3001"` (Arbeitsplatz).
- Die Sage-AG-Nummer steht an AG-Positionen also in `RessourceNummer`. Der Vergleich im Code (Filter `RessourceTyp` „AG", Treffer auf `ArbeitsgangNummer` **oder** `RessourceNummer`) passt dazu.
- `PpsFaBelegeInfo` liefert `IstBegonnen`/`IstBeendet`/`IstGesperrt`/`Aktiv`/`Fertigungmenge` so, wie der Code sie liest.

### 3. Nachweisart je AC
| AC | Nachweis |
|---|---|
| 1 | Test `Ak1_SiebenZeilen_…` + DEV IDEAL (mit gesetzten Test-Nummern) OK |
| 2 | Test (Strict-Mock ohne Sage) + DEV-Log: ohne „prüfen" keine SData-Zeile |
| 3 | „anlegbar": DEV OK (1065153 u. a.). „in Sage schon vorhanden": nur Test `MitSageStand_Status_JeRegel…`, **Manual-UAT** (kein DEV-FA hatte passende AG-Positionen) |
| 4 | Test + DEV OK (1059463 „FA beendet/gesperrt") |
| 5 | Test `Blaetter_Gezaehlt_Verschwundene_Weg…` |
| 6 | Spaltenfilter-Map + Pagination: DEV OK (82 Zeilen, Seitenwechsel). Filter je Spalte **Manual-UAT** |
| 7 | CSV: **Manual-UAT** (Download nicht ausgelöst) |
| 8 | Test `SageFehler_KeineZeileAnlegbar` + Banner-Zweige im Controller. Banner auf DEV **nicht geprüft** (dafür hätte die SData-Einstellung geändert werden müssen), Manual-UAT |
| 9 | DEV OK (manuelle Nummern in `/OperationConfigs` → Vorschau, nach App-Pool-Recycle) |
| 10 | Test `SchalterAus_NichtsEingereiht_Warnung` + DEV: kein Knopf, grauer Hinweis „ausgeschaltet" |
| 11–14 | Nur Unit-Tests mit Fake-Client (`Senden_InReihenfolge…`, `AgSchonInSage_KeinPost…`, `GesperrterFa_KeinPost…`, `SageFehler_Fehler…`): **Manual-UAT/E1b**, nicht als grün gezählt |
| 15 | Test (Counts, Logname, DI). Echter Protokolleintrag **Manual-UAT/E1b** |
| 16 | Test `NurAdmin` (Attribut). Zugriff mit Nicht-Admin **Manual-UAT** |
| 17 | Drift-Guard grün + DEV `/ServiceSettings`: Anzeigename, Verwendung (Web/Dienst), Wirkung und Warnhinweis sichtbar |

### 4. Geänderte bestehende Tests
- `NachlaufManifestDriftTests.cs`: `"SQL/160"` in der erwarteten Liste ergänzt. Listen-Ergänzung, gewollt (neue Migration). Sonst nur neue Testdateien.

### 5. Checkliste / ADR 0020
- Migration + `SQL/160` mit `OBJECT_ID`-Guard und separatem History-Batch ✔
- `00_FreshInstall.sql` an beiden Stellen ✔
- Audit (`AuditableEntity`, `ModifiedAt/By/ByWindows` beim Wieder-Einreihen und im Dienst) ✔
- Version 1.99.38 in beiden `AppVersion.cs`, Anwender- + Brain-Changelog ✔
- TS-159 in `docs/TESTSZENARIEN.md` + Index Zeile 159 ✔
- Manifest `nachlauf.psd1` (SQL/160, Prüfung, Schalter, Info) ✔
- AgentJobs nicht betroffen ✔

ADR 0020:
- 4 Keys im Katalog mit Anzeigename, Wirkung, Verwendung, Default und Standort.
- Der Schalter ist eine Checkbox mit Warnhinweis.
- Die Zahlen haben die Bereiche 0–10 / 5–1440 und werden serverseitig geprüft.
- Der Bearbeiter ist Freitext. Im WMS gibt es keinen Stammdatenverweis dafür, die Gültigkeit prüft Sage (P-4). Akzeptiert.

### 6. DEV-Deploy und Testlauf
- Die Zielordner wurden vor jedem Deploy geleert; behalten wurden nur `appsettings.DEV.json*`, `App_Data` und `logs`.
- `dev-deploy.ps1 -Standort AKE|IDEAL -Quelle <Worktree>` lief zweimal (vor und nach dem QA-Fix), Exit 0, Footer **v1.99.38** auf :8081 und :8082.
- Migration `20261117192807` ist in beiden DEV-DBs eingetragen, Tabelle + `PK`/`IX_Status`/`UX_BelId_Ag` sind vorhanden.
- Schalter `false`, Bearbeiter leer (beide DEV-DBs, nach dem Test erneut geprüft).

| Schritt | Ergebnis |
|---|---|
| 159.1 Vorschau Haupt-FA 1059463 | OK – 7 Zeilen, ISO/MK „Kürzel ohne Sage-AG", 82 Zeilen gesamt, Pagination |
| 159.2 doppelte Kürzel | nur Test (kein DEV-FA mit Dublette gefunden) |
| 159.3 Blätter | OK – Hinweis „149 Blatt-Position(en)…" |
| 159.4/159.5 Sage-Stand prüfen | OK – nur GET (Web-Log: „Sage-Stand fuer 68 FA(s) lesen", <1 s). 1059463 beendet; 1065153/55/66 u. a. „anlegbar". Der FA-Filter 1059463 zeigt auch die Unter-FAs korrekt (F1-Fix) |
| 159.6 Banner Sage nicht lesbar | NICHT GEPRÜFT (bräuchte eine Änderung an `SData:BaseUrl`) |
| 159.7 >500 FAs | NICHT MÖGLICH (DEV IDEAL hat zu wenige FAs) |
| 159.8 Schalter aus | OK – kein Knopf, grauer Hinweis. Direkter POST nur per Test |
| 159.9–159.12, 159.16 Schalter an / Senden | NICHT MÖGLICH (Grenze Gerald: Schalter auf keinem System an, nichts an Sage schreiben) |
| 159.13 AKE | OK – :8081/SageFaArbeitsgaenge leitet mit Hinweis „nur mit hierarchischen Produktionsaufträgen (IDEAL)" aufs Dashboard |
| 159.14 ServiceSettings | OK – 4 Einträge mit Texten. Dienst-Log und `SyncLogs` ohne Eintrag `SageFaArbeitsgaenge` (Schalter aus) |
| 159.15 SQL/160 erneut | OK – Exit 0, keine Ausgabe, eine History-Zeile |
| 159.x1–x5 | E1b, nicht Teil dieses Laufs |

DEV-Testdaten zurückgesetzt: `OperationConfigs.SageArbeitsgangnummerManuell` für die Ids 28/29/30/33/35 wieder `NULL`, App-Pool `IDEAL_WMS_DEV`
recycelt. `SageFaArbeitsgangAnlagen` ist in beiden DEV-DBs leer.

### 7. Publish
Nicht durchgeführt (Auftrag Gerald), siehe Deploy-Abschnitt.

### Manuelle Checkliste für Gerald (offen)
1. Publish aus dem Worktree (beide Komponenten), dann Deploy auf das IDEAL-Testsystem inkl. SQL/160. Der Schalter bleibt aus.
2. TS-159.1–159.5 auf dem Testsystem mit echten Sage-Nummern in `/OperationConfigs` durchgehen. Einen FA mit vorhandener AG-Position suchen und prüfen, dass er „in Sage schon vorhanden" zeigt (AC3).
3. TS-159.6 Banner (SData leer/unerreichbar), CSV-Export (AC7) und Spaltenfilter je Spalte (AC6) prüfen.
4. Zugriff mit einem Nicht-Admin auf Vorschau und POST prüfen (AC16).
5. Vor E1b: P-1/P-2/P-4 per GET `$template`/`$schema` FertigungsauftragService am IDEAL-Ziel klären (Feldnamen `Handle`, `PositionenArbeitsgang`, `RessourceTyp` Zahl/Text, Bearbeiter).
6. Vor E1b: **F3** lösen, also die Erfolgsregel an die angelegte Position binden (bzw. nach dem Senden nachlesen).
7. E1b nur nach Freigabe: Schreibtest an genau einem FA am IDEAL-Test (TS-159.9–159.12, 159.16, 159.x).
8. Danach Merge (Schranke 2).
