using System.Globalization;
using System.Text.Json;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.SqlClient;
using SqlConnection = Microsoft.Data.SqlClient.SqlConnection;
using SqlCommand = Microsoft.Data.SqlClient.SqlCommand;
using SqlTransaction = Microsoft.Data.SqlClient.SqlTransaction;
using SqlException = Microsoft.Data.SqlClient.SqlException;
using otelturizmnew.Models.Paneller.Admin;
using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Services;

public class AdminHotelManagementService : IAdminHotelManagementService
{
    private readonly string _connectionString;
    private readonly IWebHostEnvironment _environment;
    private readonly IImageStorageService _imageStorageService;

    public AdminHotelManagementService(IConfiguration configuration, IWebHostEnvironment environment, IImageStorageService imageStorageService)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection tanimli degil.");
        _environment = environment;
        _imageStorageService = imageStorageService;
    }

    public async Task<AdminHotelsPageViewModel> GetHotelsPageAsync(string fullName, string email, string userRole, string? searchTerm = null, string? city = null, string? district = null, string? neighborhood = null, string? publishStatus = null, string? approvalStatus = null, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var shell = await BuildShellAsync(connection, "Oteller", "Otel kayitlarini, odalari ve medya varliklarini admin tarafindan hizli sekilde yonetin.", fullName, email, userRole, cancellationToken);
        var safePageSize = Math.Clamp(pageSize, 7, 100);
        var safePage = Math.Max(1, page);
        var model = new AdminHotelsPageViewModel
        {
            Shell = shell,
            SearchTerm = searchTerm?.Trim() ?? string.Empty,
            CityFilter = city?.Trim() ?? string.Empty,
            DistrictFilter = district?.Trim() ?? string.Empty,
            NeighborhoodFilter = neighborhood?.Trim() ?? string.Empty,
            PublishStatusFilter = publishStatus?.Trim() ?? string.Empty,
            ApprovalStatusFilter = approvalStatus?.Trim() ?? string.Empty,
            Page = safePage,
            PageSize = safePageSize
        };

        model.SummaryCards.AddRange(await LoadHotelSummaryCardsAsync(connection, cancellationToken));
        await LoadHotelFilterOptionsAsync(connection, model, cancellationToken);

        const string whereSql = @"
            WHERE (@search = '' OR o.[OTEL_ADI] LIKE '%' + @search + '%' OR o.[OTEL_KODU] LIKE '%' + @search + '%' OR o.[SEHIR] LIKE '%' + @search + '%' OR o.[ILCE] LIKE '%' + @search + '%' OR COALESCE(o.[MAHALLE], '') LIKE '%' + @search + '%')
              AND (@city = '' OR o.[SEHIR] = @city)
              AND (@district = '' OR o.[ILCE] = @district)
              AND (@neighborhood = '' OR COALESCE(o.[MAHALLE], '') = @neighborhood)
              AND (@publishStatus = '' OR o.[YAYIN_DURUMU] = @publishStatus)
              AND (@approvalStatus = '' OR o.[ONAY_DURUMU] = @approvalStatus)";

        await using (var countCommand = new SqlCommand($"SELECT COUNT(*) FROM [dbo].[OTELLER] o {whereSql};", connection))
        {
            BindHotelListFilters(countCommand, model);
            model.TotalCount = Convert.ToInt32(await countCommand.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture);
        }

        if (model.TotalCount > 0 && model.Page > model.TotalPages)
        {
            model.Page = model.TotalPages;
        }

        const string selectSql = @"
            SELECT o.id,
                   o.[OTEL_KODU],
                   o.[OTEL_ADI],
                   o.[OTEL_TURU],
                   CONCAT(COALESCE(NULLIF(o.[MAHALLE], ''), o.[ILCE]), ', ', o.[ILCE], ', ', o.[SEHIR]) AS konum,
                   o.[YAYIN_DURUMU],
                   o.[ONAY_DURUMU],
                   o.[ORTALAMA_PUAN],
                   COALESCE(rooms.room_count, 0) AS room_count,
                   COALESCE(hotelPhotos.hotel_photo_count, 0) AS hotel_photo_count,
                   COALESCE(roomPhotos.room_photo_count, 0) AS room_photo_count,
                   o.[ONE_CIKAN_OTEL]
            FROM [dbo].[OTELLER] o
            LEFT JOIN (
                SELECT [OTEL_ID], COUNT(*) AS room_count
                FROM [dbo].[ODA_TIPLERI]
                GROUP BY [OTEL_ID]
            ) rooms ON rooms.[OTEL_ID] = o.id
            LEFT JOIN (
                SELECT [OTEL_ID], COUNT(*) AS hotel_photo_count
                FROM [dbo].[OTEL_GORSELLERI]
                GROUP BY [OTEL_ID]
            ) hotelPhotos ON hotelPhotos.[OTEL_ID] = o.id
            LEFT JOIN (
                SELECT od.[OTEL_ID], COUNT(og.id) AS room_photo_count
                FROM [dbo].[ODA_TIPLERI] od
                LEFT JOIN [dbo].[ODA_GORSELLERI] og ON og.[ODA_TIP_ID] = od.id
                GROUP BY od.[OTEL_ID]
            ) roomPhotos ON roomPhotos.[OTEL_ID] = o.id";

        var sql = $@"
            {selectSql}
            {whereSql}
            ORDER BY o.id DESC
            OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY;";

        await using var command = new SqlCommand(sql, connection);
        BindHotelListFilters(command, model);
        command.Parameters.AddWithValue("@offset", (model.Page - 1) * model.PageSize);
        command.Parameters.AddWithValue("@pageSize", model.PageSize);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            model.Hotels.Add(new AdminHotelListItemViewModel
            {
                HotelId = reader.GetInt64(0),
                HotelCode = reader.GetString(1),
                HotelName = reader.GetString(2),
                HotelType = reader.GetString(3),
                LocationText = reader.GetString(4),
                PublishStatus = reader.GetString(5),
                ApprovalStatus = reader.GetString(6),
                ScoreText = reader.IsDBNull(7) ? "0.0" : reader.GetDecimal(7).ToString("0.0", CultureInfo.InvariantCulture),
                RoomCount = SafeInt(reader, 8),
                HotelPhotoCount = SafeInt(reader, 9),
                RoomPhotoCount = SafeInt(reader, 10),
                IsFeatured = SafeBool(reader, 11)
            });
        }

        return model;
    }

    public async Task<AdminHotelManagementPageViewModel> GetHotelManagementPageAsync(long hotelId, string fullName, string email, string userRole, long? roomId = null, long? hotelPhotoId = null, long? roomPhotoId = null, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var hotel = await EnsureHotelExistsAsync(connection, hotelId, cancellationToken);
        var shell = await BuildShellAsync(connection, $"Otel Yonetimi · {hotel.HotelName}", "Tek ekranda otel kaydi, odalar, otel gorselleri ve oda gorsellerini yonetin.", fullName, email, userRole, cancellationToken);

        var model = new AdminHotelManagementPageViewModel
        {
            Shell = shell,
            HotelForm = await LoadHotelFormAsync(connection, hotelId, cancellationToken),
            Rooms = await LoadRoomCardsAsync(connection, hotelId, cancellationToken),
            RoomForm = roomId.HasValue ? await LoadRoomFormAsync(connection, hotelId, roomId.Value, cancellationToken) : new AdminRoomEditForm { HotelId = hotelId, IsActive = true, BabyFree = true, PrivateBathroom = true },
            HotelPhotos = await LoadHotelPhotosAsync(connection, hotelId, cancellationToken),
            HotelPhotoUploadForm = new AdminHotelPhotoUploadForm { HotelId = hotelId, PhotoType = "Genel Alan", DisplayOrder = 0 },
            HotelPhotoEditForm = hotelPhotoId.HasValue ? await LoadHotelPhotoEditFormAsync(connection, hotelId, hotelPhotoId.Value, cancellationToken) : new AdminHotelPhotoEditForm { HotelId = hotelId, PhotoType = "Genel Alan" }
        };

        model.SummaryCards.AddRange(await LoadHotelManagementCardsAsync(connection, hotelId, cancellationToken));

        var selectedRoomId = roomId ?? model.Rooms.FirstOrDefault()?.RoomId;
        model.SelectedRoomId = selectedRoomId;
        if (selectedRoomId.HasValue)
        {
            model.SelectedRoomName = model.Rooms.FirstOrDefault(x => x.RoomId == selectedRoomId.Value)?.RoomName ?? string.Empty;
            model.RoomPhotos = await LoadRoomPhotosAsync(connection, selectedRoomId.Value, cancellationToken);
            model.RoomPhotoUploadForm = new AdminRoomPhotoUploadForm { HotelId = hotelId, RoomId = selectedRoomId.Value };
            model.RoomPhotoEditForm = roomPhotoId.HasValue
                ? await LoadRoomPhotoEditFormAsync(connection, selectedRoomId.Value, roomPhotoId.Value, cancellationToken)
                : new AdminRoomPhotoEditForm { HotelId = hotelId, RoomId = selectedRoomId.Value };
        }
        else
        {
            model.RoomPhotoUploadForm = new AdminRoomPhotoUploadForm { HotelId = hotelId };
            model.RoomPhotoEditForm = new AdminRoomPhotoEditForm { HotelId = hotelId };
        }

        return model;
    }

    public async Task<(bool Success, string Message)> SaveHotelAsync(long adminUserId, AdminHotelEditForm request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.HotelName) || string.IsNullOrWhiteSpace(request.HotelCode))
        {
            return (false, "Otel kodu ve otel adi zorunludur.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await EnsureHotelExistsAsync(connection, request.HotelId, cancellationToken);
        var (previousLatitude, previousLongitude) = await LoadHotelCoordinatesAsync(connection, request.HotelId, cancellationToken);

        const string sql = @"
            UPDATE [dbo].[OTELLER]
            SET [OTEL_KODU] = @hotelCode,
                [PARTNER_ID] = @partnerId,
                [KULLANICI_ID] = @userId,
                [OTEL_ADI] = @hotelName,
                [OTEL_TURU] = @hotelType,
                [YILDIZ_SAYISI] = @starCount,
                [TURIZM_BELGE_NO] = @tourismDocumentNo,
                [TURIZM_BELGE_TURU] = @tourismDocumentType,
                ulke = @country,
                [SEHIR] = @city,
                ilce = @district,
                [MAHALLE] = @neighborhood,
                [TAM_ADRES] = @address,
                [POSTA_KODU] = @postalCode,
                [ENLEM] = @latitude,
                [BOYLAM] = @longitude,
                [TELEFON_1] = @phone1,
                [TELEFON_2] = @phone2,
                faks = @fax,
                [EPOSTA] = @contactEmail,
                [WEB_SITESI] = @website,
                [REZERVASYON_TELEFONU] = @reservationPhone,
                [SATIS_KONTAK_ADI] = @salesContactName,
                [SATIS_KONTAK_TELEFONU] = @salesContactPhone,
                [SATIS_KONTAK_EPOSTA] = @salesContactEmail,
                [SATIS_NOTLARI] = @salesNotes,
                [CHECK_IN_SAATI] = @checkInTime,
                [CHECK_OUT_SAATI] = @checkOutTime,
                [GEC_CHECK_OUT_MUMKUN_MU] = @lateCheckoutAvailable,
                [GEC_CHECK_OUT_UCRETI] = @lateCheckoutFee,
                [ERKEN_CHECK_IN_MUMKUN_MU] = @earlyCheckInAvailable,
                [ERKEN_CHECK_IN_UCRETI] = @earlyCheckInFee,
                [TOPLAM_ODA_SAYISI] = @totalRoomCount,
                [TOPLAM_YATAK_KAPASITESI] = @totalBedCapacity,
                [KAT_SAYISI] = @floorCount,
                [ASANSOR_VAR_MI] = @elevatorAvailable,
                [ASANSOR_SAYISI] = @elevatorCount,
                [KISA_ACIKLAMA] = @shortDescription,
                [UZUN_ACIKLAMA] = @description,
                [KONUM_ACIKLAMASI] = @locationDescription,
                [KOMISYON_TURU] = @commissionType,
                [VARSAYILAN_KOMISYON_ORANI] = @defaultCommissionRate,
                [KOMISYON_HESAPLAMA_TIPI] = @commissionCalculationType,
                [ODEME_VADESI] = @paymentTerm,
                [ODEME_YONTEMI] = @paymentMethod,
                [FATURA_KESIM_TURU] = @invoiceType,
                [DEPOZITO_TUTARI] = @depositAmount,
                [DEPOZITO_IADE_SURESI] = @depositReturnDays,
                [MINIMUM_KONAKLAMA_GECESI] = @minStay,
                [MAKSIMUM_KONAKLAMA_GECESI] = @maxStay,
                [KONUSULAN_DILLER] = @spokenLanguages,
                [ORTALAMA_PUAN] = @averageScore,
                [TOPLAM_YORUM_SAYISI] = @totalReviewCount,
                [TEMIZLIK_PUANI] = @cleanlinessScore,
                [KONFOR_PUANI] = @comfortScore,
                [KONUM_PUANI] = @locationScore,
                [PERSONEL_PUANI] = @staffScore,
                [FIYAT_PERFORMANS_PUANI] = @pricePerformanceScore,
                [KAPAK_FOTOGRAFI] = @coverPhotoPath,
                [VIDEO_URL] = @videoUrl,
                [SANAL_TUR_URL] = @virtualTourUrl,
                [YAYIN_DURUMU] = @publishStatus,
                [ONAY_DURUMU] = @approvalStatus,
                [ONAYLAYAN_ADMIN_ID] = @adminUserId,
                [ONAY_TARIHI] = CASE WHEN @approvalStatus = 'Onaylandı' THEN SYSUTCDATETIME() ELSE [ONAY_TARIHI] END,
                [POPULERLIK_SIRASI] = @popularityOrder,
                [ONE_CIKAN_OTEL] = @isFeatured,
                [TAVSIYE_EDILEN_OTEL] = @isRecommended,
                [GUNCELLENME_TARIHI] = SYSUTCDATETIME()
            WHERE id = @hotelId;";

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@hotelId", request.HotelId);
        command.Parameters.AddWithValue("@hotelCode", request.HotelCode.Trim());
        command.Parameters.AddWithValue("@partnerId", request.PartnerId);
        command.Parameters.AddWithValue("@userId", request.UserId.HasValue ? request.UserId.Value : DBNull.Value);
        command.Parameters.AddWithValue("@hotelName", request.HotelName.Trim());
        command.Parameters.AddWithValue("@hotelType", request.HotelType);
        command.Parameters.AddWithValue("@starCount", request.StarCount.HasValue ? request.StarCount.Value : DBNull.Value);
        command.Parameters.AddWithValue("@tourismDocumentNo", DbValue(request.TourismDocumentNo));
        command.Parameters.AddWithValue("@tourismDocumentType", DbValue(request.TourismDocumentType));
        command.Parameters.AddWithValue("@country", request.Country);
        command.Parameters.AddWithValue("@city", request.City);
        command.Parameters.AddWithValue("@district", request.District);
        command.Parameters.AddWithValue("@neighborhood", DbValue(request.Neighborhood));
        command.Parameters.AddWithValue("@address", request.Address);
        command.Parameters.AddWithValue("@postalCode", DbValue(request.PostalCode));
        command.Parameters.AddWithValue("@latitude", request.Latitude.HasValue ? request.Latitude.Value : DBNull.Value);
        command.Parameters.AddWithValue("@longitude", request.Longitude.HasValue ? request.Longitude.Value : DBNull.Value);
        command.Parameters.AddWithValue("@phone1", request.Phone1);
        command.Parameters.AddWithValue("@phone2", DbValue(request.Phone2));
        command.Parameters.AddWithValue("@fax", DbValue(request.Fax));
        command.Parameters.AddWithValue("@contactEmail", request.ContactEmail);
        command.Parameters.AddWithValue("@website", DbValue(request.Website));
        command.Parameters.AddWithValue("@reservationPhone", DbValue(request.ReservationPhone));
        command.Parameters.AddWithValue("@salesContactName", DbValue(request.SalesContactName));
        command.Parameters.AddWithValue("@salesContactPhone", DbValue(request.SalesContactPhone));
        command.Parameters.AddWithValue("@salesContactEmail", DbValue(request.SalesContactEmail));
        command.Parameters.AddWithValue("@salesNotes", DbValue(request.SalesNotes));
        command.Parameters.AddWithValue("@checkInTime", ParseTimeOrDbNull(request.CheckInTime));
        command.Parameters.AddWithValue("@checkOutTime", ParseTimeOrDbNull(request.CheckOutTime));
        command.Parameters.AddWithValue("@lateCheckoutAvailable", request.LateCheckoutAvailable ? 1 : 0);
        command.Parameters.AddWithValue("@lateCheckoutFee", request.LateCheckoutFee.HasValue ? request.LateCheckoutFee.Value : DBNull.Value);
        command.Parameters.AddWithValue("@earlyCheckInAvailable", request.EarlyCheckInAvailable ? 1 : 0);
        command.Parameters.AddWithValue("@earlyCheckInFee", request.EarlyCheckInFee.HasValue ? request.EarlyCheckInFee.Value : DBNull.Value);
        command.Parameters.AddWithValue("@totalRoomCount", request.TotalRoomCount);
        command.Parameters.AddWithValue("@totalBedCapacity", request.TotalBedCapacity.HasValue ? request.TotalBedCapacity.Value : DBNull.Value);
        command.Parameters.AddWithValue("@floorCount", request.FloorCount.HasValue ? request.FloorCount.Value : DBNull.Value);
        command.Parameters.AddWithValue("@elevatorAvailable", request.ElevatorAvailable ? 1 : 0);
        command.Parameters.AddWithValue("@elevatorCount", request.ElevatorCount.HasValue ? request.ElevatorCount.Value : DBNull.Value);
        command.Parameters.AddWithValue("@shortDescription", DbValue(request.ShortDescription));
        command.Parameters.AddWithValue("@description", DbValue(request.Description));
        command.Parameters.AddWithValue("@locationDescription", DbValue(request.LocationDescription));
        command.Parameters.AddWithValue("@commissionType", request.CommissionType);
        command.Parameters.AddWithValue("@defaultCommissionRate", request.DefaultCommissionRate);
        command.Parameters.AddWithValue("@commissionCalculationType", request.CommissionCalculationType);
        command.Parameters.AddWithValue("@paymentTerm", request.PaymentTerm);
        command.Parameters.AddWithValue("@paymentMethod", request.PaymentMethod);
        command.Parameters.AddWithValue("@invoiceType", request.InvoiceType);
        command.Parameters.AddWithValue("@depositAmount", request.DepositAmount.HasValue ? request.DepositAmount.Value : DBNull.Value);
        command.Parameters.AddWithValue("@depositReturnDays", request.DepositReturnDays.HasValue ? request.DepositReturnDays.Value : DBNull.Value);
        command.Parameters.AddWithValue("@minStay", request.MinStay.HasValue ? request.MinStay.Value : DBNull.Value);
        command.Parameters.AddWithValue("@maxStay", request.MaxStay.HasValue ? request.MaxStay.Value : DBNull.Value);
        command.Parameters.AddWithValue("@spokenLanguages", DbValue(request.SpokenLanguages));
        command.Parameters.AddWithValue("@averageScore", request.AverageScore.HasValue ? request.AverageScore.Value : DBNull.Value);
        command.Parameters.AddWithValue("@totalReviewCount", request.TotalReviewCount.HasValue ? request.TotalReviewCount.Value : DBNull.Value);
        command.Parameters.AddWithValue("@cleanlinessScore", request.CleanlinessScore.HasValue ? request.CleanlinessScore.Value : DBNull.Value);
        command.Parameters.AddWithValue("@comfortScore", request.ComfortScore.HasValue ? request.ComfortScore.Value : DBNull.Value);
        command.Parameters.AddWithValue("@locationScore", request.LocationScore.HasValue ? request.LocationScore.Value : DBNull.Value);
        command.Parameters.AddWithValue("@staffScore", request.StaffScore.HasValue ? request.StaffScore.Value : DBNull.Value);
        command.Parameters.AddWithValue("@pricePerformanceScore", request.PricePerformanceScore.HasValue ? request.PricePerformanceScore.Value : DBNull.Value);
        command.Parameters.AddWithValue("@coverPhotoPath", DbValue(request.CoverPhotoPath));
        command.Parameters.AddWithValue("@videoUrl", DbValue(request.VideoUrl));
        command.Parameters.AddWithValue("@virtualTourUrl", DbValue(request.VirtualTourUrl));
        command.Parameters.AddWithValue("@publishStatus", request.PublishStatus);
        command.Parameters.AddWithValue("@approvalStatus", request.ApprovalStatus);
        command.Parameters.AddWithValue("@adminUserId", adminUserId);
        command.Parameters.AddWithValue("@popularityOrder", request.PopularityOrder);
        command.Parameters.AddWithValue("@isFeatured", request.IsFeatured ? 1 : 0);
        command.Parameters.AddWithValue("@isRecommended", request.IsRecommended ? 1 : 0);
        await command.ExecuteNonQueryAsync(cancellationToken);

        await TryLogCoordinateChangeAsync(
            connection,
            adminUserId,
            request.HotelId,
            request.HotelName,
            previousLatitude,
            previousLongitude,
            request.Latitude,
            request.Longitude,
            cancellationToken);

        return (true, "Otel bilgileri admin panelinden guncellendi.");
    }

    private static async Task<(decimal? Latitude, decimal? Longitude)> LoadHotelCoordinatesAsync(SqlConnection connection, long hotelId, CancellationToken cancellationToken)
    {
        const string sql = "SELECT TOP (1) [ENLEM], [BOYLAM] FROM [dbo].[OTELLER] WHERE id = @hotelId;";
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@hotelId", hotelId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return (null, null);
        }

        decimal? lat = reader.IsDBNull(0) ? null : Convert.ToDecimal(reader.GetValue(0), CultureInfo.InvariantCulture);
        decimal? lng = reader.IsDBNull(1) ? null : Convert.ToDecimal(reader.GetValue(1), CultureInfo.InvariantCulture);
        return (lat, lng);
    }

    private async Task TryLogCoordinateChangeAsync(
        SqlConnection connection,
        long adminUserId,
        long hotelId,
        string hotelName,
        decimal? previousLat,
        decimal? previousLng,
        decimal? newLat,
        decimal? newLng,
        CancellationToken cancellationToken)
    {
        if (previousLat == newLat && previousLng == newLng)
        {
            return;
        }

        const string existsSql = "SELECT CASE WHEN OBJECT_ID(N'[dbo].[OTEL_KOORDINAT_DEGISIM_LOGLARI]', N'U') IS NULL THEN 0 ELSE 1 END;";
        await using (var existsCmd = new SqlCommand(existsSql, connection))
        {
            var existsObj = await existsCmd.ExecuteScalarAsync(cancellationToken);
            var exists = existsObj is not null && Convert.ToInt32(existsObj, CultureInfo.InvariantCulture) == 1;
            if (!exists)
            {
                return;
            }
        }

        const string adminNameSql = "SELECT TOP (1) COALESCE(NULLIF([AD_SOYAD],''), '-') FROM [dbo].[KULLANICILAR] WHERE id = @id;";
        string adminName;
        await using (var adminCmd = new SqlCommand(adminNameSql, connection))
        {
            adminCmd.Parameters.AddWithValue("@id", adminUserId);
            var raw = await adminCmd.ExecuteScalarAsync(cancellationToken);
            adminName = raw is null or DBNull ? "-" : Convert.ToString(raw, CultureInfo.InvariantCulture) ?? "-";
        }

        const string insertSql = @"
            INSERT INTO [dbo].[OTEL_KOORDINAT_DEGISIM_LOGLARI]
            (
                [ADMIN_KULLANICI_ID], [ADMIN_AD_SOYAD], [OTEL_ID], [OTEL_ADI],
                [ONCEKI_ENLEM], [ONCEKI_BOYLAM], [YENI_ENLEM], [YENI_BOYLAM],
                [IP_ADRESI], [NOTLAR]
            )
            VALUES
            (
                @adminUserId, @adminName, @hotelId, @hotelName,
                @prevLat, @prevLng, @newLat, @newLng,
                @ip, @note
            );";

        await using var insertCmd = new SqlCommand(insertSql, connection);
        insertCmd.Parameters.AddWithValue("@adminUserId", adminUserId);
        insertCmd.Parameters.AddWithValue("@adminName", adminName);
        insertCmd.Parameters.AddWithValue("@hotelId", hotelId);
        insertCmd.Parameters.AddWithValue("@hotelName", hotelName);
        insertCmd.Parameters.AddWithValue("@prevLat", previousLat.HasValue ? previousLat.Value : DBNull.Value);
        insertCmd.Parameters.AddWithValue("@prevLng", previousLng.HasValue ? previousLng.Value : DBNull.Value);
        insertCmd.Parameters.AddWithValue("@newLat", newLat.HasValue ? newLat.Value : DBNull.Value);
        insertCmd.Parameters.AddWithValue("@newLng", newLng.HasValue ? newLng.Value : DBNull.Value);
        insertCmd.Parameters.AddWithValue("@ip", DBNull.Value);
        insertCmd.Parameters.AddWithValue("@note", "Admin panelinden koordinat güncellendi.");
        await insertCmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<(bool Success, string Message)> SaveRoomAsync(AdminRoomEditForm request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.RoomName) || request.BasePrice <= 0)
        {
            return (false, "Oda adi ve taban fiyat zorunludur.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        var hotel = await EnsureHotelExistsAsync(connection, request.HotelId, cancellationToken);

        object featuresJson = string.IsNullOrWhiteSpace(request.FeaturesText)
            ? DBNull.Value
            : JsonSerializer.Serialize(request.FeaturesText.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

        if (request.RoomId.HasValue)
        {
            const string updateSql = @"
                UPDATE [dbo].[ODA_TIPLERI]
                SET [ODA_TIP_KODU] = @roomCode,
                    [ODA_ADI] = @roomName,
                    [ODA_KATEGORISI] = @roomCategory,
                    [MAKSIMUM_KISI_SAYISI] = @maxPeople,
                    [MAKSIMUM_YETISKIN_SAYISI] = @maxAdults,
                    [MAKSIMUM_COCUK_SAYISI] = @maxChildren,
                    [YATAK_TIPI] = @bedType,
                    [YATAK_SAYISI] = @bedCount,
                    [EK_YATAK_EKLENEBILIR_MI] = @extraBedAvailable,
                    [ODA_METREKARE] = @roomSize,
                    [BALKON_VAR_MI] = @balconyAvailable,
                    [BALKON_METREKARE] = @balconySize,
                    [MANZARA_TIPI] = @viewType,
                    [OZEL_BANYO_VAR_MI] = @privateBathroom,
                    [BANYO_TIPI] = @bathroomType,
                    [STANDART_GECELIK_FIYAT] = @basePrice,
                    [HAFTASONU_FARK_ORANI] = @weekendDifferenceRate,
                    [COCUK_INDIRIM_ORANI] = @childDiscountRate,
                    [BEBEK_UCRETSIZ_MI] = @babyFree,
                    [BEBEK_YAS_SINIRI] = @babyAgeLimit,
                    [COCUK_YAS_SINIRI] = @childAgeLimit,
                    [TOPLAM_ODA_SAYISI] = @totalRooms,
                    [OVERBOOKING_LIMIT] = @overbookingLimit,
                    [KAPAK_FOTOGRAFI] = @coverPhotoPath,
                    [GALERI] = @galleryJson,
                    [OZELLIKLER] = @featuresJson,
                    [AKTIF_MI] = @isActive,
                    [SIRALAMA] = @sortOrder,
                    [GUNCELLENME_TARIHI] = SYSUTCDATETIME()
                WHERE id = @roomId AND [OTEL_ID] = @hotelId;";

            await using var command = new SqlCommand(updateSql, connection);
            BindRoomCommand(command, request, hotel.HotelId, featuresJson);
            command.Parameters.AddWithValue("@roomId", request.RoomId.Value);
            await command.ExecuteNonQueryAsync(cancellationToken);
            return (true, "Oda tipi guncellendi.");
        }

        const string insertSql = @"
            INSERT INTO [dbo].[ODA_TIPLERI]
            ([OTEL_ID], [ODA_TIP_KODU], [ODA_ADI], [ODA_KATEGORISI], [MAKSIMUM_KISI_SAYISI], [MAKSIMUM_YETISKIN_SAYISI], [MAKSIMUM_COCUK_SAYISI], [YATAK_TIPI], [YATAK_SAYISI], [EK_YATAK_EKLENEBILIR_MI], [ODA_METREKARE], [BALKON_VAR_MI], [BALKON_METREKARE], [MANZARA_TIPI], [OZEL_BANYO_VAR_MI], [BANYO_TIPI], [STANDART_GECELIK_FIYAT], [HAFTASONU_FARK_ORANI], [COCUK_INDIRIM_ORANI], [BEBEK_UCRETSIZ_MI], [BEBEK_YAS_SINIRI], [COCUK_YAS_SINIRI], [TOPLAM_ODA_SAYISI], [OVERBOOKING_LIMIT], [KAPAK_FOTOGRAFI], [GALERI], [OZELLIKLER], [AKTIF_MI], [SIRALAMA])
            VALUES
            (@hotelId, @roomCode, @roomName, @roomCategory, @maxPeople, @maxAdults, @maxChildren, @bedType, @bedCount, @extraBedAvailable, @roomSize, @balconyAvailable, @balconySize, @viewType, @privateBathroom, @bathroomType, @basePrice, @weekendDifferenceRate, @childDiscountRate, @babyFree, @babyAgeLimit, @childAgeLimit, @totalRooms, @overbookingLimit, @coverPhotoPath, @galleryJson, @featuresJson, @isActive, @sortOrder);";

            await using var insertCommand = new SqlCommand(insertSql, connection);
            BindRoomCommand(insertCommand, request, hotel.HotelId, featuresJson);
            await insertCommand.ExecuteNonQueryAsync(cancellationToken);
            return (true, "Yeni oda tipi eklendi.");
        }

        public async Task<(bool Success, string Message)> DeactivateHotelAsync(long hotelId, long adminUserId, CancellationToken cancellationToken = default)
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            await EnsureHotelExistsAsync(connection, hotelId, cancellationToken);

            const string sql = @"
                UPDATE [dbo].[OTELLER]
                SET [YAYIN_DURUMU] = 'Kapatıldı',
                    [ONAYLAYAN_ADMIN_ID] = @adminUserId,
                    [GUNCELLENME_TARIHI] = SYSUTCDATETIME()
                WHERE id = @hotelId;";
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@hotelId", hotelId);
            command.Parameters.AddWithValue("@adminUserId", adminUserId);
            var affectedRows = await command.ExecuteNonQueryAsync(cancellationToken);
            return affectedRows > 0
                ? (true, "Otel pasif duruma alindi.")
                : (false, "Otel bulunamadi veya guncellenemedi.");
        }

        public async Task<(bool Success, string Message)> ActivateHotelAsync(long hotelId, long adminUserId, CancellationToken cancellationToken = default)
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            await EnsureHotelExistsAsync(connection, hotelId, cancellationToken);

            const string sql = @"
                UPDATE [dbo].[OTELLER]
                SET [YAYIN_DURUMU] = CASE
                        WHEN [ONAY_DURUMU] IN ('Onaylandı', 'Onaylandi') THEN 'Yayında'
                        ELSE 'Taslak'
                    END,
                    [ONAYLAYAN_ADMIN_ID] = @adminUserId,
                    [GUNCELLENME_TARIHI] = SYSUTCDATETIME()
                WHERE id = @hotelId;";
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@hotelId", hotelId);
            command.Parameters.AddWithValue("@adminUserId", adminUserId);
            var affectedRows = await command.ExecuteNonQueryAsync(cancellationToken);
            return affectedRows > 0
                ? (true, "Otel tekrar aktif duruma alindi.")
                : (false, "Otel bulunamadi veya guncellenemedi.");
        }

        public async Task<(bool Success, string Message, int UpdatedCount)> BulkUpdateHotelPublishStatusAsync(IReadOnlyList<long> hotelIds, bool publish, long adminUserId, CancellationToken cancellationToken = default)
        {
            var ids = (hotelIds ?? Array.Empty<long>()).Where(id => id > 0).Distinct().ToArray();
            if (ids.Length == 0)
            {
                return (false, "En az bir otel secmelisiniz.", 0);
            }

            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            const string publishSql = @"
                UPDATE [dbo].[OTELLER]
                SET [YAYIN_DURUMU] = CASE
                        WHEN [ONAY_DURUMU] IN (N'Onaylandı', N'Onaylandi') THEN N'Yayında'
                        ELSE N'Taslak'
                    END,
                    [ONAYLAYAN_ADMIN_ID] = @adminUserId,
                    [GUNCELLENME_TARIHI] = SYSUTCDATETIME()
                WHERE id IN (SELECT CAST(value AS bigint) FROM OPENJSON(@ids));";

            const string unpublishSql = @"
                UPDATE [dbo].[OTELLER]
                SET [YAYIN_DURUMU] = N'Kapatıldı',
                    [ONAYLAYAN_ADMIN_ID] = @adminUserId,
                    [GUNCELLENME_TARIHI] = SYSUTCDATETIME()
                WHERE id IN (SELECT CAST(value AS bigint) FROM OPENJSON(@ids));";

            await using var command = new SqlCommand(publish ? publishSql : unpublishSql, connection);
            command.Parameters.AddWithValue("@ids", JsonSerializer.Serialize(ids));
            command.Parameters.AddWithValue("@adminUserId", adminUserId);
            var affectedRows = await command.ExecuteNonQueryAsync(cancellationToken);
            if (affectedRows <= 0)
            {
                return (false, "Secilen oteller bulunamadi veya guncellenemedi.", 0);
            }

            var verb = publish ? "yayina alindi" : "yayini kapatildi";
            return (true, $"{affectedRows} otel {verb}.", affectedRows);
        }

        public async Task<(bool Success, string Message)> DeactivateRoomAsync(long hotelId, long roomId, CancellationToken cancellationToken = default)
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            await EnsureHotelExistsAsync(connection, hotelId, cancellationToken);

            const string sql = "UPDATE [dbo].[ODA_TIPLERI] SET [AKTIF_MI] = 0, [GUNCELLENME_TARIHI] = SYSUTCDATETIME() WHERE id = @roomId AND [OTEL_ID] = @hotelId;";
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@roomId", roomId);
            command.Parameters.AddWithValue("@hotelId", hotelId);
            var affectedRows = await command.ExecuteNonQueryAsync(cancellationToken);
            return affectedRows > 0 ? (true, "Oda tipi pasife alindi.") : (false, "Oda tipi bulunamadi.");
        }

    public async Task<(bool Success, string Message)> UploadHotelPhotosAsync(long adminUserId, AdminHotelPhotoUploadForm request, CancellationToken cancellationToken = default)
    {
        if (request.Files is null || request.Files.Count == 0)
        {
            return (false, "En az bir otel gorseli secmelisiniz.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        var hotel = await EnsureHotelExistsAsync(connection, request.HotelId, cancellationToken);
        var targetDirectory = MediaStoragePaths.HotelImagesDirectory(_environment.WebRootPath, hotel.HotelId);
        Directory.CreateDirectory(targetDirectory);
        var savedPhysicalPaths = new List<string>();
        var displayOrder = request.DisplayOrder;

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            var shouldMakeCover = request.MakeCover;
            foreach (var file in request.Files.Where(static item => item.Length > 0))
            {
                var storedImage = await _imageStorageService.SaveAsWebpAsync(file, new otelturizmnew.Services.Abstractions.ImageSaveRequest(
                    TargetDirectory: targetDirectory,
                    FilePrefix: $"admin-otel-{hotel.HotelId}",
                    Category: "admin-hotel-photo",
                    OwnerUserId: adminUserId,
                    ContextTable: "oteller",
                    ContextId: hotel.HotelId,
                    QualityProfile: otelturizmnew.Services.Abstractions.ImageQualityProfile.HotelPhoto,
                    GenerateThumbnails: true
                ), cancellationToken);
                var relativePath = MediaStoragePaths.HotelImagesUrl(hotel.HotelId, storedImage.FileName);
                savedPhysicalPaths.Add(Path.Combine(targetDirectory, storedImage.FileName));

                const string insertSql = @"
                    INSERT INTO [dbo].[OTEL_GORSELLERI]
                    ([OTEL_ID], [GORSEL_URL], [GORSEL_TURU], [BASLIK], [ACIKLAMA], [KAPAK_FOTOGRAFI_MI], [ONE_CIKAN], [SIRALAMA], [BOYUT_KB], [ONAY_DURUMU], [ONAYLAYAN_ADMIN_ID], [ONAY_TARIHI], [YUKLEYEN_KULLANICI_ID])
                    VALUES
                    (@hotelId, @url, @photoType, @title, @description, @isCover, @featured, @sortOrder, @sizeKb, 'Onaylandı', @adminUserId, SYSUTCDATETIME(), @adminUserId);";
                await using var insertCommand = new SqlCommand(insertSql, connection, (SqlTransaction)transaction);
                insertCommand.Parameters.AddWithValue("@hotelId", hotel.HotelId);
                insertCommand.Parameters.AddWithValue("@url", relativePath);
                insertCommand.Parameters.AddWithValue("@photoType", request.PhotoType);
                insertCommand.Parameters.AddWithValue("@title", string.IsNullOrWhiteSpace(request.Title) ? hotel.HotelName : request.Title.Trim());
                insertCommand.Parameters.AddWithValue("@description", DbValue(request.Description));
                insertCommand.Parameters.AddWithValue("@isCover", shouldMakeCover ? 1 : 0);
                insertCommand.Parameters.AddWithValue("@featured", shouldMakeCover ? 1 : 0);
                insertCommand.Parameters.AddWithValue("@sortOrder", displayOrder);
                insertCommand.Parameters.AddWithValue("@sizeKb", Math.Max(1, (int)Math.Round(storedImage.FileSizeBytes / 1024m)));
                insertCommand.Parameters.AddWithValue("@adminUserId", adminUserId);
                await insertCommand.ExecuteNonQueryAsync(cancellationToken);

                if (shouldMakeCover)
                {
                    await using var resetCover = new SqlCommand("UPDATE [dbo].[OTEL_GORSELLERI] SET [KAPAK_FOTOGRAFI_MI] = CASE WHEN [GORSEL_URL] = @coverUrl THEN 1 ELSE 0 END WHERE [OTEL_ID] = @hotelId;", connection, (SqlTransaction)transaction);
                    resetCover.Parameters.AddWithValue("@coverUrl", relativePath);
                    resetCover.Parameters.AddWithValue("@hotelId", hotel.HotelId);
                    await resetCover.ExecuteNonQueryAsync(cancellationToken);

                    await using var updateHotel = new SqlCommand("UPDATE [dbo].[OTELLER] SET [KAPAK_FOTOGRAFI] = @coverUrl WHERE id = @hotelId;", connection, (SqlTransaction)transaction);
                    updateHotel.Parameters.AddWithValue("@coverUrl", relativePath);
                    updateHotel.Parameters.AddWithValue("@hotelId", hotel.HotelId);
                    await updateHotel.ExecuteNonQueryAsync(cancellationToken);
                    shouldMakeCover = false;
                }

                displayOrder++;
            }

            await transaction.CommitAsync(cancellationToken);
            return (true, $"{savedPhysicalPaths.Count} otel gorseli yuklendi.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            foreach (var path in savedPhysicalPaths)
            {
                await _imageStorageService.DeleteAsync(path, cancellationToken);
            }
            return (false, $"Otel gorselleri yuklenirken hata olustu: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Message)> UpdateHotelPhotoAsync(AdminHotelPhotoEditForm request, CancellationToken cancellationToken = default)
    {
        if (!request.PhotoId.HasValue)
        {
            return (false, "Guncellenecek otel fotografi secilmedi.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await EnsureHotelExistsAsync(connection, request.HotelId, cancellationToken);

        const string sql = @"
            UPDATE [dbo].[OTEL_GORSELLERI]
            SET [BASLIK] = @title,
                [GORSEL_TURU] = @photoType,
                [ACIKLAMA] = @description,
                [SIRALAMA] = @displayOrder,
                [ONE_CIKAN] = @featured
            WHERE id = @photoId AND [OTEL_ID] = @hotelId;";
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@photoId", request.PhotoId.Value);
        command.Parameters.AddWithValue("@hotelId", request.HotelId);
        command.Parameters.AddWithValue("@title", DbValue(request.Title));
        command.Parameters.AddWithValue("@photoType", request.PhotoType);
        command.Parameters.AddWithValue("@description", DbValue(request.Description));
        command.Parameters.AddWithValue("@displayOrder", request.DisplayOrder);
        command.Parameters.AddWithValue("@featured", request.MarkAsFeatured ? 1 : 0);
        var affectedRows = await command.ExecuteNonQueryAsync(cancellationToken);
        return affectedRows > 0 ? (true, "Otel fotografi guncellendi.") : (false, "Otel fotografi bulunamadi.");
    }

    public async Task<(bool Success, string Message)> SetHotelCoverAsync(long hotelId, long photoId, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await EnsureHotelExistsAsync(connection, hotelId, cancellationToken);

        const string selectSql = "SELECT TOP (1) [GORSEL_URL] FROM [dbo].[OTEL_GORSELLERI] WHERE id = @photoId AND [OTEL_ID] = @hotelId;";
        await using var selectCommand = new SqlCommand(selectSql, connection);
        selectCommand.Parameters.AddWithValue("@photoId", photoId);
        selectCommand.Parameters.AddWithValue("@hotelId", hotelId);
        var url = await selectCommand.ExecuteScalarAsync(cancellationToken) as string;
        if (string.IsNullOrWhiteSpace(url))
        {
            return (false, "Kapak yapilacak otel fotografi bulunamadi.");
        }

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await using (var updatePhotos = new SqlCommand("UPDATE [dbo].[OTEL_GORSELLERI] SET [KAPAK_FOTOGRAFI_MI] = CASE WHEN id = @photoId THEN 1 ELSE 0 END WHERE [OTEL_ID] = @hotelId;", connection, (SqlTransaction)transaction))
        {
            updatePhotos.Parameters.AddWithValue("@photoId", photoId);
            updatePhotos.Parameters.AddWithValue("@hotelId", hotelId);
            await updatePhotos.ExecuteNonQueryAsync(cancellationToken);
        }
        await using (var updateHotel = new SqlCommand("UPDATE [dbo].[OTELLER] SET [KAPAK_FOTOGRAFI] = @coverUrl WHERE id = @hotelId;", connection, (SqlTransaction)transaction))
        {
            updateHotel.Parameters.AddWithValue("@coverUrl", url);
            updateHotel.Parameters.AddWithValue("@hotelId", hotelId);
            await updateHotel.ExecuteNonQueryAsync(cancellationToken);
        }
        await transaction.CommitAsync(cancellationToken);
        return (true, "Otel kapak fotografi guncellendi.");
    }

    public async Task<(bool Success, string Message)> DeleteHotelPhotoAsync(long hotelId, long photoId, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await EnsureHotelExistsAsync(connection, hotelId, cancellationToken);

        string? relativePath = null;
        var wasCover = false;
        const string selectSql = "SELECT TOP (1) [GORSEL_URL], [KAPAK_FOTOGRAFI_MI] FROM [dbo].[OTEL_GORSELLERI] WHERE id = @photoId AND [OTEL_ID] = @hotelId;";
        await using (var selectCommand = new SqlCommand(selectSql, connection))
        {
            selectCommand.Parameters.AddWithValue("@photoId", photoId);
            selectCommand.Parameters.AddWithValue("@hotelId", hotelId);
            await using var reader = await selectCommand.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                relativePath = reader.GetString(0);
                wasCover = SafeBool(reader, 1);
            }
        }

        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return (false, "Silinecek otel fotografi bulunamadi.");
        }

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await using (var deleteCommand = new SqlCommand("DELETE FROM [dbo].[OTEL_GORSELLERI] WHERE id = @photoId AND [OTEL_ID] = @hotelId;", connection, (SqlTransaction)transaction))
        {
            deleteCommand.Parameters.AddWithValue("@photoId", photoId);
            deleteCommand.Parameters.AddWithValue("@hotelId", hotelId);
            await deleteCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        if (wasCover)
        {
            await PromoteNextHotelCoverAsync(connection, (SqlTransaction)transaction, hotelId, cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        var physicalPath = Path.Combine(_environment.WebRootPath, relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        await _imageStorageService.DeleteAsync(physicalPath, cancellationToken);
        return (true, "Otel fotografi silindi.");
    }

    public async Task<(bool Success, string Message)> UploadRoomPhotosAsync(long adminUserId, AdminRoomPhotoUploadForm request, CancellationToken cancellationToken = default)
    {
        if (request.Files is null || request.Files.Count == 0)
        {
            return (false, "En az bir oda gorseli secmelisiniz.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await EnsureHotelExistsAsync(connection, request.HotelId, cancellationToken);
        var room = await EnsureRoomExistsAsync(connection, request.HotelId, request.RoomId, cancellationToken);
        var targetDirectory = MediaStoragePaths.RoomImagesDirectory(_environment.WebRootPath, request.HotelId, request.RoomId);
        Directory.CreateDirectory(targetDirectory);
        var savedPhysicalPaths = new List<string>();
        var displayOrder = request.DisplayOrder;

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            var shouldMakeCover = request.MakeCover;
            foreach (var file in request.Files.Where(static item => item.Length > 0))
            {
                var storedImage = await _imageStorageService.SaveAsWebpAsync(file, new otelturizmnew.Services.Abstractions.ImageSaveRequest(
                    TargetDirectory: targetDirectory,
                    FilePrefix: $"admin-oda-{room.RoomId}",
                    Category: "admin-room-photo",
                    OwnerUserId: adminUserId,
                    ContextTable: "oda_tipleri",
                    ContextId: room.RoomId,
                    QualityProfile: otelturizmnew.Services.Abstractions.ImageQualityProfile.RoomPhoto,
                    GenerateThumbnails: true
                ), cancellationToken);
                var relativePath = MediaStoragePaths.RoomImagesUrl(request.HotelId, request.RoomId, storedImage.FileName);
                savedPhysicalPaths.Add(Path.Combine(targetDirectory, storedImage.FileName));

                const string insertSql = @"
                    INSERT INTO [dbo].[ODA_GORSELLERI]
                    ([ODA_TIP_ID], [GORSEL_URL], [BASLIK], [ACIKLAMA], [KAPAK_FOTOGRAFI_MI], [SIRALAMA], [BOYUT_KB], [ONAY_DURUMU], [ONAYLAYAN_ADMIN_ID], [ONAY_TARIHI], [YUKLEYEN_KULLANICI_ID])
                    VALUES
                    (@roomId, @url, @title, @description, @isCover, @sortOrder, @sizeKb, 'Onaylandı', @adminUserId, SYSUTCDATETIME(), @adminUserId);";
                await using var insertCommand = new SqlCommand(insertSql, connection, (SqlTransaction)transaction);
                insertCommand.Parameters.AddWithValue("@roomId", request.RoomId);
                insertCommand.Parameters.AddWithValue("@url", relativePath);
                insertCommand.Parameters.AddWithValue("@title", string.IsNullOrWhiteSpace(request.Title) ? room.RoomName : request.Title.Trim());
                insertCommand.Parameters.AddWithValue("@description", DbValue(request.Description));
                insertCommand.Parameters.AddWithValue("@isCover", shouldMakeCover ? 1 : 0);
                insertCommand.Parameters.AddWithValue("@sortOrder", displayOrder);
                insertCommand.Parameters.AddWithValue("@sizeKb", Math.Max(1, (int)Math.Round(storedImage.FileSizeBytes / 1024m)));
                insertCommand.Parameters.AddWithValue("@adminUserId", adminUserId);
                await insertCommand.ExecuteNonQueryAsync(cancellationToken);

                if (shouldMakeCover)
                {
                    await using var resetCover = new SqlCommand("UPDATE [dbo].[ODA_GORSELLERI] SET [KAPAK_FOTOGRAFI_MI] = CASE WHEN [GORSEL_URL] = @coverUrl THEN 1 ELSE 0 END WHERE [ODA_TIP_ID] = @roomId;", connection, (SqlTransaction)transaction);
                    resetCover.Parameters.AddWithValue("@coverUrl", relativePath);
                    resetCover.Parameters.AddWithValue("@roomId", request.RoomId);
                    await resetCover.ExecuteNonQueryAsync(cancellationToken);

                    await using var updateRoom = new SqlCommand("UPDATE [dbo].[ODA_TIPLERI] SET [KAPAK_FOTOGRAFI] = @coverUrl WHERE id = @roomId;", connection, (SqlTransaction)transaction);
                    updateRoom.Parameters.AddWithValue("@coverUrl", relativePath);
                    updateRoom.Parameters.AddWithValue("@roomId", request.RoomId);
                    await updateRoom.ExecuteNonQueryAsync(cancellationToken);
                    shouldMakeCover = false;
                }

                displayOrder++;
            }

            await transaction.CommitAsync(cancellationToken);
            return (true, $"{savedPhysicalPaths.Count} oda gorseli yuklendi.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            foreach (var path in savedPhysicalPaths)
            {
                await _imageStorageService.DeleteAsync(path, cancellationToken);
            }
            return (false, $"Oda gorselleri yuklenirken hata olustu: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Message)> UpdateRoomPhotoAsync(AdminRoomPhotoEditForm request, CancellationToken cancellationToken = default)
    {
        if (!request.PhotoId.HasValue)
        {
            return (false, "Guncellenecek oda fotografi secilmedi.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await EnsureRoomExistsAsync(connection, request.HotelId, request.RoomId, cancellationToken);

        const string sql = @"
            UPDATE [dbo].[ODA_GORSELLERI]
            SET [BASLIK] = @title,
                [ACIKLAMA] = @description,
                [SIRALAMA] = @displayOrder
            WHERE id = @photoId AND [ODA_TIP_ID] = @roomId;";
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@photoId", request.PhotoId.Value);
        command.Parameters.AddWithValue("@roomId", request.RoomId);
        command.Parameters.AddWithValue("@title", DbValue(request.Title));
        command.Parameters.AddWithValue("@description", DbValue(request.Description));
        command.Parameters.AddWithValue("@displayOrder", request.DisplayOrder);
        var affectedRows = await command.ExecuteNonQueryAsync(cancellationToken);
        return affectedRows > 0 ? (true, "Oda fotografi guncellendi.") : (false, "Oda fotografi bulunamadi.");
    }

    public async Task<(bool Success, string Message)> SetRoomCoverAsync(long hotelId, long roomId, long photoId, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await EnsureRoomExistsAsync(connection, hotelId, roomId, cancellationToken);

        const string selectSql = "SELECT TOP (1) [GORSEL_URL] FROM [dbo].[ODA_GORSELLERI] WHERE id = @photoId AND [ODA_TIP_ID] = @roomId;";
        await using var selectCommand = new SqlCommand(selectSql, connection);
        selectCommand.Parameters.AddWithValue("@photoId", photoId);
        selectCommand.Parameters.AddWithValue("@roomId", roomId);
        var url = await selectCommand.ExecuteScalarAsync(cancellationToken) as string;
        if (string.IsNullOrWhiteSpace(url))
        {
            return (false, "Kapak yapilacak oda fotografi bulunamadi.");
        }

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await using (var updatePhotos = new SqlCommand("UPDATE [dbo].[ODA_GORSELLERI] SET [KAPAK_FOTOGRAFI_MI] = CASE WHEN id = @photoId THEN 1 ELSE 0 END WHERE [ODA_TIP_ID] = @roomId;", connection, (SqlTransaction)transaction))
        {
            updatePhotos.Parameters.AddWithValue("@photoId", photoId);
            updatePhotos.Parameters.AddWithValue("@roomId", roomId);
            await updatePhotos.ExecuteNonQueryAsync(cancellationToken);
        }
        await using (var updateRoom = new SqlCommand("UPDATE [dbo].[ODA_TIPLERI] SET [KAPAK_FOTOGRAFI] = @coverUrl WHERE id = @roomId;", connection, (SqlTransaction)transaction))
        {
            updateRoom.Parameters.AddWithValue("@coverUrl", url);
            updateRoom.Parameters.AddWithValue("@roomId", roomId);
            await updateRoom.ExecuteNonQueryAsync(cancellationToken);
        }
        await transaction.CommitAsync(cancellationToken);
        return (true, "Oda kapak fotografi guncellendi.");
    }

    public async Task<(bool Success, string Message)> DeleteRoomPhotoAsync(long hotelId, long roomId, long photoId, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await EnsureRoomExistsAsync(connection, hotelId, roomId, cancellationToken);

        string? relativePath = null;
        var wasCover = false;
        const string selectSql = "SELECT TOP (1) [GORSEL_URL], [KAPAK_FOTOGRAFI_MI] FROM [dbo].[ODA_GORSELLERI] WHERE id = @photoId AND [ODA_TIP_ID] = @roomId;";
        await using (var selectCommand = new SqlCommand(selectSql, connection))
        {
            selectCommand.Parameters.AddWithValue("@photoId", photoId);
            selectCommand.Parameters.AddWithValue("@roomId", roomId);
            await using var reader = await selectCommand.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                relativePath = reader.GetString(0);
                wasCover = SafeBool(reader, 1);
            }
        }

        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return (false, "Silinecek oda fotografi bulunamadi.");
        }

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await using (var deleteCommand = new SqlCommand("DELETE FROM [dbo].[ODA_GORSELLERI] WHERE id = @photoId AND [ODA_TIP_ID] = @roomId;", connection, (SqlTransaction)transaction))
        {
            deleteCommand.Parameters.AddWithValue("@photoId", photoId);
            deleteCommand.Parameters.AddWithValue("@roomId", roomId);
            await deleteCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        if (wasCover)
        {
            await PromoteNextRoomCoverAsync(connection, (SqlTransaction)transaction, roomId, cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        var physicalPath = Path.Combine(_environment.WebRootPath, relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        await _imageStorageService.DeleteAsync(physicalPath, cancellationToken);
        return (true, "Oda fotografi silindi.");
    }

    private async Task<AdminShellViewModel> BuildShellAsync(SqlConnection connection, string title, string subtitle, string fullName, string email, string userRole, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT
                (SELECT COUNT(*) FROM [dbo].[PARTNER_DETAYLARI] WHERE [ONAY_DURUMU] = 'Beklemede') AS pending_partner_applications,
                (SELECT COUNT(*) FROM [dbo].[SISTEM_ICI_BILDIRIMLER] WHERE [OKUNDU_MU] = 0) AS unread_notifications,
                (SELECT COUNT(*) FROM [dbo].[SISTEM_HATA_LOGLARI] WHERE [HATA_SEVIYESI] IN ('CRITICAL','ALERT','EMERGENCY') AND [COZULDU_MU] = 0) AS critical_logs,
                (SELECT COUNT(*) FROM [dbo].[YORUMLAR] WHERE [ONAY_DURUMU] = 'Beklemede') AS pending_reviews;";

        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var shell = new AdminShellViewModel
        {
            FullName = fullName,
            Email = email,
            UserRole = userRole,
            PanelTitle = title,
            PanelSubtitle = subtitle
        };

        if (await reader.ReadAsync(cancellationToken))
        {
            shell.PendingPartnerApplications = SafeInt(reader, 0);
            shell.UnreadNotifications = SafeInt(reader, 1);
            shell.CriticalLogs = SafeInt(reader, 2);
            shell.PendingReviews = SafeInt(reader, 3);
        }

        return shell;
    }

    private static async Task<List<AdminSummaryCardViewModel>> LoadHotelSummaryCardsAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        var cards = new List<AdminSummaryCardViewModel>();
        var definitions = new[]
        {
            ("Toplam Otel", "SELECT COUNT(*) FROM [dbo].[OTELLER]", "Tum tesis kayitlari", "info", "fa-hotel"),
            ("Yayinda", "SELECT COUNT(*) FROM [dbo].[OTELLER] WHERE [YAYIN_DURUMU] = 'Yayında'", "Canli listelenen oteller", "success", "fa-tower-broadcast"),
            ("Oda Tipi", "SELECT COUNT(*) FROM [dbo].[ODA_TIPLERI]", "Toplam oda tipi baglari", "warning", "fa-bed"),
            ("Gorsel", "SELECT (SELECT COUNT(*) FROM [dbo].[OTEL_GORSELLERI]) + (SELECT COUNT(*) FROM [dbo].[ODA_GORSELLERI])", "Otel ve oda medya varliklari", "danger", "fa-images")
        };

        foreach (var definition in definitions)
        {
            await using var command = new SqlCommand(definition.Item2, connection);
            var value = await command.ExecuteScalarAsync(cancellationToken);
            cards.Add(new AdminSummaryCardViewModel
            {
                Label = definition.Item1,
                Value = value?.ToString() ?? "0",
                Description = definition.Item3,
                ToneClass = definition.Item4,
                IconClass = definition.Item5
            });
        }

        return cards;
    }

    private static async Task LoadHotelFilterOptionsAsync(SqlConnection connection, AdminHotelsPageViewModel model, CancellationToken cancellationToken)
    {
        model.CityOptions.AddRange(await LoadDistinctHotelColumnAsync(connection, "sehir", cancellationToken));
        model.DistrictOptions.AddRange(await LoadDistinctHotelColumnAsync(connection, "ilce", cancellationToken));
        model.NeighborhoodOptions.AddRange(await LoadDistinctHotelColumnAsync(connection, "mahalle", cancellationToken));
        model.PublishStatusOptions.AddRange(await LoadDistinctHotelColumnAsync(connection, "yayin_durumu", cancellationToken));
        model.ApprovalStatusOptions.AddRange(await LoadDistinctHotelColumnAsync(connection, "onay_durumu", cancellationToken));
    }

    private static async Task<List<string>> LoadDistinctHotelColumnAsync(SqlConnection connection, string columnName, CancellationToken cancellationToken)
    {
        var safeColumn = columnName switch
        {
            "sehir" => "sehir",
            "ilce" => "ilce",
            "mahalle" => "mahalle",
            "yayin_durumu" => "yayin_durumu",
            "onay_durumu" => "onay_durumu",
            _ => throw new ArgumentOutOfRangeException(nameof(columnName), columnName, "Desteklenmeyen otel filtre kolonu.")
        };

        var values = new List<string>();
        await using var command = new SqlCommand($"""
            SELECT DISTINCT LTRIM(RTRIM({safeColumn}))
            FROM [dbo].[OTELLER]
            WHERE NULLIF(LTRIM(RTRIM(COALESCE({safeColumn}, ''))), '') IS NOT NULL
            ORDER BY LTRIM(RTRIM({safeColumn}));
            """, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            values.Add(reader.GetString(0));
        }

        return values;
    }

    private static void BindHotelListFilters(SqlCommand command, AdminHotelsPageViewModel model)
    {
        command.Parameters.AddWithValue("@search", model.SearchTerm);
        command.Parameters.AddWithValue("@city", model.CityFilter);
        command.Parameters.AddWithValue("@district", model.DistrictFilter);
        command.Parameters.AddWithValue("@neighborhood", model.NeighborhoodFilter);
        command.Parameters.AddWithValue("@publishStatus", model.PublishStatusFilter);
        command.Parameters.AddWithValue("@approvalStatus", model.ApprovalStatusFilter);
    }

    private static async Task<List<AdminSummaryCardViewModel>> LoadHotelManagementCardsAsync(SqlConnection connection, long hotelId, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT
                (SELECT COUNT(*) FROM [dbo].[ODA_TIPLERI] WHERE [OTEL_ID] = @hotelId) AS room_count,
                (SELECT COUNT(*) FROM [dbo].[OTEL_GORSELLERI] WHERE [OTEL_ID] = @hotelId) AS hotel_photo_count,
                (SELECT COUNT(*) FROM [dbo].[ODA_GORSELLERI] og INNER JOIN [dbo].[ODA_TIPLERI] od ON od.id = og.[ODA_TIP_ID] WHERE od.[OTEL_ID] = @hotelId) AS room_photo_count,
                (SELECT COUNT(*) FROM [dbo].[REZERVASYONLAR] WHERE [OTEL_ID] = @hotelId) AS reservation_count;";

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@hotelId", hotelId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var cards = new List<AdminSummaryCardViewModel>();
        if (await reader.ReadAsync(cancellationToken))
        {
            cards.Add(new AdminSummaryCardViewModel { Label = "Oda Tipi", Value = SafeInt(reader, 0).ToString(), Description = "Bagli oda kayitlari", ToneClass = "info", IconClass = "fa-bed" });
            cards.Add(new AdminSummaryCardViewModel { Label = "Otel Gorseli", Value = SafeInt(reader, 1).ToString(), Description = "Tesis galeri varliklari", ToneClass = "success", IconClass = "fa-image" });
            cards.Add(new AdminSummaryCardViewModel { Label = "Oda Gorseli", Value = SafeInt(reader, 2).ToString(), Description = "Oda bazli medya varliklari", ToneClass = "warning", IconClass = "fa-camera-retro" });
            cards.Add(new AdminSummaryCardViewModel { Label = "Rezervasyon", Value = SafeInt(reader, 3).ToString(), Description = "Bu otele bagli rezervasyonlar", ToneClass = "danger", IconClass = "fa-calendar-check" });
        }
        return cards;
    }

    private static async Task<(long HotelId, string HotelName)> EnsureHotelExistsAsync(SqlConnection connection, long hotelId, CancellationToken cancellationToken)
    {
        const string sql = "SELECT TOP (1) id, [OTEL_ADI] FROM [dbo].[OTELLER] WHERE id = @hotelId;";
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@hotelId", hotelId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            throw new InvalidOperationException("Otel bulunamadi.");
        }

        return (reader.GetInt64(0), reader.GetString(1));
    }

    private static async Task<(long RoomId, string RoomName)> EnsureRoomExistsAsync(SqlConnection connection, long hotelId, long roomId, CancellationToken cancellationToken)
    {
        const string sql = "SELECT TOP (1) id, [ODA_ADI] FROM [dbo].[ODA_TIPLERI] WHERE id = @roomId AND [OTEL_ID] = @hotelId;";
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@roomId", roomId);
        command.Parameters.AddWithValue("@hotelId", hotelId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            throw new InvalidOperationException("Oda tipi bulunamadi.");
        }

        return (reader.GetInt64(0), reader.GetString(1));
    }

    private static async Task<AdminHotelEditForm> LoadHotelFormAsync(SqlConnection connection, long hotelId, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT id, [OTEL_KODU], [PARTNER_ID], [KULLANICI_ID], [OTEL_ADI], [OTEL_TURU], [YILDIZ_SAYISI], [TURIZM_BELGE_NO], [TURIZM_BELGE_TURU],
                   ulke, [SEHIR], ilce, [MAHALLE], [TAM_ADRES], [POSTA_KODU], [ENLEM], [BOYLAM], [TELEFON_1], [TELEFON_2], faks, [EPOSTA], [WEB_SITESI],
                   [REZERVASYON_TELEFONU], [SATIS_KONTAK_ADI], [SATIS_KONTAK_TELEFONU], [SATIS_KONTAK_EPOSTA], [SATIS_NOTLARI],
                   [CHECK_IN_SAATI], [CHECK_OUT_SAATI], [GEC_CHECK_OUT_MUMKUN_MU], [GEC_CHECK_OUT_UCRETI], [ERKEN_CHECK_IN_MUMKUN_MU], [ERKEN_CHECK_IN_UCRETI],
                   [TOPLAM_ODA_SAYISI], [TOPLAM_YATAK_KAPASITESI], [KAT_SAYISI], [ASANSOR_VAR_MI], [ASANSOR_SAYISI], [KISA_ACIKLAMA], [UZUN_ACIKLAMA],
                   [KONUM_ACIKLAMASI], [KOMISYON_TURU], [VARSAYILAN_KOMISYON_ORANI], [KOMISYON_HESAPLAMA_TIPI], [ODEME_VADESI], [ODEME_YONTEMI], [FATURA_KESIM_TURU],
                   [DEPOZITO_TUTARI], [DEPOZITO_IADE_SURESI], [MINIMUM_KONAKLAMA_GECESI], [MAKSIMUM_KONAKLAMA_GECESI], [KONUSULAN_DILLER],
                   [ORTALAMA_PUAN], [TOPLAM_YORUM_SAYISI], [TEMIZLIK_PUANI], [KONFOR_PUANI], [KONUM_PUANI], [PERSONEL_PUANI], [FIYAT_PERFORMANS_PUANI],
                   [KAPAK_FOTOGRAFI], [VIDEO_URL], [SANAL_TUR_URL], [YAYIN_DURUMU], [ONAY_DURUMU], [POPULERLIK_SIRASI], [ONE_CIKAN_OTEL], [TAVSIYE_EDILEN_OTEL]
            FROM [dbo].[OTELLER] WHERE id = @hotelId;";

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@hotelId", hotelId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            throw new InvalidOperationException("Otel kaydi bulunamadi.");
        }

        return new AdminHotelEditForm
        {
            HotelId = reader.GetInt64(0), HotelCode = reader.GetString(1), PartnerId = reader.GetInt64(2), UserId = reader.IsDBNull(3) ? null : reader.GetInt64(3), HotelName = reader.GetString(4), HotelType = reader.GetString(5),
            StarCount = reader.IsDBNull(6) ? null : Convert.ToByte(reader.GetValue(6), CultureInfo.InvariantCulture), TourismDocumentNo = SafeString(reader, 7), TourismDocumentType = SafeString(reader, 8), Country = SafeString(reader, 9) ?? "Türkiye", City = reader.GetString(10), District = reader.GetString(11),
            Neighborhood = SafeString(reader, 12), Address = reader.GetString(13), PostalCode = SafeString(reader, 14), Latitude = SafeDecimalNullable(reader, 15), Longitude = SafeDecimalNullable(reader, 16), Phone1 = reader.GetString(17), Phone2 = SafeString(reader, 18), Fax = SafeString(reader, 19),
            ContactEmail = reader.GetString(20), Website = SafeString(reader, 21), ReservationPhone = SafeString(reader, 22), SalesContactName = SafeString(reader, 23), SalesContactPhone = SafeString(reader, 24), SalesContactEmail = SafeString(reader, 25), SalesNotes = SafeString(reader, 26),
            CheckInTime = SafeTime(reader, 27), CheckOutTime = SafeTime(reader, 28), LateCheckoutAvailable = SafeBool(reader, 29), LateCheckoutFee = SafeDecimalNullable(reader, 30), EarlyCheckInAvailable = SafeBool(reader, 31), EarlyCheckInFee = SafeDecimalNullable(reader, 32),
            TotalRoomCount = SafeInt(reader, 33), TotalBedCapacity = SafeNullableInt(reader, 34), FloorCount = SafeNullableInt(reader, 35), ElevatorAvailable = SafeBool(reader, 36), ElevatorCount = SafeNullableInt(reader, 37), ShortDescription = SafeString(reader, 38), Description = SafeString(reader, 39),
            LocationDescription = SafeString(reader, 40), CommissionType = SafeString(reader, 41) ?? "sabit_oran", DefaultCommissionRate = SafeDecimalNullable(reader, 42) ?? 0, CommissionCalculationType = SafeString(reader, 43) ?? "toplam_tutar_uzerinden", PaymentTerm = SafeString(reader, 44) ?? "Çıkış Günü",
            PaymentMethod = SafeString(reader, 45) ?? "Havale/EFT", InvoiceType = SafeString(reader, 46) ?? "Otel Keser", DepositAmount = SafeDecimalNullable(reader, 47), DepositReturnDays = SafeNullableInt(reader, 48), MinStay = SafeNullableInt(reader, 49), MaxStay = SafeNullableInt(reader, 50),
            SpokenLanguages = SafeString(reader, 51), AverageScore = SafeDecimalNullable(reader, 52), TotalReviewCount = SafeNullableInt(reader, 53), CleanlinessScore = SafeDecimalNullable(reader, 54), ComfortScore = SafeDecimalNullable(reader, 55), LocationScore = SafeDecimalNullable(reader, 56), StaffScore = SafeDecimalNullable(reader, 57),
            PricePerformanceScore = SafeDecimalNullable(reader, 58), CoverPhotoPath = SafeString(reader, 59), VideoUrl = SafeString(reader, 60), VirtualTourUrl = SafeString(reader, 61), PublishStatus = SafeString(reader, 62) ?? "Taslak", ApprovalStatus = SafeString(reader, 63) ?? "Beklemede", PopularityOrder = SafeInt(reader, 64),
            IsFeatured = SafeBool(reader, 65), IsRecommended = SafeBool(reader, 66)
        };
    }

    private static async Task<List<AdminRoomCardViewModel>> LoadRoomCardsAsync(SqlConnection connection, long hotelId, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT id, [ODA_TIP_KODU], [ODA_ADI], [ODA_KATEGORISI], [STANDART_GECELIK_FIYAT], [MAKSIMUM_KISI_SAYISI], [TOPLAM_ODA_SAYISI], [KAPAK_FOTOGRAFI], [AKTIF_MI]
            FROM [dbo].[ODA_TIPLERI]
            WHERE [OTEL_ID] = @hotelId
            ORDER BY [AKTIF_MI] DESC, [SIRALAMA], id DESC;";
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@hotelId", hotelId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var rooms = new List<AdminRoomCardViewModel>();
        while (await reader.ReadAsync(cancellationToken))
        {
            rooms.Add(new AdminRoomCardViewModel
            {
                RoomId = reader.GetInt64(0),
                RoomCode = reader.GetString(1),
                RoomName = reader.GetString(2),
                Category = reader.GetString(3),
                PriceText = $"{(SafeDecimalNullable(reader, 4) ?? 0).ToString("0.##", CultureInfo.InvariantCulture)} TL",
                CapacityText = $"{SafeInt(reader, 5)} kisi",
                StockText = $"{SafeInt(reader, 6)} adet",
                CoverPhotoUrl = SafeString(reader, 7) ?? string.Empty,
                IsActive = SafeBool(reader, 8)
            });
        }
        return rooms;
    }

    private static async Task<AdminRoomEditForm> LoadRoomFormAsync(SqlConnection connection, long hotelId, long roomId, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT id, [OTEL_ID], [ODA_TIP_KODU], [ODA_ADI], [ODA_KATEGORISI], [MAKSIMUM_KISI_SAYISI], [MAKSIMUM_YETISKIN_SAYISI], [MAKSIMUM_COCUK_SAYISI],
                   [YATAK_TIPI], [YATAK_SAYISI], [EK_YATAK_EKLENEBILIR_MI], [ODA_METREKARE], [BALKON_VAR_MI], [BALKON_METREKARE], [MANZARA_TIPI],
                   [OZEL_BANYO_VAR_MI], [BANYO_TIPI], [STANDART_GECELIK_FIYAT], [HAFTASONU_FARK_ORANI], [COCUK_INDIRIM_ORANI], [BEBEK_UCRETSIZ_MI],
                   [BEBEK_YAS_SINIRI], [COCUK_YAS_SINIRI], [TOPLAM_ODA_SAYISI], [OVERBOOKING_LIMIT], [KAPAK_FOTOGRAFI], [GALERI], [OZELLIKLER], [AKTIF_MI], [SIRALAMA]
            FROM [dbo].[ODA_TIPLERI]
            WHERE id = @roomId AND [OTEL_ID] = @hotelId;";
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@roomId", roomId);
        command.Parameters.AddWithValue("@hotelId", hotelId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            throw new InvalidOperationException("Oda tipi bulunamadi.");
        }

        return new AdminRoomEditForm
        {
            RoomId = reader.GetInt64(0), HotelId = reader.GetInt64(1), RoomCode = reader.GetString(2), RoomName = reader.GetString(3), RoomCategory = reader.GetString(4), MaxPeople = SafeInt(reader, 5), MaxAdults = SafeInt(reader, 6), MaxChildren = SafeInt(reader, 7),
            BedType = SafeString(reader, 8), BedCount = SafeNullableInt(reader, 9), ExtraBedAvailable = SafeBool(reader, 10), RoomSize = SafeNullableInt(reader, 11), BalconyAvailable = SafeBool(reader, 12), BalconySize = SafeNullableInt(reader, 13),
            ViewType = SafeString(reader, 14), PrivateBathroom = SafeBool(reader, 15), BathroomType = SafeString(reader, 16), BasePrice = SafeDecimalNullable(reader, 17) ?? 0, WeekendDifferenceRate = SafeDecimalNullable(reader, 18), ChildDiscountRate = SafeDecimalNullable(reader, 19),
            BabyFree = SafeBool(reader, 20), BabyAgeLimit = SafeNullableInt(reader, 21), ChildAgeLimit = SafeNullableInt(reader, 22), TotalRooms = SafeInt(reader, 23), OverbookingLimit = SafeNullableInt(reader, 24), CoverPhotoPath = SafeString(reader, 25), GalleryJson = SafeString(reader, 26),
            FeaturesText = ParseJsonArray(SafeString(reader, 27)), IsActive = SafeBool(reader, 28), SortOrder = SafeNullableInt(reader, 29)
        };
    }

    private static async Task<List<AdminPhotoCardViewModel>> LoadHotelPhotosAsync(SqlConnection connection, long hotelId, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT id, [GORSEL_URL], COALESCE([BASLIK],''), COALESCE([ACIKLAMA],''), [GORSEL_TURU], [SIRALAMA], [KAPAK_FOTOGRAFI_MI], [ONAY_DURUMU]
            FROM [dbo].[OTEL_GORSELLERI]
            WHERE [OTEL_ID] = @hotelId
            ORDER BY [KAPAK_FOTOGRAFI_MI] DESC, [SIRALAMA], id DESC;";
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@hotelId", hotelId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var photos = new List<AdminPhotoCardViewModel>();
        while (await reader.ReadAsync(cancellationToken))
        {
            photos.Add(new AdminPhotoCardViewModel
            {
                PhotoId = reader.GetInt64(0), Url = reader.GetString(1), Title = reader.GetString(2), Description = reader.GetString(3), Type = reader.GetString(4), SortOrder = SafeInt(reader, 5), IsCover = SafeBool(reader, 6), IsApproved = string.Equals(reader.GetString(7), "Onaylandı", StringComparison.OrdinalIgnoreCase)
            });
        }
        return photos;
    }

    private static async Task<AdminHotelPhotoEditForm> LoadHotelPhotoEditFormAsync(SqlConnection connection, long hotelId, long photoId, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT id, COALESCE([BASLIK],''), [GORSEL_TURU], COALESCE([ACIKLAMA],''), [SIRALAMA], [ONE_CIKAN]
            FROM [dbo].[OTEL_GORSELLERI]
            WHERE id = @photoId AND [OTEL_ID] = @hotelId;";
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@photoId", photoId);
        command.Parameters.AddWithValue("@hotelId", hotelId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return new AdminHotelPhotoEditForm { HotelId = hotelId, PhotoType = "Genel Alan" };
        }

        return new AdminHotelPhotoEditForm
        {
            HotelId = hotelId,
            PhotoId = reader.GetInt64(0),
            Title = reader.GetString(1),
            PhotoType = reader.GetString(2),
            Description = reader.GetString(3),
            DisplayOrder = SafeInt(reader, 4),
            MarkAsFeatured = SafeBool(reader, 5)
        };
    }

    private static async Task<List<AdminRoomPhotoCardViewModel>> LoadRoomPhotosAsync(SqlConnection connection, long roomId, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT og.id, og.[ODA_TIP_ID], od.[ODA_ADI], og.[GORSEL_URL], COALESCE(og.[BASLIK],''), COALESCE(og.[ACIKLAMA],''), og.[SIRALAMA], og.[KAPAK_FOTOGRAFI_MI], og.[ONAY_DURUMU]
            FROM [dbo].[ODA_GORSELLERI] og
            INNER JOIN [dbo].[ODA_TIPLERI] od ON od.id = og.[ODA_TIP_ID]
            WHERE og.[ODA_TIP_ID] = @roomId
            ORDER BY og.[KAPAK_FOTOGRAFI_MI] DESC, og.[SIRALAMA], og.id DESC;";
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@roomId", roomId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var photos = new List<AdminRoomPhotoCardViewModel>();
        while (await reader.ReadAsync(cancellationToken))
        {
            photos.Add(new AdminRoomPhotoCardViewModel
            {
                PhotoId = reader.GetInt64(0), RoomId = reader.GetInt64(1), RoomName = reader.GetString(2), Url = reader.GetString(3), Title = reader.GetString(4), Description = reader.GetString(5), SortOrder = SafeInt(reader, 6), IsCover = SafeBool(reader, 7), IsApproved = string.Equals(reader.GetString(8), "Onaylandı", StringComparison.OrdinalIgnoreCase)
            });
        }
        return photos;
    }

    private static async Task<AdminRoomPhotoEditForm> LoadRoomPhotoEditFormAsync(SqlConnection connection, long roomId, long photoId, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT id, [ODA_TIP_ID], COALESCE([BASLIK],''), COALESCE([ACIKLAMA],''), [SIRALAMA]
            FROM [dbo].[ODA_GORSELLERI]
            WHERE id = @photoId AND [ODA_TIP_ID] = @roomId;";
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@photoId", photoId);
        command.Parameters.AddWithValue("@roomId", roomId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return new AdminRoomPhotoEditForm { RoomId = roomId };
        }

        return new AdminRoomPhotoEditForm
        {
            RoomId = reader.GetInt64(1), PhotoId = reader.GetInt64(0), Title = reader.GetString(2), Description = reader.GetString(3), DisplayOrder = SafeInt(reader, 4)
        };
    }

    private static async Task PromoteNextHotelCoverAsync(SqlConnection connection, SqlTransaction transaction, long hotelId, CancellationToken cancellationToken)
    {
        const string selectNextSql = "SELECT TOP (1) id, [GORSEL_URL] FROM [dbo].[OTEL_GORSELLERI] WHERE [OTEL_ID] = @hotelId ORDER BY [SIRALAMA], id;";
        await using var selectCommand = new SqlCommand(selectNextSql, connection, (SqlTransaction)transaction);
        selectCommand.Parameters.AddWithValue("@hotelId", hotelId);
        await using var reader = await selectCommand.ExecuteReaderAsync(cancellationToken);
        long? nextPhotoId = null;
        string? nextUrl = null;
        if (await reader.ReadAsync(cancellationToken))
        {
            nextPhotoId = reader.GetInt64(0);
            nextUrl = reader.GetString(1);
        }
        await reader.CloseAsync();

        if (nextPhotoId.HasValue)
        {
            await using var updatePhotos = new SqlCommand("UPDATE [dbo].[OTEL_GORSELLERI] SET [KAPAK_FOTOGRAFI_MI] = CASE WHEN id = @photoId THEN 1 ELSE 0 END WHERE [OTEL_ID] = @hotelId;", connection, (SqlTransaction)transaction);
            updatePhotos.Parameters.AddWithValue("@photoId", nextPhotoId.Value);
            updatePhotos.Parameters.AddWithValue("@hotelId", hotelId);
            await updatePhotos.ExecuteNonQueryAsync(cancellationToken);
        }

        await using var updateHotel = new SqlCommand("UPDATE [dbo].[OTELLER] SET [KAPAK_FOTOGRAFI] = @coverUrl WHERE id = @hotelId;", connection, (SqlTransaction)transaction);
        updateHotel.Parameters.AddWithValue("@coverUrl", (object?)nextUrl ?? DBNull.Value);
        updateHotel.Parameters.AddWithValue("@hotelId", hotelId);
        await updateHotel.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task PromoteNextRoomCoverAsync(SqlConnection connection, SqlTransaction transaction, long roomId, CancellationToken cancellationToken)
    {
        const string selectNextSql = "SELECT TOP (1) id, [GORSEL_URL] FROM [dbo].[ODA_GORSELLERI] WHERE [ODA_TIP_ID] = @roomId ORDER BY [SIRALAMA], id;";
        await using var selectCommand = new SqlCommand(selectNextSql, connection, (SqlTransaction)transaction);
        selectCommand.Parameters.AddWithValue("@roomId", roomId);
        await using var reader = await selectCommand.ExecuteReaderAsync(cancellationToken);
        long? nextPhotoId = null;
        string? nextUrl = null;
        if (await reader.ReadAsync(cancellationToken))
        {
            nextPhotoId = reader.GetInt64(0);
            nextUrl = reader.GetString(1);
        }
        await reader.CloseAsync();

        if (nextPhotoId.HasValue)
        {
            await using var updatePhotos = new SqlCommand("UPDATE [dbo].[ODA_GORSELLERI] SET [KAPAK_FOTOGRAFI_MI] = CASE WHEN id = @photoId THEN 1 ELSE 0 END WHERE [ODA_TIP_ID] = @roomId;", connection, (SqlTransaction)transaction);
            updatePhotos.Parameters.AddWithValue("@photoId", nextPhotoId.Value);
            updatePhotos.Parameters.AddWithValue("@roomId", roomId);
            await updatePhotos.ExecuteNonQueryAsync(cancellationToken);
        }

        await using var updateRoom = new SqlCommand("UPDATE [dbo].[ODA_TIPLERI] SET [KAPAK_FOTOGRAFI] = @coverUrl WHERE id = @roomId;", connection, (SqlTransaction)transaction);
        updateRoom.Parameters.AddWithValue("@coverUrl", (object?)nextUrl ?? DBNull.Value);
        updateRoom.Parameters.AddWithValue("@roomId", roomId);
        await updateRoom.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void BindRoomCommand(SqlCommand command, AdminRoomEditForm request, long hotelId, object featuresJson)
    {
        command.Parameters.AddWithValue("@hotelId", hotelId);
        command.Parameters.AddWithValue("@roomCode", string.IsNullOrWhiteSpace(request.RoomCode) ? BuildRoomCode(hotelId) : request.RoomCode.Trim());
        command.Parameters.AddWithValue("@roomName", request.RoomName.Trim());
        command.Parameters.AddWithValue("@roomCategory", request.RoomCategory);
        command.Parameters.AddWithValue("@maxPeople", request.MaxPeople);
        command.Parameters.AddWithValue("@maxAdults", request.MaxAdults);
        command.Parameters.AddWithValue("@maxChildren", request.MaxChildren);
        command.Parameters.AddWithValue("@bedType", DbValue(request.BedType));
        command.Parameters.AddWithValue("@bedCount", request.BedCount.HasValue ? request.BedCount.Value : DBNull.Value);
        command.Parameters.AddWithValue("@extraBedAvailable", request.ExtraBedAvailable ? 1 : 0);
        command.Parameters.AddWithValue("@roomSize", request.RoomSize.HasValue ? request.RoomSize.Value : DBNull.Value);
        command.Parameters.AddWithValue("@balconyAvailable", request.BalconyAvailable ? 1 : 0);
        command.Parameters.AddWithValue("@balconySize", request.BalconySize.HasValue ? request.BalconySize.Value : DBNull.Value);
        command.Parameters.AddWithValue("@viewType", DbValue(request.ViewType));
        command.Parameters.AddWithValue("@privateBathroom", request.PrivateBathroom ? 1 : 0);
        command.Parameters.AddWithValue("@bathroomType", DbValue(request.BathroomType));
        command.Parameters.AddWithValue("@basePrice", request.BasePrice);
        command.Parameters.AddWithValue("@weekendDifferenceRate", request.WeekendDifferenceRate.HasValue ? request.WeekendDifferenceRate.Value : DBNull.Value);
        command.Parameters.AddWithValue("@childDiscountRate", request.ChildDiscountRate.HasValue ? request.ChildDiscountRate.Value : DBNull.Value);
        command.Parameters.AddWithValue("@babyFree", request.BabyFree ? 1 : 0);
        command.Parameters.AddWithValue("@babyAgeLimit", request.BabyAgeLimit.HasValue ? request.BabyAgeLimit.Value : DBNull.Value);
        command.Parameters.AddWithValue("@childAgeLimit", request.ChildAgeLimit.HasValue ? request.ChildAgeLimit.Value : DBNull.Value);
        command.Parameters.AddWithValue("@totalRooms", request.TotalRooms);
        command.Parameters.AddWithValue("@overbookingLimit", request.OverbookingLimit.HasValue ? request.OverbookingLimit.Value : DBNull.Value);
        command.Parameters.AddWithValue("@coverPhotoPath", DbValue(request.CoverPhotoPath));
        command.Parameters.AddWithValue("@galleryJson", DbValue(request.GalleryJson));
        command.Parameters.AddWithValue("@featuresJson", featuresJson);
        command.Parameters.AddWithValue("@isActive", request.IsActive ? 1 : 0);
        command.Parameters.AddWithValue("@sortOrder", request.SortOrder.HasValue ? request.SortOrder.Value : DBNull.Value);
    }

    private static string BuildRoomCode(long hotelId)
    {
        var code = $"ADM-ODA-{hotelId}-{Guid.NewGuid():N}";
        return code[..Math.Min(30, code.Length)];
    }

    private static object DbValue(string? value) => string.IsNullOrWhiteSpace(value) ? DBNull.Value : value.Trim();
    private static object ParseTimeOrDbNull(string? value) => TimeSpan.TryParse(value, out var parsed) ? parsed : DBNull.Value;

    private static string ParseJsonArray(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return string.Empty;
        try
        {
            var values = JsonSerializer.Deserialize<List<string>>(json);
            return values is null ? string.Empty : string.Join(", ", values);
        }
        catch
        {
            return json;
        }
    }

    private static string? SafeString(SqlDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    private static bool SafeBool(SqlDataReader reader, int ordinal) => !reader.IsDBNull(ordinal) && Convert.ToInt32(reader.GetValue(ordinal), CultureInfo.InvariantCulture) == 1;
    private static int SafeInt(SqlDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? 0 : Convert.ToInt32(reader.GetValue(ordinal), CultureInfo.InvariantCulture);
    private static int? SafeNullableInt(SqlDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : Convert.ToInt32(reader.GetValue(ordinal), CultureInfo.InvariantCulture);
    private static decimal? SafeDecimalNullable(SqlDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : Convert.ToDecimal(reader.GetValue(ordinal), CultureInfo.InvariantCulture);
    private static string? SafeTime(SqlDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetTimeSpan(ordinal).ToString(@"hh\:mm", CultureInfo.InvariantCulture);
}
