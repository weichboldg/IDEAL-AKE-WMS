using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Services.HierarchischeStruktur;

namespace IdealAkeWms.Services.Navigation;

/// <summary>v1.99.45 (ADR 0034): ein Menueziel. <see cref="Kategorie"/> null = nur im Menue, keine Kachel auf der Startseite.
/// v1.99.46: <see cref="SymbolPfad"/> = d-Attribut eines 16x16-Inline-SVG (viewBox 0 0 16 16, nur nonzero-Pfade), Pflicht bei Kachel (Waechter JedeKachel_HatEinSymbol).</summary>
public sealed record NavZiel(string Key, string Titel, string Controller, string Action, string? Kategorie = null, string? Beschreibung = null,
    string? SymbolPfad = null);

/// <summary>Katalog aller Menueziele. Reihenfolge von <see cref="Alle"/> = Kachelreihenfolge; <see cref="Kategorien"/> = Reihenfolge der
/// Startseiten-Abschnitte. Ein neuer Menuepunkt entsteht NUR hier plus eine Regel in <see cref="NavigationSicht"/> (Waechter-Tests).</summary>
public static class NavZiele
{
    public const string KatLager = "Lager", KatBestellungen = "Bestellungen", KatKommissionierung = "Kommissionierung", KatProduktionBde = "Produktion/BDE",
        KatBlechfertigung = "Blechfertigung", KatBeschichtung = "Beschichtung", KatEinkauf = "Einkauf/Wareneingang", KatStammdaten = "Stammdaten",
        KatAdministration = "Administration";

    public static readonly IReadOnlyList<string> Kategorien =
        [KatLager, KatBestellungen, KatKommissionierung, KatProduktionBde, KatBlechfertigung, KatBeschichtung, KatEinkauf, KatStammdaten, KatAdministration];

    // v1.99.46: Kachel-Symbole (Pfade aus den alten Startseiten-SVGs bzw. aus Inline-SVGs im Projekt; Plus/Minus/Doppelpfeil selbst gezeichnet).
    // QA 2026-11-20: file-earmark-pdf (Summiert.cshtml:112) und gear (Settings/Index.cshtml:14-15) zeichnen schon an der Quelle als volle Flaeche
    // (unvollstaendige Pfade) -> nicht verwenden; Kommissionierlisten/Vorschlagsliste = SymFile, Einstellungen = SymPencil.
    // v1.99.50: jedes Kachel-Symbol nur einmal (Waechter KachelSymbole_SindEindeutig); Familien Rahmen = Lagerplatz/Lager, Blatt mit Inhalt = Liste,
    // Tropfen = Beschichtung; Zahnrad vollstaendig aus column-preferences.js:415-416. Selbstgezeichnet: Aussen im, Loecher gegen den Uhrzeigersinn.
    private const string SymPlus = "M7 2h2v5h5v2H9v5H7V9H2V7h5z";
    private const string SymMinus = "M2 7h12v2H2z";
    private const string SymDoppelpfeil = "M1 3h10V1l4 3-4 3V5H1zm14 8H5V9l-4 3 4 3v-2h10z";
    private const string SymCollection = "M2.5 3.5a.5.5 0 0 1 0-1h11a.5.5 0 0 1 0 1zm2-2a.5.5 0 0 1 0-1h7a.5.5 0 0 1 0 1zM0 13a1.5 1.5 0 0 0 1.5 1.5h13A1.5 1.5 0 0 0 16 13V6a1.5 1.5 0 0 0-1.5-1.5h-13A1.5 1.5 0 0 0 0 6zm1.5.5A.5.5 0 0 1 1 13V6a.5.5 0 0 1 .5-.5h13a.5.5 0 0 1 .5.5v7a.5.5 0 0 1-.5.5z";
    private const string SymBox = "M8.186 1.113a.5.5 0 0 0-.372 0L1.846 3.5l2.404.961L10.404 2zm3.564 1.426L5.596 5 8 5.961 14.154 3.5zm3.25 1.7-6.5 2.6v7.922l6.5-2.6V4.24zM7.5 14.762V6.838L1 4.239v7.923zM7.443.184a1.5 1.5 0 0 1 1.114 0l7.129 2.852A.5.5 0 0 1 16 3.5v8.662a1 1 0 0 1-.629.928l-7.185 2.874a.5.5 0 0 1-.372 0L.63 13.09a1 1 0 0 1-.63-.928V3.5a.5.5 0 0 1 .314-.464z";
    private const string SymFileText = "M14 4.5V14a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V2a2 2 0 0 1 2-2h5.5zm-3 0A1.5 1.5 0 0 1 9.5 3V1H4a1 1 0 0 0-1 1v12a1 1 0 0 0 1 1h8a1 1 0 0 0 1-1V4.5z M4.5 12.5A.5.5 0 0 1 5 12h3a.5.5 0 0 1 0 1H5a.5.5 0 0 1-.5-.5m0-2A.5.5 0 0 1 5 10h6a.5.5 0 0 1 0 1H5a.5.5 0 0 1-.5-.5m1.639-3.708 1.33.886 1.854-1.855a.25.25 0 0 1 .289-.047l1.888.974V7.5a.5.5 0 0 1-.5.5H5a.5.5 0 0 1-.5-.5V7s1.54-1.274 1.639-1.208M6.25 6a.75.75 0 1 0 0-1.5.75.75 0 0 0 0 1.5";
    private const string SymUpcScan = "M1.5 1a.5.5 0 0 0-.5.5v3a.5.5 0 0 1-1 0v-3A1.5 1.5 0 0 1 1.5 0h3a.5.5 0 0 1 0 1zM11 .5a.5.5 0 0 1 .5-.5h3A1.5 1.5 0 0 1 16 1.5v3a.5.5 0 0 1-1 0v-3a.5.5 0 0 0-.5-.5h-3a.5.5 0 0 1-.5-.5M.5 11a.5.5 0 0 1 .5.5v3a.5.5 0 0 0 .5.5h3a.5.5 0 0 1 0 1h-3A1.5 1.5 0 0 1 0 14.5v-3a.5.5 0 0 1 .5-.5m15 0a.5.5 0 0 1 .5.5v3a1.5 1.5 0 0 1-1.5 1.5h-3a.5.5 0 0 1 0-1h3a.5.5 0 0 0 .5-.5v-3a.5.5 0 0 1 .5-.5 M3 4.5a.5.5 0 0 1 1 0v7a.5.5 0 0 1-1 0zm2 0a.5.5 0 0 1 1 0v7a.5.5 0 0 1-1 0zm2 0a.5.5 0 0 1 1 0v7a.5.5 0 0 1-1 0zm2 0a.5.5 0 0 1 .5-.5h1a.5.5 0 0 1 .5.5v7a.5.5 0 0 1-.5.5h-1a.5.5 0 0 1-.5-.5zm3 0a.5.5 0 0 1 1 0v7a.5.5 0 0 1-1 0z";
    private const string SymEnvelopeCheck = "M2 2a2 2 0 0 0-2 2v8.01A2 2 0 0 0 2 14h5.5a.5.5 0 0 0 0-1H2a1 1 0 0 1-.966-.741l5.64-3.471L8 9.583l7-4.2V8.5a.5.5 0 0 0 1 0V4a2 2 0 0 0-2-2zm3.708 6.208L1 11.105V5.383zM1 4.217V4a1 1 0 0 1 1-1h12a1 1 0 0 1 1 1v.217l-7 4.2z M16 12.5a3.5 3.5 0 1 1-7 0 3.5 3.5 0 0 1 7 0m-1.993-1.679a.5.5 0 0 0-.686.172l-1.17 1.95-.547-.547a.5.5 0 0 0-.708.708l.774.773a.75.75 0 0 0 1.174-.144l1.335-2.226a.5.5 0 0 0-.172-.686";
    private const string SymCart2 = "M0 2.5A.5.5 0 0 1 .5 2H2a.5.5 0 0 1 .485.379L2.89 4H14.5a.5.5 0 0 1 .485.621l-1.5 6A.5.5 0 0 1 13 11H4a.5.5 0 0 1-.485-.379L1.61 3H.5a.5.5 0 0 1-.5-.5M5.5 13a1 1 0 1 0 0 2 1 1 0 0 0 0-2m7 0a1 1 0 1 0 0 2 1 1 0 0 0 0-2";
    private const string SymWarnung = "M8.982 1.566a1.13 1.13 0 0 0-1.96 0L.165 13.233c-.457.778.091 1.767.98 1.767h13.713c.889 0 1.438-.99.98-1.767zM8 5c.535 0 .954.462.9.995l-.35 3.507a.552.552 0 0 1-1.1 0L7.1 5.995A.905.905 0 0 1 8 5m.002 6a1 1 0 1 1 0 2 1 1 0 0 1 0-2";
    private const string SymCardChecklist = "M14.5 3a.5.5 0 0 1 .5.5v9a.5.5 0 0 1-.5.5h-13a.5.5 0 0 1-.5-.5v-9a.5.5 0 0 1 .5-.5zm-13-1A1.5 1.5 0 0 0 0 3.5v9A1.5 1.5 0 0 0 1.5 14h13a1.5 1.5 0 0 0 1.5-1.5v-9A1.5 1.5 0 0 0 14.5 2z M7 5.5a.5.5 0 0 1 .5-.5h5a.5.5 0 0 1 0 1h-5a.5.5 0 0 1-.5-.5m-1.496-.854a.5.5 0 0 1 0 .708l-1.5 1.5a.5.5 0 0 1-.708 0l-.5-.5a.5.5 0 1 1 .708-.708l.146.147 1.146-1.147a.5.5 0 0 1 .708 0M7 9.5a.5.5 0 0 1 .5-.5h5a.5.5 0 0 1 0 1h-5a.5.5 0 0 1-.5-.5m-1.496-.854a.5.5 0 0 1 0 .708l-1.5 1.5a.5.5 0 0 1-.708 0l-.5-.5a.5.5 0 0 1 .708-.708l.146.147 1.146-1.147a.5.5 0 0 1 .708 0";
    private const string SymGrid = "M2 2a1 1 0 0 0-1 1v3a1 1 0 0 0 1 1h3a1 1 0 0 0 1-1V3a1 1 0 0 0-1-1zm7 0a1 1 0 0 0-1 1v3a1 1 0 0 0 1 1h3a1 1 0 0 0 1-1V3a1 1 0 0 0-1-1zm-7 7a1 1 0 0 0-1 1v3a1 1 0 0 0 1 1h3a1 1 0 0 0 1-1v-3a1 1 0 0 0-1-1zm7 0a1 1 0 0 0-1 1v3a1 1 0 0 0 1 1h3a1 1 0 0 0 1-1v-3a1 1 0 0 0-1-1z";
    private const string SymJournalText = "M5 10.5a.5.5 0 0 1 .5-.5h2a.5.5 0 0 1 0 1h-2a.5.5 0 0 1-.5-.5m0-2a.5.5 0 0 1 .5-.5h5a.5.5 0 0 1 0 1h-5a.5.5 0 0 1-.5-.5m0-2a.5.5 0 0 1 .5-.5h5a.5.5 0 0 1 0 1h-5a.5.5 0 0 1-.5-.5m0-2a.5.5 0 0 1 .5-.5h5a.5.5 0 0 1 0 1h-5a.5.5 0 0 1-.5-.5 M3 0h10a2 2 0 0 1 2 2v12a2 2 0 0 1-2 2H3a2 2 0 0 1-2-2v-1h1v1a1 1 0 0 0 1 1h10a1 1 0 0 0 1-1V2a1 1 0 0 0-1-1H3a1 1 0 0 0-1 1v1H1V2a2 2 0 0 1 2-2 M1 5v-.5a.5.5 0 0 1 1 0V5h.5a.5.5 0 0 1 0 1h-2a.5.5 0 0 1 0-1zm0 3v-.5a.5.5 0 0 1 1 0V8h.5a.5.5 0 0 1 0 1h-2a.5.5 0 0 1 0-1zm0 3v-.5a.5.5 0 0 1 1 0v.5h.5a.5.5 0 0 1 0 1h-2a.5.5 0 0 1 0-1z";
    private const string SymFolder = "M9.828 3h3.982a2 2 0 0 1 1.992 2.181l-.637 7A2 2 0 0 1 13.174 14H2.825a2 2 0 0 1-1.991-1.819l-.637-7a2 2 0 0 1 .342-1.31L2.1 3a2 2 0 0 1 1.542-.819l4.83-.26a2 2 0 0 1 1.356.52z";
    private const string SymPencil = "M12.146.146a.5.5 0 0 1 .708 0l3 3a.5.5 0 0 1 0 .708l-10 10a.5.5 0 0 1-.168.11l-5 2a.5.5 0 0 1-.65-.65l2-5a.5.5 0 0 1 .11-.168zM11.207 2.5 13.5 4.793 14.793 3.5 12.5 1.207zm1.586 3L10.5 3.207 4 9.707V10h.5a.5.5 0 0 1 .5.5v.5h.5a.5.5 0 0 1 .5.5v.5h.293zm-9.761 5.175-.106.106-1.528 3.821 3.821-1.528.106-.106A.5.5 0 0 1 5 12.5V12h-.5a.5.5 0 0 1-.5-.5V11h-.5a.5.5 0 0 1-.468-.325";
    private const string SymCheckSquare = "M10.97 4.97a.75.75 0 0 1 1.07 1.05l-3.99 4.99a.75.75 0 0 1-1.08.02L4.324 8.384a.75.75 0 1 1 1.06-1.06l2.094 2.093 3.473-4.425z M14 1a1 1 0 0 1 1 1v12a1 1 0 0 1-1 1H2a1 1 0 0 1-1-1V2a1 1 0 0 1 1-1zM2 0a2 2 0 0 0-2 2v12a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V2a2 2 0 0 0-2-2z";
    private const string SymTerminal = "M0 4a2 2 0 0 1 2-2h12a2 2 0 0 1 2 2v8a2 2 0 0 1-2 2H2a2 2 0 0 1-2-2zm6.5 7a.5.5 0 0 0 0 1h3a.5.5 0 0 0 0-1zm-3.354-3.354a.5.5 0 0 0 0 .708l1.5 1.5a.5.5 0 1 0 .708-.708L4.207 8l1.147-1.146a.5.5 0 1 0-.708-.708z";
    private const string SymFile = "M14 14V4.5L9.5 0H4a2 2 0 0 0-2 2v12a2 2 0 0 0 2 2h8a2 2 0 0 0 2-2M9.5 3A1.5 1.5 0 0 0 11 4.5h2V14a1 1 0 0 1-1 1H4a1 1 0 0 1-1-1V2a1 1 0 0 1 1-1h5.5z";
    private const string SymGeoAlt = "M8 16s6-5.686 6-10A6 6 0 0 0 2 6c0 4.314 6 10 6 10m0-7a3 3 0 1 1 0-6 3 3 0 0 1 0 6";
    private const string SymBarChart = "M4 11H2v3h2zm5-4H7v7h2zm5-5v12h-2V2zm-2-1a1 1 0 0 0-1 1v12a1 1 0 0 0 1 1h2a1 1 0 0 0 1-1V2a1 1 0 0 0-1-1zM6 7a1 1 0 0 1 1-1h2a1 1 0 0 1 1 1v7a1 1 0 0 1-1 1H7a1 1 0 0 1-1-1zm-5 4a1 1 0 0 1 1-1h2a1 1 0 0 1 1 1v3a1 1 0 0 1-1 1H2a1 1 0 0 1-1-1z";
    private const string SymCart = "M0 1.5A.5.5 0 0 1 .5 1H2a.5.5 0 0 1 .485.379L2.89 3H14.5a.5.5 0 0 1 .491.592l-1.5 8A.5.5 0 0 1 13 12H4a.5.5 0 0 1-.491-.408L2.01 3.607 1.61 2H.5a.5.5 0 0 1-.5-.5M3.102 4l1.313 7h8.17l1.313-7zM5 12a2 2 0 1 0 0 4 2 2 0 0 0 0-4m7 0a2 2 0 1 0 0 4 2 2 0 0 0 0-4m-7 1a1 1 0 1 1 0 2 1 1 0 0 1 0-2m7 0a1 1 0 1 1 0 2 1 1 0 0 1 0-2";
    private const string SymSearch = "M11.742 10.344a6.5 6.5 0 1 0-1.397 1.398h-.001q.044.06.098.115l3.85 3.85a1 1 0 0 0 1.415-1.414l-3.85-3.85a1 1 0 0 0-.115-.099zM12 6.5a5.5 5.5 0 1 1-11 0 5.5 5.5 0 0 1 11 0";
    private const string SymLock = "M8 1a2 2 0 0 1 2 2v4H6V3a2 2 0 0 1 2-2m3 6V3a3 3 0 0 0-6 0v4a2 2 0 0 0-2 2v5a2 2 0 0 0 2 2h6a2 2 0 0 0 2-2V9a2 2 0 0 0-2-2";
    private const string SymPrinter = "M2.5 8a.5.5 0 1 0 0-1 .5.5 0 0 0 0 1 M5 1a2 2 0 0 0-2 2v2H2a2 2 0 0 0-2 2v3a2 2 0 0 0 2 2h1v1a2 2 0 0 0 2 2h6a2 2 0 0 0 2-2v-1h1a2 2 0 0 0 2-2V7a2 2 0 0 0-2-2h-1V3a2 2 0 0 0-2-2zM4 3a1 1 0 0 1 1-1h6a1 1 0 0 1 1 1v2H4zm1 10a1 1 0 0 1-1-1v-2h8v2a1 1 0 0 1-1 1z";
    private const string SymStopwatch = "M6 .5a.5.5 0 0 1 .5-.5h3a.5.5 0 0 1 0 1H9v1.07a7.001 7.001 0 0 1 3.274 12.474l.601.602a.5.5 0 0 1-.707.708l-.746-.746A6.97 6.97 0 0 1 8 16a6.97 6.97 0 0 1-3.422-.892l-.746.746a.5.5 0 0 1-.707-.708l.602-.602A7.001 7.001 0 0 1 7 2.07V1h-.5A.5.5 0 0 1 6 .5M8 3a6 6 0 1 0 .001 12A6 6 0 0 0 8 3m0 3a.75.75 0 0 1 .75.75v2.5h1.5a.75.75 0 0 1 0 1.5h-2.25A.75.75 0 0 1 7.25 10V6.75A.75.75 0 0 1 8 6";
    private const string SymLink = "M4.715 6.542 3.343 7.914a3 3 0 1 0 4.243 4.243l1.828-1.829A3 3 0 0 0 8.586 5.5L8 6.086a1 1 0 0 0-.154.199 2 2 0 0 1 .861 3.337L6.88 11.45a2 2 0 1 1-2.83-2.83l.793-.792a4 4 0 0 1-.128-1.287z M6.586 4.672A3 3 0 0 0 7.414 9.5l.775-.776a2 2 0 0 1-.896-3.346L9.12 3.55a2 2 0 1 1 2.83 2.83l-.793.792c.112.42.155.855.128 1.287l1.372-1.372a3 3 0 1 0-4.243-4.243z";
    // v1.99.50: Familien aus Bausteinen (const-Verkettung, jeder Rahmen-/Blatt-Pfad steht nur einmal)
    private const string SymFachRahmen = "M1 2h14v12H1zm1 1v10h12V3z";
    private const string SymLagerplatzAusbuchen = SymFachRahmen + " M4 7h8v2H4z";
    private const string SymLagerplatzUmbuchen = SymFachRahmen + " M4 7h4V5l4 3-4 3V9H4z";
    private const string SymLagerFehlteile = SymFachRahmen + " M7 4h2v5H7zm0 6h2v2H7z";
    private const string SymLagerplaetze = SymFachRahmen + " M7.5 3h1v10h-1zM2 7.5h12v1H2z";
    private const string SymListeSumme = SymFile + " M5 5h6v1.5H7.3l2 2.5-2 2.5H11V13H5v-1l2.5-3L5 6z";
    private const string SymListeZeilen = SymFile + " M5 6h6v1H5zm0 2.5h6v1H5zm0 2.5h6v1H5zm0 2.5h4v1H5z";
    private const string SymListeTropfen = SymFile + " M8 6c-1 1.5-2.5 3-2.5 4.75a2.5 2.5 0 0 0 5 0C10.5 9 9 7.5 8 6z";
    private const string SymTropfen = "M8 1C6 4 3 7 3 10a5 5 0 0 0 10 0c0-3-3-6-5-9z";
    private const string SymEingangskorb = "M7 1h2v4h2.5L8 8.5 4.5 5H7zM1 9h3l1.5 2h5L12 9h3v6H1z";
    private const string SymTacho = "M8 3a7 7 0 0 0-7 7v2h2v-2a5 5 0 0 1 10 0v2h2v-2a7 7 0 0 0-7-7zM8 8.5a1.5 1.5 0 1 1 0 3 1.5 1.5 0 0 1 0-3M7.5 9.5l3-3 .7.7-3 3z";
    private const string SymRegal = "M1 1h1v14H1zm13 0h1v14h-1zM2 5h12v1H2zm0 5h12v1H2zm0 4h12v1H2zM3 2h3v3H3zm5 1h4v2H8zM4 7h4v3H4zm6 1h3v2h-3zM3 12h3v2H3zm5-.5h5V14H8z";
    private const string SymUhr = "M8 1a7 7 0 1 0 0 14A7 7 0 0 0 8 1m0 1.5a5.5 5.5 0 1 1 0 11 5.5 5.5 0 0 1 0-11M7.25 4h1.5v4.25l2.5 1.5-.75 1.3-3.25-2z";
    private const string SymWerkbank = "M0 5h16v2H0zm1 2h2v8H1zm12 0h2v8h-2zM3 11h10v1H3zM5 2h6v3H5z";
    private const string SymSanduhr = "M3 1h10v2h-1c0 2.5-2 4-3 5 1 1 3 2.5 3 5h1v2H3v-2h1c0-2.5 2-4 3-5-1-1-3-2.5-3-5H3z";
    private const string SymGluehbirne = "M8 1a5 5 0 0 0-3 9v2h6v-2a5 5 0 0 0-3-9M5.5 13h5v1h-5zm1 1.5h3V16h-3z";
    private const string SymSchluessel = "M5 3a4 4 0 1 0 0 8 4 4 0 0 0 0-8m0 2.5a1.5 1.5 0 1 1 0 3 1.5 1.5 0 0 1 0-3M8.5 6v2H10v1.5h2V8h1v2h2V8h1V6z";
    private const string SymBlechtafel = "M4 4h11l-3 8H1zM5 7a1 1 0 1 0 0 2 1 1 0 0 0 0-2m6 0a1 1 0 1 0 0 2 1 1 0 0 0 0-2";
    private const string SymLkw = "M0 3h10v9H0zm11 3h2.5l2.5 3v3h-5zM3.5 11.5a1.75 1.75 0 1 1 0 3.5 1.75 1.75 0 0 1 0-3.5m8 0a1.75 1.75 0 1 1 0 3.5 1.75 1.75 0 0 1 0-3.5";
    private const string SymKalender = "M3.5 0a.5.5 0 0 1 .5.5V1h8V.5a.5.5 0 0 1 1 0V1h1a2 2 0 0 1 2 2v11a2 2 0 0 1-2 2H2a2 2 0 0 1-2-2V3a2 2 0 0 1 2-2h1V.5a.5.5 0 0 1 .5-.5M1 4v10a1 1 0 0 0 1 1h12a1 1 0 0 0 1-1V4z";
    private const string SymZahnrad = "M8 4.754a3.246 3.246 0 1 0 0 6.492 3.246 3.246 0 0 0 0-6.492zM5.754 8a2.246 2.246 0 1 1 4.492 0 2.246 2.246 0 0 1-4.492 0z M9.796 1.343c-.527-1.79-3.065-1.79-3.592 0l-.094.319a.873.873 0 0 1-1.255.52l-.292-.16c-1.64-.892-3.433.902-2.54 2.541l.159.292a.873.873 0 0 1-.52 1.255l-.319.094c-1.79.527-1.79 3.065 0 3.592l.319.094a.873.873 0 0 1 .52 1.255l-.16.292c-.892 1.64.901 3.434 2.541 2.54l.292-.159a.873.873 0 0 1 1.255.52l.094.319c.527 1.79 3.065 1.79 3.592 0l.094-.319a.873.873 0 0 1 1.255-.52l.292.16c1.64.893 3.434-.902 2.54-2.541l-.159-.292a.873.873 0 0 1 .52-1.255l.319-.094c1.79-.527 1.79-3.065 0-3.592l-.319-.094a.873.873 0 0 1-.52-1.255l.16-.292c.893-1.64-.902-3.433-2.541-2.54l-.292.159a.873.873 0 0 1-1.255-.52l-.094-.319zm-2.633.283c.246-.835 1.428-.835 1.674 0l.094.319a1.873 1.873 0 0 0 2.693 1.115l.291-.16c.764-.415 1.6.42 1.184 1.185l-.159.292a1.873 1.873 0 0 0 1.116 2.692l.318.094c.835.246.835 1.428 0 1.674l-.319.094a1.873 1.873 0 0 0-1.115 2.693l.16.291c.415.764-.42 1.6-1.185 1.184l-.291-.159a1.873 1.873 0 0 0-2.693 1.116l-.094.318c-.246.835-1.428.835-1.674 0l-.094-.319a1.873 1.873 0 0 0-2.692-1.115l-.292.16c-.764.415-1.6-.42-1.184-1.185l.159-.291A1.873 1.873 0 0 0 1.945 8.93l-.319-.094c-.835-.246-.835-1.428 0-1.674l.319-.094A1.873 1.873 0 0 0 3.06 4.474l-.16-.292c-.415-.764.42-1.6 1.185-1.184l.292.159a1.873 1.873 0 0 0 2.692-1.115l.094-.319z";
    private const string SymHaus = "M6.5 14.5v-3.505c0-.245.20-.495.5-.495h2c.294 0 .5.25.5.495v3.505a.5.5 0 0 0 .5.5h4a.5.5 0 0 0 .5-.5v-7a.5.5 0 0 0-.146-.354L13 5.793V2.5a.5.5 0 0 0-.5-.5h-1a.5.5 0 0 0-.5.5v1.293L8.354 1.146a.5.5 0 0 0-.708 0l-6 6A.5.5 0 0 0 1.5 7.5v7a.5.5 0 0 0 .5.5h4a.5.5 0 0 0 .5-.5";

    // ---- Lager
    public static readonly NavZiel Einbuchung = new("Einbuchung", "Einbuchung", "StockMovements", "Inbound", KatLager, "Ware auf einen Lagerplatz einbuchen", SymPlus);
    public static readonly NavZiel MehrfachEinbuchung = new("MehrfachEinbuchung", "Mehrfach-Einbuchung", "StockMovements", "InboundBulk", KatLager, "Mehrere Artikel in einem Zug einbuchen", SymCollection);
    public static readonly NavZiel Ausbuchung = new("Ausbuchung", "Ausbuchung", "StockMovements", "Outbound", KatLager, "Ware von einem Lagerplatz ausbuchen", SymMinus);
    public static readonly NavZiel Umbuchung = new("Umbuchung", "Umbuchung", "StockMovements", "Transfer", KatLager, "Ware auf einen anderen Lagerplatz umbuchen", SymDoppelpfeil);
    public static readonly NavZiel Bestaende = new("Bestaende", "Bestände", "StockOverview", "Index", KatLager, "Aktuelle Bestände je Artikel und Lagerplatz", SymRegal);
    public static readonly NavZiel Bewegungshistorie = new("Bewegungshistorie", "Bewegungshistorie", "StockMovements", "Index", KatLager, "Alle Lagerbewegungen", SymFileText);
    public static readonly NavZiel Artikelinfo = new("Artikelinfo", "Artikelinfo", "Articles", "Info", KatLager, "QR-Code scannen: Details und Bestand", SymUpcScan);
    public static readonly NavZiel LagerplatzAusbuchen = new("LagerplatzAusbuchen", "Lagerplatz ausbuchen", "StockMovements", "OutboundAll", KatLager, "Einen ganzen Lagerplatz leeren", SymLagerplatzAusbuchen);
    public static readonly NavZiel LagerplatzUmbuchen = new("LagerplatzUmbuchen", "Lagerplatz umbuchen", "StockMovements", "LocationTransfer", KatLager, "Einen ganzen Lagerplatz umziehen", SymLagerplatzUmbuchen);
    public static readonly NavZiel SageLagerbuchungen = new("SageLagerbuchungen", "Sage-Lagerbuchungen", "SageBookingQueue", "Index");
    public static readonly NavZiel OffeneBestellungenLager = new("OffeneBestellungenLager", "Offene Bestellungen", "PurchaseOrders", "Index", KatEinkauf, "Offene Sage-Bestellungen für den Wareneingang", SymSanduhr);
    public static readonly NavZiel WareneingangZuBestellung = new("WareneingangZuBestellung", "Wareneingang zu Bestellung", "PurchaseOrders", "Receive", KatEinkauf, "Gelieferte Ware zur Bestellung buchen", SymLkw);
    public static readonly NavZiel SageWareneingaengeLager = new("SageWareneingaengeLager", "Sage-Wareneingänge", "PurchaseOrders", "SageWareneingaenge");

    // ---- Bestellungen
    public static readonly NavZiel Bedarfsmeldungen = new("Bedarfsmeldungen", "Bedarfsmeldungen", "PartRequisitions", "Index", KatBestellungen, "Bedarf an die Empfänger melden", SymEnvelopeCheck);
    public static readonly NavZiel Lagerbestellungen = new("Lagerbestellungen", "Lagerbestellungen", "WarehouseRequisitions", "Index", KatBestellungen, "Ware beim Lager bestellen", SymCart2);
    public static readonly NavZiel MeineFehlteile = new("MeineFehlteile", "Meine Fehlteile", "MissingParts", "Index", KatBestellungen, "Offene Fehlteile eigener Bestellungen", SymWarnung);
    public static readonly NavZiel LagerEingehendeListen = new("LagerEingehendeListen", "Lager: Eingehende Listen", "WarehousePicking", "Index", KatBestellungen, "Bestellungen an das Lager bearbeiten", SymEingangskorb);
    public static readonly NavZiel LagerFehlteile = new("LagerFehlteile", "Lager: Fehlteile", "MissingPartsLager", "Index", KatBestellungen, "Fehlteile aller Bestellungen nachliefern", SymLagerFehlteile);

    // ---- Kommissionierung
    public static readonly NavZiel Kommissionierung = new("Kommissionierung", "Kommissionierung", "Picking", "Index", KatKommissionierung, "Fertigungsaufträge kommissionieren", SymCardChecklist);
    public static readonly NavZiel Kommissionierlisten = new("Kommissionierlisten", "Kommissionierlisten", "FaHierarchyKommissionierListen", "Summiert", KatKommissionierung, "Material je Haupt-FA summiert", SymListeSumme);
    public static readonly NavZiel LeitstandKommissionierung = new("LeitstandKommissionierung", "Leitstand Kommissionierung", "PickingLeitstand", "Index", KatKommissionierung, "Aufträge freigeben und priorisieren", SymGrid);

    // ---- Produktion/BDE
    public static readonly NavZiel FaListe = new("FaListe", "FA-Liste", "ProductionOrders", "Index", KatProduktionBde, "Fertigungsaufträge mit Terminen", SymJournalText);
    public static readonly NavZiel FaStruktur = new("FaStruktur", "FA-Struktur", "FaHierarchy", "Index", KatProduktionBde, "Haupt- und Unter-FAs als Baum", SymFolder);
    public static readonly NavZiel FaVervollstaendigen = new("FaVervollstaendigen", "FA-Vervollständigen", "FaCompletion", "Index", KatProduktionBde, "Fehlende FA-Angaben ergänzen", SymPencil);
    public static readonly NavZiel FaAbarbeitungsliste = new("FaAbarbeitungsliste", "FA-Abarbeitungsliste", "FaWorklist", "Index", KatProduktionBde, "Arbeitsschritte je Werkbank abarbeiten", SymCheckSquare);
    public static readonly NavZiel Vormontage = new("Vormontage", "Vormontage", "FaHierarchyVormontage", "Index", KatProduktionBde, "Vormontage-Listen je Abteilung", SymBox);
    public static readonly NavZiel BdeTerminal = new("BdeTerminal", "Terminal", "BdeTerminal", "Index", KatProduktionBde, "BDE: Arbeitsgänge buchen", SymTerminal);
    public static readonly NavZiel BdeCockpit = new("BdeCockpit", "Cockpit", "BdeCockpit", "Index", KatProduktionBde, "BDE: laufende Arbeit im Überblick", SymTacho);
    public static readonly NavZiel BdeBuchungen = new("BdeBuchungen", "Buchungen", "BdeBookings", "Index", KatProduktionBde, "BDE: Buchungen prüfen und korrigieren", SymListeZeilen);
    public static readonly NavZiel BdeStammdaten = new("BdeStammdaten", "Stammdaten", "BdeMasterData", "Index");
    public static readonly NavZiel BdeSchichtkalender = new("BdeSchichtkalender", "Schichtkalender", "BdeShiftCalendar", "Index");
    public static readonly NavZiel TeileverfolgungRueckmeldungen = new("TeileverfolgungRueckmeldungen", "Rückmeldungen", "Tracking", "Index");
    public static readonly NavZiel TeileverfolgungOseon = new("TeileverfolgungOseon", "OSEON Aufträge", "Tracking", "OseonIndex", KatProduktionBde, "Teileverfolgung: OSEON-Aufträge", SymGeoAlt);
    public static readonly NavZiel OseonAgUebersicht = new("OseonAgUebersicht", "OSEON AG-Übersicht", "OseonReporting", "OperationsOverview", KatProduktionBde, "Reporting: Arbeitsgänge je Zeitraum", SymBarChart);

    // ---- Blechfertigung
    public static readonly NavZiel BfWarenkorb = new("BfWarenkorb", "BF-Warenkorb", "BfAuftraege", "Warenkorb", KatBlechfertigung, "Blechteile bestellen", SymCart);
    public static readonly NavZiel BfAuftraege = new("BfAuftraege", "BF-Aufträge", "BfAuftraege", "Index", KatBlechfertigung, "Bestellte Blechteile verfolgen", SymBlechtafel);

    // ---- Beschichtung
    public static readonly NavZiel BeschichtungVorschlagsliste = new("BeschichtungVorschlagsliste", "Vorschlagsliste", "FaHierarchyBeschichtung", "Summiert", KatBeschichtung, "Zu beschichtende Teile summiert", SymListeTropfen);
    public static readonly NavZiel Beschichtungsauftraege = new("Beschichtungsauftraege", "Aufträge", "Beschichtungsauftraege", "Index", KatBeschichtung, "Beschichtungsaufträge an den Beschichter", SymTropfen);

    // ---- Einkauf/Wareneingang
    public static readonly NavZiel Bestellvorschlaege = new("Bestellvorschlaege", "Bestellvorschläge", "PurchaseProposals", "Index", KatEinkauf, "Bestellvorschläge prüfen und bestellen", SymGluehbirne);
    public static readonly NavZiel OffeneBestellungenEinkauf = new("OffeneBestellungenEinkauf", "Offene Bestellungen (Einkauf)", "EinkaufPurchaseOrders", "Index", KatEinkauf, "Offene Sage-Bestellungen des Einkaufs", SymKalender);
    public static readonly NavZiel SageWareneingaengeEinkauf = new("SageWareneingaengeEinkauf", "Sage-Wareneingänge (offen)", "EinkaufPurchaseOrders", "SageWareneingaenge");

    // ---- Stammdaten
    public static readonly NavZiel Artikel = new("Artikel", "Artikel", "Articles", "Index", KatStammdaten, "Artikelstamm aus Sage", SymSearch);
    public static readonly NavZiel Lagerplaetze = new("Lagerplaetze", "Lagerplätze", "StorageLocations", "Index", KatStammdaten, "Lagerplätze pflegen", SymLagerplaetze);
    public static readonly NavZiel Werkbaenke = new("Werkbaenke", "Werkbänke", "ProductionWorkplaces", "Index", KatStammdaten, "Werkbänke der Fertigung", SymWerkbank);
    public static readonly NavZiel Artikelkategorien = new("Artikelkategorien", "Artikelkategorien", "ArticleCategories", "Index");
    public static readonly NavZiel Artikelmerkmale = new("Artikelmerkmale", "Artikelmerkmale", "ArticleAttributes", "Index");
    public static readonly NavZiel FaVorbauAg = new("FaVorbauAg", "FA-Vorbau-AG", "WorkSteps", "Index");
    public static readonly NavZiel Kommissionierbereiche = new("Kommissionierbereiche", "Kommissionierbereiche", "PickingAreas", "Index");
    public static readonly NavZiel FaMerkmale = new("FaMerkmale", "FA-Merkmale", "FaAttributes", "Index");
    public static readonly NavZiel Kreditoren = new("Kreditoren", "Kreditoren", "Creditors", "Index");
    public static readonly NavZiel Debitoren = new("Debitoren", "Debitoren", "Debitors", "Index");
    public static readonly NavZiel Empfaenger = new("Empfaenger", "Empfänger", "OrderRecipients", "Index");
    public static readonly NavZiel ArtikelgruppenZuordnung = new("ArtikelgruppenZuordnung", "Artikelgruppen-Zuordnung", "OrderRecipients", "ArticleGroupMappings");

    // ---- Administration (im Menue unter Stammdaten)
    public static readonly NavZiel Benutzer = new("Benutzer", "Benutzer", "Users", "Index", KatAdministration, "Benutzer und Rollen zuweisen", SymLock);
    public static readonly NavZiel Arbeitsplaetze = new("Arbeitsplaetze", "Arbeitsplätze", "Workstations", "Index", KatAdministration, "Rechner und Drucker zuordnen", SymPrinter);
    public static readonly NavZiel Rollen = new("Rollen", "Rollen", "Roles", "Index", KatAdministration, "Rollen und ihre Bedeutung", SymSchluessel);
    public static readonly NavZiel Arbeitsgaenge = new("Arbeitsgaenge", "Arbeitsgänge", "OperationConfigs", "Index", KatAdministration, "Arbeitsgang-Stammdaten", SymStopwatch);
    public static readonly NavZiel ArbeitsgaengeSage = new("ArbeitsgaengeSage", "Arbeitsgänge → Sage", "SageFaArbeitsgaenge", "Index", KatAdministration, "Arbeitsgänge in Sage-FAs anlegen", SymLink);
    public static readonly NavZiel Einstellungen = new("Einstellungen", "Einstellungen", "Settings", "Index", KatAdministration, "Module und fachliche Einstellungen", SymZahnrad);
    public static readonly NavZiel ServiceEinstellungen = new("ServiceEinstellungen", "Service-Einstellungen", "ServiceSettings", "Index");
    public static readonly NavZiel Standorteinstellungen = new("Standorteinstellungen", "Standorteinstellungen", "StandortEinstellungen", "Index", KatAdministration, "Einstellungen je Standort", SymHaus);
    public static readonly NavZiel AktivitaetsProtokoll = new("AktivitaetsProtokoll", "Aktivitäts-Protokoll", "SyncLog", "Index", KatAdministration, "Läufe der Hintergrunddienste", SymUhr);

    /// <summary>Alle Ziele in Kachelreihenfolge (innerhalb der Kategorie).</summary>
    public static readonly IReadOnlyList<NavZiel> Alle =
    [
        Einbuchung, MehrfachEinbuchung, Ausbuchung, Umbuchung, Bestaende, Bewegungshistorie, Artikelinfo, LagerplatzAusbuchen, LagerplatzUmbuchen,
        SageLagerbuchungen, OffeneBestellungenLager, WareneingangZuBestellung, SageWareneingaengeLager,
        Bedarfsmeldungen, Lagerbestellungen, MeineFehlteile, LagerEingehendeListen, LagerFehlteile,
        Kommissionierung, Kommissionierlisten, LeitstandKommissionierung,
        FaListe, FaStruktur, FaVervollstaendigen, FaAbarbeitungsliste, Vormontage, BdeTerminal, BdeCockpit, BdeBuchungen, BdeStammdaten,
        BdeSchichtkalender, TeileverfolgungRueckmeldungen, TeileverfolgungOseon, OseonAgUebersicht,
        BfWarenkorb, BfAuftraege, BeschichtungVorschlagsliste, Beschichtungsauftraege,
        Bestellvorschlaege, OffeneBestellungenEinkauf, SageWareneingaengeEinkauf,
        Artikel, Lagerplaetze, Werkbaenke, Artikelkategorien, Artikelmerkmale, FaVorbauAg, Kommissionierbereiche, FaMerkmale, Kreditoren, Debitoren,
        Empfaenger, ArtikelgruppenZuordnung,
        Benutzer, Arbeitsplaetze, Rollen, Arbeitsgaenge, ArbeitsgaengeSage, Einstellungen, ServiceEinstellungen, Standorteinstellungen, AktivitaetsProtokoll,
    ];
}

/// <summary>Ergebnis fuer einen Request: welche Ziele sichtbar sind.</summary>
public sealed class NavigationErgebnis(IReadOnlySet<string> sichtbar, bool leitstandAktiv)
{
    /// <summary>Fuer das Badge an „Kommissionierung“ (_Layout).</summary>
    public bool LeitstandAktiv { get; } = leitstandAktiv;

    public bool Zeigt(NavZiel z) => sichtbar.Contains(z.Key);

    /// <summary>Dropdown nur, wenn mindestens ein Eintrag sichtbar ist.</summary>
    public bool ZeigtEines(params NavZiel[] ziele) => ziele.Any(Zeigt);

    /// <summary>Startseite: nur nicht-leere Kategorien, in fester Reihenfolge.</summary>
    public IReadOnlyList<(string Kategorie, IReadOnlyList<NavZiel> Kacheln)> Kacheln => NavZiele.Kategorien
        .Select(k => (k, (IReadOnlyList<NavZiel>)NavZiele.Alle.Where(z => z.Kategorie == k && Zeigt(z)).ToList()))
        .Where(x => x.Item2.Count > 0).ToList();

    /// <summary>v1.99.51: gespeicherte Favoriten-Keys auswerten. Sichtbar = Kacheln in Katalogreihenfolge, die auch im Reiter „Alle“ stuenden
    /// (nie mehr als die Startseite). NichtVerfuegbar = uebrige Keys als Anzeigetext (Titel bzw. Key + Hinweis), doppelte einmal.</summary>
    public (IReadOnlyList<NavZiel> Sichtbar, IReadOnlyList<string> NichtVerfuegbar) Favoriten(IReadOnlyCollection<string> keys)
    {
        var sichtbar = NavZiele.Alle.Where(z => z.Kategorie != null && keys.Contains(z.Key) && Zeigt(z)).ToList();
        var nicht = keys.Distinct().Where(k => !sichtbar.Any(z => z.Key == k))
            .Select(k => NavZiele.Alle.FirstOrDefault(z => z.Key == k)?.Titel ?? $"{k} (nicht mehr vorhandene Funktion)").ToList();
        return (sichtbar, nicht);
    }
}

/// <summary>v1.99.45 (ADR 0034): EINE Sichtbarkeitsregel je Menueziel fuer Menue (_Layout) und Startseite (Home/Index).
/// Rechte ueber dieselben <see cref="ICurrentUserService"/>-Methoden wie die Filter am Ziel, Modulschalter mit derselben Lese-Semantik
/// wie vorher im Menue (ADR 0011). Scoped: einmal je Request berechnet.</summary>
public sealed class NavigationSicht(ICurrentUserService user, IAppSettingRepository appSettings, IServiceSettingRepository serviceSettings)
{
    private NavigationErgebnis? _ergebnis;

    public async Task<NavigationErgebnis> GetAsync()
    {
        if (_ergebnis != null) return _ergebnis;

        async Task<bool> An(string key) => string.Equals(await appSettings.GetValueAsync(key), "true", StringComparison.OrdinalIgnoreCase);

        var stock = await user.CanAccessStockAsync();
        var stockRead = await user.CanAccessStockReadAsync();
        var processLager = await user.CanProcessLagerAsync();
        var pick = await user.CanPickAsync();
        var lagerOderGlas = await user.CanAccessLagerbestellungAsync() || await user.CanAccessGlasbestellungAsync();
        var tracking = await user.CanViewTrackingAsync();
        var release = await user.CanManagePickingReleaseAsync();
        var vorbau = await user.HasVorbauAccessAsync();
        var useBde = await user.CanUseBdeAsync();
        var shiftlead = await user.CanManageBdeShiftleadAsync();
        var admin = await user.IsAdminAsync();
        var mdRead = await user.HasMasterDataReadAccessAsync();
        var faLesen = await user.CanViewProductionOrdersAsync();
        var transfer = await user.CanTransferStockAsync();
        var completion = await user.CanFaCompletionAsync();
        var reporting = await user.CanReportOperationsAsync();
        var beschichtungRecht = await user.HasBeschichtungsauftragAccessAsync();

        var teileverfolgung = await An(AppSettingKeys.TeileverfolgungAktiv);
        var bestellungen = await An(AppSettingKeys.BestellungenAktiv);
        // LagerbestellungAktiv: Default an, nur exakt "false" sperrt (ADR 0011)
        var lagerbestellung = !string.Equals(await appSettings.GetValueAsync(AppSettingKeys.LagerbestellungAktiv), "false", StringComparison.OrdinalIgnoreCase);
        var leitstand = await An(AppSettingKeys.LeitstandAktiv);
        var bde = await An(AppSettingKeys.BdeAktiv);
        var faCompletion = await An(AppSettingKeys.FaCompletionAktiv);
        var kommListen = await An(AppSettingKeys.FaHierarchyKommissionierlistenAktiv);
        var beschichtung = await An(AppSettingKeys.FaHierarchyBeschichtungAktiv);
        var vormontage = await An(AppSettingKeys.FaHierarchyVormontageAktiv);
        var wareneingang = await An(AppSettingKeys.WareneingangBestellungAktiv);
        var einkauf = await An(AppSettingKeys.EinkaufAktiv) && await user.CanAccessEinkaufAsync();
        var blechAn = await An(AppSettingKeys.BlechfertigungAktiv);
        var blech = blechAn && await user.CanBfBestellenAsync();
        // Master nur fuer Admins lesen (wie vorher im Menue)
        var master = admin && string.Equals(await serviceSettings.GetValueAsync(HierarchischeStrukturKeys.Master), "true", StringComparison.OrdinalIgnoreCase);
        var lagerbestellungSicht = lagerbestellung && (stock || lagerOderGlas);

        bool Regel(string key) => key switch
        {
            "Einbuchung" or "MehrfachEinbuchung" or "Ausbuchung" or "Umbuchung" => stock,
            "Bestaende" or "Bewegungshistorie" or "SageLagerbuchungen" => stockRead,
            "Artikelinfo" => mdRead,
            "LagerplatzAusbuchen" or "LagerplatzUmbuchen" => transfer,
            "OffeneBestellungenLager" or "SageWareneingaengeLager" => wareneingang && stockRead,
            "WareneingangZuBestellung" => wareneingang && stock,
            "Bedarfsmeldungen" => bestellungen && stock,
            "Lagerbestellungen" or "MeineFehlteile" => lagerbestellungSicht,
            "LagerEingehendeListen" or "LagerFehlteile" => lagerbestellungSicht && processLager,
            "Kommissionierung" => pick,
            "Kommissionierlisten" => kommListen && processLager,
            "LeitstandKommissionierung" => leitstand && (pick || release),
            "FaListe" or "FaStruktur" => faLesen,
            "FaVervollstaendigen" => faCompletion && completion,
            "FaAbarbeitungsliste" => faCompletion && vorbau,
            "Vormontage" => vormontage && vorbau,
            "BdeTerminal" => bde && useBde,
            "BdeCockpit" or "BdeBuchungen" or "BdeStammdaten" => bde && useBde && shiftlead,
            "BdeSchichtkalender" => bde && useBde && admin,
            "TeileverfolgungRueckmeldungen" or "TeileverfolgungOseon" => teileverfolgung && tracking,
            // Freigabe-Antwort 5 (2026-11-19): nur bei aktivem OSEON-Modul (TeileverfolgungAktiv, AppSettingDefinitions „Teileverfolgungs-Modul (OSEON)“)
            "OseonAgUebersicht" => teileverfolgung && reporting,
            "BfWarenkorb" or "BfAuftraege" => blech,
            "BeschichtungVorschlagsliste" or "Beschichtungsauftraege" => beschichtung && beschichtungRecht,
            "Bestellvorschlaege" or "OffeneBestellungenEinkauf" or "SageWareneingaengeEinkauf" => einkauf,
            "Artikel" or "Lagerplaetze" or "Werkbaenke" or "Artikelkategorien" or "Artikelmerkmale" or "FaVorbauAg" or "Kommissionierbereiche"
                or "FaMerkmale" or "Kreditoren" or "Debitoren" => mdRead,
            "Empfaenger" => mdRead && (bestellungen || lagerbestellung || blechAn),
            "ArtikelgruppenZuordnung" => mdRead && bestellungen,
            "Benutzer" or "Arbeitsplaetze" or "Rollen" or "Arbeitsgaenge" or "Einstellungen" or "ServiceEinstellungen" or "Standorteinstellungen"
                or "AktivitaetsProtokoll" => admin,
            "ArbeitsgaengeSage" => master,
            _ => throw new InvalidOperationException($"NavZiel '{key}' ohne Regel in NavigationSicht.")
        };

        return _ergebnis = new NavigationErgebnis(NavZiele.Alle.Where(z => Regel(z.Key)).Select(z => z.Key).ToHashSet(), leitstand);
    }
}
