---
type: aufgabe
title: "IDEAL: Materialisierung um die fachlichen Felder erweitern (K1/K2) — Umsetzung"
status: Testbereit
spec: "[[2026-08-20-materialisierung-fachliche-felder-spec]]"
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
created: 2026-09-09
updated: 2026-09-09
---

# Materialisierung fachliche Felder (Umsetzung)

Umsetzung der freigegebenen Spec [[2026-08-20-materialisierung-fachliche-felder-spec]] im
**bestehenden Buendel-Worktree** — die Spec bindet das im Frontmatter und begruendet es im
Deploy-Abschnitt: der Code, auf dem sie aufsetzt (`FaMaterializationSyncService`,
`ProductionOrderListGroup`, `SubOrderNumber`), existiert **nur** in diesem Zweig. Ausgangs-HEAD
`73cea2a` (Buendel `Testbereit` v1.36.0). Kein Merge, kein Push.

## Warum jetzt — der zweite Auftrag dieses Laufs

Der Mensch hat den Lauf ausdruecklich mit einem zweiten Ziel gestartet: **den im UAT gemeldeten
Fehler in diesem Zug mitbeheben** ([[2026-09-09-materialisierung-ohne-statuszeilen-bug]], gefuehrt
als **H0** in [[2026-09-08-ideal-code-review-nachlese]]) — ein materialisierter Sub-FA laesst sich
im Leitstand nicht freigeben, weil die `ProductionOrderPickingStatus`-Zeile fehlt.

Das trifft sich: Die Spec verlangt unter **F6** ohnehin genau diesen Eager-Create, weil sonst die
`HasCoatingParts`-Ableitung ins Leere schreibt. Der Fehler und die Spec haben **dieselbe Ursache**
und werden mit **derselben** Aenderung erledigt.

**Ruling 1 (Erweiterung ueber den Spec-Wortlaut hinaus):** F6 nennt nur
`ProductionOrderPickingStatus`. Der Bug-Record zeigt, dass `ProductionOrderBdeStatus` dieselbe
Luecke hat (BDE-Rueckmeldung, Kaskade „Alle Sub-FAs fertigmelden"). Der Eager-Create umfasst
deshalb **beide** Tabellen — der AKE-Sync legt sie ebenfalls als Paar an, und die halbe Loesung
haette denselben Fehler nur verschoben.

## Verbindliche Vorgaben aus der Freigabe (Schranke 1)

- **B1:** `HasCoatingParts` wird aus `FaHierarchyNode.Beschichtet` abgeleitet (Sub-FA selbst oder
  direktes Kind), **kein** rohes `Beschichtet`-Feld, **keine** Migration. Eager-Create der
  Statuszeile direkt im Anlege-/Update-Zweig, nicht als Nachlaufschritt.
- **B2/B3:** Fert.-Termin ← `FE_Termin`; Komm.-Termin **weiterhin berechnet**; BG-Termin aus der
  unveraenderten Kaskade; Liefertermin ← `Verladetermin_Vsl`. `KO_Termin` ist der
  **Konstruktions-Termin** und kommt **zusaetzlich** als eigenes K1-Kopfzeilenfeld.
- **B4:** Abweichungsmeldung bleibt **unscharf** („Werkbank weicht vom Quellwert ab"), kein
  `SourceWorkplaceName`, keine Migration. Umschaltpunkt auf Variante C ist menschliche Beurteilung.
- **B5:** Umfang **nur** `/ProductionOrders`. Kunde-Filter **und** Kunde-Spaltenfilter per
  `FaHierarchyOrderInfo`-Join (wirkt ueber das Repository auch auf den Leitstand). Kein
  Ausblende-Mechanismus (F7 entfaellt).
- **Rueckfrage 1:** unbekannte Arbeitsbereiche **melden**, nicht automatisch anlegen. Log je Lauf,
  Mail nur bei Aenderung der Menge (S1).
- **Rueckfrage 7:** Kombigeraet-Meldung im `FaHierarchySyncService`, einmal je Lauf als Sammelmeldung.
- **S2:** Name-Match case-insensitiv + getrimmt; bei mehreren Treffern **keine** Zuweisung, eigener
  Meldefall.

## Oberflaechen-Entwurf (Skill `frontend-design`, aufgerufen 2026-09-09)

Der Skill draengt auf eine eigenstaendige visuelle Handschrift. **CLAUDE.md stellt hier ausdruecklich
Konsistenz davor** — die FA-Liste ist ein Werkzeug an Fertigungsterminals, kein Schaustueck. Die
Entscheidung ist deshalb bewusst **kein** neues Muster, sondern die woertliche Uebernahme des bereits
etablierten Kopfzeilen-Musters aus `Views/FaHierarchy/Index.cshtml:144-190`. Begruendung: Ein Werker
wechselt zwischen FA-Struktur und FA-Liste; dieselben Daten muessen dort gleich aussehen und gleich
heissen, sonst liest er sie zweimal neu.

**Eindeutiger Kopf (Normalfall)** — eine Zeile, Abzeichen hinter „HauptFA {Nr}" + Sub-FA-Zaehler,
in Lesereihenfolge nach Wichtigkeit:

| Reihenfolge | Inhalt | Darstellung | Titel (Tooltip) |
|---|---|---|---|
| 1 | Kunde | `badge bg-primary` | Kunde |
| 2 | Prio | `badge bg-secondary`, Text `Prio N` | Prioritaet |
| 3 | Montage-Abteilung | `badge bg-light text-dark border` | Montage-Abteilung |
| 4 | AB-Nummer | `badge bg-light text-dark border`, Text `AB {Nr}` | AB-Nummer |
| 5 | Konstruktions-Termin | `badge bg-light text-dark border`, Text `KO tt.mm.jjjj` | Konstruktions-Termin |
| 6 | Fert.-Termin | `badge bg-light text-dark border`, Text `FE tt.mm.jjjj` | Fertigungstermin |
| 7 | Liefertermin | `badge bg-light text-dark border`, Text `LT tt.mm.jjjj` | Liefertermin |

`KO`/`FE` sind die bereits in der FA-Struktur benutzten Kuerzel — sie werden **nicht** umbenannt.
`LT` kommt neu dazu, mit demselben Bauprinzip. **Die Bedeutung traegt immer das Kuerzel plus der
Tooltip, nie die Farbe** (Kontrast-Leitplanke: Farbe ist nie alleiniger Bedeutungstraeger).
Dunkler Text auf hellem Grund mit Rand erfuellt AA.

**Mehrdeutiger Kopf (Kombigeraet)** — hier liegt der einzige gestalterische Akzent des Entwurfs,
und er ist fachlich begruendet: Die Oberflaeche **weigert sich, einen Wert auszuwaehlen**. Statt
Termin-Abzeichen erscheint das Abzeichen `badge bg-info text-dark` „mehrdeutig (Kombigeraet)" und
darunter — Muster `fa-head-table` aus `FaHierarchy/Index.cshtml:199-225` — eine kleine
`table-sm table-bordered` mit **einer Zeile je Kopfvariante**: Montage-Abt., Kunde, Konstr.-Termin,
Fert.-Termin, Liefertermin, Prio, AB-Nr. Grund: Bei Kombigeraeten koennen die Varianten fachlich
verschiedene Termine und Beschichter tragen; eine still gewaehlte Variante schickt Teile zum
falschen Beschichter ([[2026-08-06-kombinationsgeraete-montageabteilung]]).

**Keine Kopfdaten:** `text-muted small` mit demselben Wortlaut wie in der FA-Struktur.

**Bewusst weggelassen** (eine Sache zurueckgenommen): kein zusaetzliches Icon in der Kopfzeile. Die
FA-Struktur traegt dort ein Kisten-Icon, weil sie Knotentypen unterscheidet; die FA-Liste hat bereits
Chevron und Beschriftung. Ein weiteres Icon waere Rauschen ohne Information.

## Etappen / Tasks

Plan: `docs/superpowers/plans/2026-09-09-materialisierung-fachliche-felder.md` (Worktree, Commit
`a9ea910`) — mit Pre-Flight-Scan, der drei Fehler im Plan selbst gefunden hat (Variable ausserhalb
ihres Gueltigkeitsbereichs, anonymer Typ als Methodenparameter, geratener Feldname); alle drei vor
dem Start korrigiert.

| # | Task | Status |
|---|---|---|
| 1 | **Eager-Create beider Statuszeilen**, Anlege- und Update-Pfad — der gemeldete UAT-Fehler und Spec-F6 in einem | **erledigt** `ceaf5b3`, Review clean |
| 2 | Quell-Record + reiner Helper `FaMaterializationCoating` | **erledigt** `af25bcd` — Umsetzer fand einen **Planfehler** (Blattebene weggefiltert), Review clean |
| 3 | Werkbank aus `Arbeitsbereich` (Variante B) + drei Meldefaelle | **erledigt** `ccc1d74`, Review clean |
| 4 | Lack-Kennzeichen ueber `SetCoatingPartsAsync` schreiben (+ `nodeByKey`-Nachzug `cc1a5f5`) | **erledigt** `77aaac2`, Review clean |
| — | Nachzug: Test fuer die Mail-Entprellung + Fehler „Wiederauftreten wird nie gemeldet" | **erledigt** `520bafb` (vom Controller selbst gebaut) |
| 5 | Kombigeraet-Erkennung im Struktur-Sync, einmal je Lauf | **erledigt** `904e899`, Review clean |
| 6 | `GetByHauptFaKeysAsync` — eine Abfrage je Seite | **erledigt** `47c6712`, Review clean |
| 7 | Kunde-Freitext + Spaltenfilter hierarchisch | **erledigt** `79a174b` + Fixrunde `5022222` (Flachmodus zurueckgenommen, Mehr-Token-Tests, SQL-Uebersetzungstest) |
| 8 | ViewModel + Controller: K1, Termin-Kaskade, Beschichtungstermin | **erledigt** `f160b1d` + Fixrunde `b3d5a15` (geratener Beschichtungstermin) |
| 9 | Gruppen-Kopfzeile + Varianten-Tabelle bei Kombigeraeten | **erledigt** `86cdc26` |
| 10 | v1.37.0, Anwender-Changelog, Hilfe, TS-71 | **erledigt** `1f038f6` |
| 11 | qa-agent: Build+Tests gruen, AK 1-13 gegen Diff `a9ea910..be92ade` abgeglichen, Deploy-Abschnitt finalisiert, manuelle Test-Checkliste | **erledigt**, Status `Testbereit` |

## Vier Fehler, im Lauf gefunden und behoben

Alle vier waeren **still** gewesen — keiner haette eine Fehlermeldung erzeugt. Zwei fielen nicht
beim Programmieren auf, sondern beim **Schreiben von Tests**:

1. Der gemeldete Abnahme-Fehler (fehlende Statuszeilen) — `ceaf5b3`.
2. **Blattebene weggefiltert:** Die Knotenabfrage filterte Blaetter weg, bevor die Lack-Ableitung
   sie sah. Da ein direktes Kind meistens ein Blatt ist, haette die Ableitung fuer die Mehrheit der
   Faelle `false` geliefert — und kein Test waere rot geworden, weil die Plan-Testdaten zufaellig
   passend gewaehlt waren. `af25bcd`.
3. **Wiederkehrendes Problem nie wieder gemeldet:** Die Mail-Entprellung wurde nur fortgeschrieben,
   wenn es etwas zu melden gab. `520bafb`.
4. **Geratener Beschichtungstermin:** Bei gesetztem Fertigungstermin, aber leerem Kopf-Termin blieb
   der aus der AKE-Formel zurueckgerechnete Wert stehen. `b3d5a15`.

## Geparkte Befunde

Elf nicht blockierende Befunde in [[2026-09-09-materialisierung-nachlese]], je mit Begruendung
der Parkentscheidung.

**Brain parallel erledigt** (beruehrt den Worktree nicht): ADR
[[0014-werkbank-datenhoheit-sage-fuehrend-mit-abweichungsmeldung]] und acht Glossar-Begriffe
(H7/H8 der Kritischen Pruefung), Commit `644e3c0`.

## Entscheidungen im Dev-Lauf (Rulings)

1. Eager-Create umfasst **beide** Statustabellen, nicht nur `PickingStatus` (Begruendung oben).

## Offene Punkte

- Deploy-Vorbedingung aus der Spec: die fuenf Arbeitsplaetze (`K-02`, `S-01`, `H1-03`, `H2-02`,
  `H4-04`) **vor** dem Deploy anlegen, sonst Zwei-Lauf-Ablauf (Werkbank bleibt beim ersten Lauf leer).
- H8 der Kritischen Pruefung: ADR-Kandidat „Sage fuehrend fuer die Werkbank bei IDEAL".
- H7: Glossar-Luecken (Arbeitsbereich, Lack-T, Komm., BG-Termin, Kombinationsgeraet, HauptFA).
