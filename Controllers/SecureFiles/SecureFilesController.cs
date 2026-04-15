using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using otelturizmnew.Constants;
using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Controllers.SecureFiles;

[Route("secure-files")]
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
        return PhysicalFile(file.AbsolutePath, file.ContentType, file.OriginalFileName, enableRangeProcessing: true);
    }

    private long GetCurrentUserId()
    {
        var raw = User.FindFirstValue(AuthClaimTypes.UserId) ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return long.TryParse(raw, out var userId) ? userId : 0;
    }
}
