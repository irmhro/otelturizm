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
            FROM mesaj_konusmalari
            WHERE misafir_kullanici_id = @userId
              AND otel_id = @hotelId
              AND durum <> 'Arşivlendi'
            ORDER BY COALESCE(son_mesaj_tarihi, olusturulma_tarihi) DESC, id DESC;";

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
                INSERT INTO mesaj_konusmalari
                (
                    konusma_kodu, rezervasyon_id, otel_id, misafir_kullanici_id, otel_yetkilisi_kullanici_id,
                    konu_basligi, konu_kategorisi, durum, oncelik, son_mesaj_tarihi, son_mesaj_gonderen, son_mesaj_onizleme,
                    otel_okunmamis_sayisi, misafir_okunmamis_sayisi
                )
                VALUES
                (
                    @code, @reservationId, @hotelId, @userId, @hotelManagerUserId,
                    @subject, 'Rezervasyon', 'Açık', 'Normal', CURRENT_TIMESTAMP, 'Misafir', 'Görüşme başlatıldı.',
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
                INSERT INTO mesajlar
                (
                    konusma_id, gonderen_turu, gonderen_kullanici_id, mesaj_metni, mesaj_tipi, okundu_mu, durum, gonderim_tarihi
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
            SET m.durum = 'Silindi',
                m.silinme_tarihi = CURRENT_TIMESTAMP,
                m.silinme_nedeni = 'Kullanici tarafindan silindi',
                m.silinme_gorunum_metni = 'Bu mesaj silindi.',
                m.misafir_gizlendi_mi = 1
            FROM mesajlar m
            INNER JOIN mesaj_konusmalari mk ON mk.id = m.konusma_id
            WHERE m.id = @messageId
              AND m.konusma_id = @conversationId
              AND mk.misafir_kullanici_id = @userId
              AND m.gonderen_turu = 'Misafir'
              AND m.gonderen_kullanici_id = @userId
              AND m.durum <> 'Silindi';";

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
            SET m.durum = 'Silindi',
                m.silinme_tarihi = CURRENT_TIMESTAMP,
                m.silinme_nedeni = 'Firma tarafindan silindi',
                m.silinme_gorunum_metni = 'Bu mesaj silindi.',
                m.firma_gizlendi_mi = 1
            FROM mesajlar m
            INNER JOIN mesaj_konusmalari mk ON mk.id = m.konusma_id
            WHERE m.id = @messageId
              AND m.konusma_id = @conversationId
              AND mk.firma_id = @firmaId
              AND m.gonderen_turu = 'Firma'
              AND m.gonderen_firma_id = @firmaId
              AND m.durum <> 'Silindi';";

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
                   COALESCE(f.firma_adi, o.otel_adi, 'Otelturizm') AS title_text,
                   COALESCE(mk.konu_basligi, 'Mesajlar') AS subtitle_text,
                   COALESCE(mk.son_mesaj_onizleme, 'Henüz mesaj yok') AS preview_text,
                   CASE
                       WHEN @viewerType = 'firma' THEN COALESCE(mk.firma_okunmamis_sayisi, 0)
                       ELSE COALESCE(mk.misafir_okunmamis_sayisi, 0)
                   END AS unread_count
            FROM mesaj_konusmalari mk
            LEFT JOIN oteller o ON o.id = mk.otel_id
            LEFT JOIN firmalar f ON f.id = mk.firma_id
            WHERE ((@viewerType = 'user' AND mk.misafir_kullanici_id = @userId)
               OR  (@viewerType = 'firma' AND mk.firma_id = @firmaId))
              AND mk.durum <> 'Arşivlendi'
            ORDER BY COALESCE(mk.son_mesaj_tarihi, mk.olusturulma_tarihi) DESC, mk.id DESC;";

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
                    UnreadCount = reader.IsDBNull(4) ? 0 : reader.GetInt32(4),
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
            SELECT TOP (1) COALESCE(f.firma_adi, o.otel_adi, 'Otelturizm') AS title_text,
                   COALESCE(mk.konu_basligi, 'Mesaj detayı') AS subtitle_text
            FROM mesaj_konusmalari mk
            LEFT JOIN oteller o ON o.id = mk.otel_id
            LEFT JOIN firmalar f ON f.id = mk.firma_id
            WHERE mk.id = @conversationId
              AND ((@viewerType = 'user' AND mk.misafir_kullanici_id = @userId)
               OR  (@viewerType = 'firma' AND mk.firma_id = @firmaId));";

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
                   m.gonderen_turu,
                   COALESCE(m.mesaj_metni, ''),
                   m.gonderim_tarihi,
                   COALESCE(m.durum, 'Gönderildi'),
                   COALESCE(m.silinme_gorunum_metni, 'Bu mesaj silindi.'),
                   COALESCE(u.ad_soyad, fu.ad_soyad, f.firma_adi, o.otel_adi, 'Otelturizm') AS sender_name,
                   COALESCE(m.gonderen_kullanici_id, 0),
                   COALESCE(m.gonderen_firma_id, 0)
            FROM mesajlar m
            LEFT JOIN users u ON u.id = m.gonderen_kullanici_id
            LEFT JOIN firmalar f ON f.id = m.gonderen_firma_id
            LEFT JOIN users fu ON fu.id = m.gonderen_firma_kullanici_id
            LEFT JOIN oteller o ON o.id = m.gonderen_otel_id
            WHERE m.konusma_id = @conversationId
            ORDER BY m.gonderim_tarihi ASC, m.id ASC;";

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
            FROM rezervasyonlar
            WHERE kullanici_id = @userId
              AND otel_id = @hotelId
              AND durum NOT IN ('İptal Edildi', 'Reddedildi')
            ORDER BY COALESCE(cikis_tarihi, giris_tarihi) DESC, id DESC;";

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
                o.otel_adi,
                COALESCE(oks.user_id, o.user_id, 0) AS manager_user_id
            FROM oteller o
            LEFT JOIN otel_kullanici_sahiplikleri oks
                ON oks.otel_id = o.id
               AND oks.aktif_mi = 1
               AND oks.ana_sorumlu_mu = 1
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
            SELECT md.mesaj_id, gfv.id, gfv.orijinal_dosya_adi, gfv.mime_tipi, gfv.dosya_boyutu, gfv.gorsel_mi
            FROM mesaj_dosyalari md
            INNER JOIN guvenli_dosya_varliklari gfv ON gfv.id = md.guvenli_dosya_id
            INNER JOIN mesajlar m ON m.id = md.mesaj_id
            WHERE m.konusma_id = @conversationId
              AND md.aktif_mi = 1
              AND gfv.aktif_mi = 1
            ORDER BY md.siralama ASC, md.id ASC;";

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
                INSERT INTO mesajlar
                (
                    konusma_id, gonderen_turu, gonderen_kullanici_id, gonderen_firma_id, gonderen_firma_kullanici_id,
                    mesaj_metni, mesaj_tipi, okundu_mu, durum, ip_adresi, cihaz_bilgisi, gonderim_tarihi
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
                        INSERT INTO mesaj_dosyalari
                        (mesaj_id, guvenli_dosya_id, gosterim_adi, siralama, aktif_mi)
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
                UPDATE mesaj_konusmalari
                SET son_mesaj_tarihi = CURRENT_TIMESTAMP,
                    son_mesaj_gonderen = @senderType,
                    son_mesaj_onizleme = LEFT(@preview, 100),
                    misafir_okunmamis_sayisi = CASE WHEN @senderType = 'Firma' THEN misafir_okunmamis_sayisi + 1 ELSE 0 END,
                    firma_okunmamis_sayisi = CASE WHEN @senderType = 'Misafir' THEN firma_okunmamis_sayisi + 1 ELSE 0 END,
                    misafir_son_okuma_tarihi = CASE WHEN @senderType = 'Misafir' THEN CURRENT_TIMESTAMP ELSE misafir_son_okuma_tarihi END,
                    firma_son_okuma_tarihi = CASE WHEN @senderType = 'Firma' THEN CURRENT_TIMESTAMP ELSE firma_son_okuma_tarihi END
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
            UPDATE mesaj_konusmalari
            SET misafir_okunmamis_sayisi = 0,
                misafir_son_okuma_tarihi = CURRENT_TIMESTAMP
            WHERE id = @conversationId
              AND misafir_kullanici_id = @userId;", connection);
        command.Parameters.AddWithValue("@conversationId", conversationId);
        command.Parameters.AddWithValue("@userId", userId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task MarkFirmaConversationAsReadAsync(SqlConnection connection, long conversationId, long firmaId, CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand(@"
            UPDATE mesaj_konusmalari
            SET firma_okunmamis_sayisi = 0,
                firma_son_okuma_tarihi = CURRENT_TIMESTAMP
            WHERE id = @conversationId
              AND firma_id = @firmaId;", connection);
        command.Parameters.AddWithValue("@conversationId", conversationId);
        command.Parameters.AddWithValue("@firmaId", firmaId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<bool> AuthorizeConversationForUserAsync(SqlConnection connection, long conversationId, long userId, CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand("SELECT COUNT(*) FROM mesaj_konusmalari WHERE id = @id AND misafir_kullanici_id = @userId;", connection);
        command.Parameters.AddWithValue("@id", conversationId);
        command.Parameters.AddWithValue("@userId", userId);
        var count = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken) ?? 0, CultureInfo.InvariantCulture);
        return count > 0;
    }

    private static async Task<bool> AuthorizeConversationForFirmaAsync(SqlConnection connection, long conversationId, long firmaId, CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand("SELECT COUNT(*) FROM mesaj_konusmalari WHERE id = @id AND firma_id = @firmaId;", connection);
        command.Parameters.AddWithValue("@id", conversationId);
        command.Parameters.AddWithValue("@firmaId", firmaId);
        var count = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken) ?? 0, CultureInfo.InvariantCulture);
        return count > 0;
    }

    private static async Task<long> ResolveFirmaIdAsync(SqlConnection connection, long userId, CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand("SELECT TOP (1) COALESCE(firma_id, 0) FROM users WHERE id = @userId;", connection);
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
