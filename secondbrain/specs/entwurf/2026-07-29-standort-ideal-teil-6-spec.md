---
type: spec
title: "IDEAL-Standort Teil 6 — Standorteinstellungen-Maske"
slug: 2026-07-29-standort-ideal-teil-6-spec
status: Entwurf
created: 2026-08-06
updated: 2026-08-06
source_backlog: "[[2026-07-29-Standort-IDEAL]]"
depends_on: "[[2026-07-29-standort-ideal-teil-1-spec]]"
task: ""
worktree: ""
branch: ""
affected_code:
  - IdealAkeWms/Controllers/StandortEinstellungenController.cs (neu, Name provisorisch)
  - IdealAkeWms/Views/StandortEinstellungen/Index.cshtml (neu)
  - IdealAkeWms/Models/ServiceSettingDefinitions.cs
  - IdealAkeWms/Models/AppSettingKeys.cs
  - docs/TESTSZENARIEN.md
  - secondbrain/tests/testszenarien-index.md
open_questions:
  - "Referenz-Notiz [[2026-08-03-standorteinstellungen-maske]] existiert nicht in secondbrain/ideen/ — Inhalt/Umfang dieser Maske ist ausschliesslich aus der Standort-IDEAL-Notiz abgeleitet, nicht aus einer eigenen Spezifikation"
  - "Vollstaendige Liste der zu buendelnden Werte (Firmenname, Adresse, Mandant, View-Namen, Schalter) noch nicht final — welche AppSettings/ServiceSettings-Keys genau darunter fallen"
  - "Verhaeltnis zur generischen /Settings- bzw. /ServiceSettings-Oberflaeche: bleiben die Keys dort zusaetzlich editierbar, oder wird diese Maske der EINZIGE Weg fuer Standort-Keys (Waechter-Konsistenz beachten, siehe Teil 7 Einweg-Tor)?"
  - "Rollen/Zugriff: admin-only (analog Settings) oder eigene Rolle?"
epic: false
etappen: []
deploy:
  web: true
  service: false
  migration: false
freigabe_entscheidung: ""
freigabe_von: ""
freigabe_am: ""
---

## Ziel / Nutzen (das Warum)

Bis zu diesem Teil laufen alle IDEAL-Schalter (View-Namen, Feature-Toggles, der Master
`ProduktionsauftragHierarchisch` und seine Abhaengigen) als normale Eintraege in der generischen
`ServiceSettings`/`AppSettings`-Oberflaeche — das kostet laut Notiz „Null Zusatzaufwand", ist aber
unuebersichtlich, sobald mehrere standortspezifische Werte (Firmenname, Adresse, Mandant,
View-Namen, Schalter) zusammengehoeren. Teil 6 buendelt diese Werte in einer eigenen,
gruppierten Maske — ein reines UX-/Organisations-Feature ohne neue fachliche Logik.

**Hinweis:** Die in der Backlog-Notiz referenzierte Detail-Notiz
`[[2026-08-03-standorteinstellungen-maske]]` existiert derzeit **nicht** in `secondbrain/ideen/`
(siehe offene Rueckfrage 1) — diese Spec ist deshalb bewusst grob gehalten und nur ein Geruest.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope:** eine neue, gruppierte Admin-Maske, die bestehende `ServiceSettings`-/
`AppSettings`-Keys mit Standortbezug (View-Namen aus Teil 1, Toggles aus Teil 2–5, ggf. Firmen-
/Adressdaten) **liest und schreibt** — keine neue Datenhaltung, nur eine kuratierte Oberflaeche
ueber bestehende Katalog-Infrastruktur (ADR 0008/0011).

**Out-of-Scope:** keine neuen fachlichen Werte, die nicht bereits in Teil 1–5 als Setting-Key
entstanden sind; der Master-Schalter `ProduktionsauftragHierarchisch` (Teil 7) darf **nicht** ueber
diese Maske am Waechter vorbei umgelegt werden koennen (siehe Teil 7 „Migrationstor") — diese Spec
verweist auf den zentralen Waechter, implementiert ihn aber nicht selbst.

## Fachliche Anforderungen

- Gruppierte Darstellung mindestens der Kategorien: Firmenname/Adresse/Mandant (falls als
  Settings-Keys existent), View-Namen (Teil 1: `Sync:FaHierarchyListeViewName`/
  `Sync:FaHierarchyInfosViewName`), Feature-Toggles (Teil 2–5), Master + abhaengige Schalter
  (Teil 7 — nur Anzeige/Verlinkung, Schreibzugriff geht durch den zentralen Waechter).
- Wiederverwendung der bestehenden typisierten Katalog-Infrastruktur (Bool-Toggle/Int/String,
  ADR 0008) statt einer Parallel-Implementierung.

## Technischer Loesungsentwurf

Neuer Controller/View, der eine kuratierte Teilmenge der `ServiceSettings`-/`AppSettings`-Keys
gruppiert rendert und ueber die bestehenden Set-Mechanismen schreibt (kein neuer Speicherort).
Fuer den Master-Key ruft der Schreibpfad **denselben** Domaenen-Waechter auf, den Teil 7 einfuehrt
(nicht dupliziert implementieren — siehe dortiger Abschnitt „Der Waechter gehoert in die
Service-/Domaenenschicht, nicht in die Maske").

## Migrations-/SQL-Auswirkungen

Keine — keine neuen Tabellen/Spalten, nur eine kuratierte UI ueber bestehende Settings-Tabellen.

## Audit-Feld-Auswirkungen

Keine neuen Entitaeten. Aenderungen an `ServiceSettings`/`AppSettings` ueber diese Maske
protokollieren sich wie bisher (Settings-Tabellen sind laut Fallstrick explizit **keine**
`AuditableEntity`).

## Akzeptanzkriterien

1. Alle in dieser Maske editierbaren Keys bleiben **weiterhin** korrekt in den bestehenden
   Katalog-Tabellen (`ServiceSettings`/`AppSettings`) — kein Parallel-Speicherort.
2. Ein Aenderungsversuch am Master-Schalter ueber diese Maske durchlaeuft denselben Waechter wie
   die generische Oberflaeche (kein Umgehen der Sperre — siehe Teil 7).
3. AKE-Standort (ohne IDEAL-Keys gesetzt) zeigt eine leere oder deaktivierte Sektion, keinen
   Fehler.

## Test-Szenarien

Neues Kapitel „IDEAL Teil 6 — Standorteinstellungen": Aenderung eines View-Namens ueber die neue
Maske spiegelt sich in `/ServiceSettings` wider und umgekehrt; Versuch, den gesperrten Master
umzulegen, wird abgelehnt und protokolliert (sobald Teil 7 den Waechter liefert — Abhaengigkeit
vermerken).

## Deploy

- **Web-App:** ja.
- **Service:** nein.
- **Migration:** nein.
- **Publish-Befehle:** `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`
  (provisorisch).

## Offene Rueckfragen

1. Referenz-Notiz `[[2026-08-03-standorteinstellungen-maske]]` existiert nicht — Umfang dieser
   Spec ist ein Geruest, keine vollstaendige Spezifikation.
2. Vollstaendige Key-Liste fuer die Buendelung.
3. Bleibt die generische `/Settings`/`/ServiceSettings`-Oberflaeche fuer dieselben Keys zusaetzlich
   offen, oder wird diese Maske exklusiv?
4. Rollen/Zugriff fuer diese Maske.

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

1. →Pfad: C:\Git\IDEAL-AKE-WMS\secondbrain\backlog\2026-08-03-standorteinstellungen-maske.md
2. →
3. →ich würde hier nur spezifische standorbezogene felder und auch firmendaten zb. Anschrift, Firmenname, Adresse, etc. einbauen. und eben die Schalter für die Hauptlogiken wie zb. FA-Hierarchie, PPS Settings - berechnend vs aus view, ...
4. →admin

### Ausformuliert (2026-08-06)

**Hinweis zur Zuordnung:** Die Antwort in Feld 3 beantwortet inhaltlich **Frage 2** (Key-Liste).
Die eigentliche Frage 3 (generische Oberflaeche parallel oder exklusiv) war offen und ist unten
beantwortet.

**Zu 1 — Referenz-Notiz existiert.** `secondbrain/backlog/2026-08-03-standorteinstellungen-maske.md`
(sie liegt in `backlog/`, nicht in `ideen/` — daher der Fehlbefund). Sie traegt bereits den Vermerk
"nicht separat spezifizieren, wird als Teil 6 gefuehrt". Diese Spec ist damit kein Geruest mehr
ohne Grundlage; Inhalt beim Ausarbeiten von dort uebernehmen.

**Zu 2 — Umfang der Maske (aus Antwort 3).** Gebuendelt werden:
- **Firmendaten:** Firmenname, Anschrift/Adresse (Standortkopf).
- **Standortbezogene technische Werte:** Mandant/Dataset, die beiden View-Namen aus Teil 1.
- **Schalter der Hauptlogiken:** FA-Hierarchie (Master, nur Anzeige/Verlinkung — Schreibzugriff
  geht durch den Teil-7-Waechter), PPS-Termine (berechnend vs. aus View), Baumanzeige, die
  Listen-Toggles der Teile 3–5.
- **Nicht** in die Maske: alles ohne Standortbezug — die generische Oberflaeche bleibt dafuer
  zustaendig.

**WICHTIG — Scope-Erweiterung, die daraus folgt:** Die **Firmendaten (Firmenname, Anschrift)
existieren bisher vermutlich als gar keine Settings-Keys.** Der Out-of-Scope-Satz dieser Spec
("keine neuen fachlichen Werte, die nicht bereits in Teil 1–5 als Setting-Key entstanden sind")
trifft damit nicht mehr zu. Vor dem Dev-Lauf pruefen:
- Existieren Firmenname/Adresse schon irgendwo (AppSettings, Druckkopf-Konstante, hartkodiert)?
- Falls **ja**: nur buendeln, kein neuer Key.
- Falls **nein**: Teil 6 legt sie als neue `AppSettings`-Keys an (inkl. Katalog-Eintrag und Seed) —
  das ist dann echter Zusatzumfang und gehoert in `affected_code` und die Akzeptanzkriterien.
Die Firmendaten werden ausserdem absehbar im Druckkopf der Teile 3/4/5 gebraucht — wer sie zuerst
braucht, legt sie an; die Reihenfolge ist entsprechend abzustimmen.

**Zu 3 — Die generische Oberflaeche bleibt PARALLEL offen; die Maske ist NICHT exklusiv.**
Begruendung: Die Keys leben weiterhin im bestehenden Katalog (ADR 0008/0011) — die Maske ist eine
kuratierte Sicht darauf, kein zweiter Speicherort und kein zweiter Zugangsweg mit eigenen Regeln.
Wuerde man die Standort-Keys aus der generischen Liste ausblenden, brauchte es eine Ausnahmeliste,
die bei jedem neuen Key gepflegt werden muss — zusaetzliche Komplexitaet ohne Gewinn.
Der Schutz des Master-Schalters haengt ohnehin **nicht** an der Oberflaeche, sondern am zentralen
Waechter in der Domaenenschicht (Teil 7): Er greift ueber **jeden** Weg. Exklusivitaet wuerde also
nichts absichern, was nicht schon abgesichert ist.

**Zu 4 — Zugriff: admin-only**, analog zur bestehenden Settings-Oberflaeche. Keine neue Rolle —
der Dev-Lauf uebernimmt den Class-Level-Filter der generischen Settings-Controller 1:1.

## Kritische Pruefung (2026-08-06)

Anwalt-des-Teufels-Durchsicht **nach** den ausformulierten Freigabe-Antworten. Gegengelesen: diese
Spec, die Backlog-Notiz [[2026-08-03-standorteinstellungen-maske]], die Teil-1-Spec (Toggle-Heimat
`ServiceSettings`, View-Namen), die Teil-7-Spec (Master `ProduktionsauftragHierarchisch` +
`HierarchischeStrukturGuard`), die Uebersichts-Spec (Uebergreifende Rueckfrage 2 + Antwort:
AppSettings fuer UI-Schalter, ServiceSettings fuer Service-Schalter), ADR 0006/0008/0011 sowie der
**echte main-Code**: `ServiceSettingsController` (`IServiceSettingRepository.UpsertAsync`,
Katalog `ServiceSettingDefinitions`), `SettingsController`
(`IAppSettingRepository.SetValueAsync`), `AppSettingKeys`, `RequireAdminAccessAttribute`.

### BLOCKER — vor der Freigabe zu klaeren

**B-1 — Zwei getrennte Konfig-Backends in EINER Maske, aber die Schreib-/Transaktions-Semantik ist
voellig unspezifiziert.** Die Uebersichts-Antwort 2 legt verbindlich fest, dass die Maske
**beide** Welten buendelt: AppSettings (`ProduktionsauftragBaumAnzeige`, Stueckliste-Quelle, die
Listen-Toggles der Teile 3/4/5) **und** ServiceSettings (`Sync:HierarchicalFaEnabled`, die beiden
View-Namen, der Master). Im echten main-Code sind das **zwei physisch getrennte Speicherpfade mit
unterschiedlichen Schreibmethoden**: `IServiceSettingRepository.UpsertAsync(key, value, category,
description)` vs. `IAppSettingRepository.SetValueAsync(key, value)`. Beide bestehenden Controller
schreiben in einer **Per-Key-Schleife ohne Transaktion**; `ServiceSettingsController.SaveSettings`
kann bei einem ungueltigen Int-Wert **mitten in der Schleife teil-scheitern** (bereits gespeicherte
Keys bleiben, spaetere nicht — `ModelState`-Fehler, Rueckgabe der View). Diese Spec sagt zur
zentralen Frage des Features **nichts**: Wie werden in EINEM POST beide Backends konsistent
geschrieben? Was passiert bei Teil-Scheitern (AppSettings gespeichert, ServiceSettings-Int
abgelehnt)? Gibt es Rollback/Transaktion ueber zwei Repositories, oder wird der bekannte
Partial-Save-Zustand bewusst akzeptiert? Ohne diese Entscheidung baut der Dev-Lauf entweder still
einen inkonsistenten Zwischenzustand oder erfindet eine Transaktionslogik, die es fuer AppSettings
heute nicht gibt. **Das ist der Kern des Features, nicht ein Detail** — die Spec bezeichnet sich
selbst als "reines UX-Feature ohne neue Logik", aber das Zusammenfuehren zweier Backends IST neue
Logik.

**B-2 — Master-Schalter: „nur Anzeige/Verlinkung" (Antwort Zu 2) widerspricht AK 2 („Aenderungs-
versuch durchlaeuft denselben Waechter").** Die ausformulierte Antwort sagt fuer den Master
„**nur Anzeige/Verlinkung** — Schreibzugriff geht durch den Teil-7-Waechter". Akzeptanzkriterium 2
und der Technische Loesungsentwurf sagen dagegen, ein **Aenderungsversuch am Master ueber diese
Maske** laufe durch den `HierarchischeStrukturGuard`. Das ist ein direkter Widerspruch: Entweder
ist der Master in der Maske **schreibbar** (dann braucht Teil 6 den Guard und muss ihn aufrufen),
oder er ist **read-only/Link** (dann ist AK 2 gegenstandslos und nicht testbar). Verbindlich
entscheiden — und die Spec konsistent machen. Empfehlung: read-only Anzeige + Deep-Link auf die
generische Maske, damit Teil 6 den Guard gar nicht selbst aufrufen muss (siehe B-3).

**B-3 — Fehlende Abhaengigkeit zu Teil 7 (`depends_on` unvollstaendig).** `depends_on` nennt nur
Teil 1. Aber Teil 6 referenziert (a) den Master-Key `ProduktionsauftragHierarchisch` und (b) den
`HierarchischeStrukturGuard` — **beide entstehen erst in Teil 7** (`ServiceSettingDefinitions`-
Eintrag des Masters, `IdealAkeWms/Services/HierarchischeStrukturGuard.cs`). Laut Uebersicht sind
Teil 2–6 „untereinander unabhaengig und in beliebiger Reihenfolge lieferbar", Teil 7 kommt danach.
Wird Teil 6 **vor** Teil 7 umgesetzt, existiert der Master-Key nicht (nichts anzuzeigen/zu
verlinken) und der Guard-Aufruf laesst sich nicht kompilieren. Aufloesen: **entweder** den Master
in Teil 6 explizit aus dem Scope nehmen, bis Teil 7 steht (Sektion erscheint erst dann — deckt
sich mit AK 3 „leere/deaktivierte Sektion"), **oder** `depends_on` um
[[2026-07-29-standort-ideal-teil-7-spec]] ergaenzen und die Reihenfolge festschreiben. Aktuell ist
beides offen und das Test-Szenario schiebt es mit „sobald Teil 7 den Waechter liefert" vage
beiseite.

**B-4 — Firmendaten sind NEUE Keys ohne definierten Anlege-/Render-/Seed-Weg — Scope, `affected_code`
und `deploy` stimmen nicht.** Bestaetigt am Code: In `AppSettingKeys.cs` existiert **kein**
Firmenname/Adresse/Anschrift-Key (auch sonst nirgends im Code — die einzigen Adress-Treffer sind
`OrderRecipients`, fachlich unbezogen). Die ausformulierte Antwort erkennt das („Scope-Erweiterung"),
zieht die Konsequenz aber nicht durch:
- **Render-Problem:** AppSettings hat — anders als `ServiceSettingDefinitions` — **keinen typisierten
  Katalog**. `SettingsController.Index` rendert nur, was in der DB liegt bzw. was die View hart kennt.
  Fuer neue Firmendaten-Felder braucht die Maske eine **explizite Feld-/Key-Liste** (wo definiert?
  Neuer AppSettings-Katalog? Hardcoded in der View?) — die Spec sagt es nicht.
- **Seed-Problem:** Die Antwort spricht von „inkl. Katalog-Eintrag und Seed" — fuer AppSettings gibt
  es aber **keinen** Katalog wie bei ServiceSettings; ein „Seed" hiesse ein SQL-Seed-Skript
  (+`00_FreshInstall.sql`). Dann ist `deploy.migration: false` **falsch** und `affected_code` muss
  ein `SQL/XX_*.sql` (+ FreshInstall) enthalten. Steht beides nicht drin.
- **`affected_code`:** listet `AppSettingKeys.cs`, aber weder Seed-SQL noch (falls doch
  ServiceSettings) `ServiceSettingDefinitionsTests.cs` (Drift-Guard-InlineData, ADR 0008 Pflicht).

Solange nicht entschieden ist, **ob** die Firmendaten in diesem Teil neu angelegt werden (und wenn
ja, in welchem Backend + mit welchem Seed), ist der Umfang der Spec offen.

### SOLLTE

**S-1 — Cross-Part-Reihenfolge Firmendaten vs. Druckkopf Teil 3/4/5 nicht aufgeloest.** Die Antwort
sagt selbst, die Firmendaten wuerden „absehbar im Druckkopf der Teile 3/4/5 gebraucht; wer sie
zuerst braucht, legt sie an — Reihenfolge abzustimmen". Genau diese Abstimmung fehlt. Wenn Teil 3
(Referenzimplementierung Druck) vor Teil 6 landet und einen Firmennamen im Kopf braucht, ist
unklar, wer den Key definiert. Als konkrete `depends_on`-/Reihenfolge-Entscheidung festhalten,
nicht als Prosa.

**S-2 — ADR-0006-Nachweis nur halb.** Antwort 4 (admin-only, bestehender `[RequireAdminAccess]`,
keine neue Rolle) ist korrekt und deckt sich mit `ServiceSettingsController`/`SettingsController`
(beide class-level `[RequireAdminAccess]`). Da **kein neuer Filter/keine neue Rolle** entsteht,
entfaellt die RoleOverview-Pflicht — aber der neue Controller gehoert dennoch in die
codebase-Karte (`secondbrain/codebase/controller.md`). In den Aufgaben/Checkliste vermerken.

**S-3 — Redundanz/Drift zur generischen Maske akzeptiert, aber Lese-Zusammenfuehrung offen.**
Antwort 3 (Maske nicht exklusiv, Keys bleiben auch generisch editierbar) ist plausibel begruendet
(ein Speicherort, Guard schuetzt backend-seitig). Offen bleibt die **Lese-Seite**: Die Maske muss
ServiceSettings ueber `ServiceSettingDefinitions`+`IServiceSettingRepository` und AppSettings ueber
`IAppSettingRepository` **gemeinsam** in ein ViewModel ziehen und die Typ-/Bool-/Int-Behandlung des
generischen `ServiceSettingsController.SaveSettings` (Checkbox→"true"/"false", Int-Parse) je Backend
korrekt spiegeln. Kein Blocker, aber im Loesungsentwurf zu benennen, sonst driftet die kuratierte
Sicht vom generischen Verhalten ab.

### HINWEIS

**H-1 — View-Namen-Validierung liegt korrekt beim Sync, nicht in der Maske.** Falls die beiden
View-Namen hier editierbar sind: Die Whitelist-Regex/`QUOTENAME`-Absicherung aus Teil 1 sitzt im
Sync-Service (`FaHierarchySql.ValidateViewName`) zur **Lesezeit**, nicht im Settings-Schreibpfad.
Die generische Maske speichert den String roh; Teil 6 tut dasselbe — **kein** neuer
Injection-Vektor, aber die Maske darf keine Validierung **vortaeuschen** (ein ungueltiger Name wird
erst beim naechsten Sync-Lauf als Fehlermail sichtbar). Als UX-Hinweis vermerken.

**H-2 — Controllername `StandortEinstellungenController` (provisorisch).** Vertretbar; die
Namens-Konvention der Uebersicht verbietet nur den Standort **„Ideal"** in Code-Bezeichnern,
nicht das generische „Standort". Kein Handlungsbedarf, nur zur Kenntnis.

**H-3 — AK sind grob, aber ehrlich.** AK 3 (AKE-Standort → leere/deaktivierte Sektion) ist gut und
deckt den Regressionsfall. Es fehlen AK zu B-1 (Zwei-Backend-Schreiben/Teil-Scheitern) und B-4
(Firmendaten-Anlage) — nachziehen, sobald diese Blocker entschieden sind.

### Verdikt

Die Grundidee (kuratierte Sicht auf bestehende Keys, kein zweiter Speicherort, admin-only, Guard
backend-seitig) ist richtig und deckt sich mit der Architektur. Aber die Spec ist an den zwei
Stellen unspezifiziert, die das Feature ausmachen: (1) **wie zwei Konfig-Backends in einem POST
konsistent geschrieben werden** (B-1) und (2) **ob/wie die neuen Firmendaten-Keys entstehen** (B-4)
— plus ein echter Selbstwiderspruch beim Master (B-2) und eine fehlende Teil-7-Abhaengigkeit (B-3).
Das sind keine Feinheiten, sondern der Kern bzw. Freigabe-Voraussetzungen.

NACHBESSERUNG NOETIG: Zwei-Backend-Schreibsemantik/Transaktion (B-1), Master read-only-vs-Guard-
Widerspruch (B-2), Teil-7-Abhaengigkeit im `depends_on`/Scope (B-3) und der Anlege-/Seed-/Deploy-Weg
der neuen Firmendaten-Keys (B-4) sind vor der Freigabe zu entscheiden.
