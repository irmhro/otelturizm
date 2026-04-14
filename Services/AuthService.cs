using Microsoft.Extensions.Configuration;
using MySqlConnector;
using otelturizmnew.Models.Giris;
using otelturizmnew.Models.Register;
using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Services;

public class AuthService : IAuthService
{
    private readonly string _connectionString;

    public AuthService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection tanimli degil.");
    }

    public async Task<UserSessionModel?> AuthenticateUserAsync(string identity, string password, CancellationToken cancellationToken = default)
    {
        var user = await GetUserAsync(identity, password, false, cancellationToken);
        if (user is null)
        {
            return null;
        }

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

        user.AccountType = "user";
        return user;
    }

    public async Task<UserSessionModel?> AuthenticatePartnerAsync(string identity, string password, CancellationToken cancellationToken = default)
    {
        var user = await GetUserAsync(identity, password, true, cancellationToken);
        if (user is null)
        {
            return null;
        }

        user.AccountType = "partner";
        return user;
    }

    public async Task<UserSessionModel?> AuthenticateFirmaAsync(string identity, string password, CancellationToken cancellationToken = default)
    {
        var user = await GetUserAsync(identity, password, false, cancellationToken);
        if (user is null || !string.Equals(user.AccountType, "firma", StringComparison.OrdinalIgnoreCase))
        {
            return null;
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
        return user is null
            ? (false, "Kayit olusturuldu ancak oturum bilgisi alinamadi.", null)
            : (true, "Kayit basariyla tamamlandi.", user);
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

        if (string.Equals(session.UserRole, "admin", StringComparison.OrdinalIgnoreCase) || roles.Count > 0)
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
            AccountType = string.Equals(userRole, "admin", StringComparison.OrdinalIgnoreCase) || roles.Count > 0
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
}

