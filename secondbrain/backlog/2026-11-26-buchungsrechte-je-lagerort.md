---
type: backlog
typ: feature
status: offen
created: 2026-11-26
quelle: Gerald im Chat 2026-11-26 (Brainstorming, Richtung freigegeben)
---
# Buchungsrechte je Zone/Lager (Buchungsgruppen)

## Anlass
In der Vorkommissionierung wurde bei einer freien **Aus-/Umbuchung** ein Artikel von einem Lagerplatz des
Hauptlagers gebucht. Heute gibt es keine Einschränkung je Lagerplatz/Benutzer – nur Rollen.

## Abgestimmte Richtung (Gerald 2026-11-26: „klingt ausreichend“, muss einfach verwaltbar sein)
Nie Rechte je einzelnem Lagerplatz (zu viele Plätze), sondern zwei grobe Stufen:
1. **Buchungsgruppe → Zonen**: wenige Gruppen (z. B. „Hauptlager“, „Vorkommissionierung“) mit den erlaubten
   **Zonen** (Mehrfachauswahl aus den vorhandenen `StorageLocations.Zone`-Werten). Pflege selten.
2. **Benutzer → eine Buchungsgruppe**: ein Auswahlfeld am Benutzer. **Ohne Gruppe = darf überall** (kein Verhaltenswechsel beim Deploy).

- Serverseitige Prüfung bei freier Einbuchung, Ausbuchung, Umbuchung (Quelle **und** Ziel) und „Lagerplatz ausbuchen“ (en bloc);
  bei Verstoß klare Meldung („Zone X ist für dich nicht freigegeben“).
- **Nicht** betroffen (System gibt den Platz vor): Kommissionieraufträge, Wareneingang, BDE.

## Befund Produktion (2026-11-26, `IDEAL_AKE_WMS` nur gelesen, auf Geralds Anweisung)
- **`StorageAreaId` ist bei allen 4425 Lagerplätzen NULL** → Lagerort-Ebene (`StorageArea`) in Prod unbrauchbar.
- **`Zone` ist der tatsächliche Lager-Schlüssel:** bei Sage-Plätzen schreibt der Abgleich die Sage-Lagerkennung hinein
  (`LagerplatzSyncService.cs:125`, `zone = dto.Lagerkennung`): HR 1997, GL 1225, SL 101, VT 81, LL 69, KonsHainzl 13, ~16 Einzel-Lager.
  Manuelle Plätze (920, `Source = Manual`) tragen freie Zonen: „Halle E - Regalzeile 1…7“, „Halle E - RAL Regalzeile 1/2“,
  „Kleinteileshop“, „K-02“, „VK Regal … Stirnseite“, „Durchgang“, „Halle E“ (u. a. WAGEN) … – rund 40 aktive Zonen insgesamt.
- Nur **18** aktive Plätze ohne Zone.
- Kommissionierbereiche haben **keine** Lagerort-Regel (bestätigt Verwerfen von Variante B).

## Entscheidungen (Claude, auf Geralds Auftrag „entscheide“, 2026-11-26)
- **Schlüssel = `Zone`**, nicht `StorageArea` (Befund oben). Rund 40 Werte sind gut pflegbar.
- **Plätze ohne Zone:** für Benutzer *mit* Gruppe gesperrt, Meldung „Lagerplatz hat keine Zone – Zone pflegen“; die Pflegemaske
  listet diese Plätze als Hinweis (melden statt still).
- **Zone in der Gruppe, zu der kein aktiver Platz mehr existiert** (Umbenennung/Tippfehler bei manuellen Zonen): Warnhinweis in der
  Pflegemaske, nicht still ignorieren.
- **Kommissionierwagen (`IsPickingTransport`) immer erlaubt** – die Vorkommissionierung braucht sie, egal welche Zone.
- **Einbuchung eingeschlossen** (gleiches Risiko; das Umbuchungsziel ist ohnehin geprüft).
- **Admin-Wildcard** (ADR 0006): darf überall buchen. **Pflege** der Gruppen und der Zuordnung nur Admin (Benutzerverwaltung).
- **Kein Feature-Schalter:** „ohne Gruppe = überall“ ist bereits der Aus-Zustand.
- Gilt für beide Standorte gleich (Zone existiert an beiden).

## Verworfen
- Zuordnung über Kommissionierbereiche: deren Regeln sortieren Auftragszeilen (auch Artikel-/Artikelgruppen-Regeln);
  Regeländerung für die Kommissionierung würde unbemerkt Buchungsrechte ändern.
- AD-Gruppe je Lagerort: Pflege nur über IT.

## Für die Spec offen
- Fachlich (Gerald/Lagerleitung, kein Spec-Blocker): welche Zonen gehören zur Vorkommissionierung, welche zum Hauptlager
  (HR? GL? Halle E?) – wird beim Anlegen der Gruppen gepflegt, nicht im Code.
- Pflege-Maske nach ADR 0020 (Zonen als Auswahl aus vorhandenen Werten, Wirkungsbeschreibung).
- Alle Buchungswege in `StockMovementsController` (+ API/Scanner-Endpunkte) über **eine** Prüfstelle führen – Schreibwege belegen.
