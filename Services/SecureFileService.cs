using System.Globalization;
using System.Security.Cryptography;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Http;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using otelturizmnew.Models.Messages;
using otelturizmnew.Services.Abstractions;
using otelturizmnew.Utils;

namespace otelturizmnew.Services;

public class SecureFileService : ISecureFileService
{
    private readonly string _connectionString;
    private readonly IWebHostEnvironment _environment;
    private readonly IUploadScanService _uploadScanService;
    private readonly IUploadAuditService _uploadAuditService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    private const long MaxUploadBytes = 25 * 1024 * 1024; // 25 MB (mesaj eki / dekont / sözleşme gibi kullanım için)
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf",
        ".jpg", ".jpeg", ".png", ".webp", ".gif"
    };

    public SecureFileService(
        IConfiguration configuration,
        IWebHostEnvironment environment,
        IUploadScanService uploadScanService,
        IUploadAuditService uploadAuditService,
        IHttpContextAccessor httpContextAccessor)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection tanimli degil.");
        _environment = environment;
        _uploadScanService = uploadScanService;
        _uploadAuditService = uploadAuditService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<StoredSecureFileResult> SaveAsync(IFormFile file, SecureFileSaveRequest request, CancellationToken cancellationToken = default)
    {
        if (file is null || file.Length <= 0)
        {
            throw new InvalidOperationException("Kaydedilecek dosya bulunamadi.");
        }

        if (file.Length > MaxUploadBytes)
        {
            throw new InvalidOperationException("Dosya boyutu cok buyuk. En fazla 25 MB yukleyebilirsiniz.");
        }

        var safeCategory = NormalizeCategory(request.Category);
        if (string.IsNullOrWhiteSpace(safeCategory))
        {
            throw new InvalidOperationException("Dosya kategorisi gecersiz.");
        }

        var safeExtension = NormalizeAndValidateExtension(file.FileName, file.ContentType);
        await ValidateMagicHeaderAsync(file, safeExtension, cancellationToken);
        var isImage = IsImageContentType(file.ContentType);
        var storedExtension = isImage ? ".webp" : safeExtension;
        var storedContentType = isImage ? "image/webp" : string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType;
        var storedName = $"{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}{storedExtension}";
        var root = request.HotelId.HasValue && request.HotelId.Value > 0
            ? MediaStoragePaths.HotelFilesDirectory(_environment.WebRootPath, request.HotelId.Value, safeCategory)
            : BuildSecureRoot(safeCategory, request);
        Directory.CreateDirectory(root);
        var absolutePath = Path.Combine(root, storedName);

        if (isImage)
        {
            await SaveImageAsWebpAsync(file, absolutePath, cancellationToken);
        }
        else
        {
            await AtomicFileWriter.CopyFromFormFileAtomicAsync(file, absolutePath, cancellationToken);
        }

        var hash = await AtomicFileWriter.ComputeSha256Async(absolutePath, cancellationToken);
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = @"
            INSERT INTO [dbo].[GUVENLI_DOSYA_VARLIKLARI]
            (
                [BAGLAM_TABLO], [BAGLAM_KAYIT_ID], [SAHIBI_KULLANICI_ID], [SAHIBI_FIRMA_ID],
                [KATEGORI], [GORUNURLUK_KAPSAMI], [ORIJINAL_DOSYA_ADI], [DEPOLANAN_DOSYA_ADI], [DEPOLAMA_YOLU],
                [MIME_TIPI], [DOSYA_UZANTISI], [DOSYA_BOYUTU], [SHA256_OZETI], [GORSEL_MI]
            )
            VALUES
            (
                @contextTable, @contextId, @ownerUserId, @ownerFirmaId,
                @category, @scope, @originalFileName, @storedFileName, @storagePath,
                @contentType, @extension, @size, @hash, @isImage
            );
            SELECT CAST(SCOPE_IDENTITY() AS bigint);";

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@contextTable", request.ContextTable);
        command.Parameters.AddWithValue("@contextId", request.ContextId);
        command.Parameters.AddWithValue("@ownerUserId", request.OwnerUserId.HasValue ? request.OwnerUserId.Value : DBNull.Value);
        command.Parameters.AddWithValue("@ownerFirmaId", request.OwnerFirmaId.HasValue ? request.OwnerFirmaId.Value : DBNull.Value);
        command.Parameters.AddWithValue("@category", safeCategory);
        command.Parameters.AddWithValue("@scope", request.VisibilityScope);
        command.Parameters.AddWithValue("@originalFileName", Path.GetFileName(file.FileName));
        command.Parameters.AddWithValue("@storedFileName", storedName);
        command.Parameters.AddWithValue("@storagePath", absolutePath);
        command.Parameters.AddWithValue("@contentType", storedContentType);
        command.Parameters.AddWithValue("@extension", storedExtension);
        command.Parameters.AddWithValue("@size", file.Length);
        command.Parameters.AddWithValue("@hash", hash);
        command.Parameters.AddWithValue("@isImage", isImage ? 1 : 0);

        var fileId = Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken) ?? 0L, CultureInfo.InvariantCulture);
        var stored = new StoredSecureFileResult
        {
            FileId = fileId,
            StoredPath = absolutePath,
            OriginalFileName = Path.GetFileName(file.FileName),
            ContentType = storedContentType,
            IsImage = isImage,
            SizeInBytes = file.Length
        };

        // p78: Upload scan kancası (opsiyonel) — default no-op.
        await _uploadScanService.ScanOrThrowAsync(absolutePath, stored, cancellationToken);

        try
        {
            var http = _httpContextAccessor.HttpContext;
            await _uploadAuditService.RecordAsync(new UploadAuditEvent(
                Kind: "secure-file",
                Category: safeCategory,
                SizeBytes: file.Length,
                StoredName: storedName,
                StoredPathOrUrl: absolutePath,
                ContentType: stored.ContentType,
                Extension: storedExtension,
                Sha256: hash,
                OwnerUserId: request.OwnerUserId,
                OwnerFirmaId: request.OwnerFirmaId,
                ContextTable: request.ContextTable,
                ContextId: request.ContextId,
                RemoteIp: http?.Connection.RemoteIpAddress?.ToString(),
                UserAgent: http?.Request.Headers.UserAgent.ToString()
            ), cancellationToken);
        }
        catch
        {
            // audit fail-safe
        }
        return stored;
    }

    public async Task<string> CreateAccessUrlAsync(long fileId, long viewerUserId, string viewerAccountType, CancellationToken cancellationToken = default)
    {
        var token = Guid.NewGuid().ToString("N");
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = @"
            INSERT INTO [dbo].[GUVENLI_DOSYA_ERISIM_TOKENLARI]
            (
                [GUVENLI_DOSYA_ID], [ERISIM_TOKENI], [KULLANICI_ID], [HESAP_TIPI],
                [GECERLILIK_TARIHI], [MAKSIMUM_KULLANIM_SAYISI]
            )
            VALUES
            (
                @fileId, @token, @userId, @accountType,
                DATEADD(MINUTE, 30, SYSUTCDATETIME()), 30
            );";

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@fileId", fileId);
        command.Parameters.AddWithValue("@token", token);
        command.Parameters.AddWithValue("@userId", viewerUserId);
        command.Parameters.AddWithValue("@accountType", viewerAccountType);
        await command.ExecuteNonQueryAsync(cancellationToken);

        return $"/secure-files/{token}";
    }

    public async Task<SecureFileDownloadResult?> ResolveDownloadAsync(string token, long viewerUserId, string viewerAccountType, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = @"
            SELECT TOP (1) gfv.[DEPOLAMA_YOLU], gfv.[ORIJINAL_DOSYA_ADI], gfv.[MIME_TIPI],
                   gdet.id, gdet.[KULLANIM_SAYISI], gdet.[MAKSIMUM_KULLANIM_SAYISI]
            FROM [dbo].[GUVENLI_DOSYA_ERISIM_TOKENLARI] gdet
            INNER JOIN [dbo].[GUVENLI_DOSYA_VARLIKLARI] gfv ON gfv.id = gdet.[GUVENLI_DOSYA_ID]
            WHERE gdet.[ERISIM_TOKENI] = @token
              AND gdet.[IPTAL_TARIHI] IS NULL
              AND gdet.[KULLANICI_ID] = @userId
              AND gdet.[HESAP_TIPI] = @accountType
              AND gdet.[GECERLILIK_TARIHI] >= SYSUTCDATETIME()
              AND gfv.[AKTIF_MI] = 1;";

        long accessTokenId;
        long usageCount;
        int? maxUsage;
        string path;
        string originalName;
        string contentType;

        await using (var command = new SqlCommand(sql, connection))
        {
            command.Parameters.AddWithValue("@token", token);
            command.Parameters.AddWithValue("@userId", viewerUserId);
            command.Parameters.AddWithValue("@accountType", viewerAccountType);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                return null;
            }

            path = reader.GetString(0);
            originalName = reader.GetString(1);
            contentType = reader.GetString(2);
            accessTokenId = Convert.ToInt64(reader.GetValue(3), CultureInfo.InvariantCulture);
            usageCount = reader.IsDBNull(4) ? 0L : Convert.ToInt64(reader.GetValue(4), CultureInfo.InvariantCulture);
            maxUsage = reader.IsDBNull(5) ? null : Convert.ToInt32(reader.GetValue(5), CultureInfo.InvariantCulture);
        }

        if (maxUsage.HasValue && usageCount >= maxUsage.Value)
        {
            return null;
        }

        if (!File.Exists(path))
        {
            return null;
        }

        await using (var updateCommand = new SqlCommand(@"
            UPDATE [dbo].[GUVENLI_DOSYA_ERISIM_TOKENLARI]
            SET [KULLANIM_SAYISI] = [KULLANIM_SAYISI] + 1,
                [SON_ERISIM_TARIHI] = SYSUTCDATETIME()
            WHERE id = @id;", connection))
        {
            updateCommand.Parameters.AddWithValue("@id", accessTokenId);
            await updateCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        return new SecureFileDownloadResult
        {
            AbsolutePath = path,
            OriginalFileName = originalName,
            ContentType = contentType
        };
    }

    public async Task<bool> DeleteOwnedFileAsync(long fileId, long ownerUserId, string category, CancellationToken cancellationToken = default)
    {
        if (fileId <= 0 || ownerUserId <= 0)
        {
            return false;
        }

        var safeCategory = NormalizeCategory(category);
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        string? path = null;
        await using (var lookup = new SqlCommand("""
            SELECT TOP (1) [DEPOLAMA_YOLU]
            FROM [dbo].[GUVENLI_DOSYA_VARLIKLARI]
            WHERE id = @fileId
              AND [SAHIBI_KULLANICI_ID] = @ownerUserId
              AND [KATEGORI] = @category;
            """, connection))
        {
            lookup.Parameters.AddWithValue("@fileId", fileId);
            lookup.Parameters.AddWithValue("@ownerUserId", ownerUserId);
            lookup.Parameters.AddWithValue("@category", safeCategory);
            path = Convert.ToString(await lookup.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture);
        }

        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            await using (var tokenDelete = new SqlCommand("DELETE FROM [dbo].[GUVENLI_DOSYA_ERISIM_TOKENLARI] WHERE [GUVENLI_DOSYA_ID] = @fileId;", connection, (SqlTransaction)transaction))
            {
                tokenDelete.Parameters.AddWithValue("@fileId", fileId);
                await tokenDelete.ExecuteNonQueryAsync(cancellationToken);
            }

            await using (var fileDelete = new SqlCommand("""
                DELETE FROM [dbo].[GUVENLI_DOSYA_VARLIKLARI]
                WHERE id = @fileId
                  AND [SAHIBI_KULLANICI_ID] = @ownerUserId
                  AND [KATEGORI] = @category;
                """, connection, (SqlTransaction)transaction))
            {
                fileDelete.Parameters.AddWithValue("@fileId", fileId);
                fileDelete.Parameters.AddWithValue("@ownerUserId", ownerUserId);
                fileDelete.Parameters.AddWithValue("@category", safeCategory);
                var affected = await fileDelete.ExecuteNonQueryAsync(cancellationToken);
                if (affected <= 0)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return false;
                }
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // Dosya kilitliyse DB temizliği korunur; bakım görevi artık dosya kalıntısını toplayabilir.
        }

        return true;
    }

    private static bool IsImageContentType(string? contentType)
        => !string.IsNullOrWhiteSpace(contentType) && contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);

    private static async Task SaveImageAsWebpAsync(IFormFile file, string absolutePath, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(absolutePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var tempPath = $"{absolutePath}.{Guid.NewGuid():N}.tmp";
        await using (var input = file.OpenReadStream())
        using (var image = await Image.LoadAsync(input, cancellationToken))
        {
            image.Mutate(x =>
            {
                x.AutoOrient();
                if (image.Width > 1600 || image.Height > 1600)
                {
                    x.Resize(new ResizeOptions
                    {
                        Mode = ResizeMode.Max,
                        Size = new Size(1600, 1600)
                    });
                }
            });
            await image.SaveAsWebpAsync(tempPath, cancellationToken);
        }

        if (File.Exists(absolutePath))
        {
            File.Delete(absolutePath);
        }

        File.Move(tempPath, absolutePath);
    }

    private string BuildSecureRoot(string safeCategory, SecureFileSaveRequest request)
    {
        if (string.Equals(safeCategory, "profile", StringComparison.OrdinalIgnoreCase)
            && request.OwnerUserId.HasValue
            && request.OwnerUserId.Value > 0)
        {
            return Path.Combine(
                _environment.ContentRootPath,
                "App_Data",
                "secure-storage",
                "profile",
                request.OwnerUserId.Value.ToString(CultureInfo.InvariantCulture));
        }

        return Path.Combine(_environment.ContentRootPath, "App_Data", "secure-storage", safeCategory);
    }

    private static string NormalizeCategory(string? category)
    {
        var raw = (category ?? string.Empty).Trim();
        if (raw.Length == 0) return string.Empty;

        // Sadece klasör adı olarak güvenli karakterleri kabul et.
        Span<char> buffer = stackalloc char[Math.Min(raw.Length, 64)];
        var idx = 0;
        foreach (var ch in raw)
        {
            if (idx >= buffer.Length) break;
            if ((ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch <= 'Z') || (ch >= '0' && ch <= '9') || ch is '-' or '_')
            {
                buffer[idx++] = char.ToLowerInvariant(ch);
            }
        }

        return idx == 0 ? string.Empty : new string(buffer[..idx]);
    }

    private static string NormalizeAndValidateExtension(string fileName, string? contentType)
    {
        var ext = Path.GetExtension(fileName ?? string.Empty);
        if (string.IsNullOrWhiteSpace(ext) || ext.Length > 10)
        {
            throw new InvalidOperationException("Dosya uzantisi gecersiz.");
        }

        if (!AllowedExtensions.Contains(ext))
        {
            throw new InvalidOperationException("Yalnizca PDF veya gorsel (JPG/PNG/WebP/GIF) yukleyebilirsiniz.");
        }

        // Content-Type, kullanıcı tarafından spoof edilebilir; sadece “ek sinyal” olarak kontrol ediyoruz.
        if (!string.IsNullOrWhiteSpace(contentType))
        {
            var ct = contentType.Trim();
            var isPdf = string.Equals(ext, ".pdf", StringComparison.OrdinalIgnoreCase);
            var ctLooksPdf = ct.Contains("pdf", StringComparison.OrdinalIgnoreCase);
            if (isPdf && !ctLooksPdf && !ct.StartsWith("application/", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("PDF dosya tipi dogrulanamadi.");
            }

            var isImage = !isPdf;
            if (isImage && !ct.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Gorsel dosya tipi dogrulanamadi.");
            }
        }

        return ext.ToLowerInvariant();
    }

    private static async Task ValidateMagicHeaderAsync(IFormFile file, string ext, CancellationToken cancellationToken)
    {
        // Minimum magic-byte kontrolü (tam bir AV yerine temel güvenlik).
        byte[] header = new byte[16];
        await using var stream = file.OpenReadStream();
        var read = await stream.ReadAsync(header.AsMemory(0, header.Length), cancellationToken);
        if (read < 4)
        {
            throw new InvalidOperationException("Dosya icerigi okunamadi.");
        }

        static bool StartsWith(byte[] h, params byte[] sig)
        {
            if (h.Length < sig.Length) return false;
            for (var i = 0; i < sig.Length; i++)
            {
                if (h[i] != sig[i]) return false;
            }
            return true;
        }

        if (string.Equals(ext, ".pdf", StringComparison.OrdinalIgnoreCase))
        {
            // %PDF
            if (!StartsWith(header, 0x25, 0x50, 0x44, 0x46))
            {
                throw new InvalidOperationException("PDF imzasi dogrulanamadi.");
            }
            return;
        }

        // Görseller
        var ok =
            string.Equals(ext, ".jpg", StringComparison.OrdinalIgnoreCase) || string.Equals(ext, ".jpeg", StringComparison.OrdinalIgnoreCase)
                ? StartsWith(header, 0xFF, 0xD8, 0xFF)
            : string.Equals(ext, ".png", StringComparison.OrdinalIgnoreCase)
                ? StartsWith(header, 0x89, 0x50, 0x4E, 0x47)
            : string.Equals(ext, ".gif", StringComparison.OrdinalIgnoreCase)
                ? StartsWith(header, 0x47, 0x49, 0x46, 0x38)
            : string.Equals(ext, ".webp", StringComparison.OrdinalIgnoreCase)
                ? (read >= 12 && header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46 && header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50)
            : false;

        if (!ok)
        {
            throw new InvalidOperationException("Gorsel imzasi dogrulanamadi.");
        }
    }
}
