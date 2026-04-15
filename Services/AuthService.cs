using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using MySqlConnector;
using otelturizmnew.Models.Email;
using otelturizmnew.Models.Giris;
using otelturizmnew.Models.Register;
using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Services;

public class AuthService : IAuthService
{
    private readonly string _connectionString;
    private readonly IEmailQueueService _emailQueueService;
    private readonly string _publicBaseUrl;

    public AuthService(IConfiguration configuration, IEmailQueueService emailQueueService)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection tanimli degil.");
        _emailQueueService = emailQueueService;
        _publicBaseUrl = (configuration["App:PublicBaseUrl"] ?? "https://localhost:7223").TrimEnd('/');
    }

    public async Task<UserSessionModel?> AuthenticateUserAsync(string identity, string password, CancellationToken cancellationToken = default)
    {
        var candidate = await FindAuthCandidateAsync(identity, false, cancellationToken);
        if (candidate is not null && candidate.LockoutEndUtc.HasValue && candidate.LockoutEndUtc.Value > DateTime.UtcNow)
        {
            throw new AuthFlowException($"Bu hesap gecici olarak kilitlendi. Lutfen {candidate.LockoutEndUtc.Value.ToLocalTime():HH:mm} sonrasinda tekrar deneyin.");
        }

        var user = await GetUserAsync(identity, password, false, cancellationToken);
        if (user is null)
        {
            if (candidate is not null)
            {
                var lockoutEnd = await RegisterFailedLoginAttemptAsync(candidate.UserId, cancellationToken);
                if (lockoutEnd.HasValue && lockoutEnd.Value > DateTime.UtcNow)
                {
                    throw new AuthFlowException("Arka arkaya 5 hatali deneme algilandi. Hesap 10 dakika kilitlendi.");
                }
            }

            return null;
        }

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
            return user;
        }

        if (!await IsEmailVerifiedAsync(user.UserId, cancellationToken))
        {
            throw new AuthFlowException("E-posta adresinizi onaylamadan giris yapamazsiniz. Lütfen gelen kutunuzu kontrol edin veya doğrulama kodunu yeniden isteyin.");
        }

        user.AccountType = "user";
        return user;
    }

    public async Task<UserSessionModel?> AuthenticatePartnerAsync(string identity, string password, CancellationToken cancellationToken = default)
    {
        var candidate = await FindAuthCandidateAsync(identity, true, cancellationToken);
        if (candidate is not null && candidate.LockoutEndUtc.HasValue && candidate.LockoutEndUtc.Value > DateTime.UtcNow)
        {
            throw new AuthFlowException($"Bu hesap gecici olarak kilitlendi. Lutfen {candidate.LockoutEndUtc.Value.ToLocalTime():HH:mm} sonrasinda tekrar deneyin.");
        }

        var user = await GetUserAsync(identity, password, true, cancellationToken);
        if (user is null)
        {
            if (candidate is not null)
            {
                var lockoutEnd = await RegisterFailedLoginAttemptAsync(candidate.UserId, cancellationToken);
                if (lockoutEnd.HasValue && lockoutEnd.Value > DateTime.UtcNow)
                {
                    throw new AuthFlowException("Arka arkaya 5 hatali deneme algilandi. Hesap 10 dakika kilitlendi.");
                }
            }
            return null;
        }

        await ResetFailedLoginAttemptAsync(user.UserId, cancellationToken);
        user.AccountType = "partner";
        return user;
    }

    public async Task<UserSessionModel?> AuthenticateFirmaAsync(string identity, string password, CancellationToken cancellationToken = default)
    {
        var candidate = await FindAuthCandidateAsync(identity, false, cancellationToken);
        if (candidate is not null && candidate.LockoutEndUtc.HasValue && candidate.LockoutEndUtc.Value > DateTime.UtcNow)
        {
            throw new AuthFlowException($"Bu hesap gecici olarak kilitlendi. Lutfen {candidate.LockoutEndUtc.Value.ToLocalTime():HH:mm} sonrasinda tekrar deneyin.");
        }

        var user = await GetUserAsync(identity, password, false, cancellationToken);
        if (user is null || !string.Equals(user.AccountType, "firma", StringComparison.OrdinalIgnoreCase))
        {
            if (candidate is not null)
            {
                var lockoutEnd = await RegisterFailedLoginAttemptAsync(candidate.UserId, cancellationToken);
                if (lockoutEnd.HasValue && lockoutEnd.Value > DateTime.UtcNow)
                {
                    throw new AuthFlowException("Arka arkaya 5 hatali deneme algilandi. Hesap 10 dakika kilitlendi.");
                }
            }
            return null;
        }

        await ResetFailedLoginAttemptAsync(user.UserId, cancellationToken);
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

        if (!model.AcceptTerms)
        {
            return (false, "Kayit icin kullanim kosullari ve gizlilik onayi zorunludur.", null);
        }

        if (string.IsNullOrWhiteSpace(model.Password) || model.Password.Length < 4)
        {
            return (false, "Sifre en az 4 karakter olmalidir.", null);
        }

        if (!string.Equals(model.Password, model.ConfirmPassword, StringComparison.Ordinal))
        {
            return (false, "Sifre tekrari eslesmiyor.", null);
        }

        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var userColumns = await GetUsersTableColumnsAsync(connection, cancellationToken);

        const string existsSql = """
            SELECT
                SUM(CASE WHEN eposta = @email THEN 1 ELSE 0 END) AS email_count,
                SUM(CASE WHEN @phone IS NOT NULL AND telefon = @phone THEN 1 ELSE 0 END) AS phone_count
            FROM users;
            """;

        await using (var existsCommand = new MySqlCommand(existsSql, connection))
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
            "SHA2(@password, 256)",
            "1",
            "'tr'",
            "'TRY'",
            "'Turkiye'",
            "NOW()"
        };

        if (userColumns.Contains("kvkk_onay_tarihi"))
        {
            insertColumns.Add("kvkk_onay_tarihi");
            insertValues.Add("NOW()");
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

            SELECT LAST_INSERT_ID();
            """;

        long newUserId;
        try
        {
            await using var insertCommand = new MySqlCommand(insertSql, connection);
            insertCommand.Parameters.AddWithValue("@fullName", $"{firstName} {lastName}".Trim());
            insertCommand.Parameters.AddWithValue("@email", email);
            insertCommand.Parameters.AddWithValue("@phone", (object?)phone ?? DBNull.Value);
            insertCommand.Parameters.AddWithValue("@password", model.Password);
            insertCommand.Parameters.AddWithValue("@marketing", model.AcceptMarketing ? 1 : 0);

            var result = await insertCommand.ExecuteScalarAsync(cancellationToken);
            newUserId = Convert.ToInt64(result);
        }
        catch (MySqlException ex)
        {
            return (false, $"Kayit veritabani hatasi: {ex.Message}", null);
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

        if (!model.AcceptAgreement || !model.DeclareAccurate)
        {
            return (false, "Partner kaydi icin sozlesme ve beyan onaylari zorunludur.", null);
        }

        if (string.IsNullOrWhiteSpace(model.Password) || model.Password.Length < 4)
        {
            return (false, "Sifre en az 4 karakter olmalidir.", null);
        }

        if (!string.Equals(model.Password, model.ConfirmPassword, StringComparison.Ordinal))
        {
            return (false, "Sifre tekrari eslesmiyor.", null);
        }

        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var userColumns = await GetUsersTableColumnsAsync(connection, cancellationToken);

        const string existsSql = """
            SELECT
                (SELECT COUNT(*) FROM users WHERE eposta = @email) AS email_count,
                (SELECT COUNT(*) FROM users WHERE telefon = @phone) AS phone_count,
                (SELECT COUNT(*) FROM partner_detaylari WHERE vergi_numarasi = @taxNumber) AS tax_count,
                (SELECT COUNT(*) FROM partner_detaylari WHERE iban = @iban) AS iban_count;
            """;

        await using (var existsCommand = new MySqlCommand(existsSql, connection))
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
                "SHA2(@password, 256)",
                "1",
                "'tr'",
                "'TRY'",
                "'Turkiye'",
                "NOW()"
            };

            if (userColumns.Contains("kvkk_onay_tarihi"))
            {
                insertColumns.Add("kvkk_onay_tarihi");
                insertValues.Add("NOW()");
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

                SELECT LAST_INSERT_ID();
                """;

            long userId;
            await using (var insertUserCommand = new MySqlCommand(insertUserSql, connection, transaction))
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
                    'Onaylandi',
                    NOW(),
                    @website,
                    @description,
                    NOW()
                );

                SELECT LAST_INSERT_ID();
                """;

            long partnerId;
            await using (var insertPartnerCommand = new MySqlCommand(insertPartnerSql, connection, transaction))
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

            const string insertUserPartnerSql = """
                INSERT INTO users_partner
                (
                    user_id,
                    partner_id,
                    rol,
                    aktif_mi,
                    ana_hesap_mi,
                    olusturulma_tarihi
                )
                VALUES
                (
                    @userId,
                    @partnerId,
                    'owner',
                    1,
                    1,
                    NOW()
                );
                """;

            await using (var insertUserPartnerCommand = new MySqlCommand(insertUserPartnerSql, connection, transaction))
            {
                insertUserPartnerCommand.Parameters.AddWithValue("@userId", userId);
                insertUserPartnerCommand.Parameters.AddWithValue("@partnerId", partnerId);
                await insertUserPartnerCommand.ExecuteNonQueryAsync(cancellationToken);
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

                await using var updateOwnershipCommand = new MySqlCommand(updateOwnershipSql, connection, transaction);
                updateOwnershipCommand.Parameters.AddWithValue("@partnerId", partnerId);
                updateOwnershipCommand.Parameters.AddWithValue("@userId", userId);
                await updateOwnershipCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);

            var user = await GetUserByIdAsync(connection, userId, cancellationToken);
            return user is null
                ? (false, "Partner hesabi olusturuldu ancak oturum bilgisi hazirlanamadi.", null)
                : (true, "Partner kaydi tamamlandi.", user);
        }
        catch (MySqlException ex)
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

        if (string.IsNullOrWhiteSpace(model.Password) || model.Password.Length < 4)
        {
            return (false, "Şifre en az 4 karakter olmalıdır.", null);
        }

        if (!string.Equals(model.Password, model.ConfirmPassword, StringComparison.Ordinal))
        {
            return (false, "Şifre tekrarı eşleşmiyor.", null);
        }

        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var userColumns = await GetUsersTableColumnsAsync(connection, cancellationToken);

        const string existsSql = """
            SELECT
                (SELECT COUNT(*) FROM firmalar WHERE vergi_no = @taxNumber) AS tax_count,
                (SELECT COUNT(*) FROM firmalar WHERE firma_eposta = @companyEmail OR yetkili_eposta = @contactEmail) AS firm_email_count,
                (SELECT COUNT(*) FROM users WHERE eposta = @contactEmail) AS user_email_count;
            """;

        await using (var existsCommand = new MySqlCommand(existsSql, connection))
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
            var firmaCode = await GenerateFirmaCodeAsync(connection, transaction, cancellationToken);

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
                    @contactPhone, 'Beklemede', NOW(), 1, 0,
                    24, 'web_firma_register', NOW(), NOW(), @note, NOW()
                );

                SELECT LAST_INSERT_ID();
                """;

            long firmaId;
            await using (var insertFirmaCommand = new MySqlCommand(insertFirmaSql, connection, transaction))
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
                "SHA2(@password, 256)",
                "'firma_admin'",
                "@firmaId",
                "'Kurumsal Satın Alma'",
                "@contactTitle",
                "1",
                "1",
                "'tr'",
                "'TRY'",
                "'Türkiye'",
                "NOW()"
            };

            if (userColumns.Contains("kvkk_onay_tarihi"))
            {
                insertColumns.Add("kvkk_onay_tarihi");
                insertValues.Add("NOW()");
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

            await using (var insertUserCommand = new MySqlCommand(insertUserSql, connection, transaction))
            {
                insertUserCommand.Parameters.AddWithValue("@fullName", contactName);
                insertUserCommand.Parameters.AddWithValue("@contactEmail", contactEmail);
                insertUserCommand.Parameters.AddWithValue("@contactPhone", contactPhone);
                insertUserCommand.Parameters.AddWithValue("@password", model.Password);
                insertUserCommand.Parameters.AddWithValue("@firmaId", firmaId);
                insertUserCommand.Parameters.AddWithValue("@contactTitle", contactTitle);
                insertUserCommand.Parameters.AddWithValue("@personelCode", $"{firmaCode}-ADM");
                await insertUserCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            return (true, "Firma başvurunuz alındı. Yönetici onayı tamamlanınca giriş yapabilirsiniz.", null);
        }
        catch (MySqlException ex)
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

        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        const string sql = """
            SELECT id, kullanici_id, gecerlilik_suresi, kullanildi_mi, deneme_sayisi, maksimum_deneme, token
            FROM email_dogrulama_tokenlari
            WHERE eposta = @email
              AND dogrulama_kodu = @code
            ORDER BY olusturulma_tarihi DESC
            LIMIT 1;
            """;

        long tokenId;
        long userId;
        DateTime expiryUtc;
        bool used;
        int attemptCount;
        int maxAttempt;
        string storedToken;

        await using (var command = new MySqlCommand(sql, connection, transaction))
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
            await IncrementVerificationAttemptAsync(connection, transaction, tokenId, cancellationToken);
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

        await MarkVerificationTokenUsedAsync(connection, transaction, tokenId, cancellationToken);
        await using (var verifyUserCommand = new MySqlCommand("""
            UPDATE users
            SET email_dogrulama_tarihi = COALESCE(email_dogrulama_tarihi, UTC_TIMESTAMP()),
                email_dogrulama_son_gonderim_tarihi = UTC_TIMESTAMP()
            WHERE id = @userId;
            """, connection, transaction))
        {
            verifyUserCommand.Parameters.AddWithValue("@userId", userId);
            await verifyUserCommand.ExecuteNonQueryAsync(cancellationToken);
        }

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

        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string userSql = """
            SELECT id, ad_soyad, email_dogrulama_tarihi, email_dogrulama_son_gonderim_tarihi
            FROM users
            WHERE eposta = @email
              AND hesap_durumu = 1
            LIMIT 1;
            """;

        long userId;
        string fullName;
        DateTime? verifiedAt;
        DateTime? lastSentAt;
        await using (var command = new MySqlCommand(userSql, connection))
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

        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string userSql = """
            SELECT id, ad_soyad
            FROM users
            WHERE eposta = @email
              AND hesap_durumu = 1
            LIMIT 1;
            """;

        long userId;
        string fullName;
        await using (var command = new MySqlCommand(userSql, connection))
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

        await using (var insertCommand = new MySqlCommand("""
            INSERT INTO sifre_sifirlama_tokenlari
            (kullanici_id, eposta, token, ip_adresi, user_agent, kullanildi_mi, gecerlilik_suresi, olusturulma_tarihi)
            VALUES
            (@userId, @email, @token, @ipAddress, @userAgent, 0, DATE_ADD(UTC_TIMESTAMP(), INTERVAL 1 HOUR), UTC_TIMESTAMP());
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

        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 8)
        {
            return (false, "Yeni şifre en az 8 karakter olmalıdır.");
        }

        if (!string.Equals(newPassword, confirmPassword, StringComparison.Ordinal))
        {
            return (false, "Şifre tekrarı eşleşmiyor.");
        }

        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        long tokenId;
        long userId;
        bool used;
        DateTime expiryUtc;
        await using (var command = new MySqlCommand("""
            SELECT id, kullanici_id, kullanildi_mi, gecerlilik_suresi
            FROM sifre_sifirlama_tokenlari
            WHERE token = @token
            LIMIT 1;
            """, connection, transaction))
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

        await using (var updateUserCommand = new MySqlCommand("""
            UPDATE users
            SET sifre = SHA2(@password, 256),
                basarisiz_giris_sayisi = 0,
                son_basarisiz_giris_tarihi = NULL,
                giris_kilit_bitis_tarihi = NULL
            WHERE id = @userId;
            """, connection, transaction))
        {
            updateUserCommand.Parameters.AddWithValue("@password", newPassword);
            updateUserCommand.Parameters.AddWithValue("@userId", userId);
            await updateUserCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var updateTokenCommand = new MySqlCommand("""
            UPDATE sifre_sifirlama_tokenlari
            SET kullanildi_mi = 1,
                kullanilma_tarihi = UTC_TIMESTAMP()
            WHERE id = @tokenId;
            """, connection, transaction))
        {
            updateTokenCommand.Parameters.AddWithValue("@tokenId", tokenId);
            await updateTokenCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return (true, "Şifreniz başarıyla güncellendi. Yeni şifrenizle giriş yapabilirsiniz.");
    }

    private async Task<UserSessionModel?> GetUserAsync(string identity, string password, bool requirePartner, CancellationToken cancellationToken)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var authSchema = await GetAuthSchemaAsync(connection, cancellationToken);

        var roleSelect = authSchema.HasUserRoleColumn
            ? "u.rol"
            : "'user'";

        var ownershipPartnerSelect = authSchema.HasOwnershipPartnerColumn
            ? "u.sahiplik_partner_id"
            : "NULL";

        var partnerIdSelect = authSchema.HasOwnershipPartnerColumn
            ? "COALESCE(up.partner_id, u.sahiplik_partner_id)"
            : "up.partner_id";

        var managedHotelSelect = authSchema.HasHotelOwnershipTable
            ? """
                (
                    SELECT GROUP_CONCAT(DISTINCT oku.otel_id ORDER BY oku.otel_id)
                    FROM otel_kullanici_sahiplikleri oku
                    WHERE oku.user_id = u.id
                      AND oku.aktif_mi = 1
                )
                """
            : "NULL";

        var sql = $"""
            SELECT
                u.id,
                u.ad_soyad,
                u.eposta,
                {partnerIdSelect} AS partner_id,
                {roleSelect} AS user_role,
                {ownershipPartnerSelect} AS sahiplik_partner_id,
                {managedHotelSelect} AS managed_hotel_ids,
                MAX(COALESCE(up.ana_hesap_mi, 0)) AS ana_hesap_mi_order,
                MIN(COALESCE(up.id, 0)) AS user_partner_row_id,
                GROUP_CONCAT(DISTINCT r.rol_kodu) AS role_codes
            FROM users u
            LEFT JOIN users_partner up
                ON up.user_id = u.id
               AND up.aktif_mi = 1
            LEFT JOIN kullanici_rolleri kr
                ON kr.kullanici_id = u.id
               AND (kr.bitis_tarihi IS NULL OR kr.bitis_tarihi > NOW())
            LEFT JOIN roller r
                ON r.id = kr.rol_id
            WHERE u.hesap_durumu = 1
              AND u.sifre = SHA2(@password, 256)
              AND (
                    u.eposta = @identity
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
              AND (@requirePartner = 0 OR {partnerIdSelect} IS NOT NULL)
            GROUP BY u.id, u.ad_soyad, u.eposta, {partnerIdSelect}
            ORDER BY ana_hesap_mi_order DESC, user_partner_row_id ASC
            LIMIT 1;
            """;

        await using var command = new MySqlCommand(sql, connection);
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
        else if (session.UserRole.StartsWith("firma_", StringComparison.OrdinalIgnoreCase))
        {
            session.AccountType = "firma";
        }
        else if (session.UserRole.StartsWith("sales_", StringComparison.OrdinalIgnoreCase))
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

        var normalized = identity.Trim().ToUpperInvariant();
        return normalized.StartsWith("OTL", StringComparison.OrdinalIgnoreCase)
            ? normalized
            : null;
    }

    private static async Task<UserSessionModel?> GetUserByIdAsync(MySqlConnection connection, long userId, CancellationToken cancellationToken)
    {
        var authSchema = await GetAuthSchemaAsync(connection, cancellationToken);

        var roleSelect = authSchema.HasUserRoleColumn
            ? "u.rol"
            : "'user'";

        var ownershipPartnerSelect = authSchema.HasOwnershipPartnerColumn
            ? "u.sahiplik_partner_id"
            : "NULL";

        var partnerIdSelect = authSchema.HasOwnershipPartnerColumn
            ? "COALESCE(up.partner_id, u.sahiplik_partner_id)"
            : "up.partner_id";

        var managedHotelSelect = authSchema.HasHotelOwnershipTable
            ? """
                (
                    SELECT GROUP_CONCAT(DISTINCT oku.otel_id ORDER BY oku.otel_id)
                    FROM otel_kullanici_sahiplikleri oku
                    WHERE oku.user_id = u.id
                      AND oku.aktif_mi = 1
                )
                """
            : "NULL";

        var sql = $"""
            SELECT
                u.id,
                u.ad_soyad,
                u.eposta,
                {partnerIdSelect} AS partner_id,
                {roleSelect} AS user_role,
                {ownershipPartnerSelect} AS sahiplik_partner_id,
                {managedHotelSelect} AS managed_hotel_ids,
                MAX(COALESCE(up.ana_hesap_mi, 0)) AS ana_hesap_mi_order,
                MIN(COALESCE(up.id, 0)) AS user_partner_row_id,
                GROUP_CONCAT(DISTINCT r.rol_kodu) AS role_codes
            FROM users u
            LEFT JOIN users_partner up
                ON up.user_id = u.id
               AND up.aktif_mi = 1
            LEFT JOIN kullanici_rolleri kr
                ON kr.kullanici_id = u.id
               AND (kr.bitis_tarihi IS NULL OR kr.bitis_tarihi > NOW())
            LEFT JOIN roller r
                ON r.id = kr.rol_id
            WHERE u.id = @userId
            GROUP BY u.id, u.ad_soyad, u.eposta, {partnerIdSelect}
            ORDER BY ana_hesap_mi_order DESC, user_partner_row_id ASC
            LIMIT 1;
            """;

        await using var command = new MySqlCommand(sql, connection);
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
                : (userRole.StartsWith("firma_", StringComparison.OrdinalIgnoreCase)
                    ? "firma"
                    : (userRole.StartsWith("sales_", StringComparison.OrdinalIgnoreCase)
                        ? "sales"
                    : (!reader.IsDBNull(partnerIdOrdinal) || userRole.StartsWith("partner_", StringComparison.OrdinalIgnoreCase) ? "partner" : "user"))
                  )
        };
    }

    private static async Task<HashSet<string>> GetUsersTableColumnsAsync(MySqlConnection connection, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT COLUMN_NAME
            FROM information_schema.COLUMNS
            WHERE TABLE_SCHEMA = DATABASE()
              AND TABLE_NAME = 'users';
            """;

        var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        await using var command = new MySqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            columns.Add(reader.GetString(0));
        }

        return columns;
    }

    private static async Task<AuthSchemaInfo> GetAuthSchemaAsync(MySqlConnection connection, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                SUM(CASE WHEN TABLE_NAME = 'users' AND COLUMN_NAME = 'rol' THEN 1 ELSE 0 END) AS has_user_role_column,
                SUM(CASE WHEN TABLE_NAME = 'users' AND COLUMN_NAME = 'sahiplik_partner_id' THEN 1 ELSE 0 END) AS has_ownership_partner_column,
                SUM(CASE WHEN TABLE_NAME = 'otel_kullanici_sahiplikleri' THEN 1 ELSE 0 END) AS has_hotel_ownership_table
            FROM information_schema.COLUMNS
            WHERE TABLE_SCHEMA = DATABASE()
              AND (
                    (TABLE_NAME = 'users' AND COLUMN_NAME IN ('rol', 'sahiplik_partner_id'))
                 OR TABLE_NAME = 'otel_kullanici_sahiplikleri'
              );
            """;

        await using var command = new MySqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            return new AuthSchemaInfo();
        }

        return new AuthSchemaInfo
        {
            HasUserRoleColumn = !reader.IsDBNull(0) && reader.GetInt64(0) > 0,
            HasOwnershipPartnerColumn = !reader.IsDBNull(1) && reader.GetInt64(1) > 0,
            HasHotelOwnershipTable = !reader.IsDBNull(2) && reader.GetInt64(2) > 0
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

    private static List<long> ParseManagedHotelIds(MySqlDataReader reader, int ordinal)
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
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = """
            SELECT COUNT(*)
            FROM users u
            INNER JOIN firmalar f
                ON f.id = u.firma_id
            WHERE u.id = @userId
              AND u.hesap_durumu = 1
              AND f.aktif_mi = 1
              AND f.onay_durumu = 'Onaylandı'
              AND COALESCE(f.giris_izni_aktif_mi, 0) = 1;
            """;

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@userId", userId);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result) > 0;
    }

    private async Task<AuthCandidateInfo?> FindAuthCandidateAsync(string identity, bool requirePartner, CancellationToken cancellationToken)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var authSchema = await GetAuthSchemaAsync(connection, cancellationToken);
        var roleSelect = authSchema.HasUserRoleColumn ? "u.rol" : "'user'";
        var ownershipPartnerSelect = authSchema.HasOwnershipPartnerColumn ? "u.sahiplik_partner_id" : "NULL";
        var partnerIdSelect = authSchema.HasOwnershipPartnerColumn ? "COALESCE(up.partner_id, u.sahiplik_partner_id)" : "up.partner_id";

        var sql = $"""
            SELECT
                u.id,
                u.eposta,
                {roleSelect} AS user_role,
                u.email_dogrulama_tarihi,
                u.giris_kilit_bitis_tarihi,
                {partnerIdSelect} AS partner_id,
                {ownershipPartnerSelect} AS sahiplik_partner_id
            FROM users u
            LEFT JOIN users_partner up
                ON up.user_id = u.id
               AND up.aktif_mi = 1
            WHERE u.hesap_durumu = 1
              AND (
                    u.eposta = @identity
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
              AND (@requirePartner = 0 OR {partnerIdSelect} IS NOT NULL)
            ORDER BY u.id ASC
            LIMIT 1;
            """;

        await using var command = new MySqlCommand(sql, connection);
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
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = """
            UPDATE users
            SET basarisiz_giris_sayisi = COALESCE(basarisiz_giris_sayisi, 0) + 1,
                son_basarisiz_giris_tarihi = UTC_TIMESTAMP(),
                giris_kilit_bitis_tarihi = CASE
                    WHEN COALESCE(basarisiz_giris_sayisi, 0) + 1 >= 5 THEN DATE_ADD(UTC_TIMESTAMP(), INTERVAL 10 MINUTE)
                    ELSE giris_kilit_bitis_tarihi
                END
            WHERE id = @userId;
            """;

        await using (var command = new MySqlCommand(sql, connection))
        {
            command.Parameters.AddWithValue("@userId", userId);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        await using var readCommand = new MySqlCommand("SELECT giris_kilit_bitis_tarihi FROM users WHERE id = @userId LIMIT 1;", connection);
        readCommand.Parameters.AddWithValue("@userId", userId);
        var result = await readCommand.ExecuteScalarAsync(cancellationToken);
        return result is DBNull or null ? null : Convert.ToDateTime(result, CultureInfo.InvariantCulture);
    }

    private async Task ResetFailedLoginAttemptAsync(long userId, CancellationToken cancellationToken)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new MySqlCommand("""
            UPDATE users
            SET basarisiz_giris_sayisi = 0,
                son_basarisiz_giris_tarihi = NULL,
                giris_kilit_bitis_tarihi = NULL,
                son_giris_tarihi = UTC_TIMESTAMP()
            WHERE id = @userId;
            """, connection);
        command.Parameters.AddWithValue("@userId", userId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task<bool> IsEmailVerifiedAsync(long userId, CancellationToken cancellationToken)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new MySqlCommand("SELECT email_dogrulama_tarihi FROM users WHERE id = @userId LIMIT 1;", connection);
        command.Parameters.AddWithValue("@userId", userId);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is not null && result is not DBNull;
    }

    private async Task CreateAndQueueEmailVerificationAsync(
        MySqlConnection connection,
        MySqlTransaction? transaction,
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

        await using (var insertCommand = new MySqlCommand("""
            INSERT INTO email_dogrulama_tokenlari
            (kullanici_id, eposta, token, dogrulama_kodu, kullanildi_mi, deneme_sayisi, maksimum_deneme, ip_adresi, user_agent, gecerlilik_suresi, olusturulma_tarihi)
            VALUES
            (@userId, @email, @token, @code, 0, 0, 5, @ipAddress, @userAgent, DATE_ADD(UTC_TIMESTAMP(), INTERVAL 24 HOUR), UTC_TIMESTAMP());
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

        await using (var updateUserCommand = new MySqlCommand("""
            UPDATE users
            SET email_dogrulama_son_gonderim_tarihi = UTC_TIMESTAMP()
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

    private static async Task MarkVerificationTokenUsedAsync(MySqlConnection connection, MySqlTransaction transaction, long tokenId, CancellationToken cancellationToken)
    {
        await using var command = new MySqlCommand("""
            UPDATE email_dogrulama_tokenlari
            SET kullanildi_mi = 1,
                kullanilma_tarihi = UTC_TIMESTAMP()
            WHERE id = @tokenId;
            """, connection, transaction);
        command.Parameters.AddWithValue("@tokenId", tokenId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task IncrementVerificationAttemptAsync(MySqlConnection connection, MySqlTransaction transaction, long tokenId, CancellationToken cancellationToken)
    {
        await using var command = new MySqlCommand("""
            UPDATE email_dogrulama_tokenlari
            SET deneme_sayisi = COALESCE(deneme_sayisi, 0) + 1
            WHERE id = @tokenId;
            """, connection, transaction);
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

    private static async Task<string> GenerateFirmaCodeAsync(MySqlConnection connection, MySqlTransaction transaction, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT COALESCE(MAX(id), 0) + 1
            FROM firmalar;
            """;

        await using var command = new MySqlCommand(sql, connection, transaction);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        var nextId = Convert.ToInt64(result);
        return $"OTLTRZM-FRM-{nextId:0000}";
    }

    private sealed class AuthSchemaInfo
    {
        public bool HasUserRoleColumn { get; init; }
        public bool HasOwnershipPartnerColumn { get; init; }
        public bool HasHotelOwnershipTable { get; init; }
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

