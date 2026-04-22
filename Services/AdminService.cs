using System.Globalization;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using SqlConnection = Microsoft.Data.SqlClient.SqlConnection;
using SqlCommand = Microsoft.Data.SqlClient.SqlCommand;
using SqlTransaction = Microsoft.Data.SqlClient.SqlTransaction;
using SqlException = Microsoft.Data.SqlClient.SqlException;
using otelturizmnew.Models.Paneller.Admin;
using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Services;

public class AdminService : IAdminService
{
    private readonly string _connectionString;

    public AdminService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection tanimli degil.");
    }

    public async Task<AdminDashboardViewModel> GetDashboardAsync(string fullName, string email, string userRole, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var shell = await GetShellAsync(connection, "Dashboard", "Panel genel operasyon durumunu ve kritik metrikleri canli verilerle takip edin.", fullName, email, userRole, cancellationToken);
        var model = new AdminDashboardViewModel { Shell = shell };

        const string metricsSql = @"
            SELECT
                (SELECT COUNT(*) FROM oteller) AS total_hotels,
                (SELECT COUNT(*) FROM rezervasyonlar) AS total_reservations,
                (SELECT COUNT(*) FROM odeme_islemleri WHERE odeme_durumu IN ('Başarılı','Geri Ödendi','Kısmi Geri Ödendi')) AS successful_payments,
                (SELECT COUNT(*) FROM users WHERE rol = 'admin') AS admin_count,
                (SELECT COUNT(*) FROM partner_detaylari WHERE onay_durumu = 'Beklemede') AS pending_partner_count,
                (SELECT COUNT(*) FROM firmalar WHERE COALESCE(onay_durumu, 'Beklemede') = 'Beklemede') AS pending_company_count,
                (SELECT COUNT(*) FROM oteller WHERE yayin_durumu = 'Yayında' AND onay_durumu = 'Onaylandı') AS active_hotel_count,
                (SELECT COUNT(*) FROM oteller WHERE COALESCE(onay_durumu, '') = 'Beklemede') AS pending_hotel_count;";

        await using (var command = new SqlCommand(metricsSql, connection))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            if (await reader.ReadAsync(cancellationToken))
            {
                model.Metrics.Add(new AdminMetricCardViewModel { Label = "Toplam Otel", Value = SafeInt(reader, 0).ToString(), TrendText = "Yayin, taslak ve bakim tum oteller", IconClass = "fa-hotel", ToneClass = "info" });
                model.Metrics.Add(new AdminMetricCardViewModel { Label = "Toplam Rezervasyon", Value = SafeInt(reader, 1).ToString(), TrendText = "Tum rezervasyon kayitlari", IconClass = "fa-calendar-check", ToneClass = "success" });
                model.Metrics.Add(new AdminMetricCardViewModel { Label = "Basarili Odeme", Value = SafeInt(reader, 2).ToString(), TrendText = "Tahsilat ve iade dahil tamamlanan islemler", IconClass = "fa-credit-card", ToneClass = "warning" });
                model.Metrics.Add(new AdminMetricCardViewModel { Label = "Admin Kullanici", Value = SafeInt(reader, 3).ToString(), TrendText = "Yonetsel yetkili aktif hesaplar", IconClass = "fa-user-shield", ToneClass = "danger" });
                model.Metrics.Add(new AdminMetricCardViewModel { Label = "Bekleyen Partner", Value = SafeInt(reader, 4).ToString(), TrendText = "Onay bekleyen partner basvurulari", IconClass = "fa-handshake", ToneClass = "warning" });
                model.Metrics.Add(new AdminMetricCardViewModel { Label = "Bekleyen Firma", Value = SafeInt(reader, 5).ToString(), TrendText = "Onay bekleyen firma basvurulari", IconClass = "fa-building", ToneClass = "info" });
                model.Metrics.Add(new AdminMetricCardViewModel { Label = "Açık Otel", Value = SafeInt(reader, 6).ToString(), TrendText = "Yayinda ve onayli tesisler", IconClass = "fa-tower-broadcast", ToneClass = "success" });
                model.Metrics.Add(new AdminMetricCardViewModel { Label = "Bekleyen Otel", Value = SafeInt(reader, 7).ToString(), TrendText = "Onay/yayin aksiyonunda bekleyen tesisler", IconClass = "fa-hourglass-half", ToneClass = "danger" });
            }
        }

        const string chartSql = @"
            SELECT FORMAT(olusturulma_tarihi, 'MMM', 'tr-TR') AS ay, COUNT(*) AS adet
            FROM rezervasyonlar
            WHERE olusturulma_tarihi >= DATEADD(MONTH, -5, DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1))
            GROUP BY YEAR(olusturulma_tarihi), MONTH(olusturulma_tarihi), FORMAT(olusturulma_tarihi, 'MMM', 'tr-TR')
            ORDER BY YEAR(olusturulma_tarihi), MONTH(olusturulma_tarihi);";

        var chartRows = new List<(string Label, int Value)>();
        await using (var chartCommand = new SqlCommand(chartSql, connection))
        await using (var chartReader = await chartCommand.ExecuteReaderAsync(cancellationToken))
        {
            while (await chartReader.ReadAsync(cancellationToken))
            {
                chartRows.Add((chartReader.GetString(0), SafeInt(chartReader, 1)));
            }
        }

        var maxChart = Math.Max(chartRows.Count == 0 ? 0 : chartRows.Max(static item => item.Value), 1);
        foreach (var row in chartRows)
        {
            model.ReservationChart.Add(new AdminChartBarViewModel
            {
                Label = row.Label,
                Value = row.Value,
                HeightPercent = Math.Max(12, (int)Math.Round(row.Value * 100m / maxChart))
            });
        }

        const string activitySql = @"
            SELECT TOP (6) *
            FROM
            (
                SELECT 'Partner basvurusu' AS baslik,
                       CONCAT(p.firma_unvani, ' · ', p.onay_durumu) AS alt_baslik,
                       p.olusturulma_tarihi AS zaman
                FROM partner_detaylari p
                UNION ALL
                SELECT 'Admin islemi',
                       CONCAT(a.hedef_tablo, ' · ', a.islem_turu),
                       a.islem_tarihi
                FROM admin_islem_loglari a
                UNION ALL
                SELECT 'Sistem hatasi',
                       CONCAT(s.hata_seviyesi, ' · ', LEFT(s.hata_mesaji, 70)),
                       s.olusma_tarihi
                FROM sistem_hata_loglari s
            ) activity_feed
            ORDER BY zaman DESC;";

        await using (var activityCommand = new SqlCommand(activitySql, connection))
        await using (var activityReader = await activityCommand.ExecuteReaderAsync(cancellationToken))
        {
            while (await activityReader.ReadAsync(cancellationToken))
            {
                var title = activityReader.GetString(0);
                var tone = title.Contains("hata", StringComparison.OrdinalIgnoreCase)
                    ? "danger"
                    : title.Contains("Partner", StringComparison.OrdinalIgnoreCase) ? "warning" : "info";

                model.Activities.Add(new AdminActivityViewModel
                {
                    Title = title,
                    Subtitle = activityReader.GetString(1),
                    TimeText = FormatRelative(activityReader.IsDBNull(2) ? null : activityReader.GetDateTime(2)),
                    IconClass = title.Contains("hata", StringComparison.OrdinalIgnoreCase) ? "fa-triangle-exclamation" : title.Contains("Admin", StringComparison.OrdinalIgnoreCase) ? "fa-user-gear" : "fa-file-signature",
                    ToneClass = tone
                });
            }
        }

        const string hotelsSql = @"
            SELECT TOP (6)
                o.otel_adi,
                CONCAT(o.ilce, ', ', o.sehir) AS sehir_label,
                o.yayin_durumu,
                o.ortalama_puan,
                COUNT(r.id) AS rezervasyon_adedi
            FROM oteller o
            LEFT JOIN rezervasyonlar r ON r.otel_id = o.id
            GROUP BY o.id, o.otel_adi, o.ilce, o.sehir, o.yayin_durumu, o.ortalama_puan
            ORDER BY rezervasyon_adedi DESC, o.id DESC;";

        await using (var hotelsCommand = new SqlCommand(hotelsSql, connection))
        await using (var hotelsReader = await hotelsCommand.ExecuteReaderAsync(cancellationToken))
        {
            while (await hotelsReader.ReadAsync(cancellationToken))
            {
                var status = hotelsReader.GetString(2);
                model.HighlightHotels.Add(new AdminDashboardHotelRowViewModel
                {
                    HotelName = hotelsReader.GetString(0),
                    CityLabel = hotelsReader.GetString(1),
                    StatusLabel = status,
                    StatusToneClass = MapStatusTone(status),
                    ScoreText = hotelsReader.IsDBNull(3) ? "-" : hotelsReader.GetDecimal(3).ToString("0.0"),
                    ReservationText = SafeInt(hotelsReader, 4).ToString()
                });
            }
        }

        return model;
    }

    public async Task<AdminSectionPageViewModel> GetSectionPageAsync(string sectionKey, string fullName, string email, string userRole, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var config = GetSectionConfig(sectionKey);
        var model = new AdminSectionPageViewModel
        {
            SectionKey = sectionKey,
            Shell = await GetShellAsync(connection, config.Title, config.Subtitle, fullName, email, userRole, cancellationToken),
            EmptyStateMessage = config.EmptyMessage,
            InfoNote = config.InfoNote
        };

        model.Columns.AddRange(config.Columns.Select(static column => new AdminTableColumnViewModel { Label = column }));

        await FillSummaryCardsAsync(connection, model, sectionKey, cancellationToken);
        await FillTableAsync(connection, model, sectionKey, cancellationToken);

        return model;
    }

    public async Task<AdminPartnerApplicationsPageViewModel> GetPartnerApplicationsAsync(string fullName, string email, string userRole, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var model = new AdminPartnerApplicationsPageViewModel
        {
            Shell = await GetShellAsync(connection, "Partner Basvurulari", "Partner onboarding, e-posta dogrulama ve admin onay akislarini yonetin.", fullName, email, userRole, cancellationToken)
        };

        var summaryDefinitions = GetSummaryDefinitions("partner-applications").ToList();
        model.SummaryCards.AddRange(summaryDefinitions.Select(static item => new AdminSummaryCardViewModel
        {
            Label = item.Label,
            Description = item.Description,
            ToneClass = item.ToneClass,
            IconClass = item.IconClass
        }));

        for (var i = 0; i < model.SummaryCards.Count; i++)
        {
            await using var command = new SqlCommand(summaryDefinitions[i].Sql, connection);
            var raw = await command.ExecuteScalarAsync(cancellationToken);
            model.SummaryCards[i].Value = FormatScalar(raw);
        }

        const string sql = @"
            SELECT p.id, p.kullanici_id, o.id AS hotel_id, p.firma_unvani, COALESCE(o.otel_adi, p.firma_unvani),
                   p.yetkili_ad_soyad, p.yetkili_eposta, p.vergi_numarasi, p.onay_durumu, p.olusturulma_tarihi,
                   p.onay_tarihi, u.email_dogrulama_tarihi,
                   (SELECT COUNT(*) FROM partner_basvuru_evraklari ped WHERE ped.partner_id = p.id) AS document_count,
                   COALESCE(p.red_nedeni, '')
            FROM partner_detaylari p
            INNER JOIN users u ON u.id = p.kullanici_id
            LEFT JOIN oteller o ON o.partner_id = p.id
            ORDER BY
                CASE p.onay_durumu
                    WHEN 'Beklemede' THEN 0
                    WHEN 'Reddedildi' THEN 1
                    WHEN 'Askida' THEN 2
                    ELSE 3
                END,
                p.olusturulma_tarihi DESC;";

        await using var listCommand = new SqlCommand(sql, connection);
        await using var reader = await listCommand.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var status = reader.GetString(8);
            model.Applications.Add(new AdminPartnerApplicationRowViewModel
            {
                PartnerId = reader.GetInt64(0),
                UserId = reader.GetInt64(1),
                HotelId = reader.IsDBNull(2) ? null : reader.GetInt64(2),
                CompanyName = reader.GetString(3),
                HotelName = reader.GetString(4),
                ContactName = reader.GetString(5),
                Email = reader.GetString(6),
                TaxNumber = reader.GetString(7),
                StatusText = status,
                StatusToneClass = status switch
                {
                    "Onaylandi" => "success",
                    "Reddedildi" => "danger",
                    "Askida" => "warning",
                    _ => "info"
                },
                RegistrationDateText = reader.GetDateTime(9).ToString("dd.MM.yyyy HH:mm", CultureInfo.GetCultureInfo("tr-TR")),
                ApprovalDateText = reader.IsDBNull(10) ? null : reader.GetDateTime(10).ToString("dd.MM.yyyy HH:mm", CultureInfo.GetCultureInfo("tr-TR")),
                EmailVerified = !reader.IsDBNull(11),
                DocumentCount = SafeInt(reader, 12),
                ReviewNote = reader.IsDBNull(13) ? null : reader.GetString(13)
            });
        }

        return model;
    }

    public async Task<(bool Success, string Message)> ReviewPartnerApplicationAsync(long adminUserId, AdminPartnerApplicationDecisionRequest request, CancellationToken cancellationToken = default)
    {
        if (request.PartnerId <= 0)
        {
            return (false, "Guncellenecek partner basvurusu bulunamadi.");
        }

        var targetStatus = request.TargetStatus switch
        {
            "Onaylandi" => "Onaylandi",
            "Reddedildi" => "Reddedildi",
            "Askida" => "Askida",
            _ => string.Empty
        };

        if (string.IsNullOrWhiteSpace(targetStatus))
        {
            return (false, "Gecersiz partner basvuru durumu secildi.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            const string readSql = """
                SELECT TOP (1) kullanici_id, onay_durumu
                FROM partner_detaylari
                WHERE id = @partnerId;
                """;

            long userId;
            string currentStatus;
            await using (var readCommand = new SqlCommand(readSql, connection, (SqlTransaction)transaction))
            {
                readCommand.Parameters.AddWithValue("@partnerId", request.PartnerId);
                await using var reader = await readCommand.ExecuteReaderAsync(cancellationToken);
                if (!await reader.ReadAsync(cancellationToken))
                {
                    return (false, "Partner basvurusu bulunamadi.");
                }

                userId = reader.GetInt64(0);
                currentStatus = reader.GetString(1);
            }

            const string updateSql = @"
                UPDATE partner_detaylari
                SET onay_durumu = @targetStatus,
                    onay_tarihi = CASE WHEN @targetStatus = 'Onaylandi' THEN SYSUTCDATETIME() ELSE onay_tarihi END,
                    onaylayan_admin_id = @adminUserId,
                    red_nedeni = @note,
                    guncellenme_tarihi = SYSUTCDATETIME()
                WHERE id = @partnerId;";

            await using (var updateCommand = new SqlCommand(updateSql, connection, (SqlTransaction)transaction))
            {
                updateCommand.Parameters.AddWithValue("@targetStatus", targetStatus);
                updateCommand.Parameters.AddWithValue("@adminUserId", adminUserId);
                updateCommand.Parameters.AddWithValue("@note", string.IsNullOrWhiteSpace(request.Note) ? DBNull.Value : request.Note.Trim());
                updateCommand.Parameters.AddWithValue("@partnerId", request.PartnerId);
                await updateCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            const string hotelUpdateSql = @"
                UPDATE oteller
                SET onay_durumu = CASE
                        WHEN @targetStatus = 'Onaylandi' THEN 'Onaylandı'
                        WHEN @targetStatus = 'Reddedildi' THEN 'Reddedildi'
                        ELSE 'Beklemede'
                    END,
                    yayin_durumu = CASE
                        WHEN @targetStatus = 'Askida' THEN 'Askıda'
                        WHEN @targetStatus = 'Reddedildi' THEN 'Taslak'
                        ELSE yayin_durumu
                    END,
                    onay_tarihi = CASE WHEN @targetStatus = 'Onaylandi' THEN SYSUTCDATETIME() ELSE onay_tarihi END
                WHERE partner_id = @partnerId;";

            await using (var hotelUpdateCommand = new SqlCommand(hotelUpdateSql, connection, (SqlTransaction)transaction))
            {
                hotelUpdateCommand.Parameters.AddWithValue("@targetStatus", targetStatus);
                hotelUpdateCommand.Parameters.AddWithValue("@partnerId", request.PartnerId);
                await hotelUpdateCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            if (await TableExistsAsync(connection, "partner_basvuru_hareketleri", cancellationToken, (SqlTransaction?)transaction))
            {
                const string historySql = @"
                    INSERT INTO partner_basvuru_hareketleri
                    (partner_id, onceki_durum, yeni_durum, islem_tipi, aciklama, islem_yapan_kullanici_id, olusturulma_tarihi)
                    VALUES
                    (@partnerId, @currentStatus, @targetStatus, 'AdminPartnerBasvuruKarari', @note, @adminUserId, SYSUTCDATETIME());";

                await using var historyCommand = new SqlCommand(historySql, connection, (SqlTransaction)transaction);
                historyCommand.Parameters.AddWithValue("@partnerId", request.PartnerId);
                historyCommand.Parameters.AddWithValue("@currentStatus", currentStatus);
                historyCommand.Parameters.AddWithValue("@targetStatus", targetStatus);
                historyCommand.Parameters.AddWithValue("@note", string.IsNullOrWhiteSpace(request.Note) ? "Admin partner basvurusunu guncelledi." : request.Note.Trim());
                historyCommand.Parameters.AddWithValue("@adminUserId", adminUserId);
                await historyCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            const string userSql = """
                UPDATE users
                SET hesap_durumu = CASE WHEN @targetStatus = 'Kara Liste' THEN 0 ELSE hesap_durumu END
                WHERE id = @userId;
                """;

            await using (var userCommand = new SqlCommand(userSql, connection, (SqlTransaction)transaction))
            {
                userCommand.Parameters.AddWithValue("@targetStatus", targetStatus);
                userCommand.Parameters.AddWithValue("@userId", userId);
                await userCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            return (true, targetStatus switch
            {
                "Onaylandi" => "Partner basvurusu onaylandi. Partner artik yayin oncesi son icerik adimlarini tamamlayabilir.",
                "Reddedildi" => "Partner basvurusu reddedildi.",
                "Askida" => "Partner basvurusu askiya alindi.",
                _ => "Partner basvurusu guncellendi."
            });
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<AdminCommissionManagementPageViewModel> GetCommissionManagementAsync(string fullName, string email, string userRole, long? hotelId = null, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var model = new AdminCommissionManagementPageViewModel
        {
            Shell = await GetShellAsync(connection, "Komisyon ve Vergi Ayarlari", "Otel bazli komisyon, KDV ve konaklama vergisi kurallarini tarih bazli yonetin.", fullName, email, userRole, cancellationToken)
        };

        const string hotelsSql = @"
            SELECT o.id, o.otel_adi, o.otel_kodu, CONCAT(o.ilce, ', ', o.sehir) AS sehir_label
            FROM oteller o
            ORDER BY o.otel_adi ASC;";

        await using (var hotelCommand = new SqlCommand(hotelsSql, connection))
        await using (var hotelReader = await hotelCommand.ExecuteReaderAsync(cancellationToken))
        {
            while (await hotelReader.ReadAsync(cancellationToken))
            {
                model.Hotels.Add(new AdminCommissionHotelOptionViewModel
                {
                    HotelId = hotelReader.GetInt64(0),
                    HotelName = hotelReader.GetString(1),
                    HotelCode = hotelReader.IsDBNull(2) ? string.Empty : hotelReader.GetString(2),
                    CityLabel = hotelReader.IsDBNull(3) ? string.Empty : hotelReader.GetString(3),
                    IsSelected = hotelId.HasValue && hotelId.Value == hotelReader.GetInt64(0)
                });
            }
        }

        model.Form.HotelId = hotelId ?? model.Hotels.FirstOrDefault()?.HotelId ?? 0;

        const string summarySql = @"
            SELECT
                (SELECT COUNT(*) FROM komisyon_vergiler) AS total_rule_count,
                (SELECT COUNT(DISTINCT otel_id) FROM komisyon_vergiler WHERE aktif_mi = 1) AS active_hotel_count,
                (SELECT COALESCE(AVG(komisyon_orani), 0) FROM komisyon_vergiler WHERE aktif_mi = 1) AS avg_commission_rate,
                (
                    SELECT COALESCE(SUM(COALESCE(kdv_orani, 0) + COALESCE(konaklama_vergisi_orani, 0)), 0)
                    FROM komisyon_vergiler
                    WHERE aktif_mi = 1
                ) AS total_tax_rate_sum;";

        await using (var summaryCommand = new SqlCommand(summarySql, connection))
        await using (var summaryReader = await summaryCommand.ExecuteReaderAsync(cancellationToken))
        {
            if (await summaryReader.ReadAsync(cancellationToken))
            {
                model.SummaryCards.Add(new AdminSummaryCardViewModel { Label = "Toplam Kural", Value = SafeInt(summaryReader, 0).ToString(), Description = "Tarih bazli komisyon ve vergi setleri", ToneClass = "info", IconClass = "fa-layer-group" });
                model.SummaryCards.Add(new AdminSummaryCardViewModel { Label = "Aktif Otel", Value = SafeInt(summaryReader, 1).ToString(), Description = "En az bir aktif kural tanimli oteller", ToneClass = "success", IconClass = "fa-hotel" });
                model.SummaryCards.Add(new AdminSummaryCardViewModel { Label = "Ort. Komisyon", Value = $"{SafeDecimal(summaryReader, 2):0.##}%", Description = "Aktif kurallarin ortalama komisyon orani", ToneClass = "warning", IconClass = "fa-percent" });
                model.SummaryCards.Add(new AdminSummaryCardViewModel { Label = "Toplam Vergi Oranlari", Value = $"{SafeDecimal(summaryReader, 3):0.##}%", Description = "KDV + konaklama vergisi toplamlari", ToneClass = "danger", IconClass = "fa-receipt" });
            }
        }

        const string rulesSql = @"
            SELECT TOP (100)
                kv.id,
                kv.otel_id,
                o.otel_adi,
                o.otel_kodu,
                CONCAT(o.ilce, ', ', o.sehir) AS sehir_label,
                kv.baslangic_tarihi,
                kv.bitis_tarihi,
                kv.komisyon_orani,
                kv.komisyon_gelir_vergisi_orani,
                kv.kdv_orani,
                kv.konaklama_vergisi_orani,
                kv.aktif_mi,
                kv.aciklama
            FROM komisyon_vergiler kv
            INNER JOIN oteller o ON o.id = kv.otel_id
            WHERE (@hotelId IS NULL OR kv.otel_id = @hotelId)
            ORDER BY kv.otel_id ASC, kv.baslangic_tarihi DESC, kv.id DESC;";

        await using (var rulesCommand = new SqlCommand(rulesSql, connection))
        {
            rulesCommand.Parameters.AddWithValue("@hotelId", hotelId.HasValue ? hotelId.Value : DBNull.Value);
            await using var reader = await rulesCommand.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var commissionRate = SafeDecimal(reader, 7);
                var commissionIncomeTaxRate = SafeDecimal(reader, 8);
                var vatRate = SafeDecimal(reader, 9);
                var accommodationTaxRate = SafeDecimal(reader, 10);
                var grossCommissionAmount = Math.Round(3500m * commissionRate / 100m, 2, MidpointRounding.AwayFromZero);
                var netCommissionAmount = grossCommissionAmount - Math.Round(grossCommissionAmount * commissionIncomeTaxRate / 100m, 2, MidpointRounding.AwayFromZero);

                model.Rules.Add(new AdminCommissionRuleRowViewModel
                {
                    RuleId = reader.GetInt64(0),
                    HotelId = reader.GetInt64(1),
                    HotelName = reader.GetString(2),
                    HotelCode = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                    CityLabel = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                    DateRangeText = reader.IsDBNull(6)
                        ? $"{reader.GetDateTime(5):dd.MM.yyyy} - Acik Uclu"
                        : $"{reader.GetDateTime(5):dd.MM.yyyy} - {reader.GetDateTime(6):dd.MM.yyyy}",
                    CommissionText = $"%{commissionRate:0.##} komisyon / %{commissionIncomeTaxRate:0.##} gelir vergisi",
                    TaxText = $"KDV %{vatRate:0.##} + Konaklama %{accommodationTaxRate:0.##}",
                    NetText = $"{grossCommissionAmount:0.##} brüt / {netCommissionAmount:0.##} net",
                    IsActive = SafeBool(reader, 11),
                    Note = reader.IsDBNull(12) ? null : reader.GetString(12)
                });
            }
        }

        if (hotelId.HasValue)
        {
            var selectedRule = model.Rules.FirstOrDefault(static item => item.IsActive);
            if (selectedRule is not null)
            {
                model.Form.HotelId = selectedRule.HotelId;
            }
        }

        const string financeSql = @"
            SELECT TOP (20)
                o.id,
                o.otel_adi,
                COALESCE(reservationStats.gross_revenue, 0) AS gross_revenue,
                COALESCE(commissionStats.total_commission, 0) AS total_commission,
                COALESCE(commissionStats.paid_commission, 0) AS paid_commission
            FROM oteller o
            OUTER APPLY
            (
                SELECT SUM(COALESCE(r.toplam_tutar, 0)) AS gross_revenue
                FROM rezervasyonlar r
                WHERE r.otel_id = o.id
                  AND COALESCE(r.durum, '') <> 'İptal Edildi'
            ) reservationStats
            OUTER APPLY
            (
                SELECT
                    SUM(COALESCE(k.komisyon_tutari, 0)) AS total_commission,
                    SUM(CASE WHEN COALESCE(k.otele_odeme_durumu, '') = 'Ödendi' THEN COALESCE(k.komisyon_tutari, 0) ELSE 0 END) AS paid_commission
                FROM komisyon_muhasebe_kayitlari k
                WHERE k.otel_id = o.id
            ) commissionStats
            WHERE (@hotelId IS NULL OR o.id = @hotelId)
              AND (COALESCE(reservationStats.gross_revenue, 0) > 0 OR COALESCE(commissionStats.total_commission, 0) > 0)
            ORDER BY COALESCE(reservationStats.gross_revenue, 0) DESC, o.id DESC;";

        await using (var financeCommand = new SqlCommand(financeSql, connection))
        {
            financeCommand.Parameters.AddWithValue("@hotelId", hotelId.HasValue ? hotelId.Value : DBNull.Value);
            await using var financeReader = await financeCommand.ExecuteReaderAsync(cancellationToken);
            while (await financeReader.ReadAsync(cancellationToken))
            {
                model.HotelFinanceRows.Add(new AdminHotelCommissionFinanceRowViewModel
                {
                    HotelId = financeReader.GetInt64(0),
                    HotelName = financeReader.GetString(1),
                    GrossRevenue = SafeDecimal(financeReader, 2),
                    TotalCommission = SafeDecimal(financeReader, 3),
                    PaidCommission = SafeDecimal(financeReader, 4)
                });
            }
        }

        return model;
    }

    public async Task<(bool Success, string Message)> SaveCommissionRuleAsync(long adminUserId, AdminCommissionRuleForm request, CancellationToken cancellationToken = default)
    {
        if (request.HotelId <= 0)
        {
            return (false, "Komisyon tanimi icin otel secilmelidir.");
        }

        if (request.EndDate.HasValue && request.EndDate.Value.Date < request.StartDate.Date)
        {
            return (false, "Bitis tarihi baslangic tarihinden once olamaz.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = @"
            IF EXISTS (SELECT 1 FROM komisyon_vergiler WHERE id = @ruleId)
            BEGIN
                UPDATE komisyon_vergiler
                SET otel_id = @hotelId,
                    baslangic_tarihi = @startDate,
                    bitis_tarihi = @endDate,
                    komisyon_orani = @commissionRate,
                    komisyon_gelir_vergisi_orani = @commissionIncomeTaxRate,
                    kdv_orani = @vatRate,
                    konaklama_vergisi_orani = @accommodationTaxRate,
                    para_birimi = @currency,
                    aktif_mi = 1,
                    aciklama = @note,
                    guncelleyen_kullanici_id = @adminUserId,
                    guncellenme_tarihi = SYSUTCDATETIME()
                WHERE id = @ruleId;
            END
            ELSE
            BEGIN
                INSERT INTO komisyon_vergiler
                (
                    otel_id, baslangic_tarihi, bitis_tarihi, komisyon_orani, komisyon_gelir_vergisi_orani,
                    kdv_orani, konaklama_vergisi_orani, para_birimi, aktif_mi, aciklama, olusturan_kullanici_id, guncelleyen_kullanici_id
                )
                VALUES
                (
                    @hotelId, @startDate, @endDate, @commissionRate, @commissionIncomeTaxRate,
                    @vatRate, @accommodationTaxRate, @currency, 1, @note, @adminUserId, @adminUserId
                );
            END;";

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@ruleId", request.RuleId.HasValue ? request.RuleId.Value : DBNull.Value);
        command.Parameters.AddWithValue("@hotelId", request.HotelId);
        command.Parameters.AddWithValue("@startDate", request.StartDate.Date);
        command.Parameters.AddWithValue("@endDate", request.EndDate.HasValue ? request.EndDate.Value.Date : DBNull.Value);
        command.Parameters.AddWithValue("@commissionRate", request.CommissionRate);
        command.Parameters.AddWithValue("@commissionIncomeTaxRate", request.CommissionIncomeTaxRate);
        command.Parameters.AddWithValue("@vatRate", request.VatRate);
        command.Parameters.AddWithValue("@accommodationTaxRate", request.AccommodationTaxRate);
        command.Parameters.AddWithValue("@currency", string.IsNullOrWhiteSpace(request.Currency) ? "TRY" : request.Currency.Trim().ToUpperInvariant());
        command.Parameters.AddWithValue("@note", string.IsNullOrWhiteSpace(request.Note) ? DBNull.Value : request.Note.Trim());
        command.Parameters.AddWithValue("@adminUserId", adminUserId);
        await command.ExecuteNonQueryAsync(cancellationToken);

        return (true, "Komisyon ve vergi kurali kaydedildi.");
    }

    private async Task<AdminShellViewModel> GetShellAsync(SqlConnection connection, string title, string subtitle, string fullName, string email, string userRole, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT
                (SELECT COUNT(*) FROM partner_detaylari WHERE onay_durumu = 'Beklemede') AS pending_partner_applications,
                (SELECT COUNT(*) FROM firmalar WHERE COALESCE(onay_durumu, 'Beklemede') = 'Beklemede') AS pending_company_applications,
                (SELECT COUNT(*) FROM sistem_ici_bildirimler WHERE okundu_mu = 0) AS unread_notifications,
                (SELECT COUNT(*) FROM sistem_hata_loglari WHERE hata_seviyesi IN ('CRITICAL','ALERT','EMERGENCY') AND cozuldu_mu = 0) AS critical_logs,
                (SELECT COUNT(*) FROM yorumlar WHERE onay_durumu = 'Beklemede') AS pending_reviews;";

        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var shell = new AdminShellViewModel { FullName = fullName, Email = email, UserRole = userRole, PanelTitle = title, PanelSubtitle = subtitle };
        if (await reader.ReadAsync(cancellationToken))
        {
            shell.PendingPartnerApplications = SafeInt(reader, 0);
            shell.PendingCompanyApplications = SafeInt(reader, 1);
            shell.UnreadNotifications = SafeInt(reader, 2);
            shell.CriticalLogs = SafeInt(reader, 3);
            shell.PendingReviews = SafeInt(reader, 4);
        }

        return shell;
    }

    private static async Task FillSummaryCardsAsync(SqlConnection connection, AdminSectionPageViewModel model, string sectionKey, CancellationToken cancellationToken)
    {
        var cards = GetSummaryDefinitions(sectionKey);
        foreach (var card in cards)
        {
            await using var command = new SqlCommand(card.Sql, connection);
            var rawValue = await command.ExecuteScalarAsync(cancellationToken);
            model.SummaryCards.Add(new AdminSummaryCardViewModel
            {
                Label = card.Label,
                Value = FormatScalar(rawValue),
                Description = card.Description,
                ToneClass = card.ToneClass,
                IconClass = card.IconClass
            });
        }
    }

    private static async Task FillTableAsync(SqlConnection connection, AdminSectionPageViewModel model, string sectionKey, CancellationToken cancellationToken)
    {
        var sql = GetTableSql(sectionKey);
        if (string.IsNullOrWhiteSpace(sql))
        {
            return;
        }

        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var row = new List<string>();
            for (var i = 0; i < reader.FieldCount; i++)
            {
                row.Add(reader.IsDBNull(i) ? "-" : reader.GetValue(i)?.ToString() ?? "-");
            }

            model.Rows.Add(row);
        }
    }

    private static (string Title, string Subtitle, string[] Columns, string EmptyMessage, string? InfoNote) GetSectionConfig(string sectionKey)
    {
        return sectionKey switch
        {
            "users" => ("Kullanicilar", "Tum kullanici tiplerini, rollerini ve hesap durumlarini veritabani kayitlari ile yonetin.", new[] { "Kullanici", "E-posta", "Telefon", "Uyelik", "Rezervasyon", "Puan", "Durum", "Islem" }, "Kullanici kaydi bulunamadi.", null),
            "managers" => ("Yoneticiler", "Admin ve ekip kullanicilarini departman ve rol dagilimi ile izleyin.", new[] { "Ad Soyad", "E-posta", "Departman", "Rol", "Son Giris" }, "Yonetici kaydi bulunamadi.", null),
            "hotels" => ("Oteller", "Otel, yayin ve onay durumlarini tek ekranda izleyin.", new[] { "Otel", "Konum", "Tur", "Yayin", "Onay", "Puan" }, "Otel kaydi bulunamadi.", null),
            "hotel-detail" => ("Otel Detay", "Referans admin otel detay ekranini, secili otelin tum panel verileri ile baglayacagiz.", Array.Empty<string>(), "Detay ekrani icin otel secimi gerekiyor.", "Bu ekran sonraki adimda secili otel bazli detay verilerle ayri servisle baglanacak."),
            "reservations" => ("Rezervasyonlar", "Rezervasyon hareketlerini dogrudan yerel veritabanindan izleyin.", new[] { "Rez. No", "Misafir", "Giris", "Cikis", "Durum", "Tutar" }, "Rezervasyon bulunamadi.", null),
            "payments" => ("Odemeler", "Tahsilat, iade ve risk durumlarini odeme tablosu uzerinden yonetin.", new[] { "Islem No", "Tur", "Durum", "Yontem", "Tahsilat", "Tarih" }, "Odeme kaydi bulunamadi.", null),
            "invoices" => ("Faturalar", "Platform ve otel faturalarini veritabani kayitlari ile izleyin.", new[] { "Fatura No", "Tarih", "Tur", "Durum", "Toplam", "PB" }, "Fatura kaydi bulunamadi.", null),
            "commissions" => ("Komisyonlar", "Komisyon muhasebe ve mutabakat durumlarini takip edin.", new[] { "Kayit No", "Donem", "Otel", "Komisyon", "Odeme Durumu", "Mutabakat" }, "Komisyon kaydi bulunamadi.", null),
            "partner-applications" => ("Partner Basvurulari", "Partner onboarding surecini ve onay akisini yonetin.", new[] { "Firma", "Yetkili", "E-posta", "Vergi No", "Durum", "Kayit" }, "Partner basvurusu bulunamadi.", null),
            "company-applications" => ("Firma Basvurulari", "Firma onboarding durumlarini ve rezervasyon onay akislarini izleyin.", new[] { "Firma", "Onay", "Firma Kullanicisi", "Rezervasyon", "Kayit" }, "Firma basvurusu bulunamadi.", null),
            "platform-officials" => ("Platform Yetkilileri", "Admin ve superadmin hesaplarin durumunu ve erisim izlerini yonetin.", new[] { "Ad Soyad", "E-posta", "Rol", "Durum", "Son Giris", "Kayit" }, "Platform yetkilisi bulunamadi.", null),
            "active-hotels" => ("Acik Oteller", "Yayinda ve onayli otelleri operasyonel performans ile izleyin.", new[] { "Otel", "Konum", "Puan", "Rezervasyon", "Gelir", "Guncelleme" }, "Acik otel bulunamadi.", null),
            "pending-hotels" => ("Bekleyen Oteller", "Onay veya yayin bekleyen tesisleri hizli aksiyon listesi olarak yonetin.", new[] { "Otel", "Konum", "Onay", "Yayin", "Olusturma", "Son Guncelleme" }, "Bekleyen otel bulunamadi.", null),
            "reviews" => ("Degerlendirmeler", "Yorum moderasyonu, raporlanan yorumlar ve dogrulanmis konaklama kayitlarini yonetin.", new[] { "Baslik", "Puan", "Durum", "Rapor", "Dogrulama", "Tarih" }, "Yorum kaydi bulunamadi.", null),
            "reports" => ("Raporlar", "Rapor ekranini mevcut operasyon verileri uzerinden kurgulayacagiz.", Array.Empty<string>(), "Rapor veri matrisi bir sonraki fazda kurulur.", "Bu ekran icin rapor snapshot / export altyapisi migration ile eklenecek."),
            "campaigns" => ("Kampanyalar", "Kampanya performansini ve yayindaki indirim kurallarini izleyin.", new[] { "Kampanya", "Tur", "Baslangic", "Bitis", "Aktif", "Kullanim" }, "Kampanya bulunamadi.", null),
            "notifications" => ("Bildirimler", "Panel ici bildirimler ve sablon akislarini yonetin.", new[] { "Baslik", "Tur", "Onem", "Okundu", "Arsiv", "Olusturma" }, "Bildirim bulunamadi.", null),
            "settings" => ("Ayarlar", "Genel ayarlar icin veritabani karsiligi olan ayar tablolarini bir sonraki migration fazinda kuracagiz.", Array.Empty<string>(), "Ayar kaydi icin ayar tablolari gerekiyor.", "Bu ekran mevcut migration setinde karsiligi olmayan yeni tablo ailesi gerektiriyor."),
            "security" => ("Guvenlik", "Guvenlik paneli icin oturum, IP, 2FA ve audit yapisini genisletecegiz.", Array.Empty<string>(), "Guvenlik paneli migration fazinda detaylandirilacak.", "Mevcut tablolar log verir, ancak referans guvenlik ekrani icin ek yapilar gerekiyor."),
            "blog" => ("Blog Yonetimi", "Blog modulu icin yeni tablo ve medya baglantilari olusturulacak.", Array.Empty<string>(), "Blog icin veritabani tablolari henuz eklenmedi.", "Bu ekran icin blog kategori, yazi, etiket ve medya migration'lari acilacak."),
            "email-templates" => ("E-posta Sablonlari", "Mesaj ve bildirim sablonlarini veritabani uzerinden yonetin.", new[] { "Sablon", "Kategori", "Dil", "Aktif", "Sistem Geneli", "Konu" }, "Sablon kaydi bulunamadi.", null),
            "faq" => ("SSS Yonetimi", "SSS kategori ve soru/cevap akisini veritabani kayitlari ile yonetin.", new[] { "Kategori", "Soru", "One Cikan", "Aktif", "Olusturma" }, "SSS kaydi bulunamadi.", null),
            "complaints" => ("Sikayetler", "Sikayet ve itiraz yonetimi icin yeni tablo ailesi planlanacak.", Array.Empty<string>(), "Sikayet modulu tablolari henuz eklenmedi.", "Yorum raporlari var; ancak referanstaki sikayet modulu icin ayri veri modeli gerekiyor."),
            "logs" => ("Log Kayitlari", "Admin islem, sistem hata ve API loglarini merkezi olarak izleyin.", new[] { "Hedef", "Islem", "IP", "Tarih", "Kaynak", "Not" }, "Log kaydi bulunamadi.", null),
            "backups" => ("Yedekleme", "Yedekleme operasyonu icin snapshot kaydi ve dosya metadata tablolarini ekleyecegiz.", Array.Empty<string>(), "Yedekleme kaydi henuz bulunmuyor.", "Referans yedekleme ekrani icin yeni migration gerekir."),
            _ => ("Admin Panel", "Bu admin bolumu icin veritabani baglantisi hazirlaniyor.", Array.Empty<string>(), "Veri bulunamadi.", null)
        };
    }

    private static IEnumerable<(string Label, string Sql, string Description, string ToneClass, string IconClass)> GetSummaryDefinitions(string sectionKey)
    {
        return sectionKey switch
        {
            "users" =>
            [
                ("Toplam Kullanici", "SELECT COUNT(*) FROM users", "Tum hesaplar", "info", "fa-users"),
                ("Aktif Kullanici", "SELECT COUNT(*) FROM users WHERE hesap_durumu = 1", "Giris yapabilen hesaplar", "success", "fa-circle-check"),
                ("Onaysiz E-posta", "SELECT COUNT(*) FROM users WHERE email_dogrulama_tarihi IS NULL", "E-posta dogrulamasi bekleyenler", "warning", "fa-envelope-circle-check"),
                ("Pasif Kullanici", "SELECT COUNT(*) FROM users WHERE COALESCE(hesap_durumu, 0) = 0", "Panele veya siteye erisemeyen hesaplar", "danger", "fa-user-slash")
            ],
            "managers" =>
            [
                ("Yonetici", "SELECT COUNT(*) FROM users WHERE rol = 'admin'", "Admin rolundeki kullanicilar", "danger", "fa-user-tie"),
                ("Departman", "SELECT COUNT(*) FROM departmanlar", "Organizasyon birimleri", "info", "fa-sitemap"),
                ("Rol", "SELECT COUNT(*) FROM roller", "Sistem rolleri", "warning", "fa-key"),
                ("Rol Atamasi", "SELECT COUNT(*) FROM kullanici_rolleri", "Aktif veya gecmis rol kayitlari", "success", "fa-user-check")
            ],
            "hotels" =>
            [
                ("Toplam Otel", "SELECT COUNT(*) FROM oteller", "Tum tesis kayitlari", "info", "fa-hotel"),
                ("Yayinda", "SELECT COUNT(*) FROM oteller WHERE yayin_durumu = 'Yayında'", "Canli satistaki tesisler", "success", "fa-tower-broadcast"),
                ("Bekleyen Onay", "SELECT COUNT(*) FROM oteller WHERE onay_durumu = 'Beklemede'", "Inceleme bekleyen tesisler", "warning", "fa-hourglass-half"),
                ("Oda Tipi", "SELECT COUNT(*) FROM oda_tipleri", "Toplam oda tipi sayisi", "danger", "fa-bed")
            ],
            "reservations" =>
            [
                ("Toplam Rezervasyon", "SELECT COUNT(*) FROM rezervasyonlar", "Tum rezervasyon kayitlari", "info", "fa-calendar-check"),
                ("Onay Bekliyor", "SELECT COUNT(*) FROM rezervasyonlar WHERE durum = 'Onay Bekliyor'", "Islem bekleyen rezervasyonlar", "warning", "fa-clock"),
                ("Tamamlandi", "SELECT COUNT(*) FROM rezervasyonlar WHERE durum = 'Tamamlandı'", "Konaklamasi biten rezervasyonlar", "success", "fa-circle-check"),
                ("Iptal", "SELECT COUNT(*) FROM rezervasyonlar WHERE durum = 'İptal Edildi'", "Iptal edilenler", "danger", "fa-ban")
            ],
            "payments" =>
            [
                ("Odeme Islemi", "SELECT COUNT(*) FROM odeme_islemleri", "Tum odeme hareketleri", "info", "fa-credit-card"),
                ("Basarili", "SELECT COUNT(*) FROM odeme_islemleri WHERE odeme_durumu = 'Başarılı'", "Tamamlanan tahsilatlar", "success", "fa-circle-check"),
                ("Basarisiz", "SELECT COUNT(*) FROM odeme_islemleri WHERE odeme_durumu = 'Başarısız'", "Reddedilen islemler", "danger", "fa-circle-xmark"),
                ("Askida/Bekleyen", "SELECT COUNT(*) FROM odeme_islemleri WHERE odeme_durumu IN ('Beklemede','İşleniyor','Askıda')", "Inceleme veya islem bekleyenler", "warning", "fa-hourglass-half")
            ],
            "invoices" =>
            [
                ("Toplam Fatura", "SELECT COUNT(*) FROM faturalar", "Sistemdeki tum fatura kayitlari", "info", "fa-file-invoice"),
                ("Kesildi", "SELECT COUNT(*) FROM faturalar WHERE fatura_durumu = 'Kesildi'", "Aktif kesilmis faturalar", "success", "fa-file-circle-check"),
                ("Taslak", "SELECT COUNT(*) FROM faturalar WHERE fatura_durumu = 'Taslak'", "Hazirlik asamasindakiler", "warning", "fa-file-pen"),
                ("Iptal", "SELECT COUNT(*) FROM faturalar WHERE fatura_durumu = 'İptal Edildi'", "Iptal edilen faturalar", "danger", "fa-file-circle-xmark")
            ],
            "commissions" =>
            [
                ("Komisyon Kaydi", "SELECT COUNT(*) FROM komisyon_muhasebe_kayitlari", "Muhasebe donem kayitlari", "info", "fa-percent"),
                ("Beklemede", "SELECT COUNT(*) FROM komisyon_muhasebe_kayitlari WHERE otele_odeme_durumu = 'Beklemede'", "Otele odeme bekleyenler", "warning", "fa-wallet"),
                ("Odendi", "SELECT COUNT(*) FROM komisyon_muhasebe_kayitlari WHERE otele_odeme_durumu = 'Ödendi'", "Kapatilan odemeler", "success", "fa-money-bill-transfer"),
                ("Itirazli", "SELECT COUNT(*) FROM komisyon_muhasebe_kayitlari WHERE itiraz_var_mi = 1", "Mutabakat itirazli kayitlar", "danger", "fa-scale-balanced")
            ],
            "partner-applications" =>
            [
                ("Toplam Partner", "SELECT COUNT(*) FROM partner_detaylari", "Tum partner hesaplari", "info", "fa-handshake-angle"),
                ("Beklemede", "SELECT COUNT(*) FROM partner_detaylari WHERE onay_durumu = 'Beklemede'", "Inceleme bekleyen basvurular", "warning", "fa-hourglass-half"),
                ("Onaylandi", "SELECT COUNT(*) FROM partner_detaylari WHERE onay_durumu = 'Onaylandi'", "Aktif partner hesaplari", "success", "fa-circle-check"),
                ("Reddedildi", "SELECT COUNT(*) FROM partner_detaylari WHERE onay_durumu = 'Reddedildi'", "Reddedilen kayitlar", "danger", "fa-circle-xmark")
            ],
            "company-applications" =>
            [
                ("Toplam Firma", "SELECT COUNT(*) FROM firmalar", "Tum firma profilleri", "info", "fa-building"),
                ("Beklemede", "SELECT COUNT(*) FROM firmalar WHERE COALESCE(onay_durumu,'Beklemede') = 'Beklemede'", "Onay bekleyen firmalar", "warning", "fa-hourglass-half"),
                ("Onaylandi", "SELECT COUNT(*) FROM firmalar WHERE COALESCE(onay_durumu,'') = 'Onaylandı'", "Aktif firma hesaplari", "success", "fa-circle-check"),
                ("Firma Rezervasyonu", "SELECT COUNT(*) FROM rezervasyonlar WHERE firma_id IS NOT NULL", "Firma baglantili rezervasyonlar", "danger", "fa-briefcase")
            ],
            "platform-officials" =>
            [
                ("Yetkili Hesap", "SELECT COUNT(*) FROM users WHERE rol IN ('admin','superadmin')", "Admin ve superadmin kullanicilar", "info", "fa-user-shield"),
                ("Aktif Yetkili", "SELECT COUNT(*) FROM users WHERE rol IN ('admin','superadmin') AND hesap_durumu = 1", "Panele erisebilen yetkililer", "success", "fa-user-check"),
                ("Departman Kaydi", "SELECT COUNT(*) FROM kullanici_departman", "Yetkili departman baglantilari", "warning", "fa-sitemap"),
                ("Rol Kaydi", "SELECT COUNT(*) FROM kullanici_rolleri", "Rol atama kayitlari", "danger", "fa-key")
            ],
            "active-hotels" =>
            [
                ("Yayinda", "SELECT COUNT(*) FROM oteller WHERE yayin_durumu = 'Yayında' AND onay_durumu = 'Onaylandı'", "Yayinda ve onayli oteller", "success", "fa-tower-broadcast"),
                ("Toplam Oda Tipi", "SELECT COUNT(*) FROM oda_tipleri ot INNER JOIN oteller o ON o.id = ot.otel_id WHERE o.yayin_durumu = 'Yayında' AND o.onay_durumu = 'Onaylandı'", "Acik otellerdeki oda tipleri", "info", "fa-bed"),
                ("Toplam Rezervasyon", "SELECT COUNT(*) FROM rezervasyonlar r INNER JOIN oteller o ON o.id = r.otel_id WHERE o.yayin_durumu = 'Yayında' AND o.onay_durumu = 'Onaylandı'", "Acik otellere gelen rezervasyonlar", "warning", "fa-calendar-check"),
                ("Toplam Gelir", "SELECT COALESCE(SUM(COALESCE(r.toplam_tutar,0)),0) FROM rezervasyonlar r INNER JOIN oteller o ON o.id = r.otel_id WHERE o.yayin_durumu = 'Yayında' AND o.onay_durumu = 'Onaylandı' AND COALESCE(r.durum,'') <> 'İptal Edildi'", "Iptal disi rezervasyon gelirleri", "danger", "fa-money-bill-wave")
            ],
            "pending-hotels" =>
            [
                ("Bekleyen Onay", "SELECT COUNT(*) FROM oteller WHERE onay_durumu = 'Beklemede'", "Onay bekleyen tesisler", "warning", "fa-hourglass-half"),
                ("Taslak Yayin", "SELECT COUNT(*) FROM oteller WHERE yayin_durumu <> 'Yayında'", "Yayina alinmamis tesisler", "info", "fa-file-pen"),
                ("Partner Basvuru Bekliyor", "SELECT COUNT(*) FROM partner_detaylari WHERE onay_durumu = 'Beklemede'", "Partner adiminda bekleyenler", "danger", "fa-user-clock"),
                ("Eksik Medya", "SELECT COUNT(*) FROM oteller o WHERE NOT EXISTS (SELECT 1 FROM otel_gorselleri g WHERE g.otel_id = o.id)", "Gorsel yuklenmemis oteller", "success", "fa-image")
            ],
            "reviews" =>
            [
                ("Toplam Yorum", "SELECT COUNT(*) FROM yorumlar", "Tesis yorumlari", "info", "fa-star"),
                ("Beklemede", "SELECT COUNT(*) FROM yorumlar WHERE onay_durumu = 'Beklemede'", "Moderasyon bekleyenler", "warning", "fa-hourglass-half"),
                ("Onaylandi", "SELECT COUNT(*) FROM yorumlar WHERE onay_durumu = 'Onaylandı'", "Yayinda olan yorumlar", "success", "fa-thumbs-up"),
                ("Raporlandi", "SELECT COUNT(*) FROM yorumlar WHERE rapor_sayisi > 0", "Incelenmesi gerekenler", "danger", "fa-flag")
            ],
            "campaigns" =>
            [
                ("Kampanya", "SELECT COUNT(*) FROM kampanyalar", "Tum kampanya kayitlari", "info", "fa-bullhorn"),
                ("Aktif", "SELECT COUNT(*) FROM kampanyalar WHERE aktif_mi = 1", "Yayinda kampanyalar", "success", "fa-badge-percent"),
                ("One Cikan", "SELECT COUNT(*) FROM kampanyalar WHERE one_cikan_kampanya = 1", "Ana sayfa on plana cikacak kampanyalar", "warning", "fa-fire"),
                ("Toplam Kullanim", "SELECT COALESCE(SUM(kullanilan_adet),0) FROM kampanyalar", "Kampanya kullanim adedi", "danger", "fa-chart-column")
            ],
            "notifications" =>
            [
                ("Sistem Bildirimi", "SELECT COUNT(*) FROM sistem_ici_bildirimler", "Tum panel bildirimleri", "info", "fa-bell"),
                ("Okunmamis", "SELECT COUNT(*) FROM sistem_ici_bildirimler WHERE okundu_mu = 0", "Henuz gorulmeyen bildirimler", "warning", "fa-envelope-open-text"),
                ("Bildirim Sablonu", "SELECT COUNT(*) FROM bildirim_sablonlari", "Push/SMS/mail sablonlari", "success", "fa-file-lines"),
                ("Mesaj Sablonu", "SELECT COUNT(*) FROM mesaj_sablonlari", "Operasyonel mesaj sablonlari", "danger", "fa-comments")
            ],
            "logs" =>
            [
                ("Admin Islem Logu", "SELECT COUNT(*) FROM admin_islem_loglari", "Yonetici aksiyon kayitlari", "info", "fa-clipboard-list"),
                ("Sistem Hata", "SELECT COUNT(*) FROM sistem_hata_loglari", "Uygulama hata kayitlari", "danger", "fa-bug"),
                ("API Logu", "SELECT COUNT(*) FROM api_loglari", "API erisim loglari", "warning", "fa-cloud-arrow-up"),
                ("Kullanici Aktivitesi", "SELECT COUNT(*) FROM kullanici_aktivite_loglari", "Oturum ve hareket gecmisi", "success", "fa-user-clock")
            ],
            "email-templates" =>
            [
                ("Mesaj Sablonu", "SELECT COUNT(*) FROM mesaj_sablonlari", "Mail/mesaj sablon seti", "info", "fa-envelope"),
                ("Bildirim Sablonu", "SELECT COUNT(*) FROM bildirim_sablonlari", "Push/SMS/system ici sablonlar", "warning", "fa-paper-plane"),
                ("Aktif Mesaj", "SELECT COUNT(*) FROM mesaj_sablonlari WHERE aktif_mi = 1", "Kullanilan mail sablonlari", "success", "fa-circle-check"),
                ("Aktif Bildirim", "SELECT COUNT(*) FROM bildirim_sablonlari WHERE aktif_mi = 1", "Yayinda bildirim sablonlari", "danger", "fa-bell-concierge")
            ],
            "faq" =>
            [
                ("SSS Kategorisi", "SELECT COUNT(*) FROM sss_kategorileri WHERE aktif_mi = 1", "Aktif destek kategorileri", "info", "fa-layer-group"),
                ("Toplam Soru", "SELECT COUNT(*) FROM sss_sorulari", "Tum soru ve cevap kayitlari", "warning", "fa-circle-question"),
                ("One Cikan", "SELECT COUNT(*) FROM sss_sorulari WHERE one_cikan_mi = 1", "Ana akista vurgulanan sorular", "success", "fa-fire"),
                ("Aktif", "SELECT COUNT(*) FROM sss_sorulari WHERE aktif_mi = 1", "Yayinda olan soru/cevaplar", "danger", "fa-circle-check")
            ],
            _ => []
        };
    }

    private static string GetTableSql(string sectionKey)
    {
        return sectionKey switch
        {
            "users" => @"SELECT TOP (40)
                                CAST(u.id AS nvarchar(30)),
                                COALESCE(NULLIF(u.ad_soyad, ''), '-'),
                                COALESCE(NULLIF(u.eposta, ''), '-'),
                                COALESCE(NULLIF(u.telefon, ''), NULLIF(u.telefon_e164, ''), '-'),
                                CASE
                                    WHEN reservationStats.reservation_count >= 10 OR reservationStats.total_spent >= 100000 THEN 'Gold'
                                    WHEN reservationStats.reservation_count >= 4 OR reservationStats.total_spent >= 30000 THEN 'Silver'
                                    ELSE 'Bronze'
                                END,
                                CAST(reservationStats.reservation_count AS nvarchar(20)),
                                FORMAT(reservationStats.loyalty_points, 'N0', 'tr-TR'),
                                CASE
                                    WHEN COALESCE(u.hesap_durumu, 0) = 0 THEN 'Pasif'
                                    WHEN u.email_dogrulama_tarihi IS NULL THEN 'Onaysiz'
                                    ELSE 'Aktif'
                                END,
                                COALESCE(NULLIF(u.rol, ''), 'user'),
                                FORMAT(u.olusturulma_tarihi, 'dd.MM.yyyy', 'tr-TR')
                         FROM users u
                         OUTER APPLY
                         (
                             SELECT
                                 COUNT(r.id) AS reservation_count,
                                 COALESCE(SUM(COALESCE(r.toplam_tutar, 0)), 0) AS total_spent,
                                 CAST(ROUND(COALESCE(SUM(COALESCE(r.toplam_tutar, 0)), 0) / 12.5, 0) AS int) AS loyalty_points
                             FROM rezervasyonlar r
                             WHERE r.kullanici_id = u.id
                               AND COALESCE(r.durum, '') <> 'İptal Edildi'
                         ) reservationStats
                         ORDER BY u.id DESC;",
            "managers" => @"SELECT TOP (12) u.ad_soyad, u.eposta, COALESCE(d.departman_adi, '-'), COALESCE(r.rol_adi, u.rol), COALESCE(FORMAT(u.son_giris_tarihi, 'dd.MM.yyyy HH:mm', 'tr-TR'), '-') FROM users u LEFT JOIN kullanici_departman kd ON kd.kullanici_id = u.id LEFT JOIN departmanlar d ON d.id = kd.departman_id LEFT JOIN kullanici_rolleri kr ON kr.kullanici_id = u.id AND (kr.bitis_tarihi IS NULL OR kr.bitis_tarihi > SYSUTCDATETIME()) LEFT JOIN roller r ON r.id = kr.rol_id WHERE u.rol = 'admin' ORDER BY u.id DESC;",
            "hotels" => @"SELECT TOP (12) otel_adi, CONCAT(ilce, ', ', sehir), otel_turu, yayin_durumu, onay_durumu, FORMAT(ortalama_puan, '0.0', 'tr-TR') FROM oteller ORDER BY id DESC;",
            "reservations" => @"SELECT TOP (12) rezervasyon_no, misafir_ad_soyad, FORMAT(giris_tarihi, 'dd.MM.yyyy', 'tr-TR'), FORMAT(cikis_tarihi, 'dd.MM.yyyy', 'tr-TR'), durum, FORMAT(toplam_tutar, 'N0', 'tr-TR') FROM rezervasyonlar ORDER BY id DESC;",
            "payments" => @"SELECT TOP (12) islem_no, odeme_turu, odeme_durumu, odeme_yontemi, FORMAT(toplam_tahsilat, 'N0', 'tr-TR'), FORMAT(odeme_baslangic_tarihi, 'dd.MM.yyyy HH:mm', 'tr-TR') FROM odeme_islemleri ORDER BY id DESC;",
            "invoices" => @"SELECT TOP (12) fatura_no, FORMAT(fatura_tarihi, 'dd.MM.yyyy', 'tr-TR'), fatura_turu, fatura_durumu, FORMAT(genel_toplam, 'N0', 'tr-TR'), para_birimi FROM faturalar ORDER BY id DESC;",
            "commissions" => @"SELECT TOP (12) kayit_no, donem, o.otel_adi, FORMAT(komisyon_tutari, 'N0', 'tr-TR'), otele_odeme_durumu, mutabakat_durumu FROM komisyon_muhasebe_kayitlari k LEFT JOIN oteller o ON o.id = k.otel_id ORDER BY k.id DESC;",
            "partner-applications" => @"SELECT TOP (12) firma_unvani, yetkili_ad_soyad, yetkili_eposta, vergi_numarasi, onay_durumu, FORMAT(olusturulma_tarihi, 'dd.MM.yyyy', 'tr-TR') FROM partner_detaylari ORDER BY id DESC;",
            "company-applications" => @"SELECT TOP (20) f.firma_adi, COALESCE(f.onay_durumu, 'Beklemede'),
                                                (SELECT COUNT(*) FROM users u WHERE u.firma_id = f.id AND u.rol LIKE 'firma_%'),
                                                (SELECT COUNT(*) FROM rezervasyonlar r WHERE r.firma_id = f.id),
                                                FORMAT(f.olusturulma_tarihi, 'dd.MM.yyyy', 'tr-TR')
                                         FROM firmalar f
                                         ORDER BY f.id DESC;",
            "platform-officials" => @"SELECT TOP (20) u.ad_soyad, u.eposta, COALESCE(NULLIF(u.rol, ''), 'admin'),
                                               CASE WHEN COALESCE(u.hesap_durumu, 0) = 1 THEN 'Aktif' ELSE 'Pasif' END,
                                               COALESCE(FORMAT(u.son_giris_tarihi, 'dd.MM.yyyy HH:mm', 'tr-TR'), '-'),
                                               FORMAT(u.olusturulma_tarihi, 'dd.MM.yyyy', 'tr-TR')
                                        FROM users u
                                        WHERE u.rol IN ('admin', 'superadmin')
                                        ORDER BY COALESCE(u.son_giris_tarihi, u.olusturulma_tarihi) DESC;",
            "active-hotels" => @"SELECT TOP (20)
                                        o.otel_adi,
                                        CONCAT(o.ilce, ', ', o.sehir),
                                        FORMAT(COALESCE(o.ortalama_puan, 0), '0.0', 'tr-TR'),
                                        (SELECT COUNT(*) FROM rezervasyonlar r WHERE r.otel_id = o.id),
                                        FORMAT(COALESCE((SELECT SUM(COALESCE(r.toplam_tutar,0)) FROM rezervasyonlar r WHERE r.otel_id = o.id AND COALESCE(r.durum,'') <> 'İptal Edildi'),0), 'N0', 'tr-TR'),
                                        FORMAT(COALESCE(o.guncellenme_tarihi, o.olusturulma_tarihi), 'dd.MM.yyyy HH:mm', 'tr-TR')
                                     FROM oteller o
                                     WHERE o.yayin_durumu = 'Yayında' AND o.onay_durumu = 'Onaylandı'
                                     ORDER BY COALESCE(o.ortalama_puan, 0) DESC, o.id DESC;",
            "pending-hotels" => @"SELECT TOP (20)
                                         o.otel_adi,
                                         CONCAT(o.ilce, ', ', o.sehir),
                                         COALESCE(o.onay_durumu, '-'),
                                         COALESCE(o.yayin_durumu, '-'),
                                         FORMAT(o.olusturulma_tarihi, 'dd.MM.yyyy', 'tr-TR'),
                                         FORMAT(COALESCE(o.guncellenme_tarihi, o.olusturulma_tarihi), 'dd.MM.yyyy HH:mm', 'tr-TR')
                                  FROM oteller o
                                  WHERE COALESCE(o.onay_durumu, '') = 'Beklemede'
                                     OR COALESCE(o.yayin_durumu, '') <> 'Yayında'
                                  ORDER BY o.olusturulma_tarihi DESC;",
            "reviews" => @"SELECT TOP (12) COALESCE(yorum_basligi, 'Basliksiz'), genel_puan, onay_durumu, rapor_sayisi, dogrulanmis_konaklama, FORMAT(olusturulma_tarihi, 'dd.MM.yyyy', 'tr-TR') FROM yorumlar ORDER BY id DESC;",
            "campaigns" => @"SELECT TOP (12) kampanya_adi, tur, FORMAT(baslangic_tarihi, 'dd.MM.yyyy', 'tr-TR'), FORMAT(bitis_tarihi, 'dd.MM.yyyy', 'tr-TR'), aktif_mi, kullanilan_adet FROM kampanyalar ORDER BY id DESC;",
            "notifications" => @"SELECT TOP (12) baslik, bildirim_turu, onem_derecesi, okundu_mu, arsivlendi_mi, FORMAT(olusturulma_tarihi, 'dd.MM.yyyy HH:mm', 'tr-TR') FROM sistem_ici_bildirimler ORDER BY id DESC;",
            "logs" => @"SELECT TOP (6) hedef_tablo, islem_turu, ip_adresi, FORMAT(islem_tarihi, 'dd.MM.yyyy HH:mm', 'tr-TR'), 'Admin Islem', '' FROM admin_islem_loglari ORDER BY id DESC;",
            "email-templates" => @"SELECT TOP (12) sablon_adi, kategori, dil, aktif_mi, sistem_geneli_mi, konu_basligi FROM mesaj_sablonlari ORDER BY id DESC;",
            "faq" => @"SELECT TOP (20) k.kategori_adi, s.soru, s.one_cikan_mi, s.aktif_mi, FORMAT(s.olusturulma_tarihi, 'dd.MM.yyyy', 'tr-TR') FROM sss_sorulari s INNER JOIN sss_kategorileri k ON k.id = s.sss_kategori_id ORDER BY k.siralama, s.siralama, s.id;",
            _ => string.Empty
        };
    }

    private static int SafeInt(SqlDataReader reader, int ordinal)
    {
        return reader.IsDBNull(ordinal) ? 0 : Convert.ToInt32(reader.GetValue(ordinal));
    }

    private static bool SafeBool(SqlDataReader reader, int ordinal)
    {
        return !reader.IsDBNull(ordinal) && Convert.ToInt32(reader.GetValue(ordinal), CultureInfo.InvariantCulture) == 1;
    }

    private static decimal SafeDecimal(SqlDataReader reader, int ordinal)
    {
        return reader.IsDBNull(ordinal) ? 0m : Convert.ToDecimal(reader.GetValue(ordinal), CultureInfo.InvariantCulture);
    }

    private static string FormatScalar(object? value)
    {
        return value switch
        {
            null or DBNull => "0",
            decimal number => number.ToString("0.##"),
            double number => number.ToString("0.##"),
            float number => number.ToString("0.##"),
            _ => value?.ToString() ?? "0"
        };
    }

    private static string FormatRelative(DateTime? value)
    {
        if (!value.HasValue)
        {
            return "Zaman bilgisi yok";
        }

        var diff = DateTime.Now - value.Value;
        if (diff.TotalMinutes < 1) return "Az once";
        if (diff.TotalHours < 1) return $"{Math.Max(1, (int)diff.TotalMinutes)} dk once";
        if (diff.TotalDays < 1) return $"{Math.Max(1, (int)diff.TotalHours)} saat once";
        return $"{Math.Max(1, (int)diff.TotalDays)} gun once";
    }

    private static string MapStatusTone(string status)
    {
        return status switch
        {
            "Yayında" or "Onaylandı" => "success",
            "Bakımda" or "Beklemede" => "warning",
            "Kapatıldı" or "Reddedildi" => "danger",
            _ => "info"
        };
    }

    private static async Task<bool> TableExistsAsync(
        SqlConnection connection,
        string tableName,
        CancellationToken cancellationToken,
        SqlTransaction? transaction = null)
    {
        const string sql = """
            SELECT COUNT(*)
            FROM INFORMATION_SCHEMA.TABLES
            WHERE TABLE_SCHEMA = 'dbo'
              AND TABLE_NAME = @tableName;
            """;

        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("@tableName", tableName);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture) > 0;
    }
}

