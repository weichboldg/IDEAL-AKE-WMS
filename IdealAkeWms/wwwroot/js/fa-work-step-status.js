// Gemeinsamer AJAX-Handler fuer das 3-State-Status-Dropdown (.fa-status-select) der FA-Vorbau-AGs.
// Verwendet in FA-Abarbeitungsliste (FaWorklist) UND Leitstand (VK-VA).
// Sendet POST /api/fa-work-steps/set-status { faWorkStepId, status }. Fehlerfall: voriger Wert zurueck.
(function () {
    'use strict';

    // Vorigen Wert beim Fokus merken — change feuert erst NACH dem Wechsel.
    document.addEventListener('focus', function (e) {
        var sel = e.target && e.target.closest ? e.target.closest('.fa-status-select') : null;
        if (sel) sel.dataset.prevValue = sel.value;
    }, true);

    document.addEventListener('change', function (e) {
        var sel = e.target.closest('.fa-status-select');
        if (!sel) return;

        var faWorkStepId = parseInt(sel.getAttribute('data-fa-work-step-id'));
        var status = parseInt(sel.value);

        fetch('/api/fa-work-steps/set-status', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ faWorkStepId: faWorkStepId, status: status })
        }).then(function (resp) {
            if (!resp.ok) {
                sel.value = sel.dataset.prevValue || '0';
                alert('Fehler beim Speichern.');
            }
        }).catch(function () {
            sel.value = sel.dataset.prevValue || '0';
            alert('Fehler beim Speichern.');
        });
    });
})();
