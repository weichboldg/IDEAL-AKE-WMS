---
type: changelog
version: 1.99.42
date: 2026-11-18
---
# v1.99.42 — Nachbesserungen: WiCAM-CSV nach Vorlage, Benutzer-Kürzel, Beschichtungsauftrag, Userfeld-Meldung, Sage-AG unklar

Bündel `feature/2026-08-07-ideal-teile-1-5`, Basis `9b6b3e0d` (v1.99.41). Commits `1a972e72` (Kürzel), `9fd5181d` (BF-CSV),
`9d0a55e3` (Beschichtung), `3e02dde7` (Userfelder), `cccf906f` (N1), `93d7795e` (Doku/TS-163), `0af91b73` (Manifesttext), QA-Fix `b5463336` (Unklar-Erkennung über gemeinsamen Anfang „Antwort unklar“ – alte Zeilen bis v1.99.41 werden nicht automatisch wiederholt). Testbereit (qa-agent), Paket 7.
Goal-Lauf [[2026-11-18-goal-lauf-nachbesserungen-v1-99-42]]. Nicht gemergt.

## Kernpunkte

- **BF-CSV exakt nach `BF00000002.csv`** ([[2026-11-13-blechfertigung-bf-auftraege-wicam-csv-spec]], P-1 (2)): 13 Felder je Zeile, Kopfschlüssel
  in B/Wert C, Z10/Z12 leer, Z11 zehn ORDER_*-Spalten, Z13 deutsche Überschrift, ab Z14 Positionen. ORDER_POSITION = voller Lagerplatz `;`→`,`,
  ORDER_REMARK_2 = Hauptlagerplatz (vor dem ersten Trenner), ORDER_REMARK_4 = Besteller-Kürzel, ORDER_REMARK_1 = Arbeitsschritte
  (Snapshot `BfAuftragPositionen.Arbeitsschritte` aus `USER_OSArbeitsschritte`), Mat.-Nr. ganzzahlig (sonst Freigabe-Sperre).
  `;` sperrt wieder in allen anderen Feldern (B-1 Variante c ersetzt). Byte-Vergleichstest gegen `IDEALAKEWMSService.Tests/TestData/BF00000002.csv`.
- **Benutzer-Kürzel** `Users.Kuerzel` (≤ 10, `A–Z0–9`, groß, gefilterter UNIQUE `UQ_Users_Kuerzel`), Maske Stammdaten › Benutzer;
  Snapshot `BfAuftraege.BestellerKuerzel` bei Freigabe; fehlt es, ist die Freigabe gesperrt (Hinweis im Detail).
- **Beschichtungsauftrag:** Kreditor in Übergeben nachtragbar, solange `CreditorId` leer (`BearbeitenAsync(kreditorNachtragen)`); Einmal-Skript
  `SQL/Einmalig/2026-11-18_v1.99.42_BeschichtungsauftragKreditorNachtragen.sql` (Kurzname-Regel wie Vorbelegung, Trockenlauf im Manifest);
  Druck ruhiger, Barcode 10 mm rechts oben, „Seite x von y“ immer (auch ohne Fuß in der Vorlage).
- **Userfelder:** Hinweis auf die Einstellung steht jetzt **vor** der Sage-Meldung und kommt aus `SdataLeseException.Diagnose` → überlebt die
  600-Zeichen-Kappung; alle Abgleiche über `SageUserfelder.Erklaeren(ex)`. Fallstrick 53.
- **Sage-AG N1:** 2xx ohne Fund beim Nachlesen = „Antwort unklar“ (nie automatisch wiederholt); Knopf „In Sage vorhanden – bestätigen“ (Admin,
  auch bei Schalter aus, schreibt nichts an Sage). Schalter `SageFaArbeitsgaengeAnlegenAktiv` bleibt aus.

## Deploy-Risiken

- **SQL/162** `20261118144836_AddUserKuerzelUndBfBestellerKuerzel` und **SQL/163** `20261118145602_AddBfPositionArbeitsschritte` (additiv;
  163 trägt Arbeitsschritte für vorhandene Positionen aus dem Merkmal nach). FreshInstall an beiden Stellen, AgentJobs nicht betroffen.
- **Kürzel vor BF-Freigabe pflegen** – sonst ist jede Freigabe gesperrt (Manifesttext `v1.99.35-BfCsv` sagt es).
- Positionen mit nicht ganzzahliger Materialnummer oder `;` außerhalb des Lagerplatzes werden bei Freigabe gesperrt (vorher durchgelassen).
- **Einmal-Skript** nur nach Trockenlauf und Freigabe scharf (DB-Backup); ändert nur Aufträge ohne Kreditor mit genau einem Beschichter-Treffer.
- N1: keine automatische Wiederholung mehr bei 2xx ohne Fund – unklare Zeilen brauchen eine Hand.
