using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using otelturizmnew.Models.Destek;
using otelturizmnew.Models.Kurumsal;
using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Services;

public class SupportService : ISupportService
{
    private readonly string _connectionString;

    public SupportService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection tanimli degil.");
    }

    public async Task<YardimMerkeziViewModel> GetHelpCenterAsync(string? searchTerm, CancellationToken cancellationToken = default)
    {
        var model = new YardimMerkeziViewModel
        {
            SearchTerm = searchTerm?.Trim() ?? string.Empty
        };

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string categoriesSql = @"
            SELECT [KATEGORI_ADI], [SEO_SLUG], [KISA_ACIKLAMA], [KATEGORI_IKON], [RENK_KODU]
            FROM [dbo].[DESTEK_KATEGORILERI]
            WHERE [DURUM] = 1
            ORDER BY [SIRALAMA], [ID];";

        await using (var command = new SqlCommand(categoriesSql, connection))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                var slug = reader.GetString(1);
                model.Categories.Add(new DestekKategoriViewModel
                {
                    Name = FixMojibake(reader.GetString(0)),
                    Slug = slug,
                    Description = reader.IsDBNull(2) ? string.Empty : FixMojibake(reader.GetString(2)),
                    IconClass = reader.IsDBNull(3) ? "fa-circle-info" : FixMojibake(reader.GetString(3)),
                    ColorHex = reader.IsDBNull(4) ? "#003B95" : reader.GetString(4),
                    LinkUrl = $"/yardim-merkezi/kategori/{Uri.EscapeDataString(slug)}"
                });
            }
        }

        var topicsSql = @"
            SELECT [ID], [BASLIK], [OZET], COALESCE([IKON], 'fa-circle-question') AS ikon
            FROM [dbo].[DESTEK_MAKALELERI]
            WHERE [DURUM] = 1
              AND [YARDIM_MERKEZINDE_GOSTER] = 1";

        if (!string.IsNullOrWhiteSpace(model.SearchTerm))
        {
            topicsSql += " AND ([BASLIK] LIKE '%' + @search + '%' OR [OZET] LIKE '%' + @search + '%' OR [ICERIK] LIKE '%' + @search + '%')";
        }

        topicsSql += " ORDER BY [ONE_CIKAN_MI] DESC, [SIRALAMA], [ID] OFFSET 0 ROWS FETCH NEXT 6 ROWS ONLY;";

        await using (var command = new SqlCommand(topicsSql, connection))
        {
            if (!string.IsNullOrWhiteSpace(model.SearchTerm))
            {
                command.Parameters.AddWithValue("@search", model.SearchTerm);
            }

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var title = FixMojibake(reader.GetString(1));
                model.PopularTopics.Add(new DestekMakaleViewModel
                {
                    Id = reader.GetInt64(0),
                    Title = title,
                    Summary = reader.IsDBNull(2) ? string.Empty : FixMojibake(reader.GetString(2)),
                    IconClass = FixMojibake(reader.GetString(3)),
                    LinkUrl = $"/sss?ara={Uri.EscapeDataString(title)}"
                });
            }
        }

        const string channelsSql = @"
            SELECT [KANAL_ADI], [ACIKLAMA], [IKON], [BUTON_METIN], [BAGLANTI_URL], [EK_BILGI], [RENK_TONU], [KANAL_TURU]
            FROM [dbo].[DESTEK_KANALLARI]
            WHERE [AKTIF_MI] = 1
            ORDER BY [SIRALAMA], [ID];";

        await using (var command = new SqlCommand(channelsSql, connection))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                var channel = new DestekKanalViewModel
                {
                    Name = FixMojibake(reader.GetString(0)),
                    Description = reader.IsDBNull(1) ? string.Empty : FixMojibake(reader.GetString(1)),
                    IconClass = reader.IsDBNull(2) ? "fa-headset" : FixMojibake(reader.GetString(2)),
                    ButtonText = reader.IsDBNull(3) ? "Detay" : FixMojibake(reader.GetString(3)),
                    Url = reader.IsDBNull(4) ? "#" : reader.GetString(4),
                    Note = reader.IsDBNull(5) ? string.Empty : FixMojibake(reader.GetString(5)),
                    Tone = reader.IsDBNull(6) ? "primary" : reader.GetString(6)
                };

                model.SupportChannels.Add(channel);
                if (string.Equals(reader.IsDBNull(7) ? string.Empty : reader.GetString(7), "canli_destek", StringComparison.OrdinalIgnoreCase))
                {
                    model.LiveChatChannel = channel;
                }
            }
        }

        model.PlatformTeam = await LoadPlatformTeamAsync(connection, cancellationToken);
        return model;
    }

    public async Task<YardimMerkeziKategoriDetaySayfaViewModel?> GetHelpCategoryAsync(string slug, string? searchTerm, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return null;
        }

        var normalizedSlug = slug.Trim();
        var model = new YardimMerkeziKategoriDetaySayfaViewModel
        {
            SearchTerm = searchTerm?.Trim() ?? string.Empty
        };

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string categorySql = @"
            SELECT TOP (1)
                k.[ID],
                k.[KATEGORI_ADI],
                k.[SEO_SLUG],
                COALESCE(k.[KISA_ACIKLAMA], N'') AS [KISA_ACIKLAMA],
                COALESCE(k.[KATEGORI_IKON], N'fa-circle-info') AS [KATEGORI_IKON],
                COALESCE(k.[RENK_KODU], N'#003B95') AS [RENK_KODU],
                COALESCE(d.[HERO_BASLIK], N'') AS hero_baslik,
                COALESCE(d.[HERO_ALT_BASLIK], N'') AS hero_alt_baslik,
                COALESCE(d.[HERO_GORSEL_URL], N'') AS hero_gorsel_url,
                COALESCE(d.[TAM_ACIKLAMA], N'') AS tam_aciklama
            FROM [dbo].[DESTEK_KATEGORILERI] k
            LEFT JOIN [dbo].[YARDIM_MERKEZI_KATEGORI_DETAYLARI] d ON d.[DESTEK_KATEGORI_ID] = k.[ID] AND d.[AKTIF_MI] = 1
            WHERE k.[DURUM] = 1
              AND k.[SEO_SLUG] = @slug;";

        long categoryId;
        await using (var cmd = new SqlCommand(categorySql, connection))
        {
            cmd.Parameters.AddWithValue("@slug", normalizedSlug);
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                return null;
            }

            categoryId = reader.GetInt64(0);
            model.Category = new DestekKategoriViewModel
            {
                Name = FixMojibake(reader.GetString(1)),
                Slug = reader.GetString(2),
                Description = FixMojibake(reader.GetString(3)),
                IconClass = FixMojibake(reader.GetString(4)),
                ColorHex = reader.GetString(5),
                LinkUrl = $"/yardim-merkezi/kategori/{Uri.EscapeDataString(reader.GetString(2))}",
                HeroTitle = reader.IsDBNull(6) ? null : FixMojibake(reader.GetString(6)),
                HeroSubtitle = reader.IsDBNull(7) ? null : FixMojibake(reader.GetString(7)),
                HeroImageUrl = reader.IsDBNull(8) ? null : reader.GetString(8),
                FullContentHtml = reader.IsDBNull(9) ? null : FixMojibake(reader.GetString(9))
            };
        }

        try
        {
            const string faqSql = @"
                SELECT [SORU], [CEVAP]
                FROM [dbo].[YARDIM_MERKEZI_KATEGORI_SSS]
                WHERE [AKTIF_MI] = 1 AND [DESTEK_KATEGORI_ID] = @categoryId
                ORDER BY [SIRALAMA] ASC, [ID] ASC;";
            await using var faqCmd = new SqlCommand(faqSql, connection);
            faqCmd.Parameters.AddWithValue("@categoryId", categoryId);
            await using var faqReader = await faqCmd.ExecuteReaderAsync(cancellationToken);
            while (await faqReader.ReadAsync(cancellationToken))
            {
                model.Category.FaqItems.Add(new YardimMerkeziFaqItemViewModel
                {
                    Question = FixMojibake(faqReader.GetString(0)),
                    AnswerHtml = FixMojibake(faqReader.GetString(1))
                });
            }
        }
        catch (SqlException ex) when (IsMissingTableOrColumn(ex))
        {
            // opsiyonel
        }

        const string relatedSql = @"
            SELECT TOP (12) [ID], [BASLIK], [OZET], COALESCE([IKON], 'fa-circle-question') AS ikon
            FROM [dbo].[DESTEK_MAKALELERI]
            WHERE [DURUM] = 1
              AND [DESTEK_KATEGORI_ID] = @categoryId
              AND [YARDIM_MERKEZINDE_GOSTER] = 1
              AND (@q IS NULL OR ([BASLIK] LIKE '%' + @q + '%' OR [OZET] LIKE '%' + @q + '%' OR [ICERIK] LIKE '%' + @q + '%'))
            ORDER BY [ONE_CIKAN_MI] DESC, [SIRALAMA], [ID] DESC;";
        await using (var cmd = new SqlCommand(relatedSql, connection))
        {
            cmd.Parameters.AddWithValue("@categoryId", categoryId);
            cmd.Parameters.AddWithValue("@q", string.IsNullOrWhiteSpace(model.SearchTerm) ? (object)DBNull.Value : model.SearchTerm);
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var title = FixMojibake(reader.GetString(1));
                model.RelatedArticles.Add(new DestekMakaleViewModel
                {
                    Id = reader.GetInt64(0),
                    Title = title,
                    Summary = reader.IsDBNull(2) ? string.Empty : FixMojibake(reader.GetString(2)),
                    IconClass = FixMojibake(reader.GetString(3)),
                    LinkUrl = $"/sss?ara={Uri.EscapeDataString(title)}"
                });
            }
        }

        return model;
    }

    public async Task<YardimMerkeziIcerikSayfaViewModel?> GetHelpContentPageAsync(string type, string slug, CancellationToken cancellationToken = default)
    {
        var t = (type ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(t) || string.IsNullOrWhiteSpace(slug))
        {
            return null;
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        try
        {
            const string sql = @"
                SELECT TOP (1)
                    [ICERIK_TURU], [BASLIK], COALESCE([OZET], N''), COALESCE([HERO_BASLIK], N''), COALESCE([HERO_ALT_BASLIK], N''),
                    COALESCE([HERO_GORSEL_URL], N''), [ICERIK]
                FROM [dbo].[YARDIM_MERKEZI_ICERIKLER]
                WHERE [AKTIF_MI] = 1
                  AND [ICERIK_TURU] = @type
                  AND [SEO_SLUG] = @slug;";
            await using var cmd = new SqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@type", t);
            cmd.Parameters.AddWithValue("@slug", slug.Trim());
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                return null;
            }

            return new YardimMerkeziIcerikSayfaViewModel
            {
                ContentType = reader.GetString(0),
                Title = FixMojibake(reader.GetString(1)),
                Summary = FixMojibake(reader.GetString(2)),
                HeroTitle = reader.IsDBNull(3) ? null : FixMojibake(reader.GetString(3)),
                HeroSubtitle = reader.IsDBNull(4) ? null : FixMojibake(reader.GetString(4)),
                HeroImageUrl = reader.IsDBNull(5) ? null : reader.GetString(5),
                Html = FixMojibake(reader.GetString(6))
            };
        }
        catch (SqlException ex) when (IsMissingTableOrColumn(ex))
        {
            return null;
        }
    }

    private async Task<List<YardimMerkeziTeamMemberViewModel>> LoadPlatformTeamAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        try
        {
            const string sql = @"
                SELECT [AD_SOYAD], [UNVAN], [EPOSTA], COALESCE([AVATAR_URL], N'') AS [AVATAR_URL]
                FROM [dbo].[PLATFORM_EKIP_UYELERI]
                WHERE [AKTIF_MI] = 1
                ORDER BY [SIRALAMA] ASC, [ID] ASC;";
            await using var cmd = new SqlCommand(sql, connection);
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            var list = new List<YardimMerkeziTeamMemberViewModel>();
            while (await reader.ReadAsync(cancellationToken))
            {
                var name = FixMojibake(reader.GetString(0));
                var avatar = reader.IsDBNull(3) ? string.Empty : reader.GetString(3);
                if (string.IsNullOrWhiteSpace(avatar))
                {
                    var q = Uri.EscapeDataString(name);
                    avatar = $"https://ui-avatars.com/api/?name={q}&size=160&background=0b57d0&color=ffffff&bold=true&format=png";
                }

                list.Add(new YardimMerkeziTeamMemberViewModel
                {
                    Name = MaskSurname(name),
                    Title = FixMojibake(reader.GetString(1)),
                    Email = reader.GetString(2),
                    AvatarUrl = avatar
                });
            }

            return list.Count > 0 ? list : BuildDefaultPlatformTeam();
        }
        catch (SqlException ex) when (IsMissingTableOrColumn(ex))
        {
            return BuildDefaultPlatformTeam();
        }
    }

    private static List<YardimMerkeziTeamMemberViewModel> BuildDefaultPlatformTeam()
    {
        var raw = new (string Name, string Title, string Email)[]
        {
            ("Deniz Aksoy", "Platform yönetimi", "info+admin@otelturizm.com"),
            ("İrem Yalçın", "İnsan kaynakları", "info+ik.yonetici@otelturizm.com"),
            ("Hande Mert", "Hukuk & uyum", "info+hukuk.musaviri@otelturizm.com"),
            ("Volkan Yıldız", "Veri & altyapı", "info+dba@otelturizm.com"),
            ("Yiğit Uslu", "Uygulama geliştirme", "info+yazilim.uzmani@otelturizm.com"),
            ("Tolga Levent", "Mimari & kod kalitesi", "info+teknik.lider@otelturizm.com"),
            ("Pelin Yaman", "Büyüme & kampanya", "info+pazarlama.yonetici@otelturizm.com"),
            ("Derya Uçar", "Müşteri destek", "info+destek.uzmani@otelturizm.com"),
            ("Defne Yıldırım", "Destek operasyonları", "info+destek.yonetici@otelturizm.com"),
            ("Okan Yalın", "Operasyon & SLA", "info+operasyon.yonetici@otelturizm.com"),
            ("Merve Uzun", "Muhasebe işlemleri", "info+muhasebe.uzmani@otelturizm.com"),
            ("Faruk Yılmaz", "Finans & tahsilat", "info+finans.yonetici@otelturizm.com"),
            ("Gökhan Mutlu", "Yönetim", "info+genelmudur@otelturizm.com")
        };

        var list = new List<YardimMerkeziTeamMemberViewModel>(raw.Length);
        foreach (var m in raw)
        {
            var q = Uri.EscapeDataString(m.Name);
            list.Add(new YardimMerkeziTeamMemberViewModel
            {
                Name = MaskSurname(m.Name),
                Title = m.Title,
                Email = m.Email,
                AvatarUrl = $"https://ui-avatars.com/api/?name={q}&size=160&background=0b57d0&color=ffffff&bold=true&format=png"
            });
        }

        return list;
    }

    private static string MaskSurname(string fullName)
    {
        var parts = (fullName ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length < 2)
        {
            return fullName ?? string.Empty;
        }

        return $"{string.Join(' ', parts[..^1])} {parts[^1][0]}.";
    }

    private static bool IsMissingTableOrColumn(SqlException ex)
    {
        var msg = ex.Message ?? string.Empty;
        return msg.Contains("Invalid object name", StringComparison.OrdinalIgnoreCase)
               || msg.Contains("Invalid column name", StringComparison.OrdinalIgnoreCase);
    }

    private static string FixMojibake(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        // Common UTF-8-as-ANSI mojibake fixes for Turkish content.
        // (E.g. "Ä°Ã§erik" -> "İçerik", "GÃ¶rsel" -> "Görsel")
        return value
            .Replace("Ã‡", "Ç", StringComparison.Ordinal)
            .Replace("Ã§", "ç", StringComparison.Ordinal)
            .Replace("Ä±", "ı", StringComparison.Ordinal)
            .Replace("Ä°", "İ", StringComparison.Ordinal)
            .Replace("Ã–", "Ö", StringComparison.Ordinal)
            .Replace("Ã¶", "ö", StringComparison.Ordinal)
            .Replace("Ãœ", "Ü", StringComparison.Ordinal)
            .Replace("Ã¼", "ü", StringComparison.Ordinal)
            .Replace("Åž", "Ş", StringComparison.Ordinal)
            .Replace("ÅŸ", "ş", StringComparison.Ordinal)
            .Replace("ÄŸ", "ğ", StringComparison.Ordinal)
            .Replace("Äž", "Ğ", StringComparison.Ordinal)
            .Replace("Ä", "Ğ", StringComparison.Ordinal)
            .Replace("Ä", "ğ", StringComparison.Ordinal)
            .Replace("Â", "", StringComparison.Ordinal);
    }

    public async Task<SssViewModel> GetFaqPageAsync(string? categorySlug, string? searchTerm, CancellationToken cancellationToken = default)
    {
        var normalizedCategory = string.IsNullOrWhiteSpace(categorySlug) ? "tumu" : categorySlug.Trim().ToLowerInvariant();
        var model = new SssViewModel
        {
            SearchTerm = searchTerm?.Trim() ?? string.Empty,
            ActiveCategorySlug = normalizedCategory
        };

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string categorySql = @"
            SELECT [KATEGORI_ADI], [SEO_SLUG]
            FROM [dbo].[SSS_KATEGORILERI]
            WHERE [AKTIF_MI] = 1
            ORDER BY [SIRALAMA], id;";

        model.Categories.Add(new SssKategoriViewModel
        {
            Name = "Tümü",
            Slug = "tumu",
            IsActive = normalizedCategory == "tumu"
        });

        await using (var command = new SqlCommand(categorySql, connection))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                var slug = reader.GetString(1);
                model.Categories.Add(new SssKategoriViewModel
                {
                    Name = reader.GetString(0),
                    Slug = slug,
                    IsActive = normalizedCategory == slug
                });
            }
        }

        var faqSql = @"
            SELECT
                k.[KATEGORI_ADI],
                k.[SEO_SLUG],
                COALESCE(k.[IKON], 'fa-circle-question') AS ikon,
                s.id,
                s.[SORU],
                s.[CEVAP]
            FROM [dbo].[SSS_SORULARI] s
            INNER JOIN [dbo].[SSS_KATEGORILERI] k ON k.id = s.[SSS_KATEGORI_ID]
            WHERE s.[AKTIF_MI] = 1
              AND k.[AKTIF_MI] = 1";

        if (normalizedCategory != "tumu")
        {
            faqSql += " AND k.seo_slug = @category";
        }

        if (!string.IsNullOrWhiteSpace(model.SearchTerm))
        {
            faqSql += " AND (s.soru LIKE '%' + @search + '%' OR s.cevap LIKE '%' + @search + '%')";
        }

        faqSql += " ORDER BY k.siralama, k.id, s.siralama, s.id;";

        var sections = new Dictionary<string, SssBolumViewModel>(StringComparer.OrdinalIgnoreCase);
        await using (var command = new SqlCommand(faqSql, connection))
        {
            if (normalizedCategory != "tumu")
            {
                command.Parameters.AddWithValue("@category", normalizedCategory);
            }

            if (!string.IsNullOrWhiteSpace(model.SearchTerm))
            {
                command.Parameters.AddWithValue("@search", model.SearchTerm);
            }

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var slug = reader.GetString(1);
                if (!sections.TryGetValue(slug, out var section))
                {
                    section = new SssBolumViewModel
                    {
                        Name = reader.GetString(0),
                        Slug = slug,
                        IconClass = reader.GetString(2)
                    };

                    sections[slug] = section;
                    model.Sections.Add(section);
                }

                section.Questions.Add(new SssSoruViewModel
                {
                    Id = reader.GetInt64(3),
                    Question = reader.GetString(4),
                    Answer = reader.GetString(5)
                });
            }
        }

        const string ctaSql = @"
            SELECT TOP (1) [KANAL_ADI], [ACIKLAMA], [IKON], [BUTON_METIN], [BAGLANTI_URL], [EK_BILGI], [RENK_TONU]
            FROM [dbo].[DESTEK_KANALLARI]
            WHERE [AKTIF_MI] = 1 AND [KANAL_TURU] = 'canli_destek'
            ORDER BY [SIRALAMA], [ID];";

        await using (var command = new SqlCommand(ctaSql, connection))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            if (await reader.ReadAsync(cancellationToken))
            {
                model.CtaChannel = new DestekKanalViewModel
                {
                    Name = reader.GetString(0),
                    Description = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                    IconClass = reader.IsDBNull(2) ? "fa-headset" : reader.GetString(2),
                    ButtonText = reader.IsDBNull(3) ? "Canlı Desteğe Bağlan" : reader.GetString(3),
                    Url = reader.IsDBNull(4) ? "#" : reader.GetString(4),
                    Note = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                    Tone = reader.IsDBNull(6) ? "primary" : reader.GetString(6)
                };
            }
        }

        return model;
    }

    public async Task<KurumsalBlogListingViewModel> GetCompanyBlogListingAsync(CancellationToken cancellationToken = default)
    {
        var model = new KurumsalBlogListingViewModel();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        try
        {
            const string sql = @"
                SELECT [BASLIK], COALESCE([OZET], N''), [SEO_SLUG], COALESCE([HERO_GORSEL_URL], N'')
                FROM [dbo].[YARDIM_MERKEZI_ICERIKLER]
                WHERE [AKTIF_MI] = 1 AND [ICERIK_TURU] = N'blog'
                ORDER BY [ONE_CIKAN_MI] DESC, [SIRALAMA] ASC, [ID] DESC;";
            await using var cmd = new SqlCommand(sql, connection);
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var slug = reader.GetString(2).Trim();
                if (string.IsNullOrWhiteSpace(slug))
                {
                    continue;
                }

                model.Posts.Add(new KurumsalBlogCardViewModel
                {
                    Title = FixMojibake(reader.GetString(0)),
                    Summary = FixMojibake(reader.GetString(1)),
                    Slug = slug,
                    HeroImageUrl = reader.IsDBNull(3) ? null : reader.GetString(3),
                    LinkUrl = $"/blog/{Uri.EscapeDataString(slug)}"
                });
            }
        }
        catch (SqlException ex) when (IsMissingTableOrColumn(ex))
        {
            // tablo yoksa statik teaser kartları view tarafında gösterilir
        }

        return model;
    }
}
