---
typ: bug
---
# Bestandsübersicht übernimmt die FA aus dem QR-Code immer (QrMitFaNummer wirkt dort nicht)

**Aufgenommen 2026-10-27** (Gerald, Antwort 4 der Spec [[2026-10-23-qr-code-matchcode-statt-artikelnummer-spec]]): so lassen,
eigener Backlog-Punkt.

## Befund (spec-agent, Bündel v1.67.1)

- Die Bestandsübersicht übergibt die FA-Übernahme fest mit `true`; die Einstellung `QrMitFaNummer` wirkt dort nicht.
- Beim Sammel-Wareneingang wird der Wert übergeben, aber in der Seite nie gelesen.
- Auf Stückliste, Artikelinfo, FA-Liste, Leitstand und OSEON greift `QrMitFaNummer` nie.

## Offen

- Soll `QrMitFaNummer` überall gelten, wo ein Artikel-QR gescannt wird? Fundstellen beim /spec mit Datei:Zeile belegen.
