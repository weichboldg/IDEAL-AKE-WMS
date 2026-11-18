// v1.99.41 A4/A7: Zeilen „Position ohne FA“ (Partial _BeschichtungFreiePositionen). Jede Zeile = Artikel (Select2, gleiche
// Suche wie _Select2ArticlePartial: /api/articles/search, id = Artikel-Id) + Menge; Felder gehoeren per form-Attribut zum
// Formular aus data-frei-add. Server: Controller TryFreiePositionen (paarweise, leere Zeile uebersprungen).
(function () {
    'use strict';
    var nr = 0;

    function zeile(formId) {
        nr++;
        var artId = 'frei-art-' + nr, mengeId = 'frei-menge-' + nr;
        var $tr = $('<tr>');
        var $sel = $('<select class="form-select form-select-sm">').attr({ id: artId, name: 'freiArtikelId', form: formId });
        var $menge = $('<input class="form-control form-control-sm text-end" inputmode="decimal" placeholder="Menge">')
            .attr({ id: mengeId, name: 'freiMenge', form: formId });
        var $weg = $('<button type="button" class="btn btn-sm btn-outline-danger">Entfernen</button>');
        $tr.append($('<td>').append($('<label class="visually-hidden">').attr('for', artId).text('Artikel'), $sel));
        $tr.append($('<td>').append($('<label class="visually-hidden">').attr('for', mengeId).text('Menge'), $menge));
        $tr.append($('<td class="text-end">').append($weg));
        $weg.on('click', function () {
            var $tab = $tr.closest('table');
            $sel.select2('destroy');
            $tr.remove();
            if (!$tab.find('tbody tr').length) $tab.addClass('d-none');
        });
        return { $tr: $tr, $sel: $sel };
    }

    $(document).on('click', '[data-frei-add]', function () {
        var formId = $(this).data('frei-add');
        var $tab = $('table[data-frei-tabelle="' + formId + '"]');
        var z = zeile(formId);
        $tab.removeClass('d-none').find('tbody').append(z.$tr);
        z.$sel.select2({
            theme: 'bootstrap-5',
            ajax: {
                url: '/api/articles/search', dataType: 'json', delay: 300,
                data: function (p) { return { q: p.term, limit: 50 }; },
                processResults: function (d) { return { results: d }; },
                cache: true
            },
            placeholder: '-- Artikel suchen --', minimumInputLength: 3, allowClear: true, language: 'de', width: '100%'
        });
        z.$sel.select2('open');
    });
})();
