---
type: changelog
version: 1.49.0
date: 2026-10-09
---
# v1.49.0 — IDEAL: Briefkopf auf dem Beschichtungsauftrag

Beschichtungsauftrag-Ausbau **Teil 2** ([[2026-10-06-beschichtungsauftrag-ausbau-teil-2-spec]], Übersicht
[[2026-10-06-beschichtungsauftrag-ausbau-uebersicht]]). **Direkt im Bündel** `feature/2026-08-07-ideal-teile-1-5`
(Entscheidung 2026-10-09), Start `cc86d373` (v1.48.0), Commits `1c560d30`, `1cda91dc`, `742f7855`, QA-Fix `020bf8af` (unausgeglichenes Markup ⇒ Ersatz-Kopf). **Keine Migration.**
Umsetzung [[2026-10-09-beschichtungsauftrag-ausbau-teil-2-umsetzung]].

**Warum:** Der Beschichtungsauftrag ist das einzige Dokument, das das Haus verlässt (an den externen Beschichter) —
bisher ohne Absender, Logo und Empfängerfeld.

## Kernpunkte

- Briefkopf aus einer HTML-Datei der IT (`Branding\briefkopf.html`, neben dem Publish-Ordner), Pfad in der neuen
  Einstellung „Briefkopf-Vorlage (Pfad)" (Standorteinstellungen → Firmendaten, nur Admin). Bilder und Schriften aus
  demselben Ordner als Data-URI eingebettet — Bildschirmdruck und Headless-Edge-PDF identisch.
- Schlanke eigene Bereinigung (Antwort 11, `ponytail:`-Grenze im Code), kein SVG, keine fremden/absoluten Verweise,
  Grenzen 256 KB / 1 MB / 3 MB als Konstanten, kein Cache (Antwort 10).
- Ersatz-Kopf aus Firmenname + Anschrift bei fehlender/unlesbarer/zu großer Vorlage; Hinweis am Bildschirm (nicht im
  Druck/PDF) + Log-Warnung; der Druck bricht nie ab.
- Empfänger = Dienstleister aus Sage, sonst sichtbar „Empfänger fehlt — in Sage nachpflegen" bzw. „mehrdeutig".
- Nur Seite 1 je HauptFA-Dokument, Einzel- und Summiert-Druck; keine Kommissionierlisten. „Seite X von Y" bewusst
  weggelassen ([[2026-10-09-briefkopf-seitenzaehlung]]).
- Muster `Branding/briefkopf.html` + Logo im Repo (CD-Satzspiegel, Adresse In der Schörihub 28, 4810 Gmunden),
  Schriftdateien nicht im Repo. TS-82.

## Deploy-Risiken

Nur Web-Publish. **Manuelle Einrichtung je Server:** Ordner `Branding\` neben dem Publish-Ordner, Muster + Logo +
lizenzierte Schriftdateien hinein, Leserecht App-Pool, Einstellung setzen. Ohne Einrichtung druckt der Ersatz-Kopf
(kein Fehler). Vor der Abnahme Firmenanschrift korrigieren (Tippfehler „38, 4840" → „28, 4810") und Prüfschritt TS-82.1.
