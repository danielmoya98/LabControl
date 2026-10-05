using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;

namespace LabControl.Api.Controllers;

[ApiController]
[Route("api/actualizaciones")]
public class ActualizacionesController : ControllerBase
{
    private readonly IWebHostEnvironment _environment;
    private readonly IConfiguration _configuration;

    public ActualizacionesController(IWebHostEnvironment environment, IConfiguration configuration)
    {
        _environment = environment;
        _configuration = configuration;
    }

    private string ObtenerRutaDirectorioUpdates()
    {
        var path = Path.Combine(_environment.ContentRootPath, "Updates");
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
        return path;
    }

    private (string FilePath, string FileName, string Version, DateTime FechaUtc) ObtenerUltimoBinarioDisponible()
    {
        var dir = ObtenerRutaDirectorioUpdates();
        var files = Directory.GetFiles(dir, "*.exe")
            .Select(f => new FileInfo(f))
            .OrderByDescending(f => f.LastWriteTimeUtc)
            .ToList();

        if (files.Count > 0)
        {
            var f = files[0];
            return (f.FullName, f.Name, "1.0.1", f.LastWriteTimeUtc);
        }

        // Fallback: verificar en dist/LabControl-Kiosk-Client si se compiló localmente
        var distPath = Path.GetFullPath(Path.Combine(_environment.ContentRootPath, "..", "..", "dist", "LabControl-Kiosk-Client", "LabControl.Client.Kiosk.exe"));
        if (System.IO.File.Exists(distPath))
        {
            var f = new FileInfo(distPath);
            return (distPath, "LabControl.Client.Kiosk.exe", "1.0.0", f.LastWriteTimeUtc);
        }

        return (string.Empty, string.Empty, "0.0.0", DateTime.MinValue);
    }

    [HttpGet("info")]
    public IActionResult ObtenerInfoActualizacion()
    {
        var (filePath, fileName, version, fechaUtc) = ObtenerUltimoBinarioDisponible();

        if (string.IsNullOrEmpty(filePath) || !System.IO.File.Exists(filePath))
        {
            return Ok(new
            {
                disponible = false,
                mensaje = "No hay paquetes de actualización publicados en el servidor."
            });
        }

        var fileInfo = new FileInfo(filePath);
        string hashSha256;
        using (var sha = SHA256.Create())
        using (var stream = System.IO.File.OpenRead(filePath))
        {
            var hashBytes = sha.ComputeHash(stream);
            hashSha256 = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        }

        return Ok(new
        {
            disponible = true,
            version,
            nombreArchivo = fileName,
            tamanoBytes = fileInfo.Length,
            tamanoMb = Math.Round((double)fileInfo.Length / (1024 * 1024), 2),
            sha256 = hashSha256,
            fechaPublicacionUtc = fechaUtc,
            urlDescarga = "api/actualizaciones/descargar-cliente"
        });
    }

    [HttpGet("descargar-cliente")]
    public IActionResult DescargarCliente()
    {
        var (filePath, fileName, _, _) = ObtenerUltimoBinarioDisponible();

        if (string.IsNullOrEmpty(filePath) || !System.IO.File.Exists(filePath))
        {
            return NotFound(new { error = "No se encontró ningún binario o instalador del cliente para descargar." });
        }

        var stream = System.IO.File.OpenRead(filePath);
        return File(stream, "application/octet-stream", fileName);
    }

    [HttpPost("subir")]
    [RequestSizeLimit(150_000_000)] // Hasta 150 MB para instalador autónomo
    public async Task<IActionResult> SubirNuevaVersion([FromForm] IFormFile archivo, [FromForm] string? version = null)
    {
        if (archivo == null || archivo.Length == 0)
        {
            return BadRequest(new { error = "Debe proporcionar un archivo ejecutable válido." });
        }

        var dir = ObtenerRutaDirectorioUpdates();
        var fileName = Path.GetFileName(archivo.FileName);
        if (!fileName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { error = "Solo se admiten archivos ejecutables (.exe)." });
        }

        var destino = Path.Combine(dir, fileName);
        using (var stream = new FileStream(destino, FileMode.Create))
        {
            await archivo.CopyToAsync(stream);
        }

        string hashSha256;
        using (var sha = SHA256.Create())
        using (var stream = System.IO.File.OpenRead(destino))
        {
            var hashBytes = sha.ComputeHash(stream);
            hashSha256 = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        }

        return Ok(new
        {
            message = $"Versión '{version ?? fileName}' subida correctamente y lista para distribución OTA.",
            fileName,
            sha256 = hashSha256
        });
    }
}
