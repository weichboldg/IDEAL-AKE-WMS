---
type: backlog
status: offen
created: 2026-11-26
quelle: Walter (IDEAL), weitergeleitet von Gerald im Chat 2026-11-26; Prüfung gegen idealweb01:88 (v1.99.81)
---
# BDE-Zahnrad: Verwendungsart, Ist Lagerartikel, Teileart

## Wunsch (Walter)
Zahnrad aller BDE-Ansichten (Einzeln, Summiert, FA) um drei Eigenschaften ergänzen:
Verwendungsart (`KHKArtikel_USER_OSVerwendungsart`), Ist Lagerartikel (`KHKArtikel_USER_OSIstLagerartikel`),
Teileart (`KHKArtikel_User_OSArtikeltyp`).

**Nicht hier:** FA-Struktur „alle Stücklisten-Eigenschaftsfelder“ – überarbeitet Gerald anderswo (Chat 2026-11-26).

## Befund (2026-11-26, nur gelesen)
- BDE-Teileliste: Spalten fest im Code (`BdeTeilelisteService.SpaltenEinzeln`/`SpaltenZeile`/`SpaltenSummiert`, `Views/BdeTeileliste/*`), keine der drei.
- WMS-Artikelmerkmale (`IDEAL_AKE_WMS_TEST`, Sage-Quelle, `ArticleAttributeDefinitions.SyncFieldName`): `USER_OSIstLagerartikel` (1287 Werte)
  und `USER_OSArtikeltyp` (1286) vorhanden; **`USER_OSVerwendungsart` nicht**.
- Sage `IDEAL` (idealsql2\SAGE100TEST): `KHKArtikel` hat **keine** Spalte `USER_OSVerwendungsart` (nur `USER_ELVerwendung`);
  `USER_OSVerwendungsart` gibt es an `KHKPpsRessourcenkopf`, `KHKPpsRessourcenPositionen`, `KHKPpsFaBelege`, `KHKPpsFaBelegePositionen`,
  `KHKPpsStlImport*`. Die FA-Struktur gleicht 13 FA-Positionsfelder ab (`SageUserfelder` FaStruktur), Verwendungsart nicht.
- Lagerbestell-Zeilen der Teileliste sind Artikel-Projektionen ohne FA-Position (HauptFA 0).

## Rückfragen
- Verwendungsart: Feld der Stückliste/FA-Position (`KHKPpsFaBelegePositionen.USER_OSVerwendungsart`)? Am Artikel gibt es es nicht.
- Teileart = `USER_OSArtikeltyp`: aus dem Artikelmerkmal oder aus der FA-Position (beide vorhanden)?
- „FA“-Ansicht = FA-Sicht der BDE-Teileliste (`Fa.cshtml`)?
