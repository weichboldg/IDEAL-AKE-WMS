// Spec 2026-09-25-kommissionierliste-summierung-pdf, Abschnitt B: haengt die zur Klickzeit im DOM
// sichtbaren Spalten (data-col-key) als visibleColumns-Query-Parameter an Drucken-/PDF-Links an,
// BEVOR der Browser der Verlinkung folgt. Kein preventDefault noetig - die href wird vorher
// geaendert, die normale <a href>-Navigation bleibt erhalten. Gemeinsam fuer Index.cshtml und
// Summiert.cshtml (a[data-print-link="<ViewKey>"] -> table[data-view-key="<ViewKey>"]).
(function () {
    'use strict';

    document.addEventListener('DOMContentLoaded', function () {
        document.querySelectorAll('a[data-print-link]').forEach(function (a) {
            a.addEventListener('click', function () {
                var viewKey = a.getAttribute('data-print-link');
                var visible = [];
                document.querySelectorAll('table[data-view-key="' + viewKey + '"] thead th[data-col-key]')
                    .forEach(function (th) {
                        if (th.style.display !== 'none' && !th.classList.contains('d-none')) {
                            visible.push(th.getAttribute('data-col-key'));
                        }
                    });
                if (visible.length === 0) return;
                var url = new URL(a.href);
                url.searchParams.set('visibleColumns', visible.join(','));
                a.href = url.toString();
            });
        });
    });
})();
