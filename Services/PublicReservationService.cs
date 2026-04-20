using System.Globalization;
using Microsoft.Data.SqlClient;
using SqlConnection = Microsoft.Data.SqlClient.SqlConnection;
using SqlCommand = Microsoft.Data.SqlClient.SqlCommand;
using SqlTransaction = Microsoft.Data.SqlClient.SqlTransaction;
using SqlException = Microsoft.Data.SqlClient.SqlException;
using otelturizmnew.Models.Email;
using otelturizmnew.Models.Reservations;
using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Services;

public class PublicReservationService : IPublicReservationService
{
    private readonly string _connectionString;
    private readonly IHotelPricingReadService _hotelPricingReadService;
    private readonly IReservationDraftService _reservationDraftService;
    private readonly IEmailQueueService _emailQueueService;
    private readonly ILogger<PublicReservationService> _logger;

    public PublicReservationService(
        IConfiguration configuration,
        IHotelPricingReadService hotelPricingReadService,
        IReservationDraftService reservationDraftService,
        IEmailQueueService emailQueueService,
        ILogger<PublicReservationService> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection tanimli degil.");
        _hotelPricingReadService = hotelPricingReadService;
        _reservationDraftService = reservationDraftService;
        _emailQueueService = emailQueueService;
        _logger = logger;
    }

    public Task<ReservationDraftSummaryViewModel?> GetActiveDraftAsync(long? userId, string? sessionKey, CancellationToken cancellationToken = default)
        => _reservationDraftService.GetActiveDraftAsync(userId, sessionKey, cancellationToken);

    public async Task<PublicReservationPriceQuoteViewModel> GetPriceQuoteAsync(long roomTypeId, DateOnly checkInDate, DateOnly checkOutDate, int roomCount, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        var pricing = await BuildPriceSummaryAsync(connection, roomTypeId, checkInDate, checkOutDate, roomCount, cancellationToken);
        return new PublicReservationPriceQuoteViewModel
        {
            NightCount = Math.Max(1, checkOutDate.DayNumber - checkInDate.DayNumber),
            NightlyPrice = pricing.NightlyPrice,
            RoomTotal = pricing.RoomTotal,
            NetRoomAmount = pricing.NetRoomAmount,
            VatRate = pricing.VatRate,
            VatAmount = pricing.VatAmount,
            AccommodationTaxRate = pricing.AccommodationTaxRate,
            AccommodationTaxAmount = pricing.AccommodationTaxAmount,
            TaxAmount = pricing.TaxAmount,
            TotalAmount = pricing.TotalAmount,
            IsAvailable = pricing.IsAvailable,
            AvailabilityMessage = pricing.AvailabilityMessage,
            NightlyBreakdown = pricing.NightlyBreakdown
                .Select(static item => new PublicReservationNightlyBreakdownItemViewModel
                {
                    Date = item.Date,
                    DateText = item.Date.ToString("dd MMM yyyy", CultureInfo.GetCultureInfo("tr-TR")),
                    Price = item.EffectivePrice,
                    BasePrice = item.BasePrice,
                    DiscountPrice = item.DiscountPrice,
                    PriceText = item.EffectivePrice.ToString("N0", CultureInfo.GetCultureInfo("tr-TR")),
                    IsDiscounted = item.DiscountPrice.HasValue && item.DiscountPrice.Value > 0m && item.DiscountPrice.Value < item.BasePrice,
                    IsClosed = item.IsClosed,
                    RemainingRooms = item.RemainingRooms
                })
                .ToList()
        };
    }

    public async Task<PublicReservationResult> StartReservationAsync(long? userId, string? sessionKey, PublicHotelReservationForm form, CancellationToken cancellationToken = default)
    {
        if (form.HotelId <= 0 || form.RoomTypeId <= 0)
        {
            return new PublicReservationResult { Message = "Otel ve oda tipi secilmeden rezervasyon baslatilamaz." };
        }

        if (form.CheckOutDate <= form.CheckInDate)
        {
            return new PublicReservationResult { Message = "Cikis tarihi giris tarihinden sonra olmalidir." };
        }

        if (IsOnlinePaymentPendingFeature(form.PaymentMethod))
        {
            return new PublicReservationResult
            {
                Message = "Online ödeme altyapısı şu anda geliştirme aşamasında. Şimdilik kapıda ödeme / nakit ile devam edebilirsiniz."
            };
        }

        var paymentMethod = NormalizePaymentMethod(form.PaymentMethod);

        await using var connection = await OpenConnectionAsync(cancellationToken);
        var hotel = await LoadHotelAsync(connection, form.HotelId, form.RoomTypeId, cancellationToken);
        var occupancyValidationMessage = ValidateRoomOccupancy(form, hotel);
        if (!string.IsNullOrWhiteSpace(occupancyValidationMessage))
        {
            return new PublicReservationResult
            {
                Message = occupancyValidationMessage,
                RedirectUrl = $"/oteller/{hotel.Slug}"
            };
        }

        var pricing = await BuildPriceSummaryAsync(connection, form.RoomTypeId, form.CheckInDate, form.CheckOutDate, form.RoomCount, cancellationToken);
        if (!pricing.IsAvailable)
        {
            return new PublicReservationResult
            {
                Message = string.IsNullOrWhiteSpace(pricing.AvailabilityMessage)
                    ? "Secilen tarih araliginda uygun oda veya fiyat bulunamadi."
                    : pricing.AvailabilityMessage,
                RedirectUrl = $"/oteller/{hotel.Slug}"
            };
        }

        var draftRequest = new ReservationDraftUpsertRequest
        {
            UserId = userId,
            SessionKey = sessionKey,
            Source = "Public",
            HotelId = form.HotelId,
            RoomTypeId = form.RoomTypeId,
            CheckInDate = form.CheckInDate,
            CheckOutDate = form.CheckOutDate,
            AdultCount = form.AdultCount,
            ChildCount = form.ChildCount,
            RoomCount = form.RoomCount,
            NightlyPrice = pricing.NightlyPrice,
            NetRoomAmount = pricing.NetRoomAmount,
            VatRate = pricing.VatRate,
            VatAmount = pricing.VatAmount,
            AccommodationTaxRate = pricing.AccommodationTaxRate,
            AccommodationTaxAmount = pricing.AccommodationTaxAmount,
            TaxAmount = pricing.TaxAmount,
            TotalAmount = pricing.TotalAmount,
            ReturnUrl = $"/oteller/{hotel.Slug}?continueDraft=1",
            ProfileCompletionUrl = $"/oteller/{hotel.Slug}?continueDraft=1&openProfile=1",
            Notes = "Public otel detay sayfasindan baslatildi."
        };

        if (userId.GetValueOrDefault() <= 0)
        {
            var guestDraft = CloneDraftRequest(draftRequest);
            guestDraft.Status = "Giris Bekliyor";
            await _reservationDraftService.SaveOrUpdateAsync(guestDraft, cancellationToken);
            return new PublicReservationResult
            {
                Message = "Rezervasyonunuz kaydedildi. Devam etmek icin once giris yapiniz.",
                RedirectUrl = $"/kullanici-giris?ReturnUrl={Uri.EscapeDataString($"/oteller/{hotel.Slug}")}"
            };
        }

        var authenticatedUserId = userId.GetValueOrDefault();
        var userProfile = await LoadUserProfileAsync(connection, authenticatedUserId, cancellationToken);
        if (!userProfile.IsProfileComplete)
        {
            var profileDraft = CloneDraftRequest(draftRequest);
            profileDraft.Status = "Profil Eksik";
            profileDraft.GuestFullName = userProfile.FullName;
            profileDraft.GuestEmail = userProfile.Email;
            profileDraft.GuestPhone = userProfile.Phone;
            profileDraft.GuestCity = userProfile.City;
            profileDraft.GuestDistrict = userProfile.District;
            profileDraft.GuestNeighborhood = userProfile.Neighborhood;
            profileDraft.GuestAddress = userProfile.Address;
            await _reservationDraftService.SaveOrUpdateAsync(profileDraft, cancellationToken);

            return new PublicReservationResult
            {
                Message = "Rezervasyon taslak olarak kaydedildi. Lutfen profil bilgilerinizi tamamlayin.",
                RedirectUrl = $"/oteller/{hotel.Slug}?continueDraft=1&openProfile=1"
            };
        }

        var readyDraft = CloneDraftRequest(draftRequest);
        readyDraft.Status = "Taslak";
        readyDraft.GuestFullName = userProfile.FullName;
        readyDraft.GuestEmail = userProfile.Email;
        readyDraft.GuestPhone = userProfile.Phone;
        readyDraft.GuestCity = userProfile.City;
        readyDraft.GuestDistrict = userProfile.District;
        readyDraft.GuestNeighborhood = userProfile.Neighborhood;
        readyDraft.GuestAddress = userProfile.Address;
        var draftId = await _reservationDraftService.SaveOrUpdateAsync(readyDraft, cancellationToken);

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            var reservationNo = await GenerateReservationNoAsync(connection, (SqlTransaction)transaction, cancellationToken);
            const string insertSql = @"
                INSERT INTO rezervasyonlar
                (
                    rezervasyon_no, otel_id, oda_tip_id, kullanici_id,
                    misafir_ad_soyad, misafir_eposta, misafir_telefon, misafir_notu,
                    misafir_sehir, misafir_ilce, misafir_mahalle, misafir_adres,
                    giris_tarihi, cikis_tarihi, yetiskin_sayisi, cocuk_sayisi, oda_sayisi,
                    gecelik_fiyat, net_oda_tutari, toplam_oda_tutari, vergi_tutari, toplam_vergi_tutari, kdv_orani, kdv_tutari,
                    konaklama_vergisi_orani, konaklama_vergisi_tutari, toplam_tutar, vergiler_dahil_toplam_tutar,
                    komisyon_vergi_kural_id, komisyon_orani, komisyon_tutari, komisyon_gelir_vergisi_orani, komisyon_gelir_vergisi_tutari,
                    platform_net_komisyon_tutari, otele_odenecek_tutar,
                    durum, odeme_durumu, odeme_yontemi, kapida_odeme_tutari, kapida_odeme_durumu, online_odeme_tutari, online_odeme_durumu,
                    tahsil_edilen_tutar, kalan_tahsil_edilecek_tutar, on_odeme_tutari, kalan_odeme_tutari,
                    otel_onay_durumu, firma_onay_durumu,
                    kaynak, rezervasyon_kanali, ozel_istekler, rezervasyon_taslagi_id
                )
                VALUES
                (
                    @reservationNo, @hotelId, @roomTypeId, @userId,
                    @fullName, @email, @phone, @note,
                    @city, @district, @neighborhood, @address,
                    @checkIn, @checkOut, @adultCount, @childCount, @roomCount,
                    @nightlyPrice, @netRoomAmount, @roomTotal, @taxAmount, @taxAmount, @vatRate, @vatAmount,
                    @accommodationTaxRate, @accommodationTaxAmount, @totalAmount, @totalAmount,
                    @commissionRuleId, @commissionRate, @commissionAmount, @commissionIncomeTaxRate, @commissionIncomeTaxAmount,
                    @platformNetCommissionAmount, @hotelPayoutAmount,
                    'Onay Bekliyor', 'Beklemede', @paymentMethod, @cashAtHotelAmount, @cashAtHotelStatus, @onlinePaymentAmount, @onlinePaymentStatus,
                    0, @remainingCollectionAmount, 0, @remainingCollectionAmount,
                    'Beklemede', 'Onay Gerekmiyor',
                    'Web', 'Web', @note, @draftId
                );
                SELECT CAST(SCOPE_IDENTITY() AS bigint);";

            long reservationId;
            await using (var insertCommand = new SqlCommand(insertSql, connection, (SqlTransaction)transaction))
            {
                insertCommand.Parameters.AddWithValue("@reservationNo", reservationNo);
                insertCommand.Parameters.AddWithValue("@hotelId", form.HotelId);
                insertCommand.Parameters.AddWithValue("@roomTypeId", form.RoomTypeId);
                insertCommand.Parameters.AddWithValue("@userId", authenticatedUserId);
                insertCommand.Parameters.AddWithValue("@fullName", userProfile.FullName);
                insertCommand.Parameters.AddWithValue("@email", userProfile.Email);
                insertCommand.Parameters.AddWithValue("@phone", userProfile.Phone);
                insertCommand.Parameters.AddWithValue("@note", "Public rezervasyon talebi");
                insertCommand.Parameters.AddWithValue("@city", userProfile.City);
                insertCommand.Parameters.AddWithValue("@district", userProfile.District);
                insertCommand.Parameters.AddWithValue("@neighborhood", userProfile.Neighborhood);
                insertCommand.Parameters.AddWithValue("@address", userProfile.Address);
                insertCommand.Parameters.AddWithValue("@checkIn", form.CheckInDate.ToDateTime(TimeOnly.MinValue));
                insertCommand.Parameters.AddWithValue("@checkOut", form.CheckOutDate.ToDateTime(TimeOnly.MinValue));
                insertCommand.Parameters.AddWithValue("@adultCount", form.AdultCount);
                insertCommand.Parameters.AddWithValue("@childCount", form.ChildCount);
                insertCommand.Parameters.AddWithValue("@roomCount", form.RoomCount);
                insertCommand.Parameters.AddWithValue("@nightlyPrice", pricing.NightlyPrice);
                insertCommand.Parameters.AddWithValue("@netRoomAmount", pricing.NetRoomAmount);
                insertCommand.Parameters.AddWithValue("@roomTotal", pricing.RoomTotal);
                insertCommand.Parameters.AddWithValue("@taxAmount", pricing.TaxAmount);
                insertCommand.Parameters.AddWithValue("@vatRate", pricing.VatRate);
                insertCommand.Parameters.AddWithValue("@vatAmount", pricing.VatAmount);
                insertCommand.Parameters.AddWithValue("@accommodationTaxRate", pricing.AccommodationTaxRate);
                insertCommand.Parameters.AddWithValue("@accommodationTaxAmount", pricing.AccommodationTaxAmount);
                insertCommand.Parameters.AddWithValue("@totalAmount", pricing.TotalAmount);
                insertCommand.Parameters.AddWithValue("@commissionRuleId", pricing.CommissionRuleId.HasValue ? pricing.CommissionRuleId.Value : DBNull.Value);
                insertCommand.Parameters.AddWithValue("@commissionRate", pricing.CommissionRate);
                insertCommand.Parameters.AddWithValue("@commissionAmount", pricing.CommissionAmount);
                insertCommand.Parameters.AddWithValue("@commissionIncomeTaxRate", pricing.CommissionIncomeTaxRate);
                insertCommand.Parameters.AddWithValue("@commissionIncomeTaxAmount", pricing.CommissionIncomeTaxAmount);
                insertCommand.Parameters.AddWithValue("@platformNetCommissionAmount", pricing.PlatformNetCommissionAmount);
                insertCommand.Parameters.AddWithValue("@hotelPayoutAmount", pricing.HotelPayoutAmount);
                insertCommand.Parameters.AddWithValue("@paymentMethod", paymentMethod);
                insertCommand.Parameters.AddWithValue("@cashAtHotelAmount", paymentMethod == "Kapıda Ödeme" ? pricing.TotalAmount : 0m);
                insertCommand.Parameters.AddWithValue("@cashAtHotelStatus", paymentMethod == "Kapıda Ödeme" ? "Odenmedi" : "Uygulanmiyor");
                insertCommand.Parameters.AddWithValue("@onlinePaymentAmount", paymentMethod == "Sanal POS" ? pricing.TotalAmount : 0m);
                insertCommand.Parameters.AddWithValue("@onlinePaymentStatus", paymentMethod == "Sanal POS" ? "Beklemede" : "Uygulanmiyor");
                insertCommand.Parameters.AddWithValue("@remainingCollectionAmount", pricing.TotalAmount);
                insertCommand.Parameters.AddWithValue("@draftId", draftId);
                var reservationIdRaw = await insertCommand.ExecuteScalarAsync(cancellationToken);
                reservationId = Convert.ToInt64(reservationIdRaw ?? 0L, CultureInfo.InvariantCulture);
            }

            await _emailQueueService.QueueTemplateAsync(connection, (SqlTransaction)transaction, new QueuedEmailTemplateRequest
            {
                UserId = authenticatedUserId,
                RecipientEmail = userProfile.Email,
                TemplateCode = "reservation_received_customer",
                RelatedTable = "rezervasyonlar",
                RelatedRecordId = reservationId,
                Tokens = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["user_first_name"] = userProfile.FirstName,
                    ["booking_reference"] = reservationNo,
                    ["hotel_name"] = hotel.Name,
                    ["check_in_date"] = form.CheckInDate.ToString("dd MMM yyyy", CultureInfo.GetCultureInfo("tr-TR")),
                    ["check_out_date"] = form.CheckOutDate.ToString("dd MMM yyyy", CultureInfo.GetCultureInfo("tr-TR")),
                    ["total_price"] = pricing.TotalAmount.ToString("N2", CultureInfo.GetCultureInfo("tr-TR")),
                    ["room_type_name"] = hotel.RoomName,
                    ["booking_details_link"] = "/panel/user/rezervasyonlarim",
                    ["hotel_address"] = hotel.Address
                }
            }, cancellationToken);

            var partnerRecipient = await ResolvePartnerRecipientAsync(connection, (SqlTransaction)transaction, form.HotelId, cancellationToken);
            await _emailQueueService.QueueTemplateAsync(connection, (SqlTransaction)transaction, new QueuedEmailTemplateRequest
            {
                UserId = partnerRecipient.UserId,
                RecipientEmail = partnerRecipient.Email,
                TemplateCode = "reservation_new_partner",
                RelatedTable = "rezervasyonlar",
                RelatedRecordId = reservationId,
                Tokens = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["hotel_manager_name"] = partnerRecipient.ManagerName,
                    ["hotel_name"] = hotel.Name,
                    ["booking_reference"] = reservationNo,
                    ["guest_full_name"] = userProfile.FullName,
                    ["guest_email"] = userProfile.Email,
                    ["guest_phone"] = userProfile.Phone,
                    ["total_price"] = pricing.TotalAmount.ToString("N2", CultureInfo.GetCultureInfo("tr-TR")),
                    ["check_in_date"] = form.CheckInDate.ToString("dd MMM yyyy", CultureInfo.GetCultureInfo("tr-TR")),
                    ["check_out_date"] = form.CheckOutDate.ToString("dd MMM yyyy", CultureInfo.GetCultureInfo("tr-TR")),
                    ["room_type_name"] = hotel.RoomName,
                    ["room_count"] = form.RoomCount.ToString(CultureInfo.InvariantCulture)
                }
            }, cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            try
            {
                await _reservationDraftService.MarkCompletedAsync(draftId, reservationId, cancellationToken);
            }
            catch (Exception draftCleanupException)
            {
                _logger.LogWarning(draftCleanupException, "Rezervasyon tamamlandi ancak taslak temizlenemedi. DraftId: {DraftId}, ReservationId: {ReservationId}", draftId, reservationId);
            }

            return new PublicReservationResult
            {
                Success = true,
                Message = $"Rezervasyonunuz alindi: {reservationNo}",
                ReservationId = reservationId,
                RedirectUrl = "/panel/user/rezervasyonlarim"
            };
        }
        catch (Exception ex)
        {
            try
            {
                await transaction.RollbackAsync(cancellationToken);
            }
            catch (Exception rollbackException)
            {
                _logger.LogWarning(rollbackException, "Public rezervasyon rollback asamasinda ikinci bir hata olustu. DraftId: {DraftId}", draftId);
            }

            _logger.LogError(ex, "Public rezervasyon finalize edilemedi. DraftId: {DraftId}, HotelId: {HotelId}, RoomTypeId: {RoomTypeId}, UserId: {UserId}", draftId, form.HotelId, form.RoomTypeId, authenticatedUserId);
            return new PublicReservationResult
            {
                Message = $"Rezervasyon olusturulurken hata olustu: {ex.Message}",
                RedirectUrl = $"/oteller/{hotel.Slug}"
            };
        }
    }

    private async Task<UserProfileSnapshot> LoadUserProfileAsync(SqlConnection connection, long userId, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT TOP (1) ad_soyad, eposta, COALESCE(telefon, ''), COALESCE(sehir, ''), COALESCE(ilce, ''), COALESCE(mahalle, ''), COALESCE(adres, ''), dogum_tarihi, COALESCE(cinsiyet, '')
            FROM users
            WHERE id = @userId;";
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@userId", userId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            throw new InvalidOperationException("Kullanici profili bulunamadi.");
        }

        var fullName = reader.GetString(0);
        return new UserProfileSnapshot
        {
            FullName = fullName,
            FirstName = SplitFirstName(fullName),
            Email = reader.GetString(1),
            Phone = reader.GetString(2),
            City = reader.GetString(3),
            District = reader.GetString(4),
            Neighborhood = reader.GetString(5),
            Address = reader.GetString(6),
            BirthDate = reader.IsDBNull(7) ? null : reader.GetDateTime(7),
            Gender = reader.GetString(8)
        };
    }

    private async Task<HotelSnapshot> LoadHotelAsync(SqlConnection connection, long hotelId, long roomTypeId, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT TOP (1)
                o.otel_adi,
                o.otel_kodu,
                o.tam_adres,
                COALESCE(o.varsayilan_komisyon_orani, 0),
                ot.oda_adi,
                COALESCE(ot.maksimum_kisi_sayisi, 1),
                COALESCE(ot.maksimum_yetiskin_sayisi, 1),
                COALESCE(ot.maksimum_cocuk_sayisi, 0)
            FROM oteller o
            INNER JOIN oda_tipleri ot ON ot.id = @roomTypeId AND ot.otel_id = o.id
            WHERE o.id = @hotelId;";
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@hotelId", hotelId);
        command.Parameters.AddWithValue("@roomTypeId", roomTypeId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            throw new InvalidOperationException("Otel veya oda tipi bulunamadi.");
        }

        var name = reader.GetString(0);
        var code = reader.GetString(1);
        return new HotelSnapshot
        {
            Name = name,
            HotelCode = code,
            Address = reader.GetString(2),
            CommissionRate = reader.GetDecimal(3),
            RoomName = reader.GetString(4),
            MaxGuestCount = Convert.ToInt32(reader.GetValue(5), CultureInfo.InvariantCulture),
            MaxAdultCount = Convert.ToInt32(reader.GetValue(6), CultureInfo.InvariantCulture),
            MaxChildCount = Convert.ToInt32(reader.GetValue(7), CultureInfo.InvariantCulture),
            Slug = BuildSlug(name, code)
        };
    }

    private static string? ValidateRoomOccupancy(PublicHotelReservationForm form, HotelSnapshot hotel)
    {
        var roomCount = Math.Max(1, form.RoomCount);
        var adultCount = Math.Max(1, form.AdultCount);
        var childCount = Math.Max(0, form.ChildCount);
        var totalGuestCount = adultCount + childCount;

        var maxGuestPerRoom = Math.Max(1, hotel.MaxGuestCount);
        var maxAdultPerRoom = Math.Max(1, hotel.MaxAdultCount);
        var maxChildPerRoom = Math.Max(0, hotel.MaxChildCount);

        var maxGuestTotal = maxGuestPerRoom * roomCount;
        var maxAdultTotal = maxAdultPerRoom * roomCount;
        var maxChildTotal = maxChildPerRoom * roomCount;

        if (adultCount > maxAdultTotal)
        {
            var requiredRoomCount = (int)Math.Ceiling(adultCount / (double)maxAdultPerRoom);
            return $"{hotel.RoomName} odasında en fazla {maxAdultPerRoom} yetişkin konaklayabilir. {adultCount} yetişkin için en az {requiredRoomCount} oda kiralamanız gerekmektedir.";
        }

        if (childCount > maxChildTotal)
        {
            var requiredRoomCount = maxChildPerRoom <= 0
                ? roomCount + 1
                : (int)Math.Ceiling(childCount / (double)maxChildPerRoom);
            return $"{hotel.RoomName} odasında en fazla {maxChildPerRoom} çocuk konaklayabilir. {childCount} çocuk için en az {requiredRoomCount} oda kiralamanız gerekmektedir.";
        }

        if (totalGuestCount > maxGuestTotal)
        {
            var requiredRoomCount = (int)Math.Ceiling(totalGuestCount / (double)maxGuestPerRoom);
            return $"{hotel.RoomName} odası en fazla {maxGuestPerRoom} kişiliktir. Toplam {totalGuestCount} misafir için en az {requiredRoomCount} oda kiralamanız gerekmektedir.";
        }

        return null;
    }

    private async Task<(long UserId, string Email, string ManagerName)> ResolvePartnerRecipientAsync(SqlConnection connection, SqlTransaction transaction, long hotelId, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT COALESCE(o.user_id, oks.user_id, 1),
                   COALESCE(u.eposta, o.satis_kontak_eposta, o.eposta, 'partner@otelturizm.com'),
                   COALESCE(u.ad_soyad, o.satis_kontak_adi, 'Partner Yetkilisi')
            FROM oteller o
            LEFT JOIN otel_kullanici_sahiplikleri oks ON oks.otel_id = o.id AND oks.aktif_mi = 1
            LEFT JOIN users u ON u.id = COALESCE(o.user_id, oks.user_id)
            WHERE o.id = @hotelId
            ORDER BY oks.ana_sorumlu_mu DESC, oks.id ASC
            OFFSET 0 ROWS FETCH NEXT 1 ROWS ONLY;";
        await using var command = new SqlCommand(sql, connection, (SqlTransaction)transaction);
        command.Parameters.AddWithValue("@hotelId", hotelId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return (reader.IsDBNull(0) ? 1L : reader.GetInt64(0), reader.GetString(1), reader.GetString(2));
        }

        return (1L, "partner@otelturizm.com", "Partner Yetkilisi");
    }

    private async Task<CommissionTaxRuleSnapshot> LoadActiveCommissionRuleAsync(SqlConnection connection, long roomTypeId, DateOnly effectiveDate, CancellationToken cancellationToken)
    {
        const string tableCheckSql = """
            SELECT COUNT(*)
            FROM INFORMATION_SCHEMA.TABLES
            WHERE TABLE_CATALOG = DB_NAME()
              AND TABLE_NAME = 'komisyon_vergiler';
            """;

        await using (var tableCheckCommand = new SqlCommand(tableCheckSql, connection))
        {
            var exists = Convert.ToInt32(await tableCheckCommand.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture) > 0;
            if (!exists)
            {
                return await LoadFallbackCommissionRuleAsync(connection, roomTypeId, cancellationToken);
            }
        }

        const string sql = @"
            SELECT TOP (1)
                o.id,
                kv.id,
                COALESCE(kv.komisyon_orani, o.varsayilan_komisyon_orani, 0),
                COALESCE(kv.komisyon_gelir_vergisi_orani, 20),
                COALESCE(kv.kdv_orani, 10),
                COALESCE(kv.konaklama_vergisi_orani, 2),
                COALESCE(kv.para_birimi, N'TRY')
            FROM oda_tipleri ot
            INNER JOIN oteller o ON o.id = ot.otel_id
            OUTER APPLY
            (
                SELECT TOP (1) *
                FROM komisyon_vergiler kv
                WHERE kv.otel_id = o.id
                  AND kv.aktif_mi = 1
                  AND kv.baslangic_tarihi <= @effectiveDate
                  AND (kv.bitis_tarihi IS NULL OR kv.bitis_tarihi >= @effectiveDate)
                ORDER BY kv.baslangic_tarihi DESC, kv.id DESC
            ) kv
            WHERE ot.id = @roomTypeId;";

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@roomTypeId", roomTypeId);
        command.Parameters.AddWithValue("@effectiveDate", effectiveDate.ToDateTime(TimeOnly.MinValue));
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return new CommissionTaxRuleSnapshot
            {
                HotelId = reader.GetInt64(0),
                RuleId = reader.IsDBNull(1) ? null : reader.GetInt64(1),
                CommissionRate = SafeGetDecimal(reader, 2),
                CommissionIncomeTaxRate = SafeGetDecimal(reader, 3),
                VatRate = SafeGetDecimal(reader, 4),
                AccommodationTaxRate = SafeGetDecimal(reader, 5),
                Currency = reader.IsDBNull(6) ? "TRY" : reader.GetString(6)
            };
        }

        return await LoadFallbackCommissionRuleAsync(connection, roomTypeId, cancellationToken);
    }

    private static async Task<CommissionTaxRuleSnapshot> LoadFallbackCommissionRuleAsync(SqlConnection connection, long roomTypeId, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT TOP (1)
                o.id,
                COALESCE(o.varsayilan_komisyon_orani, 0)
            FROM oda_tipleri ot
            INNER JOIN oteller o ON o.id = ot.otel_id
            WHERE ot.id = @roomTypeId;";

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@roomTypeId", roomTypeId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return new CommissionTaxRuleSnapshot
            {
                HotelId = reader.GetInt64(0),
                CommissionRate = SafeGetDecimal(reader, 1),
                CommissionIncomeTaxRate = 20m,
                VatRate = 10m,
                AccommodationTaxRate = 2m,
                Currency = "TRY"
            };
        }

        return new CommissionTaxRuleSnapshot
        {
            CommissionRate = 0m,
            CommissionIncomeTaxRate = 20m,
            VatRate = 10m,
            AccommodationTaxRate = 2m,
            Currency = "TRY"
        };
    }

    private async Task<PriceSummary> BuildPriceSummaryAsync(SqlConnection connection, long roomTypeId, DateOnly checkIn, DateOnly checkOut, int roomCount, CancellationToken cancellationToken)
    {
        var nightlyBreakdown = await _hotelPricingReadService.GetRoomNightlyBreakdownAsync(roomTypeId, checkIn, checkOut, cancellationToken);
        if (nightlyBreakdown.Count == 0)
        {
            return new PriceSummary
            {
                IsAvailable = false,
                AvailabilityMessage = "Seçilen tarihler için fiyat üretilemedi."
            };
        }

        var unavailableDays = nightlyBreakdown.Where(static item => !item.IsAvailable).ToList();
        var isAvailable = unavailableDays.Count == 0;
        var effectiveRoomCount = Math.Max(1, roomCount);
        var averageNightly = nightlyBreakdown.Average(static item => item.EffectivePrice);
        var roomTotal = nightlyBreakdown.Sum(static item => item.EffectivePrice) * effectiveRoomCount;
        var commissionRule = await LoadActiveCommissionRuleAsync(connection, roomTypeId, checkIn, cancellationToken);
        var netRoomAmount = roomTotal;
        var vatAmount = Math.Round(netRoomAmount * commissionRule.VatRate / 100m, 2, MidpointRounding.AwayFromZero);
        var accommodationTaxAmount = Math.Round(netRoomAmount * commissionRule.AccommodationTaxRate / 100m, 2, MidpointRounding.AwayFromZero);
        var tax = vatAmount + accommodationTaxAmount;
        var commissionAmount = Math.Round(netRoomAmount * commissionRule.CommissionRate / 100m, 2, MidpointRounding.AwayFromZero);
        var commissionIncomeTaxAmount = Math.Round(commissionAmount * commissionRule.CommissionIncomeTaxRate / 100m, 2, MidpointRounding.AwayFromZero);
        return new PriceSummary
        {
            NightlyPrice = averageNightly,
            RoomTotal = roomTotal,
            NetRoomAmount = netRoomAmount,
            VatRate = commissionRule.VatRate,
            VatAmount = vatAmount,
            AccommodationTaxRate = commissionRule.AccommodationTaxRate,
            AccommodationTaxAmount = accommodationTaxAmount,
            TaxAmount = tax,
            TotalAmount = roomTotal + tax,
            CommissionRuleId = commissionRule.RuleId,
            CommissionRate = commissionRule.CommissionRate,
            CommissionAmount = commissionAmount,
            CommissionIncomeTaxRate = commissionRule.CommissionIncomeTaxRate,
            CommissionIncomeTaxAmount = commissionIncomeTaxAmount,
            PlatformNetCommissionAmount = commissionAmount - commissionIncomeTaxAmount,
            HotelPayoutAmount = netRoomAmount - commissionAmount,
            IsAvailable = isAvailable,
            AvailabilityMessage = isAvailable
                ? null
                    : unavailableDays.Any(static item => item.IsClosed)
                    ? "Seçilen tarih aralığında satışa kapalı günler bulunuyor."
                    : "Seçilen tarih aralığında yeterli oda bulunmuyor.",
            NightlyBreakdown = nightlyBreakdown.ToList()
        };
    }

    private async Task<string> GenerateReservationNoAsync(SqlConnection connection, SqlTransaction transaction, CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand("SELECT COUNT(*) + 1 FROM rezervasyonlar WHERE CAST(olusturulma_tarihi AS date) = CAST(SYSUTCDATETIME() AS date);", connection, (SqlTransaction)transaction);
        var seq = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture);
        return $"WEB-{DateTime.Now:yyyyMMdd}-{seq:0000}";
    }

    private static string SplitFirstName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName)) return "Misafir";
        return fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "Misafir";
    }

    private static string NormalizePaymentMethod(string? paymentMethod)
    {
        var normalized = (paymentMethod ?? string.Empty).Trim();
        if (normalized.Equals("Sanal POS", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("Online Ödeme", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("Online Odeme", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("Kart ile Öde", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("Kart ile Ode", StringComparison.OrdinalIgnoreCase))
        {
            return "Sanal POS";
        }

        if (normalized.Equals("Kredi Kartı", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("Kredi Karti", StringComparison.OrdinalIgnoreCase))
        {
            return "Kredi Kartı";
        }

        return "Kapıda Ödeme";
    }

    private static bool IsOnlinePaymentPendingFeature(string? paymentMethod)
    {
        var normalized = (paymentMethod ?? string.Empty).Trim();
        return normalized.Equals("Online Ödeme", StringComparison.OrdinalIgnoreCase)
               || normalized.Equals("Online Odeme", StringComparison.OrdinalIgnoreCase)
               || normalized.Equals("Sanal POS", StringComparison.OrdinalIgnoreCase)
               || normalized.Equals("Kart ile Öde", StringComparison.OrdinalIgnoreCase)
               || normalized.Equals("Kart ile Ode", StringComparison.OrdinalIgnoreCase);
    }

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

    private static ReservationDraftUpsertRequest CloneDraftRequest(ReservationDraftUpsertRequest source)
        => new()
        {
            UserId = source.UserId,
            SessionKey = source.SessionKey,
            Source = source.Source,
            Status = source.Status,
            HotelId = source.HotelId,
            RoomTypeId = source.RoomTypeId,
            GuestFullName = source.GuestFullName,
            GuestEmail = source.GuestEmail,
            GuestPhone = source.GuestPhone,
            GuestCity = source.GuestCity,
            GuestDistrict = source.GuestDistrict,
            GuestNeighborhood = source.GuestNeighborhood,
            GuestAddress = source.GuestAddress,
            CheckInDate = source.CheckInDate,
            CheckOutDate = source.CheckOutDate,
            AdultCount = source.AdultCount,
            ChildCount = source.ChildCount,
            RoomCount = source.RoomCount,
            NightlyPrice = source.NightlyPrice,
            NetRoomAmount = source.NetRoomAmount,
            VatRate = source.VatRate,
            VatAmount = source.VatAmount,
            AccommodationTaxRate = source.AccommodationTaxRate,
            AccommodationTaxAmount = source.AccommodationTaxAmount,
            TaxAmount = source.TaxAmount,
            TotalAmount = source.TotalAmount,
            Currency = source.Currency,
            ReturnUrl = source.ReturnUrl,
            ProfileCompletionUrl = source.ProfileCompletionUrl,
            Notes = source.Notes
        };

    private async Task<SqlConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    private sealed class UserProfileSnapshot
    {
        public string FullName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string District { get; set; } = string.Empty;
        public string Neighborhood { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public DateTime? BirthDate { get; set; }
        public string Gender { get; set; } = string.Empty;
        public bool IsProfileComplete =>
            !string.IsNullOrWhiteSpace(FullName) &&
            !string.IsNullOrWhiteSpace(Email) &&
            !string.IsNullOrWhiteSpace(Phone) &&
            !string.IsNullOrWhiteSpace(City) &&
            !string.IsNullOrWhiteSpace(District) &&
            !string.IsNullOrWhiteSpace(Neighborhood) &&
            !string.IsNullOrWhiteSpace(Address) &&
            BirthDate.HasValue &&
            !string.IsNullOrWhiteSpace(Gender);
    }

    private sealed class HotelSnapshot
    {
        public string Name { get; set; } = string.Empty;
        public string HotelCode { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public decimal CommissionRate { get; set; }
        public string RoomName { get; set; } = string.Empty;
        public int MaxGuestCount { get; set; }
        public int MaxAdultCount { get; set; }
        public int MaxChildCount { get; set; }
        public string Slug { get; set; } = string.Empty;
    }

    private sealed class PriceSummary
    {
        public decimal NightlyPrice { get; set; }
        public decimal RoomTotal { get; set; }
        public decimal NetRoomAmount { get; set; }
        public decimal VatRate { get; set; }
        public decimal VatAmount { get; set; }
        public decimal AccommodationTaxRate { get; set; }
        public decimal AccommodationTaxAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public long? CommissionRuleId { get; set; }
        public decimal CommissionRate { get; set; }
        public decimal CommissionAmount { get; set; }
        public decimal CommissionIncomeTaxRate { get; set; }
        public decimal CommissionIncomeTaxAmount { get; set; }
        public decimal PlatformNetCommissionAmount { get; set; }
        public decimal HotelPayoutAmount { get; set; }
        public bool IsAvailable { get; set; }
        public string? AvailabilityMessage { get; set; }
        public List<RoomNightlyPricePoint> NightlyBreakdown { get; set; } = new();
    }

    private sealed class CommissionTaxRuleSnapshot
    {
        public long HotelId { get; set; }
        public long? RuleId { get; set; }
        public decimal CommissionRate { get; set; }
        public decimal CommissionIncomeTaxRate { get; set; }
        public decimal VatRate { get; set; }
        public decimal AccommodationTaxRate { get; set; }
        public string Currency { get; set; } = "TRY";
    }

    private static decimal SafeGetDecimal(SqlDataReader reader, int ordinal)
    {
        if (reader.IsDBNull(ordinal))
        {
            return 0m;
        }

        return Convert.ToDecimal(reader.GetValue(ordinal), CultureInfo.InvariantCulture);
    }
}
