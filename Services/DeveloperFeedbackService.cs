using Microsoft.Data.SqlClient;
using otelturizmnew.Models.DeveloperFeedback;
using otelturizmnew.Models.Email;
using otelturizmnew.Services.Abstractions;
using System.Globalization;

namespace otelturizmnew.Services;

public sealed class DeveloperFeedbackService : IDeveloperFeedbackService
{
    private const string NotifyEmail = "irmhro0@gmail.com";
    private readonly string _connectionString;
    private readonly IWebHostEnvironment _environment;
    private readonly IEmailQueueService _emailQueueService;

    public DeveloperFeedbackService(IConfiguration configuration, IWebHostEnvironment environment, IEmailQueueService emailQueueService)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection tanimli degil.");
        _environment = environment;
        _emailQueueService = emailQueueService;
    }

    public async Task<(bool Success, string Message)> CreateAsync(
        long userId,
        string? fullName,
        string? email,
        string? accountType,
        string? ipAddress,
        string? userAgent,
        DeveloperFeedbackForm form,
        CancellationToken cancellationToken = default)
    {
        var title = (form.Title ?? string.Empty).Trim();
        var content = (form.Content ?? string.Empty).Trim();
        if (title.Length < 2 || content.Length < 3)
        {
            return (false, "Başlık ve talep içeriğini doldurun.");
        }

        var screenshot = await SaveScreenshotAsync(form.Screenshot, cancellationToken);
        var imageUrl = screenshot.StoredValue;
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await EnsureSchemaAsync(connection, cancellationToken);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);
        var hasPageTitle = await ColumnExistsAsync(connection, transaction, "dbo.developer_bildirimleri", "sayfa_basligi", cancellationToken);

        long feedbackId;
        var insertSql = hasPageTitle
            ? """
            INSERT INTO dbo.developer_bildirimleri
            (
                kaynak_panel, kaynak_rol, kullanici_id, kullanici_eposta, ad_soyad,
                bildirim_turu, baslik, icerik, sayfa_url, ip_adresi, user_agent,
                ekran_bilgisi, cihaz_bilgisi, gorsel_url, sayfa_basligi, durum, oncelik,
                olusturulma_tarihi, guncellenme_tarihi
            )
            OUTPUT INSERTED.id
            VALUES
            (
                @panel, @role, NULLIF(@userId, 0), NULLIF(@email, N''), NULLIF(@fullName, N''),
                @type, @title, @content, NULLIF(@pageUrl, N''), NULLIF(@ipAddress, N''), NULLIF(@userAgent, N''),
                NULLIF(@viewport, N''), NULLIF(@deviceInfo, N''), NULLIF(@imageUrl, N''), NULLIF(@pageTitle, N''), N'Yeni', N'Orta',
                SYSUTCDATETIME(), SYSUTCDATETIME()
            );
            """
            : """
            INSERT INTO dbo.developer_bildirimleri
            (
                kaynak_panel, kaynak_rol, kullanici_id, kullanici_eposta, ad_soyad,
                bildirim_turu, baslik, icerik, sayfa_url, ip_adresi, user_agent,
                ekran_bilgisi, cihaz_bilgisi, gorsel_url, durum, oncelik,
                olusturulma_tarihi, guncellenme_tarihi
            )
            OUTPUT INSERTED.id
            VALUES
            (
                @panel, @role, NULLIF(@userId, 0), NULLIF(@email, N''), NULLIF(@fullName, N''),
                @type, @title, @content, NULLIF(@pageUrl, N''), NULLIF(@ipAddress, N''), NULLIF(@userAgent, N''),
                NULLIF(@viewport, N''), NULLIF(@deviceInfo, N''), NULLIF(@imageUrl, N''), N'Yeni', N'Orta',
                SYSUTCDATETIME(), SYSUTCDATETIME()
            );
            """;
        await using (var command = new SqlCommand(insertSql, connection, transaction))
        {
            command.Parameters.AddWithValue("@panel", Normalize(form.PanelKey, 50));
            command.Parameters.AddWithValue("@role", Normalize(accountType, 50));
            command.Parameters.AddWithValue("@userId", userId);
            command.Parameters.AddWithValue("@email", Normalize(email, 256));
            command.Parameters.AddWithValue("@fullName", Normalize(fullName, 200));
            command.Parameters.AddWithValue("@type", Normalize(form.FeedbackType, 60));
            command.Parameters.AddWithValue("@title", title);
            command.Parameters.AddWithValue("@content", content);
            command.Parameters.AddWithValue("@pageUrl", Normalize(form.PageUrl, 1000));
            command.Parameters.AddWithValue("@ipAddress", Normalize(ipAddress, 80));
            command.Parameters.AddWithValue("@userAgent", Normalize(userAgent, 1000));
            command.Parameters.AddWithValue("@viewport", Normalize(form.Viewport, 120));
            command.Parameters.AddWithValue("@deviceInfo", Normalize(form.DeviceInfo, 500));
            command.Parameters.AddWithValue("@imageUrl", Normalize(imageUrl, 1000));
            if (hasPageTitle) command.Parameters.AddWithValue("@pageTitle", Normalize(form.PageTitle, 220));
            feedbackId = Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken), System.Globalization.CultureInfo.InvariantCulture);
        }

        try
        {
            var attachmentPath = screenshot.AttachmentPath ?? imageUrl ?? string.Empty;

            await _emailQueueService.QueueTemplateAsync(connection, transaction, new QueuedEmailTemplateRequest
            {
                UserId = userId,
                RecipientEmail = NotifyEmail,
                TemplateCode = "developer_feedback",
                SenderEmailOverride = "bildiri@otelturizm.com",
                SubjectOverride = $"[BETA BİLDİRİM] {title}",
                RelatedTable = "developer_bildirimleri",
                RelatedRecordId = feedbackId,
                Tokens = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["feedback_id"] = feedbackId.ToString(CultureInfo.InvariantCulture),
                    ["panel_key"] = Normalize(form.PanelKey, 50),
                    ["feedback_type"] = Normalize(form.FeedbackType, 60),
                    ["title"] = title,
                    ["content"] = content,
                    ["page_url"] = Normalize(form.PageUrl, 1000),
                    ["page_title"] = Normalize(form.PageTitle, 220),
                    ["user_full_name"] = Normalize(fullName, 200),
                    ["user_email"] = Normalize(email, 256),
                    ["account_type"] = Normalize(accountType, 50),
                    ["ip_address"] = Normalize(ipAddress, 80),
                    ["user_agent"] = Normalize(userAgent, 1000),
                    ["viewport"] = Normalize(form.Viewport, 120),
                    ["device_info"] = Normalize(form.DeviceInfo, 500),
                    ["image_url"] = Normalize(imageUrl, 1000),
                    ["created_at"] = DateTime.Now.ToString("dd.MM.yyyy HH:mm")
                },
                Attachments = string.IsNullOrWhiteSpace(attachmentPath)
                    ? null
                    : [new QueuedEmailAttachment { FileName = Path.GetFileName(attachmentPath), FilePathOrUrl = attachmentPath, ContentType = form.Screenshot?.ContentType ?? "image/png" }]
            }, cancellationToken);

            await using var update = new SqlCommand("""
                UPDATE dbo.developer_bildirimleri
                SET email_kuyruga_alindi_mi = 1, guncellenme_tarihi = SYSUTCDATETIME()
                WHERE id = @id;
                """, connection, transaction);
            update.Parameters.AddWithValue("@id", feedbackId);
            await update.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            await using var update = new SqlCommand("""
                UPDATE dbo.developer_bildirimleri
                SET admin_notu = LEFT(CONCAT(COALESCE(admin_notu, N''), CASE WHEN COALESCE(admin_notu, N'') = N'' THEN N'' ELSE CHAR(10) END, N'E-posta kuyruğu hatası: ', @error), 4000),
                    guncellenme_tarihi = SYSUTCDATETIME()
                WHERE id = @id;
                """, connection, transaction);
            update.Parameters.AddWithValue("@id", feedbackId);
            update.Parameters.AddWithValue("@error", Normalize(ex.Message, 3000));
            await update.ExecuteNonQueryAsync(cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(screenshot.Warning))
        {
            await using var warn = new SqlCommand("""
                UPDATE dbo.developer_bildirimleri
                SET admin_notu = LEFT(CONCAT(COALESCE(admin_notu, N''), CASE WHEN COALESCE(admin_notu, N'') = N'' THEN N'' ELSE CHAR(10) END, @warning), 4000),
                    guncellenme_tarihi = SYSUTCDATETIME()
                WHERE id = @id;
                """, connection, transaction);
            warn.Parameters.AddWithValue("@id", feedbackId);
            warn.Parameters.AddWithValue("@warning", Normalize(screenshot.Warning, 1000));
            await warn.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return (true, "Gönderildi. İlginiz için teşekkür ederiz. Talep adımlarınızı Geçmiş Bildirimler alanında kontrol edebilirsiniz.");
    }

    public async Task<DeveloperFeedbackHistoryResponse> GetUserHistoryAsync(long userId, CancellationToken cancellationToken = default)
    {
        var response = new DeveloperFeedbackHistoryResponse { Success = true };
        if (userId <= 0) return response;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await EnsureSchemaAsync(connection, cancellationToken);
        var hasPageTitle = await ColumnExistsAsync(connection, null, "dbo.developer_bildirimleri", "sayfa_basligi", cancellationToken);
        var sql = hasPageTitle
            ? """
            SELECT TOP (20) id,bildirim_turu,baslik,icerik,COALESCE(sayfa_basligi,sayfa_url,N'Bilinmeyen sayfa') AS sayfa,durum,admin_notu,olusturulma_tarihi
            FROM dbo.developer_bildirimleri
            WHERE kullanici_id = @userId
            ORDER BY olusturulma_tarihi DESC, id DESC;
            """
            : """
            SELECT TOP (20) id,bildirim_turu,baslik,icerik,COALESCE(sayfa_url,N'Bilinmeyen sayfa') AS sayfa,durum,admin_notu,olusturulma_tarihi
            FROM dbo.developer_bildirimleri
            WHERE kullanici_id = @userId
            ORDER BY olusturulma_tarihi DESC, id DESC;
            """;

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@userId", userId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var status = reader.IsDBNull(5) ? "Yeni" : reader.GetString(5);
            response.Items.Add(new DeveloperFeedbackHistoryItemViewModel
            {
                Id = reader.GetInt64(0),
                FeedbackType = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                Title = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                Content = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                PageLabel = reader.IsDBNull(4) ? "Bilinmeyen sayfa" : reader.GetString(4),
                Status = status,
                StatusTone = MapStatusTone(status),
                AdminNote = reader.IsDBNull(6) ? null : reader.GetString(6),
                CreatedAtText = reader.GetDateTime(7).ToLocalTime().ToString("dd.MM.yyyy HH:mm", CultureInfo.GetCultureInfo("tr-TR")),
                CanEdit = IsEditableStatus(status),
                CanDelete = true
            });
        }

        return response;
    }

    public async Task<(bool Success, string Message)> UpdateAsync(long userId, DeveloperFeedbackForm form, CancellationToken cancellationToken = default)
    {
        if (userId <= 0 || !form.FeedbackId.HasValue || form.FeedbackId.Value <= 0)
        {
            return (false, "Düzenlenecek bildirim bulunamadı.");
        }

        var title = (form.Title ?? string.Empty).Trim();
        var content = (form.Content ?? string.Empty).Trim();
        if (title.Length < 2 || content.Length < 3)
        {
            return (false, "Başlık ve talep içeriğini doldurun.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await EnsureSchemaAsync(connection, cancellationToken);
        var current = await LoadOwnedFeedbackAsync(connection, userId, form.FeedbackId.Value, cancellationToken);
        if (current is null)
        {
            return (false, "Bildirim bulunamadı.");
        }

        var currentValue = current.Value;

        if (!IsEditableStatus(currentValue.Status))
        {
            return (false, "Bu bildirim artık düzenlenemez.");
        }

        var imageUrl = currentValue.ImageUrl;
        string? updateWarning = null;
        if (form.Screenshot is not null && form.Screenshot.Length > 0)
        {
            var screenshot = await SaveScreenshotAsync(form.Screenshot, cancellationToken);
            imageUrl = string.IsNullOrWhiteSpace(screenshot.StoredValue) ? currentValue.ImageUrl : screenshot.StoredValue;
            updateWarning = screenshot.Warning;
        }

        var hasPageTitle = await ColumnExistsAsync(connection, null, "dbo.developer_bildirimleri", "sayfa_basligi", cancellationToken);
        var sql = hasPageTitle
            ? """
            UPDATE dbo.developer_bildirimleri
            SET bildirim_turu = @type,
                baslik = @title,
                icerik = @content,
                sayfa_url = NULLIF(@pageUrl, N''),
                sayfa_basligi = NULLIF(@pageTitle, N''),
                ekran_bilgisi = NULLIF(@viewport, N''),
                cihaz_bilgisi = NULLIF(@deviceInfo, N''),
                gorsel_url = NULLIF(@imageUrl, N''),
                guncellenme_tarihi = SYSUTCDATETIME()
            WHERE id = @id AND kullanici_id = @userId;
            """
            : """
            UPDATE dbo.developer_bildirimleri
            SET bildirim_turu = @type,
                baslik = @title,
                icerik = @content,
                sayfa_url = NULLIF(@pageUrl, N''),
                ekran_bilgisi = NULLIF(@viewport, N''),
                cihaz_bilgisi = NULLIF(@deviceInfo, N''),
                gorsel_url = NULLIF(@imageUrl, N''),
                guncellenme_tarihi = SYSUTCDATETIME()
            WHERE id = @id AND kullanici_id = @userId;
            """;

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", form.FeedbackId.Value);
        command.Parameters.AddWithValue("@userId", userId);
        command.Parameters.AddWithValue("@type", Normalize(form.FeedbackType, 60));
        command.Parameters.AddWithValue("@title", title);
        command.Parameters.AddWithValue("@content", content);
        command.Parameters.AddWithValue("@pageUrl", Normalize(form.PageUrl, 1000));
        command.Parameters.AddWithValue("@viewport", Normalize(form.Viewport, 120));
        command.Parameters.AddWithValue("@deviceInfo", Normalize(form.DeviceInfo, 500));
        command.Parameters.AddWithValue("@imageUrl", Normalize(imageUrl, 1000));
        if (hasPageTitle) command.Parameters.AddWithValue("@pageTitle", Normalize(form.PageTitle, 220));
        await command.ExecuteNonQueryAsync(cancellationToken);
        return string.IsNullOrWhiteSpace(updateWarning)
            ? (true, "Bildirim güncellendi.")
            : (true, "Bildirim güncellendi. Görsel yazılamadı; metin kaydı korundu.");
    }

    public async Task<(bool Success, string Message)> DeleteAsync(long userId, long feedbackId, CancellationToken cancellationToken = default)
    {
        if (userId <= 0 || feedbackId <= 0)
        {
            return (false, "Silinecek bildirim bulunamadı.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await EnsureSchemaAsync(connection, cancellationToken);
        await using var command = new SqlCommand("""
            DELETE FROM dbo.developer_bildirimleri
            WHERE id = @id AND kullanici_id = @userId;
            """, connection);
        command.Parameters.AddWithValue("@id", feedbackId);
        command.Parameters.AddWithValue("@userId", userId);
        var affected = await command.ExecuteNonQueryAsync(cancellationToken);
        return affected > 0 ? (true, "Bildirim silindi.") : (false, "Bildirim bulunamadı.");
    }

    private async Task<SavedScreenshotResult> SaveScreenshotAsync(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return SavedScreenshotResult.Empty;
        }

        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp" };
        var ext = Path.GetExtension(file.FileName);
        if (!allowed.Contains(ext) || file.Length > 8 * 1024 * 1024)
        {
            throw new InvalidOperationException("Görsel jpg, png veya webp olmalı ve 8 MB sınırını aşmamalı.");
        }

        var fileName = $"{Guid.NewGuid():N}{ext.ToLowerInvariant()}";
        var year = DateTime.UtcNow.ToString("yyyy");
        var month = DateTime.UtcNow.ToString("MM");
        var relativeDir = Path.Combine("uploads", "developer-bildirimleri", year, month);
        var webRootPath = string.IsNullOrWhiteSpace(_environment.WebRootPath)
            ? Path.Combine(_environment.ContentRootPath, "wwwroot")
            : _environment.WebRootPath;

        try
        {
            var absoluteDir = Path.Combine(webRootPath, relativeDir);
            Directory.CreateDirectory(absoluteDir);
            var absolutePath = Path.Combine(absoluteDir, fileName);
            await using var stream = File.Create(absolutePath);
            await file.CopyToAsync(stream, cancellationToken);
            return new SavedScreenshotResult("/" + Path.Combine(relativeDir, fileName).Replace('\\', '/'), absolutePath, null);
        }
        catch (UnauthorizedAccessException)
        {
            var fallbackDir = Path.Combine(_environment.ContentRootPath, "App_Data", "developer-bildirimleri", year, month);
            Directory.CreateDirectory(fallbackDir);
            var fallbackPath = Path.Combine(fallbackDir, fileName);
            await using var stream = File.Create(fallbackPath);
            await file.CopyToAsync(stream, cancellationToken);
            return new SavedScreenshotResult(fallbackPath, fallbackPath, "Görsel public uploads alanına yazılamadı; App_Data fallback ile kaydedildi.");
        }
        catch (IOException ex) when (ex.Message.Contains("denied", StringComparison.OrdinalIgnoreCase) || ex.Message.Contains("erişim", StringComparison.OrdinalIgnoreCase))
        {
            var fallbackDir = Path.Combine(_environment.ContentRootPath, "App_Data", "developer-bildirimleri", year, month);
            Directory.CreateDirectory(fallbackDir);
            var fallbackPath = Path.Combine(fallbackDir, fileName);
            await using var stream = File.Create(fallbackPath);
            await file.CopyToAsync(stream, cancellationToken);
            return new SavedScreenshotResult(fallbackPath, fallbackPath, "Görsel public uploads alanına yazılamadı; App_Data fallback ile kaydedildi.");
        }
    }

    private sealed record SavedScreenshotResult(string? StoredValue, string? AttachmentPath, string? Warning)
    {
        public static readonly SavedScreenshotResult Empty = new(null, null, null);
    }

    private static string Normalize(string? value, int maxLength)
    {
        var normalized = (value ?? string.Empty).Trim();
        return normalized.Length <= maxLength ? normalized : normalized[..maxLength];
    }

    private static async Task<bool> ColumnExistsAsync(SqlConnection connection, SqlTransaction? transaction, string tableName, string columnName, CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand("SELECT COL_LENGTH(@tableName, @columnName);", connection, transaction);
        command.Parameters.AddWithValue("@tableName", tableName);
        command.Parameters.AddWithValue("@columnName", columnName);
        var scalar = await command.ExecuteScalarAsync(cancellationToken);
        return scalar is not null && scalar != DBNull.Value;
    }

    private static async Task EnsureSchemaAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        const string sql = """
            IF OBJECT_ID(N'dbo.developer_bildirimleri', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.developer_bildirimleri
                (
                    id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_developer_bildirimleri PRIMARY KEY,
                    bildirim_kodu UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_developer_bildirimleri_kod DEFAULT NEWID(),
                    kaynak_panel NVARCHAR(50) NOT NULL,
                    kaynak_rol NVARCHAR(50) NULL,
                    kullanici_id BIGINT NULL,
                    kullanici_eposta NVARCHAR(256) NULL,
                    ad_soyad NVARCHAR(200) NULL,
                    bildirim_turu NVARCHAR(60) NOT NULL,
                    baslik NVARCHAR(220) NOT NULL,
                    icerik NVARCHAR(MAX) NOT NULL,
                    sayfa_url NVARCHAR(1000) NULL,
                    ip_adresi NVARCHAR(80) NULL,
                    user_agent NVARCHAR(1000) NULL,
                    ekran_bilgisi NVARCHAR(120) NULL,
                    cihaz_bilgisi NVARCHAR(500) NULL,
                    gorsel_url NVARCHAR(1000) NULL,
                    durum NVARCHAR(60) NOT NULL CONSTRAINT DF_developer_bildirimleri_durum DEFAULT N'Yeni',
                    oncelik NVARCHAR(40) NOT NULL CONSTRAINT DF_developer_bildirimleri_oncelik DEFAULT N'Orta',
                    email_kuyruga_alindi_mi BIT NOT NULL CONSTRAINT DF_developer_bildirimleri_email DEFAULT 0,
                    admin_notu NVARCHAR(MAX) NULL,
                    olusturulma_tarihi DATETIME2(7) NOT NULL CONSTRAINT DF_developer_bildirimleri_olusturma DEFAULT SYSUTCDATETIME(),
                    guncellenme_tarihi DATETIME2(7) NOT NULL CONSTRAINT DF_developer_bildirimleri_guncelleme DEFAULT SYSUTCDATETIME()
                );
                CREATE INDEX IX_developer_bildirimleri_kullanici ON dbo.developer_bildirimleri(kullanici_id, olusturulma_tarihi DESC);
            END;
            IF COL_LENGTH('dbo.developer_bildirimleri', 'sayfa_basligi') IS NULL
            BEGIN
                ALTER TABLE dbo.developer_bildirimleri ADD sayfa_basligi NVARCHAR(220) NULL;
            END;
            IF NOT EXISTS (
                SELECT 1 FROM dbo.bildirim_sablonlari
                WHERE sablon_kodu = N'developer_feedback' AND tur = N'E-posta' AND dil = N'tr'
            )
            BEGIN
                INSERT INTO dbo.bildirim_sablonlari
                (sablon_kodu, sablon_adi, tur, dil, konu, baslik, icerik, degiskenler, aktif_mi, olusturulma_tarihi)
                VALUES
                (
                    N'developer_feedback', N'Beta Geri Bildirim', N'E-posta', N'tr',
                    N'[BETA BİLDİRİM] {{title}}', N'Beta Geri Bildirim', N'Views/Email/tr/Developer Bildirim.cshtml',
                    N'feedback_id,panel_key,feedback_type,title,content,page_url,page_title,user_full_name,user_email,account_type,ip_address,user_agent,viewport,device_info,image_url,created_at',
                    1, SYSUTCDATETIME()
                );
            END;
            """;

        await using var command = new SqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static string MapStatusTone(string status)
    {
        var v = (status ?? string.Empty).Trim().ToLowerInvariant();
        if (v.Contains("redd")) return "danger";
        if (v.Contains("onay")) return "success";
        if (v.Contains("geliştir") || v.Contains("incele")) return "info";
        if (v.Contains("bek")) return "warning";
        return "neutral";
    }

    private static bool IsEditableStatus(string status)
    {
        var v = (status ?? string.Empty).Trim().ToLowerInvariant();
        return v == string.Empty || v.Contains("yeni") || v.Contains("bek") || v.Contains("incele");
    }

    private static async Task<(string Status, string? ImageUrl)?> LoadOwnedFeedbackAsync(SqlConnection connection, long userId, long feedbackId, CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand("""
            SELECT TOP (1) durum, gorsel_url
            FROM dbo.developer_bildirimleri
            WHERE id = @id AND kullanici_id = @userId;
            """, connection);
        command.Parameters.AddWithValue("@id", feedbackId);
        command.Parameters.AddWithValue("@userId", userId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;
        return (reader.IsDBNull(0) ? "Yeni" : reader.GetString(0), reader.IsDBNull(1) ? null : reader.GetString(1));
    }
}
