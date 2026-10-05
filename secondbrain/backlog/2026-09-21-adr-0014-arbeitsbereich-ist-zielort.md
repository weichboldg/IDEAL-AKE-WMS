---
typ: bug
---
# ADR 0014: Der Arbeitsbereich ist ein Zielort, keine Werkbank

**Aufgenommen 2026-09-21.** Befund aus der Spec [[2026-09-21-ideal-bde-arbeitsgaenge-aus-struktur-spec]].

## Befund

[[0014-werkbank-datenhoheit-sage-fuehrend-mit-abweichungsmeldung]] und die Materialisierung
([[2026-08-20-materialisierung-fachliche-felder-spec]], v1.37, Testbereit, nicht gemergt) leiten
`ProductionOrder.ProductionWorkplaceId` - die **Werkbank** - aus dem Struktur-Feld **Arbeitsbereich** ab.

Grundlage war die Annahme vom August: *"Werkbank = Arbeitsbereich aus der FA-Struktur."* Sie war
ausdruecklich als Annahme formuliert ("nehmen wir an"), weil die Sage-Arbeitsplatz-Stammdaten damals
noch nicht vorlagen.

**Am 2026-09-21 vom Menschen klargestellt - die Annahme ist widerlegt:**

| Begriff | Bedeutung |
|---|---|
| **Arbeitsbereich** (`K-02`, `S-01`, `H4-04`) | **Zielort** - wohin das Teil kommt, wenn alle Arbeitsschritte erledigt sind, bzw. allgemein sein Bestimmungsort. Begrifflich aus **Lagerorten** abgeleitet. |
| **Werkbank** | **Arbeitsplatz**, an dem gearbeitet wird - Sage `KHKPpsArbeitsplaetze` (`2300` / "Kanterei W1") |

Arbeitsbereich und Arbeitsschritt stehen in keiner hierarchischen Beziehung. Ein Zielort ist keine
Werkbank.

## Warum es heute noch keinen Schaden gibt - und wann es welchen gibt

**Heute folgenlos:** Bei IDEAL existieren noch **keine** `ProductionWorkplaces`. Die Ableitung findet
nichts und schreibt nichts.

**Sobald die IDEAL-Werkbaenke angelegt sind** (Voraussetzung der BDE-Spec), tritt eines von zwei ein:
- Die Werkbaenke heissen wie die Arbeitsplaetze ("Kanterei W1") - dann findet die Ableitung **keinen**
  Arbeitsbereich und meldet bei jedem Lauf jeden Arbeitsbereich als "unbekannte Werkbank". **Dauerrauschen**,
  das echte Meldungen verdeckt.
- Jemand nennt eine Werkbank `K-02` - dann schreibt die Ableitung einen **Zielort in das Werkbank-Feld**.
  **Falsche Daten**, und still.

**Reihenfolge:** Diese Notiz muss **vor dem Anlegen der IDEAL-Werkbaenke** geklaert sein.

## Was zu entscheiden ist

1. **Die Werkbank-Ableitung aus dem Arbeitsbereich entfernen?** Fuer IDEAL faellt die Werkbank dann nicht
   mehr beim Materialisieren an - sie ergibt sich aus den Arbeitsgaengen (je Arbeitsgang eine Werkbank,
   siehe BDE-Spec). Ein Auftrag kann durch mehrere Werkbaenke laufen; **eine** Werkbank am Auftrag ist
   bei IDEAL womoeglich gar nicht das richtige Modell.
2. **Wohin gehoert der Arbeitsbereich stattdessen?** Er ist ein Zielort. Eigene Anzeige-Spalte
   "Zielort" am Auftrag bzw. in den Listen? Und: Besteht eine Verwandtschaft zum **Kommissionierziel**
   (`FaHierarchyNode.Kommissionieren`), das ebenfalls ein Ziel beschreibt? **Nicht annehmen** - die
   Struktur fuehrt beide als getrennte Felder, und es gibt einen Grund dafuer, den der Fachbereich kennt.
3. **Was wird aus ADR 0014 selbst?** Die Datenhoheits-Regel (Sage fuehrend, Abweichung melden, Umschaltpunkt
   auf Variante C) bleibt als **Muster** richtig - sie wurde auf das falsche Feld angewandt. Die ADR
   braucht einen Nachtrag, der das klarstellt, statt stillschweigend zu veralten.
4. **AKE:** Betrifft die Werkbank-Ableitung AKE? Nach bisherigem Stand laeuft die Materialisierung nur bei
   Master `true` - dann ist AKE nicht beruehrt. Zu bestaetigen.

## Folgen fuer bereits Gebautes (v1.37, im Buendel-Zweig)

- `FaMaterializationSyncService`: die Werkbank-Ableitung aus dem Arbeitsbereich samt Abweichungs-Meldung.
- Die Sammelmeldung "unbekannte Arbeitsbereiche" - nach der Klaerung gegenstandslos oder umzudeuten.
- Der Klassenkommentar, der `Workplace` aus der app-verwalteten Liste nimmt (zweite Z1-Ausnahme) - zu
  pruefen, ob die Ausnahme noch gebraucht wird.
- Die Kopfzeilen-/Spalten-Anzeige der Werkbank in den FA-Listen.

**Nichts davon ist produktiv** - der Zweig ist ungemergt. Das ist der billigste Zeitpunkt fuer die
Korrektur.

## Bezug

[[0014-werkbank-datenhoheit-sage-fuehrend-mit-abweichungsmeldung]],
[[2026-08-20-materialisierung-fachliche-felder-spec]],
[[2026-09-21-ideal-bde-arbeitsgaenge-aus-struktur-spec]]
