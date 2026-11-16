using IdealAkeWms.Data;
using IdealAkeWms.Models;

namespace IdealAkeWms.Services;

/// <summary>
/// Start-Seed des Benutzers <c>admin</c> (ADR 0027, Regel P, Freigabe-Antwort 11 = S1): fehlt <c>admin</c>, hat er kein Kennwort
/// (auch Rueckweg „PasswordHash per SQL auf NULL + Neustart") oder noch den Leer-Hash alter Staende, bekommt er das dokumentierte
/// Standard-Erstkennwort mit Pflichtwechsel. Ein <c>admin</c> mit eigenem Kennwort bleibt unberuehrt.
/// </summary>
public static class AdminSeed
{
    public const string AdminName = "admin";

    /// <summary>Im Betriebshandbuch dokumentiert (docs/betrieb/03, 07). Gilt nur bis zur ersten Anmeldung (Pflichtwechsel).</summary>
    public const string StandardErstkennwort = "wms-admin";

    /// <summary>Legt <c>admin</c> an bzw. stellt ihn auf das Standard-Erstkennwort um. Liefert den (gespeicherten) Benutzer.</summary>
    public static User Ensure(ApplicationDbContext db, IPasswordService passwordService)
    {
        var admin = db.Users.FirstOrDefault(u => u.Name == AdminName);
        if (admin == null)
        {
            admin = new User
            {
                Name = AdminName,
                IsActive = true,
                PasswordHash = passwordService.HashPassword(StandardErstkennwort),
                MussKennwortAendern = true,
                CreatedAt = DateTime.Now,
                CreatedBy = "system",
                CreatedByWindows = "system"
            };
            db.Users.Add(admin);
            db.SaveChanges();
        }
        else if (string.IsNullOrEmpty(admin.PasswordHash) || passwordService.VerifyPassword(admin.PasswordHash, string.Empty))
        {
            admin.PasswordHash = passwordService.HashPassword(StandardErstkennwort);
            admin.MussKennwortAendern = true;
            admin.ModifiedAt = DateTime.Now;
            admin.ModifiedBy = "system";
            admin.ModifiedByWindows = "system";
            db.SaveChanges();
        }
        return admin;
    }
}
