using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Services;
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
    public async Task<IActionResult> Upload([FromForm] int productionOrderId, IFormFile photo)
    {
        var order = await _productionOrderRepository.GetByIdAsync(productionOrderId);
        if (order == null)
            return NotFound();

        var photosDir = PhotoStorage.GetDirectory(_env);
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

        var photosDir = PhotoStorage.GetDirectory(_env);

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

    /// <summary>v1.30.4: Loeschen nur fuer ein Foto DIESES FA (Dateiname beginnt mit <c>{OrderNumber}_</c>, Regel wie Upload/Get).</summary>
    [HttpPost("delete")]
    public async Task<IActionResult> Delete([FromForm] int productionOrderId, [FromForm] string fileName)
    {
        var order = await _productionOrderRepository.GetByIdAsync(productionOrderId);
        if (order == null)
            return NotFound();
        if (!PhotoStorage.TryResolve(PhotoStorage.GetDirectory(_env), fileName, out var filePath)
            || !fileName.StartsWith($"{order.OrderNumber}_", StringComparison.Ordinal))
            return BadRequest();

        if (System.IO.File.Exists(filePath))
        {
            System.IO.File.Delete(filePath);
        }

        return Ok();
    }

    /// <summary>v1.30.4 (Hotfix Kommissionierfotos): Auslieferung unter der bisherigen URL, jetzt hinter <see cref="RequirePickingAccessAttribute"/>
    /// (Class-Level) und der Login-Middleware. Die URL ist kein Dateipfad mehr – die Datei liegt in App_Data.</summary>
    [HttpGet("/Fotos/Kommissionierung/{fileName}")]
    public IActionResult GetFile(string fileName)
    {
        if (!PhotoStorage.TryResolve(PhotoStorage.GetDirectory(_env), fileName, out var filePath))
            return BadRequest();
        if (!System.IO.File.Exists(filePath))
            return NotFound();
        return PhysicalFile(filePath, "image/jpeg");
    }
}
