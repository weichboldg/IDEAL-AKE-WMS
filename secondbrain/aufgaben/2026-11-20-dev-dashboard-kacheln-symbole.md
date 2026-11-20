---
type: aufgabe
status: erledigt
created: 2026-11-20
spec: "[[2026-11-20-bugfix-dashboard-kacheln-symbole-fehlen-spec]]"
---
# Dev-Lauf: Dashboard-Kacheln wieder mit Symbolen (v1.99.46, TS-174)

Bündel-Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, Zweig `feature/2026-08-07-ideal-teile-1-5`, Basis v1.99.45.
Reihenfolge laut Gerald 2026-11-20: Dashboard zuerst als 1.99.46, Teil 6 (noch nicht freigegeben) danach als 1.99.47.

## Stand
- [x] Katalog `SymbolPfad` + View + Wächter-Tests (`6e91fef2`)
- [x] Version, Changelog, TESTSZENARIEN TS-174, Doku Kapitel 18; Fotos 18-01/18-02/18-05 neu (QA `2af81077`), 18-03/18-04 unverändert (zeigen Menü/Fokus)
- [x] QA (qa-agent) auf DEV AKE + IDEAL: Testbereit. Befund Q1 behoben (`2f3e98cc`): Kommissionierlisten/Vorschlagsliste = leeres Blatt,
      Einstellungen = Stift statt der freigegebenen Tabelle (Quell-SVGs im Projekt unvollständig) – **Gerald bei TS-174.1 ansehen**
- [x] Publish (Commit `2af81077`), Paket-ZIP `paket11-v1.99.46.zip`, 236 878 221 Byte, SHA-256 `6A56528285422CF8CFF01DAEC992859FBF5EB2E7CF992FD7E2012051FAB3B9FC`
  - `\\AKENET01.ake.at\Installsourcen`: Größe + SHA-256 gleich
  - `\\IDEALWEB01.ideal.ideal-ake.at\InstallSourcen`: Größe + SHA-256 gleich
  - lokales `publish\` danach geleert (paket8–11 liegen auf beiden Shares)
- [x] Brain: Changelog, feature-map, fallstricke §58, Testindex. Bug-Record bleibt „offen“ bis zum Merge (QA-Entscheid).

## Confluence (für spätere Übernahme)
- Anwender Kapitel 18 „Startseite und Menü“ (Stand 1.99.46, Schritt 3, Hinweis Symbole, Fotos 18-01/18-02/18-05)
- Anwender README „Stand und was noch folgt“ (Zeile 1.99.46)
- Betrieb 00-index (Stand, Zeile 1.99.46)
