using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using otelturizmnew.Constants;
using otelturizmnew.Models.Paneller.Satis;
using otelturizmnew.Services.Abstractions;
using otelturizmnew.Utils;

namespace otelturizmnew.Controllers.Paneller.Satis;

[Authorize(Policy = "SalesPanel")]
[Route("panel/satis")]
public class SalesPanelController : Controller
{
    private readonly ISalesService _salesService;
    private readonly IAuthService _authService;
    private readonly IIdempotencyService _idempotency;

    public SalesPanelController(ISalesService salesService, IAuthService authService, IIdempotencyService idempotency)
    {
        _salesService = salesService;
        _authService = authService;
        _idempotency = idempotency;
    }

    [HttpGet("")]
    [HttpGet("dashboard")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var model = await _salesService.GetDashboardAsync(GetUserId(), cancellationToken);
        ApplyFeedback(model.Shell);
        ViewData["Title"] = "Satış Dashboard";
        ViewData["PageCssPath"] = "paneller/satis/dashboard";
        return View("~/Views/Paneller/Satis/Dashboard.cshtml", model);
    }

    [HttpGet("guvenlik")]
    public async Task<IActionResult> Security(CancellationToken cancellationToken)
    {
        var dashboard = await _salesService.GetDashboardAsync(GetUserId(), cancellationToken);
        ApplyFeedback(dashboard.Shell);
        ViewData["SalesShell"] = dashboard.Shell;
        var model = await _authService.GetTwoFactorSecurityAsync(GetUserId(), "sales", cancellationToken);
        TempData["SalesSuccess"] ??= TempData["UserSecuritySuccess"];
        TempData["SalesError"] ??= TempData["UserSecurityError"];
        ViewData["Title"] = "Satış Güvenlik";
        ViewData["PageCssPath"] = "panel-user-security";
        ViewData["PageCssMobile"] = "panel-user-security.mobile";
        return View("~/Views/Paneller/Satis/Security.cshtml", model);
    }

    [HttpPost("guvenlik/iki-asamali")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveTwoFactor(otelturizmnew.Models.Paneller.User.UserTwoFactorForm form, CancellationToken cancellationToken)
    {
        var result = await _authService.SaveTwoFactorSecurityAsync(GetUserId(), form, cancellationToken);
        TempData[result.Success ? "SalesSuccess" : "SalesError"] = result.Message;
        return Redirect("/panel/satis/guvenlik");
    }

    [HttpGet("yeni-rezervasyon")]
    public async Task<IActionResult> CreateReservation(long? hotelId = null, long? roomTypeId = null, long? customerId = null, string? searchTerm = null, string? city = null, string? district = null, string? neighborhood = null, decimal? minPrice = null, decimal? maxPrice = null, decimal? minimumRating = null, int? minimumReviewCount = null, string? feature = null, CancellationToken cancellationToken = default)
    {
        var model = await _salesService.GetCreateReservationAsync(GetUserId(), hotelId, roomTypeId, customerId, searchTerm, city, district, neighborhood, minPrice, maxPrice, minimumRating, minimumReviewCount, feature, cancellationToken);
        ApplyFeedback(model.Shell);
        ViewData["Title"] = "Yeni Rezervasyon Oluştur";
        ViewData["PageCssPath"] = "paneller/satis/create-reservation";
        return View("~/Views/Paneller/Satis/CreateReservation.cshtml", model);
    }

    [HttpPost("yeni-rezervasyon")]
    [ValidateAntiForgeryToken]
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("reservation-create")]
    public async Task<IActionResult> CreateReservationPost(SalesReservationCreateModel model, CancellationToken cancellationToken)
    {
        var idemKey = IdempotencyKey.ForObject($"sales-res-create:{GetUserId()}", model);
        var result = await _idempotency.GetOrCreateAsync(
            idemKey,
            async ct => await _salesService.CreateReservationAsync(GetUserId(), model, ct),
            ttl: TimeSpan.FromSeconds(25),
            cancellationToken: cancellationToken);
        SetFeedback(result.Success, result.Message);
        if (result.Success && result.ReservationId.HasValue && result.ReservationId.Value > 0)
        {
            TempData["SalesLastReservationId"] = result.ReservationId.Value.ToString();
            TempData["SalesLastReservationEmail"] = (model.CustomerEmail ?? string.Empty).Trim();
        }
        return Redirect("/panel/satis/yeni-rezervasyon");
    }

    [HttpGet("rezervasyon-pdf/{reservationId:long}")]
    public async Task<IActionResult> ReservationPdf(long reservationId, CancellationToken cancellationToken)
    {
        var dashboard = await _salesService.GetDashboardAsync(GetUserId(), cancellationToken);
        ApplyFeedback(dashboard.Shell);
        dashboard.Shell.ActiveSectionKey = "reservations";
        dashboard.Shell.PanelTitle = "Rezervasyon PDF";
        dashboard.Shell.PanelSubtitle = "E-posta olmayan misafirler için anında PDF üretin, indirin ve paylaşın.";
        ViewData["SalesShell"] = dashboard.Shell;
        ViewData["Title"] = "Rezervasyon PDF";
        ViewData["PageCssPath"] = "paneller/satis/reservation-pdf";
        ViewData["ReservationId"] = reservationId;
        return View("~/Views/Paneller/Satis/ReservationPdf.cshtml");
    }

    [HttpGet("api/rezervasyon-pdf/{reservationId:long}")]
    public async Task<IActionResult> ReservationPdfData(long reservationId, CancellationToken cancellationToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(8)); // p166: timebox
        try
        {
            var data = await _salesService.GetReservationPdfDataAsync(GetUserId(), reservationId, cts.Token);
            if (data is null) return NotFound(new { success = false, message = "Rezervasyon bulunamadı." });
            return Ok(new { success = true, data });
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            return StatusCode(504, new { success = false, message = "PDF verisi hazırlanırken zaman aşımı oluştu. Lütfen tekrar deneyin." });
        }
    }

    [HttpGet("yeni-rezervasyon/otel-asistani")]
    public async Task<IActionResult> SearchHotelsAssistant(
        string? q = null,
        string? city = null,
        string? district = null,
        string? neighborhood = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        decimal? minimumRating = null,
        int? minimumReviewCount = null,
        string? feature = null,
        int take = 8,
        CancellationToken cancellationToken = default)
    {
        var hotels = await _salesService.SearchHotelsForAssistantAsync(
            GetUserId(),
            q,
            city,
            district,
            neighborhood,
            minPrice,
            maxPrice,
            minimumRating,
            minimumReviewCount,
            feature,
            take,
            cancellationToken);

        return Ok(new
        {
            success = true,
            count = hotels.Count,
            hotels = hotels.Select(item => new
            {
                item.HotelId,
                item.HotelName,
                item.City,
                item.District,
                item.Address,
                item.Phone,
                item.RatingText,
                item.ReviewCountText,
                item.PriceText,
                item.TodayDemandText,
                item.LocationText,
                item.FeatureBadges
            })
        });
    }

    [HttpGet("musaitlik-takvimi")]
    public async Task<IActionResult> Availability(long? hotelId = null, long? roomTypeId = null, string? search = null, int? year = null, int? month = null, CancellationToken cancellationToken = default)
    {
        DateOnly? targetMonth = null;
        if (year.HasValue && month.HasValue) targetMonth = new DateOnly(year.Value, month.Value, 1);
        var model = await _salesService.GetAvailabilityAsync(GetUserId(), hotelId, roomTypeId, search, targetMonth, cancellationToken);
        ApplyFeedback(model.Shell);
        ViewData["Title"] = "Müsaitlik Takvimi";
        ViewData["PageCssPath"] = "paneller/satis/availability";
        return View("~/Views/Paneller/Satis/Availability.cshtml", model);
    }

    [HttpGet("rezervasyonlarim")]
    public async Task<IActionResult> Reservations(string? search = null, string? status = null, string? approval = null, DateOnly? startDate = null, DateOnly? endDate = null, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var filters = new SalesReservationsFilterViewModel
        {
            Search = search,
            Status = status,
            Approval = approval,
            StartDate = startDate,
            EndDate = endDate,
            Page = page,
            PageSize = pageSize
        };
        var model = await _salesService.GetReservationsAsync(GetUserId(), filters, cancellationToken);
        ApplyFeedback(model.Shell);
        ViewData["Title"] = "Rezervasyonlarım";
        ViewData["PageCssPath"] = "paneller/satis/reservations";
        return View("~/Views/Paneller/Satis/Reservations.cshtml", model);
    }

    [HttpGet("musteri-yonetimi")]
    public async Task<IActionResult> Customers(string? search = null, CancellationToken cancellationToken = default)
    {
        var model = await _salesService.GetCustomersAsync(GetUserId(), search, cancellationToken);
        ApplyFeedback(model.Shell);
        ViewData["Title"] = "Müşteri Yönetimi";
        ViewData["PageCssPath"] = "paneller/satis/customers";
        return View("~/Views/Paneller/Satis/Customers.cshtml", model);
    }

    [HttpPost("musteri-yonetimi/yeni")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCustomer(SalesCustomerCreateModel model, CancellationToken cancellationToken)
    {
        var result = await _salesService.CreateCustomerAsync(GetUserId(), model, cancellationToken);
        SetFeedback(result.Success, result.Message);
        return Redirect("/panel/satis/musteri-yonetimi");
    }

    [HttpGet("raporlar")]
    public async Task<IActionResult> Reports(int year = 0, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var targetYear = year <= 0 ? DateTime.Today.Year : year;
        var model = await _salesService.GetReportsAsync(GetUserId(), targetYear, page, pageSize, cancellationToken);
        ApplyFeedback(model.Shell);
        ViewData["Title"] = "Raporlar";
        ViewData["PageCssPath"] = "paneller/satis/reports";
        return View("~/Views/Paneller/Satis/Reports.cshtml", model);
    }

    [HttpGet("otel-rehberi")]
    public async Task<IActionResult> Hotels(string? search = null, CancellationToken cancellationToken = default)
    {
        var model = await _salesService.GetHotelGuideAsync(GetUserId(), search, cancellationToken);
        ApplyFeedback(model.Shell);
        ViewData["Title"] = "Otel Rehberi";
        ViewData["PageCssPath"] = "paneller/satis/hotels";
        return View("~/Views/Paneller/Satis/Hotels.cshtml", model);
    }

    private long GetUserId()
        => long.TryParse(User.FindFirstValue(AuthClaimTypes.UserId) ?? User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) ? userId : 0;

    private void SetFeedback(bool success, string message)
    {
        if (success)
        {
            TempData["SalesSuccess"] = message;
            TempData.Remove("SalesError");
        }
        else
        {
            TempData["SalesError"] = message;
            TempData.Remove("SalesSuccess");
        }
    }

    private void ApplyFeedback(SalesPanelShellViewModel shell)
    {
        shell.SuccessMessage = TempData["SalesSuccess"] as string;
        shell.ErrorMessage = TempData["SalesError"] as string;
    }
}
