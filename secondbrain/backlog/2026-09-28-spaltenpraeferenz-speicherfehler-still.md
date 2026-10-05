---
typ: bug
---
# Spaltenauswahl (Zahnrad): fehlgeschlagenes Speichern landet nur in der Konsole

**Aufgenommen 2026-09-28.** Herausgeloest aus der Kritischen Pruefung von
[[2026-09-25-kommissionierliste-summierung-pdf-spec]] (Befund S3), dort bewusst **nicht** mitgeloest —
vorbestehend und betrifft **alle** Listen mit Spaltenauswahl, nicht nur die Kommissionierliste.

## Befund (am Code, Buendel-Worktree `feature/2026-08-07-ideal-teile-1-5`)

`wwwroot/js/column-preferences.js` speichert jede Zahnrad-Aenderung automatisch — entprellt
(`SAVE_DELAY = 1500`, `scheduleSave`) per `PUT` an `/api/userviewpreferences`. Schlaegt das Speichern
fehl, passiert **nur** `console.warn('[column-preferences] save failed:', err)` (`saveSettings`).
Dasselbe gilt fuer das Zuruecksetzen (`resetToDefault`, `DELETE`).

Folge: Der Anwender sieht seine Spaltenauswahl am Bildschirm, gespeichert ist sie aber nicht. Beim
naechsten Laden ist sie weg — **ohne jede Meldung**. Serverseitige Verbraucher der gespeicherten
Praeferenz (Druckansichten mit Server-Rueckfall, z. B. `WarehousePickingPrintLayout`) sehen den alten
Stand.

**Haeufigste bekannte Ursache:** ein `viewKey`, der nicht in `ColumnDefinitions.GetByViewKey`
registriert ist → API antwortet **400** ([[fallstricke]] §3, „Neuer viewKey ohne
GetByViewKey-Registrierung"). So war es z. B. bei `FaHierarchyKommissionierSummiert` (Nebenbefund D der
genannten Spec) und frueher bei `FaWorklist`.

**Schaerfer als der Titel (am Code belegt 2026-09-28):** `fetch` wirft bei HTTP 400/500 **nicht**, und
`saveSettings` prueft `response.ok` **nicht** (nur `.catch`, `column-preferences.js` ~Z.305-314). Die
Konsole sieht also nur Netzwerkfehler. Der haeufigste Fall — **400 bei unregistriertem viewKey** — ist
heute **komplett unsichtbar**, auch in der Konsole.

## Anforderung (Hausregel „melden statt still behandeln")

Scheitert das Speichern oder Zuruecksetzen der Spaltenauswahl, sieht der Anwender das — z. B. ein
kurzer Hinweis am Zahnrad/an der Liste („Spaltenauswahl konnte nicht gespeichert werden"). Kein Modal,
kein Blockieren der Liste.

## Offene Fragen

1. Form des Hinweises: Toast, Inline-Hinweis am Zahnrad, oder Badge? (Konsistenz mit bestehenden
   Mustern der Anwendung pruefen.)
2. Soll ein Drift-Guard-Test sicherstellen, dass **jeder** `data-view-key` in den Views in
   `ColumnDefinitions.GetByViewKey` registriert ist? Das wuerde die haeufigste Ursache strukturell
   ausschliessen.

## Bezug

[[2026-09-25-kommissionierliste-summierung-pdf-spec]] (Kritische Pruefung S3, Antwort B1) ·
[[0005-listen-view-pattern-mit-server-side-spaltenfilter]] (Spaltenpraeferenzen) · [[fallstricke]] §3
