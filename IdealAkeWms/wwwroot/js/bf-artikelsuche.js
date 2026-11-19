// BF-Warenkorb: Vorschläge beim Tippen am Suchfeld #q (v1.99.45, Spec 2026-11-19-bf-warenkorb-artikelsuche-type-ahead-bug-spec).
// Natives <datalist> (Muster fa-feld.js); der Endpunkt nutzt dieselbe Suche wie die Server-Suche. Hinzufügen bleibt die Server-Trefferzeile.
(function () {
    var input = document.getElementById('q');
    var liste = document.getElementById('bfArtikelVorschlaege');
    if (!input || !liste || !input.form) return;
    var timer = null, seq = 0;

    function vorschlagen() {
        var q = input.value.trim();
        var meine = ++seq;
        if (q.length < 2) { liste.replaceChildren(); return; }
        fetch('/api/bf-auftraege/artikel?limit=20&q=' + encodeURIComponent(q), { credentials: 'same-origin' })
            .then(function (r) { return r.ok ? r.json() : []; })
            .then(function (treffer) {
                if (meine !== seq) return;
                liste.replaceChildren();
                treffer.forEach(function (t) {
                    var o = document.createElement('option');
                    o.value = t.articleNumber;
                    o.textContent = [t.matchcode, t.description].filter(Boolean).join(' · ');
                    liste.appendChild(o);
                });
            })
            .catch(function () { if (meine === seq) liste.replaceChildren(); }); // Vorschläge sind optional, Server-Suche bleibt
    }

    input.addEventListener('input', function (e) {
        clearTimeout(timer);
        // Auswahl aus der Liste → gleich suchen
        if (e.inputType === 'insertReplacementText') { input.form.requestSubmit(); return; }
        timer = setTimeout(vorschlagen, 300);
    });
    // Synthetisches Enter des Hardware-Scanners löst keinen Submit aus → hier suchen. Echte Tastatur-Enter bleibt beim Browser.
    input.addEventListener('keydown', function (e) {
        if (e.key === 'Enter' && !e.isTrusted) { e.preventDefault(); clearTimeout(timer); input.form.requestSubmit(); }
    });
})();
