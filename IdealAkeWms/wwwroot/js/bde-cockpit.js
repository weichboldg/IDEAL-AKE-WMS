(function () {
    'use strict';
    const grid = document.getElementById('cockpitGrid');
    const serverTimeEl = document.getElementById('serverTime');

    function colorForBooking(b) {
        switch (b.bookingType) {
            case 'Setup': return 'bg-orange';
            case 'Production': return 'bg-success text-white';
            case 'Activity': return 'bg-info text-white';
            default: return 'bg-secondary text-white';
        }
    }

    function fmt(secs) {
        const h = Math.floor(secs/3600), m = Math.floor((secs%3600)/60);
        return h > 0 ? `${h}h ${m}m` : `${m}m`;
    }

    function render(data) {
        grid.innerHTML = data.workplaces.map(t => {
            if (t.status === 'Idle') {
                return `<div class="col-md-4 col-lg-3"><div class="card text-white bg-secondary">
                    <div class="card-body"><h5>${t.workplaceName}</h5><em>Frei</em></div></div></div>`;
            }
            const bookingHtml = t.bookings.map(b => {
                const target = b.orderNumber
                    ? `${b.orderNumber} / ${b.operationNumber} — ${b.operationName}`
                    : b.activityName;
                return `<div class="cockpit-booking ${colorForBooking(b)} p-2 rounded mb-1">
                    <strong>${b.bookingType}</strong> · ${b.operatorName}
                    <div class="small">${target || ''}</div>
                    <div class="small">Seit: ${fmt(b.runtimeSeconds)}</div>
                </div>`;
            }).join('');
            return `<div class="col-md-4 col-lg-3"><div class="card">
                <div class="card-header"><h5 class="mb-0">${t.workplaceName}</h5></div>
                <div class="card-body p-2">${bookingHtml}</div>
            </div></div>`;
        }).join('');
        serverTimeEl.textContent = new Date(data.serverTime).toLocaleTimeString();
    }

    async function tick() {
        try {
            const r = await fetch('/api/bde/cockpit');
            if (r.ok) render(await r.json());
        } catch (e) { /* ignore transient */ }
    }

    tick();
    setInterval(tick, 5000);
})();
