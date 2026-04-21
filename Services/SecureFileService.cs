using System.Globalization;
using System.Security.Cryptography;
using Microsoft.Data.SqlClient;
using otelturizmnew.Models.Messages;
using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Services;

public class SecureFileService : ISecureFileService
{
    private readonly string _connectionString;
    private readonly IWebHostEnvironment _environment;

    public SecureFileService(IConfiguration configuration, IWebHostEnvironment environment)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection tanimli degil.");
        _environment = environment;
    }

    public async Task<StoredSecureFileResult> SaveAsync(IFormFile file, SecureFileSaveRequest request, CancellationToken cancellationToken = default)
    {
        if (file is null || file.Length <= 0)
        {
            throw new InvalidOperationException("Kaydedilecek dosya bulunamadi.");
        }

        var safeExtension = Path.GetExtension(file.FileName);
        var storedName = $"{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}{safeExtension}";
        var root = Path.Combine(_environment.ContentRootPath, "App_Data", "secure-storage", request.Category);
        Directory.CreateDirectory(root);
        var absolutePath = Path.Combine(root, storedName);

        await using (var stream = new FileStream(absolutePath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        var hash = await ComputeSha256Async(absolutePath, cancellationToken);
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = @"
            INSERT INTO guvenli_dosya_varliklari
            (
                baglam_tablo, baglam_kayit_id, sahibi_kullanici_id, sahibi_firma_id,
                kategori, gorunurluk_kapsami, orijinal_dosya_adi, depolanan_dosya_adi, depolama_yolu,
                mime_tipi, dosya_uzantisi, dosya_boyutu, sha256_ozeti, gorsel_mi
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
        command.Parameters.AddWithValue("@category", request.Category);
        command.Parameters.AddWithValue("@scope", request.VisibilityScope);
        command.Parameters.AddWithValue("@originalFileName", Path.GetFileName(file.FileName));
        command.Parameters.AddWithValue("@storedFileName", storedName);
        command.Parameters.AddWithValue("@storagePath", absolutePath);
        command.Parameters.AddWithValue("@contentType", string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType);
        command.Parameters.AddWithValue("@extension", safeExtension);
        command.Parameters.AddWithValue("@size", file.Length);
        command.Parameters.AddWithValue("@hash", hash);
        command.Parameters.AddWithValue("@isImage", IsImageContentType(file.ContentType) ? 1 : 0);

        var fileId = Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken) ?? 0L, CultureInfo.InvariantCulture);
        return new StoredSecureFileResult
        {
            FileId = fileId,
            StoredPath = absolutePath,
            OriginalFileName = Path.GetFileName(file.FileName),
            ContentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType,
            IsImage = IsImageContentType(file.ContentType),
            SizeInBytes = file.Length
        };
    }

    public async Task<string> CreateAccessUrlAsync(long fileId, long viewerUserId, string viewerAccountType, CancellationToken cancellationToken = default)
    {
        var token = Guid.NewGuid().ToString("N");
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = @"
            INSERT INTO guvenli_dosya_erisim_tokenlari
            (
                guvenli_dosya_id, erisim_tokeni, kullanici_id, hesap_tipi,
                gecerlilik_tarihi, maksimum_kullanim_sayisi
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
            SELECT TOP (1) gfv.depolama_yolu, gfv.orijinal_dosya_adi, gfv.mime_tipi,
                   gdet.id, gdet.kullanim_sayisi, gdet.maksimum_kullanim_sayisi
            FROM guvenli_dosya_erisim_tokenlari gdet
            INNER JOIN guvenli_dosya_varliklari gfv ON gfv.id = gdet.guvenli_dosya_id
            WHERE gdet.erisim_tokeni = @token
              AND gdet.iptal_tarihi IS NULL
              AND gdet.kullanici_id = @userId
              AND gdet.hesap_tipi = @accountType
              AND gdet.gecerlilik_tarihi >= SYSUTCDATETIME()
              AND gfv.aktif_mi = 1;";

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
            accessTokenId = reader.GetInt64(3);
            usageCount = reader.IsDBNull(4) ? 0L : reader.GetInt64(4);
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
            UPDATE guvenli_dosya_erisim_tokenlari
            SET kullanim_sayisi = kullanim_sayisi + 1,
                son_erisim_tarihi = SYSUTCDATETIME()
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

    private static async Task<string> ComputeSha256Async(string absolutePath, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(absolutePath);
        using var sha = SHA256.Create();
        var hash = await sha.ComputeHashAsync(stream, cancellationToken);
        return Convert.ToHexString(hash);
    }

    private static bool IsImageContentType(string? contentType)
        => !string.IsNullOrWhiteSpace(contentType) && contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
}
