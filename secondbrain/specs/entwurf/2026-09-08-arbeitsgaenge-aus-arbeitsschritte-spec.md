---
type: spec
title: "IDEAL: FaWorkSteps explizit aus FaHierarchyNode.Arbeitsschritte ableiten (Struktur statt Heuristik)"
slug: 2026-09-08-arbeitsgaenge-aus-arbeitsschritte-spec
status: Freigegeben
created: 2026-09-16
updated: 2026-09-18
source_backlog: "[[2026-09-08-arbeitsgaenge-aus-arbeitsschritte]]"
depends_on: "[[2026-09-08-bom-schnittstellen-bridge-hierarchisch-spec]]"
task: ""
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
affected_code:
  - "IDEALAKEWMSService/Services/FaWorkStepStructureDetectionService.cs (NEU) + IFaWorkStepStructureDetectionService.cs (NEU) — Vorlage 1:1 IDEALAKEWMSService/Services/FaWorkStepDetectionService.cs (verifiziert), aber Quelle FaHierarchyNode.Arbeitsschritte (Leerzeichen-Split) statt CachedBomItems.Bezeichnung1/2.Contains, Matching exakt ueber WorkStep.Code statt SearchString"
  - "IDEALAKEWMSService/Common/IUnknownWorkStepTokenState.cs (NEU) — 1:1 Musterkopie von IDEALAKEWMSService/Common/IUnknownWorkplaceState.cs (verifiziert: HasChanged(IReadOnlyCollection<string>), In-Memory HashSet-Singleton), eigene Instanz fuer unbekannte Arbeitsschritt-Token (nicht dieselbe Singleton-Instanz wie Werkbank, sonst vermischen sich zwei fachlich unabhaengige Meldezustaende)"
  - "IdealAkeWms/Models/FaWorkStep.cs — FaWorkStepSources um Struktur = \"Struktur\" ergaenzen (Zeile 3-7 verifiziert: bisher nur Sync/Manual); Source-Spalte bereits NVARCHAR(20) (Kommentar Zeile 27), \"Struktur\" (8 Zeichen) passt ohne Aenderung"
  - "IdealAkeWms/Models/ServiceSettingDefinitions.All — neuer Key Sync:FaWorkStepStructureDetectionEnabled (Bool, Default false, Kategorie FA-Hierarchie), Zeile analog Sync:FaWorkStepDetectionEnabled (verifiziert Zeile 62 im Worktree)"
  - "IdealAkeWms/Services/SyncLogger/SyncLogServices.cs — neue Konstante FaWorkStepStructureDetection, Eintrag in .All (verifiziert Muster Zeile 19/41/43)"
  - "IDEALAKEWMSService/Workers/SyncWorker.cs — neuer Block NACH dem bestehenden 'FA-Materialisierung'-Block (verifiziert Zeile 357-369 im Worktree), gated auf ProduktionsauftragHierarchisch UND Sync:FaWorkStepStructureDetectionEnabled, ueber RunResilientAsync (Muster identisch zum FA-Materialisierung-Block)"
  - "IDEALAKEWMSService/Program.cs — DI-Registrierung IFaWorkStepStructureDetectionService -> FaWorkStepStructureDetectionService (Scoped) + IUnknownWorkStepTokenState -> UnknownWorkStepTokenState (Singleton), analog bestehender Zeile 71 (IFaWorkStepDetectionService) bzw. der IUnknownWorkplaceState-Registrierung"
  - "docs/TESTSZENARIEN.md (neues Kapitel, TS-76 — naechste freie Nummer im Worktree verifiziert, Stand TS-75)"
  - "secondbrain/tests/testszenarien-index.md"
open_questions: []
beantwortete_rueckfragen:
  - "Definitive Token-Menge gegen die massgebliche Sage-View (nicht die Rohspalten-Naeherung) + fachlicher Abgleich Token -> WorkStep-Code/Name je Token (KA/LS/AV/EG/PG/SW/SÄ/SL/PL/EK/BR/SWL/VI). ERLEDIGT: IDEAL sucht nicht, die View liefert die Arbeitsschritte fertig mit - der SearchString-Abgleich ist der AKE-Mechanismus und hat im IDEAL-Pfad keine Aufgabe. Unbekannte Token werden gemeldet, nicht abgeglichen; AKE-Codes werden ignoriert."
epic: false
etappen: []
deploy:
  web: true
  service: true
  migration: false
freigabe_entscheidung: "Eigener FaWorkStepStructureDetectionService (kein parametrisierter Bestandsservice) - fuenf gemessene Unterschiede, kein Eingriff in den live laufenden AKE-Pfad. Token-Katalog blockiert nicht: IDEAL sucht nicht, die View liefert; unbekannte Token werden gemeldet, AKE-Codes ignoriert."
freigabe_von: "Gerald Weichbold"
freigabe_am: 2026-09-18
# Flache Schluessel mit Absicht: Obsidians Property-Editor kann verschachtelte
# YAML-Objekte NICHT bearbeiten - und genau diesen Block fuellt der Mensch aus.
---

> **Code-Verifikation dieser Spec:** Datei-/Zeilenangaben sind gegen den Worktree
> `feature/2026-08-07-ideal-teile-1-5` (`C:\Git\IDEAL-AKE-WMS\.claude\worktrees\2026-08-07-ideal-teile-1-5`)
> per Read/Grep verifiziert (2026-09-16), inkl. des bereits umgesetzten Klasse-D-Gates in
> `FaWorkStepDetectionService.cs` aus [[2026-09-08-bom-schnittstellen-bridge-hierarchisch-spec]]
> (v1.36.0) und der K2-Werkbank-/Coating-Ableitung aus
> [[2026-08-20-materialisierung-fachliche-felder-spec]] (v1.37.0). Aktuelle Version im Worktree:
> `1.40.0` (`AppVersion.cs`) — naechste freie Nummer fuer diese Spec vermutlich `1.41.0`, im
> Dev-Lauf zu bestaetigen.

## Ziel / Nutzen (das Warum)

Auf AKE entstehen `FaWorkSteps` (Arbeitsgaenge je Auftrag) per **Textheuristik**:
`FaWorkStepDetectionService` durchsucht `CachedBomItems.Bezeichnung1/2` nach den Suchbegriffen jedes
`WorkStep` (`Contains`, verifiziert `FaWorkStepDetectionService.cs:63-64`). Diese Heuristik ist seit
v1.36.0 fuer hierarchische (IDEAL-)Auftraege bereits **hart abgeschaltet** (Klasse-D-Gate,
[[2026-09-08-bom-schnittstellen-bridge-hierarchisch-spec]], Design H) — sie wuerde auf IDEAL-Daten
Fehldaten erzeugen, weil IDEAL die Wahrheit **explizit** liefert:
`FaHierarchyNode.Arbeitsschritte` ist eine **leerzeichen-getrennte** Token-Liste je Position
(Anhang [[sage-views-ideal]] Z. 74 — **nicht** Komma), zusammen mit `Arbeitsbereich` (Z. 73).

Seit diesem Gate laeuft bei Master `true` **gar keine** Arbeitsgang-Erkennung mehr — IDEAL-Auftraege
bekommen keine `FaWorkSteps` (weder Sync noch Struktur), solange dieser Folgeblock nicht umgesetzt
ist. Diese Spec schliesst genau diese Luecke: ein zweiter, eigenstaendiger Sync-Schritt, der die
Struktur-Tokens **explizit** (kein Contains, kein Rateschritt) gegen den bestehenden `WorkStep`-Katalog
matcht und daraus `FaWorkStep`-Zeilen ableitet — analog zur Werkbank-Ableitung aus `Arbeitsbereich`
(ADR 0014) und zur `HasCoatingParts`-Ableitung aus `Beschichtet`
([[2026-08-20-materialisierung-fachliche-felder-spec]], K2): **dritte** Struktur-Ableitung in derselben
Familie, aber als eigener Service statt als weiteres Feld im Materialisierungs-Sync (Begruendung siehe
Design A).

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**

1. Neuer Sync-Service `FaWorkStepStructureDetectionService`, der je materialisiertem Sub-FA
   (`ProductionOrder.SubOrderNumber`) die Arbeitsschritt-Tokens aus **seiner eigenen
   `FaHierarchyNode`-Zeile plus seinen direkten Kindern** (`BomScope.DirectChildren`-Analogie,
   bestaetigte Entscheidung, siehe Fachliche Anforderungen) sammelt, gegen den `WorkStep`-Katalog
   matcht (exakt ueber `WorkStep.Code`) und fehlende `FaWorkStep`-Zeilen mit `Source = "Struktur"`
   anlegt.
2. Nur-hinzufuegen-Semantik identisch zum AKE-Vorbild: bestehende `FaWorkStep`-Zeilen — auch manuell
   entfernte (`IsRemoved = true`) — werden nie erneut angelegt oder veraendert.
3. Unbekannte Token: **melden, nicht automatisch anlegen** (bestaetigte Entscheidung durch Analogie
   zur Werkbank-Stammdaten-Frage, ADR 0014). Sammelmeldung je Lauf mit Token-Liste und Anzahl
   betroffener Sub-FA-Positionen; Sammelmail nur bei Aenderung der Token-**Menge** gegenueber dem
   letzten Lauf (S1-Muster, `IUnknownWorkplaceState`-Vorbild).
4. Neue `FaWorkStepSources.Struktur`-Konstante, neuer Service-Key, neuer `SyncLogServices`-Eintrag,
   Einbindung in den `SyncWorker` **nach** dem bestehenden FA-Materialisierung-Block (Fremdschluessel-
   Abhaengigkeit, siehe Migrations-/SQL-Auswirkungen).
5. Testszenarien inkl. Negativfaellen (unbekanntes Token, manuell entfernter AG, Doppelzaehlungs-
   Gegenprobe DirectChildren vs. Enkelknoten).

**Out-of-Scope**

- Aenderungen an `FaWorkStepDetectionService` (AKE-Heuristik) selbst — bleibt unveraendert inkl.
  seines bestehenden Klasse-D-Gates.
- Aenderungen an `FaWorklist`/der Abarbeitungsliste (Merkmal-Spalten je AG, seit v1.35.0 gruppiert,
  [[2026-07-29-standort-ideal-teil-8-spec]]-Umfeld) — sobald die `FaWorkSteps` korrekt vorliegen,
  zeigt sie dieselben Daten wie bei AKE, ohne Code-Aenderung.
- Pflege des `WorkStep`-Katalogs selbst (neue Codes fuer die 13 IDEAL-Tokens anlegen) — das ist
  Fachbereichs-Arbeit auf der bestehenden `/WorkSteps`-Seite (generisches CRUD, keine Code-Aenderung
  noetig), abhaengig von der offenen Rueckfrage 1.
- Jede Aenderung an `FaHierarchyNode`, `FaHierarchySyncService` oder der Materialisierung selbst
  (Quelle bleibt unveraendert, dieser Service liest nur lesend).

## Fachliche Anforderungen

**Bereits entschiedene Punkte (aus dem Backlog uebernommen, nicht erneut zur Diskussion):**

1. **Unbekannte Token → melden, nicht anlegen.** Dieselbe Entscheidung wie fuer die Werkbank-
   Stammdaten (ADR 0014, [[2026-08-20-materialisierung-fachliche-felder-spec]]): Stammdaten aus einer
   Fremdquelle automatisch zu erzeugen fuellt den Katalog mit Tippfehlern/Altlasten, ohne dass es
   jemand entschieden hat. Sammelmeldung je Lauf mit Token-Liste **und** Anzahl betroffener
   Sub-FA-Positionen; Mail nur bei Aenderung der Menge (Rausch-Vermeidung, S1-Muster).
2. **Zuordnungsebene = `DirectChildren`.** Wie bei der BOM-Bridge
   ([[2026-09-08-bom-schnittstellen-bridge-hierarchisch-spec]], Design D): Die Arbeitsgaenge eines
   Sub-FA ergeben sich aus **seiner eigenen Zeile plus seinen direkten Kindern**
   (`FaHierarchyNode`-Zeilen mit `VaterFA = <SubFA>`), nicht aus allen Nachfahren. Ueber alle Ebenen
   zu sammeln erzeugte dieselbe Doppelzaehlung wie bei der Stueckliste — ein Token, das nur auf einem
   Enkelknoten steht, gehoert zum Arbeitsgang **seines eigenen** materialisierten Sub-FA (falls er
   selbst einer ist), nicht zum Grossvater.
3. **Fremdschluessel bestaetigt Zwei-Lauf-Ablauf.** `FaWorkStep.WorkStepId` ist ein Pflicht-FK auf
   `WorkStep` (`Models/FaWorkStep.cs:14-15`, `Models/WorkStep.cs`, beide verifiziert). Ein `FaWorkStep`
   kann folglich erst angelegt werden, wenn der passende `WorkStep`-Katalogeintrag existiert — der
   Zwei-Lauf-Ablauf (erst Katalog pflegen, dann greift die Anlage) ist damit zwingend, nicht optional.
   Siehe Deploy-Abschnitt.
4. **Matching-Schluessel = `WorkStep.Code`.** `WorkStep.Code` (`[StringLength(20)]`, verifiziert
   `Models/WorkStep.cs:7-10`) ist der korrekte Match-Schluessel — **nicht** `SearchString`
   (`[StringLength(500)]`, `Models/WorkStep.cs:17-19`, kommasepariert, ausschliesslich AKE-Heuristik-
   Feld). `SearchString` bleibt fuer IDEAL-Codes leer/ungenutzt; der Struktur-Service liest ihn nicht.
   Begruendung: Die 13 real vorkommenden Token (siehe unten) sind bereits kurze, eindeutige Codes
   (analog zu den 5 bestehenden AKE-Codes VA/VE/VK/VL/VT) — ein zweites Freitext-Suchfeld waere fuer
   einen exakten Struktur-Wert unnoetige Komplexitaet (YAGNI). Vergleich case-insensitiv + getrimmt
   (analog dem Werkbank-Name-Match, ADR 0014/S2).
   **Ein Katalog, additiv erweitert:** Es bleibt **eine** gemeinsame `WorkSteps`-Tabelle. Die fuenf
   bestehenden AKE-Codes (`VA`/`VE`/`VK`/`VL`/`VT`, mit ihren beschreibenden `SearchString`-Werten fuer
   die AKE-`Contains`-Heuristik) bleiben unveraendert; die IDEAL-Operations-Codes werden **zusaetzlich**
   angelegt (Rueckfrage 1). Kein zweiter Katalog, und die AKE-Codes werden **nicht** fuer IDEAL
   umgedeutet — `SearchString` ist und bleibt ausschliesslich das AKE-Heuristik-Feld, der IDEAL-Pfad
   liest es nicht (die Struktur liefert die Arbeitsschritte explizit mit).

**Reale Token-Menge — Naeherungswert, NICHT die massgebliche Quelle (offene Rueckfrage 1):**

Aus `IDEAL_Test.dbo.IDEAL_faap.USER_Arbeitsschritt` (Rohspalte, Leerzeichen-gesplittet — **nicht** die
massgebliche Sage-View `vw_IDEAL-AKE_Kommissionierung_FAListe`, auf die kein Zugriff bestand):
13 distinkte Token mit Haeufigkeit — `KA` 30671 · `LS` 30109 · `AV` 29334 · `EG` 19465 · `PG` 9775 ·
`SW` 7391 · `SÄ` 3472 · `SL` 2662 · `PL` 2189 · `EK` 806 · `BR` 191 · `SWL` 8 · `VI` 2.
**Pre-Flight-Abgleich gegen den WMS-Katalog:** Der produktive `WorkSteps`-Katalog enthaelt aktuell nur
5 AKE-Codes (`VA`/`VE`/`VK`/`VL`/`VT`) — **keiner** der 13 IDEAL-Token matcht. Am Tag 1 nach Deploy
sind demnach **alle** Token „unbekannt" — der Zwei-Lauf-Ablauf ist real, nicht theoretisch. Was jedes
Token fachlich bedeutet und welcher `WorkStep`-Code/-Name dafuer angelegt werden soll, ist
Rueckfrage 1 (unten) — **nicht geraten**.

## Technischer Loesungsentwurf

Referenzmuster: [[0010-aktivitaets-protokoll-mit-isolierten-dbcontexts]] (`ISyncLogger` letzter
Ctor-Parameter, deutsche Counts-Keys), [[0014-werkbank-datenhoheit-sage-fuehrend-mit-abweichungsmeldung]]
(Struktur-Ableitung + Melde-statt-Anlege-Prinzip), ADR 0013 (Bridge-Repository-Muster, zur Abgrenzung
in Design A). Betroffene Module: [[services]] (Sync-Services, `SyncWorker`), [[datenmodell]]
(`FaWorkStep`, `WorkStep`, `FaHierarchyNode`).

### A — Eigener Service statt Standort-Switch (Design-Entscheidung, begruendet)

Der Backlog fragt ausdruecklich, ob ein Standort-Switch (analog zur View-Standortaufloesung) oder ein
eigener Service sauberer ist. **Entscheidung: eigener Service, kein DI-Switch/Bridge.**

- Die **Bridge**-Muster im Paket (`FaHierarchyBomRepository` hinter `IBomRepository`/
  `IBomCacheRepository`, ADR 0013) existieren, weil **viele** Web-Aufrufer (Picking, Vorbau,
  Artikelinfo, Bedarfsmeldungen) an **derselben** Schnittstelle haengen — eine zweite
  Interface-Implementierung deckt sie alle gleichzeitig ab.
- `FaWorkStepDetectionService`/`IFaWorkStepDetectionService` hat dagegen **genau einen** Aufrufer: den
  `SyncWorker` (verifiziert per Grep, einziger Treffer `SyncWorker.cs:223`). Eine zweite
  Interface-Implementierung mit DI-Weiche brächte hier keinen Vorteil gegenüber einer zweiten, eigenen
  Service-Klasse mit eigenem `SyncWorker`-Aufrufblock — im Gegenteil, sie würde eine künstliche
  gemeinsame Abstraktion über zwei fachlich unterschiedliche Ableitungen (Text-Contains vs.
  Token-Exact-Match) erzwingen.
- Auch die **Klasse-D-Gates** (`CoatingDetectionService`, `FaWorkStepDetectionService`,
  `BomCacheSyncService`, alle verifiziert v1.36.0) sind kein Vorbild fuer eine interne Gate-Pruefung
  in diesem neuen Service: Sie existieren, weil jene Services **mehrere Eintrittspunkte** haben
  (`BomCacheSyncService` hat zwei oeffentliche Methoden, eine davon von `SageImportService` ohne
  Fenster-Filter aufgerufen — ein echter Umgehungspfad). Der neue Struktur-Service hat **einen**
  Eintrittspunkt (`SyncWorker`). Das passende Vorbild ist stattdessen `FaHierarchySyncService`/
  `FaMaterializationSyncService`: **beide** verlassen sich vollstaendig auf den `SyncWorker`-seitigen
  `ProduktionsauftragHierarchisch`-Check (verifiziert, keine interne Gate-Pruefung in beiden
  Service-Klassen) — derselbe Aufbau gilt hier.
- **Konsequenz:** kein neues `IHierarchicalModeReader`-Konstruktor-Argument, kein Standort-Switch im
  Service selbst. Der Master-Check + der neue Feature-Toggle sitzen **ausschliesslich** an der
  Aufrufstelle im `SyncWorker` (siehe Design C).

### B — Ableitung als eigener Service statt Erweiterung von `FaMaterializationSyncService`

`HasCoatingParts` (K2) wurde bewusst **in** `FaMaterializationSyncService` integriert (dieselbe
Transaktion, dasselbe Eager-Create der Satelliten-Zeile). Arbeitsgaenge folgen **nicht** demselben
Weg, aus zwei Gruenden:

1. **Andere Ziel-Tabelle, andere Kardinalitaet.** `HasCoatingParts` ist ein 1:1-Flag auf der bereits
   vorhandenen `ProductionOrderPickingStatus`-Satelliten-Zeile (ADR 0009) — ein einzelnes Feld je
   Sub-FA. `FaWorkStep` ist eine **1:n-Relation** (mehrere Arbeitsgaenge je Sub-FA) mit eigenem
   Fremdschluessel-Katalog (`WorkStep`) und eigener Nur-hinzufuegen-/`IsRemoved`-Semantik — dieselbe
   Struktur wie die bestehende AKE-Heuristik, die bereits als eigener Service existiert.
2. **Bestehendes Vorbild wiederverwenden statt neu erfinden (ponytail: Schritt 2 der Leiter).**
   `FaWorkStepDetectionService` ist exakt das Muster fuer „Arbeitsgang-Erkennung als eigener,
   nachgelagerter Sync-Schritt" — der Struktur-Service kopiert diese Form 1:1 (Signatur, Nur-
   hinzufuegen-Query, Cap/Zusammenfassungs-Logging) und tauscht nur die Quelle/den Match-Mechanismus.
   Eine Integration in `FaMaterializationSyncService` wuerde diesen bereits etablierten,
   funktionierenden Rahmen verwerfen und durch eine Sonderlogik in einem fachlich fremden Service
   ersetzen.

**Reihenfolge im `SyncWorker`:** FA-Hierarchie-Sync (Struktur-Refresh) → FA-Materialisierung
(Struktur → `ProductionOrders`) → **NEU: FA-Arbeitsgang-Erkennung (Struktur)**. Die letzte
Abhaengigkeit ist zwingend: Ohne materialisierte `ProductionOrder`-Zeilen gibt es kein Ziel fuer
`FaWorkStep.ProductionOrderId` (siehe Design D).

### C — Gate im `SyncWorker` (Doppel-Gate, analog dem AKE-Vorbild)

```
if (await ServiceSettings.GetBoolSafeAsync(_configuration, "ProduktionsauftragHierarchisch", false, stoppingToken)
    && await ServiceSettings.GetBoolSafeAsync(_configuration, "Sync:FaWorkStepStructureDetectionEnabled", false, stoppingToken))
{
    await RunResilientAsync("FA-Arbeitsgang-Erkennung (Struktur)", async () =>
    {
        using var scope = _scopeFactory.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<IFaWorkStepStructureDetectionService>();
        var r = await svc.DetectAsync(dryRun: dryRun, stoppingToken);
        _logger.LogInformation("FA-Arbeitsgang-Erkennung (Struktur) fertig: Neu={Ins}, Err={Err}", r.Inserted, r.Errors);
    }, stoppingToken);
}
```

**Warum zwei Gates statt nur `ProduktionsauftragHierarchisch`** (anders als der FA-Materialisierung-
Block, der nur den Master prueft): Der Master allein schaltet die Materialisierung scharf — das
Einwegtor (ADR 0012) macht diesen Schritt bewusst schwer rueckgaengig. Die Arbeitsgang-Erkennung ist
dagegen **risikoarm und reversibel** (Nur-hinzufuegen, kein Einwegtor) und haengt zusaetzlich an einem
vollstaendig zu pflegenden Katalog (Rueckfrage 1). Ein eigener Toggle erlaubt, den Master scharf zu
schalten und die Materialisierung zu beobachten, **bevor** die Arbeitsgang-Erkennung anlaeuft — exakt
das bestehende Muster von `Sync:BomCacheEnabled` + `Sync:FaWorkStepDetectionEnabled` (zwei
unabhaengige Schalter fuer zwei logisch aufeinanderfolgende Schritte), hier auf die hierarchische
Seite gespiegelt.

**Beide Gates ueber `GetBoolSafeAsync`** — bewusst dieselbe (fehlertolerante) Variante wie der
unmittelbar vorausgehende FA-Materialisierung-Block, der `ProduktionsauftragHierarchisch` ebenfalls
mit `GetBoolSafeAsync` liest (verifiziert Worktree `SyncWorker.cs:357`). So wirft ein transienter
DB-Fehler oder ein noch nicht geseedeter neuer Key den SyncWorker nicht, sondern faellt auf den
Default (`false`) zurueck. (Der benachbarte AKE-BomCache-/`FaWorkStepDetection`-Block nutzt die
werfende `GetBoolAsync`-Variante; die neue hierarchische Kette folgt aber der Konvention ihres
direkten Nachbarn, nicht der AKE-Kette.)

### D — `DetectAsync`-Ablauf (Kern der Ableitung)

1. `BeginRunAsync(SyncLogServices.FaWorkStepStructureDetection)`.
2. `_db.FaHierarchyNodes.AsNoTracking().ToListAsync()` — vollstaendiger Struktur-Stand (Full-Refresh-
   Cache, Teil 1), analog `FaMaterializationSyncService.RunAsync` (verifiziert Zeile 73-74).
3. Lookup `childrenByParent`: `Dictionary<int, List<FaHierarchyNode>>`, Key = `VaterFA` (nur Zeilen mit
   `VaterFA.HasValue`), Value = alle Zeilen mit diesem `VaterFA` — eine Gruppierung, kein N+1.
4. Fuer jede Zeile mit `SubFA != 0` (identische Grundmenge wie `FaMaterializationSyncService`, Zeile
   79 verifiziert — „nur Nicht-Blatt-Knoten werden materialisiert"): DirectChildren-Scope = diese
   Zeile **plus** `childrenByParent.GetValueOrDefault(SubFA)`. Tokens = alle `Arbeitsschritte`-Werte
   dieser Zeilen, an Leerzeichen gesplittet (`Split(' ', StringSplitOptions.RemoveEmptyEntries |
   StringSplitOptions.TrimEntries)`), `Distinct(StringComparer.OrdinalIgnoreCase)`. Leere/`null`-Werte
   ergeben eine leere Token-Menge — dieser Sub-FA wird uebersprungen (kein Fehler).
5. `_db.WorkSteps.Where(w => w.IsActive).ToListAsync()`, Dictionary `Code (OrdinalIgnoreCase,
   getrimmt) → WorkStep`.
6. Je Sub-FA-Token-Paar: bekannt (Katalog-Treffer) → Kandidat; unbekannt → in
   `Dictionary<string, HashSet<string>>` sammeln (Token → Menge betroffener `SubOrderNumber`-Werte).
7. **Eine** gebuendelte `ProductionOrder`-Abfrage ueber alle betroffenen `SubOrderNumber`-Werte
   (`Where(o => keys.Contains(o.SubOrderNumber) && !o.IsDone && !o.IsCancelled && !(o.PickingStatus !=
   null && o.PickingStatus.IsDonePicking))`) — derselbe Fertig-/Storno-Filter wie im AKE-Vorbild
   (`FaWorkStepDetectionService.cs:88-98` verifiziert), damit abgeschlossene Auftraege keine neuen
   Arbeitsgaenge mehr bekommen. `SubOrderNumber` ist der bestaetigte Schluessel (siehe Design E) —
   **keine** Migration/kein neuer Lookup-Pfad noetig.
8. Nur-hinzufuegen-Filter identisch zum AKE-Vorbild: `!_db.FaWorkSteps.Any(f => f.ProductionOrderId ==
   o.Id && f.WorkStepId == step.Id)` — greift unabhaengig von `IsRemoved` (aktive **und** manuell
   entfernte Zeilen sperren das Re-Add, exakt wie bei der AKE-Heuristik).
9. Insert je Kandidat: `FaWorkStep { ProductionOrderId, WorkStepId, Source =
   FaWorkStepSources.Struktur, CreatedAt = DateTime.Now, CreatedBy = "FaWorkStepStructureDetection",
   CreatedByWindows = "FaWorkStepStructureDetection" }`, `LogInfoAsync($"FA {OrderNumber} → AG {Code}
   {Name} erkannt (Struktur, Token {token})", reference: OrderNumber)`.
10. `SaveChangesAsync` nur wenn `!dryRun` (DryRun zaehlt/loggt, schreibt nicht — identisches Muster).
11. Unbekannte Token: **jeder** Lauf loggt eine Warnung mit Token-Liste + Anzahl betroffener Positionen
    (`"Unbekannte Arbeitsschritte ohne WorkStep-Zuordnung (N Tokens, M Auftraege betroffen): TOKEN1 (n1
    Auftraege), TOKEN2 (n2 Auftraege), ..."`); Sammelmail nur, wenn
    `IUnknownWorkStepTokenState.HasChanged(unknownTokens.Keys)` `true` liefert (S1-Muster, Menge =
    Token-**Namen**-Set, nicht nur die Anzahl — sonst faellt eine Token-Verschiebung bei gleicher
    Gesamtzahl nicht auf).
12. `FinishSuccessAsync` mit Counts `neu`, `uebersprungen`, `arbeitsschritt_unbekannt` (Anzahl
    distinkter unbekannter Token), `arbeitsschritt_unbekannt_auftraege` (Anzahl distinkter betroffener
    Sub-FA-Positionen).

### E — Sub-FA → `ProductionOrder`-Zuordnung: bereits am Code bestaetigt, keine offene Frage

Der Backlog/Auftrag benennt dies als moeglicherweise offene Frage — sie ist **bereits geklaert**:
`FaMaterializationSyncService` matcht durchgaengig ueber `ProductionOrder.SubOrderNumber ==
FaHierarchyNode.SubFA.ToString()` (verifiziert u. a. Zeile 92-96, 195, 263-264 im Worktree — sowohl
beim Erstellen als auch bei jeder Folgeabfrage). `SubFA` ist die BelID des Knotens, eine globale
Sage-Surrogat-ID (nicht je HauptFA vergeben) — `srcByKey = source.ToDictionary(s =>
s.SubOrderNumber)` in `FaMaterializationSyncService` waere sonst nicht kollisionsfrei moeglich. Der
Struktur-Service uebernimmt exakt dieselbe Zuordnung (Design D, Schritt 7) — kein neuer Lookup-Pfad,
keine Erweiterung von `IProductionOrderRepository` noetig.

**Nebenbefund, dokumentiert statt zur Frage gemacht:** Ist ein direktes Kind eines Sub-FA selbst ein
materialisierter Sub-FA (eigene `SubFA != 0`-Zeile), traegt dessen Zeile `Arbeitsschritte` in **zwei**
Kontexten bei: einmal als Teil des DirectChildren-Scopes seines **Elternteils** (Arbeitsgaenge zur
Fertigung/zum Einbau dieser Baugruppen-Position) und einmal als **eigene** Zeile bei seiner eigenen
Verarbeitung (Arbeitsgaenge seines eigenen Fertigungsauftrags). Das ist **kein** Fehler, sondern die
mechanische Konsequenz der bestaetigten DirectChildren-Regel — dieselbe Sage-Spalte beschreibt an
dieser Stelle zwei verschiedene, beide gueltige fachliche Sachverhalte (Position im Elternkontext vs.
eigener Auftrag). Zur Sichtung beim ersten echten Datenlauf vormerken (siehe Testszenarien).

## Migrations-/SQL-Auswirkungen

**Keine Migration.** Verifiziert:

- `FaWorkStep.Source` ist bereits `NVARCHAR(20)` (Kommentar `Models/FaWorkStep.cs:27`) — `"Struktur"`
  (8 Zeichen) passt ohne Schema-Aenderung.
- `WorkStep.Code` (`[StringLength(20)]`) ist eine bereits bestehende, bereits befuellte Spalte — kein
  neues Feld.
- Neue `ServiceSettings`-Keys sind reine Katalog-/Laufzeit-Konfiguration
  (`ServiceSettingDefinitions.All`, DB-first per ADR 0008) — kein Schema-Impact, nur ein neuer
  Katalogeintrag (Drift-Guard-Test schlaegt sonst fehl).
- `FaWorkStepStructureDetectionService`/`IUnknownWorkStepTokenState` sind reine C#-Klassen ohne
  DB-Schema-Beruehrung.

**Fremdschluessel-Konsequenz (kein Schema-Thema, aber Betriebs-Ablauf, siehe Deploy):**
`FaWorkStep.WorkStepId` ist Pflicht-FK auf `WorkStep` — der Struktur-Service kann fuer ein Token erst
dann eine Zeile anlegen, wenn der passende `WorkStep`-Katalogeintrag existiert. Das ist **keine**
Migrationsfrage, sondern eine **Datenpflege-Reihenfolge** (Zwei-Lauf-Ablauf, siehe Deploy).

## Audit-Feld-Auswirkungen

`FaWorkStep` erbt `AuditableEntity`. Der Struktur-Service setzt bei jeder neu angelegten Zeile
`CreatedAt = DateTime.Now`, `CreatedBy = "FaWorkStepStructureDetection"`, `CreatedByWindows =
"FaWorkStepStructureDetection"` — der Service-Name als Autor, analog dem bestehenden AKE-Vorbild
(`CreatedBy = "FaWorkStepDetection"`, verifiziert `FaWorkStepDetectionService.cs:127-128`) und ADR
0003 („Service-Syncs schreiben ihren Service-Namen"). Der Service **aktualisiert nie** bestehende
`FaWorkStep`-Zeilen (reine Nur-hinzufuegen-Semantik) — `ModifiedAt`/`ModifiedBy` sind fuer diesen Pfad
nicht relevant.

## Betroffene Rollen / Zugriffsfilter

Keine Aenderung. Der Service laeuft ausschliesslich im Hintergrund (Windows-Service, kein Web-Endpoint,
kein neuer Controller). Die bestehenden Ansichten, die `FaWorkStep`-Daten zeigen (`/WorkSteps`-Katalog,
Vorbau, Leitstand-AG-Spalten, `FaWorklist` seit v1.35.0 gruppiert), behalten ihre bestehenden
`RequireXxxAccess`-Filter unveraendert — diese Spec liefert nur zusaetzliche Datenzeilen in bereits
zugriffsgeschuetzte, bestehende Ansichten hinein.

## Listen-View-Pattern-Pflichten (ADR 0005)

Nicht anwendbar. Keine neue Tabellen-Ansicht, kein neuer `ColumnDef`-Eintrag — `FaWorkStep`-Zeilen
erscheinen in bereits bestehenden Ansichten (Vorbau, Leitstand, `FaWorklist`), die `Source` nicht als
eigene, gefilterte Spalte rendern (verifiziert: kein Fundstelle `FaWorkStepSources`/Source-Fallunter-
scheidung in `Views/`, `Source` wird nur beim **Schreiben** verwendet, z. B.
`FaWorkStepRepository.cs:134/144` beim manuellen Hinzufuegen/Entfernen). Der neue Wert `"Struktur"`
erfordert daher **keine** View-Aenderung.

## Akzeptanzkriterien

1. **AKE unveraendert.** Bei Master `false` wird `FaWorkStepStructureDetectionService` nie aufgerufen
   (`SyncWorker`-Gate greift vor der Service-Aufloesung) — kein SyncLog-Eintrag
   „FaWorkStepStructureDetection", keine `FaWorkStep`-Zeile mit `Source = "Struktur"` entsteht.
2. **Zwischenzustand ohne Doppel-Erkennung.** Bei Master `true` und `Sync:FaWorkStepStructureDetectionEnabled = false`:
   weder die AKE-Heuristik (Klasse-D-Gate bereits aktiv) noch der Struktur-Service laufen — es entsteht
   **keine** `FaWorkStep`-Erkennung, weder `Sync` noch `Struktur`, kein Fehler.
3. **Struktur-Ableitung korrekt.** Bei Master `true` und Toggle `true`: fuer jeden materialisierten
   Sub-FA mit mindestens einem im Katalog bekannten Token in seiner DirectChildren-Token-Menge entsteht
   je bekanntem Token **genau eine** neue `FaWorkStep`-Zeile mit `Source = "Struktur"`.
4. **Nur-hinzufuegen greift wie im AKE-Vorbild.** Ein manuell entfernter (`IsRemoved = true`)
   `FaWorkStep`-Datensatz wird vom Struktur-Service **nie** erneut angelegt, auch wenn das zugehoerige
   Token weiterhin in der Struktur steht.
5. **DirectChildren, keine Doppelzaehlung ueber Enkel.** Ein Token, das ausschliesslich auf einem
   Enkelknoten (nicht auf der eigenen Zeile oder einem direkten Kind) eines Sub-FA steht, erzeugt
   **keinen** `FaWorkStep` fuer diesen Sub-FA.
6. **Matching exakt, nicht heuristisch.** Das Matching erfolgt ausschliesslich ueber `WorkStep.Code`
   (case-insensitiv, getrimmt) — ein Treffer ueber `SearchString`/`Contains` findet fuer IDEAL-Token
   nicht statt (Regressionstest: ein `WorkStep` mit passendem `SearchString`, aber abweichendem `Code`,
   erzeugt **keinen** Treffer).
7. **Unbekannte Token gemeldet, nicht angelegt.** Kein `WorkStep`-Datensatz wird automatisch erzeugt;
   jeder Lauf mit unbekannten Token protokolliert Token-Liste **und** Anzahl betroffener Positionen
   (Counts `arbeitsschritt_unbekannt`/`arbeitsschritt_unbekannt_auftraege`); die Sammelmail geht **nur**
   bei Aenderung der Token-Menge gegenueber dem letzten Lauf raus (Nachweis: zwei aufeinanderfolgende
   Laeufe mit identischer unbekannter Menge → genau eine Mail, nicht zwei).
8. **Fehlende Materialisierung kein Fehler.** Ein `FaHierarchyNode`-Sub-FA ohne passenden
   `ProductionOrder` (noch nicht materialisiert) erzeugt keinen Fehler/Abbruch — wird beim naechsten
   Lauf nach der Materialisierung automatisch beruecksichtigt.
9. **Abgeschlossene Auftraege bleiben unangetastet.** Ein `ProductionOrder` mit `IsDone`/`IsCancelled`/
   `PickingStatus.IsDonePicking = true` bekommt keine neue `FaWorkStep`-Zeile, auch bei bekanntem
   Token (Regressionsfilter analog AKE-Vorbild).
10. **Audit korrekt.** Jede neu angelegte `FaWorkStep`-Zeile traegt `CreatedBy`/`CreatedByWindows =
    "FaWorkStepStructureDetection"`.
11. **Aktivitaets-Protokoll vollstaendig.** Jeder Lauf (auch ohne Treffer) erzeugt einen SyncLog-Eintrag
    unter dem Service-Namen `FaWorkStepStructureDetection` mit den definierten Counts-Keys.
12. **Fremdschluessel-Grenze sichtbar.** Solange kein passender `WorkStep`-Katalogeintrag existiert,
    bleibt das Token durchgaengig „unbekannt" gemeldet — kein stiller Teilerfolg, keine Exception.

## Test-Szenarien

Neues Kapitel „IDEAL — FA-Arbeitsgang-Erkennung aus der Struktur" in `docs/TESTSZENARIEN.md`,
**TS-76** (naechste freie Nummer im Worktree, Stand TS-75 — Dev-Lauf bestaetigt gegen den dann
aktuellen Stand):

- **TS-76.1 Grundfall (AK 3).** Vorbedingung: Master `true`, Toggle `true`, `WorkStep`-Katalog
  enthaelt einen Eintrag mit `Code = "KA"`; ein materialisierter Sub-FA hat `Arbeitsschritte` mit dem
  Token `KA` auf seiner eigenen Zeile. Schritte: Sync-Lauf abwarten/ausloesen. Erwartung: genau eine
  neue `FaWorkStep`-Zeile mit `Source = "Struktur"` fuer diesen Sub-FA und den `KA`-`WorkStep`.
- **TS-76.2 DirectChildren-Gegenprobe (AK 5).** Vorbedingung: Sub-FA X hat ein direktes Kind Y, das
  **selbst** ein Sub-FA ist (`SubFA != 0`); Y hat ein eigenes Kind (Enkel von X) Z, das ein Token `SÄ`
  traegt, das **nur** auf Z vorkommt (nicht auf X, nicht auf Y). Schritte: Sync-Lauf. Erwartung: `SÄ`
  erscheint als Arbeitsgang bei **Y** (Y’s DirectChildren-Scope = Y-Zeile + direkte Kinder, enthaelt Z),
  **nicht** bei X (X’s DirectChildren-Scope = X-Zeile + direkte Kinder, enthaelt Y’s eigene Zeile, aber
  **nicht** Z).
- **TS-76.3 Nur-hinzufuegen / manuell entfernt (AK 4).** Vorbedingung: ein `FaWorkStep` mit
  `IsRemoved = true` fuer (Sub-FA, WorkStep) existiert bereits. Schritte: Sync-Lauf, Token weiterhin in
  der Struktur vorhanden. Erwartung: keine neue Zeile, die entfernte Zeile bleibt unveraendert
  (`IsRemoved = true`).
- **TS-76.4 Unbekanntes Token — Meldung, kein Insert (AK 7, AK 12).** Vorbedingung: Struktur enthaelt
  ein Token ohne passenden `WorkStep`. Schritte: zwei aufeinanderfolgende Laeufe mit identischer
  unbekannter Token-Menge. Erwartung: kein `FaWorkStep` fuer dieses Token; SyncLog zeigt die
  Sammelmeldung bei **beiden** Laeufen; die Sammelmail geht (bei aktivierter `ErrorNotification`) nur
  beim **ersten** der beiden Laeufe raus. Negativfall: ein zusaetzliches unbekanntes Token taucht im
  dritten Lauf neu auf → erneute Mail.
- **TS-76.5 Abgeschlossener Auftrag (AK 9).** Vorbedingung: ein Sub-FA mit `IsDone = true` und
  bekanntem Token. Schritte: Sync-Lauf. Erwartung: keine neue `FaWorkStep`-Zeile.
- **TS-76.6 Fehlende Materialisierung (AK 8).** Vorbedingung: `FaHierarchyNode`-Sub-FA existiert, aber
  `FaMaterializationSyncService` hat ihn noch nicht materialisiert (z. B. Reihenfolge-Test: Struktur-
  Sync vor Materialisierungs-Lauf). Schritte: Struktur-Arbeitsgang-Erkennung laufen lassen. Erwartung:
  kein Fehler, keine Exception, Sub-FA einfach ausgelassen; nach dem naechsten Materialisierungs-Lauf
  + erneuter Arbeitsgang-Erkennung erscheint der Arbeitsgang.
- **TS-76.7 AKE-Flachmodus-Regression (AK 1).** Vorbedingung: Master `false`. Schritte: Sync-Zyklus.
  Erwartung: bestehendes Verhalten der AKE-Heuristik unveraendert (`FaWorkStepDetectionService`
  weiterhin aktiv wie vor dieser Spec); kein SyncLog-Eintrag „FaWorkStepStructureDetection".
- **TS-76.8 Zwischenzustand (AK 2).** Vorbedingung: Master `true`, `Sync:FaWorkStepStructureDetectionEnabled = false`.
  Schritte: Sync-Zyklus. Erwartung: weder AKE-Heuristik (bereits durch Klasse-D-Gate inaktiv) noch
  Struktur-Service erzeugen `FaWorkStep`-Zeilen; kein Fehler.
- **TS-76.9 Matching exakt statt heuristisch (AK 6).** Vorbedingung: `WorkStep` mit `Code = "XY"` und
  `SearchString`, der zufaellig ein IDEAL-Token als Teilstring enthaelt. Schritte: Sync-Lauf.
  Erwartung: kein Treffer ueber den `SearchString` — nur ein `WorkStep` mit exakt passendem `Code`
  erzeugt einen `FaWorkStep`.

Nach Abschluss `secondbrain/tests/testszenarien-index.md` nachziehen (neue TS-76-Zeile).

## Deploy

**Provisorisch (Spec-Agent) — der Dev-/qa-agent-Lauf bestaetigt gegen den echten Diff.**

- **Web-App: ja (provisorisch, geringer Verhaltens-Impact).** `IdealAkeWms/Models/FaWorkStep.cs`
  (neue `FaWorkStepSources.Struktur`-Konstante) und `IdealAkeWms/Models/ServiceSettingDefinitions.cs`
  sowie `IdealAkeWms/Services/SyncLogger/SyncLogServices.cs` liegen im Web-Projekt (gemeinsam genutzte
  Assembly mit dem Service). Kein View-/Controller-Verhalten aendert sich (verifiziert: `Source` wird
  in keiner View unterschieden gerendert) — der Web-Deploy ist wegen der gemeinsamen Modell-/
  Katalog-Datei formal noetig, nicht wegen sichtbarer Web-Funktionalitaet.
- **Service: ja.** Neuer Service (`FaWorkStepStructureDetectionService.cs`,
  `IFaWorkStepStructureDetectionService.cs`), neue Singleton-Klasse
  (`IUnknownWorkStepTokenState.cs`), `SyncWorker.cs` (neuer Gate-Block), `Program.cs`
  (zwei neue DI-Registrierungen).
- **Migration: nein.** Siehe Migrations-Abschnitt — keine Schema-Aenderung.

**Zwei-Lauf-Ablauf (PFLICHT, aus dem Fremdschluessel-Befund, Fachliche Anforderung 3/ADR 0014-Analogie):**

1. **Vor dem Deploy — Rueckfrage 1 beantworten** und die passenden `WorkStep`-Katalogeintraege
   (`Code` je Token, `Name`, optional `SortOrder`) **auf der bestehenden `/WorkSteps`-Seite** anlegen.
   Analog zur Werkbank-Empfehlung in ADR 0014 („die fuenf Arbeitsplaetze vor dem Deploy anlegen"):
   sind die Katalogeintraege vor dem ersten scharfen Lauf vorhanden, greift die Anlage sofort und der
   Zwei-Lauf-Fall tritt praktisch nicht ein.
2. **Deploy** (Web + Service, siehe oben).
3. **`Sync:FaWorkStepStructureDetectionEnabled` zunaechst mit `WorkerSettings:SyncDryRun = true`
   testen** (bestehender globaler DryRun-Schalter) — SyncLog/Sammelmeldung pruefen, bevor scharf
   geschaltet wird (kein neuer Mechanismus, nutzt den bestehenden DryRun-Pfad des `SyncWorker`).
4. Danach `DryRun` deaktivieren — ab hier greift die Nur-hinzufuegen-Semantik dauerhaft.

**Ablauf (Mensch): Publish aus dem Worktree → Testsystem → testen → Merge (Schranke 2).**

```
dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb
dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService
```

## Reihenfolge / Einordnung

- **Im Buendel-Zweig**, wie alle IDEAL-Bausteine seit August (`feature/2026-08-07-ideal-teile-1-5`) —
  kein neuer Worktree.
- **Setzt voraus:** [[2026-09-08-bom-schnittstellen-bridge-hierarchisch-spec]] (Klasse-D-Gate der
  AKE-Heuristik bereits umgesetzt, Voraussetzung fuer AK 2) und
  [[2026-08-20-materialisierung-fachliche-felder-spec]] (materialisierte `ProductionOrder`-Zeilen als
  Ziel, Voraussetzung fuer AK 3/8) — beide bereits `Testbereit` im selben Worktree.
- **Blockiert von Rueckfrage 1.** Ohne die massgebliche Token-Liste + den fachlichen Abgleich bleibt
  der `WorkStep`-Katalog fuer IDEAL leer und **jedes** Token laeuft als „unbekannt" durch die Meldung
  (kein Absturz, aber wirkungslos) — funktional nutzbar wird dieser Baustein erst nach der Katalog-
  Pflege.

## Brain-Pflichten bei Umsetzung (Merkliste fuer den Dev-Lauf)

- Version-Bump in **beiden** `AppVersion.cs` — naechste freie Nummer nach dem Stand dieser Spec
  vermutlich `1.41.0`, im Dev-Lauf zu bestaetigen.
- Anwender-Changelog `Views/Help/Changelog.cshtml` + Brain-Changelog
  `secondbrain/changelog/YYYY-MM-DD-vX-Y-Z-fa-arbeitsgang-struktur.md`.
- `secondbrain/feature-map.md` — neue Zeile im IDEAL-Abschnitt.
- `secondbrain/codebase/services.md` — neuen Service in die Sync-Services-Tabelle aufnehmen (analog
  dem bestehenden Eintrag fuer `FaWorkStepDetectionService.cs`), neuen `ServiceSettings`-Key in die
  Katalog-Tabelle.
- `secondbrain/architektur/fallstricke.md` — Eintrag zum Doppel-Kontext derselben `Arbeitsschritte`-
  Zeile (Design E, Nebenbefund) — dieselbe Klasse Fallstrick wie die bereits dokumentierten
  Struktur-Eigenheiten (Umhaeng-Konflikt, SubFA=0).
- Hilfeseite (`Views/Help/`) — konkrete Details zur neuen Struktur-Ableitung, insbesondere den
  Zwei-Lauf-Hinweis (`feedback_help_details`-Regel).
- `secondbrain/tests/testszenarien-index.md` — TS-76-Zeile.

## Offene Rueckfragen

1. **Massgebliche Token-Menge + fachlicher Abgleich.** Die in dieser Spec verwendete Token-Liste
   (`KA`/`LS`/`AV`/`EG`/`PG`/`SW`/`SÄ`/`SL`/`PL`/`EK`/`BR`/`SWL`/`VI`) stammt aus der Rohspalte
   `IDEAL_Test.dbo.IDEAL_faap.USER_Arbeitsschritt`, **nicht** aus der massgeblichen Sage-View
   `vw_IDEAL-AKE_Kommissionierung_FAListe` (Zugriff verweigert). Bitte (a) die Token-Menge gegen die
   echte View bestaetigen/korrigieren und (b) je Token festlegen, welcher `WorkStep`-`Code`/`Name`
   dafuer im Katalog angelegt wird. Ohne diese Antwort bleibt der Struktur-Service nach dem Deploy
   wirkungslos (alle Token laufen als „unbekannt").

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

1. → **Der Blocker entfaellt — `IDEAL_TEST_2026_05_03` wird nicht gebraucht.**

   **Die massgebliche Quelle liegt bereits in der WMS-Datenbank.** Was
   `vw_IDEAL-AKE_Kommissionierung_FAListe` liefert, hat der Struktur-Sync schon nach
   **`FaHierarchyNode.Arbeitsschritte`** geschrieben (533 Knoten, befuellt). Darauf besteht Zugriff:

   ```sql
   SELECT value AS Token, COUNT(*) AS Vorkommen
   FROM FaHierarchyNode
   CROSS APPLY STRING_SPLIT(Arbeitsschritte, ' ')
   WHERE Arbeitsschritte IS NOT NULL AND Arbeitsschritte <> '' AND value <> ''
   GROUP BY value
   ORDER BY Vorkommen DESC;
   ```

   Diese Liste ist **verbindlich** — sie hat den Weg durch die View bereits hinter sich. Der Befund
   aus `IDEAL_faap.USER_Arbeitsschritt` ist als **nicht massgeblich** zu kennzeichnen, nicht nur als
   Naeherungswert.

   ### Einordnung korrigiert: es sind NICHT zwei Vokabulare

   **AKE und IDEAL liefern dieselben Arbeitsschritte — nur mit verschiedenem Trennzeichen:**
   AKE **komma**-getrennt, IDEAL **leerzeichen**-getrennt. Gleiche Bedeutung, **gleicher
   `WorkStep`-Katalog**. Der Katalog wird geteilt, nicht gedoppelt.

   Damit ist auch der Befund „null von dreizehn Token matchen" **mit hoher Wahrscheinlichkeit ein
   Artefakt der falschen Quelle**, nicht ein fachlicher Totalausfall. Er ist mit der Abfrage oben
   neu zu erheben, bevor daraus irgendetwas gefolgert wird.

   ### Folge fuer den Loesungsentwurf — zu pruefen, bevor gebaut wird

   Reduziert sich der Unterschied zwischen den Standorten auf **das Trennzeichen beim Split**, ist
   ein **eigener** `FaWorkStepStructureDetectionService` schwer zu begruenden. Matching-Logik,
   Katalog und Zielentitaet waeren identisch — unterschiedlich waere ein einziges Zeichen.

   **Bitte pruefen:** Genuegt der bestehende `FaWorkStepDetectionService` mit einem
   **Trennzeichen-Parameter** (und der anderen Quellspalte), statt ein zweites, fast gleiches
   Modul zu pflegen? Zwei Fassungen desselben Verfahrens laufen erfahrungsgemaess auseinander —
   und genau davor soll die `ponytail`-Leiter bewahren (Sprosse 2: gibt es das im Codebestand
   schon?).
   Spricht etwas dagegen — etwa ein abweichender Gate-Bedarf oder eine andere Fehlerbehandlung —
   **melden statt still den zweiten Service bauen.**

   ### Wenn der Abgleich danach immer noch nicht trifft

   Liefert die Abfrage gegen `FaHierarchyNode` Token, die **weiterhin** keinem `WorkStep.Code`
   entsprechen, ist **das** der echte Befund. Dann **melden, nicht selbst aufloesen** — es waere
   eine fachliche Frage (welcher Katalogeintrag gehoert zu welchem Token), und die beantwortet der
   Fachbereich, nicht der Lauf.
   Die Regel „MELDEN statt anlegen" bleibt davon unberuehrt und gilt unveraendert.

## KORREKTUR meiner Antwort 1 (2026-09-18) — beide Blocker treffen zu

Die Pruefung hat zwei Praemissen meiner Antwort widerlegt, und zwar messbar. Beide Korrekturen
uebernehme ich ohne Einschraenkung.

### Zu B1 — „Artefakt der falschen Quelle" war falsch. Der Nicht-Treffer ist echt.

Mein Schluss war: Falsche Quellspalte → falsche Token → deshalb kein Match. Der Reviewer zeigt,
dass das **unabhaengig von der Quelle** nicht aufgeht: Der `WorkSteps`-Katalog enthaelt heute nur
**fuenf AKE-Vorfertigungs-Codes** (`VA/VE/VK/VL/VT`) mit **beschreibenden** `SearchString`s
(„Verdampfer", „Luefter"). Kurze Operations-Kuerzel wie `KA`/`LS`/`AV` koennen dagegen **nie**
matchen, gleich aus welcher Spalte sie stammen.
**Der Katalog fehlt schlicht** — das ist der Befund, und er ist fachlich, nicht technisch.

**Und meine Abfrage war nicht ausfuehrbar:** `FaHierarchyNode` existiert in keiner Datenbank, die
der Lauf erreicht. Die Tabelle liegt auf der **befuellten IDEAL-Testinstanz**. Ich habe eine
Messung vorgeschlagen, die nur **der Mensch** ausfuehren kann — das gehoert benannt, nicht dem Lauf
als Aufgabe gegeben.

**Damit verbleibt Rueckfrage 1 offen, in zwei Teilen:**
- **(a)** Token-Liste per `STRING_SPLIT(Arbeitsschritte, ' ')` gegen `FaHierarchyNode` auf der
  befuellten Instanz erheben — **durch den Menschen**.
- **(b)** Je Token festlegen, welcher `WorkStep`-`Code`/`Name` im Katalog angelegt wird —
  **fachliche Entscheidung**, nicht ableitbar.

### Zu B2 — „nur das Trennzeichen unterscheidet sich" war falsch.

Die Messung nennt **fuenf** Unterschiede, nicht einen: Quelle, Match-Mechanismus
(Contains-Heuristik vs. exakt), Match-Feld (`SearchString` vs. `Code`), Scope (Artikel vs.
`DirectChildren`) und Zielaufloesung (`ArticleNumber` vs. `SubOrderNumber`). Ein
Trennzeichen-Parameter deckt davon **eine** ab.

**Damit stuetzt die Messung die urspruengliche Spec-Entscheidung: eigener Service.**
Mein ponytail-Einwand („Sprosse 2 — gibt es das schon?") war richtig gestellt, aber falsch
beantwortet: Die Leiter verlangt, **vorher den Code zu lesen**. Haette ich das getan, waeren die
fuenf Unterschiede sichtbar gewesen. *Faul in der Loesung, nie im Lesen* — genau die Regel, die ich
selbst in `CLAUDE.md` geschrieben habe.

**ENTSCHEIDUNG: eigener `FaWorkStepStructureDetectionService`, wie in der Spec entworfen.**
Der parametrisierte Weg faende den **live laufenden AKE-Pfad** an — fuenf Verzweigungen in einem
produktiv genutzten Dienst, fuer eine Ersparnis, die es nicht gibt. Das Regressionsrisiko waere
real, der Gewinn eingebildet.

### Zu den drei SOLLTE-Punkten und den Hinweisen — uebernommen.

Katalog-Formulierung praezisieren, Gate-Helfer konsistent als `…SafeAsync`, TS-76.2
widerspruchsfrei neu fassen; dazu die Hinweise (Umlaut-Token `SÄ`, Doppel-Kontext-Annahme,
Manual-UAT-Markierung).

**Die Spec bleibt vorlaeufig `Entwurf`.** — **UEBERHOLT durch den Abschnitt „AUFLOESUNG von B1"
weiter unten (2026-09-18).** Diese Einschaetzung entstand, bevor geklaert war, dass IDEAL die
Arbeitsschritte **geliefert bekommt** statt sie zu suchen. Massgeblich ist die Aufloesung, nicht
dieser Absatz.

## AUFLOESUNG von B1 (2026-09-18) — IDEAL SUCHT NICHT, IDEAL BEKOMMT GELIEFERT

**Der Kern, der beiden Blockern zugrunde lag:** Bei **AKE** muss die Anwendung **raten** — sie
durchsucht Bezeichnungen per `SearchString`-Heuristik („Verdampfer", „Luefter"), weil Sage dort
keine Arbeitsschritte mitliefert. Bei **IDEAL liefert die View die Arbeitsschritte fertig mit.**
Es ist nichts zu suchen und nichts zu erraten.

**Damit war der Abgleich gegen `SearchString` von Anfang an die falsche Vorstellung** — er ist der
AKE-Mechanismus und hat in IDEAL keine Aufgabe. Der Befund „0 von 13 matchen den Katalog" misst
etwas, das im IDEAL-Pfad gar nicht stattfindet.

**Folgen:**
- **B1 ist aufgeloest und blockiert nicht.** Die Token-Abfrage ist keine Vorbedingung mehr;
  sie dient hoechstens der Bestaetigung der Liste.
- **B2 wird zusaetzlich gestuetzt:** Wenn IDEAL gar nicht sucht, unterscheiden sich die beiden
  Wege noch deutlicher als die fuenf gemessenen Dimensionen nahelegen. **Eigener Service bleibt
  richtig** — unveraendert.
- **Kollision mit den AKE-Codes:** ausdruecklich **kein Thema**. `VA/VE/VK/VL/VT` bleiben
  unberuehrt; die IDEAL-Token sind davon verschieden. Beim Matching der IDEAL-Seite werden
  AKE-Codes **ignoriert**, nicht abgeglichen.
- **Was bleibt, ist reines Anlegen:** Die Token brauchen `WorkStep`-Eintraege mit Klartextnamen,
  damit die Pflicht-Fremdschluesselzuordnung greift. Das ist Stammdatenpflege, keine
  Zuordnungsarbeit — und es faellt unter die bestaetigte Regel **MELDEN statt automatisch anlegen**:
  Der Sync meldet unbekannte Token als Sammelmeldung, ein Mensch pflegt sie einmal nach.

**Rueckfrage 1 ist damit geschlossen.** Die Spec ist freigabereif.

## Kritische Pruefung (2026-09-16)

> Anwalt-des-Teufels-Durchsicht **vor** dem Dev-Lauf. Wo moeglich habe ich Behauptungen an der
> echten DB (`AKESQL20.ake.at`) **gemessen**, statt sie zu glauben. Die Freigabe-Antwort schliesst
> Rueckfrage 1 **nicht sauber** — sie ersetzt sie durch zwei Muss-Klaerungen (eine Messung, die ich
> nicht reproduzieren kann, und eine Architektur-Prämisse, die ich widerlegen konnte). Beide sind
> vor der Freigabe zu klaeren.

### BLOCKER

**B1 — Die maßgebliche Abfrage ist aus keiner erreichbaren DB reproduzierbar; die „0/13 =
Artefakt"-Vermutung ist gegenmessbar falsch.**
Die Freigabe-Antwort erklaert den Blocker fuer entfallen, weil die maßgebliche Quelle
`FaHierarchyNode.Arbeitsschritte` „bereits in der WMS-Datenbank" liege (533 Knoten). **Gemessen:**
Die Tabelle `FaHierarchyNode` existiert in **keiner** von dieser Session erreichbaren Datenbank
(`IDEAL_AKE_WMS`, `IDEAL_AKE_WMS_Test`, `IDEALAKEWMSIDEAL`, `IDEAL_Test` — `OBJECT_ID` jeweils NULL;
die Migration ist dort nicht angewandt). Die vorgeschlagene `STRING_SPLIT`-Abfrage gegen
`FaHierarchyNode` kann daher **weder ich noch der automatische Dev-Lauf** ausfuehren — sie laeuft nur
auf der Instanz, auf der die 533 Knoten liegen (offenbar eine lokale/andere Instanz des Menschen).
Zweitens spricht die **Messung gegen die Artefakt-These:** Der produktive `WorkSteps`-Katalog
enthaelt aktuell ausschliesslich fuenf **AKE-Vorfertigungs**-Codes mit *beschreibenden* SearchStrings —
`VA` (Aufbau, Spange, Rahmen), `VE` (Steuerung), `VK` (Verdampfer, Maschinenfach), `VL` (Lüfter),
`VT` (Tür). Kurze Operations-Codes wie `KA`/`LS`/`AV`/`SÄ` koennen dagegen **unabhaengig von der
Quellspalte** nicht matchen — es fehlt schlicht der Katalog. Der Nicht-Treffer ist also **real**, kein
Quell-Artefakt.
→ **Frage an den Menschen:** Bitte die `FaHierarchyNode`-Abfrage auf der befuellten Instanz selbst
ausfuehren und (a) die verbindliche Token-Liste hier eintragen sowie (b) je Token den anzulegenden
`WorkStep.Code`/`Name` festlegen. Der Dev-Lauf kann das nicht selbst erheben.

**B2 — Die Design-Prämisse „nur das Trennzeichen unterscheidet sich" ist widerlegt; die Wahl
eigener Service vs. parametrisierter Bestandsservice muss auf dieser Faktenlage **bewusst**
bestaetigt werden (der Mensch hat „melden statt still bauen" ausdruecklich verlangt).**
Die Freigabe-Antwort vermutet, AKE und IDEAL lieferten „dieselben Arbeitsschritte, nur mit anderem
Trennzeichen … gleicher Katalog", und fragt, ob der bestehende `FaWorkStepDetectionService` mit einem
**Trennzeichen-Parameter** genuegt. **Gemessen + am Code verifiziert:** Die beiden Ableitungen
unterscheiden sich in **fuenf** Dimensionen, nicht in einer —
1. **Quelle:** `CachedBomItems.Bezeichnung1/2` (AKE) vs. `FaHierarchyNode.Arbeitsschritte` (IDEAL);
2. **Match-Mechanismus:** unscharfes `SearchString.Contains(term)` (AKE, `FaWorkStepDetectionService.cs:62-67`)
   vs. **exakter** `WorkStep.Code == token` (IDEAL);
3. **Match-Feld:** `SearchString` (kommasepariert, beschreibende Wortlisten wie „Verdampfer,
   Maschinenfach") vs. `Code` (Operations-Kuerzel);
4. **Scope:** artikelbasiert ueber `ArticleNumber` (AKE) vs. hierarchisches `DirectChildren` ueber
   `SubOrderNumber` (IDEAL);
5. **Zielaufloesung:** `stepMatchedArticles.Contains(o.ArticleNumber)` (AKE) vs.
   `keys.Contains(o.SubOrderNumber)` (IDEAL).
Ein „Trennzeichen-Parameter" deckt davon **eine** Dimension ab. Die Prämisse traegt also nicht — und
die Messung **stuetzt** die Spec-Entscheidung „eigener Service" (Design A/B). Zusaetzlich: Ein
gemeinsamer, parametrisierter Service muesste in **jedem** dieser Verzweigungspunkte umschalten und
wuerde den **produktiv laufenden AKE-Pfad** anfassen — das erhoeht das Regressionsrisiko auf einer
Live-Funktion deutlich, gegen das AK 1/2/7 dann nur unvollstaendig schuetzen.
→ **Frage an den Menschen (Entweder/Oder, vor der Freigabe zu setzen):** Genuegt Ihnen angesichts
dieser fuenf Unterschiede die Spec-Entscheidung **eigener Service**? Oder wollen Sie trotzdem den
**einen parametrisierten Service** (mit der zusaetzlichen internen Verzweigung und dem Eingriff in den
AKE-Pfad)? Solange das offen ist, darf der Dev-Lauf die Architektur nicht waehlen.

### SOLLTE

**S1 — „Gleicher Katalog" praezisieren.** Die Formulierung „der Katalog wird geteilt, nicht
gedoppelt" ist richtig gemeint, aber missverstehbar: Es bleibt **eine** `WorkSteps`-Tabelle, die um
~13 IDEAL-Codes **erweitert** wird; die fuenf AKE-Codes dienen **nicht** zugleich IDEAL. Vorschlag:
Im Abschnitt „Fachliche Anforderungen" bzw. Out-of-Scope explizit „ein Katalog, additiv um die
IDEAL-Codes ergaenzt" schreiben, damit niemand die AKE-Codes umzudeuten versucht.

**S2 — Gate-Helfer konsistent waehlen.** Design C nutzt `GetBoolSafeAsync` fuer den Master, aber
`GetBoolAsync` fuer den neuen Toggle. Vorschlag: fuer beide die `…SafeAsync`-Variante, damit ein
(noch) fehlender `ServiceSettings`-Key den SyncWorker nicht wirft, bevor der Drift-Guard-Seed
durchgelaufen ist. Im Dev-Lauf gegen das echte `SyncWorker`-Muster verifizieren.

**S3 — TS-76.2 sauber neu fassen.** Das Szenario widerspricht sich im Text („fachlich unmoeglich bei
einem Blatt, daher stattdessen …"). Vorschlag: nur der klare Fall — Sub-FA X → direktes Kind Y (selbst
Sub-FA) → Enkel Z mit Token `SÄ`, das **nur** auf Z steht; Erwartung: `SÄ` bei Y, nicht bei X. Den
verworfenen Halbsatz streichen.

### HINWEIS

**H1 — Umlaut-Token `SÄ`.** Ein Katalog-`Code` muss das Token **zeichengleich** treffen. Bei
`OrdinalIgnoreCase` matcht `SÄ` nur `SÄ`/`sä`, **nicht** `SAE`. Beim Anlegen der Katalogeintraege (B1)
auf exakte Umlaut-Schreibweise achten; ggf. als Testfall in TS-76 aufnehmen.

**H2 — Nebenbefund Doppel-Kontext (Design E)** ist plausibel dokumentiert, bleibt aber eine
**unbewiesene** fachliche Annahme (dieselbe `Arbeitsschritte`-Zeile zaehlt im Eltern- und im
Eigen-Scope). Beim ersten echten Datenlauf mit dem Fachbereich gegenpruefen, ob das gewollt ist —
nicht erst im Betrieb auffallen lassen.

**H3 — Reproduzierbarkeit generell.** Weil `FaHierarchyNode` in keiner erreichbaren DB liegt, sind
alle datenabhaengigen Akzeptanzkriterien (AK 3/5/8) reine **Manual-UAT** auf der befuellten Instanz —
das ist konsistent mit der „nicht InMemory-testbar"-Regel, sollte aber im Testabschnitt als solches
markiert sein, damit der qa-agent nicht auf gruene Automated-Tests als Beweis verweist.

### Empfehlung

**NACHBESSERUNG NOETIG:** Rueckfrage 1 ist nicht geschlossen, sondern in zwei offene Muss-Klaerungen
verwandelt (B1: maßgebliche Token-Messung nur auf der befuellten Instanz moeglich + Katalog-Zuordnung;
B2: Architektur-Entscheidung eigener vs. parametrisierter Service auf Basis der gemessenen fuenf
Unterschiede bewusst bestaetigen).

### Nachtrag — zweite Durchsicht (2026-09-16)

Zwei Aenderungen gegenueber der ersten Durchsicht oben:

**B2 geschlossen (durch Aussage des Menschen).** In der zweiten `/review`-Runde hat der Mensch
ausdruecklich bestaetigt: „Der ganze `SearchString`-Abgleich ist der AKE-Mechanismus und hat im
IDEAL-Pfad nichts zu suchen, weil die View die Arbeitsschritte mitliefert." Damit ist die
Prämisse „nur das Trennzeichen unterscheidet sich" verworfen und die Spec-Entscheidung **eigener
Service mit Exakt-`Code`-Matching** bestaetigt. B2 ist **kein** offener Blocker mehr. (Formaler
Vollzug erst mit `freigabe_entscheidung`/Freigabe-Antworten durch den Menschen — der Review-Abschnitt
darf dort nicht hineinschreiben.)

**S1–S3 in den Rumpf gezogen** (waren zuvor nur im Pruef-Abschnitt): S1 → „Ein Katalog, additiv
erweitert" in Fachliche Anforderungen 4; S2 → beide Gates in Design C auf `GetBoolSafeAsync`
vereinheitlicht (Konvention des Nachbar-Blocks `SyncWorker.cs:357`, verifiziert) + Begruendungssatz;
S3 → TS-76.2 widerspruchsfrei neu gefasst. Die Hinweise H1–H3 bleiben bewusst nur im Pruef-Abschnitt
(Beobachtungen ohne Rumpf-Relevanz; H1 als moeglicher Testfall bei der Katalog-Pflege).

**Zur Prozessfrage des Menschen** („nachziehen oder dem Lauf als Schritt 0 mitgeben?"): nachgezogen —
die drei SOLLTE sind reine Spec-Klarheit, kein Implementierungsschritt. Sie gehoeren in den Rumpf,
damit der Dev-Lauf gegen einen selbstkonsistenten Text baut und **nicht** Review-Prosa als „Schritt 0"
neu interpretieren muss (fragil, vermischt Spec-Autorschaft mit Umsetzung). Der Dev-Lauf startet damit
sauber bei der Umsetzung.

**Revidierte Empfehlung:** **NACHBESSERUNG NOETIG — nur noch B1.** Es bleibt die eine echte
Muss-Klaerung: die maßgebliche Token-Liste auf der befuellten `FaHierarchyNode`-Instanz erheben
(die Abfrage ist aus keiner von hier erreichbaren DB reproduzierbar) **und** je Token den
anzulegenden `WorkStep.Code`/`Name` festlegen. Sobald diese Liste im Freigabe-Block steht, ist die
Spec bereit.
