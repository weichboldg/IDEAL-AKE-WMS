---
type: spec
title: "Kommissionierauftrag: Maske umschaltbar Stückliste / Liste, Standard „Liste“ bei aktiven Kommissionierlisten"
slug: 2026-11-26-beschichtung-summe-leitstand-hauptfa-kommissionierauftrag-liste-teil-3-spec
status: Entwurf
created: 2026-11-26
updated: 2026-11-26
source_backlog: "[[2026-11-26-beschichtung-summe-leitstand-hauptfa-kommissionierauftrag-liste]]"
uebersicht: "[[2026-11-26-beschichtung-summe-leitstand-hauptfa-kommissionierauftrag-liste-uebersicht]]"
depends_on: "[[2026-11-26-beschichtung-summe-leitstand-hauptfa-kommissionierauftrag-liste-teil-2-spec]]"
task: ""
jira: ""
worktree: ""
branch: ""
standort: IDEAL
affected_code:
  - IdealAkeWms/Controllers/PickingController.cs (Auftrag Z. 252-258, BomSeiteAsync Z. 260-442, Parameter ansicht)
  - IdealAkeWms/Models/ViewModels/BomViewModels.cs (Ansicht Liste/Stueckliste, KannListe)
  - IdealAkeWms/Views/Picking/Bom.cshtml (Umschalter im Auftragskopf, flache Darstellung, eigener view-key, column-config)
  - IdealAkeWms/Models/ViewModels/ColumnDefinitions.cs (neue ViewConfig KommissionierauftragListe + Lookup Z. ~1024)
  - IdealAkeWms.Tests (Controller Auftrag, ViewKeyDriftGuardTests)
  - IdealAkeWms/Views/Help/* (Kommissionierung), Changelog
  - scripts/deploy/nachlauf.psd1
  - docs/TESTSZENARIEN.md
open_questions:
  - "1 Spaltensatz der Maske „Liste“: Komm.-Ziel, Hauptlagerplatz, Position, Artikelnummer, Matchcode, Bezeichnung, Menge Soll, Menge Ist, Lagerplatz/Quell-Lagerplatz, Abteilung (ausblendbar) - einverstanden, oder fehlt eine Spalte der Kommissionierliste?"
  - "2 Zeilen der Liste: je Auftragszeile (wie Stückliste, nötig für Haken/Ist/Umbuchen) statt summiert je Artikel wie die Kommissionierliste - einverstanden?"
  - "3 Sortierung der Liste: Komm.-Ziel, dann Hauptlagerplatz, dann Position (Laufweg) - einverstanden?"
  - "4 Baugruppen-Zeilen in der flachen Liste: zeigen (mit Kennzeichen „BG“, ohne Einrücken), wenn die Auftragszeile eine ist - einverstanden?"
  - "5 Gewählte Maske merken: nein (Umschalter wirkt per URL, jeder Aufruf startet mit dem Standard) - einverstanden?"
  - "6 Soll die Kommissionierliste (/FaHierarchyKommissionierListen) je HauptFA auf die zugehörigen Kommissionieraufträge verlinken? Empfehlung: nicht in diesem Teil."
epic: false
etappen: []
deploy:
  web: true
  service: false
  migration: false
freigabe_entscheidung: ""
freigabe_von: ""
freigabe_am: ""
---

## Ziel / Nutzen (das Warum)

An der IDEAL arbeiten die Kommissionierer mit der Kommissionierliste (flach, nach Ziel und Lagerplatz), gebucht wird aber über die
Kommissionieraufträge (KO-…), die nur die Stücklisten-Maske (Baum) haben. Wer einen Auftrag wie KO-2026-00004 öffnet, soll zwischen
Maske 1 „Stückliste“ und Maske 2 „Liste“ wechseln können; mit aktiven Kommissionierlisten ist „Liste“ der Standard. Haken,
Soll/Ist, Umbuchung auf den Kommissionierwagen, Drucken/Abschließen und Fotos gelten in beiden Masken.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**

- `/Picking/Auftrag/{id}`: Umschalter „Stückliste | Liste“ im Auftragskopf, Parameter `ansicht=stueckliste|liste`.
- Maske „Liste“: dieselbe Seite, flach, eigene Spaltenauswahl, Sortierung nach Laufweg, alle Bedienelemente der Auftragsseite.
- Standard „Liste“, wenn `FaHierarchyKommissionierlistenAktiv = true` und die Stückliste aus der FA-Struktur kommt.

**Out-of-Scope**

- `/Picking/Bom/{id}` (Stückliste ohne Auftrag), AKE-Ablauf.
- Kommissionierliste `/FaHierarchyKommissionierListen` selbst (bleibt Lese-Ansicht, Rückfrage 6).
- Neue Buchungslogik, neue Endpunkte, neue Rolle, neue Einstellung.
- Auftragsdruck `AuftragDrucken` (v1.97.0, schon flach mit Sortierwahl) bleibt wie er ist.

## Fachliche Anforderungen

### Ist-Stand (belegt; Bündel-Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, AppVersion 1.99.81)

- `Auftrag(int id, string? filterText)` lädt den Auftrag mit Zeilen und ruft `BomSeiteAsync(order, auftrag, filterText)`
  (`Controllers/PickingController.cs:252-258`). `BomSeiteAsync` (`:260-442`) filtert die Stücklisten-Positionen auf die Auftragszeilen
  (Schlüssel `PickingOrderRestmengen.Schluessel(ArticleNumber, BomPosition)`, `:288-293`), füllt `BomItemViewModel` (Kommissionieren,
  Hauptlagerplatz, Ebene, VaterSubFa, SagePosition, Lagerplätze, IstMenge, Quell-Lagerplatz-Vorschlag), sortiert mit
  `NaturalPositionComparer`, setzt `ReadOnly` (Status nicht Offen/InArbeit), `Hierarchical` (Quelle FaHierarchie) und
  `TargetStorageLocations` (Kommissionierwagen) und rendert `View("Bom", vm)`.
- `Views/Picking/Bom.cshtml` ist die eine Maske für Stückliste und Auftrag: Auftragskopf `:47-57`; Tabelle `id="bomTable"`
  `data-view-key="Bom"` (`:191`); Baum über `data-parent-pos`, `style="display:none"` für Kinder und Einrückung
  `TreeLevel*24` (`:248-254`); Haken `.picking-checkbox` (`:260-263`); Ist-Menge `.ist-menge` nur mit Auftrag (`:205-208`, `:307-309`);
  Quell-Lagerplatz `.source-location-select` (`:361`); Umbuchungskarte mit Wagen-Auswahl, Scan, „Gepickte Artikel umbuchen“,
  „Auftrag drucken“, „Auftrag abschließen …“ (`:457-496`); Fotos (`:498-518`, `initPhotoUpload` `:1727-1731`, Fotos hängen am FA);
  Baum-Skript `:975-1007`, `:1235`; Umbuchungs-Skript liest angehakte `.picking-checkbox` und `.ist-menge` (`:1603-1639`);
  Spaltenkatalog `:752-779`.
- Aktionen: `AuftragUmbuchen` (`:444-474`), `AuftragAbschliessen` (`:476-489`), `AuftragDrucken` GET/POST (`:604-613`). Zugriff
  `[RequirePickingAccess]` je Aktion (Klasse ohne Attribut).
- ViewConfig `Bom` (`Models/ViewModels/ColumnDefinitions.cs:241`, `SupportsReorder: true`), Lookup `:1024`.
- Kommissionierliste: `FaHierarchyKommissionierListenController` (`[RequireLagerProcessingAccess]` +
  `[RequireFaHierarchyKommissionierlistenAktiv]`), Spalten `ColumnDefinitions.FaHierarchyKommissionierListen` (`:334-354`): HauptFA,
  Haupt-Artnr., Artnr., Matchcode, Sollmenge, Hauptlagerplatz, Lagerplätze, Kommissionieren (Ziel), Arbeitsbereich, Montage-Abteilung,
  ausgeblendet Artikeltyp/beschichtet/Material. Die Liste summiert Knoten, sie hat keine Auftragszeilen und keine Buchung.
- Einstellung `FaHierarchyKommissionierlistenAktiv` (`AppSettingKeys.cs:68`, Definition `AppSettingDefinitions.cs:258-259`: Bool,
  Default `false`, Gruppe IDEAL, „Schaltet das Kommissionierlisten-Modul ein.“, Standort IDEAL, Seed). Gelesen von
  `RequireFaHierarchyKommissionierlistenAktivAttribute.cs:45`, `StandortSettingsCatalog.cs:60`, `NavigationKatalog.cs:242`.

### Soll

| ID | Anforderung |
|---|---|
| K1 | Auftragskopf der Auftragsseite zeigt einen Umschalter (Bootstrap `btn-group`, zwei Links) „Stückliste“ / „Liste“; aktive Maske hervorgehoben **und** mit `aria-current`. Filtertext bleibt beim Wechsel erhalten. |
| K2 | Standard ohne `ansicht`: „Liste“, wenn `FaHierarchyKommissionierlistenAktiv = true` **und** `Hierarchical` (Quelle FaHierarchie); sonst „Stückliste“. Ungültiger Wert = Standard. |
| K3 | Ist die Kommissionierliste aus oder die Quelle nicht FaHierarchie, gibt es keinen Umschalter; `ansicht=liste` wird ignoriert (AKE unverändert). |
| K4 | Maske „Liste“: gleiche Zeilen wie die Stückliste des Auftrags (je Auftragszeile), flach: kein Einrücken, kein Chevron, keine versteckten Kinder; Baugruppen-Zeilen mit Kennzeichen „BG“ (Text). Sortierung Komm.-Ziel → Hauptlagerplatz → Position (natürliche Sortierung, leere zuletzt). |
| K5 | Spalten der Liste (eigener View-Key `KommissionierauftragListe`, Spaltenauswahl/Umsortieren wie überall): Haken, Komm.-Ziel, Hauptlagerplatz, Position, Artikelnummer, Matchcode, Bezeichnung 1, Menge Soll, Menge Ist, Lagerplatz, Quell-Lagerplatz; ausgeblendet: Bezeichnung 2, Abteilung, Vater-Sub-FA, Sage-Position. |
| K6 | In der Liste funktionieren unverändert: Haken (inkl. „alle“), Ist-Menge, Quell-Lagerplatz, Umbuchung auf Kommissionierwagen (Auswahl + Scan), „Auftrag drucken“, „Auftrag abschließen“, Fotos, Filterfeld, Schreibschutz bei Status ≠ Offen/InArbeit. |
| K7 | Umschalten verliert nichts Gebuchtes (alles liegt in `PickingOrderLine`); nicht gespeicherte Haken/Ist-Eingaben gehen wie bei jedem Seitenwechsel verloren – der Umschalter fragt nach, wenn Haken gesetzt sind (vorhandenes `confirm`-Muster). |

## Technischer Loesungsentwurf

Ponytail-Leiter: keine neue View, kein neuer Controller, kein neues JS-Modul. Eine Maske = dieselbe `Bom.cshtml` mit einem Schalter.

1. **Controller:** `Auftrag(int id, string? filterText, string? ansicht)`. `BomSeiteAsync` bekommt `ansicht`; nach dem Bau von `vm`:
   `vm.KannListe = vm.Hierarchical && await _appSettings.GetBoolAsync(FaHierarchyKommissionierlistenAktiv)` (vorhandenen Lesezugriff
   des Controllers nutzen, sonst `IAppSettingRepository` wie im Attribut `:45`); `vm.Ansicht = KannListe && ansicht != "stueckliste" ? Liste : Stueckliste`.
   Bei `Liste`: `Items` mit `OrderBy(Kommissionieren leer zuletzt).ThenBy(Hauptlagerplatz leer zuletzt).ThenBy(Position, NaturalPositionComparer)`,
   `TreeLevel`/Kindbeziehung für die Darstellung ignorieren.
2. **ViewModel** (`BomViewModels.cs`): `Ansicht` (enum oder bool `IstListe`), `KannListe`. Abteilung je Zeile: nur wenn Rückfrage 1
   sie will – Quelle `KommissionierListenService.AbteilungJeHauptFa` (`Services/KommissionierListenService.cs:94-96`) bzw. der HauptFA
   des Auftrags (eine Abteilung je Auftrag).
3. **View `Bom.cshtml`:** Umschalter im Kopf (`:47-57`). Bei `IstListe`: `data-view-key="KommissionierauftragListe"`, kein
   `data-parent-pos`, kein `display:none`, kein Einrück-Padding, kein Chevron; zweiter `column-config`-Block für die Liste. Die Zellen
   bleiben dieselben Partials/Markups (gleiche Klassen `.picking-checkbox`, `.ist-menge`, `.source-location-select`), damit das
   Umbuchungs- und Druck-Skript unverändert greift. Baum-Skript läuft ohne `data-parent-pos` ins Leere (prüfen, sonst `if`).
4. **ColumnDefinitions:** `ViewConfig KommissionierauftragListe` (`SupportsReorder: true`) + Lookup + `ViewKeyDriftGuardTests`.
5. Frontend: Skill `frontend-design` ausdrücklich aufrufen. Umschalter groß genug für Tablet-Bedienung, Kontrast AA, aktive Maske
   nicht nur über Farbe.

## Migrations-/SQL-Auswirkungen

Keine Migration, kein Schema. Spaltenauswahl je Benutzer liegt in der vorhandenen Präferenz-Tabelle unter dem neuen View-Key (kein
Seed nötig, Default aus `column-config`). `SQL/00_FreshInstall.sql`, `SQL/AgentJobs/` unberührt.

Prüfabfrage (WMS-DB je Standort, nur lesen) – bestimmt, wo der Standard „Liste“ greift:

```sql
SELECT [Key], [Value] FROM dbo.AppSettings
WHERE [Key] IN (N'FaHierarchyKommissionierlistenAktiv', N'LeitstandAktiv');
-- Master hierarchisch steht in den ServiceSettings:
SELECT [Key], [Value] FROM dbo.ServiceSettings WHERE [Key] = N'ProduktionsauftragHierarchisch';
```

Tabellen-/Spaltennamen beim Ausführen gegen `SQL/00_FreshInstall.sql` prüfen (nicht aus dem Namen abgeleitet: AppSettings-Zugriff
über `AppSettingKeys`, ServiceSettings über `ServiceSettingDefinitions`).

## Einstellungen

Keine neue oder geänderte Einstellung. Gelesen wird die vorhandene:

| Key | Typ | Erlaubte Werte | Default | Wirkung | Verwendung | Standort | Warnhinweis |
|---|---|---|---|---|---|---|---|
| `FaHierarchyKommissionierlistenAktiv` (unverändert) | Bool | true/false (Auswahl) | false | zusätzlich: Standard-Maske „Liste“ am Kommissionierauftrag und Umschalter sichtbar | Menü, Kommissionierlisten, **neu** Kommissionierauftrag | IDEAL | keine Außenwirkung |

Die Wirkungsbeschreibung in `AppSettingDefinitions.cs:258-259` („Menü, Kommissionierlisten“) um „Kommissionierauftrag: Standard-Maske Liste“
ergänzen (ADR 0020).

## Audit-Feld-Auswirkungen

Keine neuen Schreibwege. Umbuchen, Abschließen, Fotos schreiben wie bisher über `AuftragUmbuchen`/`AuftragAbschliessen`/Foto-API mit
ihren vorhandenen Audit-Feldern.

## Akzeptanzkriterien

1. IDEAL, Kommissionierlisten an: `/Picking/Auftrag/{id}` öffnet in „Liste“; Umschalter sichtbar, „Liste“ aktiv.
2. Klick „Stückliste“: Baumansicht wie heute (Einrücken, Chevrons); Klick „Liste“ zurück; Filtertext bleibt.
3. Liste flach, sortiert Komm.-Ziel → Hauptlagerplatz → Position; Baugruppen mit „BG“.
4. In der Liste: Zeilen anhaken, Ist-Menge ändern, Wagen wählen, „Gepickte Artikel umbuchen“ → Zeilen Umgebucht, Ergebnis gleich wie aus der Stückliste.
5. „Auftrag drucken“, „Auftrag abschließen“ und Foto hochladen funktionieren aus der Liste.
6. Auftrag im Status Abgeschlossen: Liste schreibgeschützt (keine Haken/Eingaben), wie die Stückliste.
7. Spaltenauswahl der Liste ist unabhängig von der der Stückliste (eigener View-Key).
8. Kommissionierlisten aus (oder AKE): kein Umschalter, `?ansicht=liste` zeigt die Stückliste.
9. Haken gesetzt, Umschalter geklickt → Rückfrage; Abbrechen bleibt auf der Seite.
10. `ViewKeyDriftGuardTests` und Controller-Test (Standard je Einstellung, `ansicht`-Parameter, ungültiger Wert) grün; Build grün.

## Test-Szenarien

Neues Kapitel in `docs/TESTSZENARIEN.md` (Nummer beim Dev-Start prüfen; nach Teil 2), Verweise auf das KO-Auftrags-Kapitel
(Spec [[2026-11-08-kommissionierung-auftraege-kopf-zeilen-spec]]).

- **Vorbedingung:** IDEAL-Testsystem, `FaHierarchyKommissionierlistenAktiv = true`, freigegebener HauptFA mit offenem
  Kommissionierauftrag (≥ 5 Zeilen, verschiedene Ziele/Lagerplätze, eine Baugruppe), aktiver Kommissionierwagen, Benutzer mit Picking-Recht.
- **x.1** Auftrag öffnen → Liste (AK 1), umschalten (AK 2), Sortierung/BG (AK 3).
- **x.2** Umbuchen aus der Liste (AK 4); Drucken, Foto, Abschließen (AK 5).
- **x.3** Abgeschlossenen Auftrag öffnen (AK 6).
- **x.4** Spaltenauswahl in Liste ändern, Stückliste prüfen (AK 7).
- **x.5** Umschalter mit gesetzten Haken (AK 9).
- **Negativ:** Einstellung aus bzw. AKE-Test: kein Umschalter, `?ansicht=liste` wirkungslos (AK 8).

## Etappen (nur bei epic: true)

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | (kein Epic) | – | – |

## Deploy

*Vorläufig, der QA-Lauf bestätigt gegen den echten Diff.*

- **Web-App:** ja
- **Service:** nein
- **Migration:** nein
- **Manifest-Einträge** (`scripts/deploy/nachlauf.psd1`):

| Id | Version | Typ | Firma | Phase | Ausführung | Text |
|---|---|---|---|---|---|---|
| `v<Version>-KoAuftragListe` | (Dev-Lauf) | Info | IDEAL | Nachher | anzeigen | „Kommissionieraufträge öffnen bei aktiven Kommissionierlisten in der Maske „Liste“. Umschalten auf „Stückliste“ oben im Auftrag.“ |

- **Publish-Befehle:** `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb` (im Worktree).

## Offene Rueckfragen

1. **Spaltensatz „Liste“.** **Empfehlung:** sichtbar Haken, Komm.-Ziel, Hauptlagerplatz, Position, Artikelnummer, Matchcode,
   Bezeichnung 1, Menge Soll, Menge Ist, Lagerplatz, Quell-Lagerplatz; ausgeblendet Bezeichnung 2, Abteilung, Vater-Sub-FA,
   Sage-Position. Haupt-Artnr. und HauptFA der Kommissionierliste entfallen (ein Auftrag gehört zu genau einem FA, steht im Kopf).
2. **Zeilen je Auftragszeile statt summiert.** Die Kommissionierliste summiert je Artikel; Haken, Ist-Menge und Umbuchung arbeiten aber
   je `PickingOrderLine`. **Empfehlung:** je Auftragszeile. Summieren hieße, eine Ist-Menge auf mehrere Zeilen zu verteilen – neue
   Buchungslogik.
3. **Sortierung.** **Empfehlung:** Komm.-Ziel → Hauptlagerplatz → Position (Laufweg wie Kommissionierliste); über das Filterfeld
   und die Spaltensortierung weiter änderbar.
4. **Baugruppen.** **Empfehlung:** Baugruppen-Zeilen zeigen, die der Auftrag enthält, mit Kennzeichen „BG“, ohne Einrücken.
5. **Maske merken.** **Empfehlung:** nein; jeder Aufruf startet mit dem Standard, der Wechsel ist ein Klick. Merken je Benutzer
   wäre eine neue Präferenz.
6. **Link aus der Kommissionierliste zu den KO-Aufträgen.** **Empfehlung:** nicht in diesem Teil (eigener Backlog-Eintrag bei Bedarf);
   der Weg in den Auftrag bleibt Leitstand bzw. Kommissionierung → Aufträge.

## ANTWORTEN auf Rueckfragen - Fachbereich (aus Jira)

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
  Bei Varianten-Specs zusaetzlich freigabe_entscheidung im Frontmatter setzen.
-->

1. Spaltensatz der Maske „Liste“ wie Empfehlung? →
2. Zeilen je Auftragszeile (nicht summiert)? →
3. Sortierung Komm.-Ziel → Hauptlagerplatz → Position? →
4. Baugruppen mit „BG“, ohne Einrücken? →
5. Maske nicht merken? →
6. Link Kommissionierliste → KO-Aufträge nicht in diesem Teil? →
