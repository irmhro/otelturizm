using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using otelturizmnew.Constants;
using otelturizmnew.Models.Giris;
using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Controllers.Login;

public class AuthController : Controller
{
    private const string UserLoginPath = "/kullanici-giris";
    private const string PartnerLoginPath = "/partner-giris";
    private const string FirmaLoginPath = "/firma-giris";
    private const string AdminLoginPath = "/admin-giris";
    private const string LogoutPath = "/cikis-yap";
    private const string VerifyEmailPath = "/eposta-dogrula";
    private const string ForgotPasswordPath = "/sifremi-unuttum";
    private const string ResetPasswordPath = "/sifre-sifirla";

    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpGet(UserLoginPath)]
    public IActionResult UserLogin()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return Redirect(GetRedirectPathByClaims());
        }

        ViewData["PageCss"] = "user-login";
        return View("~/Views/Login/UserLogin.cshtml");
    }

    [HttpGet(AdminLoginPath)]
    public IActionResult AdminLogin()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return Redirect(GetRedirectPathByClaims());
        }

        ViewData["PageCss"] = "admin-login";
        return View("~/Views/Login/AdminLogin.cshtml");
    }

    [HttpGet(FirmaLoginPath)]
    public IActionResult FirmaLogin()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return Redirect(GetRedirectPathByClaims());
        }

        ViewData["PageCss"] = "firma-login";
        return View("~/Views/Login/FirmaLogin.cshtml");
    }

    [HttpGet("/adminx")]
    public IActionResult LegacyAdminLogin()
    {
        return Redirect(AdminLoginPath);
    }

    [HttpPost(UserLoginPath)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UserLogin(string loginEmail, string loginPassword, bool rememberMe = false, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(loginEmail) || string.IsNullOrWhiteSpace(loginPassword))
        {
            TempData["UserLoginError"] = "E-posta ve sifre zorunludur.";
            return Redirect(UserLoginPath);
        }

        UserSessionModel? user;
        try
        {
            user = await _authService.AuthenticateUserAsync(loginEmail, loginPassword, cancellationToken);
        }
        catch (AuthFlowException ex)
        {
            TempData["UserLoginError"] = ex.Message;
            SetResendVerifyTempData(ex, loginEmail);
            return Redirect(UserLoginPath);
        }
        catch
        {
            TempData["UserLoginError"] = "Veritabani baglantisi veya giris dogrulama sirasinda hata olustu.";
            return Redirect(UserLoginPath);
        }

        if (user is null)
        {
            TempData["UserLoginError"] = "Giris bilgileri hatali veya hesap aktif degil.";
            return Redirect(UserLoginPath);
        }

        await SignInAsync(user, rememberMe);
        return Redirect(GetRedirectPath(user));
    }

    [HttpPost(AdminLoginPath)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AdminLogin(string adminEmail, string adminPassword, bool rememberMe = true, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
        {
            TempData["AdminLoginError"] = "Admin girisi icin e-posta ve sifre zorunludur.";
            return Redirect(AdminLoginPath);
        }

        UserSessionModel? user;
        try
        {
            user = await _authService.AuthenticateUserAsync(adminEmail, adminPassword, cancellationToken);
        }
        catch (AuthFlowException ex)
        {
            TempData["AdminLoginError"] = ex.Message;
            return Redirect(AdminLoginPath);
        }
        catch (Exception ex)
        {
            TempData["AdminLoginError"] = $"Veritabani baglantisi veya giris dogrulama sirasinda hata olustu: {ex.Message}";
            return Redirect(AdminLoginPath);
        }

        if (user is null)
        {
            TempData["AdminLoginError"] = "Admin hesabi bulunamadi veya sifre yanlis.";
            return Redirect(AdminLoginPath);
        }

        if (!string.Equals(user.AccountType, "admin", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(user.UserRole, "admin", StringComparison.OrdinalIgnoreCase)
            && !user.RoleCodes.Any(static code => string.Equals(code, "superadmin", StringComparison.OrdinalIgnoreCase) || string.Equals(code, "admin", StringComparison.OrdinalIgnoreCase)))
        {
            TempData["AdminLoginError"] = "Bu hesap admin paneline erisemiyor.";
            return Redirect(AdminLoginPath);
        }

        await SignInAsync(user, rememberMe);
        return Redirect(Url.Action("Dashboard", "AdminPanel") ?? "/admin/dashboard");
    }

    [HttpPost(FirmaLoginPath)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> FirmaLogin(string firmaIdentity, string firmaPassword, bool rememberMe = false, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(firmaIdentity) || string.IsNullOrWhiteSpace(firmaPassword))
        {
            TempData["FirmaLoginError"] = "Firma girişi için e-posta ve şifre zorunludur.";
            return Redirect(FirmaLoginPath);
        }

        UserSessionModel? user;
        try
        {
            user = await _authService.AuthenticateFirmaAsync(firmaIdentity, firmaPassword, cancellationToken);
        }
        catch (AuthFlowException ex)
        {
            TempData["FirmaLoginError"] = ex.Message;
            SetResendVerifyTempData(ex, firmaIdentity);
            return Redirect(FirmaLoginPath);
        }
        catch
        {
            TempData["FirmaLoginError"] = "Veritabanı bağlantısı veya firma giriş doğrulaması sırasında hata oluştu.";
            return Redirect(FirmaLoginPath);
        }

        if (user is null)
        {
            TempData["FirmaLoginError"] = "Firma hesabı bulunamadı, henüz onaylanmadı veya şifre yanlış.";
            return Redirect(FirmaLoginPath);
        }

        await SignInAsync(user, rememberMe);
        return Redirect(GetRedirectPath(user));
    }

    [HttpGet(PartnerLoginPath)]
    public IActionResult PartnerLogin()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return Redirect(GetRedirectPathByClaims());
        }

        ViewData["PageCss"] = "partner-login";
        return View("~/Views/Login/PartnerLogin.cshtml");
    }

    [HttpPost(PartnerLoginPath)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PartnerLogin(string partnerIdentity, string partnerPassword, bool rememberMe = false, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(partnerIdentity) || string.IsNullOrWhiteSpace(partnerPassword))
        {
            TempData["PartnerLoginError"] = "Partner girisi icin kimlik ve sifre zorunludur.";
            return Redirect(PartnerLoginPath);
        }

        UserSessionModel? user;
        try
        {
            user = await _authService.AuthenticatePartnerAsync(partnerIdentity, partnerPassword, cancellationToken);
        }
        catch (AuthFlowException ex)
        {
            TempData["PartnerLoginError"] = ex.Message;
            SetResendVerifyTempData(ex, partnerIdentity);
            return Redirect(PartnerLoginPath);
        }
        catch
        {
            TempData["PartnerLoginError"] = "Veritabani baglantisi veya partner giris dogrulama sirasinda hata olustu.";
            return Redirect(PartnerLoginPath);
        }

        if (user is null)
        {
            TempData["PartnerLoginError"] = "Partner hesabi bulunamadi veya sifre yanlis.";
            return Redirect(PartnerLoginPath);
        }

        await SignInAsync(user, rememberMe);
        return Redirect(GetRedirectPath(user));
    }

    [HttpGet(LogoutPath)]
    public IActionResult Logout()
    {
        return Redirect("/");
    }

    [HttpPost(LogoutPath)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LogoutPost()
    {
        var redirectPath = GetRedirectLoginPathByClaims();
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        HttpContext.Response.Cookies.Delete("Otelturizm.SessionKey");
        HttpContext.Response.Cookies.Delete("Otelturizm.LastSeenUtc");
        return Redirect(redirectPath);
    }

    [HttpGet(VerifyEmailPath)]
    public async Task<IActionResult> VerifyEmail(string? email, string? token, string? code, CancellationToken cancellationToken = default)
    {
        ViewData["PageCss"] = "user-login";

        if (!string.IsNullOrWhiteSpace(email) && !string.IsNullOrWhiteSpace(token) && !string.IsNullOrWhiteSpace(code))
        {
            var result = await _authService.VerifyEmailAsync(email, code, token, HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString(), cancellationToken);
            TempData[result.Success ? "UserLoginSuccess" : "UserLoginError"] = result.Message;
            var redirectPath = await _authService.ResolveLoginPathByEmailAsync(email, cancellationToken);
            return Redirect(redirectPath);
        }

        return View("~/Views/Login/VerifyEmail.cshtml", new EmailVerificationViewModel
        {
            Email = email ?? string.Empty,
            Token = token
        });
    }

    [HttpPost(VerifyEmailPath)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyEmailPost(EmailVerificationViewModel model, CancellationToken cancellationToken = default)
    {
        ViewData["PageCss"] = "user-login";
        if (!ModelState.IsValid)
        {
            return View("~/Views/Login/VerifyEmail.cshtml", model);
        }

        var result = await _authService.VerifyEmailAsync(model.Email, model.Code, model.Token, HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString(), cancellationToken);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return View("~/Views/Login/VerifyEmail.cshtml", model);
        }

        TempData["UserLoginSuccess"] = result.Message;
        var verifyRedirectPath = await _authService.ResolveLoginPathByEmailAsync(model.Email, cancellationToken);
        return Redirect(verifyRedirectPath);
    }

    [HttpPost("/eposta-dogrula/tekrar-gonder")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResendVerifyEmail(string email, CancellationToken cancellationToken = default)
    {
        var result = await _authService.ResendVerificationEmailAsync(email, HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString(), cancellationToken);
        TempData[result.Success ? "UserLoginSuccess" : "UserLoginError"] = result.Message;
        return Redirect($"{VerifyEmailPath}?email={Uri.EscapeDataString(email ?? string.Empty)}");
    }

    [HttpGet(ForgotPasswordPath)]
    public IActionResult ForgotPassword()
    {
        ViewData["PageCss"] = "user-login";
        return View("~/Views/Login/ForgotPassword.cshtml", new ForgotPasswordViewModel());
    }

    [HttpPost(ForgotPasswordPath)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model, CancellationToken cancellationToken = default)
    {
        ViewData["PageCss"] = "user-login";
        if (!ModelState.IsValid)
        {
            return View("~/Views/Login/ForgotPassword.cshtml", model);
        }

        var result = await _authService.SendPasswordResetAsync(model.Email, HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString(), cancellationToken);
        TempData[result.Success ? "UserLoginSuccess" : "UserLoginError"] = result.Message;
        var forgotRedirectPath = await _authService.ResolveLoginPathByEmailAsync(model.Email, cancellationToken);
        return Redirect(forgotRedirectPath);
    }

    [HttpGet(ResetPasswordPath)]
    public IActionResult ResetPassword(string token)
    {
        ViewData["PageCss"] = "user-login";
        return View("~/Views/Login/ResetPassword.cshtml", new ResetPasswordViewModel
        {
            Token = token ?? string.Empty
        });
    }

    [HttpPost(ResetPasswordPath)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model, CancellationToken cancellationToken = default)
    {
        ViewData["PageCss"] = "user-login";
        if (!ModelState.IsValid)
        {
            return View("~/Views/Login/ResetPassword.cshtml", model);
        }

        var result = await _authService.ResetPasswordAsync(model.Token, model.NewPassword, model.ConfirmPassword, cancellationToken);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return View("~/Views/Login/ResetPassword.cshtml", model);
        }

        TempData["UserLoginSuccess"] = result.Message;
        var resetRedirectPath = await _authService.ResolveLoginPathByResetTokenAsync(model.Token, cancellationToken);
        return Redirect(resetRedirectPath);
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
            new(AuthClaimTypes.AccountType, user.AccountType),
            new(AuthClaimTypes.RememberMe, rememberMe ? "true" : "false")
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
            "sales" => Url.Action("Index", "SalesPanel") ?? "/panel/satis",
            _ => Url.Action("Index", "UserPanel") ?? "/panel/user"
        };
    }

    private string GetRedirectPathByClaims()
    {
        var accountType = User.FindFirstValue(AuthClaimTypes.AccountType);
        return accountType switch
        {
            "admin" => Url.Action("Dashboard", "AdminPanel") ?? "/admin/dashboard",
            "partner" => Url.Action("Index", "PartnerPanel") ?? "/panel/partner",
            "firma" => Url.Action("Index", "FirmaPanel") ?? "/panel/firma",
            "sales" => Url.Action("Index", "SalesPanel") ?? "/panel/satis",
            _ => Url.Action("Index", "UserPanel") ?? "/panel/user"
        };
    }

    private string GetRedirectLoginPathByClaims()
    {
        var accountType = User.FindFirstValue(AuthClaimTypes.AccountType);
        return accountType switch
        {
            "admin" => AdminLoginPath,
            "partner" => PartnerLoginPath,
            "firma" => FirmaLoginPath,
            _ => UserLoginPath
        };
    }

    private void SetResendVerifyTempData(AuthFlowException ex, string? fallbackIdentity)
    {
        if (!string.Equals(ex.ErrorCode, AuthFlowErrorCodes.EmailNotVerified, StringComparison.Ordinal))
        {
            return;
        }

        var normalizedFallback = (fallbackIdentity ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalizedFallback) || !normalizedFallback.Contains('@', StringComparison.Ordinal))
        {
            normalizedFallback = string.Empty;
        }

        TempData["ShowResendVerifyButton"] = "1";
        TempData["ResendVerifyEmail"] = ex.RelatedEmail ?? normalizedFallback;
    }
}





