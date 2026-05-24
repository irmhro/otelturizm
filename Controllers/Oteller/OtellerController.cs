using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using System.Linq;
using System.Security.Claims;
using otelturizmnew.Constants;
using otelturizmnew.Models.Payments;
using otelturizmnew.Models.Paneller.User;
using otelturizmnew.Models.Reservations;
using otelturizmnew.Services;
using otelturizmnew.Services.Abstractions;
using otelturizmnew.Utils;

namespace otelturizmnew.Controllers.Oteller;

[Route("oteller")]
[Route("en/hotels")]
[Route("de/hotels")]
[Route("fr/hotels")]
[Route("es/hoteles")]
[Route("ru/oteli")]
public class OtellerController : Controller
{
    private const string ReservationDraftCookieName = "Otelturizm.ReservationDraftKey";
    private const string LocationSessionCookieName = "Otelturizm.LocationSession";
    private readonly IHotelService _hotelService;
    private readonly IPublicReservationService _publicReservationService;
    private readonly IWeatherService _weatherService;
    private readonly IUserFavoriteService _userFavoriteService;
    private readonly IUserPanelService _userPanelService;
    private readonly IMessageCenterService _messageCenterService;
    private readonly ILocationLogService _locationLogService;
    private readonly IIdempotencyService _idempotency;
    private readonly IMemoryCache _memoryCache;
    private readonly IPublicGrowthSignalsService _growthSignals;
    private readonly IReservationVelocityGuard _velocityGuard;
    private readonly HotelPresenceTracker _presenceTracker;
    private readonly InternationalSeoService _internationalSeo;
    private readonly ILogger<OtellerController> _logger;

    public OtellerController(
        IHotelService hotelService,
        IPublicReservationService publicReservationService,
        IWeatherService weatherService,
        IUserFavoriteService userFavoriteService,
        IUserPanelService userPanelService,
        IMessageCenterService messageCenterService,
        ILocationLogService locationLogService,
        IIdempotencyService idempotency,
        IMemoryCache memoryCache,
        IPublicGrowthSignalsService growthSignals,
        IReservationVelocityGuard velocityGuard,
        HotelPresenceTracker presenceTracker,
        InternationalSeoService internationalSeo,
        ILogger<OtellerController> logger)
    {
        _hotelService = hotelService;
        _publicReservationService = publicReservationService;
        _weatherService = weatherService;
        _userFavoriteService = userFavoriteService;
        _userPanelService = userPanelService;
        _messageCenterService = messageCenterService;
        _locationLogService = locationLogService;
        _idempotency = idempotency;
        _memoryCache = memoryCache;
        _growthSignals = growthSignals;
        _velocityGuard = velocityGuard;
        _presenceTracker = presenceTracker;
        _internationalSeo = internationalSeo;
        _logger = logger;
    }

    [HttpGet("")]
    [HttpGet("istanbul")]
    [OutputCache(PolicyName = "public-short")]
    public async Task<IActionResult> OtelListeleme([FromQuery] string? q, [FromQuery] string? city, [FromQuery] string? etiket, [FromQuery] string? filter, [FromQuery] string? kampanya, [FromQuery] int page, CancellationToken cancellationToken)
    {
        var listingCulture = ApplyRouteListingCulture();
        ViewData["PageCss"] = "otel-listeleme";
        ViewData["PageCssMobile"] = "otel-listeleme.mobile";
        var searchTermRaw = !string.IsNullOrWhiteSpace(q) ? q : city;
        var searchTerm = SearchTextNormalizer.Normalize(searchTermRaw);
        var etiketN = HotelService.ResolveListingCampaignTag(etiket, filter);
        var kampanyaN = SearchTextNormalizer.Normalize(kampanya);
        var ctxBoost = Request.Cookies.TryGetValue("Otelturizm.SearchCtx", out var cx) ? cx.ToString() : null;
        var model = await _hotelService.GetHotelListingPageAsync(searchTerm, etiketN, kampanyaN, page <= 0 ? 1 : page, ctxBoost, cancellationToken);
        await ApplyFavoriteStatesAsync(model, cancellationToken);
        ApplyListingLoyaltyTouchpoints(model);
        var listingMeta = _internationalSeo.BuildListingMeta(
            listingCulture,
            ResolveListingCitySlug(searchTerm, model.City, model.SearchLabel),
            model.TotalCount,
            page <= 0 ? 1 : page);
        ViewData["Title"] = string.IsNullOrWhiteSpace(model.CampaignTitle) ? listingMeta.Title : model.CampaignTitle;
        ViewData["MetaDescription"] = listingMeta.Description;
        ApplyListingSeoViewData(searchTerm, etiketN, kampanyaN, page <= 0 ? 1 : page, listingCulture);
        return View("~/Views/Oteller/OtelListeleme.cshtml", model);
    }

    [HttpGet("harita")]
    [OutputCache(PolicyName = "public-short")]
    public async Task<IActionResult> HaritaOteller([FromQuery] string? q, [FromQuery] string? city, [FromQuery] string? etiket, [FromQuery] string? filter, [FromQuery] string? kampanya, CancellationToken cancellationToken)
    {
        ViewData["PageCss"] = "haritaoteller";
        ViewData["PageCssMobile"] = "haritaoteller.mobile";
        var searchTermRaw = !string.IsNullOrWhiteSpace(q) ? q : city;
        var searchTerm = SearchTextNormalizer.Normalize(searchTermRaw);
        var etiketN = HotelService.ResolveListingCampaignTag(etiket, filter);
        var kampanyaN = SearchTextNormalizer.Normalize(kampanya);
        var ctxBoost = Request.Cookies.TryGetValue("Otelturizm.SearchCtx", out var cx) ? cx.ToString() : null;
        var model = await _hotelService.GetHotelListingPageAsync(searchTerm, etiketN, kampanyaN, 1, ctxBoost, cancellationToken);
        await ApplyFavoriteStatesAsync(model, cancellationToken);
        var mapCulture = ApplyRouteListingCulture();
        ViewData["Title"] = string.IsNullOrWhiteSpace(model.SearchLabel)
            ? (mapCulture == "en" ? "Hotels on map" : mapCulture == "de" ? "Hotels auf der Karte" : "Haritada Oteller")
            : (mapCulture == "en" ? $"{model.SearchLabel} hotels on map" : mapCulture == "de" ? $"{model.SearchLabel} auf der Karte" : $"{model.SearchLabel} haritası");
        ApplyListingSeoViewData(searchTerm, etiketN, kampanyaN, 1, mapCulture, forceNoIndex: true);
        return View("~/Views/Oteller/HaritaOteller.cshtml", model);
    }

    [HttpGet("{slug}")]
    [OutputCache(PolicyName = "public-short")]
    public async Task<IActionResult> OtelDetay(string slug, CancellationToken cancellationToken)
    {
        var model = await _hotelService.GetHotelDetailPageAsync(slug, cancellationToken);
        if (model is null)
        {
            return NotFound();
        }

        model.Weather = await _weatherService.GetForecastAsync(model.District, model.City, model.Latitude, model.Longitude, cancellationToken);
        await ApplyFavoriteStateAsync(model, cancellationToken);

        Response.Cookies.Append(
            "Otelturizm.SearchCtx",
            model.City.Trim(),
            new CookieOptions
            {
                HttpOnly = false,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                MaxAge = TimeSpan.FromDays(30),
                Path = "/",
                IsEssential = true
            });

        model.ActiveViewerBand = _growthSignals.GetActiveViewerBand(model.Id);
        model.LivePresenceCount = _presenceTracker.GetActiveCount(model.Id);

        if (Request.Query.TryGetValue("trip", out var tripRaw))
        {
            var trip = tripRaw.ToString().Trim().ToLowerInvariant();
            model.IntentSegmentLabel = trip switch
            {
                "business" or "is" => "İş seyahati",
                "honeymoon" or "balayi" => "Balayı",
                "family" or "aile" => "Aile tatili",
                _ => string.Empty
            };
            if (!string.IsNullOrWhiteSpace(model.IntentSegmentLabel))
            {
                Response.Cookies.Append(
                    "Otelturizm.UserIntent",
                    trip,
                    new CookieOptions
                    {
                        HttpOnly = false,
                        Secure = Request.IsHttps,
                        SameSite = SameSiteMode.Lax,
                        MaxAge = TimeSpan.FromDays(90),
                        Path = "/",
                        IsEssential = false
                    });
            }
        }
        else if (Request.Cookies.TryGetValue("Otelturizm.UserIntent", out var intentCookie))
        {
            var t = intentCookie.ToString().Trim().ToLowerInvariant();
            model.IntentSegmentLabel = t switch
            {
                "business" or "is" => "İş seyahati",
                "honeymoon" or "balayi" => "Balayı",
                "family" or "aile" => "Aile tatili",
                _ => string.Empty
            };
        }

        var activeDraft = await _publicReservationService.GetActiveDraftAsync(GetCurrentUserIdOrNull(), GetCurrentReservationSessionKey(), cancellationToken);
        model.ActiveDraft = activeDraft;
        model.ReservationForm = new PublicHotelReservationForm
        {
            HotelId = model.Id,
            RoomTypeId = model.Rooms.FirstOrDefault()?.RoomTypeId ?? 0,
            PaymentMethod = string.Empty
        };
        if (activeDraft is not null && activeDraft.HotelId == model.Id)
        {
            model.ReservationForm.CheckInDate = activeDraft.CheckInDate;
            model.ReservationForm.CheckOutDate = activeDraft.CheckOutDate;
            model.ReservationForm.RoomTypeId = activeDraft.RoomTypeId ?? model.ReservationForm.RoomTypeId;
            model.ReservationForm.AdultCount = activeDraft.AdultCount;
            model.ReservationForm.ChildCount = activeDraft.ChildCount;
            model.ReservationForm.RoomCount = activeDraft.RoomCount;
        }

        model.IsLoggedInUser = CanAccessUserFeatures();
        model.ShouldResumeDraftOnLoad = Request.Query.TryGetValue("continueDraft", out var continueDraft) && string.Equals(continueDraft.ToString(), "1", StringComparison.OrdinalIgnoreCase);
        if (model.IsLoggedInUser)
        {
            var userId = GetCurrentUserId();
            if (userId > 0)
            {
                model.ProfilePrompt = await BuildProfilePromptAsync(userId, $"/oteller/{slug}?continueDraft=1", cancellationToken);
                var conversationAccess = await _messageCenterService.CanStartHotelConversationAsync(userId, model.Id, cancellationToken);
                model.HasCompletedReservationAtHotel = conversationAccess.Allowed;
                model.ConversationInfoMessage = conversationAccess.Message;
                model.EligibleReviewStays = (await _userPanelService.GetEligibleReviewStaysForHotelAsync(userId, model.Id, cancellationToken)).ToList();
            }
        }

        var detailCulture = ApplyRouteListingCulture();
        var detailMeta = _internationalSeo.BuildHotelDetailMeta(detailCulture, model.Name, model.City);
        ViewData["Title"] = detailMeta.Title;
        ViewData["MetaDescription"] = detailMeta.Description;
        ViewData["PageCss"] = "paneller/otel/otel-detay";
        ViewData["PageCssMobile"] = "paneller/otel/otel-detay.mobile";
        ViewData["SuppressGlobalDraftBanner"] = true;
        ApplyDetailSeoViewData(slug, detailCulture);
        return View("~/Views/Oteller/OtelDetay.cshtml", model);
    }

    [HttpPost("{slug}/rezervasyon")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("reservation-create")]
    [RequestFormLimits(MultipartBodyLengthLimit = 31457280)]
    [RequestSizeLimit(31457280)]
    public async Task<IActionResult> StartReservation(string slug, PublicHotelReservationForm form, IFormFile? BankTransferReceipt, CancellationToken cancellationToken)
    {
        try
        {
            var validationError = ValidateReservationStartForm(form);
            if (!string.IsNullOrWhiteSpace(validationError))
            {
                TempData["PublicReservationInfo"] = validationError;
                return Redirect($"/oteller/{slug}");
            }

            if (!_velocityGuard.TryAllowReservationAttempt(HttpContext, out var velocityMsg))
            {
                TempData["PublicReservationInfo"] = velocityMsg;
                _logger.LogWarning(
                    "RESERVATION_VELOCITY_BLOCK slug={Slug} ip={Ip}",
                    slug,
                    HttpContext.Connection.RemoteIpAddress?.ToString());
                return Redirect($"/oteller/{slug}");
            }

            var risk = _velocityGuard.ComputeRiskScore01(HttpContext);
            _logger.LogInformation(
                "RESERVATION_RISK_SCORE score={Score} slug={Slug}",
                risk,
                slug);

            var userId = GetCurrentUserIdOrNull();
            var sessionKey = EnsureReservationSessionKey();
            var idemKey = IdempotencyKey.ForObject(
                $"public-res-start:{userId?.ToString() ?? "guest"}:{sessionKey}",
                new
                {
                    form.HotelId,
                    form.RoomTypeId,
                    form.CheckInDate,
                    form.CheckOutDate,
                    form.AdultCount,
                    form.ChildCount,
                    form.RoomCount,
                    form.PaymentMethod,
                    form.CardAmount,
                    form.BankTransferAmount,
                    form.CashAtHotelAmountSplit,
                    form.BankTransferReference,
                    form.RoomsJson
                });

            var result = await _idempotency.GetOrCreateAsync(
                idemKey,
                async ct => await _publicReservationService.StartReservationAsync(userId, sessionKey, form, BankTransferReceipt, ct),
                ttl: TimeSpan.FromSeconds(25),
                cancellationToken: cancellationToken);

            TempData[result.Success ? "PublicReservationSuccess" : "PublicReservationInfo"] = result.Message;
            if (!string.IsNullOrWhiteSpace(result.RedirectUrl))
            {
                return Redirect(result.RedirectUrl);
            }

            return Redirect($"/oteller/{slug}");
        }
        catch (Exception ex)
        {
            var correlationId = HttpContext.Items.TryGetValue("CorrelationId", out var cidObj) ? cidObj as string : null;
            _logger.LogError(ex, "Public reservation start failed. cid={CorrelationId} slug={Slug}", correlationId, slug);
            TempData["PublicReservationInfo"] = "Rezervasyon başlatılamadı. Lütfen tekrar deneyin."
                                               + (string.IsNullOrWhiteSpace(correlationId) ? string.Empty : $" (Takip: {correlationId})");
            return Redirect($"/oteller/{slug}");
        }
    }

    [HttpGet("{slug}/rezervasyon")]
    public IActionResult ReservationGetFallback(string slug)
    {
        return Redirect($"/oteller/{slug}");
    }

    [HttpPost("{slug}/profil-bilgilerini-tamamla")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CompleteProfileInline(string slug, [FromForm] UserProfileForm form, CancellationToken cancellationToken)
    {
        if (!CanAccessUserFeatures())
        {
            return Unauthorized(new { success = false, message = "Profil bilgilerini güncellemek için önce giriş yapmalısınız." });
        }

        var existingProfile = await _userPanelService.GetProfileAsync(GetCurrentUserId(), cancellationToken);
        form.FirstName = string.IsNullOrWhiteSpace(form.FirstName) ? existingProfile.Form.FirstName : form.FirstName;
        form.LastName = string.IsNullOrWhiteSpace(form.LastName) ? existingProfile.Form.LastName : form.LastName;
        form.IdentityNumber = string.IsNullOrWhiteSpace(form.IdentityNumber) ? existingProfile.Form.IdentityNumber : form.IdentityNumber;
        form.PostalCode = string.IsNullOrWhiteSpace(form.PostalCode) ? existingProfile.Form.PostalCode : form.PostalCode;
        form.RoomPreference = string.IsNullOrWhiteSpace(form.RoomPreference) ? existingProfile.Form.RoomPreference : form.RoomPreference;
        form.BedPreference = string.IsNullOrWhiteSpace(form.BedPreference) ? existingProfile.Form.BedPreference : form.BedPreference;
        form.SpokenLanguages = string.IsNullOrWhiteSpace(form.SpokenLanguages) ? existingProfile.Form.SpokenLanguages : form.SpokenLanguages;
        form.TravelPurpose = string.IsNullOrWhiteSpace(form.TravelPurpose) ? existingProfile.Form.TravelPurpose : form.TravelPurpose;
        form.SpecialRequests = string.IsNullOrWhiteSpace(form.SpecialRequests) ? existingProfile.Form.SpecialRequests : form.SpecialRequests;
        form.Phone = string.IsNullOrWhiteSpace(form.Phone) ? existingProfile.Form.Phone : form.Phone;

        var validationMessage = ValidateReservationProfile(form);
        if (!string.IsNullOrWhiteSpace(validationMessage))
        {
            return BadRequest(new { success = false, message = validationMessage });
        }

        var saved = await _userPanelService.SaveProfileAsync(GetCurrentUserId(), form, cancellationToken);
        if (!saved)
        {
            return BadRequest(new
            {
                success = false,
                message = "Profil bilgileri kaydedilemedi. E-posta zaten kullanimda olabilir veya zorunlu alanlar eksik olabilir."
            });
        }

        return Json(new
        {
            success = true,
            message = "Profil bilgileriniz güncellendi. Rezervasyona devam edebilirsiniz."
        });
    }

    [HttpPost("{slug}/gorusme-baslat")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StartConversation(string slug, CancellationToken cancellationToken)
    {
        if (!CanAccessUserFeatures())
        {
            return Unauthorized(new { success = false, message = "Görüşme başlatmak için kullanıcı hesabınızla giriş yapmalısınız." });
        }

        var hotel = await _hotelService.GetHotelDetailPageAsync(slug, cancellationToken);
        if (hotel is null)
        {
            return NotFound(new { success = false, message = "Otel bulunamadı." });
        }

        var result = await _messageCenterService.StartHotelConversationForUserAsync(GetCurrentUserId(), hotel.Id, cancellationToken);
        if (!result.Success || !result.ConversationId.HasValue)
        {
            return BadRequest(new { success = false, message = result.Message });
        }

        return Json(new
        {
            success = true,
            message = result.Message,
            redirectUrl = $"/panel/user/mesajlarim?conversationId={result.ConversationId.Value}"
        });
    }

    [HttpGet("{slug}/fiyat-teklifi")]
    [EnableRateLimiting("quote-strict")]
    public async Task<IActionResult> GetPriceQuote(string slug, [FromQuery] long roomTypeId, [FromQuery] DateOnly checkInDate, [FromQuery] DateOnly checkOutDate, [FromQuery] int roomCount = 1, CancellationToken cancellationToken = default)
    {
        if (roomCount < 1 || roomCount > 50)
        {
            return BadRequest(CheckoutErrorCatalog.JsonError(CheckoutErrorCatalog.InvalidRoomCount, "Oda adedi 1-50 aralığında olmalıdır."));
        }
        if (checkOutDate <= checkInDate)
        {
            return BadRequest(CheckoutErrorCatalog.JsonError(CheckoutErrorCatalog.InvalidDateRange, "Çıkış tarihi giriş tarihinden sonra olmalıdır."));
        }
        var nightCount = checkOutDate.DayNumber - checkInDate.DayNumber;
        if (nightCount <= 0 || nightCount > 60)
        {
            return BadRequest(CheckoutErrorCatalog.JsonError(CheckoutErrorCatalog.NightRange, "Konaklama süresi 1-60 gece aralığında olmalıdır."));
        }

        var hotel = await _hotelService.GetHotelDetailPageAsync(slug, cancellationToken);
        if (hotel is null || roomTypeId <= 0)
        {
            return BadRequest(CheckoutErrorCatalog.JsonError(CheckoutErrorCatalog.HotelNotFound, "Fiyat özeti hesaplanamadı."));
        }

        if (!hotel.Rooms.Any(r => r.RoomTypeId == roomTypeId))
        {
            return BadRequest(CheckoutErrorCatalog.JsonError(CheckoutErrorCatalog.RoomMismatch, "Seçilen oda tipi bu otel ile eşleşmiyor."));
        }

        var shieldKey = $"currency-shield:v1:{slug}:{roomTypeId}:{checkInDate:O}:{checkOutDate:O}:{roomCount}";
        if (_memoryCache.TryGetValue(shieldKey, out PublicReservationPriceQuoteViewModel? cachedQuote) && cachedQuote is not null)
        {
            _logger.LogInformation("QUOTE_SHIELD_HIT slug={Slug} roomTypeId={RoomTypeId}", slug, roomTypeId);
            return Json(BuildPriceQuotePayload(cachedQuote));
        }

        var quote = await _publicReservationService.GetPriceQuoteAsync(roomTypeId, checkInDate, checkOutDate, Math.Max(1, roomCount), cancellationToken);
        _memoryCache.Set(
            shieldKey,
            quote,
            new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(22) });

        var correlationId = HttpContext.Items.TryGetValue("CorrelationId", out var cidObj) ? cidObj as string : null;
        _logger.LogInformation("QUOTE cid={CorrelationId} slug={Slug} roomTypeId={RoomTypeId} in={CheckIn} out={CheckOut} rooms={RoomCount} ok={Ok}",
            correlationId, slug, roomTypeId, checkInDate, checkOutDate, roomCount, quote.IsAvailable);

        var bucketTry = (int)(decimal.Round(quote.TotalAmount / 500m, 0, MidpointRounding.AwayFromZero) * 500m);
        _logger.LogInformation("PRICE_ELASTICITY slug={Slug} total_bucket_try={BucketTry} nights={Nights} available={Available}",
            slug, bucketTry, nightCount, quote.IsAvailable);

        return Json(BuildPriceQuotePayload(quote));
    }

    private static object BuildPriceQuotePayload(PublicReservationPriceQuoteViewModel quote)
    {
        if (!quote.IsAvailable)
        {
            return new
            {
                success = false,
                errorCode = CheckoutErrorCatalog.QuoteFailed,
                message = string.IsNullOrWhiteSpace(quote.AvailabilityMessage)
                    ? "Seçilen tarih aralığında uygun müsaitlik bulunamadı."
                    : quote.AvailabilityMessage,
                isAvailable = false,
                nightCount = quote.NightCount
            };
        }

        return new
        {
            success = true,
            isAvailable = quote.IsAvailable,
            availabilityMessage = quote.AvailabilityMessage,
            nightCount = quote.NightCount,
            nightlyPrice = quote.NightlyPrice,
            netRoomAmount = quote.NetRoomAmount,
            vatRate = quote.VatRate,
            vatAmount = quote.VatAmount,
            accommodationTaxRate = quote.AccommodationTaxRate,
            accommodationTaxAmount = quote.AccommodationTaxAmount,
            roomTotal = quote.RoomTotal,
            taxAmount = quote.TaxAmount,
            totalAmount = quote.TotalAmount,
            nightlyBreakdown = quote.NightlyBreakdown.Select(item => new
            {
                date = item.Date.ToString("yyyy-MM-dd"),
                dateText = item.DateText,
                price = item.Price,
                basePrice = item.BasePrice,
                discountPrice = item.DiscountPrice,
                priceText = item.PriceText,
                isDiscounted = item.IsDiscounted,
                isClosed = item.IsClosed,
                remainingRooms = item.RemainingRooms
            })
        };
    }

    [HttpPost("konum-kaydet")]
    [IgnoreAntiforgeryToken]
    [EnableRateLimiting("location-strict")]
    [RequestSizeLimit(32 * 1024)]
    public async Task<IActionResult> SaveUserLocation([FromBody] UserLocationLogRequest? request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(new { success = false, message = "Konum bilgisi bulunamadı." });
        }

        if (request.Latitude < -90m || request.Latitude > 90m || request.Longitude < -180m || request.Longitude > 180m)
        {
            return BadRequest(new { success = false, message = "Geçersiz koordinat bilgisi." });
        }

        // Abuse guard: log şişmesini önle
        if (request.RadiusKm < 0m || request.RadiusKm > 250m)
        {
            return BadRequest(new { success = false, message = "Geçersiz arama yarıçapı." });
        }

        if (request.VisibleHotelCount < 0 || request.VisibleHotelCount > 5000)
        {
            return BadRequest(new { success = false, message = "Geçersiz liste sayısı." });
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm) && request.SearchTerm.Length > 120)
        {
            request.SearchTerm = request.SearchTerm[..120];
        }

        if (!string.IsNullOrWhiteSpace(request.ListedHotelIds) && request.ListedHotelIds.Length > 4000)
        {
            request.ListedHotelIds = request.ListedHotelIds[..4000];
        }

        var userAgent = Request.Headers.UserAgent.ToString();
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var deviceInfo = ParseDeviceInfo(userAgent);
        try
        {
            await _locationLogService.SaveUserLocationAsync(new LocationLogEntryInput
            {
                UserId = GetCurrentUserIdOrNull(),
                SessionKey = EnsureLocationSessionKey(),
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                RadiusKm = request.RadiusKm,
                VisibleHotelCount = request.VisibleHotelCount,
                ListedHotelIds = request.ListedHotelIds ?? string.Empty,
                SearchTerm = request.SearchTerm ?? string.Empty,
                SearchRegion = request.SearchRegion ?? string.Empty,
                Source = string.IsNullOrWhiteSpace(request.Source) ? "otel-listeleme" : request.Source.Trim(),
                UserAgent = userAgent,
                IpAddress = ipAddress ?? string.Empty,
                DeviceType = deviceInfo.DeviceType,
                DeviceModel = deviceInfo.DeviceModel,
                Platform = deviceInfo.Platform,
                Browser = deviceInfo.Browser,
                PhoneHint = deviceInfo.PhoneHint,
                PageUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}{Request.Path}{Request.QueryString}"
            }, cancellationToken);

            return Json(new { success = true });
        }
        catch
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { success = false });
        }
    }

    private string EnsureLocationSessionKey()
    {
        if (Request.Cookies.TryGetValue(LocationSessionCookieName, out var existing) && !string.IsNullOrWhiteSpace(existing))
        {
            return existing;
        }

        var key = Guid.NewGuid().ToString("N");
        Response.Cookies.Append(LocationSessionCookieName, key, new CookieOptions
        {
            HttpOnly = true,
            IsEssential = true,
            SameSite = SameSiteMode.Lax,
            Secure = Request.IsHttps,
            Expires = DateTimeOffset.UtcNow.AddYears(1)
        });
        return key;
    }

    private static (string DeviceType, string DeviceModel, string Platform, string Browser, string PhoneHint) ParseDeviceInfo(string? userAgent)
    {
        var ua = userAgent ?? string.Empty;
        var lower = ua.ToLowerInvariant();

        var platform = lower.Contains("android") ? "Android"
            : lower.Contains("iphone") || lower.Contains("ipad") || lower.Contains("ios") ? "iOS"
            : lower.Contains("windows") ? "Windows"
            : lower.Contains("mac os") || lower.Contains("macintosh") ? "macOS"
            : lower.Contains("linux") ? "Linux"
            : "Bilinmiyor";

        var browser = lower.Contains("edg/") ? "Edge"
            : lower.Contains("opr/") || lower.Contains("opera") ? "Opera"
            : lower.Contains("samsungbrowser") ? "Samsung Internet"
            : lower.Contains("chrome/") ? "Chrome"
            : lower.Contains("firefox/") ? "Firefox"
            : lower.Contains("safari/") ? "Safari"
            : "Bilinmiyor";

        var deviceType = lower.Contains("mobile") || lower.Contains("iphone") || lower.Contains("android")
            ? "Mobil"
            : lower.Contains("ipad") || lower.Contains("tablet")
                ? "Tablet"
                : "Masaustu";

        var deviceModel = lower.Contains("iphone") ? "iPhone"
            : lower.Contains("ipad") ? "iPad"
            : lower.Contains("samsung") ? "Samsung"
            : lower.Contains("huawei") ? "Huawei"
            : lower.Contains("redmi") ? "Redmi"
            : lower.Contains("xiaomi") ? "Xiaomi"
            : lower.Contains("poco") ? "Poco"
            : lower.Contains("oppo") ? "Oppo"
            : lower.Contains("vivo") ? "Vivo"
            : lower.Contains("windows") ? "Windows PC"
            : lower.Contains("macintosh") || lower.Contains("mac os") ? "Mac"
            : lower.Contains("android") ? "Android Cihaz"
            : "Bilinmiyor";

        var phoneHint = deviceType == "Mobil" || deviceType == "Tablet"
            ? $"{platform} · {deviceModel}"
            : string.Empty;

        return (deviceType, deviceModel, platform, browser, phoneHint);
    }

    private string ApplyRouteListingCulture()
    {
        var culture = _internationalSeo.ResolveCultureFromPath(Request.Path);
        ViewData["SeoCulture"] = culture;
        var cultureInfo = _internationalSeo.ResolveRequestCulture(culture);
        var requestCulture = new Microsoft.AspNetCore.Localization.RequestCulture(cultureInfo, cultureInfo);
        System.Globalization.CultureInfo.CurrentCulture = cultureInfo;
        System.Globalization.CultureInfo.CurrentUICulture = cultureInfo;
        HttpContext.Features.Set<Microsoft.AspNetCore.Localization.IRequestCultureFeature>(
            new Microsoft.AspNetCore.Localization.RequestCultureFeature(requestCulture, provider: null));
        return culture;
    }

    private static string? ResolveListingCitySlug(string? searchTerm, string city, string searchLabel)
    {
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            return searchTerm.Trim().ToLowerInvariant();
        }

        if (!string.IsNullOrWhiteSpace(city))
        {
            return city.Trim().ToLowerInvariant();
        }

        if (!string.IsNullOrWhiteSpace(searchLabel))
        {
            return searchLabel.Trim().ToLowerInvariant().Replace(" ", "-");
        }

        return null;
    }

    private void ApplyListingSeoViewData(
        string? normalizedSearchTerm,
        string normalizedTag,
        string normalizedCampaignSlug,
        int currentPage,
        string? listingCulture = null,
        bool forceNoIndex = false)
    {
        var configuration = HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var publicBase = (configuration["App:PublicBaseUrl"] ?? $"{Request.Scheme}://{Request.Host}").TrimEnd('/');
        var culture = listingCulture ?? _internationalSeo.ResolveCultureFromPath(Request.Path);
        var seo = HotelListingSeo.Build(
            publicBase,
            Request.Path,
            normalizedSearchTerm,
            normalizedTag,
            normalizedCampaignSlug,
            currentPage,
            Request.Query,
            culture);
        ViewData["Canonical"] = seo.Canonical;
        ViewData["HreflangAlternates"] = _internationalSeo.BuildHreflangAlternatesFromRequest(publicBase, Request.Path, Request.Query);
        if (forceNoIndex || !string.IsNullOrWhiteSpace(seo.Robots))
        {
            ViewData["Robots"] = forceNoIndex ? "noindex, follow" : seo.Robots;
        }
    }

    private void ApplyDetailSeoViewData(string hotelSlug, string culture)
    {
        var configuration = HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var publicBase = (configuration["App:PublicBaseUrl"] ?? $"{Request.Scheme}://{Request.Host}").TrimEnd('/');
        var canonicalPath = InternationalSeoPaths.BuildPublicPath(culture, InternationalSeoPaths.PageKindDetail, citySlug: null, hotelSlug);
        ViewData["Canonical"] = publicBase + canonicalPath;
        ViewData["HreflangAlternates"] = _internationalSeo.BuildHreflangAlternates(
            publicBase,
            InternationalSeoPaths.PageKindDetail,
            citySlug: null,
            hotelSlug);
    }

    private async Task ApplyFavoriteStatesAsync(otelturizmnew.Models.Oteller.HotelListingPageViewModel model, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId <= 0)
        {
            return;
        }

        var favoriteIds = await _userFavoriteService.GetFavoriteHotelIdsAsync(userId, model.Hotels.Select(x => x.Id), cancellationToken);
        foreach (var hotel in model.Hotels)
        {
            hotel.IsFavorite = favoriteIds.Contains(hotel.Id);
        }
    }

    private void ApplyListingLoyaltyTouchpoints(otelturizmnew.Models.Oteller.HotelListingPageViewModel model)
    {
        model.LoyaltyUserSignedIn = GetCurrentUserId() > 0;
        model.ShowLoyaltyTouchpoints = true;
        foreach (var hotel in model.Hotels)
        {
            var nightly = hotel.DiscountedPrice ?? hotel.StartingPrice;
            hotel.EstimatedLoyaltyPoints = otelturizmnew.Utils.LoyaltyPointsEstimator.EstimateFromNightlyPrice(nightly);
        }
    }

    private static string? ValidateReservationStartForm(PublicHotelReservationForm form)
    {
        if (form.HotelId <= 0) return "Otel seçimi zorunludur.";
        if (form.RoomTypeId <= 0 && string.IsNullOrWhiteSpace(form.RoomsJson)) return "Oda tipi seçimi zorunludur.";
        if (form.CheckOutDate <= form.CheckInDate) return "Çıkış tarihi giriş tarihinden sonra olmalıdır.";
        if (form.AdultCount < 1 || form.AdultCount > 12) return "Yetişkin sayısı geçersiz.";
        if (form.ChildCount < 0 || form.ChildCount > 10) return "Çocuk sayısı geçersiz.";
        if (form.RoomCount < 1 || form.RoomCount > 50) return "Oda sayısı geçersiz.";
        var nightCount = form.CheckOutDate.DayNumber - form.CheckInDate.DayNumber;
        if (nightCount <= 0 || nightCount > 60) return "Konaklama süresi 1-60 gece aralığında olmalıdır.";
        return null;
    }

    private async Task ApplyFavoriteStateAsync(otelturizmnew.Models.Oteller.HotelDetailPageViewModel model, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId <= 0)
        {
            return;
        }

        var favoriteIds = await _userFavoriteService.GetFavoriteHotelIdsAsync(userId, new[] { model.Id }, cancellationToken);
        model.IsFavorite = favoriteIds.Contains(model.Id);
    }

    private long GetCurrentUserId()
    {
        var raw = User.FindFirstValue(AuthClaimTypes.UserId) ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return long.TryParse(raw, out var userId) ? userId : 0;
    }

    private long? GetCurrentUserIdOrNull()
    {
        var userId = GetCurrentUserId();
        return userId > 0 ? userId : null;
    }

    private bool CanAccessUserFeatures()
    {
        var accountType = User.FindFirstValue(AuthClaimTypes.AccountType);
        return string.Equals(accountType, "user", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<otelturizmnew.Models.Oteller.HotelProfileCompletionPromptViewModel> BuildProfilePromptAsync(long userId, string returnUrl, CancellationToken cancellationToken)
    {
        var profile = await _userPanelService.GetProfileAsync(userId, cancellationToken);
        var isAdultEligible = IsReservationAdultEligible(profile.Form.BirthDateText);
        return new otelturizmnew.Models.Oteller.HotelProfileCompletionPromptViewModel
        {
            ReturnUrl = returnUrl,
            FirstName = profile.Form.FirstName,
            LastName = profile.Form.LastName,
            Email = profile.Form.Email,
            Phone = profile.Form.Phone,
            BirthDateText = profile.Form.BirthDateText,
            Gender = profile.Form.Gender,
            Nationality = profile.Form.Nationality,
            City = profile.Form.City,
            District = profile.Form.District,
            Neighborhood = profile.Form.Neighborhood,
            Address = profile.Form.Address,
            IsProfileIncomplete = string.IsNullOrWhiteSpace(profile.Form.FirstName)
                                || string.IsNullOrWhiteSpace(profile.Form.LastName)
                                || string.IsNullOrWhiteSpace(profile.Form.Email)
                                || string.IsNullOrWhiteSpace(profile.Form.Phone)
                                || string.IsNullOrWhiteSpace(profile.Form.BirthDateText)
                                || !isAdultEligible
                                || string.IsNullOrWhiteSpace(profile.Form.Gender)
                                || string.IsNullOrWhiteSpace(profile.Form.City)
                                || string.IsNullOrWhiteSpace(profile.Form.District)
                                || string.IsNullOrWhiteSpace(profile.Form.Neighborhood)
        };
    }

    private static string? ValidateReservationProfile(UserProfileForm form)
    {
        if (string.IsNullOrWhiteSpace(form.FirstName))
        {
            return "Rezervasyon için ad alanı zorunludur.";
        }

        if (string.IsNullOrWhiteSpace(form.LastName))
        {
            return "Rezervasyon için soyad alanı zorunludur.";
        }

        if (string.IsNullOrWhiteSpace(form.Email))
        {
            return "Rezervasyon için e-posta alanı zorunludur.";
        }

        if (string.IsNullOrWhiteSpace(form.Phone))
        {
            return "Rezervasyon için telefon numarası zorunludur.";
        }

        if (string.IsNullOrWhiteSpace(form.BirthDateText))
        {
            return "Rezervasyon için doğum tarihi zorunludur.";
        }

        if (!TryParseBirthDate(form.BirthDateText, out var birthDate))
        {
            return "Doğum tarihi geçerli bir formatta girilmelidir.";
        }

        if (!IsReservationAdultEligible(birthDate))
        {
            return "Rezervasyon oluşturabilmek için 18 yaşını doldurmuş olmanız ve doğum gününüzün üzerinden en az 1 gün geçmiş olması gerekir.";
        }

        if (string.IsNullOrWhiteSpace(form.Gender))
        {
            return "Rezervasyon için cinsiyet seçimi zorunludur.";
        }

        if (string.IsNullOrWhiteSpace(form.City))
        {
            return "Rezervasyon için il seçimi zorunludur.";
        }

        if (string.IsNullOrWhiteSpace(form.District))
        {
            return "Rezervasyon için ilçe seçimi zorunludur.";
        }

        if (string.IsNullOrWhiteSpace(form.Neighborhood))
        {
            return "Rezervasyon için mahalle seçimi zorunludur.";
        }

        return null;
    }

    private static bool IsReservationAdultEligible(string? birthDateText)
        => TryParseBirthDate(birthDateText, out var birthDate) && IsReservationAdultEligible(birthDate);

    private static bool IsReservationAdultEligible(DateTime birthDate)
        => birthDate.Date <= DateTime.Today.AddYears(-18).AddDays(-1);

    private static bool TryParseBirthDate(string? birthDateText, out DateTime birthDate)
        => DateTime.TryParse(birthDateText, out birthDate);

    private string? GetCurrentReservationSessionKey()
        => Request.Cookies.TryGetValue(ReservationDraftCookieName, out var key) ? key : null;

    private string EnsureReservationSessionKey()
    {
        if (Request.Cookies.TryGetValue(ReservationDraftCookieName, out var existing) && !string.IsNullOrWhiteSpace(existing))
        {
            return existing;
        }

        var key = Guid.NewGuid().ToString("N");
        Response.Cookies.Append(ReservationDraftCookieName, key, new CookieOptions
        {
            HttpOnly = true,
            IsEssential = true,
            SameSite = SameSiteMode.Lax,
            Secure = true,
            Expires = DateTimeOffset.UtcNow.AddDays(90)
        });
        return key;
    }

public sealed class UserLocationLogRequest
{
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public int? RadiusKm { get; set; }
    public int? VisibleHotelCount { get; set; }
    public string? ListedHotelIds { get; set; }
    public string? SearchTerm { get; set; }
    public string? SearchRegion { get; set; }
    public string? Source { get; set; }
  }
}
