using System.Globalization;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using otelturizmnew.Models.Paneller.Admin;
using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Services;

public class AdminSupportArticleService : IAdminSupportArticleService
{
    private readonly string _connectionString;

    public AdminSupportArticleService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection tanimli degil.");
    }

    public async Task<AdminSupportArticlePageViewModel> GetPageAsync(
        AdminShellViewModel shell,
        string? searchText,
        long? categoryIdFilter,
        string? statusFilter,
        long? editArticleId,
        CancellationToken cancellationToken = default)
    {
        var normalizedSearch = (searchText ?? string.Empty).Trim();
        var normalizedStatus = NormalizeStatusFilter(statusFilter);
        var model = new AdminSupportArticlePageViewModel
        {
            Shell = shell,
            SearchText = normalizedSearch,
            CategoryIdFilter = categoryIdFilter,
            StatusFilter = normalizedStatus,
            EditArticleId = editArticleId
        };

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string categorySql = @"
            SELECT [ID], [KATEGORI_ADI], [SEO_SLUG]
            FROM [dbo].[DESTEK_KATEGORILERI]
            ORDER BY [SIRALAMA], [KATEGORI_ADI], [ID];";

        await using (var command = new SqlCommand(categorySql, connection))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                var categoryId = reader.GetInt64(0);
                model.Categories.Add(new AdminSupportArticleCategoryOptionViewModel
                {
                    CategoryId = categoryId,
                    CategoryName = reader.GetString(1),
                    CategorySlug = reader.GetString(2),
                    IsSelected = categoryIdFilter.HasValue && categoryIdFilter.Value == categoryId
                });
            }
        }

        const string summarySql = @"
            SELECT
                COUNT(*) AS toplam,
                SUM(CASE WHEN [DURUM] = 1 THEN 1 ELSE 0 END) AS aktif,
                SUM(CASE WHEN [YARDIM_MERKEZINDE_GOSTER] = 1 THEN 1 ELSE 0 END) AS yardim_merkezi,
                SUM(CASE WHEN [ONE_CIKAN_MI] = 1 THEN 1 ELSE 0 END) AS [ONE_CIKAN]
            FROM [dbo].[DESTEK_MAKALELERI];";

        await using (var command = new SqlCommand(summarySql, connection))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            if (await reader.ReadAsync(cancellationToken))
            {
                model.SummaryCards.Add(new AdminSummaryCardViewModel
                {
                    Label = "Toplam Makale",
                    Value = SafeInt(reader, 0).ToString(CultureInfo.InvariantCulture),
                    Description = "Destek içerik havuzu",
                    ToneClass = "info",
                    IconClass = "fa-book-open"
                });
                model.SummaryCards.Add(new AdminSummaryCardViewModel
                {
                    Label = "Aktif Makale",
                    Value = SafeInt(reader, 1).ToString(CultureInfo.InvariantCulture),
                    Description = "Yayında olan içerikler",
                    ToneClass = "success",
                    IconClass = "fa-circle-check"
                });
                model.SummaryCards.Add(new AdminSummaryCardViewModel
                {
                    Label = "Yardım Merkezinde",
                    Value = SafeInt(reader, 2).ToString(CultureInfo.InvariantCulture),
                    Description = "Yardım merkezinde listelenen",
                    ToneClass = "warning",
                    IconClass = "fa-headset"
                });
                model.SummaryCards.Add(new AdminSummaryCardViewModel
                {
                    Label = "Öne Çıkan",
                    Value = SafeInt(reader, 3).ToString(CultureInfo.InvariantCulture),
                    Description = "Vitrinde gösterilen",
                    ToneClass = "primary",
                    IconClass = "fa-star"
                });
            }
        }

        var listSql = @"
            SELECT TOP (500)
                dm.[ID],
                dm.[DESTEK_KATEGORI_ID],
                dk.[KATEGORI_ADI],
                dm.[BASLIK],
                dm.[SEO_SLUG],
                COALESCE(dm.[OZET], '') AS ozet,
                COALESCE(dm.[IKON], 'fa-circle-question') AS ikon,
                dm.[ONE_CIKAN_MI],
                dm.[YARDIM_MERKEZINDE_GOSTER],
                dm.[SIRALAMA],
                dm.[DURUM],
                COALESCE(dm.[GUNCELLENME_TARIHI], dm.[OLUSTURULMA_TARIHI]) AS guncel_tarih
            FROM [dbo].[DESTEK_MAKALELERI] dm
            INNER JOIN [dbo].[DESTEK_KATEGORILERI] dk ON dk.[ID] = dm.[DESTEK_KATEGORI_ID]
            WHERE 1 = 1";

        if (!string.IsNullOrWhiteSpace(normalizedSearch))
        {
            listSql += " AND (dm.[BASLIK] LIKE '%' + @search + '%' OR dm.[SEO_SLUG] LIKE '%' + @search + '%' OR dm.[OZET] LIKE '%' + @search + '%' OR dm.[ICERIK] LIKE '%' + @search + '%')";
        }

        if (categoryIdFilter.HasValue && categoryIdFilter.Value > 0)
        {
            listSql += " AND dm.[DESTEK_KATEGORI_ID] = @categoryId";
        }

        if (normalizedStatus == "active")
        {
            listSql += " AND dm.[DURUM] = 1";
        }
        else if (normalizedStatus == "passive")
        {
            listSql += " AND dm.[DURUM] = 0";
        }

        listSql += " ORDER BY dm.[ONE_CIKAN_MI] DESC, dm.[SIRALAMA], dm.[ID] DESC;";

        await using (var command = new SqlCommand(listSql, connection))
        {
            if (!string.IsNullOrWhiteSpace(normalizedSearch))
            {
                command.Parameters.AddWithValue("@search", normalizedSearch);
            }

            if (categoryIdFilter.HasValue && categoryIdFilter.Value > 0)
            {
                command.Parameters.AddWithValue("@categoryId", categoryIdFilter.Value);
            }

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                model.Articles.Add(new AdminSupportArticleRowViewModel
                {
                    ArticleId = reader.GetInt64(0),
                    CategoryId = reader.GetInt64(1),
                    CategoryName = reader.GetString(2),
                    Title = reader.GetString(3),
                    Slug = reader.GetString(4),
                    Summary = reader.GetString(5),
                    IconClass = reader.GetString(6),
                    IsFeatured = reader.GetBoolean(7),
                    ShowInHelpCenter = reader.GetBoolean(8),
                    SortOrder = reader.GetInt32(9),
                    IsActive = reader.GetBoolean(10),
                    UpdatedAtText = reader.IsDBNull(11) ? "-" : reader.GetDateTime(11).ToString("dd.MM.yyyy HH:mm", CultureInfo.GetCultureInfo("tr-TR"))
                });
            }
        }

        model.Form = new AdminSupportArticleForm();
        if (model.Categories.Count > 0)
        {
            model.Form.CategoryId = categoryIdFilter.HasValue && categoryIdFilter.Value > 0
                ? categoryIdFilter.Value
                : model.Categories[0].CategoryId;
        }

        if (editArticleId.HasValue && editArticleId.Value > 0)
        {
            const string editSql = @"
                SELECT TOP (1)
                    id,
                    [DESTEK_KATEGORI_ID],
                    [BASLIK],
                    [SEO_SLUG],
                    COALESCE(ozet, '') AS ozet,
                    [ICERIK],
                    COALESCE(ikon, 'fa-circle-question') AS ikon,
                    [ONE_CIKAN_MI],
                    [YARDIM_MERKEZINDE_GOSTER],
                    [SIRALAMA],
                    [DURUM]
                FROM [dbo].[DESTEK_MAKALELERI]
                WHERE id = @id;";

            await using var editCommand = new SqlCommand(editSql, connection);
            editCommand.Parameters.AddWithValue("@id", editArticleId.Value);
            await using var editReader = await editCommand.ExecuteReaderAsync(cancellationToken);
            if (await editReader.ReadAsync(cancellationToken))
            {
                model.Form = new AdminSupportArticleForm
                {
                    ArticleId = editReader.GetInt64(0),
                    CategoryId = editReader.GetInt64(1),
                    Title = editReader.GetString(2),
                    SeoSlug = editReader.GetString(3),
                    Summary = editReader.GetString(4),
                    Content = editReader.GetString(5),
                    IconClass = editReader.GetString(6),
                    IsFeatured = editReader.GetBoolean(7),
                    ShowInHelpCenter = editReader.GetBoolean(8),
                    SortOrder = editReader.GetInt32(9),
                    IsActive = editReader.GetBoolean(10)
                };
            }
        }

        return model;
    }

    public async Task<AdminSupportArticleActionResult> SaveAsync(long adminUserId, AdminSupportArticleForm form, CancellationToken cancellationToken = default)
    {
        _ = adminUserId;
        if (form.CategoryId <= 0)
        {
            return new AdminSupportArticleActionResult { Success = false, Message = "Makale kategorisi secmelisiniz." };
        }

        var title = (form.Title ?? string.Empty).Trim();
        var content = (form.Content ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(title))
        {
            return new AdminSupportArticleActionResult { Success = false, Message = "Makale basligi zorunludur." };
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            return new AdminSupportArticleActionResult { Success = false, Message = "Makale icerigi zorunludur." };
        }

        var slug = string.IsNullOrWhiteSpace(form.SeoSlug) ? BuildSlug(title) : BuildSlug(form.SeoSlug);
        if (string.IsNullOrWhiteSpace(slug))
        {
            slug = $"makale-{DateTime.UtcNow:yyyyMMddHHmmss}";
        }

        var summary = (form.Summary ?? string.Empty).Trim();
        var iconClass = string.IsNullOrWhiteSpace(form.IconClass) ? "fa-circle-question" : form.IconClass.Trim();

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var ensureSlugCommand = new SqlCommand(@"
            SELECT COUNT(*)
            FROM [dbo].[DESTEK_MAKALELERI]
            WHERE [SEO_SLUG] = @slug
              AND (@articleId IS NULL OR id <> @articleId);", connection);
        ensureSlugCommand.Parameters.AddWithValue("@slug", slug);
        ensureSlugCommand.Parameters.AddWithValue("@articleId", form.ArticleId.HasValue ? form.ArticleId.Value : DBNull.Value);
        var slugCount = Convert.ToInt32(await ensureSlugCommand.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture);
        if (slugCount > 0)
        {
            slug = $"{slug}-{DateTime.UtcNow:HHmmss}";
        }

        if (form.ArticleId.HasValue && form.ArticleId.Value > 0)
        {
            await using var updateCommand = new SqlCommand(@"
                UPDATE [dbo].[DESTEK_MAKALELERI]
                SET
                    [DESTEK_KATEGORI_ID] = @categoryId,
                    [BASLIK] = @title,
                    [SEO_SLUG] = @slug,
                    ozet = @summary,
                    [ICERIK] = @content,
                    ikon = @icon,
                    [ONE_CIKAN_MI] = @featured,
                    [YARDIM_MERKEZINDE_GOSTER] = @showInHelpCenter,
                    [SIRALAMA] = @sortOrder,
                    [DURUM] = @status,
                    [GUNCELLENME_TARIHI] = SYSUTCDATETIME()
                WHERE id = @articleId;", connection);

            BindSaveParameters(updateCommand, form.ArticleId.Value, form.CategoryId, title, slug, summary, content, iconClass, form.IsFeatured, form.ShowInHelpCenter, form.SortOrder, form.IsActive);
            var affected = await updateCommand.ExecuteNonQueryAsync(cancellationToken);
            if (affected <= 0)
            {
                return new AdminSupportArticleActionResult { Success = false, Message = "Makale guncellenemedi.", ArticleId = form.ArticleId };
            }

            return new AdminSupportArticleActionResult { Success = true, Message = "Makale guncellendi.", ArticleId = form.ArticleId };
        }
        else
        {
            await using var insertCommand = new SqlCommand(@"
                INSERT INTO [dbo].[DESTEK_MAKALELERI]
                (
                    [DESTEK_KATEGORI_ID],
                    [BASLIK],
                    [SEO_SLUG],
                    ozet,
                    [ICERIK],
                    ikon,
                    [ONE_CIKAN_MI],
                    [YARDIM_MERKEZINDE_GOSTER],
                    [SIRALAMA],
                    [DURUM],
                    [OLUSTURULMA_TARIHI],
                    [GUNCELLENME_TARIHI]
                )
                OUTPUT INSERTED.id
                VALUES
                (
                    @categoryId,
                    @title,
                    @slug,
                    @summary,
                    @content,
                    @icon,
                    @featured,
                    @showInHelpCenter,
                    @sortOrder,
                    @status,
                    SYSUTCDATETIME(),
                    SYSUTCDATETIME()
                );", connection);

            BindSaveParameters(insertCommand, DBNull.Value, form.CategoryId, title, slug, summary, content, iconClass, form.IsFeatured, form.ShowInHelpCenter, form.SortOrder, form.IsActive);
            var insertedIdObject = await insertCommand.ExecuteScalarAsync(cancellationToken);
            var insertedId = insertedIdObject is long longId ? longId : Convert.ToInt64(insertedIdObject, CultureInfo.InvariantCulture);
            return new AdminSupportArticleActionResult { Success = true, Message = "Makale eklendi.", ArticleId = insertedId };
        }
    }

    public async Task<AdminSupportArticleActionResult> DeleteAsync(long adminUserId, long articleId, CancellationToken cancellationToken = default)
    {
        _ = adminUserId;
        if (articleId <= 0)
        {
            return new AdminSupportArticleActionResult { Success = false, Message = "Silinecek makale secilmedi." };
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("DELETE FROM [dbo].[DESTEK_MAKALELERI] WHERE id = @id;", connection);
        command.Parameters.AddWithValue("@id", articleId);
        var affected = await command.ExecuteNonQueryAsync(cancellationToken);
        if (affected <= 0)
        {
            return new AdminSupportArticleActionResult { Success = false, Message = "Makale bulunamadi veya silinemedi." };
        }

        return new AdminSupportArticleActionResult { Success = true, Message = "Makale silindi.", ArticleId = articleId };
    }

    private static void BindSaveParameters(
        SqlCommand command,
        object articleId,
        long categoryId,
        string title,
        string slug,
        string summary,
        string content,
        string iconClass,
        bool isFeatured,
        bool showInHelpCenter,
        int sortOrder,
        bool isActive)
    {
        command.Parameters.AddWithValue("@articleId", articleId);
        command.Parameters.AddWithValue("@categoryId", categoryId);
        command.Parameters.AddWithValue("@title", title);
        command.Parameters.AddWithValue("@slug", slug);
        command.Parameters.AddWithValue("@summary", string.IsNullOrWhiteSpace(summary) ? DBNull.Value : summary);
        command.Parameters.AddWithValue("@content", content);
        command.Parameters.AddWithValue("@icon", iconClass);
        command.Parameters.AddWithValue("@featured", isFeatured);
        command.Parameters.AddWithValue("@showInHelpCenter", showInHelpCenter);
        command.Parameters.AddWithValue("@sortOrder", sortOrder);
        command.Parameters.AddWithValue("@status", isActive);
    }

    private static int SafeInt(SqlDataReader reader, int index)
    {
        if (reader.IsDBNull(index))
        {
            return 0;
        }

        return reader.GetFieldType(index) == typeof(long) ? Convert.ToInt32(reader.GetInt64(index), CultureInfo.InvariantCulture) : reader.GetInt32(index);
    }

    private static string NormalizeStatusFilter(string? value)
    {
        if (string.Equals(value, "active", StringComparison.OrdinalIgnoreCase))
        {
            return "active";
        }

        if (string.Equals(value, "passive", StringComparison.OrdinalIgnoreCase))
        {
            return "passive";
        }

        return "all";
    }

    private static string BuildSlug(string value)
    {
        var raw = (value ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(raw))
        {
            return string.Empty;
        }

        raw = raw
            .Replace("ı", "i", StringComparison.Ordinal)
            .Replace("ğ", "g", StringComparison.Ordinal)
            .Replace("ü", "u", StringComparison.Ordinal)
            .Replace("ş", "s", StringComparison.Ordinal)
            .Replace("ö", "o", StringComparison.Ordinal)
            .Replace("ç", "c", StringComparison.Ordinal)
            .Replace("â", "a", StringComparison.Ordinal)
            .Replace("î", "i", StringComparison.Ordinal)
            .Replace("û", "u", StringComparison.Ordinal);

        Span<char> buffer = stackalloc char[raw.Length];
        var length = 0;
        var previousDash = false;
        foreach (var ch in raw)
        {
            if ((ch >= 'a' && ch <= 'z') || (ch >= '0' && ch <= '9'))
            {
                buffer[length++] = ch;
                previousDash = false;
                continue;
            }

            if (previousDash)
            {
                continue;
            }

            buffer[length++] = '-';
            previousDash = true;
        }

        var slug = new string(buffer[..length]).Trim('-');
        if (slug.Length > 170)
        {
            slug = slug[..170].Trim('-');
        }

        return slug;
    }
}
