// IDEAL FA-Liste-Hierarchie: Wiederholungswerte je HauptFA-Gruppe nur an der ersten Zeile zeigen.
// Client-seitig, weil die Sortierung client-seitig laeuft (table-filter.js) — eine serverseitig
// gesetzte Markierung waere nach dem ersten Sortierklick falsch.
// Der Wert bleibt im DOM; unterdrueckt wird ausschliesslich die Darstellung (color: transparent,
// siehe .fa-liste-repeat-hidden in site.css), damit Markieren, Kopieren, Spaltenfilter und ein
// spaeterer Export den Wert weiterhin sehen.
(function () {
    'use strict';

    var CLASS = 'fa-liste-repeat-hidden';

    function apply(table) {
        table.querySelectorAll('tbody.fa-liste-group').forEach(function (tbody) {
            var last = {};
            // ':scope >' statt eines Descendant-Selectors: Die Kombigeraete-Varianten-Zeile
            // (tr.fa-liste-group-heads) traegt ebenfalls fa-liste-node-row und enthaelt eine
            // verschachtelte Tabelle mit eigenen <tr>/<td>. Direkte Kinder zu verlangen haelt
            // diesen Baustein aus der inneren Tabelle heraus — auch dann, wenn dort spaeter
            // einmal data-repeat-key-Zellen auftauchen.
            tbody.querySelectorAll(':scope > tr.fa-liste-node-row').forEach(function (row) {
                // Unsichtbare Zeile zaehlt nicht mit: zugeklappte Gruppe (fa-liste-gruppierung.js)
                // und client-seitiger Filter (table-filter.js) setzen beide style.display = 'none'.
                // Sonst wuerde eine ausgeblendete Zeile den Wert der ersten sichtbaren verschlucken.
                if (row.style.display === 'none') return;
                row.querySelectorAll(':scope > [data-repeat-key]').forEach(function (cell) {
                    var key = cell.getAttribute('data-repeat-key');
                    var val = (cell.textContent || '').trim();
                    // Ein leerer Wert wird nie unterdrueckt und wird auch nie Vergleichsbasis,
                    // sonst koennte eine leere Zelle eine darunterliegende gefuellte maskieren.
                    if (val !== '' && last[key] === val) {
                        cell.classList.add(CLASS);
                    } else {
                        cell.classList.remove(CLASS);
                        last[key] = val;
                    }
                });
            });
        });
    }

    // Gate: nur im hierarchischen Modus (Master an). Bei data-hierarchical="false" trifft der
    // Selektor nichts — AKE bleibt unberuehrt.
    function applyAll() {
        document.querySelectorAll('table[data-hierarchical="true"]').forEach(apply);
    }

    // Die Funktion ist idempotent (sie setzt je Zelle genau eine Klasse oder entfernt sie und
    // liest nur textContent, den die Klasse nicht veraendert). Darum ist es unschaedlich, dass
    // table-filter.js beim Init einmal sortiert und damit direkt nach dem Laden erneut ausloest.
    applyAll();
    document.addEventListener('table-sorted', applyAll);
    document.addEventListener('table-filter-applied', applyAll);
    document.addEventListener('fa-liste-group-toggled', applyAll);
})();
