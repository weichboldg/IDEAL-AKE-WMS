// IDEAL FA-Liste-Hierarchie (Teil 7/8): gemeinsamer Zusatzbaustein fuer die tabellarischen Listen
// (ProductionOrders/FaWorklist/FaCompletion/Tracking/Picking/PickingLeitstand) im hierarchischen Modus.
// Aktiv NUR, wenn der Server die Tabelle mit data-hierarchical="true" rendert (Master an).
//
// Leistungen:
//   1. Chevron-Collapse je Gruppen-<tbody> (Standard: aufgeklappt, §3). Analog oseon-toggle/oseon-chevron.
//   2. Auto-Expand bei Suchtreffer in einer ZUGEKLAPPTEN Gruppe (F2, gefaehrlichster stiller Fehler):
//      ein optionaler client-seitiger Hervorheben-Filter [data-fa-liste-filter] meldet den Suchtext;
//      Gruppen mit passender Zeile werden aufgeklappt.
//
// KEIN Eingriff in table-filter.js — dieser Baustein beobachtet nur und klappt bei Bedarf auf/zu.
(function () {
    'use strict';

    document.querySelectorAll('table[data-hierarchical="true"]').forEach(initGroupedTable);

    function initGroupedTable(table) {
        table.querySelectorAll('.fa-liste-group-toggle').forEach(function (toggle) {
            toggle.addEventListener('click', function () { toggleGroup(groupOf(toggle)); });
            toggle.addEventListener('keydown', function (e) {
                if (e.key === 'Enter' || e.key === ' ') { e.preventDefault(); toggleGroup(groupOf(toggle)); }
            });
        });

        // Optionaler „Alle auf/zu"-Schalter (falls die View ihn rendert).
        bindAll(table, '[data-fa-liste-expand-all]', true);
        bindAll(table, '[data-fa-liste-collapse-all]', false);

        // F2 — Auto-Expand: der client-seitige Hervorheben-Filter (falls vorhanden) meldet den Suchtext.
        var filter = document.querySelector('[data-fa-liste-filter]');
        if (filter) {
            filter.addEventListener('input', function () {
                autoExpandOnFilter(table, (filter.value || '').trim().toLowerCase());
            });
        }
    }

    function bindAll(table, selector, open) {
        var btn = document.querySelector(selector);
        if (!btn) return;
        btn.addEventListener('click', function () {
            table.querySelectorAll('tbody.fa-liste-group').forEach(function (g) { setGroup(g, open); });
        });
    }

    function groupOf(el) { return el.closest('tbody.fa-liste-group'); }

    // toggle: ohne Argument umschalten; sonst gezielt open/collapsed setzen.
    function toggleGroup(tbody) {
        if (!tbody) return;
        setGroup(tbody, tbody.classList.contains('fa-liste-collapsed'));
    }

    function setGroup(tbody, open) {
        if (!tbody) return;
        tbody.classList.toggle('fa-liste-collapsed', !open);
        tbody.querySelectorAll('.fa-liste-node-row').forEach(function (r) {
            r.style.display = open ? '' : 'none';
        });
        var chev = tbody.querySelector('.fa-liste-chevron');
        if (chev) chev.style.transform = open ? 'rotate(90deg)' : 'rotate(0deg)';
        var toggle = tbody.querySelector('.fa-liste-group-toggle');
        if (toggle) toggle.setAttribute('aria-expanded', open ? 'true' : 'false');

        // Views mit zeilengebundener Auswahl (z.B. PickingLeitstand-Bulk-Select) muessen beim
        // Zuklappen die nun versteckten Zeilen aus der Auswahl nehmen — analog Filter. Wir melden
        // nur das Ereignis; die view-spezifische Logik reagiert (dieser Baustein bleibt generisch).
        document.dispatchEvent(new CustomEvent('fa-liste-group-toggled', {
            detail: { open: open, group: tbody.getAttribute('data-group') }
        }));
    }

    function autoExpandOnFilter(table, q) {
        if (!q) return;
        table.querySelectorAll('tbody.fa-liste-group.fa-liste-collapsed').forEach(function (tbody) {
            var hit = Array.prototype.some.call(
                tbody.querySelectorAll('.fa-liste-node-row'),
                function (r) { return (r.textContent || '').toLowerCase().indexOf(q) !== -1; });
            if (hit) setGroup(tbody, true);
        });
    }
})();
