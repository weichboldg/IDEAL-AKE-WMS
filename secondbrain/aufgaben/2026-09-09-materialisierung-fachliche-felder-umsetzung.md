---
type: aufgabe
title: "IDEAL: Materialisierung um die fachlichen Felder erweitern (K1/K2) — Umsetzung"
status: InUmsetzung
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

## Etappen / Tasks

_(wird im Dev-Lauf gefuellt)_

## Entscheidungen im Dev-Lauf (Rulings)

1. Eager-Create umfasst **beide** Statustabellen, nicht nur `PickingStatus` (Begruendung oben).

## Offene Punkte

- Deploy-Vorbedingung aus der Spec: die fuenf Arbeitsplaetze (`K-02`, `S-01`, `H1-03`, `H2-02`,
  `H4-04`) **vor** dem Deploy anlegen, sonst Zwei-Lauf-Ablauf (Werkbank bleibt beim ersten Lauf leer).
- H8 der Kritischen Pruefung: ADR-Kandidat „Sage fuehrend fuer die Werkbank bei IDEAL".
- H7: Glossar-Luecken (Arbeitsbereich, Lack-T, Komm., BG-Termin, Kombinationsgeraet, HauptFA).
