---
typ: feature
split: true
anhaenge:
  - anhaenge/2026-07-29-standort-ideal/sage-views-ideal.md
---
# IDEAL-Standort live schalten (eigenes Deployment, hierarchische Produktionsauftraege)

## Worum geht es

Der IdealAkeWms soll an einem ZWEITEN Standort **IDEAL** laufen. Rahmen:

- **Eigenes Deployment je Standort** — eigene WMS-DB, eigene SAGE-Installation,
  eigener Connection-String, eigene Einstellungen. **Keine Mandantenfaehigkeit im
  Code**, kein geteilter Prozess.
- **Gemeinsamer Code-Stamm.** Beide Standorte laufen auf demselben Quellcode;
  Unterschiede werden ueber Einstellungen geschaltet, nicht ueber Forks.
- IDEAL arbeitet fachlich anders als AKE. Die Umstellung der Arbeitsweisen
  passiert **step-by-step ueber laengere Zeit** — die Architektur muss dieses
  schrittweise Vorgehen tragen.

## Leitentscheidung: Verhaltensschalter statt Standortweiche

**Master-Schalter `ProduktionsauftragHierarchisch`** (bool, Default `false`) plus
drei davon abhaengige Betriebsschalter. Die Umstellung der Arbeitsweisen laeuft
step-by-step ueber laengere Zeit — ein einzelner Schalter waere ein Big Bang und
koennte das nicht tragen.

```
ProduktionsauftragHierarchisch     Master, Default false  (EINWEG, s. u.)
├─ ProduktionsauftragBaumAnzeige    Default false, nur wirksam wenn Master an
├─ TermineAusPpsView                Default false, nur wirksam wenn Master an
└─ StuecklisteAusIdealView          Default false, nur wirksam wenn Master an
                                    (Teil 1b; BOM haengt am Sub-FA)
```

- **Master `false`** (= AKE heute): flache 1:1-Hierarchie, bestehende Kette
  `vw_AKE_Kommissionierung_WAListe`, Termin-Logik aus den Settings.
- **Master `true`** (= IDEAL): Import aus den IDEAL-Views, Sub-FAs in der DB,
  hierarchiefeste Lookup-Semantik. Was *darauf* passiert, entscheiden die
  abhaengigen Schalter einzeln.

**Warum ein Verhaltens- und kein Standortschalter:** Der Code muss nicht wissen,
an welchem Standort er laeuft, sondern nur, ob die Auftraege hierarchisch sind.
Das entkoppelt das Feature vom Standort und macht die Regressionsgarantie
pruefbar: Master aus = heutiges Verhalten, unveraendert.

**Warum die Aufteilung:** Die drei Achsen sind fachlich unabhaengig.
- *Baumanzeige* ist technisch trennbar (hierarchische Daten kann man flach
  listen) und zugleich der riskanteste Teil (Paging ueber Gruppen statt Zeilen,
  Phantom-Header, Auto-Expand bei Filtertreffer). Klemmt sie, will man sie
  abschalten koennen, ohne den Import zu verlieren.
  **Achtung bei der Erwartung:** "Baum aus" bei hierarchischen Daten ergibt NICHT
  die heutige Ansicht, sondern eine flache Liste ALLER Sub-FAs (deutlich mehr
  Zeilen). Das ist ein dritter Zustand, kein Rueckfall auf heute.
- *Termine* haengen einseitig ab: PPS-Termine setzen die IDEAL-Views voraus, die
  Hierarchie aber keine PPS-Termine. Man kann also hierarchisch importieren und
  weiter aus den Settings rechnen — sinnvoll, weil Termine Fertigungsprioritaeten
  steuern und man sie erst umstellt, wenn die Struktur nachweislich stimmt.

**Gegen die kombinatorische Explosion:** Der Master ist das Tor. Ohne ihn haben
die abhaengigen Schalter keine Bedeutung — der Zustandsraum schrumpft von 2^4 auf
1 + 2^3. Ungueltige Kombinationen (abhaengiger Schalter an, Master aus) werden
nicht ignoriert, sondern beim Start protokolliert und wie "aus" behandelt, damit
keine stillen Fehlzustaende entstehen.

### Der Master ist ein Migrationstor, kein Betriebsschalter (EINWEG)

Sobald hierarchisch importiert wurde, stehen Zeilen mit
`OrderNumber != SubOrderNumber` in der DB. Ein Zurueckschalten laesst dann Code
mit Eindeutigkeitsannahme auf Daten los, die sie nicht mehr erfuellen — stille
Falschtreffer statt Fehler. Der Master wird deshalb **einmal umgelegt**, nach
Test auf einer Kopie.

**Sperrbedingung datengetrieben, nicht schaltergetrieben:** gesperrt genau dann,
wenn `EXISTS(SELECT 1 FROM ProductionOrders WHERE OrderNumber <> SubOrderNumber)`.
Wer versehentlich umstellt und es vor dem ersten hierarchischen Import merkt,
kann gefahrlos zurueck — gleicher Schutz dort, wo er zaehlt, ohne den Fehlklick zu
bestrafen.

**Der Waechter gehoert in die Service-/Domaenenschicht, nicht in die Maske.** Die
generische ServiceSettings-Oberflaeche darf den Key sonst am Dialog vorbei
umlegen. Eine zentrale Uebergangspruefung greift ueber jeden Weg (generische
Maske, Standorteinstellungen-Maske, kuenftige API); die Dialoge sind nur die
Umgangsform darueber.

Verhalten:
- **Beim Einschalten** Bestaetigung erzwingen, sinngemaess: *"Umstellung auf
  hierarchische Produktionsauftraege. Nach dem ersten hierarchischen Import ist
  eine Rueckkehr zur flachen Struktur nicht mehr moeglich. Vorher auf einer
  Datenbank-Kopie testen. Wirklich umstellen?"*
- **Solange noch keine hierarchischen Daten existieren:** Schalter bleibt
  aenderbar, mit Hinweis, dass die Sperre mit dem ersten Import greift.
- **Sobald hierarchische Daten existieren:** Schalter wird schreibgeschuetzt
  angezeigt mit Begruendung — *"Kann nicht mehr deaktiviert werden: es liegen
  Auftraege mit Sub-FA-Struktur vor."* Ein Aenderungsversuch ueber einen anderen
  Weg wird abgelehnt und protokolliert.
- **Audit:** Wer den Master wann umgelegt hat, wird protokolliert (Aktivitaets-
  Protokoll / `SyncLogServices`) — bei einer Einwegtuer gehoert das nachvollziehbar.
- **Rueckweg** existiert nur als bewusste Datenbereinigung ausserhalb der
  Anwendung (dokumentiert, nicht per Klick). Ehrlich benennen: "nicht ueber die
  Anwendung umkehrbar", nicht "physikalisch unmoeglich".

### Wichtig: Das Schema ist NICHT schaltbar

`SubOrderNumber` kann nicht mal unique sein und mal nicht. Die Schema-Inversion
passiert daher **einmalig und unbedingt**:

- `OrderNumber` = FA-Nummer (Sage StrukturID) — **nicht mehr unique**
- `SubOrderNumber` = Sub-FA-Nummer (Sage BelID) — **unique**
- Hauptauftrag genau dann, wenn `OrderNumber == SubOrderNumber`
- Backfill bei der Migration: `SubOrderNumber = OrderNumber` fuer alle Bestandszeilen

Im flachen Modus gilt damit die Invariante `OrderNumber == SubOrderNumber` —
AKE-Daten bleiben faktisch eindeutig, das Verhalten identisch. Der Schalter
steuert nur, was *darueber* passiert.

## Geklaert (Antworten aus den Rueckfragen)

- **Deployment:** eigenes Deployment pro Standort, Connection-String pro Instanz.
- **Sub-FA-Rueckmeldung:** Sub-FAs muessen rueckmeldefaehig sein (BDE etc.) —
  nicht nur lesend darstellbar.
- **Standort-Grundschalter:** zusaetzlich zu den fachlichen Feature-Toggles sinnvoll.
- **Termine:** Bei IDEAL ersetzen die PPS-View-Termine (KO_Termin, FE_Termin,
  Start_Beschichtung, Neuer_PT_PPS, Verladetermin_Vsl ...) die Settings-basierte
  Terminlogik **vollstaendig** — wir konsumieren, rechnen nicht.
- **Views:** Die IDEAL-Views (`vw_IDEAL-AKE_Kommissionierung_FAListe` / `_FAInfos`)
  liefern **andere Inhalte** als die AKE-WAListe — aehnlich, aber nicht dasselbe.
  Deshalb zwei getrennte Lesepfade, ueber den Schalter gewaehlt; View-Namen
  konfigurierbar (Whitelist-Regex gegen Injection).
- **View-DDL:** kommt versioniert ins Repo unter `SQL/sage-views/` (Dokumentation
  der Fremd-DB, keine WMS-Migration).

## Zerlegung & Reihenfolge (risiko-aufsteigend)

**Folgt aus B5.** Weil die IDEAL-Struktur in eine eigene Tabelle mit eigener
Domain geht, haengen die Listen-Features an nichts aus dem Kern. Der
Schema-Umbau an `ProductionOrders` kommt erst dort, wo er wirklich gebraucht
wird: bei der Rueckmeldung. Reihenfolge deshalb **risiko-aufsteigend** statt
"groesster Brocken zuerst".

**Gesamtform `split`, der BDE-Teil ist seinerseits ein `epic`.**

| Teil | Inhalt | Kern beruehrt |
|---|---|---|
| 1 | Struktur-Fundament `IdealFaStruktur` | nein |
| 2 | Struktur-/Baumanzeige (rekursiv) | nein |
| 3 | Kommissionierlisten | nein |
| 4 | Beschichtungsauftrag | nein |
| 5 | Vormontage-Listen | nein |
| 6 | Standorteinstellungen-Maske | nein |
| 7 | Materialisierung nach `ProductionOrders` | **ja** |
| 8 | Sub-FA-Rueckmeldung / BDE (`epic`) | **ja** |

- **Teil 1 — Struktur-Fundament `IdealFaStruktur`.**
  Import aus `_FAListe` + `_FAInfos` in eine eigene Tabelle: getreue Projektion
  mit `HauptFA`, `VaterFA`, `SubFA`, `Position` und den fachlichen Attributen.
  **Mehrstufig** (B1), Datenverfuegbarkeits-Regel INNER JOIN beachten,
  Kombinationsgeraete ueber `HauptFA` + Montage-Abteilung trennen (B3).
  Eigene Domain-Objekte, Repository + Cache-Decorator, konfigurierbare
  View-Namen (Whitelist-Regex), View-DDL nach `SQL/sage-views/`.
  Weil `FAListe` zugleich die Stueckliste ist (B4), speist dieselbe Tabelle
  Struktur UND Stueckliste — ein Lesepfad, zwei Projektionen.
  *Wichtig:* Die Stueckliste je FA ist **flach** — die Mehrstufigkeit entsteht
  nicht durch verschachtelte BOM-Zeilen, sondern durch Positionen, die auf
  Baugruppen anderer FAs zeigen (`SubFA != 0`). Der Haupt-FA fuehrt die
  Baugruppen seiner Sub-FAs plus Zusatzartikel (z. B. Zukaufteile); ein Sub-FA
  fuehrt die Bestandteile seiner Baugruppe plus Zukaeufe plus ggf. eine
  Baugruppe, die in einem ANDEREN Sub-FA gefertigt wird.
  *Ziel: IDEAL-Daten sind im System und abfragbar.* `ProductionOrders`
  unberuehrt, AKE unberuehrt.

- **Teil 2 — Struktur-/Baumanzeige.** Rekursive Darstellung auf
  `IdealFaStruktur` (nicht zweistufig gruppiert — B1 hat D3 ueberholt).
  Riskantester Anzeigeteil (Paging ueber Gruppen, Phantom-Header, Auto-Expand bei
  Filtertreffer), deshalb eigener Schalter und eigener Teil.

- **Teil 3 — Kommissionierlisten.** Filter
  `Kommissionieren IS NOT NULL AND <> ''`, gruppiert nach `HauptFA`
  (+ Montage-Abteilung), Kopf aus FAInfos, Barcode `HauptFA`.
  **Vor der Spezifizierung pruefen:** Gibt es Zeilen mit `SubFA != 0` UND
  gesetztem `Kommissionieren`? Wenn nein, ist `Kommissionieren` selbst die
  Trennlinie zwischen Lagerentnahme und Eigenfertigung, und es kann nichts
  doppelt gezaehlt werden. Wenn ja, zusaetzlich ueber `SubFA = 0` bzw.
  `Beschaffungsartikel` filtern.

- **Teil 4 — Beschichtungsauftrag.** Filter `Beschichtet = -1` (Sage-Boolean),
  Druckdokument mit Dienstleister-Kopf.

- **Teil 5 — Vormontage-Listen.** Filter `VMBedarf`, drei Sichten,
  Isolierfraesen-Export.

- **Teil 6 — Standorteinstellungen-Maske.**
  Siehe [[2026-08-03-standorteinstellungen-maske]]. Gruppiert die
  standortspezifischen Werte (Firmenname, Adresse, Mandant, View-Namen,
  Schalter). Bis dahin laufen die Schalter als normale
  `ServiceSettingDefinitions`-Keys (bestehende DB-first-Infrastruktur inkl.
  Drift-Guard-Test, Null Zusatzaufwand).

- **Teil 7 — Materialisierung nach `ProductionOrders`.** Ab hier wird der Kern
  angefasst. Aus `IdealFaStruktur` werden die Zeilen mit `SubFA != 0` (plus
  Wurzel) als echte Produktionsauftraege materialisiert: `SubOrderNumber` =
  eigene BelID (unique), `OrderNumber` = `HauptFA` (nicht unique),
  `ParentSubOrderNumber` = `VaterFA`. Blaetter (`SubFA = 0`) wandern NICHT mit —
  das ist Material, kein Auftrag.
  Enthaelt: Schema-Inversion + Backfill, Einwegtor-Waechter, Durchzug von
  `SubOrderNumber` durch Repositories/Controller/Scan-Lookups und das
  **adversariale Review der FA-Zusatzinfos-Kollision**.
  Richtung beachten: Die Struktur ist die Quelle, `ProductionOrders` ist
  abgeleitet — eine Transformation, kein zweiter Import aus der View. So laufen
  die beiden nicht auseinander, und die Transformation ist ohne DB testbar.

- **Teil 8 — Sub-FA-Rueckmeldung / BDE (eigener `epic`).**
  Weil die FAs nach Teil 7 echte `ProductionOrders` sind, greifen
  Arbeitsgaenge, Teileverfolgung und Rueckmeldung grundsaetzlich unveraendert.
  Etappen grob: Rueckmelde-Datenmodell -> Repositories/Services ->
  Rueckmelde-Logik -> BDE-Anbindung -> Tests. EIN langlebiger Worktree,
  `sync-worktree.ps1` haelt ihn auf main-Stand.

**Alternative Reihenfolge:** Muss die Rueckmeldefaehigkeit bei IDEAL von Tag eins
stehen, werden 7 und 8 vorgezogen — Teil 1 bleibt dann trotzdem das Fundament.
Die Architektur aendert sich dadurch nicht, nur die Lieferreihenfolge.

## Synchronisation Struktur -> ProductionOrders (Teil 7/8)

Die Struktur-Tabelle ist ein Cache und darf jederzeit komplett neu aufgebaut
werden. Bei den materialisierten `ProductionOrders` gilt das **nicht** — dort
haengen Rueckmeldungen dran. Regeln:

- **Neuer Sub-FA taucht auf** -> anlegen.
- **FA verschwindet aus der View, hat aber schon Rueckmeldungen** -> **NICHT
  loeschen.** Status setzen ("nicht mehr in Sage") und in der Oberflaeche
  kenntlich machen. Ein Sync, der loeschen darf, vernichtet Buchungsdaten.
- **Sub-FA haengt unter einem anderen Vater (Umhaengung)** -> **darf fachlich
  nicht vorkommen.** Genau deshalb als **Invariante behandeln, nicht als
  Annahme:** Der Sync erkennt eine geaenderte `VaterFA` bei bereits
  materialisiertem Sub-FA, **aendert sie nicht stillschweigend**, sondern
  protokolliert und meldet den Fall zur Klaerung. Waere die Zusage verletzt und
  wir wuerden blind umhaengen, haengen Rueckmeldungen am falschen Auftrag — ohne
  dass es jemand bemerkt.

Diese drei Regeln gehoeren als **Akzeptanzkriterien** in Teil 7, nicht als
Randnotiz.


## Harte Akzeptanzbedingungen (fuer JEDEN Teil)

1. **AKE-Verhalten unveraendert.** Bei `ProduktionsauftragHierarchisch = false`
   verhaelt sich das System exakt wie heute — gleiche Views, gleiche Termine,
   gleiche Darstellung. Nach jedem Teil-Merge nachweisbar.
2. **Eindeutigkeits-Lookups sind hierarchiefest.** Siehe Risiko unten.

## Groesstes technisches Risiko: FA-Zusatzinfos (v1.26.0)

Das seit v1.26.0 bestehende Feature **FA-Zusatzinfos (Sage)** inkl. Auto-Erledigt
und BDE-Sperre matcht `[WA Nummer]` auf `ProductionOrders.OrderNumber` — und
verlaesst sich darauf, dass die Spalte eindeutig ist. Nach der Inversion ist sie
das **nicht mehr** (im hierarchischen Modus liefert ein `OrderNumber`-Lookup
mehrere Zeilen).

Zu pruefen und explizit zu entscheiden, je Fundstelle:
- `FaZusatzinfoSyncService` und die FA-Reconciliation
- alle weiteren `OrderNumber`-basierten Single-/First-Lookups im heutigen main
- Scan-/QR-Lookups (QR traegt an Index 2 die BelID = kuenftig `SubOrderNumber`)

**Regel:** eindeutige Lookups -> `SubOrderNumber`; Gruppen-Lookups (alle Sub-FAs
einer Haupt-FA) -> `OrderNumber`.

Dieser Punkt gehoert bei der Spezifizierung von Teil 1 **adversarial reviewt**
(`/review`), nicht nur erwaehnt.

## Migrationsnummern

`SQL/82` und `SQL/83` sind seit v1.28.0 (Sage-Lagerbuchungen) belegt, main laeuft
damit bis 83. Die IDEAL-Migrationen daher **ab `SQL/84`** anlegen, EF-Migrationen
mit **aktuellen** Timestamps generieren (sonst spielt EF vermeintlich alte
Migrationen gegen eine weiter migrierte DB). Alles idempotent mit
OBJECT_ID/COL_LENGTH/sys.indexes-Guards, `SQL/00_FreshInstall.sql` und
`__EFMigrationsHistory` mitziehen. **Vor dem Dev-Lauf erneut pruefen**, ob 84/85
noch frei sind.

## Referenzen

- **Anhang [[sage-views-ideal]]** — maßgebliche technische Grundlage: View- und
  Spaltendefinitionen, Verknuepfungsschluessel, Filter, Fallstricke,
  Integrationsvorschlag.
- **[[2026-07-27-ideal-anpassungen-neu-nachbilden]]** — **historische Referenz,
  KEIN Auftrag.** Alter Entwicklungsstand aus dem veralteten Branch
  `feature/ideal-anpassungen-v1` (Basis ~v1.12.0). Zeigt, wie der Gedanke damals
  aussah; die dortigen Design-Entscheidungen D1/D2/D3/D7/D10 sind hier
  uebernommen, der Rest ist ueberholt (insbesondere: die dort genannten Views
  sind NICHT die IDEAL-Views dieser Idee, und die Migrationsnummern stimmen
  nicht mehr). Nicht als Spezifikationsgrundlage verwenden.
- **[[2026-08-03-standorteinstellungen-maske]]** — wird als Teil 1c aufgenommen.

## Befunde aus dem Anhang (widersprechen teils den Alt-Entscheidungen)

Beim Gegenlesen der Spaltendefinitionen sind vier Punkte aufgefallen, die das
Datenmodell und die Zerlegung betreffen. **Vor der Spezifizierung von Teil 1 zu
entscheiden.**

### B1 — Die FA-Struktur ist MEHRstufig, nicht zweistufig [ENTSCHIEDEN]

**Verbindlich: mehrstufig.** Der Anhang zeigt es in den Spaltendefinitionen; die
Spec zieht die Struktur genau von dort. Die Alt-Entscheidungen D1 (zwei Ebenen)
und D3 ("2-Ebenen, server-gruppiert") sind damit **ueberholt** und duerfen nicht
als Vorlage dienen.

Die View liefert einen echten Elternzeiger:
- `VaterFA` (int NULL) = BelID des **uebergeordneten Sub-FA**, in dessen
  Stueckliste die Position steht. NULL nur bei der Wurzel.
- `SubFA` (int) = eigene BelID des Nachfolgers. **`0` = Blatt** (Kaufteil /
  Endmaterial ohne eigenen FA).

Waere die Struktur zweistufig, waere `VaterFA` ueberfluessig — es waere immer der
Haupt-FA. Der Elternzeiger existiert, weil eine Baugruppe in einer anderen
Baugruppe stecken kann; genau das deckt sich mit der fachlichen Aussage, dass ein
Sub-FA eine Baugruppe braucht, die ein anderer Sub-FA fertigt.

**Konsequenz:** Das Modell `OrderNumber` + `SubOrderNumber` (Alt-Entscheidung D1)
kann nur ZWEI Ebenen ausdruecken — Wurzel und Kinder. Es kann nicht abbilden,
dass Sub-FA B unter Sub-FA A haengt. Empfehlung: dritte Spalte
**`ParentSubOrderNumber`** (= `VaterFA`, nullable) mitfuehren. Kosten jetzt: eine
nullable Spalte. Kosten spaeter: erneute Migration samt Nach-Import aus Sage.
Damit wird auch die Baumanzeige rekursiv statt zweistufig gruppiert (die
Alt-Entscheidung D3 "2-Ebenen, server-gruppiert" greift dann zu kurz).

### B2 — `HauptFA` ist der Scan-Identifier und ist NICHT eindeutig [MITGENOMMEN]

**Annahme fuer den ersten Test:** Vermutlich existieren bei IDEAL auch Barcodes
je Sub-FA. Wird beim ersten Test am realen System geprueft — bis dahin als offene
Annahme fuehren, nicht als Faktum spezifizieren.

Der Anhang ist eindeutig: *"`HauptFA` ist der einzige Produktions-Identifier.
`SubFA` wird in Barcodes und Kommunikation nicht verwendet — auch wenn Werker
einen Sub-FA scannen, muessen wir intern auf HauptFA aufloesen."*

Das kollidiert mit der Regel "eindeutige Lookups -> `SubOrderNumber`": Bei IDEAL
liefert ein Scan `HauptFA`, also `OrderNumber` — und die ist nach der Inversion
bewusst **nicht mehr eindeutig**. Ein Scan identifiziert damit eine **Gruppe**,
keinen einzelnen Auftrag.

**Konsequenz:** Fuer Teil 2 (BDE/Rueckmeldung) und Teil 3 (Kommissionierung,
"Barcode HauptFA") muss entschieden werden, was nach dem Scan passiert: Wird auf
Haupt-FA-Ebene rueckgemeldet, oder waehlt der Werker aus den Sub-FAs aus?

### B3 — Kombinationsgeraete: `HauptFA` allein reicht nicht einmal als Struktur-Schluessel

*"Kombinationsgeraete mit gleichem HauptFA: `[Montage-Abteilung]` aus FAInfos ist
bei diesen Faellen der weitere Identifier. FAListe kann sie nicht unterscheiden."*

Es gibt also Faelle, in denen zwei logische Auftraege denselben `HauptFA` tragen
und erst der Join mit FAInfos sie trennt. Das ist ein **zusammengesetzter
Schluessel** (`HauptFA` + Montage-Abteilung) und muss im Datenmodell und in jeder
Gruppierung beruecksichtigt werden.

### B4 — `FAListe` IST die Stueckliste, nicht eine FA-Liste

Granularitaet laut Anhang: *"eine Zeile = eine Position bzw. der Hauptauftrag
selbst (Wurzel)"*; Zweck: *"Stueckliste je Struktur (Haupt-FA + alle Sub-FAs +
Blaetter)"*.

Die View ist also die **komplette mehrstufige Stueckliste einer Struktur**, aus
der sich die FA-Hierarchie ableitet (Zeilen mit `SubFA != 0` sind Baugruppen mit
eigenem FA; `SubFA = 0` sind Blaetter).

**Konsequenz:** Teil 1 und Teil 1b lesen **dieselbe View** — sie sind zwei
Projektionen einer Datenmenge, keine zwei unabhaengigen Importe. Das sollte die
Spec so bauen (ein Lesepfad, zwei Projektionen), sonst entstehen zwei Importe,
die auseinanderlaufen koennen.

**Nebenbefund, der eine offene Frage schliesst:** Wie man eine hausintern
gefertigte Position erkennt, steht bereits in der View — `SubFA != 0` (hat einen
eigenen FA) bzw. `Beschaffungsartikel = Ja/Nein` aus `IstBestellartikel`. Ein
Abgleich ueber Artikelnummern ist nicht noetig.

### B5 — Architektur: eigener Lesepfad mit eigener Tabelle [ENTSCHIEDEN]

**Entschieden: eigene Domain, eigene FeatureServices, eigene Tabelle.** Die
IDEAL-Struktur wird als getreue Projektion der View in eine **eigene Tabelle**
importiert (Arbeitsname `IdealFaStruktur` — bewusst NICHT
`ProductionOrdersHierarchy`, s. u.), mit `HauptFA`, `VaterFA`, `SubFA`,
`Position` und den fachlichen Attributen. Darauf setzen eigene
Repositories/FeatureServices auf.

**Was die eigene Tabelle NICHT sein soll: eine zweite Auftragstabelle.** Wuerde
der Schalter zwischen `ProductionOrders` und einer gleichbedeutenden Tabelle
umschalten, muesste JEDER nachgelagerte Verbraucher wissen, aus welcher Tabelle
er liest — BDE, Teileverfolgung, Arbeitsgaenge, Leitstand, FA-Zusatzinfos,
Kommissionierung. Das `if hierarchisch` wanderte durch die halbe Codebasis, und
zwei Tabellen mit derselben Bedeutung driften auseinander. Deshalb:

| Tabelle | Aufgabe |
|---|---|
| `IdealFaStruktur` | Projektion der View: mehrstufige Struktur + Stueckliste. Eigene Domain, eigene Services. Speist Teil 3-5. |
| `ProductionOrders` | Bleibt DIE Auftragstabelle. Fuer Rueckmeldefaehigkeit (Teil 2) werden die FAs aus der Struktur dorthin **materialisiert** — dann greifen BDE, Teileverfolgung und Arbeitsgaenge unveraendert. |

Zwei Tabellen mit **verschiedenen Aufgaben** statt zwei mit derselben.

**Folge fuer die Reihenfolge:** Teil 3-5 haengen damit an nichts aus dem Kern —
kein Schema-Umbau, keine Einwegtuer, keine FA-Zusatzinfos-Kollision. Sie sind
frueh lieferbar. Die Schema-Inversion verschiebt sich auf den Zeitpunkt, an dem
sie wirklich gebraucht wird: Teil 2 (BDE). **Die Zerlegung oben ist entsprechend
neu zu ordnen.**

Der Anhang empfiehlt einen **parallelen Lesepfad** (`IFaListeRepository`,
`CachedFaListeRepository`, eigene Domain-Objekte `FaListEntry`/`FaInfoEntry`,
darauf Feature-Services) — und laesst `ProductionOrders` unangetastet. D1 dagegen
baut die IDEAL-Auftraege in die bestehende `ProductionOrders`-Tabelle ein.

Beides ist vertretbar, aber fuer Verschiedenes:
- **Fuer Teil 3-5** (Vormontage-, Beschichtungs-, Kommissionierlisten) genuegt der
  parallele Lesepfad — reine Projektionen, **kein Schema-Umbau, keine
  FA-Zusatzinfos-Kollision, keine Einwegtuer.**
- **Fuer Teil 1/2** (Auftraege im System, BDE-faehig) muessen die FAs echte
  `ProductionOrders` sein, sonst greifen Teileverfolgung, Arbeitsgaenge und
  Rueckmeldung nicht.

**Zu entscheiden:** Bekommen Teil 3-5 den leichtgewichtigen Lesepfad (und damit
keinerlei Abhaengigkeit von der Schema-Inversion) — oder setzen sie ebenfalls auf
den importierten `ProductionOrders` auf? Ersteres wuerde die drei Listen-Features
**vor** Teil 1 lieferbar machen und das Risiko deutlich senken.

## Offene Fragen

1. **Produktiv-DB/Server der IDEAL-Instanz.** Anhang nennt `AKESQL20.ake.at` /
   `IDEAL_TEST_2026_05_03` (Test). Produktivname und Server offen —
   Infrastruktur-Detail, kann in die Spec-Rueckfragen von Teil 1.
2. **Alter Worktree `ideal-anpassungen-v1`:** existiert er noch? Falls ja,
   enthaelt er `docs/V1.12.0-VERIFICATION.md` mit manuellen Testszenarien
   (BOM-Mengen-Check, Scan/QR-Lookup, Tree-Expand/Filter), die sich fuer die
   Test-Checkliste von Teil 1 wiederverwenden lassen.
3. **Geschwister-Abhaengigkeiten zwischen Sub-FAs (neu, betrifft Teil 3).**
   Aus der BOM-Klaerung folgt: Ein Sub-FA kann eine Baugruppe brauchen, die ein
   ANDERER Sub-FA desselben Haupt-FA fertigt. Die FA-Struktur bleibt zweistufig,
   aber zwischen den Geschwistern entsteht ein **gerichteter Materialfluss**. Das
   steht weder im Anhang noch im alten Referenzdokument. Zu klaeren:
   - **Wie erkennt man eine hausintern gefertigte Position?** Gibt es in der
     Stueckliste ein Kennzeichen (Eigenfertigung/Zukauf, Verweis auf den
     erzeugenden Sub-FA) — oder muss man ueber die Artikelnummer gegen die
     Ausgangsartikel der Geschwister-FAs matchen? Ein Kennzeichen in der View
     waere deutlich robuster als ein Abgleich per Artikelnummer.
   - **Muss das WMS die Reihenfolge kennen** (Sub-FA A vor B, weil B dessen
     Baugruppe braucht), oder ist die Abhaengigkeit nur Information und die
     Steuerung passiert im PPS? Das entscheidet, ob Teil 3 eine
     Verfuegbarkeits-/Reihenfolgelogik braucht oder nur eine Liste druckt.
4. **Kommissionier-Ebene (betrifft Teil 3, wichtig fuer Doppelzaehlung).**
   Wenn der Haupt-FA die Baugruppen seiner Sub-FAs fuehrt UND jeder Sub-FA seine
   eigenen Bestandteile — dann wuerde ein Aufsummieren beider Ebenen **doppelt
   zaehlen**. Also: Wird je Sub-FA kommissioniert, je Haupt-FA, oder gemischt
   (Zukaufteile am Haupt-FA, Fertigungsmaterial am Sub-FA)? Der Anhang nennt
   "Barcode HauptFA" fuer die Kommissionierliste — das deutet auf Haupt-FA-Ebene
   hin, passt aber nicht offensichtlich dazu, dass die Bestandteile am Sub-FA
   haengen. **Vor Teil 3 zwingend zu klaeren**, sonst kommissioniert das System
   Material doppelt oder gar nicht.

## Reif fuer den Backlog?

**Ja, fuer Teil 1.** Die Architektur ist entschieden (Verhaltensschalter,
unbedingte Schema-Inversion, getrennte Lesepfade, eigenes Deployment). Die
offenen Fragen sind Detail- bzw. Infrastrukturpunkte, die in die
Spec-Rueckfragen wandern koennen.

Vor dem Umzug nach `backlog/`:
1. Anhang mitnehmen: Ordner `ideen/Anhang/2026-0-29-Standort-IDEAL/` nach
   `backlog/anhaenge/2026-07-29-standort-ideal/` kopieren (Datums-Tippfehler im
   Ordnernamen dabei korrigieren; der `anhaenge:`-Pfad im Frontmatter zeigt
   schon aufs Ziel).
2. Spezifizierung **interaktiv** starten (`/spec`), nicht headless — wegen
   Anhang und Groesse.
3. Beim Spezifizieren entstehen Uebersichts-Notiz + Teil-Specs
   (`-teil-1-spec` ...), Teil-2-Spec mit `epic: true` und Etappen-Tabelle.
4. Danach **zwingend `/review`** auf die Teil-1-Spec — wegen der
   FA-Zusatzinfos-Kollision.

## Kritische Pruefung / Aufbereitung (2026-08-06)

Anwalt-des-Teufels-Durchsicht dieser IDEEN-Notiz (kein Spec — es gibt keine „Freigabe-Antworten",
aber die Notiz behauptet selbst „reif fuer den Backlog, Teil 1"; genau das wird hier geprueft).
Gegengelesen: die Notiz, der maßgebliche Anhang [[sage-views-ideal]], die verwandten Brain-Dateien
und der aktuelle main-Stand (inkl. der gerade gemergten Sage-Lagerbuchung v1.28.0 und der in
`entwurf/` liegenden WmsBugs-Teil-Specs). Die Architektur ist gut — aber es gibt echte Risse, die
`/spec` fehlleiten wuerden.

### BLOCKER — vor der Spezifizierung von Teil 1 zu klaeren

**B-1 — Drei widerspruechliche Teil-Nummerierungen in EINEM Dokument.**
- Die Zerlegungstabelle (Abschnitt „Zerlegung & Reihenfolge") sagt: Materialisierung = **Teil 7**,
  BDE-`epic` = **Teil 8**.
- Der B5-Fließtext sagt dagegen mehrfach „Rueckmeldefaehigkeit (**Teil 2**)", „Schema-Inversion …
  **Teil 2 (BDE)**", „**Teil 1/2** (Auftraege, BDE-faehig)". Ebenso die Ueberschrift „Synchronisation
  … (Teil 7/8)" vs. dieser Prosa.
- Zusaetzlich tauchen **Teil 1b** (Stueckliste, im Toggle-Block) und **Teil 1c** (Standorteinstellungen,
  Abschnitt Teil 6) auf — obwohl die Tabelle Standorteinstellungen als **Teil 6** fuehrt.

Die Notiz sagt selbst „**Die Zerlegung oben ist entsprechend neu zu ordnen**" (B5) — hat es aber nie
getan. `/spec` wuerde gegen zwei/drei Nummernschemata laufen und die falschen Teile bauen.
  **Auftrag (aufbereiten):** Die Tabelle ist die konsistente Wahrheit (risiko-aufsteigend: 1
  Struktur-Fundament, 2 Baumanzeige, 3 Kommissionier, 4 Beschichtung, 5 Vormontage, 6
  Standorteinstellungen, 7 Materialisierung/Kern, 8 BDE/`epic`). Die **stale Prosa** angleichen: im
  B5-Text „Teil 2" → „Teil 8", „Teil 1/2" → „Teil 7/8"; die „Teil 1b/1c"-Bezeichner streichen
  (Stueckliste ist laut B4 Teil VON Teil 1 — „ein Lesepfad, zwei Projektionen"; Standorteinstellungen
  ist Teil 6). Ein einziges Nummernschema.

**B-2 — „zweistufig" vs. „mehrstufig" widersprechen sich direkt.**
B1 [ENTSCHIEDEN] stellt verbindlich fest: **mehrstufig** (echter Elternzeiger `VaterFA` →
`ParentSubOrderNumber`, rekursive Anzeige; D1/D3 „zwei Ebenen" ueberholt). **Offene Frage 3** sagt
woertlich: „**Die FA-Struktur bleibt zweistufig**, aber zwischen den Geschwistern …". Das ist der
gegenteilige Satz. Einer von beiden ist ein Alt-Rest.
  **Frage/Aufbereitung:** Bestaetigen, dass B1 (mehrstufig) ueberall gilt, und den zweistufig-Satz in
  Offener Frage 3 auf „mehrstufig; zusaetzlich gerichteter Materialfluss zwischen Geschwistern"
  korrigieren. Sonst baut Teil 1/2 ein Modell, das die dritte Ebene nicht traegt (genau der Fehler,
  vor dem B1 warnt).

**B-3 — Der `anhaenge:`-Frontmatter-Pfad ist kaputt → der spec-agent bekommt die technische Grundlage NICHT.**
Frontmatter: `anhaenge/2026-07-29-standort-ideal/sage-views-ideal.md`. Tatsaechlicher Ordner:
`anhaenge/2026-0-29-Standort-IDEAL/sage-views-ideal.md` (Datums-Tippfehler `2026-0-29` statt
`2026-07-29`, **und** Groß/Klein `standort-ideal` vs. `Standort-IDEAL`). Der spec-agent liest den
`anhaenge:`-Pfad — mit diesem Mismatch liest er den Anhang (die maßgebliche View-/Spaltengrundlage)
**nicht** und wuerde raten.
  **Fix vor `/spec`:** Ordner auf den korrigierten Namen umbenennen (`2026-07-29-standort-ideal`,
  passend zum Frontmatter) ODER das Frontmatter auf den Ist-Ordner setzen — eine kanonische Schreibweise
  festlegen. (Deckt sich mit „Reif fuer den Backlog" Schritt 1, ist aber JETZT schon ein Lesefehler.)
  Hinweis: Der `anhaenge:`-Ordner ist derzeit uncommittet/mitten im Verschieben — bitte du entscheiden,
  ich fasse den halb-verschobenen Ordner nicht an.

**B-4 — B3 (Kombinationsgeraete) ist NICHT entschieden, bricht aber das Kern-Modell „OrderNumber = HauptFA".**
Der Anhang ist eindeutig: Kombinationsgeraete teilen sich **denselben `HauptFA`** und werden erst
ueber `[Montage-Abteilung]` aus FAInfos getrennt — `HauptFA` allein ist nicht einmal ein eindeutiger
**Struktur**-Schluessel. Teil 1 nennt das („HauptFA + Montage-Abteilung trennen"), aber die Folge fuer
Teil 7 ist offen: dort wird `OrderNumber = HauptFA` materialisiert — dann ist selbst der GRUPPEN-
Schluessel `OrderNumber` **mehrdeutig** (zwei logische Auftraege, gleiche OrderNumber). Wo lebt
`Montage-Abteilung` im `ProductionOrders`-Modell? Das ist keine Detailfrage — es betrifft den
Tabellen-Schluessel und jede Gruppierung.
  **Entscheidung vor Teil 1 (nicht erst Teil 7):** B3 von [MITGENOMMEN] auf [ENTSCHIEDEN] heben — wie
  wird `Montage-Abteilung` Teil der Identitaet (eigene Spalte in `IdealFaStruktur` UND spaeter in
  `ProductionOrders`; zusammengesetzter Gruppen-Schluessel `OrderNumber + Montage-Abteilung`?).

**B-5 — B2-Konsequenz (Scan = Gruppe, nie Einzelauftrag) ist offen und fundamental.**
Anhang: „`HauptFA` ist der EINZIGE Produktions-Identifier; `SubFA` wird nie gescannt." Nach der
Inversion ist `HauptFA = OrderNumber` bewusst **nicht** eindeutig → ein Scan identifiziert eine
**Gruppe**. Damit kollidiert die Regel „eindeutige Lookups → SubOrderNumber" frontal mit der Realitaet
bei IDEAL. Die Notiz fragt „Haupt-FA-Ebene rueckmelden oder Werker waehlt Sub-FA?" — unbeantwortet,
aber Grundlage fuer Teil 8 (BDE) UND Teil 3 (Kommissionierung).
  **Entscheidung noetig**, bevor die betroffenen Teile spezifiziert werden.

### SOLLTE — macht die spaeteren Dev-Laeufe sicherer

**S-1 — Offene Frage 4 (Kommissionier-Doppelzaehlung) ist ein echter Teil-3-Blocker.** Haupt-FA fuehrt
die Baugruppen seiner Sub-FAs UND jeder Sub-FA seine Bestandteile → Aufsummieren beider Ebenen zaehlt
doppelt. Anhang sagt „Barcode HauptFA" (Haupt-Ebene), Bestandteile haengen am Sub-FA — Spannung. Vor
Teil 3 zwingend klaeren und als **Akzeptanzkriterium** (keine Doppel-/Nullzaehlung) verankern.

**S-2 — Migrationsnummern-Kollision mit den WmsBugs-Specs.** Die Notiz plant „ab `SQL/84`". Die gerade
finalisierte WmsBugs-**Teil-7**-Spec belegt bereits `SQL/84` (WarehouseRequisitionComment) **und**
`SQL/85` (SeedDummyArticle). Wer zuerst umsetzt, gewinnt; die IDEAL-Migrationen muessen dann die real
freie Nummer nehmen (86+). Die Notiz mahnt „vor Dev-Lauf pruefen" — jetzt gibt es einen konkreten
Konkurrenten, bitte hier verlinken.

**S-3 — Alt-Spec als Landmine.** `secondbrain/specs/entwurf/2026-07-28-ideal-anpassungen-neu-nachbilden-spec.md`
ist durch diese Idee ueberholt (D1/D2/D3 verworfen), liegt aber noch **aktiv in `entwurf/`**. Jemand
koennte sie `/review`-en oder `/dev`-en. Als „superseded/archiviert" kennzeichnen (Status + Verweis auf
diese Idee), damit sie nicht versehentlich aufgegriffen wird.

**S-4 — Harte Regeln als Akzeptanzkriterien fixieren (nicht als Prosa).** Die Notiz sagt es fuer die
Sync-Regeln selbst — konsequent in die Teil-Specs ziehen: (a) INNER-JOIN-Datenverfuegbarkeit (keine
FAListe-Anzeige ohne FAInfos-Eintrag, Anhang), (b) die drei Sync-Regeln (nicht loeschen bei vorhandenen
Rueckmeldungen; Umhaengung nicht still aendern, sondern melden), (c) FA-Zusatzinfos-Kollision adversarial
reviewen. Alle drei als AK, nicht als Randnotiz.

### HINWEIS — Staerken und Beobachtungen

**H-1 — Ausgezeichnet gedacht:** das EINWEG-Master-**Migrationstor** (datengetrieben gesperrt per
`EXISTS(OrderNumber<>SubOrderNumber)`, Waechter in der Domaenen-, nicht der Maskenschicht, Audit, ehrliche
„nicht ueber die App umkehrbar"-Formulierung) ist selten so sauber durchdacht — beibehalten. Ebenso die
korrekte Identifikation der FA-Zusatzinfos-Kollision als groesstes Risiko.

**H-2 — FA-Zusatzinfos ist teilweise schon abgesichert:** `FaZusatzinfoSyncService` ist seit v1.26.0
mehrfachtreffer-faehig (GroupBy `OrderNumber`, Upsert je Id) — die Schreib-Seite vertraegt die
Nicht-Eindeutigkeit bereits. Das RESTrisiko ist die FA-Reconciliation (`OrderNumber`-UPDATE) — exakt
dasselbe Muster wie im Sage-Lagerbuchungs-Review. Beim adversarialen Teil-1/7-Review gezielt darauf.

**H-3 — Termine-Toggle vs. „vollstaendig ersetzen":** klein — der Toggle `TermineAusPpsView` erlaubt
das Staging, die „Geklaert"-Aussage „PPS ersetzt die Settings-Logik vollstaendig" ist der Zielzustand.
Beide Formulierungen konsistent machen (Toggle = Weg dorthin).

**H-4 — Die Selbst-Einschaetzung „reif fuer den Backlog, Teil 1; offene Fragen nur Detail/Infrastruktur"
ist zu optimistisch:** B-1/B-2 (Widersprueche) und B-4/B-5 (Kern-Design B2/B3) sind KEINE Details — sie
betreffen bereits Teil 1 (Schluessel, Tabelle) bzw. leiten `/spec` falsch. Nach deren Aufloesung ist
Teil 1 tatsaechlich backlog-reif.

### Empfehlung

**NACHBESSERUNG NOETIG vor `/spec` Teil 1: (1) die interne Nummerierung auf EIN Schema angleichen
(B-1), (2) den zwei-/mehrstufig-Widerspruch aufloesen (B-2), (3) den Anhang-Pfad reparieren, damit der
spec-agent die technische Grundlage liest (B-3), und (4) die zwei Kern-Design-Entscheidungen B2 (Scan =
Gruppe) und B3 (HauptFA + Montage-Abteilung als Schluessel) treffen (B-4/B-5). Architektur und
Migrationstor sind stark; die Bloecke sind Konsistenz- und zwei Modell-Entscheidungen, keine
Neukonzeption.**
