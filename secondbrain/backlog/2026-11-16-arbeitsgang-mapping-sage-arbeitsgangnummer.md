---
type: backlog
status: offen
created: 2026-11-16
quelle: Gerald, Chat 2026-11-16 15:48
standort: beide (zuerst IDEAL)
---
# Arbeitsgang-Mapping: Kürzel → Sage-Arbeitsgang (und Arbeitsplatz) in den Arbeitsgang-Stammdaten

## Wunsch (Gerald)
Mapping-Informationen in der WMS-Arbeitsgänge-Tabelle (bzw. eigene Mapping-Tabelle), damit das WMS Arbeitsgänge aus den Userfeldern
(`USER_OSArbeitsschritte`) korrekt an Sage zurückgeben / in Sage-FAs anlegen kann. In Sage kommen die Arbeitsgänge per **Excel-Import nur in die
Userfelder** (keine AG-Positionen in der Ressourcenliste) – Lösung über das WMS „wäre eine gute Lösung“.

## Belege
- Sage IDEAL `PpsArbeitsgaengeInfo.USER_PNKZ` = Kürzel (Liste unten, 16.11. Prod, nur GET; [[2026-10-30-fa-rueckmeldung-an-sage]]).
- WMS `OperationConfig` (`IdealAkeWms/Models/OperationConfig.cs`): `OperationName` (Kürzel), `DisplayName`, `DueDateOffsetDays`,
  `IsExternalSystemRelevant`, `Source`, `SageMissingSince` – **keine Sage-Arbeitsgangnummer**.
- Arbeitsgang-Abgleich `OperationConfigSyncService` liest aus der View `Arbeitsgangnummer, Bezeichnung, AG`, speichert die Nummer aber nicht
  (`IDEALAKEWMSService/Services/OperationConfigSyncService.cs`, `IOperationConfigSyncService.cs:5`).
- Werkbank → Sage-Arbeitsplatz gibt es: `ProductionWorkplace.ArbeitsschrittCode` + `SageArbeitsplatznummer`.

## Zuordnung Sage IDEAL (aktive Arbeitsgänge, `USER_PNKZ`)
| Kürzel | Sage-AG | Bezeichnung | | Kürzel | Sage-AG | Bezeichnung |
|---|---|---|---|---|---|---|
| ST | 120 | Stanzen | | KL | 210 | Kleben |
| ZU | 130 | FR-Zuschnitt | | IF | 220 | Isolierfräsen |
| EG | 140 | Entgraten Fläche | | VM | 230 | Vormontieren |
| EK | 142 | Entgraten Kanten | | SÄ | 240 | Schäumen |
| KA | 150 | Kanten | | IS | 250 | Isolieren |
| SL | 161 | Schweißen Laser | | VL | 260 | Löten |
| SM | 162 | Schweißen Manuell | | M31 / M42 / M45 / M43 / M41 / M52 | 270 / 280 / 281 / 282 / 283 / 284 | Montieren H3-01 / H4-02 / H4-05 / H4-03 / H4-01 / H5-02 |
| BR | 170 | Berohren | | PR | 310 | Prüfen |
| PU | 180 | Punkten | | PL | 320 | Probelauf |
| ES | 190 | Schleifen | | VP | 330 | Verpacken/Versand |
| BE | 200 | Beschichten | | – | 110 | Schachteln (ohne Kürzel) |
Ohne Zuordnung, aber in FA 1059463 verwendet: **ISO, MK**. Kein **LS**. Am Sage-Arbeitsgang ist **kein Arbeitsplatz** gepflegt.

## Vorschlag Koordination
- `OperationConfig` um **`SageArbeitsgangnummer`** (int?) erweitern, automatisch gefüllt vom Arbeitsgang-Abgleich (View-Spalte `Arbeitsgangnummer`
  bzw. SData `USER_PNKZ`), von Hand überschreibbar; Quelle/Abweichung anzeigen.
- Arbeitsplatz je Rückmeldung aus der **Werkbank** (`ProductionWorkplace.SageArbeitsplatznummer`) – kein zweites Mapping.
- Prüfliste/Hinweis in `/OperationConfigs`: Kürzel aus FA-Userfeldern ohne Sage-Arbeitsgang (ISO, MK …), Werkbänke ohne Sage-Arbeitsplatz.
- Grundlage für: Anlage der AG-Positionen im Sage-FA (`FertigungsauftragService` ServiceOption 4) und `PpsRueckmeldungArbeitsplatzService`
  ([[2026-10-30-fa-rueckmeldung-an-sage]]). Schreiben erst nach Entscheidung + Schreibtest am IDEAL-Test.
