---
type: spec
title: "FA-Struktur Arbeitsgänge als Symbole — Teil 2: Status-Farben bei aktivem BDE"
slug: 2026-10-06-fa-struktur-arbeitsgaenge-symbole-teil-2-spec
status: Entwurf
created: 2026-10-06
updated: 2026-10-06
source_backlog: "[[2026-10-06-fa-struktur-arbeitsgaenge-symbole]]"
depends_on: "[[2026-10-06-fa-struktur-arbeitsgaenge-symbole-teil-1-spec]]"
task: ""
worktree: ""
branch: ""
affected_code:
  - "IdealAkeWms/Data/Repositories/IWorkOperationRepository.cs + WorkOperationRepository.cs — EINE gebuendelte Status-Abfrage fuer eine Menge WorkOperation-Ids (abgeschlossen = BdeBookingQuantity.IsFinal auf nicht stornierter Buchung, Praedikat wie BdeApiController.cs:252-256; ggf. IsReported je Rueckfrage 3; ggf. laufende Buchung je Rueckfrage 5)"
  - "IdealAkeWms/Models/ViewModels/FaHierarchyTreeViewModel.cs — WorkOperationSummary (Z.210) um Status erweitern; Ableitung offen/erledigt/ueberfaellig als reine, unit-testbare Funktion"
  - "IdealAkeWms/Controllers/FaHierarchyController.cs — Status-Abfrage dort, wo Teil 1 die Arbeitsgaenge laedt (Variante A: Index Z.52-133; Variante B: WorkOperations Z.139-155); BDE-Aktiv-Gate je Rueckfrage 4 (IAppSettingRepository ist bereits injiziert, Z.29)"
  - "IdealAkeWms/Views/FaHierarchy/_FaWorkOperationsModal.cshtml bzw. _FaHierarchyNode.cshtml — Statusdarstellung am Symbol aus Teil 1 (Form/Icon + Text + Farbe)"
  - "IdealAkeWms/wwwroot/css/site.css — Statusfarben mit WCAG-AA-Kontrast"
  - "IdealAkeWms.Tests — Status-Ableitung (Tabellentest), Repository-Abfrage (InMemory), ToQueryString-Test falls Contains ueber viele Ids"
  - "docs/TESTSZENARIEN.md + secondbrain/tests/testszenarien-index.md — TS-Kapitel naechste freie Nummer zum Umsetzungszeitpunkt"
open_questions:
  - "Farbe nie alleiniger Bedeutungstraeger (Pflicht-Leitplanke): WIE genau wird der Status zusaetzlich codiert — Form/Icon je Status und Text (Tooltip, Screenreader)? Empfehlung der Notiz: Form/Icon und Text zusaetzlich zur Farbe, Kontrast WCAG AA."
  - "Ueberfaellig-Definition: Es gibt kein Soll-Datum je Arbeitsgang. Welcher Auftragstermin gilt (Empfehlung der Notiz: z. B. FE_Termin ueberschritten und Arbeitsgang nicht rueckgemeldet; Kandidat laut View-Doku auch Neuer_PT_PPS)? Und welche Zeile gilt bei Kombigeraeten mit mehreren Kopfzeilen je HauptFA?"
  - "Was heisst 'erledigt' (gruen)? Am Code gibt es zwei getrennte Signale: das BDE-Terminal wertet eine Abschlussmeldung (BdeBookingQuantity.IsFinal auf nicht stornierter Buchung) aus, WorkOperation.IsReported setzt nur die manuelle Rueckmeldung im Tracking. Nur IsFinal, nur IsReported oder eines von beiden?"
  - "Was heisst 'bei aktivem BDE'? Globaler AppSetting-Schalter BdeAktiv oder je Werkbank ProductionWorkplace.BdeAktiv (oder beides)? Was zeigen die Symbole, wenn BDE nicht aktiv ist — neutral wie Teil 1?"
  - "Ein Arbeitsgang mit laufender (gestarteter, nicht abgeschlossener) Buchung: grau wie offen, oder ein eigener vierter Zustand 'laeuft'?"
epic: false
etappen: []
deploy:
  web: true
  service: false
  migration: false
freigabe_entscheidung: ""
freigabe_von: ""
freigabe_am: ""
# Flache Schluessel mit Absicht: Obsidians Property-Editor kann verschachtelte
# YAML-Objekte NICHT bearbeiten - und genau diesen Block fuellt der Mensch aus.
---

> **Umsetzungsort (Hinweis, keine Entscheidung):** nach Schranke 2 des IDEAL-Bündels, aus `main`
> (neuer Worktree). Code-Grundlage dieser Spec ist der Bündel-Stand
> `.claude/worktrees/2026-08-07-ideal-teile-1-5` (v1.47.1); alle Zeilenangaben beziehen sich darauf und
> sind nach dem Merge gegen `main` nachzuprüfen. TS-Nummer und Version: nächste freie zum
> Umsetzungszeitpunkt. `worktree`/`branch` setzt der Dev-Lauf.
>
> **Abhängigkeit:** setzt [[2026-10-06-fa-struktur-arbeitsgaenge-symbole-teil-1-spec]] voraus (die
> Symbole, die hier eingefärbt werden). Wo die Status-Abfrage läuft, folgt aus Rückfrage 1 von Teil 1.

## Ziel / Nutzen (das Warum)

Bei aktivem BDE soll an den Arbeitsgang-Symbolen aus Teil 1 der Fortschritt ablesbar sein:
**Grün = erledigt, Grau = offen, Rot = überfällig** (Anforderung Gerald,
[[2026-10-06-fa-struktur-arbeitsgaenge-symbole]]). Planung und Fertigungsleitung sehen so je Auftrag,
welche Arbeitsgänge fehlen oder hängen, ohne das BDE-Cockpit zu durchsuchen. Standort: **IDEAL**
(Arbeitsgänge aus der Struktur-Erkennung, Termine aus den IDEAL-Sage-Views).

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**
- Status je Arbeitsgang ableiten (live, nicht gespeichert) und am Symbol aus Teil 1 darstellen.
- Statuscodierung über Farbe **und** Form/Icon **und** Text (Rückfrage 1).
- Gate „BDE aktiv“ (Rückfrage 4).

**Out-of-Scope**
- Kein Soll-Datum je Arbeitsgang einführen (kein Datenmodell-Umbau; „überfällig“ wird aus dem
  Auftragstermin abgeleitet, Rückfrage 2).
- Keine Änderung an BDE-Buchungslogik, Terminal, Tracking-Rückmeldung.
- BDE-Auftragsanzeige → Teil 3.

## Fachliche Anforderungen

1. **Status ist abgeleitet, nicht gespeichert.** Die Quellen können sich nachträglich ändern:
   Buchungskorrekturen setzen `IsFinal` um (`BdeBookingsController.cs:236`, `q.IsFinal = row.IsFinal`),
   Buchungen können storniert werden (`IsCancelled`), die Tracking-Rückmeldung ist umkehrbar
   (`TrackingController.cs:283-286`, `UndoReport`). Ein gespeicherter Status würde veralten.
2. **„Erledigt“ — Befund am Code (entscheidet Rückfrage 3):**
   - Das BDE-Terminal betrachtet einen Arbeitsgang als abgeschlossen, wenn eine
     `BdeBookingQuantity` mit `IsFinal = true` auf einer **nicht stornierten** Buchung dieses Arbeitsgangs
     existiert (`BdeApiController.cs:251-257`, Ausschluss aus der Auswahl `:273`). `IsFinal = true`
     schreibt `BdeBookingService` beim Beenden mit Menge (`BdeBookingService.cs:357-361`).
   - `WorkOperation.IsReported`/`ReportedAt` werden im gesamten Code **nur** von der manuellen
     Rückmeldung im Tracking-Modul geschrieben: `TrackingController.Report` (`TrackingController.cs:254-255`,
     `ReportedAt = DateTime.UtcNow`) und `UndoReport` (`:283-284`); Anlage mit `IsReported = false`
     (`WorkOperationStructureDetectionService.cs:175`). Weder `BdeBookingService` noch ein Service-Sync noch
     ein Skript unter `SQL/` (inkl. `SQL/AgentJobs/`) setzt sie (Grep über `*.cs`/`*.sql`, Migrationen
     ausgenommen). **Die BDE-Buchung setzt `IsReported` also nicht.**
   - `IsReportable` wird bei IDEAL immer `true` gesetzt (`WorkOperationStructureDetectionService.cs:173`,
     `BdeDefaultWorkOperationService.cs:41`) und nur im Tracking gelesen (`_OperationStatusBadge.cshtml:10`,
     `Views/Tracking/*`) — als Statusquelle hier ohne Aussagekraft.
   - Achtung Zeitbasis: `ReportedAt` ist **UTC** (`TrackingController.cs:255`), `BdeBookingQuantity.ReportedAt`
     ist **Lokalzeit** (`BdeBookingService.cs:386`, `DateTime.Now`). Wird ein Zeitpunkt angezeigt oder
     verglichen, muss das berücksichtigt werden.
3. **„Offen“ (grau):** nicht erledigt und nicht überfällig. Laufende Buchung: Rückfrage 5.
4. **„Überfällig“ (rot):** nicht erledigt **und** der in Rückfrage 2 festgelegte Auftragstermin liegt vor
   heute (Lokaldatum, nur Datumsteil — die Sage-Spalten sind `date`, siehe unten).
5. **Termine — Befund an der Quelle (entscheidet Rückfrage 2):**
   - `FaHierarchyOrderInfo.KO_Termin`/`FE_Termin`/`Start_Beschichtung` (und `Neuer_PT_PPS`,
     `Verladetermin_Vsl`) sind **1:1-Kopien** der Spalten der Sage-View
     `vw_IDEAL-AKE_Kommissionierung_FAInfos` (`FaHierarchySql.cs:135-143`, Zuordnung
     `FaHierarchySyncService.cs:285-290`), keine Umrechnung.
   - Bedeutung laut View-Dokumentation (`SQL/sage-views/vw_IDEAL-AKE_Kommissionierung_FAInfos.sql:26-37`):
     `KO_Termin` „Konstruktions-Termin“, `FE_Termin` „Wann muss die Blechfertigung fertig sein“,
     `Start_Beschichtung` „Wann Beschichtung starten“, `Neuer_PT_PPS` „Aktueller PPS-Fertigstellungstermin“.
     Quelle der View ist `[PPSImport].[dbo].[vw_PPSData]` (Z.9); Spaltennamen/Typen sind gegen die reale View
     verifiziert (Z.13), der **View-Text selbst ist nicht im Repo** (Z.16-17, Z.50) — die Beschreibungen sind
     damit **nicht** am Datenfluss geprüft. **Unverifiziert**, siehe Prüfschritt P1.
   - Termine liegen **nur je HauptFA**, nicht je Sub-FA: `FaHierarchyNode` hat kein Datumsfeld außer
     `SyncedAt` (`FaHierarchyNode.cs:62`). Bei Kombigeräten gibt es mehrere `FaHierarchyOrderInfo`-Zeilen je
     HauptFA (`FaHierarchyOrderInfo.cs:7-9`), die Kopfzeile der Baumanzeige zeigt dann bewusst **keinen**
     Termin (`Index.cshtml:149-151`).
   - Bestehende Verwendung zur Orientierung: Die FA-Liste nutzt `FE_Termin` als Produktionsdatum
     (`ProductionOrdersController.cs:299`), die Vormontage-Listen filtern auf `FE_Termin`, die
     Kommissionierlisten auf `KO_Termin` (`VormontageService.cs:169`, `KommissionierListenService.cs:164`).
6. **Gate „BDE aktiv“ (Rückfrage 4) — Befund:** Es gibt zwei Schalter. Global `AppSettingKeys.BdeAktiv`
   (`AppSettingKeys.cs:9`; gelesen von `RequireBdeActiveAttribute.cs:24` und `HomeController.cs:36`) und je
   Werkbank `ProductionWorkplace.BdeAktiv` (`ProductionWorkplace.cs:21`; das Terminal bietet nur Arbeitsgänge
   BDE-aktiver Werkbänke an, `BdeApiController.cs:197`). Ohne aktives BDE: Symbole wie in Teil 1 (neutral) —
   vorbehaltlich Rückfrage 4.
7. **Leitplanken (verbindlich):** Farbe ist nie alleiniger Bedeutungsträger — jeder Status hat zusätzlich
   eine eigene Form/ein Icon **und** einen Text (sichtbar oder `aria-label`/visuell versteckt, nicht nur
   `title`, der an Touch-Terminals nicht erscheint). Kontrast WCAG AA (4,5:1 Text, 3:1 Konturen/Icons), auch
   bei Rot/Grün-Sehschwäche unterscheidbar. Die genaue Codierung legt Rückfrage 1 fest.

## Technischer Loesungsentwurf

- **Eine gebündelte Abfrage** je Ladevorgang über die Ids der angezeigten Arbeitsgänge — kein N+1. Prädikat
  „erledigt“ identisch zum Terminal (`BdeApiController.cs:252-256`):
  `BdeBookingQuantities.Where(q => q.IsFinal && !q.BdeBooking.IsCancelled && ids.Contains(q.BdeBooking.WorkOperationId))`.
  Wiederverwendung statt Neuerfindung: dieselbe Bedingung als Repository-Methode; das Terminal selbst
  bleibt unverändert (kein Refactoring außerhalb des Umfangs).
- Laufende Buchung (falls Rückfrage 5 einen eigenen Zustand will): `BdeBookings` mit `EndedAt == null`
  und `!IsCancelled` (Muster `BdeApiController.cs:260-268`; „pausiert“ hat `EndedAt` gesetzt,
  [[fallstricke]] §6 „`Paused` hat `EndedAt` gesetzt“).
- Termin: aus den bereits geladenen `FaHierarchyOrderInfo` der Struktur (Variante A: im Index ohnehin
  geladen, `FaHierarchyController.cs:68`; Variante B: der Modal-Endpunkt baut den Baum heute **ohne**
  OrderInfos, `FaHierarchyController.cs:148` — dort ist der Kopf der einen HauptFA nachzuladen, z. B. über
  das vorhandene `IFaHierarchyOrderInfoRepository`, `:27`).
- Statusableitung als reine Funktion (Eingaben: erledigt?, läuft?, Termin, heute) → Tabellentest.
- Variante A (Baum): Contains über viele Ids → EF 10 `OPENJSON` mit einem Parameter
  ([[fallstricke]] §8), per ToQueryString-Test belegen.
- **frontend-design-Skill ist im Dev-Lauf Pflicht** (Views/CSS). Bootstrap-5-Farben/Icons der Anwendung,
  keine neue Icon-Bibliothek ohne ADR.

## Migrations-/SQL-Auswirkungen

Keine (Status wird abgeleitet). Keine Migration, `SQL/00_FreshInstall.sql` und `SQL/AgentJobs/` unberührt.
Sollte Rückfrage 2 einen Termin verlangen, der heute nicht importiert wird, entstünde eine Migration —
das wäre eine Umfangsänderung und ist dann neu zu spezifizieren. Alle Kandidaten (`KO_Termin`, `FE_Termin`,
`Start_Beschichtung`, `Neuer_PT_PPS`, `Verladetermin_Vsl`) werden bereits importiert
(`FaHierarchySql.cs:140-141`).

## Audit-Feld-Auswirkungen

Keine — reine Lesedarstellung.

## Rollen / Zugriffsfilter

Unverändert `[RequirePickingOrTrackingOrLeitstandAccess]` (`FaHierarchyController.cs:23`). Die Ansicht
zeigt BDE-Fortschritt jetzt auch Rollen ohne BDE-Rolle (`bde_user`/`bde_shiftlead`/`bde_admin`,
`RoleKeys.cs:14-16`) — es ist nur ein Status je Arbeitsgang, keine Bediener- oder Zeitdaten. Falls das nicht
gewollt ist, gehört es in die Antwort zu Rückfrage 4.

## Listen-View-Pattern (ADR 0005)

Kein neuer Listen-View. Falls Teil 1 Variante A gewählt ist: die Status-Spalte ist dieselbe Spalte wie in
Teil 1 (keine weitere Registrierung); ein Statusfilter in der Filterzeile wäre neu und ist nicht verlangt.

## Akzeptanzkriterien

1. BDE aktiv (laut Rückfrage 4): Ein Arbeitsgang mit einer `IsFinal`-Menge auf nicht stornierter Buchung
   (bzw. dem in Rückfrage 3 festgelegten Signal) erscheint als **erledigt** (grün + Erledigt-Form + Text).
2. Ein nicht erledigter Arbeitsgang, dessen maßgeblicher Auftragstermin (Rückfrage 2) vor heute liegt,
   erscheint als **überfällig** (rot + eigene Form + Text).
3. Alle übrigen erscheinen als **offen** (grau + eigene Form + Text); laufende gemäß Rückfrage 5.
4. Storno der abschließenden Buchung oder Korrektur `IsFinal → false` lässt das Symbol beim nächsten Laden
   wieder offen/überfällig werden (kein gespeicherter Status).
5. Ohne Auftragstermin (`null`) ist ein Arbeitsgang nie überfällig.
6. BDE nicht aktiv: Symbole wie Teil 1, keine Statusfarben.
7. Jeder Status ist ohne Farbwahrnehmung erkennbar (Graustufen-Screenshot) und per Screenreader angesagt.
8. Kontrast aller Statusvarianten ≥ WCAG AA, gemessen am Testsystem.
9. Je Ladevorgang genau **eine** zusätzliche Status-Abfrage (Query-Zählung im Dev-Lauf).
10. Status-Ableitung durch Tabellentest abgedeckt (erledigt vor überfällig; Termin heute = nicht überfällig;
    Termin `null`; Kombigerät-Regel aus Rückfrage 2).

## Test-Szenarien

Neues Kapitel in `docs/TESTSZENARIEN.md` (Nummer: nächste freie zum Umsetzungszeitpunkt), Index nachziehen.

- **Vorbedingungen:** IDEAL-Testsystem, BDE aktiv (laut Rückfrage 4), Werkbank `BdeAktiv`, Struktur mit
  ≥ 3 erkannten Arbeitsgängen, Kopfzeile mit Termin in der Vergangenheit und eine mit Termin in der Zukunft.
- **S1 Erledigt:** Am BDE-Terminal einen AG starten und mit Abschluss beenden → Symbol grün/Erledigt-Form.
- **S2 Offen:** AG ohne Buchung, Termin in der Zukunft → grau.
- **S3 Überfällig:** AG ohne Abschluss, Termin gestern → rot/Überfällig-Form.
- **S4 Storno/Korrektur:** Abschlussbuchung im BDE-Buchungs-Editor auf „nicht final“ setzen oder stornieren
  → Symbol nach Neuladen nicht mehr grün.
- **S5 Laufend:** AG gestartet, nicht beendet → Darstellung laut Rückfrage 5.
- **S6 BDE aus:** BDE deaktivieren → neutrale Symbole.
- **S7 Barrierefreiheit:** Graustufen-Ansicht + Screenreader + Kontrastmessung am Fertigungsterminal.
- **Negativ:** Kombigerät mit abweichenden Terminen → Verhalten laut Rückfrage 2; Struktur ohne Kopfdaten
  (`FaHierarchyOrderInfo` fehlt) → nie überfällig, kein Fehler.

## Pruefschritte fuer den Menschen (nur Sage/PPS beantwortet)

**P1 — Bedeutung der Termine (vor Antwort auf Rückfrage 2).** Auf der IDEAL-Sage-Instanz:

```sql
SELECT TOP 50 HauptFA, Abteilung, [Status], KO_Termin, FE_Termin, Start_Beschichtung,
       Neuer_PT_PPS, Verladetermin_Vsl
FROM dbo.[vw_IDEAL-AKE_Kommissionierung_FAInfos]
ORDER BY FE_Termin DESC;
```

Mit der PPS-Verantwortlichen abgleichen: Ist `FE_Termin` der Termin, bis zu dem **alle** Fertigungs-
Arbeitsgänge der Struktur erledigt sein müssen, oder nur die Blechfertigung? Ist `Neuer_PT_PPS` ein
verschobener Ersatz für `FE_Termin`? Zusätzlich den View-Text sichern:
`SELECT OBJECT_DEFINITION(OBJECT_ID(N'dbo.[vw_IDEAL-AKE_Kommissionierung_FAInfos]'));`

**P2 — Kombigeräte mit abweichenden Terminen:**

```sql
SELECT HauptFA, COUNT(*) AS Zeilen,
       COUNT(DISTINCT FE_Termin) AS FE_Varianten,
       COUNT(DISTINCT Neuer_PT_PPS) AS PT_Varianten
FROM dbo.[vw_IDEAL-AKE_Kommissionierung_FAInfos]
GROUP BY HauptFA
HAVING COUNT(*) > 1;
```

Gibt es Treffer mit `FE_Varianten > 1`, braucht Rückfrage 2 eine Regel (z. B. frühester Termin).

## Etappen (nur bei epic: true)

Entfällt (`epic: false`).

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | – | – | – |

## Deploy

**Vorläufig — der QA-Lauf bestätigt gegen den echten Diff.**

- **Web-App:** ja (Repository, ViewModel, Controller, View, CSS).
- **Service:** nein.
- **Migration:** nein.
- **Publish-Befehle:**

```bash
dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb
```

## Offene Rueckfragen

1. **Codierung ohne Farbe — wie genau?** (Notiz-Punkt 3) „Farbe nie alleiniger Bedeutungsträger“ ist
   Pflicht (CLAUDE.md, Fertigungsterminals). Empfehlung der Notiz: Status zusätzlich über Form/Icon und
   Text (Tooltip, Screenreader), Kontrast WCAG AA. Zu entscheiden: welche Form/welches Icon je Status
   (z. B. Haken / leerer Kreis / Warndreieck), und ob der Statustext sichtbar am Symbol steht oder nur
   per `aria-label` (Hinweis: `title` erscheint an Touch-Terminals nicht).
2. **„Überfällig“ — welcher Termin?** (Notiz-Punkt 4) Es gibt kein Soll-Datum je Arbeitsgang. Empfehlung
   der Notiz: z. B. Auftragstermin (`FE_Termin`?) überschritten und Arbeitsgang nicht rückgemeldet; welcher
   Termin gilt, entscheidet Gerald. Befund: Kandidaten sind `FE_Termin` („Blechfertigung fertig“ laut
   View-Doku) und `Neuer_PT_PPS` („aktueller PPS-Fertigstellungstermin“) — beide unverifiziert, siehe P1.
   Teilfrage: Welche Zeile gilt bei Kombigeräten mit mehreren Kopfzeilen je HauptFA (siehe P2)?
3. **„Erledigt“ — welches Signal?** (neu, aus dem Code-Befund) BDE-Abschluss (`IsFinal`, wie das Terminal)
   oder manuelle Tracking-Rückmeldung (`IsReported`) oder eines von beiden? Empfehlung der Spec: `IsFinal`,
   weil die Anforderung „bei aktivem BDE“ lautet und das Terminal genau dieses Signal nutzt; die
   Entscheidung bleibt beim Menschen.
4. **„Bei aktivem BDE“ — welcher Schalter?** (neu, aus dem Code-Befund) Globales `BdeAktiv` (AppSetting),
   `BdeAktiv` der Werkbank des Arbeitsgangs, oder beides? Und: neutrale Symbole bei BDE aus — richtig?
5. **Laufende Buchung** (neu, aus dem Code-Befund): grau wie „offen“ oder ein vierter Zustand „läuft“
   (eigene Form/Text)?

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
  Bei Varianten-Specs zusaetzlich freigabe_entscheidung im Frontmatter setzen.
-->

1. Codierung ohne Farbe — Form/Icon je Status, Statustext sichtbar oder nur aria-label? →
2. Überfällig — welcher Termin (FE_Termin / Neuer_PT_PPS / anderer), Regel bei Kombigeräten? →
3. Erledigt — IsFinal (BDE), IsReported (Tracking) oder eines von beiden? →
4. BDE aktiv — globaler Schalter, Werkbank-Schalter oder beides; neutral bei BDE aus? →
5. Laufende Buchung — grau oder eigener Zustand „läuft“? →
