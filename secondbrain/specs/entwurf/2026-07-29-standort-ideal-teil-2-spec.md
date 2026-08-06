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

1. →wenn hierarchisch dargestellt, alle zur hierarchie gehörenden aufträge anzeigen, bei flacher liste kann gern nach 25 gewechselt werden
2. →ProduktionsauftragBaumAnzeige betrifft eher die appsettings als den service
3. →keine neue rollen, alles wie gehabt
4. →hierarchische darstellung kann im nächsten step erfolgen

## Kritische Pruefung (2026-08-06)

Anwalt-des-Teufels-Durchsicht VOR der Freigabe. Gegengelesen: diese Teil-2-Spec, die Teil-1-Spec
(Datenquelle `FaHierarchyNode`/`FaHierarchyOrderInfo`), die Uebersichts-Spec, die Ideen-Notiz inkl.
„Kritische Pruefung", der Anhang [[sage-views-ideal]], ADR 0005 (Listen-View) und ADR 0006 (Rollen)
sowie der reale Code (`ReadOnlyBomBuilder`/`BomViewModels` als einzige bestehende „Baum"-Sicht,
`RoleKeys`, `AppSettingKeys`, `controller.md`).

**Kern-Beobachtung vorweg:** Die Freigabe-Antworten wurden eingetragen, **aber der Spec-Rumpf
(In-Scope, Technischer Loesungsentwurf, Akzeptanzkriterien, Offene Rueckfragen) ist unveraendert
stale**. Die AKs und die vier „Offenen Rueckfragen" beschreiben noch den Zustand VOR den Antworten.
Ein `/dev`-Lauf auf diesem Text baut gegen einen widerspruechlichen Auftrag.

### BLOCKER — vor der Freigabe/dem Dev-Lauf zu klaeren

**B-1 — Freigabe-Antwort 4 stellt den GESAMTEN In-Scope in Frage (teuerster Fehler).**
Die In-Scope-, Anforderungs- und AK-Bloecke drehen sich zentral um den **rekursiven Baum**
(Paging ueber Gruppen, Phantom-Header, Auto-Expand) — den die Spec selbst als „riskantesten Teil"
bezeichnet. Antwort 4 sagt aber woertlich: „**hierarchische darstellung kann im naechsten step
erfolgen**". Das deutet auf ein **Descoping der Baumdarstellung** hin: zuerst die flache Liste,
Baum spaeter. Zusaetzlich kollidiert Antwort 1 („bei flacher liste kann gern nach 25 gewechselt
werden") mit AK 1, die die flache Liste als „**alle** `FaHierarchyNode`-Zeilen" definiert (alles
vs. paginiert 25). Bevor irgendein Code entsteht, muss entschieden werden: liefert Teil 2 jetzt
**nur die flache, paginierte Liste** (und der Baum wird eigener Folge-Teil), oder den vollen Baum?
Ohne diese Entscheidung baut der Dev-Lauf mit hoher Wahrscheinlichkeit genau die aufwaendige
rekursive Paging-Mechanik, die der Mensch gerade verschoben hat.

**B-2 — Erste erreichbare IDEAL-Route ohne benannten Access-Filter.**
Teil 1 hatte bewusst keine Route; Teil 2 fuegt die **erste** hinzu. Antwort 3 („keine neue Rollen,
alles wie gehabt") ist **kein Filtername**. ADR 0006 verlangt einen Class-Level-Read-Filter; ein
Controller **ohne** `RequireXxxAccess`-Attribut ist fuer **jeden eingeloggten Benutzer** erreichbar
— das ist eine stille Zugriffsentscheidung, kein „wie gehabt". Der konkrete bestehende Filter muss
benannt werden (Kandidat analog zur slim FA-Liste: `[RequirePickingOrTrackingOrLeitstandAccess]`
auf `ProductionOrdersController`; oder `picking`/`vorbau`). Read-only-View ⇒ Read-Filter genuegt,
kein Edit-Split noetig — aber der Name gehoert in die Spec, nicht in den Dev-Lauf.

**B-3 — Rekursion ohne Zyklen-/Tiefenschutz = Produktions-Endlosschleife.**
Die Anforderung lautet „rekursiv bis keine weiteren Kinder". Es gibt **keinen** Visited-Set-, kein
Max-Tiefen-Guard. Eine defekte `VaterFA`-Kette (Zyklus, oder `VaterFA` zeigt im Kreis) fuehrt zu
Stack-Overflow/Hänger — und die Ideen-Notiz behandelt genau eine geaenderte/kaputte `VaterFA`
(Umhaengung) explizit als **real moegliche Invarianz-Verletzung, nicht als Annahme**. Wichtig:
**es gibt keinen wiederverwendbaren Praezedenzfall** — die einzige bestehende „Baum"-Sicht
(`ReadOnlyBomBuilder`) leitet die Ebene aus der **Positions-Zeichenkette** (Anzahl Punkte in „15.1")
ab, **nicht** aus einer Zeiger-Rekursion; sie kann also gar nicht zyklen-sicher sein. Zyklenschutz +
Tiefen-Cap muessen als AK spezifiziert werden.

**B-4 — Die Kern-Mechanik ist Prosa, kein Spec.**
Der Technische Loesungsentwurf gibt selbst zu: Paging ueber Gruppen, Phantom-Header bei
abgeschnittenen Strukturen, Zeilenzahl-Schaetzung und Auto-Expand-Algorithmus sind „**bewusst noch
nicht im Detail ausgearbeitet**" (Offene Rueckfrage 1). Damit ist der eigentliche Inhalt von Teil 2
nicht implementierbar. Antwort 1 loest das nur teilweise (im Baum „alle zur Hierarchie gehoerenden
Auftraege" — was faktisch **kein Paging innerhalb einer Struktur** bedeutet, aber die Frage nach
einer Obergrenze fuer die Zahl der Strukturen pro Seite offen laesst). Solange dieser Teil offen
ist, ist die Spec fuer den Baum-Anteil **nicht freigabereif** (fuer die flache Liste ggf. schon —
siehe B-1/S-4).

### SOLLTE — macht den Dev-Lauf sicherer

**S-1 — Widerspruch zu ADR 0005 (Baum ist dort ausdruecklich Ausnahme).**
Die Spec behauptet „Spaltenfilter (ADR 0005) gelten weiterhin serverseitig". ADR 0005 listet
hierarchische Baumdarstellungen (namentlich **BOM-Tree**) aber als **begruendete Ausnahme** vom
Pattern. Server-Mode-Spaltenfilter sind mit Rekursion **und** Paging-ueber-Gruppen kaum vereinbar.
Entscheiden: Client-Mode (wie BOM-Tree) oder echter Server-Mode? Zusatz: die
Datums-in-C#-nach-Termin-Berechnung-Regel passt nicht, weil IDEAL-Termine **gespeichert** aus der
PPS-View kommen (`FE_Termin` etc.), nicht berechnet werden — und diese Felder liegen auf
`FaHierarchyOrderInfo`, das die Spec gar nicht liest (siehe S-2).

**S-2 — `FaHierarchyOrderInfo` fehlt in der Anzeige komplett.**
Der Baum/die Liste liest ausschliesslich `FaHierarchyNode` (nur `IFaHierarchyNodeRepository` im
Technischen Loesungsentwurf). Woher kommen die Auftrags-Kopfdaten (Kunde, Termine, Status), die den
ganzen INNER-JOIN-Sinn aus Teil 1 tragen? Entweder sind sie Out-of-Scope (dann explizit sagen) oder
`IFaHierarchyOrderInfoRepository` + Join gehoeren in den Loesungsentwurf.

**S-3 — Verwaiste Knoten verschwinden lautlos (verletzt AK 4).**
Baut man den Baum aus den `VaterFA IS NULL`-Wurzeln, wird eine Zeile, deren `VaterFA` auf einen
**nicht importierten** Sub-FA zeigt (z. B. weil dessen Struktur durch die INNER-JOIN-Regel oder
einen Teil-Import wegfiel), von **keiner** Wurzel erreicht → sie faellt still aus der Anzeige. AK 4
verspricht „keine stillen Caps/Datenverluste", aber nichts erkennt Waisen. Orphan-Detection als AK
ergaenzen.

**S-4 — Zu gross fuer einen sauberen Dev-Lauf; Split empfohlen.**
Rekursiver Baum + Gruppen-Paging + Phantom-Header + Auto-Expand + flacher Dritt-Zustand +
Spaltenfilter + neuer Controller/View/JS + AppSetting-Seed ist zu viel auf einmal — und der
riskanteste Teil ist unspezifiziert. Deckt sich mit Antwort 4. Empfehlung: **Teil 2a** (flache,
paginierte Liste — sofort lieferbar) / **Teil 2b** (rekursiver Baum — naechster Step).

**S-5 — AppSetting-Seed nicht in `affected_code`.**
Die Migrations-Sektion nennt einen „reinen Daten-Seed (`AppSettings`-Tabelle)", aber `affected_code`
listet nur `AppSettingKeys.cs` — **kein** `SQL/xx`-Seed-Skript und **kein** `00_FreshInstall.sql`.
Klaeren: braucht `ProduktionsauftragBaumAnzeige` eine DB-Seed-Zeile (dann Skript benennen) oder
defaultet der Key im Code ohne Zeile (dann so sagen)?

### HINWEIS

**H-1 — Der markierte „teuerste Fehler" (Toggle-Baum vs. B5) ist faktisch aufgeloest — aber nur
durch die Architektur, nicht durch eine ausdrueckliche Aussage.** Teil 2 liest `FaHierarchyNode`
(von Teil 1s `Sync:HierarchicalFaEnabled` befuellt) und beruehrt **weder** `ProductionOrders` **noch**
den Master `ProduktionsauftragHierarchisch`. Damit ist die Baumanzeige **unabhaengig vom
(noch nicht existenten) Master** und tatsaechlich VOR Teil 7 lieferbar — genau wie B5 verlangt und
die Uebersicht (Offene Rueckfrage 2) bestaetigt. Der Schalterbaum der Ideen-Notiz
(„nur wirksam wenn Master an") ist ueberholt. **Ein Satz in der Spec, der das explizit festhaelt,
verhindert, dass ein Dev den Toggle faelschlich an den Master haengt.**

**H-2 — View-Ordner-Mismatch.** `affected_code` nennt `Views/FaHierarchyNode/Index.cshtml`, der
Controller heisst aber `FaHierarchyController` → MVC sucht in `Views/FaHierarchy/`. Angleichen.

**H-3 — Toggle-Namensfamilie.** `ProduktionsauftragBaumAnzeige` (deutsch, `ProduktionsauftragHierarchisch`-
Familie) passt zur bestehenden `AppSettingKeys`-Konvention (deutsche PascalCase-Keys), weicht aber
von Teil 1s englischen Konzeptnamen (`Sync:HierarchicalFaEnabled`) ab. Bewusst so waehlen und den
Key in `AppSettingKeys.cs` eintragen (Datei ist in `affected_code`, gut).

**H-4 — Spec-Rumpf an die Freigabe-Antworten angleichen.** Die vier „Offenen Rueckfragen" und die
AKs muessen nach den Antworten neu geschrieben werden (Antwort 2 ⇒ AppSettings ist entschieden;
Antwort 1 ⇒ Paging-Regel; Antwort 3 ⇒ Filtername; Antwort 4 ⇒ Scope). Aktuell steht der
Vor-Antwort-Zustand — Verwechslungsgefahr im Dev-Lauf.

NACHBESSERUNG NOETIG: Scope nach Antwort 4 klaeren (flache Liste jetzt vs. Baum spaeter, B-1),
Access-Filter benennen (B-2), Zyklen-/Tiefenschutz spezifizieren (B-3) und die Paging-/Auto-Expand-
Mechanik ausarbeiten oder den Baum-Anteil abspalten (B-4). Die Architektur (eigene Tabelle,
Master-Unabhaengigkeit) traegt — die Luecken sind eine Scope- und drei Design-Entscheidungen.
