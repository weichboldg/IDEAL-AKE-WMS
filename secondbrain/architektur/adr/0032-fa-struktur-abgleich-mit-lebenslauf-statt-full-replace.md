---
type: adr
id: 0032
title: FA-Struktur als Abgleich mit Lebenslauf statt Full-Replace
status: accepted
date: 2026-11-17
supersedes: ""
superseded_by: ""
---

> Spec [[2026-11-17-fa-struktur-abgleich-statt-full-replace-spec]] (v1.99.37, SQL/159, TS-158; Freigabe-Antworten 1–4). Ergänzt [[0029-fa-struktur-ideal-knoten-aus-sdata-fainfos-aus-view-hybrid]].
> [[0030-teilmeldung-mit-fachlichem-schluessel-neben-workoperation]] bleibt gültig. Bündel `feature/2026-08-07-ideal-teile-1-5`, Commits `7a1f7301` `94e327e7` `026d0d32` `354344d8` `8ea8e98d` `2abad047`+`b1f83b1a` `0963a0a5` `7fa7603d`+`96049d0d`.

## Kontext und Problem

- Der Lauf „FA-Hierarchie“ löschte bisher je Lauf alle Cache-Zeilen (`FaHierarchyNodes`, `FaHierarchyOrderInfos`, `ProductionOrderPositions`)
  und legte sie neu an. Eine in Sage beendete FA verschwand damit sofort aus FA-Struktur, Kommissionierliste und BDE-Teileliste, obwohl im WMS
  noch Arbeit daran hing (R2 aus [[2026-11-16-fa-struktur-ideal-ueber-sdata-spec]]).
- Ein leerer oder halber Read hätte den ganzen Bestand gelöscht.

## Entscheidung

- **Abgleich über den fachlichen Schlüssel:** Knoten `(HauptFA, VaterFA, SubFA, Position, Artnr)`, OrderInfos `(HauptFA, ABNr, Pos)`,
  Positionen über ihren bisherigen Schlüssel. UNIQUE-Indizes `UX_FaHierarchyNodes_Schluessel`/`UX_FaHierarchyOrderInfos_Schluessel` (SQL/159).
  Planer rein in `IDEALAKEWMSService/Services/FaStrukturAbgleich.cs`; neu / geändert / unverändert / wieder da / verschwunden.
- **Lebenslauf je Zeile:** `ErstGesehen`, `ZuletztGesehen`, `VerschwundenSeit`, `VerschwundenGrund` (`beendet`/`inaktiv`/`unklar`).
- **Verschwundene Zeile:** Sage wird gefragt (`KHKPpsFaBelege`, `BelID, IstBeendet, Aktiv`, Blöcke zu 1000). Beendet/inaktiv → behalten mit
  Grund; offen+aktiv → `unklar` + Fehlermail; in Sage nicht gefunden → entfernen; Position aus der Stückliste weg (FA noch geliefert) → entfernen.
  Sage-Abfrage fehlgeschlagen → nichts markieren, nichts entfernen.
- **Wächter:** leerer Read und „alle bisherigen HauptFA fehlen“ → nichts ändern, Mail; doppelter Schlüssel im Read → diese HauptFA sperren, Mail.
- **Aufräumen:** eine vollständig verschwundene HauptFA wird entfernt, wenn alle `ProductionOrders` mit `OrderNumber == HauptFA` erledigt oder
  storniert sind, sonst nach `Sync:FaStrukturAufbewahrungTage` (Default 90, 1–3650).
- **Konsumenten:** Materialisierung und Arbeitsgang-Erkennung lesen nur aktive Knoten. Arbeitslisten zeigen verschwundene Zeilen mit
  Text-Schild (`_FaStrukturBadge`), die BDE-Teileliste lässt sie nicht melden (Freigabe-Antwort 3).
- `FaHierarchyNode.Id` bleibt jetzt stabil, ist aber weiterhin **kein** fachlicher Schlüssel (ADR 0030).

## Verworfene Alternativen

- Full-Replace mit Ausnahmeliste für beendete FAs: hätte weiter Ids getauscht und den Leer-Read nicht abgefangen.
- Soft-Delete ohne Sage-Rückfrage: hätte gelöschte Positionen und echte Lesefehler nicht unterschieden.

## Folgen

- Einmal-Skripte und Ad-hoc-Abfragen sehen jetzt auch verschwundene Zeilen – nach `VerschwundenSeit IS NULL` filtern ([[fallstricke]] Nr. 48).
- Die Sage-Abfrage filtert noch **nicht nach Mandant** (P-AB4) – Blocker vor Prod an IDEAL.
- Druckansichten zeigen kein Schild (bewusst offen, Koordination).
