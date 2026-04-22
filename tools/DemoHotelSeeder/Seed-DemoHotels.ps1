param(
    [string]$ConnectionString = "Server=(localdb)\MSSQLLocalDB;Database=otelturizm_2026db;Trusted_Connection=True;TrustServerCertificate=True;",
    [int]$DaysToSeed = 90
)

Add-Type -AssemblyName System.Data
Add-Type -AssemblyName System.Drawing

$projectRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$hotelImageSourceRoot = Join-Path $projectRoot 'wwwroot\uploads\demo\hotels\seyhli-grand-hotel'
$roomImageSourceRoot = Join-Path $projectRoot 'wwwroot\uploads\demo\rooms\seyhli-grand-hotel'
$hotelOutputRoot = Join-Path $projectRoot 'wwwroot\uploads\demo\hotels'
$roomOutputRoot = Join-Path $projectRoot 'wwwroot\uploads\demo\rooms'

$hotelImageTypes = @('dis_mekan', 'lobi', 'restoran', 'havuz', 'manzara', 'genel_alan', 'spa', 'kahvalti', 'fitness', 'teras')
$roomTemplates = @(
    @{
        Key = 'standard'
        SourceKey = 'standart'
        Name = 'Standart Oda'
        Code = 'STD'
        Category = 'Standart'
        MaxPerson = 2
        MaxAdult = 2
        MaxChild = 1
        Bed = 'Queen Bed'
        BedCount = 1
        RoomCount = 12
        FeatureIds = @(1, 6, 9, 15, 20, 24, 25)
        Description = 'Sehir manzarali, hizli internet ve rahat calisma masasi sunan fonksiyonel oda.'
    },
    @{
        Key = 'deluxe'
        SourceKey = 'deluxe'
        Name = 'Deluxe Oda'
        Code = 'DLX'
        Category = 'Deluxe'
        MaxPerson = 3
        MaxAdult = 2
        MaxChild = 1
        Bed = 'King Bed'
        BedCount = 1
        RoomCount = 10
        FeatureIds = @(1, 11, 12, 15, 20, 24, 25)
        Description = 'Daha genis yasam alani, akilli TV ve oturma kosesiyle premium deneyim sunar.'
    },
    @{
        Key = 'suite'
        SourceKey = 'suite'
        Name = 'Aile Suiti'
        Code = 'SUI'
        Category = 'Suite'
        MaxPerson = 4
        MaxAdult = 3
        MaxChild = 2
        Bed = 'King + Sofa'
        BedCount = 2
        RoomCount = 6
        FeatureIds = @(1, 12, 13, 15, 20, 24, 25, 29)
        Description = 'Aileler icin ekstra oturma alani, yemek bolumu ve yuksek kapasite sunan suit plan.'
    }
)

$hotelConfigs = @(
    @{
        HotelCode = 'OTLTRZM-NVS-URG-0002'
        Slug = 'kapadokya-panorama-cave'
        Name = 'Kapadokya Panorama Cave Hotel'
        HotelType = 'Boutique Cave Hotel'
        StarCount = 5
        Country = 'Turkiye'
        City = 'Nevsehir'
        District = 'Urgup'
        Neighborhood = 'Esbelli'
        Address = 'Esbelli Mah. Panorama Sok. No:7 Urgup / Nevsehir'
        PostalCode = '50400'
        Phone = '03842120002'
        ReservationPhone = '03842120012'
        Email = 'rezervasyon@kapadokyapanorama.demo'
        Website = 'https://kapadokyapanorama.demo.otelturizm.test'
        SalesName = 'Deniz Kara'
        SalesPhone = '05320001112'
        SalesEmail = 'satis@kapadokyapanorama.demo'
        Floors = 4
        ElevatorCount = 2
        TotalRooms = 28
        TotalBeds = 68
        Summary = 'Balon manzarasi, tas odalari ve butik servis ritmiyle Kapadokya deneyimini test etmek icin hazir demo tesis.'
        LongDescription = 'Kapadokya Panorama Cave Hotel; tas doku, teras kahvalti ve premium oda galerileriyle listeleme, detay ve rezervasyon akislarini test etmek icin uretilmis tam kapsamli demo tesistir.'
        LocationDescription = 'Urgup merkezine ve gun dogumu izleme noktalarina kisa surus mesafesindedir.'
        Languages = 'Turkce, Ingilizce'
        Rating = 9.1
        ReviewCount = 186
        Cleanliness = 9.0
        Comfort = 9.1
        LocationScore = 9.4
        StaffScore = 9.2
        PriceScore = 8.8
        Featured = 1
        Recommended = 1
        Accent = @{
            R = 1.08; G = 0.98; B = 0.88; Brightness = 0.02; FlipOdd = $false
        }
        HotelFeatureIds = @(1, 14, 15, 20, 23, 25, 38, 43)
        BasePrices = @{
            standard = 5850
            deluxe = 7425
            suite = 9850
        }
    },
    @{
        HotelCode = 'OTLTRZM-MUG-BDR-0003'
        Slug = 'bodrum-azure-resort'
        Name = 'Bodrum Azure Resort'
        HotelType = 'Beach Resort'
        StarCount = 5
        Country = 'Turkiye'
        City = 'Mugla'
        District = 'Bodrum'
        Neighborhood = 'Bitez'
        Address = 'Bitez Sahil Yolu No:18 Bodrum / Mugla'
        PostalCode = '48470'
        Phone = '02523100003'
        ReservationPhone = '02523100013'
        Email = 'rezervasyon@bodrumazure.demo'
        Website = 'https://bodrumazure.demo.otelturizm.test'
        SalesName = 'Selin Demir'
        SalesPhone = '05320001113'
        SalesEmail = 'satis@bodrumazure.demo'
        Floors = 6
        ElevatorCount = 3
        TotalRooms = 34
        TotalBeds = 82
        Summary = 'Sahil hissi, havuz alanlari ve resort vitriniyle yaz sezonu sayfalarini test etmek icin hazir demo tesis.'
        LongDescription = 'Bodrum Azure Resort; havuz, restoran, spa ve aile odakli alanlariyla ana sayfa, kampanya ve otel detay tasarimlarinda kullanilmak uzere kurgulanmis demo tesistir.'
        LocationDescription = 'Bitez sahiline yurume mesafesinde, Bodrum merkezine hizli ulasim sunar.'
        Languages = 'Turkce, Ingilizce'
        Rating = 8.9
        ReviewCount = 241
        Cleanliness = 8.8
        Comfort = 9.0
        LocationScore = 9.1
        StaffScore = 8.9
        PriceScore = 8.5
        Featured = 1
        Recommended = 1
        Accent = @{
            R = 0.90; G = 1.03; B = 1.12; Brightness = 0.03; FlipOdd = $true
        }
        HotelFeatureIds = @(1, 9, 14, 20, 23, 24, 25, 29, 31, 38, 40, 47)
        BasePrices = @{
            standard = 7250
            deluxe = 9180
            suite = 12840
        }
    },
    @{
        HotelCode = 'OTLTRZM-KOC-KRT-0004'
        Slug = 'kartepe-forest-lodge'
        Name = 'Kartepe Forest Lodge'
        HotelType = 'Mountain Lodge'
        StarCount = 4
        Country = 'Turkiye'
        City = 'Kocaeli'
        District = 'Kartepe'
        Neighborhood = 'Masukiye'
        Address = 'Masukiye Orman Yolu No:4 Kartepe / Kocaeli'
        PostalCode = '41295'
        Phone = '02623550004'
        ReservationPhone = '02623550014'
        Email = 'rezervasyon@kartepeforest.demo'
        Website = 'https://kartepeforest.demo.otelturizm.test'
        SalesName = 'Can Yildiz'
        SalesPhone = '05320001114'
        SalesEmail = 'satis@kartepeforest.demo'
        Floors = 3
        ElevatorCount = 1
        TotalRooms = 24
        TotalBeds = 56
        Summary = 'Doga, kis sezonu ve aile odakli kacamak akislarini test etmek icin kurgulanmis orman temali demo tesis.'
        LongDescription = 'Kartepe Forest Lodge; kis ve doga konseptli listeleme kartlari, oda galeri denemeleri ve panel akislarinda gercekci veri saglamak icin hazirlandi.'
        LocationDescription = 'Masukiye merkezine yakin, kayak rotalarina aracla kisa surede erisilir.'
        Languages = 'Turkce, Ingilizce'
        Rating = 8.7
        ReviewCount = 154
        Cleanliness = 8.8
        Comfort = 8.7
        LocationScore = 8.9
        StaffScore = 9.0
        PriceScore = 8.6
        Featured = 0
        Recommended = 1
        Accent = @{
            R = 0.92; G = 1.08; B = 0.92; Brightness = 0.01; FlipOdd = $false
        }
        HotelFeatureIds = @(1, 15, 19, 20, 23, 25, 31, 38, 43, 47)
        BasePrices = @{
            standard = 4680
            deluxe = 6240
            suite = 8920
        }
    },
    @{
        HotelCode = 'OTLTRZM-IZM-CSM-0005'
        Slug = 'alacati-windmill-house'
        Name = 'Alacati Windmill House'
        HotelType = 'Boutique Hotel'
        StarCount = 4
        Country = 'Turkiye'
        City = 'Izmir'
        District = 'Cesme'
        Neighborhood = 'Alacati'
        Address = 'Alacati Mah. Yel Degirmeni Sok. No:11 Cesme / Izmir'
        PostalCode = '35937'
        Phone = '02327100005'
        ReservationPhone = '02327100015'
        Email = 'rezervasyon@alacatiwindmill.demo'
        Website = 'https://alacatiwindmill.demo.otelturizm.test'
        SalesName = 'Ece Tan'
        SalesPhone = '05320001115'
        SalesEmail = 'satis@alacatiwindmill.demo'
        Floors = 4
        ElevatorCount = 1
        TotalRooms = 26
        TotalBeds = 60
        Summary = 'Tasarim oteli hissi, kahvalti ve butik deneyim odagini test etmek icin uretilmis sahil kasabasi demo tesisi.'
        LongDescription = 'Alacati Windmill House; butik otel kartlari, kampanya vitrini ve mobil detay sayfalarinda kullanilacak gorsel ile veri yogunlugunu saglamak icin olusturuldu.'
        LocationDescription = 'Alacati merkezine yurume mesafesinde, restoran ve plaj rotalarina yakin konumdadir.'
        Languages = 'Turkce, Ingilizce'
        Rating = 8.8
        ReviewCount = 173
        Cleanliness = 8.9
        Comfort = 8.8
        LocationScore = 9.2
        StaffScore = 9.0
        PriceScore = 8.4
        Featured = 0
        Recommended = 1
        Accent = @{
            R = 1.05; G = 1.00; B = 0.92; Brightness = 0.03; FlipOdd = $true
        }
        HotelFeatureIds = @(1, 9, 20, 23, 24, 25, 38, 40, 47)
        BasePrices = @{
            standard = 6540
            deluxe = 8260
            suite = 11120
        }
    }
)

function Open-Connection {
    $connection = New-Object System.Data.SqlClient.SqlConnection $ConnectionString
    $connection.Open()
    return $connection
}

function Invoke-NonQuery {
    param(
        [System.Data.SqlClient.SqlConnection]$Connection,
        [string]$Sql,
        [hashtable]$Parameters = @{}
    )
    $command = $Connection.CreateCommand()
    $command.CommandText = $Sql
    foreach ($key in $Parameters.Keys) {
        $null = $command.Parameters.AddWithValue($key, $Parameters[$key])
    }
    $null = $command.ExecuteNonQuery()
    $command.Dispose()
}

function Invoke-Scalar {
    param(
        [System.Data.SqlClient.SqlConnection]$Connection,
        [string]$Sql,
        [hashtable]$Parameters = @{}
    )
    $command = $Connection.CreateCommand()
    $command.CommandText = $Sql
    foreach ($key in $Parameters.Keys) {
        $null = $command.Parameters.AddWithValue($key, $Parameters[$key])
    }
    $result = $command.ExecuteScalar()
    $command.Dispose()
    return $result
}

function Remove-HotelData {
    param(
        [System.Data.SqlClient.SqlConnection]$Connection,
        [string]$HotelCode
    )

    $hotelId = Invoke-Scalar -Connection $Connection -Sql "SELECT TOP 1 id FROM oteller WHERE otel_kodu = @hotelCode;" -Parameters @{ '@hotelCode' = $HotelCode }
    if ($null -eq $hotelId -or $hotelId -eq [DBNull]::Value) {
        return
    }

    $deleteSql = @"
DELETE FROM kampanya_oteller WHERE otel_id = @hotelId;
DELETE FROM oda_fiyat_musaitlik WHERE otel_id = @hotelId;
DELETE FROM oda_gorselleri WHERE oda_tip_id IN (SELECT id FROM oda_tipleri WHERE otel_id = @hotelId);
DELETE FROM oda_tipi_ozellikleri WHERE oda_tip_id IN (SELECT id FROM oda_tipleri WHERE otel_id = @hotelId);
DELETE FROM oda_tipleri WHERE otel_id = @hotelId;
DELETE FROM otel_gorselleri WHERE otel_id = @hotelId;
DELETE FROM otel_ozellik_iliskileri WHERE otel_id = @hotelId;
DELETE FROM oteller WHERE id = @hotelId;
"@
    Invoke-NonQuery -Connection $Connection -Sql $deleteSql -Parameters @{ '@hotelId' = [int64]$hotelId }
}

function New-ColorMatrix {
    param($accent)

    $matrix = New-Object System.Drawing.Imaging.ColorMatrix
    $matrix.Matrix00 = [single]$accent.R
    $matrix.Matrix11 = [single]$accent.G
    $matrix.Matrix22 = [single]$accent.B
    $matrix.Matrix33 = 1.0
    $matrix.Matrix40 = [single]$accent.Brightness
    $matrix.Matrix41 = [single]$accent.Brightness
    $matrix.Matrix42 = [single]$accent.Brightness
    $matrix.Matrix44 = 1.0
    return $matrix
}

function New-VariantImage {
    param(
        [string]$SourcePath,
        [string]$DestinationPath,
        [hashtable]$Accent,
        [int]$Index
    )

    $sourceImage = [System.Drawing.Image]::FromFile($SourcePath)
    if ($Accent.FlipOdd -and ($Index % 2 -eq 1)) {
        $sourceImage.RotateFlip([System.Drawing.RotateFlipType]::RotateNoneFlipX)
    }

    $bitmap = New-Object System.Drawing.Bitmap $sourceImage.Width, $sourceImage.Height
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
    $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic

    $imageAttributes = New-Object System.Drawing.Imaging.ImageAttributes
    $imageAttributes.SetColorMatrix((New-ColorMatrix $Accent))

    $rect = New-Object System.Drawing.Rectangle 0, 0, $bitmap.Width, $bitmap.Height
    $graphics.DrawImage($sourceImage, $rect, 0, 0, $sourceImage.Width, $sourceImage.Height, [System.Drawing.GraphicsUnit]::Pixel, $imageAttributes)

    $overlayBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(18 + ($Index % 3) * 8, 8, 15, 32))
    $graphics.FillRectangle($overlayBrush, 0, 0, $bitmap.Width, $bitmap.Height)

    $penColor = [System.Drawing.Color]::FromArgb(65, [int](255 * [math]::Min($Accent.R, 1.0)), [int](255 * [math]::Min($Accent.G, 1.0)), [int](255 * [math]::Min($Accent.B, 1.0)))
    $framePen = New-Object System.Drawing.Pen ($penColor, 10)
    $graphics.DrawRectangle($framePen, 5, 5, $bitmap.Width - 10, $bitmap.Height - 10)

    $bitmap.Save($DestinationPath, [System.Drawing.Imaging.ImageFormat]::Jpeg)

    $framePen.Dispose()
    $overlayBrush.Dispose()
    $imageAttributes.Dispose()
    $graphics.Dispose()
    $bitmap.Dispose()
    $sourceImage.Dispose()
}

function New-ImageSet {
    param($config)

    $hotelFolder = Join-Path $hotelOutputRoot $config.Slug
    $roomFolder = Join-Path $roomOutputRoot $config.Slug
    New-Item -ItemType Directory -Path $hotelFolder -Force | Out-Null
    New-Item -ItemType Directory -Path $roomFolder -Force | Out-Null

    $hotelImages = @()
    for ($i = 1; $i -le 10; $i++) {
        $sourcePath = Join-Path $hotelImageSourceRoot ("hotel-{0}.jpg" -f $i)
        $destinationPath = Join-Path $hotelFolder ("hotel-{0}.jpg" -f $i)
        New-VariantImage -SourcePath $sourcePath -DestinationPath $destinationPath -Accent $config.Accent -Index $i
        $hotelImages += [pscustomobject]@{
            Url = "/uploads/demo/hotels/$($config.Slug)/hotel-$i.jpg"
            Path = $destinationPath
            Type = $hotelImageTypes[$i - 1]
            Title = "$($config.Name) Gorsel $i"
            Description = "$($config.Name) demo hotel image $i"
            Order = $i
            IsCover = ($i -eq 1)
            IsFeatured = ($i -le 2)
        }
    }

    $roomImages = @{}
    foreach ($template in $roomTemplates) {
        $items = @()
        for ($i = 1; $i -le 5; $i++) {
            $sourcePath = Join-Path $roomImageSourceRoot ("{0}-{1}.jpg" -f $template.SourceKey, $i)
            $destinationPath = Join-Path $roomFolder ("{0}-{1}.jpg" -f $template.Key, $i)
            New-VariantImage -SourcePath $sourcePath -DestinationPath $destinationPath -Accent $config.Accent -Index ($i + 10)
            $items += [pscustomobject]@{
                Url = "/uploads/demo/rooms/$($config.Slug)/$($template.Key)-$i.jpg"
                Path = $destinationPath
                Title = "$($template.Name) $i"
                Description = "$($config.Name) $($template.Name) demo image $i"
                Order = $i
                IsCover = ($i -eq 1)
            }
        }
        $roomImages[$template.Key] = $items
    }

    return @{
        Hotel = $hotelImages
        Rooms = $roomImages
    }
}

function Get-ImageInfo {
    param([string]$Path)
    $fileInfo = Get-Item $Path
    $image = [System.Drawing.Image]::FromFile($Path)
    try {
        return @{
            SizeKb = [int][math]::Ceiling($fileInfo.Length / 1kb)
            Width = $image.Width
            Height = $image.Height
        }
    }
    finally {
        $image.Dispose()
    }
}

function Get-PriceForDate {
    param(
        [int]$BasePrice,
        [datetime]$Date
    )

    $price = $BasePrice
    if ($Date.DayOfWeek -in @([DayOfWeek]::Friday, [DayOfWeek]::Saturday)) {
        $price += [int][math]::Round($BasePrice * 0.08)
    }
    elseif ($Date.DayOfWeek -eq [DayOfWeek]::Sunday) {
        $price += [int][math]::Round($BasePrice * 0.04)
    }

    if ($Date.Month -in 6, 7, 8) {
        $price += [int][math]::Round($BasePrice * 0.06)
    }

    return $price
}

$connection = Open-Connection
try {
    $publishedStatus = Invoke-Scalar -Connection $connection -Sql "SELECT TOP 1 yayin_durumu FROM oteller WHERE id = 45;"
    $approvalStatus = Invoke-Scalar -Connection $connection -Sql "SELECT TOP 1 onay_durumu FROM oteller WHERE id = 45;"
    $approvalDate = Get-Date

    foreach ($config in $hotelConfigs) {
        Remove-HotelData -Connection $connection -HotelCode $config.HotelCode
        $imageSet = New-ImageSet -config $config
        $hotelGalleryJson = (($imageSet.Hotel | ForEach-Object { $_.Url }) | ConvertTo-Json -Compress)

        $insertHotelSql = @"
INSERT INTO oteller
(
    otel_kodu, partner_id, user_id, otel_adi, otel_turu, yildiz_sayisi, turizm_belge_no, turizm_belge_turu,
    ulke, sehir, ilce, mahalle, tam_adres, posta_kodu, telefon_1, eposta, web_sitesi, rezervasyon_telefonu,
    satis_kontak_adi, satis_kontak_telefonu, satis_kontak_eposta, check_in_saati, check_out_saati, toplam_oda_sayisi,
    toplam_yatak_kapasitesi, kat_sayisi, asansor_var_mi, asansor_sayisi, kisa_aciklama, uzun_aciklama, konum_aciklamasi,
    komisyon_turu, varsayilan_komisyon_orani, komisyon_hesaplama_tipi, odeme_vadesi, odeme_yontemi, fatura_kesim_turu,
    minimum_konaklama_gecesi, maksimum_konaklama_gecesi, konusulan_diller, ortalama_puan, toplam_yorum_sayisi,
    temizlik_puani, konfor_puani, konum_puani, personel_puani, fiyat_performans_puani, kapak_fotografi, galeri,
    yayin_durumu, onay_durumu, onay_tarihi, onaylayan_admin_id, populerlik_sirasi, one_cikan_otel, tavsiye_edilen_otel,
    olusturulma_tarihi, guncellenme_tarihi
)
VALUES
(
    @hotelCode, 45, 101, @name, @hotelType, @starCount, @documentNo, NULL,
    @country, @city, @district, @neighborhood, @address, @postalCode, @phone, @email, @website, @reservationPhone,
    @salesName, @salesPhone, @salesEmail, '14:00', '12:00', @totalRooms,
    @totalBeds, @floors, 1, @elevatorCount, @summary, @longDescription, @locationDescription,
    'sabit_oran', 0, 'gecelik_fiyat_uzerinden', 'Rezervasyon Aninda', 'Havale/EFT', 'Otel Keser',
    1, 30, @languages, @rating, @reviewCount,
    @cleanliness, @comfort, @locationScore, @staffScore, @priceScore, @coverPhoto, @gallery,
    @publishedStatus, @approvalStatus, @approvalDate, 32, 1, @featured, @recommended,
    SYSUTCDATETIME(), SYSUTCDATETIME()
);
SELECT CAST(SCOPE_IDENTITY() AS bigint);
"@

        $hotelId = [int64](Invoke-Scalar -Connection $connection -Sql $insertHotelSql -Parameters @{
            '@hotelCode' = $config.HotelCode
            '@name' = $config.Name
            '@hotelType' = $config.HotelType
            '@starCount' = $config.StarCount
            '@documentNo' = ("TRB-DEMO-{0}" -f $config.HotelCode.Substring($config.HotelCode.Length - 4))
            '@country' = $config.Country
            '@city' = $config.City
            '@district' = $config.District
            '@neighborhood' = $config.Neighborhood
            '@address' = $config.Address
            '@postalCode' = $config.PostalCode
            '@phone' = $config.Phone
            '@email' = $config.Email
            '@website' = $config.Website
            '@reservationPhone' = $config.ReservationPhone
            '@salesName' = $config.SalesName
            '@salesPhone' = $config.SalesPhone
            '@salesEmail' = $config.SalesEmail
            '@totalRooms' = $config.TotalRooms
            '@totalBeds' = $config.TotalBeds
            '@floors' = $config.Floors
            '@elevatorCount' = $config.ElevatorCount
            '@summary' = $config.Summary
            '@longDescription' = $config.LongDescription
            '@locationDescription' = $config.LocationDescription
            '@languages' = $config.Languages
            '@rating' = [decimal]$config.Rating
            '@reviewCount' = [int]$config.ReviewCount
            '@cleanliness' = [decimal]$config.Cleanliness
            '@comfort' = [decimal]$config.Comfort
            '@locationScore' = [decimal]$config.LocationScore
            '@staffScore' = [decimal]$config.StaffScore
            '@priceScore' = [decimal]$config.PriceScore
            '@coverPhoto' = $imageSet.Hotel[0].Url
            '@gallery' = $hotelGalleryJson
            '@publishedStatus' = $publishedStatus
            '@approvalStatus' = $approvalStatus
            '@approvalDate' = $approvalDate
            '@featured' = $config.Featured
            '@recommended' = $config.Recommended
        })

        foreach ($hotelImage in $imageSet.Hotel) {
            $imageInfo = Get-ImageInfo -Path $hotelImage.Path
            Invoke-NonQuery -Connection $connection -Sql @"
INSERT INTO otel_gorselleri
(
    otel_id, gorsel_url, thumbnail_url, gorsel_turu, baslik, aciklama, kapak_fotografi_mi, one_cikan,
    siralama, boyut_kb, genislik, yukseklik, onay_durumu, onay_tarihi, olusturulma_tarihi
)
VALUES
(
    @hotelId, @imageUrl, NULL, @imageType, @title, @description, @isCover, @isFeatured,
    @displayOrder, @sizeKb, @width, @height, @approvalStatus, @approvalDate, SYSUTCDATETIME()
);
"@ -Parameters @{
                '@hotelId' = $hotelId
                '@imageUrl' = $hotelImage.Url
                '@imageType' = $hotelImage.Type
                '@title' = $hotelImage.Title
                '@description' = $hotelImage.Description
                '@isCover' = [int]$hotelImage.IsCover
                '@isFeatured' = [int]$hotelImage.IsFeatured
                '@displayOrder' = $hotelImage.Order
                '@sizeKb' = $imageInfo.SizeKb
                '@width' = $imageInfo.Width
                '@height' = $imageInfo.Height
                '@approvalStatus' = $approvalStatus
                '@approvalDate' = $approvalDate
            }
        }

        foreach ($featureId in $config.HotelFeatureIds) {
            Invoke-NonQuery -Connection $connection -Sql @"
INSERT INTO otel_ozellik_iliskileri (otel_id, ozellik_id, ek_ucret, aciklama)
VALUES (@hotelId, @featureId, 0, @description);
"@ -Parameters @{
                '@hotelId' = $hotelId
                '@featureId' = [int]$featureId
                '@description' = "$($config.Name) demo ozellik kaydi"
            }
        }

        $roomSequence = 1
        foreach ($template in $roomTemplates) {
            $roomCode = "{0}-{1:00}" -f $template.Code, $roomSequence
            $roomImages = $imageSet.Rooms[$template.Key]
            $roomGalleryJson = (($roomImages | ForEach-Object { $_.Url }) | ConvertTo-Json -Compress)

            $roomFeaturesJson = @{
                aciklama = $template.Description
                ozellikler = @('Wi-Fi', 'Smart TV', 'Kettle', 'Dus', 'Mini Bar')
            } | ConvertTo-Json -Compress

            $roomId = [int64](Invoke-Scalar -Connection $connection -Sql @"
INSERT INTO oda_tipleri
(
    otel_id, oda_tip_kodu, oda_adi, oda_kategorisi, maksimum_kisi_sayisi, maksimum_yetiskin_sayisi,
    maksimum_cocuk_sayisi, yatak_tipi, yatak_sayisi, ek_yatak_eklenebilir_mi, balkon_var_mi, ozel_banyo_var_mi,
    standart_gecelik_fiyat, haftasonu_fark_orani, cocuk_indirim_orani, bebek_ucretsiz_mi, bebek_yas_siniri,
    cocuk_yas_siniri, toplam_oda_sayisi, overbooking_limit, kapak_fotografi, galeri, ozellikler, aktif_mi, siralama,
    olusturulma_tarihi, guncellenme_tarihi
)
VALUES
(
    @hotelId, @roomCode, @roomName, @roomCategory, @maxPerson, @maxAdult, @maxChild, @bedType, @bedCount, 0, 0, 1,
    @basePrice, 0, 0, 1, 2, 12, @roomCount, 0, @coverPhoto, @gallery, @featuresJson, 1, @displayOrder,
    SYSUTCDATETIME(), SYSUTCDATETIME()
);
SELECT CAST(SCOPE_IDENTITY() AS bigint);
"@ -Parameters @{
                '@hotelId' = $hotelId
                '@roomCode' = $roomCode
                '@roomName' = $template.Name
                '@roomCategory' = $template.Category
                '@maxPerson' = $template.MaxPerson
                '@maxAdult' = $template.MaxAdult
                '@maxChild' = $template.MaxChild
                '@bedType' = $template.Bed
                '@bedCount' = $template.BedCount
                '@basePrice' = [decimal]$config.BasePrices[$template.Key]
                '@roomCount' = $template.RoomCount
                '@coverPhoto' = $roomImages[0].Url
                '@gallery' = $roomGalleryJson
                '@featuresJson' = $roomFeaturesJson
                '@displayOrder' = $roomSequence
            })

            foreach ($featureId in $template.FeatureIds) {
                Invoke-NonQuery -Connection $connection -Sql @"
INSERT INTO oda_tipi_ozellikleri (oda_tip_id, ozellik_id, miktar)
VALUES (@roomId, @featureId, 1);
"@ -Parameters @{
                    '@roomId' = $roomId
                    '@featureId' = [int]$featureId
                }
            }

            foreach ($roomImage in $roomImages) {
                $imageInfo = Get-ImageInfo -Path $roomImage.Path
                Invoke-NonQuery -Connection $connection -Sql @"
INSERT INTO oda_gorselleri
(
    oda_tip_id, gorsel_url, thumbnail_url, baslik, aciklama, kapak_fotografi_mi, siralama, boyut_kb, onay_durumu,
    onay_tarihi, olusturulma_tarihi
)
VALUES
(
    @roomId, @imageUrl, NULL, @title, @description, @isCover, @displayOrder, @sizeKb, @approvalStatus,
    @approvalDate, SYSUTCDATETIME()
);
"@ -Parameters @{
                    '@roomId' = $roomId
                    '@imageUrl' = $roomImage.Url
                    '@title' = $roomImage.Title
                    '@description' = $roomImage.Description
                    '@isCover' = [int]$roomImage.IsCover
                    '@displayOrder' = $roomImage.Order
                    '@sizeKb' = $imageInfo.SizeKb
                    '@approvalStatus' = $approvalStatus
                    '@approvalDate' = $approvalDate
                }
            }

            for ($offset = 0; $offset -lt $DaysToSeed; $offset++) {
                $date = (Get-Date).Date.AddDays($offset)
                $nightlyPrice = Get-PriceForDate -BasePrice $config.BasePrices[$template.Key] -Date $date
                $discountedPrice = $null
                $campaignLabel = $null
                if ($offset % 7 -eq 0) {
                    $discountedPrice = [int][math]::Round($nightlyPrice * 0.92)
                    $campaignLabel = 'Erken Rezervasyon'
                }
                elseif ($offset % 11 -eq 0) {
                    $discountedPrice = [int][math]::Round($nightlyPrice * 0.95)
                    $campaignLabel = 'Akilli Fiyat'
                }

                Invoke-NonQuery -Connection $connection -Sql @"
INSERT INTO oda_fiyat_musaitlik
(
    oda_tip_id, otel_id, tarih, gecelik_fiyat, indirimli_fiyat, kampanya_id, toplam_oda_sayisi,
    satilan_oda_sayisi, bloke_oda_sayisi, minimum_geceleme, maksimum_geceleme, kapali_satis,
    kampanya_etiketi, fiyat_notu, guncelleyen_kullanici_id, sadece_gunubirlik, iptal_politikasi_override, guncellenme_tarihi
)
VALUES
(
    @roomId, @hotelId, @date, @nightlyPrice, @discountedPrice, NULL, @roomCount,
    0, 0, 1, 30, 0, @campaignLabel, 'Demo takvim fiyat kaydi', 101, 0, 'Iptal kosulu giristen 24 saat oncesine kadar ucretsizdir.', SYSUTCDATETIME()
);
"@ -Parameters @{
                    '@roomId' = $roomId
                    '@hotelId' = $hotelId
                    '@date' = $date
                    '@nightlyPrice' = [decimal]$nightlyPrice
                    '@discountedPrice' = if ($null -eq $discountedPrice) { [DBNull]::Value } else { [decimal]$discountedPrice }
                    '@roomCount' = $template.RoomCount
                    '@campaignLabel' = if ($null -eq $campaignLabel) { [DBNull]::Value } else { $campaignLabel }
                }
            }

            $roomSequence++
        }
    }
}
finally {
    $connection.Close()
    $connection.Dispose()
}

Write-Output "Demo hotels seeded successfully. Total hotels targeted: $($hotelConfigs.Count + 1)"
