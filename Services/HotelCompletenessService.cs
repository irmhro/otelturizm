using System.Globalization;
using Microsoft.Data.SqlClient;
using otelturizmnew.Models;
using otelturizmnew.Models.Paneller.Admin;
using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Services;

public class HotelCompletenessService : IHotelCompletenessService
{
    private readonly string _connectionString;

    public HotelCompletenessService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection tanimli degil.");
    }

    public HotelCompletenessSnapshot Evaluate(AdminHotelEditForm form, int roomCount, int hotelPhotoCount)
    {
        var rules = BuildRules(form, roomCount, hotelPhotoCount);
        var missing = rules.Where(x => x.IsMissing).ToList();
        var completed = rules.Count - missing.Count;
        var score = rules.Count == 0 ? 100 : (int)Math.Round(completed * 100.0 / rules.Count, MidpointRounding.AwayFromZero);

        return new HotelCompletenessSnapshot
        {
            Score = score,
            TotalRules = rules.Count,
            CompletedRules = completed,
            MissingCount = missing.Count,
            CriticalMissingCount = missing.Count(x => string.Equals(x.Severity, "critical", StringComparison.OrdinalIgnoreCase)),
            Rules = rules
        };
    }

    public async Task<PartnerHotelCompletenessViewModel?> GetPartnerHotelCompletenessAsync(long hotelId, CancellationToken cancellationToken = default)
    {
        if (hotelId <= 0)
        {
            return null;
        }

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            var payload = await LoadHotelPayloadAsync(connection, hotelId, cancellationToken);
            return payload is null ? null : MapPartnerViewModel(hotelId, payload.Value.Form, payload.Value.RoomCount, payload.Value.HotelPhotoCount, payload.Value.HotelName);
        }
        catch (SqlException)
        {
            return null;
        }
    }

    public async Task<List<PartnerHotelCompletenessViewModel>> GetPartnerManagedHotelsCompletenessAsync(long userId, CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
        {
            return new List<PartnerHotelCompletenessViewModel>();
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string hotelIdsSql = @"
            SELECT oks.[OTEL_ID]
            FROM [dbo].[OTEL_KULLANICI_SAHIPLIKLERI] oks
            WHERE oks.[KULLANICI_ID] = @userId
              AND oks.[AKTIF_MI] = 1
            ORDER BY oks.[OTEL_ID];";

        var hotelIds = new List<long>();
        await using (var command = new SqlCommand(hotelIdsSql, connection))
        {
            command.Parameters.AddWithValue("@userId", userId);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                hotelIds.Add(reader.GetInt64(0));
            }
        }

        var results = new List<PartnerHotelCompletenessViewModel>();
        foreach (var hotelId in hotelIds)
        {
            try
            {
                var payload = await LoadHotelPayloadAsync(connection, hotelId, cancellationToken);
                if (payload is null)
                {
                    continue;
                }

                results.Add(MapPartnerViewModel(hotelId, payload.Value.Form, payload.Value.RoomCount, payload.Value.HotelPhotoCount, payload.Value.HotelName));
            }
            catch (SqlException)
            {
                // Tek otel sorgusu başarısız olsa bile diğerlerini göster.
            }
        }

        return results.OrderBy(x => x.CompletenessScore).ThenByDescending(x => x.MissingCount).ToList();
    }

    private static PartnerHotelCompletenessViewModel MapPartnerViewModel(long hotelId, AdminHotelEditForm form, int roomCount, int hotelPhotoCount, string hotelName)
    {
        var snapshot = EvaluateStatic(form, roomCount, hotelPhotoCount);

        return new PartnerHotelCompletenessViewModel
        {
            HotelId = hotelId,
            HotelName = hotelName,
            CompletenessScore = snapshot.Score,
            MissingCount = snapshot.MissingCount,
            CriticalMissingCount = snapshot.CriticalMissingCount,
            MissingItems = snapshot.MissingRules.Select(rule => new PartnerCompletenessItemViewModel
            {
                FieldKey = rule.FieldKey,
                Label = rule.FieldLabel,
                Severity = rule.Severity,
                FixUrl = BuildPartnerFixUrl(hotelId, rule.PartnerFixPath),
                IconClass = rule.IconClass
            }).ToList()
        };
    }

    private static HotelCompletenessSnapshot EvaluateStatic(AdminHotelEditForm form, int roomCount, int hotelPhotoCount)
    {
        var rules = BuildRules(form, roomCount, hotelPhotoCount);
        var missing = rules.Where(x => x.IsMissing).ToList();
        var completed = rules.Count - missing.Count;
        var score = rules.Count == 0 ? 100 : (int)Math.Round(completed * 100.0 / rules.Count, MidpointRounding.AwayFromZero);
        return new HotelCompletenessSnapshot
        {
            Score = score,
            TotalRules = rules.Count,
            CompletedRules = completed,
            MissingCount = missing.Count,
            CriticalMissingCount = missing.Count(x => string.Equals(x.Severity, "critical", StringComparison.OrdinalIgnoreCase)),
            Rules = rules
        };
    }

    private async Task<(AdminHotelEditForm Form, int RoomCount, int HotelPhotoCount, string HotelName)?> LoadHotelPayloadAsync(SqlConnection connection, long hotelId, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT TOP (1)
                   o.[OTEL_KODU],
                   o.[PARTNER_ID],
                   o.[KULLANICI_ID],
                   o.[OTEL_ADI],
                   o.[OTEL_TURU],
                   o.[YILDIZ_SAYISI],
                   o.[TURIZM_BELGE_NO],
                   o.[TURIZM_BELGE_TURU],
                   o.ulke,
                   o.[SEHIR],
                   o.ilce,
                   o.[MAHALLE],
                   o.[TAM_ADRES],
                   o.[POSTA_KODU],
                   o.[ENLEM],
                   o.[BOYLAM],
                   o.[TELEFON_1],
                   o.[TELEFON_2],
                   o.faks,
                   o.[EPOSTA],
                   o.[WEB_SITESI],
                   o.[REZERVASYON_TELEFONU],
                   o.[SATIS_KONTAK_ADI],
                   o.[SATIS_KONTAK_TELEFONU],
                   o.[SATIS_KONTAK_EPOSTA],
                   o.[SATIS_NOTLARI],
                   o.[CHECK_IN_SAATI],
                   o.[CHECK_OUT_SAATI],
                   o.[GEC_CHECK_OUT_MUMKUN_MU],
                   o.[GEC_CHECK_OUT_UCRETI],
                   o.[ERKEN_CHECK_IN_MUMKUN_MU],
                   o.[ERKEN_CHECK_IN_UCRETI],
                   o.[TOPLAM_ODA_SAYISI],
                   o.[TOPLAM_YATAK_KAPASITESI],
                   o.[KAT_SAYISI],
                   o.[ASANSOR_VAR_MI],
                   o.[ASANSOR_SAYISI],
                   o.[KISA_ACIKLAMA],
                   o.[UZUN_ACIKLAMA],
                   o.[KONUM_ACIKLAMASI],
                   o.[KOMISYON_TURU],
                   o.[VARSAYILAN_KOMISYON_ORANI],
                   o.[KOMISYON_HESAPLAMA_TIPI],
                   o.[ODEME_VADESI],
                   o.[ODEME_YONTEMI],
                   o.[FATURA_KESIM_TURU],
                   o.[DEPOZITO_TUTARI],
                   o.[DEPOZITO_IADE_SURESI],
                   o.[MINIMUM_KONAKLAMA_GECESI],
                   o.[MAKSIMUM_KONAKLAMA_GECESI],
                   o.[KONUSULAN_DILLER],
                   o.[ORTALAMA_PUAN],
                   o.[TOPLAM_YORUM_SAYISI],
                   o.[TEMIZLIK_PUANI],
                   o.[KONFOR_PUANI],
                   o.[KONUM_PUANI],
                   o.[PERSONEL_PUANI],
                   o.[FIYAT_PERFORMANS_PUANI],
                   o.[KAPAK_FOTOGRAFI],
                   o.[VIDEO_URL],
                   o.[SANAL_TUR_URL],
                   o.[YAYIN_DURUMU],
                   o.[ONAY_DURUMU],
                   o.[POPULERLIK_SIRASI],
                   o.[ONE_CIKAN_OTEL],
                   o.[TAVSIYE_EDILEN],
                   COALESCE(rooms.room_count, 0) AS room_count,
                   COALESCE(photos.photo_count, 0) AS photo_count
            FROM [dbo].[OTELLER] o
            LEFT JOIN (
                SELECT [OTEL_ID], COUNT(*) AS room_count
                FROM [dbo].[ODA_TIPLERI]
                WHERE [AKTIF_MI] = 1
                GROUP BY [OTEL_ID]
            ) rooms ON rooms.[OTEL_ID] = o.id
            LEFT JOIN (
                SELECT [OTEL_ID], COUNT(*) AS photo_count
                FROM [dbo].[OTEL_GORSELLERI]
                GROUP BY [OTEL_ID]
            ) photos ON photos.[OTEL_ID] = o.id
            WHERE o.id = @hotelId;";

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@hotelId", hotelId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        var form = new AdminHotelEditForm
        {
            HotelId = hotelId,
            HotelCode = reader.IsDBNull(0) ? string.Empty : reader.GetString(0),
            PartnerId = reader.IsDBNull(1) ? 0 : reader.GetInt64(1),
            UserId = reader.IsDBNull(2) ? null : reader.GetInt64(2),
            HotelName = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
            HotelType = reader.IsDBNull(4) ? "Otel" : reader.GetString(4),
            StarCount = reader.IsDBNull(5) ? null : reader.GetInt32(5),
            TourismDocumentNo = reader.IsDBNull(6) ? null : reader.GetString(6),
            TourismDocumentType = reader.IsDBNull(7) ? null : reader.GetString(7),
            Country = reader.IsDBNull(8) ? "Türkiye" : reader.GetString(8),
            City = reader.IsDBNull(9) ? string.Empty : reader.GetString(9),
            District = reader.IsDBNull(10) ? string.Empty : reader.GetString(10),
            Neighborhood = reader.IsDBNull(11) ? null : reader.GetString(11),
            Address = reader.IsDBNull(12) ? string.Empty : reader.GetString(12),
            PostalCode = reader.IsDBNull(13) ? null : reader.GetString(13),
            Latitude = reader.IsDBNull(14) ? null : reader.GetDecimal(14),
            Longitude = reader.IsDBNull(15) ? null : reader.GetDecimal(15),
            Phone1 = reader.IsDBNull(16) ? string.Empty : reader.GetString(16),
            Phone2 = reader.IsDBNull(17) ? null : reader.GetString(17),
            Fax = reader.IsDBNull(18) ? null : reader.GetString(18),
            ContactEmail = reader.IsDBNull(19) ? string.Empty : reader.GetString(19),
            Website = reader.IsDBNull(20) ? null : reader.GetString(20),
            ReservationPhone = reader.IsDBNull(21) ? null : reader.GetString(21),
            SalesContactName = reader.IsDBNull(22) ? null : reader.GetString(22),
            SalesContactPhone = reader.IsDBNull(23) ? null : reader.GetString(23),
            SalesContactEmail = reader.IsDBNull(24) ? null : reader.GetString(24),
            SalesNotes = reader.IsDBNull(25) ? null : reader.GetString(25),
            CheckInTime = reader.IsDBNull(26) ? null : reader.GetString(26),
            CheckOutTime = reader.IsDBNull(27) ? null : reader.GetString(27),
            LateCheckoutAvailable = !reader.IsDBNull(28) && reader.GetBoolean(28),
            LateCheckoutFee = reader.IsDBNull(29) ? null : reader.GetDecimal(29),
            EarlyCheckInAvailable = !reader.IsDBNull(30) && reader.GetBoolean(30),
            EarlyCheckInFee = reader.IsDBNull(31) ? null : reader.GetDecimal(31),
            TotalRoomCount = reader.IsDBNull(32) ? 0 : reader.GetInt32(32),
            TotalBedCapacity = reader.IsDBNull(33) ? null : reader.GetInt32(33),
            FloorCount = reader.IsDBNull(34) ? null : reader.GetInt32(34),
            ElevatorAvailable = !reader.IsDBNull(35) && reader.GetBoolean(35),
            ElevatorCount = reader.IsDBNull(36) ? null : reader.GetInt32(36),
            ShortDescription = reader.IsDBNull(37) ? null : reader.GetString(37),
            Description = reader.IsDBNull(38) ? null : reader.GetString(38),
            LocationDescription = reader.IsDBNull(39) ? null : reader.GetString(39),
            CommissionType = reader.IsDBNull(40) ? "sabit_oran" : reader.GetString(40),
            DefaultCommissionRate = reader.IsDBNull(41) ? 0 : reader.GetDecimal(41),
            CommissionCalculationType = reader.IsDBNull(42) ? "toplam_tutar_uzerinden" : reader.GetString(42),
            PaymentTerm = reader.IsDBNull(43) ? "Çıkış Günü" : reader.GetString(43),
            PaymentMethod = reader.IsDBNull(44) ? "Havale/EFT" : reader.GetString(44),
            InvoiceType = reader.IsDBNull(45) ? "Otel Keser" : reader.GetString(45),
            DepositAmount = reader.IsDBNull(46) ? null : reader.GetDecimal(46),
            DepositReturnDays = reader.IsDBNull(47) ? null : reader.GetInt32(47),
            MinStay = reader.IsDBNull(48) ? null : reader.GetInt32(48),
            MaxStay = reader.IsDBNull(49) ? null : reader.GetInt32(49),
            SpokenLanguages = reader.IsDBNull(50) ? null : reader.GetString(50),
            AverageScore = reader.IsDBNull(51) ? null : reader.GetDecimal(51),
            TotalReviewCount = reader.IsDBNull(52) ? null : reader.GetInt32(52),
            CleanlinessScore = reader.IsDBNull(53) ? null : reader.GetDecimal(53),
            ComfortScore = reader.IsDBNull(54) ? null : reader.GetDecimal(54),
            LocationScore = reader.IsDBNull(55) ? null : reader.GetDecimal(55),
            StaffScore = reader.IsDBNull(56) ? null : reader.GetDecimal(56),
            PricePerformanceScore = reader.IsDBNull(57) ? null : reader.GetDecimal(57),
            CoverPhotoPath = reader.IsDBNull(58) ? null : reader.GetString(58),
            VideoUrl = reader.IsDBNull(59) ? null : reader.GetString(59),
            VirtualTourUrl = reader.IsDBNull(60) ? null : reader.GetString(60),
            PublishStatus = reader.IsDBNull(61) ? "Taslak" : reader.GetString(61),
            ApprovalStatus = reader.IsDBNull(62) ? "Beklemede" : reader.GetString(62),
            PopularityOrder = reader.IsDBNull(63) ? 0 : reader.GetInt32(63),
            IsFeatured = !reader.IsDBNull(64) && reader.GetBoolean(64),
            IsRecommended = !reader.IsDBNull(65) && reader.GetBoolean(65)
        };

        var roomCount = reader.IsDBNull(66) ? 0 : Convert.ToInt32(reader.GetValue(66), CultureInfo.InvariantCulture);
        var photoCount = reader.IsDBNull(67) ? 0 : Convert.ToInt32(reader.GetValue(67), CultureInfo.InvariantCulture);
        return (form, roomCount, photoCount, form.HotelName);
    }

    private static List<HotelCompletenessRuleResult> BuildRules(AdminHotelEditForm form, int roomCount, int hotelPhotoCount)
    {
        var rules = new List<HotelCompletenessRuleResult>();

        void Add(string key, string label, bool missing, string severity, string tab, string partnerPath, string icon = "fa-circle-exclamation")
        {
            rules.Add(new HotelCompletenessRuleResult
            {
                FieldKey = key,
                FieldLabel = label,
                Severity = severity,
                AdminTabTarget = tab,
                PartnerFixPath = partnerPath,
                IconClass = icon,
                IsMissing = missing
            });
        }

        Add("hotel_name", "Otel adı", string.IsNullOrWhiteSpace(form.HotelName), "critical", "tab-genel", "/panel/partner/otel-bilgileri", "fa-hotel");
        Add("hotel_code", "Otel kodu", string.IsNullOrWhiteSpace(form.HotelCode), "critical", "tab-genel", "/panel/partner/otel-bilgileri", "fa-barcode");
        Add("city", "Şehir", string.IsNullOrWhiteSpace(form.City), "critical", "tab-konum", "/panel/partner/tesis/konum", "fa-location-dot");
        Add("district", "İlçe", string.IsNullOrWhiteSpace(form.District), "critical", "tab-konum", "/panel/partner/tesis/konum", "fa-map");
        Add("address", "Tam adres", string.IsNullOrWhiteSpace(form.Address), "critical", "tab-konum", "/panel/partner/tesis/konum", "fa-road");
        Add("phone", "Telefon", string.IsNullOrWhiteSpace(form.Phone1), "critical", "tab-genel", "/panel/partner/otel-bilgileri", "fa-phone");
        Add("email", "E-posta", string.IsNullOrWhiteSpace(form.ContactEmail), "critical", "tab-genel", "/panel/partner/otel-bilgileri", "fa-envelope");
        Add("coordinates", "Koordinat (enlem/boylam)", !form.Latitude.HasValue || !form.Longitude.HasValue, "warning", "tab-konum", "/panel/partner/tesis/konum#harita", "fa-map-pin");
        Add("description", "Açıklama", string.IsNullOrWhiteSpace(form.ShortDescription) && string.IsNullOrWhiteSpace(form.Description), "warning", "tab-genel", "/panel/partner/otel-bilgileri", "fa-align-left");
        Add("cover_photo", "Kapak / otel görseli", string.IsNullOrWhiteSpace(form.CoverPhotoPath) && hotelPhotoCount == 0, "warning", "tab-gorseller", "/panel/partner/fotograflar", "fa-image");
        Add("room_types", "En az bir oda tipi", roomCount == 0, "critical", "tab-odalar", "/panel/partner/oda-yonetimi", "fa-bed");
        Add("approval", "Onay durumu (Onaylandı değil)", !IsApproved(form.ApprovalStatus), "warning", "tab-onay", "/panel/partner/otel-bilgileri", "fa-circle-check");
        Add("tourism_doc", "Turizm belge no", string.IsNullOrWhiteSpace(form.TourismDocumentNo), "warning", "tab-onay", "/panel/partner/basvuru-ve-evraklar", "fa-file-contract");

        return rules;
    }

    private static bool IsApproved(string? approvalStatus)
    {
        var normalized = (approvalStatus ?? string.Empty).Trim();
        return string.Equals(normalized, "Onaylandı", StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalized, "Onaylandi", StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildPartnerFixUrl(long hotelId, string path)
    {
        var separator = path.Contains('?', StringComparison.Ordinal) ? "&" : "?";
        return $"{path}{separator}otelId={hotelId}";
    }
}
