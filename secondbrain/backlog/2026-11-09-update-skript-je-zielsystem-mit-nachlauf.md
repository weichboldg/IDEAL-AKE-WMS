---
typ: feature
created: 2026-11-09
quelle: "Gerald 2026-11-09 (Chat Koordination)"
---
# Update-Skript je Zielsystem mit Nachlauf-Liste

Wunsch Gerald 2026-11-09: ein Skript für den Update-Prozess. Es enthält die Parameter für AKE-Prod, AKE-Test, IDEAL-Prod und
IDEAL-Test (Installpfade). Nach dem Update gibt es alle nachgelagerten Skripte und Schritte aus.

## Ausgangslage (belegt 2026-11-09, Lesen im Repo)

- Es gibt kein Update-Skript für die Zielsysteme. Vorhanden sind:
  - `D:\Dev\scripts\dev-deploy.ps1` (nur DEV, außerhalb des Repos; [[2026-10-29-dev-umgebung-lokal]]).
  - `Install-IdealAkeWms-IDEAL.ps1`, die Neuinstallation ([[2026-11-04-ideal-produktiv-installation]]), nicht im Repo.
  - README „Update (neue Version einspielen)“: nur Dienst, Pfad `C:\Services\IDEALAKEWMSService`.
- Die README-Update-Tabelle endet bei 80 (plus 95 und 97). 81–94, 96 und 98–141 fehlen.
- Web-Start ruft `db.Database.Migrate()` (`IdealAkeWms/Program.cs:202`). Laut [[fallstricke]] §8 werden die Bündel-Migrationen trotzdem per
  SQL-Skript plus History-Insert eingespielt und nicht per `Migrate()`. Der Dienst migriert nicht.
- Diese Skripte haben **keinen** History-Eintrag, `Migrate()` erledigt sie also nie:
  - `SQL/108`: Daten, idempotent.
  - `SQL/110`: Backfill, Trockenlauf und dann scharf.
  - alle `SQL/Einmalig/*`: nie zweimal (ADR 0015).
- Ob ein Einmal-Skript auf einem System lief, weiß nur das Deploy-Protokoll. ADR 0015 hat einen Marker bewusst abgelehnt, mit dem Zusatz
  „bei häufigerem Bedarf neu entscheiden“. Mittlerweile gibt es 4 Einmal-Skripte und 2 Daten-Skripte, die Trockenlauf oder Löschen brauchen.

## Was beim Bündel-Deploy (bis v1.95.0) nachzuführen ist

Stand je System laut Brain, **nicht** an der DB geprüft:

| System | Stand laut Brain | Letzte Migration |
|---|---|---|
| AKE-Prod | main, v1.30.3 nicht bestätigt | SQL/88, `20260806120650_AllowMultipleDummyRequisitionItems` |
| AKE-Test (akenet01) | v1.30.1 (20.10.) | wie AKE-Prod |
| IDEAL-Prod | v1.71.1 (04.11.) | SQL/97, `20261019092105_…` |
| IDEAL-Test (idealweb01:88) | ≥ v1.67 | unbekannt |

**Vor dem Deploy (beide Standorte):**
- DB-Backup.
- AKE: auf AKESQL20 per `sysjobs` prüfen, dass kein Agent-Job in `ProductionOrders` schreibt (v1.32.0, SQL/90).
- Edge auf dem Web-Server (PDF, v1.39.0).
- `appsettings.Production.json` im Web-Ordner sichern (v1.71.0).
- Ordner `Branding\` im Programmordner (v1.49.0/v1.59.0).

**Schema, in dieser Reihenfolge (die Deploy-Liste gilt, nicht die Dateisortierung, ADR 0004):**
- 89–97, nur AKE:
  - 90: daten-konvertierend, vorher den Dienst stoppen.
  - 95: App-Pool vorher stoppen, das Skript vollständig ausführen.
- 98–107, beide Standorte.
- 109, 112, 113, 133, 140, 141, beide Standorte.

**Daten, beide Standorte:**
- `SQL/108`: vorher P-4 notieren.
- `SQL/110`: Trockenlauf, dann scharf, dann ein zweiter Lauf, der 0 liefern muss.
- `SQL/Einmalig/2026-11-08_v1.88.0_Kommissionierauftraege_Altbestand.sql`:
  1. Web gestoppt; SQL/133 und SQL/141 vorher.
  2. Trockenlauf, Gerald gibt die Zählung frei.
  3. Scharf ausführen, im Wartungsfenster.

Nur IDEAL, und nur wenn auf dem System noch nicht gelaufen: `2026-10-07_v1.47.4_…` (nach dem Dienst-Deploy) und
`2026-10-15_…Bereinigung00erArtikel` (entfällt an IDEAL-Prod, [[2026-11-04-ideal-produktiv-installation]]).

**Nach dem Start:**
- Web zuerst, dann Dienst. Der Web-Start legt die neuen Setting-Keys an.
- Rolle `beschichtungsauftrag_schreiben` anlegen und zuweisen, nur AKE (v1.51.0). IDEAL hat sie aus Teil 2 der Installation.
- `BdeTerminalMaxHauptFa = 5` (v1.64.0).
- Einstellungsmasken je Standort einmal speichern (v1.79.0 F4). Aktivitäts-Protokoll „Einstellungsprüfung“ (v1.77.0).
- Leitstand:
  1. „Fehlende Kommissionieraufträge erzeugen“.
  2. Liste „Altbestand prüfen“ abarbeiten (v1.89.0).
- SData-Schalter je Standort:
  1. Kreditoren, Debitoren, Artikel-Lieferant.
  2. `Sync:EinkaufBestellungenEnabled`, dann P-W6.
  3. Erst dann `WareneingangBestellungAktiv` (v1.84.0).
- Meldebestand: die erste Mail ist lang (v1.93.0).
- Anwender-Info:
  - Suchsyntax `;` (v1.58.1).
  - Fortsetzen an fremder Werkbank (v1.63.0).
  - Beschichtung im Menü (v1.56.0).

## Zielsysteme (Gerald 2026-11-09, Screenshots idealweb01)

Je Firma ein Server mit Prod und Test nebeneinander. AKE ist gleich aufgebaut, nur heißt der Ordner `AKE` statt `IDEAL`.
IIS ist an beiden Servern gleich.

| Firma / System | Dienst (Anzeigename) | Dienst-Ordner | Web-Ordner (IIS) |
|---|---|---|---|
| IDEAL Prod | `IDEAL AKE WMS Service` | `C:\IDEAL\IdealAkeWmsService` | `C:\inetpub\wwwroot\IDEALAKEWMS` |
| IDEAL Test | `IDEAL AKE WMS Service TEST` | `C:\IDEAL\IdealAkeWmsService_Test` | `C:\inetpub\wwwroot\IDEALAKEWMS_Test` |
| AKE Prod | `IDEAL AKE WMS Service` | `C:\AKE\IdealAkeWmsService` | `C:\inetpub\wwwroot\IDEALAKEWMS` |
| AKE Test | `IDEAL AKE WMS Service TEST` | `C:\AKE\IdealAkeWmsService_Test` | `C:\inetpub\wwwroot\IDEALAKEWMS_Test` |

- Server: IDEAL `idealweb01`, AKE `akenet01`. Belegt ist nur die Testadresse; dass Prod auf demselben Server liegt, ist eine Annahme.
- App-Pool, Site und DB stehen nicht fest. Das Skript ermittelt sie selbst:
  - Site und App-Pool aus dem IIS, über den physischen Pfad.
  - DB aus `ConnectionStrings:DefaultConnection` in `appsettings.json` bzw. `appsettings.Production.json` des Zielordners.
- Die Dienste sind über den Anzeigenamen belegt, die internen Dienstnamen nicht. Das Skript sucht den Dienst über den Pfad der Exe.
- Bedienung: Das Skript **fragt Firma (AKE/IDEAL) und System(e) (Prod, Test oder beide)**. Parameter überspringen die Abfrage.
  Bei „beide“ gilt die Reihenfolge Test vor Prod, und vor Prod wird ausdrücklich nachgefragt.
- Inhalt: das Bündel (v1.95.0).

## Vorschlag Skript (Koordination)

1. Zieldatei `deploy/ziele.psd1` mit vier Einträgen: Server, SQL-Instanz, DB, Web-Pfad, Dienst-Pfad, Dienstname, App-Pool, Site-URL,
   DataProtection-Pfad, Branding-Pfad. Die Datei liegt nicht im Repo oder enthält keine Geheimnisse.
2. Manifest `deploy/nachlauf.psd1` je Version: Skript oder Schritt, Typ (Schema / Daten / Einmalig / Einstellung / Leitstand / Info),
   Standort, Voraussetzung, Ausführungsart (automatisch / nur Trockenlauf / nur anzeigen). Pflicht bei jeder Spec mit Deploy-Schritt.
3. `Update-IdealAkeWms.ps1 -Ziel AKE-PROD|AKE-TEST|IDEAL-PROD|IDEAL-TEST`:
   1. Prüft vorher, ob Version und `__EFMigrationsHistory` zum Ziel passen.
   2. Weist auf das Backup hin.
   3. Stoppt Dienst und App-Pool.
   4. Spielt die fehlenden Schema-Skripte der Reihe nach ein (Guard je Skript).
   5. Kopiert die Dateien ohne `appsettings*.json` und `Branding\`.
   6. Startet Web, dann Dienst, und prüft die Fußzeilen-Version.
   7. Gibt die **Nachlauf-Liste** des Ziels aus und schreibt das Protokoll.
4. Einmal- und Datenskripte führt das Skript **nie** scharf aus, höchstens im Trockenlauf (ADR 0015).
5. Marker-Tabelle für gelaufene Einmal-Skripte: das wäre ein Nachtrag zu ADR 0015. Entscheidung Gerald.

## Publish nach jedem Build (Gerald 2026-11-09)

- Nach jedem Build eines Laufs (/dev, /goal-run, QA-Reparatur) werden **beide** Apps in `<Worktree>\publish\` publiziert:
  - Web nach `publish\IDEALAKEWMSWeb`
  - Dienst nach `publish\IDEALAKEWMSWebService`
- Die Befehle stehen schon in `.claude/agents/qa-agent.md:99-100`, bisher gibt der Agent sie nur als Text an den Menschen aus.
- Der Ordner existiert im Bündel-Worktree (Stand 09.11. 12:02) und ist per `.gitignore:4` aus Git ausgeschlossen.
- Die Ordner vorher leeren, damit keine alten Dateien liegen bleiben. Erst nach einem grünen Build und grünen Tests publizieren; ein
  fehlgeschlagener Publish bricht den Lauf ab.
- Daneben eine `publish\publish-info.txt` mit Version, Commit, Zweig und Zeit. Das Update-Skript liest sie als Standardquelle, zeigt sie an
  und bricht ab, wenn Web und Dienst unterschiedliche Versionen oder Commits haben.
- **Erledigt 2026-11-09 (Koordination, Auftrag Gerald):** `.claude/agents/qa-agent.md` (main-Checkout) Punkt 7 „Publish to the worktree“ plus Verweis in „On success“. Die veraltete Kopie im Bündel-Worktree (Stand 05.10.) bleibt unverändert. Den Abschnitt „Publish nach jedem Build“ braucht die Spec damit nicht mehr; Commit durch Gerald.
- Gerald 2026-11-09: IDEAL nutzt Kommissionieraufträge noch nicht, gebraucht werden sie erst bei AKE. Bis dahin soll das Update-Skript fertig sein. An IDEAL trotzdem SQL/133 und SQL/141 einspielen (gemeinsames EF-Modell). Das Einmal-Skript Altbestand dort nur im Trockenlauf; bei 0 Treffern entfällt es, bei Treffern entscheidet Gerald.
