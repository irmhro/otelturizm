using System.Globalization;
using Microsoft.Data.SqlClient;
using SqlConnection = Microsoft.Data.SqlClient.SqlConnection;
using SqlCommand = Microsoft.Data.SqlClient.SqlCommand;
using SqlTransaction = Microsoft.Data.SqlClient.SqlTransaction;
using SqlException = Microsoft.Data.SqlClient.SqlException;
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
            var resolvedUserId = userId.GetValueOrDefault();
            const string attachSql = @"
                UPDATE rezervasyon_taslaklari
                SET user_id = @userId
                WHERE user_id IS NULL
                  AND session_anahtari = @sessionKey
                  AND durum IN ('Taslak','Profil Eksik','Giris Bekliyor');";
            await using var attachCommand = new SqlCommand(attachSql, connection);
            attachCommand.Parameters.AddWithValue("@userId", resolvedUserId);
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
                COALESCE(rt.net_oda_tutari, 0),
                COALESCE(rt.kdv_orani, 0),
                COALESCE(rt.kdv_tutari, 0),
                COALESCE(rt.konaklama_vergisi_orani, 0),
                COALESCE(rt.konaklama_vergisi_tutari, 0),
                o.otel_kodu
            FROM rezervasyon_taslaklari rt
            INNER JOIN oteller o ON o.id = rt.otel_id
            LEFT JOIN oda_tipleri ot ON ot.id = rt.oda_tip_id
            WHERE rt.durum IN ('Taslak','Profil Eksik','Giris Bekliyor')
              AND ((@userId > 0 AND rt.user_id = @userId) OR (@sessionKey <> '' AND rt.session_anahtari = @sessionKey))
            ORDER BY rt.son_aktivite_tarihi DESC, rt.id DESC
            OFFSET 0 ROWS FETCH NEXT 1 ROWS ONLY;";

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@userId", userId.GetValueOrDefault());
        command.Parameters.AddWithValue("@sessionKey", sessionKey?.Trim() ?? string.Empty);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        var draftId = reader.GetInt64(0);
        var hotelId = reader.GetInt64(1);
        long? roomTypeId = reader.IsDBNull(2) ? null : reader.GetInt64(2);
        var hotelName = reader.GetString(3);
        var hotelCode = reader.GetString(17);
        var roomName = reader.GetString(4);
        var status = reader.GetString(5);
        var checkInDate = DateOnly.FromDateTime(reader.GetDateTime(6));
        var checkOutDate = DateOnly.FromDateTime(reader.GetDateTime(7));
        var adultCount = Convert.ToInt32(reader.GetValue(8), CultureInfo.InvariantCulture);
        var childCount = Convert.ToInt32(reader.GetValue(9), CultureInfo.InvariantCulture);
        var roomCount = Convert.ToInt32(reader.GetValue(10), CultureInfo.InvariantCulture);
        var totalAmount = ReadDecimal(reader, 11);
        await reader.DisposeAsync();

        var resolvedStatus = status;
        if (userId.GetValueOrDefault() > 0)
        {
            var hasRequiredProfile = await HasRequiredReservationProfileAsync(connection, userId.GetValueOrDefault(), cancellationToken);
            if (string.Equals(status, "Giris Bekliyor", StringComparison.OrdinalIgnoreCase))
            {
                resolvedStatus = hasRequiredProfile ? "Taslak" : "Profil Eksik";
            }
            else if (string.Equals(status, "Profil Eksik", StringComparison.OrdinalIgnoreCase) && hasRequiredProfile)
            {
                resolvedStatus = "Taslak";
            }

            if (!string.Equals(resolvedStatus, status, StringComparison.OrdinalIgnoreCase))
            {
                const string updateSql = @"
                    UPDATE rezervasyon_taslaklari
                    SET durum = @status,
                        son_aktivite_tarihi = SYSUTCDATETIME()
                    WHERE id = @draftId;";
                await using var updateCommand = new SqlCommand(updateSql, connection);
                updateCommand.Parameters.AddWithValue("@status", resolvedStatus);
                updateCommand.Parameters.AddWithValue("@draftId", draftId);
                await updateCommand.ExecuteNonQueryAsync(cancellationToken);
            }
        }

        var hotelSlug = BuildSlug(hotelName, hotelCode);
        var requiresProfileCompletion = string.Equals(resolvedStatus, "Profil Eksik", StringComparison.OrdinalIgnoreCase);

        return new ReservationDraftSummaryViewModel
        {
            DraftId = draftId,
            HotelId = hotelId,
            RoomTypeId = roomTypeId,
            HotelName = hotelName,
            RoomName = roomName,
            Status = resolvedStatus,
            CheckInDate = checkInDate,
            CheckOutDate = checkOutDate,
            CheckInText = checkInDate.ToDateTime(TimeOnly.MinValue).ToString("dd MMM yyyy", CultureInfo.GetCultureInfo("tr-TR")),
            CheckOutText = checkOutDate.ToDateTime(TimeOnly.MinValue).ToString("dd MMM yyyy", CultureInfo.GetCultureInfo("tr-TR")),
            AdultCount = adultCount,
            ChildCount = childCount,
            RoomCount = roomCount,
            TotalText = FormatMoney(totalAmount),
            Message = BuildStatusMessage(resolvedStatus),
            RequiresProfileCompletion = requiresProfileCompletion,
            ResumeUrl = $"/oteller/{hotelSlug}?continueDraft=1",
            ProfileCompletionUrl = $"/oteller/{hotelSlug}?continueDraft=1&openProfile=1"
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
            await using var attachCommand = new SqlCommand(attachSql, connection);
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
            OFFSET 0 ROWS FETCH NEXT 1 ROWS ONLY;";

        long? existingId = null;
        await using (var findCommand = new SqlCommand(findSql, connection))
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
                    net_oda_tutari = @netRoomAmount,
                    kdv_orani = @vatRate,
                    kdv_tutari = @vatAmount,
                    konaklama_vergisi_orani = @accommodationTaxRate,
                    konaklama_vergisi_tutari = @accommodationTaxAmount,
                    vergi_tutari = @taxAmount,
                    toplam_tutar = @totalAmount,
                    para_birimi = @currency,
                    donus_url = @returnUrl,
                    profil_tamamlanma_url = @profileCompletionUrl,
                    notlar = @notes,
                    gecerlilik_tarihi = DATEADD(DAY, 30, SYSUTCDATETIME()),
                    son_aktivite_tarihi = SYSUTCDATETIME()
                WHERE id = @draftId;";
            await using var updateCommand = new SqlCommand(updateSql, connection);
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
                gecelik_fiyat, net_oda_tutari, kdv_orani, kdv_tutari, konaklama_vergisi_orani, konaklama_vergisi_tutari,
                vergi_tutari, toplam_tutar, para_birimi, donus_url, profil_tamamlanma_url, notlar,
                gecerlilik_tarihi
            )
            VALUES
            (
                @draftCode, @userId, @sessionKey, @source, @status, @hotelId, @roomTypeId,
                @guestFullName, @guestEmail, @guestPhone, @guestCity, @guestDistrict, @guestNeighborhood, @guestAddress,
                @checkIn, @checkOut, @adultCount, @childCount, @roomCount,
                @nightlyPrice, @netRoomAmount, @vatRate, @vatAmount, @accommodationTaxRate, @accommodationTaxAmount,
                @taxAmount, @totalAmount, @currency, @returnUrl, @profileCompletionUrl, @notes,
                DATEADD(DAY, 30, SYSUTCDATETIME())
            );
            SELECT CAST(SCOPE_IDENTITY() AS bigint);";
        await using var insertCommand = new SqlCommand(insertSql, connection);
        BindDraftParameters(insertCommand, request);
        insertCommand.Parameters.AddWithValue("@draftCode", Guid.NewGuid().ToString("D", CultureInfo.InvariantCulture));
        return Convert.ToInt64(await insertCommand.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture);
    }

    public async Task MarkCompletedAsync(long draftId, long reservationId, CancellationToken cancellationToken = default)
    {
        _ = reservationId;
        await using var connection = await OpenConnectionAsync(cancellationToken);
        const string sql = @"
            DELETE FROM rezervasyon_taslaklari
            WHERE id = @draftId;";
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@draftId", draftId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>
    /// Taslagi sil; sadece oturum veya kullaniciya bagli kayitlar iptal edilebilir.
    /// </summary>
    public async Task CancelDraftAsync(long draftId, long userId, string? sessionKey, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        const string sql = @"
            DELETE FROM rezervasyon_taslaklari
            WHERE id = @draftId
              AND durum IN ('Taslak','Profil Eksik','Giris Bekliyor')
              AND (
                  (@userId > 0 AND user_id = @userId)
                  OR (@sessionKey <> '' AND session_anahtari = @sessionKey)
              );";
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@draftId", draftId);
        command.Parameters.AddWithValue("@userId", userId);
        command.Parameters.AddWithValue("@sessionKey", sessionKey?.Trim() ?? string.Empty);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void BindDraftParameters(SqlCommand command, ReservationDraftUpsertRequest request)
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
        command.Parameters.AddWithValue("@netRoomAmount", request.NetRoomAmount.HasValue ? request.NetRoomAmount.Value : DBNull.Value);
        command.Parameters.AddWithValue("@vatRate", request.VatRate.HasValue ? request.VatRate.Value : DBNull.Value);
        command.Parameters.AddWithValue("@vatAmount", request.VatAmount.HasValue ? request.VatAmount.Value : DBNull.Value);
        command.Parameters.AddWithValue("@accommodationTaxRate", request.AccommodationTaxRate.HasValue ? request.AccommodationTaxRate.Value : DBNull.Value);
        command.Parameters.AddWithValue("@accommodationTaxAmount", request.AccommodationTaxAmount.HasValue ? request.AccommodationTaxAmount.Value : DBNull.Value);
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

    private static decimal ReadDecimal(SqlDataReader reader, int ordinal)
        => reader.IsDBNull(ordinal) ? 0m : Convert.ToDecimal(reader.GetValue(ordinal), CultureInfo.InvariantCulture);

    private static string FormatMoney(decimal amount)
        => amount <= 0 ? "Tutar bekleniyor" : $"₺{amount:N0}";

    private static async Task<bool> HasRequiredReservationProfileAsync(SqlConnection connection, long userId, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT COALESCE(eposta, ''),
                   dogum_tarihi,
                   COALESCE(cinsiyet, ''),
                   COALESCE(sehir, ''),
                   COALESCE(ilce, ''),
                   COALESCE(mahalle, '')
            FROM users
            WHERE id = @userId
            ORDER BY id
            OFFSET 0 ROWS FETCH NEXT 1 ROWS ONLY;";
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@userId", userId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return false;
        }

        return !string.IsNullOrWhiteSpace(reader.GetString(0))
               && !reader.IsDBNull(1)
               && !string.IsNullOrWhiteSpace(reader.GetString(2))
               && !string.IsNullOrWhiteSpace(reader.GetString(3))
               && !string.IsNullOrWhiteSpace(reader.GetString(4))
               && !string.IsNullOrWhiteSpace(reader.GetString(5));
    }

    private async Task<SqlConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}
