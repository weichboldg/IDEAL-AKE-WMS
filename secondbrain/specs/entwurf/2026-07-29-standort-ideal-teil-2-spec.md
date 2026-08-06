---
type: spec
title: "IDEAL-Standort Teil 2 — Struktur-/Baumanzeige (rekursiv)"
slug: 2026-07-29-standort-ideal-teil-2-spec
status: Entwurf
created: 2026-08-06
updated: 2026-08-06
source_backlog: "[[2026-07-29-Standort-IDEAL]]"
depends_on: "[[2026-07-29-standort-ideal-teil-1-spec]]"
task: ""
worktree: ""
branch: ""
affected_code:
  - IdealAkeWms/Controllers/FaHierarchyController.cs (neu, Name provisorisch)
  - IdealAkeWms/Views/FaHierarchyNode/Index.cshtml (neu)
  - IdealAkeWms/Models/ViewModels/FaHierarchyTreeViewModel.cs (neu)
  - IdealAkeWms/Models/AppSettingKeys.cs
  - IdealAkeWms/wwwroot/js/ideal-fa-struktur-tree.js (neu)
  - docs/TESTSZENARIEN.md
  - secondbrain/tests/testszenarien-index.md
open_questions:
  - "Rekursive Baumanzeige vs. bestehendes Listen-View-Pattern (ADR 0005): Pagination ueber Gruppen (Strukturen) statt Zeilen — genaues UX-Konzept fuer Paging/Phantom-Header/Auto-Expand bei Filtertreffer noch offen"
  - "Toggle-Heimat ProduktionsauftragBaumAnzeige: AppSettings (ADR 0011) oder ServiceSettings? (siehe Uebersichts-Rueckfrage 2)"
  - "Rollen/Zugriff fuer diese Ansicht: bestehende Rolle wiederverwenden (z. B. picking/vorbau) oder neue IDEAL-spezifische Rolle?"
  - "Fallback-Verhalten bei Baum aus: laut Notiz KEINE Rueckkehr zur heutigen Ansicht, sondern eine flache Liste ALLER Sub-FAs (dritter Zustand) — muss das explizit als eigene View/Modus umgesetzt werden oder genuegt Teil 1's Rohliste?"
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

## Ziel / Nutzen (das Warum)

Teil 1 legt die mehrstufige Struktur (`FaHierarchyNode`) in der DB ab; Teil 2 macht sie fuer
Anwender sichtbar — als rekursiver Baum (Haupt-FA → Sub-FA → Sub-Sub-FA …), nicht als
zweistufige Gruppierung (B1 hat den alten 2-Ebenen-Entwurf ueberholt). Ohne diesen Teil bleiben
die importierten Daten unsichtbar; laut Notiz ist dies zugleich der **riskanteste** Anzeigeteil
und bekommt deshalb einen eigenen Schalter, um ihn bei Problemen isoliert abschalten zu koennen,
ohne den Import (Teil 1) zu verlieren.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope:** rekursive Baum-Darstellung von `FaHierarchyNode` je Haupt-FA; Server-seitiges
Paging **ueber Strukturen** (nicht ueber Einzelzeilen, da eine Struktur beliebig viele Positionen
haben kann); Expand/Collapse; Auto-Expand bis zum ersten Filtertreffer; „Baum aus"-Modus als
**dritter Zustand** (flache Liste ALLER Sub-FA-Zeilen, **nicht** die heutige AKE-Ansicht — Notiz
warnt explizit davor, das zu verwechseln).

**Out-of-Scope:** `ProductionOrders`/AKE unveraendert; keine Rueckmeldefunktion (Teil 8); keine
Kommissionier-/Beschichtungs-/Vormontage-spezifischen Filter (Teil 3–5, eigene Views).

## Fachliche Anforderungen

- Baum-Wurzel: `VaterFA IS NULL`. Kinder: `VaterFA = <BelID des Elternteils>`, rekursiv bis keine
  weiteren Kinder mehr existieren.
- Blaetter (`SubFA = 0`) werden im Baum als Endknoten dargestellt (Kaufteil/Material), nicht
  weiter aufklappbar.
- Toggle `ProduktionsauftragBaumAnzeige` (Default `false`): aus ⇒ flache Liste **aller**
  `FaHierarchyNode`-Zeilen (Warnung: mehr Zeilen als die heutige AKE-Liste, kein Rueckfall-Modus);
  an ⇒ rekursiver Baum.
- Filterung: ein Treffer in einer tiefen Ebene klappt den Pfad bis zur Wurzel automatisch auf
  (Auto-Expand), ohne Geschwisterknoten zu verbergen, die selbst nicht matchen.

## Technischer Loesungsentwurf

- Liest ausschliesslich ueber `IFaHierarchyNodeRepository` (Teil 1, Cache-Decorator) — kein
  direkter DB-Zugriff im Controller.
- Server-seitiges Paging ueber Struktur-Gruppen (eine „Seite" = N Haupt-FA-Strukturen samt aller
  Unterzeilen), nicht ueber Roh-Zeilen — Abweichung vom Standard-Listen-View-Pattern (ADR 0005
  geht von Zeilen-Pagination aus). Das genaue Verfahren (Phantom-Header bei abgeschnittenen
  Strukturen, Zeilenzahl-Schaetzung) ist als riskantester Teil bewusst noch nicht im Detail
  ausgearbeitet — siehe offene Rueckfrage 1.
- Spaltenfilter (ADR 0005) gelten weiterhin serverseitig auf den sichtbaren Attributen; Filter
  vor Pagination, `TotalCount` aus gefilterter Menge — Umsetzung im Baum-Kontext ist Teil der
  offenen Rueckfrage 1.

## Migrations-/SQL-Auswirkungen

Keine — reine Lesefunktion auf der in Teil 1 angelegten Tabelle. Falls der Toggle
`ProduktionsauftragBaumAnzeige` als neuer `AppSettingKeys`-Eintrag umgesetzt wird (siehe offene
Rueckfrage 2), ist das ein reiner Daten-Seed (`AppSettings`-Tabelle, kein Schema-Update, siehe
Fallstrick „`AppSettings` ist kein `AuditableEntity`").

## Audit-Feld-Auswirkungen

Keine neuen Entitaeten mit Audit-Pflicht. Reine Anzeige.

## Akzeptanzkriterien

1. Bei `ProduktionsauftragBaumAnzeige = false` erscheint die flache Liste aller
   `FaHierarchyNode`-Zeilen (mit sichtbarem Hinweis, dass dies NICHT die heutige AKE-Ansicht ist).
2. Bei `true` wird jede Struktur als rekursiver Baum dargestellt; ein Sub-Sub-FA (dritte Ebene)
   ist sichtbar und korrekt unter seinem Sub-FA-Elternteil eingeordnet.
3. Ein Spaltenfilter-Treffer auf einer tiefen Ebene klappt den Pfad zur Wurzel automatisch auf.
4. Pagination liefert bei grossen Datenmengen keine „stillen" Caps — ein sichtbarer Hinweis analog
   `IsCappedAtAll`, falls eine Grenze greift.
5. AKE-Verhalten (bestehende FA-Liste) bleibt unveraendert (harte Akzeptanzbedingung fuer jeden
   Teil).

## Test-Szenarien

Neues Kapitel „IDEAL Teil 2 — Baumanzeige" in `docs/TESTSZENARIEN.md`: Toggle aus/an vergleichen;
dreistufige Test-Struktur (Wurzel → Sub-FA → Sub-Sub-FA) korrekt verschachtelt darstellen; Filter
auf tiefer Ebene loest Auto-Expand aus; grosse Struktur (viele Positionen) prueft Paging-Verhalten
ohne Datenverlust. Nach Klaerung von offener Rueckfrage 1 zu praezisieren.

## Deploy

- **Web-App:** ja (neue Controller/Views/JS).
- **Service:** nein.
- **Migration:** nein (ausser AppSetting-Seed, kein Schema-Update).
- **Publish-Befehle:** `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`
  (provisorisch, vom Dev-Lauf zu bestaetigen).

## Offene Rueckfragen

1. Genaues UX-/Technik-Konzept fuer Paging ueber Gruppen statt Zeilen, Phantom-Header bei
   abgeschnittenen Strukturen, Auto-Expand-Algorithmus — laut Notiz der riskanteste Teil, bewusst
   nicht vorentschieden.
2. Toggle-Heimat `ProduktionsauftragBaumAnzeige`: `AppSettings` oder `ServiceSettings`?
3. Rollen/Zugriff fuer diese Ansicht.
4. Exakte Umsetzung des „Baum aus"-Modus (flache Liste aller Sub-FAs) — reicht Teil 1s
   Rohtabellen-Sicht oder braucht es eine eigene, aufbereitete Liste?

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

1. →
2. →
3. →
4. →
