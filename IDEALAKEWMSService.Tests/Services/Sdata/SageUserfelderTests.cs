using System.Text.Json;
using FluentAssertions;
using IdealAkeWms.Models;
using IdealAkeWms.Services;
using IdealAkeWms.Services.SageSdata;
using IdealAkeWms.Services.Settings;
using IDEALAKEWMSService.Services;
using IDEALAKEWMSService.Services.Sdata;
using IDEALAKEWMSService.Workers;

namespace IDEALAKEWMSService.Tests.Services.Sdata;

/// <summary>v1.99.40 (Spec 2026-11-18-sage-userfelder-als-einstellung, ADR 0033, TS-161): Sage-Benutzerfelder aus ServiceSettings.</summary>
public class SageUserfelderTests
{
    private static SageUserfelder Mit(string abgleich, string standard, string? wert) =>
        SageUserfelder.Aus(k => k == SageUserfelder.Key(abgleich, standard) ? wert : null);

    // ----- R1: AKE liest zeichengleich (keine Einstellungszeilen = Default) -----

    [Fact]
    public void Default_selects_unveraendert_wie_v1_99_39()
    {
        var f = SageUserfelder.Standard;
        KontokorrentSyncService.SelectMit("Kto,Matchcode,EUUStID,Zahlungskond,Gruppe,Aktiv,Adresse",
                f.Feld(SageUserfelder.Kreditoren, KontokorrentSyncService.FeldMindestbestellwert))
            .Should().Be("Kto,Matchcode,EUUStID,Zahlungskond,Gruppe,Aktiv,Adresse,USER_EKMindestbestellwert");
        KontokorrentSyncService.SelectMit(KontokorrentSyncService.SelectAdressen, f.Feld(SageUserfelder.Debitoren, KontokorrentSyncService.FeldKundenNameFa))
            .Should().Be(KontokorrentSyncService.SelectAdressenDebitoren);
        SdataArtikelReader.SelectVariantenFuer(f).Should().Be(SdataArtikelReader.SelectVarianten);
        SageArbeitsplatzSql.TableSqlFuer(f.Feld(SageUserfelder.Arbeitsplaetze, SageArbeitsplatzSql.FeldArbeitsSchritt))
            .Should().Be(SageArbeitsplatzSql.TableSql);
        SageArbeitsplatzSql.Build("[dbo].[KHKPpsArbeitsplaetze]").Should().Be(SageArbeitsplatzSql.TableSql);
        f.Konfiguriert(SageUserfelder.Blechfertigung, BfArtikelFelder.SageFelder).Select(k => k.Feld).Should().Equal(BfArtikelFelder.SageFelder);
        f.Konfiguriert(SageUserfelder.FaStruktur, SdataFaStrukturReader.FelderFa).Select(k => k.Feld).Should().Equal(SdataFaStrukturReader.FelderFa);
        f.Feld(SageUserfelder.Bestellungen, "USER_AB").Should().Be("USER_AB");
        f.Feld(SageUserfelder.Bestellungen, "USER_Kom").Should().Be("USER_Kom");
        f.Hinweise.Should().BeEmpty();
    }

    [Fact]
    public void Katalog_vollstaendig_im_ServiceSetting_Katalog_mit_Muster_und_Metadaten()
    {
        SageUserfelder.Katalog.Should().HaveCount(28);
        SageUserfelder.Katalog.Select(k => k.Key).Should().OnlyHaveUniqueItems();
        foreach (var u in SageUserfelder.Katalog)
        {
            ServiceSettingDefinitions.TryGet(u.Key, out var d).Should().BeTrue(u.Key);
            d!.DefaultValue.Should().Be(u.Default);
            d.Anzeigename.Should().Be(u.Anzeigename);
            d.Category.Should().StartWith("Sage-Benutzerfelder: ");
            d.Warnhinweis.Should().NotBeNullOrWhiteSpace();
            SettingValidator.Validate(d, "").Gueltig.Should().BeTrue("leer = nicht konfiguriert");
            SettingValidator.Validate(d, " USER_Neu_1 ").Kanonisch.Should().Be("USER_Neu_1");
            SettingValidator.Validate(d, "KHKEKBelege_USER_AB").Gueltig.Should().BeFalse("Praefix gehoert nie in die Einstellung");
            SettingValidator.Validate(d, "USER_X,USER_Y").Gueltig.Should().BeFalse();
        }
    }

    // ----- R1/R2: leer = nicht anfragen, ungueltig = wie leer mit Hinweis -----

    [Fact]
    public void Leer_laesst_das_Feld_aus_der_Abfrage_weg()
    {
        var kr = Mit(SageUserfelder.Kreditoren, KontokorrentSyncService.FeldMindestbestellwert, "");
        kr.Feld(SageUserfelder.Kreditoren, KontokorrentSyncService.FeldMindestbestellwert).Should().BeNull();
        KontokorrentSyncService.SelectMit("Kto", null).Should().Be("Kto");
        kr.Hinweise.Should().BeEmpty("leer ist gewollt, keine Warnung");

        SdataArtikelReader.SelectVariantenFuer(Mit(SageUserfelder.Artikel, SdataArtikelReader.FeldMindestbestand, "  "))
            .Should().EndWith(",USER_LagerplatzID").And.NotContain("USER_Mindestbestand");
        SageArbeitsplatzSql.TableSqlFuer(null).Should().Contain("CAST(NULL AS nvarchar(50)) AS USER_ArbeitsSchritt");
        Mit(SageUserfelder.FaStruktur, "USER_OSMaterial", "").Konfiguriert(SageUserfelder.FaStruktur, SdataFaStrukturReader.FelderFa)
            .Should().HaveCount(12).And.NotContain(k => k.Logisch == "USER_OSMaterial");
    }

    [Fact]
    public void Ungueltig_wird_nicht_angefragt_und_gemeldet()
    {
        var f = Mit(SageUserfelder.Arbeitsplaetze, SageArbeitsplatzSql.FeldArbeitsSchritt, "USER_X; DROP TABLE y");
        f.Feld(SageUserfelder.Arbeitsplaetze, SageArbeitsplatzSql.FeldArbeitsSchritt).Should().BeNull();
        f.Hinweise.Should().ContainSingle().Which.Should().Contain("Arbeitsplätze: Arbeitsschritt-Kürzel").And.Contain("kein gültiger Feldname");
    }

    [Fact]
    public void Umbenannt_wird_angefragt_und_unter_dem_logischen_Namen_gefuehrt()
    {
        var f = Mit(SageUserfelder.FaStruktur, "USER_OSMaterial", "USER_Werkstoff");
        f.Konfiguriert(SageUserfelder.FaStruktur, SdataFaStrukturReader.FelderFa).Should().Contain(("USER_OSMaterial", "USER_Werkstoff"));
        SageArbeitsplatzSql.TableSqlFuer("USER_AS2").Should().Contain("SELECT Arbeitsplatznummer, Bezeichnung1, USER_AS2");
    }

    // ----- Werte: typisierter Default-Weg und abweichender Name (JsonExtensionData) -----

    [Fact]
    public void Wert_unter_abweichendem_Namen_kommt_aus_den_weiteren_Feldern()
    {
        var zeile = JsonSerializer.Deserialize<SdataKontoZeile>("""{"Kto":"K1","USER_EKMindestbestellwert":10.5,"USER_MBW2":"12.345"}""")!;
        zeile.USER_EKMindestbestellwert.Should().Be(10.5m, "Default-Name fuellt die typisierte Eigenschaft");
        SageUserfelder.Text("USER_EKMindestbestellwert", "USER_EKMindestbestellwert", zeile.USER_EKMindestbestellwert, zeile.Weitere).Should().Be("10.5");
        SageUserfelder.Dezimal(SageUserfelder.Text("usER_mbw2", "USER_EKMindestbestellwert", zeile.USER_EKMindestbestellwert, zeile.Weitere))
            .Should().Be(12.345m, "Gross/Klein egal");
        SageUserfelder.Text(null, "USER_EKMindestbestellwert", zeile.USER_EKMindestbestellwert, zeile.Weitere).Should().BeNull("nicht konfiguriert");
    }

    [Fact]
    public void Abbilden_liest_Mindestbestand_unter_konfiguriertem_Namen()
    {
        var variante = JsonSerializer.Deserialize<SdataArtikelVarianteZeile>(
            """{"Artikelnummer":"A1","AuspraegungID":0,"USER_Mindestbestand":5,"USER_MB2":7.1234,"USER_LagerplatzID":11}""")!;
        var artikel = JsonSerializer.Deserialize<SdataArtikelZeile>("""{"Artikelnummer":"A1"}""")!;
        var plaetze = Array.Empty<SdataLagerplatzZeile>();

        SdataArtikelReader.Abbilden(new[] { artikel }, Array.Empty<string>(), new[] { variante }, plaetze)[0].Details!
            .Should().Match<SageArtikelDetails>(d => d.UserMindestbestand == 5m && d.UserLagerplatzId == 11, "Default = AKE");
        var umbenannt = SageUserfelder.Aus(k => k == SageUserfelder.Key(SageUserfelder.Artikel, "USER_Mindestbestand") ? "USER_MB2"
            : k == SageUserfelder.Key(SageUserfelder.Artikel, "USER_LagerplatzID") ? "" : null);
        SdataArtikelReader.Abbilden(new[] { artikel }, Array.Empty<string>(), new[] { variante }, plaetze, umbenannt)[0].Details!
            .Should().Match<SageArtikelDetails>(d => d.UserMindestbestand == 7.123m && d.UserLagerplatzId == null);
    }

    // ----- R4: „Ungueltiger Spaltenname“ nennt die Einstellung -----

    [Fact]
    public void Erklaeren_nennt_die_Einstellung_beim_ungueltigen_Spaltennamen()
    {
        var f = SageUserfelder.Standard;
        var m = f.Erklaeren("SData Kontokorrent: HTTP 500 – Ungültiger Spaltenname \"USER_EKMindestbestellwert\".");
        m.Should().Contain("Sage kennt das Feld „USER_EKMindestbestellwert“ nicht")
            .And.Contain("„Kreditoren: Mindestbestellwert“ (Sync:Userfeld:Kreditoren:USER_EKMindestbestellwert)").And.Contain("leer setzen");
        f.Erklaeren("Invalid column name 'KHKEKBelege_USER_AB'.").Should().Contain("Offene Bestellungen: AB-Nummer").And.NotContain("Kommission");
        f.Erklaeren("Ungültiger Spaltenname 'USER_ABC'.").Should().Be("Ungültiger Spaltenname 'USER_ABC'.", "USER_AB ist nicht USER_ABC");
        f.Erklaeren("HTTP 401 – nicht angemeldet").Should().Be("HTTP 401 – nicht angemeldet");
    }

    /// <summary>v1.99.42 (Befund 13:25): bei HTTP 500 steht vorn das SQL, die Message ist gekappt — der Hinweis muss trotzdem
    /// sichtbar sein (voran, aus der ungekappten Diagnose) und darf beim zweiten Erklaeren nicht doppelt kommen.</summary>
    [Fact]
    public void Erklaeren_LangeSqlDiagnose_HinweisVorn_AusDiagnose_NieDoppelt()
    {
        var f = SageUserfelder.Standard;
        var diagnose = "SELECT " + new string('x', 900) + " FROM KHKKontokorrent – Ungültiger Spaltenname 'USER_EKMindestbestellwert'.";
        var ex = new SdataLeseException("Kontokorrent", 500, diagnose);
        ex.Message.Should().NotContain("USER_EKMindestbestellwert", "Vorbedingung: der Name liegt hinter der Kappung");

        var m = f.Erklaeren(ex);
        m.Should().StartWith("Sage kennt das Feld „USER_EKMindestbestellwert“ nicht – Einstellung „Kreditoren: Mindestbestellwert“ (Sync:Userfeld:Kreditoren:USER_EKMindestbestellwert)");
        f.Erklaeren(m).Should().Be(m, "schon erklaert");
        new SdataLeseException("Kontokorrent", 500, f.Erklaeren(diagnose)).Message.Should().Contain("Sync:Userfeld:Kreditoren:USER_EKMindestbestellwert",
            "auch der neu geworfene Fehler behaelt den Hinweis trotz Kappung");
    }

    // ----- R5: HTTP 404 = Resource fehlt -----

    [Fact]
    public void Http404_meldet_fehlende_Resource_verstaendlich_und_wird_in_der_Kette_gefunden()
    {
        var ex = new SdataLeseException("ArtikelLieferantInfo", 404, "Die Resource 'ArtikelLieferantInfo' ist nicht definiert");
        ex.Message.Should().StartWith("Resource „ArtikelLieferantInfo“ am Sage-Server nicht vorhanden (HTTP 404) – Abgleich in den Service-Einstellungen ausschalten.");
        SyncWorker.ResourceFehlt(new InvalidOperationException("x", ex)).Should().BeSameAs(ex);
        SyncWorker.ResourceFehlt(new SdataLeseException("Kontokorrent", 500, "Ungültiger Spaltenname")).Should().BeNull();
        new SdataLeseException("Kontokorrent", 500, "y").Message.Should().Be("SData Kontokorrent: HTTP 500 – y", "andere Status unveraendert");
    }
}
