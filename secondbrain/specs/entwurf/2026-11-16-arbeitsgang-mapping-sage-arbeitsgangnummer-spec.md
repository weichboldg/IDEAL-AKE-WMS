---
type: spec
title: Arbeitsgang-Mapping – Sage-Arbeitsgangnummer in den Arbeitsgang-Stammdaten + Prüfliste
slug: 2026-11-16-arbeitsgang-mapping-sage-arbeitsgangnummer-spec
status: Freigegeben
created: 2026-11-16
updated: 2026-11-16
source_backlog: "[[2026-11-16-arbeitsgang-mapping-sage-arbeitsgangnummer]]"
task: ""
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
standort: beide
affected_code:
  - IdealAkeWms/Models/OperationConfig.cs
  - IdealAkeWms/Data/ApplicationDbContext.cs
  - IdealAkeWms/Data/Repositories/OperationConfigRepository.cs
  - IdealAkeWms/Controllers/OperationConfigsController.cs
  - IdealAkeWms/Models/ViewModels/OperationConfigIndexViewModel.cs
  - IdealAkeWms/Models/ViewModels/ColumnDefinitions.cs
  - IdealAkeWms/Views/OperationConfigs/Index.cshtml
  - IDEALAKEWMSService/Services/OperationConfigSyncService.cs
  - IdealAkeWms/Migrations/<neu>_AddOperationConfigSageArbeitsgangnummer.cs
  - SQL/153_AddOperationConfigSageArbeitsgangnummer.sql
  - SQL/00_FreshInstall.sql
  - scripts/deploy/nachlauf.psd1
  - docs/TESTSZENARIEN.md
open_questions:
  - "1. Konflikt Werkbank-Nummer: Die Werkbank-Anlage schreibt bei Quelle = View die View-Spalte Arbeitsgangnummer (140, 150 …) in ProductionWorkplace.SageArbeitsplatznummer; echte Sage-Arbeitsplätze an AG-Positionen sind 4-stellig (3001/2201/2000). Prüfabfragen P-AG1..P-AG5 ausführen. Ist die View-Nummer eine Arbeitsgangnummer, taugt die Werkbank-Nummer NICHT als Arbeitsplatz für die spätere Rückmeldung. Prüfliste (b2) 'Werkbank-Nummer gleicht einer Sage-Arbeitsgangnummer' mitbauen?"
  - "2. Datentyp: string (NVARCHAR(31), wie Sage-Stamm und SageArbeitsplatznummer) statt int? wie im Backlog vorgeschlagen?"
  - "3. Herkunft/Hand-Pflege: Variante A (zwei Spalten SageArbeitsgangnummer = Sage-Wert, SageArbeitsgangnummerManuell = Handwert; wirksam = Hand vor Sage) statt B (eine Spalte + Bool-Flag) oder C (eine Spalte + Quelle-Text)?"
  - "4. SData-Quelle (PpsArbeitsgaengeInfo.USER_PNKZ): in dieser Spec nur View; SData-Leser als eigene Folgeaufgabe?"
  - "5. Prüfliste (c) 'Sage-Arbeitsgänge ohne Kürzel': nur als Zeile im Aktivitäts-Protokoll des Arbeitsgang-Abgleichs (mit Link aus /OperationConfigs) statt eigener Speicherung?"
  - "6. Sage-Wert, wenn das Kürzel in der View steht, aber ohne Nummer: auf leer spiegeln (Empfehlung) oder letzten Wert behalten?"
  - "7. Prüfliste nur bei Master ProduktionsauftragHierarchisch = true anzeigen (AKE ohne Prüfliste)?"
epic: false
etappen: []
deploy:
  web: true
  service: true
  migration: true
freigabe_entscheidung: "Empfehlungen 1–7, Variante A; P-AG1..5 vor Dev-Lauf"
freigabe_von: "Gerald Weichbold"
freigabe_am: "2026-11-16"
---

# Arbeitsgang-Mapping: Kürzel → Sage-Arbeitsgangnummer in den Arbeitsgang-Stammdaten

> [!info] Nummern beim Start prüfen
> Vorschlag, beim Start des Dev-Laufs gegen den Worktree prüfen: **Version 1.99.27**, **SQL/153**, **TS-151**, Migration nach
> `20261114080950_AddPurchaseOrderReceiptAbsendekennung`.
> Bereits reserviert sind: 1.99.24 / SQL/151 / TS-139 / ADR 0027 ([[2026-11-14-sicherheit-anmeldung-und-installer-spec]]),
> 1.99.25 / SQL/152 / TS-140..145 / ADR 0028 ([[2026-11-16-beschichtungslogik-je-standort-spec]]) und
> 1.99.26 / TS-146..150 / ADR 0029 ([[2026-11-16-fa-struktur-ideal-ueber-sdata-spec]]). Die FA-Struktur-Spec braucht keine Migration
> und lässt SQL/153 ausdrücklich frei (dort Abschnitt Migrationen). Im Worktree ist `SQL/150_…` die höchste Datei, und
> `AppVersion.cs:5` steht auf 1.99.23.

## Ziel / Nutzen (das Warum)

Das WMS soll später Arbeitsgänge aus den FA-Userfeldern (`USER_OSArbeitsschritte`, im WMS `FaHierarchyNode.Arbeitsschritte`) an Sage
zurückmelden bzw. als AG-Positionen in Sage-FAs anlegen. Dafür muss es je Kürzel die **Sage-Arbeitsgangnummer** kennen (EG → 140,
KA → 150 …). Heute liest der Arbeitsgang-Abgleich diese Nummer aus der View, verwirft sie aber. Diese Spec

- speichert die Nummer,
- lässt sie von Hand übersteuern,
- macht Lücken sichtbar: Kürzel ohne Sage-Arbeitsgang, Werkbänke ohne Sage-Arbeitsplatz, Sage-Arbeitsgänge ohne Kürzel.

Die Spec ist nur Stammdaten und Anzeige und schreibt **nichts nach Sage**. Sie ist die Grundlage für [[2026-10-30-fa-rueckmeldung-an-sage]].

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**
1. `OperationConfig` bekommt die Sage-Arbeitsgangnummer. Den Wert schreibt der Abgleich, ein Handwert kann ihn übersteuern
   (Variante A, Rückfrage 3). Dazu gehören Migration, `SQL/153` und FreshInstall.
2. `OperationConfigSyncService` füllt den Sage-Wert aus der View-Spalte `Arbeitsgangnummer`. Er überschreibt nie den Handwert und meldet
   Abweichungen zwischen Sage und Hand.
3. In `/OperationConfigs` kommen hinzu: die Listenspalte „Sage-AG“ (mit Spaltenfilter), ein Eingabefeld für den Handwert in der
   Inline-Bearbeitung und ein Banner „Abweichung Sage ↔ Hand“.
4. In `/OperationConfigs` kommt eine Prüfliste als reine Anzeige hinzu:
   - (a) Kürzel aus den FA-Userfeldern ohne Sage-Arbeitsgang;
   - (b) Werkbänke ohne Sage-Arbeitsplatz;
   - (b2) Werkbank-Nummer gleicht einer Sage-Arbeitsgangnummer (abhängig von Rückfrage 1);
   - (c) Sage-Arbeitsgänge ohne Kürzel, als Protokollzeile (Rückfrage 5).

**Out-of-Scope** (als Folgearbeit festhalten, siehe unten)
- Jedes Schreiben nach Sage, also die AG-Anlage im Sage-FA (`FertigungsauftragService`, ServiceOption 4) und
  `PpsRueckmeldungArbeitsplatzService`.
- Der SData-Leser für `PpsArbeitsgaengeInfo.USER_PNKZ` (Rückfrage 4).
- Ein zweites Mapping Kürzel → Arbeitsplatz. Der Arbeitsplatz kommt aus der Werkbank, siehe „Kürzel → Werkbank → Sage-Arbeitsplatz“.
- Korrekturen an der Werkbank-Nummer selbst. Wenn Rückfrage 1 den Verdacht bestätigt, wird das eine eigene Spec.
- Ein Handwert schon im Anlegen-Formular. Bei Bedarf wird der Handwert nach dem Anlegen in der Inline-Bearbeitung gesetzt.

## Fachliche Anforderungen

**FA-1 Speichern.** Je Arbeitsgang-Stammsatz gibt es zwei Werte (Variante A, Rückfrage 3):
- `SageArbeitsgangnummer` ist der Sage-Wert. Nur der Abgleich schreibt ihn.
- `SageArbeitsgangnummerManuell` ist der Handwert. Nur `/OperationConfigs` schreibt ihn.

Wirksam ist `Manuell ?? Sage`. Das bildet eine nicht gemappte Property `WirksameSageArbeitsgangnummer` ab. Beide Werte sind Text
(höchstens 31 Zeichen, getrimmt, leer = NULL). Es wird nie `int.Parse` verwendet, denn „0140“ ≠ „140“ (Rückfrage 2).

**FA-2 Füllen durch den Abgleich** (nur Quelle View, und nur wenn die View das Kürzel liefert, `OperationConfigSyncService.cs:123-131`).
Je Kürzel-Gruppe des Plans (`OperationConfigSyncService.cs:281-287`) gilt:
- **genau eine** unterschiedliche, nicht leere Nummer → Sage-Wert = diese Nummer (nur schreiben, wenn sie sich ändert);
- **keine** Nummer → Sage-Wert = NULL (Spiegel, Rückfrage 6);
- **mehrere** Nummern → Sage-Wert bleibt, dazu die Warnung „Kürzel X: mehrere Sage-Arbeitsgangnummern (…) – Nummer nicht geändert“;
- Nummer länger als 31 → Sage-Wert bleibt, dazu eine Warnung.

Das gilt für **jede** Quelle (`Sage`, `Manual`, `ExternalSystem`), denn der Sage-Wert ist eine eigene Spalte und berührt keine Pflege.
Ausgeschlossene Kürzel (`Sync:ProductionWorkplaceSyncAusschlussliste`, `OperationConfigSyncService.cs:294`) bleiben unberührt. Eine
Neuanlage (`:169-178`) bekommt den Sage-Wert gleich mit. Ein Kürzel, das nicht mehr in der View steht, behält seinen letzten Sage-Wert.
Es wird ohnehin als „nicht in Sage“ gekennzeichnet (`:330-344`).

**FA-3 Handwert nie überschreiben.** Der Abgleich fasst `SageArbeitsgangnummerManuell` nie an, weder beim Anlegen noch beim Ändern.

**FA-4 Abweichung melden.** Eine Abweichung liegt vor, wenn Handwert und Sage-Wert beide gesetzt und verschieden sind (Ordinal nach Trim).
- **Web:** Das Banner „Abweichung Sage ↔ Hand“ in `/OperationConfigs` listet `Kürzel: Sage 140 / Hand 141`, im Muster des
  NotInSage-Banners `Index.cshtml:26-32`. Es zeigt sich unabhängig vom Abgleich-Schalter, denn ohne Sage-Wert gibt es keine Abweichung.
- **Protokoll:** Der Zähler `abweichung_hand` steht in jedem Lauf. Eine Warn-Detailzeile gibt es nur, wenn der Lauf den Sage-Wert eines
  Eintrags mit Handwert ändert und danach eine Abweichung besteht: „Sage-Arbeitsgangnummer geändert, Handwert weicht ab: EG Sage 140 →
  145 / Hand 141“. Es gibt keine Dauerwarnung je Lauf.

**FA-5 Pflege in `/OperationConfigs`.**
- Die Listenspalte „Sage-AG“ zeigt den wirksamen Wert. Ist ein Handwert gesetzt, folgt das Abzeichen „Hand“, bei Abweichung zusätzlich
  „Sage: 140“. Das ist Text plus Abzeichen, nicht nur Farbe.
- Das Inline-Bearbeitungsformular bekommt das Textfeld „Sage-AG (Hand)“ mit maxlength 31.
  - Ist das Feld leer, wird der Handwert gelöscht. Dann wirkt wieder der Sage-Wert.
  - Mehr als 31 Zeichen → `WarningMessage`, nichts gespeichert. Das ist dasselbe Muster wie Anzeigename > 200, `OperationConfigsController.cs:142-146`.

**FA-6 Prüfliste** (reine Anzeige, eigene Karten unter der Tabelle, nur bei Master `ProduktionsauftragHierarchisch` = true, Rückfrage 7):
- **(a) Kürzel ohne Sage-Arbeitsgang.** Ermittelt aus allen FA-Strukturknoten `FaHierarchyNode.Arbeitsschritte`, zerlegt mit dem
  vorhandenen `FaArbeitsschrittSymbole.Tokenize` (`IdealAkeWms/Models/ViewModels/FaHierarchyTreeViewModel.cs:261-264`). Das ist dieselbe
  Trennregel wie im Dienst (`WorkOperationStructureDetectionService.cs:35`, `:112-115`). Ohne die Ausschlussliste.
  - Gelistet wird jedes Token, für das es keinen `OperationConfig` gibt oder dessen wirksame Sage-Arbeitsgangnummer leer ist.
  - Angezeigt werden Kürzel, „nicht in Arbeitsgang-Stammdaten“ bzw. „ohne Sage-AG“ und die Anzahl der Knoten.
  - Erwartet laut Backlog: ISO, MK.
- **(b) Werkbänke ohne Sage-Arbeitsplatz.** Das sind `ProductionWorkplace` mit leerer `SageArbeitsplatznummer`. Angezeigt werden Name,
  Kürzel (`ArbeitsschrittCode`) und BDE aktiv ja/nein.
- **(b2) Werkbank-Nummer = Sage-Arbeitsgangnummer** (nur wenn Rückfrage 1 = ja). Gelistet werden Werkbänke, deren
  `SageArbeitsplatznummer` einer wirksamen Sage-Arbeitsgangnummer irgendeines Arbeitsgangs gleicht. Der Hinweistext lautet: „Nummer ist
  vermutlich eine Arbeitsgangnummer, kein Sage-Arbeitsplatz – vor einer Rückmeldung an Sage klären.“
- **(c) Sage-Arbeitsgänge ohne Kürzel.**
  - Der Abgleich schreibt je Lauf eine Info-Detailzeile „Sage-Arbeitsgänge ohne Kürzel: 110 Schachteln, …“. Das sind die View-Zeilen
    mit leerem AG, die heute nur als `uebersprungen` gezählt werden (`:285`). Dazu kommt der Zähler `ohne_kuerzel`.
  - Die Karte (c) in `/OperationConfigs` enthält nur einen Text und einen Link auf das Aktivitäts-Protokoll, gefiltert auf
    „Arbeitsgang-Abgleich“ (`SyncLogController` ist `[RequireAdminAccess]`, `SyncLogController.cs:11`).
  - Ob die View solche Zeilen überhaupt liefert, ist **ungeprüft** (P-AG2). Liefert sie keine, bleibt (c) leer, bis der SData-Leser
    kommt (Rückfrage 4/5).
- Jede Karte ist eine schlichte Liste mit `<ul>` bzw. `<dl>`, wie „Nicht konfigurierte Kürzel“ in `Index.cshtml:36-57`. Es gibt **keine
  zweite filterbare Tabelle** auf der Seite (fallstricke §22). Eine leere Karte wird ausgeblendet.

## Technischer Lösungsentwurf

### Ist-Stand (belegt)

- **`OperationConfig`** hat `OperationName`, `DisplayName`, `DueDateOffsetDays`, `IsExternalSystemRelevant`, `Source` und
  `SageMissingSince` (`IdealAkeWms/Models/OperationConfig.cs:17-48`). Ein Nummernfeld gibt es nicht.
- **Nummer im Abgleich.** `SageArbeitsplatzSql.Build` liest bei einer View `SELECT Arbeitsgangnummer, Bezeichnung, AG`
  (`IDEALAKEWMSService/Services/SageArbeitsplatzReader.cs:90-93`). Der Leser legt Spalte 0 in das DTO-Feld
  **`Arbeitsplatznummer`** (`:43-47`, DTO `ISageArbeitsplatzReader.cs:8-9`). Die View-Arbeitsgangnummer heißt im Code also
  `row.Arbeitsplatznummer`. Der Abgleich nutzt davon nur `Arbeitsschritt` und `Bezeichnung1` (`OperationConfigSyncService.cs:284`,
  `:316`). Die Nummer geht verloren.
- **SData.** `SdataArbeitsplatzReader` liest `PpsArbeitsplaetzeInfo`, nicht `PpsArbeitsgaengeInfo`
  (`IDEALAKEWMSService/Services/Sdata/SdataArbeitsplatzReader.cs:23`, `:41-42`). Er meldet `ArbeitsschrittGeliefert:false` (`:47`,
  `:53`), und der Abgleich überspringt dann mit Warnung (`OperationConfigSyncService.cs:123-131`). `USER_PNKZ` liest heute kein Code; die
  Suche nach „PNKZ“ im Code findet nichts, nur Brain-Notizen. Unter `Sync:Quelle:Arbeitsplaetze` = SData füllt diese Spec deshalb
  **nichts**, und das ist gewollt (Rückfrage 4).
- **Pflegefelder.** `OperationConfigRepository.UpdateAsync` setzt `IsModified` nur für `EditableProperties` = DisplayName,
  DueDateOffsetDays, IsExternalSystemRelevant und Modified* (`IdealAkeWms/Data/Repositories/OperationConfigRepository.cs:106-124`). Der
  Abgleich schreibt über seinen eigenen, getrackten `_ctx`. `Change()` setzt nur die geänderte Property (`OperationConfigSyncService.cs:189-196`),
  und EF schreibt bei Snapshot-Tracking nur geänderte Spalten.
  - Damit gehören Handwert und Sage-Wert getrennten Schreibern und können sich nicht gegenseitig überschreiben, auch nicht bei
    gleichzeitigem Speichern.
  - Schreibwege für `OperationConfig` außerhalb der Tests (vollständige Suche nach `new OperationConfig` / `OperationConfigs.Add|Update` /
    `_repo.UpdateAsync|AddAsync`):
    - Controller Add `OperationConfigsController.cs:121`;
    - Update `:160`;
    - Abgleich `OperationConfigSyncService.cs:169-179` und `:189-216`;
    - Seed `IdealAkeWms/Program.cs:349`;
    - FreshInstall-Seed `SQL/00_FreshInstall.sql:1636-1662`.
    Keiner davon setzt die neuen Spalten außer den hier geplanten Stellen.
- **Cache.** `CachedOperationConfigRepository` cacht `GetAllAsync` 10 Minuten und invalidiert nur bei Web-Schreibvorgängen
  (`CachedOperationConfigRepository.cs:10`, `:31-39`, `:60-62`). Was der Abgleich im Dienst schreibt, erscheint deshalb in `/OperationConfigs`
  erst nach bis zu 10 Minuten. Das ist das heutige Verhalten, gilt schon für `SageMissingSince` und wird nicht geändert. Es wird aber im
  Testszenario genannt.

### Kürzel → Werkbank → Sage-Arbeitsplatz (Punkt 3 des Auftrags, belegt)

- **Kürzel → Werkbank ist 1:n.** `ProductionWorkplace.ArbeitsschrittCode` (`IdealAkeWms/Models/ProductionWorkplace.cs:38-42`) hat
  **keinen** Unique-Index (`ApplicationDbContext`/FreshInstall: Unique gilt nur für `SageArbeitsplatznummer`, `SQL/00_FreshInstall.sql:376-378`).
  Das ist gewollt: `WerkbankKuerzelFamilie` fasst alle Werkbänke eines Kürzels zusammen
  (`IdealAkeWms/Services/WerkbankKuerzelFamilie.cs:22-39`). `Bevorzugt` wählt deterministisch: zuerst BDE-aktiv, dann die kleinste Id
  (`:42-43`). Der Katalog der Struktur-Erkennung führt je Code eine Liste (`WorkOperationStructureDetectionService.cs:80-90`).
- **Wer schreibt `ArbeitsschrittCode`:**
  - die Werkbank-Anlage, beim Anlegen `ProductionWorkplaceSyncService.cs:160` und bei Abweichung Sage-geführt `:209`;
  - der Controller, Create `ProductionWorkplacesController.cs:119` und Edit `:216-217`, aber nur ohne Sage-Nummer;
  - das Repository `ProductionWorkplaceRepository.cs:24`, `:31-34`, nur ohne Sage-Nummer.
- **Wer schreibt `SageArbeitsplatznummer`:** nur die Werkbank-Anlage beim Insert (`ProductionWorkplaceSyncService.cs:156-165`, Feld
  `:158`). Die vollständige Suche `SageArbeitsplatznummer\s*=` in `*.cs` außerhalb der Tests findet sonst nur das Lese-Mapping in das
  ViewModel `ProductionWorkplacesController.cs:166`. Das Repository schließt die Spalte vom Update aus (`ProductionWorkplaceRepository.cs:23`),
  und `SQL/93` legt nur die Spalte an. Die Nummer ist Unique (gefilterter Index, `ProductionWorkplace.cs:30-36`). Hand-Werkbänke haben
  immer NULL.
- **Für die spätere Rückmeldung** ist die Eindeutigkeit Kürzel → Werkbank **nicht nötig**. Eine BDE-Buchung trägt ihre Werkbank selbst
  (`BdeBooking.ProductionWorkplaceId`, `IdealAkeWms/Models/BdeBooking.cs:12`). Der Arbeitsplatz einer Rückmeldung ist
  `Buchung.Werkbank.SageArbeitsplatznummer`, ohne zweites Mapping. Nur die AG-Anlage ohne Buchung (Folgearbeit) bräuchte Kürzel → Werkbank
  und nähme dann `WerkbankKuerzelFamilie.Bevorzugt`.
- **Konflikt (Rückfrage 1, ungeprüft).** Die Werkbank-Anlage liest dieselbe Quelle wie der Abgleich, wenn
  `Sync:ProductionWorkplaceSyncViewName` auf `[vw_IDEAL_AKE_WMS_Arbeitsgaenge]` steht. Dann landet die View-`Arbeitsgangnummer` als
  `nummer` (`ProductionWorkplaceSyncService.cs:129`) in `SageArbeitsplatznummer` (`:158`). Die Setting-Beschreibung behauptet: „Die
  View liefert trotz ihres Namens ARBEITSPLAETZE“ (`IdealAkeWms/Models/ServiceSettingDefinitions.cs:226`). Dagegen sprechen drei Befunde:
  - die View-Nummern 140/150/320 … sind genau die Sage-**Arbeitsgang**nummern aus `USER_PNKZ` (Backlog-Tabelle);
  - an echten AG-Positionen stehen 4-stellige Arbeitsplätze 3001/2201/2000 ([[2026-10-30-fa-rueckmeldung-an-sage]], Lesetest L10-L12 in
    [[sage-sdata-benutzerfelder-lesetest]]);
  - alte W1-Werkbänke haben 4-stellige Nummern ([[2026-10-19-werkbank-katalog-veraltete-sage-arbeitsplaetze]]).

  Wenn das stimmt, ist „Arbeitsplatz aus der Werkbank“ für View-angelegte Werkbänke **falsch**. Diese Spec ändert daran nichts. Sie macht
  es nur in (b2) sichtbar. Welche Quelle auf IDEAL-Test/Prod eingestellt ist, beantwortet P-AG5.

### Änderungen

1. **Model** `OperationConfig.cs`:
   ```csharp
   [StringLength(31)] public string? SageArbeitsgangnummer { get; set; }          // nur Arbeitsgang-Abgleich
   [StringLength(31)] public string? SageArbeitsgangnummerManuell { get; set; }   // nur /OperationConfigs
   [NotMapped] public string? WirksameSageArbeitsgangnummer => SageArbeitsgangnummerManuell ?? SageArbeitsgangnummer;
   ```
   Kein Index, denn es wird nicht danach gesucht. Die Doc-Kommentare nennen den jeweiligen Schreiber.
2. **Migration** `dotnet ef migrations add AddOperationConfigSageArbeitsgangnummer` ergibt zwei `nvarchar(31) NULL`. Die
   `ApplicationDbContext.cs:991-1003` braucht keine Zusatzkonfiguration (`StringLength` reicht). Das wird geprüft, falls der Kontext dort
   `HasMaxLength` explizit führt, und dann wird es angeglichen.
3. **Repository** `OperationConfigRepository.EditableProperties` (`:106-110`) um `SageArbeitsgangnummerManuell` ergänzen. Der
   Sage-Wert kommt **nicht** in die Liste. Ein Test zeigt: `UpdateAsync` mit geändertem `SageArbeitsgangnummer` schreibt diesen nicht.
4. **Controller** `OperationConfigsController`:
   - `Update(...)` (`:138`) bekommt den Parameter `string? sageArbeitsgangnummerManuell` (Trim, leer → null, > 31 → Warnung, nichts
     speichern).
   - `ColumnMap` (`:48-56`) bekommt `["sageag"] = c => c.WirksameSageArbeitsgangnummer`. Das ist ein Textfilter, also **kein**
     `ColumnFilterTypes`-Eintrag (`ColumnFilterSyntax.cs:31`, Register nur für Zahl/Datum).
   - `Index` (`:58-89`) bekommt:
     - die Abweichungsliste aus `all`;
     - die Prüfliste, nur wenn `HierarchischeStrukturKeys.Master` = true. Der Master wird gelesen wie in `FaCompletionController.cs:80-81`,
       über das vorhandene `_serviceSettings`.
   - Der Ctor bekommt neu `IFaHierarchyNodeRepository` (gecachter Decorator, `Program.cs:111`) und `IProductionWorkplaceRepository`
     (`GetAllOrderedAsync`, `ProductionWorkplaceRepository.cs:38`). Zusätzlich wird die Ausschlussliste über `_serviceSettings` gelesen,
     mit derselben Trenn-/Trim-Regel wie im Dienst (`OperationConfigSyncService.cs:103-105`).
   - Die Prüfliste wird in einer `internal static`-Methode berechnet (rein, testbar, ohne DB), im Muster `OperationConfigSyncService.Plan`.
5. **ViewModel** `OperationConfigIndexViewModel` bekommt `Abweichungen`, `PrueflisteAktiv`, `KuerzelOhneSageAg`,
   `WerkbaenkeOhneArbeitsplatz`, `WerkbankNummerGleichAg` (b2) und `SyncLogLink`.
6. **View** `Index.cshtml`:
   - die neue `<th data-col-key="sageag">Sage-AG</th>` (`:67-72`) und die Zelle mit Abzeichen;
   - das Feld im Inline-Formular (`:85-104`); dieses Formular postet das Feld **immer**, sonst würde ein fehlendes Feld den Handwert löschen;
   - `colspan` 6 → 7 (`:128`);
   - das Banner Abweichung nach `:26-32` und die Prüflisten-Karten nach der Tabelle;
   - der Legenden-Eintrag (`:195-228`).
   - Bei Views ist der Skill `frontend-design` Pflicht. Kontrast AA; Abzeichen mit Text, Farbe ist nie allein der Bedeutungsträger.
7. **ColumnDefinitions** `ViewConfig OperationConfigs` (`ColumnDefinitions.cs:560-573`) bekommt die Spalte `sageag` („Sage-AG“) vor
   `actions`. Damit greift das Zahnrad (`#column-config`).
8. **Dienst** `OperationConfigSyncService`:
   - `ExistingOperationConfig` (`:16`) um `string? SageArbeitsgangnummer, string? SageArbeitsgangnummerManuell` erweitern und den
     Aufruf `:152` anpassen.
   - `OperationConfigSyncPlan` (`:19-31`) bekommt `NummerSetzen: List<(int Id, string? Nummer)>`, Nummern für Neuanlagen (das
     `New`-Tupel wird um `Nummer` erweitert), `OhneKuerzel: List<string>` und `NummerMehrdeutig`.
   - `Plan` (`:277-346`):
     - Zeilen mit leerem AG sammeln. Heute werden sie nur gezählt (`:285`); neu kommen sie mit `Nummer Bezeichnung` in `OhneKuerzel`.
     - Je Gruppe die Nummern bestimmen (FA-2).
     - Für vorhandene Einträge (`:309-314`) `NummerSetzen`, wenn sie sich vom gespeicherten Sage-Wert unterscheidet.
   - Ausführung: `Change(id, e => e.SageArbeitsgangnummer = n)`. Die Insert-Entität (`:169-178`) bekommt `SageArbeitsgangnummer`.
   - Detailzeilen:
     - „Sage-Arbeitsgangnummer gesetzt: EG 140, …“ (info);
     - Abweichungs-Detail (warn, nur Übergang, FA-4);
     - „Sage-Arbeitsgänge ohne Kürzel: …“ (info).
   - Zähler (deutsch, ADR 0010): `nummer_gesetzt`, `nummer_mehrdeutig`, `abweichung_hand`, `ohne_kuerzel`. Die Rückgabe `SyncResult`
     (`:239`) zählt `nummer_gesetzt` bei den geänderten Einträgen mit.
   - Kein neuer Lauf, kein neuer Schalter, kein neuer Ctor-Parameter. `ISyncLogger` bleibt letzter Parameter.

### Ponytail-Leiter (bewusst nicht gebaut)

- Keine eigene Mapping-Tabelle, denn eine Nummer je Kürzel passt in `OperationConfig`.
- Kein Quelle-Enum. Die Herkunft ergibt sich aus „Handwert gesetzt?“.
- Kein SData-Leser.
- Keine Persistenz für (c).
- Keine neue Rolle und keine neue Einstellung.

## Migrations-/SQL-Auswirkungen

- **Migration** `<Zeitstempel>_AddOperationConfigSageArbeitsgangnummer`: zwei nullable Spalten. Additiv und nicht destruktiv; kein Backup
  über das Übliche hinaus.
- **`SQL/153_AddOperationConfigSageArbeitsgangnummer.sql`** (Nummer beim Start prüfen), im Muster `SQL/96`:
  ```sql
  IF COL_LENGTH(N'dbo.OperationConfigs', N'SageArbeitsgangnummer') IS NULL
      ALTER TABLE [dbo].[OperationConfigs] ADD [SageArbeitsgangnummer] NVARCHAR(31) NULL;
  GO
  IF COL_LENGTH(N'dbo.OperationConfigs', N'SageArbeitsgangnummerManuell') IS NULL
      ALTER TABLE [dbo].[OperationConfigs] ADD [SageArbeitsgangnummerManuell] NVARCHAR(31) NULL;
  GO
  IF NOT EXISTS (SELECT 1 FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = N'<MigrationId>')
     AND COL_LENGTH(N'dbo.OperationConfigs', N'SageArbeitsgangnummer') IS NOT NULL
     AND COL_LENGTH(N'dbo.OperationConfigs', N'SageArbeitsgangnummerManuell') IS NOT NULL
      INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'<MigrationId>', N'10.0.2');
  GO
  ```
  Die Tabelle `OperationConfigs` setzt `SQL/95`/`96` voraus, daher `Braucht=@('SQL/96')` im Manifest.
- **`SQL/00_FreshInstall.sql` an zwei Stellen:**
  - die beiden Spalten in `CREATE TABLE [dbo].[OperationConfigs]` (Abschnitt 16b, `:1353-1368`);
  - den `MigrationId`-Insert mit `COL_LENGTH`-Guard nach `:3262-3264`, im Muster `:3183-3185`.
- **`SQL/AgentJobs/`:** nicht betroffen. Die Suche nach `OperationConfig` dort findet nichts.
- **Keine Daten-Migration.** Der erste Abgleich-Lauf nach dem Deploy füllt die Sage-Werte. Handwerte sind anfangs leer.

## Einstellungen

keine. Wiederverwendet und unverändert bleiben:
- `Sync:OperationConfigSyncEnabled`, `Sync:OperationConfigSyncViewName` und `Sync:Quelle:Arbeitsplaetze`
  (`ServiceSettingDefinitions.cs:64-65`, `:109`, `:228-229`);
- `Sync:ProductionWorkplaceSyncAusschlussliste` (`:227`);
- der Master `ProduktionsauftragHierarchisch`, der nur gelesen wird.

Nur der Beschreibungstext von `Sync:OperationConfigSyncEnabled` (`:228`) bekommt den Satz „füllt die Sage-Arbeitsgangnummer
(Handwert in /OperationConfigs hat Vorrang)“. Das ist keine neue Einstellung und ändert keinen Wert. Es wird nichts auf Test oder Prod gesetzt.

## Audit-Feld-Auswirkungen

- Für Web-Update gelten `ModifiedAt/By/ByWindows` wie bisher (`OperationConfigsController.cs:156-158`). Sie stehen bereits in
  `EditableProperties`.
- Der Abgleich setzt beim Schreiben des Sage-Werts `ModifiedAt = now` und `ModifiedBy/ByWindows = ServiceUser` („OperationConfigSync“),
  über das vorhandene `Change()` (`OperationConfigSyncService.cs:194`).
- Eine Neuanlage bekommt `CreatedBy = ServiceUser` wie bisher (`:177`).
- Ohne Wertänderung wird nichts geschrieben, also auch kein Audit-Rauschen je Lauf.

## Rollen / Zugriff

- `/OperationConfigs` bleibt `[RequireAdminAccess]` auf Klassenebene (`OperationConfigsController.cs:18`). Damit liegen auch der
  Handwert und die Prüfliste nur bei Admin.
- Das Aktivitäts-Protokoll hinter dem Link (c) ist ebenfalls `[RequireAdminAccess]` (`SyncLogController.cs:11`).
- Es gibt keine neue Rolle und keinen neuen Filter, daher keine Änderung an `controller.md` oder `RoleOverview`.

## Listen-View-Pattern (ADR 0005)

- Die neue Spalte hängt an der bestehenden Server-Filter-Tabelle: `data-col-key="sageag"`, `ColumnMap`-Eintrag, `ColumnDefinitions`.
- Pagination und Filterkarte bestehen schon (`OperationConfigsController.cs:58-89`).
- Die Prüflisten-Karten sind keine Tabellen: unpaginiert, ungefiltert, eine kurze Liste, nur eine filterbare Tabelle je Seite (§22).
  - Falls (a) sehr lang wird (viele unbekannte Kürzel), wird sie auf die 50 häufigsten gekürzt, mit dem Hinweis „und n weitere“.
  - Das ist eine sichtbare Kürzung, kein stilles Take: `ponytail:` Kappung 50, voller Export erst bei Bedarf.

## Prüfabfragen für den Menschen (nur lesend)

Auf der **Sage-DB IDEAL** (Test, danach Prod):

- **P-AG1** – Definition und Spaltentypen der View:
  ```sql
  SELECT OBJECT_DEFINITION(OBJECT_ID(N'dbo.vw_IDEAL_AKE_WMS_Arbeitsgaenge'));
  SELECT c.name, t.name AS typ, c.max_length FROM sys.columns c JOIN sys.types t ON t.user_type_id = c.user_type_id
  WHERE c.object_id = OBJECT_ID(N'dbo.vw_IDEAL_AKE_WMS_Arbeitsgaenge') ORDER BY c.column_id;
  ```
  Erwartung: Aus welcher Sage-Tabelle stammt `Arbeitsgangnummer`, aus der Arbeitsgang- oder der Arbeitsplatz-Tabelle? Welchen Typ hat
  sie (Rückfrage 2)?
- **P-AG2** – Inhalt, Zeilen ohne Kürzel und Kürzel mit mehreren Nummern:
  ```sql
  SELECT Arbeitsgangnummer, Bezeichnung, AG FROM dbo.vw_IDEAL_AKE_WMS_Arbeitsgaenge ORDER BY Arbeitsgangnummer;
  SELECT LTRIM(RTRIM(AG)) AS AG, COUNT(DISTINCT Arbeitsgangnummer) AS n FROM dbo.vw_IDEAL_AKE_WMS_Arbeitsgaenge
  WHERE LTRIM(RTRIM(ISNULL(AG, ''))) <> '' GROUP BY LTRIM(RTRIM(AG)) HAVING COUNT(DISTINCT Arbeitsgangnummer) > 1;
  ```
  Erwartung: Die Werte stimmen mit der Backlog-Tabelle überein (ST 120 … VP 330). Erscheint 110 „Schachteln“ mit leerem AG? Erscheint
  ja, liefert (c) etwas; erscheint nein, bleibt (c) leer.
- **P-AG3** – Sind die View-Nummern echte Arbeitsplätze?
  ```sql
  SELECT Mandant, Arbeitsplatznummer, Bezeichnung1, USER_ArbeitsSchritt, Aktiv FROM dbo.KHKPpsArbeitsplaetze
  WHERE Arbeitsplatznummer IN (SELECT CAST(Arbeitsgangnummer AS varchar(31)) FROM dbo.vw_IDEAL_AKE_WMS_Arbeitsgaenge)
  ORDER BY Arbeitsplatznummer;
  ```
  (Spaltennamen wie im Leser, `SageArbeitsplatzReader.cs:71-76`.) Keine Treffer → die View-Nummern sind keine Arbeitsplätze.
- **P-AG4** – Gegenprobe Arbeitsgangstamm. Der Tabellenname ist ungeprüft, daher erst suchen:
  ```sql
  SELECT name FROM sys.tables WHERE name LIKE N'KHKPps%Arbeitsg%';
  -- dann z. B.: SELECT TOP 100 * FROM dbo.<gefundene Tabelle> ORDER BY 1;   (Arbeitsgangnummer vs. USER_PNKZ)
  ```

Auf der **WMS-DB IDEAL** (Test, danach Prod):

- **P-AG5** – Welche Quelle nutzt die Werkbank-Anlage, und was steht in den Werkbänken?
  ```sql
  SELECT [Key], [Value] FROM dbo.ServiceSettings WHERE [Key] IN (N'Sync:ProductionWorkplaceSyncViewName', N'Sync:OperationConfigSyncViewName',
    N'Sync:Quelle:Arbeitsplaetze', N'Sync:OperationConfigSyncEnabled', N'Sync:ProductionWorkplaceSyncEnabled');
  SELECT Id, Name, SageArbeitsplatznummer, ArbeitsschrittCode, BdeAktiv, SageMissingSince FROM dbo.ProductionWorkplaces
  ORDER BY ArbeitsschrittCode, SageArbeitsplatznummer;
  ```
  Erwartung bei bestätigtem Verdacht: Werkbänke mit 3-stelligen Nummern 140/150 … neben alten 4-stelligen.

Bis P-AG1..5 ausgeführt sind, gelten als **ungeprüft**:
- „View-Nummer = Sage-Arbeitsgangnummer“;
- „Werkbank-Nummer ist kein Arbeitsplatz“;
- „(c) liefert 110 Schachteln“.

## Akzeptanzkriterien

1. Nach Migration bzw. `SQL/153` hat `dbo.OperationConfigs` die Spalten `SageArbeitsgangnummer` und `SageArbeitsgangnummerManuell`
   (`nvarchar(31) NULL`). Ein zweiter Lauf von `SQL/153` ändert nichts und gibt keinen Fehler. FreshInstall auf leerer DB legt beide
   Spalten und den History-Eintrag an.
2. Abgleich mit View: Ein bestehender Eintrag `EG` ohne Nummer bekommt nach einem Lauf `SageArbeitsgangnummer = '140'`, dazu
   `ModifiedBy = OperationConfigSync`. Der Zähler `nummer_gesetzt` ≥ 1.
3. Ein zweiter Lauf ohne Änderung in Sage schreibt nichts: `ModifiedAt` bleibt, `nummer_gesetzt = 0`.
4. Ist `SageArbeitsgangnummerManuell = '141'` gesetzt, lässt der Abgleich den Handwert unverändert. Er schreibt nur den Sage-Wert. Die
   Liste zeigt `141` mit Abzeichen „Hand“ und „Sage: 140“, und das Banner „Abweichung Sage ↔ Hand“ nennt EG.
5. Ändert sich der Sage-Wert eines Eintrags mit Handwert, schreibt der Lauf genau eine Warn-Detailzeile mit altem und neuem Sage-Wert und
   dem Handwert. Ein Lauf ohne Änderung schreibt keine solche Zeile, nur den Zähler `abweichung_hand`.
6. Liefert die View für ein Kürzel zwei verschiedene Nummern, bleibt der Sage-Wert unverändert. Es gibt eine Warnung „mehrere
   Sage-Arbeitsgangnummern“, und `nummer_mehrdeutig` = 1.
7. Ein neu angelegtes Kürzel bekommt die Nummer gleich bei der Anlage.
8. Ein Kürzel aus der Ausschlussliste bekommt keine Nummer.
9. `Sync:Quelle:Arbeitsplaetze = SData`: Der Abgleich überspringt wie heute mit „Kuerzel nicht geliefert“, und keine Nummer ändert sich.
10. Inline-Bearbeitung: „Sage-AG (Hand)“ = `141` speichert den Handwert und setzt Modified*. Ein leeres Feld löscht den Handwert, danach
    zeigt die Liste wieder den Sage-Wert ohne Abzeichen. 32 Zeichen → `WarningMessage`, nichts gespeichert, auch nicht Anzeigename oder Offset.
11. `UpdateAsync` schreibt `SageArbeitsgangnummer` nie (Repository-Test: Property geändert, gespeichert, neu geladen, der alte Wert steht).
12. Der Spaltenfilter „Sage-AG“ filtert serverseitig auf den wirksamen Wert. Das Zahnrad blendet die Spalte aus und ein.
13. Prüfliste (a) bei Master = true: Kommt ein FA-Knoten mit `Arbeitsschritte = "PU ISO VL MK"` vor, nennt die Karte ISO und MK, aber
    nicht PU und VL, sofern diese einen Eintrag mit Nummer haben. Ausgeschlossene Kürzel erscheinen nicht.
14. Prüfliste (b) nennt jede Werkbank mit leerer `SageArbeitsplatznummer`. (b2), falls freigegeben, nennt jede Werkbank, deren Nummer einer
    wirksamen Sage-Arbeitsgangnummer gleicht.
15. Prüfliste (c): Der Lauf schreibt die Info-Zeile „Sage-Arbeitsgänge ohne Kürzel: …“, wenn die View Zeilen mit leerem AG liefert. Die Karte
    verlinkt auf das Protokoll, gefiltert auf den Arbeitsgang-Abgleich.
16. Master = false (AKE): `/OperationConfigs` zeigt keine Prüflisten-Karten. Spalte, Handwert und Banner funktionieren auch an AKE.
17. Kein Code-Pfad schreibt nach Sage. Der Diff enthält keinen SData-POST/PUT und keinen neuen Sage-Schreibdienst.

## Test-Szenarien

Neues Kapitel **TS-151 „Arbeitsgang-Mapping: Sage-Arbeitsgangnummer“** in `docs/TESTSZENARIEN.md` (Nummer beim Start prüfen), dann
`secondbrain/tests/testszenarien-index.md` nachziehen.

- **Vorbedingungen:** IDEAL-Test, Master `ProduktionsauftragHierarchisch` = true, `Sync:OperationConfigSyncEnabled` = true,
  `Sync:Quelle:Arbeitsplaetze` = View, View-Name gesetzt. P-AG2 wurde ausgeführt, und die Ergebnisliste liegt bereit. Rolle Admin. Nach
  jedem Lauf **bis 10 min warten oder den App-Pool recyceln** (Cache `CachedOperationConfigRepository`, 10 min).
- **TS-151.1 Erstfüllung:** Arbeitsgang-Abgleich laufen lassen → in `/OperationConfigs` stimmt die Spalte „Sage-AG“ für alle Kürzel aus P-AG2
  (EG 140, KA 150 …). Das Protokoll zeigt `nummer_gesetzt` > 0.
- **TS-151.2 Wiederholung:** zweiter Lauf → `nummer_gesetzt = 0`, und `ModifiedAt` ist unverändert (per SQL prüfen).
- **TS-151.3 Handwert:** Bei EG „Sage-AG (Hand)“ = 141 speichern → Liste zeigt 141, „Hand“, „Sage: 140“ und das Banner. Danach einen Lauf
  starten → 141 bleibt, und es gibt keine Warn-Detailzeile.
- **TS-151.4 Handwert löschen:** Feld leeren → Liste zeigt 140 ohne Abzeichen, und das Banner verschwindet.
- **TS-151.5 Prüfliste:**
  - (a) nennt ISO, MK (laut Backlog), sofern FA 1059463 oder ein anderer Knoten sie enthält;
  - (b) nennt die Werkbänke ohne Nummer;
  - (b2) gemäß Rückfrage 1;
  - (c) Link führt ins Protokoll, und die Info-Zeile nennt 110 Schachteln, falls P-AG2 sie zeigt.
- **TS-151.6 Spaltenfilter/Zahnrad:** Filter „14“ auf Sage-AG → nur 140/142. Die Spalte ist ausblendbar.
- **Negativfälle:**
  - **N1** Handwert mit 32 Zeichen → Warnung, nichts gespeichert, Offset unverändert.
  - **N2** `Sync:Quelle:Arbeitsplaetze` = SData → Lauf überspringt, und keine Nummer ändert sich. (Nur auf Test umstellen und
    zurückstellen; keine Einstellung auf Prod.)
  - **N3** Abgleich-Schalter aus → keine Nummern-Änderung. Spalte und Handwert funktionieren weiter.
  - **N4** AKE-Regression (Master false): Seite ohne Prüfliste, keine Fehler, Spalte leer.
  - **N5** Benutzer ohne Admin → `/OperationConfigs` verweigert.
- **Unit-Tests** (xUnit, ohne Sage):
  - `OperationConfigSyncService.Plan`: eine Nummer, keine Nummer, zwei Nummern, ausgeschlossen, Neuanlage mit Nummer, Handwert bleibt,
    Abweichungs-Übergang, leeres AG → `OhneKuerzel`;
  - Repository `UpdateAsync` lässt `SageArbeitsgangnummer` weg;
  - Controller `Update` mit Längen-Guard und leerem Feld → null;
  - Prüflisten-Methode: Tokenize, Ausschluss, (b), (b2).

## Etappen (nur bei epic: true)

entfällt (epic: false).

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | – | – | – |

## Deploy

*Vorläufig. Die QA bestätigt das gegen den echten Diff.*

- **Web-App:** ja (Controller, View, ViewModel, Repository, Model).
- **Service:** ja (`OperationConfigSyncService`, Model über das gemeinsame Projekt).
- **Migration:** ja, additiv. Reihenfolge wie üblich: erst das Schema (`SQL/153`), dann Dienst und Web. Der Dienst darf nicht vor dem
  Schema starten, sonst wirft EF bei den neuen Spalten.
- **Manifest-Einträge** (`scripts/deploy/nachlauf.psd1`):
  - `@{ Id='SQL/153'; Version='1.99.27'; Typ='Schema'; Firma='beide'; Phase='Schema'; Ausfuehrung='automatisch'; Braucht=@('SQL/96');
    Datei='153_AddOperationConfigSageArbeitsgangnummer.sql'; MigrationId='<beim Start>'; Voraussetzung='additiv, zwei Spalten NULL';
    Quelle='<changelog-slug v1.99.27>' }`. Er wird hinter `SQL/152` eingereiht; die Deploy-Reihenfolge ist nicht die Dateisortierung.
  - `@{ Id='v1.99.27-ArbeitsgangNummerPruefen'; Version='1.99.27'; Typ='Info'; Firma='IDEAL'; Phase='Nachher'; Ausfuehrung='anzeigen';
    Text='Nach dem ersten Arbeitsgang-Abgleich /OperationConfigs ansehen: Spalte Sage-AG gegen die Sage-Liste pruefen, Pruefliste
    (Kuerzel ohne Sage-AG, Werkbaenke ohne Sage-Arbeitsplatz) an Produktion/AV geben. Nichts wird nach Sage geschrieben.' }`
  - Kein Pruefung-Eintrag: Der Füllstand hängt vom Abgleich-Lauf ab und ist kein Deploy-Zustand.
- **Publish-Befehle** (im Worktree):
  ```
  dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb
  dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService
  ```

## Folgearbeit (nach Merge als Aufgabe in `secondbrain/aufgaben/` festhalten)

1. SData-Leser `PpsArbeitsgaengeInfo` (`USER_PNKZ`, Arbeitsgangnummer). `orderBy` ist bei `where` Pflicht, und die Feldnamen werden erst
   gegen `$schema` geprüft (fallstricke SData-Abschnitt). Erst dann füllt der Abgleich auch unter SData.
2. Werkbank-Nummer korrigieren, falls P-AG3/P-AG5 den Verdacht aus Rückfrage 1 bestätigen.
3. AG-Positionen im Sage-FA anlegen (`FertigungsauftragService` ServiceOption 4) und `PpsRueckmeldungArbeitsplatzService`. Beides erst
   nach Entscheidung und Schreibtest am IDEAL-Test ([[2026-10-30-fa-rueckmeldung-an-sage]]).
4. Brain: `fallstricke.md`. Die View `vw_IDEAL_AKE_WMS_Arbeitsgaenge` liefert laut Befund Arbeitsgang-, nicht Arbeitsplatznummern; das
   wird nach P-AG1 bestätigt oder verworfen. Dazu die Beschreibung `ServiceSettingDefinitions.cs:226` korrigieren.

## Offene Rueckfragen

1. **Konflikt Werkbank-Nummer.** Die Werkbank-Anlage schreibt bei Quelle = View die View-Spalte `Arbeitsgangnummer` (140, 150 …) in
   `ProductionWorkplace.SageArbeitsplatznummer` (`ProductionWorkplaceSyncService.cs:129`, `:158`). Echte Sage-Arbeitsplätze an
   AG-Positionen sind 4-stellig (3001/2201/2000). Das widerspricht dem Auftrag „Arbeitsplatz aus der Werkbank, kein zweites Mapping“,
   sobald die Werkbänke aus der View stammen. Bitte P-AG1..P-AG5 ausführen. Soll die Prüfliste (b2) „Werkbank-Nummer gleicht einer
   Sage-Arbeitsgangnummer“ mitgebaut werden?
   *Empfehlung:* ja. Sie kostet eine Zeile Vergleich, macht die falsche Annahme sichtbar und wird überflüssig, sobald die Folgearbeit 2
   erledigt ist.
2. **Datentyp.** Der Backlog schlägt `int?` vor.
   *Empfehlung:* `string` NVARCHAR(31), aus drei Gründen:
   - im Sage-Stamm ist die Arbeitsgangnummer string(31) ([[sage-sdata-masterdata]] Zeile 57);
   - das WMS behandelt Sage-Nummern bewusst als Text („nie int.Parse“, wie `SageArbeitsplatznummer`, `ProductionWorkplace.cs:30-36`);
   - der Leser liefert ohnehin `ToString()` (`SageArbeitsplatzReader.cs:43-47`).

   Der Typ der View-Spalte ist laut [[2026-10-14-ideal-sage-views-umstellung-spec]] Q2-3 unbekannt, siehe P-AG1.
3. **Herkunft und Hand-Pflege.** Zur Wahl stehen:
   - **A:** zwei Wertspalten (Sage-Wert und Handwert, wirksam = Hand vor Sage);
   - **B:** eine Spalte plus Bool „manuell“. Der Abgleich überspringt manuelle Einträge, und eine Abweichung ist nur im Protokoll sichtbar,
     weil der Sage-Wert nicht gespeichert wird;
   - **C:** eine Spalte plus Quelle-Text, wie B.

   *Empfehlung:* A. Beide Schreiber haben je eine eigene Spalte, „nie überschreiben“ gilt dadurch schon durch den Bau. Die Abweichung ist
   im Web dauerhaft sichtbar, ohne Zusatzzustand. Und „Handwert löschen“ führt ohne Sonderlogik auf den Sage-Wert zurück.
4. **SData-Quelle.** `USER_PNKZ` steht in `PpsArbeitsgaengeInfo`. Den Endpunkt liest heute kein Code; der Arbeitsplatz-Leser liest
   `PpsArbeitsplaetzeInfo` ohne Kürzel (`SdataArbeitsplatzReader.cs:23`, `:47`).
   *Empfehlung:* In dieser Spec nur die View. Der SData-Leser wird eine eigene Folgeaufgabe, nach dem Abschluss von P-A1 und der
   FA-Struktur-SData-Spec. Unter SData bleibt der Abgleich wie heute übersprungen.
5. **Prüfliste (c) „Sage-Arbeitsgänge ohne Kürzel“.** Das Web hat keine Sage-Verbindung.
   *Empfehlung:* nur als Info-Zeile und Zähler im Aktivitäts-Protokoll des Abgleichs, mit Link aus `/OperationConfigs`. Keine eigene
   Tabelle oder Spalte. Hängt davon ab, ob die View Zeilen mit leerem AG liefert (P-AG2).
6. **Kürzel in der View ohne Nummer.**
   *Empfehlung:* den Sage-Wert auf leer spiegeln. Er bildet dann ehrlich den Sage-Stand ab, und Prüfliste (a) meldet das Kürzel.
   Alternative: den letzten Wert behalten.
7. **Prüfliste nur bei Master `ProduktionsauftragHierarchisch` = true?** An AKE gäbe es ohne den Master keine FA-Strukturknoten, und (b)
   würde alle OSEON-Werkbänke ohne Sage-Nummer nennen. Das wäre reines Rauschen.
   *Empfehlung:* ja. Spalte, Handwert und Banner bleiben an beiden Standorten.

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
-->

1. Konflikt Werkbank-Nummer: P-AG1..5 ausgeführt? (b2) mitbauen? → **Gerald 2026-11-16 16:17 „passt“ (Empfehlung): (b2) mitbauen. P-AG1..5 noch nicht ausgeführt – vor dem Dev-Lauf erheben (Gerald). Echte Arbeitsplatz-Zuordnung je Werkbank = Folgeaufgabe vor jeder Rückmeldung an Sage.**
2. Datentyp string NVARCHAR(31) statt int? → **Gerald 2026-11-16 16:17 „passt“ (Empfehlung): string NVARCHAR(31); P-AG1 bestätigt den View-Typ vor dem Dev-Lauf.**
3. Herkunft/Hand-Pflege: Variante A, B oder C? → **Gerald 2026-11-16 16:17 „passt“ (Empfehlung): A (zwei Spalten, Hand vor Sage).**
4. SData-Leser PpsArbeitsgaengeInfo als Folgeaufgabe, hier nur View? → **Gerald 2026-11-16 16:17 „passt“ (Empfehlung): ja, Folgeaufgabe (orderBy Pflicht, Lesetest L11).**
5. Prüfliste (c) nur als Protokollzeile mit Link? → **Gerald 2026-11-16 16:17 „passt“ (Empfehlung): ja.**
6. Kürzel ohne Nummer in der View: auf leer spiegeln oder letzten Wert behalten? → **Gerald 2026-11-16 16:17 „passt“ (Empfehlung): auf leer spiegeln.**
7. Prüfliste nur bei Master ProduktionsauftragHierarchisch = true? → **Gerald 2026-11-16 16:17 „passt“ (Empfehlung): ja; Spalte, Handwert und Banner an beiden Standorten.**

## Lauf-Vermerk 2026-11-16 (goal-Lauf)

**Blocker, nicht begonnen:** P-AG1..5 (Gerald) sind nicht im Abschnitt Pruefabfragen eingetragen (Freigabe-Antworten 1/2 verlangen sie vor dem Dev-Lauf). Status bleibt Freigegeben. Siehe [[2026-11-16-goal-lauf-beschichtung-fa-sdata-arbeitsgaenge]].
