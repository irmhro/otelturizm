Add-Type -AssemblyName System.Drawing

$root = Split-Path -Parent $PSScriptRoot
$projectRoot = Split-Path -Parent $root
$outputRoot = Join-Path $projectRoot 'wwwroot\uploads\demo\campaigns'

$campaigns = @(
    @{ Slug = 'yilbasi-ozel'; Title = 'Yilbasi Ozel'; Slogan = 'Yeni yila canli fiyat, hizli rezervasyon ve premium kacislarla girin.'; Colors = @('#081b3a', '#0b5fd7', '#f97316') },
    @{ Slug = 'sevgililer-gunu-romantik-kacis'; Title = 'Sevgililer Gunu'; Slogan = 'Romantik kacislar, ozel suitler ve unutulmayacak hafta sonlari sizi bekliyor.'; Colors = @('#3b0a28', '#c2185b', '#ff8a65') },
    @{ Slug = 'erken-rezervasyon-avantaji'; Title = 'Erken Rezervasyon'; Slogan = 'Planini simdiden yap, secili otellerde avantajli fiyatlari bugunden yakala.'; Colors = @('#0f172a', '#1d4ed8', '#22c55e') },
    @{ Slug = 'akilli-fiyat-seckisi'; Title = 'Akilli Fiyat'; Slogan = 'Fiyat-performans dengesi yuksek tesisleri tek ekranda karsilastirin.'; Colors = @('#132238', '#0f766e', '#38bdf8') },
    @{ Slug = 'bayram-tatili-seckisi'; Title = 'Bayram Tatili'; Slogan = 'Bayram donemi icin secili otellerde sehir, aile ve sahil secenekleri.'; Colors = @('#3f1d12', '#d97706', '#fde68a') },
    @{ Slug = 'flash-indirim'; Title = 'Flash Indirim'; Slogan = 'Kisa sureli fiyat dususlerini kacirmadan rezervasyona gecebilirsiniz.'; Colors = @('#111827', '#2563eb', '#ef4444') },
    @{ Slug = 'ay-sonu-ozel'; Title = 'Ay Sonu Ozel'; Slogan = 'Ay sonu planlari icin secili tesislerde hizli kampanya secimi.'; Colors = @('#172554', '#1d4ed8', '#f59e0b') },
    @{ Slug = 'ultra-luks-secki'; Title = 'Ultra Luks'; Slogan = 'Premium segment, ozel hizmet ve yuksek puanli oteller bir arada.'; Colors = @('#111827', '#4f46e5', '#eab308') },
    @{ Slug = 'anneler-gunu-kacamagi'; Title = 'Anneler Gunu'; Slogan = 'Sehir kacamagi, wellness ve rahatlatan konaklama secenekleri burada.'; Colors = @('#3f0d2e', '#db2777', '#f9a8d4') },
    @{ Slug = 'gece-yarisi-flas-fiyat'; Title = 'Gece Yarisi Fiyat'; Slogan = 'Gece saatlerinde acilan ozel fiyat pencereleri ile hizli karar verin.'; Colors = @('#020617', '#1e3a8a', '#38bdf8') },
    @{ Slug = 'hafta-sonu-firsatlari'; Title = 'Hafta Sonu Firsatlari'; Slogan = 'Kisa kacamak, hizli ulasim ve hafta sonuna hazir secili tesisler.'; Colors = @('#0f172a', '#0369a1', '#22c55e') },
    @{ Slug = 'havuz-keyfi-kampanyasi'; Title = 'Havuz Keyfi'; Slogan = 'Havuz, gunes ve dinlenme odakli otellerde secili kampanya avantaji.'; Colors = @('#083344', '#0891b2', '#67e8f9') },
    @{ Slug = 'butceye-uygun-oteller'; Title = 'Butceye Uygun'; Slogan = 'Ekonomik fiyat bandinda kaliteyi koruyan secenekleri karsilastirin.'; Colors = @('#052e16', '#16a34a', '#bef264') },
    @{ Slug = 'spa-ve-wellness-gunleri'; Title = 'Spa ve Wellness'; Slogan = 'Dinlenme, yenilenme ve sessiz kacis arayanlar icin secili tesisler.'; Colors = @('#082f49', '#0f766e', '#a7f3d0') },
    @{ Slug = 'sehir-kacamagi'; Title = 'Sehir Kacamagi'; Slogan = 'Merkezi lokasyon, kolay ulasim ve dinamik konaklama planlari.'; Colors = @('#172554', '#2563eb', '#60a5fa') },
    @{ Slug = 'mobil-uygulama-ozel'; Title = 'Mobil Ozel'; Slogan = 'Mobilde hizli aksiyon alin, secili kampanya fiyatlarini kacirmayin.'; Colors = @('#1e1b4b', '#7c3aed', '#22d3ee') },
    @{ Slug = 'uzun-konaklama-avantaji'; Title = 'Uzun Konaklama'; Slogan = 'Birden fazla gece planlayanlar icin daha esnek fiyat avantajlari.'; Colors = @('#083344', '#0f766e', '#facc15') },
    @{ Slug = 'sadik-misafir-avantaji'; Title = 'Sadik Misafir'; Slogan = 'Tekrar gelen misafirler icin vitrine cikan secili konaklama teklifleri.'; Colors = @('#172554', '#7c3aed', '#f472b6') },
    @{ Slug = 'aile-kacamagi'; Title = 'Aile Kacamagi'; Slogan = 'Cocuk dostu ve ferah odali tesisleri aile planlariniz icin listeleyin.'; Colors = @('#14532d', '#22c55e', '#fde047') },
    @{ Slug = 'seyahat-planlama-asistani-seckisi'; Title = 'Seyahat Planlama'; Slogan = 'Karar vermeyi kolaylastiran secili tesis listesi ve hizli rota fikri.'; Colors = @('#0f172a', '#334155', '#fb7185') }
)

function New-Brushes([string[]]$colors, [System.Drawing.Rectangle]$bounds) {
    $brush = New-Object System.Drawing.Drawing2D.LinearGradientBrush($bounds, [System.Drawing.ColorTranslator]::FromHtml($colors[0]), [System.Drawing.ColorTranslator]::FromHtml($colors[1]), 35.0)
    $blend = New-Object System.Drawing.Drawing2D.ColorBlend
    $blend.Positions = @(0.0, 0.6, 1.0)
    $blend.Colors = @(
        [System.Drawing.ColorTranslator]::FromHtml($colors[0]),
        [System.Drawing.ColorTranslator]::FromHtml($colors[1]),
        [System.Drawing.ColorTranslator]::FromHtml($colors[2])
    )
    $brush.InterpolationColors = $blend
    return $brush
}

foreach ($campaign in $campaigns) {
    $campaignFolder = Join-Path $outputRoot $campaign.Slug
    New-Item -ItemType Directory -Path $campaignFolder -Force | Out-Null
    $filePath = Join-Path $campaignFolder 'campaign-hero.png'

    $bitmap = New-Object System.Drawing.Bitmap 1600, 900
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
    $graphics.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit

    $bounds = New-Object System.Drawing.Rectangle 0, 0, 1600, 900
    $brush = New-Brushes $campaign.Colors $bounds
    $graphics.FillRectangle($brush, $bounds)

    $circleBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(35, 255, 255, 255))
    $graphics.FillEllipse($circleBrush, 980, 70, 430, 430)
    $graphics.FillEllipse($circleBrush, 1120, 410, 320, 320)
    $graphics.FillEllipse($circleBrush, 850, 520, 220, 220)

    $panelBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(60, 7, 14, 28))
    $graphics.FillRectangle($panelBrush, 90, 110, 840, 650)

    $badgeBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(220, 255, 255, 255))
    $badgeRect = New-Object System.Drawing.RectangleF 120, 142, 260, 56
    $graphics.FillRectangle($badgeBrush, $badgeRect.X, $badgeRect.Y, $badgeRect.Width, $badgeRect.Height)

    $badgeFont = New-Object System.Drawing.Font('Segoe UI', 16, [System.Drawing.FontStyle]::Bold)
    $titleFont = New-Object System.Drawing.Font('Segoe UI Semibold', 58, [System.Drawing.FontStyle]::Bold)
    $bodyFont = New-Object System.Drawing.Font('Segoe UI', 26, [System.Drawing.FontStyle]::Regular)
    $ctaFont = New-Object System.Drawing.Font('Segoe UI', 18, [System.Drawing.FontStyle]::Bold)
    $whiteBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 255, 255, 255))
    $darkBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 18, 24, 39))
    $mutedBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(230, 227, 232, 240))
    $outlinePen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(55, 255, 255, 255), 2)

    $graphics.DrawString('Canli Kampanya', $badgeFont, $darkBrush, 146, 156)
    $graphics.DrawString($campaign.Title, $titleFont, $whiteBrush, 118, 250)
    $graphics.DrawString($campaign.Slogan, $bodyFont, $mutedBrush, (New-Object System.Drawing.RectangleF(122, 395, 700, 160)))

    $ctaRect = New-Object System.Drawing.RectangleF 124, 612, 280, 58
    $graphics.DrawRectangle($outlinePen, $ctaRect.X, $ctaRect.Y, $ctaRect.Width, $ctaRect.Height)
    $graphics.DrawString('Secili otelleri listele', $ctaFont, $whiteBrush, 152, 628)

    $graphics.DrawString('Otelturizm kampanya vitrini', (New-Object System.Drawing.Font('Segoe UI', 18, [System.Drawing.FontStyle]::Regular)), $mutedBrush, 124, 706)

    $bitmap.Save($filePath, [System.Drawing.Imaging.ImageFormat]::Png)

    $outlinePen.Dispose()
    $mutedBrush.Dispose()
    $darkBrush.Dispose()
    $whiteBrush.Dispose()
    $ctaFont.Dispose()
    $bodyFont.Dispose()
    $titleFont.Dispose()
    $badgeFont.Dispose()
    $badgeBrush.Dispose()
    $panelBrush.Dispose()
    $circleBrush.Dispose()
    $brush.Dispose()
    $graphics.Dispose()
    $bitmap.Dispose()
}
