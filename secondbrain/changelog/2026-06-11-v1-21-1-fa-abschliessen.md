---
type: changelog
version: 1.21.1
date: 2026-06-11
---
# v1.21.1 — Bugfix: FA-Abschliessen wirkt wieder

- `Picking/ToggleDone` schrieb seit v1.11 `PickingStatus.IsDonePicking`, aber **keine** Query las
  das Flag — der Abschliessen-Klick war ueber ein Jahr wirkungslos.
- Jetzt gilt ueberall „erledigt = `IsDone || IsDonePicking`": `GetForLeitstandAsync` (Filter **und**
  Projektion), ViewModel-Mapping in FA-Liste und Leitstand, Picking-Worklist.
- Sage-`IsDone` wird weiterhin **nicht** beschrieben — der Sage-Sync wuerde es ueberschreiben. Nur
  die Lese-Seite wurde korrigiert → [[0009-app-status-in-satelliten-tabellen-neben-sage-master]].
