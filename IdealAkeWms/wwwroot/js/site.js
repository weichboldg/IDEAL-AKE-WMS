// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Toolbar-Host ueber einer Tabelle: EIN Ort fuer Zahnrad (column-preferences.js) und CSV-Knopf (csv-export.js), v1.65.0.
// Ersetzt die Nachbar-Heuristik „das DIV direkt vor .table-responsive“ (fallstricke §19).
// 1. Element mit data-table-toolbar direkt vor dem Wrapper (vorhandener oder hier angelegter Host);
// 2. sonst ein explizit gesetzter Marker der View (z. B. ueber der Zaehlzeile, in der Werkzeugzeile);
// 3. sonst wird ein Host direkt vor dem Wrapper angelegt. Er traegt data-table-toolbar="auto" und wird nur ueber Stufe 1
//    gefunden — sonst landeten auf Seiten mit mehreren Tabellen (BdeMasterData) alle Knoepfe ueber der ersten.
// ponytail: Stufe 2 sucht dokumentweit, Grenze = eine Tabelle je Seite mit explizitem Marker; bei mehreren solchen
// Tabellen den Marker relativ zur Tabelle suchen.
window.tableToolbarHost = function (table) {
    var wrapper = table.closest('.table-responsive') || table.parentElement;
    var prev = wrapper.previousElementSibling;
    if (prev && prev.hasAttribute('data-table-toolbar')) return prev;
    var marked = document.querySelector('[data-table-toolbar]:not([data-table-toolbar="auto"])');
    if (marked) return marked;
    var host = document.createElement('div');
    host.className = 'd-flex justify-content-end mb-1';
    host.setAttribute('data-table-toolbar', 'auto');
    wrapper.parentElement.insertBefore(host, wrapper);
    return host;
};

// Responsive table: sticky scrollbar
// Bei langen Tabellen ist die echte horizontale Scrollbar weit unten (nach allen Zeilen).
// Dieses Script erstellt eine synchronisierte Scrollbar die am unteren Viewport-Rand klebt.
(function () {
    'use strict';

    // Etwas verzoegert starten damit table-filter.js die Filter-Zeilen einfuegen kann
    // (die Filter-Zeile veraendert die Tabellenbreite)
    document.addEventListener('DOMContentLoaded', function () {
        requestAnimationFrame(function () {
            document.querySelectorAll('.table-responsive').forEach(initStickyScrollbar);
        });
    });

    function initStickyScrollbar(wrapper) {
        // Sticky Scrollbar: div mit overflow-x: auto und einem breiten Kind-Element
        var stickyBar = document.createElement('div');
        stickyBar.className = 'table-sticky-scrollbar';
        var spacer = document.createElement('div');
        spacer.className = 'table-sticky-scrollbar-spacer';
        stickyBar.appendChild(spacer);

        // Nach dem Wrapper einfuegen
        wrapper.parentNode.insertBefore(stickyBar, wrapper.nextSibling);

        // Scroll synchronisieren (bidirektional)
        var syncing = false;
        wrapper.addEventListener('scroll', function () {
            if (syncing) return;
            syncing = true;
            stickyBar.scrollLeft = wrapper.scrollLeft;
            syncing = false;
        });
        stickyBar.addEventListener('scroll', function () {
            if (syncing) return;
            syncing = true;
            wrapper.scrollLeft = stickyBar.scrollLeft;
            syncing = false;
        });

        // Sentinel direkt UNTER dem Wrapper: IntersectionObserver blendet sticky bar aus
        // wenn die echte Scrollbar im Viewport sichtbar wird. Nicht IM Wrapper (v1.64.1): dort
        // scrollt der Sentinel horizontal mit, faellt beim Rechtsscrollen aus dem Clip-Bereich
        // und die sticky bar erschien zusaetzlich zur echten Scrollbar (zwei Balken).
        var sentinel = document.createElement('div');
        sentinel.className = 'table-scroll-sentinel';
        sentinel.style.height = '1px';
        sentinel.style.width = '100%';
        sentinel.style.marginTop = '-1px';
        wrapper.parentNode.insertBefore(sentinel, stickyBar);

        var observer = new IntersectionObserver(function (entries) {
            entries.forEach(function (entry) {
                stickyBar.classList.toggle('is-hidden', entry.isIntersecting);
                // display:none verwirft die Scroll-Position der Bar: beim Wiedereinblenden auf die
                // Tabelle setzen, sonst steht der Balken auf 0 und der naechste Zug springt die Tabelle zurueck.
                if (!entry.isIntersecting) stickyBar.scrollLeft = wrapper.scrollLeft;
            });
        }, { threshold: 0.1 });
        observer.observe(sentinel);

        // Initiale Sync + laufende Updates
        syncWidths(wrapper, stickyBar, spacer);

        // Bei jeder Groessenaenderung von Wrapper oder Tabelle nachziehen: Fenster-Resize,
        // Filter-Rows, aufgeklappte Zeilen (z. B. Stueckliste „Alle aufklappen“ verbreitert die Tabelle
        // erst nach dem Laden — sonst endet die sticky bar vor dem Tabellenende).
        var resizeObserver = new ResizeObserver(function () {
            syncWidths(wrapper, stickyBar, spacer);
        });
        resizeObserver.observe(wrapper);
        if (wrapper.firstElementChild) resizeObserver.observe(wrapper.firstElementChild);
    }

    function syncWidths(wrapper, stickyBar, spacer) {
        var hasOverflow = wrapper.scrollWidth > wrapper.clientWidth + 1;

        if (!hasOverflow) {
            stickyBar.classList.add('no-overflow');
            return;
        }
        stickyBar.classList.remove('no-overflow');
        wrapper.classList.add('has-overflow');

        // Spacer bekommt exakt die gleiche Breite wie der scrollbare Inhalt der Tabelle
        spacer.style.width = wrapper.scrollWidth + 'px';

        // Sticky Bar bekommt KEINE explizite Breite — sie fuellt automatisch
        // die gleiche Breite wie ihr Parent-Container (= gleich breit wie .table-responsive)
        stickyBar.style.width = '';
    }
})();

// Pagination: shared handler fuer das _Pagination-Partial.
// - Klick auf .page-link[data-page] setzt ?page=N (1-based, kein Reset von pageSize)
// - Change auf .pagination-page-size setzt ?pageSize=N und resetted page=1
// Alle anderen Query-Parameter (Filter etc.) bleiben unveraendert.
(function () {
    'use strict';

    function navigate(params) {
        var url = new URL(window.location.href);
        Object.keys(params).forEach(function (k) {
            var v = params[k];
            if (v === null || v === undefined || v === '') {
                url.searchParams.delete(k);
            } else {
                url.searchParams.set(k, v);
            }
        });
        window.location.href = url.toString();
    }

    document.addEventListener('click', function (e) {
        var link = e.target.closest('.pagination-bar .page-link[data-page]');
        if (!link) return;
        var li = link.closest('li');
        if (li && li.classList.contains('disabled')) {
            e.preventDefault();
            return;
        }
        e.preventDefault();
        var page = parseInt(link.getAttribute('data-page'), 10);
        if (isNaN(page) || page < 1) return;
        navigate({ page: page });
    });

    document.addEventListener('change', function (e) {
        if (!e.target.classList || !e.target.classList.contains('pagination-page-size')) return;
        var size = e.target.value;
        navigate({ pageSize: size, page: 1 });
    });
})();

// v1.99.23 (Spec 2026-11-14-schutz-doppel-absenden, Freigabe-Nachtrag, ADR 0026): Doppel-Absenden-Schutz im Browser – nur Komfort,
// der Schutz selbst liegt am Server (Pruefung am Schreibort). Global fuer alle POST-Formulare; ausgenommen GET, target="_blank"
// (auch per formtarget am Knopf) und data-mehrfach-erlaubt. Hat ein Seiten-Listener das Absenden abgebrochen (preventDefault, confirm
// „Abbrechen“), bleibt alles frei. Freigabe nach 10 s und bei pageshow (Zurueck-Taste/bfcache), damit nie etwas dauerhaft haengt.
// form.submit() aus Skripten loest kein submit-Ereignis aus und ist nicht betroffen.
// window.wmsSperren(knoepfe) fuer AJAX-Knoepfe: sperrt und gibt die Freigabe-Funktion zurueck; null = laeuft schon (Aufrufer bricht ab).
(function () {
    'use strict';
    var FREIGABE_MS = 10000;

    function knoepfeVon(form) {
        var liste = Array.prototype.slice.call(form.querySelectorAll('button:not([type]), button[type="submit"], input[type="submit"]'));
        if (form.id) {
            Array.prototype.forEach.call(document.querySelectorAll('[form="' + form.id + '"]'), function (k) {
                if (k.matches('button:not([type]), button[type="submit"], input[type="submit"]') && liste.indexOf(k) < 0) liste.push(k);
            });
        }
        return liste;
    }

    function sperren(knoepfe) {
        knoepfe.forEach(function (k) {
            if (k.dataset.wmsGesperrt === '1') return;
            k.dataset.wmsGesperrt = '1';
            k.dataset.wmsWarDisabled = k.disabled ? '1' : '0';
            k.disabled = true;
            k.setAttribute('aria-disabled', 'true');
            k.setAttribute('aria-busy', 'true');
            if (k.tagName === 'INPUT') {
                k.dataset.wmsText = k.value;
                k.value = 'wird gesendet …';
            } else {
                k.dataset.wmsHtml = k.innerHTML;
                // Knopf nur mit Symbol: Breite halten, Text nur fuer Screenreader
                var mitText = k.textContent.trim().length > 0;
                k.innerHTML = '<span class="spinner-border spinner-border-sm' + (mitText ? ' me-1' : '') + '" aria-hidden="true"></span>'
                    + (mitText ? 'wird gesendet …' : '<span class="visually-hidden">wird gesendet …</span>');
            }
        });
    }

    function freigeben(knoepfe) {
        knoepfe.forEach(function (k) {
            if (k.dataset.wmsGesperrt !== '1') return;
            k.disabled = k.dataset.wmsWarDisabled === '1';
            k.removeAttribute('aria-disabled');
            k.removeAttribute('aria-busy');
            if (k.tagName === 'INPUT') k.value = k.dataset.wmsText;
            else k.innerHTML = k.dataset.wmsHtml;
            delete k.dataset.wmsGesperrt; delete k.dataset.wmsWarDisabled; delete k.dataset.wmsText; delete k.dataset.wmsHtml;
        });
    }

    function formFreigeben(form) {
        if (form.dataset.wmsSendet !== '1') return;
        delete form.dataset.wmsSendet;
        clearTimeout(form._wmsTimer);
        freigeben(knoepfeVon(form));
    }

    document.addEventListener('submit', function (e) {
        var form = e.target;
        if (!(form instanceof HTMLFormElement) || e.defaultPrevented) return;
        var k = e.submitter;
        var methode = ((k && k.getAttribute('formmethod')) || form.getAttribute('method') || 'get').toLowerCase();
        var ziel = ((k && k.getAttribute('formtarget')) || form.getAttribute('target') || '').toLowerCase();
        if (methode !== 'post' || ziel === '_blank' || form.hasAttribute('data-mehrfach-erlaubt')) return;
        if (form.dataset.wmsSendet === '1') { e.preventDefault(); return; }   // zweites Absenden verworfen
        form.dataset.wmsSendet = '1';
        // Erst nach dem Absenden sperren: sofortiges disabled naehme Name/Wert des ausloesenden Knopfs aus den Formulardaten.
        // Spaeter registrierte Listener (jQuery-Delegation) koennen noch abbrechen – dann nichts sperren.
        setTimeout(function () {
            if (e.defaultPrevented) { delete form.dataset.wmsSendet; return; }
            sperren(knoepfeVon(form));
            form._wmsTimer = setTimeout(function () { formFreigeben(form); }, FREIGABE_MS);
        }, 0);
    });

    window.addEventListener('pageshow', function (e) {
        if (!e.persisted) return;
        Array.prototype.forEach.call(document.querySelectorAll('form[data-wms-sendet]'), formFreigeben);
        freigeben(Array.prototype.slice.call(document.querySelectorAll('[data-wms-gesperrt]')));
    });

    window.wmsSperren = function (knoepfe) {
        var liste = knoepfe instanceof Element ? [knoepfe] : Array.prototype.slice.call(knoepfe);
        if (liste.some(function (k) { return k.dataset.wmsGesperrt === '1'; })) return null;
        sperren(liste);
        var timer = setTimeout(function () { freigeben(liste); }, FREIGABE_MS);
        return function () { clearTimeout(timer); freigeben(liste); };
    };
})();

// v1.99.54: Mengenfelder – Pfeil/Spinner zaehlen ±1 ab dem aktuellen Wert statt um den step (aus 1 wurde 1,001).
// Waehrend der Bedienung step="any" (Browser-Standardschritt 1, ohne Raster), beim Verlassen das Original zurueck –
// die Absende-Pruefung (z. B. hoechstens 3 Nachkommastellen) bleibt. Delegiert am document: gilt auch fuer per JS erzeugte Felder.
// Mausrad im fokussierten Zahlenfeld aendert nie den Wert: Feld verliert den Fokus, die Seite scrollt.
(function () {
    function dezimalFeld(el) {
        if (!(el instanceof HTMLInputElement) || el.type !== 'number' || el.hasAttribute('data-step-orig')) return false;
        var s = el.getAttribute('step');
        return s !== null && s !== 'any' && parseFloat(s) < 1;
    }
    function aufAny(e) {
        var el = e.target;
        if (!dezimalFeld(el)) return;
        el.setAttribute('data-step-orig', el.getAttribute('step'));
        el.setAttribute('step', 'any');
    }
    document.addEventListener('pointerdown', aufAny, true);
    document.addEventListener('focusin', aufAny, true);
    document.addEventListener('focusout', function (e) {
        var el = e.target;
        if (!(el instanceof HTMLInputElement) || !el.hasAttribute('data-step-orig')) return;
        el.setAttribute('step', el.getAttribute('data-step-orig'));
        el.removeAttribute('data-step-orig');
    }, true);
    document.addEventListener('wheel', function (e) {
        var el = e.target;
        if (el instanceof HTMLInputElement && el.type === 'number' && el === document.activeElement) el.blur();
    }, { passive: true, capture: true });
})();
