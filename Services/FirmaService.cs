using System.Globalization;
using MySqlConnector;
using otelturizmnew.Models.Firma;
using otelturizmnew.Models.Paneller.Firma;
using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Services;

public class FirmaService : IFirmaService
{
    private readonly string _connectionString;

    public FirmaService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection tanimli degil.");
    }

    public async Task<FirmaLandingPageViewModel> GetLandingPageAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var model = new FirmaLandingPageViewModel
        {
            HeroDescription = "5 ve üzeri oda ihtiyacınızda, otellerin firmalar için özel belirlediği indirimli fiyatlardan yararlanın. Tek fatura, çalışan limitleri ve detaylı raporlarla seyahat operasyonunuzu kontrol edin."
        };

        const string summarySql = @"
            SELECT
                (SELECT COUNT(*) FROM firmalar WHERE aktif_mi = 1 AND onay_durumu = 'Onaylandı') AS active_companies,
                (SELECT COUNT(DISTINCT otel_id) FROM firma_ozel_fiyatlar WHERE aktif_mi = 1) AS contracted_hotels,
                (SELECT COALESCE(MAX(indirim_orani), 0) FROM firma_ozel_fiyatlar WHERE aktif_mi = 1) AS max_discount;";

        await using (var summaryCommand = new MySqlCommand(summarySql, connection))
        await using (var reader = await summaryCommand.ExecuteReaderAsync(cancellationToken))
        {
            if (await reader.ReadAsync(cancellationToken))
            {
                model.ActiveCompanyCount = SafeInt(reader, 0);
                model.ContractedHotelCount = SafeInt(reader, 1);
                model.MaxDiscountRate = SafeDecimal(reader, 2);
            }
        }

        model.HeroStats = new List<FirmaLandingStatViewModel>
        {
            new() { Value = model.ContractedHotelCount.ToString("N0", CultureInfo.GetCultureInfo("tr-TR")), Label = "Anlaşmalı Otel" },
            new() { Value = $"%{model.MaxDiscountRate:0}", Label = "İndirim Tavanı" },
            new() { Value = model.ActiveCompanyCount.ToString("N0", CultureInfo.GetCultureInfo("tr-TR")), Label = "Kayıtlı Firma" }
        };

        const string dealsSql = @"
            SELECT ot.id, ot.otel_adi, ot.sehir, od.standart_gecelik_fiyat, foz.ozel_fiyat, foz.indirim_orani, foz.minimum_oda_sayisi
            FROM firma_ozel_fiyatlar foz
            INNER JOIN oteller ot ON ot.id = foz.otel_id
            LEFT JOIN oda_tipleri od ON od.id = foz.oda_tip_id
            WHERE foz.aktif_mi = 1
            ORDER BY foz.indirim_orani DESC, foz.ozel_fiyat ASC
            LIMIT 6;";

        await using (var dealsCommand = new MySqlCommand(dealsSql, connection))
        await using (var reader = await dealsCommand.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                var standard = SafeDecimal(reader, 3);
                var corporate = SafeDecimal(reader, 4);
                model.FeaturedDeals.Add(new FirmaLandingDealViewModel
                {
                    HotelId = reader.GetInt64(0),
                    HotelName = reader.GetString(1),
                    City = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                    StandardPriceText = FormatMoney(standard),
                    CorporatePriceText = FormatMoney(corporate),
                    DiscountRateText = $"%{SafeDecimal(reader, 5):0}",
                    MinRoomText = $"{SafeInt(reader, 6)} oda",
                    SavingsText = FormatMoney(Math.Max(0, standard - corporate))
                });
            }
        }

        model.PricingTiers = new List<FirmaLandingTierViewModel>
        {
            new() { Name = "Giriş Seviyesi", Range = "Minimum 3-5 Oda", DiscountText = "%10 - %15", ExampleText = "Kısa süreli ekip konaklamaları için" },
            new() { Name = "Standart Firma", Range = "Minimum 5-10 Oda", DiscountText = "%15 - %25", ExampleText = "En sık kullanılan kurumsal indirim yapısı", Highlighted = true },
            new() { Name = "Premium Hacim", Range = "Minimum 10+ Oda", DiscountText = "%25 - %35", ExampleText = "Toplu ekip, etkinlik ve saha operasyonları" }
        };

        model.Benefits = new List<FirmaLandingBenefitViewModel>
        {
            new() { Title = "Firma bazlı özel fiyatlar", Description = "Her otelin firmalara özel minimum oda ve indirim tanımı canlı veritabanından okunur.", IconClass = "fa-tags" },
            new() { Title = "Çalışan limit yönetimi", Description = "Departman ve kullanıcı bazlı harcama limitlerini firma panelinden kontrol edin.", IconClass = "fa-users-gear" },
            new() { Title = "Tek merkezden fatura takibi", Description = "Kurumsal faturaları ve rezervasyon finans hareketlerini tek panelde görün.", IconClass = "fa-file-invoice" },
            new() { Title = "Harcama ve otel bazlı rapor", Description = "Aylık toplam spend ve otel performansını karşılaştırmalı izleyin.", IconClass = "fa-chart-column" }
        };

        return model;
    }

    public async Task<FirmaDashboardPageViewModel> GetDashboardAsync(long userId, CancellationToken cancellationToken = default)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        var context = await BuildContextAsync(connection, userId, "Dashboard", "Kurumsal fiyatları, çalışanları ve seyahat bütçesini tek panelden yönetin.", "dashboard", cancellationToken);

        var model = new FirmaDashboardPageViewModel { Shell = context.Shell };

        const string statsSql = @"
            SELECT
                (SELECT COUNT(*) FROM users u WHERE u.firma_id = @firmaId AND u.rol LIKE 'firma_%' AND u.hesap_durumu = 1) AS employee_count,
                (SELECT COUNT(*) FROM firma_ozel_fiyatlar f WHERE f.firma_id = @firmaId AND f.aktif_mi = 1) AS deal_count,
                (SELECT COUNT(*) FROM rezervasyonlar r WHERE r.firma_id = @firmaId) AS reservation_count,
                (SELECT COUNT(*) FROM rezervasyonlar r WHERE r.firma_id = @firmaId AND r.firma_onay_durumu = 'Beklemede') AS pending_approval_count;";

        await using (var command = new MySqlCommand(statsSql, connection))
        {
            command.Parameters.AddWithValue("@firmaId", context.FirmaId);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                model.SummaryCards.Add(new FirmaPanelStatCardViewModel { Label = "Çalışan", Value = SafeInt(reader, 0).ToString(), Description = "Aktif firma kullanıcıları", IconClass = "fa-users", ToneClass = "info" });
                model.SummaryCards.Add(new FirmaPanelStatCardViewModel { Label = "Özel Fiyat", Value = SafeInt(reader, 1).ToString(), Description = "Kurumsal anlaşmalı fiyat kartı", IconClass = "fa-tags", ToneClass = "success" });
                model.SummaryCards.Add(new FirmaPanelStatCardViewModel { Label = "Rezervasyon", Value = SafeInt(reader, 2).ToString(), Description = "Firma adına açılmış kayıtlar", IconClass = "fa-calendar-check", ToneClass = "warning" });
                model.SummaryCards.Add(new FirmaPanelStatCardViewModel { Label = "Onay Bekleyen", Value = SafeInt(reader, 3).ToString(), Description = "Firma onay akışında bekleyen işlem", IconClass = "fa-hourglass-half", ToneClass = "danger" });
            }
        }

        model.HighlightDeals = await LoadDealsAsync(connection, context.FirmaId, 4, null, null, null, cancellationToken);
        model.FeaturedEmployees = await LoadEmployeesAsync(connection, context.FirmaId, 4, cancellationToken);
        model.RecentReservations = await LoadReservationsAsync(connection, context.FirmaId, 6, cancellationToken);
        return model;
    }

    public async Task<FirmaDealsPageViewModel> GetDealsAsync(long userId, string? city = null, int? minRoomCount = null, string? search = null, CancellationToken cancellationToken = default)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        var context = await BuildContextAsync(connection, userId, "Firma Fiyatları", "Otellerin firmanız için tanımladığı özel kurumsal fiyatları canlı takip edin.", "deals", cancellationToken);
        return new FirmaDealsPageViewModel
        {
            Shell = context.Shell,
            Deals = await LoadDealsAsync(connection, context.FirmaId, 100, city, minRoomCount, search, cancellationToken),
            Filter = new FirmaDealsFilterModel { City = city, MinRoomCount = minRoomCount, Search = search },
            AvailableCities = await LoadDealCitiesAsync(connection, context.FirmaId, cancellationToken)
        };
    }

    public async Task<FirmaReservationsPageViewModel> GetReservationsAsync(long userId, CancellationToken cancellationToken = default)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        var context = await BuildContextAsync(connection, userId, "Rezervasyonlar", "Firma adına oluşturulan tüm konaklama kayıtlarını görün.", "reservations", cancellationToken);
        return new FirmaReservationsPageViewModel { Shell = context.Shell, Reservations = await LoadReservationsAsync(connection, context.FirmaId, 200, cancellationToken) };
    }

    public async Task<FirmaEmployeesPageViewModel> GetEmployeesAsync(long userId, CancellationToken cancellationToken = default)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        var context = await BuildContextAsync(connection, userId, "Çalışanlar", "Firma kullanıcılarını, departmanlarını ve harcama yetkilerini yönetin.", "employees", cancellationToken);
        var employees = await LoadEmployeesAsync(connection, context.FirmaId, 200, cancellationToken);
        return new FirmaEmployeesPageViewModel
        {
            Shell = context.Shell,
            Employees = employees,
            TravelingEmployeeCount = employees.Count(static x => x.ReservationCountText != "0"),
            AverageLimitText = employees.Count == 0
                ? "₺0"
                : FormatMoney(ParseMoneyAverage(employees.Select(static x => x.LimitText)))
        };
    }

    public async Task<FirmaLimitsPageViewModel> GetLimitsAsync(long userId, CancellationToken cancellationToken = default)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        var context = await BuildContextAsync(connection, userId, "Limitler & Onaylar", "Departman ve çalışan bazlı harcama limitleri ile onay akışlarını yönetin.", "limits", cancellationToken);
        var model = new FirmaLimitsPageViewModel { Shell = context.Shell };

        const string sql = @"
            SELECT id,
                   CASE WHEN kullanici_id IS NOT NULL THEN CONCAT('Kullanıcı · ', COALESCE((SELECT ad_soyad FROM users u WHERE u.id = fhl.kullanici_id LIMIT 1), 'Kayıt'))
                        WHEN departman IS NOT NULL THEN CONCAT('Departman · ', departman)
                        ELSE 'Firma Geneli' END AS scope_text,
                   gecelik_limit, rezervasyon_basi_limit, aylik_limit, onay_gereksinimi
            FROM firma_harcama_limitleri fhl
            WHERE firma_id = @firmaId AND aktif_mi = 1
            ORDER BY kullanici_id IS NULL DESC, departman ASC, id ASC;";

        await using (var command = new MySqlCommand(sql, connection))
        {
            command.Parameters.AddWithValue("@firmaId", context.FirmaId);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                model.Limits.Add(new FirmaPanelLimitRowViewModel
                {
                    LimitId = reader.GetInt64(0),
                    ScopeText = reader.GetString(1),
                    NightlyLimitText = reader.IsDBNull(2) ? "-" : FormatMoney(reader.GetDecimal(2)),
                    ReservationLimitText = reader.IsDBNull(3) ? "-" : FormatMoney(reader.GetDecimal(3)),
                    MonthlyLimitText = reader.IsDBNull(4) ? "-" : FormatMoney(reader.GetDecimal(4)),
                    ApprovalText = !reader.IsDBNull(5) && reader.GetBoolean(5) ? "Onay gerekli" : "Otomatik onay"
                });
            }
        }

        model.PendingApprovals = await LoadPendingApprovalsAsync(connection, context.FirmaId, cancellationToken);
        model.Employees = await LoadEmployeesAsync(connection, context.FirmaId, 200, cancellationToken);
        return model;
    }

    public async Task<FirmaInvoicesPageViewModel> GetInvoicesAsync(long userId, CancellationToken cancellationToken = default)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        var context = await BuildContextAsync(connection, userId, "Faturalar", "Kurumsal rezervasyonlara ait fatura kayıtlarını takip edin.", "invoices", cancellationToken);
        var model = new FirmaInvoicesPageViewModel { Shell = context.Shell };

        const string sql = @"
            SELECT fatura_no, fatura_tarihi, fatura_turu, fatura_alici_unvan, genel_toplam, fatura_durumu
            FROM faturalar
            WHERE firma_id = @firmaId
            ORDER BY fatura_tarihi DESC, id DESC
            LIMIT 100;";

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@firmaId", context.FirmaId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            model.Invoices.Add(new FirmaPanelInvoiceRowViewModel
            {
                InvoiceNo = reader.GetString(0),
                InvoiceDateText = reader.GetDateTime(1).ToString("dd.MM.yyyy", CultureInfo.GetCultureInfo("tr-TR")),
                InvoiceType = reader.GetString(2),
                RecipientName = reader.GetString(3),
                TotalText = FormatMoney(SafeDecimal(reader, 4)),
                StatusText = reader.IsDBNull(5) ? string.Empty : reader.GetString(5)
            });
        }

        return model;
    }

    public async Task<FirmaSpendingReportsPageViewModel> GetSpendingReportsAsync(long userId, CancellationToken cancellationToken = default)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        var context = await BuildContextAsync(connection, userId, "Harcama Raporları", "Aylık kurumsal spend ve rezervasyon adetlerini izleyin.", "spending", cancellationToken);
        var model = new FirmaSpendingReportsPageViewModel { Shell = context.Shell };

        const string sql = @"
            SELECT DATE_FORMAT(olusturulma_tarihi, '%b') AS ay, COALESCE(SUM(toplam_tutar), 0) AS toplam, COUNT(*) AS rezervasyon_sayisi
            FROM rezervasyonlar
            WHERE firma_id = @firmaId
              AND olusturulma_tarihi >= DATE_SUB(CURDATE(), INTERVAL 5 MONTH)
            GROUP BY YEAR(olusturulma_tarihi), MONTH(olusturulma_tarihi), DATE_FORMAT(olusturulma_tarihi, '%b')
            ORDER BY YEAR(olusturulma_tarihi), MONTH(olusturulma_tarihi);";

        await using (var command = new MySqlCommand(sql, connection))
        {
            command.Parameters.AddWithValue("@firmaId", context.FirmaId);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                model.MonthlySpend.Add(new FirmaMonthlySpendRowViewModel
                {
                    Label = reader.GetString(0),
                    Amount = SafeDecimal(reader, 1),
                    ReservationCount = SafeInt(reader, 2)
                });
            }
        }

        var maxAmount = Math.Max(1m, model.MonthlySpend.Count == 0 ? 0m : model.MonthlySpend.Max(x => x.Amount));
        foreach (var item in model.MonthlySpend)
        {
            item.HeightPercent = Math.Max(16, (int)Math.Round(item.Amount * 100m / maxAmount));
        }

        model.TotalSpendText = FormatMoney(model.MonthlySpend.Sum(x => x.Amount));
        return model;
    }

    public async Task<FirmaHotelReportsPageViewModel> GetHotelReportsAsync(long userId, CancellationToken cancellationToken = default)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        var context = await BuildContextAsync(connection, userId, "Otel Bazlı Rapor", "Hangi otelde ne kadar rezervasyon ve tasarruf oluştuğunu görün.", "hotels", cancellationToken);
        var model = new FirmaHotelReportsPageViewModel { Shell = context.Shell };

        const string sql = @"
            SELECT o.otel_adi, CONCAT(o.ilce, ', ', o.sehir) AS city_text, COUNT(*) AS rezervasyon_sayisi,
                   COALESCE(SUM(r.toplam_tutar), 0) AS toplam, COALESCE(SUM(r.toplam_tasarruf), 0) AS tasarruf
            FROM rezervasyonlar r
            INNER JOIN oteller o ON o.id = r.otel_id
            WHERE r.firma_id = @firmaId
            GROUP BY o.id, o.otel_adi, o.ilce, o.sehir
            ORDER BY toplam DESC, rezervasyon_sayisi DESC;";

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@firmaId", context.FirmaId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            model.HotelReports.Add(new FirmaHotelReportRowViewModel
            {
                HotelName = reader.GetString(0),
                CityText = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                ReservationCount = SafeInt(reader, 2),
                GrossAmountText = FormatMoney(SafeDecimal(reader, 3)),
                SavingsText = FormatMoney(SafeDecimal(reader, 4))
            });
        }

        return model;
    }

    public async Task<(bool Success, string Message)> CreateEmployeeAsync(long userId, FirmaEmployeeCreateModel model, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(model.FullName) || string.IsNullOrWhiteSpace(model.Email))
        {
            return (false, "Çalışan adı ve e-posta zorunludur.");
        }

        var normalizedEmail = model.Email.Trim().ToLowerInvariant();
        var role = NormalizeFirmaRole(model.Role);

        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        var context = await BuildContextAsync(connection, userId, string.Empty, string.Empty, "employees", cancellationToken);

        const string existsSql = "SELECT COUNT(*) FROM users WHERE eposta = @email;";
        await using (var existsCommand = new MySqlCommand(existsSql, connection))
        {
            existsCommand.Parameters.AddWithValue("@email", normalizedEmail);
            var existingCount = Convert.ToInt32(await existsCommand.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture);
            if (existingCount > 0)
            {
                return (false, "Bu e-posta adresi zaten kullanılıyor.");
            }
        }

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            const string insertUserSql = @"
                INSERT INTO users
                (
                    ad_soyad, eposta, telefon, sifre, rol, firma_id, departman, gorev_unvani,
                    harcama_limiti, onay_gereksinimi, personel_kodu, firma_yonetici_mi,
                    hesap_durumu, dil_tercihi, para_birimi, ulke, olusturulma_tarihi
                )
                VALUES
                (
                    @fullName, @email, @phone, SHA2('1585', 256), @role, @firmaId, @department, @title,
                    @nightlyLimit, @approvalRequired, @personelCode, @isManager,
                    1, 'tr', 'TRY', 'Türkiye', NOW()
                );
                SELECT LAST_INSERT_ID();";

            long createdUserId;
            await using (var insertUserCommand = new MySqlCommand(insertUserSql, connection, transaction))
            {
                insertUserCommand.Parameters.AddWithValue("@fullName", model.FullName.Trim());
                insertUserCommand.Parameters.AddWithValue("@email", normalizedEmail);
                insertUserCommand.Parameters.AddWithValue("@phone", string.IsNullOrWhiteSpace(model.Phone) ? DBNull.Value : (object)model.Phone.Trim());
                insertUserCommand.Parameters.AddWithValue("@role", role);
                insertUserCommand.Parameters.AddWithValue("@firmaId", context.FirmaId);
                insertUserCommand.Parameters.AddWithValue("@department", string.IsNullOrWhiteSpace(model.Department) ? DBNull.Value : (object)model.Department.Trim());
                insertUserCommand.Parameters.AddWithValue("@title", string.IsNullOrWhiteSpace(model.Title) ? DBNull.Value : (object)model.Title.Trim());
                insertUserCommand.Parameters.AddWithValue("@nightlyLimit", model.NightlyLimit.HasValue ? (object)model.NightlyLimit.Value : DBNull.Value);
                insertUserCommand.Parameters.AddWithValue("@approvalRequired", model.ApprovalRequired ? 1 : 0);
                insertUserCommand.Parameters.AddWithValue("@personelCode", BuildPersonelCode(context.FirmaId));
                insertUserCommand.Parameters.AddWithValue("@isManager", role is "firma_admin" or "firma_manager" ? 1 : 0);
                createdUserId = Convert.ToInt64(await insertUserCommand.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture);
            }

            if (model.NightlyLimit.HasValue || model.ApprovalRequired)
            {
                const string limitSql = @"
                    INSERT INTO firma_harcama_limitleri
                    (
                        firma_id, kullanici_id, departman, gecelik_limit, onay_gereksinimi, aktif_mi, olusturulma_tarihi
                    )
                    VALUES
                    (
                        @firmaId, @userId, @department, @nightlyLimit, @approvalRequired, 1, NOW()
                    );";

                await using var limitCommand = new MySqlCommand(limitSql, connection, transaction);
                limitCommand.Parameters.AddWithValue("@firmaId", context.FirmaId);
                limitCommand.Parameters.AddWithValue("@userId", createdUserId);
                limitCommand.Parameters.AddWithValue("@department", string.IsNullOrWhiteSpace(model.Department) ? DBNull.Value : (object)model.Department.Trim());
                limitCommand.Parameters.AddWithValue("@nightlyLimit", model.NightlyLimit.HasValue ? (object)model.NightlyLimit.Value : DBNull.Value);
                limitCommand.Parameters.AddWithValue("@approvalRequired", model.ApprovalRequired ? 1 : 0);
                await limitCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            return (true, $"Çalışan oluşturuldu. İlk giriş şifresi: 1585 · E-posta: {normalizedEmail}");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            return (false, $"Çalışan ekleme sırasında hata oluştu: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Message)> UpsertLimitAsync(long userId, FirmaLimitUpsertModel model, CancellationToken cancellationToken = default)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        var context = await BuildContextAsync(connection, userId, string.Empty, string.Empty, "limits", cancellationToken);

        if (model.UserId is null && string.IsNullOrWhiteSpace(model.Department) && model.MonthlyLimit is null && model.NightlyLimit is null && model.ReservationLimit is null)
        {
            return (false, "Limit kaydı için en az bir kapsam veya tutar alanı girilmelidir.");
        }

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            if (model.UserId.HasValue)
            {
                const string validateUserSql = "SELECT COUNT(*) FROM users WHERE id = @userId AND firma_id = @firmaId;";
                await using var validateCommand = new MySqlCommand(validateUserSql, connection, transaction);
                validateCommand.Parameters.AddWithValue("@userId", model.UserId.Value);
                validateCommand.Parameters.AddWithValue("@firmaId", context.FirmaId);
                var exists = Convert.ToInt32(await validateCommand.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture) > 0;
                if (!exists)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return (false, "Seçilen çalışan bu firmaya bağlı değil.");
                }
            }

            const string findSql = @"
                SELECT id
                FROM firma_harcama_limitleri
                WHERE firma_id = @firmaId
                  AND aktif_mi = 1
                  AND ((@userId IS NOT NULL AND kullanici_id = @userId)
                    OR (@userId IS NULL AND @department IS NOT NULL AND departman = @department)
                    OR (@userId IS NULL AND @department IS NULL AND kullanici_id IS NULL AND departman IS NULL))
                ORDER BY id DESC
                LIMIT 1;";

            long? existingId = null;
            await using (var findCommand = new MySqlCommand(findSql, connection, transaction))
            {
                findCommand.Parameters.AddWithValue("@firmaId", context.FirmaId);
                findCommand.Parameters.AddWithValue("@userId", model.UserId.HasValue ? (object)model.UserId.Value : DBNull.Value);
                findCommand.Parameters.AddWithValue("@department", string.IsNullOrWhiteSpace(model.Department) ? DBNull.Value : (object)model.Department.Trim());
                var result = await findCommand.ExecuteScalarAsync(cancellationToken);
                if (result is not null)
                {
                    existingId = Convert.ToInt64(result, CultureInfo.InvariantCulture);
                }
            }

            if (existingId.HasValue)
            {
                const string updateSql = @"
                    UPDATE firma_harcama_limitleri
                    SET departman = @department,
                        kullanici_id = @userId,
                        gecelik_limit = @nightlyLimit,
                        rezervasyon_basi_limit = @reservationLimit,
                        aylik_limit = @monthlyLimit,
                        onay_gereksinimi = @approvalRequired,
                        aktif_mi = 1
                    WHERE id = @id;";

                await using var updateCommand = new MySqlCommand(updateSql, connection, transaction);
                updateCommand.Parameters.AddWithValue("@id", existingId.Value);
                updateCommand.Parameters.AddWithValue("@department", string.IsNullOrWhiteSpace(model.Department) ? DBNull.Value : (object)model.Department.Trim());
                updateCommand.Parameters.AddWithValue("@userId", model.UserId.HasValue ? (object)model.UserId.Value : DBNull.Value);
                updateCommand.Parameters.AddWithValue("@nightlyLimit", model.NightlyLimit.HasValue ? (object)model.NightlyLimit.Value : DBNull.Value);
                updateCommand.Parameters.AddWithValue("@reservationLimit", model.ReservationLimit.HasValue ? (object)model.ReservationLimit.Value : DBNull.Value);
                updateCommand.Parameters.AddWithValue("@monthlyLimit", model.MonthlyLimit.HasValue ? (object)model.MonthlyLimit.Value : DBNull.Value);
                updateCommand.Parameters.AddWithValue("@approvalRequired", model.ApprovalRequired ? 1 : 0);
                await updateCommand.ExecuteNonQueryAsync(cancellationToken);
            }
            else
            {
                const string insertSql = @"
                    INSERT INTO firma_harcama_limitleri
                    (
                        firma_id, departman, kullanici_id, gecelik_limit, rezervasyon_basi_limit, aylik_limit,
                        onay_gereksinimi, aktif_mi, olusturulma_tarihi
                    )
                    VALUES
                    (
                        @firmaId, @department, @userId, @nightlyLimit, @reservationLimit, @monthlyLimit,
                        @approvalRequired, 1, NOW()
                    );";

                await using var insertCommand = new MySqlCommand(insertSql, connection, transaction);
                insertCommand.Parameters.AddWithValue("@firmaId", context.FirmaId);
                insertCommand.Parameters.AddWithValue("@department", string.IsNullOrWhiteSpace(model.Department) ? DBNull.Value : (object)model.Department.Trim());
                insertCommand.Parameters.AddWithValue("@userId", model.UserId.HasValue ? (object)model.UserId.Value : DBNull.Value);
                insertCommand.Parameters.AddWithValue("@nightlyLimit", model.NightlyLimit.HasValue ? (object)model.NightlyLimit.Value : DBNull.Value);
                insertCommand.Parameters.AddWithValue("@reservationLimit", model.ReservationLimit.HasValue ? (object)model.ReservationLimit.Value : DBNull.Value);
                insertCommand.Parameters.AddWithValue("@monthlyLimit", model.MonthlyLimit.HasValue ? (object)model.MonthlyLimit.Value : DBNull.Value);
                insertCommand.Parameters.AddWithValue("@approvalRequired", model.ApprovalRequired ? 1 : 0);
                await insertCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            if (model.UserId.HasValue)
            {
                const string userUpdateSql = @"
                    UPDATE users
                    SET harcama_limiti = @nightlyLimit,
                        onay_gereksinimi = @approvalRequired
                    WHERE id = @userId AND firma_id = @firmaId;";
                await using var userUpdateCommand = new MySqlCommand(userUpdateSql, connection, transaction);
                userUpdateCommand.Parameters.AddWithValue("@nightlyLimit", model.NightlyLimit.HasValue ? (object)model.NightlyLimit.Value : DBNull.Value);
                userUpdateCommand.Parameters.AddWithValue("@approvalRequired", model.ApprovalRequired ? 1 : 0);
                userUpdateCommand.Parameters.AddWithValue("@userId", model.UserId.Value);
                userUpdateCommand.Parameters.AddWithValue("@firmaId", context.FirmaId);
                await userUpdateCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            return (true, "Limit ve onay ayarı kaydedildi.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            return (false, $"Limit kaydı sırasında hata oluştu: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Message)> UpdateReservationApprovalAsync(long userId, FirmaReservationDecisionModel model, CancellationToken cancellationToken = default)
    {
        if (model.ReservationId <= 0)
        {
            return (false, "Geçerli bir rezervasyon seçilmedi.");
        }

        var isApprove = string.Equals(model.Decision, "approve", StringComparison.OrdinalIgnoreCase);
        var isReject = string.Equals(model.Decision, "reject", StringComparison.OrdinalIgnoreCase);
        if (!isApprove && !isReject)
        {
            return (false, "Geçerli bir onay kararı seçilmedi.");
        }

        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        var context = await BuildContextAsync(connection, userId, string.Empty, string.Empty, "limits", cancellationToken);

        const string updateSql = @"
            UPDATE rezervasyonlar
            SET firma_onay_durumu = @approvalStatus,
                firma_onaylayan_kullanici_id = @userId,
                firma_onay_tarihi = NOW(),
                durum = @reservationStatus,
                iptal_nedeni = @cancelReason
            WHERE id = @reservationId
              AND firma_id = @firmaId
              AND firma_onay_durumu = 'Beklemede';";

        await using var command = new MySqlCommand(updateSql, connection);
        command.Parameters.AddWithValue("@approvalStatus", isApprove ? "Onaylandı" : "Reddedildi");
        command.Parameters.AddWithValue("@reservationStatus", isApprove ? "Onaylandı" : "İptal Edildi");
        command.Parameters.AddWithValue("@cancelReason", isReject ? (object)"Firma paneli üzerinden rezervasyon reddedildi." : DBNull.Value);
        command.Parameters.AddWithValue("@userId", userId);
        command.Parameters.AddWithValue("@reservationId", model.ReservationId);
        command.Parameters.AddWithValue("@firmaId", context.FirmaId);

        var affected = await command.ExecuteNonQueryAsync(cancellationToken);
        return affected > 0
            ? (true, isApprove ? "Rezervasyon firma tarafından onaylandı." : "Rezervasyon firma tarafından reddedildi.")
            : (false, "Rezervasyon bulunamadı veya daha önce işlem görmüş.");
    }

    private async Task<FirmaContext> BuildContextAsync(MySqlConnection connection, long userId, string title, string subtitle, string activeSectionKey, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT f.id, f.firma_adi, COALESCE(f.onay_durumu, 'Beklemede') AS onay_durumu,
                   u.ad_soyad, u.eposta, u.rol,
                   (SELECT COUNT(*) FROM users fu WHERE fu.firma_id = f.id AND fu.rol LIKE 'firma_%' AND fu.hesap_durumu = 1) AS employee_count,
                   (SELECT COUNT(*) FROM firma_ozel_fiyatlar ff WHERE ff.firma_id = f.id AND ff.aktif_mi = 1) AS deal_count,
                   (SELECT COUNT(*) FROM rezervasyonlar r WHERE r.firma_id = f.id AND r.firma_onay_durumu = 'Beklemede') AS pending_count
            FROM users u
            INNER JOIN firmalar f ON f.id = u.firma_id
            WHERE u.id = @userId
            LIMIT 1;";

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@userId", userId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            throw new InvalidOperationException("Firma paneli için aktif firma bağlantısı bulunamadı.");
        }

        var shell = new FirmaPanelShellViewModel
        {
            UserId = userId,
            FirmaId = reader.GetInt64(0),
            CompanyName = reader.GetString(1),
            CompanyBadgeText = reader.IsDBNull(2) ? "Beklemede" : reader.GetString(2),
            FullName = reader.GetString(3),
            Email = reader.GetString(4),
            UserRole = reader.GetString(5),
            EmployeeCount = SafeInt(reader, 6),
            DealCount = SafeInt(reader, 7),
            PendingApprovalCount = SafeInt(reader, 8),
            PanelTitle = title,
            PanelSubtitle = subtitle,
            ActiveSectionKey = activeSectionKey
        };

        return new FirmaContext(shell.FirmaId, shell);
    }

    private static string NormalizeFirmaRole(string? role)
        => role?.Trim() switch
        {
            "firma_admin" => "firma_admin",
            "firma_manager" => "firma_manager",
            _ => "firma_staff"
        };

    private static string BuildPersonelCode(long firmaId)
        => $"FRM{firmaId:D3}-{DateTime.UtcNow:HHmmss}";

    private static string GetRoleLabel(string role)
        => role switch
        {
            "firma_admin" => "Firma Yöneticisi",
            "firma_manager" => "Departman Yöneticisi",
            "firma_staff" => "Firma Personeli",
            _ => role
        };

    private static async Task<List<string>> LoadDealCitiesAsync(MySqlConnection connection, long firmaId, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT DISTINCT ot.sehir
            FROM firma_ozel_fiyatlar foz
            INNER JOIN oteller ot ON ot.id = foz.otel_id
            WHERE foz.firma_id = @firmaId
              AND foz.aktif_mi = 1
              AND ot.sehir IS NOT NULL
              AND ot.sehir <> ''
            ORDER BY ot.sehir;";

        var cities = new List<string>();
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@firmaId", firmaId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            cities.Add(reader.GetString(0));
        }

        return cities;
    }

    private async Task<List<FirmaPanelDealRowViewModel>> LoadDealsAsync(MySqlConnection connection, long firmaId, int take, string? city = null, int? minRoomCount = null, string? search = null, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT foz.id, ot.otel_adi, od.oda_adi, CONCAT(ot.ilce, ', ', ot.sehir) AS city_text,
                   od.standart_gecelik_fiyat, foz.ozel_fiyat, foz.indirim_orani, foz.minimum_oda_sayisi, ot.id,
                   CONCAT(DATE_FORMAT(foz.gecerlilik_baslangic, '%d.%m.%Y'), ' - ', DATE_FORMAT(foz.gecerlilik_bitis, '%d.%m.%Y')) AS validity_text
            FROM firma_ozel_fiyatlar foz
            INNER JOIN oteller ot ON ot.id = foz.otel_id
            LEFT JOIN oda_tipleri od ON od.id = foz.oda_tip_id
            WHERE foz.firma_id = @firmaId AND foz.aktif_mi = 1
              AND (@city IS NULL OR ot.sehir = @city)
              AND (@minRoomCount IS NULL OR foz.minimum_oda_sayisi >= @minRoomCount)
              AND (@search IS NULL OR ot.otel_adi LIKE CONCAT('%', @search, '%') OR ot.sehir LIKE CONCAT('%', @search, '%') OR ot.ilce LIKE CONCAT('%', @search, '%'))
            ORDER BY foz.indirim_orani DESC, foz.ozel_fiyat ASC
            LIMIT @take;";

        var items = new List<FirmaPanelDealRowViewModel>();
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@firmaId", firmaId);
        command.Parameters.AddWithValue("@take", take);
        command.Parameters.AddWithValue("@city", string.IsNullOrWhiteSpace(city) ? DBNull.Value : (object)city);
        command.Parameters.AddWithValue("@minRoomCount", minRoomCount.HasValue ? (object)minRoomCount.Value : DBNull.Value);
        command.Parameters.AddWithValue("@search", string.IsNullOrWhiteSpace(search) ? DBNull.Value : (object)search.Trim());
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var standardPrice = SafeDecimal(reader, 4);
            var corporatePrice = SafeDecimal(reader, 5);
            items.Add(new FirmaPanelDealRowViewModel
            {
                DealId = reader.GetInt64(0),
                HotelId = reader.GetInt64(8),
                HotelName = reader.GetString(1),
                RoomName = reader.IsDBNull(2) ? "Tüm odalar" : reader.GetString(2),
                CityText = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                StandardPriceText = FormatMoney(standardPrice),
                CorporatePriceText = FormatMoney(corporatePrice),
                DiscountText = $"%{SafeDecimal(reader, 6):0}",
                MinimumRoomText = $"Min. {SafeInt(reader, 7)} oda",
                ValidityText = reader.IsDBNull(9) ? "Süresiz" : reader.GetString(9),
                SavingsText = FormatMoney(Math.Max(0m, standardPrice - corporatePrice))
            });
        }
        return items;
    }

    private async Task<List<FirmaPanelEmployeeRowViewModel>> LoadEmployeesAsync(MySqlConnection connection, long firmaId, int take, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT u.id, u.ad_soyad, COALESCE(u.departman, 'Tanımsız'), COALESCE(u.gorev_unvani, u.rol), u.eposta,
                   u.harcama_limiti, u.onay_gereksinimi, u.rol, u.firma_yonetici_mi,
                   COUNT(r.id) AS rezervasyon_sayisi, COALESCE(SUM(r.toplam_tutar), 0) AS harcama_toplami
            FROM users u
            LEFT JOIN rezervasyonlar r ON r.firma_calisan_id = u.id
            WHERE u.firma_id = @firmaId AND u.rol LIKE 'firma_%'
            GROUP BY u.id, u.ad_soyad, u.departman, u.gorev_unvani, u.eposta, u.harcama_limiti, u.onay_gereksinimi, u.rol, u.firma_yonetici_mi
            ORDER BY u.firma_yonetici_mi DESC, u.ad_soyad ASC
            LIMIT @take;";

        var items = new List<FirmaPanelEmployeeRowViewModel>();
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@firmaId", firmaId);
        command.Parameters.AddWithValue("@take", take);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var fullName = reader.GetString(1);
            items.Add(new FirmaPanelEmployeeRowViewModel
            {
                UserId = reader.GetInt64(0),
                FullName = fullName,
                Department = reader.GetString(2),
                Title = reader.GetString(3),
                Email = reader.GetString(4),
                LimitText = reader.IsDBNull(5) ? "-" : FormatMoney(reader.GetDecimal(5)),
                ApprovalText = !reader.IsDBNull(6) && reader.GetBoolean(6) ? "Onaylı akış" : "Serbest rezervasyon",
                Initials = GetInitials(fullName),
                RoleText = GetRoleLabel(reader.IsDBNull(7) ? string.Empty : reader.GetString(7)),
                IsManager = !reader.IsDBNull(8) && reader.GetBoolean(8),
                ReservationCountText = SafeInt(reader, 9).ToString(CultureInfo.InvariantCulture),
                SpendText = FormatMoney(SafeDecimal(reader, 10))
            });
        }
        return items;
    }

    private async Task<List<FirmaPanelReservationRowViewModel>> LoadReservationsAsync(MySqlConnection connection, long firmaId, int take, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT r.id, r.rezervasyon_no, COALESCE(u.ad_soyad, r.misafir_ad_soyad) AS employee_name, o.otel_adi,
                   CONCAT(o.ilce, ', ', o.sehir) AS city_text,
                   r.giris_tarihi, r.cikis_tarihi, r.durum, r.firma_onay_durumu, r.toplam_tutar
            FROM rezervasyonlar r
            INNER JOIN oteller o ON o.id = r.otel_id
            LEFT JOIN users u ON u.id = r.firma_calisan_id
            WHERE r.firma_id = @firmaId
            ORDER BY r.olusturulma_tarihi DESC, r.id DESC
            LIMIT @take;";

        var items = new List<FirmaPanelReservationRowViewModel>();
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@firmaId", firmaId);
        command.Parameters.AddWithValue("@take", take);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new FirmaPanelReservationRowViewModel
            {
                ReservationId = reader.GetInt64(0),
                ReservationNo = reader.GetString(1),
                EmployeeName = reader.GetString(2),
                HotelName = reader.GetString(3),
                HotelCityText = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                StayText = $"{reader.GetDateTime(5):dd.MM.yyyy} - {reader.GetDateTime(6):dd.MM.yyyy}",
                StatusText = reader.GetString(7),
                ApprovalText = reader.IsDBNull(8) ? "-" : reader.GetString(8),
                TotalText = FormatMoney(SafeDecimal(reader, 9)),
                CanApprove = string.Equals(reader.IsDBNull(8) ? null : reader.GetString(8), "Beklemede", StringComparison.OrdinalIgnoreCase)
            });
        }
        return items;
    }

    private async Task<List<FirmaPanelReservationRowViewModel>> LoadPendingApprovalsAsync(MySqlConnection connection, long firmaId, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT r.id, r.rezervasyon_no, COALESCE(u.ad_soyad, r.misafir_ad_soyad) AS employee_name, o.otel_adi,
                   CONCAT(o.ilce, ', ', o.sehir) AS city_text,
                   r.giris_tarihi, r.cikis_tarihi, r.durum, r.firma_onay_durumu, r.toplam_tutar
            FROM rezervasyonlar r
            INNER JOIN oteller o ON o.id = r.otel_id
            LEFT JOIN users u ON u.id = r.firma_calisan_id
            WHERE r.firma_id = @firmaId AND r.firma_onay_durumu = 'Beklemede'
            ORDER BY r.olusturulma_tarihi DESC, r.id DESC
            LIMIT 20;";

        var items = new List<FirmaPanelReservationRowViewModel>();
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@firmaId", firmaId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new FirmaPanelReservationRowViewModel
            {
                ReservationId = reader.GetInt64(0),
                ReservationNo = reader.GetString(1),
                EmployeeName = reader.GetString(2),
                HotelName = reader.GetString(3),
                HotelCityText = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                StayText = $"{reader.GetDateTime(5):dd.MM.yyyy} - {reader.GetDateTime(6):dd.MM.yyyy}",
                StatusText = reader.GetString(7),
                ApprovalText = reader.IsDBNull(8) ? "-" : reader.GetString(8),
                TotalText = FormatMoney(SafeDecimal(reader, 9)),
                CanApprove = true
            });
        }
        return items;
    }

    private static decimal ParseMoneyAverage(IEnumerable<string> moneyValues)
    {
        var values = moneyValues
            .Where(static value => !string.IsNullOrWhiteSpace(value) && value != "-")
            .Select(static value =>
            {
                var cleaned = value.Replace("₺", string.Empty, StringComparison.Ordinal)
                    .Replace(".", string.Empty, StringComparison.Ordinal)
                    .Replace(",", ".", StringComparison.Ordinal)
                    .Trim();
                return decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed) ? parsed : 0m;
            })
            .Where(static value => value > 0m)
            .ToList();

        return values.Count == 0 ? 0m : values.Average();
    }

    private static int SafeInt(MySqlDataReader reader, int ordinal)
        => reader.IsDBNull(ordinal) ? 0 : Convert.ToInt32(reader.GetValue(ordinal), CultureInfo.InvariantCulture);

    private static decimal SafeDecimal(MySqlDataReader reader, int ordinal)
        => reader.IsDBNull(ordinal) ? 0m : Convert.ToDecimal(reader.GetValue(ordinal), CultureInfo.InvariantCulture);

    private static string FormatMoney(decimal value)
        => string.Format(CultureInfo.GetCultureInfo("tr-TR"), "{0:C0}", value);

    private static string GetInitials(string fullName)
        => string.Concat(fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2).Select(static x => x[0])).ToUpperInvariant();

    private sealed record FirmaContext(long FirmaId, FirmaPanelShellViewModel Shell);
}
