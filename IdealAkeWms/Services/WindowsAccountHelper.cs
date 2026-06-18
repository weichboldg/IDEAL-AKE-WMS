namespace IdealAkeWms.Services;

/// <summary>Extrahiert den SAM-Account aus einem Windows-Identity-Namen.</summary>
public static class WindowsAccountHelper
{
    /// <summary>"DOMAIN\\sam" -> "sam"; "sam@domain" -> "sam"; sonst Eingabe getrimmt; leer -> null.</summary>
    public static string? ExtractSam(string? identityName)
    {
        if (string.IsNullOrWhiteSpace(identityName))
            return null;

        var value = identityName.Trim();
        var backslash = value.LastIndexOf('\\');
        if (backslash >= 0)
            return value[(backslash + 1)..];

        var at = value.IndexOf('@');
        if (at > 0)
            return value[..at];

        return value;
    }
}
