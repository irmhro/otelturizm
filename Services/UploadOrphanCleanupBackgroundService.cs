using System.Globalization;
using Microsoft.Data.SqlClient;
using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Services;

public sealed class UploadOrphanCleanupBackgroundService : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<UploadOrphanCleanupBackgroundService> _logger;

    public UploadOrphanCleanupBackgroundService(IConfiguration configuration, IWebHostEnvironment environment, ILogger<UploadOrphanCleanupBackgroundService> logger)
    {
        _configuration = configuration;
        _environment = environment;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var enabled = _configuration.GetValue("Uploads:OrphanCleanupEnabled", false);
                if (enabled)
                {
                    await RunOnceAsync(stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Upload orphan cleanup failed.");
            }

            // günde 1
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }

    private async Task RunOnceAsync(CancellationToken ct)
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        var minAgeHours = _configuration.GetValue("Uploads:OrphanCleanupMinAgeHours", 168); // 7 gün
        var cutoff = DateTime.UtcNow.AddHours(-Math.Max(1, minAgeHours));

        var uploadsRoot = Path.Combine(_environment.WebRootPath, "uploads");
        if (!Directory.Exists(uploadsRoot))
        {
            return;
        }

        var referenced = await LoadReferencedUploadUrlsAsync(connectionString, ct);
        var deleted = 0;
        var scanned = 0;

        foreach (var filePath in Directory.EnumerateFiles(uploadsRoot, "*.*", SearchOption.AllDirectories))
        {
            ct.ThrowIfCancellationRequested();
            scanned++;

            var ext = Path.GetExtension(filePath);
            if (string.IsNullOrWhiteSpace(ext)) continue;
            if (!ext.Equals(".webp", StringComparison.OrdinalIgnoreCase)
                && !ext.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
                && !ext.Equals(".jpeg", StringComparison.OrdinalIgnoreCase)
                && !ext.Equals(".png", StringComparison.OrdinalIgnoreCase)
                && !ext.Equals(".gif", StringComparison.OrdinalIgnoreCase)
                && !ext.Equals(".svg", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var fi = new FileInfo(filePath);
            if (!fi.Exists) continue;
            if (fi.LastWriteTimeUtc > cutoff) continue;

            var relativeUrl = "/uploads/" + Path.GetRelativePath(uploadsRoot, filePath).Replace('\\', '/');
            if (referenced.Contains(relativeUrl))
            {
                continue;
            }

            try
            {
                fi.Delete();
                deleted++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not delete orphan upload {Path}", filePath);
            }
        }

        _logger.LogInformation("UPLOAD_ORPHAN_CLEANUP scanned={Scanned} deleted={Deleted} cutoffUtc={Cutoff}",
            scanned, deleted, cutoff.ToString("O", CultureInfo.InvariantCulture));
    }

    private static async Task<HashSet<string>> LoadReferencedUploadUrlsAsync(string connectionString, CancellationToken ct)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync(ct);

        // Best-effort: şema farklılıklarına dayanıklı olsun diye TRY/CATCH yerine IF OBJECT_ID ile ilerliyoruz.
        const string sql = @"
            DECLARE @urls TABLE(url nvarchar(2048));

            IF OBJECT_ID('dbo.users', 'U') IS NOT NULL AND COL_LENGTH('dbo.users', 'profil_resim_url') IS NOT NULL
                INSERT INTO @urls(url)
                SELECT profil_resim_url FROM dbo.users WHERE profil_resim_url LIKE '/uploads/%';

            IF OBJECT_ID('dbo.otel_gorselleri', 'U') IS NOT NULL AND COL_LENGTH('dbo.otel_gorselleri', 'gorsel_url') IS NOT NULL
                INSERT INTO @urls(url)
                SELECT gorsel_url FROM dbo.otel_gorselleri WHERE gorsel_url LIKE '/uploads/%';

            IF OBJECT_ID('dbo.oda_gorselleri', 'U') IS NOT NULL AND COL_LENGTH('dbo.oda_gorselleri', 'gorsel_url') IS NOT NULL
                INSERT INTO @urls(url)
                SELECT gorsel_url FROM dbo.oda_gorselleri WHERE gorsel_url LIKE '/uploads/%';

            IF OBJECT_ID('dbo.oteller', 'U') IS NOT NULL AND COL_LENGTH('dbo.oteller', 'kapak_fotografi') IS NOT NULL
                INSERT INTO @urls(url)
                SELECT kapak_fotografi FROM dbo.oteller WHERE kapak_fotografi LIKE '/uploads/%';

            SELECT DISTINCT url FROM @urls WHERE url IS NOT NULL AND url <> '';";

        await using var cmd = new SqlCommand(sql, conn) { CommandTimeout = 10 };
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            if (!reader.IsDBNull(0))
            {
                var url = reader.GetString(0);
                if (!string.IsNullOrWhiteSpace(url))
                {
                    set.Add(url.Trim());
                }
            }
        }

        return set;
    }
}

