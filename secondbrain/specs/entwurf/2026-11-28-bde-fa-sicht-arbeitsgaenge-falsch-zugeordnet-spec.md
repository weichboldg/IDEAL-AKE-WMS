---
type: spec
title: BDE FA-Sicht - Arbeitsgänge passen nicht zur Strukturzeile (Herkunft kennzeichnen, KA-Altbestand bereinigen)
slug: 2026-11-28-bde-fa-sicht-arbeitsgaenge-falsch-zugeordnet-spec
status: Entwurf
created: 2026-11-28
updated: 2026-11-28
source_backlog: "[[2026-11-28-bde-fa-sicht-arbeitsgaenge-falsch-zugeordnet]]"
bug: "[[2026-11-28-bde-fa-sicht-arbeitsgaenge-falsch-zugeordnet-bug]]"
task: ""
jira: WMS-10
worktree: ""
branch: ""
standort: IDEAL
affected_code:
  - "[[2026-11-28-bde-fa-sicht-arbeitsgaenge-falsch-zugeordnet-bug]]"
  - IdealAkeWms/Services/BdeFaAngebot.cs (FaSichtAsync :107-147, Spalten :158-182)
  - IdealAkeWms/Models/ViewModels/FaHierarchyTreeViewModel.cs (FaArbeitsschrittSymbol :228-256)
  - IdealAkeWms/Views/Shared/_FaAgSymbole.cshtml
  - IdealAkeWms/Views/BdeTeileliste/Fa.cshtml (:35 Spaltentitel AG-Status)
  - scripts/deploy/nachlauf.psd1 (neuer Eintrag #2 zu :338-340)
  - IdealAkeWms.Tests (FA-Sicht-Test)
  - docs/anwender/17-bde-teileliste-werkbank.md, docs/TESTSZENARIEN.md (TS-197 + neues TS)
open_questions:
  - "1. (Fachbereich: Benjamin Andl-Braun) AG-Status der FA-Sicht: Variante A (alle Arbeitsgänge der Sub-FA bleiben, die von Blechteilen der Baugruppe stammenden sind sichtbar als „an Teilen“ gekennzeichnet), B (nur die Arbeitsgänge aus den Arbeitsschritten der Zeile plus der Arbeitsgang der Zeile) oder C (keine Änderung der Anzeige, nur Datenbereinigung und Erklärung in der Hilfe)? Empfehlung A."
  - "2. (Fachbereich: Benjamin Andl-Braun) Falls an KA-Arbeitsgängen von Baugruppen, deren KA nur an Blechteilen steht, schon gebucht wurde: bleiben diese Arbeitsgänge stehen und werden zu Ende gebucht, oder storniert der BDE-Admin die Buchung, damit die Bereinigung sie entfernen kann?"
  - "3. Auf welcher IDEAL-Datenbank entstand der Screenshot (Test oder Produktion)? Dort laufen die Prüfabfragen P-1 bis P-5."
  - "4. Datenbereinigung: neuer Manifest-Eintrag mit neuer Id (…#2) im nächsten Paket (Empfehlung) oder sofort manueller Lauf des Einmal-Skripts auf der betroffenen DB, unabhängig vom Release?"
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

> [!info] Bug-Record
> [[2026-11-28-bde-fa-sicht-arbeitsgaenge-falsch-zugeordnet-bug]]. Quelle: Mail „WMS Anpassungen“ Punkt 1 (Benjamin Andl-Braun), Jira WMS-10,
> Screenshot `anhaenge/2026-11-28-mail-wms-anpassungen-1.png` (lesbar; Werte unten aus dem Bild abgelesen).

## Ziel / Nutzen (das Warum)
Am KA-Terminal (IDEAL) sieht der Werker in der FA-Sicht Arbeitsgänge, die nicht in den Arbeitsschritten des angezeigten Teils stehen. Er hält
das für einen Zuordnungsfehler und hat recht, dass die Zeile in sich nicht stimmig ist. Ziel: (1) Altbestand entfernen, der nach der
Teileliste-Regel (V1, ADR 0030) gar nicht mehr existieren dürfte. (2) Die FA-Sicht zeigt so an, woher ein Arbeitsgang der Sub-FA kommt, dass
Zeile und Arbeitsschritte zusammenpassen.

## Umfang (In-Scope / Out-of-Scope)
**In-Scope**
- Prüfabfragen P-1 bis P-5 (vor dem Code, Ergebnis in diese Spec).
- Datenbereinigung KA-Altbestand über das vorhandene Einmal-Skript v1.99.32, neu angeboten unter neuer Manifest-Id.
- Anzeige AG-Status in der FA-Sicht nach Freigabe-Variante (Empfehlung A).
- Hilfe/Anwenderdoku Kapitel 17, TS-197 ergänzen, neues TS.

**Out-of-Scope**
- Regel der Arbeitsgang-Erkennung (DirectChildren-Scope, V1): unverändert, bewusst so entschieden (fallstricke „Dieselbe Arbeitsschritte-Zeile
  zählt in ZWEI Scopes“, `fallstricke.md:1166-1179`; Vorgänger-Spec [[2026-11-13-bde-vereinfacht-teileliste-blechfertigung-spec]] Antwort 1 = V1).
- Teile Einzeln/Summiert, FA-Struktur: unverändert (dort stimmt es, siehe Ursache).
- AKE: FA-Sicht dort ohne Strukturzeile (`Knoten` null, `BdeFaAngebot.cs:12-14`), keine Änderung.
- Werkbank-Gate für den AG-Status der FA-Sicht (bewusste Vereinfachung der E4-Spec, [[2026-11-25-bde-masken-vereinheitlichung-spec]] Z. 892).

## Fachliche Anforderungen

### Ursache (Befund 2026-11-28, Code main = Bündel v1.99.89)

**Teil 1 – belegt aus dem Code: AG-Status und Arbeitsschritte haben verschiedene Quellen.**
- AG-Status der FA-Sicht = **alle** `WorkOperations` des Auftrags (Sub-FA) der Zeile: `BdeFaAngebot.cs:107-108` (laden je `ProductionOrderId`),
  `:124` (Lookup je Auftrag), `:130-140` (ein Symbol je WorkOperation, Zeilen-AG markiert). So entschieden in E4-2 / Antwort 4 der
  Masken-Spec („alle AG der Sub-FA, eigener markiert“).
- Spalte Arbeitsschritte (auch Matchcode, Material, Position …) = Feld des **Kopfknotens** der Sub-FA: `BdeFaAngebot.cs:117-119` (Knoten mit
  `SubFA` = Sub-FA, `SubFA != 0`) und `:174`.
- Woher die WorkOperations einer IDEAL-Sub-FA kommen: Es gibt genau zwei Schreibwege. (a) Die Erkennung legt je Token des
  **DirectChildren-Scopes** = eigene Kopfzeile **plus direkte Blatt-Kinder** (`SubFA = 0`, `VaterFA = Sub-FA`) einen Arbeitsgang an der Sub-FA an
  (`IDEALAKEWMSService/Services/WorkOperationStructureDetectionService.cs:108-147`, Scope `:113-114`, Anlage `:179-192`). (b) Der
  NurFA-Default „01“ (`IdealAkeWms/Services/BdeDefaultWorkOperationService.cs:27-47`). Weitere Schreibwege gibt es nicht: Suche nach
  `WorkOperations.Add`, `new WorkOperation`, `INSERT`/`MERGE` auf `WorkOperations` in Web, Dienst und `SQL/AgentJobs` liefert nur diese zwei
  (OSEON schreibt `OseonWorkOperations`, `OseonSyncService.cs:568`).
- Folge: ST und EG an 1069721 stammen von Blechteilen der Baugruppe „Verdampfer CUCU kompl.“. Sie sind **richtig dem Auftrag**
  zugeordnet, stehen aber nicht in den Arbeitsschritten der Kopfzeile. Die Zeile zeigt sie ohne diesen Zusammenhang.
- Warum Einzeln/Summiert passen: Dort bekommt jede Zeile nur die Token ihres **eigenen** Feldes (`FaHierarchyTreeViewModel.cs:285-296`,
  aufgerufen in `FaAgStatusService.cs:37`). Ein Blechteil-Token wird dem Arbeitsgang der Vater-Sub-FA zugeordnet (`FaArbeitsgangStatus.cs:66-72`),
  ohne in der Baugruppen-Zeile aufzutauchen.
- Die Hypothese 1 der Backlog-Notiz („Strukturzeile ist ein Unterteil statt des Kopfteils“) ist durch den Code widerlegt. Die Gruppe enthält
  nur Knoten mit `SubFA` = Sub-FA, Blechteile haben `SubFA = 0` (`BdeFaAngebot.cs:116-118`, gleiche Regel `BdeTeilelisteService.cs:153-154`).
  „Erster nach Position“ wirkt nur, wenn derselbe Sub-FA mehrfach als Kopfknoten vorkommt (P-5). Das Screenshot passt dazu: Bezeichnung und
  Matchcode der Zeile 1069721 gehören zur Baugruppe.

**Teil 2 – vermutet, nur mit Daten klärbar: KA an diesen Sub-FAs dürfte es gar nicht mehr geben.**
- Die Teile-Reiter sind am KA-Terminal sichtbar (laut Meldung). Das setzt `TeilelisteWirksam` voraus = `BdeAktiv && BdeTeileliste && Kürzel`
  (`WerkbankKuerzelFamilie.cs:52-53,60`).
- Für Kürzel solcher Werkbänke legt die Erkennung seit v1.99.32 **keinen** Arbeitsgang aus Blatt-Token an der Vater-Sub-FA an
  (`WorkOperationStructureDetectionService.cs:96-98,125-129`). Bei 1069721 steht KA nicht in der Kopfzeile (Arbeitsschritte „VM“), also kann
  ein offener KA-Arbeitsgang nur Altbestand sein (vor v1.99.32 oder vor dem Setzen der Option angelegt). Die Erkennung löscht nie
  (Nur-hinzufügen, `:162-176`).
- Den Altbestand entfernt `SQL/Einmalig/2026-11-17_v1.99.32_RemoveParentWorkOperationsFromLeafTokensTeileliste.sql` (Kandidat `:62-73`, löscht
  nur ungebuchte `:94-102`). P-TL3 auf IDEAL TEST am 2026-11-17: KA 120, ST 120 Kandidaten (`:30`).
- Wahrscheinlicher Grund, warum er noch da ist: Lief der Trockenlauf, **bevor** die Option an der KA-Werkbank gesetzt war, war TREFFER 0. Dann
  vermerkt das Update-Skript „Entfaellt“ (`scripts/deploy/Update-IdealAkeWms.ps1:572-573,824-825`). Ein Eintrag mit Marker wird nie wieder
  angeboten (`nachlauf.psd1:19-20`). Der Info-Text `nachlauf.psd1:590` verlangt in diesem Fall ein manuelles Neu-Anstoßen.
- Folge am KA-Terminal: Die FA-Sicht bietet für 1069721, 1069724 usw. eine KA-Zeile an (die Zeile existiert nur wegen dieses Arbeitsgangs,
  `BdeFaAngebot.cs:62,83-84`). Gleichzeitig meldet derselbe Werker die KA-Blechteile in Teile Einzeln/Summiert. Das sind zwei Meldewege für
  dieselbe Arbeit, und genau das sollte V1 verhindern.

### Prüfabfragen (IDEAL-WMS-DB, Umgebung laut Rückfrage 3; nur lesend)
Alle Aussagen von Teil 2 bleiben **ungeprüft**, bis diese Abfragen gelaufen sind. Ergebnis in den Abschnitt „Befund Prüfabfragen“ unten.

```sql
-- P-1 Struktur: Kopfknoten und direkte Kinder der beiden Sub-FAs
SELECT HauptFA, VaterFA, SubFA, Position, Artnr, Matchcode, Arbeitsschritte, VerschwundenSeit
FROM dbo.FaHierarchyNodes
WHERE HauptFA = 1069617 AND (SubFA IN (1069721, 1069724) OR VaterFA IN (1069721, 1069724))
ORDER BY COALESCE(VaterFA, SubFA), SubFA DESC, Position;
-- Erwartung: je Sub-FA ein Kopfknoten (Arbeitsschritte VM bzw. M31), Blatt-Kinder (SubFA 0) mit ST/EG/KA.

-- P-2 Arbeitsgaenge der beiden Sub-FAs mit Herkunft und Buchungen
SELECT po.OrderNumber, po.SubOrderNumber, wo.Id, wo.OperationNumber, wo.Name, wo.Sequence, wo.ProductionWorkplaceId,
       wo.CreatedBy, wo.CreatedAt, wo.IsReported,
       (SELECT COUNT(*) FROM dbo.BdeBookings b WHERE b.WorkOperationId = wo.Id) AS Buchungen
FROM dbo.WorkOperations wo
JOIN dbo.ProductionOrders po ON po.Id = wo.ProductionOrderId
WHERE po.OrderNumber = N'1069617' AND po.SubOrderNumber IN (N'1069721', N'1069724')
ORDER BY po.SubOrderNumber, wo.Sequence;
-- Erwartung: CreatedBy = WorkOperationStructureDetection; KA mit CreatedAt vor v1.99.32-Deploy bzw. vor Setzen der Option.

-- P-3 Werkbaenke der betroffenen Kuerzel
SELECT Id, Name, ArbeitsschrittCode, BdeAktiv, BdeTeileliste, SageMissingSince
FROM dbo.ProductionWorkplaces
WHERE LTRIM(RTRIM(ArbeitsschrittCode)) IN (N'VM', N'M31', N'ST', N'EG', N'KA', N'IS', N'PU', N'SM', N'SL');
-- Erwartung: KA mit BdeAktiv = 1 und BdeTeileliste = 1; ST/EG ohne Teileliste (dann bleiben sie an der Sub-FA, Teil 1).

-- P-4 Lief das Einmal-Skript, und wie?
SELECT ManifestId, Mode, Hits, PackageVersion, ExecutedAt, ExecutedBy, Note
FROM dbo.DeployScriptRuns
WHERE ManifestId LIKE N'%RemoveParentWorkOperationsFromLeafTokensTeileliste%'
ORDER BY ExecutedAt;
-- Vermutung: Mode = Entfaellt, Hits = 0 (oder keine Zeile).

-- P-5 Kopfknoten mehrfach (nur dann waere "erster nach Position" relevant)
SELECT HauptFA, SubFA, COUNT(*) AS Anzahl
FROM dbo.FaHierarchyNodes
WHERE SubFA <> 0 AND VerschwundenSeit IS NULL
GROUP BY HauptFA, SubFA HAVING COUNT(*) > 1;
```
Ergänzend den **Trockenlauf** des Einmal-Skripts unverändert auf derselben DB ausführen (Default `@Trockenlauf = 1`, ändert nichts). Er liefert die
Zählung je Kürzel, die Meldeliste der gebuchten Kandidaten und `TREFFER: n`.

**Entscheidungsweg nach den Abfragen:**
- P-3 KA mit Teileliste, P-2 KA von der Erkennung angelegt, P-4 „Entfaellt“ oder leer → Teil 2 bestätigt, Bereinigung wie unten.
- P-3 KA **ohne** Teileliste → Teil 2 entfällt. KA an der Sub-FA ist dann gewollt (Teil 1). Neue Rückfrage an Gerald, warum die Teile-Reiter
  trotzdem sichtbar waren.
- P-4 „Scharf“ **nach** dem Setzen der Option und P-2 zeigt KA trotzdem mit späterem `CreatedAt` → Erkennung legt weiter an. Als offene Frage
  festhalten und die Bereinigung zurückstellen; der Code hier bleibt.

### Anforderungen
- **FA-1 Datenbereinigung (alle Varianten):** Das Einmal-Skript v1.99.32 läuft auf IDEAL erneut als Trockenlauf → SCHARF → zweiter Trockenlauf,
  über einen neuen Manifest-Eintrag mit neuer Id (Weg nach `nachlauf.psd1:20`; Rückfrage 4 kann stattdessen einen manuellen Lauf festlegen).
  Gebuchte Kandidaten bleiben stehen und kommen auf die Meldeliste (Skript `:81-84`). Ihr Umgang richtet sich nach Rückfrage 2.
- **FA-2 Anzeige (nach Rückfrage 1):**
  - *Variante A (Empfehlung):* Alle Arbeitsgänge der Sub-FA bleiben im AG-Status. Ein Symbol, dessen Kürzel **nicht** in den Arbeitsschritten
    der Strukturzeile steht, wird als „an Teilen“ gekennzeichnet: sichtbarer Text bzw. Zeichen, nicht nur Farbe (G2 der Masken-Spec), und im
    `aria-label`/`title` „ – an Teilen der Baugruppe“. Der Zeilen-AG bleibt zusätzlich markiert. Ohne Strukturzeile (AKE, Haupt-FA-Ebene) gibt
    es keine Kennzeichnung.
  - *Variante B:* Der AG-Status zeigt nur Arbeitsgänge, deren Kürzel in den Arbeitsschritten der Strukturzeile steht, plus immer den Zeilen-AG.
    Ohne Strukturzeile wie heute alle. Das nimmt Antwort 4 der Masken-Spec teilweise zurück.
  - *Variante C:* Keine Codeänderung, nur FA-1 und ein Satz in Hilfe/Anwenderdoku („AG-Status zeigt alle Arbeitsgänge des Auftrags, auch die
    seiner Blechteile“).
- **Empfehlung A.** Gründe: Nach FA-1 verschwinden zwar die KA-Zeilen am KA-Terminal. Das Muster bleibt aber an jeder Werkbank ohne Teileliste
  (z. B. ST/EG, falls P-3 das bestätigt): Dort ist die FA-Zeile der einzige Meldeweg, und der Werker muss sehen, dass „sein“ ST an Blechteilen
  der Baugruppe liegt. B versteckt Arbeitsgänge, die zum Auftrag gehören, und weicht von Antwort 4 ab. C lässt die Verwechslung bestehen.
  A ist eine Eigenschaft mehr am Symbol und eine Zeile Logik in `FaSichtAsync`.

## Technischer Loesungsentwurf
**FA-1:** kein neuer SQL-Code. Neuer Eintrag in `scripts/deploy/nachlauf.psd1` direkt nach `:338-340`, gleiche `Datei`, gleiche `Braucht`/
`Voraussetzung`, neue Id `Einmalig/2026-11-17_v1.99.32_RemoveParentWorkOperationsFromLeafTokensTeileliste#2`, Version = Version dieses Laufs.
Der Dev-Lauf prüft, ob `NachlaufManifestDriftTests` eine zweite Referenz auf dieselbe Datei zulässt (Muster „SQL/110#2“ steht nur als Beispiel im
Kopf `:20`, einen echten Präzedenzfall gibt es noch nicht). Falls nicht: den Test minimal um „gleiche Datei, Id mit `#n`“ erweitern.

**FA-2 Variante A** (ponytail: vorhandenes Symbol und Partial wiederverwenden, keine neue Klasse):
- `FaArbeitsschrittSymbol` (`FaHierarchyTreeViewModel.cs:228-256`): neue Init-Eigenschaft `bool AnTeilen`, Default false. Dadurch bleiben
  FA-Struktur, Teile Einzeln/Summiert und das Arbeitsgänge-Modal unverändert, denn dort setzt sie niemand. `AriaLabel` (`:252`) hängt bei
  `AnTeilen` „ – an Teilen der Baugruppe“ an (vor „ – dieser Arbeitsgang“).
- `BdeFaAngebot.FaSichtAsync`: Den Knoten je Zeile **vor** dem Symbol-Bau auflösen (heute erst `:144`). Danach
  `eigene = FaArbeitsschrittSymbole.Tokenize(knoten?.Arbeitsschritte)` (OrdinalIgnoreCase, Trim wie die Erkennung) und je Symbol
  `AnTeilen = knoten != null && !eigene.Contains(w.OperationNumber.Trim())`. Der Default-Arbeitsgang „01“ (NurFA) wird nie gekennzeichnet,
  er stammt nicht aus der Struktur.
- `_FaAgSymbole.cshtml`: Bei `AnTeilen` ein sichtbares Zusatzzeichen bzw. Kurztext, z. B. hochgestelltes „T“ mit derselben `title`/`aria`-Quelle.
  Kontrast AA, Farbe nie allein (Skill `frontend-design` Pflicht). Die Legende/der Spaltentitel in `Fa.cshtml:35` nennt die Bedeutung.
- Der Spaltenfilter `ag-status` filtert schon auf `AriaLabel` (`BdeFaAngebot.cs:163`), „an Teilen“ ist also ohne weitere Änderung filterbar.

**FA-2 Variante B** (nur falls freigegeben): in `FaSichtAsync` `agJeAuftrag[po.Id]` mit Knoten auf
`eigene.Contains(...) || w.Id == z.Ag.Op.Id` filtern, ohne Knoten alle. Keine Modelländerung.

Listen-View-Pattern: keine neue Tabelle. Pagination und Spaltenfilter der FA-Sicht bleiben (`BdeTeilelisteController.cs:159-183`).

## Migrations-/SQL-Auswirkungen
Keine Migration, kein `SQL/XX_*.sql`, kein `00_FreshInstall.sql`, `SQL/AgentJobs` nicht betroffen (Suche nach `WorkOperations` dort: keine
Treffer). FA-1 nutzt das vorhandene Einmal-Skript **datenlöschend** (nur ungebuchte, von der Erkennung angelegte Kandidaten; `:17-19`).
**DB-Backup vor SCHARF**, das erzwingt das Update-Skript (Backup aus diesem Lauf). Keine Änderung an der Skriptdatei, also bleibt die SHA
gleich. Damit bleiben `TREFFER`-Zeile und `DECLARE @Trockenlauf BIT = 1;` wie gehabt (ADR 0023). Das Skript hat bewusst keine Wiederholungssperre (`:24-25`).

## Einstellungen
keine

| Key | Typ | Erlaubte Werte | Default | Wirkung | Verwendung | Standort | Warnhinweis |
|---|---|---|---|---|---|---|---|
| – | – | – | – | – | – | – | – |

## Audit-Feld-Auswirkungen
Keine Entität wird geändert. Das Einmal-Skript löscht `WorkOperations` ohne Audit (wie die Präzedenz v1.99.32). Nachweis sind die Zählung und
die Meldeliste im Deploy-Protokoll sowie der Marker in `DeployScriptRuns`.

## Rollen / Zugriff
Unverändert. Die BDE-Seite bleibt hinter `[RequireBdeActive]` und `[RequireBdeUserAccess]` (`BdeTeilelisteController.cs:17-18`). Es gibt keine neue
Rolle und keinen neuen Filter.

## Akzeptanzkriterien
1. P-1 bis P-5 und der Trockenlauf sind auf der DB aus Rückfrage 3 gelaufen. Ergebnis und Entscheidungsweg stehen in dieser Spec, **bevor** Code
   entsteht.
2. `nachlauf.psd1` enthält den Eintrag `…#2` (Typ Einmalig, Firma IDEAL, Phase Nachher, Ausführung Trockenlauf). `NachlaufManifestDriftTests` ist grün.
3. Nach SCHARF auf IDEAL zeigt die FA-Sicht am KA-Terminal mit Spaltenfilter Haupt-FA = 1069617 keine Zeilen mehr für Sub-FAs, deren
   Kopfzeile KA nicht trägt (1069719, 1069721–1069727, 1069729, 1069732, 1069733 laut Screenshot). Ausgenommen sind Kandidaten auf der
   Meldeliste (gebucht). Bei 1069719 ist das KA-Symbol im Screenshot hervorgehoben, dort läuft womöglich schon eine Buchung (P-2 bzw.
   Meldeliste). Die Zeilen 1069728 und 1069730 (Kopfzeile KA) bleiben.
4. Ein zweiter Trockenlauf liefert `TREFFER: 0`.
5. (A) In der FA-Sicht trägt jedes Symbol, dessen Kürzel nicht in den Arbeitsschritten der Zeile steht, das sichtbare Kennzeichen „an Teilen“.
   Sein `aria-label`/`title` enthält „an Teilen der Baugruppe“. Symbole mit Kürzel in den Arbeitsschritten sind unverändert.
6. (A) Spaltenfilter AG-Status „an Teilen“ findet genau die Zeilen mit mindestens einem so gekennzeichneten Symbol.
7. (A) Teile Einzeln, Teile Summiert, FA-Struktur und das Arbeitsgänge-Modal zeigen nirgends „an Teilen“.
8. (A) AKE (Master `ProduktionsauftragHierarchisch` aus, keine Strukturzeile): Die FA-Sicht ist unverändert, ohne Kennzeichnung.
9. Unit-Test (InMemory) für `FaSichtAsync`: Sub-FA mit Kopfknoten „VM“ und WorkOperations VM, ST. ST ist `AnTeilen`, VM nicht. Ohne Knoten ist
   keines `AnTeilen`.
10. Hilfe (`Views/Help`) und `docs/anwender/17-bde-teileliste-werkbank.md` Abschnitt FA-Sicht erklären den AG-Status (alle Arbeitsgänge des
    Auftrags, Kennzeichen „an Teilen“). Foto der FA-Sicht von IDEAL-DEV.
11. Build und alle Tests grün. Die Version steht in beiden `AppVersion.cs`, das Anwender-Changelog ist ergänzt.

(Bei Variante B treten an die Stelle von AK 5–7: Mit Strukturzeile zeigt der AG-Status nur Kürzel aus den Arbeitsschritten der Zeile plus den
Zeilen-AG. Bei Variante C entfallen AK 5–9, es bleibt AK 10 mit dem Erklärsatz.)

## Test-Szenarien
- **TS-197 (FA-Sicht) ergänzen** um Schritt „AG-Status mit Kennzeichen an Teilen“ (Variante A).
- **Neues TS-<nächste freie Nummer, derzeit nach TS-209>: „BDE FA-Sicht – Arbeitsgänge der Blechteile, KA-Altbestand“** (IDEAL-DEV)
  - Vorbedingungen: Master an. Werkbank KA mit BDE aktiv und BDE-Teileliste. Werkbank ST ohne Teileliste. Ein Haupt-FA mit Baugruppe (Kopfzeile
    „VM“) und Blechteilen „ST KA“. Erkennung gelaufen.
  - Schritte: (1) Update-Skript-Nachlauf: Trockenlauf `…#2` zeigt Treffer > 0 bei vorhandenem KA-Altbestand → SCHARF → zweiter Trockenlauf 0.
    (2) KA-Terminal, Reiter FA: keine Zeile der Baugruppe. Reiter Teile Einzeln: Blechteile mit KA meldbar. (3) ST-Terminal, Reiter FA:
    Zeile der Baugruppe mit Arbeitsgang ST. Im AG-Status ist ST mit „an Teilen“ gekennzeichnet, VM nicht, Arbeitsschritte = „VM“.
    (4) Spaltenfilter AG-Status „an Teilen“.
  - Negativfälle: Kandidat mit BDE-Buchung bleibt stehen und steht auf der Meldeliste. AKE-FA-Sicht ohne Kennzeichen. Baugruppe ohne
    eigene Arbeitsschritte (wie 1069723): alle Symbole gekennzeichnet.
- `docs/TESTSZENARIEN.md` und `secondbrain/tests/testszenarien-index.md` nachziehen.

## Etappen (nur bei epic: true)
entfällt (kein Epic)

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | – | – | – |

## Deploy
*Vorläufig – der QA-Lauf bestätigt es gegen den echten Diff.*
- **Web-App:** ja (Variante A/B; bei C nur wegen Hilfe-Text)
- **Service:** nein
- **Migration:** nein
- **Manifest-Einträge** (`scripts/deploy/nachlauf.psd1`):
  - `Einmalig/2026-11-17_v1.99.32_RemoveParentWorkOperationsFromLeafTokensTeileliste#2`, Version = neue Version, Typ Einmalig, Firma IDEAL,
    Phase Nachher, Ausführung Trockenlauf, `Datei='Einmalig\2026-11-17_v1.99.32_RemoveParentWorkOperationsFromLeafTokensTeileliste.sql'`,
    `Braucht=@('SQL/154')`. Text: „Wiederholung (WMS-10): entfernt KA-/Teileliste-Arbeitsgänge an Baugruppen, die nur von Blechteilen stammen
    und nach dem Setzen der Option BDE-Teileliste übrig blieben. Trockenlauf automatisch, TREFFER 0 → entfällt. Sonst Zählung und Meldeliste
    prüfen, SCHARF (Backup aus diesem Lauf Pflicht).“
  - `v<neu>-BdeFaSichtAnTeilenInfo`, Typ Info, Firma IDEAL, Phase Nachher, Text: „BDE-Seite, Reiter FA: Arbeitsgänge, die an Blechteilen der
    Baugruppe liegen, sind im AG-Status als ‚an Teilen‘ gekennzeichnet. An KA-Werkbänken mit Teileliste erscheinen Baugruppen ohne eigenes KA
    nicht mehr im Reiter FA. Werker kurz informieren.“ (nur Variante A)
- **Publish-Befehle:** `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`

## Befund Prüfabfragen
*(vom Menschen bzw. Dev-Lauf nach P-1 bis P-5 einzutragen)*

## Offene Rueckfragen
1. (Fachbereich: Benjamin Andl-Braun) AG-Status der FA-Sicht: Variante A (alle Arbeitsgänge der Sub-FA bleiben, die von Blechteilen der
   Baugruppe stammenden sind sichtbar als „an Teilen“ gekennzeichnet), B (nur die Arbeitsgänge aus den Arbeitsschritten der Zeile plus der
   Arbeitsgang der Zeile) oder C (keine Änderung der Anzeige, nur Datenbereinigung und Erklärung in der Hilfe)? Empfehlung A.
2. (Fachbereich: Benjamin Andl-Braun) Falls an KA-Arbeitsgängen von Baugruppen, deren KA nur an Blechteilen steht, schon gebucht wurde:
   bleiben diese Arbeitsgänge stehen und werden zu Ende gebucht, oder storniert der BDE-Admin die Buchung, damit die Bereinigung sie entfernen kann?
3. Auf welcher IDEAL-Datenbank entstand der Screenshot (Test oder Produktion)? Dort laufen die Prüfabfragen P-1 bis P-5.
4. Datenbereinigung: neuer Manifest-Eintrag mit neuer Id (…#2) im nächsten Paket (Empfehlung) oder sofort manueller Lauf des Einmal-Skripts
   auf der betroffenen DB, unabhängig vom Release?

## ANTWORTEN auf Rueckfragen - Fachbereich (aus Jira)

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

1. (Fachbereich) Variante A / B / C für den AG-Status der FA-Sicht →
2. (Fachbereich) Gebuchte KA-Arbeitsgänge an Baugruppen: stehen lassen oder stornieren →
3. DB des Screenshots (Test/Produktion) →
4. Bereinigung per Manifest-Eintrag #2 oder manueller Sofortlauf →
