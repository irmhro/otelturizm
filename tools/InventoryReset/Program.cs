using System.Globalization;
using System.Text.Json;
using MySqlConnector;

var rootPath = args.Length > 0 ? Path.GetFullPath(args[0]) : Directory.GetCurrentDirectory();
var appSettingsPath = Path.Combine(rootPath, "appsettings.json");
if (!File.Exists(appSettingsPath))
{
    throw new FileNotFoundException($"appsettings.json bulunamadi: {appSettingsPath}");
}

var connectionString = LoadConnectionString(appSettingsPath);
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("DefaultConnection baglanti bilgisi bulunamadi.");
}

var roomUploadRoot = Path.Combine(rootPath, "wwwroot", "uploads", "hotels", "partner");

await using var connection = new MySqlConnection(connectionString);
await connection.OpenAsync();
var existingTables = await LoadExistingTablesAsync(connection);

Console.WriteLine("Envanter reset basliyor...");
Console.WriteLine($"Calisma klasoru: {rootPath}");

var filesToDelete = await LoadRoomFilePathsAsync(connection, rootPath);
var roomDirectoriesToDelete = LoadRoomDirectories(roomUploadRoot);

var before = await LoadSummaryAsync(connection);
Console.WriteLine($"Oncesi -> Otel: {before.HotelCount}, Oda: {before.RoomCount}, Fiyat: {before.PriceCount}, Rezervasyon: {before.ReservationCount}, Taslak: {before.DraftCount}");

await DropReservationDeleteTriggerAsync(connection);

try
{
    await using (var transaction = await connection.BeginTransactionAsync())
    {
        await ExecuteAsync(connection, transaction, "SET FOREIGN_KEY_CHECKS=0;");

        await ExecuteIfTableExistsAsync(connection, transaction, existingTables, "faturalar", "UPDATE faturalar SET rezervasyon_id = NULL, odeme_islem_id = NULL;");
        await ExecuteIfTableExistsAsync(connection, transaction, existingTables, "mesaj_konusmalari", "UPDATE mesaj_konusmalari SET rezervasyon_id = NULL;");
        await ExecuteIfTableExistsAsync(connection, transaction, existingTables, "sepet_blokajlari", "UPDATE sepet_blokajlari SET rezervasyon_id = NULL;");
        await ExecuteIfTableExistsAsync(connection, transaction, existingTables, "basarisiz_odeme_denemeleri", "DELETE FROM basarisiz_odeme_denemeleri;");
        await ExecuteIfTableExistsAsync(connection, transaction, existingTables, "komisyon_muhasebe_kayitlari", "DELETE FROM komisyon_muhasebe_kayitlari;");
        await ExecuteIfTableExistsAsync(connection, transaction, existingTables, "odeme_islemleri", "DELETE FROM odeme_islemleri;");
        await ExecuteIfTableExistsAsync(connection, transaction, existingTables, "rezervasyon_taslaklari", "DELETE FROM rezervasyon_taslaklari;");
        await ExecuteIfTableExistsAsync(connection, transaction, existingTables, "rezervasyonlar", "DELETE FROM rezervasyonlar;");
        await ExecuteIfTableExistsAsync(connection, transaction, existingTables, "oda_fiyat_musaitlik", "DELETE FROM oda_fiyat_musaitlik;");
        await ExecuteIfTableExistsAsync(connection, transaction, existingTables, "oda_gorseller", "DELETE FROM oda_gorseller;");
        await ExecuteIfTableExistsAsync(connection, transaction, existingTables, "oda_tipi_ozellikleri", "DELETE FROM oda_tipi_ozellikleri;");
        await ExecuteIfTableExistsAsync(connection, transaction, existingTables, "firma_ozel_fiyatlar", "DELETE FROM firma_ozel_fiyatlar;");
        await ExecuteIfTableExistsAsync(connection, transaction, existingTables, "oda_tipleri", "DELETE FROM oda_tipleri;");

        await ExecuteAsync(connection, transaction, "SET FOREIGN_KEY_CHECKS=1;");
        await transaction.CommitAsync();
    }

    DeleteRoomArtifacts(filesToDelete, roomDirectoriesToDelete, rootPath);

    var featureIds = await EnsureStandardFeaturesAsync(connection);
    var hotelIds = await LoadHotelIdsAsync(connection);

    await using (var transaction = await connection.BeginTransactionAsync())
    {
        foreach (var hotelId in hotelIds)
        {
            var roomId = await InsertDefaultRoomAsync(connection, transaction, hotelId);
            await InsertDefaultRoomPricesAsync(connection, transaction, roomId, 30, 3500m, 5);
            await LinkRoomFeaturesAsync(connection, transaction, existingTables, roomId, featureIds);
        }

        await transaction.CommitAsync();
    }
}
finally
{
    await CreateReservationDeleteTriggerAsync(connection);
}

var after = await LoadSummaryAsync(connection);
Console.WriteLine($"Sonrasi -> Otel: {after.HotelCount}, Oda: {after.RoomCount}, Fiyat: {after.PriceCount}, Rezervasyon: {after.ReservationCount}, Taslak: {after.DraftCount}");
Console.WriteLine("Her otel icin Deluxe Oda ve 30 gunluk 3500 TL fiyat kaydi olusturuldu.");

static string? LoadConnectionString(string appSettingsPath)
{
    using var stream = File.OpenRead(appSettingsPath);
    using var document = JsonDocument.Parse(stream);
    if (document.RootElement.TryGetProperty("ConnectionStrings", out var connectionStrings)
        && connectionStrings.TryGetProperty("DefaultConnection", out var defaultConnection))
    {
        return defaultConnection.GetString();
    }

    return null;
}

static async Task ExecuteAsync(MySqlConnection connection, MySqlTransaction transaction, string sql)
{
    await using var command = new MySqlCommand(sql, connection, transaction);
    await command.ExecuteNonQueryAsync();
}

static async Task ExecuteIfTableExistsAsync(MySqlConnection connection, MySqlTransaction transaction, HashSet<string> existingTables, string tableName, string sql)
{
    if (!existingTables.Contains(tableName))
    {
        return;
    }

    await ExecuteAsync(connection, transaction, sql);
}

static async Task<ResetSummary> LoadSummaryAsync(MySqlConnection connection)
{
    return new ResetSummary(
        await CountRowsAsync(connection, "oteller"),
        await CountRowsAsync(connection, "oda_tipleri"),
        await CountRowsAsync(connection, "oda_fiyat_musaitlik"),
        await CountRowsAsync(connection, "rezervasyonlar"),
        await CountRowsAsync(connection, "rezervasyon_taslaklari"));
}

static async Task DropReservationDeleteTriggerAsync(MySqlConnection connection)
{
    await using var command = new MySqlCommand("DROP TRIGGER IF EXISTS tr_rezervasyonlar_prevent_delete;", connection);
    await command.ExecuteNonQueryAsync();
}

static async Task CreateReservationDeleteTriggerAsync(MySqlConnection connection)
{
    const string sql = """
        DROP TRIGGER IF EXISTS tr_rezervasyonlar_prevent_delete;
        CREATE TRIGGER tr_rezervasyonlar_prevent_delete
        BEFORE DELETE ON rezervasyonlar
        FOR EACH ROW
        SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'Rezervasyon kayitlari silinemez. Sadece iptal sureci kullanilabilir.';
        """;

    await using var command = new MySqlCommand(sql, connection);
    await command.ExecuteNonQueryAsync();
}

static async Task<List<string>> LoadRoomFilePathsAsync(MySqlConnection connection, string rootPath)
{
    var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    if (await TableExistsAsync(connection, "oda_tipleri"))
    {
        const string roomCoverSql = "SELECT kapak_fotografi FROM oda_tipleri WHERE kapak_fotografi IS NOT NULL AND kapak_fotografi <> '';";
        await AppendFilePathsAsync(connection, result, roomCoverSql, rootPath);
    }

    if (await TableExistsAsync(connection, "oda_gorseller"))
    {
        const string roomImageSql = """
            SELECT gorsel_url FROM oda_gorseller WHERE gorsel_url IS NOT NULL AND gorsel_url <> ''
            UNION
            SELECT thumbnail_url FROM oda_gorseller WHERE thumbnail_url IS NOT NULL AND thumbnail_url <> '';
            """;
        await AppendFilePathsAsync(connection, result, roomImageSql, rootPath);
    }

    return result.ToList();
}

static List<string> LoadRoomDirectories(string roomUploadRoot)
{
    if (!Directory.Exists(roomUploadRoot))
    {
        return new List<string>();
    }

    return Directory.GetDirectories(roomUploadRoot, "rooms", SearchOption.AllDirectories)
        .Where(path => path.StartsWith(roomUploadRoot, StringComparison.OrdinalIgnoreCase))
        .ToList();
}

static void DeleteRoomArtifacts(IEnumerable<string> filesToDelete, IEnumerable<string> roomDirectoriesToDelete, string rootPath)
{
    foreach (var file in filesToDelete)
    {
        try
        {
            if (File.Exists(file) && file.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase))
            {
                File.Delete(file);
            }
        }
        catch
        {
            // Test resetinde dosya silinemediyse devam et.
        }
    }

    foreach (var directory in roomDirectoriesToDelete.OrderByDescending(path => path.Length))
    {
        try
        {
            if (Directory.Exists(directory) && directory.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase))
            {
                Directory.Delete(directory, true);
            }
        }
        catch
        {
            // Kilitli veya halihazirda silinmis dizinlerde reseti durdurma.
        }
    }
}

static async Task<Dictionary<string, short>> EnsureStandardFeaturesAsync(MySqlConnection connection)
{
    if (!await TableExistsAsync(connection, "oda_ozellikleri"))
    {
        return new Dictionary<string, short>(StringComparer.OrdinalIgnoreCase);
    }

    var features = new[]
    {
        new StandardFeature("Genel", "Klima", "fa-wind", 10),
        new StandardFeature("Teknoloji", "Smart TV", "fa-tv", 20),
        new StandardFeature("Mutfak", "Kettle", "fa-mug-hot", 30),
        new StandardFeature("Banyo", "Havlu", "fa-soap", 40),
        new StandardFeature("Genel", "Wi-Fi", "fa-wifi", 50)
    };

    var result = new Dictionary<string, short>(StringComparer.OrdinalIgnoreCase);
    foreach (var feature in features)
    {
        const string selectSql = "SELECT id FROM oda_ozellikleri WHERE ozellik_adi = @name LIMIT 1;";
        await using var selectCommand = new MySqlCommand(selectSql, connection);
        selectCommand.Parameters.AddWithValue("@name", feature.Name);
        var existing = await selectCommand.ExecuteScalarAsync();
        if (existing is not null and not DBNull)
        {
            result[feature.Name] = Convert.ToInt16(existing, CultureInfo.InvariantCulture);
            continue;
        }

        const string insertSql = """
            INSERT INTO oda_ozellikleri (kategori, ozellik_adi, ozellik_ikon, siralama, aktif_mi)
            VALUES (@category, @name, @icon, @sortOrder, 1);
            SELECT LAST_INSERT_ID();
            """;
        await using var insertCommand = new MySqlCommand(insertSql, connection);
        insertCommand.Parameters.AddWithValue("@category", feature.Category);
        insertCommand.Parameters.AddWithValue("@name", feature.Name);
        insertCommand.Parameters.AddWithValue("@icon", feature.Icon);
        insertCommand.Parameters.AddWithValue("@sortOrder", feature.SortOrder);
        var inserted = await insertCommand.ExecuteScalarAsync();
        result[feature.Name] = Convert.ToInt16(inserted, CultureInfo.InvariantCulture);
    }

    return result;
}

static async Task<List<long>> LoadHotelIdsAsync(MySqlConnection connection)
{
    var result = new List<long>();
    if (!await TableExistsAsync(connection, "oteller"))
    {
        return result;
    }

    const string sql = "SELECT id FROM oteller ORDER BY id;";
    await using var command = new MySqlCommand(sql, connection);
    await using var reader = await command.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        result.Add(reader.GetInt64(0));
    }

    return result;
}

static async Task<long> InsertDefaultRoomAsync(MySqlConnection connection, MySqlTransaction transaction, long hotelId)
{
    const string sql = """
        INSERT INTO oda_tipleri
        (
            otel_id, oda_tip_kodu, oda_adi, oda_kategorisi,
            maksimum_kisi_sayisi, maksimum_yetiskin_sayisi, maksimum_cocuk_sayisi,
            yatak_tipi, yatak_sayisi, ek_yatak_eklenebilir_mi,
            oda_metrekare, balkon_var_mi, manzara_tipi,
            ozel_banyo_var_mi, banyo_tipi,
            standart_gecelik_fiyat, haftasonu_fark_orani, cocuk_indirim_orani,
            bebek_ucretsiz_mi, bebek_yas_siniri, cocuk_yas_siniri,
            toplam_oda_sayisi, overbooking_limit,
            ozellikler, aktif_mi, siralama
        )
        VALUES
        (
            @hotelId, @roomCode, 'Deluxe Oda', 'Deluxe',
            2, 2, 0,
            'Çift Kişilik', 1, 0,
            28, 0, 'Şehir',
            1, 'Duş',
            3500.00, 0.00, 0.00,
            1, 2, 12,
            5, 0,
            @featuresJson, 1, 1
        );
        SELECT LAST_INSERT_ID();
        """;

    var featuresJson = """
        {"klima":true,"smart_tv":true,"kettle":true,"havlu":true,"wifi":true}
        """;

    await using var command = new MySqlCommand(sql, connection, transaction);
    command.Parameters.AddWithValue("@hotelId", hotelId);
    command.Parameters.AddWithValue("@roomCode", $"DLX-{hotelId}");
    command.Parameters.AddWithValue("@featuresJson", featuresJson);
    var inserted = await command.ExecuteScalarAsync();
    return Convert.ToInt64(inserted, CultureInfo.InvariantCulture);
}

static async Task InsertDefaultRoomPricesAsync(MySqlConnection connection, MySqlTransaction transaction, long roomId, int dayCount, decimal nightlyPrice, short totalRooms)
{
    const string sql = """
        INSERT INTO oda_fiyat_musaitlik
        (
            oda_tip_id, tarih, gecelik_fiyat, indirimli_fiyat, kampanya_id,
            toplam_oda_sayisi, satilan_oda_sayisi, bloke_oda_sayisi,
            minimum_geceleme, maksimum_geceleme, kapali_satis, sadece_gunubirlik, iptal_politikasi_override
        )
        VALUES
        (
            @roomId, @date, @nightlyPrice, NULL, NULL,
            @totalRooms, 0, 0,
            1, 30, 0, 0, NULL
        );
        """;

    for (var i = 0; i < dayCount; i++)
    {
        await using var command = new MySqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("@roomId", roomId);
        command.Parameters.AddWithValue("@date", DateTime.Today.AddDays(i));
        command.Parameters.AddWithValue("@nightlyPrice", nightlyPrice);
        command.Parameters.AddWithValue("@totalRooms", totalRooms);
        await command.ExecuteNonQueryAsync();
    }
}

static async Task LinkRoomFeaturesAsync(MySqlConnection connection, MySqlTransaction transaction, HashSet<string> existingTables, long roomId, Dictionary<string, short> featureIds)
{
    if (featureIds.Count == 0 || !existingTables.Contains("oda_tipi_ozellikleri"))
    {
        return;
    }

    const string sql = """
        INSERT INTO oda_tipi_ozellikleri (oda_tip_id, ozellik_id, miktar)
        VALUES (@roomId, @featureId, 1)
        ON DUPLICATE KEY UPDATE miktar = VALUES(miktar);
        """;

    foreach (var featureId in featureIds.Values)
    {
        await using var command = new MySqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("@roomId", roomId);
        command.Parameters.AddWithValue("@featureId", featureId);
        await command.ExecuteNonQueryAsync();
    }
}

static async Task<int> CountRowsAsync(MySqlConnection connection, string tableName)
{
    if (!await TableExistsAsync(connection, tableName))
    {
        return 0;
    }

    await using var command = new MySqlCommand($"SELECT COUNT(*) FROM `{tableName}`;", connection);
    return Convert.ToInt32(await command.ExecuteScalarAsync() ?? 0, CultureInfo.InvariantCulture);
}

static async Task<bool> TableExistsAsync(MySqlConnection connection, string tableName)
{
    const string sql = """
        SELECT COUNT(*)
        FROM information_schema.TABLES
        WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = @tableName;
        """;
    await using var command = new MySqlCommand(sql, connection);
    command.Parameters.AddWithValue("@tableName", tableName);
    return Convert.ToInt32(await command.ExecuteScalarAsync() ?? 0, CultureInfo.InvariantCulture) > 0;
}

static async Task<HashSet<string>> LoadExistingTablesAsync(MySqlConnection connection)
{
    var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    const string sql = "SELECT TABLE_NAME FROM information_schema.TABLES WHERE TABLE_SCHEMA = DATABASE();";
    await using var command = new MySqlCommand(sql, connection);
    await using var reader = await command.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        result.Add(reader.GetString(0));
    }

    return result;
}

static async Task AppendFilePathsAsync(MySqlConnection connection, HashSet<string> result, string sql, string rootPath)
{
    await using var command = new MySqlCommand(sql, connection);
    await using var reader = await command.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        var path = reader.GetString(0);
        if (string.IsNullOrWhiteSpace(path))
        {
            continue;
        }

        var normalized = path.Replace('/', Path.DirectorySeparatorChar).TrimStart(Path.DirectorySeparatorChar);
        if (normalized.StartsWith("uploads", StringComparison.OrdinalIgnoreCase))
        {
            result.Add(Path.Combine(rootPath, "wwwroot", normalized));
        }
    }
}

record ResetSummary(int HotelCount, int RoomCount, int PriceCount, int ReservationCount, int DraftCount);
record StandardFeature(string Category, string Name, string Icon, int SortOrder);
