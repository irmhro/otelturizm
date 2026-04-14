using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using otelturizmnew.Data;
using otelturizmnew.Services;
using otelturizmnew.Services.Abstractions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});
builder.Services.AddScoped<SqlMigrationRunner>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAddressLookupService, AddressLookupService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IAdminHotelManagementService, AdminHotelManagementService>();
builder.Services.AddScoped<ICampaignService, CampaignService>();
builder.Services.AddScoped<IFirmaService, FirmaService>();
builder.Services.AddScoped<IHotelService, HotelService>();
builder.Services.AddScoped<IImageStorageService, ImageStorageService>();
builder.Services.AddScoped<IPartnerService, PartnerService>();
builder.Services.AddScoped<ISalesService, SalesService>();
builder.Services.AddScoped<IUserFavoriteService, UserFavoriteService>();
builder.Services.AddScoped<IUserPanelService, UserPanelService>();
builder.Services.AddScoped<ISessionSecurityService, SessionSecurityService>();
builder.Services.AddScoped<ISupportService, SupportService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient<IWeatherService, WeatherService>();
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
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.IsEssential = true;
    options.HeaderName = "X-CSRF-TOKEN";
});
builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.HttpOnly = HttpOnlyPolicy.Always;
    options.MinimumSameSitePolicy = SameSiteMode.Lax;
    options.Secure = CookieSecurePolicy.Always;
});
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/kullanici-giris";
        options.AccessDeniedPath = "/kullanici-giris";
        options.Cookie.Name = "Otelturizm.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.IsEssential = true;
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.Cookie.MaxAge = options.ExpireTimeSpan;
        options.Events = new CookieAuthenticationEvents
        {
            OnRedirectToLogin = context =>
            {
                var targetPath = context.Request.Path.StartsWithSegments("/panel/partner", StringComparison.OrdinalIgnoreCase)
                    ? "/partner-giris"
                    : context.Request.Path.StartsWithSegments("/panel/firma", StringComparison.OrdinalIgnoreCase)
                        ? "/firma-giris"
                        : context.Request.Path.StartsWithSegments("/panel/satis", StringComparison.OrdinalIgnoreCase)
                            ? "/kullanici-giris"
                            : context.Request.Path.StartsWithSegments("/admin", StringComparison.OrdinalIgnoreCase)
                                ? "/admin-giris"
                                : "/kullanici-giris";

                context.Response.Redirect($"{targetPath}?ReturnUrl={Uri.EscapeDataString(context.Request.Path + context.Request.QueryString)}");
                return Task.CompletedTask;
            },
            OnRedirectToAccessDenied = context =>
            {
                var targetPath = context.Request.Path.StartsWithSegments("/panel/partner", StringComparison.OrdinalIgnoreCase)
                    ? "/partner-giris"
                    : context.Request.Path.StartsWithSegments("/panel/firma", StringComparison.OrdinalIgnoreCase)
                        ? "/firma-giris"
                    : context.Request.Path.StartsWithSegments("/admin", StringComparison.OrdinalIgnoreCase)
                        ? "/admin-giris"
                        : "/kullanici-giris";

                context.Response.Redirect(targetPath);
                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

var runMigrations = builder.Configuration.GetValue<bool>("Database:RunMigrationsOnStartup", false);
if (runMigrations)
{
    await app.Services.RunSqlMigrationsAsync();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseCookiePolicy();
app.UseRouting();
app.UseAuthentication();
app.Use(async (context, next) =>
{
    if (!context.Response.Headers.ContainsKey("X-Content-Type-Options"))
    {
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
        context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        context.Response.Headers["Permissions-Policy"] = "geolocation=(), camera=(), microphone=()";
        context.Response.Headers["Content-Security-Policy"] = "default-src 'self' https: data: blob:; script-src 'self' 'unsafe-inline' https:; style-src 'self' 'unsafe-inline' https:; img-src 'self' data: blob: https:; font-src 'self' data: https:; connect-src 'self' https: wss:; frame-ancestors 'self'; base-uri 'self'; form-action 'self';";
    }

    var tracker = context.RequestServices.GetRequiredService<ISessionSecurityService>();
    await tracker.TrackAsync(context, context.RequestAborted);
    await next();
});
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();


