---
type: adr
id: 0006
title: Rollenkonzept — statische Rollen-Keys, Admin-Wildcard, ausschliesslich explizite Zuweisung
status: accepted
date: 2026-03-20
supersedes: ""
superseded_by: ""
---

> **Nachtraeglich erfasst** (2026-07-27). Bestandsdokumentation; Grundlage aus v1.0-Zeit
> (Spec `../../../docs/superpowers/specs/2026-03-20-rollenkonzept-design.md`), Read/Edit-Split in
> v1.20.0, Wegfall von `Role.AdGroup` in v1.23.0.

## Kontext und Problem

Zugriff muss nach Taetigkeit getrennt werden: Lager, Kommissionierung, Leitstand, Vorbau,
Stammdaten, BDE, Bestellwesen. Die Gruppen ueberlappen (ein Picker darf Bestand sehen, ein
Vorbau-Werker eine read-only Stueckliste). Anfangs wurde versucht, das an AD-Gruppen zu haengen
(`Role.AdGroup` → automatische Rollenzuweisung). Das erwies sich als unpraktikabel: AD-Gruppen
werden von der IT nach anderen Kriterien gepflegt, Aenderungen waren nicht nachvollziehbar, und
die Zuordnung war weder im WMS sichtbar noch dort korrigierbar.

## Betrachtete Optionen

- **Rollen aus AD-Gruppen ableiten** — keine Doppelpflege, aber Berechtigungen aendern sich
  ausserhalb der App unkontrolliert und ohne Audit.
- **Feingranulare Permissions (Claim pro Action)** — maximale Flexibilitaet, aber fuer ein Team
  dieser Groesse Overkill und in der UI kaum vermittelbar.
- **Feste Rollen-Keys, explizit pro Benutzer zugewiesen, Zugriff ueber Filter-Attribute** —
  wenig Freiheitsgrade, dafuer nachvollziehbar und im WMS pflegbar.

## Entscheidung

- Rollen sind **statische Keys** in `../../../IdealAkeWms/Models/RoleKeys.cs`, persistiert in
  `Role` + Junction `UserRole` (Many-to-Many).
- Zuweisung erfolgt **ausschliesslich explizit pro Benutzer**. `Role.AdGroup` und die
  AD-Gruppe→Rolle-Automatik sind in v1.23.0 entfallen (Spalte gedroppt). AD spielt nur noch beim
  *Anlegen* eines Benutzers eine Rolle (Picker liest die Mitglieder der
  `WindowsAuthBerechtigungsgruppe`) — nicht bei der Autorisierung.
- Der Key `admin` ist ein **Wildcard**: er ueberspringt alle Pruefungen.
- Durchgesetzt wird ueber **Filter-Attribute** in `../../../IdealAkeWms/Filters/`
  (`RequireXxxAccessAttribute`). Ueberlappende Bedarfe werden als **Composite-Filter**
  modelliert (`RequirePickingOrLeitstandAccess`, `RequireStockOrLagerbestellungAccess`, …) statt
  Rollen aufzuweichen.
- **Read/Edit-Split**: Class-Level traegt den Read-Filter, schreibende Actions verschaerfen mit
  dem Edit-Filter. ASP.NET kumuliert beide. Views blenden Buttons ueber
  `ICurrentUserService`-Checks aus (`masterdata_read`, `stock_read`).
- Feature-Gates (AppSetting-Schalter wie `LagerbestellungAktiv`, `BdeAktiv`) sind **kumulativ**
  zum Rollen-Filter, kein Ersatz — siehe [[0011-feature-toggles-ueber-appsettings]].

Die aktuelle Filter→Rollen→Controller-Matrix liegt in [[controller]]; die Rollen-Bedeutungen im
[[glossar]]. Fuer Endanwender gibt es zusaetzlich die **hand-gepflegte** Uebersicht
`/Users/RoleOverview`.

## Konsequenzen

**Positiv**
- Berechtigungen sind im WMS sichtbar, aenderbar und ueber die Audit-Felder nachvollziehbar.
- Zugriffsschutz ist deklarativ am Controller/Action lesbar.
- Neue Bedarfe kosten ein Composite-Filter, nicht ein Umbau des Modells.

**Negativ / Risiken**
- Doppelpflege gegenueber AD beim Onboarding (Benutzer muss im WMS angelegt werden).
- Die Zahl der Filter-Attribute waechst (aktuell 29) — Composite-Kombinatorik ist der Preis fuer
  feste Rollen.
- **Drei Stellen driften leicht auseinander**: Filter-Attribut, die Tabelle in [[controller]] und
  die hand-gepflegte View `Views/Users/RoleOverview.cshtml`. Bei jeder Filter-Aenderung sind alle
  drei nachzuziehen.
