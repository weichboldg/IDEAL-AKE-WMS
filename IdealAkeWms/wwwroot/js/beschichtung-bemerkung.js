// v1.99.41 A12 (Spec 2026-11-18-beschichtungsauftrag-ausbau, Rueckfrage 16): Die Bemerkung hat ein eigenes Formular.
// Wer sie aendert und dann druckt, ein PDF laedt, speichert oder den Status wechselt, verliert sie bzw. druckt den alten
// Stand. Deshalb: sichtbarer Text „nicht gespeichert“ am Feld und eine Rueckfrage an allen [data-bemerkung-pruefen].
(function () {
    'use strict';
    var feld = document.getElementById('bemerkung');
    var hinweis = document.getElementById('bemerkung-ungespeichert');
    if (!feld || !hinweis) return;
    var gespeichert = feld.value;
    function geaendert() { return feld.value !== gespeichert; }
    feld.addEventListener('input', function () { hinweis.hidden = !geaendert(); });
    // Capture-Phase: laeuft vor den Klick-Handlern der Ziele (z. B. print-visible-columns.js)
    document.addEventListener('click', function (e) {
        var ziel = e.target.closest('[data-bemerkung-pruefen]');
        if (!ziel || !geaendert()) return;
        if (!window.confirm('Bemerkung ist nicht gespeichert — trotzdem fortfahren?')) {
            e.preventDefault();
            e.stopImmediatePropagation();
        }
    }, true);
})();
