using System.Globalization;
using Microsoft.Data.SqlClient;
using SqlConnection = Microsoft.Data.SqlClient.SqlConnection;
using SqlCommand = Microsoft.Data.SqlClient.SqlCommand;
using SqlTransaction = Microsoft.Data.SqlClient.SqlTransaction;
using SqlException = Microsoft.Data.SqlClient.SqlException;
using otelturizmnew.Models.Messages;
using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Services;

public class MessageCenterService : IMessageCenterService
{
    private readonly string _connectionString;
    private readonly ISecureFileService _secureFileService;

    public MessageCenterService(IConfiguration configuration, ISecureFileService secureFileService)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection tanimli degil.");
        _secureFileService = secureFileService;
    }

    public async Task<MessageInboxResult> GetUserInboxAsync(long userId, long? conversationId, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        var result = await LoadInboxAsync(connection, userId, null, conversationId, "user", "/panel/user/mesajlarim", cancellationToken);
        if (result.SelectedConversationId.HasValue)
        {
            await MarkUserConversationAsReadAsync(connection, result.SelectedConversationId.Value, userId, cancellationToken);
        }

        return result;
    }

    public async Task<MessageInboxResult> GetFirmaInboxAsync(long userId, long? conversationId, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        var firmaId = await ResolveFirmaIdAsync(connection, userId, cancellationToken);
        var result = await LoadInboxAsync(connection, userId, firmaId, conversationId, "firma", "/panel/firma/mesajlar", cancellationToken);
        if (result.SelectedConversationId.HasValue)
        {
            await MarkFirmaConversationAsReadAsync(connection, result.SelectedConversationId.Value, firmaId, cancellationToken);
        }

        return result;
    }

    public async Task<(bool Allowed, string Message)> CanStartHotelConversationAsync(long userId, long hotelId, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        var reservationId = await ResolveEligibleReservationIdAsync(connection, userId, hotelId, cancellationToken);
        return reservationId > 0
            ? (true, "Görüşme başlatabilirsiniz.")
            : (false, "Otel ile görüşme başlatmak için bu tesiste daha önce rezervasyon yapmış olmanız gerekir.");
    }

    public async Task<(bool Success, string Message, long? ConversationId)> StartHotelConversationForUserAsync(long userId, long hotelId, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        var reservationId = await ResolveEligibleReservationIdAsync(connection, userId, hotelId, cancellationToken);
        if (reservationId <= 0)
        {
            return (false, "Otel ile görüşme başlatmak için önce bu tesiste rezervasyon geçmişiniz olmalıdır.", null);
        }

        const string existingSql = @"
            SELECT TOP (1) id
            FROM [dbo].[MESAJ_KONUSMALARI]
            WHERE [MISAFIR_KULLANICI_ID] = @userId
              AND [OTEL_ID] = @hotelId
              AND [KONUSMA_TURU] = 'Otel'
              AND [DURUM] <> 'Arşivlendi'
            ORDER BY COALESCE([SON_MESAJ_TARIHI], [OLUSTURULMA_TARIHI]) DESC, id DESC;";

        await using (var existingCommand = new SqlCommand(existingSql, connection))
        {
            existingCommand.Parameters.AddWithValue("@userId", userId);
            existingCommand.Parameters.AddWithValue("@hotelId", hotelId);
            var existingId = await existingCommand.ExecuteScalarAsync(cancellationToken);
            if (existingId is not null && existingId != DBNull.Value)
            {
                return (true, "Mevcut görüşme açıldı.", Convert.ToInt64(existingId, CultureInfo.InvariantCulture));
            }
        }

        var hotelInfo = await LoadHotelConversationContextAsync(connection, hotelId, cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            const string insertConversationSql = @"
                INSERT INTO [dbo].[MESAJ_KONUSMALARI]
                (
                    [KONUSMA_KODU], [REZERVASYON_ID], [OTEL_ID], [MISAFIR_KULLANICI_ID], [OTEL_YETKILISI_KULLANICI_ID],
                    [KONU_BASLIGI], [KONUSMA_TURU], [KONU_KATEGORISI], [DURUM], [ONCELIK], [SON_MESAJ_TARIHI], [SON_MESAJ_GONDEREN], [SON_MESAJ_ONIZLEME],
                    [OTEL_OKUNMAMIS_SAYISI], [MISAFIR_OKUNMAMIS_SAYISI]
                )
                VALUES
                (
                    @code, @reservationId, @hotelId, @userId, @hotelManagerUserId,
                    @subject, 'Otel', 'Rezervasyon', 'Açık', 'Normal', CURRENT_TIMESTAMP, 'Misafir', 'Görüşme başlatıldı.',
                    1, 0
                );
                SELECT CAST(SCOPE_IDENTITY() AS bigint);";

            long conversationId;
            await using (var insertConversationCommand = new SqlCommand(insertConversationSql, connection, (SqlTransaction)transaction))
            {
            insertConversationCommand.Parameters.AddWithValue("@code", $"MS{DateTime.UtcNow:yyMMddHHmm}{Random.Shared.Next(100, 999)}");
                insertConversationCommand.Parameters.AddWithValue("@reservationId", reservationId);
                insertConversationCommand.Parameters.AddWithValue("@hotelId", hotelId);
                insertConversationCommand.Parameters.AddWithValue("@userId", userId);
                insertConversationCommand.Parameters.AddWithValue("@hotelManagerUserId", hotelInfo.ManagerUserId > 0 ? hotelInfo.ManagerUserId : DBNull.Value);
                insertConversationCommand.Parameters.AddWithValue("@subject", $"{hotelInfo.HotelName} ile görüşme");
                var conversationIdRaw = await insertConversationCommand.ExecuteScalarAsync(cancellationToken);
                conversationId = Convert.ToInt64(conversationIdRaw ?? 0L, CultureInfo.InvariantCulture);
            }

            const string insertMessageSql = @"
                INSERT INTO [dbo].[MESAJLAR]
                (
                    [KONUSMA_ID], [GONDEREN_TURU], [GONDEREN_KULLANICI_ID], [MESAJ_METNI], [MESAJ_TIPI], [OKUNDU_MU], [DURUM], [GONDERIM_TARIHI]
                )
                VALUES
                (
                    @conversationId, 'Sistem', NULL, @message, 'Sistem Bildirimi', 0, 'Gönderildi', CURRENT_TIMESTAMP
                );";

            await using (var insertMessageCommand = new SqlCommand(insertMessageSql, connection, (SqlTransaction)transaction))
            {
                insertMessageCommand.Parameters.AddWithValue("@conversationId", conversationId);
                insertMessageCommand.Parameters.AddWithValue("@message", $"Görüşme {hotelInfo.HotelName} için başlatıldı. Mesajınızı yazabilirsiniz.");
                await insertMessageCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            return (true, "Görüşme başlatıldı.", conversationId);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            return (false, $"Görüşme başlatılamadı: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message)> SendFromUserAsync(long userId, MessageSendRequest request, IReadOnlyList<IFormFile>? attachments, HttpContext httpContext, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        var conversationAuthorized = await AuthorizeConversationForUserAsync(connection, request.ConversationId, userId, cancellationToken);
        if (!conversationAuthorized)
        {
            return (false, "Mesaj göndermek için yetkiniz bulunmuyor.");
        }

        return await SaveMessageAsync(connection, request, attachments, httpContext, userId, null, "Misafir", cancellationToken);
    }

    public async Task<(bool Success, string Message)> SendFromFirmaAsync(long userId, MessageSendRequest request, IReadOnlyList<IFormFile>? attachments, HttpContext httpContext, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        var firmaId = await ResolveFirmaIdAsync(connection, userId, cancellationToken);
        var conversationAuthorized = await AuthorizeConversationForFirmaAsync(connection, request.ConversationId, firmaId, cancellationToken);
        if (!conversationAuthorized)
        {
            return (false, "Mesaj göndermek için yetkiniz bulunmuyor.");
        }

        return await SaveMessageAsync(connection, request, attachments, httpContext, userId, firmaId, "Firma", cancellationToken);
    }

    public async Task<(bool Success, string Message)> DeleteForUserAsync(long userId, MessageDeleteRequest request, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        const string sql = @"
            UPDATE m
            SET m.[DURUM] = 'Silindi',
                m.[SILINME_TARIHI] = CURRENT_TIMESTAMP,
                m.[SILINME_NEDENI] = 'Kullanici tarafindan silindi',
                m.[SILINME_GORUNUM_METNI] = 'Bu [MESAJ] silindi.',
                m.[MISAFIR_GIZLENDI_MI] = 1
            FROM [dbo].[MESAJLAR] m
            INNER JOIN [dbo].[MESAJ_KONUSMALARI] mk ON mk.id = m.[KONUSMA_ID]
            WHERE m.id = @messageId
              AND m.[KONUSMA_ID] = @conversationId
              AND mk.[MISAFIR_KULLANICI_ID] = @userId
              AND m.[GONDEREN_TURU] = 'Misafir'
              AND m.[GONDEREN_KULLANICI_ID] = @userId
              AND m.[DURUM] <> 'Silindi';";

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@messageId", request.MessageId);
        command.Parameters.AddWithValue("@conversationId", request.ConversationId);
        command.Parameters.AddWithValue("@userId", userId);
        var affected = await command.ExecuteNonQueryAsync(cancellationToken);
        return affected > 0 ? (true, "Mesaj silindi olarak işaretlendi.") : (false, "Mesaj silinemedi.");
    }

    public async Task<(bool Success, string Message)> DeleteForFirmaAsync(long userId, MessageDeleteRequest request, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        var firmaId = await ResolveFirmaIdAsync(connection, userId, cancellationToken);
        const string sql = @"
            UPDATE m
            SET m.[DURUM] = 'Silindi',
                m.[SILINME_TARIHI] = CURRENT_TIMESTAMP,
                m.[SILINME_NEDENI] = 'Firma tarafindan silindi',
                m.[SILINME_GORUNUM_METNI] = 'Bu [MESAJ] silindi.',
                m.[FIRMA_GIZLENDI_MI] = 1
            FROM [dbo].[MESAJLAR] m
            INNER JOIN [dbo].[MESAJ_KONUSMALARI] mk ON mk.id = m.[KONUSMA_ID]
            WHERE m.id = @messageId
              AND m.[KONUSMA_ID] = @conversationId
              AND mk.[FIRMA_ID] = @firmaId
              AND m.[GONDEREN_TURU] = 'Firma'
              AND m.[GONDEREN_FIRMA_ID] = @firmaId
              AND m.[DURUM] <> 'Silindi';";

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@messageId", request.MessageId);
        command.Parameters.AddWithValue("@conversationId", request.ConversationId);
        command.Parameters.AddWithValue("@firmaId", firmaId);
        var affected = await command.ExecuteNonQueryAsync(cancellationToken);
        return affected > 0 ? (true, "Mesaj silindi olarak işaretlendi.") : (false, "Mesaj silinemedi.");
    }

    private async Task<MessageInboxResult> LoadInboxAsync(SqlConnection connection, long userId, long? firmaId, long? conversationId, string viewerAccountType, string routeBase, CancellationToken cancellationToken)
    {
        var result = new MessageInboxResult();

        const string threadsSql = @"
            SELECT mk.id,
                   COALESCE(f.[FIRMA_ADI], o.[OTEL_ADI], 'Otelturizm') AS title_text,
                   COALESCE(mk.[KONU_BASLIGI], 'Mesajlar') AS subtitle_text,
                   COALESCE(mk.[SON_MESAJ_ONIZLEME], 'Henüz [MESAJ] yok') AS preview_text,
                   CASE
                       WHEN @viewerType = 'firma' THEN COALESCE(mk.[FIRMA_OKUNMAMIS_SAYISI], 0)
                       ELSE COALESCE(mk.[MISAFIR_OKUNMAMIS_SAYISI], 0)
                   END AS unread_count
            FROM [dbo].[MESAJ_KONUSMALARI] mk
            LEFT JOIN [dbo].[OTELLER] o ON o.id = mk.[OTEL_ID]
            LEFT JOIN [dbo].[FIRMALAR] f ON f.id = mk.[FIRMA_ID]
            WHERE ((@viewerType = 'user' AND mk.[MISAFIR_KULLANICI_ID] = @userId)
               OR  (@viewerType = 'firma' AND mk.[FIRMA_ID] = @firmaId))
              AND mk.[DURUM] <> 'Arşivlendi'
            ORDER BY COALESCE(mk.[SON_MESAJ_TARIHI], mk.[OLUSTURULMA_TARIHI]) DESC, mk.id DESC;";

        await using (var command = new SqlCommand(threadsSql, connection))
        {
            command.Parameters.AddWithValue("@viewerType", viewerAccountType);
            command.Parameters.AddWithValue("@userId", userId);
            command.Parameters.AddWithValue("@firmaId", firmaId.HasValue ? firmaId.Value : DBNull.Value);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var title = reader.GetString(1);
                var subtitle = reader.GetString(2);
                result.Threads.Add(new MessageCenterThreadViewModel
                {
                    ConversationId = reader.GetInt64(0),
                    Title = title,
                    Subtitle = subtitle,
                    Preview = reader.GetString(3),
                UnreadCount = reader.IsDBNull(4) ? 0 : Convert.ToInt32(reader.GetValue(4), CultureInfo.InvariantCulture),
                    AvatarText = BuildAvatar(title),
                    AvatarTone = result.Threads.Count % 3 == 0 ? "blue" : result.Threads.Count % 3 == 1 ? "green" : string.Empty,
                    RouteUrl = $"{routeBase}?conversationId={reader.GetInt64(0)}"
                });
            }
        }

        result.SelectedConversationId = conversationId ?? result.Threads.FirstOrDefault()?.ConversationId;
        foreach (var thread in result.Threads)
        {
            thread.IsActive = thread.ConversationId == result.SelectedConversationId;
        }

        if (!result.SelectedConversationId.HasValue)
        {
            result.SelectedTitle = viewerAccountType == "firma" ? "Henüz kullanıcı mesajı yok" : "Henüz konuşma yok";
            result.SelectedSubtitle = "Yeni konuşmalar burada görünecek.";
            return result;
        }

        const string titleSql = @"
            SELECT TOP (1) COALESCE(f.[FIRMA_ADI], o.[OTEL_ADI], 'Otelturizm') AS title_text,
                   COALESCE(mk.[KONU_BASLIGI], 'Mesaj detayı') AS subtitle_text
            FROM [dbo].[MESAJ_KONUSMALARI] mk
            LEFT JOIN [dbo].[OTELLER] o ON o.id = mk.[OTEL_ID]
            LEFT JOIN [dbo].[FIRMALAR] f ON f.id = mk.[FIRMA_ID]
            WHERE mk.id = @conversationId
              AND ((@viewerType = 'user' AND mk.[MISAFIR_KULLANICI_ID] = @userId)
               OR  (@viewerType = 'firma' AND mk.[FIRMA_ID] = @firmaId));";

        await using (var titleCommand = new SqlCommand(titleSql, connection))
        {
            titleCommand.Parameters.AddWithValue("@conversationId", result.SelectedConversationId.Value);
            titleCommand.Parameters.AddWithValue("@viewerType", viewerAccountType);
            titleCommand.Parameters.AddWithValue("@userId", userId);
            titleCommand.Parameters.AddWithValue("@firmaId", firmaId.HasValue ? firmaId.Value : DBNull.Value);
            await using var reader = await titleCommand.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                result.SelectedTitle = reader.GetString(0);
                result.SelectedSubtitle = reader.GetString(1);
            }
        }

        var attachmentMap = await LoadAttachmentMapAsync(connection, result.SelectedConversationId.Value, userId, viewerAccountType, cancellationToken);

        const string messagesSql = @"
            SELECT m.id,
                   m.[GONDEREN_TURU],
                   COALESCE(m.[MESAJ_METNI], ''),
                   m.[GONDERIM_TARIHI],
                   COALESCE(m.[DURUM], 'Gönderildi'),
                   COALESCE(m.[SILINME_GORUNUM_METNI], 'Bu [MESAJ] silindi.'),
                   COALESCE(u.[AD_SOYAD], fu.[AD_SOYAD], f.[FIRMA_ADI], o.[OTEL_ADI], 'Otelturizm') AS sender_name,
                   COALESCE(m.[GONDEREN_KULLANICI_ID], 0),
                   COALESCE(m.[GONDEREN_FIRMA_ID], 0)
            FROM [dbo].[MESAJLAR] m
            LEFT JOIN [dbo].[KULLANICILAR] u ON u.id = m.[GONDEREN_KULLANICI_ID]
            LEFT JOIN [dbo].[FIRMALAR] f ON f.id = m.[GONDEREN_FIRMA_ID]
            LEFT JOIN [dbo].[KULLANICILAR] fu ON fu.id = m.[GONDEREN_FIRMA_KULLANICI_ID]
            LEFT JOIN [dbo].[OTELLER] o ON o.id = m.[GONDEREN_OTEL_ID]
            WHERE m.[KONUSMA_ID] = @conversationId
            ORDER BY m.[GONDERIM_TARIHI] ASC, m.id ASC;";

        await using (var messageCommand = new SqlCommand(messagesSql, connection))
        {
            messageCommand.Parameters.AddWithValue("@conversationId", result.SelectedConversationId.Value);
            await using var reader = await messageCommand.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var messageId = reader.GetInt64(0);
                var senderType = reader.GetString(1);
                var senderUserId = reader.IsDBNull(7) ? 0L : reader.GetInt64(7);
                var senderFirmaId = reader.IsDBNull(8) ? 0L : reader.GetInt64(8);
                result.Messages.Add(new MessageCenterItemViewModel
                {
                    MessageId = messageId,
                    IsOutgoing = viewerAccountType == "firma"
                        ? string.Equals(senderType, "Firma", StringComparison.OrdinalIgnoreCase) && senderFirmaId == firmaId
                        : string.Equals(senderType, "Misafir", StringComparison.OrdinalIgnoreCase) && senderUserId == userId,
                    IsDeleted = string.Equals(reader.GetString(4), "Silindi", StringComparison.OrdinalIgnoreCase),
                    CanDelete = viewerAccountType == "firma"
                        ? string.Equals(senderType, "Firma", StringComparison.OrdinalIgnoreCase) && senderFirmaId == firmaId
                        : string.Equals(senderType, "Misafir", StringComparison.OrdinalIgnoreCase) && senderUserId == userId,
                    SenderName = reader.GetString(6),
                    MessageText = reader.GetString(2),
                    TimeText = reader.GetDateTime(3).ToLocalTime().ToString("dd MMM HH:mm", CultureInfo.GetCultureInfo("tr-TR")),
                    DeletedText = reader.GetString(5),
                    Attachments = attachmentMap.TryGetValue(messageId, out var attachments) ? attachments : new List<MessageAttachmentViewModel>()
                });
            }
        }

        return result;
    }

    private static async Task<long> ResolveEligibleReservationIdAsync(SqlConnection connection, long userId, long hotelId, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT TOP (1) id
            FROM [dbo].[REZERVASYONLAR]
            WHERE [KULLANICI_ID] = @userId
              AND [OTEL_ID] = @hotelId
              AND [DURUM] NOT IN ('İptal Edildi', 'Reddedildi')
            ORDER BY COALESCE([CIKIS_TARIHI], [GIRIS_TARIHI]) DESC, id DESC;";

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@userId", userId);
        command.Parameters.AddWithValue("@hotelId", hotelId);
        var scalar = await command.ExecuteScalarAsync(cancellationToken);
        return scalar is null || scalar == DBNull.Value
            ? 0
            : Convert.ToInt64(scalar, CultureInfo.InvariantCulture);
    }

    private static async Task<(string HotelName, long ManagerUserId)> LoadHotelConversationContextAsync(SqlConnection connection, long hotelId, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT TOP (1)
                o.[OTEL_ADI],
                COALESCE(oks.[KULLANICI_ID], o.[KULLANICI_ID], 0) AS manager_user_id
            FROM [dbo].[OTELLER] o
            LEFT JOIN [dbo].[OTEL_KULLANICI_SAHIPLIKLERI] oks
                ON oks.[OTEL_ID] = o.id
               AND oks.[AKTIF_MI] = 1
               AND oks.[ANA_SORUMLU_MU] = 1
            WHERE o.id = @hotelId
            ORDER BY oks.id ASC;";

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@hotelId", hotelId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return (reader.GetString(0), reader.IsDBNull(1) ? 0L : reader.GetInt64(1));
        }

        return ("Otel", 0);
    }

    private async Task<Dictionary<long, List<MessageAttachmentViewModel>>> LoadAttachmentMapAsync(SqlConnection connection, long conversationId, long viewerUserId, string viewerAccountType, CancellationToken cancellationToken)
    {
        var result = new Dictionary<long, List<MessageAttachmentViewModel>>();
        const string sql = @"
            SELECT md.[MESAJ_ID], gfv.id, gfv.[ORIJINAL_DOSYA_ADI], gfv.[MIME_TIPI], gfv.[DOSYA_BOYUTU], gfv.[GORSEL_MI]
            FROM [dbo].[MESAJ_DOSYALARI] md
            INNER JOIN [dbo].[GUVENLI_DOSYA_VARLIKLARI] gfv ON gfv.id = md.[GUVENLI_DOSYA_ID]
            INNER JOIN [dbo].[MESAJLAR] m ON m.id = md.[MESAJ_ID]
            WHERE m.[KONUSMA_ID] = @conversationId
              AND md.[AKTIF_MI] = 1
              AND gfv.[AKTIF_MI] = 1
            ORDER BY md.[SIRALAMA] ASC, md.id ASC;";

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@conversationId", conversationId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var rows = new List<(long MessageId, long FileId, string Name, string Mime, long Size, bool IsImage)>();
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add((
                reader.GetInt64(0),
                reader.GetInt64(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetInt64(4),
                !reader.IsDBNull(5) && reader.GetBoolean(5)));
        }

        foreach (var row in rows)
        {
            if (!result.TryGetValue(row.MessageId, out var list))
            {
                list = new List<MessageAttachmentViewModel>();
                result[row.MessageId] = list;
            }

            list.Add(new MessageAttachmentViewModel
            {
                FileId = row.FileId,
                DisplayName = row.Name,
                ContentType = row.Mime,
                SizeText = FormatFileSize(row.Size),
                AccessUrl = await _secureFileService.CreateAccessUrlAsync(row.FileId, viewerUserId, viewerAccountType, cancellationToken),
                IsImage = row.IsImage
            });
        }

        return result;
    }

    private async Task<(bool Success, string Message)> SaveMessageAsync(SqlConnection connection, MessageSendRequest request, IReadOnlyList<IFormFile>? attachments, HttpContext httpContext, long senderUserId, long? senderFirmaId, string senderType, CancellationToken cancellationToken)
    {
        if (request.ConversationId <= 0 || string.IsNullOrWhiteSpace(request.Message) && (attachments is null || attachments.Count == 0))
        {
            return (false, "Mesaj veya ek yüklemeden gönderim yapılamaz.");
        }

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            const string insertSql = @"
                INSERT INTO [dbo].[MESAJLAR]
                (
                    [KONUSMA_ID], [GONDEREN_TURU], [GONDEREN_KULLANICI_ID], [GONDEREN_FIRMA_ID], [GONDEREN_FIRMA_KULLANICI_ID],
                    [MESAJ_METNI], [MESAJ_TIPI], [OKUNDU_MU], [DURUM], [IP_ADRESI], [CIHAZ_BILGISI], [GONDERIM_TARIHI]
                )
                VALUES
                (
                    @conversationId, @senderType, @senderUserId, @senderFirmaId, @senderFirmaUserId,
                    @message, @messageType, 0, 'Gönderildi', @ipAddress, @deviceInfo, CURRENT_TIMESTAMP
                );
                SELECT CAST(SCOPE_IDENTITY() AS bigint);";

            await using var insertCommand = new SqlCommand(insertSql, connection, (SqlTransaction)transaction);
            insertCommand.Parameters.AddWithValue("@conversationId", request.ConversationId);
            insertCommand.Parameters.AddWithValue("@senderType", senderType);
            insertCommand.Parameters.AddWithValue("@senderUserId", senderUserId);
            insertCommand.Parameters.AddWithValue("@senderFirmaId", senderFirmaId.HasValue ? senderFirmaId.Value : DBNull.Value);
            insertCommand.Parameters.AddWithValue("@senderFirmaUserId", senderType == "Firma" ? senderUserId : DBNull.Value);
            insertCommand.Parameters.AddWithValue("@message", request.Message?.Trim() ?? string.Empty);
            insertCommand.Parameters.AddWithValue("@messageType", attachments is { Count: > 0 } ? "Dosya" : "Metin");
            insertCommand.Parameters.AddWithValue("@ipAddress", httpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty);
            insertCommand.Parameters.AddWithValue("@deviceInfo", httpContext.Request.Headers.UserAgent.ToString());
            var messageIdRaw = await insertCommand.ExecuteScalarAsync(cancellationToken);
            var messageId = Convert.ToInt64(messageIdRaw ?? 0L, CultureInfo.InvariantCulture);

            if (attachments is { Count: > 0 })
            {
                var order = 1;
                foreach (var file in attachments.Where(static x => x is not null && x.Length > 0))
                {
                    var stored = await _secureFileService.SaveAsync(file, new SecureFileSaveRequest
                    {
                        ContextTable = "mesajlar",
                        ContextId = messageId,
                        OwnerUserId = senderUserId,
                        OwnerFirmaId = senderFirmaId,
                        VisibilityScope = "token-only",
                        Category = "message-attachments"
                    }, cancellationToken);

                    await using var fileCommand = new SqlCommand(@"
                        INSERT INTO [dbo].[MESAJ_DOSYALARI]
                        ([MESAJ_ID], [GUVENLI_DOSYA_ID], [GOSTERIM_ADI], [SIRALAMA], [AKTIF_MI])
                        VALUES
                        (@messageId, @fileId, @displayName, @orderNo, 1);", connection, (SqlTransaction)transaction);
                    fileCommand.Parameters.AddWithValue("@messageId", messageId);
                    fileCommand.Parameters.AddWithValue("@fileId", stored.FileId);
                    fileCommand.Parameters.AddWithValue("@displayName", stored.OriginalFileName);
                    fileCommand.Parameters.AddWithValue("@orderNo", order++);
                    await fileCommand.ExecuteNonQueryAsync(cancellationToken);
                }
            }

            await using (var updateConversationCommand = new SqlCommand(@"
                UPDATE [dbo].[MESAJ_KONUSMALARI]
                SET [SON_MESAJ_TARIHI] = CURRENT_TIMESTAMP,
                    [SON_MESAJ_GONDEREN] = @senderType,
                    [SON_MESAJ_ONIZLEME] = LEFT(@preview, 100),
                    [MISAFIR_OKUNMAMIS_SAYISI] = CASE WHEN @senderType = 'Firma' THEN [MISAFIR_OKUNMAMIS_SAYISI] + 1 ELSE 0 END,
                    [FIRMA_OKUNMAMIS_SAYISI] = CASE WHEN @senderType = 'Misafir' THEN [FIRMA_OKUNMAMIS_SAYISI] + 1 ELSE 0 END,
                    [MISAFIR_SON_OKUMA_TARIHI] = CASE WHEN @senderType = 'Misafir' THEN CURRENT_TIMESTAMP ELSE [MISAFIR_SON_OKUMA_TARIHI] END,
                    [FIRMA_SON_OKUMA_TARIHI] = CASE WHEN @senderType = 'Firma' THEN CURRENT_TIMESTAMP ELSE [FIRMA_SON_OKUMA_TARIHI] END
                WHERE id = @conversationId;", connection, (SqlTransaction)transaction))
            {
                updateConversationCommand.Parameters.AddWithValue("@conversationId", request.ConversationId);
                updateConversationCommand.Parameters.AddWithValue("@senderType", senderType);
                updateConversationCommand.Parameters.AddWithValue("@preview", string.IsNullOrWhiteSpace(request.Message) ? "Ek dosya paylaşıldı." : request.Message.Trim());
                await updateConversationCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            return (true, "Mesaj güvenli şekilde gönderildi.");
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static async Task MarkUserConversationAsReadAsync(SqlConnection connection, long conversationId, long userId, CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand(@"
            UPDATE [dbo].[MESAJ_KONUSMALARI]
            SET [MISAFIR_OKUNMAMIS_SAYISI] = 0,
                [MISAFIR_SON_OKUMA_TARIHI] = CURRENT_TIMESTAMP
            WHERE id = @conversationId
              AND [MISAFIR_KULLANICI_ID] = @userId;", connection);
        command.Parameters.AddWithValue("@conversationId", conversationId);
        command.Parameters.AddWithValue("@userId", userId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task MarkFirmaConversationAsReadAsync(SqlConnection connection, long conversationId, long firmaId, CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand(@"
            UPDATE [dbo].[MESAJ_KONUSMALARI]
            SET [FIRMA_OKUNMAMIS_SAYISI] = 0,
                [FIRMA_SON_OKUMA_TARIHI] = CURRENT_TIMESTAMP
            WHERE id = @conversationId
              AND [FIRMA_ID] = @firmaId;", connection);
        command.Parameters.AddWithValue("@conversationId", conversationId);
        command.Parameters.AddWithValue("@firmaId", firmaId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<bool> AuthorizeConversationForUserAsync(SqlConnection connection, long conversationId, long userId, CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand("SELECT COUNT(*) FROM [dbo].[MESAJ_KONUSMALARI] WHERE id = @id AND [MISAFIR_KULLANICI_ID] = @userId;", connection);
        command.Parameters.AddWithValue("@id", conversationId);
        command.Parameters.AddWithValue("@userId", userId);
        var count = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken) ?? 0, CultureInfo.InvariantCulture);
        return count > 0;
    }

    private static async Task<bool> AuthorizeConversationForFirmaAsync(SqlConnection connection, long conversationId, long firmaId, CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand("SELECT COUNT(*) FROM [dbo].[MESAJ_KONUSMALARI] WHERE id = @id AND [FIRMA_ID] = @firmaId;", connection);
        command.Parameters.AddWithValue("@id", conversationId);
        command.Parameters.AddWithValue("@firmaId", firmaId);
        var count = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken) ?? 0, CultureInfo.InvariantCulture);
        return count > 0;
    }

    private static async Task<long> ResolveFirmaIdAsync(SqlConnection connection, long userId, CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand("SELECT TOP (1) COALESCE([FIRMA_ID], 0) FROM [dbo].[KULLANICILAR] WHERE id = @userId;", connection);
        command.Parameters.AddWithValue("@userId", userId);
        var scalar = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt64(scalar ?? 0L, CultureInfo.InvariantCulture);
    }

    private async Task<SqlConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    private static string BuildAvatar(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return "OT";
        }

        return string.Concat(title
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Take(2)
            .Select(static x => char.ToUpperInvariant(x[0])));
    }

    private static string FormatFileSize(long bytes)
    {
        if (bytes >= 1024 * 1024)
        {
            return $"{bytes / (1024d * 1024d):0.0} MB";
        }

        if (bytes >= 1024)
        {
            return $"{bytes / 1024d:0.0} KB";
        }

        return $"{bytes} B";
    }
}
