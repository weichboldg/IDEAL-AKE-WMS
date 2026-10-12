---
type: spec
title: "Beschichtungsauftrag-Ausbau Teil 4 — Oberflaeche (Vorschlagsliste, Reklamation, Druck)"
slug: 2026-10-06-beschichtungsauftrag-ausbau-teil-4-spec
status: Testbereit
created: 2026-10-06
updated: 2026-10-12
source_backlog: "[[2026-10-06-beschichtungsauftrag-ausbau]]"
depends_on:
  - "[[2026-10-06-beschichtungsauftrag-ausbau-teil-2-spec]]"
  - "[[2026-10-06-beschichtungsauftrag-ausbau-teil-3-spec]]"
task: "[[2026-10-12-beschichtungsauftrag-ausbau-teil-4-umsetzung]]"
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
affected_code:
  - IdealAkeWms/Controllers/BeschichtungsauftraegeController.cs (neu — Liste, Details, Entwurf bearbeiten (POST), Positionen hinzufuegen (POST, ruft Teil-3 PositionenHinzufuegenAsync fuer Erst- und Reklamations-Entwurf; Details laedt die Kandidatenmenge je Entwurfsart aus Teil 3), Bemerkung (POST), Anlegen je HauptFA (POST), Statuswechsel (POST; Ablehnung ohne Dienstleister als WarningMessage), Reklamation (GET Auswahl + POST), Print/{id}, Pdf/{id}; Class-Level wie FaHierarchyBeschichtungController.cs:27-28, schreibende Actions mit Action-Level-Schreibfilter laut Teil-3-Antwort 3, alle POSTs [ValidateAntiForgeryToken])
  - IdealAkeWms/Views/Beschichtungsauftraege/Index.cshtml (neu, ADR-0005-Liste der Auftraege)
  - IdealAkeWms/Views/Beschichtungsauftraege/Details.cshtml (neu — Kopf + Positionen, Bearbeiten nur im Entwurf, Positionsauswahl 'Positionen hinzufuegen' (Erst-Entwurf: freie Positionen des HauptFA; Reklamations-Entwurf: noch nicht enthaltene Positionen des Ursprungs; Mehrfachauswahl, nur Entwurf + Schreibrecht), Bemerkung, Status- und Reklamations-Buttons, Verweise Ursprung/Folgeauftraege)
  - IdealAkeWms/Views/Beschichtungsauftraege/Reklamation.cshtml (neu — Teileauswahl aus den Ursprungspositionen, nichts vorausgewaehlt)
  - IdealAkeWms/Views/Beschichtungsauftraege/Print.cshtml (neu — Aussendokument ab Uebergeben; Briefkopf-Partial aus Teil 2, Positionen summiert nach Artnr mit Maßen, Barcode = Auftragsnummer, CSS eingebettet)
  - IdealAkeWms/Views/FaHierarchyBeschichtung/Summiert.cshtml (aus Teil 1, Standardansicht) und Index.cshtml (Einzelliste, Gruppenkopf Zeilen 64-103) — je HauptFA-Gruppenkopf Button 'Aus Vorschlagsliste erstellen' (POST-Form) bzw. Auftragsnummer(n) als Link
  - IdealAkeWms/Services/BeschichtungsauftragService.cs bzw. Controller (Vergabe-Info je HauptFA/Position fuer die Markierung, ueber das Repository aus Teil 3)
  - IdealAkeWms/Services/PdfFileNameBuilder.cs (Variante fuer die Auftragsnummer; heute nur Build(string, int hauptFa, DateTime) bei :9)
  - IdealAkeWms/Filters/ + Models/RoleKeys.cs + Services/ICurrentUserService.cs + Services/CurrentUserService.cs (Schreibrolle laut Teil-3-Antwort 3)
  - secondbrain/codebase/controller.md + IdealAkeWms/Views/Users/RoleOverview.cshtml (Pflichtstellen 2 und 3 bei neuer Rolle)
  - IdealAkeWms/Views/Shared/_Layout.cshtml (Navigationseintrag 'Beschichtungsauftraege' neben :112-118, gleiche Bedingung)
  - IdealAkeWms/Models/ViewModels/ColumnDefinitions.cs (ViewConfig der neuen Liste)
  - IdealAkeWms/Views/Help/ + Views/Help/Changelog.cshtml + beide AppVersion.cs
  - docs/TESTSZENARIEN.md + secondbrain/tests/testszenarien-index.md
open_questions: []
beantwortete_rueckfragen:
  - "1 Doppelvergabe-Darstellung -> markieren mit Auftragsnummer, erneut nur ueber Reklamation (Keyuser 2026-10-07, Freigabe-Block Nr. 1)"
  - "2 Auswahl-Einheit -> alle Positionen des HauptFA automatisch, danach im Entwurf editierbar, keine Haekchen (Keyuser 2026-10-07, Freigabe-Block Nr. 2)"
  - "3 Reklamation -> Teile auswaehlen, nichts vorbelegen (gilt nur fuer die Teileauswahl, Teil-3-Antwort 13), Vermerk am Ursprung (Keyuser 2026-10-07, Freigabe-Block Nr. 3)"
  - "4 Dienstleister -> Standard-Beschichter aus PPS-View (nur Erstauftraege, Teil-3-Antwort 13), als Freitext ueberschreibbar, kein Kreditoren-Stamm (Keyuser + Nachtrag Gerald 2026-10-07, Freigabe-Block Nr. 4)"
  - "5 Briefkopf -> Teil 2 Pflicht, Empfaenger = Beschichter des Auftrags (Gerald 2026-10-07, Freigabe-Block Nr. 5)"
  - "6 Druck -> summiert nach Teil-1-Schluessel, Masse angedruckt, Barcode = Auftragsnummer (Gerald 2026-10-07, Freigabe-Block Nr. 6)"
  - "7 Bearbeitbarkeit -> Kopf und Positionen nur im Entwurf, danach Status und Bemerkung (Gerald 2026-10-07, Freigabe-Block Nr. 7)"
  - "8 Drucken im Entwurf -> nein, ab Uebergeben (Gerald 2026-10-07, Freigabe-Block Nr. 8)"
  - "9 Dienstleister Pflicht -> im Entwurf optional, Pflicht beim Wechsel nach Uebergeben, Workflow lehnt mit Klartext ab (Gerald 2026-10-09, Freigabe-Block Nr. 9)"
epic: false
etappen: []
deploy:
  web: true
  service: false
  migration: false
freigabe_entscheidung: "Freigegeben wie Antworten inkl. 9 (Dienstleister Pflicht ab Uebergeben) und Folgen aus Teil 3 (Positionsauswahl Erst-/Reklamationsentwurf). Umsetzung direkt im Buendel nach Teil 3."
freigabe_von: "Gerald Weichbold"
freigabe_am: 2026-10-12
---

> **Umsetzungsort (Entscheidung Gerald 2026-10-09):** direkt im Bündel-Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5` auf Zweig `feature/2026-08-07-ideal-teile-1-5` (kein eigener Worktree, ein Lauf zur Zeit). SQL-Nummer, TS-Nummer und Version: naechste freie zum Umsetzungszeitpunkt (SQL in **beiden**
> Zweigen pruefen). Code-Belege beziehen sich auf den Buendel-Stand
> `.claude/worktrees/2026-08-07-ideal-teile-1-5` (v1.47.1).

> [!info] Einarbeitung 2026-10-09
> Antwort 9 (Gerald 2026-10-09) und die Folgen der Teil-3-Antworten 10–13 sind in Rumpf, `affected_code`,
> Akzeptanzkriterien und Testszenarien eingearbeitet: Positionsauswahl im Entwurf (Teil-3-Antwort 12),
> Reklamations-Kopf vom Ursprung und PPS-View-Vorbelegung nur fuer Erstauftraege (Teil-3-Antwort 13),
> Reklamation nur aus `Erledigt` (Teil-3-Antwort 11), Dienstleister-Pflicht beim Wechsel nach `Uebergeben`
> (Antwort 9; die Pruefung selbst sitzt im Teil-3-Workflow, Anforderung 5). Teil 4 hat keine eigene offene
> Rueckfrage mehr, haengt aber an den offenen Teil-3-Rueckfragen 14 (Hinzufuegen im Reklamations-Entwurf) und
> 15 (Reichweite der Reklamations-Ausnahme) aus [[2026-10-06-beschichtungsauftrag-ausbau-teil-3-spec]].
> Dienstleister bleibt die **Freitext**-Spalte `Beschichtungsauftrag.Dienstleister` aus Teil 3 — es gibt keinen
> `CreditorId` (verworfen mit Antwort 4 + Nachtrag).

> [!info] Einarbeitung 2026-10-12
> Folgen der Teil-3-Antworten 14 und 15 (Gerald 2026-10-12) eingearbeitet: Die Positionsauswahl erscheint jetzt
> auch im **Reklamations-Entwurf**, dort nur mit noch nicht enthaltenen Positionen des Ursprungs (Anforderung
> 4a, AK 6d); Reklamation ist auch an einem erledigten Reklamationsauftrag und mehrfach je Position moeglich
> (Anforderung 5, AK 5c). Die Abhaengigkeit von offenen Teil-3-Rueckfragen im Kasten oben ist damit erledigt —
> Teil 3 hat keine offene Rueckfrage mehr.

Teil 4 von 4 — Uebersicht: [[2026-10-06-beschichtungsauftrag-ausbau-uebersicht]]. Standort: **IDEAL**.

**Abhaengigkeiten (Stand 2026-10-07):** Teil 3 (Objekt, Workflow, Schreibrolle) und Teil 2 (Briefkopf-Partial,
Pflicht laut Antwort 5). **Keine** Abhaengigkeit mehr zur [[2026-10-06-kreditoren-stammdaten-spec]]: der
Dienstleister wird als Freitext ueberschrieben (Antwort 4, Nachtrag Gerald); die Auswahl aus dem
Kreditoren-Stamm kommt spaeter, ausserhalb dieses Pakets. Teil 1 ist weich: der Auftragsdruck summiert nach
dem Teil-1-Schluessel (Antwort 6, eigene GroupBy auf den gespeicherten Positionen, kein Code aus Teil 1
noetig); ist Teil 1 gemergt (empfohlene Reihenfolge), bekommt auch dessen `Summiert.cshtml` den Anlege-Button.
~~(Kreditor-Entitaet). Die Kreditoren-Spec existiert zum Zeitpunkt dieser Spec noch nicht — Entitaets-,
Feld- und Rollennamen des Kreditors sind hier Platzhalter und werden nach deren Freigabe eingesetzt.~~
~~*(ueberholt 2026-10-06: die Kreditoren-Spec liegt vor, Namen sind unten eingesetzt — siehe Namensabgleich.)*
Teil 2 (Briefkopf) ist weich vorausgesetzt (Rueckfrage 5).~~ *(ueberholt mit Antworten 4 und 5)*

> [!note] Namensabgleich (2026-10-06) — UEBERHOLT 2026-10-07 (verworfen mit Antwort 4: kein Kreditor-FK in Teil 4)
> Die Kreditor-Namen in dieser Spec stammen aus [[2026-10-06-kreditoren-stammdaten-spec]] und sind
> **vorlaeufig**: Entitaet `Creditor`, Tabelle `Creditors`, Sage-Schluessel `SageCreditorNumber`, Felder
> `ShortName`, `IsActive`, `Name`, `Street`, `PostalCode`, `City`, `Country`, Repository `ICreditorRepository`.
> Abgeleitet hier: FK-Spalte `Beschichtungsauftrag.CreditorId`, Navigation `Creditor`, Migration
> `AddBeschichtungsauftragCreditor`, EF-Konventionsnamen `FK_Beschichtungsauftraege_Creditors_CreditorId` und
> `IX_Beschichtungsauftraege_CreditorId`. Alle diese Namen werden **zusammen** bei Schranke 1 beider Specs
> bestaetigt. Der Tabellenteil `Beschichtungsauftraege` haengt zusaetzlich an Teil 3 („Namen im Dev-Lauf final",
> [[2026-10-06-beschichtungsauftrag-ausbau-teil-3-spec]]) und zieht bei einer Aenderung dort mit. Rollennamen fuer den Kreditor braucht Teil 4 nicht — das Setzen des Kreditors am Auftrag laeuft
> ueber die Schreibrolle aus Teil 3; die Stammdaten-Rollen (`masterdata`/`masterdata_read`) betreffen nur die
> Pflege des Stamms.

> [!warning] Hinweis Abgleich — Vorbelegung — UEBERHOLT 2026-10-07 (geloest mit Antwort 4: Vorbelegung = Sage-Text `FaHierarchyOrderInfo.Dienstleister`, keine Zuordnung zum Kreditor noetig)
> Rueckfrage 4 empfiehlt die Vorbelegung des Kreditors „ueber die Zuordnung der Kreditoren-Spec". Die
> Kreditoren-Spec sieht dafuer nur eine **Meldefunktion** vor (`CreditorAssignment.FindUnassigned(infos, creditors)`,
> Text → „nicht zugeordnet"/„inaktiv"), **keine** wiederverwendbare Abfrage „Dienstleister-Text → Kreditor".
> Zudem ist `ShortName` nur bei bestaetigtem Kurzname-Abgleich (deren Rueckfrage 2) eindeutig. Wird die
> Vorbelegung freigegeben, muss eine der beiden Specs die Zuordnungsfunktion als gemeinsame Methode vorsehen —
> bei Schranke 1 gemeinsam entscheiden.

## Ziel / Nutzen (das Warum)

Teil 3 schafft das gespeicherte Objekt, aber niemand kann es bedienen. Teil 4 liefert den Arbeitsablauf aus
der Anforderung: aus der heutigen Ansicht (Vorschlagsliste) je HauptFA einen Auftrag anlegen → im Entwurf
nachbearbeiten → uebergeben → drucken (Freigabe kommt spaeter), Reklamation mit leicht editierbaren Details,
Dienstleister am Auftrag ueberschreibbar. ~~Positionen ... waehlen~~ *(verworfen mit Antwort 2)*;
~~Dienstleister als gepflegter Kreditor statt Sage-Freitext — damit das Dokument eine echte Empfaengeradresse
traegt~~ *(verworfen mit Antwort 4: Freitext genuegt, Kreditoren-Stamm spaeter)*.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**
- Vorschlagsliste: Button **„Aus Vorschlagsliste erstellen"** je HauptFA-Gruppe in der Beschichtungs-Ansicht
  (summierte Standardsicht aus Teil 1 und Einzelliste), Markierung bereits vergebener Positionen mit
  Auftragsnummer. ~~Auswahl~~ *(verworfen mit Antwort 2: keine Einzelauswahl per Haekchen)*
- Neue Liste `/Beschichtungsauftraege` (ADR 0005), Detail, Bearbeiten im Entwurf, Bemerkung, Statuswechsel-Buttons.
- **Positionsauswahl im Entwurf** („Positionen hinzufuegen"), Mehrfachauswahl, ruft Teil-3
  `PositionenHinzufuegenAsync`: im Erst-Entwurf freie Positionen desselben HauptFA (Teil-3-Antwort 12), im
  Reklamations-Entwurf nur noch nicht enthaltene Positionen des Ursprungs (Teil-3-Antwort 14).
- Button **„Reklamation"** am Auftrag (nur `Erledigt`): Teile auswaehlen, nichts vorausgewaehlt, Vermerk am
  Ursprung (Antwort 3); Kopf kommt 1:1 vom Ursprung (Teil-3-Antwort 13).
- Dienstleister als Freitext am Auftrag, bei Erstauftraegen vorbelegt aus Sage, ueberschreibbar (Antwort 4);
  Pflicht beim Wechsel nach `Uebergeben` (Antwort 9).
  ~~Dienstleister-Auswahl aus dem Kreditoren-Stamm (FK auf den Kreditor) — Migration.~~ *(verworfen mit
  Antwort 4)*
- Druck + PDF des gespeicherten Auftrags ab `Uebergeben` (Antwort 8), mit Briefkopf (Antwort 5), summiert
  (Antwort 6).
- Schreibrolle und Action-Level-Filter laut Teil-3-Antwort 3 (drei Pflichtstellen bei neuer Rolle).

**Out-of-Scope**
- Freigabe-Schritt (Teil-3-Rueckfrage 6), Positions-Haekchen Reklamation/Neubeschichtung und Anzeige des
  urspruenglichen HauptFA (Teil-3-Rueckfrage 7).
- Dienstleister-Auswahl aus dem Kreditoren-Stamm, Kreditor-Fremdschluessel, Empfaengeradresse aus dem Kreditor
  — spaeter (Antwort 4, Nachtrag Gerald); Pflege des Stamms selbst ist die Kreditoren-Spec.
- Einzelauswahl von Positionen per Haekchen (Antwort 2).
- Mailversand, enaio-Ablage.
- AKE (eine AKE-Variante war in [[2026-07-29-standort-ideal-teil-4-spec]] Antwort 3 angedacht, ist nicht
  Teil dieser Anforderung).

## Fachliche Anforderungen

1. **Vorschlagsliste** = die Beschichtungs-Ansicht (Filter `Beschichtet == true`,
   `BeschichtungsauftragService.cs:85`) — Standard ist die summierte Sicht aus Teil 1, daneben die Einzelliste
   (`Index.cshtml`). Je HauptFA-Gruppenkopf (`Index.cshtml:64-103` bzw. Gruppenkopf der Summiert-Sicht) steht
   der Button **„Aus Vorschlagsliste erstellen"** (nur mit Schreibrecht). Er uebernimmt **automatisch alle**
   beschichteten Positionen des HauptFA (Antwort 2); ~~Auswahl je Position/Gruppe~~ *(verworfen mit Antwort 2)*.
   Bereits vergebene Positionen werden **markiert** mit Auftragsnummer als Link und sind nicht erneut vergebbar
   ausser ueber Reklamation (Antwort 1, Teil-3-Antwort 4): der Gruppenkopf nennt die Auftragsnummer(n) — offene
   Reklamationsauftraege eingeschlossen (sie zaehlen nach Teil-3-Anforderung 6 mit, Herleitung 2026-10-12); in der
   Einzelliste traegt zusaetzlich jede vergebene Position ein Badge „in BA-…" in einer bestehenden Zelle (keine
   neue Datenspalte, daher keine neue Spaltenfilter-Pflicht). Sind alle Positionen des HauptFA vergeben,
   erscheint statt des Buttons nur die Auftragsnummer. **Teilweise vergebene HauptFA** (Teil-3-Antwort 12): der
   Gruppenkopf zeigt die Auftragsnummer(n) **und** den Button; er legt einen weiteren Auftrag nur mit den freien
   Positionen an (Teil-3-Anforderung 7). Alternativ fuegt der Anwender freie Positionen im Detail eines
   bestehenden Entwurfs hinzu (Anforderung 4a). ~~Teilweise vergebene HauptFA: Teil-3-Rueckfrage 12 (bis zur
   Antwort lehnt der Workflow ab, die Sammelmeldung nennt die vergebenen Positionen).~~ *(ueberholt mit
   Teil-3-Antwort 12)*
2. **„Aus Vorschlagsliste erstellen"** POSTet den HauptFA an den Teil-3-Workflow (`AnlegenAsync(hauptFa)`).
   Ablehnungen (alle Positionen vergeben, keine beschichteten Positionen mehr im Cache) erscheinen als
   sichtbare Sammelmeldung mit Positionsliste (`TempData["WarningMessage"]`); der Hinweis „Kopf aus Sage
   mehrdeutig" (Kombigeraet, Teil-3-Anforderung 4) und der Hinweis „n Positionen bereits in BA-… vergeben,
   nicht uebernommen" (Teil-3-Anforderung 7) erscheinen nach erfolgreicher Anlage als `WarningMessage` im Detail.
   Bei Erfolg Redirect auf das Detail des neuen Auftrags mit `TempData["SuccessMessage"]` inkl. Nummer.
3. **Liste der Auftraege** (ADR 0005): Pagination (`PageSize.Resolve` + `PaginationState` + `_Pagination`),
   Filterkarte (mind. Status), Server-Spaltenfilter an **jeder** Spalte (`data-server-column-filter="true"`,
   `data-col-key`, `ColumnFilterHelper.ReadFromQuery`); Spalten mind. Nummer, HauptFA, Status, Dienstleister,
   RAL, Start, Retour, Anzahl Positionen, Ursprungsauftrag, Angelegt am/von. Datumsspalten in C# nach der
   Projektion filtern. Spalten in `<script id="column-config">` **und** `ColumnDefinitions` (fallstricke.md,
   Zeilen 960-966).
4. **Detail:** Kopf, Positionen, Statushistorie aus den Statusfeldern, Verweis Ursprungs-/Folgeauftrag;
   Statuswechsel nur ueber die erlaubten Uebergaenge (Teil 3). **Bearbeitbarkeit** (Antwort 7): Kopf
   (Dienstleister, RAL, Start, Retour) und Positionen (Menge, entfernen, hinzufuegen) nur im `Entwurf`; danach
   nur Statuswechsel und Bemerkung.
4a. **Positionsauswahl im Entwurf** (Teil-3-Antwort 12, Teil-3-Anforderung 10a): Im Detail eines Erst-Entwurfs
   zeigt ein Bereich „Positionen hinzufuegen" (nur mit Schreibrecht) die **freien** beschichteten Positionen
   desselben HauptFA — ermittelt beim Seitenaufbau aus dem Cache (`IFaHierarchyNodeRepository.GetByHauptFaAsync`,
   `FaHierarchyNodeRepository.cs:18-26`) abzueglich der Vergabe-Abfrage aus Teil 3 (Status != `Storniert`) — als
   Mehrfachauswahl (Checkbox je Position, **nichts** vorausgewaehlt) mit Artnr, Matchcode, Sollmenge, Maßen.
   Gibt es keine freie Position, steht dort der Text „Keine freien beschichteten Positionen fuer diesen HauptFA"
   statt einer leeren Liste. Absenden = POST mit Antiforgery-Token, RowVersion und den Herkunftsschluesseln
   (`VaterFA`, `SubFA`, `Position` — **nicht** die Knoten-Id, sie aendert sich mit jedem Sync) an
   `PositionenHinzufuegen`, der `PositionenHinzufuegenAsync` aufruft. Ablehnungen des Workflows (z. B. Position
   inzwischen vergeben oder nicht mehr im Cache, leere Auswahl) → `TempData["WarningMessage"]` mit
   Positionsliste, Redirect auf das Detail; Erfolg → `SuccessMessage` mit Anzahl.
   ~~Im Reklamations-Entwurf wird der Bereich bis zur Antwort auf Teil-3-Rueckfrage 14 **nicht** angezeigt (der
   Workflow lehnt dort ohnehin ab).~~ *(ueberholt mit Teil-3-Antwort 14)*
   **Im Reklamations-Entwurf** (Teil-3-Antwort 14 b, Teil-3-Anforderung 10a) zeigt derselbe Bereich unter der
   Ueberschrift „Positionen des Ursprungs BA-… hinzufuegen" (Nummer als Link auf den Ursprung) nur die Positionen
   des **direkten** Ursprungsauftrags, die noch nicht in diesem Entwurf stehen — aus den gespeicherten
   Ursprungspositionen, **nicht** aus dem Cache (der HauptFA kann dort fehlen), mit Artnr, Matchcode, Menge (des
   Ursprungs) und Maßen; nichts vorausgewaehlt. Freie Positionen des HauptFA erscheinen hier **nicht** (sie gehen
   in einen Erstauftrag). Sind alle Ursprungspositionen enthalten, steht dort „Alle Positionen des Ursprungs sind
   bereits enthalten". Eine Ursprungsposition, die zugleich in einer anderen Reklamation steckt, ist auswaehlbar
   (Teil-3-Antwort 15). Gleiches Formular, gleiche Action, gleiches Eingabeformat (Herkunftsschluessel) wie im
   Erst-Entwurf; Ablehnungen und Erfolg wie oben.
   Kein seitenuebergreifender Picker, keine eigene Seite — die Menge ist auf einen HauptFA bzw. einen Ursprung
   begrenzt.
5. **Reklamation** (Antwort 3, Teil-3-Antworten 2, 11, 13): Button am Auftrag **nur** im Status `Erledigt`
   (Teil-3-Antwort 11; in anderen Status kein Button, direkter Aufruf → `WarningMessage`) oeffnet eine Auswahl
   der Ursprungspositionen, **nichts** vorausgewaehlt; Absenden legt den Folgeauftrag (`Entwurf`) an und oeffnet
   ihn editierbar. ~~(ob auch `Uebergeben`: Teil-3-Rueckfrage 11)~~ *(entschieden mit Teil-3-Antwort 11)*
   ~~Folgeauftrag mit allen Positionen des Ursprungs vorbelegt~~ *(verworfen mit Antwort 3)*. **Vermerk am
   Ursprungsauftrag:** dessen Detail und die Liste zeigen „Reklamiert → BA-…" mit Link auf jeden Folgeauftrag
   (aus `UrsprungsauftragId`, Teil 3); der Folgeauftrag zeigt den Ursprung. **Kopf des Folgeauftrags**
   (Teil-3-Antwort 13): Dienstleister, RAL, Start, Retour 1:1 vom Ursprung (der Workflow kopiert sie), im
   `Entwurf` aenderbar; „nichts vorbelegen" (Antwort 3) betrifft nur die Teileauswahl. ~~Kopf-Vorbelegung des
   Folgeauftrags: Teil-3-Rueckfrage 13.~~ *(entschieden mit Teil-3-Antwort 13)*
   **Mehrfach und verkettet** (Teil-3-Antwort 15): Der Button steht an **jedem** `Erledigt`-Auftrag — auch an
   einem erledigten Reklamationsauftrag (Reklamation einer Reklamation) und auch dann, wenn zu diesem Ursprung
   schon eine Reklamation besteht; die Auswahl bietet alle Ursprungspositionen an, auch solche in einer anderen
   Reklamation. Der Vermerk „Reklamiert → BA-…" listet alle Folgeauftraege.
6. **Dienstleister** (Antwort 4, Antwort 9): Freitextfeld am Auftrag. **Erstauftrag:** vorbelegt mit dem
   Standard-Beschichter aus der PPS-View (`FaHierarchyOrderInfo.Dienstleister`, `FaHierarchyOrderInfo.cs:27`,
   nur vom Sync geschrieben `FaHierarchySyncService.cs:282`). **Reklamations-Folgeauftrag:** Wert des Ursprungs
   (Teil-3-Antwort 13 — Antwort 4 „Standard-Beschichter aus der PPS-View" gilt nur fuer Erstauftraege). In
   beiden Faellen im `Entwurf` ueberschreibbar — auch und gerade beim Reklamations-Folgeauftrag. Weicht der Wert
   vom aktuellen Sage-Wert ab, zeigt das Detail den Sage-Wert daneben (live aus dem Cache; fehlt der HauptFA
   dort, kein Vergleich). **Pflicht** (Antwort 9): im `Entwurf` optional; der Wechsel nach `Uebergeben` wird
   ohne Dienstleister vom Teil-3-Workflow abgelehnt (Teil-3-Anforderung 5), der Controller zeigt die Ablehnung
   als `TempData["WarningMessage"]` im Detail, der Status bleibt `Entwurf`. Der Uebergeben-Button bleibt
   sichtbar (die Meldung erklaert, was fehlt); zusaetzlich zeigt das Detail im Entwurf bei leerem Dienstleister
   den Hinweis „Dienstleister fehlt — zum Uebergeben erforderlich". Der Briefkopf-Text „Empfaenger fehlt — am
   Auftrag nachtragen" bleibt als Invarianten-Anzeige fuer den Fall, der nach Antwort 9 nicht mehr eintreten
   darf (Melden statt still). ~~Pflicht laut Rueckfrage 9; bis zur Antwort gilt: ein leerer Dienstleister wird
   nie still gedruckt~~ *(ueberholt mit Antwort 9)*
   ~~Auswahl eines aktiven Kreditors (`Creditor.IsActive == true`) ... Fehlende Zuordnung wird gemeldet~~
   *(verworfen mit Antwort 4)*
7. **Druck/PDF** des Auftrags; Druck und PDF sind dasselbe Dokument (dieselbe View ueber `IRazorViewRenderer` +
   `IPdfRenderService`, Muster `FaHierarchyBeschichtungController.cs:110-136`); PDF-Knopf nur bei
   `PdfRenderStatus.IsAvailable`, ohne `target="_blank"`; Dateiname ohne Umlaute/Klarnamen, mit Auftragsnummer.
   - **Nur ab `Uebergeben`** (Antwort 8): im `Entwurf` (und `Storniert`) keine Druck-/PDF-Knoepfe; ein direkter
     Aufruf von `Print`/`Pdf` fuer einen Entwurf fuehrt mit `WarningMessage` „Im Entwurf nicht druckbar — erst
     uebergeben" zurueck auf das Detail. ~~Drucken im Entwurf erlaubt, ohne Wasserzeichen~~ *(verworfen mit
     Antwort 8)*
   - **Briefkopf** aus Teil 2 (`_PrintBriefkopf`) ist Pflicht (Antwort 5); **Empfaenger** = Dienstleister des
     Auftrags (Freitext). ~~Empfaengeradresse aus dem Kreditor~~ *(verworfen mit Antwort 4)*
   - **Positionen summiert** nach dem Teil-1-Schluessel (Antwort 6): da ein Auftrag genau einen HauptFA hat
     (Teil-3-Antwort 5), ist das eine Zeile je `Artnr` mit Summe der gespeicherten `Menge`, Matchcode und
     **Maße angedruckt** (aus der ersten Position der Gruppe; abweichende Maße → „Maße uneinheitlich" wie in
     Teil 1, Anforderung 1).
   - **Barcode = Auftragsnummer** (Antwort 6), Code 128 ueber `IBarcodeService.EncodeCode128Base64`
     (`FaHierarchyBeschichtungController.cs:94`).
8. **Zugriff:** Class-Level `[RequireBeschichtungsauftragAccess]` + `[RequireFaHierarchyBeschichtungAktiv]`
   auf dem neuen Controller (Lesen); **jede** schreibende Action (Anlegen, Entwurf bearbeiten, Positionen
   hinzufuegen, Bemerkung, Status, Reklamation) zusaetzlich Action-Level-Schreibfilter der neuen Berechtigungsgruppe
   „Beschichtungsaufträge" (Teil-3-Antwort 3; ADR 0006 Read/Edit-Split). ~~Kreditor setzen~~ *(verworfen mit
   Antwort 4)*. Schreib-Buttons — auch „Aus Vorschlagsliste erstellen" in der Beschichtungs-Ansicht — nur bei
   Schreibrecht rendern. POSTs mit `[ValidateAntiForgeryToken]`. Neue Rolle = drei Stellen: Attribut,
   `secondbrain/codebase/controller.md`, `Views/Users/RoleOverview.cshtml` (dort klar als Schreibrecht neben der
   Leserolle „Beschichtungsauftrag (IDEAL)", `RoleOverview.cshtml:194`).
9. **Navigation:** Eintrag „Beschichtungsauftraege" unter derselben Bedingung wie der bestehende Eintrag
   (`_Layout.cshtml:112-118`: Rolle + Toggle).

## Technischer Loesungsentwurf

- **Neuer Controller** `BeschichtungsauftraegeController` statt Ausbau von `FaHierarchyBeschichtungController`:
  dieser ist die Lese-Sicht auf den Sage-Cache; das gespeicherte Objekt hat eigene Routen mit `id`. Die
  Vorschlagsliste bleibt im bestehenden Controller und POSTet an `BeschichtungsauftraegeController.Create`.
- **Anlegen aus der Vorschlagsliste:** je HauptFA-Gruppenkopf ein kleines `<form method="post">` mit
  Hidden-Feld `hauptFa` und Antiforgery-Token, Ziel `BeschichtungsauftraegeController.Create`. Keine
  Checkbox-Spalte, `colCount` (`Index.cshtml:11`) bleibt. ~~`<form method="post">` um die Tabelle, Checkbox je
  Positionszeile ... Gruppen-Checkbox~~ *(verworfen mit Antwort 2)*
- **Positionsauswahl im Detail** (Anforderung 4a): ein `<form method="post">` mit Antiforgery-Token und
  RowVersion-Hidden-Feld, eine Checkbox je freier Position, deren Wert den Herkunftsschluessel traegt; Action
  `PositionenHinzufuegen(int id, …, byte[] rowVersion)` mit Action-Level-Schreibfilter, uebersetzt das
  Workflow-Ergebnis in `SuccessMessage`/`WarningMessage` + `RedirectToAction(Details)`. Die Kandidaten liefert
  Teil 3 je Entwurfsart (Erst-Entwurf: freie Positionen ueber dieselbe Vergabe-Abfrage wie die Markierung;
  Reklamations-Entwurf: noch nicht enthaltene Ursprungspositionen, Teil-3-Anforderung 10a) — keine zweite Logik
  im Controller; `Details` waehlt nur nach `UrsprungsauftragId` Ueberschrift und Leer-Text. Die
  Positions-Tabelle des Details und die Auswahl-Tabelle sind klein, unpaginiert und auf einen Auftrag bzw.
  HauptFA vorgefiltert → Spaltenfilter im **Client-Mode** (ADR 0005-Ausnahme), `<th>` mit `data-col-key`.
- **Vergabe-Markierung:** eine Abfrage „Schluessel → Auftragsnummer" fuer die HauptFA der aktuellen Seite
  ueber `IBeschichtungsauftragRepository` (kein Join gegen den Cache, keine N+1-Abfrage je Zeile). Badge-Text
  in einer bestehenden Zelle darf Server-Spaltenfilter und Client-Sortierung (`table-filter.js`) nicht
  verfaelschen — im Dev-Lauf pruefen.
- ~~**Kreditor-FK:** `Beschichtungsauftrag.CreditorId` (int?) + Navigation `Creditor` ... Namen vorlaeufig
  (Namensabgleich oben).~~ *(verworfen mit Antwort 4 — Dienstleister bleibt die Freitext-Spalte aus Teil 3)*
- **Druck:** `Views/Beschichtungsauftraege/Print.cshtml` mit `_PrintBriefkopf` (Teil 2, Empfaenger =
  `Beschichtungsauftrag.Dienstleister`), Kopf (Nummer, HauptFA, RAL, Start, Retour, Bemerkung), Positionen
  summiert je `Artnr` (GroupBy auf den gespeicherten Positionen im ViewModel-Aufbau; Maße-Invariante wie
  Teil 1), CSS eingebettet, Barcode der Auftragsnummer als Data-URI (`IBarcodeService.EncodeCode128Base64`,
  wie `FaHierarchyBeschichtungController.cs:94`). ~~Empfaengerblock aus `Creditor`~~ *(verworfen mit Antwort 4)*
  `Print`/`Pdf` pruefen den Status serverseitig (Entwurf/Storniert → Redirect mit `WarningMessage`).
- **PDF:** `[HttpGet("Beschichtungsauftraege/Pdf/{id:int}")]`; Fehler `PdfRenderException` →
  `TempData["WarningMessage"]` + Redirect auf das Detail. `PdfFileNameBuilder` um eine Variante fuer eine
  String-Kennung erweitern (heute `Build(string, int hauptFa, DateTime)`, `PdfFileNameBuilder.cs:9`); Nummer
  nur ASCII.
- **Concurrency:** RowVersion aus Teil 3 als Hidden-Feld in Bearbeiten/Status-Formularen; Konflikt →
  verstaendliche Meldung + Neuladen.
- `frontend-design`-Skill im Dev-Lauf Pflicht (Views); Bootstrap-5-Muster, Kontrast WCAG AA, Status nie
  nur ueber Farbe (Badge mit Text).

## Migrations-/SQL-Auswirkungen

**Keine** (Stand 2026-10-07). Mit Antwort 4 entfaellt der Kreditor-Fremdschluessel; der Dienstleister ist die
Freitext-Spalte aus Teil 3, `HauptFA` am Kopf ebenfalls Teil 3. Kein `SQL/XX_*.sql`, `SQL/00_FreshInstall.sql`
unveraendert, `SQL/AgentJobs/` unberuehrt. Voraussetzung im Deploy: die Teil-3-Tabellen sind angelegt.
Die neue Rolle ist ein statischer Key (ADR 0006): die bestehende Leserolle `beschichtungsauftrag` hat kein
SQL-Seed (Grep `beschichtungsauftrag` in `*.sql`/`*.cs` des Buendels: nur `RoleKeys.cs:24`,
`RequireBeschichtungsauftragAccessAttribute.cs`, `ICurrentUserService.cs`, `CurrentUserService.cs:138`,
Controller) — die Schreibrolle braucht daher ebenfalls kein Skript.

> [!quote]- UEBERHOLT — verworfen mit Antwort 4 (urspruenglicher Entwurf, nur Audit-Spur, NICHT umsetzen)
> Eine Spalte (Kreditor-FK `CreditorId`) auf der Teil-3-Tabelle:
> - Model → `dotnet ef migrations add AddBeschichtungsauftragCreditor --project IdealAkeWms` →
>   `SQL/XX_AddBeschichtungsauftragCreditor.sql` idempotent (`COL_LENGTH(N'dbo.Beschichtungsauftraege', N'CreditorId') IS NULL`
>   fuer die Spalte `CreditorId INT NULL`, `OBJECT_ID(N'dbo.FK_Beschichtungsauftraege_Creditors_CreditorId', N'F')` fuer
>   den FK-Constraint auf `dbo.Creditors(Id)` ohne Cascade, `sys.indexes` fuer `IX_Beschichtungsauftraege_CreditorId`;
>   Namen exakt wie von EF erzeugt, DDL je eigener Batch) → `__EFMigrationsHistory`-Insert separat →
>   `SQL/00_FreshInstall.sql` zwei Stellen.
> - XX = naechste freie Nummer in **beiden** Zweigen zum Umsetzungszeitpunkt; die Tabelle `dbo.Creditors` muss vorher
>   angelegt sein (Reihenfolge der Skripte im Deploy beachten).
> - Nicht daten-destruktiv (nullable Spalte). `SQL/AgentJobs/` unberuehrt.
> - Falls die Kreditoren-Spec und Teil 4 im selben Wartungsfenster deployt werden: Skriptreihenfolge
>   Kreditoren → Teil 3 → Teil 4.

## Audit-Feld-Auswirkungen

Keine neue Entitaet. Alle Schreibvorgaenge laufen ueber den Teil-3-Workflow und setzen dort `Created*` bzw.
`ModifiedAt`, `ModifiedBy`, `ModifiedByWindows` (aus `ICurrentUserService`) — auch das Ueberschreiben des
Dienstleisters, jede Positions-/Kopfaenderung im Entwurf und jede Bemerkungsaenderung. Statuswechsel zusaetzlich
Statuszeitpunkt + Benutzer. Das Hinzufuegen von Positionen setzt `Created*` an den neuen Positionen und
`Modified*` am Kopf (Teil-3-Workflow). Eine abgelehnte Aktion (z. B. Uebergeben ohne Dienstleister) aendert
keine Audit-Felder. ~~auch das Setzen des Kreditors~~ *(verworfen mit Antwort 4)*

## Akzeptanzkriterien

1. In der Beschichtungs-Ansicht (summiert und Einzelliste) hat jeder HauptFA-Gruppenkopf fuer Benutzer mit
   Schreibrecht den Button „Aus Vorschlagsliste erstellen"; er legt einen Auftrag im Status `Entwurf` mit
   **allen** beschichteten Positionen des HauptFA an und oeffnet dessen Detail mit Erfolgsmeldung inkl. Nummer.
   Es gibt keine Checkboxen.
2. Nach der Anlage zeigt der Gruppenkopf die Auftragsnummer als Link statt des Buttons; die Einzelliste
   markiert jede vergebene Position mit „in BA-…". Ein manipulierter POST fuer einen HauptFA, dessen Positionen
   **alle** vergeben sind, wird vom Workflow abgelehnt (Sammelmeldung mit Positionen und Auftragsnummer).
2a. Ist ein HauptFA nur teilweise vergeben (z. B. nach Entfernen einer Position oder Storno, oder weil Sage eine
   Position neu als beschichtet markiert hat), zeigt der Gruppenkopf Auftragsnummer(n) **und** den Button; der
   Klick legt einen weiteren Auftrag nur mit den freien Positionen an, das Detail meldet die nicht
   uebernommenen Positionen als `WarningMessage`.
3. `/Beschichtungsauftraege` erfuellt ADR 0005 vollstaendig (Pagination, Filterkarte, Server-Spaltenfilter an
   jeder Spalte).
4. Statuswechsel-Buttons zeigen nur erlaubte Uebergaenge; jeder Wechsel ist im Detail mit Zeitpunkt und
   Benutzer sichtbar.
5. „Reklamation" an einem `Erledigt`-Auftrag zeigt die Ursprungspositionen ohne Vorauswahl; Absenden ohne
   Auswahl wird abgelehnt; mit Auswahl entsteht ein `Entwurf`-Folgeauftrag mit genau diesen Positionen. Der
   Ursprung zeigt „Reklamiert → BA-…" (Detail und Liste), der Folgeauftrag den Ursprung; beide verlinkt.
5a. In `Entwurf`, `Uebergeben` und `Storniert` gibt es keinen Reklamations-Button; ein direkter Aufruf (GET oder
   POST) fuehrt mit `WarningMessage` zurueck auf das Detail, es entsteht kein Auftrag.
5b. Der Kopf des Reklamations-Folgeauftrags zeigt Dienstleister, RAL, Start und Retour exakt wie der Ursprung —
   auch wenn die PPS-View fuer den HauptFA inzwischen andere Werte liefert oder ihn nicht mehr enthaelt; die
   Felder sind im Entwurf aenderbar.
5c. Ein erledigter Reklamationsauftrag hat den Reklamations-Button; daraus entsteht ein Folgeauftrag mit Verweis
   auf diese Reklamation. Zu einem `Erledigt`-Ursprung mit bereits offener Reklamation (`Entwurf`/`Uebergeben`)
   laesst sich eine zweite Reklamation derselben Position anlegen; der Ursprung zeigt beide Folgeauftraege.
   „Aus Vorschlagsliste erstellen" uebernimmt diese Position in keinem Fall (Gruppenkopf nennt Ursprung und
   Reklamation(en)).
6. Der Dienstleister eines **Erstauftrags** ist beim Anlegen mit `FaHierarchyOrderInfo.Dienstleister` vorbelegt
   und im `Entwurf` als Freitext ueberschreibbar (ebenso im Reklamations-Folgeauftrag, dort vorbelegt vom
   Ursprung); nach `Uebergeben` nicht mehr aenderbar. Weicht er vom aktuellen Sage-Wert ab, zeigt das Detail den
   Sage-Wert daneben.
6b. Ein Entwurf ohne Dienstleister laesst sich speichern; „Uebergeben" auf diesem Entwurf zeigt eine
   `WarningMessage` „Dienstleister fehlt …", der Status bleibt `Entwurf`, kein Druckknopf erscheint. Nach
   Eintragen des Dienstleisters gelingt das Uebergeben.
6c. Im Detail eines Erst-Entwurfs listet „Positionen hinzufuegen" genau die freien beschichteten Positionen des
   HauptFA (keine vergebenen, keine aus anderen HauptFA, keine schon enthaltenen), nichts vorausgewaehlt;
   Mehrfachauswahl + Absenden fuegt sie mit Snapshot hinzu (`SuccessMessage` mit Anzahl). Leere Auswahl, eine
   inzwischen vergebene Position oder ein veralteter Stand (RowVersion) → `WarningMessage`, nichts
   hinzugefuegt. Ohne freie Positionen erscheint der Hinweistext statt der Liste. In `Uebergeben`/`Erledigt`/
   `Storniert` fehlt der Bereich; ein direkter POST wird abgelehnt. Ohne Schreibrecht fehlt der Bereich, ein
   direkter POST endet in AccessDenied. ~~und im Reklamations-Entwurf (bis Teil-3-Rueckfrage 14)~~ *(ueberholt mit
   Teil-3-Antwort 14 → AK 6d)*
6d. Im Detail eines **Reklamations-Entwurfs** listet der Bereich „Positionen des Ursprungs BA-… hinzufuegen" genau
   die Positionen des direkten Ursprungs, die noch nicht im Entwurf stehen (keine freien HauptFA-Positionen),
   nichts vorausgewaehlt — auch wenn der HauptFA nicht mehr im Cache steht. Hinzufuegen uebernimmt Menge und
   Maße der Ursprungsposition (`SuccessMessage` mit Anzahl). Eine Position, die zugleich in einer anderen
   Reklamation steckt, laesst sich hinzufuegen. Ein manipulierter POST mit einer freien HauptFA-Position oder
   einer schon enthaltenen Position → `WarningMessage`, nichts hinzugefuegt. Sind alle Ursprungspositionen
   enthalten, erscheint „Alle Positionen des Ursprungs sind bereits enthalten".
6a. Kopf und Positionen sind nur im `Entwurf` aenderbar; in `Uebergeben`/`Erledigt` ist nur die Bemerkung
    editierbar (UI zeigt keine anderen Eingabefelder, ein direkter POST wird abgelehnt).
7. Druck und PDF des Auftrags (ab `Uebergeben`) zeigen den Briefkopf aus Teil 2 mit dem Dienstleister des
   Auftrags als Empfaenger, den Kopf, die Positionen **summiert je Artnr** mit Maßen und den Barcode der
   Auftragsnummer; PDF-Dateiname enthaelt die Auftragsnummer und keine Klarnamen.
7a. Im `Entwurf` gibt es keinen Druck-/PDF-Knopf; ein direkter Aufruf von `Print`/`Pdf` fuehrt mit
    `WarningMessage` zurueck auf das Detail.
8. Benutzer nur mit Leserecht sehen Liste, Detail, Druck und PDF, aber keine Schreib-Buttons; ein direkter
   POST auf eine schreibende Action endet in AccessDenied.
9. Toggle `FaHierarchyBeschichtungAktiv` aus → alle neuen Seiten gesperrt, Navigationseintrag fehlt.
10. Paralleles Bearbeiten desselben Auftrags fuehrt beim zweiten Speichern zu einer Concurrency-Meldung.
11. Bei neuer Rolle: Attribut, `controller.md` und `RoleOverview.cshtml` enthalten sie.
12. Bestehende Liste, Druck und PDF der Vorschlagsliste (Einzel- und Summiert-Sicht) funktionieren unveraendert;
    Benutzer ohne Schreibrecht sehen dort keinen Anlege-Button (Regression TS-60/TS-74).

## Test-Szenarien

Neues Kapitel (naechste freie Nummer), Manual-UAT am IDEAL-Testsystem mit beschichteten Positionen
(Vorbedingung wie TS-60: produktivnahe Daten):
- Happy Path: HauptFA in der summierten Sicht → „Aus Vorschlagsliste erstellen" → Detail (alle Positionen) →
  Menge aendern, eine Position entfernen, Dienstleister ueberschreiben → uebergeben → drucken/PDF (Briefkopf,
  summiert, Maße, Barcode = Nummer) → erledigt.
- Entwurf: Druck-/PDF-Knopf fehlt; direkter Aufruf `Print`/`Pdf` → WarningMessage.
- Nach `Uebergeben`: nur Bemerkung editierbar; direkter POST auf Kopf/Menge → Ablehnung.
- Doppelvergabe: Gruppenkopf zeigt Auftragsnummer statt Button; manipulierter POST → Ablehnung.
- Sync zwischen Seitenaufbau und Anlegen (HauptFA ohne beschichtete Positionen) → Ablehnung mit Klartext.
- Kombigeraet → Anlage gelingt, Kopffelder mit abweichenden Werten leer, Hinweis „Kopf aus Sage mehrdeutig".
- Reklamation aus `Erledigt` → keine Vorauswahl; leere Auswahl → Ablehnung; zwei Teile waehlen → Folgeauftrag
  mit genau diesen Teilen, Kopf (Dienstleister, RAL, Start, Retour) wie der Ursprung, Dienstleister
  ueberschreiben, Verweise in beiden Richtungen, Vermerk am Ursprung. Negativ: Reklamations-Button fehlt in
  `Uebergeben`; direkter Aufruf → WarningMessage. Vorbedingung fuer „Kopf vom Ursprung": im Entwurf des
  Ursprungs vorher einen vom Sage-Wert abweichenden Dienstleister eintragen — der Folgeauftrag muss diesen
  Wert tragen, nicht den Sage-Wert.
- Dienstleister-Pflicht: Entwurf mit geleertem Dienstleister speichern (gelingt) → Uebergeben → WarningMessage,
  Status bleibt Entwurf → Dienstleister eintragen → Uebergeben gelingt.
- Positionen hinzufuegen: im Entwurf eine Position entfernen → sie erscheint unter „Positionen hinzufuegen" →
  auswaehlen und hinzufuegen → wieder im Auftrag mit Sollmenge. Zweiter Entwurf desselben HauptFA (nach Storno
  oder Teilvergabe) zeigt die Position des ersten nicht. Negativ: zwei Browser, dieselbe freie Position in zwei
  Entwuerfe hinzufuegen → der zweite erhaelt WarningMessage (vergeben) oder Concurrency-Meldung; leere Auswahl
  → WarningMessage.
- Positionen im Reklamations-Entwurf: Reklamation mit einer von drei Ursprungspositionen anlegen → Bereich
  „Positionen des Ursprungs BA-… hinzufuegen" zeigt genau die zwei anderen, keine freien HauptFA-Positionen →
  eine hinzufuegen (Menge/Maße wie im Ursprung) → sie verschwindet aus der Auswahl. Negativ: manipulierter POST
  mit freier HauptFA-Position → WarningMessage; alle hinzugefuegt → Text „Alle Positionen des Ursprungs sind
  bereits enthalten". Vorbedingung fuer den Cache-Fall: HauptFA nicht mehr in der PPS-View — Auswahl bleibt
  befuellt.
- Mehrfach-/Ketten-Reklamation: zweite Reklamation derselben Position, waehrend die erste `Uebergeben` ist →
  gelingt, Ursprung zeigt beide; erste Reklamation erledigen → deren Reklamations-Button → Folgeauftrag mit
  Verweis auf die Reklamation. Vorschlagsliste: Gruppenkopf nennt Ursprung und Reklamationen, der Anlege-Button
  uebernimmt die Position nicht.
- Teilvergabe in der Vorschlagsliste: nach Entfernen einer Position bzw. Storno zeigt der Gruppenkopf Nummer
  **und** Button; Klick legt einen weiteren Auftrag nur mit den freien Positionen an.
- ~~Positionen waehlen → anlegen → Kreditor setzen~~, ~~mehrere HauptFA~~ *(verworfen mit Antworten 2, 4 und
  Teil-3-Antwort 5)*
- Unerlaubter Statuswechsel (direkter POST) → Ablehnung.
- Rechte: Leserecht ohne Schreib-Buttons; POST → AccessDenied; Toggle aus.
- Concurrency: zwei Browser, gleicher Auftrag.
- PDF ohne Edge → Knopf fehlt; PDF-Fehler → WarningMessage mit Rueckfallweg Bildschirmdruck.
- Liste: Spaltenfilter je Spalte, Pagination, Statusfilter.

## Etappen (nur bei epic: true)

Nicht zutreffend (`epic: false`).

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| 1 | — | — | — |

## Deploy

*Finalisiert vom QA-Lauf 2026-10-12 gegen den echten Diff `a5b27550..91c77105`.*

- **Web-App:** ja — `IdealAkeWms/` (Controller, Views, Filter, Rolle, Services, Layout, Hilfe, Changelog, `AppVersion` 1.51.0).
- **Service:** nein — in `IDEALAKEWMSService/` nur der Versions-Bump `AppVersion.cs` (1.51.0); kein funktionaler Unterschied,
  ein Service-Publish ist nicht noetig (nur fuer die Versionsanzeige).
- **Migration:** nein — keine Datei unter `*/Migrations/`, kein `SQL/`-Diff. Setzt die Teil-3-Tabellen (Migration 94,
  v1.50.0) voraus; im Buendel kommen sie mit.
- **Voraussetzung:** Teil 2 (Briefkopf) und Teil 3 (Tabellen) im selben Buendel; Firmendaten/Briefkopf-Vorlage gepflegt.
- **Betrieb (R1):** Rolle `beschichtungsauftrag_schreiben` hat **kein SQL-Seed** — unter `/Roles` anlegen und den
  zustaendigen Benutzern zuweisen, sonst sieht niemand die Schreib-Knoepfe.
- **Ablauf Mensch:** aus dem Worktree publishen → Testsystem → TS-84 testen → Merge. Nach dem Merge nur dann erneut aus
  `main` publishen, wenn der Merge getestete Dateien mit parallelen `main`-Aenderungen kombiniert hat.
- **Publish-Befehl (nur Web):**
  `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`

## Offene Rueckfragen

1. **Doppelvergabe in der Vorschlagsliste (Notiz Punkt 5):** markieren (mit Auftragsnummer, nicht waehlbar)
   oder ausblenden (ggf. Schalter „vergebene zeigen")? Muss zur Teil-3-Antwort 4 passen.
   *Empfehlung:* markieren, nicht ausblenden — sonst ist nicht erkennbar, warum ein Teil fehlt.
   *Beantwortet mit Antwort 1: markieren mit Auftragsnummer, erneut nur ueber Reklamation.*
2. **Auswahl:** einzelne Positionen, ganze HauptFA-Gruppe oder beides? Nur aktuelle Seite?
   *Beantwortet mit Antwort 2: alle Positionen des HauptFA automatisch, danach im Entwurf editierbar.*
   *Empfehlung:* ~~Checkbox je Position plus Gruppen-Checkbox; Auswahl gilt nur fuer die aktuelle Seite (die
   Seiteneinheit ist ohnehin die HauptFA-Gruppe) — kein seitenuebergreifender Merkzettel.~~ *(verworfen mit
   Antwort 2)*
3. **Reklamation:** Ablauf (laut Teil-3-Antwort 2 Folgeauftrag) und welche Details „leicht editierbar":
   Positionen entfernen/hinzufuegen, Menge, Reklamationsgrund/Bemerkung, Dienstleister, Termine?
   *Beantwortet mit Antwort 3: Teile auswaehlen, nichts vorbelegen, Vermerk am Ursprung (Kopf-Vorbelegung →
   Teil-3-Rueckfrage 13).*
   *Empfehlung:* ~~Folgeauftrag mit allen Positionen des Ursprungs vorbelegt~~ *(verworfen mit Antwort 3)*, im
   Entwurf alles editierbar (Positionen entfernen, Menge, Grund, Dienstleister, Termine); Positionen
   hinzufuegen nur aus dem Ursprung.
   *Kopf-Teil eingearbeitet (Teil-3-Antwort 13, 2026-10-09): „nichts vorbelegen" gilt nur fuer die
   Teileauswahl, Kopf 1:1 vom Ursprung — Anforderung 5, AK 5b. Hinzufuegen im Reklamations-Entwurf →
   Teil-3-Rueckfrage 14.* *(Teil-3-Rueckfrage 14 beantwortet 2026-10-12 — (b), wie hier empfohlen: Anforderung
   4a, AK 6d.)*
4. **Dienstleister aus dem Kreditoren-Stamm:** ab welchem Status Pflicht? Vorbelegung aus dem Sage-Freitext
   ueber die Zuordnung der Kreditoren-Spec? Sage-Freitext daneben anzeigen?
   *Beantwortet mit Antwort 4 (+ Nachtrag): Vorbelegung aus der PPS-View, Ueberschreiben als Freitext, kein
   Kreditoren-Stamm. Die Pflicht-Frage ist offen geblieben → Rueckfrage 9.*
   *Empfehlung:* Pflicht beim Wechsel nach `Uebergeben`, im Entwurf optional; ~~Vorbelegung nur bei eindeutiger
   Zuordnung~~ *(verworfen mit Antwort 4)*; Sage-Freitext immer daneben sichtbar.
5. **Briefkopf/Empfaenger im Auftragsdruck:** Ist Teil 2 Pflicht vor Teil 4 (dann `depends_on` um Teil 2
   ergaenzen), und kommt die Empfaengeradresse aus dem Kreditor?
   *Beantwortet mit Antwort 5: Briefkopf Pflicht (depends_on Teil 2), Empfaenger = Beschichter des Auftrags.*
   *Empfehlung:* ja zu beidem — der gespeicherte Auftrag ist das Aussendokument; ohne Briefkopf waere es
   unvollstaendig ([[2026-09-17-druck-briefkopf-logo-adresse]]). ~~Empfaengeradresse aus dem Kreditor~~
   *(verworfen mit Antwort 4/5 — Empfaenger ist der Freitext)*
6. **Positionsdarstellung im Druck:** summiert nach dem Teil-1-Schluessel (dann Teil 1 als weitere
   Abhaengigkeit) oder Einzelpositionen? Barcode = Auftragsnummer?
   *Beantwortet mit Antwort 6: summiert, Maße angedruckt, Barcode = Auftragsnummer.*
   *Empfehlung:* summiert je HauptFA nach dem Teil-1-Schluessel, Barcode = Auftragsnummer ~~(ein Auftrag kann
   mehrere HauptFA enthalten, Teil-3-Antwort 5)~~ *(Begruendung ueberholt: Teil-3-Antwort 5 = ein HauptFA je
   Auftrag)*.
7. **Bearbeitbarkeit je Status:** Kopf und Positionen nur im `Entwurf`, danach nur Statuswechsel und
   Bemerkung?
   *Beantwortet mit Antwort 7: ja.*
   *Empfehlung:* ja.
8. **Drucken im Entwurf:** erlaubt? Mit Kennzeichnung „ENTWURF"?
   *Beantwortet mit Antwort 8: nein, gedruckt wird ab „Uebergeben".*
   *Empfehlung:* ~~erlaubt (Ablauf „speichern, ausdrucken"; Freigabe kommt spaeter), ohne Wasserzeichen —
   sonst druckt der heutige Ablauf nur gekennzeichnete Dokumente. Mit dem Freigabe-Schritt neu bewerten.~~
   *(verworfen mit Antwort 8)*
9. **Dienstleister Pflicht (neu 2026-10-07):** Antwort 4 klaert Quelle und Ueberschreiben, nicht die Pflicht.
   Muss der Dienstleister — er ist der Empfaenger des Aussendokuments (Antwort 5), und gedruckt wird ab
   `Uebergeben` (Antwort 8) — beim Wechsel nach `Uebergeben` gefuellt sein?
   *Empfehlung:* ja, Pflicht beim Wechsel nach `Uebergeben` (Workflow lehnt mit Klartext ab), im Entwurf
   optional — dann kann ein Aussendokument nie ohne Empfaenger entstehen.
   *Eingearbeitet (Antwort 9, 2026-10-09) — Anforderung 6, AK 6b; Pruefung im Teil-3-Workflow
   (Teil-3-Anforderung 5, AK 5a).*

*Stand 2026-10-12: alle Rueckfragen dieses Teils beantwortet und eingearbeitet; die Teil-3-Rueckfragen 14 und 15
sind beantwortet (2026-10-12) und hier eingearbeitet (Anforderungen 4a, 5; AK 5c, 6c, 6d). Keine offene
Abhaengigkeit.* ~~*Stand 2026-10-09: alle Rueckfragen dieses Teils beantwortet und eingearbeitet. Abhaengig von
den offenen Teil-3-Rueckfragen 14 und 15 (siehe Einarbeitungs-Hinweis oben).*~~ *(ueberholt 2026-10-12)*

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
-->

1. Doppelvergabe-Darstellung → **Nicht erneut wählbar außer über Reklamation (Keyuser 2026-10-07). Darstellung: markieren mit Auftragsnummer (Vorschlag Koordination).**
2. Auswahl-Einheit / Seite → **Vorschlagsliste automatisch übernehmen (alle Positionen des HauptFA), danach im Entwurf editierbar (Keyuser 2026-10-07) – keine Einzelauswahl per Häkchen.**
3. Reklamation: Ablauf und editierbare Details → **Teile auswählen, nichts vorbelegen; Vermerk am Ursprungsauftrag (Keyuser 2026-10-07).**
4. Kreditor: Pflicht / Vorbelegung / Sage-Text → **Standard-Beschichter aus der PPS-View (`FaHierarchyOrderInfo.Dienstleister`), am Auftrag überschreibbar – besonders bei Reklamation (Keyuser 2026-10-07). Offen: Überschreiben als Freitext oder Auswahl aus dem Kreditoren-Stamm (der ist zurückgestellt).** **Nachtrag (Gerald 2026-10-07): Überschreiben als Freitext genügt; Teil 4 hängt damit nicht am Kreditoren-Stamm (depends_on auf die Kreditoren-Spec entfällt, Auswahl aus dem Stamm später).**
5. Briefkopf-Pflicht und Empfaengeradresse → **Wie vorgeschlagen (Gerald 2026-10-07): Briefkopf aus Teil 2 Pflicht (depends_on Teil 2), Empfänger = Beschichter des Auftrags.**
6. Positionsdarstellung im Druck / Barcode → **Wie vorgeschlagen (Gerald 2026-10-07): summiert nach dem Teil-1-Schlüssel, Maße angedruckt, Barcode = Auftragsnummer.**
7. Bearbeitbarkeit je Status → **Kopf und Positionen nur im Entwurf; danach Status und Bemerkung änderbar (Gerald 2026-10-07).**
8. Drucken im Entwurf → **Nein – im Entwurf nicht druckbar (Gerald 2026-10-07) (abweichend vom Vorschlag). Gedruckt wird ab „Übergeben“.**
9. Dienstleister Pflicht beim Wechsel nach Uebergeben → **Ja (Gerald 2026-10-09): im Entwurf optional, Pflicht beim Wechsel nach `Uebergeben`; der Workflow lehnt ohne Dienstleister mit Klartext ab.** **Hinweis Koordination 2026-10-09:** Teil-3-Antworten 12 (Positionen im Entwurf wieder hinzufuegen ⇒ Positionsauswahl im Entwurf, nur freie Positionen desselben HauptFA) und 13 (Reklamations-Kopf 1:1 vom Ursprung, nicht aus PPS-View) betreffen die Oberflaeche dieses Teils.

## Umsetzung (2026-10-12, Dev-Lauf)

Direkt im Bündel `feature/2026-08-07-ideal-teile-1-5` (Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`), Start
`a5b27550` (v1.50.0). Plan `docs/superpowers/plans/2026-10-12-beschichtungsauftrag-ausbau-teil-4.md`, native Ausführung.
Aufgabe [[2026-10-12-beschichtungsauftrag-ausbau-teil-4-umsetzung]]. **Version 1.51.0, TS-84, keine Migration.**

| Commit | Inhalt |
|---|---|
| `3b5149af` | Schreibrolle `beschichtungsauftrag_schreiben`, `RequireBeschichtungsauftragWriteAccess`, Leseebene schliesst Schreibrolle ein; `Vergabe` um `HauptFA`, `GetVergabenAsync(hauptFas)` (eine Abfrage je Seite); `BeschichtungsauftragVergabeAnzeige`; `PdfFileNameBuilder.Build(art, kennung, ts)` |
| `8d60cb10` | `BeschichtungsauftraegeController` (Index, Details, Create, Bearbeiten, PositionenHinzufuegen, Bemerkung, Status, Reklamation GET/POST, Print, `Pdf/{id}`), ViewModels, `ColumnDefinitions.Beschichtungsauftraege` + `BeschichtungsauftragPositionen`, Repository `GetAlleAsync`/`GetDetailAsync`, Workflow `IstErlaubt`/`Anzeige` public, Vergabe in `FaHierarchyBeschichtungController` |
| `17bbf3be` | Views `Beschichtungsauftraege/{Index,Details,Reklamation,Print,_StatusBadge}`, Gruppenkopf-Partial `_VorschlagAuftraege` in Liste + Summiert, Badge „in BA-…" (Beschichtet-Zelle), Navigation, `PrintBriefkopfModel.FuerAuftrag` (Empfänger = Dienstleister des Auftrags, „Empfänger fehlt — am Auftrag nachtragen") |
| `5af265f4` | v1.51.0 (beide `AppVersion.cs`), Anwender-Changelog, Hilfe, `RoleOverview.cshtml`, TS-84 — **feature-complete** |
| `91c77105` | README: Schreibrolle beim Schalter `FaHierarchyBeschichtungAktiv` |

**Beweis (vor QA):** `dotnet build IdealAkeWms.slnx` → 0 Fehler, 12 Warnungen (alle vorbestehend: `FaWorklist/Index.cshtml`
CS8321, `TrackingController.cs` CS8602). `dotnet test IdealAkeWms.slnx` → Web **1619 bestanden, 1 übersprungen** (vorher
1568), Service **272 bestanden**. Neu: `BeschichtungsauftraegeControllerTests` (28, echter Teil-3-Workflow auf InMemory),
`BeschichtungsauftragVergabeAnzeigeTests` (6), `BeschichtungsauftraegeViewTests` (8), `RequireBeschichtungsauftragWriteAccessFilterTests` (2),
Rollen-Theory in `CurrentUserServiceRoleTests` (4), Vergabe-Theory in `FaHierarchyBeschichtungControllerTests` (2),
`PdfFileNameBuilderTests` (+1), DI-Test um Anzeige erweitert. `docs/TESTSZENARIEN.md` Index-Blob LF (CRLF-Zählung 0).

**Teil-3-Hinweise umgesetzt:** je Request genau EIN schreibender Workflow-Aufruf, danach `RedirectToAction`; RowVersion
als Base64-Hidden-Feld in jedem Schreibformular, Konflikt → Klartext-`WarningMessage`; TS-83.6 als Test
(`Status_VeralteteRowVersion_Konfliktmeldung_StatusBleibt`, `DieselbeFreiePosition_InZweiEntwuerfe_ZweiterErhaeltWarnung`)
und Manual-UAT TS-84.13.

### Entscheidungen im Dev-Lauf (Rulings)

- **R1 Rolle:** Key `beschichtungsauftrag_schreiben`, Anzeige „Beschichtungsaufträge (Schreibrecht)". Die Schreibrolle
  erfüllt auch die Leseebene (eine Zuweisung genügt). Kein Seed — Deploy-Schritt: Rolle unter `/Roles` anlegen + zuweisen.
- **R2 Vergabe-Markierung** liest die Knoten über den gecachten Decorator (bis 5 min alt); maßgeblich ist der ungecachte
  Workflow beim Anlegen, der bei veraltetem Cache mit Klartext ablehnt. Kosten bei falsch: Knopf erscheint kurz zu lange.
- **R3 Badge „in BA-…"** in der Beschichtet-Zelle (immer „Ja"): Server-Filter liest die ColumnMap, nicht den Zelltext;
  Client-Sortierung gruppiert vergebene Zeilen — gewollt.
- **R4 Nur eine filterbare Tabelle je Seite:** `table-filter.js`/`column-preferences.js` halten genau eine Tabelle (neu
  [[fallstricke]] §22). Die Positionstabelle im Detail hat Client-Filter + Zahnrad; Positionsauswahl und
  Reklamations-Teileauswahl sind bewusst ungefilterte Auswahltabellen (klein, auf einen HauptFA/Auftrag begrenzt).
  Abweichung vom Lösungsentwurf („Auswahl-Tabelle Client-Mode").
- **R5 Mengen** werden als Text gebunden und kulturunabhängig gelesen (Komma oder Punkt), weil die Anwendung keine
  Request-Kultur setzt ([[fallstricke]] §22). Ungültige Menge oder ungültiges Datum → nichts gespeichert, Warnung.
- **R6 Druckspalten** folgen der Zahnrad-Auswahl der Detail-Positionstabelle (`BeschichtungsauftragPositionen`); der
  summierte Druck kennt dazu „Positionen" (immer gedruckt), druckt die Herkunftsspalte nicht. `artnr` und `matchcode`
  sind gesperrt (Matchcode trägt „Maße uneinheitlich").
- **R7 Druck** ab `Übergeben` heißt auch in `Erledigt`; `Storniert` → Warnung „… ist storniert und nicht druckbar".
- **R8 Statushistorie** aus den Statusfeldern, Benutzername über `IUserRepository.GetAllWithRolesAsync` (nur geladen,
  wenn eine Status-UserId gesetzt ist); unbekannte Id → „Benutzer #n".
- **R9 Storno** mit Browser-Rückfrage (`confirm`, wie `ArticleAttributes`), weil Storno nicht rückgängig zu machen ist.
- **R10 Leere RowVersion** („") wird angenommen (nur InMemory kennt leere RowVersions; auf SQL Server meldet der
  Konfliktcheck); fehlendes Feld oder kaputtes Base64 → „Stand nicht mehr gültig — bitte neu laden".

### Zusatzfunde (nicht mitgebaut)

- **Z1** Die Einzelliste `FaHierarchyBeschichtung/Index.cshtml` lädt `column-preferences.js`, hat aber keinen Zahnrad-Host
  vor `.table-responsive` ([[fallstricke]] §19; nur Summiert hat Host + Wächter). Vorbestehend seit v1.48.0.
- **Z2** Bestehende Formulare mit `asp-for` auf `decimal` (`StockMovements/*`, `Articles/*`, `StorageLocations/*`,
  `BdeBookings/Edit`, `FaCompletion/Edit`) hängen an der Server-Kultur — auf dem IIS mit `de-AT` würde „2.5" als 25 gelesen.
  Nicht geprüft, nicht geändert; Prüfabfrage für den Menschen: Kultur des App-Pool-Kontos auf dem Produktivserver.

## QA (2026-10-12)

Unabhaengiger QA-Lauf (qa-agent) im Buendel-Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, Zweig
`feature/2026-08-07-ideal-teile-1-5`, Diff `a5b27550..91c77105` (5 Commits, 44 Dateien, +2385/-21), Worktree sauber.
Web-App nicht gestartet, keine DB, kein Push/Merge. **Urteil: Testbereit** — kein Critical, kein Important; sechs Minor
(M1-M6), keiner blockierend.

### 1. Build und Tests (im QA-Lauf selbst ausgefuehrt)

- `dotnet build IdealAkeWms.slnx` → **0 Fehler**, 8 Warnungen (inkrementeller Build; vorbestehend, u. a. NU1902 MimeKit
  im Service — keine aus dem Teil-4-Diff).
- `dotnet test IdealAkeWms.slnx --no-build`:
  - `IdealAkeWms.Tests` → **1619 bestanden, 1 uebersprungen, 0 Fehler** (gesamt 1620)
  - `IDEALAKEWMSService.Tests` → **272 bestanden, 0 uebersprungen, 0 Fehler**
- `docs/TESTSZENARIEN.md`: CRLF-Zaehlung im Commit-Blob `HEAD` = 0, im Index-Blob = 0 (LF).

### 2. Code-Review (selbst, in diesem Lauf)

Geprueft: `BeschichtungsauftraegeController.cs` vollstaendig, `BeschichtungsauftraegeViewModels.cs`,
`BeschichtungsauftragVergabeAnzeige.cs`, Repository-/Interface-Diff, `RequireBeschichtungsauftragWriteAccessAttribute.cs`,
`CurrentUserService`/`ICurrentUserService`/`RoleKeys`, `PdfFileNameBuilder`, `PrintBriefkopfModel`, Workflow-Diff sowie
`AnlegenAsync`/`BemerkungAendernAsync`/`EntwurfBearbeitenAsync`-Validierung, alle neuen Views (`Index`, `Details`,
`Reklamation`, `Print`, `_StatusBadge`, `_VorschlagAuftraege`), Diffs `FaHierarchyBeschichtung/{Index,Summiert}.cshtml`,
`_Layout`, `_PrintBriefkopf`, `RoleOverview`, `ColumnDefinitions`, `column-preferences.js` (Umgang mit `<th>` ohne
`data-col-key`), `CachedFaHierarchyNodeRepository`.

Pruefpunkte des Auftrags:
1. **Ein Workflow-Schreibaufruf je Request + Redirect:** erfuellt. `Create`, `Bearbeiten`, `PositionenHinzufuegen`,
   `Bemerkung`, `Status`, `Reklamation` (POST) rufen genau eine schreibende Workflow-Methode und enden in `RedirectToAction`
   (`BeschichtungsauftraegeController.cs:144-154, 177-179, 195-196, 205, 214-215, 235-239`). `Details` ruft nur den
   lesenden `GetHinzufuegbarePositionenAsync` (`:106-108`).
2. **Read/Edit-Split:** Class-Level `[RequireBeschichtungsauftragAccess]` + `[RequireFaHierarchyBeschichtungAktiv]` (`:28-29`);
   alle sechs POSTs + Reklamation-GET mit `[RequireBeschichtungsauftragWriteAccess]`, alle POSTs `[ValidateAntiForgeryToken]`
   — per Reflection-Waechter `JedeSchreibendeAction_HatSchreibfilter_UndPostsAntiforgery` abgesichert. Schreib-Knoepfe in
   `Details.cshtml` nur bei `DarfSchreiben` (`:13, 35, 97, 189`), Gruppenkopf-Button nur bei `DarfSchreiben &&
   FreiePositionen > 0` (`_VorschlagAuftraege.cshtml:19`). Drei Pflichtstellen vorhanden: Attribut + `RoleKeys.cs`,
   `secondbrain/codebase/controller.md:76-77, 108, 118-121` (Hauptcheckout), `RoleOverview.cshtml` (+13 Zeilen).
3. **RowVersion:** Hidden-Feld `rowVersion` in `entwurf-form` (`Details.cshtml:126-128`, Eingaben ueber `form="entwurf-form"`,
   keine verschachtelten Formulare), Status (`:104`), Bemerkung (`:192`), Positionsauswahl (`:296`). Fehlend/kaputt →
   „Stand nicht mehr gueltig" (`Controller:303-316`), Konflikt aus dem Workflow → WarningMessage (Test
   `Status_VeralteteRowVersion_Konfliktmeldung_StatusBleibt`). Reklamation-POST traegt bewusst keine RowVersion (legt
   neuen Auftrag an, aendert den Ursprung nicht).
4. **ADR 0005 Liste:** Pagination (`PageSize.Resolve` + `PaginationState` + `_Pagination`), Filterkarte (Status),
   `data-server-column-filter="true"`, alle 12 `<th>` mit `data-col-key`, `ColumnFilterHelper.ReadFromQuery/Apply` auf der
   Projektion (Datumsspalten als gerenderter Text danach), `ColumnDefinitions.Beschichtungsauftraege` + `GetByViewKey`-
   Switch + `#column-config` identisch (Drift-Test `Liste_VierQuellen_Gleich`), Zahnrad-Host direkt vor
   `.table-responsive` (§19, Test `ZahnradHost_DirektVorTableResponsive`). Detail-Positionstabelle Client-Mode mit
   `data-col-key`; die Spalte „Entfernen" ohne Key sortiert `column-preferences.js:201-211` ans Ende (ok).
5. **Druck/PDF:** nur `Uebergeben`/`Erledigt` (`BeschichtungsauftraegeDruck.IstDruckbar`, serverseitig in
   `PrintModelAsync`, `Controller:271-274`), Briefkopf ueber `PrintBriefkopfModel.FuerAuftrag` (Empfaenger = Dienstleister,
   leer → sichtbarer Text „Empfaenger fehlt — am Auftrag nachtragen"), Summierung ueber
   `BeschichtungsauftragService.AggregateSummiert` (keine zweite Aggregation), Barcode `EncodeCode128Base64(a.Nummer)`,
   Dateiname `Beschichtungsauftrag_BA-JJJJ-NNNNN_yyyyMMdd-HHmm.pdf` (ASCII), `PdfRenderException` → WarningMessage +
   Detail (`:261-264`). PDF-Link ohne `target="_blank"`.
6. **Vorschlagsliste:** Partial in beiden Gruppenkoepfen, Badge „in BA-…" in der Beschichtet-Zelle (Einzelliste);
   `BeschichtungsauftragVergabeAnzeige` = EINE Vergabe-Abfrage (`GetVergabenAsync(IReadOnlyCollection<int>)`, `Contains`)
   + der ohnehin von der Liste genutzte gecachte `GetAllAsync` — kein N+1. R2 im Code bestaetigt: der Workflow liest Knoten
   ungecacht (`BeschichtungsauftragWorkflow.cs:61`, konkreter Typ `FaHierarchyNodeRepository`, nicht der Decorator).
   Bestehende Liste/Summiert/Druck: nur additive Aenderungen, `FaHierarchyBeschichtungControllerTests` gruen.
7. **Parsen:** `TryMenge` invariant mit Komma→Punkt (`:318-319`), Workflow lehnt `<= 0` ab (`Workflow.cs:173-174`);
   Datum ueber `type="date"` (ISO) + `ModelState.IsValid`-Guard (`:165-166`); ungueltiger Positionsschluessel → Warnung
   ohne Workflow-Aufruf (`:190-193`, Theory mit drei Faellen).

Abgeklopfte Fehlerklassen: Werte in `<script>`-Bloecken — nur statisches JSON (`#view-config`, `#column-config`), keine
Modellwerte; `confirm('@a.Nummer …')` (`Details.cshtml:108`) ist ein Attribut mit systemvergebener ASCII-Nummer, von
Razor HTML-kodiert — kein Befund. `hidden` + `d-*` — nicht vorhanden. Stille Fallbacks — keine `?? 0`; `GetValueOrDefault
?? Leer` in den Gruppenkoepfen greift nie, weil `ErmittelnAsync` fuer jeden HauptFA der Seite einen Eintrag liefert.
Positionsbindung paralleler Arrays — keine: `mengen[<PositionsId>]` (Dictionary nach Id), `entfernen`/`positionIds` =
Ids, `schluessel` selbsttragend. Browser-only-Validierung — `maxlength` 200/1000 serverseitig im Workflow
(`MaxKopfTextLaenge`, `MaxBemerkungLaenge`) erzwungen, Menge > 0 ebenso. Eine Regel an mehreren Stellen — siehe M6.

**Befunde**

| # | Schwere | Fundstelle | Befund | Umgang |
|---|---|---|---|---|
| M1 | Minor | `BeschichtungsauftraegeController.cs:165-166` | Jeder Bindungsfehler (auch manipulierter `entfernen`-Wert oder `mengen`-Schluessel) erzeugt die Meldung „ein Datum ist ungueltig" — irrefuehrend, es wird aber nichts gespeichert. | offen, nicht blockierend (nur manipulierte POSTs) |
| M2 | Minor | `BeschichtungsauftraegeController.cs:318-319` | `TryMenge` liest „1.000" (deutsche Tausendergruppe) als 1. Die Anzeige rendert ohne Gruppierung (`0.###`); betroffen ist nur Handeingabe ≥ 1000 mit Tausenderpunkt. | offen; Grenze bekannt |
| M3 | Minor | `BeschichtungsauftragRepository.cs:55-62`, `Controller:80-84` | Liste laedt alle Auftraege inkl. aller Positionen je Request (In-Memory-Filter + Paging). Bei heutiger Menge unkritisch, waechst linear. | offen; spaeter Anzahl per Projektion statt `Include(Positionen)` |
| M4 | Minor | `Details.cshtml:228` | Leer-Zeile `colspan="9"`, ohne Bearbeitungsrecht hat die Tabelle 8 Spalten (kosmetisch). | offen |
| M5 | Minor (Brain) | `secondbrain/tests/testszenarien-index.md` (Hauptcheckout) | TS-84 fehlt im Testindex (letzte Zeile 83); ebenso fehlen Brain-Changelog `2026-10-12-v1-51-0-*.md` und die `feature-map`-Zeile. | an den Aufrufer: im Brain-Commit nachziehen (QA hat die Datei nicht angefasst — sie traegt fremde ungestagte Aenderungen) |
| M6 | Minor | `BeschichtungsauftragVergabeAnzeige.cs:50` vs. `BeschichtungsauftragWorkflow.cs:92-102` und `:221-222` | Regel „frei = beschichtet und nicht in sperrendem Auftrag" dreifach ausgeschrieben (Anzeige gecacht, Workflow ungecacht). Abgesichert durch `BeschichtungsauftragVergabeAnzeigeTests`; eine kuenftige Regelaenderung im Workflow faellt in der Anzeige nicht automatisch auf. | akzeptiert (R2), Hinweis fuer kuenftige Aenderungen |

**Rulings R1-R10 gegen den Spec-Rumpf:** R1 (Rolle, Schreibrolle schliesst Lesen ein, kein Seed) deckt Anforderung 8 und
die Migrationsaussage; R2 im Code bestaetigt (s. o.); R3 entspricht Anforderung 1 („bestehende Zelle"); **R4 weicht vom
Loesungsentwurf ab** (Auswahl-/Reklamationstabellen ohne Client-Filter statt Client-Mode) — technisch begruendet (eine
filterbare Tabelle je Seite, [[fallstricke]] §22), klein und vorgefiltert; vom Menschen bei Schranke 2 zur Kenntnis zu
nehmen. R5 deckt Pruefpunkt 7; R6 ist eine Erweiterung ohne Widerspruch (AK 7 bleibt erfuellt, `artnr`/`matchcode`
gesperrt); R7 = „ab Uebergeben" (Antwort 8); R8/R9/R10 ohne Widerspruch zum Rumpf. Z1/Z2 sind vorbestehend und korrekt
als Zusatzfunde gefuehrt (Z2: Pruefabfrage Kultur des App-Pool-Kontos bleibt fuer den Menschen offen).

### 3. Beweisart je Akzeptanzkriterium

| AK | Automatisiert | Manual-UAT (zaehlt nicht als gruen) |
|---|---|---|
| 1 | `Create_Erfolg_RedirectDetail_MitNummer`, `Summiert_Und_Index_TragenVergabeUndSchreibrecht` | Button-Rendering in beiden Sichten, keine Checkboxen — TS-84.1 |
| 2 | `Create_AllesVergeben_WarnungZurueckZurVorschlagsliste`, `VollVergeben_KeineFreien_NummerJePosition_…` | Link statt Button, Badge „in BA-…" — TS-84.1, 84.3 |
| 2a | `TeilweiseVergeben_FreieZaehlenWieder`, `Storniert_GibtFrei`, Teil-3-Workflowtests | Nummer + Button, Hinweis im Detail — TS-84.2 |
| 3 | `Index_StatusFilter_Spaltenfilter_Pagination`, `Liste_VierQuellen_Gleich`, `ZahnradHost_…` | Spaltenfilter-UI/JS im Browser — TS-84.15 |
| 4 | — (Uebergaenge aus Teil 3 getestet) | Status-Knoepfe + Verlauf mit Zeit/Benutzer — TS-84.7 |
| 5 / 5a | `Reklamation_Erledigt_LegtFolgeauftragAn_UndLeereAuswahlAbgelehnt`, `Reklamation_NichtErledigt_GetUndPost_Warnung_KeinAuftrag` | Auswahl ohne Haken, Verweise beidseitig, Liste — TS-84.9 |
| 5b | Teil-3-Workflowtests (Kopf vom Ursprung) | TS-84.9 |
| 5c | `Reklamation_ZaehltMit_PositionTraegtBeideNummern` + Teil-3-Tests | TS-84.11 |
| 6 | `Details_Entwurf_ZeigtKandidatenNurMitSchreibrecht_UndSageAbweichung`, `Bearbeiten_LeereDaten_Null_MengeMitKommaOderPunkt` | Sage-Wert daneben, Vorbelegung aus PPS-View (Sage-Read) — TS-84.4 |
| 6a | `Bearbeiten_NachUebergeben_Abgelehnt` | keine Eingabefelder nach Uebergeben — TS-84.7 |
| 6b | `Uebergeben_OhneDienstleister_Warnung_StatusBleibtEntwurf` | TS-84.6 |
| 6c | `DieselbeFreiePosition_InZweiEntwuerfe_ZweiterErhaeltWarnung`, `PositionenHinzufuegen_LeereAuswahl_Warnung`, `…_UngueltigerSchluessel_Warnung` | echte Model-Bindung `schluessel`/`mengen[id]`, Leertext — TS-84.5 |
| 6d | Teil-3-Workflowtests (Anforderung 10a) | Cache-Fall, Ueberschrift mit Link — TS-84.10 |
| 7 | `Print_Uebergeben_SummiertJeArtnr_MasseUneinheitlich_BarcodeNummer`, `Pdf_Erfolg_DateinameMitNummer`, `Pdf_RenderFehler_RedirectDetail`, `Druck_BriefkopfAlsErstesElement_…` | echtes PDF (Edge), Briefkopf-Bild — TS-84.8 |
| 7a | `Print_Entwurf_Warnung_RedirectDetail` (Pdf teilt `PrintModelAsync`) | Knopf fehlt — TS-84.7 |
| 8 | `RequireBeschichtungsauftragWriteAccessFilterTests`, `Beschichtungsauftrag_LesenUndSchreiben`, Reflection-Waechter | Knoepfe fuer Leser unsichtbar, AccessDenied im Browser — TS-84.14 |
| 9 | Class-Attribut per Reflection | Navigation/Schalter — TS-84.14 |
| 10 | `Status_VeralteteRowVersion_Konfliktmeldung_StatusBleibt` (InMemory) | **echte SQL-RowVersion nur Manual** — TS-84.13 |
| 11 | Dokumentpruefung im QA-Lauf (drei Stellen vorhanden) | — |
| 12 | `FaHierarchyBeschichtungControllerTests` gesamt gruen | Liste/Summiert/Druck/PDF unveraendert — TS-84.16 |

### 4. Geaenderte Bestandstests

| Test | Aenderung | Einordnung |
|---|---|---|
| `FaHierarchyBeschichtungControllerTests` (Fixture) | neuer Ctor-Parameter `BeschichtungsauftragVergabeAnzeige` mit leerem Vergabe-Mock; bestehende Asserts unveraendert; neue Theory dazu | Typ-/Compile-Anpassung (legitim) |
| `KommissionierSpaltenKonsistenzTests` | drei Helfer `private` → `internal` (Wiederverwendung in `BeschichtungsauftraegeViewTests`) | Sichtbarkeit, kein Verhalten (legitim) |
| `BeschichtungsauftragDiResolutionTests` | +4 Zeilen: Aufloesung von `BeschichtungsauftragVergabeAnzeige` | Erweiterung (legitim) |
| `FakeCurrentUserService` | neue Interface-Methode `HasBeschichtungsauftragWriteAccessAsync` | Compile-Fix (legitim) |
| `CurrentUserServiceRoleTests`, `PdfFileNameBuilderTests` | nur neue Tests angehaengt | Erweiterung |

Kein „Other": keine Fixture wurde angepasst, um einen Test gruen zu bekommen.

### 5. CLAUDE.md-Checkliste

- Migration / `SQL/` / FreshInstall / AgentJobs: entfaellt (kein Diff; „keine Migration" bestaetigt).
- Audit-Felder: laufen ueber den Teil-3-Workflow, der Controller schreibt keine Entitaet selbst.
- Version 1.51.0 in **beiden** `AppVersion.cs` ✔, Anwender-Changelog ✔, Hilfe ✔, README ✔.
- `docs/TESTSZENARIEN.md` TS-84.1–84.16 ✔ (LF).
- Testindex / Brain-Changelog / feature-map: **offen → M5** (Aufrufer).
- Neue Rolle an drei Stellen ✔.
- Unabhaengiges Review: dieser Abschnitt.

### Manuelle Testcheckliste (Mensch, Testsystem IDEAL)

Vorbereitung: Web aus dem Worktree publishen (Deploy-Abschnitt), Rolle `beschichtungsauftrag_schreiben` unter `/Roles`
anlegen; Benutzer **S** (Schreibrolle) und **L** (nur `beschichtungsauftrag`); Schalter `FaHierarchyBeschichtungAktiv` an.

1. TS-84.1 — S: Summiert-Sicht → „Aus Vorschlagsliste erstellen" → Detail mit allen Positionen, gruene Meldung mit Nummer;
   zurueck: Gruppenkopf zeigt Nummer als Link, Einzelliste Badge „in BA-…"; keine Checkboxen.
2. TS-84.2 — Position entfernen, speichern → Gruppenkopf Nummer **und** Button → zweiter Auftrag nur mit der freien Position.
3. TS-84.3 — alter Tab, Knopf erneut → gelbe Sammelmeldung, kein Auftrag.
4. TS-84.4 — Kopf aendern (Dienstleister, RAL, Datum leeren), Menge **„2,5"** und **„2.5"** eingeben → beide als 2,5
   gespeichert; Sage-Wert erscheint neben abweichendem Dienstleister.
5. TS-84.5 — Positionen hinzufuegen (Erst-Entwurf); leere Auswahl → Warnung; zwei Browser dieselbe Position → der zweite
   erhaelt eine Warnung.
6. TS-84.6 — Dienstleister leeren → Hinweis; Uebergeben → Warnung, Status bleibt Entwurf; eintragen → Uebergeben gelingt.
7. TS-84.7 — nach Uebergeben nur Bemerkung editierbar, Verlauf mit Zeit/Benutzer; im Entwurf kein Druck-Knopf,
   `/Beschichtungsauftraege/Print/<id>` bzw. `/Beschichtungsauftraege/Pdf/<id>` direkt → Warnung.
8. TS-84.8 — Druck + PDF: Briefkopf, Empfaenger = Dienstleister, summiert je Artnr, Masse, „Masse uneinheitlich", Barcode
   scannt die Nummer, Dateiname `Beschichtungsauftrag_BA-…_….pdf`; Spalten per Zahnrad ausblenden → Druck folgt.
9. TS-84.9 — Erledigt → Reklamation (nichts vorausgewaehlt), leer absenden → Warnung; zwei Teile → Folgeauftrag mit dem
   Kopf des Ursprungs (vorher abweichenden Dienstleister am Ursprung setzen), Verweise beidseitig + Listenspalte.
10. TS-84.10 — Reklamations-Entwurf: Bereich „Positionen des Ursprungs BA-… hinzufuegen" zeigt nur fehlende Ursprungspositionen.
11. TS-84.11 — zweite Reklamation derselben Position und Ketten-Reklamation; Gruppenkopf nennt alle Nummern.
12. TS-84.12 — Storno mit Rueckfrage; Positionen wieder frei.
13. TS-84.13 — **Concurrency auf SQL Server**: zwei Browser, derselbe Entwurf → der zweite erhaelt die Konfliktmeldung (nur
    hier beweisbar, InMemory deckt die echte RowVersion nicht ab).
14. TS-84.14 — L: Liste/Detail/Druck/PDF ja, keine Schreib-Knoepfe (auch nicht in der Vorschlagsliste); direkter POST →
    AccessDenied; Schalter aus → alle Seiten gesperrt, Menuepunkt weg.
15. TS-84.15 — Liste: Statusfilter, Spaltenfilter jeder Spalte inkl. Datum, Pagination, Zahnrad.
16. TS-84.16 — Regression Vorschlagsliste Liste/Summiert inkl. Druck/PDF (TS-60/74/81/82).
17. Z2-Pruefabfrage: Kultur des App-Pool-Kontos am Produktivserver feststellen (betrifft bestehende Formulare, nicht Teil 4).

### QA-Nachlauf (2026-10-12, Dev-Lauf)

- **M1 behoben** (`5764c664`): Bindungsfehler im Entwurf-Formular nennt das betroffene Feld statt pauschal „Datum".
- **M4 behoben** (`5764c664`): Leerzeile der Positionstabelle `colspan` 9 bzw. 8 je nach Bearbeitbarkeit.
- **M2, M3, M6 akzeptiert** wie von QA eingestuft (Tausenderpunkt-Handeingabe, Liste lädt alle Aufträge — Upgrade auf DB-Paging, wenn die Zahl spürbar wächst; „frei"-Regel an drei Stellen, durch Tests gesichert). **M5** mit dem Brain-Commit erledigt.
- Nach den Fixes: `dotnet build IdealAkeWms.slnx` 0 Fehler; `dotnet test IdealAkeWms.slnx` Web 1619 bestanden / 1 übersprungen, Service 272 bestanden.
