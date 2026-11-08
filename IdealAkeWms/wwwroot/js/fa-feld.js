// v1.87.0 (Spec 2026-11-08-lagerbuchungen-fa-feld-und-verlinkung, E3): FA-Feld der Lager-Masken.
// Pruefkarte #faPruefung unter dem Feld (api/stock/fa-lookup) + Vorschlagsliste <datalist id="faVorschlaege"> (api/stock/fa-search).
// Reagiert auf Tippen (verzoegert), Verlassen/change und das Setzen durch barcode-scanner.js (clearAndFillFaNumber feuert 'change').
// Daten nur per textContent (kein HTML aus Daten). Der Server loest beim Speichern selbst auf – die Karte ist nur Vorschau.
(function () {
    'use strict';
    var input = document.getElementById('ProductionOrder');
    var karte = document.getElementById('faPruefung');
    var liste = document.getElementById('faVorschlaege');
    if (!input || !karte) return;

    var seq = 0, timer = null, letzte = null;

    function el(tag, cls, text) {
        var e = document.createElement(tag);
        if (cls) e.className = cls;
        if (text !== undefined && text !== null) e.textContent = text;
        return e;
    }

    function leeren() {
        karte.replaceChildren();
        karte.className = 'mt-2';
    }

    function zeigen(d) {
        leeren();
        var warn = d.status !== 'gefunden' || d.warnStatus;
        var box = el('div', 'alert mb-0 py-2 ' + (warn ? 'alert-warning' : 'alert-light border'));
        if (d.status === 'gefunden') {
            var kopf = el('div', 'fw-semibold', 'FA ' + d.anzeigeNummer);
            box.appendChild(kopf);
            if (d.description) box.appendChild(el('div', null, d.description));
            if (d.articleNumber) box.appendChild(el('div', 'small', 'Artikel ' + d.articleNumber));
            box.appendChild(el('div', warn ? 'fw-semibold' : 'small', (warn ? 'Achtung: Status ' : 'Status: ') + d.statusText));
            if (d.link) {
                var a = el('a', 'small alert-link', 'FA anzeigen');
                a.href = d.link;
                box.appendChild(a);
            }
        } else {
            box.appendChild(el('div', 'fw-semibold', 'Achtung: ' + d.hinweis));
            // v1.90.0 (Entscheidung Q-1): Text je nach FA-Pflicht der Buchungsart (Server-Regel, data-buchungsart der Karte)
            box.appendChild(el('div', d.pflicht ? 'fw-semibold' : 'small', d.pflicht
                ? 'FA nicht eindeutig – Buchen erst mit gültigem FA möglich.'
                : 'Buchen ist möglich, die Buchung wird dann nicht mit einem FA verknüpft.'));
        }
        karte.appendChild(box);
    }

    function pruefen() {
        var nr = input.value.trim();
        if (nr === letzte) return;
        letzte = nr;
        if (nr === '') { leeren(); return; }
        var meine = ++seq;
        var art = karte.getAttribute('data-buchungsart') || '';
        fetch('/api/stock/fa-lookup?nr=' + encodeURIComponent(nr) + '&art=' + encodeURIComponent(art), { credentials: 'same-origin' })
            .then(function (r) { return r.ok ? r.json() : null; })
            .then(function (d) { if (meine === seq && d) zeigen(d); })
            .catch(function () {
                if (meine !== seq) return;
                leeren();
                karte.appendChild(el('div', 'small text-body-secondary', 'FA konnte gerade nicht geprüft werden – der Server prüft beim Speichern.'));
            });
    }

    function vorschlagen() {
        if (!liste) return;
        var q = input.value.trim();
        if (q.length < 2) return;
        fetch('/api/stock/fa-search?q=' + encodeURIComponent(q), { credentials: 'same-origin' })
            .then(function (r) { return r.ok ? r.json() : []; })
            .then(function (treffer) {
                liste.replaceChildren();
                treffer.forEach(function (t) {
                    var o = document.createElement('option');
                    o.value = t.orderNumber;
                    o.textContent = t.text;
                    liste.appendChild(o);
                });
            })
            .catch(function () { /* Vorschlaege sind optional */ });
    }

    input.addEventListener('input', function () {
        clearTimeout(timer);
        timer = setTimeout(function () { vorschlagen(); pruefen(); }, 400);
    });
    input.addEventListener('change', pruefen);
    input.addEventListener('blur', pruefen);
    if (input.value.trim() !== '') pruefen(); // Wiederanzeige nach Validierungsfehler
})();
