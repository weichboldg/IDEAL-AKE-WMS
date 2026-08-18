---
type: spec
title: "IDEAL-Standort Teil 6 — Standorteinstellungen-Maske"
slug: 2026-07-29-standort-ideal-teil-6-spec
status: InUmsetzung
created: 2026-08-06
updated: 2026-08-18
source_backlog: "[[2026-07-29-Standort-IDEAL]]"
depends_on: "[[2026-07-29-standort-ideal-teil-1-spec]], [[2026-07-29-standort-ideal-teil-3-spec]], [[2026-07-29-standort-ideal-teil-7-spec]]"
task: ""
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
affected_code:
  - IdealAkeWms/Controllers/StandortEinstellungenController.cs (neu, Name provisorisch)
  - IdealAkeWms/Views/StandortEinstellungen/Index.cshtml (neu)
  - IdealAkeWms/Models/ServiceSettingDefinitions.cs
  - IdealAkeWms/Models/AppSettingKeys.cs (Firmendaten-Keys — gemeinsamer Definitionsort mit Teil 3, siehe Fachliche Anforderungen)
  - IdealAkeWms/Data/Repositories/IAppSettingRepository.cs + AppSettingRepository.cs (transaktionsfaehige Variante, siehe Technischer Loesungsentwurf)
  - IdealAkeWms/Data/Repositories/IServiceSettingRepository.cs + ServiceSettingRepository.cs (transaktionsfaehige Variante)
  - secondbrain/codebase/controller.md (neuer Controller-Eintrag, kein neuer Filter/keine neue Rolle)
  - docs/TESTSZENARIEN.md
  - secondbrain/tests/testszenarien-index.md
open_questions: []
epic: false
etappen: []
deploy:
  web: true
  service: false
  migration: false
freigabe_entscheidung: ""
freigabe_von: "Gerald Weichbold"
freigabe_am: 2026-08-12
---

## Ziel / Nutzen (das Warum)

Bis zu diesem Teil laufen alle IDEAL-Schalter (View-Namen, Feature-Toggles, der Master
`ProduktionsauftragHierarchisch` und seine Abhaengigen) als normale Eintraege in der generischen
`ServiceSettings`/`AppSettings`-Oberflaeche — das kostet laut Notiz „Null Zusatzaufwand", ist aber
unuebersichtlich, sobald mehrere standortspezifische Werte (Firmenname, Adresse, Mandant,
View-Namen, Schalter) zusammengehoeren. Teil 6 buendelt diese Werte in einer eigenen, gruppierten
Maske — inklusive der bisher nirgends vorhandenen Firmendaten (Firmenname, Anschrift), die hier
erstmals als `AppSettings`-Werte entstehen. Der Master-Schalter `ProduktionsauftragHierarchisch`
(Teil 7) wird in dieser Maske **nur read-only** angezeigt — sein Schreibweg ist die eigene,
dedizierte Teil-7-Umschaltseite, kein Kontrollkaestchen zwischen zwanzig anderen Einstellungen.

Grundlage ist die Backlog-Notiz `secondbrain/backlog/2026-08-03-standorteinstellungen-maske.md`
(dort vermerkt: „nicht separat spezifizieren, wird als Teil 6 gefuehrt").

## Umfang (In-Scope / Out-of-Scope)

**In-Scope:** eine neue, gruppierte Admin-Maske, die (a) bestehende `ServiceSettings`-/
`AppSettings`-Keys mit Standortbezug (View-Namen aus Teil 1, Toggles aus Teil 2–5) liest und
schreibt, und (b) neue Firmendaten-Keys (Firmenname, Anschrift) als `AppSettings`-Werte anzeigt und
schreibt — deren Definition in `AppSettingKeys.cs` gemeinsam mit Teil 3 abgestimmt ist (siehe
Fachliche Anforderungen). Kein neuer Speicherort fuer bestehende Keys, nur eine kuratierte
Oberflaeche ueber die bestehende Katalog-Infrastruktur (ADR 0008/0011) plus die neuen
Firmendaten-Werte.

**Out-of-Scope:** Der Master-Schalter `ProduktionsauftragHierarchisch` (Teil 7) ist in dieser Maske
**strikt read-only** (Zustand, Sperrstatus, seit wann) mit Link zur dedizierten
Teil-7-Umschaltseite — er ist **niemals** Teil des Sammel-POSTs dieser Maske, und diese Maske ruft
den `HierarchischeStrukturGuard` nicht auf. Keine neue Rolle.

## Fachliche Anforderungen

- Gruppierte Darstellung mindestens der Kategorien: Firmendaten (Firmenname, Anschrift — neu,
  `AppSettings`), Mandant/Dataset + View-Namen (Teil 1: `Sync:FaHierarchyListeViewName`/
  `Sync:FaHierarchyInfosViewName`), Feature-Toggles (Teil 2–5), Master + abhaengige Schalter
  (Teil 7 — **nur read-only Statusanzeige**: an/aus, gesperrt ja/nein, seit wann, mit Link zur
  Teil-7-Umschaltseite; **kein** Schreib-Bedienelement dafuer in dieser Maske).
- Wiederverwendung der bestehenden typisierten Katalog-Infrastruktur (Bool-Toggle/Int/String,
  ADR 0008) statt einer Parallel-Implementierung.
- **Firmendaten-Definitionsort:** `AppSettingKeys.cs` ist der EINE gemeinsame Definitionsort. Nach
  Antwort S-1 ist Teil 3 (Druckkopf) der voraussichtliche Erstbraucher und legt die Konstanten dort
  an; Teil 6 fuegt sie danach nur der kuratierten Maske hinzu. **Fallback**, falls Teil 3 die Keys
  bis zum Teil-6-Dev-Lauf nicht angelegt hat: Teil 6 legt sie dann selbst in `AppSettingKeys.cs` an
  — so entsteht in jedem Fall genau ein Definitionsort, unabhaengig von der Merge-Reihenfolge, und
  Teil 6 bleibt eigenstaendig lieferbar.
- **Zwei-Backend-Schreiben in einem Speichervorgang:** ein POST kann sowohl `AppSettings`- als auch
  `ServiceSettings`-Werte enthalten und muss beide **atomar** schreiben (Details siehe Technischer
  Loesungsentwurf).

## Technischer Loesungsentwurf

Neuer Controller/View, der eine kuratierte Teilmenge der `ServiceSettings`-/`AppSettings`-Keys
gruppiert rendert und ueber die bestehenden Set-Mechanismen schreibt (kein neuer Speicherort). Fuer
den Master-Key liest die Maske ausschliesslich den in Teil 7 gecachten Sperrzustand (siehe
dortiger Abschnitt „Anzeige gecacht") und zeigt ihn read-only mit Link zur Teil-7-Umschaltseite —
diese Maske ruft den `HierarchischeStrukturGuard` **nicht** auf und bietet kein
Schreib-Bedienelement fuer den Master.

**Zwei-Backend-POST (AppSettings + ServiceSettings) — atomare Schreibsemantik.**
Beide Backends leben in derselben Datenbank und teilen sich pro Request dieselbe scoped
`ApplicationDbContext`-Instanz (verifiziert: `IAppSettingRepository` → `CachedSettingRepository` →
`AppSettingRepository` → `ApplicationDbContext`; `IServiceSettingRepository` →
`ServiceSettingRepository` → `ApplicationDbContext`) — eine gemeinsame Transaktion ist moeglich,
kein Zwei-Phasen-Commit noetig. Beide Repository-Methoden rufen aber **intern**
`SaveChangesAsync` (`AppSettingRepository.SetValueAsync`, `ServiceSettingRepository.UpsertAsync`) —
es gibt heute **keine** Transaktions-Naht.

Anforderung (verbindlich, nicht optional): Ein Speichervorgang schreibt beide Backends in **einer**
Transaktion. Schlaegt ein Teil fehl (z. B. ein ungueltiger Int-Wert bei einem ServiceSettings-Key),
wird **alles** zurueckgerollt und die Maske zeigt den Fehler mit den **eingegebenen Werten** — kein
Partial-Save, keine verlorene Eingabe.

Umsetzungsweg (vom Dev-Lauf zu entscheiden — zwei tragfaehige Optionen mit ihrer jeweiligen
Spannung):
- (a) eine gemeinsame `BeginTransactionAsync`-Klammer um beide Repository-Aufrufe — die
  bestehenden Repository-Interfaces geben dafuer aktuell **keinen** `DbContext`-Zugriff frei, das
  ist eine Spannung zu ADR 0001 („Datenzugriff nur ueber Repository-Interfaces") und muss sauber
  geloest werden (z. B. ein duenner Unit-of-Work-Baustein statt direktem Context-Zugriff im
  Controller);
- (b) SaveChanges-freie Repository-Varianten, die der aufrufende Service explizit committet.

Zusaetzlich zu beachten: `CachedSettingRepository.SetValueAsync` entfernt den Cache-Key **nach**
dem inneren `SaveChanges` — bei einem Rollback bleibt die geaenderte Entitaet im Change-Tracker der
geteilten Context-Instanz haengen (EF revertet Tracking nicht automatisch). Der
Transaktionsentwurf muss das beruecksichtigen (z. B. Cache-Invalidierung erst nach erfolgreichem
Commit, Tracking bei Rollback sauber zuruecksetzen/Context neu beziehen).

## Migrations-/SQL-Auswirkungen

Keine Migration, kein `SQL/XX_*.sql`. Die neuen Firmendaten-Keys (Firmenname, Anschrift) sind reine
`AppSettings`-Werte ohne Katalog und ohne Seed: `AppSettingRepository.GetValueAsync` liefert bei
einem nie geschriebenen Key `null` (keine Exception), `SetValueAsync` legt die Zeile beim ersten
Speichern per `Add` an (am Code verifiziert). Die kuratierte Maske bringt dafuer eine **eigene,
explizite Feldliste** mit und rendert sie unabhaengig davon, ob bereits eine DB-Zeile existiert.
Bewusst akzeptierte Folge: Ein Firmendaten-Feld, das nie gespeichert wurde, erscheint **nicht** in
der generischen `/Settings`-Oberflaeche (die zeigt nur vorhandene DB-Zeilen) — ein kosmetischer
Nachteil gegenueber einem Seed, bewusst in Kauf genommen.

## Audit-Feld-Auswirkungen

Keine neuen Entitaeten mit `AuditableEntity`. Aenderungen an `ServiceSettings`/`AppSettings` ueber
diese Maske protokollieren sich wie bisher (Settings-Tabellen sind laut Fallstrick explizit
**keine** `AuditableEntity`). Das Audit-Protokoll des Master-Schalters (wer/wann umgelegt) gehoert
vollstaendig zu Teil 7 (eigener `SyncLog`-Eintrag) — diese Maske schreibt den Master nicht und
erzeugt daher keinen eigenen Audit-Eintrag dafuer.

## Akzeptanzkriterien

1. Alle in dieser Maske editierbaren Keys bleiben **weiterhin** korrekt in den bestehenden
   Katalog-Tabellen (`ServiceSettings`/`AppSettings`) — kein Parallel-Speicherort.
2. Der Master-Schalter `ProduktionsauftragHierarchisch` ist in dieser Maske **ausschliesslich
   read-only** sichtbar (Zustand an/aus, gesperrt ja/nein, seit wann) mit funktionierendem Link zur
   Teil-7-Umschaltseite; die Maske bietet **kein** Schreib-Bedienelement dafuer und ruft den
   `HierarchischeStrukturGuard` nicht auf.
3. AKE-Standort (ohne IDEAL-Keys gesetzt) zeigt eine leere oder deaktivierte Sektion, keinen
   Fehler.
4. Ein Speichervorgang, der sowohl AppSettings- als auch ServiceSettings-Werte in einem POST
   aendert, schreibt beide atomar; schlaegt ein Teil fehl, werden beide zurueckgerollt und die
   Maske zeigt den Fehler mit den eingegebenen Werten (kein Partial-Save).
5. Ein Firmendaten-Feld ohne vorhandene DB-Zeile wird leer gerendert (kein Fehler) und beim ersten
   Speichern als neue `AppSettings`-Zeile angelegt.
6. Zugriff auf die Maske ist auf `[RequireAdminAccess]` beschraenkt (Class-Level, wie
   `ServiceSettingsController`/`SettingsController`) — ohne Admin-Rolle ist die Maske nicht
   erreichbar.

## Test-Szenarien

Neues Kapitel „IDEAL Teil 6 — Standorteinstellungen":

- Aenderung eines View-Namens ueber die neue Maske spiegelt sich in `/ServiceSettings` wider und
  umgekehrt.
- Der Master-Schalter erscheint in der neuen Maske ausschliesslich als read-only Status (inkl.
  Link zur Teil-7-Umschaltseite) — kein Bedienelement zum Umschalten vorhanden; ein direkter
  Schreibversuch auf den Master-Key ueber diese Maske (z. B. manipulierter Request) wird
  serverseitig abgelehnt.
- Gemischte Aenderung in einem Speichervorgang (ein gueltiges AppSettings-Feld + ein ungueltiger
  ServiceSettings-Int-Wert): keiner der beiden Werte wird gespeichert, Fehleranzeige behaelt beide
  eingegebenen Werte.
- Firmendaten-Feld (Firmenname) erstmalig befuellen: Wert wird gespeichert, erscheint danach
  **nicht** automatisch in der generischen `/Settings`-Liste — als bekannte, akzeptierte
  Abweichung dokumentieren, nicht als Fehler werten.
- AKE-Standort: Maske zeigt leere/deaktivierte Sektionen ohne Fehler.

Nach Abschluss `secondbrain/tests/testszenarien-index.md` nachziehen.

## Deploy

- **Web-App:** ja.
- **Service:** nein.
- **Migration:** nein.
- **Publish-Befehle:** `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`
  (provisorisch, vom Dev-Lauf gegen den tatsaechlichen Diff zu bestaetigen).

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

## Antworten auf die Kritische Pruefung (2026-08-06)

**Zu B-4 — Firmendaten existieren heute NICHT. Sie entstehen hier — aber ohne Seed und ohne
Migration.**
Bestaetigt: Firmenname/Anschrift gibt es bisher nirgends als Wert. Der Weg ist trotzdem
leichtgewichtiger, als die Pruefung annimmt:
- **Backend: AppSettings** (ADR 0011) — es sind reine Anzeige-/Druckkopf-Daten, kein
  Service-Verhalten.
- **Kein Seed-SQL, kein `deploy.migration: true`.** Die Maske ist ohnehin **kuratiert** — sie
  bringt ihre **eigene, explizite Feldliste** mit (das ist ihr Wesen, nicht ein Workaround). Sie
  rendert die Felder unabhaengig davon, ob eine DB-Zeile existiert, und legt sie beim ersten
  Speichern an. Ein Katalog wie bei `ServiceSettings` wird dafuer nicht gebraucht.
- **Keys in `AppSettingKeys.cs`** aufnehmen (Konstanten), Feldliste + Beschriftung im
  ViewModel/der View der neuen Maske.
- **Bewusst akzeptierte Folge:** Solange ein Firmendaten-Feld nie gespeichert wurde, taucht es in
  der **generischen** Settings-Oberflaeche nicht auf (die zeigt nur, was in der DB liegt). Das ist
  ein kosmetischer Nachteil gegenueber einem Seed — und deutlich billiger als eine
  daten-schreibende Migration plus `00_FreshInstall.sql`-Pflege fuer drei Textfelder.
- `deploy.migration` bleibt damit **`false`**; `affected_code` braucht **kein** `SQL/XX_*.sql`.

**Zu S-1 — Reihenfolge Firmendaten vs. Druckkopf: Teil 3 legt sie an, Teil 6 buendelt sie.**
Als Entscheidung, nicht als Prosa: **Teil 3** (Referenzimplementierung des Druckgeruests) braucht
den Firmennamen zuerst im Druckkopf und **definiert daher die Keys** in `AppSettingKeys.cs` samt
Lesepfad. **Teil 6** fuegt sie nur der kuratierten Maske hinzu. Damit gibt es genau einen
Definitionsort, unabhaengig davon, welcher Teil zuerst gemerged wird. `depends_on` von Teil 6 um
Teil 3 ergaenzen.

**Zu B-1 — Zwei Konfig-Backends in einem POST: kein verteiltes Transaktions-Kunststueck.**
`AppSettings` und `ServiceSettings` liegen in **derselben Datenbank** — eine gemeinsame
EF-Transaktion ueber beide Repositories genuegt, kein Zwei-Phasen-Commit, keine Kompensationslogik.
Verbindlich:
- Ein Speichervorgang schreibt **beide** Backends in **einer** Transaktion; schlaegt ein Teil fehl,
  wird alles zurueckgerollt und die Maske zeigt den Fehler mit den **eingegebenen Werten** an
  (kein halb gespeicherter Zustand, keine verlorene Eingabe).
- **Der Master-Schalter ist davon ausgenommen** — er laeuft immer durch den Waechter (siehe B-2)
  und wird nie als Teil des Sammel-POST geschrieben.

**Zu B-2 — Master-Widerspruch aufgeloest: In dieser Maske nur ANZEIGEN, nicht schreiben.**
Die Spec sagte an einer Stelle „nur Anzeige/Verlinkung", an anderer „Schreibversuch durchlaeuft den
Waechter". Verbindlich ist die **engere** Lesart: Teil 6 zeigt den Master-Zustand (an/aus, gesperrt
ja/nein, seit wann) **read-only** an und verlinkt zum Umschaltweg. Umgelegt wird er ausschliesslich
ueber den Weg, den Teil 7 baut — mit Bestaetigungsdialog und Waechter.
Begruendung: Ein Einwegtor gehoert nicht in eine Sammelmaske, in der man nebenbei die Anschrift
korrigiert. Ein versehentlicher Klick soll dort gar nicht moeglich sein.

**Zu B-3 — `depends_on` um Teil 7 und Teil 3 ergaenzen.** Die Master-Anzeige (Sperrzustand,
„seit wann") setzt den Waechter und den gecachten Zustand aus Teil 7 voraus; die Firmendaten-Keys
kommen aus Teil 3 (siehe S-1). Ohne Teil 7 kann Teil 6 den Sperrzustand nicht darstellen.

**Zu S-2 — uebernommen.** Kein neuer Filter, keine neue Rolle (`[RequireAdminAccess]`,
Class-Level, wie `ServiceSettingsController`/`SettingsController`) ⇒ **keine** RoleOverview-Pflicht.
Der neue Controller gehoert aber in `secondbrain/codebase/controller.md` — in die Checkliste.

**Zu S-3 — uebernommen.** Der Loesungsentwurf benennt ausdruecklich, dass die Maske
`ServiceSettings` (ueber `ServiceSettingDefinitions` + `IServiceSettingRepository`) und
`AppSettings` (ueber `IAppSettingRepository`) **gemeinsam** in ein ViewModel zieht und die
Typ-Behandlung des generischen `ServiceSettingsController.SaveSettings` je Backend **spiegelt**
(Checkbox → "true"/"false", Int-Parse). Sonst driftet die kuratierte Sicht vom generischen
Verhalten ab.

**Zu H-1 — uebernommen, als UX-Hinweis in der Maske.** Die View-Namen werden hier **roh**
gespeichert; die Whitelist-/`QUOTENAME`-Absicherung sitzt im Sync (`FaHierarchySql.ValidateViewName`)
und greift erst zur Lesezeit. Die Maske darf **keine Validierung vortaeuschen** — stattdessen ein
Hinweis, dass ein ungueltiger Name erst beim naechsten Sync-Lauf als Fehlermail auffaellt.

**Zu H-3 — AK nachziehen**, sobald B-1/B-2/B-4 eingearbeitet sind: je ein AK zur
Transaktions-Semantik (Teil-Scheitern rollt alles zurueck) und zur Firmendaten-Anlage (Feld ohne
DB-Zeile wird leer gerendert und beim ersten Speichern angelegt).

## Kritische Pruefung (2026-08-07)

Zweiter Anwalt-des-Teufels-Durchgang **nach** dem Antwortblock „Antworten auf die Kritische
Pruefung (2026-08-06)". Gegengelesen: diese Spec komplett, Teil-1-Spec (ServiceSettings-Keys),
Teil-3-Spec (angeblicher Firmendaten-Definitionsort), Teil-7-Spec (Master + Guard), die Uebersicht,
ADR 0006/0008/0011 sowie der **echte main-Code**: `AppSettingRepository`, `CachedSettingRepository`,
`ServiceSettingRepository`, `SettingsController`, `ServiceSettingsController`,
`RequireAdminAccessAttribute`, `AppSettingKeys`, `Program.cs` (DI-Registrierung). Die
Freigabe-Entscheidungen sind inhaltlich groesstenteils richtig — aber sie leben **ausschliesslich**
im Antwortblock; Rumpf und Frontmatter sind nicht nachgezogen und widersprechen den Antworten an
mehreren Stellen weiterhin. Zusaetzlich bricht die read-only-Entscheidung eine Annahme in Teil 7.

### BLOCKER — vor der Freigabe zu klaeren

**Z2-B1 — Rumpf und Frontmatter sind NICHT nachgezogen; der Body widerspricht den eigenen Antworten
weiter (ein Dev setzt die falsche Variante um).** Die Antworten sind Entscheidungen auf dem Papier,
aber die massgeblichen Spec-Teile stehen unveraendert im alten Stand:
- **Frontmatter `depends_on` (Zeile 9)** nennt nur Teil 1. Antwort B-3 verlangt verbindlich
  **+ Teil 7 + Teil 3**. Nicht eingetragen — Dataview/HOME-Dashboard zeigen die Abhaengigkeiten
  falsch.
- **Frontmatter `open_questions` (Zeilen 20-24)** listen weiterhin alle vier Fragen als offen,
  obwohl beantwortet. Wie bei Teil 7 (H7-2): solange sie stehen, gilt die Spec dem Dashboard als
  offen. Leeren.
- **Master-Selbstwiderspruch (der Erstreview-Blocker B-2) ist physisch immer noch im Body:**
  Akzeptanzkriterium 2 (Zeilen 90-93), Fachliche Anforderung (Zeile 66) und Technischer
  Loesungsentwurf (Zeilen 74-76) beschreiben den Master weiterhin als **in dieser Maske schreibbar,
  der den Waechter aufruft** — das genaue Gegenteil der Antwort B-2 („nur ANZEIGEN, nie schreiben").
  Ein Dev liest den Body, nicht den Antwortblock, und baut den schreibbaren Master samt
  Guard-Aufruf.
- **Out-of-Scope (Zeilen 56-59)** sagt weiter „keine neuen fachlichen Werte" — steht gegen die
  Firmendaten-Diskussion; `affected_code` listet `AppSettingKeys.cs`, obwohl S-1 die Definition zu
  Teil 3 verschiebt (siehe Z2-S1).
- **Akzeptanzkriterien** sind weiter nur 3; Antwort H-3 versprach je ein AK zu Transaktion und
  Firmendaten-Anlage. Fehlen. `affected_code` fehlt der laut S-2 verbindliche
  `secondbrain/codebase/controller.md`-Eintrag sowie ViewModel/Transaktionslogik.

Solange Body + Frontmatter nicht auf die Antworten gezogen sind, ist die Spec nicht umsetzbar —
sie beschreibt zwei gegensaetzliche Features gleichzeitig.

**Z2-B2 — Master „read-only" (Antwort B-2) kollidiert mit Teil 7, das die Teil-6-Maske als
GESICHERTEN SCHREIBWEG annimmt und testet.** Teil 7 fuehrt die Teil-6-Maske ausdruecklich als
Schreibpfad, den der Waechter abfaengt: Loesungsentwurf „`HierarchischeStrukturGuard` … wird von
**jedem** Schreibpfad auf den Master-Key aufgerufen (generische ServiceSettings-Maske,
**Teil-6-Maske**, kuenftige API)", Teil-7-**AK 2** („Deaktivierungsversuch ueber **jeden**
Schreibweg (generische Maske, **Teil-6-Maske**) abgelehnt und protokolliert") und das
Teil-7-Test-Szenario („Versuch ueber die generische Maske UND … die Teil-6-Maske beide abgelehnt").
Macht Teil 6 den Master jetzt **read-only** (kein Schreibweg), ist die Teil-6-Klausel in Teil-7-AK 2
und im Teil-7-Test **gegenstandslos und nicht testbar** — die beiden Specs widersprechen sich, ob
Teil 6 den Master ueberhaupt schreiben kann. Muss teiluebergreifend aufgeloest werden: entweder
Teil 7 (AK 2, Loesungsentwurf, Test) auf „Teil 6 ist read-only, kein Schreibweg" nachziehen, oder
die read-only-Entscheidung revidieren. Ich darf nur diese Datei editieren — daher hier als
Cross-Spec-Blocker vermerkt (Teil 7 nachzuziehen). Nebeneffekt, positiv: die Teil-7-Sorge S7-6
(„Teil 6 macht spaeter einen zweiten Schreibweg auf") entfaellt durch read-only — aber der
Teil-7-Text bleibt bis zur Korrektur stale.

### SOLLTE

**Z2-S1 — Die S-1-Entscheidung „Teil 3 legt die Firmendaten-Keys an" ist in Teil 3 NICHT abgebildet
— unbesitzte Abhaengigkeit; zudem widerspricht sich der Antwortblock selbst.** Verifiziert:
Die Teil-3-Spec erwaehnt Firmenname/Anschrift/Druckkopf-Firmendaten **nirgends** (Volltext-Suche
leer); ihr `AppSettingKeys.cs`-Bezug betrifft ausschliesslich den Kommissionierlisten-Toggle
`FaHierarchyKommissionierlistenAktiv`. Teil 3 ownt die Keys also aktuell **nicht**. Gleichzeitig
widerspricht sich der Teil-6-Antwortblock: **B-4** sagt „Keys in `AppSettingKeys.cs` aufnehmen"
(Teil 6 als Eigentuemer), **S-1** sagt „**Teil 3** definiert die Keys … Teil 6 fuegt sie nur der
Maske hinzu" — und `affected_code` (Teil 6) listet `AppSettingKeys.cs`. Wer die Konstanten anlegt,
ist damit offen. Aufloesen: entweder Teil 3 verbindlich zum Eigentuemer machen (dann Teil-3-Spec
nachziehen — sie plant die Firmendaten heute nicht) **oder** Teil 6 als Eigentuemer festschreiben
(dann `depends_on` Teil 3 fuer die Keys wieder streichen). So wie jetzt zeigt `depends_on` Teil 3
auf einen Definitionsort, den es dort nicht gibt.

**Z2-S2 — B-1 „eine gemeinsame EF-Transaktion" ist technisch moeglich (verifiziert), aber der Weg
dahin ist im Antwortblock verharmlost.** Am echten Code bestaetigt: `IAppSettingRepository` →
`CachedSettingRepository` → `AppSettingRepository` → `ApplicationDbContext` (scoped);
`IServiceSettingRepository` → `ServiceSettingRepository` → `ApplicationDbContext` (scoped). Beide
teilen pro Request **dieselbe** scoped-Instanz — eine gemeinsame Transaktion ist also moeglich, die
Kernaussage stimmt, kein Zwei-Phasen-Commit noetig. **Aber** die Antwort uebergeht das Wesentliche:
- Beide Repo-Methoden rufen **intern `SaveChangesAsync()`** (`AppSettingRepository.SetValueAsync`
  Z. 46, `ServiceSettingRepository.UpsertAsync` Z. 56). Es gibt heute **keine Transaktions-Naht**.
  Atomares Alles-oder-Nichts erfordert ein explizit geoeffnetes `BeginTransactionAsync` mit Zugriff
  auf den `ApplicationDbContext` — den **kein Repository-Interface freigibt**. Der neue
  Controller/Service muesste den DbContext also **direkt** anfassen, in Spannung zu ADR 0001
  („Datenzugriff nur ueber Repository-Interfaces"). Wo die Transaktion lebt und wie auf den Context
  zugegriffen wird, muss der Loesungsentwurf benennen.
- `CachedSettingRepository.SetValueAsync` entfernt den Cache-Key **nach** dem inneren SaveChanges;
  bei einem Rollback bleibt die geaenderte Entitaet im Change-Tracker der geteilten Context-Instanz
  (EF revertet sie nicht) → in-memory-Drift innerhalb desselben Requests. „Kein Kunststueck /
  genuegt" untertreibt diese Interaktion.

Kein Neukonzept — aber ohne diese Praezisierung baut der Dev entweder den vom Erstreview gewarnten
Partial-Save oder erfindet eine Ad-hoc-Transaktionslogik.

**Z2-S3 — Reihenfolge-Widerspruch zur Uebersicht: Teil 6 ist jetzt NACH Teil 7 lieferbar.**
Die Uebersicht haelt fest, die Schalter der Teile 1-6 seien „**unabhaengig** vom Master" und „in
beliebiger Reihenfolge lieferbar", Teil 7 komme danach. Antwort B-3 macht Teil 6 aber
`depends_on` Teil 7 (die Master-Anzeige — Sperrzustand, „seit wann" — braucht Guard + gecachten
Zustand aus Teil 7). Damit ist Teil 6 **nicht mehr** frei vor Teil 7 lieferbar (Nummer 6 vor 7,
Lieferung 7 vor 6). Entweder in Uebersicht/Reihenfolge festschreiben, **oder** Teil 6 splitten:
Firmendaten + View-Namen (ohne Teil-7-Dep) zuerst, die Master-Anzeige als spaeterer Nachtrag nach
Teil 7. So wie jetzt ist die Uebersicht-Aussage „Teil 2-6 in beliebiger Reihenfolge" fuer Teil 6
faktisch falsch.

### HINWEIS

**Z2-H1 — B-4-Render-/Seed-Weg am Code verifiziert korrekt (Lob mit Beleg).** Bestaetigt:
`AppSettingRepository.GetValueAsync` liefert bei nie geschriebenem Key `null` (keine Exception,
kein Default-Wurf), `SetValueAsync` legt die Zeile beim ersten Speichern per `Add` an. Die
kuratierte Maske kann Felder ohne DB-Zeile also leer rendern und beim ersten Speichern anlegen —
**kein Seed, keine Migration noetig**, `deploy.migration: false` bleibt korrekt. Auch die
„kosmetische Folge" stimmt: `SettingsController.Index` rendert nur DB-Zeilen (`GetAllAsync`),
`ServiceSettingsController` rendert dagegen den Katalog inkl. Defaults — ein nie gespeicherter
Firmendaten-Key erscheint in der generischen `/Settings`-Maske folglich nicht. B-4 ist sachlich
richtig; nur das zugehoerige AK fehlt noch (Z2-B1).

**Z2-H2 — ADR 0006 / admin-only bestaetigt.** `[RequireAdminAccess]` ist class-level auf
`SettingsController` (Z. 10) und `ServiceSettingsController` (Z. 14); kein neuer Filter, keine neue
Rolle → keine RoleOverview-Pflicht (S-2 korrekt). Der `controller.md`-Eintrag bleibt Pflicht — er
fehlt bislang im `affected_code` (siehe Z2-B1).

### Verdikt

Die fachlichen Entscheidungen (kuratierte Sicht, kein zweiter Speicherort, admin-only, Master
read-only, AppSettings ohne Seed, gemeinsame Transaktion in einer DB) sind richtig und am Code
gedeckt — B-4 und die Transaktions-Kernaussage habe ich am echten Code verifiziert. Aber die
Freigabe scheitert daran, dass diese Entscheidungen **nur im Antwortblock** stehen: Rumpf und
Frontmatter sind nicht nachgezogen und tragen den Master-Selbstwiderspruch, die falschen
`depends_on` und die offenen `open_questions` unveraendert weiter (Z2-B1). Dazu bricht die
read-only-Entscheidung eine explizite Annahme in Teil 7 (Z2-B2), und die Firmendaten-Eigentuemer-
frage ist zwischen Teil 3, Teil 6, B-4 und S-1 widerspruechlich (Z2-S1).

NACHBESSERUNG NOETIG: Body + Frontmatter auf die Antworten ziehen (Master read-only ueberall,
`depends_on` +Teil 7/+Teil 3, `open_questions` leeren, AK ergaenzen, `controller.md` in
`affected_code`) — Z2-B1; Teil-7-Widerspruch zur read-only-Maske aufloesen — Z2-B2;
Firmendaten-Key-Eigentuemer zwischen Teil 3/Teil 6 eindeutig festlegen — Z2-S1.

### Nachbesserung 2 (2026-08-07)

Rumpf und Frontmatter wurden auf den Antwortblock vom 2026-08-06 sowie auf die zweite Kritische
Pruefung (Z2-B1/Z2-B2/Z2-S1/Z2-S2) gezogen — massgeblich ist zusaetzlich die teiluebergreifende
Aufloesung in Teil 7 („Entscheidungen zu den Rest-Blockern (2026-08-07)"): Teil 7 baut eine eigene,
dedizierte Umschaltseite fuer den Master; sowohl die generische `ServiceSettings`-Maske als auch
diese Teil-6-Maske zeigen ihn ausschliesslich read-only, keine von beiden ist ein Schreibweg.

- **Z2-B1 behoben.** `depends_on` (Frontmatter) um Teil 3 (Firmendaten-Keys) und Teil 7
  (Master-Anzeige/Guard-Cache) ergaenzt; `open_questions` geleert — alle vier Fragen sind seit dem
  Freigabe-Antworten-Block vom 2026-08-06 beantwortet. Der Master-Selbstwiderspruch ist im Rumpf
  aufgeloest: AK 2, Fachliche Anforderungen und Technischer Loesungsentwurf beschreiben den Master
  jetzt durchgaengig als read-only mit Link, nirgends mehr als in dieser Maske schreibbar oder
  Guard-aufrufend. Out-of-Scope praezisiert (Firmendaten sind bewusster, benannter Zusatzumfang,
  kein Widerspruch mehr zum ehemaligen „keine neuen fachlichen Werte"-Satz). Akzeptanzkriterien von
  3 auf 6 erweitert (Master read-only, Transaktions-Atomaritaet, Firmendaten-Erstanlage,
  Admin-only-Zugriff). `affected_code` um `secondbrain/codebase/controller.md`, die
  Repository-Dateien (Transaktionsmechanik) und den praezisierten `AppSettingKeys.cs`-Eintrag
  ergaenzt.
- **Z2-B2 — teiluebergreifend geloest, hier nur die Teil-6-Seite nachgezogen.** Die Kollision mit
  Teil 7 („Teil-6-Maske als gesicherter Schreibweg") ist durch Teil 7s Entscheidung vom 2026-08-07
  aufgeloest: Teil 7 fuehrt eine eigene Umschaltseite, generische Maske UND Teil-6-Maske sind beide
  ausschliesslich read-only. Diese Spec ist damit konsistent zu Teil 7. Eine ggf. noch stale
  Teil-7-Textstelle (Loesungsentwurf „wird von jedem Schreibpfad … Teil-6-Maske … aufgerufen") ist
  ein Nachziehpunkt in der Teil-7-Datei, nicht in dieser.
- **Z2-S1 — entschaerft durch Fallback-Regel, nicht einseitig entschieden.** Die Fachlichen
  Anforderungen legen jetzt fest: Definitionsort der Firmendaten-Keys ist `AppSettingKeys.cs`;
  voraussichtlich legt Teil 3 sie an (Druckkopf-Erstbedarf), **falls** sie dort beim
  Teil-6-Dev-Lauf noch fehlen, legt Teil 6 sie selbst an. Damit ist Teil 6 in jedem Fall lieferbar,
  unabhaengig davon, ob Teil 3 nachzieht. Bleibt als Cross-Spec-Notiz (ausserhalb des Mandats
  dieses Laufs, da nur die Teil-6-Datei editierbar ist): Teil 3 plant die Firmendaten-Keys aktuell
  nicht — ein Nachziehen dort waere sauberer.
- **Z2-S2 uebernommen.** Der Technische Loesungsentwurf benennt jetzt konkret die fehlende
  Transaktions-Naht (beide Repositories rufen intern `SaveChangesAsync`), die Spannung zu ADR 0001
  bei direktem `DbContext`-Zugriff und den Cache-Rollback-Fallstrick von `CachedSettingRepository`.
  Die konkrete Umsetzungsvariante — (a) gemeinsame Transaktion via duennem Unit-of-Work-Baustein
  oder (b) SaveChanges-freie Repository-Varianten — bleibt bewusst dem Dev-Lauf ueberlassen, beide
  sind tragfaehig; keine weitere Rueckfrage an den Menschen noetig.
- **Z2-S3 — bleibt als Cross-Spec-Notiz offen, kein Blocker dieser Datei.** Teil 6 haengt jetzt
  (Antwort B-3) an Teil 7, obwohl die Uebersicht „Teil 2–6 in beliebiger Reihenfolge lieferbar"
  festhaelt. Diese Spec kann das nicht selbst aufloesen — die Uebersicht ist eine andere Datei und
  ausserhalb des Mandats dieses Laufs. Beim naechsten Uebersichts-Update nachzuziehen.

Status bleibt `Entwurf`; die Freigabe-Antworten oben sind unveraendert aus dem Block vom
2026-08-06 uebernommen, nicht neu erfunden. `updated` im Frontmatter auf 2026-08-07 gesetzt.
