// Spec 2026-11-07-einstellungen-auswahlfelder-und-beschreibung (E2/E3): Verhalten des Partials _SettingField fuer /Settings und
// /ServiceSettings. Bool: Hidden-Feld traegt den Wert (Checkbox ohne name). MultiChoice: Checkboxen -> kommagetrennt in Katalog-
// Reihenfolge ins Hidden-Feld. Suche + "Nur geaenderte": blendet Zeilen und leere Gruppen clientseitig aus.
(function () {
    document.querySelectorAll('.bool-toggle').forEach(function (cb) {
        cb.addEventListener('change', function () {
            var row = this.closest('.setting-row');
            var hidden = row && row.querySelector('.bool-hidden');
            if (hidden) hidden.value = this.checked ? 'true' : 'false';
            var label = this.parentElement.querySelector('.form-check-label');
            if (label) label.textContent = this.checked ? 'Aktiviert' : 'Deaktiviert';
        });
    });

    document.querySelectorAll('.multi-toggle').forEach(function (cb) {
        cb.addEventListener('change', function () {
            var row = this.closest('.setting-row');
            var werte = Array.prototype.filter.call(row.querySelectorAll('.multi-toggle'), function (c) { return c.checked; })
                .map(function (c) { return c.value; });
            row.querySelector('.multi-hidden').value = werte.join(',');
        });
    });

    var suche = document.getElementById('settingsSuche');
    var nurGeaendert = document.getElementById('settingsNurGeaendert');
    if (!suche && !nurGeaendert) return;
    function filtern() {
        var q = (suche ? suche.value : '').trim().toLowerCase();
        var nur = nurGeaendert && nurGeaendert.checked;
        document.querySelectorAll('.setting-row').forEach(function (row) {
            var treffer = (!q || row.getAttribute('data-suche').indexOf(q) >= 0) && (!nur || row.getAttribute('data-weicht-ab') === 'true');
            row.classList.toggle('d-none', !treffer);
        });
        document.querySelectorAll('[data-setting-gruppe]').forEach(function (g) {
            g.classList.toggle('d-none', !g.querySelector('.setting-row:not(.d-none)'));
        });
    }
    if (suche) suche.addEventListener('input', filtern);
    if (nurGeaendert) nurGeaendert.addEventListener('change', filtern);
})();
