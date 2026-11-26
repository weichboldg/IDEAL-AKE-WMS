// v1.99.32 BDE-Teileliste (Spec 2026-11-13-bde-vereinfacht-teileliste-blechfertigung): Werkbank-Wechsel ueber das Terminal
// (das Terminal entscheidet, ob die Werkbank Teileliste oder Auftragsliste zeigt), Einzelmeldung im Fenster, Block mit Bestaetigung.
// v1.99.75 (E3): Fenster mit Teilfertig/Fertig, Vorgabe = Restmenge; Bestaetigungen im eigenen Modal statt window.confirm.
// Der Server prueft jede Meldung selbst (ADR 0026); site.js sperrt den Absende-Knopf gegen Doppeltipp.
(function () {
    'use strict';
    var sel = document.getElementById('teilelisteWerkbank');
    if (sel) {
        sel.addEventListener('change', function () {
            // v1.99.81 (Task 4b): nur beim BDE-Admin traegt die Auswahl data-operator-id, dann bleibt der Werker angemeldet
            window.location.href = sel.dataset.ziel + '?workplaceId=' + encodeURIComponent(sel.value)
                + (sel.dataset.operatorId ? '&operatorId=' + encodeURIComponent(sel.dataset.operatorId) : '');
        });
    }

    // v1.99.81 (K-1): Kopf ein-/ausklappen, Zustand je Geraet (nicht je Werker – das Geraet steht an einer Werkbank)
    var kopfBereich = document.getElementById('bdeKopfBereich');
    if (kopfBereich) {
        var KOPF = 'wms.bdeKopfZu', kopfKnopf = document.getElementById('bdeKopfKnopf');
        try {
            if (localStorage.getItem(KOPF) === '1') { kopfBereich.classList.remove('show'); kopfKnopf.setAttribute('aria-expanded', 'false'); }
        } catch (e) { /* ohne Speicher bleibt der Kopf offen */ }
        kopfBereich.addEventListener('shown.bs.collapse', function () { try { localStorage.setItem(KOPF, '0'); } catch (e) { } });
        kopfBereich.addEventListener('hidden.bs.collapse', function () { try { localStorage.setItem(KOPF, '1'); } catch (e) { } });
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
            modal.querySelector('[data-feld="gemeldet"]').textContent = fmt(parseFloat(b.dataset.gemeldet || '0'));
            modal.querySelector('[data-feld="rest"]').textContent = fmt(parseFloat(b.dataset.rest));
            modal.querySelector('#meldenGut').value = b.dataset.rest;
            modal.querySelector('#meldenAusschuss').value = '';
        });
        modal.addEventListener('shown.bs.modal', function () { modal.querySelector('#meldenGut').select(); });
    }

    // Bestaetigungs-Modal (Bootstrap, einmal je Seite): Touch-Knoepfe btn-lg, Fokus auf "Abbrechen" (sicherer Standard).
    var bestaetigen = (function () {
        var el = document.createElement('div');
        el.className = 'modal fade bde-terminal';
        el.tabIndex = -1;
        el.setAttribute('aria-labelledby', 'tlBestaetigenText');
        el.innerHTML = '<div class="modal-dialog"><div class="modal-content"><div class="modal-body">' +
            '<p class="fs-5 mb-0" id="tlBestaetigenText"></p></div><div class="modal-footer">' +
            '<button type="button" class="btn btn-outline-secondary btn-lg" data-bs-dismiss="modal">Abbrechen</button>' +
            '<button type="button" class="btn btn-primary btn-lg" data-ja>Ja</button></div></div></div>';
        document.body.appendChild(el);
        var m = bootstrap.Modal.getOrCreateInstance(el);
        var beiJa = null, beiNein = null;
        el.querySelector('[data-ja]').addEventListener('click', function () { var f = beiJa; beiJa = null; beiNein = null; m.hide(); if (f) f(); });
        // v1.99.77: optionaler Rueckruf bei Abbrechen/Schliessen (bde-fa-sicht.js gibt damit den gesperrten Knopf frei)
        el.addEventListener('hidden.bs.modal', function () { var n = beiNein; beiJa = null; beiNein = null; if (n) n(); });
        el.addEventListener('shown.bs.modal', function () { el.querySelector('[data-bs-dismiss]').focus(); });
        return function (text, jaText, f, nein) {
            el.querySelector('#tlBestaetigenText').textContent = text;
            el.querySelector('[data-ja]').textContent = jaText;
            beiJa = f; beiNein = nein || null;
            m.show();
        };
    })();
    window.bdeBestaetigen = bestaetigen; // v1.99.77: auch fuer bde-fa-sicht.js (Gut < Soll), statt eines zweiten Bestaetigungs-Dialogs
    // Formulare mit data-bestaetigen (Ruecknahme in _TeilStatus): erst nach "Zuruecknehmen" im Modal absenden.
    document.addEventListener('submit', function (ev) {
        var f = ev.target;
        if (!f.dataset || f.dataset.bestaetigen == null || f.dataset.bestaetigt === '1') return;
        ev.preventDefault();
        ev.stopImmediatePropagation();
        bestaetigen(f.dataset.bestaetigen, 'Zurücknehmen', function () { f.dataset.bestaetigt = '1'; f.requestSubmit(); delete f.dataset.bestaetigt; });
    }, true);

    var form = document.getElementById('blockForm');
    if (!form) return;
    var knopf = document.getElementById('blockMelden');
    var alle = document.getElementById('alleAuswaehlen');
    var boxen = function () { return Array.prototype.slice.call(document.querySelectorAll('.tl-auswahl')); };
    // v1.99.74 (E2-2, Summiert): Summen-Haken [data-gruppe] traegt alle offenen Schluessel der Gruppe; ein mit ihm gesetzter Einzel-Haken
    // [data-in-gruppe] zaehlt nicht doppelt (gesendet wird er trotzdem – der Server entdoppelt per DistinctBy).
    var summe = function (g) { return document.querySelector('.tl-auswahl[data-gruppe="' + g + '"]'); };
    var auswahl = function () {
        var gewaehlt = boxen().filter(function (c) {
            var s = c.dataset.inGruppe != null ? summe(c.dataset.inGruppe) : null;
            return c.checked && !(s && s.checked);
        });
        return {
            zeilen: gewaehlt.reduce(function (s, c) { return s + parseInt(c.dataset.anzahl || '1', 10); }, 0),
            summe: gewaehlt.reduce(function (s, c) { return s + parseFloat(c.dataset.soll); }, 0)
        };
    };
    var aktualisieren = function () {
        var a = auswahl();
        knopf.disabled = a.zeilen === 0;
        knopf.textContent = a.zeilen === 0 ? 'Auswahl fertigmelden' : 'Auswahl fertigmelden (' + a.zeilen + (a.zeilen === 1 ? ' Zeile' : ' Zeilen') + ', Summe ' + fmt(a.summe) + ')';
    };
    document.addEventListener('change', function (ev) {
        if (ev.target === alle) boxen().forEach(function (c) { c.checked = alle.checked; });
        var t = ev.target;
        if (t.dataset && t.dataset.gruppe != null) {
            document.querySelectorAll('.tl-auswahl[data-in-gruppe="' + t.dataset.gruppe + '"]').forEach(function (c) { c.checked = t.checked; });
        } else if (t.dataset && t.dataset.inGruppe != null && summe(t.dataset.inGruppe)) {
            // v1.99.79 F-36: Summen-Haken folgt den Einzel-Haken in beide Richtungen (alle gesetzt → wieder gesetzt)
            summe(t.dataset.inGruppe).checked = Array.prototype.every.call(
                document.querySelectorAll('.tl-auswahl[data-in-gruppe="' + t.dataset.inGruppe + '"]'), function (c) { return c.checked; });
        }
        if (ev.target === alle || ev.target.classList.contains('tl-auswahl')) aktualisieren();
    });
    form.addEventListener('submit', function (ev) {
        if (form.dataset.bestaetigt === '1') return;
        ev.preventDefault();
        ev.stopImmediatePropagation();
        var a = auswahl();
        bestaetigen(a.zeilen + (a.zeilen === 1 ? ' Zeile' : ' Zeilen') + ' mit ihrer Restmenge fertigmelden (Summe ' + fmt(a.summe) + ')? Die Zeilen sind danach abgeschlossen.',
            'Fertig melden', function () { form.dataset.bestaetigt = '1'; form.requestSubmit(); delete form.dataset.bestaetigt; });
    }, true);
    aktualisieren();
})();
