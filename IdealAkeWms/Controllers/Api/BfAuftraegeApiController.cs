using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using Microsoft.AspNetCore.Mvc;

namespace IdealAkeWms.Controllers.Api;

/// <summary>v1.99.35 (Spec 2026-11-13-blechfertigung-bf-auftraege-wicam-csv): Artikelsuche des BF-Warenkorbs — nur Lagerteile BG/FT
/// (Filter in SQL im Repository), mit den Feldern, die die Position uebernimmt.</summary>
[ApiController]
[Route("api/bf-auftraege")]
[RequireBfBestellenAccess]
[RequireBlechfertigungAktiv]
public class BfAuftraegeApiController : ControllerBase
{
    private readonly IBfAuftragRepository _repo;

    public BfAuftraegeApiController(IBfAuftragRepository repo) => _repo = repo;

    [HttpGet("artikel")]
    public async Task<IActionResult> Artikel(string? q, int limit = 20) =>
        Ok((await _repo.SucheArtikelAsync(q, limit)).Select(a => new
        {
            a.ArticleNumber, a.Description, a.Matchcode, a.Teileart, a.Material, a.Materialnummer, a.FertigungsInfoPN, a.Hauptlagerplatz,
            a.Bestellmenge,
            Hinweis = string.IsNullOrWhiteSpace(a.Materialnummer) ? "keine Materialnummer – wird nicht an WiCAM übergeben" : null,
        }));
}
