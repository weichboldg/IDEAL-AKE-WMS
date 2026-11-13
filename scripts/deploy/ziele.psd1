# Zielsysteme fuer Update-IdealAkeWms.ps1 / Install-IdealAkeWms.ps1 (Stand Gerald 2026-11-09/11). Nur ASCII.
# Ein neues System = eine Zeile. App-Pool, Site, Dienst und DB ermittelt das Skript selbst (ueber die Pfade).
# Url = Adresse der Startseite (http/https). Hat Vorrang vor der IIS-Bindung (v1.98.1); -SiteUrl ueberschreibt sie.
# Passen Schema oder Port nicht zur Bindung, warnt das Skript. Mit -WebPfad (anderer Ort) gilt die Url nicht.
# IDEAL auf idealweb01, AKE auf akenet01 (alle drei Systeme).
# DEV (v1.99.16): Entwickler-PC, IIS-Sites AKE_WMS_DEV :8081 / IDEAL_WMS_DEV :8082, Dienste AKE_WMS_DEV_Service / IDEAL_WMS_DEV_Service.
@{
Ziele = @(
  @{ Firma='IDEAL'; System='Prod';       Prod=$true;  WebPfad='C:\inetpub\wwwroot\IDEALAKEWMS';           DienstPfad='C:\IDEAL\IdealAkeWmsService';             Url='https://idealweb01.ideal.ideal-ake.at/' }
  @{ Firma='IDEAL'; System='Test';       Prod=$false; WebPfad='C:\inetpub\wwwroot\IDEALAKEWMS_Test';      DienstPfad='C:\IDEAL\IdealAkeWmsService_Test';        Url='http://idealweb01.ideal.ideal-ake.at:88/' }
  @{ Firma='AKE';   System='Prod';       Prod=$true;  WebPfad='C:\inetpub\wwwroot\IDEALAKEWMS';           DienstPfad='C:\AKE\IdealAkeWmsService';               Url='https://akenet01.ake.at:4443/' }
  @{ Firma='AKE';   System='Test';       Prod=$false; WebPfad='C:\inetpub\wwwroot\IDEALAKEWMS_Test';      DienstPfad='C:\AKE\IdealAkeWmsService_Test';        Url='https://akenet01.ake.at:4444/' }
  @{ Firma='AKE';   System='Test-IDEAL'; Prod=$false; WebPfad='C:\inetpub\wwwroot\IdealAkeWMS-TESTIDEAL'; DienstPfad='C:\AKE\IDEALAKEWMSService-TestIDEAL'; Url='https://akenet01.ake.at:4445/' }
  @{ Firma='AKE';   System='DEV';        Prod=$false; WebPfad='D:\Dev\AKE\Web';                      DienstPfad='D:\Dev\AKE\Service';                   Url='http://localhost:8081/' }
  @{ Firma='IDEAL'; System='DEV';        Prod=$false; WebPfad='D:\Dev\IDEAL\Web';                    DienstPfad='D:\Dev\IDEAL\Service';                 Url='http://localhost:8082/' }
)
}
