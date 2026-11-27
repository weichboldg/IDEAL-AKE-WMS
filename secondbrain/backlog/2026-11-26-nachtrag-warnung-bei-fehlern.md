---
typ: feature
status: offen
created: 2026-11-26
herkunft: "Besprechung 2026-11-26, Entscheid 18 (H-7/F-3 A), Ziel v1.99.82"
standort: beide
---
# Kommissionier-Nachtrag: Warnung bei `fehler > 0` (H-7/F-3, v1.99.82)

Bezug: [[2026-11-26-loesungsvorschlaege-deploy-sage-doku]] (H-7 / F-3), [[2026-11-26-besprechung-0830-kritische-pruefung]] (Block 4, Entscheid 18),
[[2026-11-25-kommissionierauftraege-nachtrag-automatisch-knopf-nur-admin]].

- Der Tages-Marker bleibt wie er ist (auch bei Totalausfall gesetzt – sonst läuft der Lauf jeden Zyklus erneut).
- Neu: endet der Nachtrag mit `fehler > 0`, schreibt er eine sichtbare Warnung ins Aktivitäts-Protokoll (Status Warnung, Count „Fehler“) und –
  sofern die Fehlermail des Dienstes konfiguriert ist – eine Mail. Ziel: ein ausgefallener Tag ist sichtbar.
- H-7 (Marker hängt am Schreiben von „Run gestartet“) dabei mit prüfen: scheitert der SyncLog-Eintrag, wiederholt sich der Lauf (idempotent, teuer).
