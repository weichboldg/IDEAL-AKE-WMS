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

    // v1.99.65 B4: „Verpackt“ und „Entfernen“ schliessen sich aus. Entfernen angehakt => Verpackt derselben Zeile abgehakt und
    // gesperrt (disabled = fuer Screenreader gesperrt, title nennt den Grund); Entfernen geloest => Verpackt wieder frei.
    // Der Server lehnt den Widerspruch zusaetzlich ab (Workflow.BearbeitenAsync).
    function verpacktZu(id) { return document.querySelector('[data-verpackt="' + id + '"]'); }
    document.addEventListener('change', function (e) {
        var entf = e.target.closest('[data-entfernen]');
        var v = entf && verpacktZu(entf.getAttribute('data-entfernen'));
        if (!v) return;
        if (entf.checked) v.checked = false;
        v.disabled = entf.checked;
        if (entf.checked) v.title = 'Wird entfernt'; else v.removeAttribute('title');
    });

    // v1.99.65 (Antwort R-1): „Auf Übergeben setzen“ mit offenen Positionen fragt per Bootstrap-Modal: offene entfernen und
    // uebergeben, oder offen beibehalten und uebergeben. Offen = Verpackt nicht angehakt und nicht schon zum Entfernen markiert.
    var modalEl = document.getElementById('uebergeben-frage-modal');
    if (!entwurf || !modalEl || !window.bootstrap) return;
    var modal = bootstrap.Modal.getOrCreateInstance(modalEl);
    var knopf = null;
    function offene() {
        return Array.prototype.filter.call(document.querySelectorAll('[data-verpackt]'), function (v) {
            var entf = document.querySelector('[data-entfernen="' + v.getAttribute('data-verpackt') + '"]');
            return !v.checked && !(entf && entf.checked);
        });
    }
    function dienstleisterGesetzt() {
        var k = document.getElementById('creditorId'), d = document.getElementById('dienstleister');
        if (!k && !d) return true;   // Kopf nicht bearbeitbar: Server entscheidet
        return !!((k && k.value) || (d && d.value.trim()));
    }
    document.addEventListener('click', function (e) {
        var ziel = e.target.closest('[data-uebergeben-frage]');
        if (!ziel) return;
        var liste = offene();
        if (liste.length === 0 || !dienstleisterGesetzt()) return;   // ohne Dienstleister lehnt der Server zuerst ab – nichts entfernen
        e.preventDefault();
        knopf = ziel;
        modalEl.querySelector('[data-uebergeben-anzahl]').textContent =
            liste.length === 1 ? '1 Position' : liste.length + ' Positionen';
        var ul = modalEl.querySelector('[data-uebergeben-liste]');
        ul.replaceChildren.apply(ul, liste.map(function (v) {
            var li = document.createElement('li');
            li.textContent = v.getAttribute('data-bezeichnung');
            return li;
        }));
        modal.show();
    });
    modalEl.addEventListener('click', function (e) {
        var wahl = e.target.closest('[data-uebergeben-wahl]');
        if (!wahl || !knopf) return;
        if (wahl.getAttribute('data-uebergeben-wahl') === 'entfernen') {
            offene().forEach(function (v) {
                var entf = document.querySelector('[data-entfernen="' + v.getAttribute('data-verpackt') + '"]');
                if (entf) { entf.checked = true; entf.dispatchEvent(new Event('change', { bubbles: true })); }
            });
        }
        wahl.disabled = true;
        entwurf.requestSubmit(knopf);   // sendet ziel=Uebergeben wie der Knopf selbst (Speichern, dann Statuswechsel)
    });
})();
