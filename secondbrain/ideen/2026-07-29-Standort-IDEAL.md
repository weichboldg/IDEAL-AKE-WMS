---
typ: feature
epic: true
anhaenge:
  - anhaenge/2026-07-29-standort-ideal/sage-views-ideal.md
---
# IDEAL-Standort live schalten (eigene DB, standortspezifische Logik)

## Worum geht es

Ein neuer Standort **IDEAL** soll live gehen. Rahmenbedingungen:

- **Eigene SAGE-Installation** am Standort IDEAL (getrennt von AKE).
- **Eigenes IDEAL-AKE-WMS-Datenbanksystem** — also KEINE Single-DB-Installation,
  sondern eine zweite WMS-DB. AKE und IDEAL laufen getrennt, aber auf
  **gemeinsamem Code-Stamm** (Single-Source-Strategie im Quellcode).
- IDEAL arbeitet fachlich anders als AKE. Deshalb muessen sich gewisse Dinge
  **pro Standort schalten** lassen — v. a. welche SAGE-Views verwendet werden
  und welche Logiken/Settings gelten (Konfiguration ueber unsere Einstellungen,
  z. B. AppSettings).
- Wo sinnvoll, sollen die Standort-Unterschiede im Code **getrennt** werden
  (Wartbarkeit). Umstellung der Arbeitsweisen passiert step-by-step ueber
  laengere Zeit — die Architektur muss dieses schrittweise Vorgehen tragen.

### Konkrete fachliche Unterschiede IDEAL <-> AKE

1. **Fertigungs-/Produktionsauftrag ist hierarchisch.** Bei IDEAL hat ein
   Produktionsauftrag mehrere **Sub-Auftraege**; AKE hat derzeit eine flache
   1:1-Hierarchie. Als Basis fuer die hierarchische Nachbildung kann die
   **OSEON-Auftragsimplementierung** dienen (die ist bereits hierarchisch).
2. **Terminplanung.** Bei AKE laufen Termine derzeit ueber Werte in unseren
   Settings. Bei IDEAL gibt es ein **eigenes PPS-Tool mit eigener View**, das
   die Terminierung liefert — wir konsumieren die Termine, statt sie zu rechnen.
3. **Neue Feature-Bereiche fuer IDEAL** (Details im Anhang): Vormontage-Listen,
   Beschichtungsauftrag, Kommissionierlisten — gespeist aus zwei neuen
   SAGE-Views (`vw_IDEAL-AKE_Kommissionierung_FAListe` / `_FAInfos`).

Die genauen View- und Spaltendefinitionen, Verknuepfungsschluessel, Filter,
Fallstricke und ein konkreter Integrationsvorschlag (Repository/Decorator +
Standort-Resolver + Feature-Toggles) stehen im Anhang
[[sage-views-ideal]] (`anhaenge/2026-07-29-standort-ideal/sage-views-ideal.md`).

## Offene Fragen / Unklarheiten

Diese muessen VOR bzw. spaetestens bei der Spec-Freigabe geklaert sein. Ich habe
je Frage eine erste Einschaetzung ergaenzt — bitte bestaetigen oder korrigieren.

1. **Mandantenfaehigkeit: eine App-Instanz pro Standort oder eine gemeinsame?**
   Der Anhang spricht von einem `IFaListeStandortResolver` (Laufzeit-Weiche im
   selben Prozess). „Eigenes Datenbanksystem" klingt eher nach **separater
   Deployment-Instanz pro Standort** (eigene AppSettings, eigene DB-Verbindung).
   -> *Einschaetzung:* getrennte Instanz je Standort ist sauberer (getrennte
   Connection-Strings, getrennte Feature-Toggles, kein Cross-Contamination-Risiko
   in Produktionsdaten). Der Standort-Resolver waere dann nur fuer den
   Datenquellen-Pfad innerhalb einer Instanz noetig, nicht fuer Mandantentrennung.
   **Zu bestaetigen: ein Deployment je Standort?** => JA, eigenes DEPLOYMENT pro Standort
2. **Hierarchischer Produktionsauftrag — Umfang der Datenmodell-Aenderung.**
   Die Sub-FA-Hierarchie (HauptFA/VaterFA/SubFA) ist der groesste Brocken und
   beruehrt potenziell das Kern-Datenmodell (heute 1:1). -> *Frage:* Wird die
   Hierarchie nur **lesend aus der FAListe-View** abgebildet (View liefert den
   Baum, WMS stellt dar), oder muessen wir Sub-FAs auch **persistieren/
   rueckmelden** (BDE, Teileverfolgung auf Sub-FA-Ebene)? Das entscheidet, ob es
   eine reine Anzeige-Erweiterung oder ein echter Datenmodell-Umbau ist. -> muss auch rückgemeldet werden können für BDE etc.
3. **AKE-Regressionsrisiko.** Der gemeinsame Code-Stamm heisst: jede
   Standortweiche darf AKE **nicht** veraendern. -> *Einschaetzung:* Alle
   IDEAL-Features hinter Feature-Toggles mit Default `false` (wie im Anhang
   vorgeschlagen) + AKE-Pfad bleibt exakt die bestehende Kette
   (`vw_AKE_Kommissionierung_WAListe`). **Zu bestaetigen als harte
   Akzeptanzbedingung: AKE-Verhalten unveraendert nach jedem Teil-Merge.**
4. **Standort-Schalter: ein Flag oder Feature-Toggle-Set?**
   Der Anhang empfiehlt ein Set von Feature-Toggles pro Standort statt einer
   harten Standort-Weiche. -> *Einschaetzung:* zustimmen, plus EIN
   `Standort`-Grundschalter (AKE|IDEAL) fuer den Datenquellen-Resolver; die
   fachlichen Features einzeln toggeln. **Zu bestaetigen.** -> Standort Grundschalter sicher zusätzlich sinnvoll
5. **Termin-Uebernahme aus PPS.** Ersetzt die PPS-View die bestehende
   Settings-basierte Terminlogik bei IDEAL vollstaendig, oder nur teilweise?
   Gibt es Termine, die IDEAL trotzdem selbst rechnet? -> *offen, fachlich zu
   klaeren.* -> alle in der view genannten termine ersetzen vollständig. 
6. **View-DDL ins Repo.** Der Anhang notiert die zwei Views als „TODO: ins Repo
   aufnehmen" unter `SQL/sage-views/`. -> *Einschaetzung:* ja, als versionierte
   DDL (nicht als WMS-Migration, da Fremd-DB). Sollte Teil-1 sein, damit die
   Views nachvollziehbar sind.
7. **Zieldatenbank/Server der zweiten Standort-DB.** Anhang nennt
   `AKESQL20.ake.at` / `IDEAL_TEST_2026_05_03` (Test). Produktiv-DB-Name/Server
   + Connection-String-Verwaltung (pro Instanz) -> *fachlich/infrastrukturell zu
   klaeren.* -> Connection String ist pro Instanz getrennt

## Zerlegung & Reihenfolge (Inbetriebnahme-getrieben)

Entscheidungen aus den Rueckfragen sind eingearbeitet: eigenes Deployment je
Standort, getrennte Connection-Strings, Sub-FA muss rueckmeldefaehig sein (BDE),
Standort-Grundschalter zusaetzlich zu Feature-Toggles, PPS-Termine ersetzen die
Settings-Termine bei IDEAL vollstaendig.

**Leitgedanke (wichtig): Reihenfolge nach Inbetriebnahme, nicht nach
technischer Schicht.** Ohne die Produktionsauftraege (Sub-FA-Struktur) im
System gibt es bei IDEAL nichts zu testen und nichts zu kommissionieren. Der
groesste Brocken kommt also ZUERST — aber in sich noch einmal geteilt, damit
kein wochenlanger Monster-Branch entsteht.

**Gesamtform: `split` (mehrere Teile in fester Reihenfolge), und Teil 2 ist
seinerseits ein `epic`** (Datenmodell-Umbau in Etappen). Also ein split-Paket,
in dem ein Teilstueck ein epic ist.

- **Teil 1 — FA-Struktur LESEND (Inbetriebnahme-Fundament).**
  View-DDL ins Repo (`SQL/sage-views/`), `IFaListeRepository` +
  `CachedFaListeRepository` + `IFaListeStandortResolver` + Domain-Objekte,
  Standort-Grundschalter (AKE|IDEAL), hierarchische Produktionsauftraege
  einlesen und **darstellen** (HauptFA/VaterFA/SubFA-Baum). Datenverfuegbarkeits-
  Regel (INNER JOIN FAInfos) beachten.
  *Ziel: IDEAL-Auftraege sind im System und sichtbar — testbar.* AKE unberuehrt
  (Toggle/Standort aus). Einzeln mergebar.
- **Teil 2 — Sub-FA-RUECKMELDUNG (Datenmodell-Umbau, BDE) — als eigener `epic`.**
  Der groesste technische Brocken: Sub-FAs persistieren und rueckmelden
  (BDE/Teileverfolgung auf Sub-FA-Ebene). Baut auf Teil 1 auf. Etappen grob:
  Entities + Migration -> Repositories/Services -> Rueckmelde-Logik ->
  BDE-Anbindung -> Tests. Laeuft in EINEM langlebigen Worktree, `sync-worktree.ps1`
  haelt ihn auf main-Stand.
  *Ziel: IDEAL-Auftraege sind bearbeit-/rueckmeldbar.*
- **Teil 3 — Kommissionierlisten.** Filter `Kommissionieren`, Barcode HauptFA.
  Nutzt Fundament.
- **Teil 4 — Beschichtungsauftrag.** Filter `Beschichtet = -1`, Druckdokument
  mit Dienstleister-Kopf.
- **Teil 5 — Vormontage-Listen.** Filter `VMBedarf`, 3 Sichten, Isolierfraesen-
  Export.

Teil 1 und 2 bringen IDEAL in einen betreibbaren Zustand (Auftraege da +
rueckmeldbar). Teil 3-5 sind die Anwender-Features darauf, einzeln toggelbar
(`IdealKommissionierlistenAktiv`, `IdealBeschichtungAktiv`, `IdealVormontageAktiv`).

**Termine:** Bei IDEAL ersetzen die PPS-View-Termine (KO_Termin, FE_Termin,
Start_Beschichtung, Neuer_PT_PPS, Verladetermin_Vsl ...) die bisherige
Settings-basierte Terminlogik vollstaendig — wir konsumieren, rechnen nicht.

**Harte Akzeptanzbedingung fuer JEDEN Teil:** AKE-Verhalten bleibt unveraendert
(bestehende Kette `vw_AKE_Kommissionierung_WAListe`, IDEAL-Logik nur bei
Standort/Toggle IDEAL aktiv).

## Reif fuer den Backlog?

**Noch nicht ganz.** Vor dem Umzug nach `backlog/`:

1. Offene Fragen 1-4 beantworten (die entscheiden Architektur + Scope; 5-7
   koennen teils in die Spec-Rueckfragen wandern).
2. split vs. epic final entscheiden und Frontmatter bereinigen (aktuell steht
   `epic: true` — bei Entscheidung fuer split austauschen).
3. Anhang beim Verschieben mitnehmen: Ordner nach
   `backlog/anhaenge/2026-07-29-standort-ideal/` kopieren, damit der
   `anhaenge:`-Verweis stimmt (aktuell liegt er unter `ideen/Anhang/...` — der
   Pfad im Frontmatter ist schon auf das Backlog-Ziel ausgerichtet).
4. Weil Anhaenge im Spiel sind: die Spezifizierung **interaktiv** starten
   (nicht headless), damit der Anhang verlaesslich gelesen wird.
