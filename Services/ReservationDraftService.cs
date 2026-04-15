using System.Globalization;
using MySqlConnector;
using otelturizmnew.Models.Reservations;
using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Services;

public class ReservationDraftService : IReservationDraftService
{
    public const string DraftCookieName = "Otelturizm.ReservationDraftKey";

    private readonly string _connectionString;

    public ReservationDraftService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection tanimli degil.");
    }

    public string EnsureSessionKey(HttpContext httpContext)
    {
        if (httpContext.Request.Cookies.TryGetValue(DraftCookieName, out var existing) && !string.IsNullOrWhiteSpace(existing))
        {
            return existing;
        }

        var key = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);
        httpContext.Response.Cookies.Append(DraftCookieName, key, new CookieOptions
        {
            HttpOnly = true,
            IsEssential = true,
            SameSite = SameSiteMode.Lax,
            Secure = true,
            Expires = DateTimeOffset.UtcNow.AddDays(90)
        });
        return key;
    }

    public async Task<ReservationDraftSummaryViewModel?> GetActiveDraftAsync(long? userId, string? sessionKey, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);

        if (userId.GetValueOrDefault() > 0 && !string.IsNullOrWhiteSpace(sessionKey))
        {
            const string attachSql = @"
                UPDATE rezervasyon_taslaklari
                SET user_id = @userId
                WHERE user_id IS NULL
                  AND session_anahtari = @sessionKey
                  AND durum IN ('Taslak','Profil Eksik','Giris Bekliyor');";
            await using var attachCommand = new MySqlCommand(attachSql, connection);
            attachCommand.Parameters.AddWithValue("@userId", userId.Value);
            attachCommand.Parameters.AddWithValue("@sessionKey", sessionKey.Trim());
            await attachCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        const string sql = @"
            SELECT
                rt.id,
                rt.otel_id,
                rt.oda_tip_id,
                o.otel_adi,
                COALESCE(ot.oda_adi, 'Oda secimi bekleniyor'),
                rt.durum,
                rt.giris_tarihi,
                rt.cikis_tarihi,
                rt.yetiskin_sayisi,
                rt.cocuk_sayisi,
                rt.oda_sayisi,
                COALESCE(rt.toplam_tutar, 0),
                o.otel_kodu
            FROM rezervasyon_taslaklari rt
            INNER JOIN oteller o ON o.id = rt.otel_id
            LEFT JOIN oda_tipleri ot ON ot.id = rt.oda_tip_id
            WHERE rt.durum IN ('Taslak','Profil Eksik','Giris Bekliyor')
              AND ((@userId > 0 AND rt.user_id = @userId) OR (@sessionKey <> '' AND rt.session_anahtari = @sessionKey))
            ORDER BY rt.son_aktivite_tarihi DESC, rt.id DESC
            LIMIT 1;";

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@userId", userId.GetValueOrDefault());
        command.Parameters.AddWithValue("@sessionKey", sessionKey?.Trim() ?? string.Empty);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        var hotelName = reader.GetString(3);
        var hotelCode = reader.GetString(12);
        var status = reader.GetString(5);

        return new ReservationDraftSummaryViewModel
        {
            DraftId = reader.GetInt64(0),
            HotelId = reader.GetInt64(1),
            RoomTypeId = reader.IsDBNull(2) ? null : reader.GetInt64(2),
            HotelName = hotelName,
            RoomName = reader.GetString(4),
            Status = status,
            CheckInText = reader.GetDateTime(6).ToString("dd MMM yyyy", CultureInfo.GetCultureInfo("tr-TR")),
            CheckOutText = reader.GetDateTime(7).ToString("dd MMM yyyy", CultureInfo.GetCultureInfo("tr-TR")),
            AdultCount = reader.GetInt32(8),
            ChildCount = reader.GetInt32(9),
            RoomCount = reader.GetInt32(10),
            TotalText = FormatMoney(ReadDecimal(reader, 11)),
            Message = BuildStatusMessage(status),
            ResumeUrl = $"/oteller/{BuildSlug(hotelName, hotelCode)}"
        };
    }

    public async Task<long> SaveOrUpdateAsync(ReservationDraftUpsertRequest request, CancellationToken cancellationToken = default)
    {
        if (request.HotelId <= 0)
        {
            throw new InvalidOperationException("Taslak icin otel bilgisi zorunludur.");
        }

        await using var connection = await OpenConnectionAsync(cancellationToken);

        if (request.UserId.GetValueOrDefault() > 0 && !string.IsNullOrWhiteSpace(request.SessionKey))
        {
            const string attachSql = @"
                UPDATE rezervasyon_taslaklari
                SET user_id = @userId
                WHERE user_id IS NULL
                  AND session_anahtari = @sessionKey
                  AND durum IN ('Taslak','Profil Eksik','Giris Bekliyor');";
            await using var attachCommand = new MySqlCommand(attachSql, connection);
            attachCommand.Parameters.AddWithValue("@userId", request.UserId!.Value);
            attachCommand.Parameters.AddWithValue("@sessionKey", request.SessionKey!.Trim());
            await attachCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        const string findSql = @"
            SELECT id
            FROM rezervasyon_taslaklari
            WHERE otel_id = @hotelId
              AND ((@userId > 0 AND user_id = @userId) OR (@sessionKey <> '' AND session_anahtari = @sessionKey))
              AND durum IN ('Taslak','Profil Eksik','Giris Bekliyor')
            ORDER BY son_aktivite_tarihi DESC, id DESC
            LIMIT 1;";

        long? existingId = null;
        await using (var findCommand = new MySqlCommand(findSql, connection))
        {
            findCommand.Parameters.AddWithValue("@hotelId", request.HotelId);
            findCommand.Parameters.AddWithValue("@userId", request.UserId.GetValueOrDefault());
            findCommand.Parameters.AddWithValue("@sessionKey", request.SessionKey?.Trim() ?? string.Empty);
            var scalar = await findCommand.ExecuteScalarAsync(cancellationToken);
            if (scalar is not null and not DBNull)
            {
                existingId = Convert.ToInt64(scalar, CultureInfo.InvariantCulture);
            }
        }

        if (existingId.HasValue)
        {
            const string updateSql = @"
                UPDATE rezervasyon_taslaklari
                SET user_id = @userId,
                    session_anahtari = @sessionKey,
                    kaynak = @source,
                    durum = @status,
                    oda_tip_id = @roomTypeId,
                    misafir_ad_soyad = @guestFullName,
                    misafir_eposta = @guestEmail,
                    misafir_telefon = @guestPhone,
                    misafir_sehir = @guestCity,
                    misafir_ilce = @guestDistrict,
                    misafir_mahalle = @guestNeighborhood,
                    misafir_adres = @guestAddress,
                    giris_tarihi = @checkIn,
                    cikis_tarihi = @checkOut,
                    yetiskin_sayisi = @adultCount,
                    cocuk_sayisi = @childCount,
                    oda_sayisi = @roomCount,
                    gecelik_fiyat = @nightlyPrice,
                    vergi_tutari = @taxAmount,
                    toplam_tutar = @totalAmount,
                    para_birimi = @currency,
                    donus_url = @returnUrl,
                    profil_tamamlanma_url = @profileCompletionUrl,
                    notlar = @notes,
                    gecerlilik_tarihi = DATE_ADD(UTC_TIMESTAMP(), INTERVAL 30 DAY),
                    son_aktivite_tarihi = UTC_TIMESTAMP()
                WHERE id = @draftId;";
            await using var updateCommand = new MySqlCommand(updateSql, connection);
            BindDraftParameters(updateCommand, request);
            updateCommand.Parameters.AddWithValue("@draftId", existingId.Value);
            await updateCommand.ExecuteNonQueryAsync(cancellationToken);
            return existingId.Value;
        }

        const string insertSql = @"
            INSERT INTO rezervasyon_taslaklari
            (
                taslak_kodu, user_id, session_anahtari, kaynak, durum, otel_id, oda_tip_id,
                misafir_ad_soyad, misafir_eposta, misafir_telefon, misafir_sehir, misafir_ilce, misafir_mahalle, misafir_adres,
                giris_tarihi, cikis_tarihi, yetiskin_sayisi, cocuk_sayisi, oda_sayisi,
                gecelik_fiyat, vergi_tutari, toplam_tutar, para_birimi, donus_url, profil_tamamlanma_url, notlar,
                gecerlilik_tarihi
            )
            VALUES
            (
                @draftCode, @userId, @sessionKey, @source, @status, @hotelId, @roomTypeId,
                @guestFullName, @guestEmail, @guestPhone, @guestCity, @guestDistrict, @guestNeighborhood, @guestAddress,
                @checkIn, @checkOut, @adultCount, @childCount, @roomCount,
                @nightlyPrice, @taxAmount, @totalAmount, @currency, @returnUrl, @profileCompletionUrl, @notes,
                DATE_ADD(UTC_TIMESTAMP(), INTERVAL 30 DAY)
            );
            SELECT LAST_INSERT_ID();";
        await using var insertCommand = new MySqlCommand(insertSql, connection);
        BindDraftParameters(insertCommand, request);
        insertCommand.Parameters.AddWithValue("@draftCode", Guid.NewGuid().ToString("D", CultureInfo.InvariantCulture));
        return Convert.ToInt64(await insertCommand.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture);
    }

    public async Task MarkCompletedAsync(long draftId, long reservationId, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        const string sql = @"
            UPDATE rezervasyon_taslaklari
            SET durum = 'Tamamlandi',
                tamamlanan_rezervasyon_id = @reservationId,
                son_aktivite_tarihi = UTC_TIMESTAMP(),
                son_bildirim_tarihi = UTC_TIMESTAMP()
            WHERE id = @draftId;";
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@draftId", draftId);
        command.Parameters.AddWithValue("@reservationId", reservationId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void BindDraftParameters(MySqlCommand command, ReservationDraftUpsertRequest request)
    {
        command.Parameters.AddWithValue("@userId", request.UserId.HasValue ? request.UserId.Value : DBNull.Value);
        command.Parameters.AddWithValue("@sessionKey", string.IsNullOrWhiteSpace(request.SessionKey) ? DBNull.Value : request.SessionKey!.Trim());
        command.Parameters.AddWithValue("@source", request.Source);
        command.Parameters.AddWithValue("@status", request.Status);
        command.Parameters.AddWithValue("@hotelId", request.HotelId);
        command.Parameters.AddWithValue("@roomTypeId", request.RoomTypeId.HasValue ? request.RoomTypeId.Value : DBNull.Value);
        command.Parameters.AddWithValue("@guestFullName", NullIfWhiteSpace(request.GuestFullName));
        command.Parameters.AddWithValue("@guestEmail", NullIfWhiteSpace(request.GuestEmail));
        command.Parameters.AddWithValue("@guestPhone", NullIfWhiteSpace(request.GuestPhone));
        command.Parameters.AddWithValue("@guestCity", NullIfWhiteSpace(request.GuestCity));
        command.Parameters.AddWithValue("@guestDistrict", NullIfWhiteSpace(request.GuestDistrict));
        command.Parameters.AddWithValue("@guestNeighborhood", NullIfWhiteSpace(request.GuestNeighborhood));
        command.Parameters.AddWithValue("@guestAddress", NullIfWhiteSpace(request.GuestAddress));
        command.Parameters.AddWithValue("@checkIn", request.CheckInDate.ToDateTime(TimeOnly.MinValue));
        command.Parameters.AddWithValue("@checkOut", request.CheckOutDate.ToDateTime(TimeOnly.MinValue));
        command.Parameters.AddWithValue("@adultCount", request.AdultCount);
        command.Parameters.AddWithValue("@childCount", request.ChildCount);
        command.Parameters.AddWithValue("@roomCount", request.RoomCount);
        command.Parameters.AddWithValue("@nightlyPrice", request.NightlyPrice.HasValue ? request.NightlyPrice.Value : DBNull.Value);
        command.Parameters.AddWithValue("@taxAmount", request.TaxAmount.HasValue ? request.TaxAmount.Value : DBNull.Value);
        command.Parameters.AddWithValue("@totalAmount", request.TotalAmount.HasValue ? request.TotalAmount.Value : DBNull.Value);
        command.Parameters.AddWithValue("@currency", request.Currency);
        command.Parameters.AddWithValue("@returnUrl", NullIfWhiteSpace(request.ReturnUrl));
        command.Parameters.AddWithValue("@profileCompletionUrl", NullIfWhiteSpace(request.ProfileCompletionUrl));
        command.Parameters.AddWithValue("@notes", NullIfWhiteSpace(request.Notes));
    }

    private static object NullIfWhiteSpace(string? value)
        => string.IsNullOrWhiteSpace(value) ? DBNull.Value : value.Trim();

    private static string BuildStatusMessage(string status) => status switch
    {
        "Profil Eksik" => "Profil bilgileriniz eksik kaldigi icin rezervasyonunuz taslakta bekliyor.",
        "Giris Bekliyor" => "Devam etmek icin once giris yapmaniz gerekiyor.",
        _ => "Tamamlanmamis rezervasyonunuz sizi bekliyor."
    };

    private static string BuildSlug(string hotelName, string hotelCode)
    {
        var source = string.IsNullOrWhiteSpace(hotelName) ? hotelCode : hotelName;
        var chars = new List<char>(source.Length);
        foreach (var ch in source.ToLowerInvariant())
        {
            chars.Add(ch switch
            {
                'ı' => 'i',
                'ğ' => 'g',
                'ü' => 'u',
                'ş' => 's',
                'ö' => 'o',
                'ç' => 'c',
                _ when char.IsLetterOrDigit(ch) => ch,
                _ => '-'
            });
        }

        var slug = new string(chars.ToArray()).Trim('-');
        while (slug.Contains("--", StringComparison.Ordinal))
        {
            slug = slug.Replace("--", "-", StringComparison.Ordinal);
        }

        return string.IsNullOrWhiteSpace(slug) ? hotelCode.ToLowerInvariant() : slug;
    }

    private static decimal ReadDecimal(MySqlDataReader reader, int ordinal)
        => reader.IsDBNull(ordinal) ? 0m : Convert.ToDecimal(reader.GetValue(ordinal), CultureInfo.InvariantCulture);

    private static string FormatMoney(decimal amount)
        => amount <= 0 ? "Tutar bekleniyor" : $"₺{amount:N0}";

    private async Task<MySqlConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}
