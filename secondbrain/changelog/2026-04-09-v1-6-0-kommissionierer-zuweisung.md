---
type: changelog
version: 1.6.0
date: 2026-04-09
---
# v1.6.0 — Kommissionierer-Zuweisung

- Bei der Leitstand-Freigabe kann ein Kommissionierer zugewiesen werden; dieser sieht in seiner
  Picking-Liste per Default nur die ihm zugewiesenen Auftraege („Alle anzeigen" hebt das auf).
- Neues Benutzermerkmal „Ist Kommissionierer" — nur so markierte Benutzer stehen im
  Zuweisung-Dropdown.
- Feature-Toggle `KommissionierungMitZuweisung`; Voraussetzung ist zusaetzlich `LeitstandAktiv`
  (kumulative Gates) → [[0011-feature-toggles-ueber-appsettings]].
- Neue Spalten `ProductionOrders.AssignedPickerId`, `AppUsers.IsPickingUser` (`SQL/38`).

> **Hinweis zur Quelle:** `PROJECT_STATUS.md` fuehrte dieses Feature faelschlich unter „v1.4.0".
> Maßgeblich ist die anwendersichtbare Changelog-View (`Views/Help/Changelog.cshtml`), die es als
> v1.6.0 (09.04.2026) ausweist; v1.4.0 sind die Artikelkategorien/-merkmale.
