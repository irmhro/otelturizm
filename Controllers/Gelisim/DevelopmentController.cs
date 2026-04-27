using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using otelturizmnew.Models.Gelisim;
using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Controllers.Gelisim;

[Route("gelisim")]
public sealed class DevelopmentController : Controller
{
    private readonly IWebHostEnvironment _environment;
    private readonly IDevelopmentAccessService _developmentAccessService;

    public DevelopmentController(IWebHostEnvironment environment, IDevelopmentAccessService developmentAccessService)
    {
        _environment = environment;
        _developmentAccessService = developmentAccessService;
    }

    [HttpGet("")]
    public IActionResult Index()
    {
        return View("~/Views/Gelisim/Index.cshtml", BuildModel());
    }

    [HttpPost("kilit-ac")]
    [IgnoreAntiforgeryToken]
    public IActionResult Unlock([FromBody] DevelopmentUnlockRequest? request)
    {
        if (_developmentAccessService.TryUnlock(HttpContext, request?.Code, out var expiresAt))
        {
            return Json(new
            {
                success = true,
                expiresAt = expiresAt.ToString("O")
            });
        }

        Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Json(new { success = false, message = "Kod hatalı." });
    }

    [HttpPost("kilidi-kapat")]
    [IgnoreAntiforgeryToken]
    public IActionResult Lock()
    {
        _developmentAccessService.RevokeAccess(HttpContext);
        return Json(new { success = true });
    }

    private DevelopmentPageViewModel BuildModel(string? error = null)
    {
        ViewData["Title"] = "Gelişim";
        ViewData["PageCss"] = "gelisim-page";
        ViewData["PageCssMobile"] = "gelisim-page.mobile";
        ViewData["HeaderContext"] = "auth";

        var model = new DevelopmentPageViewModel
        {
            PasswordError = error ?? string.Empty,
            IsUnlocked = _developmentAccessService.TryGetAccessExpiration(HttpContext, out var expiresAt)
        };

        model.AccessExpiresAt = expiresAt;

        if (model.IsUnlocked)
        {
            var path = Path.Combine(_environment.ContentRootPath, "improvements-first-200.md");
            if (System.IO.File.Exists(path))
            {
                var raw = System.IO.File.ReadAllText(path, Encoding.UTF8);
                model.RenderedHtml = RenderMarkdown(raw);
            }
            else
            {
                model.RenderedHtml = "<p>Dosya bulunamadı: improvements-first-200.md</p>";
            }
        }

        return model;
    }

    private static string RenderMarkdown(string markdown)
    {
        var lines = markdown.Replace("\r\n", "\n").Split('\n');
        var sb = new StringBuilder();
        var inUnorderedList = false;
        var inOrderedList = false;
        var inCodeBlock = false;

        void CloseLists()
        {
            if (inUnorderedList)
            {
                sb.AppendLine("</ul>");
                inUnorderedList = false;
            }

            if (inOrderedList)
            {
                sb.AppendLine("</ol>");
                inOrderedList = false;
            }
        }

        foreach (var sourceLine in lines)
        {
            var line = sourceLine.TrimEnd();
            var trimmed = line.Trim();

            if (trimmed.StartsWith("```", StringComparison.Ordinal))
            {
                CloseLists();
                if (!inCodeBlock)
                {
                    sb.AppendLine("<pre class=\"dev-markdown-code\"><code>");
                    inCodeBlock = true;
                }
                else
                {
                    sb.AppendLine("</code></pre>");
                    inCodeBlock = false;
                }
                continue;
            }

            if (inCodeBlock)
            {
                sb.AppendLine(WebUtility.HtmlEncode(line));
                continue;
            }

            if (string.IsNullOrWhiteSpace(trimmed))
            {
                CloseLists();
                continue;
            }

            if (trimmed.StartsWith("### ", StringComparison.Ordinal))
            {
                CloseLists();
                sb.Append("<h3>")
                    .Append(FormatInline(trimmed[4..]))
                    .AppendLine("</h3>");
                continue;
            }

            if (trimmed.StartsWith("## ", StringComparison.Ordinal))
            {
                CloseLists();
                sb.Append("<h2>")
                    .Append(FormatInline(trimmed[3..]))
                    .AppendLine("</h2>");
                continue;
            }

            if (trimmed.StartsWith("# ", StringComparison.Ordinal))
            {
                CloseLists();
                sb.Append("<h1>")
                    .Append(FormatInline(trimmed[2..]))
                    .AppendLine("</h1>");
                continue;
            }

            var orderedMatch = Regex.Match(trimmed, @"^(\d+)\.\s+(.*)$");
            if (orderedMatch.Success)
            {
                if (!inOrderedList)
                {
                    CloseLists();
                    sb.AppendLine("<ol>");
                    inOrderedList = true;
                }

                sb.Append("<li>")
                    .Append(FormatInline(orderedMatch.Groups[2].Value))
                    .AppendLine("</li>");
                continue;
            }

            if (trimmed.StartsWith("- ", StringComparison.Ordinal) || trimmed.StartsWith("* ", StringComparison.Ordinal))
            {
                if (!inUnorderedList)
                {
                    CloseLists();
                    sb.AppendLine("<ul>");
                    inUnorderedList = true;
                }

                sb.Append("<li>")
                    .Append(FormatInline(trimmed[2..]))
                    .AppendLine("</li>");
                continue;
            }

            CloseLists();
            sb.Append("<p>")
                .Append(FormatInline(trimmed))
                .AppendLine("</p>");
        }

        CloseLists();

        if (inCodeBlock)
        {
            sb.AppendLine("</code></pre>");
        }

        return sb.ToString();
    }

    private static string FormatInline(string text)
    {
        var encoded = WebUtility.HtmlEncode(text);
        encoded = Regex.Replace(encoded, @"\*\*(.+?)\*\*", "<strong>$1</strong>");
        encoded = Regex.Replace(encoded, @"`(.+?)`", "<code>$1</code>");
        encoded = encoded.Replace("⏳", "<span class=\"dev-markdown-flag is-pending\">⏳</span>", StringComparison.Ordinal)
                         .Replace("✅", "<span class=\"dev-markdown-flag is-done\">✅</span>", StringComparison.Ordinal);
        return encoded;
    }

    public sealed class DevelopmentUnlockRequest
    {
        public string? Code { get; init; }
    }
}
