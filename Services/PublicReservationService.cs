using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using SqlConnection = Microsoft.Data.SqlClient.SqlConnection;
using SqlCommand = Microsoft.Data.SqlClient.SqlCommand;
using SqlTransaction = Microsoft.Data.SqlClient.SqlTransaction;
using SqlException = Microsoft.Data.SqlClient.SqlException;
using otelturizmnew.Models.Email;
using otelturizmnew.Models.Oteller;
using otelturizmnew.Models.Messages;
using otelturizmnew.Models.Payments;
using otelturizmnew.Models.Reservations;
using otelturizmnew.Pricing;
using otelturizmnew.Services.Abstractions;
using otelturizmnew.Utils;

namespace otelturizmnew.Services;

public class PublicReservationService : IPublicReservationService
{
    private readonly string _connectionString;
    private readonly IHotelPricingReadService _hotelPricingReadService;
    private readonly IReservationDraftService _reservationDraftService;
    private readonly IEmailQueueService _emailQueueService;
    private readonly IPhoneVerificationService _phoneVerificationService;
    private readonly ISecureFileService _secureFileService;
    private readonly IWeatherService _weatherService;
    private readonly IDawnSurpriseService _dawnSurpriseService;
    private readonly IUserLoyaltyPointsService _loyaltyPointsService;
    private readonly IHotelPointsService _hotelPointsService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<PublicReservationService> _logger;

    public PublicReservationService(
        IConfiguration configuration,
        IHotelPricingReadService hotelPricingReadService,
        IReservationDraftService reservationDraftService,
        IEmailQueueService emailQueueService,
        IPhoneVerificationService phoneVerificationService,
        ISecureFileService secureFileService,
        IWeatherService weatherService,
        IDawnSurpriseService dawnSurpriseService,
        IUserLoyaltyPointsService loyaltyPointsService,
        IHotelPointsService hotelPointsService,
        IHttpContextAccessor httpContextAccessor,
        ILogger<PublicReservationService> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection tanimli degil.");
        _hotelPricingReadService = hotelPricingReadService;
        _reservationDraftService = reservationDraftService;
        _emailQueueService = emailQueueService;
        _phoneVerificationService = phoneVerificationService;
        _secureFileService = secureFileService;
        _weatherService = weatherService;
        _dawnSurpriseService = dawnSurpriseService;
        _loyaltyPointsService = loyaltyPointsService;
        _hotelPointsService = hotelPointsService;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public Task<ReservationDraftSummaryViewModel?> GetActiveDraftAsync(long? userId, string? sessionKey, CancellationToken cancellationToken = default)
        => _reservationDraftService.GetActiveDraftAsync(userId, sessionKey, cancellationToken);

    public async Task<PublicReservationPriceQuoteViewModel> GetPriceQuoteAsync(long roomTypeId, DateOnly checkInDate, DateOnly checkOutDate, int roomCount, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        var pricing = await BuildPriceSummaryAsync(connection, roomTypeId, checkInDate, checkOutDate, roomCount, cancellationToken);
        var quote = MapPriceQuoteViewModel(pricing);
        quote.NightCount = Math.Max(1, checkOutDate.DayNumber - checkInDate.DayNumber);
        return quote;
    }

    private void ApplyDawnSurpriseToSummary(PriceSummary pricing)
    {
        if (!pricing.IsAvailable || _httpContextAccessor.HttpContext is not { } httpContext)
        {
            return;
        }

        if (!_dawnSurpriseService.IsEligible(httpContext))
        {
            return;
        }

        var dawnPercent = _dawnSurpriseService.GetActive(httpContext)?.Percent ?? 0;
        if (dawnPercent <= 0)
        {
            return;
        }

        var originalTotal = pricing.TotalAmount;
        var total = pricing.TotalAmount;
        var net = pricing.NetRoomAmount;
        var vat = pricing.VatAmount;
        var acc = pricing.AccommodationTaxAmount;
        var tax = pricing.TaxAmount;
        if (DawnSurprisePricing.TryApplyPercent(ref total, ref net, ref vat, ref acc, ref tax, dawnPercent))
        {
            pricing.OriginalTotalBeforeDawn = originalTotal;
            pricing.DawnSurprisePercent = dawnPercent;
            pricing.DawnSurpriseDiscountAmount = originalTotal - total;
            pricing.TotalAmount = total;
            pricing.NetRoomAmount = net;
            pricing.VatAmount = vat;
            pricing.AccommodationTaxAmount = acc;
            pricing.TaxAmount = tax;
        }
    }

    private PublicReservationPriceQuoteViewModel MapPriceQuoteViewModel(PriceSummary pricing)
    {
        var originalTotal = pricing.TotalAmount;
        var dawnPercent = _httpContextAccessor.HttpContext is { } httpContext
            && _dawnSurpriseService.IsEligible(httpContext)
            ? _dawnSurpriseService.GetActive(httpContext)?.Percent ?? 0
            : 0;
        var discountAmount = 0m;
        if (pricing.IsAvailable && dawnPercent > 0)
        {
            var total = pricing.TotalAmount;
            var net = pricing.NetRoomAmount;
            var vat = pricing.VatAmount;
            var acc = pricing.AccommodationTaxAmount;
            var tax = pricing.TaxAmount;
            if (DawnSurprisePricing.TryApplyPercent(ref total, ref net, ref vat, ref acc, ref tax, dawnPercent))
            {
                pricing.TotalAmount = total;
                pricing.NetRoomAmount = net;
                pricing.VatAmount = vat;
                pricing.AccommodationTaxAmount = acc;
                pricing.TaxAmount = tax;
                discountAmount = originalTotal - total;
            }
        }

        return new PublicReservationPriceQuoteViewModel
        {
            NightCount = pricing.NightlyBreakdown.Count > 0
                ? pricing.NightlyBreakdown.Count
                : 1,
            NightlyPrice = pricing.NightlyPrice,
            RoomTotal = pricing.RoomTotal,
            NetRoomAmount = pricing.NetRoomAmount,
            VatRate = pricing.VatRate,
            VatAmount = pricing.VatAmount,
            AccommodationTaxRate = pricing.AccommodationTaxRate,
            AccommodationTaxAmount = pricing.AccommodationTaxAmount,
            TaxAmount = pricing.TaxAmount,
            OriginalTotalAmount = originalTotal,
            DawnSurprisePercent = dawnPercent,
            DawnSurpriseDiscountAmount = discountAmount,
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

    public async Task<PublicReservationResult> StartReservationAsync(long? userId, string? sessionKey, PublicHotelReservationForm form, IFormFile? bankTransferReceipt, CancellationToken cancellationToken = default)
    {
        if (form.HotelId <= 0)
        {
            return new PublicReservationResult { Message = "Otel ve oda tipi secilmeden rezervasyon baslatilamaz." };
        }

        var selections = ParseMultiRoomSelections(form);
        if (selections.Count == 0)
        {
            return new PublicReservationResult { Message = "Oda secimi yapilmadan rezervasyon baslatilamaz." };
        }

        await using var connection = await OpenConnectionAsync(cancellationToken);
        var hotel = await LoadHotelAsync(connection, form.HotelId, selections[0].RoomTypeId, cancellationToken);
        var occupancyValidationMessage = await ValidateRoomOccupancyMultiAsync(connection, form, selections, cancellationToken);
        if (!string.IsNullOrWhiteSpace(occupancyValidationMessage))
        {
            return new PublicReservationResult
            {
                Message = occupancyValidationMessage,
                RedirectUrl = $"/oteller/{hotel.Slug}"
            };
        }

        var perRoomPricing = new List<(PublicMultiRoomSelectionItem Selection, PriceSummary Pricing)>(selections.Count);
        foreach (var selection in selections)
        {
            if (selection.RoomTypeId <= 0)
            {
                return new PublicReservationResult { Message = "Oda tipi secilmeden rezervasyon baslatilamaz.", RedirectUrl = $"/oteller/{hotel.Slug}" };
            }
            if (selection.CheckOutDate <= selection.CheckInDate)
            {
                return new PublicReservationResult { Message = "Cikis tarihi giris tarihinden sonra olmalidir.", RedirectUrl = $"/oteller/{hotel.Slug}" };
            }

            var pricing = await BuildPriceSummaryAsync(connection, selection.RoomTypeId, selection.CheckInDate, selection.CheckOutDate, Math.Max(1, selection.RoomCount), cancellationToken);
            ApplyDawnSurpriseToSummary(pricing);
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
            perRoomPricing.Add((selection, pricing));
        }

        var totalAmountOverall = perRoomPricing.Sum(x => x.Pricing.TotalAmount);
        var dawnPercent = _httpContextAccessor.HttpContext is { } httpContext
            && _dawnSurpriseService.IsEligible(httpContext)
            ? _dawnSurpriseService.GetActive(httpContext)?.Percent ?? 0
            : 0;

        if (!TryBuildPaymentAllocation(form, totalAmountOverall, out var paymentPlan, out var paymentPlanError))
        {
            return new PublicReservationResult
            {
                Message = paymentPlanError ?? "Odeme plani gecersiz.",
                RedirectUrl = $"/oteller/{hotel.Slug}"
            };
        }

        var dawnNote = dawnPercent > 0 ? $" SafakSurpriz:%{dawnPercent}." : string.Empty;
        var draftRequest = new ReservationDraftUpsertRequest
        {
            UserId = userId,
            SessionKey = sessionKey,
            Source = "Public",
            HotelId = form.HotelId,
            RoomTypeId = selections[0].RoomTypeId,
            CheckInDate = selections[0].CheckInDate,
            CheckOutDate = selections[0].CheckOutDate,
            AdultCount = form.AdultCount,
            ChildCount = form.ChildCount,
            RoomCount = selections.Count,
            NightlyPrice = null,
            NetRoomAmount = perRoomPricing.Sum(x => x.Pricing.NetRoomAmount),
            VatRate = perRoomPricing.FirstOrDefault().Pricing.VatRate,
            VatAmount = perRoomPricing.Sum(x => x.Pricing.VatAmount),
            AccommodationTaxRate = perRoomPricing.FirstOrDefault().Pricing.AccommodationTaxRate,
            AccommodationTaxAmount = perRoomPricing.Sum(x => x.Pricing.AccommodationTaxAmount),
            TaxAmount = perRoomPricing.Sum(x => x.Pricing.TaxAmount),
            TotalAmount = totalAmountOverall,
            ReturnUrl = $"/oteller/{hotel.Slug}?continueDraft=1",
            ProfileCompletionUrl = $"/oteller/{hotel.Slug}?continueDraft=1&openProfile=1",
            Notes = (string.IsNullOrWhiteSpace(form.RoomsJson)
                ? "Public otel detay sayfasindan baslatildi."
                : "Public otel detay sayfasindan baslatildi. RoomsJson=" + form.RoomsJson) + dawnNote,
            GuestUlkeId = null
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
            ApplyGuestLocation(profileDraft, userProfile);
            await _reservationDraftService.SaveOrUpdateAsync(profileDraft, cancellationToken);

            return new PublicReservationResult
            {
                Message = "Rezervasyon taslak olarak kaydedildi. Lutfen profil bilgilerinizi tamamlayin.",
                RedirectUrl = $"/oteller/{hotel.Slug}?continueDraft=1&openProfile=1"
            };
        }

        var phoneVerificationRequirement = await _phoneVerificationService.GetReservationRequirementAsync(
            authenticatedUserId,
            $"/oteller/{hotel.Slug}?continueDraft=1",
            cancellationToken);
        if (!phoneVerificationRequirement.IsAllowed)
        {
            var verificationDraft = CloneDraftRequest(draftRequest);
            verificationDraft.Status = "Telefon Dogrulamasi Bekleniyor";
            verificationDraft.GuestFullName = userProfile.FullName;
            verificationDraft.GuestEmail = userProfile.Email;
            verificationDraft.GuestPhone = userProfile.Phone;
            verificationDraft.GuestCity = userProfile.City;
            verificationDraft.GuestDistrict = userProfile.District;
            verificationDraft.GuestNeighborhood = userProfile.Neighborhood;
            verificationDraft.GuestAddress = userProfile.Address;
            ApplyGuestLocation(verificationDraft, userProfile);
            await _reservationDraftService.SaveOrUpdateAsync(verificationDraft, cancellationToken);

            return new PublicReservationResult
            {
                Message = phoneVerificationRequirement.Message,
                RedirectUrl = phoneVerificationRequirement.RedirectUrl
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
        ApplyGuestLocation(readyDraft, userProfile);
        var draftId = await _reservationDraftService.SaveOrUpdateAsync(readyDraft, cancellationToken);

        HotelWeatherWidgetViewModel? postBookingWeather = null;
        try
        {
            var weatherAnchor = await LoadHotelAsync(connection, form.HotelId, selections[0].RoomTypeId, cancellationToken);
            postBookingWeather = await _weatherService.GetForecastAsync(
                weatherAnchor.District,
                weatherAnchor.City,
                weatherAnchor.Latitude,
                weatherAnchor.Longitude,
                cancellationToken);
        }
        catch (Exception weatherEx)
        {
            _logger.LogDebug(weatherEx, "Post-booking weather prefetch skipped. hotelId={HotelId}", form.HotelId);
        }

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            var insertSql = $@"
                INSERT INTO [dbo].[REZERVASYONLAR]
                (
                    [REZERVASYON_NO], [OTEL_ID], [ODA_TIP_ID], [KULLANICI_ID],
                    [MISAFIR_AD_SOYAD], [MISAFIR_EPOSTA], [MISAFIR_TELEFON], [MISAFIR_NOTU],
                    [MISAFIR_SEHIR], [MISAFIR_ILCE], [MISAFIR_MAHALLE], [MISAFIR_ADRES],
                    [MISAFIR_ULKE_ID], [MISAFIR_IL_ID], [MISAFIR_ILCE_ID], [MISAFIR_MAHALLE_ID],
                    [GIRIS_TARIHI], [CIKIS_TARIHI], [YETISKIN_SAYISI], [COCUK_SAYISI], [ODA_SAYISI],
                    [GECELIK_FIYAT], [NET_ODA_TUTARI], [TOPLAM_ODA_TUTARI], [VERGI_TUTARI], [TOPLAM_VERGI_TUTARI], [KDV_ORANI], [KDV_TUTARI],
                    [KONAKLAMA_VERGISI_ORANI], [KONAKLAMA_VERGISI_TUTARI], [TOPLAM_TUTAR], [VERGILER_DAHIL_TOPLAM_TUTAR],
                    [KOMISYON_VERGI_KURAL_ID], [KOMISYON_ORANI], [KOMISYON_TUTARI], [KOMISYON_GELIR_VERGISI_ORANI], [KOMISYON_GELIR_VERGISI_TUTARI],
                    [PLATFORM_NET_KOMISYON_TUTARI], [OTELE_ODENECEK_TUTAR],
                    [DURUM], [REZERVASYON_DURUMU_ID], [ODEME_DURUMU], [ODEME_YONTEMI],
                    [KAPIDA_ODEME_TUTARI], [KAPIDA_ODEME_DURUMU], [ONLINE_ODEME_TUTARI], [ONLINE_ODEME_DURUMU],
                    [HAVALE_EFT_BEKLEYEN_TUTARI], [ODEME_REFERANS_NO],
                    [TAHSIL_EDILEN_TUTAR], [KALAN_TAHSIL_EDILECEK_TUTAR], [ON_ODEME_TUTARI], [KALAN_ODEME_TUTARI],
                    [OTEL_ONAY_DURUMU], [FIRMA_ONAY_DURUMU],
                    [INDIRIM_TUTARI], [SAFAK_SURPRIZI_ORANI], [SAFAK_SURPRIZI_INDIRIM_TUTARI],
                    [KAYNAK], [REZERVASYON_KANALI], [OZEL_ISTEKLER], [REZERVASYON_TASLAGI_ID]
                )
                VALUES
                (
                    @reservationNo, @hotelId, @roomTypeId, @userId,
                    @fullName, @email, @phone, @note,
                    @city, @district, @neighborhood, @address,
                    @guestUlkeId, @guestIlId, @guestIlceId, @guestMahalleId,
                    @checkIn, @checkOut, @adultCount, @childCount, @roomCount,
                    @nightlyPrice, @netRoomAmount, @roomTotal, @taxAmount, @taxAmount, @vatRate, @vatAmount,
                    @accommodationTaxRate, @accommodationTaxAmount, @totalAmount, @totalAmount,
                    @commissionRuleId, @commissionRate, @commissionAmount, @commissionIncomeTaxRate, @commissionIncomeTaxAmount,
                    @platformNetCommissionAmount, @hotelPayoutAmount,
                    'Onay Bekliyor', (SELECT TOP (1) id FROM [dbo].[REZERVASYON_DURUM_TANIMLARI] WHERE kod = N'{RezervasyonDurumKodlari.OnayBekliyor}'), @aggregateOdemeDurumu, @legacyOdemeYontemi, @cashAtHotelAmount, @cashAtHotelStatus, @onlinePaymentAmount, @onlinePaymentStatus,
                    @havalePendingAmount, @bankTransferReferenceSql,
                    0, @remainingCollectionAmount, 0, @remainingCollectionAmount,
                    'Beklemede', 'Onay Gerekmiyor',
                    @discountAmount, @dawnSurprisePercent, @dawnSurpriseDiscountAmount,
                    'Web', 'Web', @note, @draftId
                );
                SELECT CAST(SCOPE_IDENTITY() AS bigint);";

            var createdReservationIds = new List<long>(perRoomPricing.Count);
            var createdReservationAwards = new List<(long ReservationId, decimal TotalAmount)>(perRoomPricing.Count);
            var createdReservationNos = new List<string>(perRoomPricing.Count);
            var emailJobs = new List<ReservationEmailJob>(perRoomPricing.Count * 2);
            var remainingKapida = paymentPlan.KapidaTutari;
            var remainingOnline = paymentPlan.OnlineTutari;
            var remainingHavale = paymentPlan.HavaleBekleyen;

            for (var i = 0; i < perRoomPricing.Count; i++)
            {
                var (selection, pricing) = perRoomPricing[i];
                var ratio = totalAmountOverall > 0 ? (pricing.TotalAmount / totalAmountOverall) : 0m;
                var isLast = i == perRoomPricing.Count - 1;

                var kapida = isLast ? remainingKapida : Math.Round(paymentPlan.KapidaTutari * ratio, 2, MidpointRounding.AwayFromZero);
                var online = isLast ? remainingOnline : Math.Round(paymentPlan.OnlineTutari * ratio, 2, MidpointRounding.AwayFromZero);
                var havale = isLast ? remainingHavale : Math.Round(paymentPlan.HavaleBekleyen * ratio, 2, MidpointRounding.AwayFromZero);
                remainingKapida -= kapida;
                remainingOnline -= online;
                remainingHavale -= havale;

                var reservationNo = await GenerateReservationNoAsync(connection, (SqlTransaction)transaction, cancellationToken);
                var roomHotel = await LoadHotelAsync(connection, form.HotelId, selection.RoomTypeId, cancellationToken, (SqlTransaction)transaction);

                long reservationId;
                await using (var insertCommand = new SqlCommand(insertSql, connection, (SqlTransaction)transaction))
                {
                    insertCommand.Parameters.AddWithValue("@reservationNo", reservationNo);
                    insertCommand.Parameters.AddWithValue("@hotelId", form.HotelId);
                    insertCommand.Parameters.AddWithValue("@roomTypeId", selection.RoomTypeId);
                    insertCommand.Parameters.AddWithValue("@userId", authenticatedUserId);
                    insertCommand.Parameters.AddWithValue("@fullName", userProfile.FullName);
                    insertCommand.Parameters.AddWithValue("@email", userProfile.Email);
                    insertCommand.Parameters.AddWithValue("@phone", userProfile.Phone);
                    insertCommand.Parameters.AddWithValue("@note", selections.Count > 1 ? "Web rezervasyon talebi (çoklu oda)" : "Web rezervasyon talebi");
                    insertCommand.Parameters.AddWithValue("@city", userProfile.City);
                    insertCommand.Parameters.AddWithValue("@district", userProfile.District);
                    insertCommand.Parameters.AddWithValue("@neighborhood", userProfile.Neighborhood);
                    insertCommand.Parameters.AddWithValue("@address", userProfile.Address);
                    insertCommand.Parameters.AddWithValue("@guestUlkeId", userProfile.UlkeId.HasValue ? userProfile.UlkeId.Value : DBNull.Value);
                    insertCommand.Parameters.AddWithValue("@guestIlId", userProfile.IlId.HasValue ? userProfile.IlId.Value : DBNull.Value);
                    insertCommand.Parameters.AddWithValue("@guestIlceId", userProfile.IlceId.HasValue ? userProfile.IlceId.Value : DBNull.Value);
                    insertCommand.Parameters.AddWithValue("@guestMahalleId", userProfile.MahalleId.HasValue ? userProfile.MahalleId.Value : DBNull.Value);
                    insertCommand.Parameters.AddWithValue("@checkIn", selection.CheckInDate.ToDateTime(TimeOnly.MinValue));
                    insertCommand.Parameters.AddWithValue("@checkOut", selection.CheckOutDate.ToDateTime(TimeOnly.MinValue));
                    insertCommand.Parameters.AddWithValue("@adultCount", form.AdultCount);
                    insertCommand.Parameters.AddWithValue("@childCount", form.ChildCount);
                    insertCommand.Parameters.AddWithValue("@roomCount", Math.Max(1, selection.RoomCount));
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
                    insertCommand.Parameters.AddWithValue("@aggregateOdemeDurumu", paymentPlan.AggregateOdemeDurumu);
                    insertCommand.Parameters.AddWithValue("@legacyOdemeYontemi", paymentPlan.LegacyOdemeYontemi);
                    insertCommand.Parameters.AddWithValue("@cashAtHotelAmount", kapida);
                    insertCommand.Parameters.AddWithValue("@cashAtHotelStatus", paymentPlan.KapidaDurumu);
                    insertCommand.Parameters.AddWithValue("@onlinePaymentAmount", online);
                    insertCommand.Parameters.AddWithValue("@onlinePaymentStatus", paymentPlan.OnlineDurumu);
                    insertCommand.Parameters.AddWithValue("@havalePendingAmount", havale);
                    insertCommand.Parameters.AddWithValue("@bankTransferReferenceSql", string.IsNullOrWhiteSpace(paymentPlan.BankTransferReference) ? DBNull.Value : paymentPlan.BankTransferReference.Trim());
                    insertCommand.Parameters.AddWithValue("@remainingCollectionAmount", pricing.TotalAmount);
                    insertCommand.Parameters.AddWithValue("@discountAmount", pricing.DawnSurpriseDiscountAmount > 0m ? pricing.DawnSurpriseDiscountAmount : DBNull.Value);
                    insertCommand.Parameters.AddWithValue("@dawnSurprisePercent", pricing.DawnSurprisePercent > 0 ? pricing.DawnSurprisePercent : DBNull.Value);
                    insertCommand.Parameters.AddWithValue("@dawnSurpriseDiscountAmount", pricing.DawnSurpriseDiscountAmount > 0m ? pricing.DawnSurpriseDiscountAmount : DBNull.Value);
                    insertCommand.Parameters.AddWithValue("@draftId", draftId);
                    var reservationIdRaw = await insertCommand.ExecuteScalarAsync(cancellationToken);
                    reservationId = Convert.ToInt64(reservationIdRaw ?? 0L, CultureInfo.InvariantCulture);
                }

                var splitPlan = new ReservationPaymentPlan
                {
                    AggregateOdemeDurumu = paymentPlan.AggregateOdemeDurumu,
                    LegacyOdemeYontemi = paymentPlan.LegacyOdemeYontemi,
                    KapidaTutari = kapida,
                    KapidaDurumu = paymentPlan.KapidaDurumu,
                    OnlineTutari = online,
                    OnlineDurumu = paymentPlan.OnlineDurumu,
                    HavaleBekleyen = havale,
                    BankTransferReference = paymentPlan.BankTransferReference,
                    Lines = new List<ReservationPaymentLine>()
                };
                if (kapida > 0)
                {
                    splitPlan.Lines.Add(new ReservationPaymentLine { MethodKod = OdemeYontemiKodlari.KapidaOdeme, Tutar = kapida });
                }
                if (online > 0)
                {
                    splitPlan.Lines.Add(new ReservationPaymentLine { MethodKod = OdemeYontemiKodlari.SanalPos, Tutar = online });
                }
                if (havale > 0)
                {
                    splitPlan.Lines.Add(new ReservationPaymentLine { MethodKod = OdemeYontemiKodlari.HavaleEft, Tutar = havale, HavaleReferans = splitPlan.BankTransferReference });
                }
                await InsertReservationPaymentLinesAsync(connection, (SqlTransaction)transaction, reservationId, splitPlan, cancellationToken);
                createdReservationIds.Add(reservationId);
                createdReservationAwards.Add((reservationId, pricing.TotalAmount));
                createdReservationNos.Add(reservationNo);

                emailJobs.Add(new ReservationEmailJob(
                    authenticatedUserId,
                    userProfile.Email,
                    "reservation_received_customer",
                    reservationId,
                    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["user_first_name"] = userProfile.FirstName,
                        ["booking_reference"] = reservationNo,
                        ["hotel_name"] = roomHotel.Name,
                        ["check_in_date"] = selection.CheckInDate.ToString("dd MMM yyyy", CultureInfo.GetCultureInfo("tr-TR")),
                        ["check_out_date"] = selection.CheckOutDate.ToString("dd MMM yyyy", CultureInfo.GetCultureInfo("tr-TR")),
                        ["total_price"] = pricing.TotalAmount.ToString("N2", CultureInfo.GetCultureInfo("tr-TR")),
                        ["room_type_name"] = roomHotel.RoomName,
                        ["booking_details_link"] = "/panel/user/rezervasyonlarim",
                        ["hotel_address"] = roomHotel.Address
                    }));

                var partnerRecipient = await ResolvePartnerRecipientAsync(connection, (SqlTransaction)transaction, form.HotelId, cancellationToken);
                emailJobs.Add(new ReservationEmailJob(
                    partnerRecipient.UserId,
                    partnerRecipient.Email,
                    "reservation_new_partner",
                    reservationId,
                    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["hotel_manager_name"] = partnerRecipient.ManagerName,
                        ["hotel_name"] = roomHotel.Name,
                        ["booking_reference"] = reservationNo,
                        ["guest_full_name"] = userProfile.FullName,
                        ["guest_email"] = userProfile.Email,
                        ["guest_phone"] = userProfile.Phone,
                        ["total_price"] = pricing.TotalAmount.ToString("N2", CultureInfo.GetCultureInfo("tr-TR")),
                        ["check_in_date"] = selection.CheckInDate.ToString("dd MMM yyyy", CultureInfo.GetCultureInfo("tr-TR")),
                        ["check_out_date"] = selection.CheckOutDate.ToString("dd MMM yyyy", CultureInfo.GetCultureInfo("tr-TR")),
                        ["room_type_name"] = roomHotel.RoomName,
                        ["room_count"] = Math.Max(1, selection.RoomCount).ToString(CultureInfo.InvariantCulture)
                    }));
            }

            await EnsureReservationHotelFavoriteAsync(connection, (SqlTransaction)transaction, authenticatedUserId, form.HotelId, hotel.Slug, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            if (dawnPercent > 0 && _httpContextAccessor.HttpContext is { } httpContextAfterCommit)
            {
                _dawnSurpriseService.Clear(httpContextAfterCommit);
            }
            try
            {
                foreach (var (reservationId, totalAmount) in createdReservationAwards)
                {
                    await _hotelPointsService.TryAwardReservationPointsAsync(
                        authenticatedUserId,
                        form.HotelId,
                        reservationId,
                        totalAmount,
                        cancellationToken);

                    var points = _hotelPointsService.CalculateEarnPoints(totalAmount);
                    if (points > 0)
                    {
                        await _loyaltyPointsService.TryAwardReservationPointsAsync(
                            authenticatedUserId,
                            reservationId,
                            points,
                            cancellationToken);
                    }
                }
            }
            catch (Exception loyaltyException)
            {
                _logger.LogWarning(loyaltyException, "Rezervasyon sadakat puani eklenemedi. UserId: {UserId}", authenticatedUserId);
            }

            try
            {
                await QueueReservationEmailsAsync(emailJobs, cancellationToken);
            }
            catch (Exception emailQueueException)
            {
                _logger.LogWarning(emailQueueException, "Rezervasyon e-posta kuyruğu oluşturulamadı. DraftId: {DraftId}", draftId);
            }
            try
            {
                if (createdReservationIds.Count > 0)
                {
                    await AttachBankTransferReceiptIfNeededAsync(createdReservationIds[0], authenticatedUserId, bankTransferReceipt, paymentPlan, cancellationToken);
                }
            }
            catch (Exception attachmentException)
            {
                _logger.LogWarning(attachmentException, "Havale dekontu kaydedilemedi. ReservationId: {ReservationId}", createdReservationIds.Count > 0 ? createdReservationIds[0] : 0);
            }

            try
            {
                if (createdReservationIds.Count > 0)
                {
                    await _reservationDraftService.MarkCompletedAsync(draftId, createdReservationIds[0], cancellationToken);
                }
            }
            catch (Exception draftCleanupException)
            {
                _logger.LogWarning(draftCleanupException, "Rezervasyon tamamlandi ancak taslak temizlenemedi. DraftId: {DraftId}", draftId);
            }

            var reservationNosText = createdReservationNos.Count > 0
                ? string.Join(", ", createdReservationNos)
                : "WEB";

            _logger.LogInformation(
                "RESERVATION_AUDIT create source=public userId={UserId} hotelId={HotelId} roomTypeId={RoomTypeId} reservations={ReservationNos} total={Total} payment={PaymentMethod} rooms={RoomsCount}",
                authenticatedUserId,
                form.HotelId,
                form.RoomTypeId,
                reservationNosText,
                totalAmountOverall,
                (form.PaymentMethod ?? string.Empty).Trim(),
                selections.Count);

            _logger.LogInformation(
                "POST_BOOKING_AUTOMATION hotelId={HotelId} destination_forecast_days={Days}",
                form.HotelId,
                postBookingWeather?.Days?.Count ?? 0);
            if (createdReservationIds.Count > 0)
            {
                _logger.LogInformation("NPS_LOOP_PLANNED reservationId={ReservationId}", createdReservationIds[0]);
            }

            return new PublicReservationResult
            {
                Success = true,
                Message = createdReservationNos.Count > 1
                    ? $"Rezervasyonlariniz alindi: {reservationNosText}"
                    : $"Rezervasyonunuz alindi: {reservationNosText}",
                ReservationId = createdReservationIds.Count > 0 ? createdReservationIds[0] : null,
                RedirectUrl = $"/oteller/{hotel.Slug}?reservationCreated=1"
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

    private static List<PublicMultiRoomSelectionItem> ParseMultiRoomSelections(PublicHotelReservationForm form)
    {
        if (!string.IsNullOrWhiteSpace(form.RoomsJson))
        {
            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var items = JsonSerializer.Deserialize<List<PublicMultiRoomSelectionItem>>(form.RoomsJson, options) ?? new List<PublicMultiRoomSelectionItem>();
                return items
                    .Where(x => x is not null && x.RoomTypeId > 0)
                    .Select(x => new PublicMultiRoomSelectionItem
                    {
                        RoomTypeId = x.RoomTypeId,
                        CheckInDate = x.CheckInDate,
                        CheckOutDate = x.CheckOutDate,
                        RoomCount = Math.Max(1, x.RoomCount)
                    })
                    .Take(6)
                    .ToList();
            }
            catch
            {
                // ignore malformed JSON; fallback to single-room binding
            }
        }

        if (form.RoomTypeId <= 0)
        {
            return new List<PublicMultiRoomSelectionItem>();
        }

        return new List<PublicMultiRoomSelectionItem>
        {
            new()
            {
                RoomTypeId = form.RoomTypeId,
                CheckInDate = form.CheckInDate,
                CheckOutDate = form.CheckOutDate,
                RoomCount = Math.Max(1, form.RoomCount)
            }
        };
    }

    private async Task<UserProfileSnapshot> LoadUserProfileAsync(SqlConnection connection, long userId, CancellationToken cancellationToken)
    {
        var hasGeoIds = await ColumnExistsAsync(connection, "KULLANICILAR", "ULKE_ID", cancellationToken);
        var sql = hasGeoIds
            ? @"
            SELECT TOP (1) [AD_SOYAD], [EPOSTA], COALESCE([TELEFON], ''), COALESCE([SEHIR], ''), COALESCE(ilce, ''), COALESCE([MAHALLE], ''), COALESCE([ADRES], ''),
                   [DOGUM_TARIHI], COALESCE([CINSIYET], ''), [ULKE_ID], [IL_ID], [ILCE_ID], [MAHALLE_ID]
            FROM [dbo].[KULLANICILAR]
            WHERE id = @userId;"
            : @"
            SELECT TOP (1) [AD_SOYAD], [EPOSTA], COALESCE([TELEFON], ''), COALESCE([SEHIR], ''), COALESCE(ilce, ''), COALESCE([MAHALLE], ''), COALESCE([ADRES], ''), [DOGUM_TARIHI], COALESCE([CINSIYET], '')
            FROM [dbo].[KULLANICILAR]
            WHERE id = @userId;";
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@userId", userId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            throw new InvalidOperationException("Kullanici profili bulunamadi.");
        }

        var fullName = reader.GetString(0);
        var snapshot = new UserProfileSnapshot
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
        if (hasGeoIds)
        {
            snapshot.UlkeId = reader.IsDBNull(9) ? null : reader.GetInt64(9);
            snapshot.IlId = reader.IsDBNull(10) ? null : reader.GetInt64(10);
            snapshot.IlceId = reader.IsDBNull(11) ? null : reader.GetInt64(11);
            snapshot.MahalleId = reader.IsDBNull(12) ? null : reader.GetInt64(12);
        }

        return snapshot;
    }

    private static void ApplyGuestLocation(ReservationDraftUpsertRequest draft, UserProfileSnapshot profile)
    {
        if (profile.UlkeId.HasValue)
        {
            draft.GuestUlkeId = profile.UlkeId;
        }

        if (profile.IlId.HasValue)
        {
            draft.GuestIlId = profile.IlId;
        }

        if (profile.IlceId.HasValue)
        {
            draft.GuestIlceId = profile.IlceId;
        }

        if (profile.MahalleId.HasValue)
        {
            draft.GuestMahalleId = profile.MahalleId;
        }
    }

    private static async Task<bool> ColumnExistsAsync(SqlConnection connection, string tableName, string columnName, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT COUNT(*)
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_CATALOG = DB_NAME()
              AND TABLE_NAME = @tableName
              AND COLUMN_NAME = @columnName;";
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@tableName", tableName);
        command.Parameters.AddWithValue("@columnName", columnName);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result ?? 0, CultureInfo.InvariantCulture) > 0;
    }

    private async Task<HotelSnapshot> LoadHotelAsync(SqlConnection connection, long hotelId, long roomTypeId, CancellationToken cancellationToken, SqlTransaction? transaction = null)
    {
        const string sql = @"
            SELECT TOP (1)
                o.[OTEL_ADI],
                o.[OTEL_KODU],
                o.[TAM_ADRES],
                COALESCE(o.[VARSAYILAN_KOMISYON_ORANI], 0),
                ot.[ODA_ADI],
                COALESCE(ot.[MAKSIMUM_KISI_SAYISI], 1),
                COALESCE(ot.[MAKSIMUM_YETISKIN_SAYISI], 1),
                COALESCE(ot.[MAKSIMUM_COCUK_SAYISI], 0),
                COALESCE(o.[SEHIR], ''),
                COALESCE(o.[ILCE], ''),
                o.[ENLEM],
                o.[BOYLAM]
            FROM [dbo].[OTELLER] o
            INNER JOIN [dbo].[ODA_TIPLERI] ot ON ot.id = @roomTypeId AND ot.[OTEL_ID] = o.id
            WHERE o.id = @hotelId;";
        await using var command = transaction is null
            ? new SqlCommand(sql, connection)
            : new SqlCommand(sql, connection, transaction);
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

    private async Task<string?> ValidateRoomOccupancyMultiAsync(
        SqlConnection connection,
        PublicHotelReservationForm form,
        List<PublicMultiRoomSelectionItem> selections,
        CancellationToken cancellationToken)
    {
        var adultCount = Math.Max(1, form.AdultCount);
        var childCount = Math.Max(0, form.ChildCount);
        var totalGuestCount = adultCount + childCount;

        var totalMaxGuest = 0;
        var totalMaxAdult = 0;
        var totalMaxChild = 0;
        var roomNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var selection in selections)
        {
            var snap = await LoadHotelAsync(connection, form.HotelId, selection.RoomTypeId, cancellationToken);
            roomNames.Add(snap.RoomName);
            var rc = Math.Max(1, selection.RoomCount);
            totalMaxGuest += Math.Max(1, snap.MaxGuestCount) * rc;
            totalMaxAdult += Math.Max(1, snap.MaxAdultCount) * rc;
            totalMaxChild += Math.Max(0, snap.MaxChildCount) * rc;
        }

        var isSingleRoomType = roomNames.Count <= 1;
        var roomLabel = isSingleRoomType ? roomNames.FirstOrDefault() ?? "Seçilen oda" : "Seçilen oda(lar)";

        if (adultCount > totalMaxAdult)
        {
            return $"{roomLabel} toplam yetişkin kapasitesi: en fazla {totalMaxAdult} yetişkin. {adultCount} yetişkin için lütfen bir oda daha ekleyin veya oda tipini değiştirin.";
        }

        if (childCount > totalMaxChild)
        {
            return $"{roomLabel} toplam çocuk kapasitesi: en fazla {totalMaxChild} çocuk. {childCount} çocuk için lütfen bir oda daha ekleyin veya oda tipini değiştirin.";
        }

        if (totalGuestCount > totalMaxGuest)
        {
            return $"{roomLabel} toplam kapasitesi: en fazla {totalMaxGuest} kişi. Toplam {totalGuestCount} misafir için lütfen bir oda daha ekleyin veya oda tipini değiştirin.";
        }

        return null;
    }

    private async Task<(long UserId, string Email, string ManagerName)> ResolvePartnerRecipientAsync(SqlConnection connection, SqlTransaction transaction, long hotelId, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT COALESCE(o.[KULLANICI_ID], oks.[KULLANICI_ID], 1),
                   COALESCE(NULLIF(o.[SATIS_KONTAK_EPOSTA], ''), NULLIF(o.[EPOSTA], ''), u.[EPOSTA], 'partner@otelturizm.com'),
                   COALESCE(u.[AD_SOYAD], o.[SATIS_KONTAK_ADI], 'Partner Yetkilisi')
            FROM [dbo].[OTELLER] o
            LEFT JOIN [dbo].[OTEL_KULLANICI_SAHIPLIKLERI] oks ON oks.[OTEL_ID] = o.id AND oks.[AKTIF_MI] = 1
            LEFT JOIN [dbo].[KULLANICILAR] u ON u.id = COALESCE(o.[KULLANICI_ID], oks.[KULLANICI_ID])
            WHERE o.id = @hotelId
            ORDER BY oks.[ANA_SORUMLU_MU] DESC, oks.id ASC
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

    private async Task QueueReservationEmailsAsync(List<ReservationEmailJob> jobs, CancellationToken cancellationToken)
    {
        foreach (var job in jobs.Where(static x => !string.IsNullOrWhiteSpace(x.RecipientEmail)))
        {
            await _emailQueueService.QueueTemplateAsync(new QueuedEmailTemplateRequest
            {
                UserId = job.UserId,
                RecipientEmail = job.RecipientEmail,
                TemplateCode = job.TemplateCode,
                RelatedTable = "rezervasyonlar",
                RelatedRecordId = job.ReservationId,
                Tokens = job.Tokens
            }, cancellationToken);
        }
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
                COALESCE(kv.[KOMISYON_ORANI], o.[VARSAYILAN_KOMISYON_ORANI], 0),
                COALESCE(kv.[KOMISYON_GELIR_VERGISI_ORANI], 20),
                COALESCE(kv.[KDV_ORANI], 10),
                COALESCE(kv.[KONAKLAMA_VERGISI_ORANI], 2),
                COALESCE(kv.[PARA_BIRIMI], N'TRY')
            FROM [dbo].[ODA_TIPLERI] ot
            INNER JOIN [dbo].[OTELLER] o ON o.id = ot.[OTEL_ID]
            OUTER APPLY
            (
                SELECT TOP (1) *
                FROM [dbo].[KOMISYON_VERGILER] kv
                WHERE kv.[OTEL_ID] = o.id
                  AND kv.[AKTIF_MI] = 1
                  AND kv.[BASLANGIC_TARIHI] <= @effectiveDate
                  AND (kv.[BITIS_TARIHI] IS NULL OR kv.[BITIS_TARIHI] >= @effectiveDate)
                ORDER BY kv.[BASLANGIC_TARIHI] DESC, kv.id DESC
            ) kv
            WHERE ot.id = @roomTypeId;";

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@roomTypeId", roomTypeId);
        command.Parameters.AddWithValue("@effectiveDate", effectiveDate.ToDateTime(TimeOnly.MinValue));
        {
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
        }

        return await LoadFallbackCommissionRuleAsync(connection, roomTypeId, cancellationToken);
    }

    private static async Task<CommissionTaxRuleSnapshot> LoadFallbackCommissionRuleAsync(SqlConnection connection, long roomTypeId, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT TOP (1)
                o.id,
                COALESCE(o.[VARSAYILAN_KOMISYON_ORANI], 0)
            FROM [dbo].[ODA_TIPLERI] ot
            INNER JOIN [dbo].[OTELLER] o ON o.id = ot.[OTEL_ID]
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
        var taxDivisor = 1m + commissionRule.VatRate / 100m + commissionRule.AccommodationTaxRate / 100m;
        var netRoomAmount = taxDivisor > 0m
            ? Math.Round(roomTotal / taxDivisor, 2, MidpointRounding.AwayFromZero)
            : roomTotal;
        var vatAmount = Math.Round(netRoomAmount * commissionRule.VatRate / 100m, 2, MidpointRounding.AwayFromZero);
        var accommodationTaxAmount = Math.Max(0m, roomTotal - netRoomAmount - vatAmount);
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
            TotalAmount = roomTotal,
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
        await using var command = new SqlCommand(@"
            SELECT COUNT_BIG(*) + 1
            FROM [dbo].[REZERVASYONLAR] WITH (TABLOCKX, HOLDLOCK)
            WHERE CAST([OLUSTURULMA_TARIHI] AS date) = CAST(SYSUTCDATETIME() AS date);", connection, (SqlTransaction)transaction);
        var seq = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture);
        if (seq > 900000) throw new InvalidOperationException("Günlük rezervasyon numarası kapasitesi aşıldı.");

        var businessDate = DateTime.UtcNow;
        var daySeed = int.Parse(businessDate.ToString("MMddyy", CultureInfo.InvariantCulture), CultureInfo.InvariantCulture) % 900000;
        var mixedCode = ((seq * 7919) + daySeed) % 900000 + 100000;
        return $"WEB-{businessDate:yyMMdd}-{mixedCode:000000}";
    }

    private static string SplitFirstName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName)) return "Misafir";
        return fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "Misafir";
    }

    private sealed class ReservationPaymentPlan
    {
        public string AggregateOdemeDurumu { get; set; } = "Beklemede";
        public string LegacyOdemeYontemi { get; set; } = string.Empty;
        public decimal KapidaTutari { get; set; }
        public string KapidaDurumu { get; set; } = "Uygulanmiyor";
        public decimal OnlineTutari { get; set; }
        public string OnlineDurumu { get; set; } = "Uygulanmiyor";
        public decimal HavaleBekleyen { get; set; }
        public string? BankTransferReference { get; set; }
        public List<ReservationPaymentLine> Lines { get; set; } = new();
    }

    private sealed class ReservationPaymentLine
    {
        public string MethodKod { get; set; } = string.Empty;
        public decimal Tutar { get; set; }
        public string? HavaleReferans { get; set; }
    }

    private static bool NearlyEqualsMoney(decimal total, decimal sum)
        => Math.Abs(total - sum) <= 0.05m;

    private static bool TryBuildPaymentAllocation(PublicHotelReservationForm form, decimal totalAmount, out ReservationPaymentPlan plan, out string? errorMessage)
    {
        plan = new ReservationPaymentPlan();
        errorMessage = null;
        var pm = (form.PaymentMethod ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(pm))
        {
            errorMessage = "Lütfen ödeme yönteminizi seçin.";
            return false;
        }

        if (totalAmount <= 0)
        {
            errorMessage = "Toplam tutar hesaplanamadi.";
            return false;
        }

        static decimal R(decimal v) => decimal.Round(v, 2, MidpointRounding.AwayFromZero);

        var card = R(form.CardAmount);
        var bank = R(form.BankTransferAmount);
        var cashSplit = R(form.CashAtHotelAmountSplit);
        var bankRef = string.IsNullOrWhiteSpace(form.BankTransferReference) ? null : form.BankTransferReference.Trim();

        switch (pm)
        {
            case "Kapıda Ödeme":
                plan = BuildPlanCore(
                    legacyOdemeYontemi: "Kapıda Ödeme",
                    kapida: totalAmount,
                    online: 0,
                    havale: 0,
                    lines: new List<ReservationPaymentLine>
                    {
                        new() { MethodKod = OdemeYontemiKodlari.KapidaOdeme, Tutar = totalAmount }
                    });
                return true;

            case "Online Ödeme":
            case "Sanal POS":
                plan = BuildPlanCore(
                    legacyOdemeYontemi: "Sanal POS",
                    kapida: 0,
                    online: totalAmount,
                    havale: 0,
                    lines: new List<ReservationPaymentLine>
                    {
                        new() { MethodKod = OdemeYontemiKodlari.SanalPos, Tutar = totalAmount }
                    });
                return true;

            case "Havale/EFT":
                plan = BuildPlanCore(
                    legacyOdemeYontemi: "Havale/EFT",
                    kapida: 0,
                    online: 0,
                    havale: totalAmount,
                    lines: new List<ReservationPaymentLine>
                    {
                        new() { MethodKod = OdemeYontemiKodlari.HavaleEft, Tutar = totalAmount, HavaleReferans = bankRef }
                    });
                plan.BankTransferReference = bankRef;
                return true;

            case "Karma — Kart ve Havale":
                if (card <= 0 || bank <= 0)
                {
                    errorMessage = "Kart ve havale tutarlarini pozitif giriniz.";
                    return false;
                }

                if (!NearlyEqualsMoney(totalAmount, card + bank))
                {
                    errorMessage = $"Kart ({card:N2} TL) ve havale ({bank:N2} TL) toplami, toplam tutara ({totalAmount:N2} TL) esit olmalidir.";
                    return false;
                }

                plan = BuildPlanCore(
                    legacyOdemeYontemi: "Karma Ödeme",
                    kapida: 0,
                    online: card,
                    havale: bank,
                    lines: new List<ReservationPaymentLine>
                    {
                        new() { MethodKod = OdemeYontemiKodlari.SanalPos, Tutar = card },
                        new() { MethodKod = OdemeYontemiKodlari.HavaleEft, Tutar = bank, HavaleReferans = bankRef }
                    });
                plan.BankTransferReference = bankRef;
                return true;

            case "Karma — Kart ve Kapıda":
                if (card <= 0 || cashSplit <= 0)
                {
                    errorMessage = "Kart ve kapida odeme tutarlarini pozitif giriniz.";
                    return false;
                }

                if (!NearlyEqualsMoney(totalAmount, card + cashSplit))
                {
                    errorMessage = $"Kart ve kapida tutarlari toplami toplam tutara esit olmalidir (toplam {totalAmount:N2} TL).";
                    return false;
                }

                plan = BuildPlanCore(
                    legacyOdemeYontemi: "Karma Ödeme",
                    kapida: cashSplit,
                    online: card,
                    havale: 0,
                    lines: new List<ReservationPaymentLine>
                    {
                        new() { MethodKod = OdemeYontemiKodlari.SanalPos, Tutar = card },
                        new() { MethodKod = OdemeYontemiKodlari.KapidaOdeme, Tutar = cashSplit }
                    });
                return true;

            case "Karma — Havale ve Kapıda":
                if (bank <= 0 || cashSplit <= 0)
                {
                    errorMessage = "Havale ve kapida tutarlarini pozitif giriniz.";
                    return false;
                }

                if (!NearlyEqualsMoney(totalAmount, bank + cashSplit))
                {
                    errorMessage = $"Havale ve kapida tutarlari toplami toplam tutara esit olmalidir (toplam {totalAmount:N2} TL).";
                    return false;
                }

                plan = BuildPlanCore(
                    legacyOdemeYontemi: "Karma Ödeme",
                    kapida: cashSplit,
                    online: 0,
                    havale: bank,
                    lines: new List<ReservationPaymentLine>
                    {
                        new() { MethodKod = OdemeYontemiKodlari.HavaleEft, Tutar = bank, HavaleReferans = bankRef },
                        new() { MethodKod = OdemeYontemiKodlari.KapidaOdeme, Tutar = cashSplit }
                    });
                plan.BankTransferReference = bankRef;
                return true;

            case "Karma — Kart, Havale ve Kapıda":
                if (card <= 0 || bank <= 0 || cashSplit <= 0)
                {
                    errorMessage = "Uc yontem icin de pozitif tutar giriniz.";
                    return false;
                }

                if (!NearlyEqualsMoney(totalAmount, card + bank + cashSplit))
                {
                    errorMessage = "Kart, havale ve kapida tutarlari toplami toplam tutara esit olmalidir.";
                    return false;
                }

                plan = BuildPlanCore(
                    legacyOdemeYontemi: "Karma Ödeme",
                    kapida: cashSplit,
                    online: card,
                    havale: bank,
                    lines: new List<ReservationPaymentLine>
                    {
                        new() { MethodKod = OdemeYontemiKodlari.SanalPos, Tutar = card },
                        new() { MethodKod = OdemeYontemiKodlari.HavaleEft, Tutar = bank, HavaleReferans = bankRef },
                        new() { MethodKod = OdemeYontemiKodlari.KapidaOdeme, Tutar = cashSplit }
                    });
                plan.BankTransferReference = bankRef;
                return true;

            default:
                errorMessage = "Desteklenmeyen odeme secimi.";
                return false;
        }
    }

    private static ReservationPaymentPlan BuildPlanCore(
        string legacyOdemeYontemi,
        decimal kapida,
        decimal online,
        decimal havale,
        List<ReservationPaymentLine> lines)
    {
        var kapidaDurum = kapida > 0 ? "Odenmedi" : "Uygulanmiyor";
        var onlineDurum = online > 0 ? "Beklemede" : "Uygulanmiyor";
        return new ReservationPaymentPlan
        {
            AggregateOdemeDurumu = "Beklemede",
            LegacyOdemeYontemi = legacyOdemeYontemi,
            KapidaTutari = kapida,
            KapidaDurumu = kapidaDurum,
            OnlineTutari = online,
            OnlineDurumu = onlineDurum,
            HavaleBekleyen = havale,
            Lines = lines
        };
    }

    private async Task InsertReservationPaymentLinesAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        long reservationId,
        ReservationPaymentPlan plan,
        CancellationToken cancellationToken)
    {
        var insertSql = $@"
            INSERT INTO [dbo].[REZERVASYON_ODEME_KALEMLERI]
            ([REZERVASYON_ID], [ODEME_YONTEMI_ID], [ODEME_DURUMU_ID], [TUTAR], [SIRA_NO], [HAVALE_EFT_REFERANS])
            VALUES
            (
                @rezId,
                (SELECT TOP (1) id FROM [dbo].[ODEME_YONTEMI_TANIMLARI] WHERE kod = @methodKod),
                (SELECT TOP (1) id FROM [dbo].[ODEME_DURUMU_TANIMLARI] WHERE kod = N'{OdemeDurumuKodlari.Beklemede}'),
                @tutar,
                @sira,
                @ref
            );";

        var order = 1;
        foreach (var line in plan.Lines)
        {
            await using var cmd = new SqlCommand(insertSql, connection, transaction);
            cmd.Parameters.AddWithValue("@rezId", reservationId);
            cmd.Parameters.AddWithValue("@methodKod", line.MethodKod);
            cmd.Parameters.AddWithValue("@tutar", line.Tutar);
            cmd.Parameters.AddWithValue("@sira", order++);
            cmd.Parameters.AddWithValue("@ref", string.IsNullOrWhiteSpace(line.HavaleReferans) ? DBNull.Value : line.HavaleReferans);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private static async Task EnsureReservationHotelFavoriteAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        long userId,
        long hotelId,
        string hotelSlug,
        CancellationToken cancellationToken)
    {
        const string sql = @"
            IF EXISTS (
                SELECT 1
                FROM [dbo].[KULLANICI_FAVORI_OTELLER] WITH (UPDLOCK, HOLDLOCK)
                WHERE [KULLANICI_ID] = @userId AND [OTEL_ID] = @hotelId
            )
            BEGIN
                UPDATE [dbo].[KULLANICI_FAVORI_OTELLER]
                SET [KAYNAK_SAYFA] = N'rezervasyon',
                    [KAYNAK_URL] = @sourceUrl,
                    [AKTIF_MI] = 1,
                    [KALDIRILMA_TARIHI] = NULL,
                    [SON_ISLEM_TARIHI] = SYSUTCDATETIME()
                WHERE [KULLANICI_ID] = @userId AND [OTEL_ID] = @hotelId;
            END
            ELSE
            BEGIN
                INSERT INTO [dbo].[KULLANICI_FAVORI_OTELLER]
                ([KULLANICI_ID], [OTEL_ID], [KAYNAK_SAYFA], [KAYNAK_URL], [AKTIF_MI], [OLUSTURULMA_TARIHI], [SON_ISLEM_TARIHI])
                VALUES
                (@userId, @hotelId, N'rezervasyon', @sourceUrl, 1, SYSUTCDATETIME(), SYSUTCDATETIME());
            END;";

        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("@userId", userId);
        command.Parameters.AddWithValue("@hotelId", hotelId);
        command.Parameters.AddWithValue("@sourceUrl", string.IsNullOrWhiteSpace(hotelSlug) ? $"/oteller/{hotelId}" : $"/oteller/{hotelSlug}");
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task AttachBankTransferReceiptIfNeededAsync(
        long reservationId,
        long userId,
        IFormFile? receipt,
        ReservationPaymentPlan plan,
        CancellationToken cancellationToken)
    {
        if (receipt is null || receipt.Length <= 0 || plan.HavaleBekleyen <= 0)
        {
            return;
        }

        var stored = await _secureFileService.SaveAsync(
            receipt,
            new SecureFileSaveRequest
            {
                ContextTable = "rezervasyonlar",
                ContextId = reservationId,
                OwnerUserId = userId,
                Category = "rezervasyon-havale-dekont",
                VisibilityScope = "partner"
            },
            cancellationToken);

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        const string sql = @"
            UPDATE k
            SET k.[DEKONT_GUVENLI_DOSYA_ID] = @fileId
            FROM [dbo].[REZERVASYON_ODEME_KALEMLERI] k
            INNER JOIN [dbo].[ODEME_YONTEMI_TANIMLARI] y ON y.id = k.[ODEME_YONTEMI_ID]
            WHERE k.[REZERVASYON_ID] = @rezId
              AND y.[KOD] = @havaleKod;";
        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@fileId", stored.FileId);
        cmd.Parameters.AddWithValue("@rezId", reservationId);
        cmd.Parameters.AddWithValue("@havaleKod", OdemeYontemiKodlari.HavaleEft);
        try
        {
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (SqlException ex)
        {
            _logger.LogWarning(ex, "Dekont dosya baglantisi yazilamadi. FileId: {FileId}", stored.FileId);
        }
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
            GuestUlkeId = source.GuestUlkeId,
            GuestIlId = source.GuestIlId,
            GuestIlceId = source.GuestIlceId,
            GuestMahalleId = source.GuestMahalleId,
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
        public long? UlkeId { get; set; }
        public long? IlId { get; set; }
        public long? IlceId { get; set; }
        public long? MahalleId { get; set; }
        public DateTime? BirthDate { get; set; }
        public string Gender { get; set; } = string.Empty;
        public bool IsAgeEligible => BirthDate.HasValue && BirthDate.Value.Date <= DateTime.Today.AddYears(-18).AddDays(-1);
        public bool IsProfileComplete =>
            !string.IsNullOrWhiteSpace(FullName) &&
            !string.IsNullOrWhiteSpace(Email) &&
            !string.IsNullOrWhiteSpace(Phone) &&
            !string.IsNullOrWhiteSpace(City) &&
            !string.IsNullOrWhiteSpace(District) &&
            !string.IsNullOrWhiteSpace(Neighborhood) &&
            IsAgeEligible &&
            !string.IsNullOrWhiteSpace(Gender);
    }

    private sealed record ReservationEmailJob(
        long UserId,
        string RecipientEmail,
        string TemplateCode,
        long ReservationId,
        Dictionary<string, string> Tokens);

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
        public string City { get; set; } = string.Empty;
        public string District { get; set; } = string.Empty;
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
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
        public int DawnSurprisePercent { get; set; }
        public decimal DawnSurpriseDiscountAmount { get; set; }
        public decimal OriginalTotalBeforeDawn { get; set; }
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
