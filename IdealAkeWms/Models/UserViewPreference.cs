namespace IdealAkeWms.Models;

public class UserViewPreference : AuditableEntity
{
    public int UserId { get; set; }
    public string ViewKey { get; set; } = string.Empty;
    public string SettingsJson { get; set; } = "{}";

    public User User { get; set; } = null!;
}
