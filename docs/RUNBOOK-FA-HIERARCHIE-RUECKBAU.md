# Runbook — FA-Hierarchie-Rückbau (Einwegtor)

> **Status:** Rückweg aus dem hierarchischen Produktionsauftrags-Modus. Dieser Rückbau ist
> **nicht über die Anwendung umkehrbar** — er ist eine bewusste Datenbereinigung außerhalb der App.

## Wann dieses Runbook gilt

Der Master-Schalter `ProduktionsauftragHierarchisch` (IDEAL Teil 7) wurde aktiviert **und** es
liegen bereits Aufträge mit Sub-FA-Struktur vor — technisch: es existiert mindestens eine Zeile in
`ProductionOrders` mit `OrderNumber <> SubOrderNumber`. Sobald das der Fall ist, sperrt die
Anwendung den Schalter (Datentor, kein Schaltertor): weder die Umschalt-Seite noch die generische
Service-Einstellungen-Maske noch eine technische Anfrage können den Master noch deaktivieren.
Der einzige Rückweg ist die hier beschriebene Datenbereinigung.

## Warnung — was der Rückbau verwirft

Der Rückbau führt die materialisierten Sub-FAs auf den flachen Zustand zurück. Damit gehen
**alle an den Sub-FA-Ebenen hängenden Daten verloren**, insbesondere Rückmeldungen, Arbeitsgänge
und BDE-Buchungen auf Sub-FA-Ebene (Teil 8). Diese lassen sich nach dem Rückbau **nicht**
rekonstruieren. Der Schritt ist bewusst schwer gemacht, weil er Produktionsdaten vernichtet.

Formulierung durchgängig: **nicht über die Anwendung umkehrbar** — nicht „unmöglich".

## Vorbedingungen (alle zwingend)

1. **DB-Backup** der Datenbank `IDEAL_AKE_WMS` — vollständig, verifiziert wiederherstellbar.
2. **Windows-Service gestoppt** (`IDEALAKEWMSService`) — kein laufender Materialisierungs-Sync,
   der mitten in die Bereinigung schreibt.
3. **Keine aktive Kommissionierung/BDE** auf den betroffenen FAs; Wartungsfenster angekündigt.
4. **Server-seitige Agent-Job-Prüfung** (analog Deploy-Vorbedingung Teil 7): kein aktiver
   SQL-Agent-Job schreibt `ProductionOrders`.
   ```sql
   SELECT j.name, j.enabled, s.command
   FROM msdb.dbo.sysjobs j
   JOIN msdb.dbo.sysjobsteps s ON s.job_id = j.job_id
   WHERE s.command LIKE '%ProductionOrders%';
   ```

## Betroffene Tabellen

- `ProductionOrders` — die materialisierten Sub-Zeilen (`OrderNumber <> SubOrderNumber`) sowie die
  Spalten `SubOrderNumber`, `ParentSubOrderNumber`, `SageMissingSince` (Etappe A).
- Abhängige Rückmelde-/Arbeitsgang-/BDE-Tabellen, die per FK/Referenz an den Sub-FA-Zeilen hängen
  (Teil 8). Diese müssen **vor** dem Entfernen der Sub-Zeilen betrachtet werden (Reihenfolge unten).

## Bereinigungsschritt (im Wartungsfenster, nach Backup)

> Die Anwendung lässt den Master nicht mehr los; die Bereinigung läuft **direkt in der Datenbank**
> und umgeht damit bewusst den Guard. Reihenfolge einhalten.

1. **Abhängige Sub-FA-Daten sichten/entfernen** (Teil-8-Tabellen): erst die an
   `SubOrderNumber`-Zeilen hängenden Rückmeldungen/Arbeitsgänge/BDE-Buchungen bereinigen, sonst
   brechen FKs. (Konkrete Tabellenliste ergibt sich aus Teil 8; bis dahin manuell prüfen.)
2. **Sub-FA-Zeilen entfernen** — die materialisierten Kinder:
   ```sql
   DELETE FROM dbo.ProductionOrders WHERE OrderNumber <> SubOrderNumber;
   ```
3. **Flachen Zustand wiederherstellen** — verbleibende Zeilen tragen wieder die Invariante
   `SubOrderNumber = OrderNumber` (bei reinen HauptFA-Zeilen bereits erfüllt); Hilfsspalten leeren:
   ```sql
   UPDATE dbo.ProductionOrders
      SET SubOrderNumber      = OrderNumber,
          ParentSubOrderNumber = NULL,
          SageMissingSince     = NULL;
   ```
4. **Master deaktivieren** — jetzt greift die Sperre nicht mehr (kein `OrderNumber <> SubOrderNumber`):
   ```sql
   UPDATE dbo.ServiceSettings SET Value = 'false' WHERE [Key] = 'ProduktionsauftragHierarchisch';
   ```
   > Das Schema selbst (`SubOrderNumber` NOT NULL/UNIQUE, Etappe A) bleibt invertiert — das ist
   > inhärent und kein Fehler. Der flache Betrieb funktioniert damit unverändert
   > (`OrderNumber == SubOrderNumber`).

## Danach

- **Windows-Service neu starten.** Der gecachte Anzeige-Zustand (`HierarchischeStrukturStatus`) wird
  beim Start der Web-App neu befüllt; die Umschalt-Seite zeigt den Master wieder als frei änderbar.
- **Prüfen:** `SELECT COUNT(*) FROM dbo.ProductionOrders WHERE OrderNumber <> SubOrderNumber;` → 0.
- Vorgang im Betriebslog/Aktivitäts-Protokoll dokumentieren (wer, wann, warum).
