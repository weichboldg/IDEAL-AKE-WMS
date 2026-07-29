---
typ: feature
epic: true
---
# IDEAL Standort Anpassungen

## Worum geht es
wir wollen einen neuen Standort Live schalten 
Es ist eine eigene SAGE Installation vorhanden.
Am IDEAL Standort soll ein eigenes IDEAL-AKE-WMS Datenbanksystem implementiert werden -> somit keine SINGLE DB Installation.

Der Standort Ideal arbeitet ein wenig anders als der AKE Standort. Somit müssen wir hier die Möglichkeit schaffen, gewisse Views in unseren Einstellungen (zb. Appsettings) zu definieren.

Dadurch ich gerne eine SINGLE Source Strategie anstrebe, möchte ich das in einem gemeinsamen Entwicklungscode beibehalten. 
Ein schalter für gewisse andere Settings / Logiken wäre deshalb sinnvoll.
Für die Wartbarkeit des Source Codes wären Trennungen wo möglich sinnvoll. es ist zwar geplant step by step arbeitsweisen anzupassen, das wird jedoch einige zeit dauern.

beispiel ist zb. der Fertiungs/Produktionsauftrag. In der IDEAL gibt es für einen Produktionsauftrag mehrer Subaufträge -> AKE hat nur eine flache hierarchie 1:1 derzeit.
als Basis könnte hier die OSEON implementierung der Aufträge dienen. diese ist auch bereits hierarchisch.

ein weiteres beispiel ist die Planung der Termine - diese ist bei AKE derzeit mit Werten in unseren Settings -> bei IDEAL gibt es bereits ein anderes Tool (eigene view) welche die Terminierung durchführt.

Die genauen Definitionen zu den Views bzw. notwendigen findest du im Anhang Ordner 2026-0-29-Standort-IDEAL



## Offene Fragen / Unklarheiten
<!-- ruhig unfertig lassen - hier darf gedacht werden -->

## Reif fuer den Backlog?
<!-- wenn ja: Datei nach ../backlog/ verschieben, dann startet die Kette -->
