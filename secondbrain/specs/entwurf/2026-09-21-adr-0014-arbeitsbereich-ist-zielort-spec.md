---
type: spec
title: "Rückbau: Werkbank-Ableitung aus dem Arbeitsbereich (ADR 0014 auf falsches Feld angewandt)"
slug: 2026-09-21-adr-0014-arbeitsbereich-ist-zielort-spec
status: Entwurf
created: 2026-09-21
updated: 2026-09-21
source_backlog: "[[2026-09-21-adr-0014-arbeitsbereich-ist-zielort]]"
depends_on: ""
task: ""
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
affected_code:
  - "IDEALAKEWMSService/Services/FaMaterializationSyncService.cs — Klassenkommentar (Z.12-33) auf EINE Z1-Ausnahme (HasCoatingParts/F5) reduzieren, Workplace-Absatz entfernen; Werkbank-Lookup-Block (Z.152-163); Aufrufe von ApplyWorkplace im Anlege-Pfad (Z.182-184) und Update-Pfad (Z.207-209); private Methode ApplyWorkplace + Hilfstyp WorkplaceRef (Z.346-379) komplett entfernen; Melde-/Mail-Block SendUnknownWorkplaceDigestAsync (Z.329-344) entfernen; Sammelmeldungen 'Unbekannte Arbeitsbereiche'/'mehrdeutig' (Z.228-239) entfernen; Counts-Keys werkbank_gesetzt/werkbank_abweichend/arbeitsbereich_unbekannt/arbeitsbereich_mehrdeutig an ALLEN drei FinishSuccessAsync-Aufrufen (Skip-Zweig Z.105-112, DryRun-Zweig Z.124-134, Erfolgs-Zweig Z.286-298) sowie im Log-Statement (Z.278-284) entfernen; Ctor-Parameter IUnknownWorkplaceState (Feld Z.41, Ctor-Parameter+Zuweisung Z.50/58) entfernen"
  - "IDEALAKEWMSService/Services/FaMaterializationPlanner.cs — MaterializationSourceOrder-Record verliert das Feld Arbeitsbereich (nach dem Rückbau nirgends mehr gelesen, verifiziert per Grep — YAGNI/ponytail); den einen Erzeugungsaufruf in FaMaterializationSyncService.cs (Z.78-86) entsprechend kürzen"
  - "IDEALAKEWMSService/Common/IUnknownWorkplaceState.cs — Datei komplett löschen (Interface + Implementierung UnknownWorkplaceState), nach dem Rückbau verwaist (Grep bestätigt: einzige Nutzer waren FaMaterializationSyncService, Program.cs, der eigene Test)"
  - "IDEALAKEWMSService/Program.cs — DI-Registrierung Z.51-52 (IUnknownWorkplaceState -> UnknownWorkplaceState) entfernen. NICHT verwechseln mit der bleibenden Registrierung IUnknownWorkStepTokenState (Z.53-54, gehört zur BDE-Spec/Arbeitsschritt-Erkennung, unberührt)"
  - "IDEALAKEWMSService.Tests/Services/FaMaterializationWorkplaceTests.cs — Datei komplett löschen (testet ausschließlich die jetzt entfernte Werkbank-aus-Arbeitsbereich-Ableitung)"
  - "IDEALAKEWMSService.Tests/Services/FaMaterializationSyncServiceTests.cs, FaMaterializationPlannerTests.cs — im Dev-Lauf gegen die geänderte MaterializationSourceOrder-Signatur prüfen (Arbeitsbereich-Argument entfällt an allen Konstruktionsstellen); FaMaterializationCoatingTests.cs + FaMaterializationCoatingWriteTests.cs bleiben unverändert (unabhängige Test-Dateien, Coating-Pfad nicht betroffen — Regressionsnachweis)"
  - "KEINE Code-Änderung in Views/ProductionOrders/Index.cshtml + _ProductionOrderRow.cshtml, Views/FaWorklist/Index.cshtml + _FaWorklistRow.cshtml, Views/FaCompletion/Index.cshtml + _FaCompletionRow.cshtml, Views/PickingLeitstand/Index.cshtml + _PickingLeitstandRow.cshtml — verifiziert: die Spalte 'Werkbank' (data-col-key=\"workbench\") ist eine seit vor v1.37 bestehende, AKE UND IDEAL gemeinsame Spalte (echte AKE-Werkbankzuweisung). Der Rückbau lässt sie unangetastet; sie zeigt für IDEAL-Sub-FAs nach dem Rückbau wieder denselben leeren Zustand wie vor der (nie produktiven) v1.37-Ableitung"
  - "docs/TESTSZENARIEN.md — Kapitel 'IDEAL — Materialisierung: Fachliche Felder (K1/K2/K3)' (aus [[2026-08-20-materialisierung-fachliche-felder-spec]]) anpassen: Szenarien 'Werkbank-Datenhoheit (Variante B)' und 'Unbekannter Arbeitsbereich' als 'zurückgebaut, siehe [[2026-09-21-adr-0014-arbeitsbereich-ist-zielort-spec]]' kennzeichnen statt löschen (Nachvollziehbarkeit), neues Rückbau-Verifikationsszenario ergänzen (siehe Test-Szenarien unten). Kein neues TS-Kapitel nötig — das betroffene Feature war nie produktiv"
  - "secondbrain/tests/testszenarien-index.md — Hauptcheckout, additive Ergänzung"
open_questions:
  - "Grundsatzfrage: Braucht ein IDEAL-Auftrag überhaupt EINE Werkbank am Auftrag (ProductionOrder.ProductionWorkplaceId), oder ergibt sich die Werkbank ausschließlich je Arbeitsgang (WorkOperation, siehe BDE-Spec)? Falls Letzteres: das Feld bleibt bei IDEAL dauerhaft null."
  - "Wohin gehört der Arbeitsbereich stattdessen? Eigene Anzeige-Spalte 'Zielort'? Verhältnis zum Kommissionierziel (FaHierarchyNode.Kommissionieren) — NICHT als dasselbe Feld annehmen, beide sind in der Struktur getrennt geführt."
  - "Was wird aus ADR 0014? Das Datenhoheits-Muster (Sage führend, unscharfe Abweichungsmeldung, Umschaltpunkt Variante C) bleibt als Muster richtig, war nur auf das falsche Feld angewandt — additiver Nachtrag an ADR 0014 oder neue, supersedierende ADR?"
epic: false
etappen: []
deploy:
  web: true
  service: true
  migration: false
freigabe_entscheidung: ""
freigabe_von: ""
freigabe_am: ""
# Flache Schluessel mit Absicht: Obsidians Property-Editor kann verschachtelte
# YAML-Objekte NICHT bearbeiten - und genau diesen Block fuellt der Mensch aus.
---

## Ziel / Nutzen (das Warum)

Am 2026-09-21 hat der Mensch klargestellt: **Arbeitsbereich** (`K-02`, `S-01`, `H4-04`, Feld
`FaHierarchyNode.Arbeitsbereich`, Sage-Quelle `USER_OSAbteilung`) ist ein **Zielort** — wohin ein
Bauteil kommt, begrifflich aus Lagerorten abgeleitet — und **keine Werkbank**. Werkbank ist der
Sage-Arbeitsplatz (`KHKPpsArbeitsplaetze`, z. B. `2300`/„Kanterei W1"), siehe
[[2026-09-21-ideal-bde-arbeitsgaenge-aus-struktur-spec]] (das richtige Werkbank-Modell: eine
Werkbank je Arbeitsgang, aus einem eigenen Sage-Vokabular `USER_ArbeitsSchritt`).

Die im August entstandene Annahme „Werkbank = Arbeitsbereich" — ausdrücklich als vorläufig
formuliert, weil die Sage-Arbeitsplatz-Stammdaten damals noch fehlten — ist damit **widerlegt**. Sie
steckt in [[2026-08-20-materialisierung-fachliche-felder-spec]] (v1.37.0, Status Testbereit, im
selben, **ungemergten** Bündel-Worktree) und in der begleitenden ADR
[[0014-werkbank-datenhoheit-sage-fuehrend-mit-abweichungsmeldung]]. Diese Spec baut die Ableitung
zurück, **bevor** sie je produktiv wird und **bevor** die echten IDEAL-Werkbänke angelegt werden
(Voraussetzung der BDE-Spec) — sonst entsteht entweder Dauerrauschen („jeder Arbeitsbereich ist eine
unbekannte Werkbank") oder stille Fehlzuweisungen (ein Zielort landet im Werkbank-Feld). Die Korrektur
ist rein Code, ohne produktive Auswirkung — der billigstmögliche Zeitpunkt.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**

- Den kompletten Werkbank-aus-Arbeitsbereich-Ableitungsblock aus `FaMaterializationSyncService`
  entfernen: Lookup, Zuweisung (`ApplyWorkplace`), Sammelmeldung + Sammelmail für unbekannte/
  mehrdeutige Arbeitsbereiche, zugehörige Counts-Keys, den jetzt verwaisten `IUnknownWorkplaceState`.
- Den Klassenkommentar korrigieren: von **zwei** Z1-Ausnahmen (Workplace, HasCoatingParts) auf
  **eine** (HasCoatingParts/F5, Beschichtung — bleibt unverändert bestehen).
- Tote Felder/Parameter, die nur der entfernten Ableitung dienten, mit entfernen
  (`MaterializationSourceOrder.Arbeitsbereich`), damit kein unbenutzter Rest zurückbleibt.
- Testszenarien-Kapitel und Testdateien entsprechend bereinigen bzw. kennzeichnen.
- Brain-Pflichten: ADR-Nachtrag/-Supersede-Mechanismus für ADR 0014 (Weg vorschlagen, Redaktion durch
  den Menschen — siehe Offene Rückfrage 3), Hinweis in der freigegebenen v1.37.0-Spec, Fallstricke-
  Eintrag, feature-map/codebase-Nachzug.

**Out-of-Scope**

- **Kein Neubau eines Werkbank-Modells für IDEAL.** Das leistet
  [[2026-09-21-ideal-bde-arbeitsgaenge-aus-struktur-spec]] (Baustein a: `ProductionWorkplace.
  ArbeitsschrittCode` aus Sage-Arbeitsplatz-Stammdaten). Diese Spec **entfernt** nur die falsche
  Ableitung, sie ersetzt sie nicht durch eine neue.
- **Kein neues „Zielort"-Feld/keine neue Spalte für `Arbeitsbereich`.** Das hängt an Offener
  Rückfrage 2 und ist, falls gewünscht, eine eigene Folge-Spec.
- **`FaHierarchyNode.Arbeitsbereich` selbst bleibt unverändert** (Struktur-Cache-Feld aus Teil 1,
  weiterhin von der FA-Hierarchie-Synchronisation befüllt) — nur die Weiterverarbeitung in
  `ProductionOrder.ProductionWorkplaceId` entfällt.
- **`ProductionOrder.ProductionWorkplaceId` als Spalte bleibt** (Fremdschlüssel existiert bereits seit
  vor Teil 7/AKE) — sie wird nur nicht mehr aus dem Arbeitsbereich befüllt. Für IDEAL bleibt sie nach
  dieser Spec durchgängig `null`, bis eine künftige Spec (BDE-Baustein b oder eine Antwort auf
  Rückfrage 1) sie aus einer korrekten Quelle füllt.
- **AKE** — der gesamte Materialisierungspfad läuft ausschließlich bei Master
  `ProduktionsauftragHierarchisch = true` (`SyncWorker.cs:357`); `ApplyWorkplace` ist eine private,
  nur dort aufgerufene Methode. AKEs eigene, von Menschen gepflegte Werkbank-Zuweisung ist von dieser
  Spec nicht berührt — bestätigt, nicht mehr offen (siehe Fachliche Anforderungen).
- **Coating-Ableitung (`HasCoatingParts`, F5/F6) bleibt vollständig unverändert** — andere Z1-
  Ausnahme, anderer Code-Pfad, eigene Testdateien.

## Fachliche Anforderungen

1. **Arbeitsbereich ist Zielort, nicht Werkbank** (Fakt, siehe ADR-Notiz
   [[2026-09-21-adr-0014-arbeitsbereich-ist-zielort]]) — Grundlage dieser Korrektur, nicht erneut zur
   Diskussion.
2. **AKE-Bestätigung (Backlog-Rückfrage 4, verifiziert, nicht mehr offen):** `RunAsync` von
   `FaMaterializationSyncService` läuft ausschließlich über das Master-Gate
   `ProduktionsauftragHierarchisch = true` (`SyncWorker.cs:357`). `ApplyWorkplace` ist eine private
   Methode dieser Klasse, ausschließlich aus dem Anlege- und dem Update-Pfad dieses Services
   aufgerufen. AKE nutzt für seine Werkbank-Zuweisung einen anderen, unveränderten Pfad. Der Rückbau
   hat **keine** AKE-Auswirkung.
3. **Präzisierung der Z1-Ausnahmen-Zählung (Backlog-Formulierung war vertauscht):** Der
   Klassenkommentar nennt zwei Ausnahmen von der „App-Felder werden nie überschrieben"-Regel: **(1)
   `ProductionWorkplaceId`** (Variante B, die hier zurückgebaute Ableitung — die ERSTE, nicht die
   zweite Ausnahme) und **(2) `HasCoatingParts`** (F5, Beschichtung, bleibt). Nach dem Rückbau bleibt
   genau **eine** Z1-Ausnahme: Coating.
4. **Kein Backfill/keine Bereinigung bereits geschriebener Werte.** Der Rückbau ist reiner Code, keine
   Migration. Sollte im Worktree/auf einer Testinstanz durch einen früheren Testlauf der (nie
   produktiven) v1.37.0-Ableitung bereits ein `ProductionWorkplaceId` aus einem Arbeitsbereich gesetzt
   worden sein, bleibt dieser Wert bestehen — er wird nach dem Rückbau nicht mehr weiter überschrieben,
   aber auch nicht automatisch zurückgesetzt. Da nichts davon produktiv ist, ist das folgenlos; im
   Dev-Lauf ggf. auf der eigenen Testinstanz manuell bereinigen, falls störend.

## Technischer Lösungsentwurf

Referenzmuster: der Rückbau folgt keinem neuen Muster, sondern entfernt einen bestehenden Block
vollständig, unter Beibehaltung der übrigen `FaMaterializationSyncService`-Struktur (Aktivitäts-
Protokoll `ISyncLogger`, ADR 0010; Z1-Regel, jetzt auf eine Ausnahme reduziert).

- **`FaMaterializationSyncService.RunAsync`**: Werkbank-Lookup-Block (Gruppierung der
  `ProductionWorkplace`-Namen), beide `ApplyWorkplace`-Aufrufe (Anlege- und Update-Pfad), die
  Zähler-Variablen `workplaceChanges`/`workplaceDeviations`/`werkbankGesetzt`/`werkbankAbweichend`
  sowie `unknownWorkplaceNames`/`ambiguousWorkplaceNames` und deren Meldungen entfernen. Die übrigen
  Blöcke (Umhäng-Konflikte, `SageMissingSince`, `EnsureStatusRowsAsync`, Coating-Zuweisung über
  `SetCoatingPartsAsync`, `SendMissingDigestAsync`) bleiben unverändert an ihrer Stelle.
- **Private Methode `ApplyWorkplace` + Record `WorkplaceRef`**: komplett löschen.
- **`SendUnknownWorkplaceDigestAsync`**: komplett löschen (kein weiterer Aufrufer nach dem Rückbau).
- **`FinishSuccessAsync`-Dictionaries** (Skip-, DryRun- und Erfolgs-Zweig): die vier Werkbank-/
  Arbeitsbereich-Counts-Keys überall gemeinsam entfernen, damit alle drei Stellen weiterhin identische
  Schlüsselmengen liefern (bestehendes Konsistenzmuster der Klasse).
- **Klassenkommentar** (Zeilen 12-33): den `<see cref="ProductionOrder.ProductionWorkplaceId"/>`-
  Absatz entfernen, den verbleibenden `HasCoatingParts`-Absatz von „AUSNAHMEN sind genau zwei (Z1)"
  auf „genau eine (Z1)" umschreiben.
- **`FaMaterializationPlanner.MaterializationSourceOrder`**: Feld `Arbeitsbereich` entfernen (nach dem
  Rückbau ungenutzt — verifiziert per Grep, kein zweiter Konsument); die Erzeugung in `RunAsync`
  entsprechend kürzen. Ponytail-Sprosse „Deletion over Addition": ein totes Feld auf einem sonst
  reinen, unit-getesteten Planner-Record ist unnötige Angriffsfläche für den nächsten Leser.
- **`IUnknownWorkplaceState`/`UnknownWorkplaceState`**: Datei löschen, DI-Registrierung in
  `Program.cs` löschen. Explizit **nicht** verwechseln mit `IUnknownWorkStepTokenState`
  (BDE-Spec, anderer Zustand für Arbeitsschritt-Token) — dieser bleibt unangetastet.
- **Views**: keine Code-Änderung. Die Spalte „Werkbank" (`data-col-key="workbench"`) in den vier
  Kandidaten-Listen ist eine geteilte AKE/IDEAL-Spalte, die es bereits vor v1.37.0 gab (echte
  AKE-Werkbankzuweisung). Sie bleibt bestehen; für IDEAL-Sub-FAs zeigt sie nach dem Rückbau wieder
  denselben leeren Zustand wie vor der (nie produktiven) v1.37.0-Ableitung. Eine eigene „Zielort"-
  Spalte ist Gegenstand von Offener Rückfrage 2, nicht dieser Spec.

## Migrations-/SQL-Auswirkungen

**Keine Migration.** Der Rückbau ist reiner Code:
- `ProductionOrder.ProductionWorkplaceId` (Spalte) bleibt unverändert bestehen — sie wird nur nicht
  mehr aus dem Arbeitsbereich befüllt.
- `FaHierarchyNode.Arbeitsbereich` (Spalte, Struktur-Cache) bleibt unverändert bestehen und weiterhin
  von der FA-Hierarchie-Synchronisation befüllt.
- Keine neue, geänderte oder gelöschte Tabelle/Spalte. Kein `dotnet ef migrations add`, kein
  `SQL/XX_*.sql`, keine `SQL/00_FreshInstall.sql`-Anpassung.

## Audit-Feld-Auswirkungen

Entfällt im engeren Sinn — der Rückbau ändert keine Entitäts-Felder, sondern entfernt nur eine
Schreiblogik. `ProductionOrder` bleibt `AuditableEntity`; da `ApplyWorkplace` nach dem Rückbau nicht
mehr aufgerufen wird, setzt der Materialisierungs-Sync für dieses Feld auch keine
`ModifiedAt/ModifiedBy/ModifiedByWindows` mehr — das ist beabsichtigt (kein Schreibvorgang mehr, also
kein Audit-Ereignis). Die übrigen, unveränderten Schreibvorgänge des Services (Quantity, ArticleNumber,
Description1/2, Coating-Flag) stempeln Audit-Felder weiterhin exakt wie zuvor.

## Betroffene Rollen / Zugriffsfilter

Keine Änderung. Der Rückbau betrifft ausschließlich einen Hintergrund-Sync (Windows-Service, kein
Web-Endpoint) und einen internen Zustand (`IUnknownWorkplaceState`). Kein Controller, keine
`RequireXxxAccess`-Filter, keine Rolle ist berührt.

## Listen-View-Pattern-Pflichten (ADR 0005)

Keine neue oder geänderte Tabellen-Ansicht. Die vier betroffenen Listen (`/ProductionOrders`,
`/FaWorklist`, `/FaCompletion`, `/PickingLeitstand`) behalten ihre bestehende, ADR-0005-konforme
„Werkbank"-Spalte (Server-Spaltenfilter, `data-col-key="workbench"`) unverändert — diese Spec ändert
nur, welche Daten (für IDEAL: keine) in die bereits vorhandene Spalte einlaufen.

## Akzeptanzkriterien

1. **Kein Ableitungs-Aufruf mehr.** Code-Review/Diff zeigt: `ApplyWorkplace` existiert nicht mehr in
   `FaMaterializationSyncService`; weder Anlege- noch Update-Pfad rufen eine Werkbank-aus-Arbeitsbereich-
   Zuweisung auf.
2. **Keine Meldung/Mail mehr.** Ein Materialisierungslauf mit einem `Arbeitsbereich`-Wert ohne
   passenden `ProductionWorkplace`-Stammsatz erzeugt weder einen SyncLog-Warnungseintrag „Unbekannte
   Arbeitsbereiche …" noch eine Sammelmail; `SendUnknownWorkplaceDigestAsync` existiert nicht mehr.
3. **Coating-Ableitung unverändert funktionsfähig.** `FaMaterializationCoatingTests` und
   `FaMaterializationCoatingWriteTests` laufen unverändert grün; ein Materialisierungslauf setzt
   `PickingStatus.HasCoatingParts` weiterhin korrekt aus `FaHierarchyNode.Beschichtet` ab (Regressions-
   nachweis, F5/F6 unverändert).
4. **AKE unberührt.** Bei Master `false` läuft weiterhin kein Aufruf der Materialisierung
   (unverändertes Gate `SyncWorker.cs:357`); AKEs eigener Werkbank-Zuweisungsweg ist nicht Teil dieser
   Klasse und bleibt unverändert.
5. **Klassenkommentar korrekt.** Der Kommentar von `FaMaterializationSyncService` nennt nach dem
   Rückbau genau **eine** Z1-Ausnahme (`HasCoatingParts`), keine mehr zu `ProductionWorkplaceId`.
6. **`IUnknownWorkplaceState` sauber entfernt.** Grep über das gesamte Repository findet nach dem
   Rückbau keine Referenz mehr auf `IUnknownWorkplaceState`/`UnknownWorkplaceState` (Interface, Datei,
   DI-Registrierung, Test) — `IUnknownWorkStepTokenState` bleibt dabei unverändert vorhanden.
7. **Keine Migration.** Diff enthält keine neue Datei unter `Migrations/` oder `SQL/`, keine Änderung
   an `SQL/00_FreshInstall.sql`.
8. **Build + Tests grün.** `dotnet build IdealAkeWms.slnx` und `dotnet test` (insbesondere
   `IDEALAKEWMSService.Tests`) laufen fehlerfrei; `FaMaterializationWorkplaceTests.cs` existiert nicht
   mehr, keine verwaiste Referenz auf die entfernte `Arbeitsbereich`-Eigenschaft von
   `MaterializationSourceOrder` in verbleibenden Tests.
9. **Views unverändert lauffähig.** Die vier Kandidaten-Listen zeigen für AKE weiterhin ihre reguläre
   Werkbank-Spalte; für IDEAL-Sub-FAs bleibt/wird die Spalte leer (bzw. „Keine Werkbank"-Badge in
   `FaCompletion`), ohne Exception oder fehlerhafte Anzeige.

## Test-Szenarien

Kein neues TS-Kapitel — das zurückgebaute Feature war nie produktiv. Stattdessen wird das bestehende
Kapitel „IDEAL — Materialisierung: Fachliche Felder (K1/K2/K3)" in `docs/TESTSZENARIEN.md`
(aus [[2026-08-20-materialisierung-fachliche-felder-spec]]) angepasst:

- Die Szenarien „Werkbank-Datenhoheit (Variante B, final 2026-09-09)" und „Unbekannter Arbeitsbereich"
  werden als **zurückgebaut, siehe [[2026-09-21-adr-0014-arbeitsbereich-ist-zielort-spec]]** markiert,
  nicht gelöscht (Nachvollziehbarkeit der Historie, gleiches Vorgehen wie beim TS-76-„superseded"-Muster
  der BDE-Spec).
- **Neues Rückbau-Verifikationsszenario:** Materialisierungslauf (`DryRun=false`) gegen Testdaten mit
  einem `FaHierarchyNode.Arbeitsbereich`-Wert, der vorher eine Werkbank getroffen hätte (z. B. `K-02`)
  → nach dem Lauf ist `ProductionOrder.ProductionWorkplaceId` für den betroffenen Sub-FA weiterhin
  `null` (bzw. unverändert, falls schon vorher ein Wert stand), kein SyncLog-Eintrag zu Werkbank/
  Arbeitsbereich, keine Mail.
- **Regressionsszenario Coating:** derselbe Testlauf zeigt für einen Sub-FA mit `Beschichtet = true`
  (selbst oder direktes Kind) weiterhin korrekt gesetztes `PickingStatus.HasCoatingParts = true` —
  unverändert zur Vor-Rückbau-Situation.
- **Regressionsszenario AKE:** Master `false`, bestehendes AKE-Testszenario der FA-Liste läuft
  unverändert grün, keine Werkbank-Spalte betroffen.

Alle drei Szenarien sind teilweise Manual-UAT (Sync-Lauf-Zeitpunkt, echte Struktur-Daten) — konsistent
mit dem bereits dokumentierten Muster der Materialisierungs-Spec.

Nach Abschluss `secondbrain/tests/testszenarien-index.md` nachziehen.

## Reihenfolge / Einordnung

- **Selber, bereits laufender Bündel-Worktree** (`feature/2026-08-07-ideal-teile-1-5`) — **kein**
  neuer Worktree. Der Rückbau korrigiert Code, der in genau diesem Zweig sitzt und noch nie gemergt
  wurde.
- **Voraussetzung für [[2026-09-21-ideal-bde-arbeitsgaenge-aus-struktur-spec]]**: deren Backlog-Notiz
  verlangt ausdrücklich, diese Korrektur **vor** dem Anlegen der echten IDEAL-Werkbänke (Baustein a
  der BDE-Spec) abzuschließen. Reihenfolge im Worktree: diese Spec zuerst, dann die BDE-Spec.
  Zwischen beiden besteht kein Datei-Konflikt (unterschiedliche Codeabschnitte derselben Klasse
  `FaMaterializationSyncService` bleiben unberührt von der BDE-Spec, die nur lesend auf materialisierte
  `ProductionOrder`-Zeilen zugreift).
- **Verhältnis zu v1.37.0** ([[2026-08-20-materialisierung-fachliche-felder-spec]], Status Testbereit,
  selber Worktree): Diese Korrektur-Spec nimmt der v1.37.0-Umsetzung die Werkbank-Ableitung (AK 9/10
  dort) wieder weg, bevor der gebündelte Worktree je gemergt wird. Bei Umsetzung ist in der
  Hauptcheckout-Kopie von `secondbrain/specs/freigegeben/2026-08-20-materialisierung-fachliche-felder-
  spec.md` ein additiver Hinweis zu ergänzen: AK 9/10 sowie die zugehörigen Testszenarien gelten seit
  dieser Korrektur-Spec nicht mehr (Datei selbst nicht umschreiben).
- **Verhältnis zu ADR 0014**: siehe Offene Rückfrage 3 — das Datenhoheits-Muster (Sage führend,
  unscharfe Meldung, Umschaltpunkt Variante C) bleibt als **Muster** richtig und wird an anderer Stelle
  (BDE-Spec, Baustein a) bereits erneut angewandt — nur eben korrekt auf `ProductionWorkplace.
  ArbeitsschrittCode`, nicht auf `ProductionOrder.ProductionWorkplaceId` aus `Arbeitsbereich`. ADR 0014
  selbst wird durch diese Spec **nicht** umgeschrieben (ADRs werden nie überschrieben), sondern per
  Nachtrag oder supersedierender ADR ergänzt — Mechanismus und Redaktion sind Sache des Menschen.

## Brain-Pflichten bei Umsetzung (Merkliste für den Dev-Lauf)

- Version-Bump in **beiden** `AppVersion.cs` — nächste freie Nummer im Worktree (vermutlich `1.43.0`,
  im Dev-Lauf gegen den dann aktuellen Stand verifizieren — Kollisionsgefahr mit den Baustein-Versionen
  der BDE-Spec, die im selben Worktree ebenfalls Nummern nach `1.42.0` beansprucht).
- Anwender-Changelog `Views/Help/Changelog.cshtml` + Brain-Changelog
  `secondbrain/changelog/YYYY-MM-DD-vX-Y-Z-*.md` — als Korrektur/Rückbau kennzeichnen, nicht als Feature.
- `secondbrain/feature-map.md` — v1.37.0-Zeile um den Hinweis „Werkbank-Ableitung durch
  [[2026-09-21-adr-0014-arbeitsbereich-ist-zielort-spec]] zurückgebaut" ergänzen.
- `secondbrain/specs/freigegeben/2026-08-20-materialisierung-fachliche-felder-spec.md` — additiver
  Hinweis wie oben unter „Reihenfolge/Einordnung" beschrieben (Hauptcheckout, Datei nicht umschreiben).
- `secondbrain/architektur/adr/0014-werkbank-datenhoheit-sage-fuehrend-mit-abweichungsmeldung.md` —
  Nachtrag bzw. supersedierende ADR gemäß Antwort auf Offene Rückfrage 3 (additiv, ADR nicht
  überschreiben).
- `secondbrain/architektur/fallstricke.md` — neuer Eintrag (Abschnitt 9, IDEAL-hierarchische
  Produktionsaufträge, oder neuer Abschnitt): „Arbeitsbereich ist ein Zielort, keine Werkbank" — was
  die falsche Annahme war, warum sie plausibel schien (fehlende Sage-Arbeitsplatz-Stammdaten im
  August), und der Verweis auf das echte, im selben Sage-Vokabular getrennte Werkbank-Feld
  (`USER_ArbeitsSchritt` vs. `USER_OSAbteilung`, siehe [[sage-views-ideal]]).
- `secondbrain/codebase/services.md` — Eintrag zu `FaMaterializationSyncService` um die entfernte
  Werkbank-Ableitung/`IUnknownWorkplaceState` bereinigen.
- `secondbrain/tests/testszenarien-index.md` — Nachzug wie oben beschrieben.

## Deploy

**Provisorisch (Dev-Lauf bestätigt gegen den echten Diff):**

- **Web-App: ja.** Ausschließlich wegen des paketweiten Versions-Bumps (`AppVersion.cs` +
  `Views/Help/Changelog.cshtml` liegen in `IdealAkeWms`); kein Fach-Code der Web-App ändert sich
  (Views bleiben unverändert, siehe Technischer Lösungsentwurf).
- **Service: ja.** `FaMaterializationSyncService.cs`, `FaMaterializationPlanner.cs`,
  `IUnknownWorkplaceState.cs` (gelöscht), `Program.cs` (DI-Deregistrierung), `AppVersion.cs`.
- **Migration: nein** (siehe „Migrations-/SQL-Auswirkungen").
- **Reihenfolge:** kein Datenbank-Schritt nötig — Service kann unabhängig vom Web-Deploy neu
  gestartet werden; da nichts produktiv von der entfernten Ableitung abhängt, ist kein besonderes
  Wartungsfenster nötig.
- **Publish-Befehle (aus dem Worktree):**
  ```
  dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb
  dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService
  ```
  *Nach dem Merge* nur erneut aus `main` publishen, falls der Merge tatsächlich getestete Dateien mit
  parallelen `main`-Änderungen zusammenführt.

## Offene Rückfragen

1. **Grundsatzfrage (die wichtigste).** Braucht ein IDEAL-Auftrag überhaupt EINE Werkbank am Auftrag
   (`ProductionOrder.ProductionWorkplaceId`)? Ein Teil läuft durch mehrere Arbeitsgänge, jeder an
   seiner eigenen Werkbank (BDE-Spec: Werkbank lebt je `WorkOperation`). Empfehlung dieser Spec: **nein**
   — `ProductionWorkplaceId` bleibt bei IDEAL nach diesem Rückbau schlicht `null`, die Werkbank ergibt
   sich ausschließlich je Arbeitsgang über die BDE-Spec. Begründung: ein einzelnes Feld am Auftrag kann
   fachlich nicht abbilden, dass ein Sub-FA mehrere Werkbänke durchläuft — ein Versuch, es trotzdem zu
   befüllen (z. B. „erste" oder „letzte" Werkbank), wäre eine neue Rate-Heuristik derselben Art wie die
   jetzt zurückgebaute. Entscheidung liegt beim Menschen.
2. **Wohin gehört der Arbeitsbereich stattdessen?** Er ist ein Zielort. Braucht es eine eigene
   Anzeige-Spalte „Zielort" am Auftrag/in den Listen? Und: Besteht eine Verwandtschaft zum
   **Kommissionierziel** (`FaHierarchyNode.Kommissionieren`)? **Nicht annehmen, dass beide dasselbe
   sind** — die Struktur führt sie als getrennte Felder ([[sage-views-ideal]], Zeile 72/73: Zeile 72
   `Kommissionieren` ist der Kern-Filter der Kommissionierlisten, Zeile 73 `Arbeitsbereich` ist der
   Bearbeitungsort auf Bauteil-Ebene) — der Fachbereich kennt den Grund für die Trennung, diese Spec
   rät nicht.
3. **Was wird aus ADR 0014?** Das Datenhoheits-Muster (Sage führend, Abweichung melden, Umschaltpunkt
   auf Variante C) bleibt als **Muster** richtig — es wurde nur auf das falsche Feld angewandt und wird
   an anderer Stelle (BDE-Spec, Baustein a: `ProductionWorkplace.ArbeitsschrittCode`) bereits korrekt
   erneut angewandt. Empfehlung dieser Spec: ein **additiver Nachtrag** an ADR 0014 (kurzer Abschnitt
   „Korrektur 2026-09-21: diese Entscheidung wurde fälschlich auf `Arbeitsbereich`/
   `ProductionOrder.ProductionWorkplaceId` angewandt; das Muster selbst gilt weiterhin, siehe
   [[2026-09-21-ideal-bde-arbeitsgaenge-aus-struktur-spec]] für die korrekte Anwendung") ist der
   kleinere Eingriff als eine komplett neue, supersedierende ADR — aber ADRs werden nie umgeschrieben,
   nur ergänzt/superseded (CLAUDE.md); welcher der beiden Wege gewählt wird, entscheidet der Mensch.

## Freigabe-Antworten (Mensch füllt aus — Schranke 1)

1. →
2. →
3. →

## Kritische Pruefung (2026-09-22)

> Anwalt-des-Teufels-Durchsicht **vor** dem Dev-Lauf. Der Rueckbau ist sauber und vollstaendig
> spezifiziert (Loeschliste mit exakten Zeilenbereichen, Coating als Regressionsanker, AKE korrekt
> als unberuehrt verifiziert, keine Migration). Trotzdem zwei BLOCKER — einer formal, einer
> substanziell am Code gefunden — plus ein SOLLTE.

### BLOCKER

**B1 — Die drei Rueckfragen sind NICHT beantwortet (nackte Pfeile).** Der Freigabe-Antworten-Block
enthaelt `1. →`, `2. →`, `3. →` ohne Text; `freigabe_entscheidung` im Frontmatter ist leer, Status
`Entwurf`. Genau der „Pfeil ohne Text"-Fall. Zur Fairness: Der Rueckbau-**Code** ist gegen alle drei
Antworten robust (reine Loeschung; die Vorwaerts-Teile sind sauber Out-of-Scope gestellt) — es ist ein
„bewusst entscheiden und festhalten"-Gate, kein „die Spec ist falsch"-Gate. Aber Rueckfrage 1
(braucht ein IDEAL-Auftrag EINE Werkbank?) ist die Architekturentscheidung und haengt direkt an B2.
→ **An den Menschen:** die drei Antworten ausfuellen (Q1 mindestens bewusst: `null`/Folge-Spec; Q2/Q3
duerfen kurz sein: eigene Folge-Spec bzw. Nachtrag an ADR 0014).

**B2 — Die Spec uebersieht den bestehenden MANUELLEN Werkbank-Pfad `FaCompletion.SetWorkplace`; dadurch
sind zwei Kernaussagen falsch und Rueckfrage 1 unvollstaendig informiert.** Verifiziert am Code:
`FaCompletionController` hat eine POST-Action `SetWorkplace(int id, int? workplaceId)` (Z.419-432,
setzt `order.ProductionWorkplaceId` + Audit-Felder von Hand), dazu `HasNoWorkplace`/`WorkplaceName`
(Z.147-148) und die Spalte „Keine Werkbank" (Z.250). Folgen, die die Spec nicht nennt:
1. **Die Out-of-Scope-/AK-Aussage „`ProductionWorkplaceId` bleibt fuer IDEAL durchgaengig `null`"
   (Zeilen 87-89, AK 9) ist falsch.** Sie ist `null`, **ausser** ein Anwender hat ueber
   `FaCompletion.SetWorkplace` von Hand eine Werkbank gesetzt.
2. **Verhaltensaenderung, unbenannt:** Die Z1-Ausnahme existierte laut Klassenkommentar genau deshalb,
   weil der Sync „nicht unterscheiden kann, ob [der Wert] von Hand gesetzt oder in Sage umgeplant
   wurde" — die Ableitung hat also manuelle `SetWorkplace`-Werte bei **jedem** Update **ueberschrieben**.
   Nach dem Rueckbau fasst der Sync das Feld gar nicht mehr an → **manuelle Zuweisungen bleiben jetzt
   erhalten**. Das ist vermutlich sogar gewuenscht, aber es ist eine echte, ungenannte Verhaltensaenderung.
3. **Rueckfrage 1 ist damit teil-beantwortet durch Bestandscode:** Ein „eine Werkbank je Auftrag"-Modell
   existiert bereits — als **manuelle** Zuweisung in FaCompletion, unabhaengig von der (falschen)
   Arbeitsbereich-Ableitung. Der Mensch braucht diese Tatsache, um Q1 sauber zu entscheiden.
→ **An den Menschen / in den Rumpf:** Den `FaCompletion.SetWorkplace`-Pfad in der Spec anerkennen, die
„durchgaengig null"-Aussagen (Zeilen 87-89, AK 9) auf „null, ausser manuell in FaCompletion gesetzt"
korrigieren, und Q1 mit dem Wissen um den bestehenden manuellen Pfad beantworten.

### SOLLTE

**S1 — Regressions-AK fuer die neue Persistenz manueller Zuweisungen.** Aus B2 folgt ein Test, den die
Spec noch nicht hat: Ein per `FaCompletion.SetWorkplace` manuell gesetztes `ProductionWorkplaceId`
**ueberlebt** nach dem Rueckbau einen Materialisierungslauf (vorher wurde es durch die Z1-Ableitung
geklobbert). Als AK + Testszenario aufnehmen — es sichert die Verhaltensaenderung aus B2 ab, statt sie
nur nebenbei geschehen zu lassen.

### HINWEIS

**H1 — uebrige `ProductionWorkplaceId`-Konsumenten geprueft, unkritisch.** Grep ueber den Worktree: die
weiteren Treffer betreffen fast alle `WorkOperation.ProductionWorkplaceId` (andere Entitaet, BDE/OSEON)
oder das `ProductionWorkplace`-CRUD — **kein** Filter/keine Gruppierung auf
`ProductionOrder.ProductionWorkplaceId`, die IDEAL-Auftraege bei `null` aus einer Liste ausblenden
wuerde, ausser der (unter B2 behandelten) FaCompletion-Anzeige. Die „Views rendern harmlos"-Aussage der
Spec haelt fuer die reine Anzeige. Kein Handlungszwang.

### Empfehlung

**NACHBESSERUNG NOETIG:** B1 (drei Antworten leer — mindestens Q1 bewusst entscheiden) und B2
(`FaCompletion.SetWorkplace` anerkennen, „durchgaengig null"-Aussagen korrigieren, Q1 damit informieren)
plus S1 (Regressions-AK). Der Rueckbau selbst ist korrekt zugeschnitten — die Nachbesserung betrifft die
Genauigkeit der Aussagen und die noch offene Architekturentscheidung, nicht die Loesch-Mechanik.