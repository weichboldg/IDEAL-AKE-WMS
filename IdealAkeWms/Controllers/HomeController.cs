using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using IdealAkeWms.Services.Navigation;

namespace IdealAkeWms.Controllers;

// v1.99.45 (ADR 0034): Kacheln rendert die View aus NavigationSicht, keine Rechte mehr im Controller.
// v1.99.51: Favoriten je Benutzer in UserViewPreferences (Server-Key, nicht in ColumnDefinitions -> nicht ueber die API beschreibbar).
// Kein RequireXxxAccess: jeder verwaltet nur seine eigene Zeile (Benutzer-Id aus der Session), Sichtbarkeit allein NavigationSicht.
public class HomeController(NavigationSicht navigation, IUserViewPreferenceRepository prefs, ICurrentUserService user,
    ILogger<HomeController> logger) : Controller
{
    internal const string FavoritenViewKey = "Startseite.Favoriten";

    private sealed record Ablage([property: JsonPropertyName("ziele")] List<string>? Ziele);

    public async Task<IActionResult> Index(string? reiter)
    {
        var id = user.GetCurrentAppUserId();
        return View(new StartseiteViewModel(id is { } uid ? await LesenAsync(uid) : null, Reiter(reiter)));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> FavoritUmschalten(string key, string? reiter)
    {
        if (user.GetCurrentAppUserId() is not { } id) return RedirectToAction(nameof(Index));
        var liste = await LesenAsync(id);
        var ziel = NavZiele.Alle.FirstOrDefault(z => z.Key == key);
        if (liste.Remove(key))
        {
            await SpeichernAsync(id, liste);
            TempData["SuccessMessage"] = $"„{ziel?.Titel ?? key}“ aus den Favoriten entfernt.";
        }
        else if (ziel?.Kategorie != null && (await navigation.GetAsync()).Zeigt(ziel))
        {
            liste.Add(key);
            await SpeichernAsync(id, liste);
            TempData["SuccessMessage"] = $"„{ziel.Titel}“ zu den Favoriten hinzugefügt.";
        }
        else TempData["WarningMessage"] = "Diese Funktion ist für Sie nicht verfügbar.";
        return RedirectToAction(nameof(Index), new { reiter = Reiter(reiter) });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> FavoritenBereinigen()
    {
        if (user.GetCurrentAppUserId() is not { } id) return RedirectToAction(nameof(Index));
        var liste = await LesenAsync(id);
        var sichtbar = (await navigation.GetAsync()).Favoriten(liste).Sichtbar.Select(z => z.Key).ToHashSet();
        var entfernt = liste.RemoveAll(k => !sichtbar.Contains(k));
        if (entfernt > 0) await SpeichernAsync(id, liste);
        TempData["SuccessMessage"] = $"{entfernt} nicht verfügbare Favoriten entfernt.";
        return RedirectToAction(nameof(Index), new { reiter = "favoriten" });
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    private static string? Reiter(string? reiter) => reiter is "favoriten" or "alle" ? reiter : null;

    private async Task<List<string>> LesenAsync(int userId)
    {
        var json = (await prefs.GetByUserAndViewAsync(userId, FavoritenViewKey))?.SettingsJson;
        if (string.IsNullOrWhiteSpace(json)) return [];
        try
        {
            return JsonSerializer.Deserialize<Ablage>(json)?.Ziele?.Where(k => !string.IsNullOrEmpty(k)).Distinct().ToList() ?? [];
        }
        catch (JsonException ex)
        {
            // Melden statt still: der kaputte Wert gilt als leere Liste und wird erst beim naechsten Stern-Klick ersetzt.
            logger.LogWarning(ex, "Favoriten von Benutzer {UserId} nicht lesbar ({ViewKey}), gelten als leer: {Json}", userId, FavoritenViewKey, json);
            return [];
        }
    }

    private Task SpeichernAsync(int userId, List<string> liste) => liste.Count == 0
        ? prefs.DeleteByUserAndViewAsync(userId, FavoritenViewKey)
        : prefs.SaveAsync(userId, FavoritenViewKey, JsonSerializer.Serialize(new Ablage(liste)), user.GetDisplayName(), user.GetWindowsUserName());
}
