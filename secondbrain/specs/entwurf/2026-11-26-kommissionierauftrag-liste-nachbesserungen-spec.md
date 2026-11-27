---
type: spec
title: "Kommissionierauftrag Maske „Liste“: Nachbesserungen R-1 bis R-5 aus der QA v1.99.89 und „—“ statt NAN im Druck"
slug: 2026-11-26-kommissionierauftrag-liste-nachbesserungen-spec
status: Entwurf
created: 2026-11-26
updated: 2026-11-26
source_backlog: "[[2026-11-26-kommissionierauftrag-liste-nachbesserungen]]"
uebersicht: "[[2026-11-26-kommissionierauftrag-nachbesserungen-uebersicht]]"
depends_on: "[[2026-11-26-kommissionierauftrag-filter-springt-auf-bom-spec]]"
task: ""
jira: ""
worktree: ""
branch: ""
standort: beide
affected_code:
  - IdealAkeWms/Services/VormontageService.cs (FormatTexte Z. 113-118 – optionale Sortierung, R-1)
  - IdealAkeWms/Services/KommissionierauftragListe.cs (Z. 43-47 natuerlich sortiert R-1; Z. 109, 117-119 ohne ?? 0m R-5; Offen Z. 126 → gemeinsame Regel R-4)
  - IdealAkeWms/Models/PickingOrder.cs (enum PickingOrderLineStatus Z. 20-29 – Erweiterung IstOffen(), R-4)
  - IdealAkeWms/Models/ViewModels/BomViewModels.cs (OffeneZeilenIds Z. 109, R-4)
  - IdealAkeWms/Services/PickingTransferService.cs (Einzelpruefung Z. 65, Expansion Z. 120, R-4; Texte unveraendert)
  - IdealAkeWms/Services/PickingOrderRestmengen.cs (Ueberdeckt Z. 62, R-4)
  - IdealAkeWms/Controllers/PickingController.cs (IsTransferred Z. 479 + QuellPlatz Z. 688-691 R-4/NAN; RenderAuftragDruckenAsync Z. 721-755 PDF-Hinweis R-3)
  - IdealAkeWms/Views/Picking/Bom.cshtml (Scan-Selektor Z. 888, R-2)
  - IdealAkeWms/Views/Picking/PrintAuftragListe.cshtml (Quell-Lagerplatz Z. 130) + PrintPicking.cshtml (Lagerplatz Z. 193) – NAN
  - IdealAkeWms.Tests (KommissionierauftragListeTests, PickingControllerTests, VormontageService-/FormatTexte-Tests)
  - docs/TESTSZENARIEN.md, Views/Help/Changelog.cshtml, beide AppVersion.cs
open_questions:
  - "1 R-1: Mehrwertige Zellen der Liste (Sage-Pos., Ebene, Baugruppe, Vater-Sub-FA, Arbeitsbereich) natuerlich sortiert (8, 9, 10) statt parallel zur Spalte Pos. - einverstanden?"
  - "2 R-2: Artikel-Scan ohne Gross/Klein fuer ALLE Masken der Stuecklisten-Seite (Auftrag Liste und Stueckliste, FA-Stueckliste, FA-Vervollstaendigung, Werkbank) statt nur fuer die Liste - einverstanden?"
  - "3 R-3: Nur PDF-Aufrufe ohne moeglichen summierten Druck bekommen Warnung + Ruecksprung auf den Auftrag; AuftragDrucken?ansicht=liste ohne pdf bleibt beim Druck je Position (wie K3) - einverstanden?"
  - "4 NAN: Im Druck „—“ statt „NAN“, wenn eine offene Zeile keinen Bestand hat (nur Vorschlag, nicht gebucht); gebucht von NAN bleibt „NAN“; gilt fuer den summierten Druck UND den Druck je Position (beide Standorte) - einverstanden, und Text „—“ oder „kein Bestand“?"
epic: false
etappen: []
deploy:
  web: true
  service: true
  migration: false
freigabe_entscheidung: ""
freigabe_von: ""
freigabe_am: ""
---

## Ziel / Nutzen (das Warum)

Die QA der Maske „Liste“ (Spec [[2026-11-26-beschichtung-summe-leitstand-hauptfa-kommissionierauftrag-liste-teil-3-spec]], v1.99.89,
Befunde R-1 bis R-5) fand keine Blocker, aber kleine Unschärfen: unsortierte Mehrfachwerte im Druck, ein Scan-Vergleich, der an
Groß/Klein scheitern kann, ein stilles Ausweichen beim PDF, eine an sieben Stellen kopierte Statusregel und ein wirkungsloser
Null-Fallback. Dazu zeigt der Druck „NAN“, wo nur „kein Bestand“ gemeint ist. Ziel: lesbarer Druck, eine Regel an einem Ort, kein
stilles Verhalten.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope:** R-1 bis R-5 und „NAN“ im Druck, wie im Soll beschrieben.

**Out-of-Scope:**
- Bug „Filter springt auf Bom“ – eigene Spec [[2026-11-26-kommissionierauftrag-filter-springt-auf-bom-spec]] (gleiches `Bom.cshtml`,
  deshalb davor).
- Quell-Lagerplatz-**Auswahl** auf der Seite: bleibt mit NAN vorbelegt (sonst kein Pflichtwert für die Buchung,
  `PickingTransferService.cs:74-75`; Fallstrick „Picking Source-Lagerplatz-Fallback ist NAN“).
- `PickingOrderRestmengen.Abgedeckt` (`Services/PickingOrderRestmengen.cs:24`) nutzt ebenfalls `IstMenge ?? 0m` – gleiche Beleglage wie
  R-5, aber nicht im Backlog; nur gemeldet, nicht mitbauen.
- Der Vorschlag nutzt den fest verdrahteten Code `"NAN"` (`PickingController.cs:409`, `:687`), die Negativbuchung dagegen die
  Einstellung `NegativeBuchungLagerplatz` (`PickingTransferService.cs:209`) – vorbestehende Asymmetrie, nicht Teil dieser Spec.
- Übrige Nutzer von `VormontageService.FormatTexte` (Vormontage, BDE-Teileliste, Kommissionierliste) bleiben ordinal sortiert.

## Fachliche Anforderungen

### Ist-Stand (belegt; Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, AppVersion 1.99.89)

- **R-1:** `FormatTexte` = verschiedene getrimmte Texte, `OrderBy(StringComparer.OrdinalIgnoreCase)`, „, “-getrennt
  (`Services/VormontageService.cs:113-118`). `Summiere` nutzt es für Baugruppe, Sage-Pos., Ebene, Vater-Sub-FA, Arbeitsbereich
  (`Services/KommissionierauftragListe.cs:43-47`); Seite (`_AuftragListeTabelle.cshtml:171`) und Druck (`PrintAuftragListe.cshtml:138`)
  zeigen denselben Wert. Ordinal: „10“ < „8“ < „9“ – QA-Befund KO-2026-00004, Artikel 50000469, Pos. 6.8/6.9/6.10. Die Spalte Pos. ist
  dagegen natürlich sortiert (`KommissionierauftragListe.cs:95`, `NaturalPositionComparer`).
  `NaturalPositionComparer` vergleicht je `.`-Teil numerisch, sonst `OrdinalIgnoreCase` (`Services/NaturalPositionComparer.cs:16-33`) –
  für reine Texte (Arbeitsbereich) also wie heute.
- **R-2:** Summenzeile trägt `data-article-number="@s.Ressourcenummer"` (`_AuftragListeTabelle.cshtml:62`), `Ressourcenummer` = `Norm` =
  `Trim().ToUpperInvariant()` (`KommissionierauftragListe.cs:90`, `:102`). Der Scan sucht exakt
  `tr[data-article-number="<getrimmter Scan>"]` (`Bom.cshtml:884-888`). Die Stückliste trägt den Rohwert (`Bom.cshtml:272`).
  Dieselbe View rendern auch `FaCompletionController.cs:416` und `FaWorklistController.cs:387`.
- **R-3:** `RenderAuftragDruckenAsync` druckt summiert nur bei `ansicht=liste` **und** Kommissionierlisten aktiv **und** Quelle
  FA-Struktur (`PickingController.cs:722-725`); sonst fällt es – auch mit `pdf=true` – still auf `PrintPicking` je Position
  (`:757-767`). Der PDF-Knopf erscheint nur in der Liste (`Bom.cshtml:518`), der Fall ist nur per Hand-URL erreichbar. Test
  `AuftragDrucken_ListeBeiAusgeschalteterKommissionierliste_JePosition_K3` (`PickingControllerTests.cs:511-519`) sichert das Ausweichen
  für den HTML-Druck ab.
- **R-4:** Die Regel „offen = `Offen` oder `Gepickt`“ steht wörtlich an sieben Stellen, alle auf geladenen Listen (kein EF-Ausdruck):
  `KommissionierauftragListe.cs:126`, `BomViewModels.cs:109` (`OffeneZeilenIds`), `PickingTransferService.cs:65` (Einzelprüfung) und
  `:120` (Expansion), `PickingOrderRestmengen.cs:62`, `PickingController.cs:479` (`IsTransferred` = nicht offen) und `:689` (Vorschlag nur
  für offene Zeilen). Andere Regeln, die **nicht** dazugehören: `AlleOffenenGepickt` (`BomViewModels.cs:111-112`),
  `PickingOrderWorkflow.cs:284` (Gepickt oder Umgebucht). Enum `PickingOrderLineStatus` (`Models/PickingOrder.cs:20-29`).
- **R-5:** `GebuchtIst = … umgebucht.Sum(z => z.IstMenge ?? 0m)` und `RestHinweis` mit `(z.IstMenge ?? 0m)`
  (`KommissionierauftragListe.cs:109`, `:117-119`). Schreibwege für `Status = Umgebucht` – alle setzen `IstMenge`:
  `PickingTransferService.cs:259-262` (`zeile.IstMenge = ist`, `ist` ist `decimal`) und das Einmal-Skript
  `SQL/Einmalig/2026-11-08_v1.88.0_Kommissionierauftraege_Altbestand.sql:131-133` (`IstMenge = p.Quantity` bei Status 20;
  `PickingItems.Quantity DECIMAL(18,3) NOT NULL`, `SQL/00_FreshInstall.sql:848`). Keine weiteren: Suche nach `Umgebucht`/`IstMenge =` in
  `IdealAkeWms`, `IDEALAKEWMSService`, `SQL/AgentJobs` ohne Treffer; der Abschluss setzt nur `Entfallen`/`Uebertragen`
  (`PickingOrderWorkflow.cs:212`).
- **NAN:** Druck je Position und summierter Druck nehmen `QuellPlatz(z)` = gespeicherter `SourceStorageLocationId`, sonst für offene
  Zeilen `QuellLagerplatzVorschlag.Waehle(…, nanId)` (`PickingController.cs:687-691`); ohne buchbaren Bestand liefert die Regel den
  Platz `NAN` (`Services/QuellLagerplatzVorschlag.cs:11-16`; Seed „Nicht zugeordnet (Fallback)“, `Program.cs:199-204`). Ausgabe:
  `PrintAuftragListe.cshtml:130` (über `FormatTexte`, `PickingController.cs:732`) und `PrintPicking.cshtml:193`. Die Spalte „Lagerplatz“
  zeigt ohne Bestand bereits „—“ (`PrintAuftragListe.cshtml:128`). Eine **gebuchte** Zeile mit gespeichertem Platz NAN ist eine echte
  Bewegung von NAN und kein Vorschlag.

### Soll

| ID | Anforderung |
|---|---|
| N1 (R-1) | Mehrwertige Zellen der Summenzeile (Baugruppe, Sage-Pos., Ebene, Vater-Sub-FA, Arbeitsbereich) natürlich sortiert (`NaturalPositionComparer`): „8, 9, 10“, „6.8, 6.10“. Gilt für Seite und Druck. *(Rückfrage 1)* |
| N2 (R-2) | Der Artikel-Scan findet Zeilen ohne Rücksicht auf Groß/Klein; Leerzeichen am Rand des Scans werden wie heute entfernt. *(Rückfrage 2: Geltungsbereich)* |
| N3 (R-3) | `AuftragDrucken` mit `pdf=true`, wenn der summierte Druck nicht möglich ist (keine `ansicht=liste`, Kommissionierlisten aus oder Quelle nicht FA-Struktur): kein HTML, sondern `TempData["WarningMessage"]` „PDF gibt es nur für den summierten Druck aus der Maske „Liste“ (Kommissionierlisten aktiv, Stückliste aus der FA-Struktur).“ und Redirect auf `Auftrag/{id}` (ohne `ansicht`, Standard-Maske). Ohne `pdf` unverändert (K3). *(Rückfrage 3)* |
| N4 (R-4) | Eine Regel `IstOffen()` für `PickingOrderLineStatus`; alle sieben Fundstellen nutzen sie. Verhalten und Meldungstexte unverändert. |
| N5 (R-5) | `GebuchtIst` und `RestHinweis` ohne `?? 0m`. Eine (nach Beleg unmögliche) umgebuchte Zeile ohne Ist zählt nicht und erzeugt keinen Rest-Hinweis – kein Fehler auf der Seite. |
| N6 (NAN) | Im Druck (summiert **und** je Position) steht „—“ statt „NAN“, wenn die Zeile offen ist und der Vorschlag der NAN-Fallback ist. Gespeicherte Plätze (auch NAN) werden unverändert gedruckt. In einer Summenzeile bleibt „—“ als eigener Wert sichtbar (z. B. „QA-LAGER-209, —“), damit eine Zeile ohne Bestand nicht in einem Mischwert verschwindet. Die Seite (Auswahlfeld) bleibt mit NAN vorbelegt. *(Rückfrage 4: Text und Geltungsbereich)* |

## Technischer Loesungsentwurf

Ponytail-Leiter: vorhandene Bausteine verwenden (`NaturalPositionComparer`, CSS-Attribut-Flag, `PdfRenderException`-Muster), keine
neue Klasse außer einer Ein-Zeilen-Erweiterung.

1. **R-1:** `FormatTexte(IEnumerable<string?> werte, IComparer<string?>? sortierung = null)` – Default wie heute `OrdinalIgnoreCase`,
   sonst die übergebene Sortierung (`VormontageService.cs:113-118`). `KommissionierauftragListe` reicht an den fünf Stellen `:43-47` den
   vorhandenen `Natuerlich` (`:13`) durch. Übrige Aufrufer unverändert (Default).
2. **R-2:** `Bom.cshtml:888` Selektor mit Flag `i`: `'…tr[data-article-number="' + CSS.escape(scannedValue) + '" i]'` (CSS Selectors
   Level 4, in den Terminal-Browsern vorhanden – im Dev-Lauf auf dem Android-Gerät prüfen). Kein neuer JS-Code. Bei Antwort
   „nur Liste“: Flag nur setzen, wenn die Tabelle `data-view-key="KommissionierauftragListe"` trägt.
3. **R-3:** In `RenderAuftragDruckenAsync` (`PickingController.cs:721-757`) nach dem Liste-Zweig, vor dem Druck je Position:
   `if (pdf) { TempData["WarningMessage"] = "…"; return RedirectToAction(nameof(Auftrag), new { id }); }` (Muster `:749-753`).
   Test: neuer Fall „pdf=true + Kommissionierlisten aus → Redirect + Warnung“; der vorhandene K3-Test (ohne `pdf`) bleibt grün.
4. **R-4:** In `Models/PickingOrder.cs` neben dem Enum:
   `public static class PickingOrderLineStatusRegel { public static bool IstOffen(this PickingOrderLineStatus s) => s is PickingOrderLineStatus.Offen or PickingOrderLineStatus.Gepickt; }`
   (Muster „eine Regel an einem Ort“ wie `ArbeitsgangStatusRegel`). Die sieben Stellen aus dem Ist-Stand auf `z.Status.IstOffen()` bzw.
   `!….IstOffen()` umstellen, `KommissionierauftragListe.Offen` entfällt. Keine Entity-Property (kein EF-Mapping-Risiko). Ein Theory-Test
   über alle fünf Enum-Werte.
5. **R-5:** `GebuchtIst = umgebucht.Count == 0 ? null : umgebucht.Sum(z => z.IstMenge)` (Summe über `decimal?` überspringt `null`);
   `RestHinweis` filtert `z.IstMenge < z.SollMenge` (gehobener Vergleich, `null` → false) und rechnet `z.SollMenge - z.IstMenge!.Value`
   nur für die gefilterten Zeilen. Bestehende Tests müssen unverändert grün bleiben.
6. **NAN:** `QuellPlatz` in `RenderAuftragDruckenAsync` (`:688-691`) durch eine Text-Funktion ersetzen, die beide Drucke nutzen:
   gespeicherter Platz → Code; offen und Vorschlag ≠ `nanId` → Code; offen und Vorschlag = `nanId` (oder kein Vorschlag) → „—“; sonst `null`.
   `PrintPickingItem.Lagerplatz` (`:764`) und `QuellLagerplatzText` (`:732`, über `FormatTexte`) bekommen diesen Text. Views
   unverändert. Testfall in `PickingControllerTests`: offene Zeile ohne Bestand → „—“, gebuchte Zeile mit NAN → „NAN“.
7. Frontend-Skill: nur Inhalt von Zellen, keine Layoutänderung – `frontend-design` trotzdem aufrufen (Pflicht bei View-Änderung).

## Migrations-/SQL-Auswirkungen

Keine. Kein Schema, keine Migration, keine Daten. `SQL/00_FreshInstall.sql`, `SQL/AgentJobs/` unberührt.

## Einstellungen

keine

| Key | Typ | Erlaubte Werte | Default | Wirkung | Verwendung | Standort | Warnhinweis |
|---|---|---|---|---|---|---|---|
| – | – | – | – | – | – | – | – |

## Audit-Feld-Auswirkungen

Keine. Kein neuer oder geänderter Schreibweg; R-4 ändert in `PickingTransferService` nur die Schreibweise der Prüfung, die Buchung
(`:259-265`, Audit-Felder) bleibt unverändert.

## Akzeptanzkriterien

1. (R-1) Liste und summierter Druck von KO-2026-00004 (DEV IDEAL), Artikel 50000469: Sage-Pos. „8, 9, 10“ (gleiche Reihenfolge wie Pos. 6.8, 6.9, 6.10).
2. (R-1) Vormontage-Summiert und Kommissionierliste zeigen ihre Mehrfachwerte unverändert (Stichprobe).
3. (R-2) Scan einer Artikelnummer in anderer Groß/Klein-Schreibung als in der Zeile hakt die Zeile an (Liste; je Antwort 2 auch Stückliste). Unit-Test nicht möglich (Browser-JS) – Manual-UAT mit Testeingabe in das Scan-Feld.
4. (R-3) `/Picking/AuftragDrucken/{id}?ansicht=liste&pdf=true` an einem AKE-Auftrag bzw. IDEAL mit Kommissionierlisten aus → Auftragsseite mit Warnung (Text aus N3), keine Datei, kein HTML-Druck. Ohne `pdf` → Druck je Position wie bisher.
5. (R-3) Test `PickingControllerTests`: pdf=true ohne möglichen summierten Druck → `RedirectToActionResult` „Auftrag“ + `WarningMessage`.
6. (R-4) Keine Fundstelle der Form `Offen or PickingOrderLineStatus.Gepickt` mehr außerhalb der Regel (Suche im Dev-Lauf); Theory-Test über alle Enum-Werte grün; alle bestehenden Tests grün.
7. (R-5) `KommissionierauftragListeTests` (Rest, gebuchte Summe) unverändert grün.
8. (NAN) Summierter Druck und Druck je Position: offene Zeile ohne Bestand zeigt „—“ (bzw. Text aus Antwort 4); Zeile, die von NAN gebucht wurde, zeigt „NAN“; Summenzeile mit gebuchtem Platz und offener Zeile ohne Bestand zeigt beide Werte.
9. (NAN) Seite: Quell-Lagerplatz-Auswahl einer offenen Zeile ohne Bestand bleibt mit NAN vorbelegt, Umbuchung funktioniert wie bisher.
10. Build und alle Tests grün.

## Test-Szenarien

Ergänzung zum Kapitel TS-209 (Kommissionierauftrag Maske „Liste“) in `docs/TESTSZENARIEN.md` – neue Schritte 209.x (Dev-Lauf vergibt die
Nummern nach dem Bugfix), danach `secondbrain/tests/testszenarien-index.md`.

- **Vorbedingung:** IDEAL-DEV mit `FaHierarchyKommissionierlistenAktiv = true`, KO-2026-00004 (oder ein Auftrag mit einem Artikel an ≥ 3
  Positionen x.8/x.9/x.10), ein Artikel ohne buchbaren Bestand, ein Artikel mit Bestand; AKE-DEV mit offenem Auftrag (R-3, NAN je Position).
- **x.1 (AK 1/2)** Liste und Druck: Sage-Pos. natürlich sortiert; Vormontage-Summiert Stichprobe unverändert.
- **x.2 (AK 3)** Scan-Feld: Artikelnummer klein bzw. mit Leerzeichen eingeben → Zeile angehakt; bei IDEAL zusätzlich ein Artikel mit
  Buchstaben suchen (falls vorhanden, sonst nur AKE-Stückliste).
- **x.3 (AK 4)** PDF-URL per Hand an AKE-Auftrag → Warnung auf der Auftragsseite; ohne `pdf` Druck je Position.
- **x.4 (AK 8/9)** Druck aus Liste und Stückliste: offene Zeile ohne Bestand „—“; eine Zeile mit NAN als Quelle umbuchen (Negativ), erneut
  drucken → „NAN“ bei der gebuchten Zeile; Seite weiterhin NAN vorbelegt.
- **Negativ:** Druck eines abgeschlossenen Auftrags – nicht gebuchte Zeilen (Entfallen/Übertragen) zeigen keinen Quell-Platz (wie heute).
- R-4/R-5: nur automatisiert (reine Umstellung ohne Verhaltensänderung).

## Etappen (nur bei epic: true)

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | kein Epic | – | – |

## Deploy

*Vorläufig – der QA-Lauf bestätigt gegen den echten Diff.*

- **Web-App:** ja (Controller, Services, Modelle, `Bom.cshtml`)
- **Service:** nur Versionsnummer (`IDEALAKEWMSService/AppVersion.cs`). Der Dienst bindet das Web-Projekt per ProjectReference ein
  (`Services/FaZusatzinfoStatus.cs:6`, `Services/Beschichtung/BeschichtungResolver.cs:48`) und kompiliert die neue Erweiterung mit; er nutzt
  `PickingOrderLine` nicht (Suche in `IDEALAKEWMSService` ohne Treffer) – keine Logikänderung.
- **Migration:** nein
- **Manifest-Einträge** (`scripts/deploy/nachlauf.psd1`): keine (kein SQL, keine Einstellung, kein Leitstand-Schritt; Änderungen ohne
  Bedienwirkung, die eine Anwender-Info bräuchten – der „—“-Druck steht im Changelog).
- **Publish-Befehle:** `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb` (im Worktree).

## Offene Rueckfragen

1. **R-1 Reihenfolge der Mehrfachwerte.** „Natürliche Reihenfolge wie die Spalte Pos.“ lässt zwei Lesarten zu: (a) die verschiedenen
   Werte natürlich sortiert („8, 9, 10“) oder (b) in der Reihenfolge der Positionen (Sage-Pos. der 1. Position zuerst). Im QA-Beispiel
   ergeben beide dasselbe; sie weichen nur ab, wenn Sage-Pos. und Pos. nicht gleich laufen, und (b) bleibt wegen der Zusammenfassung
   gleicher Werte ohnehin nicht spaltengenau. **Empfehlung:** (a) – eine Zeile Code, gleiche Regel für alle fünf Felder.
2. **R-2 Geltungsbereich des Groß/Klein-freien Scans.** Der Scan-Code ist einer für alle Nutzer von `Bom.cshtml` (Auftrag Liste und
   Stückliste, FA-Stückliste, FA-Vervollständigung `FaCompletionController.cs:416`, Werkbank `FaWorklistController.cs:387`).
   **Empfehlung:** überall (dieselben Ressourcenummern; heute würde ein Scan in Kleinbuchstaben auch in der Stückliste scheitern). Alternative:
   nur in der Liste (Bedingung auf den View-Key).
3. **R-3 Umfang der Meldung.** **Empfehlung:** nur `pdf=true` ohne möglichen summierten Druck → Warnung + Rücksprung (ein PDF-Wunsch, der
   HTML liefert, ist eindeutig falsch). `?ansicht=liste` ohne `pdf` bleibt beim Druck je Position, wie die Seite `?ansicht=liste` ignoriert
   (K3, AK 14 der Teil-3-Spec, Test `PickingControllerTests.cs:511-519`). Alternative: auch dort Warnung statt Druck.
4. **NAN im Druck.** **Empfehlung:** „—“ wie die Spalte „Lagerplatz“ ohne Bestand (`PrintAuftragListe.cshtml:128`), für den summierten
   Druck **und** den Druck je Position (gleiche Funktion `QuellPlatz`, `PickingController.cs:688-691`; Druck je Position gilt auch an der
   AKE). Gebuchte Zeilen von NAN bleiben „NAN“ (echte Bewegung). Alternative Texte: „kein Bestand“; oder nur im summierten Druck.

## ANTWORTEN auf Rueckfragen - Fachbereich (aus Jira)

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
  Bei Varianten-Specs zusaetzlich freigabe_entscheidung im Frontmatter setzen.
-->

1. R-1 Mehrfachwerte natürlich sortiert (a)? →
2. R-2 Groß/Klein-freier Scan in allen Masken der Stücklisten-Seite? →
3. R-3 Warnung nur bei `pdf=true`, HTML-Druck `ansicht=liste` bleibt Ausweichen je Position? →
4. NAN im Druck: „—“ (oder „kein Bestand“?), für summierten Druck und Druck je Position? →
