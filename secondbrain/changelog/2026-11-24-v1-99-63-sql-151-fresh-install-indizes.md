---
type: changelog
version: 1.99.63
date: 2026-11-24
---
# v1.99.63 — Update-Skript: SQL/151 fehlende Indizes IDEAL-Prod

Anlass: Update-Lauf IDEAL-Prod mit Paket 16 (2026-11-24 08:41) brach in der Vorprüfung ab (`Pruefung/FreshInstallIndizes` Abweichung 26, Phase Vorher → Prod-Abbruch); SQL/151 war seit P-2b (17.11.) offen.
Bezug [[2026-11-14-sicherheit-anmeldung-und-installer-spec]] Rückfrage 4 / P-2b, [[2026-11-17-pruefabfragen-gerald]]. Entscheid Gerald 2026-11-24: SQL/151 jetzt bauen; IX_StockMovements_ArticleId und IX_WorkstationUsers_WorkstationId weglassen (abgedeckt).
Commit `9dbc6ef2`, QA bestanden (Brain `84164928`, Abschnitt in der Spec). TS-188. Paket 17. Nicht gemergt.

## Kernpunkte
- `SQL/151_RepairFreshInstallIndexes.sql`: Abschnitt 15 der Erstinstallation ohne die zwei abgedeckten (24 Indizes + FK-Guard), je Anweisung ein Batch, keine Migration.
- `Pruefung/FreshInstallIndizes` jetzt Haltepunkt-Reparatur (Datei 151, nur IDEAL-Prod, nach `j`, Nachprüfung = 0).
- Neu `Pruefung/FreshInstallIndizesDuplikate` (Vorher, IDEAL-Prod): doppelte Werte der drei UNIQUE-Spalten → Abbruch vor dem Stoppen (sonst scheitert SQL/151 unter `sqlcmd -b` bei gestopptem Web).
- `KommissionierlisteNichtFreigegeben`/`VorfilterZiele` mit `Braucht=@('SQL/159')` (vorher „Ungültiger Spaltenname VerschwundenSeit“ vor dem Schema).
- Abbruchtext der Vorprüfung generisch (vorher „Wert von Hand entfernen“, passte nur zur Appsettings-Prüfung).

## Deploy-Risiken
- SQL/151 nie von Hand auf anderen Systemen (EF-Namen → Duplikat-Indizes). Index-Anlage auf großen Tabellen dauert, Web/Dienst gestoppt.
- QA-Hinweis H-1: Duplikat-Prüfung deckt `IX_StorageAreas_Lagerkennung`/`UQ_ArticleSageDetails_ArticleId` nicht ab – auf IDEAL-Prod vorhanden (P-2b).
- Aus dem abgebrochenen Lauf: Dienst war vorher schon gestoppt; Backup-Ordner auf idealsql2 fehlt.

## Paket
`publish\paket17-v1.99.63.zip` (Publish aus `9dbc6ef2`; 346 Web, 93 Dienst, 151 SQL, 5 deploy, publish-info.txt): 237 052 409 Bytes,
SHA-256 `2FD22CB0A78E8142892A33AD643B2B40C8A6CC4ADF3CEFF98E4EF428050635BC`; auf `\AKENET01.ake.at\Installsourcen` und
`\IDEALWEB01.ideal.ideal-ake.at\InstallSourcen` Größe + SHA-256 gleich (vorher nicht vorhanden). Confluence (später): Betrieb 00-index, 03.
