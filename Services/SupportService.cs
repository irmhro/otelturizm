using Microsoft.Extensions.Configuration;
using MySqlConnector;
using otelturizmnew.Models.Destek;
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

        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string categoriesSql = @"
            SELECT kategori_adi, seo_slug, kisa_aciklama, kategori_ikon, renk_kodu
            FROM destek_kategorileri
            WHERE durum = 1
            ORDER BY siralama, id;";

        await using (var command = new MySqlCommand(categoriesSql, connection))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                var slug = reader.GetString(1);
                model.Categories.Add(new DestekKategoriViewModel
                {
                    Name = reader.GetString(0),
                    Slug = slug,
                    Description = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                    IconClass = reader.IsDBNull(3) ? "fa-circle-info" : reader.GetString(3),
                    ColorHex = reader.IsDBNull(4) ? "#003B95" : reader.GetString(4),
                    LinkUrl = $"/sss?kategori={Uri.EscapeDataString(slug)}"
                });
            }
        }

        var topicsSql = @"
            SELECT id, baslik, ozet, COALESCE(ikon, 'fa-circle-question') AS ikon
            FROM destek_makaleleri
            WHERE durum = 1
              AND yardim_merkezinde_goster = 1";

        if (!string.IsNullOrWhiteSpace(model.SearchTerm))
        {
            topicsSql += " AND (baslik LIKE CONCAT('%', @search, '%') OR ozet LIKE CONCAT('%', @search, '%') OR icerik LIKE CONCAT('%', @search, '%'))";
        }

        topicsSql += " ORDER BY one_cikan_mi DESC, siralama, id LIMIT 6;";

        await using (var command = new MySqlCommand(topicsSql, connection))
        {
            if (!string.IsNullOrWhiteSpace(model.SearchTerm))
            {
                command.Parameters.AddWithValue("@search", model.SearchTerm);
            }

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var title = reader.GetString(1);
                model.PopularTopics.Add(new DestekMakaleViewModel
                {
                    Id = reader.GetInt64(0),
                    Title = title,
                    Summary = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                    IconClass = reader.GetString(3),
                    LinkUrl = $"/sss?ara={Uri.EscapeDataString(title)}"
                });
            }
        }

        const string channelsSql = @"
            SELECT kanal_adi, aciklama, ikon, buton_metin, baglanti_url, ek_bilgi, renk_tonu, kanal_turu
            FROM destek_kanallari
            WHERE aktif_mi = 1
            ORDER BY siralama, id;";

        await using (var command = new MySqlCommand(channelsSql, connection))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                var channel = new DestekKanalViewModel
                {
                    Name = reader.GetString(0),
                    Description = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                    IconClass = reader.IsDBNull(2) ? "fa-headset" : reader.GetString(2),
                    ButtonText = reader.IsDBNull(3) ? "Detay" : reader.GetString(3),
                    Url = reader.IsDBNull(4) ? "#" : reader.GetString(4),
                    Note = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                    Tone = reader.IsDBNull(6) ? "primary" : reader.GetString(6)
                };

                model.SupportChannels.Add(channel);
                if (string.Equals(reader.IsDBNull(7) ? string.Empty : reader.GetString(7), "canli_destek", StringComparison.OrdinalIgnoreCase))
                {
                    model.LiveChatChannel = channel;
                }
            }
        }

        return model;
    }

    public async Task<SssViewModel> GetFaqPageAsync(string? categorySlug, string? searchTerm, CancellationToken cancellationToken = default)
    {
        var normalizedCategory = string.IsNullOrWhiteSpace(categorySlug) ? "tumu" : categorySlug.Trim().ToLowerInvariant();
        var model = new SssViewModel
        {
            SearchTerm = searchTerm?.Trim() ?? string.Empty,
            ActiveCategorySlug = normalizedCategory
        };

        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string categorySql = @"
            SELECT kategori_adi, seo_slug
            FROM sss_kategorileri
            WHERE aktif_mi = 1
            ORDER BY siralama, id;";

        model.Categories.Add(new SssKategoriViewModel
        {
            Name = "Tümü",
            Slug = "tumu",
            IsActive = normalizedCategory == "tumu"
        });

        await using (var command = new MySqlCommand(categorySql, connection))
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
                k.kategori_adi,
                k.seo_slug,
                COALESCE(k.ikon, 'fa-circle-question') AS ikon,
                s.id,
                s.soru,
                s.cevap
            FROM sss_sorulari s
            INNER JOIN sss_kategorileri k ON k.id = s.sss_kategori_id
            WHERE s.aktif_mi = 1
              AND k.aktif_mi = 1";

        if (normalizedCategory != "tumu")
        {
            faqSql += " AND k.seo_slug = @category";
        }

        if (!string.IsNullOrWhiteSpace(model.SearchTerm))
        {
            faqSql += " AND (s.soru LIKE CONCAT('%', @search, '%') OR s.cevap LIKE CONCAT('%', @search, '%'))";
        }

        faqSql += " ORDER BY k.siralama, k.id, s.siralama, s.id;";

        var sections = new Dictionary<string, SssBolumViewModel>(StringComparer.OrdinalIgnoreCase);
        await using (var command = new MySqlCommand(faqSql, connection))
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
            SELECT kanal_adi, aciklama, ikon, buton_metin, baglanti_url, ek_bilgi, renk_tonu
            FROM destek_kanallari
            WHERE aktif_mi = 1 AND kanal_turu = 'canli_destek'
            ORDER BY siralama, id
            LIMIT 1;";

        await using (var command = new MySqlCommand(ctaSql, connection))
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
}
