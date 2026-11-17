// v1.99.32 BDE-Teileliste (Spec 2026-11-13-bde-vereinfacht-teileliste-blechfertigung): Werkbank-Wechsel ueber das Terminal
// (das Terminal entscheidet, ob die Werkbank Teileliste oder Auftragsliste zeigt).
(function () {
    'use strict';
    var sel = document.getElementById('teilelisteWerkbank');
    if (sel) {
        sel.addEventListener('change', function () {
            window.location.href = sel.dataset.ziel + '?workplaceId=' + encodeURIComponent(sel.value);
        });
    }
})();
