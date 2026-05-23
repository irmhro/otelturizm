param(
    [string]$EmailRoot = "d:\otelturizm\Views\Email",
    [string]$OutDir = (Join-Path $PSScriptRoot "rendered"),
    [string[]]$Langs = @("tr", "en", "de", "fr", "es", "ru", "ar"),
    [string]$Lang = ""
)

$ErrorActionPreference = "Stop"
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null

if (-not [string]::IsNullOrWhiteSpace($Lang)) {
    $Langs = @($Lang.Trim().ToLowerInvariant())
}

$tokens = @{
    "user_first_name" = "Test"
    "guest_full_name" = "Test Misafir"
    "guest_email" = "guest@example.com"
    "guest_phone" = "+905551112233"
    "hotel_manager_name" = "Yetkili"
    "room_count" = "2"
    "cancel_reason" = "Test iptal sebebi"
    "user_email" = "test@example.com"
    "verification_code" = "123456"
    "verification_channel" = "e-posta"
    "login_time" = "27.04.2026 20:00"
    "registration_date" = "27.04.2026"
    "verification_link" = "https://otelturizm.com/eposta-dogrula"
    "reset_link" = "https://otelturizm.com/sifre-sifirla"
    "request_ip" = "127.0.0.1"
    "booking_reference" = "OT-0001"
    "hotel_name" = "Demo Otel"
    "hotel_address" = "Istanbul"
    "check_in_date" = "01.05.2026"
    "check_out_date" = "03.05.2026"
    "room_type_name" = "Classic Room"
    "total_price" = "9999"
    "booking_details_link" = "https://otelturizm.com/panel/rezervasyonlar/OT-0001"
    "invoice_download_link" = "https://otelturizm.com/panel/faturalar/OT-0001"
    "recipient_name" = "Test"
    "module_label" = "Hesap"
    "contract_bundle_title" = "Sozlesme ve KVKK Paketiniz"
    "contract_sections_html" = "<p>Ornek icerik</p>"
    "primary_contract_url" = "https://otelturizm.com/sozlesmeler"
}

function Get-EmailLayoutMeta {
    param([string]$TemplateContent)

    $meta = @{
        EmailTitle = "Otelturizm"
        Preheader = ""
        HeaderTagline = "OTELTURIZM"
        HeaderTitle = ""
        HeaderSubtitle = ""
        FooterLine1 = ""
        FooterLine2 = ""
        FooterLegal = ""
        EmailLang = ""
    }

    if ($TemplateContent -notmatch "Layout:\s*_EmailMaster") {
        return $null
    }

    $bodyLines = New-Object 'System.Collections.Generic.List[string]'
    foreach ($line in ($TemplateContent -replace "`r`n", "`n" -split "`n")) {
        $trimmed = $line.Trim()
        if ($trimmed -match '^\@\*(.+)\*\@$') {
            $inner = $Matches[1].Trim()
            if ($inner -match '^Layout:\s*_EmailMaster') { continue }
            if ($inner -match '^([^:]+):\s*(.+)$') {
                $key = $Matches[1].Trim().ToLowerInvariant()
                $value = $Matches[2].Trim()
                switch ($key) {
                    "emailtitle" { $meta.EmailTitle = $value }
                    "preheader" { $meta.Preheader = $value }
                    "headertagline" { $meta.HeaderTagline = $value }
                    "headertitle" { $meta.HeaderTitle = $value }
                    "headersubtitle" { $meta.HeaderSubtitle = $value }
                    "footerline1" { $meta.FooterLine1 = $value }
                    "footerline2" { $meta.FooterLine2 = $value }
                    "footerlegal" { $meta.FooterLegal = $value }
                    "emaillang" { $meta.EmailLang = $value }
                }
            }
            continue
        }
        $bodyLines.Add($line) | Out-Null
    }

    return @{
        Meta = $meta
        Body = ($bodyLines -join [Environment]::NewLine).TrimStart()
    }
}

function Merge-EmailMaster {
    param(
        [string]$TemplateContent,
        [string]$LangCode,
        [string]$EmailRoot
    )

    $parsed = Get-EmailLayoutMeta -TemplateContent $TemplateContent
    if ($null -eq $parsed) {
        return $TemplateContent
    }

    $masterPath = Join-Path $EmailRoot "_EmailMaster.cshtml"
    if (-not (Test-Path -LiteralPath $masterPath)) {
        return $TemplateContent
    }

    $emailLang = if ([string]::IsNullOrWhiteSpace($parsed.Meta.EmailLang)) { $LangCode } else { $parsed.Meta.EmailLang }
    $isRtl = ($emailLang -eq "ar")
    $master = Get-Content -LiteralPath $masterPath -Raw
    $master = $master.Replace("{{Body}}", $parsed.Body)
    $master = $master.Replace("{{email_lang}}", $emailLang)
    $master = $master.Replace("{{email_dir}}", $(if ($isRtl) { "rtl" } else { "ltr" }))
    $master = $master.Replace("{{email_rtl_class}}", $(if ($isRtl) { "email-rtl" } else { "" }))
    $master = $master.Replace("{{email_title}}", $parsed.Meta.EmailTitle)
    $master = $master.Replace("{{email_preheader}}", $parsed.Meta.Preheader)
    $master = $master.Replace("{{email_header_tagline}}", $parsed.Meta.HeaderTagline)
    $master = $master.Replace("{{email_header_title}}", $parsed.Meta.HeaderTitle)
    $master = $master.Replace("{{email_header_subtitle}}", $parsed.Meta.HeaderSubtitle)
    $master = $master.Replace("{{email_footer_line1}}", $parsed.Meta.FooterLine1)
    $master = $master.Replace("{{email_footer_line2}}", $parsed.Meta.FooterLine2)
    $master = $master.Replace("{{email_footer_legal}}", $parsed.Meta.FooterLegal)
    $master = $master.Replace("{{email_footer_year}}", [string](Get-Date).Year)
    return $master
}

function Replace-EmailTokens {
    param(
        [string]$Content,
        [hashtable]$TokenMap
    )

    foreach ($k in $TokenMap.Keys) {
        $needle = "{{${k}}}"
        $replacement = [string]$TokenMap[$k]
        $Content = [regex]::Replace($Content, [regex]::Escape($needle), [System.Text.RegularExpressions.MatchEvaluator]{ param($m) $replacement }, "IgnoreCase")
    }
    return $Content
}

$allMissing = New-Object 'System.Collections.Generic.List[string]'
$totalRendered = 0

foreach ($lang in $Langs) {
    $folder = Join-Path $EmailRoot $lang
    if (-not (Test-Path -LiteralPath $folder)) {
        Write-Warning "Skipping missing lang folder: $folder"
        continue
    }

    $files = Get-ChildItem -LiteralPath $folder -Filter *.cshtml -File
    foreach ($f in $files) {
        $content = Get-Content -LiteralPath $f.FullName -Raw
        $content = Merge-EmailMaster -TemplateContent $content -LangCode $lang -EmailRoot $EmailRoot
        $content = Replace-EmailTokens -Content $content -TokenMap $tokens

        $out = Join-Path $OutDir ($lang + "-" + $f.Name.Replace(".cshtml", ".html"))
        Set-Content -LiteralPath $out -Value $content -Encoding UTF8
        $totalRendered++

        if ($content -match "\{\{[a-zA-Z0-9_\-]+\}\}") {
            $allMissing.Add("$lang/$($f.Name)")
        }
    }
}

Write-Host "Rendered: $totalRendered templates across $($Langs.Count) language(s)."
if ($allMissing.Count -gt 0) {
    Write-Host "Templates with unreplaced tokens:"
    $allMissing | Sort-Object | ForEach-Object { Write-Host " - $_" }
    exit 2
}

Write-Host "OK: all tokens replaced (for the sample set)."
