(function () {
    'use strict';

    const SPINNER_HTML = `
        <tr class="oseon-lazy-spinner">
            <td colspan="100" class="text-center py-3">
                <div class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></div>
                <span class="ms-2">Lade Details...</span>
            </td>
        </tr>`;

    function escapeHtml(s) {
        const div = document.createElement('div');
        div.textContent = s == null ? '' : String(s);
        return div.innerHTML;
    }

    function showError(tbody, msg) {
        tbody.innerHTML =
            '<tr class="oseon-lazy-error">' +
                '<td colspan="100" class="text-center py-3 text-danger">' +
                    'Fehler beim Laden: ' + escapeHtml(msg) +
                    ' <button type="button" class="btn btn-sm btn-outline-secondary ms-2 oseon-lazy-retry">' +
                        'Erneut versuchen' +
                    '</button>' +
                '</td>' +
            '</tr>';
    }

    function getQueryParam(name) {
        const params = new URLSearchParams(window.location.search);
        return params.get(name);
    }

    function findDetailsTbody(customerOrderNumber) {
        // CSS.escape um Sonderzeichen in der Customer-Order-Nummer sicher zu encoden.
        const selector = 'tbody.oseon-group-details[data-customer-order="' +
                         CSS.escape(customerOrderNumber) + '"]';
        return document.querySelector(selector);
    }

    async function loadGroupDetails(customerOrderNumber, tbody) {
        if (tbody.dataset.loaded === 'true' || tbody.dataset.loading === 'true') return;
        tbody.dataset.loading = 'true';
        tbody.innerHTML = SPINNER_HTML;
        tbody.style.display = '';

        try {
            const useRelevance = getQueryParam('useRelevanceFilter') ?? 'true';
            const showFinished = getQueryParam('showFinished') ?? 'false';
            const filterArticle = getQueryParam('filterArticle') ?? '';

            const params = new URLSearchParams();
            params.set('customerOrderNumber', customerOrderNumber);
            params.set('useRelevanceFilter', useRelevance);
            params.set('showFinished', showFinished);
            if (filterArticle) params.set('filterArticle', filterArticle);

            const url = '/Tracking/OseonGroupDetails?' + params.toString();
            const resp = await fetch(url, {
                headers: { 'Accept': 'text/html' },
                credentials: 'same-origin'
            });
            if (!resp.ok) {
                throw new Error('HTTP ' + resp.status);
            }
            const html = await resp.text();
            tbody.innerHTML = html;
            tbody.dataset.loaded = 'true';
        } catch (err) {
            showError(tbody, err.message || String(err));
        } finally {
            tbody.dataset.loading = 'false';
        }
    }

    function toggleGroup(groupRow) {
        const customerOrder = groupRow.dataset.customerOrder;
        if (!customerOrder) return;
        const tbody = findDetailsTbody(customerOrder);
        if (!tbody) return;

        if (tbody.dataset.loaded === 'true') {
            // Toggle visibility on cached content
            tbody.style.display = (tbody.style.display === 'none') ? '' : 'none';
            return;
        }

        // First-time load
        loadGroupDetails(customerOrder, tbody);
    }

    function onClick(e) {
        // Retry-Button im Error-State?
        const retryBtn = e.target.closest('button.oseon-lazy-retry');
        if (retryBtn) {
            const tbody = retryBtn.closest('tbody.oseon-group-details');
            if (tbody) {
                tbody.dataset.loaded = 'false';
                const customerOrder = tbody.dataset.customerOrder;
                if (customerOrder) loadGroupDetails(customerOrder, tbody);
            }
            return;
        }

        // Klick auf Group-Header-Zeile (irgendwo in der Row, ausser auf interaktive Elemente)?
        const groupRow = e.target.closest('tr.oseon-tree-group');
        if (!groupRow) return;

        // Ignoriere Klicks auf Links/Buttons innerhalb der Row (z.B. Bestand-Lookup).
        if (e.target.closest('a, button, input, label, select')) return;

        toggleGroup(groupRow);
    }

    function init() {
        // Document-level Delegation -- funktioniert auch fuer dynamisch geladene Rows.
        document.addEventListener('click', onClick);
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
