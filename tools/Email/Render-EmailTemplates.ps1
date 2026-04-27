param(
    [string]$EmailRoot = "d:\otelturizm\Views\Email",
    [string]$OutDir = (Join-Path $PSScriptRoot "rendered"),
    [string]$Lang = "tr"
)

$ErrorActionPreference = "Stop"
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null

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
    "hotel_address" = "İstanbul"
    "check_in_date" = "01.05.2026"
    "check_out_date" = "03.05.2026"
    "room_type_name" = "Classic Room"
    "total_price" = "9999"
    "booking_details_link" = "https://otelturizm.com/panel/rezervasyonlar/OT-0001"
    "recipient_name" = "Test"
    "module_label" = "Hesap"
    "contract_bundle_title" = "Sözleşme ve KVKK Paketiniz"
    "contract_sections_html" = "<p>Örnek içerik</p>"
    "primary_contract_url" = "https://otelturizm.com/sozlesmeler"
}

$folder = Join-Path $EmailRoot $Lang
if (-not (Test-Path $folder)) { throw "Email lang folder not found: $folder" }

$files = Get-ChildItem -LiteralPath $folder -Filter *.cshtml -File
$missing = New-Object 'System.Collections.Generic.List[string]'

foreach ($f in $files) {
    $content = Get-Content -LiteralPath $f.FullName -Raw
    foreach ($k in $tokens.Keys) {
        $needle = "{{${k}}}"
        $replacement = [string]$tokens[$k]
        # PowerShell string.Replace overloads vary; use regex replace for case-insensitive token replacement.
        $content = [regex]::Replace($content, [regex]::Escape($needle), [System.Text.RegularExpressions.MatchEvaluator]{ param($m) $replacement }, "IgnoreCase")
    }

    $out = Join-Path $OutDir ($Lang + "-" + $f.Name.Replace(".cshtml",".html"))
    Set-Content -LiteralPath $out -Value $content -Encoding UTF8

    if ($content -match "\{\{[a-zA-Z0-9_\-]+\}\}") {
        $missing.Add($f.Name)
    }
}

Write-Host "Rendered: $($files.Count)"
if ($missing.Count -gt 0) {
    Write-Host "Templates with unreplaced tokens:"
    $missing | Sort-Object | ForEach-Object { Write-Host " - $_" }
    exit 2
}

Write-Host "OK: all tokens replaced (for the sample set)."

