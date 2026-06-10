using System.Globalization;
using Microsoft.Data.SqlClient;
using otelturizmnew.Models.Paneller.User;
using otelturizmnew.Models.Payments;
using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Services;

public sealed class PaymentCardService : IPaymentCardService
{
    private readonly string _connectionString;
    private readonly PaymentCardCryptoService _crypto;

    public PaymentCardService(IConfiguration configuration, PaymentCardCryptoService crypto)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection tanimli degil.");
        _crypto = crypto;
    }

    public async Task<UserPaymentMethodsPageViewModel> GetPaymentMethodsPageAsync(long userId, CancellationToken cancellationToken = default)
    {
        var model = new UserPaymentMethodsPageViewModel();
        await using var connection = await OpenConnectionAsync(cancellationToken);

        await using (var command = new SqlCommand(@"
            SELECT id, [KART_ETIKETI], [MARKA], [SON_DORT_HANE], [SON_KULLANIM_AY], [SON_KULLANIM_YIL], [VARSAYILAN_MI],
                   COALESCE([MASKELI_PAN], N'**** **** **** ' + [SON_DORT_HANE]) AS maskeli_pan
            FROM [dbo].[KULLANICI_ODEME_YONTEMLERI]
            WHERE [KULLANICI_ID] = @userId AND [AKTIF_MI] = 1
            ORDER BY [VARSAYILAN_MI] DESC, id DESC;", connection))
        {
            command.Parameters.AddWithValue("@userId", userId);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var maskedPan = reader.GetString(7);
                model.Methods.Add(new UserPaymentMethodRowViewModel
                {
                    PaymentMethodId = reader.GetInt64(0),
                    Label = $"{reader.GetString(1)} · {reader.GetString(2)}",
                    DetailText = $"{maskedPan} · SKT {SafeInt(reader, 4):00}/{SafeInt(reader, 5)}" + (SafeBool(reader, 6) ? " · Varsayilan kart" : string.Empty),
                    MaskedPan = maskedPan,
                    IsDefault = SafeBool(reader, 6)
                });
            }
        }

        await using var billingCommand = new SqlCommand(@"
            SELECT TOP (1) [AD_SOYAD],
                   COALESCE(NULLIF([ADRES], ''), ''),
                   COALESCE(NULLIF(ilce, ''), ''),
                   COALESCE(NULLIF([SEHIR], ''), ''),
                   [EPOSTA]
            FROM [dbo].[KULLANICILAR]
            WHERE id = @userId;", connection);
        billingCommand.Parameters.AddWithValue("@userId", userId);
        await using var billingReader = await billingCommand.ExecuteReaderAsync(cancellationToken);
        if (await billingReader.ReadAsync(cancellationToken))
        {
            var invoiceName = billingReader.GetString(0);
            var addressLine = billingReader.GetString(1);
            var district = billingReader.GetString(2);
            var city = billingReader.GetString(3);
            var email = billingReader.GetString(4);
            var fullAddress = string.Join(", ", new[] { addressLine, district, city }.Where(static x => !string.IsNullOrWhiteSpace(x)));
            model.Billing = new UserBillingSummaryViewModel
            {
                InvoiceName = invoiceName,
                Address = string.IsNullOrWhiteSpace(fullAddress) ? "Adres bilgisi eklenmedi" : fullAddress,
                Email = email
            };
            model.BillingForm = new UserBillingForm
            {
                InvoiceName = invoiceName,
                AddressLine = addressLine,
                District = district,
                City = city,
                Email = email
            };
        }

        return model;
    }

    public async Task<(bool Success, string Message)> SavePaymentMethodAsync(long userId, UserPaymentMethodForm form, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(form.CardLabel) || string.IsNullOrWhiteSpace(form.CardHolder))
        {
            return (false, "Kart etiketi ve kart sahibi zorunludur.");
        }

        var pan = new string((form.CardNumber ?? string.Empty).Where(char.IsDigit).ToArray());
        if (!PaymentCardCryptoService.IsValidPan(pan))
        {
            return (false, "Gecerli bir kart numarasi girin.");
        }

        if (form.ExpiryMonth is < 1 or > 12 || form.ExpiryYear < DateTime.UtcNow.Year)
        {
            return (false, "Son kullanma tarihi gecersiz.");
        }

        var brand = string.IsNullOrWhiteSpace(form.Brand) ? PaymentCardCryptoService.DetectBrand(pan) : form.Brand.Trim();
        var lastFour = PaymentCardCryptoService.ExtractLastFour(pan);
        var maskedPan = PaymentCardCryptoService.MaskPan(pan);
        var token = PaymentCardCryptoService.CreateToken();
        var encrypted = _crypto.Encrypt(new PaymentCardPayload
        {
            Pan = pan,
            HolderName = form.CardHolder.Trim(),
            ExpiryMonth = form.ExpiryMonth,
            ExpiryYear = form.ExpiryYear,
            Brand = brand
        });

        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        if (form.SetAsDefault)
        {
            await using var clear = new SqlCommand("UPDATE [dbo].[KULLANICI_ODEME_YONTEMLERI] SET [VARSAYILAN_MI] = 0 WHERE [KULLANICI_ID] = @userId;", connection, (SqlTransaction)transaction);
            clear.Parameters.AddWithValue("@userId", userId);
            await clear.ExecuteNonQueryAsync(cancellationToken);
        }

        await using var command = new SqlCommand(@"
            INSERT INTO [dbo].[KULLANICI_ODEME_YONTEMLERI]
            ([KULLANICI_ID], [KART_ETIKETI], [KART_SAHIBI], [MARKA], [SON_DORT_HANE], [SON_KULLANIM_AY], [SON_KULLANIM_YIL],
             [VARSAYILAN_MI], [AKTIF_MI], [KART_TOKEN], [SIFRELI_KART_VERISI], [MASKELI_PAN])
            VALUES
            (@userId, @label, @holder, @brand, @lastFour, @month, @year, @isDefault, 1, @token, @encrypted, @maskedPan);", connection, (SqlTransaction)transaction);
        command.Parameters.AddWithValue("@userId", userId);
        command.Parameters.AddWithValue("@label", form.CardLabel.Trim());
        command.Parameters.AddWithValue("@holder", form.CardHolder.Trim());
        command.Parameters.AddWithValue("@brand", brand);
        command.Parameters.AddWithValue("@lastFour", lastFour);
        command.Parameters.AddWithValue("@month", form.ExpiryMonth);
        command.Parameters.AddWithValue("@year", form.ExpiryYear);
        command.Parameters.AddWithValue("@isDefault", form.SetAsDefault ? 1 : 0);
        command.Parameters.AddWithValue("@token", token);
        command.Parameters.Add("@encrypted", System.Data.SqlDbType.VarBinary, -1).Value = encrypted;
        command.Parameters.AddWithValue("@maskedPan", maskedPan);
        var affected = await command.ExecuteNonQueryAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return affected > 0
            ? (true, "Kart bilgileriniz guvenli sekilde kaydedildi.")
            : (false, "Kart kaydedilemedi.");
    }

    public async Task<bool> DeletePaymentMethodAsync(long userId, long paymentMethodId, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = new SqlCommand(@"
            UPDATE [dbo].[KULLANICI_ODEME_YONTEMLERI]
            SET [AKTIF_MI] = 0, [VARSAYILAN_MI] = 0, [GUNCELLENME_TARIHI] = SYSUTCDATETIME()
            WHERE id = @paymentMethodId AND [KULLANICI_ID] = @userId;", connection);
        command.Parameters.AddWithValue("@paymentMethodId", paymentMethodId);
        command.Parameters.AddWithValue("@userId", userId);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<bool> SetDefaultPaymentMethodAsync(long userId, long paymentMethodId, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await using (var clear = new SqlCommand("UPDATE [dbo].[KULLANICI_ODEME_YONTEMLERI] SET [VARSAYILAN_MI] = 0 WHERE [KULLANICI_ID] = @userId;", connection, (SqlTransaction)transaction))
        {
            clear.Parameters.AddWithValue("@userId", userId);
            await clear.ExecuteNonQueryAsync(cancellationToken);
        }

        await using var command = new SqlCommand(@"
            UPDATE [dbo].[KULLANICI_ODEME_YONTEMLERI]
            SET [VARSAYILAN_MI] = 1, [GUNCELLENME_TARIHI] = SYSUTCDATETIME()
            WHERE id = @paymentMethodId AND [KULLANICI_ID] = @userId AND [AKTIF_MI] = 1;", connection, (SqlTransaction)transaction);
        command.Parameters.AddWithValue("@paymentMethodId", paymentMethodId);
        command.Parameters.AddWithValue("@userId", userId);
        var affected = await command.ExecuteNonQueryAsync(cancellationToken);
        if (affected <= 0)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<SavedPaymentCardOptionViewModel>> GetUserCardOptionsAsync(long userId, CancellationToken cancellationToken = default)
    {
        var items = new List<SavedPaymentCardOptionViewModel>();
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = new SqlCommand(@"
            SELECT id, [KART_ETIKETI], [MARKA], [SON_KULLANIM_AY], [SON_KULLANIM_YIL], [VARSAYILAN_MI],
                   COALESCE([MASKELI_PAN], N'**** **** **** ' + [SON_DORT_HANE]) AS maskeli_pan
            FROM [dbo].[KULLANICI_ODEME_YONTEMLERI]
            WHERE [KULLANICI_ID] = @userId AND [AKTIF_MI] = 1 AND [SIFRELI_KART_VERISI] IS NOT NULL
            ORDER BY [VARSAYILAN_MI] DESC, id DESC;", connection);
        command.Parameters.AddWithValue("@userId", userId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new SavedPaymentCardOptionViewModel
            {
                PaymentMethodId = reader.GetInt64(0),
                Label = $"{reader.GetString(1)} · {reader.GetString(2)}",
                MaskedPan = reader.GetString(6),
                ExpiryText = $"{SafeInt(reader, 3):00}/{SafeInt(reader, 4)}",
                IsDefault = SafeBool(reader, 5)
            });
        }

        return items;
    }

    public async Task<bool> UserOwnsActiveCardAsync(long userId, long paymentMethodId, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = new SqlCommand(@"
            SELECT TOP (1) 1
            FROM [dbo].[KULLANICI_ODEME_YONTEMLERI]
            WHERE id = @paymentMethodId AND [KULLANICI_ID] = @userId AND [AKTIF_MI] = 1 AND [SIFRELI_KART_VERISI] IS NOT NULL;", connection);
        command.Parameters.AddWithValue("@paymentMethodId", paymentMethodId);
        command.Parameters.AddWithValue("@userId", userId);
        var scalar = await command.ExecuteScalarAsync(cancellationToken);
        return scalar is not null && scalar != DBNull.Value;
    }

    public async Task CreateReservationSnapshotAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        long reservationId,
        long userId,
        long savedPaymentCardId,
        CancellationToken cancellationToken = default)
    {
        await using var loadCommand = new SqlCommand(@"
            SELECT TOP (1) [KART_TOKEN], [SIFRELI_KART_VERISI], COALESCE([MASKELI_PAN], N'**** **** **** ' + [SON_DORT_HANE])
            FROM [dbo].[KULLANICI_ODEME_YONTEMLERI]
            WHERE id = @cardId AND [KULLANICI_ID] = @userId AND [AKTIF_MI] = 1 AND [SIFRELI_KART_VERISI] IS NOT NULL;", connection, transaction);
        loadCommand.Parameters.AddWithValue("@cardId", savedPaymentCardId);
        loadCommand.Parameters.AddWithValue("@userId", userId);
        await using var reader = await loadCommand.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            throw new InvalidOperationException("Kayitli kart bulunamadi.");
        }

        var token = reader.GetString(0);
        var encrypted = (byte[])reader.GetValue(1);
        var maskedPan = reader.GetString(2);
        await reader.CloseAsync();

        await using var insertCommand = new SqlCommand(@"
            INSERT INTO [dbo].[REZERVASYON_ODEME_KART_SNAPSHOT]
            ([REZERVASYON_ID], [KULLANICI_ID], [KAYITLI_KART_ID], [KART_TOKEN], [SIFRELI_KART_VERISI], [MASKELI_PAN])
            VALUES
            (@reservationId, @userId, @cardId, @token, @encrypted, @maskedPan);", connection, transaction);
        insertCommand.Parameters.AddWithValue("@reservationId", reservationId);
        insertCommand.Parameters.AddWithValue("@userId", userId);
        insertCommand.Parameters.AddWithValue("@cardId", savedPaymentCardId);
        insertCommand.Parameters.AddWithValue("@token", token);
        insertCommand.Parameters.Add("@encrypted", System.Data.SqlDbType.VarBinary, -1).Value = encrypted;
        insertCommand.Parameters.AddWithValue("@maskedPan", maskedPan);
        await insertCommand.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<PartnerPaymentCardViewResult> TryPartnerViewCardAsync(
        long partnerUserId,
        long hotelId,
        long reservationId,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        if (!await PartnerCanAccessReservationAsync(connection, partnerUserId, hotelId, reservationId, cancellationToken))
        {
            return new PartnerPaymentCardViewResult
            {
                Success = false,
                Message = "Bu rezervasyon icin kart goruntuleme yetkiniz yok."
            };
        }

        byte[]? encrypted = null;
        string? maskedPan = null;
        await using (var snapshotCommand = new SqlCommand(@"
            SELECT TOP (1) [SIFRELI_KART_VERISI], [MASKELI_PAN]
            FROM [dbo].[REZERVASYON_ODEME_KART_SNAPSHOT]
            WHERE [REZERVASYON_ID] = @reservationId;", connection))
        {
            snapshotCommand.Parameters.AddWithValue("@reservationId", reservationId);
            await using var reader = await snapshotCommand.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                return new PartnerPaymentCardViewResult
                {
                    Success = false,
                    Message = "Bu rezervasyon icin kayitli kart bulunmuyor."
                };
            }

            encrypted = (byte[])reader.GetValue(0);
            maskedPan = reader.GetString(1);
        }

        var viewCount = await CountPartnerViewsAsync(connection, reservationId, cancellationToken);
        var nextAttempt = viewCount + 1;
        if (viewCount >= PublicSavedPaymentMethods.MaxPartnerCardViews)
        {
            await InsertPartnerViewLogAsync(connection, reservationId, hotelId, partnerUserId, nextAttempt, false, ipAddress, userAgent, cancellationToken);
            return new PartnerPaymentCardViewResult
            {
                Success = false,
                LimitExceeded = true,
                ViewCount = viewCount,
                RemainingViews = 0,
                Message = "Kart bilgileri guvenlik nedeniyle gizlendi. Maksimum 3 goruntuleme limitine ulasildi.",
                MaskedPan = maskedPan
            };
        }

        PaymentCardPayload payload;
        try
        {
            payload = _crypto.Decrypt(encrypted!);
        }
        catch
        {
            await InsertPartnerViewLogAsync(connection, reservationId, hotelId, partnerUserId, nextAttempt, false, ipAddress, userAgent, cancellationToken);
            return new PartnerPaymentCardViewResult
            {
                Success = false,
                ViewCount = viewCount,
                RemainingViews = Math.Max(0, PublicSavedPaymentMethods.MaxPartnerCardViews - viewCount),
                Message = "Kart bilgileri cozulemedi."
            };
        }

        await InsertPartnerViewLogAsync(connection, reservationId, hotelId, partnerUserId, nextAttempt, true, ipAddress, userAgent, cancellationToken);
        return new PartnerPaymentCardViewResult
        {
            Success = true,
            ViewCount = nextAttempt,
            RemainingViews = Math.Max(0, PublicSavedPaymentMethods.MaxPartnerCardViews - nextAttempt),
            Message = nextAttempt >= PublicSavedPaymentMethods.MaxPartnerCardViews
                ? "Son goruntuleme hakkiniz kullanildi."
                : "Kart bilgileri goruntulendi.",
            CardHolder = payload.HolderName,
            CardNumber = payload.Pan,
            ExpiryText = $"{payload.ExpiryMonth:00}/{payload.ExpiryYear}",
            Brand = payload.Brand,
            MaskedPan = maskedPan
        };
    }

    public async Task<bool> ReservationHasSavedCardAsync(long reservationId, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = new SqlCommand(@"
            SELECT TOP (1) 1
            FROM [dbo].[REZERVASYON_ODEME_KART_SNAPSHOT]
            WHERE [REZERVASYON_ID] = @reservationId;", connection);
        command.Parameters.AddWithValue("@reservationId", reservationId);
        var scalar = await command.ExecuteScalarAsync(cancellationToken);
        return scalar is not null && scalar != DBNull.Value;
    }

    private static async Task<bool> PartnerCanAccessReservationAsync(
        SqlConnection connection,
        long partnerUserId,
        long hotelId,
        long reservationId,
        CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand(@"
            SELECT TOP (1) 1
            FROM [dbo].[REZERVASYONLAR] r
            WHERE r.id = @reservationId
              AND r.[OTEL_ID] = @hotelId
              AND EXISTS (
                    SELECT 1
                    FROM [dbo].[OTEL_KULLANICI_SAHIPLIKLERI] oks
                    WHERE oks.[OTEL_ID] = r.[OTEL_ID]
                      AND oks.[KULLANICI_ID] = @partnerUserId
                      AND COALESCE(oks.[AKTIF_MI], 1) = 1
              );", connection);
        command.Parameters.AddWithValue("@reservationId", reservationId);
        command.Parameters.AddWithValue("@hotelId", hotelId);
        command.Parameters.AddWithValue("@partnerUserId", partnerUserId);
        var scalar = await command.ExecuteScalarAsync(cancellationToken);
        return scalar is not null && scalar != DBNull.Value;
    }

    private static async Task<int> CountPartnerViewsAsync(SqlConnection connection, long reservationId, CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand(@"
            SELECT COUNT(*)
            FROM [dbo].[PARTNER_ODEME_KART_GORUNTULEME_LOG]
            WHERE [REZERVASYON_ID] = @reservationId AND [BASARILI_MI] = 1;", connection);
        command.Parameters.AddWithValue("@reservationId", reservationId);
        var scalar = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(scalar ?? 0, CultureInfo.InvariantCulture);
    }

    private static async Task InsertPartnerViewLogAsync(
        SqlConnection connection,
        long reservationId,
        long hotelId,
        long partnerUserId,
        int attemptNo,
        bool success,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand(@"
            INSERT INTO [dbo].[PARTNER_ODEME_KART_GORUNTULEME_LOG]
            ([REZERVASYON_ID], [OTEL_ID], [PARTNER_KULLANICI_ID], [DENEME_SIRASI], [BASARILI_MI], [IP_ADRESI], [KULLANICI_ARAC])
            VALUES
            (@reservationId, @hotelId, @partnerUserId, @attemptNo, @success, @ipAddress, @userAgent);", connection);
        command.Parameters.AddWithValue("@reservationId", reservationId);
        command.Parameters.AddWithValue("@hotelId", hotelId);
        command.Parameters.AddWithValue("@partnerUserId", partnerUserId);
        command.Parameters.AddWithValue("@attemptNo", attemptNo);
        command.Parameters.AddWithValue("@success", success ? 1 : 0);
        command.Parameters.AddWithValue("@ipAddress", string.IsNullOrWhiteSpace(ipAddress) ? DBNull.Value : ipAddress.Trim());
        command.Parameters.AddWithValue("@userAgent", string.IsNullOrWhiteSpace(userAgent) ? DBNull.Value : userAgent.Trim()[..Math.Min(500, userAgent.Trim().Length)]);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task<SqlConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    private static int SafeInt(SqlDataReader reader, int ordinal)
        => reader.IsDBNull(ordinal) ? 0 : Convert.ToInt32(reader.GetValue(ordinal), CultureInfo.InvariantCulture);

    private static bool SafeBool(SqlDataReader reader, int ordinal)
        => !reader.IsDBNull(ordinal) && Convert.ToBoolean(reader.GetValue(ordinal), CultureInfo.InvariantCulture);
}
