---
typ: feature
---
# FA-Liste ausbauen: Zeilenwerte, HauptFA-Zeile, Freigabe-Kaskade, Matchcode

Aufbauend auf [[2026-08-20-materialisierung-fachliche-felder-spec]] (Testbereit, v1.37) —
**eigene Spec, nicht deren Erweiterung**: Jene steht mit frischer QA auf `Testbereit`, sie dafuer
aufzureissen kostet einen vollstaendigen Re-QA-Lauf.

Umsetzung im bestehenden Buendel-Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`.

## 1 — Kopfdaten auch in der ZEILE, nicht nur in der Gruppen-Kopfzeile

Heute stehen Kunde, Termine, Prio, AB-Nummer und Montage-Abteilung ausschliesslich in der
Gruppen-Kopfzeile. Sie sollen zusaetzlich **je Zeile verfuegbar** sein.

**Wichtige Unterscheidung — verfuegbar heisst nicht sichtbar:**
- **Verfuegbar in jeder Zeile:** damit Spaltenfilter, Sortierung und ein spaeterer Export
  einheitlich funktionieren. Der heutige Sonderweg „Kunde-Filter ueber Join auf die Gruppe"
  entfaellt damit — der Filter arbeitet wieder wie in jeder anderen Liste auf Zeilen.
- **Angezeigt nur, wo der Wert wechselt:** Sonst steht derselbe Kunde 39-mal untereinander und
  erschlaegt die Zeile. Reine Darstellungsfrage, keine Datenfrage.

## 2 — Der HauptFA wird eine normale Zeile

Seine Werte stehen dann in **seinen eigenen Spalten**, nicht nur im Gruppenkopf. Daraus folgt eine
Vereinfachung: Er ist eine Zeile wie jede andere und traegt seine Aktionen **in dieser Zeile** —
kein Gruppen-Kopf-Widget noetig.
Die Kopfzeile kann dadurch **schlanker** werden (HauptFA-Nummer, Sub-FA-Zahl, Mehrdeutig-Badge);
ob sie die Kopfdaten weiterhin zusaetzlich zeigt, ist eine Anzeigeentscheidung — bei Punkt 1
(„nur wo der Wert wechselt") erscheinen sie ohnehin in der ersten Zeile.

## 3 — Freigeben am HauptFA gibt alle Sub-Auftraege frei [ENTSCHEIDUNG — kehrt eine fruehere um]

**Achtung, das widerspricht einer bestehenden Festlegung.** In
[[2026-08-18-fa-liste-hierarchie-anzeige-spec]] steht: *„`BulkRelease` ist Freigabe, nicht
Fertigmeldung — fachlich etwas anderes, keine Kaskade."* Diese Aussage wird **hiermit umgekehrt**
und muss dort mitgezogen werden, sonst widersprechen sich die beiden Specs.

**Die Umkehr ist richtig, und zwar aus einem besseren Grund als die urspruengliche Vorsicht:**
Eine **Freigabe behauptet nichts ueber geleistete Arbeit** — sie sagt nur, dass begonnen werden
darf. Sie nach unten weiterzugeben ist logisch zwingend: Ist das Geraet freigegeben, sind es seine
Baugruppen auch. Beim **Fertigmelden** war die Vorsicht berechtigt (dort wird Arbeit behauptet),
hier nicht.

**Konsequenzen, die mitgehen:**
- Kaskade ueber **alle Nachfahren**, nicht nur direkte Kinder — wie bei der Fertigmeldung.
- **Atomar**: entweder alle oder keiner.
- **Bestaetigung mit Anzahl** — ein Klick gibt potenziell 39 Auftraege frei.
- **Bereits freigegebene Sub-FAs** bleiben unveraendert (kein Zuruecksetzen); die Bestaetigung
  nennt, wie viele tatsaechlich neu freigegeben werden.
- **Keine Gruppen-Ruecknahme** — dieselbe Asymmetrie wie bei der Fertigmeldung: Zuruecknehmen
  wirkt nur auf die eigene Zeile, sonst gehen eigenstaendige Zustaende verloren.
- **`BulkRelease` (Mehrfachauswahl) bleibt unveraendert zeilenbasiert** — die Kaskade ist eine
  andere Aktion, kein neues Verhalten der bestehenden.

## 4 — Matchcode: QUERSCHNITT, nicht FA-Listen-Detail

**Der Matchcode ist bei IDEAL essentiell** und muss **ueberall dort anzeigbar sein, wo heute eine
Artikelbezeichnung steht.** Das macht ihn zu einem Querschnittsthema und ist der groesste Teil
dieser Spec.

**Quelle:** `FaHierarchyNode.Matchcode`. Verfuegbarkeit ist bewiesen — die FA-Struktur-Ansicht zeigt
ihn bereits als eigene Spalte.

**Konsequenz: die Migration kommt zurueck.** Damit Listen, die aus `ProductionOrders` lesen, ihn
zeigen koennen, muss er materialisiert werden → neue Textspalte an `ProductionOrder`,
`deploy.migration` von `false` auf `true`. Additiv, kein Datenumbau — aber bewusst zu tun.
Anlege- **und** Update-Pfad gemeinsam (F1-Regel), sonst haben Bestandsauftraege ihn nicht.

**ERHEBUNG statt Vermutung — gehoert in den Umfang:** Welche Ansichten zeigen heute eine
Artikelbezeichnung? Die sechs Listen sind der Anfang, aber es duerften mehr sein (Kommissionierung,
BOM-Ansichten, Druckdokumente, Fehlteile, Bestellungen). Dieselbe Form wie die Z4-Zaehl-Erhebung:
erst suchen, dann entscheiden — nicht raten und drei Ansichten vergessen.

**Reichweite: HAUSWEIT, nicht IDEAL-only [ENTSCHIEDEN 2026-09-10].**
Der Matchcode wird auch fuer AKE gefuehrt — falls dort noch nicht vorhanden, wird er ergaenzt.
Damit ist es eine **normale Spalte in beiden Modi**, kein Sonderfall: keine
`defaultHidden`-Unterscheidung nach Master-Schalter, keine zwei Verhaltensweisen.

**Aber — was „ergaenzen" bei AKE heisst, ist zu klaeren, und es kann eine EXTERNE Abhaengigkeit
sein:**
- **Liefert `vw_AKE_Kommissionierung_WAListe` / `_StuecklistenDB` den Matchcode bereits?**
  → Dann ist es reines Mapping, kein Zusatzaufwand.
- **Falls nicht** → die **Sage-View muss erweitert werden**. Das ist ein Fremd-DB-Objekt und damit
  dieselbe Klasse Abhaengigkeit wie die IDEAL-Views: **nicht durch einen Dev-Lauf loesbar**, sondern
  mit der Sage-Betreuung abzustimmen. Vor der Umsetzung pruefen, nicht annehmen.

**Vorgehen, das nicht blockiert:** Spalte, Materialisierung und Anzeige werden **jetzt** gebaut und
fuer **IDEAL sofort** befuellt. Fuer AKE bleibt sie leer, bis die View liefert — die Anzeige darf
daran nicht scheitern (kein Filter, keine Sortierung, die auf einem leeren Feld ins Leere laeuft,
vgl. F7). Sobald die AKE-Quelle steht, fuellt sie sich ohne weitere Code-Aenderung.
**Als bekannte Zwischeneinschraenkung in die Spec**, damit niemand die leere AKE-Spalte als Fehler
meldet.

**Platzierung:** eigene Spalte neben `Artikelnummer`/`Bezeichnung 1`, ueber das Zahnrad steuerbar
wie jede andere — `#column-config` **und** `ColumnDefinitions` gemeinsam pflegen (Fallstricke §10;
siehe auch Nachlese-Punkt 18 `warehouse-order`, dieselbe Fehlerklasse).

## Geparkte Fragen — spaeter zu pruefen, hier nur festgehalten

Damit sie nicht verlorengehen:

1. **B-0 / Ruling 4 — Vollstruktur in der Vorbau-Stueckliste einer HauptFA.** Wird getestet statt
   entschieden. **Die Pruefrage ist konkret:** *Erscheint dasselbe Material dadurch auf ZWEI
   Kommissionierlisten?* Eine Position aus einem Sub-FA heraussuchen und pruefen, ob sie auch in
   der HauptFA-Vorbauliste steht. Wenn ja → Rueckbau (Einzeiler in der Scope-Regel). Wenn nein →
   Vollstruktur bleibt, **und dann muss die Druck-Whitelist in `PrintBom.cshtml` um Ebene und
   Komm.-Ziel ergaenzt werden**, sonst ist der Ausdruck unbrauchbar. **Beides liegt vor dem Merge.**
   Am **Bildschirm** entscheiden, nicht am Ausdruck — dem fehlen genau die Spalten, die man dafuer
   braucht.
2. **`HasCoatingParts` / zweite Z1-Ausnahme.** Meine Antwort B1 hatte die Lack-Ableitung
   ausgeschlossen, weil fuer IDEAL keine `ProductionOrderPickingStatus`-Zeile existierte. Der
   F6-Fix hat genau das behoben — der Ausschlussgrund ist weggefallen, und die Ableitung wurde
   umgesetzt. **Zu pruefen:** Faellt `PickingStatus` damit aus der app-verwalteten Z1-Liste des
   `FaMaterializationSyncService`? Wenn ja, braucht es dort eine **zweite Ausnahme neben
   `Workplace`** und der Klassenkommentar ist mitzuziehen — sonst sagt der Code das eine und die
   Dokumentation das andere.

## Danach: Replikation auf die uebrigen fuenf Ansichten

Leitstand, Tracking, Picking, FaWorklist, FaCompletion zeigen Kunde und Termine je Zeile und
bleiben fuer IDEAL leer. Mit Punkt 1 dieser Spec (Werte je Zeile verfuegbar) wird die Replikation
deutlich einfacher — sie erben dann dieselbe Zeilenquelle statt einer Kopfzeilen-Sonderloesung.
**Erst diese Spec, ansehen, dann replizieren** — dasselbe Muster wie Etappe A → B.

## Bezug

[[2026-08-20-materialisierung-fachliche-felder-spec]] ·
[[2026-08-18-fa-liste-hierarchie-anzeige-spec]] (Freigabe-Kaskade dort mitziehen) ·
[[2026-09-08-bom-schnittstellen-bridge-hierarchisch-spec]] (Druck-Whitelist, B-0)
