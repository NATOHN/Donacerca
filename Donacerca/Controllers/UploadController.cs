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
        // Validaciones básicas
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No se recibió ningún archivo" });

        var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp" };
        if (!allowedTypes.Contains(file.ContentType))
            return BadRequest(new { message = "Solo se permiten imágenes JPG, PNG o WEBP" });

        if (file.Length > 5 * 1024 * 1024)
            return BadRequest(new { message = "El archivo no puede superar 5MB" });

        // Leer bytes una sola vez para reusar en Vision y Storage
        byte[] imageBytes;
        using (var ms = new MemoryStream())
        {
            await file.CopyToAsync(ms);
            imageBytes = ms.ToArray();
        }

        // Analizar con Google Vision antes de subir
        var analysis = await AnalyzeImageAsync(imageBytes);
        if (!analysis.IsAppropriate)
            return BadRequest(new { message = analysis.Reason });

        // Subir a Firebase Storage
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

        var publicUrl = $"https://storage.googleapis.com/{bucketName}/{fileName}";

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

        // Ejecutar SafeSearch y Labels en paralelo
        var safeSearchTask = client.DetectSafeSearchAsync(image);
        var labelsTask = client.DetectLabelsAsync(image);

        await Task.WhenAll(safeSearchTask, labelsTask);

        var safeSearch = await safeSearchTask;
        var labels = await labelsTask;

        // Rechazar si contiene contenido inapropiado
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