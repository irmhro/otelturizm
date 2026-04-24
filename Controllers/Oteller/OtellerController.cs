using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using otelturizmnew.Constants;
using otelturizmnew.Models.Paneller.User;
using otelturizmnew.Models.Reservations;
using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Controllers.Oteller;

[Route("oteller")]
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

    public OtellerController(IHotelService hotelService, IPublicReservationService publicReservationService, IWeatherService weatherService, IUserFavoriteService userFavoriteService, IUserPanelService userPanelService, IMessageCenterService messageCenterService, ILocationLogService locationLogService)
    {
        _hotelService = hotelService;
        _publicReservationService = publicReservationService;
        _weatherService = weatherService;
        _userFavoriteService = userFavoriteService;
        _userPanelService = userPanelService;
        _messageCenterService = messageCenterService;
        _locationLogService = locationLogService;
    }

    [HttpGet("")]
    [HttpGet("istanbul")]
    public async Task<IActionResult> OtelListeleme([FromQuery] string? q, [FromQuery] string? city, [FromQuery] string? etiket, [FromQuery] string? kampanya, [FromQuery] int page, CancellationToken cancellationToken)
    {
        ViewData["PageCss"] = "otel-listeleme";
        ViewData["PageCssMobile"] = "otel-listeleme.mobile";
        var searchTerm = !string.IsNullOrWhiteSpace(q) ? q : city;
        var model = await _hotelService.GetHotelListingPageAsync(searchTerm, etiket, kampanya, page <= 0 ? 1 : page, cancellationToken);
        await ApplyFavoriteStatesAsync(model, cancellationToken);
        ViewData["Title"] = model.CampaignTitle;
        return View("~/Views/Oteller/OtelListeleme.cshtml", model);
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> OtelDetay(string slug, CancellationToken cancellationToken)
    {
        var model = await _hotelService.GetHotelDetailPageAsync(slug, cancellationToken);
        if (model is null)
        {
            return NotFound();
        }

        model.Weather = await _weatherService.GetForecastAsync(model.District, model.City, model.Latitude, model.Longitude, cancellationToken);
        await ApplyFavoriteStateAsync(model, cancellationToken);
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
            }
        }

        ViewData["Title"] = "Otel Detay";
        ViewData["PageCss"] = "otel-detay";
        ViewData["SuppressGlobalDraftBanner"] = true;
        return View("~/Views/Oteller/OtelDetay.cshtml", model);
    }

    [HttpPost("{slug}/rezervasyon")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StartReservation(string slug, PublicHotelReservationForm form, IFormFile? BankTransferReceipt, CancellationToken cancellationToken)
    {
        var result = await _publicReservationService.StartReservationAsync(GetCurrentUserIdOrNull(), EnsureReservationSessionKey(), form, BankTransferReceipt, cancellationToken);
        TempData[result.Success ? "PublicReservationSuccess" : "PublicReservationInfo"] = result.Message;
        if (!string.IsNullOrWhiteSpace(result.RedirectUrl))
        {
            return Redirect(result.RedirectUrl);
        }

        return Redirect($"/oteller/{slug}");
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
    public async Task<IActionResult> GetPriceQuote(string slug, [FromQuery] long roomTypeId, [FromQuery] DateOnly checkInDate, [FromQuery] DateOnly checkOutDate, [FromQuery] int roomCount = 1, CancellationToken cancellationToken = default)
    {
        var hotel = await _hotelService.GetHotelDetailPageAsync(slug, cancellationToken);
        if (hotel is null || roomTypeId <= 0)
        {
            return BadRequest(new { success = false, message = "Fiyat özeti hesaplanamadı." });
        }

        var quote = await _publicReservationService.GetPriceQuoteAsync(roomTypeId, checkInDate, checkOutDate, Math.Max(1, roomCount), cancellationToken);
        return Json(new
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
        });
    }

    [HttpPost("konum-kaydet")]
    [IgnoreAntiforgeryToken]
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
