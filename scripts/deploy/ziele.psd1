# Zielsysteme fuer Update-IdealAkeWms.ps1 / Install-IdealAkeWms.ps1 (Stand Gerald 2026-11-09/11). Nur ASCII.
# Ein neues System = eine Zeile. App-Pool, Site, Dienst und DB ermittelt das Skript selbst (ueber die Pfade).
# Url optional: weicht die IIS-Bindung ab, warnt das Skript.
# IDEAL auf idealweb01, AKE auf akenet01 (alle drei Systeme).
@{
Ziele = @(
  @{ Firma='IDEAL'; System='Prod';       Prod=$true;  WebPfad='C:\inetpub\wwwroot\IDEALAKEWMS';           DienstPfad='C:\IDEAL\IdealAkeWmsService' }
  @{ Firma='IDEAL'; System='Test';       Prod=$false; WebPfad='C:\inetpub\wwwroot\IDEALAKEWMS_Test';      DienstPfad='C:\IDEAL\IdealAkeWmsService_Test' }
  @{ Firma='AKE';   System='Prod';       Prod=$true;  WebPfad='C:\inetpub\wwwroot\IDEALAKEWMS';           DienstPfad='C:\AKE\IdealAkeWmsService' }
  @{ Firma='AKE';   System='Test';       Prod=$false; WebPfad='C:\inetpub\wwwroot\IDEALAKEWMS_Test';      DienstPfad='C:\AKE\IdealAkeWmsService_Test';        Url='https://akenet01.ake.at:4444/' }
  @{ Firma='AKE';   System='Test-IDEAL'; Prod=$false; WebPfad='C:\inetpub\wwwroot\IdealAkeWMS-TESTIDEAL'; DienstPfad='C:\AKE\IDEALAKEWMSService-TestIDEAL'; Url='https://akenet01.ake.at:4445/' }
)
}
