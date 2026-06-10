using System.Text;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using System.Diagnostics;
using System.Globalization;
using System.IO.Compression;
using System.Threading.RateLimiting;
using System.Security.Claims;
using otelturizmnew.Constants;
using otelturizmnew.Data;
using otelturizmnew.Middleware;
using otelturizmnew.Services;
using otelturizmnew.Services.Abstractions;
using otelturizmnew.Services.Health;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using otelturizmnew.Infrastructure;
using otelturizmnew.Resources;

var builder = WebApplication.CreateBuilder(args);

// p52: Serilog (JSON rolling file) - mevcut ILogger kullanımını bozmadan
var logDir = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "logs");
Directory.CreateDirectory(logDir);
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.File(new RenderedCompactJsonFormatter(),
        Path.Combine(logDir, "app-.json"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 14,
        shared: true,
        flushToDiskInterval: TimeSpan.FromSeconds(2))
    .CreateLogger();

builder.Host.UseSerilog();

// Dev ortamında bazı makinelerde HTTPS/HTTP2 bağlantıları (net::ERR_*) yüzünden css/js düşebiliyor.
// launchSettings.json'daki portları kullan
builder.WebHost.ConfigureKestrel(options =>
{
    if (builder.Environment.IsDevelopment())
    {
        // HTTP/1.1 protokolünü kullan - HTTP/2 sorunlarını önle
        options.ConfigureEndpointDefaults(listenOptions =>
        {
            listenOptions.Protocols = HttpProtocols.Http1;
        });
    }
});

// Add services to the container.
builder.Services.AddMemoryCache();
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[]
    {
        "application/json",
        "application/xml",
        "text/plain",
        "text/xml",
        "image/svg+xml"
    });
});
builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Fastest;
});
builder.Services.Configure<GzipCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Fastest;
});

var mvcBuilder = builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
})
.AddViewLocalization(Microsoft.AspNetCore.Mvc.Razor.LanguageViewLocationExpanderFormat.Suffix);

// Runtime view compile only in local Development — Production must use build-time Razor views.
if (builder.Environment.IsDevelopment() && !builder.Environment.IsProduction())
{
    mvcBuilder.AddRazorRuntimeCompilation(options =>
    {
        var mainAssembly = typeof(SharedResources).Assembly;
        if (!string.IsNullOrWhiteSpace(mainAssembly.Location))
        {
            options.AdditionalReferencePaths.Add(mainAssembly.Location);
        }
    });
}

builder.Services.AddHttpContextAccessor();

// Marker type: SharedResources (.resx under Resources/) — no ResourcesPath so base name matches embedded otelturizmnew.Resources.SharedResources
builder.Services.AddLocalization();
var supportedCultures = new[]
{
    new CultureInfo("tr-TR"),
    new CultureInfo("en-US"),
    new CultureInfo("en-GB"),
    new CultureInfo("de-DE"),
    new CultureInfo("fr-FR"),
    new CultureInfo("es-ES"),
    new CultureInfo("ru-RU")
};
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.DefaultRequestCulture = new RequestCulture("tr-TR");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;

    // Path prefix only. Cookie, Accept-Language and ?lang= disabled.
    options.RequestCultureProviders = new IRequestCultureProvider[]
    {
        new RoutePrefixRequestCultureProvider()
    };
});

builder.Services.AddOutputCache(options =>
{
    // Varsayılan: kısa süreli public cache; auth’lu kullanıcıda cache yok.
    options.AddBasePolicy(policy =>
    {
        policy.Expire(TimeSpan.FromSeconds(30));
        policy.Tag("public");
        policy.SetVaryByQuery(new[] { "*" });
        policy.SetVaryByHeader(new[] { "X-Ot-Culture" });
        policy.Cache();
        policy.With(context => context.HttpContext.User?.Identity?.IsAuthenticated != true);
    });

    options.AddPolicy("public-short", policy =>
    {
        policy.Expire(TimeSpan.FromSeconds(20));
        policy.Tag("public-short");
        policy.SetVaryByQuery(new[] { "*" });
        policy.SetVaryByHeader(new[] { "X-Ot-Culture" });
        policy.Cache();
        policy.With(context => context.HttpContext.User?.Identity?.IsAuthenticated != true);
    });

    options.AddPolicy("public-medium", policy =>
    {
        policy.Expire(TimeSpan.FromSeconds(60));
        policy.Tag("public-medium");
        policy.SetVaryByQuery(new[] { "*" });
        policy.SetVaryByHeader(new[] { "X-Ot-Culture" });
        policy.Cache();
        policy.With(context => context.HttpContext.User?.Identity?.IsAuthenticated != true);
    });
});

builder.Services.AddDataProtection();
builder.Services.AddScoped<SqlMigrationRunner>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAddressLookupService, AddressLookupService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IAdminEmailRoutingService, AdminEmailRoutingService>();
builder.Services.AddScoped<IAdminRbacService, AdminRbacService>();
builder.Services.AddScoped<IDepartmentPanelService, DepartmentPanelService>();
builder.Services.AddScoped<ICurrencyFormatter, CurrencyFormatter>();
builder.Services.AddScoped<IUserPreferenceService, UserPreferenceService>();
builder.Services.AddScoped<IDawnSurpriseService, DawnSurpriseService>();
builder.Services.AddScoped<IPublicTextService, PublicTextService>();
builder.Services.AddScoped<ITimeZoneService, TimeZoneService>();
builder.Services.AddScoped<ICacheSingleFlight, CacheSingleFlight>();
builder.Services.AddSingleton<ISlowSqlTracker, SlowSqlTracker>();
builder.Services.AddScoped<IIdempotencyService, IdempotencyService>();
builder.Services.AddScoped<IAdminHotelManagementService, AdminHotelManagementService>();
builder.Services.AddScoped<IHotelCompletenessService, HotelCompletenessService>();
builder.Services.AddScoped<IAdminHomepageHotelsService, AdminHomepageHotelsService>();
builder.Services.AddScoped<IAdminOzelGunlerService, AdminOzelGunlerService>();
builder.Services.AddScoped<IAdminPuanYonetimiService, AdminPuanYonetimiService>();
builder.Services.AddScoped<IHotelPointsService, HotelPointsService>();
builder.Services.AddScoped<IAdminLocationService, AdminLocationService>();
builder.Services.AddScoped<IAdminLocationService, AdminLocationService>();
builder.Services.AddScoped<ICampaignService, CampaignService>();
builder.Services.AddScoped<IDeneyimlerService, DeneyimlerService>();
builder.Services.AddScoped<ISeyahatPlanlamaService, SeyahatPlanlamaService>();
builder.Services.AddScoped<IContractContentService, ContractContentService>();
builder.Services.AddScoped<otelturizmnew.Services.Abstractions.IPanelThemeService, otelturizmnew.Services.PanelThemeService>();
builder.Services.AddScoped<IDevelopmentRequestService, DevelopmentRequestService>();
builder.Services.AddScoped<IDeveloperFeedbackService, DeveloperFeedbackService>();
builder.Services.AddScoped<IFirmaService, FirmaService>();
builder.Services.AddScoped<IHotelService, HotelService>();
builder.Services.AddScoped<IHotelPricingReadService, HotelPricingReadService>();
builder.Services.AddScoped<IHeaderBildiriService, HeaderBildiriService>();
builder.Services.AddScoped<IUtilityPulseService, UtilityPulseService>();
builder.Services.AddScoped<IOzelGunService, OzelGunService>();
builder.Services.AddScoped<ISmartRouteService, SmartRouteService>();
builder.Services.AddScoped<IFavoritePriceAlertService, FavoritePriceAlertService>();
builder.Services.AddScoped<IPhoneVerificationService, PhoneVerificationService>();
builder.Services.AddScoped<IEmailTemplateService, EmailTemplateService>();
builder.Services.AddScoped<IEmailQueueService, EmailQueueService>();
builder.Services.AddHostedService<EmailDeliveryBackgroundService>();
builder.Services.AddHostedService<FavoritePriceAlertBackgroundService>();
builder.Services.AddHostedService<PricingRetentionBackgroundService>();
builder.Services.AddHostedService<ReservationsArchiveBackgroundService>();
builder.Services.AddScoped<IUploadAuditService, UploadAuditService>();
builder.Services.AddScoped<IImageStorageService, ImageStorageService>();
builder.Services.AddScoped<ISecureFileService, SecureFileService>();
builder.Services.AddScoped<IMessageCenterService, MessageCenterService>();
builder.Services.AddScoped<ILocationLogService, LocationLogService>();
builder.Services.AddScoped<IPartnerService, PartnerService>();
builder.Services.AddScoped<IPlatformPackageService, PlatformPackageService>();
builder.Services.AddScoped<IPublicReservationService, PublicReservationService>();
builder.Services.AddScoped<IReservationDraftService, ReservationDraftService>();
builder.Services.AddScoped<ISalesService, SalesService>();
builder.Services.AddScoped<ISitemapService, SitemapService>();
builder.Services.AddSingleton<InternationalSeoService>();
builder.Services.AddScoped<IAdminSupportArticleService, AdminSupportArticleService>();
builder.Services.AddScoped<IUserFavoriteService, UserFavoriteService>();
builder.Services.AddSingleton<PaymentCardCryptoService>();
builder.Services.AddScoped<IPaymentCardService, PaymentCardService>();
builder.Services.AddScoped<IUserPanelService, UserPanelService>();
builder.Services.AddScoped<IUserLoyaltyPointsService, UserLoyaltyPointsService>();
builder.Services.AddScoped<ISessionSecurityService, SessionSecurityService>();
builder.Services.AddScoped<ILoginTwoFactorService, LoginTwoFactorService>();
builder.Services.AddScoped<ISupportService, SupportService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<IDevelopmentAccessService, DevelopmentAccessService>();
builder.Services.AddScoped<IUploadScanService, NoOpUploadScanService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<otelturizmnew.Utils.ExternalServiceCircuitBreaker>();
builder.Services.AddSingleton<IPublicGrowthSignalsService, PublicGrowthSignalsService>();
builder.Services.AddSingleton<CommerceMetricsAccumulator>();
builder.Services.AddSingleton<HotelPresenceTracker>();
builder.Services.AddSingleton<IReservationVelocityGuard, ReservationVelocityGuard>();
builder.Services.AddSingleton<IGrowthGovernanceService, GrowthGovernanceService>();
builder.Services.AddSingleton<IDeadLinkRedirectService, DeadLinkRedirectService>();
builder.Services.AddSingleton<SqlConnectionHealthCheck>();
builder.Services.AddHealthChecks()
    .AddCheck<SqlConnectionHealthCheck>("sql_server");
builder.Services.AddScoped<IOutboxPublisher, OutboxPublisherStub>();
builder.Services.AddSingleton<IFeatureFlagService, FeatureFlagService>();
builder.Services.AddSingleton<IPaymentOrchestrationAdvisor, PaymentOrchestrationAdvisor>();

builder.Services.AddHttpClient<IWeatherService, WeatherService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(8);
});
builder.Services.AddHttpClient<IWhatsAppCloudApiService, WhatsAppCloudApiService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
});
builder.Services.AddHostedService<SitemapRefreshBackgroundService>();
builder.Services.AddHostedService<UploadOrphanCleanupBackgroundService>();
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 300 * 1024 * 1024;
    options.MultipartHeadersLengthLimit = 64 * 1024;
    options.MultipartBoundaryLengthLimit = 256;
    options.MemoryBufferThreshold = 1024 * 1024;
    options.ValueCountLimit = 4096;
    options.ValueLengthLimit = 1024 * 1024;
});
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 300 * 1024 * 1024;
});
builder.Services.AddAntiforgery(options =>
{
    options.Cookie.Name = "Otelturizm.AntiCsrf";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.IsEssential = true;
    options.HeaderName = "X-CSRF-TOKEN";
});
builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.HttpOnly = HttpOnlyPolicy.Always;
    options.MinimumSameSitePolicy = SameSiteMode.Lax;
    options.Secure = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
    // p80: Cookie hardening - tutarlılık (eksik SameSite/HttpOnly/Secure için merkezî davranış)
    options.OnAppendCookie = context =>
    {
        context.CookieOptions.HttpOnly = true;

        if (context.CookieOptions.SameSite == SameSiteMode.Unspecified)
        {
            context.CookieOptions.SameSite = SameSiteMode.Lax;
        }

        if (!builder.Environment.IsDevelopment())
        {
            context.CookieOptions.Secure = true;
        }
    };
    options.OnDeleteCookie = context =>
    {
        context.CookieOptions.HttpOnly = true;
        if (context.CookieOptions.SameSite == SameSiteMode.Unspecified)
        {
            context.CookieOptions.SameSite = SameSiteMode.Lax;
        }
        if (!builder.Environment.IsDevelopment())
        {
            context.CookieOptions.Secure = true;
        }
    };
});

// p79: HSTS - preload değerlendirmesi için config ile aç/kapat.
builder.Services.AddHsts(options =>
{
    options.MaxAge = TimeSpan.FromDays(365);
    options.IncludeSubDomains = builder.Configuration.GetValue("Security:HstsIncludeSubDomains", false);
    options.Preload = builder.Configuration.GetValue("Security:HstsPreload", false);
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
    options.ForwardLimit = null;
    options.AllowedHosts.Clear();
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.Configure<Microsoft.AspNetCore.HttpsPolicy.HttpsRedirectionOptions>(options =>
{
    options.HttpsPort = 443;
});

static string BuildReservationCreatePartitionKey(HttpContext httpContext)
{
    var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    var user = httpContext.User?.Identity?.Name;
    if (!string.IsNullOrWhiteSpace(user))
    {
        return ip + ":u:" + user.Trim();
    }

    if (httpContext.Request.Cookies.TryGetValue("Otelturizm.ReservationDraftKey", out var sessionKey) && !string.IsNullOrWhiteSpace(sessionKey))
    {
        return ip + ":s:" + sessionKey.Trim();
    }

    return ip + ":anon";
}

static string BuildQuoteAndGrowthPartitionKey(HttpContext httpContext)
{
    var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    if (httpContext.Request.Cookies.TryGetValue("Otelturizm.ClientFp", out var fp) && !string.IsNullOrWhiteSpace(fp))
    {
        var fpTrim = fp.ToString().Trim();
        var slice = fpTrim.Length <= 32 ? fpTrim : fpTrim[..32];
        return $"{ip}:{slice}";
    }

    return ip;
}

static bool IsLoopbackRequest(HttpContext context)
{
    var host = context.Request.Host.Host ?? string.Empty;
    return string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase)
        || string.Equals(host, "127.0.0.1", StringComparison.OrdinalIgnoreCase)
        || string.Equals(host, "::1", StringComparison.OrdinalIgnoreCase)
        || host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase);
}

static bool IsForwardedHttpsRequest(HttpRequest request)
{
    var forwardedProto = request.Headers["X-Forwarded-Proto"].ToString();
    if (!string.IsNullOrWhiteSpace(forwardedProto))
    {
        var proto = forwardedProto.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();
        if (string.Equals(proto, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
    }

    if (string.Equals(request.Headers["X-Forwarded-Ssl"].ToString(), "on", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(request.Headers["X-Url-Scheme"].ToString(), Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(request.Headers["Front-End-Https"].ToString(), "on", StringComparison.OrdinalIgnoreCase))
    {
        return true;
    }

    var cfVisitor = request.Headers["CF-Visitor"].ToString();
    return !string.IsNullOrWhiteSpace(cfVisitor) &&
        cfVisitor.IndexOf("\"https\"", StringComparison.OrdinalIgnoreCase) >= 0;
}

/// <summary>Genel otel/kampanya sayfaları indekslenebilir kalsın; panel ve giriş yollarında noindex.</summary>
static bool ShouldSendNoRobotsHeader(PathString path)
{
    var p = path.Value ?? string.Empty;
    if (string.IsNullOrEmpty(p))
    {
        return false;
    }

    return p.StartsWith("/panel", StringComparison.OrdinalIgnoreCase)
           || p.StartsWith("/admin", StringComparison.OrdinalIgnoreCase)
           || p.StartsWith("/gelisim", StringComparison.OrdinalIgnoreCase)
           || p.StartsWith("/secure-files", StringComparison.OrdinalIgnoreCase)
           || p.StartsWith("/paneltema", StringComparison.OrdinalIgnoreCase)
           || p.StartsWith("/development", StringComparison.OrdinalIgnoreCase)
           || string.Equals(p, "/cikis-yap", StringComparison.OrdinalIgnoreCase)
           || p.StartsWith("/kullanici-giris", StringComparison.OrdinalIgnoreCase)
           || p.StartsWith("/partner-giris", StringComparison.OrdinalIgnoreCase)
           || p.StartsWith("/firma-giris", StringComparison.OrdinalIgnoreCase)
           || string.Equals(p, "/admin-giris", StringComparison.OrdinalIgnoreCase);
}

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        // p56: 429 gövdesi standardı (JSON) + Retry-After
        try
        {
            if (context.Lease is not null
                && context.Lease.TryGetMetadata("RetryAfter", out var retryAfterObj)
                && retryAfterObj is TimeSpan retryAfter)
            {
                context.HttpContext.Response.Headers.RetryAfter = ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
            }
        }
        catch
        {
            // fail-safe
        }

        var correlationId = context.HttpContext.Items.TryGetValue("CorrelationId", out var cidObj) ? cidObj as string : null;
        context.HttpContext.Response.ContentType = "application/json; charset=utf-8";
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            error = "rate_limited",
            message = "Çok fazla istek yaptınız. Lütfen biraz sonra tekrar deneyin.",
            correlationId
        }, cancellationToken);
    };

    options.AddPolicy("public-burst", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 240,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));

    options.AddPolicy("auth-strict", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));

    options.AddPolicy("quote-strict", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: BuildQuoteAndGrowthPartitionKey(httpContext),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 120,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));

    options.AddPolicy("growth-ingest", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: BuildQuoteAndGrowthPartitionKey(httpContext),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 180,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));

    options.AddPolicy("location-strict", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 30,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));

    // p107: rezervasyon create için ayrı policy (double submit/abuse guard)
    options.AddPolicy("reservation-create", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: BuildReservationCreatePartitionKey(httpContext),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 8,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));

    // Otel detay presence heartbeat — abuse / bellek şişmesine karşı sıkı tavan
    options.AddPolicy("presence-beat", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 90,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));

    // CSP rapor uç noktası — log şişmesi / spam
    options.AddPolicy("csp-ingest", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 45,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));

    // Sağlık uçları — probe spam / DoS yüzeyini daralt
    options.AddPolicy("health-probe", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 180,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));

    // Tarayıcı JS hata raporu — spam ve payload şişmesine karşı sıkı
    options.AddPolicy("client-error-ingest", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 24,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));
});

static string ResolvePanelLoginPath(PathString path) =>
    path.StartsWithSegments("/panel/partner", StringComparison.OrdinalIgnoreCase)
        ? "/partner-giris"
        : path.StartsWithSegments("/panel/firma", StringComparison.OrdinalIgnoreCase)
            ? "/firma-giris"
            : path.StartsWithSegments("/panel/satis", StringComparison.OrdinalIgnoreCase)
                ? "/kullanici-giris"
                : path.StartsWithSegments("/admin", StringComparison.OrdinalIgnoreCase)
                    ? "/admin-giris"
                    : "/kullanici-giris";

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/kullanici-giris";
        options.AccessDeniedPath = "/kullanici-giris";
        options.Cookie.Name = "Otelturizm.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.IsEssential = true;
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.Cookie.MaxAge = options.ExpireTimeSpan;
        options.Events = new CookieAuthenticationEvents
        {
            OnRedirectToLogin = context =>
            {
                var targetPath = ResolvePanelLoginPath(context.Request.Path);
                var returnUrl = Uri.EscapeDataString(context.Request.Path + context.Request.QueryString);
                context.Response.Redirect($"{targetPath}?ReturnUrl={returnUrl}");
                return Task.CompletedTask;
            },
            OnRedirectToAccessDenied = context =>
            {
                var targetPath = ResolvePanelLoginPath(context.Request.Path);
                if (context.Request.Path.StartsWithSegments("/panel/satis", StringComparison.OrdinalIgnoreCase))
                {
                    var returnUrl = Uri.EscapeDataString(context.Request.Path + context.Request.QueryString);
                    context.Response.Redirect($"{targetPath}?ReturnUrl={returnUrl}");
                }
                else
                {
                    context.Response.Redirect(targetPath);
                }

                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SalesPanel", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireAssertion(context =>
        {
            var accountType = context.User.FindFirstValue(AuthClaimTypes.AccountType);
            var userRole = context.User.FindFirstValue(AuthClaimTypes.UserRole);
            return string.Equals(accountType, "sales", StringComparison.OrdinalIgnoreCase)
                || (!string.IsNullOrWhiteSpace(userRole) && userRole.StartsWith("sales_", StringComparison.OrdinalIgnoreCase));
        });
    });
});
var app = builder.Build();

// One-shot: run SQL migrations and exit (local DB sync)
if (args.Any(static a => string.Equals(a, "--run-sql-migrations", StringComparison.OrdinalIgnoreCase)))
{
    try
    {
        Console.WriteLine("SQL migrations baslatiliyor...");
        await app.Services.RunSqlMigrationsAsync();
        Console.WriteLine("SQL migrations tamamlandi.");
        return;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine("SQL migrations hata verdi:");
        Console.Error.WriteLine(ex.ToString());
        Environment.ExitCode = 1;
        return;
    }
}

// One-shot: generate drift repair script + run migrations (local DB full sync)
if (args.Any(static a => string.Equals(a, "--sync-local-schema", StringComparison.OrdinalIgnoreCase)))
{
    try
    {
        Console.WriteLine("Local schema drift repair script uretiliyor...");
        var script = await app.Services.GenerateSchemaDriftRepairScriptAsync();
        var outPath = Path.Combine(app.Environment.ContentRootPath, "Database", "MigrationsSql", "tablo", "migrationlar", $"{DateTime.UtcNow:yyyyMMdd}_sqlserver_local_schema_drift_repair_auto.sql");
        await File.WriteAllTextAsync(outPath, script, Encoding.UTF8);
        Console.WriteLine($"Repair script yazildi: {outPath}");

        Console.WriteLine("SQL migrations baslatiliyor...");
        await app.Services.RunSqlMigrationsAsync();
        Console.WriteLine("SQL migrations tamamlandi.");
        return;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine("Local schema sync hata verdi:");
        Console.Error.WriteLine(ex.ToString());
        Environment.ExitCode = 1;
        return;
    }
}

// One-shot: DB performance maintenance (local)
if (args.Any(static a => string.Equals(a, "--optimize-local-db", StringComparison.OrdinalIgnoreCase)))
{
    try
    {
        Console.WriteLine("Local DB optimize baslatiliyor (stats + index reorganize)...");
        await app.Services.OptimizeSqlServerAsync();
        Console.WriteLine("Local DB optimize tamamlandi.");
        return;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine("Local DB optimize hata verdi:");
        Console.Error.WriteLine(ex.ToString());
        Environment.ExitCode = 1;
        return;
    }
}

var runMigrations = builder.Configuration.GetValue<bool>("Database:RunMigrationsOnStartup", false);
if (runMigrations)
{
    await app.Services.RunSqlMigrationsAsync();
}

// Configure the HTTP request pipeline.
app.UseForwardedHeaders();

// Reverse proxy arkasında HTTPS şeması bazen origin'e HTTP olarak düşebilir.
// Antiforgery/Cookie secure policy kırılmaması için standart proxy header'larından scheme normalize edilir.
app.Use((context, next) =>
{
    if (!context.Request.IsHttps)
    {
        if (IsForwardedHttpsRequest(context.Request))
        {
            context.Request.Scheme = Uri.UriSchemeHttps;
        }
    }

    return next();
});

// Proxy dışından gelen düz HTTP GET/HEAD isteklerinde önce açık hedefle HTTPS'e al.
app.Use((context, next) =>
{
    if (!context.Request.IsHttps &&
        !IsLoopbackRequest(context) &&
        (HttpMethods.IsGet(context.Request.Method) || HttpMethods.IsHead(context.Request.Method)))
    {
        var target = $"https://{context.Request.Host}{context.Request.PathBase}{context.Request.Path}{context.Request.QueryString}";
        context.Response.Redirect(target, permanent: true);
        return Task.CompletedTask;
    }

    return next();
});

// Canonical lowercase paths for Turkish SEO routes (e.g. /Oteller → /oteller).
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value;
    if (!string.IsNullOrEmpty(path))
    {
        string? redirectTarget = null;
        if (path.StartsWith("/Oteller", StringComparison.Ordinal))
        {
            redirectTarget = "/oteller" + path["/Oteller".Length..];
        }
        else if (path.StartsWith("/Kampanyalar", StringComparison.Ordinal))
        {
            redirectTarget = "/kampanyalar" + path["/Kampanyalar".Length..];
        }
        else if (path.StartsWith("/ar/", StringComparison.OrdinalIgnoreCase))
        {
            redirectTarget = InternationalSeoPaths.LocalizePath(path, "tr");
        }

        if (redirectTarget is not null)
        {
            var query = context.Request.QueryString.HasValue ? context.Request.QueryString.Value : string.Empty;
            context.Response.Redirect(redirectTarget + query, permanent: true);
            return;
        }
    }

    await next();
});

// ?lang= query param is ignored; strip it from Turkish canonical routes to prevent locale drift.
app.Use(async (context, next) =>
{
    if (!context.Request.Query.ContainsKey("lang"))
    {
        await next();
        return;
    }

    var path = context.Request.Path.Value ?? "/";
    if (InternationalSeoPaths.HasLocalePathPrefix(path))
    {
        await next();
        return;
    }

    var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(context.Request.QueryString.Value);
    var dict = query
        .Where(kvp => !string.Equals(kvp.Key, "lang", StringComparison.OrdinalIgnoreCase))
        .ToDictionary(k => k.Key, v => v.Value.ToString(), StringComparer.OrdinalIgnoreCase);

    var target = dict.Count == 0
        ? path
        : Microsoft.AspNetCore.WebUtilities.QueryHelpers.AddQueryString(path, dict!);

    context.Response.Redirect(target, permanent: false);
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
app.UseHttpsRedirection();
app.UseResponseCompression();
app.UseRequestLocalization(app.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<RequestLocalizationOptions>>().Value);
app.UseCookiePolicy();
app.UseStaticFiles();

app.Use(async (context, next) =>
{
    var isLoopback = IsLoopbackRequest(context);
    if (isLoopback)
    {
        context.Response.OnStarting(() =>
        {
            context.Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate, max-age=0";
            context.Response.Headers["Pragma"] = "no-cache";
            context.Response.Headers["Expires"] = "0";
            return Task.CompletedTask;
        });
    }

    await next();
});

// p112: 404/4xx için kullanıcı dostu sayfa (SEO + UX)
app.UseStatusCodePagesWithReExecute("/Home/HttpStatus", "?code={0}");

// p65: OutputCache vary-by — path'ten çözülen kültür (tarayıcı Accept-Language / ?lang= yok sayılır).
app.Use((context, next) =>
{
    var path = context.Request.Path.Value ?? "/";
    var pathCulture = InternationalSeoPaths.ResolveCultureFromPath(path);
    var cultureName = pathCulture switch
    {
        "en" => "en-US",
        "de" => "de-DE",
        "fr" => "fr-FR",
        "es" => "es-ES",
        "ru" => "ru-RU",
        _ => "tr-TR"
    };
    context.Request.Headers["X-Ot-Culture"] = cultureName;
    context.Request.Headers["Accept-Language"] = cultureName;
    return next();
});

// p51: Correlation ID (X-Correlation-Id) üretimi + response header'a yazma
// Güvenlik: yalnızca güvenli karakterler (log/header injection riskini azaltır).
static string ResolveCorrelationIdHeader(string? headerValue)
{
    var t = (headerValue ?? string.Empty).Trim();
    if (t.Length is 0 or > 128)
    {
        return Guid.NewGuid().ToString("N");
    }

    foreach (var c in t)
    {
        if (!(char.IsAsciiLetterOrDigit(c) || c is '.' or '_' or ':' or '-'))
        {
            return Guid.NewGuid().ToString("N");
        }
    }

    return t;
}

app.Use(async (context, next) =>
{
    const string headerName = "X-Correlation-Id";
    var incoming = context.Request.Headers.TryGetValue(headerName, out var values) ? values.ToString() : null;
    var correlationId = ResolveCorrelationIdHeader(incoming);

    context.Items["CorrelationId"] = correlationId;
    context.TraceIdentifier = correlationId;

    context.Response.OnStarting(() =>
    {
        if (!context.Response.Headers.ContainsKey(headerName))
        {
            context.Response.Headers[headerName] = correlationId;
        }
        return Task.CompletedTask;
    });

    using (context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("Request").BeginScope(
               new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
    {
        await next();
    }
});

app.UseRouting();
app.UseMiddleware<GrowthFingerprintMiddleware>();
app.UseAuthentication();
app.UseRateLimiter();
app.UseOutputCache();
app.Use(async (context, next) =>
{
    var sw = Stopwatch.StartNew();
    if (!context.Response.Headers.ContainsKey("X-Content-Type-Options"))
    {
        var nonceBytes = new byte[16];
        System.Security.Cryptography.RandomNumberGenerator.Fill(nonceBytes);
        var nonce = Convert.ToBase64String(nonceBytes);
        context.Items["CspNonce"] = nonce;

        // CSP enforce: appsettings Security:CspEnforce, ortam değişkeni Security__CspEnforce, veya prod rollout (tools/Security/CSP-ROLLOUT.md).
        var securityConfig = context.RequestServices.GetRequiredService<IConfiguration>().GetSection("Security");
        var cspEnforce = securityConfig.GetValue("CspEnforce", false);
        if (!cspEnforce && string.Equals(Environment.GetEnvironmentVariable("OTELTURIZM_CSP_ENFORCE"), "true", StringComparison.OrdinalIgnoreCase))
        {
            cspEnforce = true;
        }

        var cspReportEnabled = securityConfig.GetValue("CspReportEnabled", true);

        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
        context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        context.Response.Headers["Permissions-Policy"] = "geolocation=(self), camera=(), microphone=()";
        if (ShouldSendNoRobotsHeader(context.Request.Path))
        {
            context.Response.Headers["X-Robots-Tag"] = "noindex, nofollow, noarchive, nosnippet, noimageindex, notranslate";
        }

        var cspReportDirectives = cspReportEnabled ? " report-uri /csp/report; report-to csp-endpoint;" : string.Empty;
        if (cspReportEnabled)
        {
            // p71: report-to header (legacy clients ignore, modern clients use it)
            var reportUrl = $"{context.Request.Scheme}://{context.Request.Host}/csp/report";
            context.Response.Headers["Report-To"] =
                "{\"group\":\"csp-endpoint\",\"max_age\":86400,\"endpoints\":[{\"url\":\"" + reportUrl + "\"}]}";
        }

        // Üçüncü taraf script/connect (Cloudflare Web Analytics beacon vb.) kapalı — sadece same-origin.
        context.Response.Headers["Content-Security-Policy"] = cspEnforce
            ? "default-src 'self' data: blob:; " +
              $"script-src 'self' 'nonce-{nonce}'; " +
              "style-src 'self' 'unsafe-inline'; " +
              "img-src 'self' data: blob: https:; " +
              "font-src 'self' data:; " +
              "connect-src 'self' wss:; " +
              "frame-src 'self' https://www.youtube.com https://www.youtube-nocookie.com; " +
              "frame-ancestors 'self'; base-uri 'self'; form-action 'self';" +
              cspReportDirectives
            : "default-src 'self' data: blob:; " +
              $"script-src 'self' 'unsafe-inline' 'nonce-{nonce}'; " +
              "style-src 'self' 'unsafe-inline'; " +
              "img-src 'self' data: blob: https:; " +
              "font-src 'self' data:; " +
              "connect-src 'self' wss:; " +
              "frame-src 'self' https://www.youtube.com https://www.youtube-nocookie.com; " +
              "frame-ancestors 'self'; base-uri 'self'; form-action 'self';" +
              cspReportDirectives;

        // Kademeli geçiş: önce Report-Only strict CSP ile nonce coverage ölçülür.
        if (!cspEnforce)
        {
            context.Response.Headers["Content-Security-Policy-Report-Only"] =
                "default-src 'self' data: blob:; " +
                $"script-src 'self' 'nonce-{nonce}'; " +
                "style-src 'self' 'unsafe-inline'; " +
                "img-src 'self' data: blob: https:; " +
                "font-src 'self' data:; " +
                "connect-src 'self' wss:; " +
                "frame-src 'self' https://www.youtube.com https://www.youtube-nocookie.com; " +
                "frame-ancestors 'self'; base-uri 'self'; form-action 'self';" +
                cspReportDirectives;
        }
    }

    var tracker = context.RequestServices.GetRequiredService<ISessionSecurityService>();
    await tracker.TrackAsync(context, context.RequestAborted);

    try
    {
        await next();
    }
    catch (Exception ex)
    {
        try
        {
            var logger = context.RequestServices
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger("UnhandledRequest");
            logger.LogError(ex,
                "UNHANDLED_REQUEST {Method} {Path}{QueryString} (trace={TraceId})",
                context.Request.Method,
                context.Request.Path.Value ?? string.Empty,
                context.Request.QueryString.HasValue ? context.Request.QueryString.Value : string.Empty,
                context.TraceIdentifier);
        }
        catch
        {
            // fail-safe
        }

        try
        {
            var audit = context.RequestServices.GetRequiredService<IAuditLogService>();
            await audit.TryLogExceptionAsync(context, ex, context.RequestAborted);
        }
        catch
        {
            // fail-safe
        }
        throw;
    }
    finally
    {
        sw.Stop();
        try
        {
            var audit = context.RequestServices.GetRequiredService<IAuditLogService>();
            await audit.TryLogApiRequestAsync(context, context.Response.StatusCode, sw.ElapsedMilliseconds, context.RequestAborted);
        }
        catch
        {
            // fail-safe
        }

        // Slow request eşikleme (p40): 1500ms üstünü WARN logla
        if (sw.ElapsedMilliseconds >= 1500)
        {
            try
            {
                var logger = context.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("SlowRequest");

                logger.LogWarning(
                    "SLOW_REQUEST {Method} {Path}{QueryString} -> {StatusCode} in {ElapsedMs}ms (trace={TraceId})",
                    context.Request.Method,
                    context.Request.Path.Value ?? string.Empty,
                    context.Request.QueryString.HasValue ? context.Request.QueryString.Value : string.Empty,
                    context.Response.StatusCode,
                    sw.ElapsedMilliseconds,
                    context.TraceIdentifier);
            }
            catch
            {
                // fail-safe
            }
        }
    }
});
app.UseAuthorization();

app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/uploads/file", StringComparison.OrdinalIgnoreCase))
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }

    await next();
});

// Static assets cache-control (query hashed via asp-append-version)
app.Use(async (context, next) =>
{
    context.Response.OnStarting(() =>
    {
        if (!HttpMethods.IsGet(context.Request.Method) && !HttpMethods.IsHead(context.Request.Method))
        {
            return Task.CompletedTask;
        }

        if (context.Response.StatusCode != StatusCodes.Status200OK)
        {
            return Task.CompletedTask;
        }

        var path = context.Request.Path.Value ?? string.Empty;
        if (path.StartsWith("/assets/", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/vendor/", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/lib/", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/js/", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/css/", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.Headers["Cache-Control"] = IsLoopbackRequest(context)
                ? "no-store, no-cache, must-revalidate, max-age=0"
                : "public,max-age=31536000,immutable";
        }

        return Task.CompletedTask;
    });

    await next();
});

app.MapHealthChecks("/health/platform", new HealthCheckOptions
    {
        ResponseWriter = HealthReportJsonWriter.WriteAsync
    })
    .AllowAnonymous()
    .RequireRateLimiting("health-probe")
    .ExcludeFromDescription();

// p55: Health endpoints
app.MapGet("/health/live", () => Results.Json(new { status = "live" }))
   .AllowAnonymous()
   .RequireRateLimiting("health-probe")
   .ExcludeFromDescription();

app.MapGet("/health/ready", async (IConfiguration cfg, IHostEnvironment env, ILoggerFactory logFactory, CancellationToken ct) =>
{
    var log = logFactory.CreateLogger("HealthReady");
    var cs = cfg.GetConnectionString("DefaultConnection");
    if (string.IsNullOrWhiteSpace(cs))
    {
        return Results.Json(new { status = "not_ready", checks = new { db = "missing_connection_string" } }, statusCode: 503);
    }

    try
    {
        await using var conn = new Microsoft.Data.SqlClient.SqlConnection(cs);
        await conn.OpenAsync(ct);
        await using var cmd = new Microsoft.Data.SqlClient.SqlCommand("SELECT 1;", conn);
        cmd.CommandTimeout = 2;
        await cmd.ExecuteScalarAsync(ct);
        return Results.Json(new { status = "ready", checks = new { db = "ok" } });
    }
    catch (Exception ex)
    {
        log.LogWarning(ex, "health_ready_db_check_failed");
        if (env.IsDevelopment())
        {
            return Results.Json(new { status = "not_ready", checks = new { db = "fail", error = ex.Message } }, statusCode: 503);
        }

        return Results.Json(new { status = "not_ready", checks = new { db = "fail" } }, statusCode: 503);
    }
})
   .AllowAnonymous()
   .RequireRateLimiting("health-probe")
   .ExcludeFromDescription();

app.MapControllers();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets()
    .RequireRateLimiting("public-burst");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets()
    .RequireRateLimiting("public-burst");

app.Run();
