---
type: changelog
version: 1.37.0
date: 2026-09-09
---
# v1.37.0 — IDEAL: Materialisierung um die fachlichen Felder erweitern (K1/K2)

Umsetzung der freigegebenen Spec [[2026-08-20-materialisierung-fachliche-felder-spec]] im
**Buendel-**Worktree `feature/2026-08-07-ideal-teile-1-5`. Die Spec bindet das im Frontmatter und
begruendet es im Deploy-Abschnitt: Der Code, auf dem sie aufsetzt, existiert **nur** in diesem
Zweig. Umsetzungsnotiz [[2026-09-09-materialisierung-fachliche-felder-umsetzung]], Entscheidung
[[0014-werkbank-datenhoheit-sage-fuehrend-mit-abweichungsmeldung]], Plan
`docs/superpowers/plans/2026-09-09-materialisierung-fachliche-felder.md`.

**Warum:** Seit Teil 7 werden IDEAL-Sub-FAs als echte Auftragszeilen materialisiert, aber der Sync
schrieb nur sieben Felder. Seit die FA-Liste dieselben Auftraege anzeigt, blieben Kunde, Werkbank,
Termine und Lack-Kennzeichen leer, obwohl die Werte in der Struktur bereits vorhanden waren. Diese
Fassung schliesst die Luecke — **nicht** pauschal, sondern nach einer Drei-Klassen-Trennung: Was am
HauptFA haengt (K1), wird nur **angezeigt** und nie materialisiert; was am Sub-FA haengt (K2), wird
geschrieben; der Kommissionier-Status (K3) bleibt zu Recht leer. **Nur bei Master
`ProduktionsauftragHierarchisch = true`; bei `false` unveraendert.**

## Umgesetzt

- **Statuszeilen eager anlegen** (`ceaf5b3`): Die Materialisierung legt jetzt zu jedem Sub-FA die
  beiden 1:1-Satellitenzeilen an (`ProductionOrderPickingStatus`, `ProductionOrderBdeStatus`) —
  im Anlege- **und** im Update-Pfad, wodurch der Bestand ohne Sonderskript nachgezogen wird.
  Idempotent, nach dem Vorbild des AKE-Eager-Create.
- **Lack-Ableitung als reiner Helper** (`af25bcd`): `FaMaterializationCoating` leitet je Sub-FA ab
  „selbst oder ein **direktes** Kind ist `Beschichtet`"; Enkel zaehlen bewusst nicht. DB-frei und
  vollstaendig unit-getestet. `MaterializationSourceOrder` traegt zusaetzlich `Arbeitsbereich` und
  das abgeleitete Flag — **ohne** Defaultwerte, damit jeder Erzeuger sie bewusst setzt.
- **Werkbank aus dem Arbeitsbereich, Variante B** (`ccc1d74`): Sage ist fuehrend, ein abweichender
  Bestandswert wird ueberschrieben und **gemeldet**. Die Meldung heisst „Werkbank weicht vom
  Quellwert ab" und behauptet ausdruecklich **nicht**, jemand habe von Hand zugewiesen — der Sync
  kann Mensch und Sage-Umplanung nicht unterscheiden (ADR 0014). Unbekannter Arbeitsbereich:
  melden, **niemals** einen Stammsatz anlegen. Mehrdeutiger Name: keine Zuweisung. Name-Vergleich
  case-insensitiv und getrimmt, **eine** Abfrage je Lauf. Sammelmail nur bei Aenderung der Menge.
- **Lack-Kennzeichen schreiben** (`77aaac2`): ueber `SetCoatingPartsAsync`, nie roh — die Methode
  kapselt den gekoppelten Ruecksetzer (`IsCoatingDone` faellt mit, Fallstrick #11). Laeuft
  zwingend **nach** dem Eager-Create, sonst schriebe es ins Leere.
- **Kombigeraet-Erkennung im Struktur-Sync** (`904e899`): HauptFA mit mehreren Auftragskoepfen
  wird **einmal je Lauf** als Sammelmeldung protokolliert, nicht je Seitenaufruf.
- **Gebuendelte Kopfdaten-Abfrage** (`47c6712`): `GetByHauptFaKeysAsync` — eine Abfrage je
  angezeigter Seite statt einer je Gruppe.
- **Kunde-Filter repariert** (`79a174b`, `5022222`): Freitextsuche **und** serverseitiger
  Spaltenfilter finden im hierarchischen Modus ueber den Auftragskopf, per korrelierter
  Unterabfrage ohne Fan-out. Wirkt ueber das Repository auch auf den Leitstand. Der Flachmodus
  laeuft weiterhin durch den **unveraenderten** Alt-Ausdruck (F4).
- **K1 im Anzeige-Modell** (`f160b1d`, `b3d5a15`): Gruppen-Kopfzeile traegt Kunde, Konstruktions-,
  Fertigungs- und Liefertermin, Prio, AB-Nummer, Montage-Abteilung. Die Termin-Kaskade rechnet auf
  dem **Gruppen**-Fertigungstermin, waehrend die **zeilen-eigene** Werkbank-Abweichung wirksam
  bleibt. Die Datenbankspalten bleiben leer — angereichert wird nur die Anzeige.
- **Kombigeraete waehlen nichts aus** (`f160b1d`, `86cdc26`): Mehrere Koepfe ⇒ alle Varianten im
  Klartext plus Kennzeichen „mehrdeutig", **keine** Einzelwerte, **kein** Beschichtungstermin auf
  irgendeiner Zeile der Gruppe. Ein still gewaehlter Kopf schickt Teile zum falschen Beschichter.

## Vier Fehler, im Lauf gefunden und behoben

Zwei davon fielen nicht beim Programmieren auf, sondern beim **Schreiben von Tests** — beide waeren
still gewesen, keiner haette eine Fehlermeldung erzeugt:

1. **Der gemeldete Abnahme-Fehler** (`ceaf5b3`): Ein Sub-FA liess sich im Leitstand nicht zur
   Kommissionierung freigeben (`HTTP 404 „PickingStatus-Zeile fehlt."`); Prioritaet und Zuweisung
   endeten in `HTTP 500`, die **Massenfreigabe uebersprang die Zeilen still**. Siehe
   [[2026-09-09-materialisierung-ohne-statuszeilen-bug]]. Ursache und Spec-Punkt F6 waren
   dieselbe Luecke.
2. **Blattebene weggefiltert** (`af25bcd`): Die Knotenabfrage filterte Blaetter bereits in der
   Datenbank weg, bevor die Lack-Ableitung sie sah. Da ein direktes Kind meistens ein Blatt ist,
   haette die Ableitung fuer die Mehrheit der Faelle still `false` geliefert. Kein Test waere rot
   geworden — die Plan-Testdaten waren zufaellig passend gewaehlt.
3. **Wiederkehrendes Problem nie wieder gemeldet** (`520bafb`): Die Mail-Entprellung wurde nur
   fortgeschrieben, wenn es unbekannte Arbeitsbereiche **gab**. Nach dem Anlegen der Werkbank blieb
   der alte Stand stehen; tauchte derselbe Name spaeter wieder auf, galt er als „unveraendert" und
   waere nie wieder gemeldet worden.
4. **Geratener Beschichtungstermin** (`b3d5a15`): Die Ueberschreibung haing daran, dass ein
   Kopf-Termin **vorhanden** ist. Bei gesetztem Fertigungstermin, aber noch nicht geplantem
   Beschichtungstermin blieb der aus der AKE-Formel **zurueckgerechnete** Wert stehen — die Zeile
   zeigte einen Termin, den niemand vergeben hatte. Genau die Klasse „Raten statt Wahrheit", gegen
   die Design H der BOM-Bridge angetreten ist.

## Migration / Deploy

- **Migration: keine.** Alle K1-Felder werden nur angezeigt; K2 nutzt bestehende Spalten
  (`ProductionWorkplaceId`, `HasCoatingParts`).
- **Deploy: `web: true`, `service: true`, `migration: false`.**
- **Deploy-Vorbedingung (wichtig):** Die Werkbaenke zu den vorkommenden Arbeitsbereichen **vor**
  dem Deploy anlegen. `ProductionWorkplaceId` ist ein Fremdschluessel — ohne Stammsatz bleibt die
  Werkbank leer, der erste Lauf meldet nur, erst der naechste fuellt (Zwei-Lauf-Ablauf).
- **Ab dem ersten Lauf entstehen echte Daten:** Werkbank-Zuweisungen, Lack-Kennzeichen,
  Protokolleintraege und ggf. eine Sammelmail.
- **Kontext:** Teil des ungemergten Buendels — Schranke 2 fuer alles zusammen, ein Merge.

## Tests

_(qa-agent-Nachweis wird beim Abschluss ergaenzt)_

## Merge-Commit

_(offen — Schranke 2, Mensch)_
