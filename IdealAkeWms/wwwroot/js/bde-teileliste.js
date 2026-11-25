// v1.99.32 BDE-Teileliste (Spec 2026-11-13-bde-vereinfacht-teileliste-blechfertigung): Werkbank-Wechsel ueber das Terminal
// (das Terminal entscheidet, ob die Werkbank Teileliste oder Auftragsliste zeigt), Einzelmeldung im Fenster, Block mit Bestaetigung.
// Der Server prueft jede Meldung selbst (ADR 0026); site.js sperrt den Absende-Knopf gegen Doppeltipp.
(function () {
    'use strict';
    var sel = document.getElementById('teilelisteWerkbank');
    if (sel) {
        sel.addEventListener('change', function () {
            window.location.href = sel.dataset.ziel + '?workplaceId=' + encodeURIComponent(sel.value);
        });
    }

    // v1.99.73 (E1-2): table-filter.js sortiert alle Zeilen je <tbody>; danach jede Kinderzeile wieder direkt hinter ihre Baugruppe.
    document.addEventListener('table-sorted', function () {
        document.querySelectorAll('table.bde-teileliste > tbody > tr[data-tl-eltern-id]').forEach(function (eltern) {
            var nach = eltern;
            eltern.parentNode.querySelectorAll(':scope > tr[data-eltern="' + eltern.dataset.tlElternId + '"]').forEach(function (kind) {
                nach.after(kind);
                nach = kind;
            });
        });
    });

    var fmt = function (n) { return n.toLocaleString('de-DE', { maximumFractionDigits: 4 }); };

    var modal = document.getElementById('meldenModal');
    if (modal) {
        modal.addEventListener('show.bs.modal', function (ev) {
            var b = ev.relatedTarget;
            modal.querySelector('[data-feld="schluessel"]').value = b.dataset.schluessel;
            modal.querySelector('[data-feld="matchcode"]').textContent = b.dataset.matchcode || '';
            modal.querySelector('[data-feld="soll"]').textContent = fmt(parseFloat(b.dataset.soll));
            modal.querySelector('#meldenGut').value = b.dataset.soll;
            modal.querySelector('#meldenAusschuss').value = '';
        });
        modal.addEventListener('shown.bs.modal', function () { modal.querySelector('#meldenGut').select(); });
    }

    var form = document.getElementById('blockForm');
    if (!form) return;
    var knopf = document.getElementById('blockMelden');
    var alle = document.getElementById('alleAuswaehlen');
    var boxen = function () { return Array.prototype.slice.call(document.querySelectorAll('.tl-auswahl')); };
    var auswahl = function () {
        var gewaehlt = boxen().filter(function (c) { return c.checked; });
        return {
            zeilen: gewaehlt.reduce(function (s, c) { return s + parseInt(c.dataset.anzahl || '1', 10); }, 0),
            summe: gewaehlt.reduce(function (s, c) { return s + parseFloat(c.dataset.soll); }, 0)
        };
    };
    var aktualisieren = function () {
        var a = auswahl();
        knopf.disabled = a.zeilen === 0;
        knopf.textContent = a.zeilen === 0 ? 'Auswahl fertigmelden' : 'Auswahl fertigmelden (' + a.zeilen + ' Zeilen, Summe ' + fmt(a.summe) + ')';
    };
    document.addEventListener('change', function (ev) {
        if (ev.target === alle) boxen().forEach(function (c) { c.checked = alle.checked; });
        if (ev.target === alle || ev.target.classList.contains('tl-auswahl')) aktualisieren();
    });
    form.addEventListener('submit', function (ev) {
        var a = auswahl();
        if (!window.confirm(a.zeilen + ' Zeilen mit voller Sollmenge fertigmelden (Summe ' + fmt(a.summe) + ')?')) {
            ev.preventDefault();
            ev.stopImmediatePropagation();
        }
    }, true);
    aktualisieren();
})();
