using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
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
    private readonly ILoginTwoFactorService _loginTwoFactorService;
    private readonly IDataProtector _login2FaProtector;
    private const string Login2FaCookieName = "Otelturizm.Login2FA";

    public AuthController(IAuthService authService, ILoginTwoFactorService loginTwoFactorService, IDataProtectionProvider dataProtectionProvider)
    {
        _authService = authService;
        _loginTwoFactorService = loginTwoFactorService;
        _login2FaProtector = dataProtectionProvider.CreateProtector("otelturizm.login-2fa.v1");
    }

    [HttpGet(UserLoginPath)]
    public IActionResult UserLogin()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return Redirect(ResolvePostLoginRedirectByClaims());
        }

        ViewData["PageCss"] = "user-login";
        ViewData["ReturnUrl"] = GetSafeReturnUrl();
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
    [EnableRateLimiting("auth-strict")]
    public async Task<IActionResult> UserLogin(string loginEmail, string loginPassword, bool rememberMe = false, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(loginEmail) || string.IsNullOrWhiteSpace(loginPassword))
        {
            TempData["UserLoginError"] = "E-posta ve sifre zorunludur.";
            return Redirect(UserLoginPathWithReturnUrl());
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
            return Redirect(UserLoginPathWithReturnUrl());
        }
        catch (Exception ex)
        {
            TempData["UserLoginError"] = $"Veritabani baglantisi veya giris dogrulama sirasinda hata olustu: {ex.Message}";
            return Redirect(UserLoginPathWithReturnUrl());
        }

        if (user is null)
        {
            TempData["UserLoginError"] = "Giris bilgileri hatali veya hesap aktif degil.";
            return Redirect(UserLoginPathWithReturnUrl());
        }

        var twoFactorRedirect = await HandleTwoFactorAsync(user, rememberMe, UserLoginPath, "UserLoginError", cancellationToken);
        if (twoFactorRedirect is not null)
        {
            return twoFactorRedirect;
        }

        await SignInAsync(user, rememberMe);
        await _authService.RecordLoginAsync(user.UserId, user.AccountType, HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString(), Request.Headers.UserAgent.ToString(), cancellationToken);
        return Redirect(ResolvePostLoginRedirect(user));
    }

    [HttpGet("/kullanici-giris-2fa")]
    public IActionResult UserLoginTwoFactor()
    {
        ViewData["PageCss"] = "user-login";
        if (!TryReadLogin2FaCookie(out _, out _, out _, out var loginPath, out var channel, out var destinationHint))
        {
            TempData["UserLoginError"] = "Güvenlik doğrulaması bulunamadı. Lütfen tekrar giriş yapın.";
            return Redirect(UserLoginPath);
        }

        return View("~/Views/Login/UserLogin2FA.cshtml", BuildTwoFactorViewModel(channel, destinationHint));
    }

    [HttpPost("/kullanici-giris-2fa")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("auth-strict")]
    public async Task<IActionResult> UserLoginTwoFactorPost(LoginTwoFactorViewModel model, CancellationToken cancellationToken = default)
    {
        ViewData["PageCss"] = "user-login";
        if (!TryReadLogin2FaCookie(out var userId, out var rememberMe, out var redirectPath, out var loginPath, out _, out _))
        {
            TempData["UserLoginError"] = "Güvenlik doğrulaması bulunamadı. Lütfen tekrar giriş yapın.";
            return Redirect(UserLoginPath);
        }

        if (!ModelState.IsValid)
        {
            if (TryReadLogin2FaCookie(out _, out _, out _, out _, out var currentChannel, out var currentDestinationHint))
            {
                var viewModel = BuildTwoFactorViewModel(currentChannel, currentDestinationHint);
                viewModel.Code = model.Code;
                return View("~/Views/Login/UserLogin2FA.cshtml", viewModel);
            }

            return View("~/Views/Login/UserLogin2FA.cshtml", model);
        }

        var verify = await _loginTwoFactorService.VerifyCodeAsync(userId, model.Code, cancellationToken);
        if (!verify.Success)
        {
            TempData["UserLoginError"] = verify.Message;
            return Redirect("/kullanici-giris-2fa");
        }

        var user = await _authService.GetUserSessionByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            TempData["UserLoginError"] = "Giriş bilgileri alınamadı. Lütfen tekrar deneyin.";
            return Redirect(UserLoginPath);
        }

        await SignInAsync(user, rememberMe);
        await _authService.RecordLoginAsync(user.UserId, user.AccountType, HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString(), Request.Headers.UserAgent.ToString(), cancellationToken);
        ClearLogin2FaCookie();
        return Redirect(string.IsNullOrWhiteSpace(redirectPath) ? GetRedirectPath(user) : redirectPath);
    }

    [HttpPost("/kullanici-giris-2fa/tekrar-gonder")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("auth-strict")]
    public async Task<IActionResult> ResendUserLoginTwoFactor(CancellationToken cancellationToken = default)
    {
        if (!TryReadLogin2FaCookie(out var userId, out _, out _, out var loginPath, out _, out var destinationHint))
        {
            TempData["UserLoginError"] = "Güvenlik doğrulaması bulunamadı. Lütfen tekrar giriş yapın.";
            return Redirect(UserLoginPath);
        }

        var sendResult = await _loginTwoFactorService.SendCodeAsync(
            userId,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString(),
            cancellationToken);

        TempData[sendResult.Success ? "UserLoginSuccess" : "UserLoginError"] = sendResult.Message;
        if (sendResult.Success)
        {
            var challengeInfo = await _authService.GetTwoFactorChallengeInfoAsync(userId, cancellationToken);
            var refreshedHint = challengeInfo.Success ? challengeInfo.DestinationHint : destinationHint;
            var refreshedChannel = challengeInfo.Success ? challengeInfo.Channel : sendResult.Channel;
            UpdateLogin2FaCookieChannel(userId, loginPath, refreshedChannel, refreshedHint);
        }
        return Redirect("/kullanici-giris-2fa");
    }

    private void SetLogin2FaCookie(long userId, bool rememberMe, string redirectPath, string loginPath, string channel, string destinationHint)
    {
        var payload = $"{userId}|{(rememberMe ? 1 : 0)}|{Uri.EscapeDataString(redirectPath ?? string.Empty)}|{DateTime.UtcNow:O}|{Uri.EscapeDataString(loginPath ?? UserLoginPath)}|{channel}|{Uri.EscapeDataString(destinationHint ?? string.Empty)}";
        var protectedValue = _login2FaProtector.Protect(payload);
        Response.Cookies.Append(Login2FaCookieName, protectedValue, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            IsEssential = true,
            Expires = DateTimeOffset.UtcNow.AddMinutes(10)
        });
    }

    private bool TryReadLogin2FaCookie(out long userId, out bool rememberMe, out string redirectPath, out string loginPath, out string channel, out string destinationHint)
    {
        userId = 0;
        rememberMe = false;
        redirectPath = string.Empty;
        loginPath = UserLoginPath;
        channel = "email";
        destinationHint = string.Empty;

        if (!Request.Cookies.TryGetValue(Login2FaCookieName, out var raw) || string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        string unprotected;
        try
        {
            unprotected = _login2FaProtector.Unprotect(raw);
        }
        catch
        {
            return false;
        }

        var parts = unprotected.Split('|');
        if (parts.Length < 4 || !long.TryParse(parts[0], out userId))
        {
            return false;
        }

        rememberMe = string.Equals(parts[1], "1", StringComparison.Ordinal);
        redirectPath = Uri.UnescapeDataString(parts[2] ?? string.Empty);
        if (parts.Length >= 5)
        {
            loginPath = Uri.UnescapeDataString(parts[4] ?? string.Empty);
        }
        if (parts.Length >= 6)
        {
            channel = parts[5];
        }
        if (parts.Length >= 7)
        {
            destinationHint = Uri.UnescapeDataString(parts[6] ?? string.Empty);
        }
        return userId > 0;
    }

    private void UpdateLogin2FaCookieChannel(long userId, string loginPath, string channel, string destinationHint)
    {
        if (!TryReadLogin2FaCookie(out _, out var rememberMe, out var redirectPath, out _, out _, out _))
        {
            return;
        }

        SetLogin2FaCookie(userId, rememberMe, redirectPath, loginPath, channel, destinationHint);
    }

    private void ClearLogin2FaCookie()
    {
        Response.Cookies.Delete(Login2FaCookieName);
    }

    private async Task<IActionResult?> HandleTwoFactorAsync(UserSessionModel user, bool rememberMe, string loginPath, string errorTempDataKey, CancellationToken cancellationToken)
    {
        if (!user.TwoFactorEnabled)
        {
            return null;
        }

        var challengeInfo = await _authService.GetTwoFactorChallengeInfoAsync(user.UserId, cancellationToken);
        if (!challengeInfo.Success)
        {
            TempData[errorTempDataKey] = challengeInfo.Message;
            return Redirect(loginPath);
        }

        var sendResult = await _loginTwoFactorService.SendCodeAsync(
            user.UserId,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString(),
            cancellationToken);

        if (!sendResult.Success)
        {
            TempData[errorTempDataKey] = sendResult.Message;
            return Redirect(loginPath);
        }

        SetLogin2FaCookie(user.UserId, rememberMe, ResolvePostLoginRedirect(user), loginPath, challengeInfo.Channel, challengeInfo.DestinationHint);
        return Redirect("/kullanici-giris-2fa");
    }

    private static LoginTwoFactorViewModel BuildTwoFactorViewModel(string channel, string destinationHint)
    {
        var normalizedChannel = string.Equals(channel, "whatsapp", StringComparison.OrdinalIgnoreCase)
            ? "whatsapp"
            : "email";

        return new LoginTwoFactorViewModel
        {
            Channel = normalizedChannel,
            ChannelLabel = normalizedChannel == "whatsapp" ? "WhatsApp" : "E-posta",
            DestinationHint = destinationHint ?? string.Empty,
            InlineHint = normalizedChannel == "whatsapp"
                ? $"Girişinizi tamamlamak için WhatsApp üzerinden gönderilen güvenlik kodunu girin. {destinationHint}".Trim()
                : $"Girişinizi tamamlamak için e-posta adresinize gönderilen güvenlik kodunu girin. {destinationHint}".Trim()
        };
    }

    [HttpPost(AdminLoginPath)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AdminLogin(string adminEmail, string adminPassword, bool rememberMe = true, CancellationToken cancellationToken = default)
    {
        adminEmail = (adminEmail ?? string.Empty).Trim();
        adminPassword = (adminPassword ?? string.Empty).Trim();

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

        var twoFactorRedirect = await HandleTwoFactorAsync(user, rememberMe, AdminLoginPath, "AdminLoginError", cancellationToken);
        if (twoFactorRedirect is not null)
        {
            return twoFactorRedirect;
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
        catch (Exception ex)
        {
            TempData["FirmaLoginError"] = $"Veritabanı bağlantısı veya firma giriş doğrulaması sırasında hata oluştu: {ex.Message}";
            return Redirect(FirmaLoginPath);
        }

        if (user is null)
        {
            TempData["FirmaLoginError"] = "Firma hesabı bulunamadı, henüz onaylanmadı veya şifre yanlış.";
            return Redirect(FirmaLoginPath);
        }

        var twoFactorRedirect = await HandleTwoFactorAsync(user, rememberMe, FirmaLoginPath, "FirmaLoginError", cancellationToken);
        if (twoFactorRedirect is not null)
        {
            return twoFactorRedirect;
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
        catch (Exception ex)
        {
            TempData["PartnerLoginError"] = $"Veritabani baglantisi veya partner giris dogrulama sirasinda hata olustu: {ex.Message}";
            return Redirect(PartnerLoginPath);
        }

        if (user is null)
        {
            TempData["PartnerLoginError"] = "Partner hesabi bulunamadi veya sifre yanlis.";
            return Redirect(PartnerLoginPath);
        }

        var twoFactorRedirect = await HandleTwoFactorAsync(user, rememberMe, PartnerLoginPath, "PartnerLoginError", cancellationToken);
        if (twoFactorRedirect is not null)
        {
            return twoFactorRedirect;
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
        HttpContext.Response.Cookies.Delete(Login2FaCookieName);
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
            "developer" => Url.Action("Index", "DeveloperPanel") ?? "/panel/developer",
            "department" or "departman" => Url.Action("Dashboard", "DepartmentPanel") ?? "/panel/departman/dashboard",
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
            "developer" => Url.Action("Index", "DeveloperPanel") ?? "/panel/developer",
            "department" or "departman" => Url.Action("Dashboard", "DepartmentPanel") ?? "/panel/departman/dashboard",
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

    private string? GetSafeReturnUrl()
    {
        var fromForm = Request.HasFormContentType ? Request.Form["ReturnUrl"].FirstOrDefault() : null;
        var fromQuery = Request.Query["ReturnUrl"].FirstOrDefault();
        var candidate = !string.IsNullOrWhiteSpace(fromForm) ? fromForm : fromQuery;
        return !string.IsNullOrWhiteSpace(candidate) && Url.IsLocalUrl(candidate) ? candidate : null;
    }

    private string UserLoginPathWithReturnUrl()
    {
        var returnUrl = GetSafeReturnUrl();
        return string.IsNullOrWhiteSpace(returnUrl)
            ? UserLoginPath
            : $"{UserLoginPath}?ReturnUrl={Uri.EscapeDataString(returnUrl)}";
    }

    private static bool CanAccessSalesReturnUrl(UserSessionModel user, string returnUrl)
        => returnUrl.StartsWith("/panel/satis", StringComparison.OrdinalIgnoreCase)
           && (string.Equals(user.AccountType, "sales", StringComparison.OrdinalIgnoreCase)
               || (!string.IsNullOrWhiteSpace(user.UserRole) && user.UserRole.StartsWith("sales_", StringComparison.OrdinalIgnoreCase)));

    private static bool CanAccessSalesReturnUrlByClaims(ClaimsPrincipal principal, string returnUrl)
    {
        if (!returnUrl.StartsWith("/panel/satis", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var accountType = principal.FindFirstValue(AuthClaimTypes.AccountType);
        var userRole = principal.FindFirstValue(AuthClaimTypes.UserRole);
        return string.Equals(accountType, "sales", StringComparison.OrdinalIgnoreCase)
            || (!string.IsNullOrWhiteSpace(userRole) && userRole.StartsWith("sales_", StringComparison.OrdinalIgnoreCase));
    }

    private string ResolvePostLoginRedirect(UserSessionModel user)
    {
        var returnUrl = GetSafeReturnUrl();
        if (string.IsNullOrWhiteSpace(returnUrl))
        {
            return GetRedirectPath(user);
        }

        if (returnUrl.StartsWith("/panel/satis", StringComparison.OrdinalIgnoreCase))
        {
            return CanAccessSalesReturnUrl(user, returnUrl) ? returnUrl : GetRedirectPath(user);
        }

        return returnUrl;
    }

    private string ResolvePostLoginRedirectByClaims()
    {
        var returnUrl = GetSafeReturnUrl();
        if (string.IsNullOrWhiteSpace(returnUrl))
        {
            return GetRedirectPathByClaims();
        }

        if (returnUrl.StartsWith("/panel/satis", StringComparison.OrdinalIgnoreCase))
        {
            return CanAccessSalesReturnUrlByClaims(User, returnUrl) ? returnUrl : GetRedirectPathByClaims();
        }

        return returnUrl;
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





