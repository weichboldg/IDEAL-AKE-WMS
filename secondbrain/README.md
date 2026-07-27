# IdealAkeWms Second Brain

Single Source of Truth des Projekts. Regeln:

1. **Brain-first**: Jeder Agent liest hier, bevor er handelt, und schreibt
   nach dem Handeln zurueck. Keine Aufgabe ist fertig ohne Brain-Update.
2. **Agenten lesen, Menschen schreiben**: `architektur/`, `codebase/`,
   `glossar/` pflegt primaer der Mensch; Agenten ergaenzen dort nur additiv
   (neue ADRs, neue Glossarbegriffe) und ueberschreiben nie Bestehendes.
   `specs/`, `aufgaben/`, `bugs/`, `changelog/` sind agenten-geschrieben.
3. **Status lebt im Frontmatter** (`status:`) - Ordner sind die menschliche
   Geste, Frontmatter die maschinenlesbare Wahrheit.

## Navigation

| Ordner | Inhalt |
|---|---|
| [[feature-map]] | Was existiert, Status, Code-Verweise |
| `architektur/` | ADRs (MADR), Muster, [[fallstricke]] - das "Warum" |
| `codebase/` | Navigierbare Karte: Module, Controller, Services, Datenmodell, Integrationen |
| `glossar/` | Domaenensprache (FA, Kommissionierung, Rollen ...) |
| `backlog/` | Formlose neue Anforderungen (Mensch legt ab) |
| `specs/entwurf/` | Vom Spec-Agent ausgearbeitete Specs (Status: Entwurf) |
| `specs/freigegeben/` | Freigegebene Specs = Startsignal Entwicklung (Schranke 1) |
| `aufgaben/` | Task-Tracking je Aufgabe |
| `bugs/` | Bugs + bekannte Probleme mit Status |
| `tests/` | Index der Testszenarien, verlinkt ../docs/TESTSZENARIEN.md |
| `changelog/` | Aenderungshistorie (Basis der Release-Seite) |

## Status-Automat

NEU -> SPEZIFIZIERT (Entwurf) -> FREIGEGEBEN -> IN_UMSETZUNG -> TESTBEREIT -> GEMERGED

Schranke 1 (manuell): Spec von entwurf/ nach freigegeben/ verschieben
UND `status: Freigegeben` setzen (macht sync-onedrive-specs.ps1 bzw. der Mensch).
Schranke 2 (manuell): Merge nach main erst nach erfolgreichem manuellem Test.
