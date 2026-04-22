using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
    private readonly IHotelService _hotelService;
    private readonly IPublicReservationService _publicReservationService;
    private readonly IWeatherService _weatherService;
    private readonly IUserFavoriteService _userFavoriteService;
    private readonly IUserPanelService _userPanelService;
    private readonly IMessageCenterService _messageCenterService;

    public OtellerController(IHotelService hotelService, IPublicReservationService publicReservationService, IWeatherService weatherService, IUserFavoriteService userFavoriteService, IUserPanelService userPanelService, IMessageCenterService messageCenterService)
    {
        _hotelService = hotelService;
        _publicReservationService = publicReservationService;
        _weatherService = weatherService;
        _userFavoriteService = userFavoriteService;
        _userPanelService = userPanelService;
        _messageCenterService = messageCenterService;
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
            RoomTypeId = model.Rooms.FirstOrDefault()?.RoomTypeId ?? 0
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
        return new otelturizmnew.Models.Oteller.HotelProfileCompletionPromptViewModel
        {
            ReturnUrl = returnUrl,
            Email = profile.Form.Email,
            Phone = profile.Form.Phone,
            BirthDateText = profile.Form.BirthDateText,
            Gender = profile.Form.Gender,
            Nationality = profile.Form.Nationality,
            City = profile.Form.City,
            District = profile.Form.District,
            Neighborhood = profile.Form.Neighborhood,
            Address = profile.Form.Address,
            IsProfileIncomplete = string.IsNullOrWhiteSpace(profile.Form.Email)
                                || string.IsNullOrWhiteSpace(profile.Form.BirthDateText)
                                || string.IsNullOrWhiteSpace(profile.Form.Gender)
                                || string.IsNullOrWhiteSpace(profile.Form.City)
                                || string.IsNullOrWhiteSpace(profile.Form.District)
                                || string.IsNullOrWhiteSpace(profile.Form.Neighborhood)
        };
    }

    private static string? ValidateReservationProfile(UserProfileForm form)
    {
        if (string.IsNullOrWhiteSpace(form.Email))
        {
            return "Rezervasyon için e-posta alanı zorunludur.";
        }

        if (string.IsNullOrWhiteSpace(form.BirthDateText))
        {
            return "Rezervasyon için doğum tarihi zorunludur.";
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
}
