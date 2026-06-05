using Google.Cloud.Storage.V1;
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

        var fileName = $"donations/photos/{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        var bucketName = _config["Firebase:StorageBucket"];

        var storageClient = StorageClient.Create();
        using var stream = file.OpenReadStream();

        await storageClient.UploadObjectAsync(
            bucket: bucketName,
            objectName: fileName,
            contentType: file.ContentType,
            source: stream
        );

        var publicUrl = $"https://storage.googleapis.com/{bucketName}/{fileName}";
        return Ok(new { url = publicUrl });
    }
}