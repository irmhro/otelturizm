using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using otelturizmnew.Constants;
using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Controllers.SecureFiles;

[Route("secure-files")]
[Authorize]
public class SecureFilesController : Controller
{
    private readonly ISecureFileService _secureFileService;

    public SecureFilesController(ISecureFileService secureFileService)
    {
        _secureFileService = secureFileService;
    }

    [HttpGet("{token}")]
    public async Task<IActionResult> Download(string token, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId <= 0)
        {
            return NotFound();
        }

        var accountType = User.FindFirstValue(AuthClaimTypes.AccountType) ?? "user";
        var file = await _secureFileService.ResolveDownloadAsync(token, userId, accountType, cancellationToken);
        if (file is null)
        {
            return NotFound();
        }

        Response.Headers.CacheControl = "no-store, no-cache, must-revalidate";
        Response.Headers.Pragma = "no-cache";
        Response.Headers["X-Content-Type-Options"] = "nosniff";

        var safeName = SanitizeDownloadName(file.OriginalFileName);
        Response.Headers["Content-Disposition"] = BuildContentDispositionAttachment(safeName);
        return PhysicalFile(file.AbsolutePath, file.ContentType, safeName, enableRangeProcessing: true);
    }

    private long GetCurrentUserId()
    {
        var raw = User.FindFirstValue(AuthClaimTypes.UserId) ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return long.TryParse(raw, out var userId) ? userId : 0;
    }

    private static string SanitizeDownloadName(string? fileName)
    {
        var name = Path.GetFileName(fileName ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return "download";
        }

        // çok uzun isimler browserlarda sorun çıkarabiliyor
        if (name.Length > 180)
        {
            var ext = Path.GetExtension(name);
            var baseName = Path.GetFileNameWithoutExtension(name);
            baseName = baseName.Length > 160 ? baseName[..160] : baseName;
            name = baseName + ext;
        }

        return name;
    }

    private static string BuildContentDispositionAttachment(string fileName)
    {
        // RFC 5987
        var encoded = Uri.EscapeDataString(fileName);
        return $"attachment; filename=\"{fileName.Replace("\"", string.Empty)}\"; filename*=UTF-8''{encoded}";
    }
}
