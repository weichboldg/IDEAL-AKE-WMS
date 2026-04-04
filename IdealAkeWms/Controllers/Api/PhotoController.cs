using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using Microsoft.AspNetCore.Mvc;

namespace IdealAkeWms.Controllers.Api;

[Route("api/photos")]
[ApiController]
[RequirePickingAccess]
public class PhotoController : ControllerBase
{
    private readonly IProductionOrderRepository _productionOrderRepository;
    private readonly IWebHostEnvironment _env;

    public PhotoController(
        IProductionOrderRepository productionOrderRepository,
        IWebHostEnvironment env)
    {
        _productionOrderRepository = productionOrderRepository;
        _env = env;
    }

    [HttpPost("upload")]
    public async Task<IActionResult> Upload(int productionOrderId, IFormFile photo)
    {
        var order = await _productionOrderRepository.GetByIdAsync(productionOrderId);
        if (order == null)
            return NotFound();

        var photosDir = Path.Combine(
            _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot"),
            "Fotos", "Kommissionierung");
        Directory.CreateDirectory(photosDir);

        // Bestehende Fotos für diesen FA zählen
        var existingPhotos = Directory.GetFiles(photosDir, $"{order.OrderNumber}_*");
        var seq = existingPhotos.Length + 1;

        var fileName = $"{order.OrderNumber}_{DateTime.Now:yyyyMMddHHmmss}_{seq}.jpg";
        var filePath = Path.Combine(photosDir, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await photo.CopyToAsync(stream);
        }

        return Ok(new
        {
            success = true,
            fileName,
            url = $"/Fotos/Kommissionierung/{fileName}"
        });
    }

    [HttpGet("{productionOrderId}")]
    public async Task<IActionResult> Get(int productionOrderId)
    {
        var order = await _productionOrderRepository.GetByIdAsync(productionOrderId);
        if (order == null)
            return NotFound();

        var photosDir = Path.Combine(
            _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot"),
            "Fotos", "Kommissionierung");

        if (!Directory.Exists(photosDir))
            return Ok(Array.Empty<object>());

        var photos = Directory.GetFiles(photosDir, $"{order.OrderNumber}_*")
            .Select(f => new
            {
                fileName = Path.GetFileName(f),
                url = $"/Fotos/Kommissionierung/{Path.GetFileName(f)}"
            })
            .OrderBy(f => f.fileName)
            .ToArray();

        return Ok(photos);
    }

    [HttpPost("delete")]
    public IActionResult Delete(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName) || fileName.Contains("..") || fileName.Contains('/') || fileName.Contains('\\'))
            return BadRequest();

        var filePath = Path.Combine(
            _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot"),
            "Fotos", "Kommissionierung", fileName);

        if (System.IO.File.Exists(filePath))
        {
            System.IO.File.Delete(filePath);
        }

        return Ok();
    }
}
