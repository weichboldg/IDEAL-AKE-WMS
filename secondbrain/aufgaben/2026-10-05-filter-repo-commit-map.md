---
typ: notiz
status: Erledigt
bezug: "[[2026-09-10-publish-zip-blockiert-push-und-merge]]"
---
# Commit-Map filter-repo 2026-10-05 (Zweig feature/2026-08-07-ideal-teile-1-5)

Zuordnung alte -> neue Commit-Hashes nach dem Entfernen von `publish.zip` aus der History (Lauf 2,
`--preserve-commit-hashes`). Enthalten sind nur die **163 geänderten** Commits (`549c5db9` und seine
Nachfahren auf dem Zweig); alle übrigen 1246 Commits des Zweigs behielten ihren Hash. Quelle:
`.git/filter-repo/commit-map`. Alte Commits bleiben über den lokalen Tag
`backup/ideal-teile-1-5-vor-filter-repo` (6c253c60) erreichbar.

Sortiert nach Commit-Datum (älteste zuerst).

| alt | neu | Datum | Betreff |
| --- | --- | --- | --- |
| `549c5db9` | `950d872a` | 2026-08-11 | epic 2026-08-07-ideal-teile-1-5: UAT-Fix - FAInfos-Spalte Abteilung (nicht Montage-Abteilu |
| `2470578e` | `f1f31903` | 2026-08-11 | epic 2026-08-07-ideal-teile-1-5: Fix - IDEAL-Feature-Toggles in /Settings seedbar+gruppier |
| `b6b6d83f` | `5a5c2a7c` | 2026-08-11 | epic 2026-08-07-ideal-teile-1-5: UI-Polish - FaHierarchy-Baumanzeige im OSEON-Tracking-Sti |
| `37e87525` | `3894e3d6` | 2026-08-12 | epic 2026-08-07-ideal-teile-1-5: Etappe 6 - Listen-Spaltenauswahl an 4 IDEAL-Listen + tabl |
| `961749f2` | `a372638a` | 2026-08-12 | wip: fa-struktur - Kontrast-Fix WCAG AA |
| `767f06fd` | `a8647452` | 2026-08-12 | epic 2026-08-07-ideal-teile-1-5: Etappe 7 - FA-Struktur seitenweite Tree-Table (OSEON) + K |
| `e83eb1ca` | `f07ea342` | 2026-08-13 | epic 2026-08-07-ideal-teile-1-5: Etappe 8 - UAT-Anpassung: SubFA-Sperre/Banner weg + Kommi |
| `fe7299ba` | `74407264` | 2026-08-14 | epic 2026-08-07-ideal-teile-1-5: Teil 7 Etappe A - Schema-Inversion ProductionOrders |
| `aed9cb53` | `e7273297` | 2026-08-17 | epic 2026-08-07-ideal-teile-1-5: Teil 7 Etappe B (Kern) - HierarchischeStrukturGuard-Plane |
| `0ae43d0b` | `58312739` | 2026-08-17 | epic 2026-08-07-ideal-teile-1-5: Teil 7 Etappe B - Umschalt-Seite + Master read-only in ge |
| `9de5b1a2` | `52e013dc` | 2026-08-17 | epic 2026-08-07-ideal-teile-1-5: Teil 7 Etappe B - Rueckbau-Runbook + README-Verlinkung (A |
| `39f78133` | `0a8175ac` | 2026-08-17 | epic 2026-08-07-ideal-teile-1-5: Teil 7 Etappe B - Testszenarien TS-63 (Einwegtor) + Servi |
| `dc297d90` | `ae9be6c9` | 2026-08-17 | epic 2026-08-07-ideal-teile-1-5: Teil 7 Etappe C (1/3) - reiner FaMaterializationPlanner ( |
| `0aaa758a` | `a8230f3c` | 2026-08-17 | epic 2026-08-07-ideal-teile-1-5: Teil 7 Etappe C (2/3) - FaMaterializationSyncService (Orc |
| `375dbc36` | `285136a0` | 2026-08-17 | epic 2026-08-07-ideal-teile-1-5: Teil 7 Etappe C (3/3) - SyncWorker-Verdrahtung (gated auf |
| `bc7e3d6a` | `285909e9` | 2026-08-17 | epic 2026-08-07-ideal-teile-1-5: Teil 7 Etappe C - Testszenarien TS-64 (Materialisierung / |
| `fb07512e` | `b9e2482f` | 2026-08-17 | epic 2026-08-07-ideal-teile-1-5: Teil 7 Etappe D (1/4) - GetAllByFaAndOperationAsync (meng |
| `ca724343` | `63191503` | 2026-08-17 | epic 2026-08-07-ideal-teile-1-5: Teil 7 Etappe D (2/4) - dreistufige Auto-Erledigt-Sperre  |
| `6a00ba77` | `ab45e555` | 2026-08-17 | epic 2026-08-07-ideal-teile-1-5: Teil 7 Etappe D (3/4) - hierarchischer Reconcile-Gruppen- |
| `660a01b3` | `d9a4d1e2` | 2026-08-17 | epic 2026-08-07-ideal-teile-1-5: Teil 7 Etappe D - Testszenarien TS-65 (Lookup-Haertung /  |
| `5cf802e0` | `db9ecab5` | 2026-08-17 | epic 2026-08-07-ideal-teile-1-5: Teil 7 Etappe E - Version-Bump v1.32.0 (Web+Service) + An |
| `abcd7185` | `b1153ebc` | 2026-08-17 | fix(teil7): EF-Migration Index-Tausch guarded (beide DB-Formen) — behebt SqlError 3701 b |
| `418f23aa` | `d95cb63a` | 2026-08-18 | epic 2026-08-07-ideal-teile-1-5: Teil 8 Etappe 1 - Verifikation (Teil-7-Naht im BDE-Kontex |
| `f86a8864` | `992d7e15` | 2026-08-18 | epic 2026-08-07-ideal-teile-1-5: Teil 8 Etappe 2 - serverseitige Aufloesungslogik (BdeScan |
| `5d3e7232` | `2b305bae` | 2026-08-18 | fix(teil7): EF-Migration SubOrderNumber-NOT-NULL guarded (behebt SqlError 3701 #2 beim Sta |
| `5fd00709` | `14d3c187` | 2026-08-18 | epic 2026-08-07-ideal-teile-1-5: Teil 8 Etappe 3 - Scan-Auswahl-UI (Normal-Modus) auf reso |
| `58c26bd0` | `5c0735d7` | 2026-08-18 | epic 2026-08-07-ideal-teile-1-5: Teil 8 Etappe 4 - NurFA-Fix (kein last-wins) + Teileverfo |
| `8395394e` | `b8847d2a` | 2026-08-18 | epic 2026-08-07-ideal-teile-1-5: Teil 8 Etappe 6 - Version v1.33.0 + Anwender-Changelog +  |
| `c8ae47fe` | `a99705f4` | 2026-08-18 | wip: 2026-07-29-standort-ideal-teil-6 feature-complete |
| `1173cf1a` | `34ca1682` | 2026-08-18 | wip: 2026-08-18-bom-guard-hierarchisch feature-complete |
| `f869bb03` | `e2d96b4f` | 2026-08-19 | fix(fa-struktur): vertikale Einrueckungs-Leitlinie im Tree-Table wiederhergestellt (Spec A |
| `30b7a8c2` | `43b6bde9` | 2026-08-20 | wip: fa-liste-hierarchie Etappe A (1/4) - Repo-Gruppenabfrage GetForLeitstandGroupedAsync |
| `93151862` | `e8d21d0c` | 2026-08-20 | wip: fa-liste-hierarchie Etappe A (2/4) - ColumnDef parent-sub-order-number + JS-Modul fa- |
| `abd2620a` | `5ee8177f` | 2026-08-20 | epic 2026-08-07-ideal-teile-1-5: Etappe A - FA-Liste hierarchisch (ProductionOrders Refere |
| `b1505799` | `4a74ea68` | 2026-08-20 | epic 2026-08-07-ideal-teile-1-5: Etappe B - FaCompletion hierarchiefaehig |
| `5b15caf0` | `bf6863cb` | 2026-08-20 | epic 2026-08-07-ideal-teile-1-5: Etappe B - PickingLeitstand hierarchiefaehig |
| `960d5c81` | `d7df8a96` | 2026-08-21 | epic 2026-08-07-ideal-teile-1-5: Etappe B - Picking (Kommissionierliste) hierarchiefaehig |
| `10921e26` | `837a1f6c` | 2026-08-21 | epic 2026-08-07-ideal-teile-1-5: Etappe B - FaWorklist hierarchiefaehig |
| `7bba52b7` | `087c3111` | 2026-08-21 | epic 2026-08-07-ideal-teile-1-5: Etappe B - Tracking/Index hierarchiefaehig (3-Ebenen) |
| `a305d6f3` | `88060161` | 2026-09-05 | epic 2026-08-07-ideal-teile-1-5: Etappe C - Kaskade 'Alle Sub-FAs fertigmelden' |
| `d9de60cc` | `e9eb0895` | 2026-09-05 | epic 2026-08-07-ideal-teile-1-5: Etappe D (Teil 1/2) - Z4-Sweep Articles/Info + Z1-Regress |
| `dde9a176` | `ea3932dc` | 2026-09-07 | epic 2026-08-07-ideal-teile-1-5: Etappe E - v1.35.0 (Testszenarien + Changelog + Version) |
| `a8d75de8` | `c02c6f97` | 2026-09-08 | fix(uat): U1 BOM-Hinweisseite ohne JS-Fehler + Sub-FA-Kopfzeile, U2 Kaskade-Zaehler immer  |
| `25399bef` | `8005eb36` | 2026-09-08 | wip: bom-bridge plan (6 Tasks, Task 2 || Task 4) |
| `f89659fd` | `ea842849` | 2026-09-08 | wip: bom-bridge Task 1 — BomKey/BomScope/MengeIstAuftragsmenge + Signaturwechsel (AKE bi |
| `56e8faa4` | `e75bcf35` | 2026-09-08 | wip: bom-bridge Task 4 - Klasse-D-Gates (Coating/WorkStep/BomCache x2) ueber IHierarchical |
| `e99bcf3c` | `7a93c2f5` | 2026-09-08 | wip: bom-bridge Task 2 — FaHierarchyBomRepository (DirectChildren/FullStructure, rekursi |
| `3784e4b8` | `e2cceae1` | 2026-09-08 | fix: bom-bridge Task 2 review — Wurzel-Kopf HauptArtnr, Kollisions-Log in DirectChildren |
| `611475d6` | `8877ef56` | 2026-09-08 | wip: bom-bridge Task 3 — Master-Weiche fuer beide Interfaces, Guard entfernt, hierarchis |
| `cd34b081` | `62685c0d` | 2026-09-08 | wip: bom-bridge Task 5 — Bom.cshtml hierarchische Spalten, Artikelinfo HauptFA/Sub-FA, M |
| `86371eeb` | `9f0f2101` | 2026-09-08 | fix: bom-bridge Task 5 — Nav-Dropdown nur bei zwei Eintraegen (AKE bit-identisch) |
| `a3f125e9` | `92de2b88` | 2026-09-08 | fix: bom-bridge Task 5 review — #column-config um hierarchische Spalten erweitert |
| `8d9468d5` | `5b83ea07` | 2026-09-08 | wip: bom-bridge feature-complete — v1.36.0, Changelog, Hilfe, TS-70 (TS-68 abgeloest), D |
| `22d31ae7` | `5226352e` | 2026-09-08 | fix: bom-bridge final review — PrintPicking-Scope, deterministischer Kollisions-Tiebreak |
| `91d8d3ef` | `fec39bed` | 2026-09-09 | chore: publish.zip aus dem Zweig entfernen und Zip-Artefakte ignorieren |
| `3ea811f2` | `16d4bbda` | 2026-09-09 | fix: KO-Termin-Tooltip auf Konstruktions-Termin korrigieren |
| `73cea2ac` | `c7acedce` | 2026-09-09 | docs: TS-70.6 Deploy-Vorbedingung akenet01, TS-70.11 beide Bewertungsfaelle |
| `a9ea910a` | `64eac531` | 2026-09-09 | docs: Plan Materialisierung fachliche Felder (K1/K2) inkl. Pre-Flight-Scan |
| `ceaf5b32` | `7ac06cb9` | 2026-09-09 | fix: Materialisierung legt PickingStatus- und BdeStatus-Zeile eager an (UAT-Befund H0, Spe |
| `904e8990` | `8531a29f` | 2026-09-09 | feat: Kombigeraet-Erkennung im FA-Struktur-Sync (einmal je Lauf) |
| `47c6712c` | `232c0661` | 2026-09-09 | feat: GetByHauptFaKeysAsync fuer die gebuendelte Kopfdaten-Abfrage |
| `af25bcdf` | `3b77f667` | 2026-09-09 | feat: Lack-Ableitung als reiner Helper + Arbeitsbereich im Quell-Record |
| `ccc1d74d` | `3371ff1d` | 2026-09-09 | feat: Werkbank aus Arbeitsbereich (Variante B) mit Meldung bei Abweichung, unbekanntem und |
| `77aaac22` | `61e2d573` | 2026-09-09 | feat: HasCoatingParts aus der FA-Struktur ableiten und schreiben (F5) |
| `cc1a5f58` | `b13797af` | 2026-09-09 | fix: nodeByKey ohne Blattknoten aufbauen (Review-Nachzug Task 2) |
| `520bafb5` | `043c278f` | 2026-09-09 | fix: Wiederauftreten eines unbekannten Arbeitsbereichs wird wieder gemeldet |
| `79a174b7` | `f5fe6f7b` | 2026-09-09 | fix: Kunde-Filter und Kunde-Spaltenfilter finden im hierarchischen Modus ueber den Auftrag |
| `f160b1d9` | `64186524` | 2026-09-09 | feat: K1-Auftragsdaten je HauptFA im ViewModel, Termin-Kaskade auf den Gruppenwert |
| `50222222` | `c63a0d98` | 2026-09-09 | fix: Kunde-Spaltenfilter nur im hierarchischen Fall ueber neuen Builder, Flachmodus unange |
| `b3d5a15b` | `1a33b202` | 2026-09-09 | fix: Beschichtungstermin-Ueberschreibung an explizites hierarchical-Flag haengen, nicht an |
| `86cdc265` | `b6d3c0ea` | 2026-09-09 | feat: Gruppen-Kopfzeile der FA-Liste zeigt Auftragsdaten, Kombigeraete alle Varianten |
| `1f038f69` | `7d5e0677` | 2026-09-09 | docs: v1.37.0, Anwender-Changelog, Hilfe und TS-71 |
| `be92ade1` | `c958edc5` | 2026-09-09 | fix: Varianten-Zeile klappt mit der Gruppe zu (Gesamt-Review, fuenfter stiller Fehler) |
| `940bab2c` | `ea2cbc8a` | 2026-09-10 | plan: fa-liste-ausbau-matchcode (16 Tasks, 4 Bloecke) |
| `461d3e5b` | `6c1e2c0f` | 2026-09-10 | feat: Spalte ProductionOrder.Matchcode (NVARCHAR(200), additiv) |
| `cbabdd9d` | `41fe0406` | 2026-09-10 | feat: Matchcode materialisieren (Anlege- und Update-Pfad, F1) |
| `f4e9a6d5` | `8cd2856e` | 2026-09-10 | feat: Matchcode-Spalte in fuenf FA-Listen (ColumnDefinitions + column-config + th + td) |
| `48e1c87c` | `b050910e` | 2026-09-10 | feat: Server-Spaltenfilter auf Matchcode (null-sicher, F7) |
| `4a0563a9` | `ab27deda` | 2026-09-10 | fix: Matchcode-Filter auf BuildExtraInfoOrContains vereinheitlichen (Review Fix-Runde 1) |
| `9b2a2583` | `b879eea3` | 2026-09-10 | feat: K1-Kopfdaten je Zeile im Anzeige-Modell (hierarchisch), AKE unveraendert |
| `13693b77` | `0ca28cea` | 2026-09-10 | fix: Customer strukturell wie Prio/AbNummer/MontageAbteilung behandeln |
| `6ec37f85` | `93900a83` | 2026-09-10 | feat: K1-Spaltenfilter (Kunde/Prio/AB-Nr./Montage-Abteilung/KO-Termin) als C#-Postfilter |
| `cddb5b0b` | `decbe56a` | 2026-09-10 | fix: K1-Postfilter nur im hierarchischen Zweig, Flachmodus-customer-Filter bit-identisch |
| `a2c7861e` | `079f2789` | 2026-09-10 | test: Leitstand-Regressionstest fuer Kunde-Freitext vor der Task-7-Aenderung |
| `20686a21` | `3691cc3f` | 2026-09-10 | feat: Kunde-Freitext der FA-Liste ueber C#-Postfilter, Leitstand-Subquery unveraendert |
| `7b15d8a6` | `b209feaf` | 2026-09-10 | docs: Testzuschreibung im Leitstand-Regressionstest richtigstellen |
| `9c95fb08` | `58090927` | 2026-09-10 | feat: Prio/AB-Nr./Montage-Abt./KO-Termin als bedingte Zeilenspalten der FA-Liste |
| `0f148501` | `63bc7951` | 2026-09-10 | feat: table-sorted-Event + Befund B-2 (stiller Sortier-Abbruch meldet sich jetzt) |
| `203e23a2` | `7e54a770` | 2026-09-10 | feat: Wiederholungswerte der FA-Liste nur an der ersten Zeile zeigen (client-seitig) |
| `a422e444` | `1662ae94` | 2026-09-10 | fix: Wiederholungswert-Regel spezifisch genug gegen Bootstrap-Tabellenfarbe |
| `b9bf3420` | `79ad96f5` | 2026-09-10 | fix: Tabellen-Selektoren auf direkte Kinder skopieren (verschachtelte Tabellen) |
| `94463f9c` | `7093029b` | 2026-09-10 | fix: Fix-Runde 1 zum Selektor-Sweep (Vorher-Zustand, Waechter-Loecher, kein stiller return |
| `9dfce867` | `e9ea2bd3` | 2026-09-10 | docs: TS-72.3 — Fix gegen eine BEREITS GESPEICHERTE Spaltenpraeferenz nachweisen |
| `fda7c7ae` | `a5e0922f` | 2026-09-10 | feat: HauptFA-Zeile gekennzeichnet, Gruppen-Kopfzeile auf Kunde + Leittermin verschlankt |
| `067f1b14` | `901e9db2` | 2026-09-10 | feat: SetReleaseForOrderNumberAsync (atomar, Prio nach SubOrderNumber, Picker fuer alle) |
| `aad4b54c` | `692956f4` | 2026-09-10 | feat: CascadeRelease/-Preview mit unveraenderter Kommissionierer-Pflicht |
| `fef12cfc` | `daf71682` | 2026-09-10 | feat: Gruppen-Knopf und Dialog fuer die Freigabe-Kaskade |
| `deff907e` | `24864e66` | 2026-09-10 | chore: v1.38.0 — Anwender-Changelog und Testszenarien TS-73 (AK 1-20) |
| `d67dd7f8` | `c2808b14` | 2026-09-14 | docs(plan): PDF-Erzeugung FaHierarchy-Druck — 7 Tasks, A1-A7 gemessene Spec-Abweichungen |
| `f6be0272` | `2f0047be` | 2026-09-14 | docs(plan): zwei Test-Entwuerfe korrigiert (Moq-Sequence, pdfQuery-Waechter) |
| `b90ff6a4` | `ade3f0a4` | 2026-09-14 | feat(pdf): PdfFileNameBuilder |
| `3bca9069` | `a5a4ab77` | 2026-09-14 | feat(pdf): PdfRenderStatus, EdgeProcessRunner, PdfRenderException |
| `8a44241f` | `0f07dbcd` | 2026-09-14 | feat(pdf): PdfRenderService mit Semaphor, Laufverzeichnis und zweistufigem Aufraeumen |
| `c8ed9a60` | `ef907601` | 2026-09-14 | fix(pdf): Registry-Lesen in PdfRenderStatus darf den App-Start nie abbrechen (Review Task  |
| `f401767b` | `60c4a04d` | 2026-09-14 | feat(pdf): RazorViewRenderer, DI-Registrierung (Singleton) und Start-Probe |
| `20549c0f` | `17de007a` | 2026-09-14 | feat(pdf): Pdf-Actions (eine Gruppe, Routen-Segment) und Filter-Hinweis im Druckkopf |
| `60a4e1c8` | `0b279a48` | 2026-09-14 | feat(pdf): PDF-Knopf je HauptFA-Gruppe, nur wenn Edge verfuegbar |
| `aa3895d3` | `2b1bdb01` | 2026-09-14 | fix(pdf): CS8602 an HttpContext.RequestAborted in beiden Pdf-Actions (Review Task 5) |
| `07cc64c1` | `c6ae0495` | 2026-09-14 | chore: v1.39.0 — Anwender-Changelog, README (Edge-Voraussetzung, PdfRender-Konfiguration |
| `d37aab7c` | `64d28cff` | 2026-09-14 | docs(ts-74): Notfallpfad AK 6 (direkter Pdf-Aufruf ohne Edge) in TS-74.6 ergaenzt (Review  |
| `85df349c` | `d96da332` | 2026-09-14 | fix(pdf): Aufrufer-Abbruch beendet Edge; Kandidatenermittlung darf App-Start nie abbrechen |
| `9e900248` | `00929acb` | 2026-09-15 | docs(plan): Matchcode im Artikelstamm — 8 Tasks, A1-A5 gemessene Spec-Korrekturen |
| `1f920aa1` | `f1cf6b1e` | 2026-09-15 | feat(matchcode): Article.Matchcode + Batch-Lookup GetMatchcodesByArticleNumbersAsync |
| `9283d5c6` | `495284a7` | 2026-09-15 | feat(matchcode): fuenf FA-Listen lesen Matchcode aus Article-Join + AK-14-Fehltreffer-Zaeh |
| `47de3ba2` | `6093936d` | 2026-09-15 | refactor(matchcode): ProductionOrder.Matchcode zurueckgebaut; Migration AddProductionOrder |
| `60150579` | `f5a3988e` | 2026-09-15 | feat(matchcode): Article-Sync liest KHKArtikel.Matchcode (CAST nvarchar(200), MAX je Artik |
| `d3307e33` | `b0cc26d1` | 2026-09-15 | feat(matchcode): Artikelstamm — Suche, Spaltenfilter, Views, Info-Exakt-Fallback |
| `1193fa8a` | `1e992aac` | 2026-09-15 | feat(matchcode): Suche über Matchcode in Bestand, Fehlteilen, Bedarfsmeldungen, FA-Fertig |
| `46d56b5d` | `3ca5aed0` | 2026-09-15 | feat(matchcode): Artikel-Typeahead erst ab 3 Zeichen (AK 15, 108k-Zeilen-Scan pro Buchstab |
| `e677d087` | `0dc1c8ce` | 2026-09-15 | chore: v1.40.0 — Anwender-Changelog, README, Testszenarien TS-75 |
| `7fa71c17` | `88ec2976` | 2026-09-15 | docs(ts-75): AK-Label TS-75.1 korrigiert (AK 8 = Bedarfsmeldungen, gehört zu TS-75.6; Ide |
| `716a676c` | `6b9f3547` | 2026-09-16 | docs(help): Hilfeseite um PDF-Download (v1.39) + Matchcode am Artikel (v1.40) ergaenzt |
| `ce3feb03` | `a9340530` | 2026-09-16 | wip: arbeitsgaenge-aus-arbeitsschritte — FaWorkStepStructureDetectionService + Verdrahtu |
| `3b5577f9` | `9edde1b0` | 2026-09-16 | wip: arbeitsgaenge-aus-arbeitsschritte — 14 Unit-Tests (Struktur-Ableitung, DirectChildr |
| `1ba39186` | `d6eeb465` | 2026-09-16 | wip: arbeitsgaenge-aus-arbeitsschritte feature-complete — Changelog v1.41.0, TS-76, Hilf |
| `ed70c2b5` | `7099c368` | 2026-09-16 | chore: CLAUDE.md aus dem Feature-Zweig zuruecknehmen |
| `f04a7e0e` | `1bef63d2` | 2026-09-21 | wip: kommissionierziel-filter — User-Feld + Migration 92 + Settings (Master-gated) + Bom |
| `92139a49` | `17af86bd` | 2026-09-21 | wip: kommissionierziel-filter Bildschirm-Teil — Select2-Dropdown, Badge fuer alle vier S |
| `5f933f1d` | `95b611bd` | 2026-09-21 | wip: kommissionierziel-filter feature-complete — Druck-Durchschlag (PrintBom Spalten+Gef |
| `f15b7ff9` | `c870c5ce` | 2026-09-21 | ux: Leerzustands-Knopf setzt ALLE Filter zurueck (statt nur Komm.-Ziel) + Label 'Alle Filt |
| `ab10b57a` | `2200c470` | 2026-09-21 | docs(ts-70): Leerzustands-Reset raeumt alle Filter (Knopf 'Alle Filter zuruecksetzen'); Ba |
| `0aceee8d` | `cddb4f9b` | 2026-09-22 | wip: adr-0014-rueckbau feature-complete — Werkbank-aus-Arbeitsbereich-Ableitung entfernt |
| `8be03982` | `8a6fd0c4` | 2026-09-22 | wip: ideal-bde Etappe A backend — ProductionWorkplace Sage-Felder + Migration 93 + Produ |
| `be0695fb` | `bc25ea21` | 2026-09-22 | wip: ideal-bde Etappe A Views — ProductionWorkplaces Sage-Felder read-only + 2 Spaltenfi |
| `920aabb7` | `6ba4e903` | 2026-09-22 | epic ideal-bde-arbeitsgaenge: Etappe B — WorkOperation-Struktur-Erkennung + Existenz-Che |
| `f829d00d` | `e1d619b7` | 2026-09-22 | fix(v1.44.0): ServiceSettings-Description von Sync:WorkOperationStructureDetectionEnabled  |
| `912e6bea` | `49298f35` | 2026-09-24 | wip: fa-struktur-arbeitsgaenge-anzeige repository + tests (AK 10 ToQueryString) |
| `71ffeb18` | `6064263f` | 2026-09-24 | wip: fa-struktur-arbeitsgaenge-anzeige feature-complete (v1.45.0) |
| `2d06d1f0` | `821d82a3` | 2026-09-24 | wip: fa-struktur-arbeitsgaenge-anzeige review-fix — Modal laedt per OrderNumber wie der  |
| `9208c3ff` | `d3daf7ed` | 2026-09-25 | wip: 2026-09-25-kommissionierung-nur-hauptfa plan |
| `36911f2f` | `ee583ed7` | 2026-09-25 | wip: 2026-09-25-kommissionierung-nur-hauptfa rueckbau freigabe-kaskade |
| `2291c950` | `a30e7647` | 2026-09-25 | wip: 2026-09-25-kommissionierung-nur-hauptfa IsHauptFa + warteschlange + bulk-skip |
| `35a65d36` | `cba1be97` | 2026-09-25 | wip: 2026-09-25-kommissionierung-nur-hauptfa guards + views hauptfa-only |
| `23942f73` | `ffb1572e` | 2026-09-25 | wip: 2026-09-25-kommissionierung-nur-hauptfa eine sub-fa-definition (bomscope, hierarchy-g |
| `a3440bc3` | `a5bc8367` | 2026-09-25 | wip: 2026-09-25-kommissionierung-nur-hauptfa readonlybom-test: sub-fa explizit |
| `8d8bf8a5` | `b9991917` | 2026-09-25 | wip: 2026-09-25-kommissionierung-nur-hauptfa fa-liste: faworklist-link nur mit vorbau-rech |
| `7d1cd74e` | `855f7310` | 2026-09-25 | wip: 2026-09-25-kommissionierung-nur-hauptfa relevanzfilter |
| `f6911db4` | `f40053d3` | 2026-09-25 | wip: 2026-09-25-kommissionierung-nur-hauptfa normalisierung komm.-ziel-standardfilter |
| `e2d6e5d9` | `6a6adee6` | 2026-09-25 | wip: 2026-09-25-kommissionierung-nur-hauptfa stueckliste alle ziele + druck-leerzustand |
| `2290c99c` | `50582f81` | 2026-09-25 | wip: 2026-09-25-kommissionierung-nur-hauptfa feature-complete (v1.46.0) |
| `4fffeae9` | `4467b894` | 2026-09-25 | wip: 2026-09-25-kommissionierung-nur-hauptfa stueckliste: kanonischer filterwert nach vorb |
| `1f128c61` | `c61cb796` | 2026-09-25 | wip: 2026-09-25-kommissionierung-nur-hauptfa docs: ts-73.14 zurueckgebaut, hilfe profil ko |
| `787c5c90` | `30c9c20e` | 2026-09-25 | wip: 2026-09-25-kommissionierung-nur-hauptfa docs: final-review (hilfe faworklist-satz, ts |
| `4a8cdb95` | `acad4972` | 2026-09-25 | wip: 2026-09-25-kommissionierung-nur-hauptfa post-qa review: hidden-attribut, js-escaping  |
| `323120d8` | `f8b58a9d` | 2026-09-28 | wip: 2026-09-25-kommissionierliste-summierung-pdf plan |
| `ab2beed2` | `06d611c7` | 2026-09-28 | wip: 2026-09-25-kommissionierliste-summierung-pdf service (kw optional, anzeigefelder, gru |
| `e407596d` | `e4c440cf` | 2026-09-28 | wip: 2026-09-25-kommissionierliste-summierung-pdf columndefinitions summiert Co-Authored-B |
| `1fb2b59d` | `02817d79` | 2026-09-28 | wip: 2026-09-25-kommissionierliste-summierung-pdf controller (visibleColumns, printsummier |
| `1977a819` | `ddd85403` | 2026-09-28 | wip: 2026-09-25-kommissionierliste-summierung-pdf views (summiert standard, druckspalten,  |
| `58d50738` | `980108b8` | 2026-09-28 | wip: 2026-09-25-kommissionierliste-summierung-pdf drift-guard spaltenschluessel |
| `e447a12c` | `e380315c` | 2026-09-28 | wip: 2026-09-25-kommissionierliste-summierung-pdf feature-complete (v1.47.0) |
| `6a043456` | `9f4ea1bc` | 2026-09-28 | wip: 2026-09-25-kommissionierliste-summierung-pdf final-review fixes (kw-merker liste, dok |
| `6c253c60` | `b877e05a` | 2026-10-05 | merge: main -> feature/2026-08-07-ideal-teile-1-5 (AKE-Hotfix 1.30.1, v1.47.1) |
