using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using otelturizmnew.Constants;
using otelturizmnew.Models.Paneller.Satis;
using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Controllers.Paneller.Satis;

[Authorize]
[Route("panel/satis")]
public class SalesPanelController : Controller
{
    private readonly ISalesService _salesService;

    public SalesPanelController(ISalesService salesService)
    {
        _salesService = salesService;
    }

    [HttpGet("")]
    [HttpGet("dashboard")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (!IsSalesUser()) return Redirect("/kullanici-giris");
        var model = await _salesService.GetDashboardAsync(GetUserId(), cancellationToken);
        ApplyFeedback(model.Shell);
        ViewData["Title"] = "Satış Dashboard";
        ViewData["PageCssPath"] = "paneller/satis/dashboard";
        return View("~/Views/Paneller/Satis/Dashboard.cshtml", model);
    }

    [HttpGet("yeni-rezervasyon")]
    public async Task<IActionResult> CreateReservation(long? hotelId = null, long? roomTypeId = null, string? searchTerm = null, string? city = null, string? district = null, string? neighborhood = null, decimal? minPrice = null, decimal? maxPrice = null, decimal? minimumRating = null, int? minimumReviewCount = null, string? feature = null, CancellationToken cancellationToken = default)
    {
        if (!IsSalesUser()) return Redirect("/kullanici-giris");
        var model = await _salesService.GetCreateReservationAsync(GetUserId(), hotelId, roomTypeId, searchTerm, city, district, neighborhood, minPrice, maxPrice, minimumRating, minimumReviewCount, feature, cancellationToken);
        ApplyFeedback(model.Shell);
        ViewData["Title"] = "Yeni Rezervasyon Oluştur";
        ViewData["PageCssPath"] = "paneller/satis/create-reservation";
        return View("~/Views/Paneller/Satis/CreateReservation.cshtml", model);
    }

    [HttpPost("yeni-rezervasyon")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateReservationPost(SalesReservationCreateModel model, CancellationToken cancellationToken)
    {
        if (!IsSalesUser()) return Redirect("/kullanici-giris");
        var result = await _salesService.CreateReservationAsync(GetUserId(), model, cancellationToken);
        SetFeedback(result.Success, result.Message);
        return Redirect("/panel/satis/yeni-rezervasyon");
    }

    [HttpGet("musaitlik-takvimi")]
    public async Task<IActionResult> Availability(long? hotelId = null, long? roomTypeId = null, int? year = null, int? month = null, CancellationToken cancellationToken = default)
    {
        if (!IsSalesUser()) return Redirect("/kullanici-giris");
        DateOnly? targetMonth = null;
        if (year.HasValue && month.HasValue) targetMonth = new DateOnly(year.Value, month.Value, 1);
        var model = await _salesService.GetAvailabilityAsync(GetUserId(), hotelId, roomTypeId, targetMonth, cancellationToken);
        ApplyFeedback(model.Shell);
        ViewData["Title"] = "Müsaitlik Takvimi";
        ViewData["PageCssPath"] = "paneller/satis/availability";
        return View("~/Views/Paneller/Satis/Availability.cshtml", model);
    }

    [HttpGet("rezervasyonlarim")]
    public async Task<IActionResult> Reservations(CancellationToken cancellationToken)
    {
        if (!IsSalesUser()) return Redirect("/kullanici-giris");
        var model = await _salesService.GetReservationsAsync(GetUserId(), cancellationToken);
        ApplyFeedback(model.Shell);
        ViewData["Title"] = "Rezervasyonlarım";
        ViewData["PageCssPath"] = "paneller/satis/reservations";
        return View("~/Views/Paneller/Satis/Reservations.cshtml", model);
    }

    [HttpGet("musteri-yonetimi")]
    public async Task<IActionResult> Customers(string? search = null, CancellationToken cancellationToken = default)
    {
        if (!IsSalesUser()) return Redirect("/kullanici-giris");
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
        if (!IsSalesUser()) return Redirect("/kullanici-giris");
        var result = await _salesService.CreateCustomerAsync(GetUserId(), model, cancellationToken);
        SetFeedback(result.Success, result.Message);
        return Redirect("/panel/satis/musteri-yonetimi");
    }

    [HttpGet("raporlar")]
    public async Task<IActionResult> Reports(CancellationToken cancellationToken)
    {
        if (!IsSalesUser()) return Redirect("/kullanici-giris");
        var model = await _salesService.GetReportsAsync(GetUserId(), cancellationToken);
        ApplyFeedback(model.Shell);
        ViewData["Title"] = "Raporlar";
        ViewData["PageCssPath"] = "paneller/satis/reports";
        return View("~/Views/Paneller/Satis/Reports.cshtml", model);
    }

    [HttpGet("otel-rehberi")]
    public async Task<IActionResult> Hotels(string? search = null, CancellationToken cancellationToken = default)
    {
        if (!IsSalesUser()) return Redirect("/kullanici-giris");
        var model = await _salesService.GetHotelGuideAsync(GetUserId(), search, cancellationToken);
        ApplyFeedback(model.Shell);
        ViewData["Title"] = "Otel Rehberi";
        ViewData["PageCssPath"] = "paneller/satis/hotels";
        return View("~/Views/Paneller/Satis/Hotels.cshtml", model);
    }

    private bool IsSalesUser()
    {
        var accountType = User.FindFirstValue(AuthClaimTypes.AccountType);
        var userRole = User.FindFirstValue(AuthClaimTypes.UserRole);
        return string.Equals(accountType, "sales", StringComparison.OrdinalIgnoreCase)
            || (!string.IsNullOrWhiteSpace(userRole) && userRole.StartsWith("sales_", StringComparison.OrdinalIgnoreCase));
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
