// v1.99.41 A12 (Spec 2026-11-18-beschichtungsauftrag-ausbau, Rueckfrage 16): Die Bemerkung hat ein eigenes Formular.
// Wer sie aendert und dann druckt, ein PDF laedt, speichert oder den Status wechselt, verliert sie bzw. druckt den alten
// Stand. Deshalb: sichtbarer Text „nicht gespeichert“ am Feld und eine Rueckfrage an allen [data-bemerkung-pruefen].
// v1.99.56 (Spec 2026-11-23 Teil 3, Rueckfrage 2): Drucken/PDF fragen auch, wenn Eingaben im entwurf-form ungespeichert sind
// (Status-Knoepfe und Speichern senden sie ja mit).
(function () {
    'use strict';
    var feld = document.getElementById('bemerkung');
    var hinweis = document.getElementById('bemerkung-ungespeichert');
    var entwurf = document.getElementById('entwurf-form');
    if (!(feld && hinweis) && !entwurf) return;
    function formText() { return new URLSearchParams(new FormData(entwurf)).toString(); }
    var gespeichert = feld ? feld.value : null;
    var entwurfStand = entwurf ? formText() : null;
    function bemerkungGeaendert() { return !!feld && feld.value !== gespeichert; }
    function entwurfGeaendert() { return !!entwurf && formText() !== entwurfStand; }
    if (feld && hinweis) feld.addEventListener('input', function () { hinweis.hidden = !bemerkungGeaendert(); });
    // Capture-Phase: laeuft vor den Klick-Handlern der Ziele (z. B. print-visible-columns.js)
    document.addEventListener('click', function (e) {
        var ziel = e.target.closest('[data-bemerkung-pruefen]');
        if (!ziel) return;
        var text = null;
        if (bemerkungGeaendert()) text = 'Bemerkung ist nicht gespeichert — trotzdem fortfahren?';
        else if (ziel.hasAttribute('data-print-link') && entwurfGeaendert())
            text = 'Änderungen an den Positionen sind nicht gespeichert — Druck zeigt den gespeicherten Stand. Trotzdem fortfahren?';
        if (text && !window.confirm(text)) {
            e.preventDefault();
            e.stopImmediatePropagation();
        }
    }, true);
})();
