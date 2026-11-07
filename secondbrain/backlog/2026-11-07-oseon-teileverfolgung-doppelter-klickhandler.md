---
typ: bug
created: 2026-11-07
quelle: "[[2026-11-07-bestandsuebersicht-qr-fa-uebernahme-fest-spec]]"
---
# OSEON-Teileverfolgung: doppelter Klickhandler am Artikel-QR-Knopf (nur AKE)

Zusatzfund aus [[2026-11-07-bestandsuebersicht-qr-fa-uebernahme-fest-spec]] (Antwort 4: eigener Backlog-Punkt, Gerald 2026-11-07).
Der Knopf heißt `btnScanArticle` (`Views/…/OseonIndex.cshtml:26`); dadurch hängt der globale Start `barcode-scanner.js:593` einen Handler für das
dort nicht vorhandene `ArticleId` an – zusätzlich zu `OseonIndex.cshtml:312`. Ein Klick öffnet das Kamera-Fenster vermutlich zweimal; welches
bleibt, hängt vom Timing ab. **Ungeprüft zur Laufzeit** – zuerst am AKE-Test im Browser nachstellen (Konsole, Anzahl Fenster).
