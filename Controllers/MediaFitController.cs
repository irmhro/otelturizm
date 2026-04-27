using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace otelturizmnew.Controllers;

/// <summary>
/// Güvenli path ile wwwroot altı görseller için anlık resize (paket 201 imageproxy benzeri).
/// </summary>
public sealed class MediaFitController : Controller
{
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<MediaFitController> _logger;

    public MediaFitController(IWebHostEnvironment environment, ILogger<MediaFitController> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    [HttpGet("/media/fit")]
    [ResponseCache(Duration = 86400, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> Fit([FromQuery] string path, [FromQuery] int w = 0, [FromQuery] int h = 0, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return BadRequest();
        }

        var trimmed = path.Trim().TrimStart('/');
        if (trimmed.Contains("..", StringComparison.Ordinal) || trimmed.StartsWith('\\'))
        {
            return BadRequest();
        }

        var ext = Path.GetExtension(trimmed).ToLowerInvariant();
        if (ext is not (".jpg" or ".jpeg" or ".png" or ".webp"))
        {
            return BadRequest();
        }

        var maxEdge = Math.Clamp(Math.Max(w, h), 64, 1600);
        if (maxEdge <= 0)
        {
            maxEdge = 800;
        }

        var physical = Path.GetFullPath(Path.Combine(_environment.WebRootPath, trimmed.Replace('/', Path.DirectorySeparatorChar)));
        var root = Path.GetFullPath(_environment.WebRootPath);
        if (!physical.StartsWith(root, StringComparison.OrdinalIgnoreCase) || !System.IO.File.Exists(physical))
        {
            _logger.LogWarning("MEDIA_FIT denied or missing path={Path}", trimmed);
            return NotFound();
        }

        await using var input = System.IO.File.OpenRead(physical);
        using var image = await Image.LoadAsync(input, cancellationToken);
        image.Mutate(ctx => ctx.Resize(new ResizeOptions
        {
            Mode = ResizeMode.Max,
            Size = new Size(maxEdge, maxEdge)
        }));

        await using var ms = new MemoryStream();
        if (ext is ".png")
        {
            await image.SaveAsync(ms, new PngEncoder(), cancellationToken);
            return File(ms.ToArray(), "image/png");
        }

        if (ext is ".webp")
        {
            await image.SaveAsync(ms, new WebpEncoder { Quality = 82 }, cancellationToken);
            return File(ms.ToArray(), "image/webp");
        }

        await image.SaveAsync(ms, new JpegEncoder { Quality = 82 }, cancellationToken);
        return File(ms.ToArray(), "image/jpeg");
    }
}
