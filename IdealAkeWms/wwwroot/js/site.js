// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

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

        // Sentinel am Ende des Wrappers: IntersectionObserver blendet sticky bar aus
        // wenn die echte Scrollbar im Viewport sichtbar wird
        var sentinel = document.createElement('div');
        sentinel.className = 'table-scroll-sentinel';
        sentinel.style.height = '1px';
        sentinel.style.width = '100%';
        sentinel.style.marginTop = '-1px';
        wrapper.appendChild(sentinel);

        var observer = new IntersectionObserver(function (entries) {
            entries.forEach(function (entry) {
                stickyBar.classList.toggle('is-hidden', entry.isIntersecting);
            });
        }, { threshold: 0.1 });
        observer.observe(sentinel);

        // Initiale Sync + laufende Updates
        syncWidths(wrapper, stickyBar, spacer);

        // Bei Resize und nach kurzen Verzoegerungen nochmal synchronisieren
        window.addEventListener('resize', function () {
            syncWidths(wrapper, stickyBar, spacer);
        });

        // Nochmal nach 500ms (fuer spaet geladene Inhalte, Filter-Rows etc.)
        setTimeout(function () { syncWidths(wrapper, stickyBar, spacer); }, 500);
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
