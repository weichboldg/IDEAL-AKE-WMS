---
type: spec
title: "IDEAL: BDE-Arbeitsgaenge (WorkOperation) aus der Struktur + Werkbank-Anlage aus Sage-Arbeitsplaetzen"
slug: 2026-09-21-ideal-bde-arbeitsgaenge-aus-struktur-spec
status: Entwurf
created: 2026-09-21
updated: 2026-09-22
source_backlog: "[[2026-09-21-ideal-bde-arbeitsgaenge-aus-struktur]]"
supersedes: "[[2026-09-08-arbeitsgaenge-aus-arbeitsschritte-spec]]"
depends_on: "[[2026-09-21-adr-0014-arbeitsbereich-ist-zielort-spec]]"
task: ""
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
affected_code:
  - "IdealAkeWms/Models/ProductionWorkplace.cs — ZWEI neue Felder: `SageArbeitsplatznummer` (der Verknuepfungsschluessel, Typ/Laenge gegen das echte Sage-Schema verifizieren — Offene Rueckfrage 9) + `ArbeitsschrittCode` (string?, MaxLength 20, getrimmt) — Baustein a"
  - "IdealAkeWms/Migrations/*_AddProductionWorkplaceSageFields.cs (NEU, beide Spalten in EINER Migration)"
  - "SQL/93_AddProductionWorkplaceSageFields.sql (NEU, je Spalte ein COL_LENGTH-Guard; Nummer verifiziert 2026-09-22 gegen den Worktree-Stand, siehe Offene Rueckfrage 7)"
  - "SQL/00_FreshInstall.sql (Schema-Objekte + MigrationId, beide Stellen)"
  - "IDEALAKEWMSService/Services/ProductionWorkplaceSyncService.cs (NEU) + IProductionWorkplaceSyncService.cs (NEU) — Baustein a, LEGT fehlende Werkbaenke aus Sage `KHKPpsArbeitsplaetze` an (Sage fuehrend fuer Nummer/Name/Kuerzel), zieht Abweichungen bei bestehenden Treffern nach UND meldet sie (ADR-0014-Melde-Muster bleibt fuer Abweichungen, nur die Anlage kehrt sich um)"
  - "IdealAkeWms/Models/ServiceSettingDefinitions.cs — drei neue Keys: `Sync:ProductionWorkplaceSyncEnabled` (Bool, Default false), `Sync:ProductionWorkplaceSyncMandant` (Int, Default 1), `Sync:ProductionWorkplaceSyncAusschlussliste` (String, Default `STO,XXX,x01,BS,BS2,FRE,AKE`) — Baustein a"
  - "IdealAkeWms/Models/Standort/StandortSettingsCatalog.cs — zwei neue `StandortField`-Eintraege (Backend `Service`, neue Gruppe z. B. `GroupWerkbaenke`/„Werkbaenke (Sage-Arbeitsplaetze)") fuer den Mandant-Filter und die Ausschlussliste, damit beide ohne Deploy aenderbar/sichtbar sind. WICHTIG: NICHT den bestehenden Eintrag `SData:Dataset` wiederverwenden — der ist der SData-URL-Dataset-String der Sage-Lagerbuchung (`ake_TEST2026;1`), keine Zahl; der Mandant-Filter dieser Spec braucht einen eigenen, numerischen Key"
  - "IdealAkeWms/Services/SyncLogger/SyncLogServices.cs — neue Konstante `ProductionWorkplaceSync` (Baustein a)"
  - "IDEALAKEWMSService/Workers/SyncWorker.cs — neuer Gate-Block Baustein a (Doppel-Gate: Master `ProduktionsauftragHierarchisch` UND eigener Toggle, S1 — sonst liest der Sync bei AKE ins Leere und meldet Rauschen); UMBAU des bestehenden Blocks 'FA-Arbeitsgang-Erkennung (Struktur)' (Baustein b, Zielservice + Key umbenannt)"
  - "IDEALAKEWMSService/Program.cs — DI-Registrierung Baustein a (neu) + Baustein b (umbenannt)"
  - "IdealAkeWms/Views/ProductionWorkplaces/Index.cshtml, Edit.cshtml, Create.cshtml — `SageArbeitsplatznummer`/`Name`/`ArbeitsschrittCode` read-only dargestellt (Sage fuehrend); neue Spalten `SageArbeitsplatznummer`/`ArbeitsschrittCode` in Index.cshtml inkl. Spaltenfilter (ADR 0005)"
  - "IDEALAKEWMSService/Services/FaWorkStepStructureDetectionService.cs -> UMBENANNT + UMGEBAUT zu WorkOperationStructureDetectionService.cs (Baustein b, 2c-Umbau: Ziel WorkOperation statt FaWorkStep, Katalog ProductionWorkplace.ArbeitsschrittCode statt WorkStep.Code, PLUS Kuerzel-Mehrdeutigkeits-Meldung wenn ein Token mehrere aktive Werkbaenke trifft, PLUS: Kuerzel auf der Ausschlussliste erzeugen KEINE Unbekannt-Meldung)"
  - "IDEALAKEWMSService/Services/IFaWorkStepStructureDetectionService.cs -> UMBENANNT zu IWorkOperationStructureDetectionService.cs"
  - "IDEALAKEWMSService/Common/IUnknownWorkStepTokenState.cs -> UMBENANNT zu IUnknownArbeitsschrittTokenState.cs (gleiches HashSet-Singleton-Muster, nur Umbenennung fuer Konsistenz mit dem neuen Match-Ziel)"
  - "IDEALAKEWMSService.Tests/Services/FaWorkStepStructureDetectionServiceTests.cs -> UMBENANNT + angepasst auf WorkOperation-Zielmodell + neue Faelle (Kuerzel-Mehrdeutigkeit, ausgeschlossenes Kuerzel = keine Unbekannt-Meldung)"
  - "IdealAkeWms/Models/FaWorkStep.cs — die in v1.41.0 vorgesehene Ergaenzung `FaWorkStepSources.Struktur` ENTFAELLT ersatzlos (FaWorkStep bleibt fuer IDEAL leer; AKE-Quellen `Sync`/`Manual` unveraendert)"
  - "IdealAkeWms/Services/BdeScanResolver.cs — voraussichtlich UNVERAENDERT (Design-Empfehlung F, bestaetigt am Code: `BdeTerminalController.Index` waehlt bereits zwischen `BdeAktiv`-Werkbaenken, der Normal-Modus-Filter laeuft an drei Stellen bereits ueber `ProductionWorkplaceId`); Verifikations-AK statt Code-Aenderung"
  - "IdealAkeWms/Services/BdeDefaultWorkOperationService.cs — Existenz-Check-Fix (B3/Antwort 6): `FindOrCreateDefaultAsync` (Zeile 25 heute: `wo.Name == defaultName`) prueft die Existenz kuenftig ueber `wo.OperationNumber == \"01\"` statt ueber `Name` — sonst bucht eine NurFA-Buchung still auf einen echten, struktur-abgeleiteten Arbeitsgang, sobald `BdeDefaultArbeitsgang` zufaellig mit einem Werkbank-Namen uebereinstimmt"
  - "IdealAkeWms.Tests/Services/BdeDefaultWorkOperationServiceTests.cs (NEU oder ergaenzt) — Test fuer den Existenz-Check-Fix (echter AG mit gleichem Namen wie der Default vorhanden -> Default wird trotzdem als eigene Zeile mit OperationNumber '01' gefunden/angelegt)"
  - "IdealAkeWms.Tests/Services/BdeScanResolverTests.cs — Regressionslauf + ggf. neue Faelle mit echten, struktur-abgeleiteten WorkOperations"
  - "docs/TESTSZENARIEN.md — neues Kapitel (naechste freie Nummer TS-77, Stand TS-76 im Worktree, verifiziert 2026-09-22) + TS-66-Ergaenzung (BDE-Disambiguierung mit echten AGs) + TS-76 als 'superseded, siehe TS-77' kennzeichnen"
  - "secondbrain/tests/testszenarien-index.md"
open_questions:
  - "Sage-Spaltentyp `KHKPpsArbeitsplaetze.Arbeitsplatznummer` (int oder nvarchar mit fuehrenden Nullen, Beispiele `2300`/`0000`) ist im WMS-Repo nicht verifizierbar (externe Sage-Tabelle, keine bestehende Code-Referenz gefunden) — vor der Migration gegen das tatsaechliche Sage-Schema pruefen (z. B. `INFORMATION_SCHEMA.COLUMNS`); `ProductionWorkplace.SageArbeitsplatznummer` spiegelt den gefundenen Typ."
  - "Migrationsnummer-Koordination: SQL/92 ist im Worktree Stand 2026-09-22 von [[2026-09-18-stueckliste-kommissionierziel-filter-spec]] belegt -> SQL/93 ist damit Stand heute die naechste freie Nummer. Im Dev-Lauf gegen den dann tatsaechlichen Stand nochmals verifizieren, nicht blind uebernehmen."
epic: true
etappen:
  - "A: Baustein (a) — ProductionWorkplace-Anlage aus Sage KHKPpsArbeitsplaetze: Modell (SageArbeitsplatznummer + ArbeitsschrittCode), Migration, ProductionWorkplaceSyncService (Anlegen + Abweichung nachziehen/melden), ServiceSettings/Standorteinstellungen (Mandant, Ausschlussliste, Toggle), SyncWorker-Doppel-Gate, UI read-only. Eigener, buildbarer Commit. STOPP + melden."
  - "B: Bausteine (b)+(c) — Umbau FaWorkStepStructureDetectionService -> WorkOperationStructureDetectionService (inkl. Kuerzel-Mehrdeutigkeit + Ausschlussliste-Sonderfall), Umbenennungen, BdeDefaultWorkOperationService-Existenz-Check-Fix, Terminal-Verifikations-AK, TS-77, Brain-Update. Eigener Commit, danach QA/Merge fuer das gesamte Buendel (Schranke 2)."
deploy:
  web: true
  service: true
  migration: true
freigabe_entscheidung: ""
freigabe_von: ""
freigabe_am: ""
# Flache Schluessel mit Absicht: Obsidians Property-Editor kann verschachtelte
# YAML-Objekte NICHT bearbeiten - und genau diesen Block fuellt der Mensch aus.
---

> **Groessen-Hinweis vorab:** Diese Spec deckt fachlich EIN Ziel ab (Sub-FA-Arbeitsgaenge am
> BDE-Terminal buchbar), technisch aber DREI klar getrennte Bausteine mit jeweils eigenem
> Schema-/Service-/UI-Anteil. Der Zuschnitt ist entschieden (Antwort 8): **Epic mit zwei Etappen**
> (A = Baustein a, STOPP davor; B = Bausteine b+c), siehe Frontmatter `etappen` und
> „Reihenfolge/Einordnung". Kein Split in getrennte Specs.

## Ziel / Nutzen (das Warum)

Am BDE-Terminal sieht ein IDEAL-Werker heute pro Sub-FA nur einen **generischen Sammel-AG**
(`BdeDefaultWorkOperationService`, `OperationNumber "01"`, Name = `ProductionWorkplace.BdeDefaultArbeitsgang`)
statt der echten Arbeitsgaenge (KA/LS/SW/…) — weil IDEAL kein OSEON hat und daher nie echte
`WorkOperation`-Zeilen aus einem Fremdsystem-Sync bekommt.

Die Struktur liefert die echten Arbeitsschritte je Sub-FA bereits mit (`FaHierarchyNode.Arbeitsschritte`).
[[2026-09-08-arbeitsgaenge-aus-arbeitsschritte-spec]] (v1.41.0, Testbereit) hat diese Ableitung bereits
gebaut — aber gegen **`FaWorkStep`** (die FA-Abarbeitungsliste), nicht gegen `WorkOperation` (die
Tabelle, gegen die das BDE-Terminal tatsaechlich bucht: `BdeApiController`, `BdeScanResolver`,
`BdeBooking`). Es gibt keinen Fremdschluessel zwischen beiden Tabellen — `FaWorkStep`-Zeilen erscheinen
am Terminal **nie**. v1.41.0 zielt fuer den eigentlichen Zweck (BDE-Rueckmeldung) auf die falsche
Tabelle.

Zweiter Befund: In den Sage-Arbeitsplatz-Stammdaten (`KHKPpsArbeitsplaetze`) traegt jeder Arbeitsplatz
genau ein Kuerzel (`USER_ArbeitsSchritt`), und die Arbeitsgang-**Namen** sind die Arbeitsplatz-
Bezeichnungen (`KA` = „Kanterei W1", `SW` = „Schweißerei W1" …). **Ein Arbeitsgang IST bei IDEAL faktisch
ein Sage-Arbeitsplatz — und ein Sage-Arbeitsplatz IST die IDEAL-Werkbank** (geklaert am 2026-09-21, siehe
„Begriffsklaerung" unten). Die Werkbank↔Arbeitsgang-Zuordnung existiert damit bereits in Sage — keine
neue manuelle Stammdatenpflege noetig.

**Dritter Befund, der Baustein (a) neu zuschneidet:** Bei IDEAL gibt es heute noch **keine einzige**
`ProductionWorkplace`-Zeile. Anders als beim ADR-0014-Muster (Arbeitsbereiche aus freiem Text an
Auftragspositionen — Melden statt Anlegen, weil Tippfehler/Altlasten) ist `KHKPpsArbeitsplaetze` eine
**gepflegte Sage-Stammdatentabelle**. Deshalb **legt** der Sync dieser Spec die Werkbaenke selbst an
(Sage fuehrend fuer Nummer, Name und Kuerzel) — Details siehe Baustein (a) und Freigabe-Antwort 4.

Diese Spec baut die Struktur-Ableitung von `FaWorkStep` auf `WorkOperation` um (2c-Umbau, kein
Zweitbau) und ergaenzt die fehlende Werkbank-Anlage/-Zuordnung aus Sage, damit der Werker am Terminal
tatsaechlich den passenden Arbeitsgang seines Sub-FA sieht. **v1.41.0 wird durch diese Spec fachlich
ersetzt** (superseded) — die Umsetzung dieser Spec ersetzt v1.41.0s Zielservice, bevor v1.41.0 je in
Produktion geht.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**

- **Baustein (a):** `ProductionWorkplace` bekommt zwei neue Felder — `SageArbeitsplatznummer` (der
  Verknuepfungsschluessel) und `ArbeitsschrittCode` (Match-Feld fuer Baustein b) — befuellt durch einen
  neuen, eigenstaendigen Sage-Sync. Der Sync **legt fehlende Werkbaenke aus `KHKPpsArbeitsplaetze` an**
  (Sage fuehrend fuer Nummer/Name/Kuerzel), findet bestehende Werkbaenke ueber die Nummer wieder und
  zieht Abweichungen bei Name/Kuerzel nach — **und meldet** sie (ADR-0014-Melde-Muster, jetzt am
  richtigen Ziel: die Abweichungsmeldung, nicht die Anlage).
- **Baustein (b):** 2c-Umbau der bestehenden Struktur-Ableitung (`FaWorkStepStructureDetectionService`
  -> `WorkOperationStructureDetectionService`): Ziel-Tabelle `WorkOperation` statt `FaWorkStep`,
  Match-Katalog `ProductionWorkplace.ArbeitsschrittCode` statt `WorkStep.Code`, inkl. Kuerzel-
  Mehrdeutigkeits-Meldung und Sonderbehandlung ausgeschlossener Kuerzel (keine Unbekannt-Meldung).
- **Baustein (c):** BDE-Terminal zeigt/bucht die neu entstehenden, echten `WorkOperation`-Zeilen. Nach
  Design-Empfehlung F **kein** Code-Eingriff in `BdeScanResolver`/`GetAvailableOperations` (die
  Terminal-Werkbank-Auswahl existiert bereits, siehe B5(b)), sondern eine Verifikations-AK **plus** der
  entschiedene Existenz-Check-Fix in `BdeDefaultWorkOperationService` (echter Bug, siehe B3).
- Testszenarien inkl. Negativfaellen (unbekanntes Kuerzel ohne Werkbank, ausgeschlossenes Kuerzel,
  Kuerzel-Mehrdeutigkeit, leere Schnittmenge, Fertig-/Storno-Filter, Existenz-Check-Fix, TS-66-Anpassung).
- Brain-Pflichten: v1.41.0 als superseded kennzeichnen (Hauptcheckout, nicht durch diese Spec selbst
  ausgefuehrt, sondern als Aufgabe fuer Umsetzung/Brain-Update vermerkt).

**Out-of-Scope**

- Der **AKE-Pfad** (`FaWorkStepDetectionService`, `FaWorkStep`, `WorkStep`-Katalog `VA/VE/VK/VL/VT`)
  bleibt **vollstaendig unveraendert**.
- Kombinationsgeraete-Grenze am BDE-Terminal (bereits bekannte, akzeptierte Einschraenkung aus
  [[2026-07-29-standort-ideal-teil-8-spec]]) — unveraendert.
- Pflege der Sage-Arbeitsplatz-Stammdaten selbst (die 5 Kuerzel ohne Werkbank `MO`/`ZS`/`LÖ`/`PR`/`BE`
  nachtragen) — Fachbereichs-/Sage-Arbeit, kein Code (Antwort 3: melden, nicht ausschliessen).
- Terminal-spezifische Werkbank-Teilmengen. Die Terminal-Auswahl zeigt heute **alle** `BdeAktiv`-
  Werkbaenke (`BdeTerminalController.Index`); bei rund 60 IDEAL-Arbeitsplaetzen kann die Liste lang
  werden. Eine engere Zuordnung „Terminal -> seine Werkbaenke" ist eine spaetere Verfeinerung (B5(b)),
  bewusst nicht Teil dieser Spec.
- Aenderungen an `FaHierarchyNode`, `FaHierarchySyncService`, `FaMaterializationSyncService` oder der
  Materialisierung selbst — alle drei Bausteine lesen nur. Die Korrektur der Materialisierungs-
  Werkbank-Ableitung (ADR 0014 beruhte auf der widerlegten Annahme „Werkbank = Arbeitsbereich") ist
  Gegenstand der separaten, vorgelagerten Spec [[2026-09-21-adr-0014-arbeitsbereich-ist-zielort-spec]]
  (siehe „Reihenfolge/Einordnung").
- **Arbeitsbereich (`USER_OSAbteilung`, `K-02`/`S-01`/…) und Arbeitsschritt/Werkbank
  (`KHKPpsArbeitsplaetze`) sind zwei unabhaengige Sage-Vokabulare** (siehe „Begriffsklaerung"). Diese
  Spec fuehrt die Werkbank-Anlage/-Zuordnung aus `KHKPpsArbeitsplaetze` ein und ruehrt den
  Arbeitsbereich nicht an.

## Fachliche Anforderungen

**Begriffsklaerung (2026-09-21, gegen die Sage-Arbeitsplatz-Stammdaten geklaert — massgeblich, nicht
neu zu erheben; volle Herleitung inkl. der verworfenen Zwischenstaende siehe „Freigabe-Antworten",
Antwort 4, Abschnitte „NEUE BLOCKER-FRAGE" bis „AUFLOESUNG"):**

| Begriff | Bedeutung | Sage-Quelle |
|---|---|---|
| **Arbeitsbereich** (`K-02`, `S-01`, `H4-04`) | **Zielort** eines Teils — physischer Bestimmungsort, wenn alle Arbeitsschritte erledigt sind; begrifflich aus Lagerorten abgeleitet. Ziel der Materialisierung (ADR 0014, `ProductionOrder.ProductionWorkplaceId`) — eine andere, unabhaengige Zuordnung (siehe Out-of-Scope). | `USER_OSAbteilung` |
| **Arbeitsschritt** (`KA`, `SW`, `LS` …) | **Arbeitsgang**, der am Teil erfolgen muss (Struktur-Token). | `FaHierarchyNode.Arbeitsschritte` |
| **Werkbank** (fuers BDE, `ProductionWorkplace`) | **Sage-Arbeitsplatz**, an dem gearbeitet wird — das Anlage-Ziel DIESER Spec. | `KHKPpsArbeitsplaetze` |

**Arbeitsbereich und Arbeitsschritt stehen in KEINER hierarchischen Beziehung** — zwei unabhaengige,
beide aus Sage stammende Vokabulare, die leicht verwechselbar sind. **Eine IDEAL-Werkbank im WMS ist der
Sage-Arbeitsplatz** (`KHKPpsArbeitsplaetze`), nicht der Arbeitsbereich. Konsequenz fuer ADR 0014/die
Materialisierung: siehe [[2026-09-21-adr-0014-arbeitsbereich-ist-zielort-spec]] (Vorbedingung dieser
Spec, siehe „Reihenfolge/Einordnung").

**Bereits entschieden (Schranke-1-Vorlage aus dem Brainstorming + Freigabe-Antworten 1-8, nicht erneut
zur Diskussion):**

1. **Ziel-Tabelle `WorkOperation`**, nicht `FaWorkStep`. `FaWorkStep` bleibt fuer den AKE-Pfad
   unveraendert.
2. **Werkbank-Anlage/-Routing aus Sage `KHKPpsArbeitsplaetze`**, nicht manuell gepflegt.
3. **2c-Umbau statt Zweitbau** — dieselbe Service-Klasse (umbenannt), derselbe Buendel-Worktree,
   v1.41.0 wird ersetzt.
4. **Baustein (a) LEGT fehlende Werkbaenke an** (Anlege-, nicht Melde-Semantik) — `KHKPpsArbeitsplaetze`
   ist eine gepflegte Sage-Stammdatentabelle, keine Freitext-Eingabe wie die Arbeitsbereiche von
   ADR 0014. Bestehende Werkbaenke werden ueber `SageArbeitsplatznummer` wiedergefunden; Abweichungen
   bei Name/Kuerzel werden nachgezogen UND gemeldet (das ADR-0014-Melde-Muster bleibt fuer Abweichungen
   bestehen, nur die Anlage selbst kehrt sich um).
5. **Verknuepfungsschluessel ist `SageArbeitsplatznummer`** (aus `KHKPpsArbeitsplaetze.Arbeitsplatznummer`),
   **nicht** der Name — am Datensatz belegt: `Matchcode`/`Bezeichnung1` weichen ab, tragen doppelte
   Leerzeichen, und eine Sage-Umbenennung braeche eine Namens-Verknuepfung. `Name = Bezeichnung1` ist
   reine Anzeige, kein Abgleichsschluessel.
6. **Epic mit zwei Etappen** (A = Baustein a, STOPP davor; B = Bausteine b+c) statt drei getrennter
   Specs — siehe Frontmatter `etappen` und „Reihenfolge/Einordnung".

**Token -> Werkbank-Zuordnung (2026-09-21, vom Menschen gegen die Sage-Arbeitsplatz-Liste
abgeglichen — massgeblich, nicht neu zu erheben):**

17 real gemeldete Kuerzel aus dem ersten Struktur-Lauf. **12 treffen eine Werkbank**
(`USER_ArbeitsSchritt` -> `Bezeichnung1`):

| Kuerzel | Werkbank |
|---|---|
| KA | Kanterei W1 |
| LS | Stanzerei W1 |
| SW | Schweißerei W1 |
| EG | Flächen entgraten |
| SÄ | Schäumerei W1 (EG) |
| VM | Vormontage W1 |
| PL | Punkten Ladenbau W1 |
| PG | Punkten Gehäusebau W1 |
| BR | Berohren W1 |
| EM | Elektromontage W1 |
| ISO | Isolierung W2 |
| SL | Schleiferei W1 |

*(Korrigiert 2026-09-22, S4: `VM` = „Vormontage W1" (Arbeitsplatz 3002). Eine fruehere Fassung dieser
Tabelle nannte faelschlich „Elektrofertigung W1 (OG)" — das ist `VM9` (Arbeitsplatz 3001), ein anderer
Datensatz.)*

**5 Kuerzel ohne Werkbank in der Liste:** `MO`, `ZS`, `LÖ`, `PR`, `BE` — heute nicht buchbar (Werkbank
fehlt in Sage), bleiben **gemeldet** (Antwort 3: melden, im Code **nicht** ausschliessen — Fachthema,
keine Code-Aufgabe, siehe Out-of-Scope). Der Melde-Mechanismus heilt sich selbst: Sobald die Werkbank in
Sage nachgetragen ist, greift Baustein (a) beim naechsten Lauf, und Baustein (b) legt die Arbeitsgaenge
an — ohne Code-Aenderung.

**Ausschlussliste (Standorteinstellung, Baustein a) — Standardwert `STO, XXX, x01, BS, BS2, FRE, AKE`**
(entschieden 2026-09-22, siehe „ANTWORTEN zu B5"): Statusmarker (Storno, Gestoppt, Reserve) und Kuerzel,
die **ausser Haus** stattfinden (externe Beschichter `BS`/`BS2`, Fremdleister `FRE`, Bestellung beim
Schwesterstandort `AKE`). Fuer diese Kuerzel legt Baustein (a) **keine** Werkbank an. Findet Baustein (b)
ein solches Kuerzel als Struktur-Token, erzeugt es **keine** `WorkOperation` **und keine**
Unbekannt-Meldung — ausgeschlossene Kuerzel sind bewusst **bekannt**, nicht **unbekannt** (sonst
Dauerrauschen bei jedem Auftrag mit externer Beschichtung). Die Unbekannt-Meldung bleibt den echten
Luecken (`MO`/`ZS`/`LÖ`/`PR`/`BE`) vorbehalten. Die Liste ist in den Standorteinstellungen erweiterbar.

Quelle: vollstaendige Arbeitsplatz-Stammdaten (`KHKPpsArbeitsplaetze`, relevante Spalten
`Arbeitsplatznummer`, `Mandant`, `Bezeichnung1`, `Aktiv`, `USER_ArbeitsSchritt`), 2026-09-21 vom
Menschen bereitgestellt (66 Zeilen, Mandant 1, aktiv) — Sync-Quelle fuer Baustein (a). **Stand
2026-09-21: bei IDEAL gibt es noch KEINE `ProductionWorkplace`-Zeile** — der erste scharf geschaltete
Sync-Lauf legt sie neu an (siehe Migrations-/SQL-Auswirkungen und Deploy).

**Datenbefunde aus der Liste, fuer den Sync verbindlich:**

- **`"EG "` mit Leerzeichen** (Arbeitsplatz 2150). **`USER_ArbeitsSchritt` beim Lesen zwingend `TRIM`en**
  — sonst faellt `EG` still in die Unbekannt-Meldung.
- **Keine doppelten Kuerzel** unter den aktiven Arbeitsplaetzen (Stand 2026-09-21). Die
  Mehrdeutigkeitsregel fuer Baustein (b) (siehe unten) bleibt trotzdem als Absicherung im Code.
- **Ausgelaufene Arbeitsplaetze** (Markierung „(X)"/„beendet") sind in Sage noch `Aktiv = -1`, darunter
  `EG`. Der Sync liest sie wie jeden aktiven Arbeitsplatz; ob Arbeit fachlich auf einen „beendeten"
  Arbeitsplatz geroutet werden soll, ist im UAT zu beobachten, keine Code-Entscheidung dieser Spec.

**Varianten-Problem (entschieden, Antwort 2):** Die Struktur (`FaHierarchyNode.Arbeitsschritte`) nutzt
Basis-Kuerzel (`KA`). Sage kennt daneben Werkbank-**Varianten** (`KA2`/`KA4`, `SW1`/`SW2`/`SWB`/`SWL`,
`SÄ2`/`SÄE`/`SÄO`, `VM1`/`VM2`, `PG2`, `PL2`, `EM2`, `SLH`). **Exakter Basis-Kuerzel-Match, kein
Praefix** — ein Praefix-Match waere mehrdeutig (`KA` traefe `KA2` UND `KA4`, welche Werkbank?). Folge, im
UAT zu beobachten: Varianten-Werkbaenke bekommen vorerst **keine** struktur-abgeleiteten Arbeitsgaenge,
nur die Basis-Werkbank. Ob Varianten alternative Werkbaenke fuer denselben Arbeitsgang oder eigene
Arbeitsgaenge sind, ist eine spaetere, mit Betriebsbeleg zu klaerende Fachfrage.

## Technischer Loesungsentwurf

Referenzmuster: [[0010-aktivitaets-protokoll-mit-isolierten-dbcontexts]] (`ISyncLogger` letzter
Ctor-Parameter, deutsche Counts-Keys), [[0014-werkbank-datenhoheit-sage-fuehrend-mit-abweichungsmeldung]]
(Sage fuehrend + Melde-Muster fuer Abweichungen — hier zusaetzlich mit Anlage, siehe Baustein a), ADR
0008 (ServiceSettings-Katalog). Betroffene Module: [[services]] (Sync-Services, `SyncWorker`),
[[datenmodell]] (`ProductionWorkplace`, `WorkOperation`).

### Baustein (a) — `ProductionWorkplace`-Anlage aus Sage `KHKPpsArbeitsplaetze`

**Modell:** `ProductionWorkplace` bekommt zwei neue Felder:

- `SageArbeitsplatznummer` (Typ/Laenge nach Offener Rueckfrage 9 gegen das echte Sage-Schema
  verifizieren) — **der Verknuepfungsschluessel**, Display „Sage-Arbeitsplatznummer". Nullable
  (bestehende AKE-Werkbaenke bekommen nie einen Wert, siehe Master-Gate unten). **Kein Unique-Index**
  (Hausmuster: Ambiguitaet wird gemeldet, nicht per DB-Constraint verhindert — ein Sync-Lauf kann besser
  erklaeren, was mehrdeutig ist, als eine Exception es koennte), aber ein normaler, **nicht-eindeutiger
  Index** fuer den Zeilen-Lookup je Sync-Durchlauf (`WHERE SageArbeitsplatznummer = @nr`).
- `[MaxLength(20)] public string? ArbeitsschrittCode { get; set; }` (Display „Arbeitsschritt-Kuerzel
  (Sage)") — Match-Feld fuer Baustein b, **getrimmt** beim Schreiben.

`Name` bleibt das bestehende `[Required][MaxLength(200)]`-Feld — wird ab dieser Spec mit `Bezeichnung1`
befuellt (Anzeige, **kein** Abgleichsschluessel).

**Sync, neuer eigenstaendiger Service (`ProductionWorkplaceSyncService`):** Es existiert heute **kein**
bestehender Sync-Schritt, der `ProductionWorkplace`-Stammdaten aus Sage befuellt (verifiziert per Grep —
`ProductionWorkplace` wird im Service-Projekt nur **gelesen**, z. B. von `FaMaterializationSyncService`,
`OseonSyncService`, `BdeAutoPauseService`, und `KHKPpsArbeitsplaetze` hat im gesamten Repo bisher keine
Code-Referenz). Ein neuer, eigener Service ist daher die einzige Option.

**Pflicht-Filter beim Lesen** (Antwort 4, „ENTSCHIEDEN 2026-09-21"):

1. **`Mandant = <Sync:ProductionWorkplaceSyncMandant>`** (Standorteinstellung, Standardwert `1`, gilt bei
   beiden Standorten immer — sichtbar/aenderbar ohne Deploy, dieselbe Begruendung wie bei allen
   Stammdaten-Konventionen dieser Spec).
2. **`Aktiv = -1`** — Sage-Konvention (wie `KHKArtikel`). Ein stillgelegter Arbeitsplatz mit derselben
   `Bezeichnung1` wie ein aktiver erzeugte sonst eine falsche Doppel-Bezeichnung (auch wenn der Abgleich
   ueber die Nummer laeuft, nicht ueber den Namen, ist die Aktiv-Zeile die richtige Quelle fuer Name/
   Kuerzel).
3. **`USER_ArbeitsSchritt` gefuellt und GETRIMMT** — Arbeitsplaetze ohne Kuerzel werden **uebersprungen**,
   nicht gemeldet (sie sind kein Arbeitsgang-Ziel). Trim ist Pflicht (`"EG "` mit Leerzeichen).
4. **Kuerzel NICHT auf der Ausschlussliste** (`Sync:ProductionWorkplaceSyncAusschlussliste`, Vergleich
   getrimmt/case-insensitiv gegen das Sage-Kuerzel) — Statusmarker/externe Vorgaenge werden **gar nicht**
   angelegt (siehe „Ausschlussliste" oben).

**Ablauf je verbleibende Sage-Zeile:**

1. `ProductionWorkplace` mit exakt passender `SageArbeitsplatznummer` suchen.
   - **Kein Treffer** -> **neue Zeile anlegen**: `SageArbeitsplatznummer` = Sage-Nummer, `Name` =
     `Bezeichnung1` (getrimmt), `ArbeitsschrittCode` = `USER_ArbeitsSchritt` (getrimmt),
     **`BdeAktiv = false`** (verifiziert: `ProductionWorkplace.BdeAktiv` ist `bool` ohne expliziten
     Default, also automatisch `false` — der Sync setzt es schlicht nicht; Terminal-Einrichtung bleibt
     eine bewusste, spaetere Handlung des Menschen), Audit `CreatedAt/CreatedBy/CreatedByWindows =
     "ProductionWorkplaceSync"`.
   - **Ein Treffer**, `Name`/`ArbeitsschrittCode` weichen vom Bestandswert ab -> **ueberschreiben**
     (Sage fuehrend) UND **melden** (alter/neuer Wert je Feld), Audit
     `ModifiedAt/ModifiedBy/ModifiedByWindows = "ProductionWorkplaceSync"`.
   - **Ein Treffer**, keine Abweichung -> kein Schreiben, kein Audit-Touch, keine Meldung.
   - **Mehr als ein Treffer** mit derselben `SageArbeitsplatznummer` (sollte durch den Sage-eigenen
     Primaerschluessel nicht vorkommen, aber Absicherung gegen manuelle Doppelanlage im WMS) -> nichts
     schreiben, „mehrdeutig" melden.
2. Sammelmeldung + Sammelmail nur bei Aenderung der Menge unbekannter/mehrdeutiger Zeilen (S1-Muster,
   analog `IUnknownWorkplaceState`).
3. Eigener `SyncLogServices`-Eintrag (`ProductionWorkplaceSync`), eigener Toggle
   (`Sync:ProductionWorkplaceSyncEnabled`, Default `false`).

**Gate (S1, Antwort 4 „Vorschlag ... Bestaetigung ausstehend" — jetzt uebernommen):**
**Doppel-Gate wie Baustein (b): Master `ProduktionsauftragHierarchisch` UND eigener Toggle.** Ohne den
Master liese der Sync bei AKE die AKE-Arbeitsplaetze, faende keine `SageArbeitsplatznummer`-Treffer
(AKE-Werkbaenke haben nie diesen Wert) und legte bei AKE fremde, dort nicht gewollte Werkbaenke an —
reines Rauschen. **Reihenfolge im `SyncWorker`:** kein FK-Bezug zu FA-Hierarchie/-Materialisierung, der
Block sollte aber **vor** Baustein (b) laufen, damit der Katalog beim WorkOperation-Aufbau moeglichst
aktuell ist (Nur-hinzufuegen-Semantik faengt eine falsche Reihenfolge beim naechsten Lauf ohnehin ab).

**UI:** `/ProductionWorkplaces` (Index/Edit/Create) zeigt `SageArbeitsplatznummer`, `Name` und
`ArbeitsschrittCode` **read-only** an (Sage fuehrend — eine manuelle Eingabe wuerde beim naechsten Lauf
ueberschrieben, derselbe Fallstrick wie ADR 0014s Konsequenz „ueberlebt hoechstens 15 Minuten"; das
gehoert auf die Hilfeseite). Bestehende Liste bleibt Listen-View-Pattern-konform (ADR 0005): die neuen
Spalten in `Index.cshtml` bekommen Spaltenfilter wie die uebrigen Spalten der Tabelle.

### Baustein (b) — 2c-Umbau: `WorkOperationStructureDetectionService`

Der bestehende `FaWorkStepStructureDetectionService` (v1.41.0, Design D des Ursprungs-Specs) wird
**umgebaut, nicht neu gebaut** — Ctor-Signatur, Full-Refresh-Cache, DirectChildren-Scope-Logik
(eigene Zeile + direkte Kinder aus `childrenByParent`), Nur-hinzufuegen-Semantik, Fertig-/Storno-Filter
und Melde-statt-Anlege-Prinzip bleiben **strukturell identisch** — nur Zieltabelle und Katalog
wechseln:

| | v1.41.0 (FaWorkStep, wird ersetzt) | Diese Spec (WorkOperation) |
|---|---|---|
| Ziel-Tabelle | `FaWorkStep` | `WorkOperation` |
| Match-Katalog | `WorkStep.Code` | `ProductionWorkplace.ArbeitsschrittCode` (Baustein a) |
| Neu angelegtes Feld „Name" | `WorkStep.Name` (referenziert) | `ProductionWorkplace.Name` (kopiert in `WorkOperation.Name`) |
| Eindeutigkeits-Schluessel je Sub-FA | `(ProductionOrderId, WorkStepId)` | `(ProductionOrderId, OperationNumber)` |
| Fremdschluessel-Zwang | Pflicht-FK auf `WorkStep` (Zwei-Lauf-Ablauf) | **kein FK-Zwang** — `WorkOperation.OperationNumber`/`Name` sind Plain-Strings, kein Katalog-FK |

**Wichtige Vereinfachung gegenueber v1.41.0:** `WorkOperation` hat **keinen** Fremdschluessel auf
einen Arbeitsgang-Katalog (verifiziert `Models/WorkOperation.cs`: `OperationNumber`/`Name` sind reine
Strings). Der bisherige „Zwei-Lauf-Ablauf" (erst Katalogeintrag anlegen, dann Ableitung) entfaellt
damit fuer Baustein (b) **strukturell** — es gibt nichts Zweites zu pflegen, sobald Baustein (a) den
Namen kennt (**bestaetigt, Antwort 5**). Ein Token bleibt weiterhin „unbekannt" (gemeldet, kein Insert),
solange **kein** `ProductionWorkplace.ArbeitsschrittCode` dazu existiert und es **nicht** auf der
Ausschlussliste steht.

**Neu angelegte `WorkOperation`-Felder je Kandidat (Token, Sub-FA):**

- `ProductionOrderId` — wie v1.41.0, gebuendelte Abfrage ueber `SubOrderNumber`, gleicher Fertig-/
  Storno-Filter (`!IsDone && !IsCancelled && !(PickingStatus?.IsDonePicking == true)`).
- `OperationNumber` = das Kuerzel selbst (z. B. `"KA"`) — kurz, menschenlesbar, kollisionsfrei mit dem
  Default-AG (der hartcodiert `"01"` verwendet, `BdeDefaultWorkOperationService.cs:34`).
- `Name` = `ProductionWorkplace.Name` des per Kuerzel gematchten Datensatzes (z. B. „Kanterei W1"). Der
  Name wird beim Anlegen **kopiert** (Antwort 5) — eine spaetere Umbenennung der Werkbank in Sage
  aendert den Namen bereits angelegter `WorkOperation`-Zeilen **nicht** nach.
- `ProductionWorkplaceId` = **Id** desselben Datensatzes (Empfehlung F, Antwort 1). Das Routing wird
  damit **beim Anlegen materialisiert**: Ordnet Sage ein Kuerzel spaeter einer anderen Werkbank zu (neue
  `SageArbeitsplatznummer` mit demselben `ArbeitsschrittCode`, oder Kuerzel-Wechsel am bestehenden
  Arbeitsplatz), routen **bereits angelegte, offene** Arbeitsgaenge weiter zur **alten** Werkbank — nur
  neue Sub-FAs gehen zur neuen. Vertretbar, weil Kuerzel-Werkbank-Zuordnungen Stammdaten sind und sich
  selten aendern, und weil Baustein (a) jede Abweichung **meldet** (die Meldung ist das Signal, offene
  Arbeitsgaenge zu pruefen). **Bewusste, bekannte Eigenschaft, kein Fehler.**
- `Sequence` = Position des Tokens innerhalb der Arbeitsschritte-Zeichenkette der Quelle (0-basiert,
  Sage-Reihenfolge bleibt erhalten); Fallback `1` falls nicht ermittelbar. Beeinflusst nur die
  Sortierung mehrerer Kandidaten am Terminal (`BdeScanResolver`/`GetOpenByWorkplaceIdAsync` sortieren
  `.ThenBy(Sequence)`), kein hartes Kriterium.
- `IsReportable = true` (treibt BDE-Buchungen, analog `BdeDefaultWorkOperationService`).
- `IsExternalSystem = false`, `IsReported = false`.
- `CreatedAt/CreatedBy/CreatedByWindows = "WorkOperationStructureDetection"` (Service-Name als Autor,
  ADR 0003).

**Unbekannte Kuerzel** (kein `ProductionWorkplace.ArbeitsschrittCode`-Treffer, NICHT auf der
Ausschlussliste): melden, nicht anlegen — identisches Sammelmeldungs-/Sammelmail-Muster wie v1.41.0
(`IUnknownArbeitsschrittTokenState`, S1-Muster: Mail nur bei Aenderung der Token-**Menge**).

**Ausgeschlossene Kuerzel** (auf der Ausschlussliste, siehe „Fachliche Anforderungen"): weder Werkbank
noch Arbeitsgang noch Unbekannt-Meldung — bewusst bekannt, kein Rauschen.

**Kuerzel-Mehrdeutigkeit (neu, B2-Nachtrag/Antwort 4):** Trifft ein Struktur-Token **mehr als eine**
aktive `ProductionWorkplace`-Zeile (gleicher `ArbeitsschrittCode`, verschiedene `SageArbeitsplatznummer`)
— heute laut Datenbefund nicht der Fall, aber als Absicherung im Code Pflicht —, wird **keine**
`WorkOperation` angelegt, sondern „mehrdeutig" gemeldet (gleiches Muster wie ueberall in dieser Spec).
Vorab pruefbar:

```sql
SELECT USER_ArbeitsSchritt, COUNT(*) AS Anzahl
FROM KHKPpsArbeitsplaetze
WHERE Aktiv = -1 AND ISNULL(USER_ArbeitsSchritt, '') <> '' -- plus Mandant-Filter
GROUP BY USER_ArbeitsSchritt HAVING COUNT(*) > 1;
```

**Umbenennung (Konsistenz statt Restbestand):** `FaWorkStepStructureDetectionService` ->
`WorkOperationStructureDetectionService` (+ Interface, Tests, `IUnknownWorkStepTokenState` ->
`IUnknownArbeitsschrittTokenState`, `ServiceSettings`-Key, `SyncLogServices`-Konstante). Begruendung:
Ein Service, der `WorkOperation` befuellt, aber „FaWorkStep" im Namen traegt, ist eine irrefuehrende
Altlast fuer jeden kuenftigen Leser (Sprachregel: Code-Namen sollen die Wahrheit sagen). Risiko gering:
keiner der umbenannten Keys/Konstanten ist bisher in einer produktiven Datenbank gesetzt/geseedet
(v1.41.0 ist nur Testbereit, nie deployt) — kein Migrations-/Datenverlust-Thema, reines Rename.

**`FaWorkStep.Source = "Struktur"`-Ergaenzung aus v1.41.0 entfaellt ersatzlos** — IDEAL-Sub-FAs
bekommen ihre Arbeitsgaenge kuenftig ausschliesslich ueber `WorkOperation`, nicht mehr zusaetzlich
ueber `FaWorkStep`. `FaWorkStep` bleibt fuer IDEAL dauerhaft leer (kein Fehler, einfach ungenutzt fuer
diesen Standort), fuer AKE unveraendert aktiv.

### Baustein (c) — BDE-Terminal: Design-Empfehlung F, bestaetigt am Code + Existenz-Check-Fix

Der Backlog skizziert Baustein (c) als Umbau von `BdeScanResolver`/`GetAvailableOperations` auf eine
**zur Scan-Zeit berechnete** Kuerzel-Schnittmenge (Werkbank-Kuerzel × Sub-FA-Kuerzel-Menge). Die
Code-Recherche zeigt einen einfacheren, bereits vorhandenen Weg:

**Befund 1 (Routing):** `BdeScanResolver.BuildNormalCandidatesAsync` UND
`WorkOperationRepository.GetOpenByWorkplaceIdAsync` UND `BdeApiController.GetAvailableOperations`
(Normal-Modus) filtern **bereits heute** ausschliesslich ueber `WorkOperation.ProductionWorkplaceId ==
workplaceId` (verifiziert, je eine Fundstelle). Setzt Baustein (b) `ProductionWorkplaceId` beim Anlegen
direkt auf die Id der gematchten Werkbank, routen **alle drei Stellen automatisch korrekt** — ohne eine
einzige Code-Aenderung an diesem Pfad.

**Befund 2 (Terminal-Werkbank-Auswahl, B5(b)):** Anfangs bedient **ein Terminal mehrere Werkbaenke** —
war offen, ist am Code beantwortet: `BdeTerminalController.Index(int? workplaceId)` uebersteuert die
**Default-Werkbank** (`BdeTerminal.DefaultProductionWorkplaceId`) per Parameter; `ViewBag.AllWorkplaces =
await _workplaces.GetBdeActiveAsync()` bietet **alle `BdeAktiv`-Werkbaenke** zur Auswahl an. Die Auswahl
existiert also bereits — genau das im Backlog verlangte Verhalten. Baustein (c) bleibt damit **klein**
(Verifikations-AK statt Umbau); die Zwei-Etappen-Einschaetzung (Antwort 8) haelt.

**Konsequenz:** `BdeScanResolver` bleibt **unveraendert**. Aus dem Backlog-„Baustein (c)" wird eine
**Verifikations-Akzeptanzkriterium** (WorkOperations aus Baustein b erscheinen am Terminal der richtigen
Werkbank, ohne Resolver-Aenderung). Die eine echte Code-Aenderung in Baustein (c) ist der
**Existenz-Check-Fix in `BdeDefaultWorkOperationService`** (B3/Antwort 6):

`FindOrCreateDefaultAsync` prueft die Existenz heute ueber den **Namen**
(`wo.Name == defaultName`, `BdeDefaultWorkOperationService.cs:25`). Echte, struktur-abgeleitete
Arbeitsgaenge tragen `Name = ProductionWorkplace.Name`; der Default-AG traegt `Name =
BdeDefaultArbeitsgang`. Stimmen beide Werte ueberein (was bei der Pflege leicht passiert, wenn jemand
`BdeDefaultArbeitsgang` gleich dem Werkbank-Namen setzt), **findet der Default-Service den echten
Arbeitsgang und bucht darauf**, statt den generischen Default anzulegen — eine NurFA-Buchung landete
still auf einem echten Arbeitsgang. **Fix:** Der Existenz-Check geht kuenftig ueber `OperationNumber ==
"01"`, nicht ueber `Name` (echte Arbeitsgaenge nutzen das Kuerzel als `OperationNumber`, das trennt
sauber). Eine Zeile Code, aber mit Test.

**Modus-Trennung (bestaetigt, Antwort 6):** „NurFA" heisst per Definition: auf den Auftrag buchen, ohne
Arbeitsgang zu waehlen — dort echte Arbeitsgaenge anzubieten widerspraeche dem Zweck des Modus. Der
Normal-Modus zeigt/bucht ab Baustein (b) automatisch die echten AGs; `BdeDefaultWorkOperationService`
wird dort gar nicht aufgerufen (haengt ausschliesslich an `StartProductionForOrder`, NurFA-Pfad). Es
gibt also **keinen Koexistenz-Konflikt zwischen den Modi** — nur den einen, oben beschriebenen Bug im
Existenz-Check des NurFA-Pfads.

## Migrations-/SQL-Auswirkungen

**Genau eine Migration, aus Baustein (a), ZWEI neue Spalten:**

- `ProductionWorkplace.SageArbeitsplatznummer` — der Verknuepfungsschluessel (Antwort 4: „Abgleich ueber
  `Arbeitsplatznummer`, NICHT ueber einen Namen"). Typ/Laenge richten sich nach dem tatsaechlichen
  Sage-Spaltentyp (Offene Rueckfrage 9 — `KHKPpsArbeitsplaetze` ist eine externe Sage-Tabelle ohne
  bisherige Code-Referenz im Repo, der Typ ist am WMS-Code nicht verifizierbar). Nullable (bestehende
  AKE-Werkbaenke bekommen wegen des Master-Gates nie einen Wert). **Kein** Unique-Constraint, aber ein
  normaler, nicht-eindeutiger Index fuer den Sync-Lookup.
- `ProductionWorkplace.ArbeitsschrittCode` — `NVARCHAR(20) NULL`, Match-/Anzeigefeld fuer Baustein b.

Ablauf:

- Model -> `dotnet ef migrations add AddProductionWorkplaceSageFields --project IdealAkeWms` ->
  `SQL/93_AddProductionWorkplaceSageFields.sql` mit je einem `COL_LENGTH`-Guard pro Spalte (ALTER TABLE
  ... ADD, analog `SQL/87_AddUserDefaultFilterBomDescription1.sql`), DDL-Batch + separater
  `__EFMigrationsHistory`-Insert-Batch (ADR 0004).
- `SQL/00_FreshInstall.sql` an **beiden** Stellen (Schema-Objekte + `MigrationId`).
- **Nummer, verifiziert 2026-09-22:** `SQL/92_AddUserDefaultFilterBomKommissionierziel.sql` liegt bereits
  im Worktree ([[2026-09-18-stueckliste-kommissionierziel-filter-spec]], Status Freigegeben) — `93` ist
  damit Stand heute die naechste freie Nummer. Im Dev-Lauf trotzdem gegen den dann tatsaechlichen Stand
  pruefen, falls bis dahin weitere Migrationen dazukommen (Offene Rueckfrage 7 — nicht blind
  uebernehmen).
- **Nicht daten-destruktiv** (zwei neue nullable Spalten, kein Datenverlust, kein Backup-Zwang ueber das
  uebliche Mass hinaus). **Aber die anschliessende Anlage ist neuartig fuer diese Tabelle:** Der erste
  scharf geschaltete Lauf von Baustein (a) legt bei IDEAL voraussichtlich rund 60 neue
  `ProductionWorkplace`-Zeilen an (66 aktive Arbeitsplaetze in Sage, abzueglich Ausschlussliste,
  abzueglich Zeilen ohne `USER_ArbeitsSchritt`) — ein DryRun vor dem scharf schalten ist Pflicht (siehe
  Deploy).

**Bausteine (b) und (c) bringen keine Migration:** `WorkOperation` und `FaWorkStep` sind bestehende
Tabellen; Baustein (b) schreibt nur neue Zeilen mit vorhandenen Spalten, Baustein (c) aendert nur eine
Zeile in `BdeDefaultWorkOperationService`. Die `ServiceSettings`-Keys (neu bzw. umbenannt) und die
`StandortSettingsCatalog`-Eintraege sind reine Katalog-/Laufzeit-Konfiguration (ADR 0008, DB-first) —
kein Schema-Impact, aber der `ServiceSettingDefinitions`-Drift-Guard-Test muss die neuen **und** den
umbenannten Key kennen.

## Audit-Feld-Auswirkungen

- `ProductionWorkplace` erbt `AuditableEntity`. Der neue Sync (Baustein a) setzt:
  - bei **neu angelegten** Zeilen `CreatedAt/CreatedBy/CreatedByWindows = "ProductionWorkplaceSync"`
    (Service-Name als Autor, ADR 0003).
  - bei **aktualisierten** Zeilen (Name/Kuerzel weicht vom Sage-Wert ab)
    `ModifiedAt/ModifiedBy/ModifiedByWindows = "ProductionWorkplaceSync"` — **nur**, wenn tatsaechlich
    geschrieben wird (kein Touch bei unveraendertem Datensatz).
- `WorkOperation` erbt `AuditableEntity`. Baustein (b) setzt bei jeder neu angelegten Zeile
  `CreatedAt/CreatedBy/CreatedByWindows = "WorkOperationStructureDetection"` (analog v1.41.0-Vorbild).
  Der Service **aktualisiert nie** bestehende `WorkOperation`-Zeilen (reine Nur-hinzufuegen-Semantik) —
  `ModifiedAt/ModifiedBy` sind fuer diesen Pfad nicht relevant.
- Baustein (c) fuehrt keine neue Entitaet ein. Der Existenz-Check-Fix in `BdeDefaultWorkOperationService`
  aendert keine Audit-Semantik — neu angelegte Default-AGs tragen weiterhin `CreatedBy =
  "BDE-AutoCreate"`.

## Betroffene Rollen / Zugriffsfilter

Keine Aenderung. Baustein (a)/(b) laufen ausschliesslich im Hintergrund (Windows-Service, kein neuer
Web-Endpoint). `/ProductionWorkplaces` behaelt seinen bestehenden `RequireXxxAccess`-Filter — nur zwei
zusaetzliche, read-only dargestellte Felder und (nach dem ersten Baustein-a-Lauf) deutlich mehr Zeilen
(~60 statt heute 0 bei IDEAL); die Liste ist bereits paginiert (ADR 0005), das ist kein neuer
Zugriffsschutz-Fall. Das BDE-Terminal behaelt seine bestehenden `RequireBdeActive`/
`RequireBdeUserAccess`/`RequireBdeShiftleadAccess`-Filter unveraendert; diese Spec liefert nur
zusaetzliche, korrekt geroutete Datenzeilen in bereits geschuetzte, bestehende Ansichten.

## Listen-View-Pattern-Pflichten (ADR 0005)

- `/ProductionWorkplaces`-Index (Baustein a): bestehende, bereits paginierte Liste bekommt zwei neue
  Spalten (`SageArbeitsplatznummer`, `ArbeitsschrittCode`) — je Spalte einen Spaltenfilter, damit die
  Tabelle ADR-0005-konform bleibt. Nach dem ersten Sync-Lauf enthaelt die Liste bei IDEAL deutlich mehr
  Zeilen als heute (0 -> ~60) — Pagination greift bereits, keine Sonderbehandlung noetig.
- Sonst keine neue Tabellen-Ansicht. `WorkOperation`-Zeilen aus Baustein (b) erscheinen in bereits
  bestehenden, zugriffsgeschuetzten Ansichten (BDE-Terminal-Listen, `WorkOperationRepository`-basierte
  Uebersichten) ohne View-Aenderung.

## Akzeptanzkriterien

1. **Baustein a — Neuanlage.** Sage-Arbeitsplatz-Stammsatz mit `Arbeitsplatznummer = 2300`,
   `Bezeichnung1 = "Kanterei W1"`, `USER_ArbeitsSchritt = "KA"`, `Aktiv = -1`, kein `ProductionWorkplace`
   mit dieser `SageArbeitsplatznummer` vorhanden -> neue Zeile wird angelegt mit
   `SageArbeitsplatznummer = 2300`, `Name = "Kanterei W1"`, `ArbeitsschrittCode = "KA"`,
   `BdeAktiv = false`, Audit `CreatedBy/CreatedByWindows = "ProductionWorkplaceSync"`.
2. **Baustein a — Wiederfinden + Abweichung.** Bestehender `ProductionWorkplace` mit
   `SageArbeitsplatznummer = 2300` hat einen abweichenden `ArbeitsschrittCode`/`Name` gegenueber Sage ->
   wird ueberschrieben (Sage fuehrend) UND gemeldet (alter/neuer Wert je Feld), Audit
   `ModifiedBy/ModifiedByWindows = "ProductionWorkplaceSync"`.
3. **Baustein a — unveraendert.** Sage-Werte entsprechen den Bestandswerten -> kein Schreiben, kein
   Audit-Touch, keine Meldung.
4. **Baustein a — Ausschlussliste.** `USER_ArbeitsSchritt` steht auf der Ausschlussliste (z. B. `STO`)
   -> keine Werkbank wird angelegt/aktualisiert, kein SyncLog-Eintrag fuer diese Zeile.
5. **Baustein a — Trim.** `USER_ArbeitsSchritt = "EG "` (mit Leerzeichen) -> `ArbeitsschrittCode = "EG"`
   (getrimmt gespeichert).
6. **Baustein a — Pflicht-Filter.** Zeilen mit `Mandant <> Sync:ProductionWorkplaceSyncMandant`,
   `Aktiv <> -1` oder leerem `USER_ArbeitsSchritt` werden uebersprungen (kein Insert, kein Update, keine
   Meldung).
7. **Baustein a — Master-Gate.** Der Sync laeuft nur bei `ProduktionsauftragHierarchisch = true` UND
   `Sync:ProductionWorkplaceSyncEnabled = true` (Doppel-Gate, `GetBoolSafeAsync` fuer beide); bei Master
   `false` kein SyncLog-Eintrag `ProductionWorkplaceSync`.
8. **Baustein b — Struktur-Ableitung korrekt.** Sub-FA mit Token `KA` in seiner DirectChildren-Token-
   Menge, `ProductionWorkplace` mit `ArbeitsschrittCode = "KA"` existiert -> genau eine neue
   `WorkOperation`-Zeile mit `OperationNumber = "KA"`, `Name` = Werkbank-Name, `ProductionWorkplaceId`
   = deren Id.
9. **Baustein b — unbekanntes Kuerzel.** Token ohne passenden `ArbeitsschrittCode`, NICHT auf der
   Ausschlussliste -> keine `WorkOperation`-Zeile, Sammelmeldung (Token-Liste + Anzahl betroffener
   Sub-FAs).
10. **Baustein b — ausgeschlossenes Kuerzel.** Token steht auf der Ausschlussliste (z. B. `BS`) -> keine
    `WorkOperation`-Zeile, **keine** Unbekannt-Meldung (bekannt, bewusst ausgenommen).
11. **Baustein b — Kuerzel-Mehrdeutigkeit.** Zwei aktive `ProductionWorkplace`-Zeilen mit identischem
    `ArbeitsschrittCode` -> keine `WorkOperation` wird angelegt, Meldung „mehrdeutig".
12. **Baustein b — Nur-hinzufuegen.** Eine bereits existierende `WorkOperation` mit gleichem
    `(ProductionOrderId, OperationNumber)` wird nie erneut angelegt oder veraendert — auch nicht, wenn
    sie manuell geloescht/deaktiviert wurde (sofern eine entsprechende App-Semantik existiert).
13. **Baustein b — DirectChildren, keine Doppelzaehlung.** Ein Token, das ausschliesslich auf einem
    Enkelknoten steht, erzeugt **keine** `WorkOperation` fuer den Grossvater-Sub-FA (identische Logik
    wie v1.41.0 AK 5).
14. **Baustein b — Fertig-/Storno-Filter.** Ein `ProductionOrder` mit `IsDone`/`IsCancelled`/
    `PickingStatus.IsDonePicking = true` bekommt keine neue `WorkOperation`, auch bei bekanntem Token.
15. **Baustein c — Verifikation ohne Resolver-Aenderung.** An einer Werkbank mit
    `ArbeitsschrittCode = "KA"` zeigt `GetAvailableOperations`/`BdeScanResolver` (Normal-Modus) fuer
    einen Sub-FA mit `WorkOperation.OperationNumber = "KA"` genau diesen Arbeitsgang — ohne dass
    `BdeScanResolver.cs` fuer diese Spec geaendert wurde (Nachweis: Diff zeigt keine Aenderung an
    dieser Datei).
16. **Baustein c — Existenz-Check-Fix.** Werkbank hat `BdeDefaultArbeitsgang = "Kanterei W1"` (identisch
    mit einer bereits angelegten, echten `WorkOperation.Name`) -> eine NurFA-Buchung ueber
    `FindOrCreateDefaultAsync` findet/legt eine eigene Zeile mit `OperationNumber = "01"` an, **bucht
    nicht** auf die echte `WorkOperation` mit `OperationNumber = "KA"`.
17. **AKE unveraendert.** `FaWorkStepDetectionService`, `FaWorkStep`, `WorkStep`-Katalog `VA/VE/VK/VL/VT`
    funktionieren nach dieser Spec identisch wie vorher; kein SyncLog-Eintrag der neuen/umbenannten
    Services bei Master `false`.
18. **Baustein-b-Gate.** Baustein (b) laeuft nur bei `ProduktionsauftragHierarchisch = true` UND dem
    (umbenannten) Toggle `true` (Doppel-Gate im `SyncWorker`, `GetBoolSafeAsync` fuer beide).
19. **Migration idempotent.** `SQL/93_AddProductionWorkplaceSageFields.sql` laeuft auf einer bestehenden
    Datenbank fehlerfrei und zweimal hintereinander ohne Aenderung beim zweiten Lauf (beide Spalten).

## Test-Szenarien

Neues Kapitel „IDEAL — Arbeitsgaenge (WorkOperation) aus der Struktur + Werkbank-Anlage" in
`docs/TESTSZENARIEN.md`, **naechste freie Nummer TS-77** (verifiziert 2026-09-22: TS-76 ist der letzte
bestehende Eintrag im Worktree):

- **TS-77.1 Baustein a — Neuanlage (AK 1).** Sage liefert einen Arbeitsplatz ohne WMS-Gegenstueck
  (`SageArbeitsplatznummer` unbekannt). Sync-Lauf. Erwartung: neue `ProductionWorkplace`-Zeile mit
  `SageArbeitsplatznummer`/`Name`/`ArbeitsschrittCode` gesetzt, `BdeAktiv = false`, Audit
  `CreatedBy = "ProductionWorkplaceSync"`.
- **TS-77.2 Baustein a — Abweichung (AK 2).** Bestehende Zeile mit abweichendem `ArbeitsschrittCode`/
  `Name` gegenueber Sage. Sync-Lauf. Erwartung: ueberschrieben + Sammelmeldung mit altem/neuem Wert.
- **TS-77.3 Baustein a — Ausschlussliste (AK 4).** Arbeitsplatz mit Kuerzel `STO`. Sync-Lauf. Erwartung:
  keine Zeile angelegt, kein SyncLog-Eintrag fuer diesen Arbeitsplatz.
- **TS-77.4 Baustein a — Trim (AK 5).** Arbeitsplatz mit Kuerzel `"EG "` (Leerzeichen). Sync-Lauf.
  Erwartung: `ArbeitsschrittCode = "EG"` ohne Leerzeichen.
- **TS-77.5 Baustein a — Pflicht-Filter (AK 6).** Arbeitsplaetze mit falschem Mandant, `Aktiv = 0` bzw.
  leerem Kuerzel. Sync-Lauf. Erwartung: alle drei uebersprungen, kein Insert, keine Meldung.
- **TS-77.6 Baustein a — Master-Gate (AK 7).** Master `false`. Sync-Lauf. Erwartung: kein Lauf, kein
  SyncLog-Eintrag `ProductionWorkplaceSync`.
- **TS-77.7 Baustein b — Grundfall (AK 8).** Sub-FA mit Token `KA`, `ProductionWorkplace` mit
  `ArbeitsschrittCode = "KA"` existiert. Erwartung: genau eine `WorkOperation` mit korrektem
  `ProductionWorkplaceId`.
- **TS-77.8 Baustein b — unbekanntes Kuerzel (AK 9).** Token ohne Katalog-Treffer, nicht auf der
  Ausschlussliste. Erwartung: keine `WorkOperation`, Sammelmeldung, keine Exception.
- **TS-77.9 Baustein b — ausgeschlossenes Kuerzel (AK 10).** Token auf der Ausschlussliste (z. B. `BS`).
  Erwartung: keine `WorkOperation`, **keine** Unbekannt-Meldung.
- **TS-77.10 Baustein b — Kuerzel-Mehrdeutigkeit (AK 11).** Zwei aktive Werkbaenke mit identischem
  `ArbeitsschrittCode`. Erwartung: keine `WorkOperation`, Meldung „mehrdeutig".
- **TS-77.11 Baustein b — DirectChildren-Gegenprobe (AK 13).** Wie TS-76.2 (v1.41.0), aber Zielentitaet
  `WorkOperation` statt `FaWorkStep`.
- **TS-77.12 Baustein c — Terminal zeigt den echten AG (AK 15).** An einer Werkbank mit passendem
  `ArbeitsschrittCode` erscheint der struktur-abgeleitete Arbeitsgang eines gescannten Sub-FA im
  Normal-Modus, ohne manuelle Auswahl eines Default-AG.
- **TS-77.13 Baustein c — Existenz-Check-Fix (AK 16).** `BdeDefaultArbeitsgang` der Werkbank ist
  identisch mit dem `Name` einer bereits angelegten, echten `WorkOperation`. NurFA-Buchung ausloesen.
  Erwartung: neue/eigene Zeile mit `OperationNumber = "01"` wird verwendet, **nicht** die echte Zeile
  mit `OperationNumber = "KA"`.
- **TS-77.14 Regressionslauf TS-66.** Bestehende BDE-Disambiguierungs-Szenarien
  ([[2026-07-29-standort-ideal-teil-8-spec]], TS-66) laufen mit echten, struktur-abgeleiteten
  `WorkOperation`s statt nur dem Default-AG unveraendert durch (insbesondere: mehrere Sub-FAs
  derselben `OrderNumber`, Auswahlliste, NurFA-Fix).
- **TS-77.15 AKE-Regression (AK 17).** Master `false`: `FaWorkStepDetectionService` unveraendert aktiv,
  kein Lauf der neuen/umbenannten Services.

Nach Abschluss `secondbrain/tests/testszenarien-index.md` nachziehen; TS-76 (v1.41.0) im Index als
„ersetzt durch TS-77" kennzeichnen statt zu loeschen (Nachvollziehbarkeit der Historie).

Alle drei Bausteine sind **nicht InMemory-vollstaendig-testbar** (Sage-Quellzugriff, Zeitpunkt-
abhaengige Sync-Reihenfolge) — Manual-UAT auf der befuellten IDEAL-Testinstanz ist Pflicht, analog dem
bereits dokumentierten Muster bei v1.41.0 (H3 dort).

## Reihenfolge / Einordnung

- **Im Buendel-Zweig**, wie alle IDEAL-Bausteine seit August (`feature/2026-08-07-ideal-teile-1-5`) —
  kein neuer Worktree.
- **Ersetzt** [[2026-09-08-arbeitsgaenge-aus-arbeitsschritte-spec]] (v1.41.0) fachlich vollstaendig;
  deren Status/Changelog-Eintrag ist bei der Umsetzung als „superseded" zu kennzeichnen (Brain-Aufgabe,
  nicht Teil dieser Spec-Datei selbst — die v1.41.0-Spec wird hier bewusst NICHT umgeschrieben).
- **Setzt voraus:** [[2026-09-21-adr-0014-arbeitsbereich-ist-zielort-spec]] — sie entfernt die falsche
  Arbeitsbereich→Werkbank-Ableitung der Materialisierung (ADR 0014 beruhte auf der inzwischen
  widerlegten Annahme „Werkbank = Arbeitsbereich"). Diese Spec legt DIE ECHTEN Werkbaenke erst an; ohne
  den Rueckbau wuerde die Materialisierung weiterhin versuchen, `Arbeitsbereich`-Werte auf
  `ProductionWorkplace.Name` zu matchen und nach der Anlage jeden Arbeitsbereich als „unbekannte
  Werkbank" melden (Rauschen). Stand Worktree 2026-09-22: die Rueckbau-Spec ist bereits **Testbereit**
  (Commits `d0bd6a1`/`4daa405`) — diese Abhaengigkeit ist damit erfuellt, sobald sie gemergt ist.
- **Setzt ausserdem voraus:** [[2026-08-20-materialisierung-fachliche-felder-spec]] (materialisierte
  `ProductionOrder`-Zeilen als Ziel von Baustein b) — bereits Testbereit im selben Worktree.
- **Reihenfolge der drei Bausteine:** a (Anlage/Katalog) vor b (Ableitung) vor c (Terminal-Verifikation)
  — b braucht a's Katalog, c braucht b's Datenzeilen zum Testen.

**Etappen-Entscheidung (Antwort 8, verbindlich): Epic mit ZWEI Etappen, EIN Merge, kein Split in
getrennte Specs.** Der Vorteil „einzeln mergbar" existiert im Buendel-Modell nicht — nichts merged,
bevor Schranke 2 fuer das ganze Buendel faellt, schnelleres Teil-Feedback gibt es also nicht, nur mehr
Koordination. Baustein (c) ist durch Empfehlung F (Antwort 1) und den Existenz-Check-Fix (Antwort 6) so
klein geworden, dass er mit Baustein (b) in einer Etappe Platz hat:

| Etappe | Inhalt | Abschluss |
|---|---|---|
| A | Baustein (a): Modell (zwei Spalten), Migration, `ProductionWorkplaceSyncService`, ServiceSettings/Standorteinstellungen (Mandant, Ausschlussliste, Toggle), SyncWorker-Doppel-Gate, UI read-only | Eigener, buildbarer Commit. **STOPP + melden** |
| B | Bausteine (b)+(c): `WorkOperationStructureDetectionService`-Umbau (inkl. Kuerzel-Mehrdeutigkeit + Ausschlussliste-Sonderfall), Umbenennungen, `BdeDefaultWorkOperationService`-Existenz-Check-Fix, Terminal-Verifikations-AK, TS-77, Brain-Update | Eigener Commit, dann QA/Merge fuer das gesamte Buendel (Schranke 2) |

Etappe B ist die riskante (Umbenennung, Zieltabellen-Wechsel) — deshalb der Halt davor. Der Zweig muss
waehrend der Arbeit ueber `scripts/sync-worktree.ps1` aktuell gehalten werden (mehrere andere IDEAL-Teile
laufen im selben Buendel-Worktree).

## Brain-Pflichten bei Umsetzung (Merkliste fuer den Dev-Lauf)

- Version-Bump in **beiden** `AppVersion.cs` — Stand Worktree 2026-09-22: aktuelle Version `1.43.0`
  (nach dem ADR-0014-Rueckbau). Naechste freie Nummer vermutlich `1.44.0` fuer Etappe A und `1.45.0`
  fuer Etappe B (oder ein gemeinsamer Bump nach Abschluss beider Etappen, falls kein Zwischen-Deploy
  erfolgt) — im Dev-Lauf zu bestaetigen.
- Anwender-Changelog `Views/Help/Changelog.cshtml` + Brain-Changelog
  `secondbrain/changelog/YYYY-MM-DD-vX-Y-Z-*.md`.
- `secondbrain/feature-map.md` — IDEAL-Abschnitt aktualisieren, v1.41.0-Zeile als superseded markieren.
- `secondbrain/specs/freigegeben/2026-09-08-arbeitsgaenge-aus-arbeitsschritte-spec.md` — im
  **Hauptcheckout** (nicht Worktree) einen Hinweis „superseded durch
  [[2026-09-21-ideal-bde-arbeitsgaenge-aus-struktur-spec]]" ergaenzen (additiv, Datei nicht
  umschreiben).
- `secondbrain/codebase/services.md` — Sync-Services-Tabelle: neuen Eintrag `ProductionWorkplaceSyncService`,
  umbenannten Eintrag `WorkOperationStructureDetectionService` (ersetzt die bestehende
  `FaWorkStepStructureDetectionService`-Zeile), `ServiceSettings`-Katalogtabelle nachziehen.
- `secondbrain/architektur/fallstricke.md` — Eintrag: **drei** leicht verwechselbare, alle aus Sage
  stammende Vokabulare bei IDEAL — `Arbeitsbereich` (`USER_OSAbteilung`, Zielort, `ProductionOrder.
  ProductionWorkplaceId` via Materialisierung/ADR 0014), `Arbeitsschritt` (`FaHierarchyNode.
  Arbeitsschritte`, Arbeitsgang-Token) und `Werkbank`/Sage-**Arbeitsplatz** (`KHKPpsArbeitsplaetze`,
  Anlage-Ziel dieser Spec, Match-Feld `WorkOperation.ProductionWorkplaceId`) — unterschiedliche
  Sage-Quellspalten, unterschiedliche Zielfelder, keine hierarchische Beziehung zueinander.
- Hilfeseite (`Views/Help/`) — Hinweis, dass `ProductionWorkplace.SageArbeitsplatznummer`/`Name`/
  `ArbeitsschrittCode` Sage-gefuehrt sind (manuelle Aenderung ueberlebt nicht den naechsten Sync-Lauf);
  Hinweis, dass ein Wechsel der Ausschlussliste bestehende Werkbaenke nicht rueckwirkend entfernt.
- `secondbrain/tests/testszenarien-index.md` — TS-77-Zeile, TS-76 als superseded markieren.

## Deploy

**Provisorisch (Dev-Lauf bestaetigt gegen den echten Diff):**

- **Web-App: ja.** `IdealAkeWms/Models/ProductionWorkplace.cs`, `ServiceSettingDefinitions.cs`,
  `Models/Standort/StandortSettingsCatalog.cs`, `SyncLogServices.cs`, `Views/ProductionWorkplaces/*.cshtml`,
  `BdeDefaultWorkOperationService.cs`, Migration.
- **Service: ja.** Neuer Service `ProductionWorkplaceSyncService` (Baustein a), umbenannter/umgebauter
  `WorkOperationStructureDetectionService` (Baustein b), `SyncWorker.cs`, `Program.cs` (DI, neu +
  umbenannt).
- **Migration: ja** — genau eine, zwei Spalten, aus Baustein (a) (siehe „Migrations-/SQL-Auswirkungen").
  Reihenfolge: DB-Migration vor Service-Neustart (Standardablauf), kein Daten-Backup-Zwang ueber das
  uebliche Mass hinaus (nicht destruktiv).
- **Ablauf ueber die zwei Etappen, mit DryRun je Etappe:**
  1. Etappe A deployen -> `Sync:ProductionWorkplaceSyncEnabled` zunaechst mit `WorkerSettings:SyncDryRun`
     beobachten: wie viele Arbeitsplaetze werden neu angelegt (~60 erwartet), welche fallen unter die
     Ausschlussliste, welche werden uebersprungen (Filter) -> scharf schalten.
  2. Nach dem scharfen Lauf: alle neuen Werkbaenke starten mit `BdeAktiv = false` — der Mensch schaltet
     die tatsaechlich terminal-relevanten Arbeitsplaetze bewusst frei, bevor Etappe B live geht.
  3. Etappe B deployen -> `Sync:FaWorkStepStructureDetectionEnabled`-Nachfolgekey mit DryRun beobachten:
     welche Sub-FAs bekommen Arbeitsgaenge, welche Token bleiben unbekannt -> scharf schalten.
  Kein separater Katalog-Pflegeschritt zwischen a und b noetig (anders als v1.41.0s `/WorkSteps`-Pflege)
  — das ist die unter „Technischer Loesungsentwurf" beschriebene strukturelle Vereinfachung.
- **Publish-Befehle (aus dem Worktree):**
  ```
  dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb
  dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService
  ```
  *Nach dem Merge* nur erneut aus `main` publishen, falls der Merge tatsaechlich getestete Dateien mit
  parallelen `main`-Aenderungen zusammengefuehrt hat.

## Offene Rueckfragen

1. **Routing-Design (Baustein c).** ~~`WorkOperation.ProductionWorkplaceId` bei der Anlage (Baustein b)
   direkt setzen — Empfehlung F — oder die im Backlog skizzierte Live-Kuerzel-Schnittmenge zur Scan-Zeit
   berechnen?~~ **ENTSCHIEDEN (Antwort 1): Empfehlung F.** Siehe Rumpf.
2. **Varianten-Regel.** ~~Struktur nutzt Basis-Kuerzel (`KA`); Sage hat Werkbank-Varianten. Exakt `KA`
   oder Praefix-/Gruppen-Match?~~ **ENTSCHIEDEN (Antwort 2): exakter Basis-Kuerzel-Match, kein
   Praefix.** Siehe Rumpf.
3. **5 unbekannte Kuerzel** (`MO`/`ZS`/`LÖ`/`PR`/`BE`): ~~melden, Werkbank in Sage nachtragen, oder aus
   dem Scope ausschliessen?~~ **ENTSCHIEDEN (Antwort 3): melden, im Code nicht ausschliessen.** Siehe
   Rumpf.
4. **Sage-Quelle Baustein a.** ~~Genaue Tabelle/View der Arbeitsplatz-Stammdaten bestaetigen.~~
   **ENTSCHIEDEN (Antwort 4): `KHKPpsArbeitsplaetze`**, inkl. Verknuepfungsschluessel
   `SageArbeitsplatznummer`, Anlege-Semantik, Ausschlussliste, Master-Gate. Siehe Rumpf.
5. **Kuerzel->Name-Katalog.** ~~Bestaetigung, dass kein separater WorkOperation-Katalog noetig ist.~~
   **ENTSCHIEDEN (Antwort 5): bestaetigt**, `Name` kommt aus `ProductionWorkplace.Name`.
6. **Koexistenz `BdeDefaultWorkOperationService`.** ~~Bei IDEAL abschalten/ersetzen?~~ **ENTSCHIEDEN
   (Antwort 6): NurFA bleibt beim Default (Modus-Definition); der eigentliche Fund ist der
   Existenz-Check-Fix (`OperationNumber` statt `Name`), jetzt in AK 16/TS-77.13.**
7. **Migrationsnummer-Koordination.** `SQL/92` ist von
   [[2026-09-18-stueckliste-kommissionierziel-filter-spec]] im selben Worktree belegt (Status
   Freigegeben). **Verifiziert 2026-09-22:** die Datei liegt tatsaechlich im Worktree -> `93` ist damit
   Stand heute die naechste freie Nummer. Bleibt ein Dev-Lauf-Check, falls bis zur Umsetzung weitere
   Migrationen dazukommen — nicht blind uebernehmen.
8. **Umfang/Schnitt.** ~~Epic mit 3 Etappen (a->b->c, ein Merge) oder 3 getrennte, einzeln mergbare
   Specs?~~ **ENTSCHIEDEN (Antwort 8): Epic mit ZWEI Etappen** (A = Baustein a; B = Bausteine b+c). Siehe
   „Reihenfolge/Einordnung".
9. **Sage-Spaltentyp `Arbeitsplatznummer`.** Ist `KHKPpsArbeitsplaetze.Arbeitsplatznummer` `int` oder
   `nvarchar` mit fuehrenden Nullen (Beispiele `2300`, `0000`)? Im WMS-Repo nicht verifizierbar (externe
   Sage-Tabelle, keine bestehende Code-Referenz) — vor der Migration gegen das tatsaechliche Sage-Schema
   pruefen (z. B. `INFORMATION_SCHEMA.COLUMNS`). `ProductionWorkplace.SageArbeitsplatznummer` spiegelt
   den gefundenen Typ.

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

1. → **Empfehlung F — `ProductionWorkplaceId` bei der Anlage setzen.** Keine Live-Schnittmenge.
   Die Begruendung der Spec traegt: Der Filter existiert an drei Stellen bereits, und
   `BdeScanResolver` ist ein **produktiv genutzter, auch von AKE durchlaufener Pfad** — ihn fuer eine
   Ersparnis umzubauen, die es nicht gibt, waere das falsche Risiko.
   **Eine Konsequenz von F, die die Spec nicht benennt und die dokumentiert gehoert:** F
   **materialisiert** das Routing zum Anlagezeitpunkt. Weil Baustein (b) Nur-hinzufuegen ist, wird eine
   bestehende `WorkOperation` **nie** nachgezogen. Ordnet Sage ein Kuerzel spaeter einer anderen
   Werkbank zu, routen **bereits angelegte, offene** Arbeitsgaenge weiter zur **alten** Werkbank; nur
   neue gehen zur neuen. Bei einer Live-Schnittmenge wirkte die Aenderung sofort.
   **Vertretbar**, weil die Zuordnung Kuerzel→Werkbank Stammdaten sind und sich selten aendern — und
   weil Baustein (a) jede Abweichung **meldet** (AK 4). Die Meldung ist damit das Signal, bestehende
   offene Arbeitsgaenge zu pruefen. **Als bekannte Eigenschaft in den Rumpf, nicht als Fehler.**

2. → **Exakter Basis-Kuerzel-Match. Kein Praefix.**
   Ein Praefix-Match waere **mehrdeutig**: `KA` traefe `KA2` **und** `KA4` — welche Werkbank? Genau
   die Mehrdeutigkeit, die das ganze Paket sonst meldet statt aufloest. Die Struktur liefert
   Basis-Kuerzel; exakt passt.
   **Folge, im UAT zu beobachten:** Werkbaenke mit Varianten-Kuerzel (`KA2`, `SW1` …) bekommen **keine**
   struktur-abgeleiteten Arbeitsgaenge — nur die Basis-Werkbank. Ob Varianten **alternative Werkbaenke
   fuer denselben Arbeitsgang** sind (dann muessten sie `KA`-Arbeit auch sehen) oder **eigene
   Arbeitsgaenge** (dann ist exakt richtig), ist eine Fachfrage. Exakt ist der sichere Start; eine
   Zuordnung Variante→Basis waere eine spaetere, eigene Erweiterung — mit Beleg aus dem Betrieb.

3. → **Melden — und im Code NICHT ausschliessen.** Der Melde-Mechanismus heilt sich selbst: Sobald die
   Werkbank in Sage steht, greift Baustein (a) beim naechsten Lauf, und Baustein (b) legt die
   Arbeitsgaenge an — **ohne Code-Aenderung**. Ein Ausschluss im Code muesste dagegen spaeter wieder
   ausgebaut werden.
   **Fachlich je Kuerzel zu pruefen** (keine Code-Aufgabe): Fehlt die Werkbank nur in Sage →
   nachtragen. Ist es ein Arbeitsgang **ohne** scannbare Werkbank (etwa extern) → bleibt er zu Recht
   unbuchbar, und die Meldung ist dann dauerhaft erklaert.

4. → **Quelle: `KHKPpsArbeitsplaetze`** (am 2026-09-21 vom Menschen bereitgestellt). Relevante Spalten:
   `Arbeitsplatznummer`, `Mandant`, `Bezeichnung1`, `Aktiv`, `USER_ArbeitsSchritt`.

   **Drei Pflicht-Filter, sonst liest der Sync falsch** — **ENTSCHIEDEN 2026-09-21:**
   - **`Mandant = 1`** — gilt bei beiden Standorten immer. **Als Parameter in den
     IDEAL-Standorteinstellungen** (Teil 6, [[2026-08-03-standorteinstellungen-maske]]) hinterlegt,
     Standardwert `1` — sichtbar, dokumentiert und ohne Deploy aenderbar (Entscheidung 2026-09-21;
     ersetzt den frueheren Vorschlag einer Konstante im Service).
   - **`Aktiv = -1`** — Sage-Konvention wie bei `KHKArtikel`. Ein stillgelegter Arbeitsplatz mit gleicher
     `Bezeichnung1` wie ein aktiver erzeugte sonst eine falsche Mehrdeutigkeit.
   - **`USER_ArbeitsSchritt` gefuellt** — Arbeitsplaetze ohne Kuerzel ueberspringen, nicht melden.

   **Doppelte Kuerzel — eine Luecke in Baustein (b):** Tragen **zwei aktive** Arbeitsplaetze dasselbe
   Kuerzel, weiss Baustein (b) nicht, welche `ProductionWorkplaceId` er setzen soll. Die Spec regelt
   Mehrdeutigkeit heute nur fuer **Namen** (Baustein a), nicht fuer **Kuerzel** (Baustein b).
   **Verbindlich:** Trifft ein Token mehrere Werkbaenke, wird **keine** `WorkOperation` angelegt, sondern
   „mehrdeutig" gemeldet — gleiches Muster wie ueberall. Vorab pruefbar:
   ```sql
   SELECT USER_ArbeitsSchritt, COUNT(*) AS Anzahl
   FROM KHKPpsArbeitsplaetze
   WHERE Aktiv = -1 AND ISNULL(USER_ArbeitsSchritt, '') <> '' -- plus Mandant-Filter
   GROUP BY USER_ArbeitsSchritt HAVING COUNT(*) > 1;
   ```

   ### NEUE BLOCKER-FRAGE: Wird `ProductionWorkplace.Name` gegen ZWEI verschiedene Vokabulare gematcht?

   `ProductionWorkplace` fuehrt **keinen** Sage-Schluessel — `Name` ist das einzige Verknuepfungsfeld
   (am Code geprueft). Und es wird damit **zweimal** gematcht, gegen **unterschiedliche** Sage-Quellen:
   - **ADR 0014 / Materialisierung:** `Name` ↔ **`Arbeitsbereich`** aus `USER_OSAbteilung` — Werte wie
     `K-02`, `S-01`, `H4-04`.
   - **Diese Spec, Baustein (a):** `Name` ↔ **`Bezeichnung1`** aus `KHKPpsArbeitsplaetze` — Werte wie
     „Kanterei W1", „Schweisserei W1".

   **Ein und dasselbe Feld kann nicht beide Konventionen gleichzeitig erfuellen.** Heissen die
   WMS-Werkbaenke `K-02`, findet Baustein (a) **keinen einzigen** Treffer und meldet jede Werkbank als
   unbekannt. Heissen sie „Kanterei W1", laeuft umgekehrt die ADR-0014-Zuordnung ins Leere. Das waere
   ein Sync, der sauber durchlaeuft und nichts tut.

   Die Spec hat den Unterschied der beiden Vokabulare selbst erkannt (Out-of-Scope, fallstricke-Merkliste)
   — aber nicht, dass **beide auf dasselbe Zielfeld** zielen.

   **VOR der Umsetzung zu pruefen:**
   ```sql
   -- WMS: wie heissen die Werkbaenke heute?
   SELECT Name FROM ProductionWorkplaces ORDER BY Name;
   -- Sage: wie heissen die Arbeitsplaetze?
   SELECT Bezeichnung1, USER_ArbeitsSchritt FROM KHKPpsArbeitsplaetze
   WHERE Aktiv = -1 ORDER BY Bezeichnung1;   -- plus Mandant-Filter
   ```
   **Moegliche Ergebnisse:**
   - **Die Namen ueberschneiden sich** (etwa weil es zwei Arten Werkbank-Eintraege gibt: Bereiche wie
     `K-02` und Arbeitsplaetze wie „Kanterei W1") → Entwurf traegt, aber die Koexistenz gehoert benannt.
   - **Sie ueberschneiden sich nicht** → der Name-Match traegt nicht. Dann ist der robuste Weg ein
     **eigener Schluessel**: `ProductionWorkplace.SageArbeitsplatznummer` aus
     `KHKPpsArbeitsplaetze.Arbeitsplatznummer`, einmalig zugeordnet, danach umbenennungssicher. Das
     waere eine zweite Spalte in derselben Migration — kein neuer Baustein.

   **Hinweis, bewusst ausserhalb des Umfangs:** Die Tabelle fuehrt eine Spalte **`BdeTerminalId`**. Moeglich,
   dass Sage selbst schon eine Zuordnung Arbeitsplatz→BDE-Terminal kennt. Nicht Teil dieser Spec — aber
   vor einem kuenftigen Terminal-Umbau wert, sie anzusehen, statt eine zweite Zuordnung zu erfinden.

   ### Stand 2026-09-21: Bei IDEAL gibt es noch KEINE `ProductionWorkplaces`

   **Folge, die bekannt sein muss:** Ohne Werkbaenke findet Baustein (a) keinen Treffer und meldet jeden
   Sage-Arbeitsplatz als unbekannt; Baustein (b) findet kein Kuerzel und legt keine Arbeitsgaenge an.
   **Die Kette liefert nichts, bis die Werkbaenke existieren.** Die Materialisierung braucht sie
   ebenfalls (Werkbank-Zuordnung aus dem Arbeitsbereich) — dort ist das Anlegen vor dem Deploy bereits
   Vorbedingung.
   **Die leere Tabelle ist zugleich die Chance:** Die Namenskonvention laesst sich jetzt ohne Altlast
   festlegen.

   **OFFEN — vor der Umsetzung vom Menschen zu entscheiden (fachlich, nicht am Code ablesbar):**
   Ist eine IDEAL-Werkbank ein **Arbeitsbereich** (`K-02`, Quelle `USER_OSAbteilung`, Ziel der
   Materialisierung) oder ein **Arbeitsplatz** („Kanterei W1", Quelle `KHKPpsArbeitsplaetze`, Ziel dieser
   Spec)?
   - **Dieselben Dinge mit zwei Namen** → einmal anlegen; `Name` traegt einen der beiden, der andere
     bekommt einen eigenen Schluessel (`SageArbeitsplatznummer`).
   - **Verschiedene Dinge** (ein Bereich enthaelt mehrere Arbeitsplaetze) → Materialisierung und
     Arbeitsgaenge zeigen auf **unterschiedliche Ebenen**; dann gehoeren sie nicht beide an
     `ProductionWorkplace.Name`.
   **AKE:** Laut Mensch keine Ueberschneidungen zu erwarten.

   ### Loesungsweg ueber die Standorteinstellungen — aber mit dem RICHTIGEN Parameter

   **Vorschlag des Menschen (2026-09-21):** die Frage als Parameter in den IDEAL-Standorteinstellungen
   hinterlegbar machen. Das traegt — **aber nur mit dem richtigen Parameter:**

   **NICHT: ein Schalter „Werkbank = Arbeitsbereich oder Arbeitsplatz".** Beide Abgleiche zielen auf
   dasselbe Feld `ProductionWorkplace.Name`. Ein solcher Schalter waehlte nur aus, **welcher** der beiden
   ins Leere laeuft — und Werkbaenke, die unter der einen Einstellung angelegt wurden, truegen nach
   einem Umschalten falsche Namen. Er verdeckt den Konflikt, statt ihn zu loesen.

   **SONDERN: „Match-Spalte fuer Baustein (a)"** — gegen welche Spalte von `KHKPpsArbeitsplaetze` wird
   `ProductionWorkplace.Name` abgeglichen: `Bezeichnung1`, `Matchcode` oder `Arbeitsplatznummer`.
   **Das kann den Konflikt tatsaechlich aufloesen:** Traegt eine dieser Spalten dieselben Werte wie der
   `Arbeitsbereich` (`K-02`, `S-01`, `H4-04` …), dann heisst die Werkbank `K-02`, die Materialisierung
   (ADR 0014) findet sie ueber den Arbeitsbereich, und Baustein (a) findet sie **ebenfalls** — nur ueber
   die gewaehlte Sage-Spalte. **Ein Name, beide Abgleiche, keine zweite Schluesselspalte.**

   **Pruefbar vor der Umsetzung:**
   ```sql
   SELECT Arbeitsplatznummer, Matchcode, Bezeichnung1, USER_ArbeitsSchritt
   FROM KHKPpsArbeitsplaetze
   WHERE Mandant = 1 AND Aktiv = -1
   ORDER BY Arbeitsplatznummer;
   ```
   - **Eine Spalte traegt Werte wie `K-02`** → sie wird die Match-Spalte, Standardwert des Parameters.
     Konflikt geloest, **Werkbaenke werden nach dem Arbeitsbereich benannt.**
   - **Keine Spalte passt** → der Parameter hilft nicht; dann bleibt die zweite Schluesselspalte
     `SageArbeitsplatznummer` der Weg (siehe oben).

   **Warum der Parameter trotzdem bleibt, auch wenn die Pruefung eindeutig ist:** Die Zuordnung ist eine
   Stammdaten-Konvention, die sich aendern kann, ohne dass jemand den Code anfasst. Die Einstellung macht
   sie sichtbar und ohne Deploy korrigierbar — dieselbe Begruendung wie fuer den Mandanten.
   **Eine Pflicht dazu:** Die Hilfeseite muss sagen, dass ein Wechsel der Match-Spalte **bestehende**
   Werkbank-Zuordnungen nicht umschreibt — er wirkt erst beim naechsten Sync-Lauf, und Werkbaenke mit
   unpassendem Namen fallen dann in die Meldung.

   ### ERGEBNIS DER ABFRAGE (2026-09-21) — die Vermutung traegt NICHT

   `KHKPpsArbeitsplaetze` (Mandant 1, aktiv) liefert 66 Arbeitsplaetze. **Keine Spalte traegt
   Arbeitsbereichs-Werte wie `K-02`:** `Arbeitsplatznummer` ist vierstellig numerisch (`2300`),
   `Matchcode` ist nahezu identisch mit `Bezeichnung1` („Kanterei W1"), `USER_ArbeitsSchritt` traegt das
   Kuerzel (`KA`). **Der Parameter „Match-Spalte" ueberbrueckt die beiden Vokabulare nicht.**

   **Damit ist die Grundfrage nicht mehr zu umgehen:** Der `Arbeitsbereich` (`K-02`, `S-01`, `H4-04`,
   Sage-Feld `USER_OSAbteilung`) und der **Sage-PPS-Arbeitsplatz** (`2300` / „Kanterei W1" / `KA`, Tabelle
   `KHKPpsArbeitsplaetze`) sind **zwei Vokabulare** — beide aus Sage, aber ohne gemeinsamen Wert.
   `ProductionWorkplace.Name` kann nicht beide tragen.
   *Korrektur 2026-09-21:* Eine fruehere Fassung dieses Absatzes deutete `USER_OSAbteilung` als
   **OSEON**-Abteilung. **Das war falsch — IDEAL hat kein OSEON** (steht bereits im Ziel-Abschnitt dieser
   Spec). Der Schluss kam aus dem Feldnamen (`OS`), nicht aus den Daten.

   **Offen — moeglicherweise gibt es die Bruecke doch:** Weil **beide Felder aus Sage** stammen, ist denkbar,
   dass Sage intern eine Zuordnung Arbeitsbereich→Arbeitsplatz kennt (eine Stammdaten-Tabelle, ein
   Feld am Arbeitsplatz). **Vor einer eigenen Zuordnungstabelle im WMS pruefen**, ob es die in Sage
   schon gibt — dieselbe Leitlinie wie ueberall (ponytail, Sprosse 2).

   **Entscheidung des Menschen noetig — praktisch gefragt:** An welcher Stelle steht das
   **BDE-Terminal**, an dem der Werker scannt — an einem **Arbeitsplatz** wie „Kanterei W1" oder an
   einem **Bereich** wie `K-02`? Das Vokabular, das den Terminal-Standort benennt, ist das, was eine
   WMS-Werkbank bei IDEAL sein muss.
   - **Arbeitsplatz** → die Werkbaenke werden aus `KHKPpsArbeitsplaetze` angelegt; die
     Materialisierung (ADR 0014, Werkbank aus dem **Arbeitsbereich**) braucht dann eine Zuordnung
     Arbeitsbereich→Arbeitsplatz, oder ihre Werkbank-Ableitung ist neu zu bewerten.
   - **Bereich** → ADR 0014 bleibt, aber das BDE-Routing (diese Spec) braucht eine Zuordnung
     Arbeitsplatz→Bereich, weil die Kuerzel an den Arbeitsplaetzen haengen.
   Beides ist machbar. **Nicht machbar ist, beide ueber denselben Namen zu verknuepfen.**
   Hintergrund: ADR 0014 entstand mit der Annahme „Werkbank = Arbeitsbereich aus der FA-Struktur" —
   bevor die Arbeitsplatz-Stammdaten vorlagen. Diese Annahme gehoert jetzt ueberprueft, nicht nur
   ergaenzt.

   **Unabhaengig von der Antwort: Abgleich ueber `Arbeitsplatznummer`, NICHT ueber einen Namen.**
   Die Namensspalten sind fuer einen Abgleich ungeeignet — am Datensatz belegt:
   - `Matchcode` und `Bezeichnung1` **weichen ab** (2150: „Flaechen entgraten (X)" vs. „Flaechen entgr
     beendet"; 2550: „Punkten Ladenbau W1" vs. „…W1 (EKP)"; 2950; 0000).
   - **Doppelte Leerzeichen** („Schaeumerei  W1 (EG)", „Endmontage SB  W1 (OG)").
   - Eine Umbenennung in Sage braeche die Verknuepfung.
   `Arbeitsplatznummer` ist dagegen **eindeutig, stabil und sauber**. Damit wird
   **`ProductionWorkplace.SageArbeitsplatznummer`** zur Pflichtspalte neben `ArbeitsschrittCode` — dieselbe
   Migration. Der Parameter „Match-Spalte" **entfaellt** (es gibt nur einen tauglichen Schluessel).

   ### Datenbefunde aus der Liste — fuer den Sync verbindlich

   - **`"EG "` mit Leerzeichen** (Arbeitsplatz 2150). Ohne `TRIM` matcht das Struktur-Token `EG` nie.
     **Kuerzel beim Lesen trimmen — Pflicht**, sonst faellt `EG` still in die Unbekannt-Meldung.
   - **Keine doppelten Kuerzel** unter den aktiven Arbeitsplaetzen. Die Mehrdeutigkeitsregel fuer
     Baustein (b) bleibt als Absicherung, tritt heute aber nicht ein.
   - **Pseudo-Arbeitsplaetze** stehen mit `Aktiv = -1` in der Liste: **Storno** (`STO`), **Gestoppt**
     (`XXX`), **Reserve** (`x01`), dazu Verkauf, Lager, Verladung, externe Beschichter, Reklamation
     (`RM-*`). Sie stoeren nicht, **solange kein Struktur-Token auf sie zeigt** — ein Arbeitsgang
     „Storno" am Terminal waere aber offensichtlich falsch. **Zu klaeren:** Sollen Kuerzel wie `STO`/`XXX`
     vom Routing ausgeschlossen werden, oder koennen sie in der Struktur ohnehin nicht vorkommen?
   - **Ausgelaufene Arbeitsplaetze** mit Markierung `(X)` bzw. „beendet" sind noch aktiv — **darunter
     `EG`**, eines der zwoelf gematchten Kuerzel. Wird `EG`-Arbeit auf einen beendeten Arbeitsplatz
     geroutet? Fachlich zu pruefen.
   - **Korrektur der Zuordnungstabelle oben:** `VM` ist **„Vormontage W1"** (3002), nicht
     „Elektrofertigung W1 (OG)" — das ist `VM9` (3001). Der Sync liest das Kuerzel aus der Quelle und
     ist davon nicht betroffen; die Tabelle ist aber als Dokumentation falsch und gehoert korrigiert.
   - **Die fuenf unbekannten Kuerzel** (`MO`/`ZS`/`LÖ`/`PR`/`BE`) stehen **nicht** in der Liste — bestaetigt.

   ### AUFLOESUNG (2026-09-21) — drei verschiedene Begriffe, vom Menschen geklaert

   | Begriff | Bedeutung | Quelle |
   |---|---|---|
   | **Arbeitsbereich** (`K-02`, `S-01`) | **Zielort** — wohin das Teil kommt, wenn alle Arbeitsschritte erledigt sind, bzw. allgemein sein Bestimmungsort. Begrifflich aus **Lagerorten** abgeleitet. | `USER_OSAbteilung` |
   | **Arbeitsschritt** (`SW`, `LS`, `KA`) | **Arbeitsgang**, der am Teil erfolgen muss | `FaHierarchyNode.Arbeitsschritte` |
   | **Werkbank** (fuers BDE) | **Arbeitsplatz**, an dem gearbeitet wird | `KHKPpsArbeitsplaetze` |

   **Arbeitsbereich und Arbeitsschritt stehen in KEINER hierarchischen Beziehung.**

   **Damit ist entschieden: Eine IDEAL-Werkbank im WMS ist der Sage-Arbeitsplatz.** BDE-Terminals sollen
   an den meisten Arbeitsplaetzen stehen.
   - **Verknuepfung ueber `SageArbeitsplatznummer`** (aus `KHKPpsArbeitsplaetze.Arbeitsplatznummer`) —
     nicht ueber den Namen (siehe Datenbefunde oben).
   - **`Name` = `Bezeichnung1`** — fuer die **Anzeige** (Entscheidung 2026-09-21), kein Abgleichsschluessel.
   - **`ArbeitsschrittCode` = `USER_ArbeitsSchritt`** (getrimmt), Sage-gefuehrt, read-only.
   - **Einrichtung — der Sync LEGT DIE WERKBAENKE AN (Entscheidung 2026-09-21):** Nummer, Name und
     Kuerzel werden **im DB-Abgleich** gesetzt, nicht von Hand. Das heisst zwingend: Baustein (a) legt
     fehlende Werkbaenke **selbst an** — ohne Namensabgleich gaebe es sonst keinen Weg, eine Nummer der
     richtigen Werkbank zuzuordnen.
     **Damit kehrt sich `melden statt anlegen` fuer DIESE Tabelle um — bewusst und begruendet:** Bei
     ADR 0014 kamen die Werte aus **freiem Text an Auftragspositionen** (Tippfehler, Altlasten).
     `KHKPpsArbeitsplaetze` ist dagegen die **gepflegte Stammdatentabelle** von Sage. Wer daraus
     anlegt, uebernimmt eine Liste, die jemand bewusst fuehrt. **Sage ist fuehrend** — fuer Nummer,
     Name und Kuerzel.
     **Bestehende Werkbaenke** werden ueber die Nummer wiedergefunden; aendern sich Name oder Kuerzel in
     Sage, zieht der Sync sie nach und **meldet** die Abweichung (ADR-0014-Muster).
   - **OFFEN — welche Sage-Zeilen werden zu WMS-Werkbaenken?** Die Tabelle enthaelt auch
     **Pseudo-Arbeitsplaetze** (Storno, Gestoppt, Reserve, Verkauf, Lager, Verladung, externe
     Beschichter, Reklamation). Sie als „Werkbank" anzulegen, machte `/ProductionWorkplaces`
     unuebersichtlich und boete sie am Terminal zur Auswahl an. Drei Wege:
     - **Alle anlegen, BDE-Sichtbarkeit ueber das bestehende `BdeAktiv`** (Standard `false`, der Mensch
       schaltet echte Werkbaenke frei). Nutzt ein vorhandenes Feld, aber die Pseudo-Eintraege stehen in
       der Werkbank-Liste.
     - **Nur Kuerzel anlegen, die in der Struktur vorkommen** (`FaHierarchyNode.Arbeitsschritte`).
       Datengetrieben — Storno und Gestoppt tauchen dort nie auf, es entstehen genau die benoetigten
       Werkbaenke. **Nachteil:** Eine Werkbank entsteht erst, wenn ein Auftrag sie braucht; ein Terminal
       laesst sich nicht vorab fuer sie einrichten.
     - **Ausschlussliste in den Standorteinstellungen** (Kuerzel wie `STO`, `XXX`, `x01`).
     *Einschaetzung:* der zweite Weg ist der sauberste, sofern die Terminal-Einrichtung warten kann.
     Zu entscheiden.
   - **Folge fuer die UI:** In `/ProductionWorkplaces` sind Nummer, Name und Kuerzel **read-only**, weil
     Sage-gefuehrt. Hinweis auf der Hilfeseite: Aenderungen gehoeren nach Sage, nicht ins WMS.
   - **Der Arbeitsbereich zielt NICHT auf `ProductionWorkplace`.** Damit entfaellt der
     Namenskonflikt dieser Spec vollstaendig. **Aber:** ADR 0014 tut genau das — siehe unten.

   **Neue Anforderung an das Terminal (Baustein c ist damit NICHT leer):**
   Anfangs wird **ein Terminal mehrere Werkbaenke** bedienen. Der Werker **waehlt die Werkbank** und sieht
   dann nur deren Auftraege. **Zu pruefen:** Kann das BDE-Terminal heute zwischen mehreren Werkbaenken
   waehlen, oder ist es fest an eine gebunden? Ist die Auswahl vorhanden, bleibt Empfehlung F
   unveraendert gueltig (gefiltert wird ohnehin nach `ProductionWorkplaceId`). Fehlt sie, ist die
   Werkbank-Auswahl am Terminal **echte Arbeit in Baustein (c)** — und die Zwei-Etappen-Einschaetzung
   (Antwort 8) ist zu ueberpruefen.

   ### KONSEQUENZ FUER ADR 0014 — ausserhalb dieser Spec, aber vor ihrer Umsetzung zu klaeren

   [[0014-werkbank-datenhoheit-sage-fuehrend-mit-abweichungsmeldung]] und die Materialisierung
   ([[2026-08-20-materialisierung-fachliche-felder-spec]], v1.37) leiten
   `ProductionOrder.ProductionWorkplaceId` — die **Werkbank** — aus dem **Arbeitsbereich** ab. Das beruht
   auf der Annahme vom August: *„Werkbank = Arbeitsbereich aus der FA-Struktur"*. **Diese Annahme ist
   jetzt widerlegt:** Der Arbeitsbereich ist ein **Zielort**, keine Werkbank.
   **Heute noch folgenlos** — bei IDEAL gibt es keine Werkbaenke, also findet die Ableitung nichts und
   schreibt nichts. **Sobald die Werkbaenke angelegt sind**, meldet sie jeden Arbeitsbereich als
   „unbekannte Werkbank" (die Namen passen nicht) — oder, falls jemand eine Werkbank `K-02` nennt,
   schreibt sie einen **Zielort in das Werkbank-Feld**.
   **Eigene Notiz:** [[2026-09-21-adr-0014-arbeitsbereich-ist-zielort]]. **Reihenfolge:** vor dem Anlegen
   der IDEAL-Werkbaenke klaeren. **Stand 2026-09-22: geklaert und umgesetzt** — siehe
   [[2026-09-21-adr-0014-arbeitsbereich-ist-zielort-spec]] (Testbereit im selben Worktree) und
   „Reihenfolge/Einordnung" oben.

   **Vorschlag (Bestaetigung ausstehend): Baustein (a) ebenfalls an den Master haengen.** Sein einziger
   Abnehmer ist Baustein (b), und der laeuft nur bei IDEAL. Ungegated laese (a) bei AKE die
   AKE-Arbeitsplaetze, faende keine passenden Werkbaenke und meldete alles als unbekannt — reines
   Rauschen. Doppel-Gate wie bei (b): Master **und** eigener Toggle.
   **Stand 2026-09-22: bestaetigt** — siehe „ANTWORTEN zu B5" (S1) und Baustein-a-Beschreibung im Rumpf.

5. → **BESTAETIGT — kein separater Katalog.** `WorkOperation` hat keinen Katalog-FK, der Name kommt
   aus `ProductionWorkplace.Name`. Das ist sauberer als v1.41 und beseitigt den Zwei-Lauf-Ablauf.
   **Dieselbe Materialisierungs-Eigenschaft wie bei Antwort 1:** Der Name wird beim Anlegen
   **kopiert**. Wird eine Werkbank spaeter umbenannt, behalten bestehende Arbeitsgaenge den alten
   Namen. Unkritisch, aber in den Rumpf.

6. → **NurFA bleibt beim generischen Default — das ist die Definition des Modus. Aber der
   Existenz-Check muss repariert werden.**
   „Nur FA" heisst: auf den Auftrag buchen, **ohne** Arbeitsgang zu waehlen. Dort echte Arbeitsgaenge
   anzubieten, widerspraeche dem Zweck. Normal-Modus zeigt die echten automatisch (die Spec hat
   verifiziert, dass der Default dort nicht aufgerufen wird). **Kein Koexistenz-Konflikt zwischen den
   Modi** — es sind zwei bewusste Wege.
   **Der eigentliche Fund steckt in einem Nebensatz der Spec:** `FindOrCreateDefaultAsync` prueft die
   Existenz ueber den **Namen**. Echte Arbeitsgaenge tragen `Name = ProductionWorkplace.Name`, der
   Default `Name = BdeDefaultArbeitsgang`. Stimmen beide ueberein — was jemand bei der Pflege leicht
   so setzt —, **findet der Default-Service den echten Arbeitsgang und bucht darauf**, statt den
   Default anzulegen. Eine NurFA-Buchung landete still auf einem echten Arbeitsgang.
   **Verbindlich:** Der Existenz-Check geht ueber `OperationNumber = "01"`, nicht ueber den Namen. Echte
   Arbeitsgaenge nutzen das Kuerzel als `OperationNumber` — die Nummer trennt sauber, der Name nicht.
   Eine Zeile Code, aber sie gehoert **mit einem Test** in den Umfang.

7. → **Naechste freie Nummer zum Umsetzungszeitpunkt.** Die Kommissionierziel-Spec ist freigegeben und
   laeuft voraussichtlich zuerst — dann ist `93` richtig. Laeuft diese Spec zuerst, nimmt sie `92`.
   **Gegen den tatsaechlichen Worktree-Stand pruefen, nicht die Nummer aus der Spec uebernehmen.**
   **Verifiziert 2026-09-22:** `SQL/92_AddUserDefaultFilterBomKommissionierziel.sql` liegt bereits im
   Worktree -> `93` ist Stand heute korrekt. Bleibt trotzdem ein Dev-Lauf-Check (siehe Offene
   Rueckfrage 7).

8. → **Epic mit ZWEI Etappen im Buendel-Worktree — nicht drei Specs.**
   **Der Vorteil getrennter Specs existiert hier nicht:** „Einzeln mergbar" setzt voraus, dass gemergt
   werden kann. Im Buendel-Modell merged **nichts**, bevor Schranke 2 fuer das ganze Buendel faellt.
   Schnelleres Teil-Feedback gibt es also nicht — nur mehr Koordination.
   **Zwei statt drei Etappen**, weil Baustein (c) durch Antwort 1 und 6 fast leer wird
   (Verifikations-AK plus der Existenz-Check-Fix):
   - **Etappe A — Baustein (a):** `ArbeitsschrittCode` + `SageArbeitsplatznummer` + Sage-Anlege-Sync +
     Migration + Standorteinstellungen. Risikoarm, in sich abgeschlossen. **STOPP + melden.**
   - **Etappe B — Bausteine (b) + (c):** Umbau auf `WorkOperation`, Umbenennung,
     Terminal-Verifikation, Existenz-Check-Fix aus Antwort 6.
   Etappe B ist die riskante (Umbenennung, Zieltabellen-Wechsel) — deshalb der Halt davor.

9. → **`varchar(31)` — am Sage-Schema gemessen (2026-09-22).**
   ```sql
   SELECT DATA_TYPE, CHARACTER_MAXIMUM_LENGTH FROM INFORMATION_SCHEMA.COLUMNS
   WHERE TABLE_NAME = 'KHKPpsArbeitsplaetze' AND COLUMN_NAME = 'Arbeitsplatznummer';
   -- Ergebnis: varchar, 31
   ```
   Bestaetigt die Vermutung aus den Daten: Die Zeile `0000` (Reserve) zeigt fuehrende Nullen — eine
   `int`-Spalte haette `0` angezeigt.
   **Verbindlich fuer `ProductionWorkplace.SageArbeitsplatznummer`:**
   - **Typ: `NVARCHAR(31)`** — gleiche Laenge wie die Quelle; `nvarchar` statt `varchar` nach Hauskonvention,
     verlustfrei, weil Obermenge.
   - **Niemals in eine Zahl umwandeln.** Kein `int.Parse`, keine numerische Sortierung, kein numerischer
     Vergleich. `0000` und `0` sind **verschiedene** Schluessel; numerisch ausgewertet kollidierte die
     Reserve-Zeile mit jeder anderen Nummer, die zu null wird. **Abgleich ausschliesslich als
     Zeichenkettenvergleich.**
   - **Beim Lesen trimmen**, wie die Kuerzel. `varchar` fuellt zwar nicht auf (anders als `char`), aber die
     Liste hat bereits gezeigt, dass bei der Erfassung Leerzeichen hineingeraten (`"EG "`). Ein Trim kostet
     nichts und verhindert, dass eine Werkbank bei jedem Lauf als „neu" erkannt und doppelt angelegt wird.
   - **Eindeutigkeit:** `Arbeitsplatznummer` ist in Sage der Schluessel des Arbeitsplatzes. Im WMS einen
     **eindeutigen Index** auf `SageArbeitsplatznummer` (gefiltert auf `IS NOT NULL`, weil AKE-Werkbaenke
     und manuell angelegte keine Nummer tragen). Ohne ihn koennte ein Fehler im Anlegepfad eine Werkbank
     **doppelt** anlegen, ohne dass es auffaellt — der Index macht daraus einen sichtbaren Fehler.
   **Damit sind alle Rueckfragen beantwortet.**

**Zur Abnahme von v1.41:** Die dortige Manual-Checkliste ist mit dieser Spec **gegenstandslos** —
v1.41 schreibt in die falsche Tabelle. **Nicht separat abnehmen**, sondern mit TS-77 dieser Spec.

## Kritische Pruefung (2026-09-22)

**Befunde am 2026-09-22 in den Rumpf eingearbeitet** (siehe Baustein a/b/c, Migrations-/SQL-
Auswirkungen, Akzeptanzkriterien, Test-Szenarien, Frontmatter `epic`/`etappen`/`affected_code`; die
drei fachlichen B5-Fragen sind unten im Abschnitt „ANTWORTEN zu B5" beantwortet und ebenfalls
eingearbeitet). Die urspruengliche Durchsicht bleibt darunter als Audit-Spur stehen — Zeilenangaben
beziehen sich auf den Stand **vor** dieser Ueberarbeitung.

> Anwalt-des-Teufels-Durchsicht **vor** dem Dev-Lauf. Die acht Freigabe-Antworten — vor allem der
> lange Antwort-4-Nachtrag mit seiner „AUFLOESUNG" — haben den Entwurf **erheblich umgebaut**, aber
> **nichts davon steht im Rumpf**. Der Rumpf (Fachliche Anforderungen, Baustein a, Migration,
> `affected_code`, AK, Testszenarien, Deploy) beschreibt durchgaengig noch das **alte** Design. Das
> ist der teure Riss, vor dem gewarnt wurde. `freigabe_entscheidung` ist ausserdem noch leer. Mehrere
> BLOCKER, ein paar SOLLTE, Hinweise. Empfehlung am Ende.

### BLOCKER — Rumpf widerspricht den getroffenen Entscheidungen

**B1 — „Anlegen" vs. „melden" (direkt die Frage des Menschen).** **Nein, es steht nicht in
`affected_code`/AK.** Rumpf: „Melde-statt-Anlege-Prinzip wie ADR 0014" (Z.95), „**keine** Werkbank
automatisch anlegen — exakt ADR 0014" (Z.198), AK 1-4 + TS-77.1-3 beschreiben Melden. **Entschieden**
(Antwort 4, Z.784-792): Baustein (a) **legt die Werkbaenke selbst an** (Nummer, Name, Kuerzel im
DB-Abgleich); „melden statt anlegen kehrt sich fuer DIESE Tabelle um" (gepflegte Stammdaten
`KHKPpsArbeitsplaetze`, anders als die Freitext-Arbeitsbereiche). → Fachliche Anforderung, Baustein-a-
Beschreibung (Z.177-216), AK 1-4, TS-77.1-3, Deploy-„DryRun beobachten"-Text und `affected_code`
muessen auf **Anlege-Semantik** umgeschrieben werden. Die ganze „exakt ADR 0014"-Begruendung ist jetzt
das Gegenteil.

**B2 — Match ueber `SageArbeitsplatznummer`, nicht ueber `Name`; zweite Migrationsspalte
(direkt die Frage des Menschen).** **Nein, der Rumpf matcht noch ueber `Name`.** Rumpf: Match
`Bezeichnung1 → ProductionWorkplace.Name` (Z.194/197), Migration mit **nur** `ArbeitsschrittCode`
(Z.314-315), AK 1 name-basiert. **Entschieden** (Antwort 4, Z.739-747, 780-782): Verknuepfung ueber
**`ProductionWorkplace.SageArbeitsplatznummer`** (aus `KHKPpsArbeitsplaetze.Arbeitsplatznummer`) —
Pflicht-**zweite Spalte in derselben Migration**; `Name = Bezeichnung1` nur zur **Anzeige**; der
Parameter „Match-Spalte" **entfaellt** (am Datensatz belegt: Namen weichen ab/haben Doppel-Leerzeichen,
66 Arbeitsplaetze, keine Spalte traegt `K-02`-Werte). → Migrations-Abschnitt (Z.312-328),
`affected_code` (nur eine Spalte gelistet), Baustein-a-Ablauf und AK 1 sind falsch. Zusaetzlich Pflicht
laut Antwort: **Kuerzel beim Lesen `TRIM`en** (`"EG "` mit Leerzeichen, Z.751) — sonst faellt `EG`
still in die Unbekannt-Meldung.

**B3 — Der Existenz-Check-Fix (Antwort 6) fehlt in `affected_code`/AK, obwohl er ein echter Bug ist.**
Entschieden (Antwort 6, Z.847-860): `BdeDefaultWorkOperationService.FindOrCreateDefaultAsync` prueft die
Existenz heute ueber den **Namen** — stimmt `BdeDefaultArbeitsgang` mit einem echten Werkbank-Namen
ueberein, bucht eine NurFA-Buchung **still auf einen echten Arbeitsgang**. Fix: Existenz-Check ueber
`OperationNumber = "01"`, **mit Test**. Der Rumpf fuehrt Baustein (c) noch als „voraussichtlich kein
Code-Eingriff" (Z.100-102, 294-296); dieser konkrete, entschiedene Code-Fix + Test gehoert in
`affected_code`, In-Scope und als AK.

**B4 — Etappen-Entscheidung nicht im Frontmatter.** Entschieden (Antwort 8): **Epic mit ZWEI Etappen**
(A = Baustein a; B = Bausteine b+c, mit STOPP vor B). Frontmatter: `epic: false`, `etappen: []`; der
Abschnitt „Reihenfolge/Einordnung" bietet noch **beide** Optionen als gleichwertig an (Z.459-477). →
`epic: true` + `etappen`-Tabelle setzen, Groessen-Abschnitt auf die getroffene 2-Etappen-Entscheidung
reduzieren.

**B5 — Noch GENUINE offene Entscheidungen, die den Umfang von Baustein (a)/(c) bestimmen — nicht nur
Rumpf-Nachzug.** Diese sind in Antwort 4 ausdruecklich als „zu entscheiden/zu pruefen" offen:
- **Welche Sage-Zeilen werden ueberhaupt WMS-Werkbaenke?** (Z.795-808) — Pseudo-Arbeitsplaetze
  (`STO`/`XXX`/`x01`, Verkauf, Lager, externe Beschichter …) stehen aktiv in der Liste. Drei Wege
  angeboten („alle + `BdeAktiv`", „nur in der Struktur vorkommende Kuerzel", „Ausschlussliste"),
  Entscheidung offen. **Das ist die Kernfrage der neuen Anlege-Semantik (B1)** — ohne sie kann
  Baustein (a) nicht gebaut werden.
- **Kann das BDE-Terminal heute zwischen mehreren Werkbaenken WAEHLEN?** (Z.814-820) — anfangs bedient
  ein Terminal mehrere Werkbaenke, der Werker waehlt. Fehlt die Auswahl, ist Baustein (c) **echte
  Arbeit**, und die „Baustein c fast leer"-Annahme (Z.100-102) samt Zwei-Etappen-Schnitt kippt. **Am
  Code zu klaeren, bevor der Umfang steht.**
- **`STO`/`XXX`-Kuerzel vom Routing ausschliessen?** (Z.755-759) und **`EG` auf einem „beendeten"
  Arbeitsplatz** (Z.760-762) — offen.
→ Diese drei muss der Mensch entscheiden bzw. am Code klaeren lassen, **bevor** Baustein (a)/(c)
umgesetzt werden — sie aendern, was gebaut wird, nicht nur wie der Rumpf klingt.

### SOLLTE

**S1 — Baustein (a) ans Master-Gate haengen (Antwort 4, Z.836-839, „Bestaetigung ausstehend").**
Sonst laeuft der Sync bei AKE ins Leere und meldet alle AKE-Arbeitsplaetze als unbekannt (Rauschen).
Doppel-Gate wie Baustein (b). Bestaetigen und in Baustein-a-Text + AK aufnehmen; im Rumpf steht noch
„kann an beliebiger Stelle stehen" (Z.207).

**S2 — Kuerzel-Mehrdeutigkeit in Baustein (b) als AK.** Antwort 4 (Z.599-609): tragen zwei aktive
Arbeitsplaetze dasselbe Kuerzel, legt Baustein (b) **keine** `WorkOperation` an, sondern meldet
„mehrdeutig". Heute regelt die Spec Mehrdeutigkeit nur fuer **Namen** in Baustein (a). Als AK + Test
ergaenzen (auch wenn aktuell keine Dubletten existieren — Absicherung).

**S3 — Selbst-markierte „in den Rumpf"-Eigenschaften einarbeiten.** Antwort 1 (Routing wird bei der
Anlage **materialisiert**; spaetere Sage-Umzuege ziehen offene Arbeitsgaenge **nicht** nach — nur
Meldung) und Antwort 5 (Werkbank-**Name** wird beim Anlegen **kopiert**, Umbenennung schlaegt nicht
durch) sind bekannte Eigenschaften, die beide Antworten ausdruecklich „in den Rumpf" verlangen.

**S4 — Doku-Korrektur + veraltete Token-Tabelle.** Antwort 4 (Z.763-765): `VM` ist „Vormontage W1"
(3002), nicht „Elektrofertigung W1 (OG)" (= `VM9`, 3001). Die Token→Werkbank-Tabelle im Rumpf
(Z.142-155) traegt den falschen `VM`-Namen und ist gegen die 66-Zeilen-Abfrage zu verifizieren.

### HINWEIS

**H1 — `freigabe_entscheidung` ist leer**, obwohl die acht Antworten ausgefuellt sind. Nach der
Rumpf-Nachbesserung gehoert die Kernentscheidung dort hinein (Anlegen aus `KHKPpsArbeitsplaetze` ueber
`SageArbeitsplatznummer`, Epic/2 Etappen, Empfehlung F) — Setzen bleibt die Geste des Menschen.

**H2 — Baustein (a) hat jetzt eine deutlich groessere Wirkflaeche.** Aus „ein Kuerzel-Feld befuellen"
ist „eine Sage-gefuehrte Stammdatentabelle mit **Anlage** neuer Zeilen" geworden. Das beruehrt
`/ProductionWorkplaces` (Nummer/Name/Kuerzel read-only), das `BdeAktiv`-Feld als Sichtbarkeits-Gate und
die Frage, was mit bestehenden, von Hand angelegten Werkbaenken passiert. Im neu zu schreibenden
Baustein-a-Text sauber fassen.

**H3 — Praktische Empfehlung: Baustein (a) neu drafteln statt flicken.** Die Divergenz ist so gross
(Anlege- statt Melde-Semantik, zweite Schluesselspalte, Master-Gate, offene Zeilen-Auswahl), dass ein
gezielter Neu-Entwurf von Fachlicher Anforderung, Baustein (a), Migration, betroffenen AK und
Frontmatter sauberer ist als stueckweises Nachziehen — nachdem B5 entschieden ist.

### Empfehlung

**NACHBESSERUNG NOETIG:** Der Rumpf ist gegenueber den getroffenen Entscheidungen grundlegend veraltet
(B1 Anlegen, B2 `SageArbeitsplatznummer`+2. Spalte+Trim, B3 Existenz-Check-Fix, B4 Epic/2 Etappen) und
**drei fachliche Entscheidungen sind noch offen** (B5: Zeilen-Auswahl/Pseudo-Arbeitsplaetze,
Terminal-Werkbank-Auswahl, Storno-Kuerzel), die den Umfang bestimmen. Erst B5 entscheiden, dann Baustein
(a) + Migration + AK neu fassen (H3), dann freigeben.

## ANTWORTEN zu B5 (2026-09-22) — eine am Code, eine daraus abgeleitet, eine offen

### B5(b) — Terminal-Werkbank-Auswahl: EXISTIERT BEREITS. Am Code belegt.

`BdeTerminalController.Index(int? workplaceId)`:
```csharp
var activeWorkplaceId = workplaceId ?? terminal.DefaultProductionWorkplaceId;
ViewBag.AllWorkplaces = await _workplaces.GetBdeActiveAsync();
ViewBag.DefaultWorkplaceId = terminal.DefaultProductionWorkplaceId;
```
Das Terminal hat eine **Default-Werkbank** (`BdeTerminal.DefaultProductionWorkplaceId`), die per
Parameter **uebersteuert** wird; die Auswahl bietet **alle `BdeAktiv`-Werkbaenke** an. Genau das
verlangte Verhalten — „ein Terminal, mehrere Werkbaenke zur Auswahl, gefiltert nach der gewaehlten".
**Folgen:**
- **Baustein (c) bleibt klein** — Verifikation plus Existenz-Check-Fix. Die Zwei-Etappen-Einschaetzung
  (Antwort 8) **haelt**.
- **Empfehlung F bestaetigt sich zusaetzlich:** Das Terminal reicht die gewaehlte `workplaceId` durch,
  und das Routing filtert `WorkOperation` danach.
- **Moegliche spaetere Verfeinerung, bewusst NICHT im Umfang:** Die Auswahl zeigt **alle**
  `BdeAktiv`-Werkbaenke, keine terminal-spezifische Teilmenge. Bei rund 40 IDEAL-Werkbaenken wird die
  Liste lang. Eine Zuordnung „Terminal → seine Werkbaenke" waere ein eigener Punkt — erst, wenn die
  lange Liste im Betrieb stoert.

### B5(a) — Welche Sage-Zeilen werden Werkbaenke: `BdeAktiv` ist der vorhandene Hebel.

B5(b) zeigt: **`BdeAktiv` steuert bereits, was am Terminal erscheint.** Damit trennen sich zwei Fragen,
die bisher vermischt waren:
- **Was ist ueberhaupt kein Ort?** Statusmarker wie **Storno** (`STO`), **Gestoppt** (`XXX`), **Reserve**
  (`x01`). Sie werden **gar nicht** angelegt — **Ausschlussliste in den Standorteinstellungen**, neben
  dem Mandanten.
- **Was ist ein Ort, aber ohne Terminal?** Verkauf, Lager, Verladung, externe Beschichter usw. Sie werden
  angelegt, erscheinen am Terminal aber nur, wenn jemand `BdeAktiv` setzt.

**Vorschlag (vom Menschen zu bestaetigen):**
- **Ausschlussliste**, Standardwert `STO, XXX, x01` — nur echte Statusmarker, erweiterbar.
- **Neu angelegte Werkbaenke starten mit `BdeAktiv = false`.** Der Mensch schaltet die echten
  Arbeitsplaetze bewusst frei. Nach dem ersten Sync erscheint also **keine** Werkbank am Terminal, bis
  sie freigegeben ist — das ist gewollt: **Terminal-Einrichtung ist eine bewusste Handlung**, keine
  Nebenwirkung eines Syncs.
- **Warum nicht nur Struktur-Kuerzel anlegen** (der fruehere Vorschlag): Der Mensch hat bestaetigt, dass
  Terminals **vorab** an den Arbeitsplaetzen eingerichtet werden. Werkbaenke, die erst entstehen, wenn
  ein Auftrag sie braucht, liessen sich nicht vorab einrichten. Der Vorschlag ist damit ueberholt.

### B5(c) — Storno-Kuerzel vom Routing ausschliessen: ERLEDIGT durch B5(a).

Stehen `STO`/`XXX`/`x01` auf der Ausschlussliste, **existiert fuer sie keine Werkbank**. Ein
Struktur-Token `STO` findet dann keinen `ArbeitsschrittCode`, faellt in die Unbekannt-Meldung und erzeugt
**keinen** Arbeitsgang. Kein eigener Ausschluss im Routing noetig — derselbe Mechanismus deckt beides.

### Stand nach B5

**ENTSCHIEDEN (2026-09-22):**
- **Ausschlussliste**, Standardwert **`STO, XXX, x01, BS, BS2, FRE, AKE`** — Statusmarker und alles, was
  **ausser Haus** stattfindet:
  - `STO` Storno, `XXX` Gestoppt, `x01` Reserve — Statusmarker, kein Ort.
  - `BS`, `BS2` externe Beschichter — laut Mensch *„wie der Name sagt extern"*. Externe Beschichtung
    laeuft ueber den **Beschichtungsauftrag** (Teil 4), nicht ueber eine BDE-Buchung im Haus.
  - `FRE` Fremdleister (allgemein) — extern, gleiche Begruendung (bestaetigt 2026-09-22).
  - `AKE` Bestellung AKE — Bestellung beim Schwesterstandort, kein Arbeitsplatz im Haus (bestaetigt
    2026-09-22).
  Die Liste ist in den Standorteinstellungen **erweiterbar**; der Standardwert ist die heutige Einschaetzung.
- **Neu angelegte Werkbaenke starten mit `BdeAktiv = false`** (Vorschlag unwidersprochen).

**B5 ist damit GESCHLOSSEN.**

### WICHTIG: Ausgeschlossene Kuerzel sind BEKANNT, nicht unbekannt

Das ist die Folge, die die Ausschlussliste erst brauchbar macht. Kommt ein ausgeschlossenes Kuerzel in
`FaHierarchyNode.Arbeitsschritte` vor — etwa `BS`, weil das Teil zum Beschichter geht —, findet Baustein
(b) dafuer keine Werkbank. **Ohne Sonderbehandlung faellt es dann in die Unbekannt-Meldung — bei jedem
Lauf, fuer jeden Auftrag mit Beschichtung.** Dauerrauschen, das genau die Meldungen verdeckt, fuer die der
Mechanismus da ist.

**Verbindlich:** Ein Kuerzel auf der Ausschlussliste ist **bewusst ausgenommen**. Es erzeugt
- **keine** Werkbank,
- **keinen** Arbeitsgang,
- **keine** Unbekannt-Meldung.
Die Unbekannt-Meldung bleibt den Kuerzeln vorbehalten, die **weder** eine Werkbank haben **noch** auf der
Liste stehen — also den echten Luecken (`MO`, `ZS`, `LÖ`, `PR`, `BE`). Als eigenes AK.

**Danach ist B5 geschlossen**, und Baustein (a) kann nach H3 **neu entworfen** werden — mit
Anlege-Semantik, `SageArbeitsplatznummer` als Schluessel, Trim, Mandant- und Ausschluss-Parameter,
Master-Gate, und der Bekannt-statt-unbekannt-Regel fuer ausgeschlossene Kuerzel.

**Stand 2026-09-22: erledigt.** Baustein (a)/(b)/(c), Migration, `affected_code`, AK, Testszenarien,
Frontmatter (`epic`/`etappen`) und Deploy im Rumpf sind auf Basis dieser Antworten neu gefasst (siehe
Hinweis am Kopf dieses Abschnitts). Verbleibend: Offene Rueckfrage 9 (Sage-Spaltentyp
`Arbeitsplatznummer`, am WMS-Code nicht verifizierbar) und die Dev-Lauf-Checks aus Offener Rueckfrage 7
(Migrationsnummer) und der Versionsnummer (Brain-Pflichten).

## Kritische Pruefung (2026-09-22, 2. Durchgang)

> Zweiter Durchgang **nach** der Ueberarbeitung (`4c74cf8`): geprueft, ob das Nachziehen der
> Entscheidungen in den Rumpf **vollstaendig und widerspruchsfrei** ist — nicht die Entscheidungen neu
> aufgerollt. Vier der fuenf gezielten Fragen sind **sauber** nachgezogen; **eine** hat einen echten,
> teuren Riss (B6).

### Sauber nachgezogen (bestaetigt)

- **„Melden statt anlegen" fuer Baustein (a):** im Rumpf **nirgends** mehr. Der einzige
  „Melde-statt-Anlege"-Treffer im Rumpf (Z.327) gehoert zu **Baustein (b)** (unbekannte Token melden) —
  dort korrekt. Baustein (a) sagt durchgaengig „neue Zeile anlegen" (Z.290). Die alten Melden-
  Formulierungen leben nur noch im Audit-Block „Freigabe-Antworten" (dort als „kehrt sich um" aufgeloest)
  und in der 1.-Durchgang-Pruefung (Zitat des Alt-Rumpfs) — beides gewollt.
- **Parameter „Match-Spalte":** im Rumpf **nicht** mehr vorhanden — nur im Audit-Block (Z.881-949, wo er
  als „entfaellt" beschlossen wird) und im 1.-Durchgang-Zitat. Sauber gestrichen.
- **„Ausgeschlossene Kuerzel sind bekannt, keine Unbekannt-Meldung":** als **AK 4** (Baustein a) und
  **AK 10** (Baustein b) formuliert und mit **TS-77.3** + **TS-77.9** getestet. Vollstaendig.
- **Etappengrenze/Abhaengigkeit:** Etappe A = Baustein (a), Etappe B = (b)+(c) (Frontmatter + Tabelle
  Z.656-659). Die Abhaengigkeit „b braucht a's Katalog" ist in „Reihenfolge/Einordnung" (Z.647-648)
  benannt; im Buendel-Modell (ein Merge) existieren a's Werkbaenke, wenn b gebaut/getestet wird. Passt.

### BLOCKER

**B6 — Der eindeutige (gefilterte) Index auf `SageArbeitsplatznummer` fehlt im Rumpf und widerspricht
der getroffenen Entscheidung; das laesst genau die Doppelanlage offen, nach der gefragt wurde.**
Der Antwortblock entscheidet ausdruecklich (Z.1104-1107): *„Im WMS einen **eindeutigen Index** auf
`SageArbeitsplatznummer` (gefiltert auf `IS NOT NULL` …). Ohne ihn koennte ein Fehler im Anlegepfad
eine Werkbank **doppelt** anlegen, ohne dass es auffaellt."* Der ueberarbeitete Rumpf sagt das
**Gegenteil**: „**Kein Unique-Index**" (Baustein a, Z.256-258) bzw. „**Kein** Unique-Constraint"
(Migration, Z.454), mit dem Hausmuster-Argument „Ambiguitaet wird gemeldet, nicht per DB-Constraint
verhindert".
**Das Hausmuster ist hier fehl am Platz:** Es gilt fuer **beschreibende** Felder (Namens-Mehrdeutigkeit),
nicht fuer den **Identitaets-/Upsert-Schluessel**, gegen den der Sync matcht. Die Laufzeit-Regel
„mehr als ein Treffer → mehrdeutig melden" (Z.300-302) **erkennt** die Dublette erst **nachdem** sie
entstanden ist — und blockiert die Nummer dann **dauerhaft** (mehrdeutig → nichts schreiben, also auch
keine Korrektur mehr). Der Anlegepfad ist „kein Treffer → INSERT": faellt der Lookup einmal nicht
(Leerzeichen, Gross/Klein, String-vs-Zahl), entsteht die zweite Zeile **still**. Der Unique-Index ist
der einzige Mechanismus, der das **verhindert** statt es nur zu melden.
→ **Nachziehen (Rumpf-Korrektur, keine neue Entscheidung — die Entscheidung steht bereits im
Antwortblock):**
1. Baustein a (Z.256-259) und Migration (Z.450-455): „Kein Unique-Index" ersetzen durch den
   **eindeutigen, auf `IS NOT NULL` gefilterten** Index (`CREATE UNIQUE INDEX … WHERE
   SageArbeitsplatznummer IS NOT NULL`). Das Hausmuster-„melden statt Constraint" gilt weiterhin fuer
   **Namens**-Mehrdeutigkeit, nicht fuer den Nummern-Schluessel — den Unterschied benennen.
2. In `affected_code` und dem `SQL/93_*.sql`-Eintrag den Unique-Filter-Index **auffuehren** (heute fehlt
   er dort komplett) + ein **AK/Testszenario** „doppelte Anlage wird vom Index verhindert".
3. Die uebrigen, im selben Antwortabschnitt (Z.1093-1103) getroffenen Schluessel-Entscheidungen
   **ebenfalls in den Rumpf**, weil sie zusammen die Doppelanlage verhindern: `SageArbeitsplatznummer`
   ist **`nvarchar`, Vergleich ausschliesslich als Zeichenkette** (nie `int.Parse` — `0000` ≠ `0`) und
   **beim Lesen getrimmt** (wie das Kuerzel). Der Rumpf trimmt heute nur `ArbeitsschrittCode` (Z.261/291)
   und laesst den Nummern-Typ als „offen" (Rueckfrage 9) — die WMS-seitige Behandlung ist aber
   entschieden.

### SOLLTE

**S5 — Offene Rueckfrage 9 ist ueberzeichnet.** Frontmatter/`open_questions` fuehren den Sage-Spaltentyp
als offen; entschieden ist die WMS-Seite (`nvarchar`-Obermenge, String-Vergleich, Trim, Unique-Index).
Rueckfrage 9 auf das reduzieren, was wirklich offen ist: **bestaetigen, dass `nvarchar(n)` eine
verlustfreie Obermenge des tatsaechlichen Sage-Typs ist** (Laenge n gegen `INFORMATION_SCHEMA` prüfen) —
nicht mehr „Typ offen".

### Empfehlung

**NACHBESSERUNG NOETIG — ein fokussierter Punkt (B6):** Der Unique-Filter-Index auf
`SageArbeitsplatznummer` samt String-/Trim-Behandlung ist bereits **entschieden** (Antwortblock), aber
beim Nachziehen ins Gegenteil verkehrt worden (Rumpf: „Kein Unique-Index") und fehlt in Migration +
`affected_code`. Das ist der eine echte Riss; die uebrigen vier gezielten Fragen sind sauber
nachgezogen. Nach dieser Korrektur ist der Rumpf konsistent zu den Entscheidungen.
