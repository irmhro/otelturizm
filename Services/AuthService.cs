using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using SqlConnection = Microsoft.Data.SqlClient.SqlConnection;
using SqlCommand = Microsoft.Data.SqlClient.SqlCommand;
using SqlTransaction = Microsoft.Data.SqlClient.SqlTransaction;
using SqlException = Microsoft.Data.SqlClient.SqlException;
using otelturizmnew.Models.Email;
using otelturizmnew.Models.Giris;
using otelturizmnew.Models.Legal;
using otelturizmnew.Models.Register;
using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Services;

public class AuthService : IAuthService
{
    private const string UserLoginPath = "/kullanici-giris";
    private const string PartnerLoginPath = "/partner-giris";
    private const string FirmaLoginPath = "/firma-giris";
    private const string AdminLoginPath = "/admin-giris";
    private const string PasswordHashSql = "LOWER(CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', CONVERT(nvarchar(max), @password)), 2))";
    private const string LegacySha1HashSql = "LOWER(CONVERT(VARCHAR(40), HASHBYTES('SHA1', CONVERT(nvarchar(max), @password)), 2))";
    private const string LegacyMd5HashSql = "LOWER(CONVERT(VARCHAR(32), HASHBYTES('MD5', CONVERT(nvarchar(max), @password)), 2))";
    private const string CurrentUtcSql = "SYSUTCDATETIME()";
    private const int FailedLoginLockoutThreshold = 3;
    private const int FailedLoginLockoutMinutes = 10;

    private readonly string _connectionString;
    private readonly IEmailQueueService _emailQueueService;
    private readonly IContractContentService _contractContentService;
    private readonly string _publicBaseUrl;

    public AuthService(IConfiguration configuration, IEmailQueueService emailQueueService, IContractContentService contractContentService)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection tanimli degil.");
        _emailQueueService = emailQueueService;
        _contractContentService = contractContentService;
        _publicBaseUrl = (configuration["App:PublicBaseUrl"] ?? "https://localhost:7223").TrimEnd('/');
    }

    public async Task<UserSessionModel?> AuthenticateUserAsync(string identity, string password, CancellationToken cancellationToken = default)
    {
        var candidate = await FindAuthCandidateAsync(identity, false, cancellationToken);
        if (candidate is not null && candidate.LockoutEndUtc.HasValue && candidate.LockoutEndUtc.Value > DateTime.Now)
        {
            throw new AuthFlowException($"Bu hesap gecici olarak kilitlendi. Lutfen {candidate.LockoutEndUtc.Value:HH:mm} sonrasinda tekrar deneyin.");
        }

        var user = await GetUserAsync(identity, password, false, cancellationToken);
        if (user is null)
        {
            if (candidate is not null)
            {
                var lockoutEnd = await RegisterFailedLoginAttemptAsync(candidate.UserId, cancellationToken);
                if (lockoutEnd.HasValue && lockoutEnd.Value > DateTime.Now)
                {
                    throw new AuthFlowException(CreateLockoutTriggeredMessage());
                }
            }

            return null;
        }

        await UpgradePasswordHashIfNeededAsync(user.UserId, password, cancellationToken);
        await ResetFailedLoginAttemptAsync(user.UserId, cancellationToken);

        if (user.AccountType == "partner")
        {
            return user;
        }

        if (user.AccountType == "admin")
        {
            return user;
        }

        if (user.AccountType == "firma")
        {
            if (!await CanFirmaUserAccessAsync(user.UserId, cancellationToken))
            {
                return null;
            }

            return user;
        }

        if (user.AccountType == "sales")
        {
            if (!await IsEmailVerifiedAsync(user.UserId, cancellationToken))
            {
                throw new AuthFlowException(
                    "E-posta adresinizi onaylamadan giris yapamazsiniz. Lütfen gelen kutunuzu kontrol edin veya doğrulama kodunu yeniden isteyin.",
                    AuthFlowErrorCodes.EmailNotVerified,
                    user.Email);
            }

            return user;
        }

        if (!await IsEmailVerifiedAsync(user.UserId, cancellationToken))
        {
            throw new AuthFlowException(
                "E-posta adresinizi onaylamadan giris yapamazsiniz. Lütfen gelen kutunuzu kontrol edin veya doğrulama kodunu yeniden isteyin.",
                AuthFlowErrorCodes.EmailNotVerified,
                user.Email);
        }

        user.AccountType = "user";
        return user;
    }

    public async Task<UserSessionModel?> AuthenticatePartnerAsync(string identity, string password, CancellationToken cancellationToken = default)
    {
        var candidate = await FindAuthCandidateAsync(identity, true, cancellationToken);
        if (candidate is not null && candidate.LockoutEndUtc.HasValue && candidate.LockoutEndUtc.Value > DateTime.Now)
        {
            throw new AuthFlowException($"Bu hesap gecici olarak kilitlendi. Lutfen {candidate.LockoutEndUtc.Value:HH:mm} sonrasinda tekrar deneyin.");
        }

        var user = await GetUserAsync(identity, password, true, cancellationToken);
        if (user is null)
        {
            if (candidate is not null)
            {
                var lockoutEnd = await RegisterFailedLoginAttemptAsync(candidate.UserId, cancellationToken);
                if (lockoutEnd.HasValue && lockoutEnd.Value > DateTime.Now)
                {
                    throw new AuthFlowException(CreateLockoutTriggeredMessage());
                }
            }
            return null;
        }

        await UpgradePasswordHashIfNeededAsync(user.UserId, password, cancellationToken);
        await ResetFailedLoginAttemptAsync(user.UserId, cancellationToken);
        if (!await IsEmailVerifiedAsync(user.UserId, cancellationToken))
        {
            throw new AuthFlowException(
                "E-posta adresinizi onaylamadan giris yapamazsiniz. Lütfen gelen kutunuzu kontrol edin veya doğrulama kodunu yeniden isteyin.",
                AuthFlowErrorCodes.EmailNotVerified,
                user.Email);
        }

        user.AccountType = "partner";
        return user;
    }

    public async Task<UserSessionModel?> AuthenticateFirmaAsync(string identity, string password, CancellationToken cancellationToken = default)
    {
        var candidate = await FindAuthCandidateAsync(identity, false, cancellationToken);
        if (candidate is not null && candidate.LockoutEndUtc.HasValue && candidate.LockoutEndUtc.Value > DateTime.Now)
        {
            throw new AuthFlowException($"Bu hesap gecici olarak kilitlendi. Lutfen {candidate.LockoutEndUtc.Value:HH:mm} sonrasinda tekrar deneyin.");
        }

        var user = await GetUserAsync(identity, password, false, cancellationToken);
        if (user is null || !string.Equals(user.AccountType, "firma", StringComparison.OrdinalIgnoreCase))
        {
            if (candidate is not null)
            {
                var lockoutEnd = await RegisterFailedLoginAttemptAsync(candidate.UserId, cancellationToken);
                if (lockoutEnd.HasValue && lockoutEnd.Value > DateTime.Now)
                {
                    throw new AuthFlowException(CreateLockoutTriggeredMessage());
                }
            }
            return null;
        }

        await UpgradePasswordHashIfNeededAsync(user.UserId, password, cancellationToken);
        await ResetFailedLoginAttemptAsync(user.UserId, cancellationToken);
        if (!await IsEmailVerifiedAsync(user.UserId, cancellationToken))
        {
            throw new AuthFlowException(
                "E-posta adresinizi onaylamadan giris yapamazsiniz. Lütfen gelen kutunuzu kontrol edin veya doğrulama kodunu yeniden isteyin.",
                AuthFlowErrorCodes.EmailNotVerified,
                user.Email);
        }

        if (!await CanFirmaUserAccessAsync(user.UserId, cancellationToken))
        {
            return null;
        }

        user.AccountType = "firma";
        return user;
    }

    public async Task<(bool Success, string Message, UserSessionModel? User)> RegisterUserAsync(UserRegistrationModel model, CancellationToken cancellationToken = default)
    {
        var firstName = model.FirstName.Trim();
        var lastName = model.LastName.Trim();
        var email = model.Email.Trim().ToLowerInvariant();
        var phone = string.IsNullOrWhiteSpace(model.PhoneNumber) ? null : model.PhoneNumber.Trim();

        if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
        {
            return (false, "Ad ve soyad zorunludur.", null);
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            return (false, "E-posta zorunludur.", null);
        }

        if (!model.AcceptTerms || !model.AcceptKvkk)
        {
            return (false, "Kayit icin kullanim kosullari ve KVKK onayi zorunludur.", null);
        }

        if (!IsPasswordPolicyValid(model.Password))
        {
            return (false, "Sifre en az 6 karakter olmali ve en az 1 harf ile 1 rakam icermelidir.", null);
        }

        if (!string.Equals(model.Password, model.ConfirmPassword, StringComparison.Ordinal))
        {
            return (false, "Sifre tekrari eslesmiyor.", null);
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var userColumns = await GetUsersTableColumnsAsync(connection, cancellationToken);

        const string existsSql = """
            SELECT
                SUM(CASE WHEN eposta = @email THEN 1 ELSE 0 END) AS email_count,
                SUM(CASE WHEN @phone IS NOT NULL AND telefon = @phone THEN 1 ELSE 0 END) AS phone_count
            FROM users;
            """;

        await using (var existsCommand = new SqlCommand(existsSql, connection))
        {
            existsCommand.Parameters.AddWithValue("@email", email);
            existsCommand.Parameters.AddWithValue("@phone", (object?)phone ?? DBNull.Value);

            await using var reader = await existsCommand.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                var emailCount = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
                var phoneCount = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);

                if (emailCount > 0)
                {
                    return (false, "Bu e-posta adresi zaten kayitli.", null);
                }

                if (phoneCount > 0)
                {
                    return (false, "Bu telefon numarasi zaten kayitli.", null);
                }
            }
        }

        var insertColumns = new List<string>
        {
            "ad_soyad",
            "eposta",
            "telefon",
            "sifre",
            "hesap_durumu",
            "dil_tercihi",
            "para_birimi",
            "ulke",
            "olusturulma_tarihi"
        };

        var insertValues = new List<string>
        {
            "@fullName",
            "@email",
            "@phone",
            PasswordHashSql,
            "1",
            "'tr'",
            "'TRY'",
            "'Turkiye'",
            CurrentUtcSql
        };

        if (userColumns.Contains("kvkk_onay_tarihi"))
        {
            insertColumns.Add("kvkk_onay_tarihi");
            insertValues.Add(CurrentUtcSql);
        }

        if (userColumns.Contains("rol"))
        {
            insertColumns.Add("rol");
            insertValues.Add("'user'");
        }

        if (userColumns.Contains("pazarlama_izni"))
        {
            insertColumns.Add("pazarlama_izni");
            insertValues.Add("@marketing");
        }

        if (userColumns.Contains("kayit_kaynagi"))
        {
            insertColumns.Add("kayit_kaynagi");
            insertValues.Add("'web_user_register'");
        }

        var insertSql = $"""
            INSERT INTO users
            (
                {string.Join(",\n                ", insertColumns)}
            )
            VALUES
            (
                {string.Join(",\n                ", insertValues)}
            );

            SELECT CAST(SCOPE_IDENTITY() AS bigint);
            """;

        long newUserId;
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            await using var insertCommand = new SqlCommand(insertSql, connection, (SqlTransaction)transaction);
            insertCommand.Parameters.AddWithValue("@fullName", $"{firstName} {lastName}".Trim());
            insertCommand.Parameters.AddWithValue("@email", email);
            insertCommand.Parameters.AddWithValue("@phone", (object?)phone ?? DBNull.Value);
            insertCommand.Parameters.AddWithValue("@password", model.Password);
            insertCommand.Parameters.AddWithValue("@marketing", model.AcceptMarketing ? 1 : 0);

            var result = await insertCommand.ExecuteScalarAsync(cancellationToken);
            newUserId = Convert.ToInt64(result);

            await _contractContentService.RecordRegistrationAcceptancesAsync(
                connection,
                (SqlTransaction)transaction,
                new ContractAcceptanceRegistrationRequest
                {
                    UserId = newUserId,
                    Audience = "user",
                    Email = email,
                    IncludePrimaryAgreement = true,
                    IncludeKvkk = true,
                    Source = "web_user_register"
                },
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        }
        catch (SqlException ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            return (false, $"Kayit veritabani hatasi: {ex.Message}", null);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        var user = await GetUserByIdAsync(connection, newUserId, cancellationToken);
        if (user is null)
        {
            return (false, "Kayit olusturuldu ancak oturum bilgisi alinamadi.", null);
        }

        await CreateAndQueueEmailVerificationAsync(
            connection,
            null,
            newUserId,
            email,
            firstName,
            null,
            null,
            cancellationToken);

        return (true, "Kayit basariyla tamamlandi. Giris yapmadan once lütfen e-posta adresinizi onaylayin.", user);
    }

    public async Task<(bool Success, string Message, UserSessionModel? User)> RegisterPartnerAsync(PartnerRegistrationModel model, CancellationToken cancellationToken = default)
    {
        var hotelName = model.HotelName.Trim();
        var companyName = string.IsNullOrWhiteSpace(model.CompanyName) ? hotelName : model.CompanyName.Trim();
        var contactName = model.ContactName.Trim();
        var contactTitle = model.ContactTitle.Trim();
        var email = model.Email.Trim().ToLowerInvariant();
        var phone = model.PhoneNumber.Trim();
        var address = model.Address.Trim();
        var city = model.City.Trim();
        var district = model.District.Trim();
        var neighborhood = string.IsNullOrWhiteSpace(model.Neighborhood) ? null : model.Neighborhood.Trim();
        var taxOffice = model.TaxOffice.Trim();
        var taxNumber = model.TaxNumber.Trim();
        var contactTcNo = model.ContactTcNo.Trim();
        var bankName = model.BankName.Trim();
        var bankBranch = string.IsNullOrWhiteSpace(model.BankBranch) ? null : model.BankBranch.Trim();
        var iban = model.Iban.Trim().Replace(" ", string.Empty, StringComparison.Ordinal);
        var website = string.IsNullOrWhiteSpace(model.Website) ? null : model.Website.Trim();
        var companyType = NormalizeCompanyType(model.CompanyType);

        if (string.IsNullOrWhiteSpace(hotelName) || string.IsNullOrWhiteSpace(contactName))
        {
            return (false, "Tesis adi ve yetkili bilgileri zorunludur.", null);
        }

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(phone))
        {
            return (false, "E-posta ve telefon zorunludur.", null);
        }

        if (string.IsNullOrWhiteSpace(address) || string.IsNullOrWhiteSpace(city) || string.IsNullOrWhiteSpace(district))
        {
            return (false, "Adres, sehir ve ilce bilgileri zorunludur.", null);
        }

        if (!string.IsNullOrWhiteSpace(neighborhood) && !address.StartsWith(neighborhood, StringComparison.OrdinalIgnoreCase))
        {
            address = $"{neighborhood}, {address}";
        }

        if (string.IsNullOrWhiteSpace(companyType) || string.IsNullOrWhiteSpace(taxOffice) || string.IsNullOrWhiteSpace(taxNumber))
        {
            return (false, "Firma tipi, vergi dairesi ve vergi numarasi zorunludur.", null);
        }

        if (string.IsNullOrWhiteSpace(contactTcNo) || string.IsNullOrWhiteSpace(bankName) || string.IsNullOrWhiteSpace(iban))
        {
            return (false, "Yetkili TC, banka adi ve IBAN zorunludur.", null);
        }

        if (!model.AcceptAgreement || !model.AcceptKvkk || !model.DeclareAccurate)
        {
            return (false, "Partner kaydi icin sözleşme, KVKK ve beyan onaylari zorunludur.", null);
        }

        if (!IsPasswordPolicyValid(model.Password))
        {
            return (false, "Sifre en az 6 karakter olmali ve en az 1 harf ile 1 rakam icermelidir.", null);
        }

        if (!string.Equals(model.Password, model.ConfirmPassword, StringComparison.Ordinal))
        {
            return (false, "Sifre tekrari eslesmiyor.", null);
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var userColumns = await GetUsersTableColumnsAsync(connection, cancellationToken);
        var usersPartnerColumns = await GetColumnsAsync(connection, "users_partner", cancellationToken);

        const string existsSql = """
            SELECT
                (SELECT COUNT(*) FROM users WHERE eposta = @email) AS email_count,
                (SELECT COUNT(*) FROM users WHERE telefon = @phone) AS phone_count,
                (SELECT COUNT(*) FROM partner_detaylari WHERE vergi_numarasi = @taxNumber) AS tax_count,
                (SELECT COUNT(*) FROM partner_detaylari WHERE iban = @iban) AS iban_count;
            """;

        await using (var existsCommand = new SqlCommand(existsSql, connection))
        {
            existsCommand.Parameters.AddWithValue("@email", email);
            existsCommand.Parameters.AddWithValue("@phone", phone);
            existsCommand.Parameters.AddWithValue("@taxNumber", taxNumber);
            existsCommand.Parameters.AddWithValue("@iban", iban);

            await using var reader = await existsCommand.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                if (!reader.IsDBNull(0) && reader.GetInt32(0) > 0)
                {
                    return (false, "Bu e-posta ile kayitli partner hesabi bulunuyor.", null);
                }

                if (!reader.IsDBNull(1) && reader.GetInt32(1) > 0)
                {
                    return (false, "Bu telefon numarasi zaten kullanimda.", null);
                }

                if (!reader.IsDBNull(2) && reader.GetInt32(2) > 0)
                {
                    return (false, "Bu vergi numarasi ile daha once partner kaydi yapilmis.", null);
                }

                if (!reader.IsDBNull(3) && reader.GetInt32(3) > 0)
                {
                    return (false, "Bu IBAN ile daha once partner kaydi yapilmis.", null);
                }
            }
        }

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var insertColumns = new List<string>
            {
                "ad_soyad",
                "eposta",
                "telefon",
                "sifre",
                "hesap_durumu",
                "dil_tercihi",
                "para_birimi",
                "ulke",
                "olusturulma_tarihi"
            };

            var insertValues = new List<string>
            {
                "@fullName",
                "@email",
                "@phone",
                PasswordHashSql,
                "1",
                "'tr'",
                "'TRY'",
                "'Turkiye'",
                CurrentUtcSql
            };

            if (userColumns.Contains("kvkk_onay_tarihi"))
            {
                insertColumns.Add("kvkk_onay_tarihi");
                insertValues.Add(CurrentUtcSql);
            }

            if (userColumns.Contains("rol"))
            {
                insertColumns.Add("rol");
                insertValues.Add("'partner_owner'");
            }

            if (userColumns.Contains("pazarlama_izni"))
            {
                insertColumns.Add("pazarlama_izni");
                insertValues.Add("0");
            }

            if (userColumns.Contains("kayit_kaynagi"))
            {
                insertColumns.Add("kayit_kaynagi");
                insertValues.Add("'web_partner_register'");
            }

            var insertUserSql = $"""
                INSERT INTO users
                (
                    {string.Join(",\n                    ", insertColumns)}
                )
                VALUES
                (
                    {string.Join(",\n                    ", insertValues)}
                );

                SELECT CAST(SCOPE_IDENTITY() AS bigint);
                """;

            long userId;
            await using (var insertUserCommand = new SqlCommand(insertUserSql, connection, (SqlTransaction)transaction))
            {
                insertUserCommand.Parameters.AddWithValue("@fullName", contactName);
                insertUserCommand.Parameters.AddWithValue("@email", email);
                insertUserCommand.Parameters.AddWithValue("@phone", phone);
                insertUserCommand.Parameters.AddWithValue("@password", model.Password);

                var result = await insertUserCommand.ExecuteScalarAsync(cancellationToken);
                userId = Convert.ToInt64(result);
            }

            const string insertPartnerSql = """
                INSERT INTO partner_detaylari
                (
                    kullanici_id,
                    firma_unvani,
                    firma_turu,
                    vergi_dairesi,
                    vergi_numarasi,
                    fatura_adresi,
                    fatura_il,
                    fatura_ilce,
                    yetkili_ad_soyad,
                    yetkili_tc_no,
                    yetkili_telefon,
                    yetkili_eposta,
                    yetkili_gorev,
                    banka_adi,
                    banka_subesi,
                    iban,
                    hesap_sahibi_adi,
                    hesap_para_birimi,
                    onay_durumu,
                    onay_tarihi,
                    web_sitesi,
                    aciklama,
                    olusturulma_tarihi
                )
                VALUES
                (
                    @userId,
                    @companyName,
                    @companyType,
                    @taxOffice,
                    @taxNumber,
                    @address,
                    @city,
                    @district,
                    @contactName,
                    @contactTcNo,
                    @phone,
                    @email,
                    @contactTitle,
                    @bankName,
                    @bankBranch,
                    @iban,
                    @accountOwner,
                    'TRY',
                    'Beklemede',
                    NULL,
                    @website,
                    @description,
                    SYSUTCDATETIME()
                );

                SELECT CAST(SCOPE_IDENTITY() AS bigint);
                """;

            long partnerId;
            await using (var insertPartnerCommand = new SqlCommand(insertPartnerSql, connection, (SqlTransaction)transaction))
            {
                insertPartnerCommand.Parameters.AddWithValue("@userId", userId);
                insertPartnerCommand.Parameters.AddWithValue("@companyName", companyName);
                insertPartnerCommand.Parameters.AddWithValue("@companyType", companyType);
                insertPartnerCommand.Parameters.AddWithValue("@taxOffice", taxOffice);
                insertPartnerCommand.Parameters.AddWithValue("@taxNumber", taxNumber);
                insertPartnerCommand.Parameters.AddWithValue("@address", address);
                insertPartnerCommand.Parameters.AddWithValue("@city", city);
                insertPartnerCommand.Parameters.AddWithValue("@district", district);
                insertPartnerCommand.Parameters.AddWithValue("@contactName", contactName);
                insertPartnerCommand.Parameters.AddWithValue("@contactTcNo", contactTcNo);
                insertPartnerCommand.Parameters.AddWithValue("@phone", phone);
                insertPartnerCommand.Parameters.AddWithValue("@email", email);
                insertPartnerCommand.Parameters.AddWithValue("@contactTitle", string.IsNullOrWhiteSpace(contactTitle) ? DBNull.Value : contactTitle);
                insertPartnerCommand.Parameters.AddWithValue("@bankName", bankName);
                insertPartnerCommand.Parameters.AddWithValue("@bankBranch", (object?)bankBranch ?? DBNull.Value);
                insertPartnerCommand.Parameters.AddWithValue("@iban", iban);
                insertPartnerCommand.Parameters.AddWithValue("@accountOwner", contactName);
                insertPartnerCommand.Parameters.AddWithValue("@website", (object?)website ?? DBNull.Value);
                insertPartnerCommand.Parameters.AddWithValue("@description", $"{hotelName} tesisi icin web partner kaydi olusturuldu.");

                var result = await insertPartnerCommand.ExecuteScalarAsync(cancellationToken);
                partnerId = Convert.ToInt64(result);
            }

            var hotelCode = await GenerateHotelCodeAsync(connection, (SqlTransaction)transaction, city, cancellationToken);
            const string insertHotelSql = """
                INSERT INTO oteller
                (
                    otel_kodu, partner_id, user_id, otel_adi, otel_turu, ulke, sehir, ilce, mahalle, tam_adres,
                    telefon_1, eposta, web_sitesi, rezervasyon_telefonu, satis_kontak_adi, satis_kontak_telefonu, satis_kontak_eposta,
                    check_in_saati, check_out_saati, toplam_oda_sayisi, kisa_aciklama, uzun_aciklama,
                    varsayilan_komisyon_orani, odeme_vadesi, odeme_yontemi, fatura_kesim_turu,
                    yayin_durumu, onay_durumu, olusturulma_tarihi
                )
                VALUES
                (
                    @hotelCode, @partnerId, @userId, @hotelName, 'Otel', 'Türkiye', @city, @district, @neighborhood, @address,
                    @phone, @email, @website, @phone, @contactName, @phone, @email,
                    '14:00:00', '12:00:00', @roomCount, @shortDescription, @description,
                    15.00, 'Çıkış Günü', 'Havale/EFT', 'Otel Keser',
                    'Taslak', 'Beklemede', SYSUTCDATETIME()
                );

                SELECT CAST(SCOPE_IDENTITY() AS bigint);
                """;

            long hotelId;
            await using (var insertHotelCommand = new SqlCommand(insertHotelSql, connection, (SqlTransaction)transaction))
            {
                insertHotelCommand.Parameters.AddWithValue("@hotelCode", hotelCode);
                insertHotelCommand.Parameters.AddWithValue("@partnerId", partnerId);
                insertHotelCommand.Parameters.AddWithValue("@userId", userId);
                insertHotelCommand.Parameters.AddWithValue("@hotelName", hotelName);
                insertHotelCommand.Parameters.AddWithValue("@city", city);
                insertHotelCommand.Parameters.AddWithValue("@district", district);
                insertHotelCommand.Parameters.AddWithValue("@neighborhood", (object?)neighborhood ?? DBNull.Value);
                insertHotelCommand.Parameters.AddWithValue("@address", address);
                insertHotelCommand.Parameters.AddWithValue("@phone", phone);
                insertHotelCommand.Parameters.AddWithValue("@email", email);
                insertHotelCommand.Parameters.AddWithValue("@website", (object?)website ?? DBNull.Value);
                insertHotelCommand.Parameters.AddWithValue("@contactName", contactName);
                insertHotelCommand.Parameters.AddWithValue("@roomCount", Math.Max(1, model.RoomCount ?? 1));
                insertHotelCommand.Parameters.AddWithValue("@shortDescription", $"{hotelName} için partner onboarding kaydı oluşturuldu.");
                insertHotelCommand.Parameters.AddWithValue("@description", "Partner başvuru aşamasındaki taslak tesis kaydı. Admin onayı tamamlanana kadar yayına alınmaz.");

                var result = await insertHotelCommand.ExecuteScalarAsync(cancellationToken);
                hotelId = Convert.ToInt64(result);
            }

            if (usersPartnerColumns.Contains("user_id") && usersPartnerColumns.Contains("partner_id"))
            {
                var usersPartnerInsertColumns = new List<string> { "user_id", "partner_id" };
                var usersPartnerInsertValues = new List<string> { "@userId", "@partnerId" };

                if (usersPartnerColumns.Contains("rol"))
                {
                    usersPartnerInsertColumns.Add("rol");
                    usersPartnerInsertValues.Add("'owner'");
                }

                if (usersPartnerColumns.Contains("aktif_mi"))
                {
                    usersPartnerInsertColumns.Add("aktif_mi");
                    usersPartnerInsertValues.Add("1");
                }

                if (usersPartnerColumns.Contains("ana_hesap_mi"))
                {
                    usersPartnerInsertColumns.Add("ana_hesap_mi");
                    usersPartnerInsertValues.Add("1");
                }

                if (usersPartnerColumns.Contains("olusturulma_tarihi"))
                {
                    usersPartnerInsertColumns.Add("olusturulma_tarihi");
                    usersPartnerInsertValues.Add("SYSUTCDATETIME()");
                }

                var insertUserPartnerSql = $"""
                    INSERT INTO users_partner
                    (
                        {string.Join(",\n                        ", usersPartnerInsertColumns)}
                    )
                    VALUES
                    (
                        {string.Join(",\n                        ", usersPartnerInsertValues)}
                    );
                    """;

                await using var insertUserPartnerCommand = new SqlCommand(insertUserPartnerSql, connection, (SqlTransaction)transaction);
                insertUserPartnerCommand.Parameters.AddWithValue("@userId", userId);
                insertUserPartnerCommand.Parameters.AddWithValue("@partnerId", partnerId);
                await insertUserPartnerCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            const string insertOwnershipSql = """
                INSERT INTO otel_kullanici_sahiplikleri
                (
                    otel_id, user_id, partner_id, rol, ana_sorumlu_mu, aktif_mi, olusturulma_tarihi
                )
                VALUES
                (
                    @hotelId, @userId, @partnerId, 'owner', 1, 1, SYSUTCDATETIME()
                );
                """;

            await using (var insertOwnershipCommand = new SqlCommand(insertOwnershipSql, connection, (SqlTransaction)transaction))
            {
                insertOwnershipCommand.Parameters.AddWithValue("@hotelId", hotelId);
                insertOwnershipCommand.Parameters.AddWithValue("@userId", userId);
                insertOwnershipCommand.Parameters.AddWithValue("@partnerId", partnerId);
                await insertOwnershipCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            if (await TableExistsAsync(connection, "partner_basvuru_hareketleri", cancellationToken, (SqlTransaction?)transaction))
            {
                const string insertHistorySql = """
                    INSERT INTO partner_basvuru_hareketleri
                    (
                        partner_id, onceki_durum, yeni_durum, islem_tipi, aciklama, islem_yapan_kullanici_id, olusturulma_tarihi
                    )
                    VALUES
                    (
                        @partnerId, NULL, 'Beklemede', 'PartnerBasvurusuOlusturuldu',
                        'Partner başvurusu oluşturuldu. E-posta doğrulaması ve admin incelemesi bekleniyor.', @userId, SYSUTCDATETIME()
                    );
                    """;

                await using var historyCommand = new SqlCommand(insertHistorySql, connection, (SqlTransaction)transaction);
                historyCommand.Parameters.AddWithValue("@partnerId", partnerId);
                historyCommand.Parameters.AddWithValue("@userId", userId);
                await historyCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            if (userColumns.Contains("sahiplik_partner_id") || userColumns.Contains("rol"))
            {
                var updateAssignments = new List<string>();

                if (userColumns.Contains("sahiplik_partner_id"))
                {
                    updateAssignments.Add("sahiplik_partner_id = @partnerId");
                }

                if (userColumns.Contains("rol"))
                {
                    updateAssignments.Add("rol = 'partner_owner'");
                }

                var updateOwnershipSql = $"""
                    UPDATE users
                    SET {string.Join(", ", updateAssignments)}
                    WHERE id = @userId;
                    """;

                await using var updateOwnershipCommand = new SqlCommand(updateOwnershipSql, connection, (SqlTransaction)transaction);
                updateOwnershipCommand.Parameters.AddWithValue("@partnerId", partnerId);
                updateOwnershipCommand.Parameters.AddWithValue("@userId", userId);
                await updateOwnershipCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            await _contractContentService.RecordRegistrationAcceptancesAsync(
                connection,
                (SqlTransaction)transaction,
                new ContractAcceptanceRegistrationRequest
                {
                    UserId = userId,
                    PartnerId = partnerId,
                    Audience = "partner",
                    Email = email,
                    IncludePrimaryAgreement = true,
                    IncludeKvkk = true,
                    Source = "web_partner_register"
                },
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            await CreateAndQueueEmailVerificationAsync(
                connection,
                null,
                userId,
                email,
                FirstNameFromFullName(contactName),
                null,
                null,
                cancellationToken);

            var user = await GetUserByIdAsync(connection, userId, cancellationToken);
            return user is null
                ? (false, "Partner hesabi olusturuldu ancak oturum bilgisi hazirlanamadi.", null)
                : (true, "Partner kaydi tamamlandi. Giris yapmadan once lütfen e-posta adresinizi onaylayin.", user);
        }
        catch (SqlException ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            return (false, $"Partner kaydi veritabani hatasi: {ex.Message}", null);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<(bool Success, string Message, UserSessionModel? User)> RegisterFirmaAsync(FirmaRegistrationModel model, CancellationToken cancellationToken = default)
    {
        var companyName = model.CompanyName.Trim();
        var companyType = NormalizeCompanyType(model.CompanyType);
        var sector = string.IsNullOrWhiteSpace(model.Sector) ? "Genel" : model.Sector.Trim();
        var taxNumber = model.TaxNumber.Trim();
        var taxOffice = model.TaxOffice.Trim();
        var tradeRegistryNumber = string.IsNullOrWhiteSpace(model.TradeRegistryNumber) ? null : model.TradeRegistryNumber.Trim();
        var mersisNumber = string.IsNullOrWhiteSpace(model.MersisNumber) ? null : model.MersisNumber.Trim();
        var companyEmail = model.CompanyEmail.Trim().ToLowerInvariant();
        var companyPhone = model.CompanyPhone.Trim();
        var website = string.IsNullOrWhiteSpace(model.Website) ? null : model.Website.Trim();
        var contactName = model.ContactName.Trim();
        var contactTitle = model.ContactTitle.Trim();
        var contactEmail = model.ContactEmail.Trim().ToLowerInvariant();
        var contactPhone = model.ContactPhone.Trim();
        var city = model.City.Trim();
        var district = model.District.Trim();
        var neighborhood = string.IsNullOrWhiteSpace(model.Neighborhood) ? null : model.Neighborhood.Trim();
        var postalCode = string.IsNullOrWhiteSpace(model.PostalCode) ? null : model.PostalCode.Trim();
        var address = model.Address.Trim();
        var employeeCount = Math.Max(0, model.EmployeeCount ?? 0);
        var monthlyTravelBudget = model.MonthlyTravelBudget;

        if (string.IsNullOrWhiteSpace(companyName) || string.IsNullOrWhiteSpace(companyType))
        {
            return (false, "Firma adı ve firma türü zorunludur.", null);
        }

        if (string.IsNullOrWhiteSpace(taxNumber) || string.IsNullOrWhiteSpace(taxOffice))
        {
            return (false, "Vergi numarası ve vergi dairesi zorunludur.", null);
        }

        if (string.IsNullOrWhiteSpace(companyEmail) || string.IsNullOrWhiteSpace(companyPhone))
        {
            return (false, "Firma e-posta ve telefon bilgileri zorunludur.", null);
        }

        if (string.IsNullOrWhiteSpace(contactName) || string.IsNullOrWhiteSpace(contactTitle))
        {
            return (false, "Yetkili adı ve unvanı zorunludur.", null);
        }

        if (string.IsNullOrWhiteSpace(contactEmail) || string.IsNullOrWhiteSpace(contactPhone))
        {
            return (false, "Yetkili e-posta ve telefon bilgileri zorunludur.", null);
        }

        if (string.IsNullOrWhiteSpace(city) || string.IsNullOrWhiteSpace(district) || string.IsNullOrWhiteSpace(address))
        {
            return (false, "Şehir, ilçe ve açık adres zorunludur.", null);
        }

        if (!string.IsNullOrWhiteSpace(neighborhood) && !address.StartsWith(neighborhood, StringComparison.OrdinalIgnoreCase))
        {
            address = $"{neighborhood}, {address}";
        }

        if (!model.AcceptAgreement || !model.AcceptKvkk)
        {
            return (false, "Firma başvurusu için sözleşme ve KVKK onayı zorunludur.", null);
        }

        if (!IsPasswordPolicyValid(model.Password))
        {
            return (false, "Sifre en az 6 karakter olmali ve en az 1 harf ile 1 rakam icermelidir.", null);
        }

        if (!string.Equals(model.Password, model.ConfirmPassword, StringComparison.Ordinal))
        {
            return (false, "Şifre tekrarı eşleşmiyor.", null);
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var userColumns = await GetUsersTableColumnsAsync(connection, cancellationToken);

        const string existsSql = """
            SELECT
                (SELECT COUNT(*) FROM firmalar WHERE vergi_no = @taxNumber) AS tax_count,
                (SELECT COUNT(*) FROM firmalar WHERE firma_eposta = @companyEmail OR yetkili_eposta = @contactEmail) AS firm_email_count,
                (SELECT COUNT(*) FROM users WHERE eposta = @contactEmail) AS user_email_count;
            """;

        await using (var existsCommand = new SqlCommand(existsSql, connection))
        {
            existsCommand.Parameters.AddWithValue("@taxNumber", taxNumber);
            existsCommand.Parameters.AddWithValue("@companyEmail", companyEmail);
            existsCommand.Parameters.AddWithValue("@contactEmail", contactEmail);

            await using var reader = await existsCommand.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                if (!reader.IsDBNull(0) && reader.GetInt32(0) > 0)
                {
                    return (false, "Bu vergi numarası ile daha önce firma başvurusu yapılmış.", null);
                }

                if (!reader.IsDBNull(1) && reader.GetInt32(1) > 0)
                {
                    return (false, "Bu e-posta ile daha önce firma hesabı açılmış.", null);
                }

                if (!reader.IsDBNull(2) && reader.GetInt32(2) > 0)
                {
                    return (false, "Yetkili e-posta adresi zaten kullanımda.", null);
                }
            }
        }

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var firmaCode = await GenerateFirmaCodeAsync(connection, (SqlTransaction)transaction, cancellationToken);

            const string insertFirmaSql = """
                INSERT INTO firmalar
                (
                    firma_kodu, firma_adi, firma_turu, vergi_no, vergi_dairesi, ticaret_sicil_no, mersis_no,
                    firma_eposta, firma_telefon, web_sitesi, sektor, calisan_sayisi, aylik_seyahat_butcesi,
                    acik_adres, sehir, ilce, posta_kodu, yetkili_ad_soyad, yetkili_unvani, yetkili_eposta,
                    yetkili_telefon, onay_durumu, basvuru_tarihi, aktif_mi, giris_izni_aktif_mi,
                    planlanan_onay_suresi_saat, kayit_kaynagi, sozlesme_onay_tarihi, kvkk_onay_tarihi, notlar, olusturulma_tarihi
                )
                VALUES
                (
                    @firmaCode, @companyName, @companyType, @taxNumber, @taxOffice, @tradeRegistryNumber, @mersisNumber,
                    @companyEmail, @companyPhone, @website, @sector, @employeeCount, @monthlyTravelBudget,
                    @address, @city, @district, @postalCode, @contactName, @contactTitle, @contactEmail,
                    @contactPhone, 'Beklemede', SYSUTCDATETIME(), 1, 0,
                    24, 'web_firma_register', SYSUTCDATETIME(), SYSUTCDATETIME(), @note, SYSUTCDATETIME()
                );

                SELECT CAST(SCOPE_IDENTITY() AS bigint);
                """;

            long firmaId;
            await using (var insertFirmaCommand = new SqlCommand(insertFirmaSql, connection, (SqlTransaction)transaction))
            {
                insertFirmaCommand.Parameters.AddWithValue("@firmaCode", firmaCode);
                insertFirmaCommand.Parameters.AddWithValue("@companyName", companyName);
                insertFirmaCommand.Parameters.AddWithValue("@companyType", companyType);
                insertFirmaCommand.Parameters.AddWithValue("@taxNumber", taxNumber);
                insertFirmaCommand.Parameters.AddWithValue("@taxOffice", taxOffice);
                insertFirmaCommand.Parameters.AddWithValue("@tradeRegistryNumber", (object?)tradeRegistryNumber ?? DBNull.Value);
                insertFirmaCommand.Parameters.AddWithValue("@mersisNumber", (object?)mersisNumber ?? DBNull.Value);
                insertFirmaCommand.Parameters.AddWithValue("@companyEmail", companyEmail);
                insertFirmaCommand.Parameters.AddWithValue("@companyPhone", companyPhone);
                insertFirmaCommand.Parameters.AddWithValue("@website", (object?)website ?? DBNull.Value);
                insertFirmaCommand.Parameters.AddWithValue("@sector", sector);
                insertFirmaCommand.Parameters.AddWithValue("@employeeCount", employeeCount);
                insertFirmaCommand.Parameters.AddWithValue("@monthlyTravelBudget", (object?)monthlyTravelBudget ?? DBNull.Value);
                insertFirmaCommand.Parameters.AddWithValue("@address", address);
                insertFirmaCommand.Parameters.AddWithValue("@city", city);
                insertFirmaCommand.Parameters.AddWithValue("@district", district);
                insertFirmaCommand.Parameters.AddWithValue("@postalCode", (object?)postalCode ?? DBNull.Value);
                insertFirmaCommand.Parameters.AddWithValue("@contactName", contactName);
                insertFirmaCommand.Parameters.AddWithValue("@contactTitle", contactTitle);
                insertFirmaCommand.Parameters.AddWithValue("@contactEmail", contactEmail);
                insertFirmaCommand.Parameters.AddWithValue("@contactPhone", contactPhone);
                insertFirmaCommand.Parameters.AddWithValue("@note", "Web üzerinden alınan firma hesabı başvurusu. Yönetici onayı bekleniyor.");

                var result = await insertFirmaCommand.ExecuteScalarAsync(cancellationToken);
                firmaId = Convert.ToInt64(result);
            }

            var insertColumns = new List<string>
            {
                "ad_soyad",
                "eposta",
                "telefon",
                "sifre",
                "rol",
                "firma_id",
                "departman",
                "gorev_unvani",
                "firma_yonetici_mi",
                "hesap_durumu",
                "dil_tercihi",
                "para_birimi",
                "ulke",
                "olusturulma_tarihi"
            };

            var insertValues = new List<string>
            {
                "@fullName",
                "@contactEmail",
                "@contactPhone",
                PasswordHashSql,
                "'firma_admin'",
                "@firmaId",
                "'Kurumsal Satın Alma'",
                "@contactTitle",
                "1",
                "1",
                "'tr'",
                "'TRY'",
                "'Türkiye'",
                CurrentUtcSql
            };

            if (userColumns.Contains("kvkk_onay_tarihi"))
            {
                insertColumns.Add("kvkk_onay_tarihi");
                insertValues.Add(CurrentUtcSql);
            }

            if (userColumns.Contains("kayit_kaynagi"))
            {
                insertColumns.Add("kayit_kaynagi");
                insertValues.Add("'web_firma_register'");
            }

            if (userColumns.Contains("personel_kodu"))
            {
                insertColumns.Add("personel_kodu");
                insertValues.Add("@personelCode");
            }

            var insertUserSql = $"""
                INSERT INTO users
                (
                    {string.Join(",\n                    ", insertColumns)}
                )
                VALUES
                (
                    {string.Join(",\n                    ", insertValues)}
                );
                """;

            long userId;
            await using (var insertUserCommand = new SqlCommand(insertUserSql + "\nSELECT CAST(SCOPE_IDENTITY() AS bigint);", connection, (SqlTransaction)transaction))
            {
                insertUserCommand.Parameters.AddWithValue("@fullName", contactName);
                insertUserCommand.Parameters.AddWithValue("@contactEmail", contactEmail);
                insertUserCommand.Parameters.AddWithValue("@contactPhone", contactPhone);
                insertUserCommand.Parameters.AddWithValue("@password", model.Password);
                insertUserCommand.Parameters.AddWithValue("@firmaId", firmaId);
                insertUserCommand.Parameters.AddWithValue("@contactTitle", contactTitle);
                insertUserCommand.Parameters.AddWithValue("@personelCode", $"{firmaCode}-ADM");
                var userIdResult = await insertUserCommand.ExecuteScalarAsync(cancellationToken);
                userId = Convert.ToInt64(userIdResult);
            }

            await _contractContentService.RecordRegistrationAcceptancesAsync(
                connection,
                (SqlTransaction)transaction,
                new ContractAcceptanceRegistrationRequest
                {
                    UserId = userId,
                    FirmaId = firmaId,
                    Audience = "firma",
                    Email = contactEmail,
                    IncludePrimaryAgreement = true,
                    IncludeKvkk = true,
                    Source = "web_firma_register"
                },
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            await CreateAndQueueEmailVerificationAsync(
                connection,
                null,
                userId,
                contactEmail,
                FirstNameFromFullName(contactName),
                null,
                null,
                cancellationToken);
            return (true, "Firma basvurunuz alindi. Giris yapmadan once e-posta adresinizi onaylayin.", null);
        }
        catch (SqlException ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            return (false, $"Firma başvurusu kaydedilirken veritabanı hatası oluştu: {ex.Message}", null);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<(bool Success, string Message)> VerifyEmailAsync(string email, string code, string? token, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = (email ?? string.Empty).Trim().ToLowerInvariant();
        var normalizedCode = (code ?? string.Empty).Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(normalizedEmail) || string.IsNullOrWhiteSpace(normalizedCode))
        {
            return (false, "E-posta ve doğrulama kodu zorunludur.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        const string sql = """
            SELECT TOP (1) id, kullanici_id, gecerlilik_suresi, kullanildi_mi, deneme_sayisi, maksimum_deneme, token
            FROM email_dogrulama_tokenlari
            WHERE eposta = @email
              AND dogrulama_kodu = @code
            ORDER BY olusturulma_tarihi DESC;
            """;

        long tokenId;
        long userId;
        DateTime expiryUtc;
        bool used;
        int attemptCount;
        int maxAttempt;
        string storedToken;

        await using (var command = new SqlCommand(sql, connection, (SqlTransaction)transaction))
        {
            command.Parameters.AddWithValue("@email", normalizedEmail);
            command.Parameters.AddWithValue("@code", normalizedCode);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                return (false, "Doğrulama kodu hatalı veya bulunamadı.");
            }

            tokenId = reader.GetInt64(0);
            userId = reader.GetInt64(1);
            expiryUtc = reader.GetDateTime(2);
            used = !reader.IsDBNull(3) && reader.GetBoolean(3);
            attemptCount = reader.IsDBNull(4) ? 0 : reader.GetInt32(4);
            maxAttempt = reader.IsDBNull(5) ? 5 : reader.GetInt32(5);
            storedToken = reader.IsDBNull(6) ? string.Empty : reader.GetString(6);
        }

        if (!string.IsNullOrWhiteSpace(token) && !string.Equals(token.Trim(), storedToken, StringComparison.Ordinal))
        {
            await IncrementVerificationAttemptAsync(connection, (SqlTransaction)transaction, tokenId, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return (false, "Doğrulama bağlantısı geçersiz.");
        }

        if (used)
        {
            return (false, "Bu doğrulama bağlantısı daha önce kullanılmış.");
        }

        if (expiryUtc <= DateTime.UtcNow)
        {
            return (false, "Doğrulama kodunun süresi dolmuş. Lütfen yeni kod isteyin.");
        }

        if (attemptCount >= maxAttempt)
        {
            return (false, "Bu doğrulama kodu çok fazla denendiği için geçersiz hale geldi.");
        }

        await MarkVerificationTokenUsedAsync(connection, (SqlTransaction)transaction, tokenId, cancellationToken);
        await using (var verifyUserCommand = new SqlCommand("""
            UPDATE users
            SET email_dogrulama_tarihi = COALESCE(email_dogrulama_tarihi, SYSUTCDATETIME()),
                email_dogrulama_son_gonderim_tarihi = SYSUTCDATETIME()
            WHERE id = @userId;
            """, connection, (SqlTransaction)transaction))
        {
            verifyUserCommand.Parameters.AddWithValue("@userId", userId);
            await verifyUserCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        await _contractContentService.FinalizeEmailVerificationAsync(
            connection,
            (SqlTransaction)transaction,
            userId,
            normalizedEmail,
            ipAddress,
            userAgent,
            cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return (true, "E-posta adresiniz başarıyla onaylandı. Artık giriş yapabilirsiniz.");
    }

    public async Task<(bool Success, string Message)> ResendVerificationEmailAsync(string email, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = (email ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalizedEmail))
        {
            return (false, "E-posta adresi zorunludur.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string userSql = """
            SELECT TOP (1) id, ad_soyad, email_dogrulama_tarihi, email_dogrulama_son_gonderim_tarihi
            FROM users
            WHERE eposta = @email
              AND hesap_durumu = 1;
            """;

        long userId;
        string fullName;
        DateTime? verifiedAt;
        DateTime? lastSentAt;
        await using (var command = new SqlCommand(userSql, connection))
        {
            command.Parameters.AddWithValue("@email", normalizedEmail);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                return (true, "Eğer bu e-posta sistemimizde kayıtlıysa doğrulama kodu yeniden gönderilecektir.");
            }

            userId = reader.GetInt64(0);
            fullName = reader.GetString(1);
            verifiedAt = reader.IsDBNull(2) ? null : reader.GetDateTime(2);
            lastSentAt = reader.IsDBNull(3) ? null : reader.GetDateTime(3);
        }

        if (verifiedAt.HasValue)
        {
            return (true, "Bu e-posta adresi zaten onaylanmış.");
        }

        if (lastSentAt.HasValue && lastSentAt.Value > DateTime.UtcNow.AddSeconds(-60))
        {
            return (false, "Yeni doğrulama kodu istemeden önce lütfen 1 dakika bekleyin.");
        }

        await CreateAndQueueEmailVerificationAsync(
            connection,
            null,
            userId,
            normalizedEmail,
            FirstNameFromFullName(fullName),
            ipAddress,
            userAgent,
            cancellationToken);

        return (true, "Doğrulama kodu e-posta adresinize yeniden gönderildi.");
    }

    public async Task<(bool Success, string Message)> SendPasswordResetAsync(string email, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = (email ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalizedEmail))
        {
            return (false, "E-posta adresi zorunludur.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string userSql = """
            SELECT TOP (1) id, ad_soyad
            FROM users
            WHERE eposta = @email
              AND hesap_durumu = 1;
            """;

        long userId;
        string fullName;
        await using (var command = new SqlCommand(userSql, connection))
        {
            command.Parameters.AddWithValue("@email", normalizedEmail);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                return (true, "Eğer e-posta adresi sistemimizde kayıtlıysa şifre sıfırlama bağlantısı gönderilecektir.");
            }

            userId = reader.GetInt64(0);
            fullName = reader.GetString(1);
        }

        var token = CreateSecureToken(48);
        var resetLink = $"{_publicBaseUrl}/sifre-sifirla?token={Uri.EscapeDataString(token)}";

        await using (var insertCommand = new SqlCommand("""
            INSERT INTO sifre_sifirlama_tokenlari
            (kullanici_id, eposta, token, ip_adresi, user_agent, kullanildi_mi, gecerlilik_suresi, olusturulma_tarihi)
            VALUES
            (@userId, @email, @token, @ipAddress, @userAgent, 0, DATEADD(HOUR, 1, SYSUTCDATETIME()), SYSUTCDATETIME());
            """, connection))
        {
            insertCommand.Parameters.AddWithValue("@userId", userId);
            insertCommand.Parameters.AddWithValue("@email", normalizedEmail);
            insertCommand.Parameters.AddWithValue("@token", token);
            insertCommand.Parameters.AddWithValue("@ipAddress", (object?)ipAddress ?? DBNull.Value);
            insertCommand.Parameters.AddWithValue("@userAgent", (object?)TrimOrNull(userAgent, 500) ?? DBNull.Value);
            await insertCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        await _emailQueueService.QueueTemplateAsync(
            connection,
            null,
            new QueuedEmailTemplateRequest
            {
                UserId = userId,
                RecipientEmail = normalizedEmail,
                TemplateCode = "password_reset",
                RelatedTable = "users",
                RelatedRecordId = userId,
                Tokens = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["user_first_name"] = FirstNameFromFullName(fullName),
                    ["user_email"] = normalizedEmail,
                    ["reset_link"] = resetLink,
                    ["request_ip"] = string.IsNullOrWhiteSpace(ipAddress) ? "-" : ipAddress
                }
            },
            cancellationToken);

        return (true, "Şifre sıfırlama bağlantısı uygunsa e-posta adresinize gönderildi.");
    }

    public async Task<(bool Success, string Message)> ResetPasswordAsync(string token, string newPassword, string confirmPassword, CancellationToken cancellationToken = default)
    {
        var normalizedToken = (token ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalizedToken))
        {
            return (false, "Şifre sıfırlama bağlantısı geçersiz.");
        }

        if (!IsPasswordPolicyValid(newPassword))
        {
            return (false, "Yeni sifre en az 6 karakter olmali ve en az 1 harf ile 1 rakam icermelidir.");
        }

        if (!string.Equals(newPassword, confirmPassword, StringComparison.Ordinal))
        {
            return (false, "Şifre tekrarı eşleşmiyor.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        long tokenId;
        long userId;
        bool used;
        DateTime expiryUtc;
        await using (var command = new SqlCommand("""
            SELECT TOP (1) id, kullanici_id, kullanildi_mi, gecerlilik_suresi
            FROM sifre_sifirlama_tokenlari
            WHERE token = @token
            ORDER BY id DESC;
            """, connection, (SqlTransaction)transaction))
        {
            command.Parameters.AddWithValue("@token", normalizedToken);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                return (false, "Şifre sıfırlama bağlantısı geçersiz.");
            }

            tokenId = reader.GetInt64(0);
            userId = reader.GetInt64(1);
            used = !reader.IsDBNull(2) && reader.GetBoolean(2);
            expiryUtc = reader.GetDateTime(3);
        }

        if (used)
        {
            return (false, "Bu şifre sıfırlama bağlantısı daha önce kullanılmış.");
        }

        if (expiryUtc <= DateTime.UtcNow)
        {
            return (false, "Şifre sıfırlama bağlantısının süresi dolmuş.");
        }

        await using (var updateUserCommand = new SqlCommand("""
            UPDATE users
            SET sifre = LOWER(CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', CONVERT(nvarchar(max), @password)), 2)),
                basarisiz_giris_sayisi = 0,
                son_basarisiz_giris_tarihi = NULL,
                giris_kilit_bitis_tarihi = NULL
            WHERE id = @userId;
            """, connection, (SqlTransaction)transaction))
        {
            updateUserCommand.Parameters.AddWithValue("@password", newPassword);
            updateUserCommand.Parameters.AddWithValue("@userId", userId);
            await updateUserCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var updateTokenCommand = new SqlCommand("""
            UPDATE sifre_sifirlama_tokenlari
            SET kullanildi_mi = 1,
                kullanilma_tarihi = SYSUTCDATETIME()
            WHERE id = @tokenId;
            """, connection, (SqlTransaction)transaction))
        {
            updateTokenCommand.Parameters.AddWithValue("@tokenId", tokenId);
            await updateTokenCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return (true, "Şifreniz başarıyla güncellendi. Yeni şifrenizle giriş yapabilirsiniz.");
    }

    public async Task<string> ResolveLoginPathByEmailAsync(string? email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = (email ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalizedEmail))
        {
            return UserLoginPath;
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = """
            SELECT TOP (1) id
            FROM users
            WHERE eposta = @email
            ORDER BY id DESC;
            """;

        long userId;
        await using (var command = new SqlCommand(sql, connection))
        {
            command.Parameters.AddWithValue("@email", normalizedEmail);
            var result = await command.ExecuteScalarAsync(cancellationToken);
            if (result is null || result is DBNull)
            {
                return UserLoginPath;
            }

            userId = Convert.ToInt64(result, CultureInfo.InvariantCulture);
        }

        var user = await GetUserByIdAsync(connection, userId, cancellationToken);
        return MapLoginPathByAccountType(user?.AccountType);
    }

    public async Task<string> ResolveLoginPathByResetTokenAsync(string? token, CancellationToken cancellationToken = default)
    {
        var normalizedToken = (token ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalizedToken))
        {
            return UserLoginPath;
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = """
            SELECT TOP (1) kullanici_id
            FROM sifre_sifirlama_tokenlari
            WHERE token = @token
            ORDER BY id DESC;
            """;

        long userId;
        await using (var command = new SqlCommand(sql, connection))
        {
            command.Parameters.AddWithValue("@token", normalizedToken);
            var result = await command.ExecuteScalarAsync(cancellationToken);
            if (result is null || result is DBNull)
            {
                return UserLoginPath;
            }

            userId = Convert.ToInt64(result, CultureInfo.InvariantCulture);
        }

        var user = await GetUserByIdAsync(connection, userId, cancellationToken);
        return MapLoginPathByAccountType(user?.AccountType);
    }

    private async Task<UserSessionModel?> GetUserAsync(string identity, string password, bool requirePartner, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var authSchema = await GetAuthSchemaAsync(connection, cancellationToken);

        var roleSelect = authSchema.HasUserRoleColumn
            ? "u.rol"
            : "'user'";

        var firmaIdSelect = authSchema.HasFirmaIdColumn
            ? "u.firma_id"
            : "NULL";

        var salesTeamSelect = authSchema.HasSalesTeamColumn
            ? "u.satis_ekibi"
            : "NULL";

        var ownershipPartnerSelect = authSchema.HasOwnershipPartnerColumn
            ? "u.sahiplik_partner_id"
            : "NULL";

        var partnerIdSelect = authSchema.HasUsersPartnerTable && authSchema.HasUsersPartnerPartnerIdColumn
            ? (authSchema.HasOwnershipPartnerColumn
                ? "COALESCE(up.partner_id, u.sahiplik_partner_id)"
                : "up.partner_id")
            : (authSchema.HasOwnershipPartnerColumn ? "u.sahiplik_partner_id" : "NULL");

        var usersPartnerJoinClause = authSchema.HasUsersPartnerTable
            ? (authSchema.HasUsersPartnerActiveColumn
                ? """
                    LEFT JOIN users_partner up
                        ON up.user_id = u.id
                       AND up.aktif_mi = 1
                    """
                : """
                    LEFT JOIN users_partner up
                        ON up.user_id = u.id
                    """)
            : string.Empty;

        var partnerSortOrderSelect = authSchema.HasUsersPartnerTable && authSchema.HasUsersPartnerMainAccountColumn
            ? "MAX(COALESCE(up.ana_hesap_mi, 0))"
            : "0";

        var partnerRowIdSelect = authSchema.HasUsersPartnerTable && authSchema.HasUsersPartnerIdColumn
            ? "MIN(COALESCE(up.id, 0))"
            : "0";

        var partnerHotelOwnershipClause = authSchema.HasHotelOwnershipTable
            ? """
                (
                    EXISTS
                    (
                        SELECT 1
                        FROM otel_kullanici_sahiplikleri oku
                        WHERE oku.user_id = u.id
                          AND oku.aktif_mi = 1
                    )
                    OR EXISTS
                    (
                        SELECT 1
                        FROM oteller o
                        WHERE o.user_id = u.id
                    )
                )
                """
            : """
                (
                    EXISTS
                    (
                        SELECT 1
                        FROM oteller o
                        WHERE o.user_id = u.id
                    )
                )
                """;

        var partnerRequirementClause = authSchema.HasUserRoleColumn
            ? $"(@requirePartner = 0 OR {partnerIdSelect} IS NOT NULL OR u.rol LIKE 'partner_%' OR {partnerHotelOwnershipClause})"
            : $"(@requirePartner = 0 OR {partnerIdSelect} IS NOT NULL OR {partnerHotelOwnershipClause})";

        var managedHotelSelect = authSchema.HasHotelOwnershipTable
            ? """
                (
                    SELECT STRING_AGG(CONVERT(varchar(20), hotel_ids.otel_id), ',') WITHIN GROUP (ORDER BY hotel_ids.otel_id)
                    FROM
                    (
                        SELECT DISTINCT oku.otel_id
                        FROM otel_kullanici_sahiplikleri oku
                        WHERE oku.user_id = u.id
                          AND oku.aktif_mi = 1
                    ) AS hotel_ids
                )
                """
            : "NULL";

        var roleCodesSelect = """
            (
                SELECT STRING_AGG(role_rows.rol_kodu, ',') WITHIN GROUP (ORDER BY role_rows.rol_kodu)
                FROM
                (
                    SELECT DISTINCT r.rol_kodu
                    FROM kullanici_rolleri kr
                    INNER JOIN roller r
                        ON r.id = kr.rol_id
                    WHERE kr.kullanici_id = u.id
                      AND (kr.bitis_tarihi IS NULL OR kr.bitis_tarihi > SYSUTCDATETIME())
                ) AS role_rows
            )
            """;

        var sql = $"""
            SELECT TOP (1)
                u.id,
                u.ad_soyad,
                u.eposta,
                {partnerIdSelect} AS partner_id,
                {roleSelect} AS user_role,
                {firmaIdSelect} AS firma_id,
                {salesTeamSelect} AS sales_team,
                {ownershipPartnerSelect} AS sahiplik_partner_id,
                {managedHotelSelect} AS managed_hotel_ids,
                {partnerSortOrderSelect} AS ana_hesap_mi_order,
                {partnerRowIdSelect} AS user_partner_row_id,
                {roleCodesSelect} AS role_codes
            FROM users u
            {usersPartnerJoinClause}
            WHERE u.hesap_durumu = 1
              AND (
                    LOWER(COALESCE(u.sifre, '')) = {PasswordHashSql}
                 OR LOWER(COALESCE(u.sifre, '')) = {LegacySha1HashSql}
                 OR LOWER(COALESCE(u.sifre, '')) = {LegacyMd5HashSql}
                 OR COALESCE(u.sifre, '') = @password
              )
              AND (
                    LOWER(u.eposta) = LOWER(@identity)
                 OR u.telefon = @identity
                 OR (@partnerIdentity IS NOT NULL AND {partnerIdSelect} = @partnerIdentity)
                 OR (
                        @hotelCode IS NOT NULL
                    AND EXISTS
                    (
                        SELECT 1
                        FROM otel_kullanici_sahiplikleri oku
                        INNER JOIN oteller o
                            ON o.id = oku.otel_id
                        WHERE oku.user_id = u.id
                          AND oku.aktif_mi = 1
                          AND UPPER(o.otel_kodu) = @hotelCode
                    )
                 )
                 OR (
                        @hotelCode IS NOT NULL
                    AND EXISTS
                    (
                        SELECT 1
                        FROM oteller o
                        WHERE o.user_id = u.id
                          AND UPPER(o.otel_kodu) = @hotelCode
                    )
                 )
              )
              AND {partnerRequirementClause}
            GROUP BY u.id, u.ad_soyad, u.eposta, {partnerIdSelect}, {roleSelect}, {firmaIdSelect}, {salesTeamSelect}, {ownershipPartnerSelect}
            ORDER BY ana_hesap_mi_order DESC, user_partner_row_id ASC
            """;

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@identity", identity.Trim());
        command.Parameters.AddWithValue("@password", password);
        command.Parameters.AddWithValue("@requirePartner", requirePartner ? 1 : 0);

        var numericPartnerIdentity = ParsePartnerIdentity(identity);
        if (numericPartnerIdentity.HasValue)
        {
            command.Parameters.AddWithValue("@partnerIdentity", numericPartnerIdentity.Value);
        }
        else
        {
            command.Parameters.AddWithValue("@partnerIdentity", DBNull.Value);
        }

        var hotelCode = ParseHotelCode(identity);
        if (!string.IsNullOrWhiteSpace(hotelCode))
        {
            command.Parameters.AddWithValue("@hotelCode", hotelCode);
        }
        else
        {
            command.Parameters.AddWithValue("@hotelCode", DBNull.Value);
        }

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        var idOrdinal = reader.GetOrdinal("id");
        var fullNameOrdinal = reader.GetOrdinal("ad_soyad");
        var emailOrdinal = reader.GetOrdinal("eposta");
        var partnerIdOrdinal = reader.GetOrdinal("partner_id");
        var userRoleOrdinal = reader.GetOrdinal("user_role");
        var firmaIdOrdinal = reader.GetOrdinal("firma_id");
        var salesTeamOrdinal = reader.GetOrdinal("sales_team");
        var ownershipPartnerOrdinal = reader.GetOrdinal("sahiplik_partner_id");
        var managedHotelIdsOrdinal = reader.GetOrdinal("managed_hotel_ids");
        var roleCodesOrdinal = reader.GetOrdinal("role_codes");

        var roles = reader.IsDBNull(roleCodesOrdinal)
            ? new List<string>()
            : reader.GetString(roleCodesOrdinal)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

        var userRole = reader.IsDBNull(userRoleOrdinal)
            ? InferUserRole(reader.IsDBNull(partnerIdOrdinal), roles)
            : reader.GetString(userRoleOrdinal);

        var session = new UserSessionModel
        {
            UserId = reader.GetInt64(idOrdinal),
            FullName = reader.GetString(fullNameOrdinal),
            Email = reader.GetString(emailOrdinal),
            PartnerId = reader.IsDBNull(partnerIdOrdinal) ? null : reader.GetInt64(partnerIdOrdinal),
            OwnershipPartnerId = reader.IsDBNull(ownershipPartnerOrdinal) ? null : reader.GetInt64(ownershipPartnerOrdinal),
            UserRole = userRole,
            ManagedHotelIds = ParseManagedHotelIds(reader, managedHotelIdsOrdinal),
            RoleCodes = roles
        };

        if (IsAdminAccount(session.UserRole, roles))
        {
            session.AccountType = "admin";
        }
        else if (!reader.IsDBNull(firmaIdOrdinal) || session.UserRole.StartsWith("firma_", StringComparison.OrdinalIgnoreCase))
        {
            session.AccountType = "firma";
        }
        else if (!reader.IsDBNull(salesTeamOrdinal) || session.UserRole.StartsWith("sales_", StringComparison.OrdinalIgnoreCase))
        {
            session.AccountType = "sales";
        }
        else if (session.PartnerId.HasValue || session.UserRole.StartsWith("partner_", StringComparison.OrdinalIgnoreCase))
        {
            session.AccountType = "partner";
        }
        else
        {
            session.AccountType = "user";
        }

        return session;
    }

    private static long? ParsePartnerIdentity(string identity)
    {
        if (long.TryParse(identity, out var rawId))
        {
            return rawId;
        }

        const string prefix = "PTR-";
        if (identity.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            && long.TryParse(identity[prefix.Length..], out var prefixedId))
        {
            return prefixedId;
        }

        return null;
    }

    private static string? ParseHotelCode(string identity)
    {
        if (string.IsNullOrWhiteSpace(identity))
        {
            return null;
        }

        var normalized = NormalizeCodeInput(identity);
        return normalized.StartsWith("OTLTRZM-", StringComparison.Ordinal)
            ? normalized
            : null;
    }

    private static async Task<UserSessionModel?> GetUserByIdAsync(SqlConnection connection, long userId, CancellationToken cancellationToken)
    {
        var authSchema = await GetAuthSchemaAsync(connection, cancellationToken);

        var roleSelect = authSchema.HasUserRoleColumn
            ? "u.rol"
            : "'user'";

        var firmaIdSelect = authSchema.HasFirmaIdColumn
            ? "u.firma_id"
            : "NULL";

        var salesTeamSelect = authSchema.HasSalesTeamColumn
            ? "u.satis_ekibi"
            : "NULL";

        var ownershipPartnerSelect = authSchema.HasOwnershipPartnerColumn
            ? "u.sahiplik_partner_id"
            : "NULL";

        var partnerIdSelect = authSchema.HasUsersPartnerTable && authSchema.HasUsersPartnerPartnerIdColumn
            ? (authSchema.HasOwnershipPartnerColumn
                ? "COALESCE(up.partner_id, u.sahiplik_partner_id)"
                : "up.partner_id")
            : (authSchema.HasOwnershipPartnerColumn ? "u.sahiplik_partner_id" : "NULL");

        var usersPartnerJoinClause = authSchema.HasUsersPartnerTable
            ? (authSchema.HasUsersPartnerActiveColumn
                ? """
                    LEFT JOIN users_partner up
                        ON up.user_id = u.id
                       AND up.aktif_mi = 1
                    """
                : """
                    LEFT JOIN users_partner up
                        ON up.user_id = u.id
                    """)
            : string.Empty;

        var partnerSortOrderSelect = authSchema.HasUsersPartnerTable && authSchema.HasUsersPartnerMainAccountColumn
            ? "MAX(COALESCE(up.ana_hesap_mi, 0))"
            : "0";

        var partnerRowIdSelect = authSchema.HasUsersPartnerTable && authSchema.HasUsersPartnerIdColumn
            ? "MIN(COALESCE(up.id, 0))"
            : "0";

        var managedHotelSelect = authSchema.HasHotelOwnershipTable
            ? """
                (
                    SELECT STRING_AGG(CONVERT(varchar(20), hotel_ids.otel_id), ',') WITHIN GROUP (ORDER BY hotel_ids.otel_id)
                    FROM
                    (
                        SELECT DISTINCT oku.otel_id
                        FROM otel_kullanici_sahiplikleri oku
                        WHERE oku.user_id = u.id
                          AND oku.aktif_mi = 1
                    ) AS hotel_ids
                )
                """
            : "NULL";

        var roleCodesSelect = """
            (
                SELECT STRING_AGG(role_rows.rol_kodu, ',') WITHIN GROUP (ORDER BY role_rows.rol_kodu)
                FROM
                (
                    SELECT DISTINCT r.rol_kodu
                    FROM kullanici_rolleri kr
                    INNER JOIN roller r
                        ON r.id = kr.rol_id
                    WHERE kr.kullanici_id = u.id
                      AND (kr.bitis_tarihi IS NULL OR kr.bitis_tarihi > SYSUTCDATETIME())
                ) AS role_rows
            )
            """;

        var sql = $"""
            SELECT TOP (1)
                u.id,
                u.ad_soyad,
                u.eposta,
                {partnerIdSelect} AS partner_id,
                {roleSelect} AS user_role,
                {firmaIdSelect} AS firma_id,
                {salesTeamSelect} AS sales_team,
                {ownershipPartnerSelect} AS sahiplik_partner_id,
                {managedHotelSelect} AS managed_hotel_ids,
                {partnerSortOrderSelect} AS ana_hesap_mi_order,
                {partnerRowIdSelect} AS user_partner_row_id,
                {roleCodesSelect} AS role_codes
            FROM users u
            {usersPartnerJoinClause}
            WHERE u.id = @userId
            GROUP BY u.id, u.ad_soyad, u.eposta, {partnerIdSelect}, {roleSelect}, {firmaIdSelect}, {salesTeamSelect}, {ownershipPartnerSelect}
            ORDER BY ana_hesap_mi_order DESC, user_partner_row_id ASC
            """;

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@userId", userId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        var idOrdinal = reader.GetOrdinal("id");
        var fullNameOrdinal = reader.GetOrdinal("ad_soyad");
        var emailOrdinal = reader.GetOrdinal("eposta");
        var partnerIdOrdinal = reader.GetOrdinal("partner_id");
        var userRoleOrdinal = reader.GetOrdinal("user_role");
        var firmaIdOrdinal = reader.GetOrdinal("firma_id");
        var salesTeamOrdinal = reader.GetOrdinal("sales_team");
        var ownershipPartnerOrdinal = reader.GetOrdinal("sahiplik_partner_id");
        var managedHotelIdsOrdinal = reader.GetOrdinal("managed_hotel_ids");
        var roleCodesOrdinal = reader.GetOrdinal("role_codes");

        var roles = reader.IsDBNull(roleCodesOrdinal)
            ? new List<string>()
            : reader.GetString(roleCodesOrdinal)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

        var userRole = reader.IsDBNull(userRoleOrdinal)
            ? InferUserRole(reader.IsDBNull(partnerIdOrdinal), roles)
            : reader.GetString(userRoleOrdinal);

        return new UserSessionModel
        {
            UserId = reader.GetInt64(idOrdinal),
            FullName = reader.GetString(fullNameOrdinal),
            Email = reader.GetString(emailOrdinal),
            PartnerId = reader.IsDBNull(partnerIdOrdinal) ? null : reader.GetInt64(partnerIdOrdinal),
            OwnershipPartnerId = reader.IsDBNull(ownershipPartnerOrdinal) ? null : reader.GetInt64(ownershipPartnerOrdinal),
            UserRole = userRole,
            ManagedHotelIds = ParseManagedHotelIds(reader, managedHotelIdsOrdinal),
            RoleCodes = roles,
            AccountType = IsAdminAccount(userRole, roles)
                ? "admin"
                : (!reader.IsDBNull(firmaIdOrdinal) || userRole.StartsWith("firma_", StringComparison.OrdinalIgnoreCase)
                    ? "firma"
                    : (!reader.IsDBNull(salesTeamOrdinal) || userRole.StartsWith("sales_", StringComparison.OrdinalIgnoreCase)
                        ? "sales"
                    : (!reader.IsDBNull(partnerIdOrdinal) || userRole.StartsWith("partner_", StringComparison.OrdinalIgnoreCase) ? "partner" : "user"))
                  )
        };
    }

    private static async Task<HashSet<string>> GetUsersTableColumnsAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT COLUMN_NAME
            FROM information_schema.COLUMNS
            WHERE TABLE_SCHEMA = 'dbo'
              AND TABLE_NAME = 'users';
            """;

        var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            columns.Add(reader.GetString(0));
        }

        return columns;
    }

    private static async Task<HashSet<string>> GetColumnsAsync(SqlConnection connection, string tableName, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT COLUMN_NAME
            FROM information_schema.COLUMNS
            WHERE TABLE_SCHEMA = 'dbo'
              AND TABLE_NAME = @tableName;
            """;

        var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@tableName", tableName);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            columns.Add(reader.GetString(0));
        }

        return columns;
    }

    private static async Task<AuthSchemaInfo> GetAuthSchemaAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                SUM(CASE WHEN c.TABLE_NAME = 'users' AND c.COLUMN_NAME = 'rol' THEN 1 ELSE 0 END) AS has_user_role_column,
                SUM(CASE WHEN c.TABLE_NAME = 'users' AND c.COLUMN_NAME = 'sahiplik_partner_id' THEN 1 ELSE 0 END) AS has_ownership_partner_column,
                SUM(CASE WHEN c.TABLE_NAME = 'users' AND c.COLUMN_NAME = 'firma_id' THEN 1 ELSE 0 END) AS has_firma_id_column,
                SUM(CASE WHEN c.TABLE_NAME = 'users' AND c.COLUMN_NAME = 'satis_ekibi' THEN 1 ELSE 0 END) AS has_sales_team_column,
                MAX(CASE WHEN t.TABLE_NAME = 'otel_kullanici_sahiplikleri' THEN 1 ELSE 0 END) AS has_hotel_ownership_table,
                MAX(CASE WHEN t.TABLE_NAME = 'users_partner' THEN 1 ELSE 0 END) AS has_users_partner_table,
                SUM(CASE WHEN c.TABLE_NAME = 'users_partner' AND c.COLUMN_NAME = 'partner_id' THEN 1 ELSE 0 END) AS has_users_partner_partner_id_column,
                SUM(CASE WHEN c.TABLE_NAME = 'users_partner' AND c.COLUMN_NAME = 'aktif_mi' THEN 1 ELSE 0 END) AS has_users_partner_active_column,
                SUM(CASE WHEN c.TABLE_NAME = 'users_partner' AND c.COLUMN_NAME = 'ana_hesap_mi' THEN 1 ELSE 0 END) AS has_users_partner_main_account_column,
                SUM(CASE WHEN c.TABLE_NAME = 'users_partner' AND c.COLUMN_NAME = 'id' THEN 1 ELSE 0 END) AS has_users_partner_id_column
            FROM information_schema.TABLES t
            LEFT JOIN information_schema.COLUMNS c
                ON c.TABLE_SCHEMA = t.TABLE_SCHEMA
               AND c.TABLE_NAME = t.TABLE_NAME
            WHERE t.TABLE_SCHEMA = 'dbo'
              AND t.TABLE_NAME IN ('users', 'otel_kullanici_sahiplikleri', 'users_partner');
            """;

        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            return new AuthSchemaInfo();
        }

        return new AuthSchemaInfo
        {
            HasUserRoleColumn = !reader.IsDBNull(0) && Convert.ToInt64(reader.GetValue(0), CultureInfo.InvariantCulture) > 0,
            HasOwnershipPartnerColumn = !reader.IsDBNull(1) && Convert.ToInt64(reader.GetValue(1), CultureInfo.InvariantCulture) > 0,
            HasFirmaIdColumn = !reader.IsDBNull(2) && Convert.ToInt64(reader.GetValue(2), CultureInfo.InvariantCulture) > 0,
            HasSalesTeamColumn = !reader.IsDBNull(3) && Convert.ToInt64(reader.GetValue(3), CultureInfo.InvariantCulture) > 0,
            HasHotelOwnershipTable = !reader.IsDBNull(4) && Convert.ToInt64(reader.GetValue(4), CultureInfo.InvariantCulture) > 0,
            HasUsersPartnerTable = !reader.IsDBNull(5) && Convert.ToInt64(reader.GetValue(5), CultureInfo.InvariantCulture) > 0,
            HasUsersPartnerPartnerIdColumn = !reader.IsDBNull(6) && Convert.ToInt64(reader.GetValue(6), CultureInfo.InvariantCulture) > 0,
            HasUsersPartnerActiveColumn = !reader.IsDBNull(7) && Convert.ToInt64(reader.GetValue(7), CultureInfo.InvariantCulture) > 0,
            HasUsersPartnerMainAccountColumn = !reader.IsDBNull(8) && Convert.ToInt64(reader.GetValue(8), CultureInfo.InvariantCulture) > 0,
            HasUsersPartnerIdColumn = !reader.IsDBNull(9) && Convert.ToInt64(reader.GetValue(9), CultureInfo.InvariantCulture) > 0
        };
    }

    private static string InferUserRole(bool hasNoPartnerId, IReadOnlyCollection<string> roleCodes)
    {
        if (!hasNoPartnerId)
        {
            return "partner_owner";
        }

        return roleCodes.Count > 0 ? "admin" : "user";
    }

    private static bool IsAdminAccount(string? userRole, IReadOnlyCollection<string> roleCodes)
    {
        if (string.Equals(userRole, "admin", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return roleCodes.Any(static code =>
            string.Equals(code, "admin", StringComparison.OrdinalIgnoreCase)
            || string.Equals(code, "super_admin", StringComparison.OrdinalIgnoreCase)
            || string.Equals(code, "superadmin", StringComparison.OrdinalIgnoreCase));
    }

    private static string MapLoginPathByAccountType(string? accountType)
    {
        return accountType?.ToLowerInvariant() switch
        {
            "admin" => AdminLoginPath,
            "partner" => PartnerLoginPath,
            "firma" => FirmaLoginPath,
            _ => UserLoginPath
        };
    }

    private static bool IsPasswordPolicyValid(string? password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
        {
            return false;
        }

        var hasLetter = password.Any(char.IsLetter);
        var hasDigit = password.Any(char.IsDigit);
        return hasLetter && hasDigit;
    }

    private static List<long> ParseManagedHotelIds(SqlDataReader reader, int ordinal)
    {
        if (reader.IsDBNull(ordinal))
        {
            return new List<long>();
        }

        return reader.GetString(ordinal)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(static value => long.TryParse(value, out var parsed) ? parsed : 0)
            .Where(static value => value > 0)
            .Distinct()
            .ToList();
    }

    private static string NormalizeCompanyType(string? value)
    {
        return value?.Trim() switch
        {
            "Anonim Şirketi" => "Anonim Şirketi",
            "Anonim Sirketi" => "Anonim Şirketi",
            "Limited Şirketi" => "Limited Şirketi",
            "Limited Sirketi" => "Limited Şirketi",
            "Şahıs Firması" => "Şahıs Firması",
            "Sahis Firmasi" => "Şahıs Firması",
            "Adi Ortaklık" => "Adi Ortaklık",
            "Adi Ortaklik" => "Adi Ortaklık",
            "Holding" => "Holding",
            "Kamu Kurumu" => "Kamu Kurumu",
            "STK" => "STK",
            "Vakıf" => "Vakıf",
            "Vakif" => "Vakıf",
            "Dernek" => "Dernek",
            "Teknoloji Şirketi" => "Teknoloji Şirketi",
            "Teknoloji Sirketi" => "Teknoloji Şirketi",
            "Turizm Şirketi" => "Turizm Şirketi",
            "Turizm Sirketi" => "Turizm Şirketi",
            "Danışmanlık Şirketi" => "Danışmanlık Şirketi",
            "Danismanlik Sirketi" => "Danışmanlık Şirketi",
            _ => string.Empty
        };
    }

    private async Task<bool> CanFirmaUserAccessAsync(long userId, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = """
            SELECT COUNT(*)
            FROM users u
            INNER JOIN firmalar f
                ON f.id = u.firma_id
            WHERE u.id = @userId
              AND u.hesap_durumu = 1
              AND f.aktif_mi = 1
              AND UPPER(COALESCE(CONVERT(nvarchar(100), f.onay_durumu), N'')) COLLATE Turkish_CI_AI LIKE N'ONAY%'
              AND COALESCE(f.giris_izni_aktif_mi, 0) = 1;
            """;

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@userId", userId);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result) > 0;
    }

    private async Task<AuthCandidateInfo?> FindAuthCandidateAsync(string identity, bool requirePartner, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var authSchema = await GetAuthSchemaAsync(connection, cancellationToken);
        var roleSelect = authSchema.HasUserRoleColumn ? "u.rol" : "'user'";
        var ownershipPartnerSelect = authSchema.HasOwnershipPartnerColumn ? "u.sahiplik_partner_id" : "NULL";
        var partnerIdSelect = authSchema.HasUsersPartnerTable && authSchema.HasUsersPartnerPartnerIdColumn
            ? (authSchema.HasOwnershipPartnerColumn
                ? "COALESCE(up.partner_id, u.sahiplik_partner_id)"
                : "up.partner_id")
            : (authSchema.HasOwnershipPartnerColumn ? "u.sahiplik_partner_id" : "NULL");

        var usersPartnerJoinClause = authSchema.HasUsersPartnerTable
            ? (authSchema.HasUsersPartnerActiveColumn
                ? """
                    LEFT JOIN users_partner up
                        ON up.user_id = u.id
                       AND up.aktif_mi = 1
                    """
                : """
                    LEFT JOIN users_partner up
                        ON up.user_id = u.id
                    """)
            : string.Empty;

        var partnerHotelOwnershipClause = authSchema.HasHotelOwnershipTable
            ? """
                (
                    EXISTS
                    (
                        SELECT 1
                        FROM otel_kullanici_sahiplikleri oku
                        WHERE oku.user_id = u.id
                          AND oku.aktif_mi = 1
                    )
                    OR EXISTS
                    (
                        SELECT 1
                        FROM oteller o
                        WHERE o.user_id = u.id
                    )
                )
                """
            : """
                (
                    EXISTS
                    (
                        SELECT 1
                        FROM oteller o
                        WHERE o.user_id = u.id
                    )
                )
                """;

        var partnerRequirementClause = authSchema.HasUserRoleColumn
            ? $"(@requirePartner = 0 OR {partnerIdSelect} IS NOT NULL OR u.rol LIKE 'partner_%' OR {partnerHotelOwnershipClause})"
            : $"(@requirePartner = 0 OR {partnerIdSelect} IS NOT NULL OR {partnerHotelOwnershipClause})";

        var sql = $"""
            SELECT TOP (1)
                u.id,
                u.eposta,
                {roleSelect} AS user_role,
                u.email_dogrulama_tarihi,
                u.giris_kilit_bitis_tarihi,
                {partnerIdSelect} AS partner_id,
                {ownershipPartnerSelect} AS sahiplik_partner_id
            FROM users u
            {usersPartnerJoinClause}
            WHERE u.hesap_durumu = 1
              AND (
                    LOWER(u.eposta) = LOWER(@identity)
                 OR u.telefon = @identity
                 OR (@partnerIdentity IS NOT NULL AND {partnerIdSelect} = @partnerIdentity)
                 OR (
                        @hotelCode IS NOT NULL
                    AND EXISTS
                    (
                        SELECT 1
                        FROM otel_kullanici_sahiplikleri oku
                        INNER JOIN oteller o ON o.id = oku.otel_id
                        WHERE oku.user_id = u.id
                          AND oku.aktif_mi = 1
                          AND UPPER(o.otel_kodu) = @hotelCode
                    )
                 )
                 OR (
                        @hotelCode IS NOT NULL
                    AND EXISTS
                    (
                        SELECT 1
                        FROM oteller o
                        WHERE o.user_id = u.id
                          AND UPPER(o.otel_kodu) = @hotelCode
                    )
                 )
              )
              AND {partnerRequirementClause}
            ORDER BY u.id ASC;
            """;

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@identity", identity.Trim());
        command.Parameters.AddWithValue("@requirePartner", requirePartner ? 1 : 0);

        var numericPartnerIdentity = ParsePartnerIdentity(identity);
        command.Parameters.AddWithValue("@partnerIdentity", numericPartnerIdentity.HasValue ? numericPartnerIdentity.Value : DBNull.Value);

        var hotelCode = ParseHotelCode(identity);
        command.Parameters.AddWithValue("@hotelCode", !string.IsNullOrWhiteSpace(hotelCode) ? hotelCode : DBNull.Value);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new AuthCandidateInfo
        {
            UserId = reader.GetInt64(0),
            Email = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
            UserRole = reader.IsDBNull(2) ? "user" : reader.GetString(2),
            EmailVerified = !reader.IsDBNull(3),
            LockoutEndUtc = reader.IsDBNull(4) ? null : reader.GetDateTime(4)
        };
    }

    private async Task<DateTime?> RegisterFailedLoginAttemptAsync(long userId, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var sql = $"""
            UPDATE users
            SET basarisiz_giris_sayisi = COALESCE(basarisiz_giris_sayisi, 0) + 1,
                son_basarisiz_giris_tarihi = SYSDATETIME(),
                giris_kilit_bitis_tarihi = CASE
                    WHEN COALESCE(basarisiz_giris_sayisi, 0) + 1 >= {FailedLoginLockoutThreshold} THEN DATEADD(MINUTE, {FailedLoginLockoutMinutes}, SYSDATETIME())
                    ELSE giris_kilit_bitis_tarihi
                END
            WHERE id = @userId;
            """;

        await using (var command = new SqlCommand(sql, connection))
        {
            command.Parameters.AddWithValue("@userId", userId);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        await using var readCommand = new SqlCommand("SELECT TOP (1) giris_kilit_bitis_tarihi FROM users WHERE id = @userId;", connection);
        readCommand.Parameters.AddWithValue("@userId", userId);
        var result = await readCommand.ExecuteScalarAsync(cancellationToken);
        return result is DBNull or null ? null : Convert.ToDateTime(result, CultureInfo.InvariantCulture);
    }

    private static string CreateLockoutTriggeredMessage()
        => $"Arka arkaya {FailedLoginLockoutThreshold} hatali deneme algilandi. Hesap {FailedLoginLockoutMinutes} dakika kilitlendi.";

    private async Task ResetFailedLoginAttemptAsync(long userId, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("""
            UPDATE users
            SET basarisiz_giris_sayisi = 0,
                son_basarisiz_giris_tarihi = NULL,
                giris_kilit_bitis_tarihi = NULL,
                son_giris_tarihi = SYSUTCDATETIME()
            WHERE id = @userId;
            """, connection);
        command.Parameters.AddWithValue("@userId", userId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task UpgradePasswordHashIfNeededAsync(long userId, string rawPassword, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = """
            UPDATE users
            SET sifre = LOWER(CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', CONVERT(nvarchar(max), @password)), 2)),
                guncellenme_tarihi = SYSUTCDATETIME()
            WHERE id = @userId
              AND (
                    sifre IS NULL
                 OR LOWER(sifre) <> LOWER(CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', CONVERT(nvarchar(max), @password)), 2))
              );
            """;

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@userId", userId);
        command.Parameters.AddWithValue("@password", rawPassword);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task<bool> IsEmailVerifiedAsync(long userId, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("SELECT TOP (1) email_dogrulama_tarihi FROM users WHERE id = @userId;", connection);
        command.Parameters.AddWithValue("@userId", userId);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is not null && result is not DBNull;
    }

    private async Task CreateAndQueueEmailVerificationAsync(
        SqlConnection connection,
        SqlTransaction? transaction,
        long userId,
        string email,
        string firstName,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken)
    {
        var token = CreateSecureToken(48);
        var code = CreateNumericCode(6);
        var verificationLink = $"{_publicBaseUrl}/eposta-dogrula?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}&code={Uri.EscapeDataString(code)}";

        await using (var insertCommand = new SqlCommand("""
            INSERT INTO email_dogrulama_tokenlari
            (kullanici_id, eposta, token, dogrulama_kodu, kullanildi_mi, deneme_sayisi, maksimum_deneme, ip_adresi, user_agent, gecerlilik_suresi, olusturulma_tarihi)
            VALUES
            (@userId, @email, @token, @code, 0, 0, 5, @ipAddress, @userAgent, DATEADD(HOUR, 24, SYSUTCDATETIME()), SYSUTCDATETIME());
            """, connection, transaction))
        {
            insertCommand.Parameters.AddWithValue("@userId", userId);
            insertCommand.Parameters.AddWithValue("@email", email);
            insertCommand.Parameters.AddWithValue("@token", token);
            insertCommand.Parameters.AddWithValue("@code", code);
            insertCommand.Parameters.AddWithValue("@ipAddress", (object?)ipAddress ?? DBNull.Value);
            insertCommand.Parameters.AddWithValue("@userAgent", (object?)TrimOrNull(userAgent, 500) ?? DBNull.Value);
            await insertCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var updateUserCommand = new SqlCommand("""
            UPDATE users
            SET email_dogrulama_son_gonderim_tarihi = SYSUTCDATETIME()
            WHERE id = @userId;
            """, connection, transaction))
        {
            updateUserCommand.Parameters.AddWithValue("@userId", userId);
            await updateUserCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        await _emailQueueService.QueueTemplateAsync(
            connection,
            transaction,
            new QueuedEmailTemplateRequest
            {
                UserId = userId,
                RecipientEmail = email,
                TemplateCode = "email_verify",
                RelatedTable = "users",
                RelatedRecordId = userId,
                Tokens = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["user_first_name"] = firstName,
                    ["user_email"] = email,
                    ["registration_date"] = DateTime.Now.ToString("dd.MM.yyyy HH:mm", CultureInfo.GetCultureInfo("tr-TR")),
                    ["verification_link"] = verificationLink,
                    ["verification_code"] = code
                }
            },
            cancellationToken);
    }

    private static async Task MarkVerificationTokenUsedAsync(SqlConnection connection, SqlTransaction transaction, long tokenId, CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand("""
            UPDATE email_dogrulama_tokenlari
            SET kullanildi_mi = 1,
                kullanilma_tarihi = SYSUTCDATETIME()
            WHERE id = @tokenId;
            """, connection, (SqlTransaction)transaction);
        command.Parameters.AddWithValue("@tokenId", tokenId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task IncrementVerificationAttemptAsync(SqlConnection connection, SqlTransaction transaction, long tokenId, CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand("""
            UPDATE email_dogrulama_tokenlari
            SET deneme_sayisi = COALESCE(deneme_sayisi, 0) + 1
            WHERE id = @tokenId;
            """, connection, (SqlTransaction)transaction);
        command.Parameters.AddWithValue("@tokenId", tokenId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static string CreateSecureToken(int byteLength)
    {
        var buffer = RandomNumberGenerator.GetBytes(byteLength);
        return Convert.ToHexString(buffer).ToLowerInvariant();
    }

    private static string CreateNumericCode(int length)
    {
        var max = (int)Math.Pow(10, length);
        var min = (int)Math.Pow(10, length - 1);
        return RandomNumberGenerator.GetInt32(min, max).ToString(CultureInfo.InvariantCulture);
    }

    private static string FirstNameFromFullName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return "Misafir";
        }

        return fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault() ?? fullName;
    }

    private static string? TrimOrNull(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }

    private static async Task<string> GenerateFirmaCodeAsync(SqlConnection connection, SqlTransaction transaction, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT COALESCE(MAX(id), 0) + 1
            FROM firmalar;
            """;

        await using var command = transaction is null
            ? new SqlCommand(sql, connection)
            : new SqlCommand(sql, connection, transaction!);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        var nextId = Convert.ToInt64(result);
        return $"OTLTRZM-FRM-{nextId:0000}";
    }

    private static async Task<string> GenerateHotelCodeAsync(SqlConnection connection, SqlTransaction transaction, string city, CancellationToken cancellationToken)
    {
        var cityCode = NormalizeAscii(city).ToUpperInvariant();
        cityCode = string.IsNullOrWhiteSpace(cityCode) ? "TR" : cityCode[..Math.Min(3, cityCode.Length)];

        const string existsSql = """
            SELECT COUNT(*)
            FROM oteller
            WHERE UPPER(otel_kodu) = @hotelCode;
            """;

        for (var attempt = 0; attempt < 32; attempt++)
        {
            var uniquePart = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6", CultureInfo.InvariantCulture);
            var candidate = $"OTLTRZM-{cityCode}-{uniquePart}";

            await using var command = transaction is null
                ? new SqlCommand(existsSql, connection)
                : new SqlCommand(existsSql, connection, transaction!);
            command.Parameters.AddWithValue("@hotelCode", candidate);

            var exists = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture) > 0;
            if (!exists)
            {
                return candidate;
            }
        }

        throw new InvalidOperationException("Benzersiz otel kodu olusturulamadi. Lutfen tekrar deneyin.");
    }

    private static string NormalizeCodeInput(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return value
            .Trim()
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace("ç", "c", StringComparison.OrdinalIgnoreCase)
            .Replace("ğ", "g", StringComparison.OrdinalIgnoreCase)
            .Replace("ı", "i", StringComparison.OrdinalIgnoreCase)
            .Replace("İ", "I", StringComparison.Ordinal)
            .Replace("ö", "o", StringComparison.OrdinalIgnoreCase)
            .Replace("ş", "s", StringComparison.OrdinalIgnoreCase)
            .Replace("ü", "u", StringComparison.OrdinalIgnoreCase)
            .ToUpperInvariant();
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

        await using var command = transaction is null
            ? new SqlCommand(sql, connection)
            : new SqlCommand(sql, connection, transaction!);
        command.Parameters.AddWithValue("@tableName", tableName);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture) > 0;
    }

    private static string NormalizeAscii(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return value
            .Trim()
            .Replace("ç", "c", StringComparison.OrdinalIgnoreCase)
            .Replace("ğ", "g", StringComparison.OrdinalIgnoreCase)
            .Replace("ı", "i", StringComparison.OrdinalIgnoreCase)
            .Replace("İ", "I", StringComparison.Ordinal)
            .Replace("ö", "o", StringComparison.OrdinalIgnoreCase)
            .Replace("ş", "s", StringComparison.OrdinalIgnoreCase)
            .Replace("ü", "u", StringComparison.OrdinalIgnoreCase)
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal);
    }

    private sealed class AuthSchemaInfo
    {
        public bool HasUserRoleColumn { get; init; }
        public bool HasOwnershipPartnerColumn { get; init; }
        public bool HasFirmaIdColumn { get; init; }
        public bool HasSalesTeamColumn { get; init; }
        public bool HasHotelOwnershipTable { get; init; }
        public bool HasUsersPartnerTable { get; init; }
        public bool HasUsersPartnerPartnerIdColumn { get; init; }
        public bool HasUsersPartnerActiveColumn { get; init; }
        public bool HasUsersPartnerMainAccountColumn { get; init; }
        public bool HasUsersPartnerIdColumn { get; init; }
    }

    private sealed class AuthCandidateInfo
    {
        public long UserId { get; init; }
        public string Email { get; init; } = string.Empty;
        public string UserRole { get; init; } = "user";
        public bool EmailVerified { get; init; }
        public DateTime? LockoutEndUtc { get; init; }
    }
}
