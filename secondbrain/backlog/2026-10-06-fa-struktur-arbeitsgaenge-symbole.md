---
typ: feature
split: true
anhaenge:
  - anhaenge/2026-10-06-fa-struktur-arbeitsgaenge-symbole/sage-fa-struktur-arbeitsschritte.png
---
# FA-Struktur: Arbeitsgänge als Symbole, später BDE-Status in Farbe

**Aufgenommen 2026-10-06** (Anforderung Gerald, Koordination).

## Anforderung (Gerald)

- Arbeitsgänge sollen als **Symbole** aufgelistet werden, nicht als Zeile.
- In weiterer Folge bei aktivem BDE: **Grün = erledigt, Grau = offen, Rot = überfällig**.
- Vorbereitung, damit die Aufträge im BDE angezeigt werden, inklusive der notwendigen Arbeitsgänge.

## Ausgangslage (am Code belegt, Bündel-Worktree)

- Heute: Knopf an der HauptFA-Kopfzeile öffnet ein Modal, das je Sub-FA die erkannten Arbeitsgänge
  **zeilenweise** listet (`Views/FaHierarchy/_FaWorkOperationsModal.cshtml`, v1.45,
  [[2026-09-23-fa-struktur-arbeitsgaenge-anzeige-spec]]). Freigabe-Entscheidung dort: Inhalt wird erst
  beim Öffnen geladen, der Seitenaufbau macht nur einen schlanken Existenz-Check.
- Datenquelle: `WorkOperation` (`Models/WorkOperation.cs`) mit `OperationNumber`, `Name`,
  `ProductionWorkplaceId`, `Sequence`, `IsReportable`, `IsReported`, `ReportedAt`.
- **Es gibt kein Soll-Datum je Arbeitsgang** in `WorkOperation`. Termine liegen nur am Auftrag
  (`FaHierarchyOrderInfo.KO_Termin`, `FE_Termin`, `Start_Beschichtung`).

## Vorbild aus Sage (Anhang, Screenshot von Gerald 2026-10-06)

![[anhaenge/2026-10-06-fa-struktur-arbeitsgaenge-symbole/sage-fa-struktur-arbeitsschritte.png]]

Sage „Fertigungsaufträge bearbeiten“ zeigt die Struktur als Baum mit den Spalten **Bezeichnung,
Abteilung, Arbeitsschritte, Artikeltyp**. Die Arbeitsschritte stehen **je Baumzeile kompakt als Kürzel
in einer Spalte** (z. B. `SW`, `ZS`, `BE`, `PG`, `VM`, `LS EG KA`, `LS KA PG`), nicht als eigene Zeilen.
Die Abteilung steht je Zeile (z. B. `H1-03`, `S-01`, `H2-02`, `K-02`). Ein Knoten `FR-Zuschnitt` hat ein
eigenes Symbol (vermutlich ein Arbeitsgang-Knoten – ungeprüft).

Das ist der Anhaltspunkt für Baustein 1: **Arbeitsgänge je Baumzeile als Kürzel-Symbole** (Badges),
in Baustein 2 eingefärbt nach BDE-Status.

Am Code dazu: `FaHierarchyNode` hat bereits die Felder `Arbeitsschritte` und `Arbeitsbereich`
(`Models/FaHierarchyNode.cs:40-41`, Bündel-Worktree). Ob die Sage-Spalten „Arbeitsschritte“ und
„Abteilung“ genau diese Felder sind, ist **nicht aus den Namen abzuleiten** – Prüfabfrage für Gerald
(IDEAL-WMS-Datenbank), Erwartung laut Screenshot:

```sql
SELECT Artnr, Bezeichnung1, Arbeitsbereich, Arbeitsschritte
FROM dbo.FaHierarchyNodes
WHERE Artnr IN ('G-KT-A-BODEN-1320-660-V01', 'FR-4301-40-20-1,25-1530,0MM-V01',
                'G-KT-Mittelholmblende-40-760-V02', 'G-KT-Korpus-Gepunktet')
   OR Bezeichnung1 IN ('G-KT-A-BODEN-1320-660-V01', 'G-KT-Korpus-Gepunktet');
-- Screenshot: A-BODEN = H1-03 / "LS KA PG"; FR-4301-...-1530 = S-01 / "ZS";
--             Mittelholmblende = H2-02 / "VM"; Korpus-Gepunktet = (leer) / "BE"
```

Offen dazu: Sind die Symbole die Sage-**Arbeitsschritte-Kürzel** (Feld am Knoten) oder die im WMS
**erkannten Arbeitsgänge** (`WorkOperation`, mit Rückmeldestatus)? Für die BDE-Farben in Baustein 2
braucht es den Bezug Kürzel → `WorkOperation`, sonst gibt es keinen Status.

## Gewünschter Schnitt (split)

1. **Symbole statt Zeilen** – reine Anzeige, ohne BDE-Status.
2. **Status-Farben bei aktivem BDE** – grün/grau/rot.
3. **Vorbereitung BDE-Auftragsanzeige** – Inhalt mit Gerald klären (siehe Frage 5).

## Für die Spec zu klären (Empfehlungen der Koordination in Klammern)

1. **Wo erscheinen die Symbole:** direkt in den Baumzeilen je (Sub-)FA oder im bestehenden Modal?
   Direkt im Baum widerspricht der v1.45-Entscheidung „erst beim Öffnen laden“ und braucht eine
   gebündelte Abfrage je Seite (kein N+1).
2. **Was zeigt ein Symbol:** Kürzel/Nummer des Arbeitsgangs bzw. der Werkbank, Name im Tooltip.
3. **Farbe darf nie alleiniger Bedeutungsträger sein** (CLAUDE.md, Fertigungsterminals): Status
   zusätzlich über Form/Icon und Text (Tooltip, Screenreader). Kontrast WCAG AA.
4. **„Überfällig“ braucht eine Definition**, weil es kein Soll-Datum je Arbeitsgang gibt: z. B.
   Auftragstermin (`FE_Termin`?) überschritten und Arbeitsgang nicht rückgemeldet. Welcher Termin gilt,
   entscheidet Gerald; die Bedeutung der Terminfelder ist nicht aus dem Namen abzuleiten, sondern an der
   Quelle zu prüfen.
5. **„Aufträge im BDE anzeigen inkl. Arbeitsgänge“:** Was genau fehlt heute im BDE (welche Ansicht,
   welche Rolle, welcher Auftragsbestand)? Bezug zu [[2026-09-21-ideal-bde-arbeitsgaenge-aus-struktur-spec]]
   (Arbeitsgänge aus der Struktur, Werkbank-Sync). Baustein 3 erst spezifizieren, wenn das beantwortet ist.

## Hinweis zur Reihenfolge

Baut auf dem noch nicht gemergten Bündel auf (v1.44/v1.45). Umsetzung nach Schranke 2 des Bündels.
