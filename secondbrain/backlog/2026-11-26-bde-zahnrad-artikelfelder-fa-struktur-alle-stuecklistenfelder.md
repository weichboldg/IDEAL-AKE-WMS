---
type: backlog
status: offen
created: 2026-11-26
quelle: Walter (IDEAL), weitergeleitet von Gerald im Chat 2026-11-26; Prüfung gegen idealweb01:88 (v1.99.81)
---
# BDE-Zahnrad: Verwendungsart / Ist Lagerartikel / Teileart · FA-Struktur: alle Stücklisten-Eigenschaftsfelder

## Wunsch (Walter)
1. Zahnrad aller BDE-Ansichten (Einzeln, Summiert, FA) um drei Eigenschaften ergänzen:
   Verwendungsart (`KHKArtikel_USER_OSVerwendungsart`), Ist Lagerartikel (`KHKArtikel_USER_OSIstLagerartikel`),
   Teileart (`KHKArtikel_User_OSArtikeltyp`).
2. FA-Struktur: alle Eigenschaftsfelder der Stückliste zur Auswahl.

## Befund (2026-11-26, nur gelesen)
- BDE-Teileliste: Spalten fest im Code (`BdeTeilelisteService.SpaltenEinzeln`/`SpaltenZeile`, `Views/BdeTeileliste/*`), keine der drei.
- WMS-Artikelmerkmale (`IDEAL_AKE_WMS_TEST`, Sage-Quelle): `USER_OSIstLagerartikel` (1287 Werte) und `USER_OSArtikeltyp` (1286) vorhanden;
  **`USER_OSVerwendungsart` nicht**.
- Sage `IDEAL` (idealsql2\SAGE100TEST): `KHKArtikel` hat **keine** Spalte `USER_OSVerwendungsart` (nur `USER_ELVerwendung`);
  `USER_OSVerwendungsart` gibt es an Stückliste/FA (`KHKPpsRessourcenkopf`, `KHKPpsRessourcenPositionen`, `KHKPpsFaBelege`,
  `KHKPpsFaBelegePositionen`, `KHKPpsStlImport*`). → Rückfrage Walter: Artikel- oder Stücklistenfeld? „KHKArtikel_…“ evtl. SData-Name.
- FA-Struktur (`Views/FaHierarchy/Index.cshtml`) bietet 9 Spalten (description, matchcode, operations, picking-target, quantity, status,
  structure, subfa, workarea). Sage `KHKPpsFaBelegePositionen` hat 22 `USER_`-Felder; abgeglichen werden 13 (`SageUserfelder` FaStruktur).
  Nicht abgeglichen: `USER_OSVerwendungsart`, `USER_OSBlechdicke`, `USER_OSMaterialnummerPN`, `USER_OSAnzPN`, `USER_OSBedarfPN`,
  `USER_OSExistenzPN`, `USER_DPSBSBereitstellungHLager`, `USER_DPSBSBereitstellungMA`, `USER_DPSBSGruppierung`.

## Rückfragen
- Verwendungsart: aus dem Artikel oder aus der Stücklisten-/FA-Position?
- „Alle Eigenschaftsfelder“: auch die DPS-Felder (`USER_DPSBS…`)?
- Teileart = `USER_OSArtikeltyp`: in der BDE aus dem Artikelmerkmal oder aus der FA-Position (beide vorhanden)?
