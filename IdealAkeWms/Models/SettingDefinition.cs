namespace IdealAkeWms.Models;

/// <summary>Typ einer Einstellung — steuert Bedienelement und Serverpruefung (ADR 0020, Spec 2026-11-07-einstellungen-auswahlfelder-und-beschreibung F1).</summary>
public enum SettingType
{
    Bool,
    Int,
    String,
    /// <summary>Fester Wertebereich, ein Wert (Dropdown).</summary>
    Choice,
    /// <summary>Fester Wertebereich, mehrere Werte kommagetrennt in Katalog-Reihenfolge (Checkbox-Gruppe).</summary>
    MultiChoice,
    /// <summary>Verweis auf Stammdaten (<see cref="SettingDefinition.Stammdaten"/>); gespeichert wird der Schluessel.</summary>
    MasterData,
    /// <summary>v1.99.14: Geheimnis (SData-Kennwort) — nie angezeigt, nie protokolliert, gespeichert verschluesselt (<c>dp1:</c>,
    /// <c>SdataZugangsdaten</c>). Leere Eingabe = unveraendert.</summary>
    Geheim
}

/// <summary>Stammdaten-Quelle eines <see cref="SettingType.MasterData"/>-Werts.</summary>
public enum SettingStammdaten
{
    Keine,
    /// <summary>Lagerplatz-Code (<c>StorageLocations.Code</c>), Textfeld mit Vorschlagsliste.</summary>
    Lagerplatz,
    /// <summary>Name einer Artikelkategorie (<c>ArticleCategories.Name</c>).</summary>
    Artikelkategorie,
    /// <summary>Id einer Bestellempfaenger-Gruppe (<c>OrderRecipientGroups.Id</c>).</summary>
    Bestellempfaengergruppe
}

/// <summary>v1.99.10: String-Einstellung mit Mail-Adressen — der Server prueft jede Adresse beim Speichern (ADR 0020 Punkt 2).</summary>
public enum SettingMail
{
    Keine,
    /// <summary>Genau eine Adresse oder leer.</summary>
    Eine,
    /// <summary>Mehrere Adressen (Komma, Semikolon oder Zeilenumbruch) oder leer.</summary>
    Liste
}

/// <summary>Wie der Lesecode einen Bool-Rohwert auslegt (F6) — damit die Maske den WIRKSAMEN Zustand zeigt und Speichern nichts umkippt.</summary>
public enum SettingBoolLesart
{
    /// <summary>Nur exakt „true“ (ohne Gross/Klein) aktiviert; alles andere = aus. Muster der meisten AppSettings.</summary>
    NurTrueAktiviert,
    /// <summary>Nur exakt „false“ sperrt; fehlend/leer/sonst = an (LagerbestellungAktiv, Vault-/enaio-Links).</summary>
    NurFalseSperrt,
    /// <summary>Leer/fehlend = Default, sonst „true“ (ohne Gross/Klein) = an (ServiceSettings, <c>ServiceSettings.GetBoolAsync</c>).</summary>
    LeerIstDefault
}

/// <summary>
/// Definiert eine Einstellung typisiert: Single Source of Truth fuer Typ, Default, Gruppe und Beschreibung — fuer ServiceSettings
/// (<see cref="ServiceSettingDefinitions"/>) und seit v1.77.0 fuer AppSettings (<see cref="AppSettingDefinitions"/>). Treibt Seeding
/// (<c>Program.cs</c>) und die typisierten Masken (/ServiceSettings, /Settings). Bis v1.76.0 hiess der Record ServiceSettingDefinition.
/// </summary>
/// <param name="Key">DB-Key (z.B. "Sync:BomCacheEnabled").</param>
/// <param name="Type">Typ fuer UI + Validierung.</param>
/// <param name="DefaultValue">Default IMMER als String im DB-Format: bool -&gt; "true"/"false", int -&gt; "60", string -&gt; Rohwert.
/// Bei AppSettings = Wert des Lesers bei FEHLENDER Zeile.</param>
/// <param name="Category">UI-Gruppe.</param>
/// <param name="Description">„Bewirkt“ — Anzeige unter dem Feld (bei ServiceSettings auch in die DB geseedet, max. 500 Zeichen).</param>
/// <param name="Multiline">String-Listen als Textarea rendern.</param>
/// <param name="Anzeigename">Deutscher Name ohne Key-Syntax (ADR 0020 Punkt 3).</param>
/// <param name="Verwendung">Wo die Einstellung wirkt (Modul, Seite, Dienst-Lauf).</param>
/// <param name="Standort">AKE | IDEAL | beide.</param>
/// <param name="Warnhinweis">Pflicht bei Aussenwirkung oder Einweg-Charakter, sonst null.</param>
/// <param name="AbhaengigVon">Key, ohne den diese Einstellung nicht wirkt.</param>
/// <param name="Werte">Erlaubte Werte bei Choice/MultiChoice (Wert, Anzeigetext).</param>
/// <param name="Min">Untergrenze bei Int — nur, wenn der Leser sie kennt.</param>
/// <param name="Max">Obergrenze bei Int — nur, wenn der Leser sie kennt.</param>
/// <param name="Einheit">Einheit bei Int (z.B. „Tage“).</param>
/// <param name="Stammdaten">Quelle bei MasterData.</param>
/// <param name="LeerErlaubt">Leerer Wert ist zulaessig (MasterData/Int).</param>
/// <param name="BoolLesart">Lesart bei Bool (F6).</param>
/// <param name="Seed">AppSettings: beim Web-Start anlegen, wenn die Zeile fehlt (Wert = Default).</param>
/// <param name="Muster">String: erlaubtes Format als Regex (getrimmter Wert) und Text fuer die Fehlermeldung (v1.99.9, System-Kuerzel).</param>
/// <param name="Mail">String: Mail-Adresse(n), jede wird beim Speichern geprueft (v1.99.10, Lieferverzug-Mail).</param>
public sealed record SettingDefinition(
    string Key,
    SettingType Type,
    string DefaultValue,
    string Category,
    string Description,
    bool Multiline = false,
    string? Anzeigename = null,
    string? Verwendung = null,
    string? Standort = null,
    string? Warnhinweis = null,
    string? AbhaengigVon = null,
    (string Wert, string Text)[]? Werte = null,
    int? Min = null,
    int? Max = null,
    string? Einheit = null,
    SettingStammdaten Stammdaten = SettingStammdaten.Keine,
    bool LeerErlaubt = false,
    SettingBoolLesart BoolLesart = SettingBoolLesart.NurTrueAktiviert,
    bool Seed = false,
    (string Regex, string Erlaubt)? Muster = null,
    SettingMail Mail = SettingMail.Keine);
