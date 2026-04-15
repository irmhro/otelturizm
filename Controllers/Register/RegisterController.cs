using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using otelturizmnew.Constants;
using otelturizmnew.Models.Giris;
using otelturizmnew.Models.Register;
using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Controllers.Register;

public class RegisterController : Controller
{
    private const string UserLoginPath = "/kullanici-giris";
    private const string UserRegisterPath = "/kullanici-kayit";
    private const string PartnerLoginPath = "/partner-giris";
    private const string PartnerRegisterPath = "/partner-kayit";
    private const string FirmaLoginPath = "/firma-giris";
    private const string FirmaRegisterPath = "/firma-kayit";

    private readonly IAuthService _authService;

    public RegisterController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost(UserRegisterPath)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UserKayit(UserRegistrationModel model, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _authService.RegisterUserAsync(model, cancellationToken);
            if (!result.Success)
            {
                TempData["UserRegisterError"] = result.Message;
                TempData["OpenUserRegisterTab"] = "1";
                return Redirect(UserLoginPath);
            }

            TempData["UserLoginSuccess"] = result.Message;
            return Redirect($"/eposta-dogrula?email={Uri.EscapeDataString(model.Email.Trim().ToLowerInvariant())}");
        }
        catch
        {
            TempData["UserRegisterError"] = "Kayıt işlemi sırasında beklenmeyen bir hata oluştu.";
            TempData["OpenUserRegisterTab"] = "1";
            return Redirect(UserLoginPath);
        }
    }

    [HttpPost(PartnerRegisterPath)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Partner(PartnerRegistrationModel model, CancellationToken cancellationToken = default)
    {
        try
        {
            var normalizedEmail = model.Email?.Trim().ToLowerInvariant() ?? string.Empty;
            var result = await _authService.RegisterPartnerAsync(model, cancellationToken);
            if (!result.Success)
            {
                TempData["PartnerRegisterError"] = result.Message;
                TempData["OpenPartnerRegisterTab"] = "1";
                return Redirect(PartnerLoginPath);
            }

            TempData["UserLoginSuccess"] = result.Message;
            return Redirect($"/eposta-dogrula?email={Uri.EscapeDataString(normalizedEmail)}");
        }
        catch
        {
            TempData["PartnerRegisterError"] = "Partner kaydı sırasında beklenmeyen bir hata oluştu.";
            TempData["OpenPartnerRegisterTab"] = "1";
            return Redirect(PartnerLoginPath);
        }
    }

    [HttpPost(FirmaRegisterPath)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Firma(FirmaRegistrationModel model, CancellationToken cancellationToken = default)
    {
        try
        {
            var normalizedEmail = model.ContactEmail?.Trim().ToLowerInvariant() ?? string.Empty;
            var result = await _authService.RegisterFirmaAsync(model, cancellationToken);
            if (!result.Success)
            {
                TempData["FirmaRegisterError"] = result.Message;
                TempData["OpenFirmaRegisterTab"] = "1";
                return Redirect(FirmaLoginPath);
            }

            TempData["UserLoginSuccess"] = result.Message;
            return Redirect($"/eposta-dogrula?email={Uri.EscapeDataString(normalizedEmail)}");
        }
        catch
        {
            TempData["FirmaRegisterError"] = "Firma başvurusu sırasında beklenmeyen bir hata oluştu.";
            TempData["OpenFirmaRegisterTab"] = "1";
            return Redirect(FirmaLoginPath);
        }
    }

    private async Task SignInAsync(UserSessionModel user, bool rememberMe)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Email, user.Email),
            new(AuthClaimTypes.UserId, user.UserId.ToString()),
            new(AuthClaimTypes.FullName, user.FullName),
            new(AuthClaimTypes.Email, user.Email),
            new(AuthClaimTypes.AccountType, user.AccountType)
        };

        if (user.PartnerId.HasValue)
        {
            claims.Add(new Claim(AuthClaimTypes.PartnerId, user.PartnerId.Value.ToString()));
        }

        if (!string.IsNullOrWhiteSpace(user.UserRole))
        {
            claims.Add(new Claim(AuthClaimTypes.UserRole, user.UserRole));
        }

        if (user.OwnershipPartnerId.HasValue)
        {
            claims.Add(new Claim(AuthClaimTypes.OwnershipPartnerId, user.OwnershipPartnerId.Value.ToString()));
        }

        foreach (var hotelId in user.ManagedHotelIds.Distinct())
        {
            claims.Add(new Claim(AuthClaimTypes.ManagedHotelIds, hotelId.ToString()));
        }

        foreach (var roleCode in user.RoleCodes)
        {
            claims.Add(new Claim(ClaimTypes.Role, roleCode));
            claims.Add(new Claim(AuthClaimTypes.RoleCodes, roleCode));
        }

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = rememberMe,
                AllowRefresh = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(rememberMe ? 14 : 1)
            });
    }

    private string GetRedirectPath(UserSessionModel user)
    {
        return user.AccountType switch
        {
            "admin" => Url.Action("Dashboard", "AdminPanel") ?? "/admin/dashboard",
            "partner" => Url.Action("Index", "PartnerPanel") ?? "/panel/partner",
            "firma" => Url.Action("Index", "FirmaPanel") ?? "/panel/firma",
            _ => Url.Action("Index", "UserPanel") ?? "/panel/user"
        };
    }
}
