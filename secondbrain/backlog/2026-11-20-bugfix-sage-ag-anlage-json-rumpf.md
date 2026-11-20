---
typ: backlog
status: in-spec
created: 2026-11-20
herkunft: "Schreibtest E1b 2026-11-20 (Gerald/Koordination), :88 gegen Prod-Sage IDEAL;1"
standort: IDEAL
art: bugfix
---
# Bugfix: Sage-AG-Anlage – Sage lehnt den JSON-Rumpf ab („Ungültige Elementdefinition“)

Bezug: [[2026-11-17-arbeitsgaenge-aus-wms-in-sage-fa-anlegen-spec]] (Abschnitt „Befund E1b“).

## Beobachtung
POST `$service/FertigungsauftragService` für FA 1065144 / AG 162 → `HTTP 500 … SDataSerializationException: Ungültige Elementdefinition`
in `DataContainerJsonReader.Read()` (Sage liest den Rumpf nicht ein). Nichts angelegt (AG in Sage = 0).

## Ursache (Vermutung, nicht belegt)
Rumpf in `IDEALAKEWMSService/Services/SageFaArbeitsgangClient.cs:57-72` ist ein freies Dictionary (`ServiceOption`, `Handle`,
Positionsliste mit `Handle`, `PositionHandle` 0, `RessourceTyp` 1 als Zahl, `Ressourcennummer`, `Menge`, `Bearbeiter`); offene TODOs P-1..P-5
(Z. 32-43). Sage-SData erwartet für Service-Aufrufe ein bestimmtes Containerformat (vgl. funktionierender `SageLagerbuchungClient.cs`).

## Vorschlag
1. Lesend `$template` und `$schema` von `$service/FertigungsauftragService` am Ziel abrufen (Register Sage-Test → Feldliste/Ressource) und den
   Rumpf daran ausrichten; Aufbau des funktionierenden Lagerbuchungs-Rumpfs als Muster.
2. Unit-Test, der den erzeugten JSON-Rumpf gegen ein am Ziel gespeichertes `$template`-Beispiel prüft.
3. Danach E1b erneut an FA 1065144 (Freigabe Gerald).
Ohne Rückfrage spezifizierbar; Dev braucht Lesezugriff aufs Ziel (oder gespeicherte `$template`-Antwort).

## Lesebefund `$template` (Koordination 2026-11-20 08:37, :88 → Prod `IDEAL;1`, GET über Register Sage-Test)
`GET …/CommonWawiServices/IDEAL;1/$service/FertigungsauftragService/$template` → HTTP 200, `application/atom+xml; type=entry`. Gerüst unter
`<request>` (Anzeige im Register nach ~3,8 KB abgeschnitten, USER_-Felder ausgelassen):
```
ServiceOption = NotSpecified          ← Enum als TEXT, nicht Zahl
PositionenMaterial
  FaBelegMaterialPosition (sdata:lookup)
    FaBelegPositionUserfields / FaBelegPositionUserfield (USER_OSArbeitsschritte, USER_OSAbteilung, …)
    GenericUserfields / GenericUserfield
    BacklogPrimaryKeys / BacklogPrimaryKey
PositionenArbeitsgang
  FaBelegArbeitsgangPosition
    FaBelegPositionUserfields / FaBelegPositionUserfield
    GenericUserfields / GenericUserfield
    BacklogPrimaryKeys / BacklogPrimaryKey
```
Abweichungen zum WMS-Rumpf (`SageFaArbeitsgangClient.cs:57-72`): kein Feld `Handle` direkt unter `request` sichtbar; Positionsliste heißt
`PositionenArbeitsgang` mit Element `FaBelegArbeitsgangPosition`; `ServiceOption` ist ein Enum-Text. Die skalaren Felder der
Arbeitsgang-Position (Handle, PositionHandle, RessourceTyp, Ressourcennummer, Menge, Bearbeiter …) sind in der Anzeige abgeschnitten →
vollständig über Register Sage-Test → **Feldliste** oder `$schema` holen (lesend). **Wichtig:** Lagerbuchungs-Client als Muster prüfen, wie
der JSON-Rumpf für einen `$service`-Aufruf aussehen muss (Hülle `request`? Feldnamen exakt wie im Template?).
