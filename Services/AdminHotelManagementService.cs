using System.Globalization;
using System.Text.Json;
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

    public async Task<AdminHotelsPageViewModel> GetHotelsPageAsync(string fullName, string email, string userRole, string? searchTerm = null, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var shell = await BuildShellAsync(connection, "Oteller", "Otel kayitlarini, odalari ve medya varliklarini admin tarafindan hizli sekilde yonetin.", fullName, email, userRole, cancellationToken);
        var model = new AdminHotelsPageViewModel
        {
            Shell = shell,
            SearchTerm = searchTerm?.Trim() ?? string.Empty
        };

        model.SummaryCards.AddRange(await LoadHotelSummaryCardsAsync(connection, cancellationToken));

        const string sql = @"
            SELECT TOP (120) o.id,
                   o.otel_kodu,
                   o.otel_adi,
                   o.otel_turu,
                   CONCAT(o.ilce, ', ', o.sehir) AS konum,
                   o.yayin_durumu,
                   o.onay_durumu,
                   o.ortalama_puan,
                   COALESCE(rooms.room_count, 0) AS room_count,
                   COALESCE(hotelPhotos.hotel_photo_count, 0) AS hotel_photo_count,
                   COALESCE(roomPhotos.room_photo_count, 0) AS room_photo_count,
                   o.one_cikan_otel
            FROM oteller o
            LEFT JOIN (
                SELECT otel_id, COUNT(*) AS room_count
                FROM oda_tipleri
                GROUP BY otel_id
            ) rooms ON rooms.otel_id = o.id
            LEFT JOIN (
                SELECT otel_id, COUNT(*) AS hotel_photo_count
                FROM otel_gorselleri
                GROUP BY otel_id
            ) hotelPhotos ON hotelPhotos.otel_id = o.id
            LEFT JOIN (
                SELECT od.otel_id, COUNT(og.id) AS room_photo_count
                FROM oda_tipleri od
                LEFT JOIN oda_gorselleri og ON og.oda_tip_id = od.id
                GROUP BY od.otel_id
            ) roomPhotos ON roomPhotos.otel_id = o.id
            WHERE (@search = '' OR o.otel_adi LIKE '%' + @search + '%' OR o.otel_kodu LIKE '%' + @search + '%' OR o.sehir LIKE '%' + @search + '%' OR o.ilce LIKE '%' + @search + '%')
            ORDER BY o.id DESC;";

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@search", model.SearchTerm);
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
                IsFeatured = !reader.IsDBNull(11) && reader.GetBoolean(11)
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

        const string sql = @"
            UPDATE oteller
            SET otel_kodu = @hotelCode,
                partner_id = @partnerId,
                user_id = @userId,
                otel_adi = @hotelName,
                otel_turu = @hotelType,
                yildiz_sayisi = @starCount,
                turizm_belge_no = @tourismDocumentNo,
                turizm_belge_turu = @tourismDocumentType,
                ulke = @country,
                sehir = @city,
                ilce = @district,
                mahalle = @neighborhood,
                tam_adres = @address,
                posta_kodu = @postalCode,
                enlem = @latitude,
                boylam = @longitude,
                telefon_1 = @phone1,
                telefon_2 = @phone2,
                faks = @fax,
                eposta = @contactEmail,
                web_sitesi = @website,
                rezervasyon_telefonu = @reservationPhone,
                satis_kontak_adi = @salesContactName,
                satis_kontak_telefonu = @salesContactPhone,
                satis_kontak_eposta = @salesContactEmail,
                satis_notlari = @salesNotes,
                check_in_saati = @checkInTime,
                check_out_saati = @checkOutTime,
                gec_check_out_mumkun_mu = @lateCheckoutAvailable,
                gec_check_out_ucreti = @lateCheckoutFee,
                erken_check_in_mumkun_mu = @earlyCheckInAvailable,
                erken_check_in_ucreti = @earlyCheckInFee,
                toplam_oda_sayisi = @totalRoomCount,
                toplam_yatak_kapasitesi = @totalBedCapacity,
                kat_sayisi = @floorCount,
                asansor_var_mi = @elevatorAvailable,
                asansor_sayisi = @elevatorCount,
                kisa_aciklama = @shortDescription,
                uzun_aciklama = @description,
                konum_aciklamasi = @locationDescription,
                komisyon_turu = @commissionType,
                varsayilan_komisyon_orani = @defaultCommissionRate,
                komisyon_hesaplama_tipi = @commissionCalculationType,
                odeme_vadesi = @paymentTerm,
                odeme_yontemi = @paymentMethod,
                fatura_kesim_turu = @invoiceType,
                depozito_tutari = @depositAmount,
                depozito_iade_suresi = @depositReturnDays,
                minimum_konaklama_gecesi = @minStay,
                maksimum_konaklama_gecesi = @maxStay,
                konusulan_diller = @spokenLanguages,
                ortalama_puan = @averageScore,
                toplam_yorum_sayisi = @totalReviewCount,
                temizlik_puani = @cleanlinessScore,
                konfor_puani = @comfortScore,
                konum_puani = @locationScore,
                personel_puani = @staffScore,
                fiyat_performans_puani = @pricePerformanceScore,
                kapak_fotografi = @coverPhotoPath,
                video_url = @videoUrl,
                sanal_tur_url = @virtualTourUrl,
                yayin_durumu = @publishStatus,
                onay_durumu = @approvalStatus,
                onaylayan_admin_id = @adminUserId,
                onay_tarihi = CASE WHEN @approvalStatus = 'Onaylandı' THEN SYSUTCDATETIME() ELSE onay_tarihi END,
                populerlik_sirasi = @popularityOrder,
                one_cikan_otel = @isFeatured,
                tavsiye_edilen_otel = @isRecommended,
                guncellenme_tarihi = SYSUTCDATETIME()
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

        return (true, "Otel bilgileri admin panelinden guncellendi.");
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
                UPDATE oda_tipleri
                SET oda_tip_kodu = @roomCode,
                    oda_adi = @roomName,
                    oda_kategorisi = @roomCategory,
                    maksimum_kisi_sayisi = @maxPeople,
                    maksimum_yetiskin_sayisi = @maxAdults,
                    maksimum_cocuk_sayisi = @maxChildren,
                    yatak_tipi = @bedType,
                    yatak_sayisi = @bedCount,
                    ek_yatak_eklenebilir_mi = @extraBedAvailable,
                    oda_metrekare = @roomSize,
                    balkon_var_mi = @balconyAvailable,
                    balkon_metrekare = @balconySize,
                    manzara_tipi = @viewType,
                    ozel_banyo_var_mi = @privateBathroom,
                    banyo_tipi = @bathroomType,
                    standart_gecelik_fiyat = @basePrice,
                    haftasonu_fark_orani = @weekendDifferenceRate,
                    cocuk_indirim_orani = @childDiscountRate,
                    bebek_ucretsiz_mi = @babyFree,
                    bebek_yas_siniri = @babyAgeLimit,
                    cocuk_yas_siniri = @childAgeLimit,
                    toplam_oda_sayisi = @totalRooms,
                    overbooking_limit = @overbookingLimit,
                    kapak_fotografi = @coverPhotoPath,
                    galeri = @galleryJson,
                    ozellikler = @featuresJson,
                    aktif_mi = @isActive,
                    siralama = @sortOrder,
                    guncellenme_tarihi = SYSUTCDATETIME()
                WHERE id = @roomId AND otel_id = @hotelId;";

            await using var command = new SqlCommand(updateSql, connection);
            BindRoomCommand(command, request, hotel.HotelId, featuresJson);
            command.Parameters.AddWithValue("@roomId", request.RoomId.Value);
            await command.ExecuteNonQueryAsync(cancellationToken);
            return (true, "Oda tipi guncellendi.");
        }

        const string insertSql = @"
            INSERT INTO oda_tipleri
            (otel_id, oda_tip_kodu, oda_adi, oda_kategorisi, maksimum_kisi_sayisi, maksimum_yetiskin_sayisi, maksimum_cocuk_sayisi, yatak_tipi, yatak_sayisi, ek_yatak_eklenebilir_mi, oda_metrekare, balkon_var_mi, balkon_metrekare, manzara_tipi, ozel_banyo_var_mi, banyo_tipi, standart_gecelik_fiyat, haftasonu_fark_orani, cocuk_indirim_orani, bebek_ucretsiz_mi, bebek_yas_siniri, cocuk_yas_siniri, toplam_oda_sayisi, overbooking_limit, kapak_fotografi, galeri, ozellikler, aktif_mi, siralama)
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
                UPDATE oteller
                SET yayin_durumu = 'Kapatıldı',
                    onaylayan_admin_id = @adminUserId,
                    guncellenme_tarihi = SYSUTCDATETIME()
                WHERE id = @hotelId;";
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@hotelId", hotelId);
            command.Parameters.AddWithValue("@adminUserId", adminUserId);
            var affectedRows = await command.ExecuteNonQueryAsync(cancellationToken);
            return affectedRows > 0
                ? (true, "Otel pasif duruma alindi.")
                : (false, "Otel bulunamadi veya guncellenemedi.");
        }

        public async Task<(bool Success, string Message)> DeactivateRoomAsync(long hotelId, long roomId, CancellationToken cancellationToken = default)
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            await EnsureHotelExistsAsync(connection, hotelId, cancellationToken);

            const string sql = "UPDATE oda_tipleri SET aktif_mi = 0, guncellenme_tarihi = SYSUTCDATETIME() WHERE id = @roomId AND otel_id = @hotelId;";
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
        var targetDirectory = Path.Combine(_environment.WebRootPath, "uploads", "hotels", "admin", hotel.HotelId.ToString(CultureInfo.InvariantCulture));
        Directory.CreateDirectory(targetDirectory);
        var savedPhysicalPaths = new List<string>();
        var displayOrder = request.DisplayOrder;

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            var shouldMakeCover = request.MakeCover;
            foreach (var file in request.Files.Where(static item => item.Length > 0))
            {
                var storedImage = await _imageStorageService.SaveAsWebpAsync(file, targetDirectory, $"admin-otel-{hotel.HotelId}", cancellationToken);
                var relativePath = $"/uploads/hotels/admin/{hotel.HotelId}/{storedImage.FileName}";
                savedPhysicalPaths.Add(Path.Combine(targetDirectory, storedImage.FileName));

                const string insertSql = @"
                    INSERT INTO otel_gorselleri
                    (otel_id, gorsel_url, gorsel_turu, baslik, aciklama, kapak_fotografi_mi, one_cikan, siralama, boyut_kb, onay_durumu, onaylayan_admin_id, onay_tarihi, yukleyen_kullanici_id)
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
                    await using var resetCover = new SqlCommand("UPDATE otel_gorselleri SET kapak_fotografi_mi = CASE WHEN gorsel_url = @coverUrl THEN 1 ELSE 0 END WHERE otel_id = @hotelId;", connection, (SqlTransaction)transaction);
                    resetCover.Parameters.AddWithValue("@coverUrl", relativePath);
                    resetCover.Parameters.AddWithValue("@hotelId", hotel.HotelId);
                    await resetCover.ExecuteNonQueryAsync(cancellationToken);

                    await using var updateHotel = new SqlCommand("UPDATE oteller SET kapak_fotografi = @coverUrl WHERE id = @hotelId;", connection, (SqlTransaction)transaction);
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
            UPDATE otel_gorselleri
            SET baslik = @title,
                gorsel_turu = @photoType,
                aciklama = @description,
                siralama = @displayOrder,
                one_cikan = @featured
            WHERE id = @photoId AND otel_id = @hotelId;";
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

        const string selectSql = "SELECT TOP (1) gorsel_url FROM otel_gorselleri WHERE id = @photoId AND otel_id = @hotelId;";
        await using var selectCommand = new SqlCommand(selectSql, connection);
        selectCommand.Parameters.AddWithValue("@photoId", photoId);
        selectCommand.Parameters.AddWithValue("@hotelId", hotelId);
        var url = await selectCommand.ExecuteScalarAsync(cancellationToken) as string;
        if (string.IsNullOrWhiteSpace(url))
        {
            return (false, "Kapak yapilacak otel fotografi bulunamadi.");
        }

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await using (var updatePhotos = new SqlCommand("UPDATE otel_gorselleri SET kapak_fotografi_mi = CASE WHEN id = @photoId THEN 1 ELSE 0 END WHERE otel_id = @hotelId;", connection, (SqlTransaction)transaction))
        {
            updatePhotos.Parameters.AddWithValue("@photoId", photoId);
            updatePhotos.Parameters.AddWithValue("@hotelId", hotelId);
            await updatePhotos.ExecuteNonQueryAsync(cancellationToken);
        }
        await using (var updateHotel = new SqlCommand("UPDATE oteller SET kapak_fotografi = @coverUrl WHERE id = @hotelId;", connection, (SqlTransaction)transaction))
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
        const string selectSql = "SELECT TOP (1) gorsel_url, kapak_fotografi_mi FROM otel_gorselleri WHERE id = @photoId AND otel_id = @hotelId;";
        await using (var selectCommand = new SqlCommand(selectSql, connection))
        {
            selectCommand.Parameters.AddWithValue("@photoId", photoId);
            selectCommand.Parameters.AddWithValue("@hotelId", hotelId);
            await using var reader = await selectCommand.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                relativePath = reader.GetString(0);
                wasCover = reader.GetBoolean(1);
            }
        }

        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return (false, "Silinecek otel fotografi bulunamadi.");
        }

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await using (var deleteCommand = new SqlCommand("DELETE FROM otel_gorselleri WHERE id = @photoId AND otel_id = @hotelId;", connection, (SqlTransaction)transaction))
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
        var targetDirectory = Path.Combine(_environment.WebRootPath, "uploads", "hotels", "admin", request.HotelId.ToString(CultureInfo.InvariantCulture), "rooms", request.RoomId.ToString(CultureInfo.InvariantCulture));
        Directory.CreateDirectory(targetDirectory);
        var savedPhysicalPaths = new List<string>();
        var displayOrder = request.DisplayOrder;

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            var shouldMakeCover = request.MakeCover;
            foreach (var file in request.Files.Where(static item => item.Length > 0))
            {
                var storedImage = await _imageStorageService.SaveAsWebpAsync(file, targetDirectory, $"admin-oda-{room.RoomId}", cancellationToken);
                var relativePath = $"/uploads/hotels/admin/{request.HotelId}/rooms/{request.RoomId}/{storedImage.FileName}";
                savedPhysicalPaths.Add(Path.Combine(targetDirectory, storedImage.FileName));

                const string insertSql = @"
                    INSERT INTO oda_gorselleri
                    (oda_tip_id, gorsel_url, baslik, aciklama, kapak_fotografi_mi, siralama, boyut_kb, onay_durumu, onaylayan_admin_id, onay_tarihi, yukleyen_kullanici_id)
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
                    await using var resetCover = new SqlCommand("UPDATE oda_gorselleri SET kapak_fotografi_mi = CASE WHEN gorsel_url = @coverUrl THEN 1 ELSE 0 END WHERE oda_tip_id = @roomId;", connection, (SqlTransaction)transaction);
                    resetCover.Parameters.AddWithValue("@coverUrl", relativePath);
                    resetCover.Parameters.AddWithValue("@roomId", request.RoomId);
                    await resetCover.ExecuteNonQueryAsync(cancellationToken);

                    await using var updateRoom = new SqlCommand("UPDATE oda_tipleri SET kapak_fotografi = @coverUrl WHERE id = @roomId;", connection, (SqlTransaction)transaction);
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
            UPDATE oda_gorselleri
            SET baslik = @title,
                aciklama = @description,
                siralama = @displayOrder
            WHERE id = @photoId AND oda_tip_id = @roomId;";
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

        const string selectSql = "SELECT TOP (1) gorsel_url FROM oda_gorselleri WHERE id = @photoId AND oda_tip_id = @roomId;";
        await using var selectCommand = new SqlCommand(selectSql, connection);
        selectCommand.Parameters.AddWithValue("@photoId", photoId);
        selectCommand.Parameters.AddWithValue("@roomId", roomId);
        var url = await selectCommand.ExecuteScalarAsync(cancellationToken) as string;
        if (string.IsNullOrWhiteSpace(url))
        {
            return (false, "Kapak yapilacak oda fotografi bulunamadi.");
        }

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await using (var updatePhotos = new SqlCommand("UPDATE oda_gorselleri SET kapak_fotografi_mi = CASE WHEN id = @photoId THEN 1 ELSE 0 END WHERE oda_tip_id = @roomId;", connection, (SqlTransaction)transaction))
        {
            updatePhotos.Parameters.AddWithValue("@photoId", photoId);
            updatePhotos.Parameters.AddWithValue("@roomId", roomId);
            await updatePhotos.ExecuteNonQueryAsync(cancellationToken);
        }
        await using (var updateRoom = new SqlCommand("UPDATE oda_tipleri SET kapak_fotografi = @coverUrl WHERE id = @roomId;", connection, (SqlTransaction)transaction))
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
        const string selectSql = "SELECT TOP (1) gorsel_url, kapak_fotografi_mi FROM oda_gorselleri WHERE id = @photoId AND oda_tip_id = @roomId;";
        await using (var selectCommand = new SqlCommand(selectSql, connection))
        {
            selectCommand.Parameters.AddWithValue("@photoId", photoId);
            selectCommand.Parameters.AddWithValue("@roomId", roomId);
            await using var reader = await selectCommand.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                relativePath = reader.GetString(0);
                wasCover = reader.GetBoolean(1);
            }
        }

        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return (false, "Silinecek oda fotografi bulunamadi.");
        }

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await using (var deleteCommand = new SqlCommand("DELETE FROM oda_gorselleri WHERE id = @photoId AND oda_tip_id = @roomId;", connection, (SqlTransaction)transaction))
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
                (SELECT COUNT(*) FROM partner_detaylari WHERE onay_durumu = 'Beklemede') AS pending_partner_applications,
                (SELECT COUNT(*) FROM sistem_ici_bildirimler WHERE okundu_mu = 0) AS unread_notifications,
                (SELECT COUNT(*) FROM sistem_hata_loglari WHERE hata_seviyesi IN ('CRITICAL','ALERT','EMERGENCY') AND cozuldu_mu = 0) AS critical_logs,
                (SELECT COUNT(*) FROM yorumlar WHERE onay_durumu = 'Beklemede') AS pending_reviews;";

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
            ("Toplam Otel", "SELECT COUNT(*) FROM oteller", "Tum tesis kayitlari", "info", "fa-hotel"),
            ("Yayinda", "SELECT COUNT(*) FROM oteller WHERE yayin_durumu = 'Yayında'", "Canli listelenen oteller", "success", "fa-tower-broadcast"),
            ("Oda Tipi", "SELECT COUNT(*) FROM oda_tipleri", "Toplam oda tipi baglari", "warning", "fa-bed"),
            ("Gorsel", "SELECT (SELECT COUNT(*) FROM otel_gorselleri) + (SELECT COUNT(*) FROM oda_gorselleri)", "Otel ve oda medya varliklari", "danger", "fa-images")
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

    private static async Task<List<AdminSummaryCardViewModel>> LoadHotelManagementCardsAsync(SqlConnection connection, long hotelId, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT
                (SELECT COUNT(*) FROM oda_tipleri WHERE otel_id = @hotelId) AS room_count,
                (SELECT COUNT(*) FROM otel_gorselleri WHERE otel_id = @hotelId) AS hotel_photo_count,
                (SELECT COUNT(*) FROM oda_gorselleri og INNER JOIN oda_tipleri od ON od.id = og.oda_tip_id WHERE od.otel_id = @hotelId) AS room_photo_count,
                (SELECT COUNT(*) FROM rezervasyonlar WHERE otel_id = @hotelId) AS reservation_count;";

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
        const string sql = "SELECT TOP (1) id, otel_adi FROM oteller WHERE id = @hotelId;";
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
        const string sql = "SELECT TOP (1) id, oda_adi FROM oda_tipleri WHERE id = @roomId AND otel_id = @hotelId;";
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
            SELECT id, otel_kodu, partner_id, user_id, otel_adi, otel_turu, yildiz_sayisi, turizm_belge_no, turizm_belge_turu,
                   ulke, sehir, ilce, mahalle, tam_adres, posta_kodu, enlem, boylam, telefon_1, telefon_2, faks, eposta, web_sitesi,
                   rezervasyon_telefonu, satis_kontak_adi, satis_kontak_telefonu, satis_kontak_eposta, satis_notlari,
                   check_in_saati, check_out_saati, gec_check_out_mumkun_mu, gec_check_out_ucreti, erken_check_in_mumkun_mu, erken_check_in_ucreti,
                   toplam_oda_sayisi, toplam_yatak_kapasitesi, kat_sayisi, asansor_var_mi, asansor_sayisi, kisa_aciklama, uzun_aciklama,
                   konum_aciklamasi, komisyon_turu, varsayilan_komisyon_orani, komisyon_hesaplama_tipi, odeme_vadesi, odeme_yontemi, fatura_kesim_turu,
                   depozito_tutari, depozito_iade_suresi, minimum_konaklama_gecesi, maksimum_konaklama_gecesi, konusulan_diller,
                   ortalama_puan, toplam_yorum_sayisi, temizlik_puani, konfor_puani, konum_puani, personel_puani, fiyat_performans_puani,
                   kapak_fotografi, video_url, sanal_tur_url, yayin_durumu, onay_durumu, populerlik_sirasi, one_cikan_otel, tavsiye_edilen_otel
            FROM oteller WHERE id = @hotelId;";

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
            StarCount = reader.IsDBNull(6) ? null : reader.GetByte(6), TourismDocumentNo = SafeString(reader, 7), TourismDocumentType = SafeString(reader, 8), Country = SafeString(reader, 9) ?? "Türkiye", City = reader.GetString(10), District = reader.GetString(11),
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
            SELECT id, oda_tip_kodu, oda_adi, oda_kategorisi, standart_gecelik_fiyat, maksimum_kisi_sayisi, toplam_oda_sayisi, kapak_fotografi, aktif_mi
            FROM oda_tipleri
            WHERE otel_id = @hotelId
            ORDER BY aktif_mi DESC, siralama, id DESC;";
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
            SELECT id, otel_id, oda_tip_kodu, oda_adi, oda_kategorisi, maksimum_kisi_sayisi, maksimum_yetiskin_sayisi, maksimum_cocuk_sayisi,
                   yatak_tipi, yatak_sayisi, ek_yatak_eklenebilir_mi, oda_metrekare, balkon_var_mi, balkon_metrekare, manzara_tipi,
                   ozel_banyo_var_mi, banyo_tipi, standart_gecelik_fiyat, haftasonu_fark_orani, cocuk_indirim_orani, bebek_ucretsiz_mi,
                   bebek_yas_siniri, cocuk_yas_siniri, toplam_oda_sayisi, overbooking_limit, kapak_fotografi, galeri, ozellikler, aktif_mi, siralama
            FROM oda_tipleri
            WHERE id = @roomId AND otel_id = @hotelId;";
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
            SELECT id, gorsel_url, COALESCE(baslik,''), COALESCE(aciklama,''), gorsel_turu, siralama, kapak_fotografi_mi, onay_durumu
            FROM otel_gorselleri
            WHERE otel_id = @hotelId
            ORDER BY kapak_fotografi_mi DESC, siralama, id DESC;";
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
            SELECT id, COALESCE(baslik,''), gorsel_turu, COALESCE(aciklama,''), siralama, one_cikan
            FROM otel_gorselleri
            WHERE id = @photoId AND otel_id = @hotelId;";
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
            SELECT og.id, og.oda_tip_id, od.oda_adi, og.gorsel_url, COALESCE(og.baslik,''), COALESCE(og.aciklama,''), og.siralama, og.kapak_fotografi_mi, og.onay_durumu
            FROM oda_gorselleri og
            INNER JOIN oda_tipleri od ON od.id = og.oda_tip_id
            WHERE og.oda_tip_id = @roomId
            ORDER BY og.kapak_fotografi_mi DESC, og.siralama, og.id DESC;";
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
            SELECT id, oda_tip_id, COALESCE(baslik,''), COALESCE(aciklama,''), siralama
            FROM oda_gorselleri
            WHERE id = @photoId AND oda_tip_id = @roomId;";
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
        const string selectNextSql = "SELECT TOP (1) id, gorsel_url FROM otel_gorselleri WHERE otel_id = @hotelId ORDER BY siralama, id;";
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
            await using var updatePhotos = new SqlCommand("UPDATE otel_gorselleri SET kapak_fotografi_mi = CASE WHEN id = @photoId THEN 1 ELSE 0 END WHERE otel_id = @hotelId;", connection, (SqlTransaction)transaction);
            updatePhotos.Parameters.AddWithValue("@photoId", nextPhotoId.Value);
            updatePhotos.Parameters.AddWithValue("@hotelId", hotelId);
            await updatePhotos.ExecuteNonQueryAsync(cancellationToken);
        }

        await using var updateHotel = new SqlCommand("UPDATE oteller SET kapak_fotografi = @coverUrl WHERE id = @hotelId;", connection, (SqlTransaction)transaction);
        updateHotel.Parameters.AddWithValue("@coverUrl", (object?)nextUrl ?? DBNull.Value);
        updateHotel.Parameters.AddWithValue("@hotelId", hotelId);
        await updateHotel.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task PromoteNextRoomCoverAsync(SqlConnection connection, SqlTransaction transaction, long roomId, CancellationToken cancellationToken)
    {
        const string selectNextSql = "SELECT TOP (1) id, gorsel_url FROM oda_gorselleri WHERE oda_tip_id = @roomId ORDER BY siralama, id;";
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
            await using var updatePhotos = new SqlCommand("UPDATE oda_gorselleri SET kapak_fotografi_mi = CASE WHEN id = @photoId THEN 1 ELSE 0 END WHERE oda_tip_id = @roomId;", connection, (SqlTransaction)transaction);
            updatePhotos.Parameters.AddWithValue("@photoId", nextPhotoId.Value);
            updatePhotos.Parameters.AddWithValue("@roomId", roomId);
            await updatePhotos.ExecuteNonQueryAsync(cancellationToken);
        }

        await using var updateRoom = new SqlCommand("UPDATE oda_tipleri SET kapak_fotografi = @coverUrl WHERE id = @roomId;", connection, (SqlTransaction)transaction);
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
    private static bool SafeBool(SqlDataReader reader, int ordinal) => !reader.IsDBNull(ordinal) && reader.GetBoolean(ordinal);
    private static int SafeInt(SqlDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? 0 : Convert.ToInt32(reader.GetValue(ordinal), CultureInfo.InvariantCulture);
    private static int? SafeNullableInt(SqlDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : Convert.ToInt32(reader.GetValue(ordinal), CultureInfo.InvariantCulture);
    private static decimal? SafeDecimalNullable(SqlDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : Convert.ToDecimal(reader.GetValue(ordinal), CultureInfo.InvariantCulture);
    private static string? SafeTime(SqlDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetTimeSpan(ordinal).ToString(@"hh\:mm", CultureInfo.InvariantCulture);
}
