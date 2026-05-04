using System.Globalization;
using Microsoft.Data.SqlClient;
using otelturizmnew.Models.Paneller.Partner;
using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Services;

public sealed class PanelThemeService : IPanelThemeService
{
    private readonly string _connectionString;
    private readonly ILogger<PanelThemeService> _logger;

    public PanelThemeService(IConfiguration configuration, ILogger<PanelThemeService> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection tanimli degil.");
        _logger = logger;
    }

    public async Task<PanelThemeViewModel> LoadAsync(string targetType, long targetId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(targetType) || targetId <= 0)
        {
            return new PanelThemeViewModel { BsTheme = "light" };
        }

        const string sql = @"
            SELECT TOP (1)
                bs_theme,
                primary_hex,
                accent_hex,
                sidebar_bg_hex,
                radius_scale,
                density,
                font_family,
                layout_mode,
                rtl
            FROM dbo.tema_panel
            WHERE aktif_mi = 1
              AND hedef_tur = @targetType
              AND hedef_id = @targetId
            ORDER BY guncellenme_tarihi DESC, id DESC;";

        try
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken);
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@targetType", targetType.Trim().ToLowerInvariant());
            cmd.Parameters.AddWithValue("@targetId", targetId);
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                return new PanelThemeViewModel
                {
                    BsTheme = reader.IsDBNull(0) ? "light" : reader.GetString(0),
                    PrimaryHex = reader.IsDBNull(1) ? null : reader.GetString(1),
                    AccentHex = reader.IsDBNull(2) ? null : reader.GetString(2),
                    SidebarBgHex = reader.IsDBNull(3) ? null : reader.GetString(3),
                    RadiusScale = reader.IsDBNull(4) ? null : reader.GetDecimal(4),
                    Density = reader.IsDBNull(5) ? null : reader.GetString(5),
                    FontFamily = reader.IsDBNull(6) ? null : reader.GetString(6),
                    LayoutMode = reader.IsDBNull(7) ? null : reader.GetString(7),
                    Rtl = !reader.IsDBNull(8) && reader.GetBoolean(8)
                };
            }
        }
        catch (SqlException ex) when (ex.Number == 208)
        {
            // tema_panel migration henüz uygulanmadıysa sessiz fallback
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Tema yuklenemedi: {TargetType}:{TargetId}", targetType, targetId);
        }

        return new PanelThemeViewModel { BsTheme = "light" };
    }

    public async Task<(bool Success, string Message)> SaveAsync(string targetType, long targetId, PanelThemeViewModel theme, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(targetType) || targetId <= 0)
        {
            return (false, "Tema hedefi geçersiz.");
        }

        var normalizedTheme = new PanelThemeViewModel
        {
            BsTheme = string.IsNullOrWhiteSpace(theme.BsTheme) ? "light" : theme.BsTheme.Trim().ToLowerInvariant(),
            PrimaryHex = string.IsNullOrWhiteSpace(theme.PrimaryHex) ? null : theme.PrimaryHex.Trim(),
            AccentHex = string.IsNullOrWhiteSpace(theme.AccentHex) ? null : theme.AccentHex.Trim(),
            SidebarBgHex = string.IsNullOrWhiteSpace(theme.SidebarBgHex) ? null : theme.SidebarBgHex.Trim(),
            RadiusScale = theme.RadiusScale,
            Density = string.IsNullOrWhiteSpace(theme.Density) ? null : theme.Density.Trim(),
            FontFamily = string.IsNullOrWhiteSpace(theme.FontFamily) ? null : theme.FontFamily.Trim(),
            LayoutMode = string.IsNullOrWhiteSpace(theme.LayoutMode) ? null : theme.LayoutMode.Trim(),
            Rtl = theme.Rtl
        };
        if (normalizedTheme.BsTheme is not ("light" or "dark" or "auto")) normalizedTheme.BsTheme = "light";

        const string sql = @"
            IF EXISTS (SELECT 1 FROM dbo.tema_panel WHERE hedef_tur = @targetType AND hedef_id = @targetId)
            BEGIN
                UPDATE dbo.tema_panel
                SET bs_theme = @bsTheme,
                    primary_hex = NULLIF(@primaryHex, ''),
                    accent_hex = NULLIF(@accentHex, ''),
                    sidebar_bg_hex = NULLIF(@sidebarBgHex, ''),
                    radius_scale = @radiusScale,
                    density = NULLIF(@density, ''),
                    font_family = NULLIF(@fontFamily, ''),
                    layout_mode = NULLIF(@layoutMode, ''),
                    rtl = @rtl,
                    aktif_mi = 1,
                    guncellenme_tarihi = SYSUTCDATETIME()
                WHERE hedef_tur = @targetType AND hedef_id = @targetId;
            END
            ELSE
            BEGIN
                INSERT INTO dbo.tema_panel
                (hedef_tur, hedef_id, bs_theme, primary_hex, accent_hex, sidebar_bg_hex, radius_scale, density, font_family, layout_mode, rtl, aktif_mi, olusturulma_tarihi, guncellenme_tarihi)
                VALUES
                (@targetType, @targetId, @bsTheme, NULLIF(@primaryHex, ''), NULLIF(@accentHex, ''), NULLIF(@sidebarBgHex, ''), @radiusScale, NULLIF(@density, ''), NULLIF(@fontFamily, ''), NULLIF(@layoutMode, ''), @rtl, 1, SYSUTCDATETIME(), SYSUTCDATETIME());
            END;";

        try
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken);
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@targetType", targetType.Trim().ToLowerInvariant());
            cmd.Parameters.AddWithValue("@targetId", targetId);
            cmd.Parameters.AddWithValue("@bsTheme", normalizedTheme.BsTheme);
            cmd.Parameters.AddWithValue("@primaryHex", (object?)normalizedTheme.PrimaryHex ?? string.Empty);
            cmd.Parameters.AddWithValue("@accentHex", (object?)normalizedTheme.AccentHex ?? string.Empty);
            cmd.Parameters.AddWithValue("@sidebarBgHex", (object?)normalizedTheme.SidebarBgHex ?? string.Empty);
            cmd.Parameters.AddWithValue("@radiusScale", normalizedTheme.RadiusScale.HasValue ? normalizedTheme.RadiusScale.Value : (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@density", (object?)normalizedTheme.Density ?? string.Empty);
            cmd.Parameters.AddWithValue("@fontFamily", (object?)normalizedTheme.FontFamily ?? string.Empty);
            cmd.Parameters.AddWithValue("@layoutMode", (object?)normalizedTheme.LayoutMode ?? string.Empty);
            cmd.Parameters.AddWithValue("@rtl", normalizedTheme.Rtl ? 1 : 0);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
            return (true, "Tema ayarları kaydedildi.");
        }
        catch (SqlException ex) when (ex.Number == 208)
        {
            return (false, "Tema tablosu bulunamadı. Önce migration’ları çalıştırın.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Tema kaydedilemedi: {TargetType}:{TargetId}", targetType, targetId);
            return (false, "Tema kaydedilemedi.");
        }
    }
}

