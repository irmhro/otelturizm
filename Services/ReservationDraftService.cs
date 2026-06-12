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
                UPDATE [dbo].[REZERVASYON_TASLAKLARI]
                SET [KULLANICI_ID] = @userId
                WHERE [KULLANICI_ID] IS NULL
                  AND [SESSION_ANAHTARI] = @sessionKey
                  AND [DURUM] IN ('Taslak','Profil Eksik','Giris Bekliyor');";
            await using var attachCommand = new SqlCommand(attachSql, connection);
            attachCommand.Parameters.AddWithValue("@userId", resolvedUserId);
            attachCommand.Parameters.AddWithValue("@sessionKey", sessionKey.Trim());
            await attachCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        const string sql = @"
            SELECT
                rt.id,
                rt.[OTEL_ID],
                rt.[ODA_TIP_ID],
                o.[OTEL_ADI],
                COALESCE(ot.[ODA_ADI], 'Oda secimi bekleniyor'),
                rt.[DURUM],
                rt.[GIRIS_TARIHI],
                rt.[CIKIS_TARIHI],
                rt.[YETISKIN_SAYISI],
                rt.[COCUK_SAYISI],
                rt.[ODA_SAYISI],
                COALESCE(rt.[TOPLAM_TUTAR], 0),
                COALESCE(rt.[NET_ODA_TUTARI], 0),
                COALESCE(rt.[KDV_ORANI], 0),
                COALESCE(rt.[KDV_TUTARI], 0),
                COALESCE(rt.[KONAKLAMA_VERGISI_ORANI], 0),
                COALESCE(rt.[KONAKLAMA_VERGISI_TUTARI], 0),
                o.[OTEL_KODU]
            FROM [dbo].[REZERVASYON_TASLAKLARI] rt
            INNER JOIN [dbo].[OTELLER] o ON o.id = rt.[OTEL_ID]
            LEFT JOIN [dbo].[ODA_TIPLERI] ot ON ot.id = rt.[ODA_TIP_ID]
            WHERE rt.[DURUM] IN ('Taslak','Profil Eksik','Giris Bekliyor')
              AND ((@userId > 0 AND rt.[KULLANICI_ID] = @userId) OR (@sessionKey <> '' AND rt.[SESSION_ANAHTARI] = @sessionKey))
            ORDER BY rt.[SON_AKTIVITE_TARIHI] DESC, rt.id DESC
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
                    UPDATE [dbo].[REZERVASYON_TASLAKLARI]
                    SET [DURUM] = @status,
                        [SON_AKTIVITE_TARIHI] = SYSUTCDATETIME()
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
            ResumeUrl = $"/hotel/{hotelSlug}?continueDraft=1",
            ProfileCompletionUrl = $"/hotel/{hotelSlug}?continueDraft=1&openProfile=1"
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
                UPDATE [dbo].[REZERVASYON_TASLAKLARI]
                SET [KULLANICI_ID] = @userId
                WHERE [KULLANICI_ID] IS NULL
                  AND [SESSION_ANAHTARI] = @sessionKey
                  AND [DURUM] IN ('Taslak','Profil Eksik','Giris Bekliyor');";
            await using var attachCommand = new SqlCommand(attachSql, connection);
            attachCommand.Parameters.AddWithValue("@userId", request.UserId!.Value);
            attachCommand.Parameters.AddWithValue("@sessionKey", request.SessionKey!.Trim());
            await attachCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        const string findSql = @"
            SELECT id
            FROM [dbo].[REZERVASYON_TASLAKLARI]
            WHERE [OTEL_ID] = @hotelId
              AND ((@userId > 0 AND [KULLANICI_ID] = @userId) OR (@sessionKey <> '' AND [SESSION_ANAHTARI] = @sessionKey))
              AND [DURUM] IN ('Taslak','Profil Eksik','Giris Bekliyor')
            ORDER BY [SON_AKTIVITE_TARIHI] DESC, id DESC
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
                UPDATE [dbo].[REZERVASYON_TASLAKLARI]
                SET [KULLANICI_ID] = @userId,
                    [SESSION_ANAHTARI] = @sessionKey,
                    [KAYNAK] = @source,
                    [DURUM] = @status,
                    [ODA_TIP_ID] = @roomTypeId,
                    [MISAFIR_AD_SOYAD] = @guestFullName,
                    [MISAFIR_EPOSTA] = @guestEmail,
                    [MISAFIR_TELEFON] = @guestPhone,
                    [MISAFIR_SEHIR] = @guestCity,
                    [MISAFIR_ILCE] = @guestDistrict,
                    [MISAFIR_MAHALLE] = @guestNeighborhood,
                    [MISAFIR_ADRES] = @guestAddress,
                    [MISAFIR_ULKE_ID] = @guestUlkeId,
                    [MISAFIR_IL_ID] = @guestIlId,
                    [MISAFIR_ILCE_ID] = @guestIlceId,
                    [MISAFIR_MAHALLE_ID] = @guestMahalleId,
                    [GIRIS_TARIHI] = @checkIn,
                    [CIKIS_TARIHI] = @checkOut,
                    [YETISKIN_SAYISI] = @adultCount,
                    [COCUK_SAYISI] = @childCount,
                    [ODA_SAYISI] = @roomCount,
                    [GECELIK_FIYAT] = @nightlyPrice,
                    [NET_ODA_TUTARI] = @netRoomAmount,
                    [KDV_ORANI] = @vatRate,
                    [KDV_TUTARI] = @vatAmount,
                    [KONAKLAMA_VERGISI_ORANI] = @accommodationTaxRate,
                    [KONAKLAMA_VERGISI_TUTARI] = @accommodationTaxAmount,
                    [VERGI_TUTARI] = @taxAmount,
                    [TOPLAM_TUTAR] = @totalAmount,
                    [PARA_BIRIMI] = @currency,
                    [DONUS_URL] = @returnUrl,
                    [PROFIL_TAMAMLANMA_URL] = @profileCompletionUrl,
                    [NOTLAR] = @notes,
                    [GECERLILIK_TARIHI] = DATEADD(DAY, 30, SYSUTCDATETIME()),
                    [SON_AKTIVITE_TARIHI] = SYSUTCDATETIME()
                WHERE id = @draftId;";
            await using var updateCommand = new SqlCommand(updateSql, connection);
            BindDraftParameters(updateCommand, request);
            updateCommand.Parameters.AddWithValue("@draftId", existingId.Value);
            await updateCommand.ExecuteNonQueryAsync(cancellationToken);
            return existingId.Value;
        }

        const string insertSql = @"
            INSERT INTO [dbo].[REZERVASYON_TASLAKLARI]
            (
                [TASLAK_KODU], [KULLANICI_ID], [SESSION_ANAHTARI], [KAYNAK], [DURUM], [OTEL_ID], [ODA_TIP_ID],
                [MISAFIR_AD_SOYAD], [MISAFIR_EPOSTA], [MISAFIR_TELEFON], [MISAFIR_SEHIR], [MISAFIR_ILCE], [MISAFIR_MAHALLE], [MISAFIR_ADRES],
                [MISAFIR_ULKE_ID], [MISAFIR_IL_ID], [MISAFIR_ILCE_ID], [MISAFIR_MAHALLE_ID],
                [GIRIS_TARIHI], [CIKIS_TARIHI], [YETISKIN_SAYISI], [COCUK_SAYISI], [ODA_SAYISI],
                [GECELIK_FIYAT], [NET_ODA_TUTARI], [KDV_ORANI], [KDV_TUTARI], [KONAKLAMA_VERGISI_ORANI], [KONAKLAMA_VERGISI_TUTARI],
                [VERGI_TUTARI], [TOPLAM_TUTAR], [PARA_BIRIMI], [DONUS_URL], [PROFIL_TAMAMLANMA_URL], [NOTLAR],
                [GECERLILIK_TARIHI]
            )
            VALUES
            (
                @draftCode, @userId, @sessionKey, @source, @status, @hotelId, @roomTypeId,
                @guestFullName, @guestEmail, @guestPhone, @guestCity, @guestDistrict, @guestNeighborhood, @guestAddress,
                @guestUlkeId, @guestIlId, @guestIlceId, @guestMahalleId,
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
            DELETE FROM [dbo].[REZERVASYON_TASLAKLARI]
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
            DELETE FROM [dbo].[REZERVASYON_TASLAKLARI]
            WHERE id = @draftId
              AND [DURUM] IN ('Taslak','Profil Eksik','Giris Bekliyor')
              AND (
                  (@userId > 0 AND [KULLANICI_ID] = @userId)
                  OR (@sessionKey <> '' AND [SESSION_ANAHTARI] = @sessionKey)
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
        command.Parameters.AddWithValue("@guestUlkeId", request.GuestUlkeId.HasValue ? request.GuestUlkeId.Value : DBNull.Value);
        command.Parameters.AddWithValue("@guestIlId", request.GuestIlId.HasValue ? request.GuestIlId.Value : DBNull.Value);
        command.Parameters.AddWithValue("@guestIlceId", request.GuestIlceId.HasValue ? request.GuestIlceId.Value : DBNull.Value);
        command.Parameters.AddWithValue("@guestMahalleId", request.GuestMahalleId.HasValue ? request.GuestMahalleId.Value : DBNull.Value);
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
            SELECT COALESCE([EPOSTA], ''),
                   [DOGUM_TARIHI],
                   COALESCE([CINSIYET], ''),
                   COALESCE([SEHIR], ''),
                   COALESCE(ilce, ''),
                   COALESCE([MAHALLE], ''),
                   [IL_ID],
                   [ILCE_ID],
                   [MAHALLE_ID]
            FROM [dbo].[KULLANICILAR]
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

        var hasTextLocation = !string.IsNullOrWhiteSpace(reader.GetString(3))
                              && !string.IsNullOrWhiteSpace(reader.GetString(4))
                              && !string.IsNullOrWhiteSpace(reader.GetString(5));
        var hasGeoLocation = !reader.IsDBNull(6) && !reader.IsDBNull(7) && !reader.IsDBNull(8);

        return !string.IsNullOrWhiteSpace(reader.GetString(0))
               && !reader.IsDBNull(1)
               && !string.IsNullOrWhiteSpace(reader.GetString(2))
               && (hasTextLocation || hasGeoLocation);
    }

    private async Task<SqlConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}
