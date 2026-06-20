using Google.Cloud.Storage.V1;
using Google.Cloud.Vision.V1;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Donacerca.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UploadController : ControllerBase
{
    private readonly IConfiguration _config;

    public UploadController(IConfiguration config)
    {
        _config = config;
    }

    [HttpPost("photo")]
    public async Task<IActionResult> UploadPhoto(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No se recibió ningún archivo" });

        var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp" };
        if (!allowedTypes.Contains(file.ContentType))
            return BadRequest(new { message = "Solo se permiten imágenes JPG, PNG o WEBP" });

        if (file.Length > 5 * 1024 * 1024)
            return BadRequest(new { message = "El archivo no puede superar 5MB" });

        byte[] imageBytes;
        using (var ms = new MemoryStream())
        {
            await file.CopyToAsync(ms);
            imageBytes = ms.ToArray();
        }

        var analysis = await AnalyzeImageAsync(imageBytes);
        if (!analysis.IsAppropriate)
            return BadRequest(new { message = analysis.Reason });

        var fileName = $"donations/photos/{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        var bucketName = _config["Firebase:StorageBucket"];

        var storageClient = StorageClient.Create();
        using var stream = new MemoryStream(imageBytes);

        await storageClient.UploadObjectAsync(
            bucket: bucketName,
            objectName: fileName,
            contentType: file.ContentType,
            source: stream
        );

        var publicUrl = $"https://firebasestorage.googleapis.com/v0/b/{bucketName}/o/{Uri.EscapeDataString(fileName)}?alt=media";

        return Ok(new
        {
            url = publicUrl,
            analysis = new
            {
                labels = analysis.Labels,
                safe = analysis.IsAppropriate
            }
        });
    }

    private static async Task<ImageAnalysisResult> AnalyzeImageAsync(byte[] imageBytes)
    {
        var client = await ImageAnnotatorClient.CreateAsync();
        var image = Image.FromBytes(imageBytes);

        var safeSearchTask = client.DetectSafeSearchAsync(image);
        var labelsTask = client.DetectLabelsAsync(image);

        await Task.WhenAll(safeSearchTask, labelsTask);

        var safeSearch = await safeSearchTask;
        var labels = await labelsTask;

        // Rechazar contenido inapropiado por SafeSearch
        if (safeSearch.Adult >= Likelihood.Possible ||
            safeSearch.Violence >= Likelihood.Possible ||
            safeSearch.Racy >= Likelihood.Possible)
        {
            return new ImageAnalysisResult
            {
                IsAppropriate = false,
                Reason = "La imagen contiene contenido inapropiado y no puede ser publicada"
            };
        }

        // Extraer etiquetas con más del 70% de confianza
        var labelDescriptions = labels
            .Where(l => l.Score > 0.7f)
            .Select(l => l.Description)
            .Take(5)
            .ToList();

        // Etiquetas prohibidas
        var forbiddenLabels = new[]
        {
            "Gun", "Firearm", "Weapon", "Pistol", "Rifle", "Shotgun",
            "Trigger", "Ammunition", "Knife", "Blade", "Explosive",
            "Bomb", "Grenade", "Drug", "Narcotics", "Cannabis",
            "Syringe", "Nudity", "Tobacco", "Cigarette"
        };

        var foundForbidden = labelDescriptions
            .FirstOrDefault(l => forbiddenLabels.Any(f =>
                l.Contains(f, StringComparison.OrdinalIgnoreCase)));

        if (foundForbidden != null)
        {
            return new ImageAnalysisResult
            {
                IsAppropriate = false,
                Reason = $"La imagen contiene contenido no permitido: {foundForbidden}"
            };
        }

        return new ImageAnalysisResult
        {
            IsAppropriate = true,
            Labels = labelDescriptions
        };
    }
}

public class ImageAnalysisResult
{
    public bool IsAppropriate { get; set; }
    public string Reason { get; set; } = string.Empty;
    public List<string> Labels { get; set; } = new();
}