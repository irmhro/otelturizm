using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using otelturizmnew.Constants;
using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Services;

public class SessionSecurityService : ISessionSecurityService
{
    private const string DeviceCookieName = "Otelturizm.DeviceKey";
    private const string SessionCookieName = "Otelturizm.SessionKey";
    private const string LastSeenCookieName = "Otelturizm.LastSeenUtc";
    private readonly string _connectionString;

    public SessionSecurityService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection tanimli degil.");
    }

    public async Task TrackAsync(HttpContext httpContext, CancellationToken cancellationToken = default)
    {
        if (httpContext.User.Identity?.IsAuthenticated != true)
        {
            return;
        }

        var path = httpContext.Request.Path.Value ?? string.Empty;
        if (string.IsNullOrWhiteSpace(path)
            || path.StartsWith("/assets", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/uploads", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/lib", StringComparison.OrdinalIgnoreCase)
            || Path.HasExtension(path))
        {
            return;
        }

        var userIdValue = httpContext.User.FindFirst(AuthClaimTypes.UserId)?.Value;
        if (!long.TryParse(userIdValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var userId) || userId <= 0)
        {
            return;
        }

        var accountType = NormalizeAccountType(httpContext.User.FindFirst(AuthClaimTypes.AccountType)?.Value);
        var rememberMe = string.Equals(httpContext.User.FindFirst(AuthClaimTypes.RememberMe)?.Value, "true", StringComparison.OrdinalIgnoreCase);
        var partnerId = ParseNullableLong(httpContext.User.FindFirst(AuthClaimTypes.PartnerId)?.Value);

        var now = DateTime.UtcNow;
        var deviceKey = EnsurePersistentCookie(httpContext, DeviceCookieName, Guid.NewGuid().ToString("N"), now.AddYears(1));
        var sessionKey = EnsureSessionCookie(httpContext, SessionCookieName, Guid.NewGuid().ToString("N"));
        var lastSeenRaw = EnsureSessionCookie(httpContext, LastSeenCookieName, now.ToString("O", CultureInfo.InvariantCulture));

        var isNewVisit = string.Equals(sessionKey, httpContext.Request.Cookies[SessionCookieName], StringComparison.Ordinal) == false;
        var durationSeconds = 0L;
        if (DateTime.TryParse(lastSeenRaw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var lastSeen))
        {
            var delta = now - lastSeen;
            if (delta.TotalSeconds > 0)
            {
                durationSeconds = (long)Math.Min(delta.TotalSeconds, 300);
            }
        }

        httpContext.Response.Cookies.Append(
            LastSeenCookieName,
            now.ToString("O", CultureInfo.InvariantCulture),
            BuildCookieOptions(now.AddHours(8), true));

        if (!isNewVisit && durationSeconds < 60)
        {
            return;
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = @"
            MERGE kullanici_oturum_istatistikleri AS target
            USING (
                SELECT
                    @userId AS [KULLANICI_ID],
                    @deviceKey AS [CIHAZ_ANAHTARI]
            ) AS source
            ON target.[KULLANICI_ID] = source.[KULLANICI_ID]
               AND target.[CIHAZ_ANAHTARI] = source.[CIHAZ_ANAHTARI]
            WHEN MATCHED THEN
                UPDATE SET
                    [HESAP_TIPI] = @accountType,
                    [PARTNER_ID] = @partnerId,
                    [CIHAZ_ETIKETI] = @deviceLabel,
                    [BENI_HATIRLA_TERCIHI] = @rememberMe,
                    [TOPLAM_ZIYARET_SAYISI] = COALESCE(target.[TOPLAM_ZIYARET_SAYISI], 0) + @visitIncrement,
                    [TOPLAM_OTURUM_SURESI_SANIYE] = COALESCE(target.[TOPLAM_OTURUM_SURESI_SANIYE], 0) + @durationIncrement,
                    [SON_OTURUM_BASLANGICI] = CASE WHEN @visitIncrement = 1 THEN @sessionStartedAt ELSE target.[SON_OTURUM_BASLANGICI] END,
                    [SON_OTURUM_BITISI] = @sessionEndedAt,
                    [SON_AKTIVITE_TARIHI] = @lastActivityAt,
                    [SON_IP_HASH] = @ipHash,
                    son_user_agent_hash = @userAgentHash,
                    [GUNCELLENME_TARIHI] = CURRENT_TIMESTAMP
            WHEN NOT MATCHED THEN
                INSERT
                ([KULLANICI_ID], [HESAP_TIPI], [PARTNER_ID], [CIHAZ_ANAHTARI], [CIHAZ_ETIKETI], [BENI_HATIRLA_TERCIHI], [TOPLAM_ZIYARET_SAYISI], [TOPLAM_OTURUM_SURESI_SANIYE], [SON_OTURUM_BASLANGICI], [SON_OTURUM_BITISI], [SON_AKTIVITE_TARIHI], [SON_IP_HASH], son_user_agent_hash)
                VALUES
                (@userId, @accountType, @partnerId, @deviceKey, @deviceLabel, @rememberMe, @visitIncrement, @durationIncrement, @sessionStartedAt, @sessionEndedAt, @lastActivityAt, @ipHash, @userAgentHash);";

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@userId", userId);
        command.Parameters.AddWithValue("@accountType", accountType);
        command.Parameters.AddWithValue("@partnerId", partnerId.HasValue ? partnerId.Value : DBNull.Value);
        command.Parameters.AddWithValue("@deviceKey", deviceKey);
        command.Parameters.AddWithValue("@deviceLabel", BuildDeviceLabel(httpContext.Request.Headers.UserAgent.ToString()));
        command.Parameters.AddWithValue("@rememberMe", rememberMe ? 1 : 0);
        command.Parameters.AddWithValue("@visitIncrement", isNewVisit ? 1 : 0);
        command.Parameters.AddWithValue("@durationIncrement", durationSeconds);
        command.Parameters.AddWithValue("@sessionStartedAt", isNewVisit ? now : DBNull.Value);
        command.Parameters.AddWithValue("@sessionEndedAt", now);
        command.Parameters.AddWithValue("@lastActivityAt", now);
        command.Parameters.AddWithValue("@ipHash", ComputeSha256(httpContext.Connection.RemoteIpAddress?.ToString()));
        command.Parameters.AddWithValue("@userAgentHash", ComputeSha256(httpContext.Request.Headers.UserAgent.ToString()));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static string EnsurePersistentCookie(HttpContext context, string cookieName, string fallbackValue, DateTimeOffset expiresAt)
    {
        if (context.Request.Cookies.TryGetValue(cookieName, out var existing) && !string.IsNullOrWhiteSpace(existing))
        {
            return existing;
        }

        context.Response.Cookies.Append(cookieName, fallbackValue, BuildCookieOptions(expiresAt, true));
        return fallbackValue;
    }

    private static string EnsureSessionCookie(HttpContext context, string cookieName, string fallbackValue)
    {
        if (context.Request.Cookies.TryGetValue(cookieName, out var existing) && !string.IsNullOrWhiteSpace(existing))
        {
            return existing;
        }

        context.Response.Cookies.Append(cookieName, fallbackValue, BuildCookieOptions(null, true));
        return fallbackValue;
    }

    private static CookieOptions BuildCookieOptions(DateTimeOffset? expiresAt, bool httpOnly)
        => new()
        {
            HttpOnly = httpOnly,
            IsEssential = true,
            SameSite = SameSiteMode.Lax,
            Secure = true,
            Expires = expiresAt
        };

    private static long? ParseNullableLong(string? value)
        => long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : null;

    private static string NormalizeAccountType(string? accountType)
        => accountType?.Trim().ToLowerInvariant() switch
        {
            "admin" => "admin",
            "partner" => "partner",
            "firma" => "firma",
            "sales" => "sales",
            _ => "user"
        };

    private static string BuildDeviceLabel(string userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
        {
            return "Bilinmeyen cihaz";
        }

        var source = userAgent.ToLowerInvariant();
        var platform = source.Contains("android", StringComparison.Ordinal) ? "Android"
            : source.Contains("iphone", StringComparison.Ordinal) || source.Contains("ipad", StringComparison.Ordinal) ? "iOS"
            : source.Contains("windows", StringComparison.Ordinal) ? "Windows"
            : source.Contains("mac os", StringComparison.Ordinal) || source.Contains("macintosh", StringComparison.Ordinal) ? "macOS"
            : source.Contains("linux", StringComparison.Ordinal) ? "Linux"
            : "Bilinmeyen OS";

        var browser = source.Contains("edg/", StringComparison.Ordinal) ? "Edge"
            : source.Contains("chrome/", StringComparison.Ordinal) ? "Chrome"
            : source.Contains("firefox/", StringComparison.Ordinal) ? "Firefox"
            : source.Contains("safari/", StringComparison.Ordinal) ? "Safari"
            : "Tarayıcı";

        return $"{platform} / {browser}";
    }

    private static string? ComputeSha256(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return null;
        }

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
